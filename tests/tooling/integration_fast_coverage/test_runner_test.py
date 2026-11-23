"""Exercise the integration selectors and Bash report prerequisites without Docker."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


TOOLS = Path(__file__).resolve().parents[3] / "tools"
DOTNET_FIXTURE = """#!/bin/sh
if [ "$1" != test ]; then
    printf 'Unexpected dotnet invocation: %s\\n' "$*" >&2
    exit 1
fi
printf '%s\\0' "$@" >> "$DOTNET_CALLS"
printf '\\0' >> "$DOTNET_CALLS"
while [ "$#" -gt 0 ]; do
    case "$1" in
        --results-directory) results=$2; shift ;;
        --report-xunit-trx-filename) filename=$2; shift ;;
    esac
    shift
done
if [ -n "${results:-}" ]; then
    printf '%s\n' "$TEST_TRX" > "$results/$filename"
fi
"""


@unittest.skipUnless(os.name == "posix" and shutil.which("bash"), "requires a POSIX host with Bash")
class IntegrationTestRunnerTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        tools = self.root / "tools"
        tools.mkdir()
        for path in (*TOOLS.glob("test-projects.*.txt"), TOOLS / "test.sh", TOOLS / "test.ps1"):
            shutil.copy2(path, tools / path.name)
        self.bin = self.root / "bin"
        self.bin.mkdir()
        dotnet = self.bin / "dotnet"
        dotnet.write_text(DOTNET_FIXTURE, encoding="utf-8")
        dotnet.chmod(0o755)
        self.calls = self.root / "dotnet-calls.txt"
        self.environment = dict(os.environ, DOTNET_CALLS=str(self.calls))
        self.environment["PATH"] = os.pathsep.join((str(self.bin), os.environ["PATH"]))
        self.bash = [shutil.which("bash"), "tools/test.sh"]

    def test_bash_integration_selectors_choose_the_expected_projects(self):
        self.assert_integration_routes(self.bash, "--filter")

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_integration_selectors_choose_the_expected_projects(self):
        # A global-tool pwsh launcher may need real dotnet before the shell starts.
        self.environment["TEST_WRAPPER_PATH"] = self.environment["PATH"]
        self.environment["PATH"] = os.environ["PATH"]
        launcher = self.root / "run-powershell.ps1"
        launcher.write_text(
            'param([string]$Filter)\n'
            '$ErrorActionPreference = "Stop"\n'
            '$env:PATH = $env:TEST_WRAPPER_PATH\n'
            '& "$PSScriptRoot/tools/test.ps1" -Filter $Filter\n',
            encoding="utf-8",
        )
        self.assert_integration_routes([shutil.which("pwsh"), "-NoProfile", "-File", str(launcher)], "-Filter")

    def test_bash_requires_python_only_for_reports_and_preserves_previous_results(self):
        for command in ("dirname", "grep", "sed", "rm", "mkdir", "basename"):
            (self.bin / command).symlink_to(shutil.which(command))
        self.environment["PATH"] = str(self.bin)
        results = self.root / "previous-results"
        results.mkdir()
        receipt = results / "receipt.txt"
        receipt.write_text("previous report", encoding="utf-8")
        command = [*self.bash, "--filter", "Category=IntegrationSlow"]

        missing = self.run_wrapper([*command, "--results-directory", results.name])
        self.assertEqual(1, missing.returncode, missing.stderr)
        self.assertIn("--results-directory requires python3", missing.stderr)
        self.assertEqual("previous report", receipt.read_text(encoding="utf-8"))
        self.assertFalse(self.calls.exists())

        broken_python = self.bin / "python3"
        broken_python.write_text(f"#!{self.bin / 'missing-interpreter'}\n", encoding="utf-8")
        broken_python.chmod(0o755)
        broken = self.run_wrapper([*command, "--results-directory", results.name])
        self.assertEqual(1, broken.returncode, broken.stderr)
        self.assertIn("--results-directory requires python3", broken.stderr)
        self.assertEqual("previous report", receipt.read_text(encoding="utf-8"))
        self.assertFalse(self.calls.exists())

        ordinary = self.run_wrapper(command)
        self.assertEqual(0, ordinary.returncode, ordinary.stderr)
        self.assertTrue(self.calls.is_file())

    def test_bash_reads_trx_counters_with_or_without_namespace(self):
        for namespace in ("", ' xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"'):
            with self.subTest(namespace=namespace):
                self.environment["TEST_TRX"] = (
                    f'<TestRun{namespace}><ResultSummary><Counters executed="1" />'
                    '</ResultSummary></TestRun>'
                )
                result = self.run_wrapper([
                    *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports",
                ])
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertIn("Executed 1 tests for", result.stdout)

    def assert_integration_routes(self, wrapper, filter_flag):
        fast = self.read_inventory("integration-fast")
        slow = self.read_inventory("integration-slow")
        self.assertTrue(fast and slow)
        self.assertFalse(set(fast).intersection(slow))
        for selector, projects, traits in (
            ("Category=Integration", fast + slow, ["--filter-trait", "Category=Integration"]),
            (
                "Category=Integration&Category!=IntegrationSlow&Category!=Performance",
                fast,
                ["--filter-trait", "Category=Integration", "--filter-not-trait", "Category=IntegrationSlow",
                 "--filter-not-trait", "Category=Performance"],
            ),
            ("Category=IntegrationSlow", slow, ["--filter-trait", "Category=IntegrationSlow"]),
            ("Category=Performance", slow, ["--filter-trait", "Category=Performance"]),
        ):
            with self.subTest(selector=selector):
                self.calls.write_bytes(b"")
                result = self.run_wrapper([*wrapper, filter_flag, selector])
                self.assertEqual(0, result.returncode, result.stderr)
                calls = self.calls.read_text(encoding="utf-8").removesuffix("\0\0").split("\0\0")
                self.assertEqual(
                    [["test", "--project", project, "--configuration", "Debug", *traits] for project in projects],
                    [call.split("\0") for call in calls],
                )

    def read_inventory(self, lane):
        contents = (TOOLS / f"test-projects.{lane}.txt").read_text(encoding="utf-8")
        return [line for line in map(str.strip, contents.splitlines()) if line and not line.startswith("#")]

    def run_wrapper(self, command):
        return subprocess.run(
            command, cwd=self.root, env=self.environment, text=True, capture_output=True, check=False, timeout=30,
        )
