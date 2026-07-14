import json
import os
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest
import zipfile
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "publish-nuget.ps1"
PACKAGE_IDS = [
    "Hexalith.Memories.Aspire",
    "Hexalith.Memories.Contracts",
    "Hexalith.Memories.Client.Rest",
    "Hexalith.Memories.Redis",
    "Hexalith.Memories.Cli",
    "Hexalith.Memories.Mcp",
    "Hexalith.Memories.ServiceDefaults",
    "Hexalith.Memories.EventStore",
    "Hexalith.Memories.Telemetry",
]


def create_package(directory: Path, package_id: str, version: str) -> Path:
    package = directory / f"{package_id}.{version}.nupkg"
    dependencies = ""
    if package_id == "Hexalith.Memories.Mcp":
        dependencies = f"""
    <dependencies>
      <group targetFramework="net10.0">
        <dependency id="Hexalith.Memories.ServiceDefaults" version="[{version}, )" />
      </group>
    </dependencies>"""
    nuspec = f"""<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>{package_id}</id>
    <version>{version}</version>
    <description>Test package</description>
    <authors>ITANEO</authors>
    <license type="expression">MIT</license>
    <projectUrl>https://github.com/Hexalith/Hexalith.Memories</projectUrl>
    <repository type="git" url="https://github.com/Hexalith/Hexalith.Memories" />
    <tags>hexalith memories test</tags>
    <readme>README.md</readme>{dependencies}
  </metadata>
</package>
"""
    with zipfile.ZipFile(package, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", nuspec)
        archive.writestr("README.md", "# Test\n")
    return package


def create_packages(directory: Path, version: str = "1.2.3") -> list[Path]:
    packages = [create_package(directory, package_id, version) for package_id in PACKAGE_IDS]
    return sorted(packages, key=lambda item: item.name)


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

            plan_path = os.environ["FAKE_DOTNET_PLAN"]
            log_path = os.environ["FAKE_DOTNET_LOG"]
            package = None
            if len(args) >= 3 and args[0:2] == ["nuget", "push"]:
                package = os.path.basename(args[2])

            with open(log_path, "a", encoding="utf-8") as log:
                log.write(json.dumps({"package": package, "args": args}) + "\\n")

            with open(plan_path, "r", encoding="utf-8") as plan_file:
                plan = json.load(plan_file)

            outcome = plan.get(package, {"exitCode": 0, "stdout": "pushed", "stderr": ""})
            if outcome.get("stdout"):
                print(outcome["stdout"])
            if outcome.get("stderr"):
                print(outcome["stderr"], file=sys.stderr)
            sys.exit(int(outcome.get("exitCode", 0)))
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )

    (directory / "dotnet.cmd").write_text(
        f"@echo off\r\n\"{sys.executable}\" \"{script}\" %*\r\n",
        encoding="utf-8",
    )

    (directory / "dotnet").write_text(
        f"#!/usr/bin/env sh\nexec \"{sys.executable}\" \"{script}\" \"$@\"\n",
        encoding="utf-8",
    )
    os.chmod(directory / "dotnet", 0o755)


def write_fake_gh(directory: Path) -> None:
    script = directory / "fake_gh.py"
    script.write_text(
        textwrap.dedent(
            """
            import json
            import os
            import sys

            args = sys.argv[1:]
            log_path = os.environ["FAKE_GH_LOG"]
            body = None
            if "--body-file" in args:
                body_file = args[args.index("--body-file") + 1]
                with open(body_file, "r", encoding="utf-8-sig") as body_handle:
                    body = body_handle.read()
            with open(log_path, "a", encoding="utf-8") as log:
                log.write(json.dumps({"args": args, "body": body}) + "\\n")

            if args[:3] == ["issue", "list", "--repo"]:
                title = os.environ.get("FAKE_GH_EXISTING_TITLE", "")
                if title:
                    print(json.dumps([{"number": 17, "title": title}]))
                else:
                    print("[]")
                sys.exit(0)

            if args[:2] in (["issue", "create"], ["issue", "comment"]):
                print("ok")
                sys.exit(0)

            print(f"unexpected gh args: {args}", file=sys.stderr)
            sys.exit(2)
            """
        ).strip()
        + "\n",
        encoding="utf-8",
    )

    (directory / "gh.cmd").write_text(
        f"@echo off\r\n\"{sys.executable}\" \"{script}\" %*\r\n",
        encoding="utf-8",
    )

    (directory / "gh").write_text(
        f"#!/usr/bin/env sh\nexec \"{sys.executable}\" \"{script}\" \"$@\"\n",
        encoding="utf-8",
    )
    os.chmod(directory / "gh", 0o755)


