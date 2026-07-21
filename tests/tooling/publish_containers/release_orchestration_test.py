import json
import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PUBLISH_SCRIPT = REPO_ROOT / "tools" / "publish-release.ps1"
PACK_SCRIPT = REPO_ROOT / "tools" / "pack-release.ps1"
VERSION = "1.2.3-test.1"


def write_executable(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")
    os.chmod(path, 0o755)


def write_fake_pwsh(directory: Path) -> None:
    script = directory / "fake_pwsh.py"
    script.write_text(
        textwrap.dedent(
            """
            import json
            import os
            import sys
            from pathlib import Path

            args = sys.argv[1:]
            try:
                script_path = Path(args[args.index("-File") + 1]).name
            except (ValueError, IndexError):
                print(f"unexpected pwsh arguments: {args}", file=sys.stderr)
                sys.exit(97)

            with open(os.environ["FAKE_ORCHESTRATION_LOG"], "a", encoding="utf-8") as log:
                log.write(json.dumps({"command": "pwsh", "script": script_path, "args": args}) + "\\n")

            def argument(name: str, default: str) -> str:
                try:
                    return args[args.index(name) + 1]
                except (ValueError, IndexError):
                    return default

            def write_summary(path: Path, artifact_kind: str, status: str, family: str) -> None:
                path.parent.mkdir(parents=True, exist_ok=True)
                failed = [] if status == "succeeded" else [{"package": family, "exitCode": 42, "error": f"{family} synthetic failure"}]
                pushed = [f"{family}-member"] if status in {"succeeded", "partial-publish"} else []
                path.write_text(
                    json.dumps(
                        {
                            "schemaVersion": 1,
                            "artifactKind": artifact_kind,
                            "status": status,
                            "version": os.environ["FAKE_RELEASE_VERSION"],
                            "packageDirectory": str(path.parent),
                            "source": "synthetic",
                            "pushed": pushed,
                            "failed": failed,
                            "notAttempted": [],
                        }
                    ),
                    encoding="utf-8",
                )

            if script_path == "validate-release-packages.ps1":
                if "-PackageDirectory" in args and os.environ.get("FAKE_PACKAGE_VALIDATION_STATUS") == "failed":
                    sys.exit(42)
                sys.exit(0)

            if script_path == "publish-nuget.ps1":
                package_directory = Path(argument("-PackageDirectory", "artifacts/packages/release"))
                if not package_directory.is_absolute():
                    package_directory = Path.cwd() / package_directory
                status = os.environ.get("FAKE_NUGET_STATUS", "succeeded")
                write_summary(package_directory / "publish-summary.json", "nuget-packages", status, "nuget")
                sys.exit(0 if status == "succeeded" else 42)

            if script_path == "publish-containers.ps1":
                output = Path(argument("-OutputDirectory", "artifacts/containers/release"))
                if not output.is_absolute():
                    output = Path.cwd() / output
                output.mkdir(parents=True, exist_ok=True)
                is_push = "-Push" in args
                if is_push:
                    status = os.environ.get("FAKE_CONTAINER_STATUS", "succeeded")
                    write_summary(output / "publish-summary.json", "container-images", status, "container")
                    sys.exit(0 if status == "succeeded" else 43)

                (output / "server.tar.gz").write_bytes(b"server")
                (output / "mcp.tar.gz").write_bytes(b"mcp")
                (output / "access-telemetry.tar.gz").write_bytes(b"access-telemetry")
                (output / "access-telemetry-clock.tar.gz").write_bytes(b"access-telemetry-clock")
                (output / "production-deployment.yaml").write_text(
                    "kind: List\\nreleaseVersion: " + os.environ["FAKE_RELEASE_VERSION"] + "\\n",
                    encoding="utf-8",
                )
                write_summary(output / "build-summary.json", "container-images", "succeeded", "container")
                sys.exit(0)

            print(f"unexpected child script: {script_path}", file=sys.stderr)
            sys.exit(98)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "pwsh",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )
    (directory / "pwsh.cmd").write_text(
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )


def write_fake_dotnet(directory: Path) -> None:
    script = directory / "fake_dotnet.py"
    script.write_text(
        textwrap.dedent(
            """
            import json
            import os
            import sys

            args = sys.argv[1:]
            if args and args[0].endswith("pwsh.dll"):
                real_dotnet = os.environ["REAL_DOTNET"]
                os.execv(real_dotnet, [real_dotnet] + args)
            with open(os.environ["FAKE_ORCHESTRATION_LOG"], "a", encoding="utf-8") as log:
                log.write(json.dumps({"command": "dotnet", "args": args}) + "\\n")
            sys.exit(0)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )
    write_executable(
        directory / "dotnet",
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
    )


def make_environment(
    root: Path,
    *,
    nuget_status: str = "succeeded",
    container_status: str = "succeeded",
    package_validation_status: str = "succeeded",
) -> tuple[dict[str, str], Path]:
    fake_bin = root / "bin"
    fake_bin.mkdir()
    write_fake_pwsh(fake_bin)
    write_fake_dotnet(fake_bin)
    log_path = root / "orchestration.jsonl"
    env = os.environ.copy()
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
    env["FAKE_ORCHESTRATION_LOG"] = str(log_path)
    env["FAKE_RELEASE_VERSION"] = VERSION
    env["FAKE_NUGET_STATUS"] = nuget_status
    env["FAKE_CONTAINER_STATUS"] = container_status
    env["FAKE_PACKAGE_VALIDATION_STATUS"] = package_validation_status
    env["FAKE_PWSH"] = str(fake_bin / "pwsh")
    return env, log_path


def read_log(path: Path) -> list[dict[str, object]]:
    return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]


class ReleaseOrchestrationTests(unittest.TestCase):
    def test_publish_release_attempts_both_families_and_writes_aggregate_partial_state(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, log_path = make_environment(root, nuget_status="publish-failed", container_status="succeeded")
            package_directory = root / "packages"
            container_directory = root / "containers"
            summary_directory = root / "release"

            result = subprocess.run(
                [
                    shutil.which("pwsh") or "pwsh", "-NoLogo", "-NoProfile", "-File", str(PUBLISH_SCRIPT),
                    "-Version", VERSION,
                    "-PackageDirectory", str(package_directory),
                    "-ContainerOutputDirectory", str(container_directory),
                    "-SummaryDirectory", str(summary_directory),
                    "-PowerShellExecutable", env["FAKE_PWSH"],
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertNotEqual(0, result.returncode)
            scripts = [entry["script"] for entry in read_log(log_path) if entry["command"] == "pwsh"]
            self.assertEqual(["publish-nuget.ps1", "publish-containers.ps1"], scripts)
            summary_path = summary_directory / "publish-summary.json"
            summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])
            self.assertEqual(["NuGet packages", "container images"], [family["name"] for family in summary["families"]])
            self.assertTrue(any("NuGet packages" in failure["package"] for failure in summary["failed"]))

    def test_publish_release_requires_both_families_for_success(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, _ = make_environment(root, nuget_status="succeeded", container_status="publish-failed")
            package_directory = root / "packages"
            container_directory = root / "containers"
            summary_directory = root / "release"

            result = subprocess.run(
                [
                    shutil.which("pwsh") or "pwsh", "-NoLogo", "-NoProfile", "-File", str(PUBLISH_SCRIPT),
                    "-Version", VERSION,
                    "-PackageDirectory", str(package_directory),
                    "-ContainerOutputDirectory", str(container_directory),
                    "-SummaryDirectory", str(summary_directory),
                    "-PowerShellExecutable", env["FAKE_PWSH"],
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((summary_directory / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])

    def test_pack_release_prebuilds_containers_and_copies_versioned_deployment_asset(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, log_path = make_environment(root)
            package_output = root / "packages"
            container_output = root / "containers"
            deployment_output = root / "deployment" / "hexalith-memories-production.yaml"

            result = subprocess.run(
                [
                    shutil.which("pwsh") or "pwsh",
                    "-NoLogo",
                    "-NoProfile",
                    "-File",
                    str(PACK_SCRIPT),
                    "-Version",
                    VERSION,
                    "-OutputDirectory",
                    str(package_output),
                    "-ContainerOutputDirectory",
                    str(container_output),
                    "-DeploymentOutputPath",
                    str(deployment_output),
                    "-PowerShellExecutable",
                    env["FAKE_PWSH"],
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertGreater((container_output / "server.tar.gz").stat().st_size, 0)
            self.assertGreater((container_output / "mcp.tar.gz").stat().st_size, 0)
            self.assertGreater((container_output / "access-telemetry.tar.gz").stat().st_size, 0)
            self.assertGreater((container_output / "access-telemetry-clock.tar.gz").stat().st_size, 0)
            self.assertEqual(
                (container_output / "production-deployment.yaml").read_text(encoding="utf-8"),
                deployment_output.read_text(encoding="utf-8"),
            )
            child_scripts = [entry["script"] for entry in read_log(log_path) if entry["command"] == "pwsh"]
            self.assertIn("publish-containers.ps1", child_scripts)
            publish_call = next(entry for entry in read_log(log_path) if entry.get("script") == "publish-containers.ps1")
            self.assertNotIn("-Push", publish_call["args"])

    def test_pack_release_package_only_stops_after_real_package_validation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, log_path = make_environment(root)
            package_output = root / "packages"

            result = subprocess.run(
                [
                    shutil.which("pwsh") or "pwsh",
                    "-NoLogo",
                    "-NoProfile",
                    "-File",
                    str(PACK_SCRIPT),
                    "-Version",
                    VERSION,
                    "-OutputDirectory",
                    str(package_output),
                    "-PackageOnly",
                    "-PowerShellExecutable",
                    env["FAKE_PWSH"],
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            entries = read_log(log_path)
            child_scripts = [entry["script"] for entry in entries if entry["command"] == "pwsh"]
            self.assertEqual(
                ["validate-release-packages.ps1", "validate-release-packages.ps1"],
                child_scripts,
            )
            generated_validation = [
                entry for entry in entries if entry.get("script") == "validate-release-packages.ps1"
            ][-1]
            self.assertIn("-PackageDirectory", generated_validation["args"])
            self.assertIn("-Version", generated_validation["args"])
            self.assertNotIn("publish-containers.ps1", child_scripts)
            dotnet_commands = [entry["args"][0] for entry in entries if entry["command"] == "dotnet"]
            self.assertEqual(["build"] + ["pack"] * 9, dotnet_commands)

    def test_pack_release_package_only_propagates_generated_validation_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            env, log_path = make_environment(root, package_validation_status="failed")
            package_output = root / "packages"

            result = subprocess.run(
                [
                    shutil.which("pwsh") or "pwsh",
                    "-NoLogo",
                    "-NoProfile",
                    "-File",
                    str(PACK_SCRIPT),
                    "-Version",
                    VERSION,
                    "-OutputDirectory",
                    str(package_output),
                    "-PackageOnly",
                    "-PowerShellExecutable",
                    env["FAKE_PWSH"],
                ],
                cwd=REPO_ROOT,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

            self.assertNotEqual(0, result.returncode)
            self.assertIn("Generated package validation failed", result.stdout + result.stderr)
            child_scripts = [
                entry["script"] for entry in read_log(log_path) if entry["command"] == "pwsh"
            ]
            self.assertEqual(
                ["validate-release-packages.ps1", "validate-release-packages.ps1"],
                child_scripts,
            )
            self.assertNotIn("publish-containers.ps1", child_scripts)


if __name__ == "__main__":
    unittest.main()
