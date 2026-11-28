"""Exercise integration selectors and report handling without Docker."""

import ctypes
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import tempfile
import time
import unittest


TOOLS = Path(__file__).resolve().parents[3] / "tools"
RESULTS_MARKER = ".hexalith-memories-test-results"
RESULTS_SIGNATURE = b"Hexalith.Memories test results v1\n"
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
if [ -n "${TEST_DOTNET_STARTED:-}" ]; then
    printf '%s\\n' "$$" > "$TEST_DOTNET_STARTED.tmp"
    mv "$TEST_DOTNET_STARTED.tmp" "$TEST_DOTNET_STARTED"
    while [ ! -e "$TEST_DOTNET_RELEASE" ]; do sleep 0.02; done
fi
if [ -n "${results:-}" ]; then
    if [ "${TEST_TRX+x}" = x ]; then
        printf '%s\\n' "$TEST_TRX" > "$results/$filename"
    fi
    if [ "${TEST_COVERAGE+x}" = x ]; then
        printf '%s\\n' "$TEST_COVERAGE" > "$results/fixture.coverage.cobertura.xml"
    fi
    if [ "${TEST_ATTACHMENT+x}" = x ]; then
        mkdir -p "$results/attachments"
        printf '%s\\n' "$TEST_ATTACHMENT" > "$results/attachments/trace.txt"
    fi
fi
if [ -n "${TEST_DOTNET_BEFORE_EXIT:-}" ]; then
    "$TEST_DOTNET_BEFORE_EXIT" || exit $?
