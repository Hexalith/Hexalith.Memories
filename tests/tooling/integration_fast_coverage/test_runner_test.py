"""Exercise integration selectors and report handling without Docker."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


TOOLS = Path(__file__).resolve().parents[3] / "tools"
RESULTS_MARKER = ".hexalith-memories-test-results"
TRX = '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
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
    if [ "${TEST_TRX+x}" = x ]; then
        printf '%s\\n' "$TEST_TRX" > "$results/$filename"
    fi
    if [ "${TEST_COVERAGE+x}" = x ]; then
        printf '%s\\n' "$TEST_COVERAGE" > "$results/fixture.coverage.cobertura.xml"
    fi
fi
status=${TEST_DOTNET_EXIT:-0}
if [ "$status" -ne 0 ]; then
    printf 'native test failure\\n' >&2
fi
exit "$status"
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
        launcher = self.root / "run-powershell.ps1"
        launcher.write_text(
            'param([string]$Filter, [string]$ResultsDirectory, [switch]$Coverage)\n'
            '$ErrorActionPreference = "Stop"\n'
            '$env:PATH = $env:TEST_WRAPPER_PATH\n'
            '& "$PSScriptRoot/tools/test.ps1" @PSBoundParameters\n',
            encoding="utf-8",
        )
        self.powershell = [shutil.which("pwsh"), "-NoProfile", "-File", str(launcher)]

    def test_bash_integration_selectors_choose_the_expected_projects(self):
        self.assert_integration_routes(self.bash, "--filter")

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_integration_selectors_choose_the_expected_projects(self):
        self.assert_integration_routes(self.powershell, "-Filter")

    def test_bash_requires_owned_results_directories(self):
        command = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory"]
        self.assert_results_ownership(command, "--coverage")

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_requires_owned_results_directories(self):
        command = [*self.powershell, "-Filter", "Category=IntegrationSlow", "-ResultsDirectory"]
        self.assert_results_ownership(command, "-Coverage")

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_wrappers_share_results_ownership(self):
        self.environment["TEST_TRX"] = TRX
        bash = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports"]
        powershell = [*self.powershell, "-Filter", "Category=IntegrationSlow", "-ResultsDirectory", "reports"]
        receipt = self.root / "reports" / "old-report.txt"

        for command in (bash, powershell, bash):
            result = self.run_wrapper(command)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertFalse(receipt.exists())
            receipt.write_text("stale report", encoding="utf-8")

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
                    *self.bash, "--filter", "Category=IntegrationSlow",
                    "--results-directory", "./reports/./nested/./",
                ])
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertIn("Executed 1 tests for", result.stdout)
                self.assertTrue(list((self.root / "reports" / "nested").glob("*/*.trx")))

    def test_bash_refuses_non_descendant_results_before_cleanup(self):
        (self.root / "checkout-link").symlink_to(self.root, target_is_directory=True)
        outside = tempfile.TemporaryDirectory()
        self.addCleanup(outside.cleanup)
        (self.root / "outside-link").symlink_to(outside.name, target_is_directory=True)
        cleanup = self.bin / "rm"
        cleanup.write_text("#!/bin/sh\necho 'unexpected results cleanup' >&2\nexit 99\n", encoding="utf-8")
        cleanup.chmod(0o755)

        for path in (".", "./", "././", "checkout-link", "outside-link/reports"):
            with self.subTest(path=path):
                result = self.run_wrapper([
                    *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", path,
                ])
                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn("--results-directory must resolve beneath the repository root", result.stderr)
                self.assertFalse(self.calls.exists())

    def test_bash_preserves_unrelated_results_when_parent_is_replaced_during_cleanup(self):
        parent = self.root / "reports"
        results = parent / "nested"
        moved_parent = self.root / "moved-reports"
        self.environment["TEST_TRX"] = TRX
        command = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports/nested"]
        first = self.run_wrapper(command)
        self.assertEqual(0, first.returncode, first.stderr)
        self.calls.unlink()
        (results / "stale").mkdir()
        (results / "stale" / "report.txt").write_text("old report", encoding="utf-8")

        outside = tempfile.TemporaryDirectory()
        self.addCleanup(outside.cleanup)
        unrelated = Path(outside.name) / "nested" / "stale" / "private.txt"
        unrelated.parent.mkdir(parents=True)
        unrelated.write_text("user managed", encoding="utf-8")

        # Replace the parent after the marker check and just before recursive deletion starts.
        (self.bin / "sitecustomize.py").write_text(
            "import os\n"
            "import sys\n"
            "\n"
            "def replace_parent(event, args):\n"
            "    if event == 'shutil.rmtree' and os.path.basename(args[0]) == 'stale':\n"
            "        parent = os.environ['TEST_RESULTS_PARENT']\n"
            "        os.rename(parent, os.environ['TEST_MOVED_PARENT'])\n"
            "        os.symlink(os.environ['TEST_REPLACEMENT_PARENT'], parent)\n"
            "\n"
            "sys.addaudithook(replace_parent)\n",
            encoding="utf-8",
        )
        self.environment.update(
            PYTHONPATH=str(self.bin),
            TEST_RESULTS_PARENT=str(parent),
            TEST_MOVED_PARENT=str(moved_parent),
            TEST_REPLACEMENT_PARENT=outside.name,
        )

        result = self.run_wrapper(command)
        self.assertTrue(parent.is_symlink(), result.stderr)
        self.assertTrue((moved_parent / "nested" / RESULTS_MARKER).is_file())
        self.assertTrue(unrelated.is_file(), "cleanup removed the unrelated fixture file")
        self.assertEqual("user managed", unrelated.read_text(encoding="utf-8"))
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("path changed during preparation", result.stderr)
        self.assertFalse(self.calls.exists())

    def test_bash_preserves_native_failure_when_reports_are_missing(self):
        self.environment["TEST_DOTNET_EXIT"] = "37"
        command = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports"]

        for coverage in ([], ["--coverage"]):
            with self.subTest(coverage=bool(coverage)):
                result = self.run_wrapper([*command, *coverage])
                self.assertEqual(37, result.returncode, result.stderr)
                self.assertIn("native test failure", result.stderr)
                self.assertIn("dotnet test failed (37) for", result.stderr)
                self.assertNotIn("Traceback", result.stderr)

    def test_bash_reads_available_trx_after_a_native_failure(self):
        self.environment["TEST_DOTNET_EXIT"] = "37"
        self.environment["TEST_TRX"] = '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
        command = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports", "--coverage"]

        valid = self.run_wrapper(command)
        self.assertEqual(37, valid.returncode, valid.stderr)
        self.assertIn("Executed 1 tests for", valid.stdout)

        for report in ('<TestRun>', '<?xml version="1.0" encoding="unknown-encoding"?><TestRun/>'):
            with self.subTest(report=report):
                self.environment["TEST_TRX"] = report
                malformed = self.run_wrapper(command)
                self.assertEqual(37, malformed.returncode, malformed.stderr)
                self.assertIn("Unable to read TRX file", malformed.stderr)
                self.assertIn("dotnet test failed (37) for", malformed.stderr)
                self.assertNotIn("Traceback", malformed.stderr)

    def test_bash_keeps_available_coverage_after_a_native_failure(self):
        self.environment["TEST_DOTNET_EXIT"] = "37"
        self.environment["TEST_COVERAGE"] = "<coverage/>"
        result = self.run_wrapper([
            *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports", "--coverage",
        ])
        self.assertEqual(37, result.returncode, result.stderr)
        reports = list((self.root / "reports").glob("*/coverage.cobertura.xml"))
        self.assertEqual(1, len(reports))
        self.assertEqual("<coverage/>\n", reports[0].read_text(encoding="utf-8"))

    def test_bash_requires_reports_after_a_successful_test_process(self):
        command = [*self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports"]
        missing_trx = self.run_wrapper(command)
        self.assertEqual(1, missing_trx.returncode, missing_trx.stderr)
        self.assertIn("Expected TRX file", missing_trx.stderr)

        self.environment["TEST_TRX"] = '<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>'
        missing_coverage = self.run_wrapper([*command, "--coverage"])
        self.assertEqual(1, missing_coverage.returncode, missing_coverage.stderr)
        self.assertIn("produced no Cobertura report", missing_coverage.stderr)

    def assert_results_ownership(self, command, coverage_flag):
        self.environment["TEST_TRX"] = TRX
        self.environment["TEST_COVERAGE"] = "<coverage/>"
        for destination, relative_file in (("src", "Project.cs"), ("private", ".hidden")):
            with self.subTest(destination=destination):
                sentinel = self.root / destination / relative_file
                sentinel.parent.mkdir(parents=True)
                sentinel.write_text("user managed", encoding="utf-8")
                refused = self.run_wrapper([*command, destination])
                self.assertNotEqual(0, refused.returncode)
                self.assertIn("empty or runner-owned directory", refused.stderr)
                self.assertEqual("user managed", sentinel.read_text(encoding="utf-8"))
                self.assertFalse(self.calls.exists())

        git_metadata = self.root / ".git"
        git_file = git_metadata / "config"
        git_empty = git_metadata / "objects" / "pack"
        git_empty.mkdir(parents=True)
        git_file.write_text("git managed", encoding="utf-8")
        for destination in (".git", ".git/objects/pack", ".git/new/nested"):
            with self.subTest(destination=destination):
                refused = self.run_wrapper([*command, destination])
                self.assertNotEqual(0, refused.returncode)
                self.assertIn(".git metadata path", refused.stderr)
                self.assertEqual("git managed", git_file.read_text(encoding="utf-8"))
                self.assertEqual([], list(git_empty.iterdir()))
                self.assertFalse((git_metadata / "new").exists())
                self.assertFalse(self.calls.exists())

        (self.root / "empty").mkdir()
        for destination in ("./reports/./nested results/./", "empty"):
            with self.subTest(destination=destination):
                results = self.root / destination
                first = self.run_wrapper([*command, destination, coverage_flag])
                self.assertEqual(0, first.returncode, first.stderr)
                self.assertTrue(list(results.glob("*/*.trx")))
                self.assertTrue(list(results.glob("*/coverage.cobertura.xml")))

                stale = results / ".stale" / "old-report.xml"
                stale.parent.mkdir()
                stale.write_text("previous report", encoding="utf-8")
                (stale.parent / "nested-link").symlink_to(self.root / "src", target_is_directory=True)
                direct_link = results / "directory-link"
                direct_link.symlink_to(git_metadata, target_is_directory=True)

                rerun = self.run_wrapper([*command, destination, coverage_flag])
                self.assertEqual(0, rerun.returncode, rerun.stderr)
                self.assertFalse(stale.parent.exists())
                self.assertFalse(direct_link.is_symlink())
                self.assertEqual("user managed", (self.root / "src" / "Project.cs").read_text(encoding="utf-8"))
                self.assertEqual("git managed", git_file.read_text(encoding="utf-8"))
                self.assertTrue(list(results.glob("*/*.trx")))

        self.calls.unlink()
        owned = self.root / "reports" / "nested results"
        sentinel = owned / "keep-on-refusal.txt"
        sentinel.write_text("owned via real path", encoding="utf-8")
        (self.root / "root-link").symlink_to(owned, target_is_directory=True)
        (self.root / "parent-link").symlink_to(owned.parent, target_is_directory=True)
        (self.root / "dangling-link").symlink_to(self.root / "missing", target_is_directory=True)
        unowned = self.root / "unowned"
        unowned.mkdir()
        unowned_file = unowned / "private.txt"
        unowned_file.write_text("user managed", encoding="utf-8")
        marker_link = unowned / RESULTS_MARKER
        marker_link.symlink_to(owned / RESULTS_MARKER)

        for destination in ("root-link", "parent-link/nested results", "dangling-link", "unowned", "reports"):
            with self.subTest(destination=destination):
                refused = self.run_wrapper([*command, destination])
                self.assertNotEqual(0, refused.returncode)
                self.assertRegex(refused.stderr, r"symbolic link|empty or runner-owned directory")
                self.assertEqual("owned via real path", sentinel.read_text(encoding="utf-8"))
                self.assertEqual("user managed", unowned_file.read_text(encoding="utf-8"))
                self.assertTrue(marker_link.is_symlink())
                self.assertFalse(self.calls.exists())

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
        environment = self.environment.copy()
        if command[0] == self.powershell[0]:
            # The global-tool launcher may need real dotnet before the shell starts.
            environment["TEST_WRAPPER_PATH"] = environment["PATH"]
            environment["PATH"] = os.environ["PATH"]
        # Allow the PowerShell runtime to start on a busy build host.
        return subprocess.run(
            command, cwd=self.root, env=environment, text=True, capture_output=True, check=False, timeout=120,
        )
