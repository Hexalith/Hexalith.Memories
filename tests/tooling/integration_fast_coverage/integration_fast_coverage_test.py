import importlib.util
import tempfile
import textwrap
import unittest
from pathlib import Path


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

            with self.assertRaisesRegex(SystemExit, "outcome=Passed"):
                MODULE.passed_tests(results)

    def test_passed_tests_rejects_skipped_only_results(self):
        with tempfile.TemporaryDirectory() as temp:
            results = Path(temp)
            self._write_trx(
                results,
                [("skipped-id", "Example.Tests.RequiredTests", "Skipped", "NotExecuted")],
            )

            with self.assertRaisesRegex(SystemExit, "outcome=Passed"):
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

    @staticmethod
    def _write_trx(
        results_directory: Path,
        tests: list[tuple[str, str, str, str | None]],
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
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
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