def run_publish(
    package_directory: Path,
    fake_bin: Path,
    plan: dict[str, dict[str, object]],
    *,
    github_actions: bool = True,
    api_key: str = "SECRET_NUGET_API_KEY_SHOULD_NOT_LEAK",
) -> tuple[subprocess.CompletedProcess[str], Path, Path]:
    plan_path = package_directory.parent / "dotnet-plan.json"
    log_path = package_directory.parent / "dotnet-calls.jsonl"
    summary_path = package_directory.parent / "step-summary.md"
    plan_path.write_text(json.dumps(plan), encoding="utf-8")

    env = os.environ.copy()
    env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_DOTNET_PLAN"] = str(plan_path)
    env["FAKE_DOTNET_LOG"] = str(log_path)
    env["NUGET_API_KEY"] = api_key
    if github_actions:
        env["GITHUB_ACTIONS"] = "true"
        env["GITHUB_STEP_SUMMARY"] = str(summary_path)

    result = subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(SCRIPT),
            "-Version",
            "1.2.3",
            "-PackageDirectory",
            str(package_directory),
        ],
        cwd=REPO_ROOT,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )
    return result, log_path, summary_path


def write_partial_summary(path: Path) -> None:
    summary = {
        "schemaVersion": 1,
        "status": "partial-publish",
        "version": "1.2.3",
        "packageDirectory": "artifacts/packages/release",
        "source": "https://api.nuget.org/v3/index.json",
        "startedAt": "2026-05-02T00:00:00.0000000Z",
        "completedAt": "2026-05-02T00:01:00.0000000Z",
        "pushed": ["Hexalith.Memories.Cli.1.2.3.nupkg"],
        "failed": [
            {
                "package": "Hexalith.Memories.Contracts.1.2.3.nupkg",
                "exitCode": 42,
                "error": "network failure",
            }
        ],
        "notAttempted": [
            {
                "package": "Hexalith.Memories.Telemetry.1.2.3.nupkg",
                "reason": "unrecoverable auth failure",
            }
        ],
        "recovery": "See docs/dev/release-runbook.md. If the exact tag or any package exists, dispatch Recover Partial Release and do not republish packages.",
    }
    path.write_text(json.dumps(summary), encoding="utf-8")


def write_publish_failed_summary(path: Path) -> None:
    summary = {
        "schemaVersion": 1,
        "status": "publish-failed",
        "version": "1.2.3",
        "packageDirectory": "artifacts/packages/release",
        "source": "https://api.nuget.org/v3/index.json",
        "startedAt": "2026-05-02T00:00:00.0000000Z",
        "completedAt": "2026-05-02T00:01:00.0000000Z",
        "pushed": [],
        "failed": [
            {
                "package": "Hexalith.Memories.Contracts.1.2.3.nupkg",
                "exitCode": 401,
                "error": "auth failure",
            }
        ],
        "notAttempted": [],
        "recovery": "See docs/dev/release-runbook.md. If the exact tag or any package exists, dispatch Recover Partial Release and do not republish packages.",
    }
    path.write_text(json.dumps(summary), encoding="utf-8")


