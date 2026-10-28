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
            operation = args[1]
            previous_calls = []
            if os.path.exists(log_path):
                with open(log_path, encoding="utf-8") as log:
                    previous_calls = [json.loads(line) for line in log]
            attempt = sum(call["args"][1] == operation for call in previous_calls)
            with open(os.environ["FAKE_GH_PLAN"], encoding="utf-8") as plan_file:
                plan = json.load(plan_file)
            outcomes = plan.get(operation, [])
            outcome = outcomes[attempt] if attempt < len(outcomes) else {}
            state_path = os.environ["FAKE_GH_STATE"]
            if os.path.exists(state_path):
                with open(state_path, encoding="utf-8") as state_file:
                    state = json.load(state_file)
            else:
                title = os.environ.get("FAKE_GH_EXISTING_TITLE", "")
                state = {"issues": [{"number": 17, "title": title, "body": "Earlier incident evidence"}] if title else [], "comments": []}
            body = None
            if "--body-file" in args:
                body_file = args[args.index("--body-file") + 1]
                with open(body_file, "r", encoding="utf-8-sig") as body_handle:
                    body = body_handle.read()
            with open(log_path, "a", encoding="utf-8") as log:
                log.write(json.dumps({"args": args, "body": body}) + "\\n")

            exit_code = int(outcome.get("exitCode", 0))
            if operation in ("create", "comment") and outcome.get("apply", exit_code == 0):
                if operation == "create":
                    state["issues"].append({"number": 17, "title": args[args.index("--title") + 1], "body": body})
                else:
                    state["comments"].append({"body": body})
                with open(state_path, "w", encoding="utf-8") as state_file:
                    json.dump(state, state_file)

            if operation == "list":
                default_output = json.dumps(state["issues"])
            elif operation == "view":
                default_output = json.dumps({"comments": state["comments"]})
            elif operation in ("create", "comment"):
                default_output = "ok"
            else:
                print(f"unexpected gh args: {args}", file=sys.stderr)
                sys.exit(2)
            output = outcome.get("stdout", default_output if exit_code == 0 else "")
            if output:
                print(output)
            if outcome.get("stderr"):
                print(outcome["stderr"], file=sys.stderr)
            sys.exit(exit_code)
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
    gh_plan: dict[str, list[dict[str, object]]] | None = None,
) -> tuple[subprocess.CompletedProcess[str], Path]:
    log_path = summary_path.parent / "gh-calls.jsonl"
    plan_path = summary_path.parent / "gh-plan.json"
    plan_path.write_text(json.dumps(gh_plan or {}), encoding="utf-8")
    env = os.environ.copy()
    env["PATH"] = str(fake_bin) + os.pathsep + env["PATH"]
    env["FAKE_GH_LOG"] = str(log_path)
    env["FAKE_GH_EXISTING_TITLE"] = existing_title
    env["FAKE_GH_PLAN"] = str(plan_path)
    env["FAKE_GH_STATE"] = str(summary_path.parent / "gh-state.json")
    env["GH_TOKEN"] = "fake-token"
    env["GITHUB_REPOSITORY"] = "Hexalith/Hexalith.Memories"
    env["GITHUB_SERVER_URL"] = "https://github.com"
    env["GITHUB_RUN_ID"] = "123456"
    env["FAKE_GH_SLEEP_LOG"] = str(summary_path.parent / "gh-sleeps.jsonl")
    driver = summary_path.parent / "issue-helper-driver.ps1"
    driver.write_text(
        'param([string]$HelperPath, [string]$SummaryPath)\n'
        "$ErrorActionPreference = 'Stop'\n"
        'function global:Start-Sleep {\n'
        '    param([int]$Seconds)\n'
        '    $callCount = @(Get-Content -LiteralPath $env:FAKE_GH_LOG).Count\n'
        '    @{ seconds = $Seconds; afterCalls = $callCount } | ConvertTo-Json -Compress |\n'
        '        Add-Content -LiteralPath $env:FAKE_GH_SLEEP_LOG\n'
        '}\n'
        '& $HelperPath -SummaryPath $SummaryPath\n',
        encoding="utf-8",
    )

    result = subprocess.run(
        [
            "pwsh",
            "-NoLogo",
            "-NoProfile",
            "-File",
            str(driver),
            str(REPO_ROOT / "tools" / "create-partial-publish-issue.ps1"),
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


class IssueHelperReliabilityTests(unittest.TestCase):
    title = "PARTIAL PUBLISH 1.2.3 - manual reconciliation required"

    def setUp(self) -> None:
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        self.fake_bin = self.root / "bin"
        self.fake_bin.mkdir()
        write_fake_gh(self.fake_bin)
        self.summary_path = self.root / "publish-summary.json"
        write_partial_summary(self.summary_path)

    def run_helper(self, **kwargs):
        result, log_path = run_issue_helper(self.summary_path, self.fake_bin, **kwargs)
        calls = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()] if log_path.exists() else []
        return result, calls

    def sleeps(self):
        path = self.root / "gh-sleeps.jsonl"
        return [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()] if path.exists() else []

    def reset_fake(self):
        for name in ("gh-calls.jsonl", "gh-state.json", "gh-sleeps.jsonl"):
            (self.root / name).unlink(missing_ok=True)

    def test_lookup_uses_quoted_title_and_explicit_limit_and_exact_match(self):
        issues = [{"number": n, "title": f"{self.title} unrelated {n}"} for n in range(1, 41)]
        issues.append({"number": 88, "title": self.title})
        result, calls = self.run_helper(gh_plan={"list": [{"stdout": json.dumps(issues)}]})
        self.assertEqual(0, result.returncode, result.stderr)
        args = calls[0]["args"]
        self.assertEqual(f'in:title "{self.title}"', args[args.index("--search") + 1])
        self.assertGreaterEqual(int(args[args.index("--limit") + 1]), 100)
        self.assertEqual(["issue", "comment", "88"], calls[1]["args"][:3])

    def test_null_artifacts_are_omitted_and_all_null_lists_render_none(self):
        summary = json.loads(self.summary_path.read_text(encoding="utf-8"))
        summary["pushed"] = [None, "kept.nupkg", None]
        summary["failed"] = [None]
        summary["notAttempted"] = None
        self.summary_path.write_text(json.dumps(summary), encoding="utf-8")
        result, calls = self.run_helper()
        self.assertEqual(0, result.returncode, result.stderr)
        body = calls[-1]["body"]
        self.assertIn("- kept.nupkg", body)
        self.assertNotIn("- :", body)
        self.assertNotIn("\n- \n", body)
        self.assertEqual(2, body.count("- None"))

    def test_malformed_summary_fails_with_context_before_github(self):
        self.summary_path.write_text('{"status": "partial-publish", SECRET_INVALID', encoding="utf-8")
        result, calls = self.run_helper()
        self.assertNotEqual(0, result.returncode)
        self.assertEqual([], calls)
        self.assertIn(str(self.summary_path), result.stderr)
        self.assertIn("publish summary", result.stderr.lower())
        self.assertIn("preserve", result.stderr.lower())
        self.assertNotIn("SECRET_INVALID", result.stderr)

    def test_empty_lookup_output_is_an_empty_collection(self):
        for output in ("", "  ", "\n\t\n"):
            with self.subTest(output=repr(output)):
                result, calls = self.run_helper(gh_plan={"list": [{"stdout": output}]})
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertEqual("create", calls[-1]["args"][1])
                (self.root / "gh-calls.jsonl").unlink()
                (self.root / "gh-state.json").unlink()

    def test_transient_lookup_failure_retries_with_visible_backoff(self):
        result, calls = self.run_helper(gh_plan={"list": [
            {"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"},
            {"exitCode": 1, "stderr": "HTTP 429: Too Many Requests"},
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "list", "list", "create"], [c["args"][1] for c in calls])
        self.assertIn("Retrying in 1 second", result.stdout)
        self.assertIn("Retrying in 120 second", result.stdout)
        self.assertEqual([1, 120], [wait["seconds"] for wait in self.sleeps()])

    def test_permanent_failures_are_not_retried(self):
        for operation in ("list", "create", "comment"):
            with self.subTest(operation=operation):
                result, calls = self.run_helper(
                    existing_title=self.title if operation == "comment" else "",
                    gh_plan={operation: [{"exitCode": 1, "stderr": "HTTP 403: Forbidden"}]},
                )
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(1, sum(c["args"][1] == operation for c in calls))
                self.assertNotIn("Retrying", result.stdout)
                (self.root / "gh-calls.jsonl").unlink()

    def test_lookup_retry_budget_exhaustion_fails_without_mutations(self):
        result, calls = self.run_helper(gh_plan={"list": [
            {"exitCode": 1, "stderr": "connection reset by peer"} for _ in range(3)
        ]})
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["list"] * 3, [c["args"][1] for c in calls])
        self.assertIn("3 attempts", result.stderr)

    def test_failed_create_is_reconciled_before_retry(self):
        result, calls = self.run_helper(gh_plan={"create": [
            {"exitCode": 1, "stderr": "TLS handshake timeout"}
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "create", "list", "create"], [c["args"][1] for c in calls])
        self.assertEqual(calls[1]["body"], calls[3]["body"])
        for call in (calls[1], calls[3]):
            args = call["args"]
            self.assertFalse(Path(args[args.index("--body-file") + 1]).exists())

    def test_successful_create_with_lost_response_is_not_repeated(self):
        result, calls = self.run_helper(gh_plan={"create": [
            {"exitCode": 1, "stderr": "HTTP 502: Bad Gateway", "apply": True}
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "create", "list"], [c["args"][1] for c in calls])
        self.assertNotIn("--search", calls[-1]["args"])
        state = json.loads((self.root / "gh-state.json").read_text(encoding="utf-8"))
        self.assertEqual(1, len(state["issues"]))

    def test_failed_comment_is_reconciled_before_retry(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={"comment": [
            {"exitCode": 1, "stderr": "unexpected EOF"}
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "comment", "view", "comment"], [c["args"][1] for c in calls])
        self.assertIn("comments", calls[2]["args"])
        self.assertEqual(calls[1]["body"], calls[3]["body"])

    def test_successful_comment_with_lost_response_is_not_repeated(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={"comment": [
            {"exitCode": 1, "stderr": "HTTP 504: Gateway Timeout", "apply": True}
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "comment", "view"], [c["args"][1] for c in calls])
        state = json.loads((self.root / "gh-state.json").read_text(encoding="utf-8"))
        self.assertEqual(1, len(state["comments"]))

    def test_failed_reconciliation_stops_before_repeating_a_write(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={
            "comment": [{"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"}],
            "view": [{"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"} for _ in range(3)],
        })
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["list", "comment", "view", "view", "view"], [c["args"][1] for c in calls])

    def test_malformed_lookup_output_fails_before_a_write(self):
        result, calls = self.run_helper(gh_plan={"list": [{"stdout": "not JSON"}]})
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["list"], [c["args"][1] for c in calls])

    def test_malformed_comment_readback_stops_before_repeating_a_write(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={
            "comment": [{"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"}],
            "view": [{"stdout": '{"comments": null}'}],
        })
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["list", "comment", "view"], [c["args"][1] for c in calls])

    def test_write_retry_budget_is_bounded_for_create_and_comment(self):
        for operation, readback in (("create", "list"), ("comment", "view")):
            with self.subTest(operation=operation):
                result, calls = self.run_helper(
                    existing_title=self.title if operation == "comment" else "",
                    gh_plan={operation: [{"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"} for _ in range(3)]},
                )
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(["list"] + [operation, readback] * 3, [c["args"][1] for c in calls])
                self.assertIn("3 attempts", result.stderr)
                (self.root / "gh-calls.jsonl").unlink()

    def test_blank_create_readback_refuses_to_repeat_a_successful_write(self):
        for output in ("", "  ", "\n\t\n"):
            with self.subTest(output=repr(output)):
                result, calls = self.run_helper(gh_plan={
                    "create": [{"exitCode": 1, "stderr": "HTTP 502: Bad Gateway", "apply": True}],
                    "list": [{}, {"stdout": output}],
                })
                self.assertNotEqual(0, result.returncode)
                self.assertEqual(["list", "create", "list"], [c["args"][1] for c in calls])
                self.assertIn("blank issue readback", result.stderr)
                self.reset_fake()

    def test_rate_limit_403_uses_exponential_minute_backoff(self):
        result, calls = self.run_helper(gh_plan={"list": [
            {"exitCode": 1, "stderr": "HTTP 403: You have exceeded a secondary rate limit"},
            {"exitCode": 1, "stderr": "GraphQL: API rate limit exceeded"},
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "list", "list", "create"], [c["args"][1] for c in calls])
        self.assertEqual([{"seconds": 60, "afterCalls": 1}, {"seconds": 120, "afterCalls": 2}], self.sleeps())

    def test_rate_limited_write_waits_before_readback(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={"comment": [
            {"exitCode": 1, "stderr": "HTTP 403: You have exceeded a secondary rate limit", "apply": True}
        ]})
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "comment", "view"], [c["args"][1] for c in calls])
        self.assertEqual([{"seconds": 60, "afterCalls": 2}], self.sleeps())

    def test_retry_after_is_honored_and_excessive_waits_fail_within_budget(self):
        for diagnostic, expected_waits, succeeds in (
            ("HTTP 429: Too Many Requests\nRetry-After: 7", [7], True),
            ("HTTP 429: Too Many Requests\nRetry-After: 181", [], False),
            ("HTTP 403: API rate limit exceeded\nx-ratelimit-reset: 4102444800", [], False),
        ):
            with self.subTest(diagnostic=diagnostic):
                result, calls = self.run_helper(gh_plan={"list": [{"exitCode": 1, "stderr": diagnostic}]})
                self.assertEqual(succeeds, result.returncode == 0, result.stderr)
                self.assertEqual(expected_waits, [wait["seconds"] for wait in self.sleeps()])
                if not succeeds:
                    self.assertEqual(["list"], [c["args"][1] for c in calls])
                    self.assertIn("180-second total wait budget", " ".join(result.stderr.split()))
                self.reset_fake()

    def test_terminal_diagnostic_is_preserved_bounded_and_redacted(self):
        result, calls = self.run_helper(gh_plan={"list": [{
            "exitCode": 1,
            "stderr": "GraphQL: Resource not accessible by integration (createIssue)\n"
                      "Authorization: Bearer SUPER_SECRET\n"
                      "token=fake-token endpoint=https://user:password@example.test/path\n" + "detail" * 200,
        }]})
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["list"], [c["args"][1] for c in calls])
        self.assertIn("Resource not accessible by integration", result.stderr)
        self.assertNotIn("SUPER_SECRET", result.stderr)
        self.assertNotIn("fake-token", result.stderr)
        self.assertNotIn("password", result.stderr)
        self.assertIn("[REDACTED]", result.stderr)
        self.assertNotIn("detail" * 100, result.stderr)

    def test_summary_root_must_be_an_object(self):
        for summary in ('[{"status":"partial-publish","version":"1.2.3"}]', '[]', 'null', '"text"', '{}', '  '):
            with self.subTest(summary=summary):
                self.reset_fake()
                self.summary_path.write_text(summary, encoding="utf-8")
                result, calls = self.run_helper()
                self.assertNotEqual(0, result.returncode)
                self.assertEqual([], calls)
                self.assertIn("JSON summary object", " ".join(result.stderr.split()))

    def test_create_recovery_adds_current_evidence_to_a_preexisting_incident(self):
        result, calls = self.run_helper(existing_title=self.title, gh_plan={
            "list": [{"stdout": "[]"}],
            "create": [{"exitCode": 1, "stderr": "HTTP 503: Service Unavailable"}],
        })
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(["list", "create", "list", "comment"], [c["args"][1] for c in calls])
        self.assertIn("https://github.com/Hexalith/Hexalith.Memories/actions/runs/123456", calls[-1]["body"])
        self.assertIn("Hexalith.Memories.Cli.1.2.3.nupkg", calls[-1]["body"])
        state = json.loads((self.root / "gh-state.json").read_text(encoding="utf-8"))
        self.assertEqual(1, len(state["issues"]))
        self.assertEqual(1, len(state["comments"]))


if __name__ == "__main__":
    unittest.main()