fi
if [ -n "${TEST_DOTNET_FINISHED:-}" ]; then
    touch "$TEST_DOTNET_FINISHED"
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
        for path in (
            *TOOLS.glob("test-projects.*.txt"), TOOLS / "test.sh", TOOLS / "test.ps1",
            TOOLS / "prepare-test-results.ps1", TOOLS / "prepare-test-results.py",
        ):
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
            'if ($env:TEST_POWERSHELL_PID) { [IO.File]::WriteAllText($env:TEST_POWERSHELL_PID, [string]$PID) }\n'
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
    def test_powershell_requires_python_only_for_reports_on_posix(self):
        self.environment["PATH"] = str(self.bin)
        results = self.root / "previous-results"
        results.mkdir()
        receipt = results / "receipt.txt"
        receipt.write_text("previous report", encoding="utf-8")
        command = [*self.powershell, "-Filter", "Category=IntegrationSlow"]

        refused = self.run_wrapper([*command, "-ResultsDirectory", results.name])
        self.assertEqual(1, refused.returncode, refused.stderr)
        self.assertIn("-ResultsDirectory requires python3", refused.stderr)
        self.assertEqual("previous report", receipt.read_text(encoding="utf-8"))
        self.assertFalse(self.calls.exists())

        ordinary = self.run_wrapper(command)
        self.assertEqual(0, ordinary.returncode, ordinary.stderr)
        self.assertTrue(self.calls.is_file())

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_pins_owned_cleanup_when_results_path_changes(self):
        (self.bin / "sitecustomize.py").write_text(
            "import os\n"
            "import sys\n"
            "\n"
            "def replace_binding(event, args):\n"
            "    if event == 'shutil.rmtree' and args[0] == 'stale':\n"
            "        binding = os.environ['TEST_RESULTS_BINDING']\n"
            "        os.rename(binding, os.environ['TEST_MOVED_BINDING'])\n"
            "        replacement = os.environ['TEST_REPLACEMENT_BINDING']\n"
            "        if os.environ['TEST_USE_SYMLINK'] == '1':\n"
            "            os.symlink(replacement, binding)\n"
            "        else:\n"
            "            os.rename(replacement, binding)\n"
            "\n"
            "sys.addaudithook(replace_binding)\n",
            encoding="utf-8",
        )
        self.environment["PYTHONPATH"] = str(self.bin)

        for name, binding_parts, suffix, use_symlink in (
            ("parent", ("reports",), ("nested",), True),
            ("destination", ("reports", "nested"), (), False),
        ):
            with self.subTest(binding=name):
                fixture = self.root / name
                binding = fixture.joinpath(*binding_parts)
                results = binding.joinpath(*suffix)
                moved = fixture / "moved"
                replacement = fixture / "unrelated"
                unrelated = replacement.joinpath(*suffix, "stale", "private.txt")
                stale = results / "stale" / "old-report.txt"
                stale.parent.mkdir(parents=True)
                stale.write_text("old report", encoding="utf-8")
                (results / RESULTS_MARKER).write_bytes(RESULTS_SIGNATURE)
                unrelated.parent.mkdir(parents=True)
                unrelated.write_text("user managed", encoding="utf-8")
                self.environment.update(
                    TEST_RESULTS_BINDING=str(binding), TEST_MOVED_BINDING=str(moved),
                    TEST_REPLACEMENT_BINDING=str(replacement), TEST_USE_SYMLINK="1" if use_symlink else "0",
                )

                result = self.run_wrapper(
                    [*self.powershell, "-Filter", "Category=IntegrationSlow",
                     "-ResultsDirectory", str(results.relative_to(self.root))],
                    cwd=self.root / "tools",
                )

                moved_results = moved.joinpath(*suffix)
                self.assertEqual(RESULTS_SIGNATURE, (moved_results / RESULTS_MARKER).read_bytes(), result.stderr)
                self.assertFalse((moved_results / "stale").exists())
                self.assertEqual("user managed", (results / "stale" / "private.txt").read_text(encoding="utf-8"))
                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn("path changed during preparation", result.stderr)
                self.assertFalse(self.calls.exists())

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

    def test_bash_refuses_an_overlapping_bash_run(self):
        self.assert_overlapping_run(self.bash, self.bash)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_refuses_an_overlapping_powershell_run(self):
        self.assert_overlapping_run(self.powershell, self.powershell)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_wrappers_share_the_active_results_lease(self):
        self.assert_overlapping_run(self.bash, self.powershell)
        self.assert_overlapping_run(self.powershell, self.bash)

    def test_bash_refuses_runs_that_overlap_by_ancestry(self):
        self.assert_ancestry_overlap_is_refused(self.bash, self.bash)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_wrappers_share_results_leases_across_ancestry(self):
        self.assert_ancestry_overlap_is_refused(self.powershell, self.powershell)
        self.assert_ancestry_overlap_is_refused(self.bash, self.powershell)
        self.assert_ancestry_overlap_is_refused(self.powershell, self.bash)

    def assert_ancestry_overlap_is_refused(self, owner_wrapper, contender_wrapper):
        self.environment["TEST_TRX"] = TRX
        for ancestor_first in (True, False):
            with self.subTest(owner=owner_wrapper[0], contender=contender_wrapper[0], ancestor_first=ancestor_first):
                fixture = Path(tempfile.mkdtemp(dir=self.root))
                ancestor = fixture / "reports"
                descendant = ancestor / "nested" / "deeper"
                owner_results, contender_results = (ancestor, descendant) if ancestor_first else (descendant, ancestor)
                if not ancestor_first:
                    ancestor.mkdir()
                    (ancestor / RESULTS_MARKER).write_bytes(RESULTS_SIGNATURE)
                    (ancestor / "previous.txt").write_text("previous report", encoding="utf-8")

                owner_command = self.report_command(owner_wrapper, str(owner_results.relative_to(self.root)))
                contender = self.report_command(contender_wrapper, str(contender_results.relative_to(self.root)))
                with tempfile.TemporaryDirectory() as scratch:
                    start, release = (Path(scratch) / name for name in ("start", "release"))
                    owner, log = self.start_report_owner(owner_command, start, release)
                    active_report = owner_results / "active-report.txt"
                    active_report.write_text("owner data", encoding="utf-8")
                    calls = self.calls.read_bytes()

                    refused = self.run_wrapper(contender)
                    self.assertEqual(1, refused.returncode, refused.stderr)
                    self.assertIn("in use by another test run", refused.stderr)
                    self.assertEqual(calls, self.calls.read_bytes())
                    self.assertEqual("owner data", active_report.read_text(encoding="utf-8"))
                    if ancestor_first:
                        self.assertFalse(descendant.parent.exists())
                    else:
                        self.assertEqual("previous report", (ancestor / "previous.txt").read_text(encoding="utf-8"))

                    sibling = owner_results.with_name(owner_results.name + "-sibling")
                    separate = self.run_wrapper(self.report_command(contender_wrapper, str(sibling.relative_to(self.root))))
                    self.assertEqual(0, separate.returncode, separate.stderr)
                    self.assertEqual([TRX], [path.read_text(encoding="utf-8").strip() for path in sibling.glob("*/*.trx")])
                    release.touch()
                    self.assertEqual(0, owner.wait(timeout=120), log.read_text(encoding="utf-8"))
                    self.assertEqual([TRX], [path.read_text(encoding="utf-8").strip() for path in owner_results.glob("*/*.trx")])

    def assert_overlapping_run(self, owner_wrapper, contender_wrapper):
        self.environment["TEST_TRX"] = TRX
        owner_command = self.report_command(owner_wrapper, "reports/nested")
        contender = self.report_command(contender_wrapper, "./reports/./nested/")
        results = self.root / "reports" / "nested"

        with tempfile.TemporaryDirectory() as scratch:
            start, release = (Path(scratch) / name for name in ("start", "release"))
            owner, log = self.start_report_owner(owner_command, start, release)
            calls = self.calls.read_bytes()
            active_report = results / "active-report.txt"
            active_report.write_text("owner data", encoding="utf-8")

            refused = self.run_wrapper(contender)
            self.assertEqual(1, refused.returncode, refused.stderr)
            self.assertIn("in use by another test run", refused.stderr)
            self.assertEqual(calls, self.calls.read_bytes())
            self.assertEqual("owner data", active_report.read_text(encoding="utf-8"))

            separate = self.run_wrapper(self.report_command(contender_wrapper, "independent"))
            self.assertEqual(0, separate.returncode, separate.stderr)
            release.touch()
            self.assertEqual(0, owner.wait(timeout=120), log.read_text(encoding="utf-8"))
            self.assertTrue(list(results.glob("*/*.trx")))

            rerun = self.run_wrapper(contender)
            self.assertEqual(0, rerun.returncode, rerun.stderr)
            self.assertFalse(active_report.exists())
            self.assertTrue(list(results.glob("*/*.trx")))

    def test_results_are_reusable_after_bash_owner_is_killed(self):
        self.assert_recovers_after_owner_exit(self.bash)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_results_are_reusable_after_powershell_owner_is_killed(self):
        self.assert_recovers_after_owner_exit(self.powershell)

    def assert_recovers_after_owner_exit(self, wrapper):
        self.environment["TEST_TRX"] = TRX
        command = self.report_command(wrapper, "recovered")
        with tempfile.TemporaryDirectory() as scratch:
            start, release, finished = (Path(scratch) / name for name in ("start", "release", "finished"))
            temp = tempfile.TemporaryDirectory()
            self.addCleanup(temp.cleanup)
            owner, _ = self.start_report_owner(command, start, release, finished=finished, temp=temp.name)
            # The dotnet global tool can run PowerShell under a separate PID.
            if wrapper == self.powershell:
                powershell_pid = int(start.with_name("powershell.pid").read_text())
                os.kill(powershell_pid, signal.SIGKILL)
            else:
                owner.kill()
            self.assertNotEqual(0, owner.wait(timeout=120))

            # The PowerShell lease helper gets EOF when its owner closes the pipe.
            # Wait for successful lease acquisition without releasing the orphaned dotnet.
            deadline = time.monotonic() + 20
            self.environment["TEST_TRX"] = TRX.replace('executed="1"', 'executed="9"')
            while True:
                result = self.run_wrapper(command)
                if "in use by another test run" not in result.stderr or time.monotonic() >= deadline:
                    break
                time.sleep(0.02)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertFalse(finished.exists())
            release.touch()
            self.wait_for_file(finished)

            reports = list((self.root / "recovered").glob("*/*.trx"))
            self.assertEqual(1, len(reports))
            self.assertEqual(self.environment["TEST_TRX"], reports[0].read_text(encoding="utf-8").strip())

    def test_bash_termination_stops_project_group_and_discards_staging(self):
        dotnet = self.bin / "dotnet"
        dotnet.write_text(
            """#!/usr/bin/env python3
import os
from pathlib import Path
import signal
import sys
import time

control = Path(os.environ["TEST_PROCESS_CONTROL"])
results = Path(sys.argv[sys.argv.index("--results-directory") + 1])
filename = sys.argv[sys.argv.index("--report-xunit-trx-filename") + 1]
(results / filename).write_text('<TestRun><ResultSummary><Counters executed="1" /></ResultSummary></TestRun>')

def record_term(*_):
    (control / "dotnet.term").write_text(str(results.is_dir()))

signal.signal(signal.SIGTERM, record_term)
child = os.fork()
if child == 0:
    def stop(*_):
        (control / "grandchild.term").touch()
        if os.environ["TEST_IGNORE_TERM"] == "0":
            raise SystemExit(0)

    signal.signal(signal.SIGTERM, stop)
    (control / "grandchild.ready").touch()
    while True:
        signal.pause()

while not (control / "grandchild.ready").exists():
    time.sleep(0.01)
receipt = control / "starting"
receipt.write_text(str(os.getpgrp()))
receipt.rename(control / "started")
os.waitpid(child, 0)
""",
            encoding="utf-8",
        )
        command = self.report_command(self.bash, "reports")
        for stubborn in (False, True):
            with self.subTest(stubborn=stubborn), tempfile.TemporaryDirectory() as scratch:
                control = Path(scratch)
                env = dict(
                    self.wrapper_environment(command), TMPDIR=scratch, TMP=scratch, TEMP=scratch,
                    TEST_PROCESS_CONTROL=scratch, TEST_IGNORE_TERM=str(int(stubborn)),
                )
                owner = subprocess.Popen(
                    command, cwd=self.root, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                    start_new_session=True, text=True,
                )
                group = None
                try:
                    start = control / "started"
                    self.wait_for_file(start, owner)
                    group = int(start.read_text())
                    self.assertEqual(1, len(list(control.glob("memories-test-reports-*/*/*.trx"))))
                    owner.terminate()
                    # The project and grandchild inherit stdout. EOF also proves
                    # they closed it, even when the system has yet to reap them.
                    output, _ = owner.communicate(timeout=15)
                    self.assertEqual(128 + signal.SIGTERM, owner.returncode, output)
                    self.assertEqual("True", (control / "dotnet.term").read_text())
                    self.assertTrue((control / "grandchild.term").is_file())
                    self.assertFalse(list(control.glob("memories-test-reports-*")))
                    self.assertEqual([RESULTS_MARKER], [path.name for path in (self.root / "reports").iterdir()])
                finally:
                    if group is not None:
                        self.kill_report_group(group)
                    self.stop_report_owner(owner)
                    owner.stdout.close()

        dotnet.write_text(DOTNET_FIXTURE, encoding="utf-8")
        self.environment["TEST_TRX"] = TRX
        rerun = self.run_wrapper(command)
        self.assertEqual(0, rerun.returncode, rerun.stderr)
        self.assertEqual(1, len(list((self.root / "reports").glob("*/*.trx"))))

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_publishes_available_reports_after_native_failure(self):
        self.environment.update(TEST_DOTNET_EXIT="37", TEST_ATTACHMENT="test trace")
        command = [*self.report_command(self.powershell, "reports"), "-Coverage"]
        reports = self.root / "reports"

        for trx, coverage in ((True, False), (False, True), (True, True)):
            with self.subTest(trx=trx, coverage=coverage):
                self.environment.pop("TEST_TRX", None)
                self.environment.pop("TEST_COVERAGE", None)
                if trx:
                    self.environment["TEST_TRX"] = TRX
                if coverage:
                    self.environment["TEST_COVERAGE"] = "<coverage/>"

                result = self.run_wrapper(command)
                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn("dotnet test failed (37)", result.stderr)
                self.assertEqual(trx, "Executed 1 tests for" in result.stdout)
                self.assertEqual(int(trx), len(list(reports.glob("*/*.trx"))))
                cobertura = list(reports.glob("*/coverage.cobertura.xml"))
                self.assertEqual(int(coverage), len(cobertura))
                if coverage:
                    self.assertEqual("<coverage/>\n", cobertura[0].read_text(encoding="utf-8"))
                (attachment,) = reports.glob("*/attachments/trace.txt")
                self.assertEqual("test trace\n", attachment.read_text(encoding="utf-8"))

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_preserves_native_failure_when_reports_are_missing(self):
        self.environment["TEST_DOTNET_EXIT"] = "37"
        command = self.report_command(self.powershell, "reports")

        for flags in ([], ["-Coverage"]):
            with self.subTest(coverage=bool(flags)):
                result = self.run_wrapper([*command, *flags])
                self.assertEqual(1, result.returncode, result.stderr)
                self.assertIn("native test failure", result.stderr)
                self.assertIn("dotnet test failed (37)", result.stderr)
                self.assertNotIn("Expected TRX file", result.stderr)
                self.assertNotIn("produced no Cobertura report", result.stderr)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_reports_invalid_trx_or_coverage(self):
        command = [*self.report_command(self.powershell, "reports"), "-Coverage"]
        cases = (
            ("<TestRun>", "<coverage/>", "Unable to read TRX file", "*.trx"),
            (TRX, "<coverage>", "Unable to read Cobertura report", "coverage.cobertura.xml"),
            (TRX, '<unexpected LocalName="coverage"/>', "expected a coverage root element", "coverage.cobertura.xml"),
        )
        for status in ("0", "37"):
            for trx, coverage, diagnostic, report_pattern in cases:
                with self.subTest(status=status, diagnostic=diagnostic):
                    self.environment.update(TEST_DOTNET_EXIT=status, TEST_TRX=trx, TEST_COVERAGE=coverage)
                    result = self.run_wrapper(command)
                    self.assertEqual(1, result.returncode, result.stderr)
                    self.assertIn(diagnostic, result.stderr)
                    if status == "37":
                        self.assertIn("dotnet test failed (37)", result.stderr)
                        self.assertTrue(list((self.root / "reports").glob(f"*/{report_pattern}")))
                    else:
                        self.assertNotIn("dotnet test failed", result.stderr)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_requires_reports_after_a_successful_test_process(self):
        command = self.report_command(self.powershell, "reports")
        missing_trx = self.run_wrapper(command)
        self.assertEqual(1, missing_trx.returncode, missing_trx.stderr)
        self.assertIn("Expected TRX file", missing_trx.stderr)

        self.environment["TEST_TRX"] = TRX
        missing_coverage = self.run_wrapper([*command, "-Coverage"])
        self.assertEqual(1, missing_coverage.returncode, missing_coverage.stderr)
        self.assertIn("produced no Cobertura report", missing_coverage.stderr)

    @unittest.skipUnless(shutil.which("pwsh"), "requires PowerShell 7")
    def test_powershell_preserves_benchmark_report_contract(self):
        command = [*self.powershell, "-Filter", "Category=Benchmark", "-ResultsDirectory", "reports"]
        for status, executed, skipped in (("0", 17, 0), ("0", 16, 0), ("0", 17, 1), ("37", 17, 0), ("37", 16, 0)):
            with self.subTest(status=status, executed=executed, skipped=skipped):
                self.environment.update(
                    TEST_DOTNET_EXIT=status,
                    TEST_TRX=f'<TestRun><ResultSummary><Counters executed="{executed}" notExecuted="{skipped}" />'
                    '</ResultSummary></TestRun>',
                )
                result = self.run_wrapper(command)
                valid_counts = executed == 17 and skipped == 0
                self.assertEqual(int(status != "0" or not valid_counts), result.returncode, result.stderr)
                if valid_counts:
                    self.assertIn("Executed 17 tests for", result.stdout)
                else:
                    self.assertIn("must execute exactly 17 tests with none skipped", result.stderr)
                if status != "0":
                    self.assertIn("dotnet test failed (37)", result.stderr)

    def report_command(self, wrapper, destination):
        options = (
            ("--filter", "--results-directory") if wrapper == self.bash else ("-Filter", "-ResultsDirectory")
        )
        return [*wrapper, options[0], "Category=IntegrationSlow", options[1], destination]

    def start_report_owner(self, command, start, release, *, finished=None, temp=None):
        environment = self.wrapper_environment(command)
        if temp is not None:
            environment.update(TMPDIR=str(temp), TMP=str(temp), TEMP=str(temp))
        environment.update(TEST_DOTNET_STARTED=str(start), TEST_DOTNET_RELEASE=str(release))
        if command[0] == self.powershell[0]:
            environment["TEST_POWERSHELL_PID"] = str(start.with_name("powershell.pid"))
        if finished:
            environment["TEST_DOTNET_FINISHED"] = str(finished)
        log = start.with_name("owner.log")
        with log.open("w", encoding="utf-8") as output:
            owner = subprocess.Popen(
                command, cwd=self.root, env=environment, stdout=output, stderr=subprocess.STDOUT,
                start_new_session=True,
            )
        self.addCleanup(self.stop_report_owner, owner)
        self.wait_for_file(start, owner, log)
        group = os.getpgid(int(start.read_text()))
        if group != owner.pid:
            self.addCleanup(self.kill_report_group, group)
        return owner, log

    def kill_report_group(self, group):
        try:
            os.killpg(group, signal.SIGKILL)
        except ProcessLookupError:
            pass

    def stop_report_owner(self, owner):
        self.kill_report_group(owner.pid)
        owner.wait(timeout=120)

    def wait_for_file(self, path, owner=None, log=None):
        deadline = time.monotonic() + 30
        while not path.exists():
            if owner is not None and owner.poll() is not None:
                detail = log.read_text(encoding="utf-8") if log else ""
                self.fail(f"Owner exited before creating {path}: {detail}")
            if time.monotonic() >= deadline:
                self.fail(f"Timed out waiting for {path}")
            time.sleep(0.02)

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

    def test_bash_preserves_unrelated_reports_when_parent_changes_before_dotnet(self):
        self.assert_reports_survive_parent_replacement("before-dotnet", use_symlink=True, native_status=0)

    def test_bash_ignores_unrelated_reports_when_parent_changes_before_collection(self):
        self.assert_reports_survive_parent_replacement("before-collection", use_symlink=False, native_status=37)

    def assert_reports_survive_parent_replacement(self, timing, *, use_symlink, native_status):
        parent = self.root / "reports"
        results = parent / "nested"
        moved = self.root / "moved-reports"
        replacement = self.root / "replacement"
        project = Path(self.read_inventory("integration-slow")[0]).stem
        project_results = replacement / "nested" / project
        project_results.mkdir(parents=True)
        foreign_reports = {
            f"{project}.trx": TRX.replace('executed="1"', 'executed="919"'),
            "fixture.coverage.cobertura.xml": "<foreign-coverage/>",
        }
        for filename, content in foreign_reports.items():
            (project_results / filename).write_text(content, encoding="utf-8")
        results.mkdir(parents=True)
        (results / RESULTS_MARKER).write_bytes(RESULTS_SIGNATURE)
        (results / "stale.txt").write_text("previous run", encoding="utf-8")

        swap = self.bin / "replace-results-parent"
        swap.write_text(
            "#!/bin/sh\n"
            'set -eu\n'
            'mv "$TEST_RESULTS_PARENT" "$TEST_MOVED_PARENT"\n'
            + (
                'ln -s "$TEST_REPLACEMENT_PARENT" "$TEST_RESULTS_PARENT"\n'
                if use_symlink else 'mv "$TEST_REPLACEMENT_PARENT" "$TEST_RESULTS_PARENT"\n'
            ),
            encoding="utf-8",
        )
        swap.chmod(0o755)
        self.environment.update(
            TEST_RESULTS_PARENT=str(parent), TEST_MOVED_PARENT=str(moved),
            TEST_REPLACEMENT_PARENT=str(replacement), TEST_REPLACE_RESULTS_PARENT=str(swap),
            TEST_TRX=TRX, TEST_COVERAGE="<owned-coverage/>", TEST_DOTNET_EXIT=str(native_status),
        )
        if timing == "before-dotnet":
            # The first project mkdir runs after cleanup and before the native process.
            mkdir = self.bin / "mkdir"
            mkdir.write_text(
                '#!/bin/sh\nset -eu\n"$TEST_REPLACE_RESULTS_PARENT"\nexec "$TEST_MKDIR" "$@"\n',
                encoding="utf-8",
            )
            mkdir.chmod(0o755)
            self.environment["TEST_MKDIR"] = shutil.which("mkdir")
        else:
            self.environment["TEST_DOTNET_BEFORE_EXIT"] = str(swap)

        result = self.run_wrapper([
            *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports/nested",
            "--coverage",
        ])
        self.assertNotIn("Executed 919 tests", result.stdout)
        self.assertEqual(RESULTS_SIGNATURE, (moved / "nested" / RESULTS_MARKER).read_bytes())
        self.assertFalse((moved / "nested" / "stale.txt").exists())
        observed_reports = {
            path.name: path.read_text(encoding="utf-8") for path in (results / project).iterdir()
        }
        self.assertEqual(foreign_reports, observed_reports)
        self.assertEqual(native_status or 1, result.returncode, result.stderr)
        self.assertIn("path changed during report generation", result.stderr)

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
        self.environment["TEST_ATTACHMENT"] = "test trace"
        result = self.run_wrapper([
            *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", "reports", "--coverage",
        ])
        self.assertEqual(37, result.returncode, result.stderr)
        reports = list((self.root / "reports").glob("*/coverage.cobertura.xml"))
        self.assertEqual(1, len(reports))
        self.assertEqual("<coverage/>\n", reports[0].read_text(encoding="utf-8"))
        self.assertEqual("<coverage/>\n", (reports[0].parent / "fixture.coverage.cobertura.xml").read_text())
        self.assertEqual("test trace\n", (reports[0].parent / "attachments" / "trace.txt").read_text())

    def test_bash_transfers_reports_when_scratch_is_on_another_filesystem(self):
        (self.bin / "sitecustomize.py").write_text(
            "import errno\n"
            "import os\n"
            "from pathlib import Path\n"
            "import sys\n"
            "\n"
            "def simulate_cross_device_transfer(event, args):\n"
            "    if event == 'os.rename' and 'memories-test-reports-' in os.fspath(args[0]):\n"
            "        raise OSError(errno.EXDEV, 'simulated cross-device rename')\n"
            "    if event == 'shutil.copytree' and Path(args[0]).parent.name.startswith('memories-test-reports-'):\n"
            "        binding = os.environ.get('TEST_TRANSFER_BINDING')\n"
            "        if binding:\n"
            "            os.rename(binding, binding + '-moved')\n"
            "            replacement = Path(binding) / Path(args[1]).parent.name\n"
            "            replacement.mkdir(parents=True)\n"
            "            (replacement / 'sentinel.txt').write_text('unrelated', encoding='utf-8')\n"
            "\n"
            "sys.addaudithook(simulate_cross_device_transfer)\n",
            encoding="utf-8",
        )
        self.environment.update(PYTHONPATH=str(self.bin), TEST_TRX=TRX, TEST_COVERAGE="<coverage/>")
        project = Path(self.read_inventory("integration-slow")[0]).stem
        for name, expected_status in (("stable", 0), ("renamed", 1)):
            with self.subTest(name=name):
                binding = self.root / name
                results = binding
                if expected_status:
                    self.environment["TEST_TRANSFER_BINDING"] = str(binding)
                    results = self.root / f"{name}-moved"
                result = self.run_wrapper([
                    *self.bash, "--filter", "Category=IntegrationSlow", "--results-directory", name, "--coverage",
                ])
                self.assertEqual(expected_status, result.returncode, result.stderr)
                self.assertCountEqual([RESULTS_MARKER, project], [path.name for path in results.iterdir()])
                self.assertEqual(TRX, (results / project / f"{project}.trx").read_text().strip())
                self.assertEqual("<coverage/>\n", (results / project / "coverage.cobertura.xml").read_text())
                if expected_status:
                    self.assertIn("path changed during report generation", result.stderr)
                    unrelated = [path for path in binding.rglob("*") if path.is_file()]
                    self.assertEqual(["sentinel.txt"], [path.name for path in unrelated])
                    self.assertEqual("unrelated", unrelated[0].read_text())

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
        nested_git_empty = self.root / "nested" / "checkout" / ".git" / "objects" / "pack"
        nested_git_empty.mkdir(parents=True)
        for destination in (
            ".git", ".git/objects/pack", ".git/new/nested",
            "./nested/./checkout/.git/objects/pack", "nested/new-checkout/.git/new/nested",
        ):
            with self.subTest(destination=destination):
                refused = self.run_wrapper([*command, destination])
                self.assertNotEqual(0, refused.returncode)
                self.assertIn(".git metadata path", refused.stderr)
                self.assertEqual("git managed", git_file.read_text(encoding="utf-8"))
                self.assertEqual([], list(git_empty.iterdir()))
                self.assertEqual([], list(nested_git_empty.iterdir()))
                self.assertFalse((git_metadata / "new").exists())
                self.assertFalse((self.root / "nested" / "new-checkout").exists())
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

    def wrapper_environment(self, command):
        environment = self.environment.copy()
        if command[0] == self.powershell[0]:
            # The global-tool launcher may need real dotnet before the shell starts.
            environment["TEST_WRAPPER_PATH"] = environment["PATH"]
            environment["PATH"] = os.environ["PATH"]
        return environment

    def run_wrapper(self, command, *, cwd=None):
        # Allow the PowerShell runtime to start on a busy build host.
        return subprocess.run(
            command, cwd=cwd or self.root, env=self.wrapper_environment(command),
            text=True, capture_output=True, check=False, timeout=120,
        )