def run_issue_helper(
    summary_path: Path,
    fake_bin: Path,
    *,
    existing_title: str = "",
) -> tuple[subprocess.CompletedProcess[str], Path]:
    log_path = summary_path.parent / "gh-calls.jsonl"
    env = os.environ.copy()
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_GH_LOG"] = str(log_path)
    env["FAKE_GH_EXISTING_TITLE"] = existing_title
    env["GH_TOKEN"] = "fake-token"
    env["GITHUB_REPOSITORY"] = "Hexalith/Hexalith.Memories"
    env["GITHUB_SERVER_URL"] = "https://github.com"
    env["GITHUB_RUN_ID"] = "123456"

    result = subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(REPO_ROOT / "tools" / "create-partial-publish-issue.ps1"),
            "-SummaryPath",
            str(summary_path),
        ],
        cwd=REPO_ROOT,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )
    return result, log_path


class PublishNuGetTests(unittest.TestCase):
    def test_all_packages_publish_successfully_without_failure_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            package_directory = root / "packages"
            fake_bin = root / "bin"
            package_directory.mkdir()
            fake_bin.mkdir()
            packages = create_packages(package_directory)
            write_fake_dotnet(fake_bin)

            result, log_path, _ = run_publish(package_directory, fake_bin, {})

            self.assertEqual(0, result.returncode, result.stderr)
            calls = [
                json.loads(line)["package"]
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual([package.name for package in packages], calls)
            self.assertFalse((package_directory / "publish-summary.json").exists())

            first_call = json.loads(log_path.read_text(encoding="utf-8").splitlines()[0])
            self.assertIn("--skip-duplicate", first_call["args"])

    def test_duplicate_compatible_success_does_not_create_failure_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            package_directory = root / "packages"
            fake_bin = root / "bin"
            package_directory.mkdir()
            fake_bin.mkdir()
            packages = create_packages(package_directory)
            write_fake_dotnet(fake_bin)

            duplicate_package = packages[0].name
            result, log_path, _ = run_publish(
                package_directory,
                fake_bin,
                {
                    duplicate_package: {
                        "exitCode": 0,
                        "stdout": "warn : The package already exists and --skip-duplicate was specified.",
                        "stderr": "",
                    }
                },
            )

            self.assertEqual(0, result.returncode, result.stderr)
            calls = [
                json.loads(line)["package"]
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual([package.name for package in packages], calls)
            self.assertFalse((package_directory / "publish-summary.json").exists())

    def test_middle_package_failure_still_attempts_later_packages_and_writes_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            package_directory = root / "packages"
            fake_bin = root / "bin"
            package_directory.mkdir()
            fake_bin.mkdir()
            packages = create_packages(package_directory)
            write_fake_dotnet(fake_bin)

            failed_package = packages[len(packages) // 2].name
            plan = {
                failed_package: {
                    "exitCode": 42,
                    "stdout": "push started",
                    "stderr": "network failure SECRET_NUGET_API_KEY_SHOULD_NOT_LEAK",
                }
            }

            result, log_path, summary_path = run_publish(package_directory, fake_bin, plan)

            self.assertNotEqual(0, result.returncode)
            calls = [
                json.loads(line)["package"]
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual([package.name for package in packages], calls)

            summary_file = package_directory / "publish-summary.json"
            self.assertTrue(summary_file.exists())
            summary = json.loads(summary_file.read_text(encoding="utf-8"))
            self.assertEqual("partial-publish", summary["status"])
            self.assertEqual("1.2.3", summary["version"])
            self.assertEqual([failed_package], [item["package"] for item in summary["failed"]])
            self.assertEqual(42, summary["failed"][0]["exitCode"])
            self.assertIn("PARTIAL PUBLISH - manual reconciliation required", result.stdout)
            self.assertIn("release-runbook.md", summary["recovery"])
            self.assertIn("release-runbook.md", summary_path.read_text(encoding="utf-8"))
            self.assertNotIn("SECRET_NUGET_API_KEY_SHOULD_NOT_LEAK", result.stdout)
            self.assertNotIn("SECRET_NUGET_API_KEY_SHOULD_NOT_LEAK", result.stderr)
            self.assertNotIn(
                "SECRET_NUGET_API_KEY_SHOULD_NOT_LEAK",
                summary_file.read_text(encoding="utf-8"),
            )

    def test_prepush_validation_failure_writes_non_publish_summary_without_dotnet_push(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            package_directory = root / "packages"
            fake_bin = root / "bin"
            package_directory.mkdir()
            fake_bin.mkdir()
            write_fake_dotnet(fake_bin)

            result, log_path, _ = run_publish(package_directory, fake_bin, {})

            self.assertNotEqual(0, result.returncode)
            self.assertFalse(log_path.exists())
            summary_file = package_directory / "publish-summary.json"
            self.assertTrue(summary_file.exists())
            summary = json.loads(summary_file.read_text(encoding="utf-8"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertEqual([], summary["pushed"])
            self.assertEqual([], summary["failed"])
            self.assertIn("Package validation failed", summary["notAttempted"][0]["reason"])

    def test_issue_helper_creates_deterministic_partial_publish_issue(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            write_fake_gh(fake_bin)
            summary_path = root / "publish-summary.json"
            write_partial_summary(summary_path)

            result, log_path = run_issue_helper(summary_path, fake_bin)

            self.assertEqual(0, result.returncode, result.stderr)
            calls = [
                json.loads(line)
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual("list", calls[0]["args"][1])
            create_call = calls[1]["args"]
            self.assertEqual(["issue", "create"], create_call[:2])
            self.assertIn("PARTIAL PUBLISH 1.2.3 - manual reconciliation required", create_call)
            body = calls[1]["body"]
            self.assertIn("https://github.com/Hexalith/Hexalith.Memories/actions/runs/123456", body)
            self.assertIn("docs/dev/release-runbook.md", body)
            self.assertIn("Recover Partial Release", body)
            self.assertNotIn("Rerun the Release workflow", body)

    def test_issue_helper_comments_on_existing_partial_publish_issue(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            write_fake_gh(fake_bin)
            summary_path = root / "publish-summary.json"
            write_partial_summary(summary_path)
            title = "PARTIAL PUBLISH 1.2.3 - manual reconciliation required"

            result, log_path = run_issue_helper(summary_path, fake_bin, existing_title=title)

            self.assertEqual(0, result.returncode, result.stderr)
            calls = [
                json.loads(line)
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual("list", calls[0]["args"][1])
            comment_call = calls[1]["args"]
            self.assertEqual(["issue", "comment", "17"], comment_call[:3])
            self.assertNotIn("create", comment_call)
            body = calls[1]["body"]
            self.assertIn("Rerun detected", body)
            self.assertIn("docs/dev/release-runbook.md", body)

    def test_all_packages_fail_yields_publish_failed_status_without_partial_annotation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            package_directory = root / "packages"
            fake_bin = root / "bin"
            package_directory.mkdir()
            fake_bin.mkdir()
            packages = create_packages(package_directory)
            write_fake_dotnet(fake_bin)

            plan = {
                package.name: {
                    "exitCode": 401,
                    "stdout": "",
                    "stderr": "auth failure",
                }
                for package in packages
            }

            result, log_path, _ = run_publish(package_directory, fake_bin, plan)

            self.assertNotEqual(0, result.returncode)
            calls = [
                json.loads(line)["package"]
                for line in log_path.read_text(encoding="utf-8").splitlines()
            ]
            self.assertEqual([package.name for package in packages], calls)

            summary_file = package_directory / "publish-summary.json"
            self.assertTrue(summary_file.exists())
            summary = json.loads(summary_file.read_text(encoding="utf-8"))
            self.assertEqual("publish-failed", summary["status"])
            self.assertEqual([], summary["pushed"])
            self.assertEqual(
                [package.name for package in packages],
                [item["package"] for item in summary["failed"]],
            )
            self.assertNotIn("PARTIAL PUBLISH - manual reconciliation required", result.stdout)

    def test_issue_helper_skips_when_summary_status_is_publish_failed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            write_fake_gh(fake_bin)
            summary_path = root / "publish-summary.json"
            write_publish_failed_summary(summary_path)

            result, log_path = run_issue_helper(summary_path, fake_bin)

            self.assertEqual(0, result.returncode, result.stderr)
            self.assertFalse(log_path.exists())
            self.assertIn("skipping partial-publish issue alert", result.stdout)


if __name__ == "__main__":
    unittest.main()
