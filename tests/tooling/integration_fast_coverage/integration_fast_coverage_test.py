from __future__ import annotations

from contextlib import redirect_stderr
import importlib.util
from io import StringIO
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT_PATH = REPO_ROOT / "tools" / "verify-integration-fast-coverage.py"
SPEC = importlib.util.spec_from_file_location("verify_integration_fast_coverage", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class IntegrationFastCoverageVerifierTests(unittest.TestCase):
    def test_passed_tests_accepts_only_mapped_passed_results(self):
        with tempfile.TemporaryDirectory() as temp:
            results = Path(temp)
            self._write_trx(
                results,
                [
                    ("passed-id", "Example.Tests.RequiredTests", "Passes", "Passed"),
                    ("skipped-id", "Example.Tests.RequiredTests", "Skipped", "NotExecuted"),
                    ("failed-id", "Example.Tests.OtherTests", "Fails", "Failed"),
                ],
            )

            passed = MODULE.passed_tests(results)

            self.assertEqual({("Example.Tests.RequiredTests", "Passes")}, passed)

    def test_passed_tests_rejects_discovered_only_definitions(self):
        with tempfile.TemporaryDirectory() as temp:
            results = Path(temp)
            self._write_trx(
                results,
                [("discovered-id", "Example.Tests.RequiredTests", "Discovered", None)],
            )

            with self.assertRaisesRegex(MODULE.NoUsableResultsError, "outcome=Passed"):
                MODULE.passed_tests(results)

    def test_passed_tests_rejects_skipped_only_results(self):
        with tempfile.TemporaryDirectory() as temp:
            results = Path(temp)
            self._write_trx(
                results,
                [("skipped-id", "Example.Tests.RequiredTests", "Skipped", "NotExecuted")],
            )

            with self.assertRaisesRegex(MODULE.NoUsableResultsError, "outcome=Passed"):
                MODULE.passed_tests(results)

    def test_class_requirement_needs_a_passed_method_in_that_class(self):
        requirements = {"required": ("Example.Tests.RequiredTests", None)}
        passed = {("Example.Tests.OtherTests", "Passes")}

        self.assertEqual(requirements, MODULE.missing_requirements(requirements, passed))

    def test_exact_method_requirement_rejects_a_different_passed_method(self):
        requirements = {
            "matrix": ("Example.Tests.OpenBaoTests", "RequiredMatrix"),
        }
        passed = {("Example.Tests.OpenBaoTests", "DifferentMethod")}

        self.assertEqual(requirements, MODULE.missing_requirements(requirements, passed))

    def test_load_requirements_supports_class_and_exact_method_entries(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "requirements.txt"
            path.write_text(
                textwrap.dedent(
                    """
                    # fixture
                    class-surface|Example.Tests.ClassTests
                    method-surface|Example.Tests.MethodTests|RequiredMethod
                    """
                ),
                encoding="utf-8",
            )

            requirements = MODULE.load_requirements(path)

            self.assertEqual(
                {
                    "class-surface": ("Example.Tests.ClassTests", None),
                    "method-surface": ("Example.Tests.MethodTests", "RequiredMethod"),
                },
                requirements,
            )

    def test_cli_distinguishes_success_missing_surfaces_and_unusable_results(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            results = root / "results"
            results.mkdir()
            requirements = root / "requirements.txt"
            requirements.write_text("required|Example.Required|Runs\n", encoding="utf-8")
            empty = self._verify(results, requirements)
            self.assertEqual(3, empty.returncode, empty.stderr)
            self.assertIn("No TRX files", empty.stderr)

            for namespace in ("", ' xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"'):
                with self.subTest(namespace=namespace):
                    self._write_trx(results, [("id", "Example.Required", "Runs", "Passed")], namespace)
                    complete = self._verify(results, requirements)
                    self.assertEqual(0, complete.returncode, complete.stderr)
                    self.assertIn("required surfaces satisfied", complete.stdout)

            self._write_trx(results, [("id", "Example.Other", "Runs", "Passed")])
            missing = self._verify(results, requirements)
            self.assertEqual(1, missing.returncode, missing.stderr)
            self.assertIn("required: Example.Required.Runs", missing.stderr)

            trx = results / "synthetic.trx"
            trx.write_text(trx.read_text().replace('<UnitTest id="id"', '<UnitTest id="other-id"'), encoding="utf-8")
            unmapped = self._verify(results, requirements)
            self.assertEqual(3, unmapped.returncode, unmapped.stderr)
            self.assertIn("did not map to TestMethod definitions", unmapped.stderr)

            self._write_trx(results, [("id", "Example.Required", "Runs", "Passed")])
            report = trx.read_text(encoding="utf-8")
            trx.write_text(report.replace("<Results>", "<Other>", 1).replace("</Results>", "</Other>", 1), encoding="utf-8")
            unrelated = self._verify(results, requirements)
            self.assertEqual(3, unrelated.returncode, unrelated.stdout + unrelated.stderr)

    def test_cli_reports_invalid_input_without_traceback(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            results = root / "results"
            results.mkdir()
            requirements = root / "requirements.txt"
            missing = self._verify(results, requirements)
            self.assertEqual(2, missing.returncode, missing.stderr)
            self.assertIn(str(requirements), missing.stderr)
            self.assertNotIn("Traceback", missing.stderr)

            requirements.write_text("invalid row\n", encoding="utf-8")
            malformed = self._verify(results, requirements)
            self.assertEqual(2, malformed.returncode, malformed.stderr)
            self.assertIn("Invalid requirement line", malformed.stderr)

            requirements.write_text("required|Example.Required|Runs\n", encoding="utf-8")
            trx = results / "malformed.trx"
            for xml in ("<TestRun>", "<OtherDocument />", '<TestRun xmlns="urn:unrelated" />'):
                with self.subTest(xml=xml):
                    trx.write_text(xml, encoding="utf-8")
                    malformed = self._verify(results, requirements)
                    self.assertEqual(2, malformed.returncode, malformed.stderr)
                    self.assertIn(str(trx), malformed.stderr)
                    self.assertNotIn("Traceback", malformed.stderr)

            wrong_path = self._verify(trx, requirements)
            self.assertEqual(2, wrong_path.returncode, wrong_path.stderr)
            self.assertIn("not a directory", wrong_path.stderr)

    def test_cli_reports_scan_failure_even_when_another_trx_passed(self):
        with tempfile.TemporaryDirectory() as temp:
            results = Path(temp)
            nested = results / "unreadable"
            nested.mkdir()
            requirements = results / "requirements.txt"
            requirements.write_text("required|Example.Required|Runs\n", encoding="utf-8")
            self._write_trx(results, [("id", "Example.Required", "Runs", "Passed")])
            scandir = MODULE.os.scandir

            def scan(path):
                if Path(path) == nested:
                    raise PermissionError(f"Cannot scan {path}")
                return scandir(path)

            command = [str(SCRIPT_PATH), "--results-directory", str(results), "--requirements", str(requirements)]
            with mock.patch.object(MODULE.os, "scandir", side_effect=scan), mock.patch.object(sys, "argv", command):
                with redirect_stderr(StringIO()) as error:
                    self.assertEqual(2, MODULE.main())
            self.assertIn(f"Cannot scan {nested}", error.getvalue())

    @staticmethod
    def _verify(results: Path, requirements: Path) -> subprocess.CompletedProcess:
        return subprocess.run(
            [sys.executable, str(SCRIPT_PATH), "--results-directory", str(results), "--requirements", str(requirements)],
            text=True, capture_output=True, check=False, timeout=10,
        )

    @staticmethod
    def _write_trx(
        results_directory: Path,
        tests: list[tuple[str, str, str, str | None]],
        namespace: str = ' xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"',
    ) -> None:
        result_rows = "\n".join(
            f'<UnitTestResult testId="{test_id}" testName="{class_name}.{method_name}" outcome="{outcome}" />'
            for test_id, class_name, method_name, outcome in tests
            if outcome is not None
        )
        definition_rows = "\n".join(
            textwrap.dedent(
                f"""
                <UnitTest id="{test_id}" name="{class_name}.{method_name}">
                  <TestMethod className="{class_name}" name="{method_name}" />
                </UnitTest>
                """
            ).strip()
            for test_id, class_name, method_name, _ in tests
        )
        trx = textwrap.dedent(
            f"""
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun{namespace}>
              <Results>
                {result_rows}
              </Results>
              <TestDefinitions>
                {definition_rows}
              </TestDefinitions>
            </TestRun>
            """
        ).lstrip()
        (results_directory / "synthetic.trx").write_text(trx, encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