@unittest.skipUnless(os.name == "nt" and shutil.which("pwsh"), "requires Windows PowerShell 7")
class WindowsTestResultsCleanupTests(unittest.TestCase):
    def test_powershell_refuses_overlap_and_reuses_results_after_owner_exit(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            tools = root / "tools"
            tools.mkdir()
            for name in ("test.ps1", "prepare-test-results.ps1", "test-projects.integration-slow.txt"):
                shutil.copy2(TOOLS / name, tools / name)
            # A native executable is required by ProcessStartInfo on Windows.
            # Use the running interpreter to execute a dummy dotnet test script.
            shutil.copy2(sys.executable, root / "dotnet.exe")
            (root / "test").write_text(
                "import os, sys, time\n"
                "from pathlib import Path\n"
                "if os.environ.get('TEST_DOTNET_STARTED'):\n"
                "    Path('child-pid').write_text(str(os.getpid()))\n"
                "    Path('started').touch()\n"
                "    deadline = time.monotonic() + 60\n"
                "    while not Path('release').exists():\n"
                "        if time.monotonic() >= deadline: raise SystemExit(99)\n"
                "        time.sleep(0.02)\n"
                "args = sys.argv[1:]\n"
                "results = Path(args[args.index('--results-directory') + 1])\n"
                "filename = args[args.index('--report-xunit-trx-filename') + 1]\n"
                "(results / filename).write_text(os.environ['TEST_TRX'], encoding='utf-8')\n"
                "(results / 'fixture.coverage.cobertura.xml').write_text('<coverage/>', encoding='utf-8')\n"
                "if os.environ.get('TEST_DOTNET_STARTED'): Path('finished').touch()\n"
                "raise SystemExit(int(os.environ.get('TEST_DOTNET_EXIT', 0)))\n",
                encoding="utf-8",
            )
            environment = dict(
                os.environ, PATH=os.pathsep.join((str(root), sys.base_prefix, os.environ["PATH"])),
                PYTHONHOME=sys.base_prefix, TEST_TRX=TRX,
            )
            command = [
                shutil.which("pwsh"), "-NoProfile", "-File", str(tools / "test.ps1"),
                "-Filter", "Category=IntegrationSlow", "-ResultsDirectory", "reports", "-Coverage",
            ]
            results = root / "reports"
            release = root / "release"
            log = root / "owner.log"
            with log.open("w", encoding="utf-8") as output:
                owner = subprocess.Popen(
                    command, cwd=root, env=dict(environment, TEST_DOTNET_STARTED="1"),
                    stdout=output, stderr=subprocess.STDOUT,
                )
            child_handle = None
            kernel = ctypes.windll.kernel32
            kernel.OpenProcess.argtypes = (ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong)
            kernel.OpenProcess.restype = ctypes.c_void_p
            kernel.WaitForSingleObject.argtypes = (ctypes.c_void_p, ctypes.c_ulong)
            kernel.CloseHandle.argtypes = (ctypes.c_void_p,)
            try:
                self.wait_for_receipt(root / "started", owner, log)
                child_pid = int((root / "child-pid").read_text())
                child_handle = kernel.OpenProcess(0x00100000, False, child_pid)  # SYNCHRONIZE
                self.assertTrue(child_handle)
                active_report = results / "active.txt"
                active_report.write_text("active report", encoding="utf-8")
                refused = subprocess.run(command, cwd=root, env=environment, capture_output=True, timeout=120)
                self.assertEqual(1, refused.returncode, refused.stderr)
                self.assertIn(b"in use by another test run", refused.stderr)
                self.assertEqual("active report", active_report.read_text(encoding="utf-8"))

                owner.kill()
                owner.wait(timeout=120)
                expected_trx = TRX.replace('executed="1"', 'executed="9"')
                failed = subprocess.run(
                    command, cwd=root, env=dict(environment, TEST_TRX=expected_trx, TEST_DOTNET_EXIT="37"),
                    capture_output=True, timeout=120,
                )
                self.assertEqual(1, failed.returncode, failed.stderr)
                self.assertIn(b"dotnet test failed (37)", failed.stderr)
                self.assertFalse(active_report.exists())
                self.assertFalse((root / "finished").exists())
                release.touch()
                self.wait_for_receipt(root / "finished", None, log)
                reports = list(results.glob("*/*.trx"))
                self.assertEqual(1, len(reports))
                self.assertEqual(expected_trx, reports[0].read_text(encoding="utf-8"))
                self.assertEqual(1, len(list(results.glob("*/coverage.cobertura.xml"))))
            finally:
                release.touch()
                if owner.poll() is None:
                    owner.wait(timeout=120)
                if child_handle:
                    try:
                        self.assertEqual(0, kernel.WaitForSingleObject(child_handle, 30000))
                    finally:
                        kernel.CloseHandle(child_handle)

    def wait_for_receipt(self, receipt, owner, log):
        deadline = time.monotonic() + 30
        while not receipt.exists():
            if (owner is not None and owner.poll() is not None) or time.monotonic() >= deadline:
                self.fail(f"Did not observe {receipt}: {log.read_text(encoding='utf-8', errors='replace')}")
            time.sleep(0.02)

    def test_owned_cleanup_pins_parent_and_destination(self):
        checkpoint = "            CleanEntries(destination, entries, keepMarker: true);"
        attempt_replacement = """
            string binding = Environment.GetEnvironmentVariable("TEST_RESULTS_BINDING");
            try
            {
                Directory.Move(binding, Environment.GetEnvironmentVariable("TEST_MOVED_BINDING"));
                Directory.Move(Environment.GetEnvironmentVariable("TEST_REPLACEMENT_BINDING"), binding);
                File.WriteAllText(Environment.GetEnvironmentVariable("TEST_REPLACEMENT_RECEIPT"), "replaced");
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                File.WriteAllText(Environment.GetEnvironmentVariable("TEST_REPLACEMENT_RECEIPT"), error.HResult.ToString("X8"));
            }
"""
        source = (TOOLS / "prepare-test-results.ps1").read_text(encoding="utf-8")
        self.assertEqual(1, source.count(checkpoint))

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            helper = root / "prepare-test-results.ps1"
            helper.write_text(source.replace(checkpoint, attempt_replacement + checkpoint), encoding="utf-8")
            for binding_name in ("parent", "destination"):
                with self.subTest(binding=binding_name):
                    repo = root / binding_name
                    parent = repo / "reports"
                    results = parent / "nested"
                    binding = parent if binding_name == "parent" else results
                    suffix = results.relative_to(binding)
                    replacement = repo / "unrelated"
                    moved = repo / "moved"
                    receipt = repo / "replacement.txt"
                    environment = dict(
                        os.environ,
                        TEST_RESULTS_BINDING=str(binding), TEST_MOVED_BINDING=str(moved),
                        TEST_REPLACEMENT_BINDING=str(replacement), TEST_REPLACEMENT_RECEIPT=str(receipt),
                    )
                    repo.mkdir()
                    # -File does not split a native argument into a PowerShell array.
                    launcher = repo / "run.ps1"
                    launcher.write_text(
                        'param([string]$Helper, [string]$Repository, [string]$Results)\n'
                        '$ErrorActionPreference = "Stop"\n'
                        '[Console]::OutputEncoding = [Text.Encoding]::UTF8\n'
                        'try { & $Helper $Repository $Results @("reports", "nested") }\n'
                        'catch { [Console]::Error.WriteLine($_.Exception.Message); exit 1 }\n',
                        encoding="utf-8",
                    )
                    command = [shutil.which("pwsh"), "-NoProfile", "-File", str(launcher), str(helper), str(repo), str(results)]
                    claimed = subprocess.run(command, env=environment, text=True, capture_output=True, timeout=120)
                    self.assertEqual(0, claimed.returncode, claimed.stderr)
                    marker = results / RESULTS_MARKER
                    self.assertEqual(RESULTS_SIGNATURE, marker.read_bytes())

                    stale = results / "stale" / "old-report.txt"
                    stale.parent.mkdir()
                    stale.write_text("old report", encoding="utf-8")
                    stale.chmod(0o444)
                    unrelated = replacement / suffix / "stale" / "private.txt"
                    unrelated.parent.mkdir(parents=True)
                    unrelated.write_text("user managed", encoding="utf-8")
                    junction = results / "stale-junction"
                    subprocess.run(
                        [os.environ["ComSpec"], "/d", "/c", "mklink", "/j", str(junction), str(unrelated.parent)],
                        check=True, capture_output=True, timeout=30,
                    )

                    cleaned = subprocess.run(command, env=environment, text=True, capture_output=True, timeout=120)
                    self.assertEqual(0, cleaned.returncode, cleaned.stderr)
                    # Windows protects the path by denying rename while the directory handles are open.
                    self.assertIn(receipt.read_text(encoding="utf-8"), ("80070005", "80070020"))
                    self.assertFalse(moved.exists())
                    self.assertEqual(RESULTS_SIGNATURE, marker.read_bytes())
                    self.assertFalse(stale.parent.exists())
                    self.assertFalse(junction.exists())
                    self.assertEqual("user managed", unrelated.read_text(encoding="utf-8"))

                    marker.unlink()
                    sentinel = results / "unowned.txt"
                    sentinel.write_text("user managed", encoding="utf-8")
                    refused = subprocess.run(command, env=environment, text=True, capture_output=True, timeout=120)
                    self.assertEqual(1, refused.returncode, refused.stderr)
                    self.assertIn("empty or runner-owned directory", refused.stderr)
                    self.assertEqual("user managed", sentinel.read_text(encoding="utf-8"))
