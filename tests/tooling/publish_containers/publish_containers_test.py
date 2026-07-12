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
SCRIPT = REPO_ROOT / "tools" / "publish-containers.ps1"
VERSION = "1.2.3-test.1"


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

            project = args[1] if len(args) >= 2 and args[0] == "publish" else ""
            if project.endswith("Hexalith.Memories.Server.csproj"):
                image = "server"
            elif project.endswith("Hexalith.Memories.Mcp.csproj"):
                image = "mcp"
            else:
                print(f"unexpected dotnet arguments: {args}", file=sys.stderr)
                sys.exit(97)

            plan_path = os.environ["FAKE_DOTNET_PLAN"]
            state_path = os.environ["FAKE_DOTNET_STATE"]
            log_path = os.environ["FAKE_DOTNET_LOG"]
            with open(plan_path, "r", encoding="utf-8") as plan_file:
                plan = json.load(plan_file)

            if os.path.exists(state_path):
                with open(state_path, "r", encoding="utf-8") as state_file:
                    state = json.load(state_file)
            else:
                state = {}

            attempt = int(state.get(image, 0))
            state[image] = attempt + 1
            with open(state_path, "w", encoding="utf-8") as state_file:
                json.dump(state, state_file)

            outcomes = plan[image]
            outcome = outcomes[min(attempt, len(outcomes) - 1)]
            with open(log_path, "a", encoding="utf-8") as log:
                log.write(json.dumps({"image": image, "attempt": attempt + 1, "args": args}) + "\\n")

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
        f'@echo off\r\n"{sys.executable}" "{script}" %*\r\n',
        encoding="utf-8",
    )
    (directory / "dotnet").write_text(
        f'#!/usr/bin/env sh\nexec "{sys.executable}" "{script}" "$@"\n',
        encoding="utf-8",
    )
    os.chmod(directory / "dotnet", 0o755)


def write_fake_kubectl(directory: Path) -> None:
    rendered = directory / "rendered.yaml"
    rendered.write_text(
        textwrap.dedent(
            """
            apiVersion: v1
            kind: List
            items:
              - image: registry.hexalith.com/hexalith/memories-server:0.0.0
              - image: registry.hexalith.com/hexalith/memories-mcp:0.0.0
            """
        ).lstrip(),
        encoding="utf-8",
    )
    (directory / "kubectl.cmd").write_text(
        f'@echo off\r\ntype "{rendered}"\r\n',
        encoding="utf-8",
    )
    (directory / "kubectl").write_text(
        f'#!/usr/bin/env sh\ncat "{rendered}"\n',
        encoding="utf-8",
    )
    os.chmod(directory / "kubectl", 0o755)


def run_publish(output_directory: Path, fake_bin: Path, plan: dict[str, list[dict[str, object]]]) -> tuple[subprocess.CompletedProcess[str], Path]:
    plan_path = output_directory.parent / "dotnet-plan.json"
    state_path = output_directory.parent / "dotnet-state.json"
    log_path = output_directory.parent / "dotnet-calls.jsonl"
    plan_path.write_text(json.dumps(plan), encoding="utf-8")

    env = os.environ.copy()
    env["REAL_DOTNET"] = shutil.which("dotnet") or "dotnet"
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_DOTNET_PLAN"] = str(plan_path)
    env["FAKE_DOTNET_STATE"] = str(state_path)
    env["FAKE_DOTNET_LOG"] = str(log_path)
    env["GITHUB_ACTIONS"] = "true"
    env["GH_TOKEN"] = "SECRET_CONTAINER_TOKEN_SHOULD_NOT_LEAK"

    result = subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(SCRIPT),
            "-Version",
            VERSION,
            "-Registry",
            "registry.test",
            "-OutputDirectory",
            str(output_directory),
            "-Push",
        ],
        cwd=REPO_ROOT,
        env=env,
        capture_output=True,
        text=True,
        check=False,
    )
    return result, log_path


class PublishContainersTests(unittest.TestCase):
    def test_partial_publish_writes_summary_and_returns_nonzero(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            write_fake_dotnet(fake_bin)
            write_fake_kubectl(fake_bin)
            output = root / "artifacts"
            plan = {
                "server": [{"exitCode": 0, "stdout": "server published"}],
                "mcp": [
                    {
                        "exitCode": 42,
                        "stderr": "registry rejected SECRET_CONTAINER_TOKEN_SHOULD_NOT_LEAK",
                    }
                ],
            }

            result, _ = run_publish(output, fake_bin, plan)

            self.assertNotEqual(0, result.returncode)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("partial-publish", summary["status"])
            self.assertTrue(summary["push"])
            self.assertEqual(
                ["registry.test/hexalith/memories-server:1.2.3-test.1"],
                summary["pushed"],
            )
            self.assertEqual(42, summary["failed"][0]["exitCode"])
            self.assertNotIn("SECRET_CONTAINER_TOKEN_SHOULD_NOT_LEAK", json.dumps(summary))
            self.assertIn("PARTIAL CONTAINER PUBLISH", result.stdout + result.stderr)

    def test_rerun_retries_both_members_and_replaces_partial_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fake_bin = root / "bin"
            fake_bin.mkdir()
            write_fake_dotnet(fake_bin)
            write_fake_kubectl(fake_bin)
            output = root / "artifacts"
            plan = {
                "server": [
                    {"exitCode": 0, "stdout": "server first publish"},
                    {"exitCode": 0, "stdout": "server retry publish"},
                ],
                "mcp": [
                    {"exitCode": 42, "stderr": "transient registry failure"},
                    {"exitCode": 0, "stdout": "mcp retry publish"},
                ],
            }

            first, log_path = run_publish(output, fake_bin, plan)
            second, _ = run_publish(output, fake_bin, plan)

            self.assertNotEqual(0, first.returncode)
            self.assertEqual(0, second.returncode, second.stdout + second.stderr)
            summary = json.loads((output / "publish-summary.json").read_text(encoding="utf-8-sig"))
            self.assertEqual("succeeded", summary["status"])
            self.assertEqual([], summary["failed"])
            self.assertEqual(
                [
                    "registry.test/hexalith/memories-server:1.2.3-test.1",
                    "registry.test/hexalith/memories-mcp:1.2.3-test.1",
                ],
                summary["pushed"],
            )
            calls = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()]
            self.assertEqual(["server", "mcp", "server", "mcp"], [call["image"] for call in calls])


if __name__ == "__main__":
    unittest.main()
