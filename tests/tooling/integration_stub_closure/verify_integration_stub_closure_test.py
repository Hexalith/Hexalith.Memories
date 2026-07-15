import importlib.util
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT_PATH = REPO_ROOT / "tools" / "verify-integration-stub-closure.py"
SPEC = importlib.util.spec_from_file_location("verify_integration_stub_closure", SCRIPT_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class VerifyIntegrationStubClosureTests(unittest.TestCase):
    def test_verify_closure_accepts_passed_target_and_structured_skip(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            targets = self._write_targets(
                root,
                ["Example.Tests.RealTests.Passes", "Example.Tests.DeferredTests.Blocked"],
            )
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class RealTests
                {
                    [Fact]
                    public void Passes() { true.ShouldBeTrue(); }
                }
                public sealed class DeferredTests
                {
                    [Fact(Skip = "26.3-EXAMPLE: current blocker. Owner: test owner. Unskip when: seam exists.")]
                    public void Blocked() { }
                }
                """,
            )
            self._write_deferred(root, "26.3-EXAMPLE")
            results = self._write_trx(root, [("Example.Tests.RealTests.Passes", "Passed"), ("Example.Tests.DeferredTests.Blocked", "NotExecuted")])

            report = MODULE.verify_closure(
                targets,
                root / "tests" / "Hexalith.Memories.IntegrationTests",
                root / "deferred-work.md",
                results,
                expected_count=2,
                priority_originals={"Example.Tests.RealTests.Passes"},
            )

            self.assertEqual(1, report.passed)
            self.assertEqual(1, report.skipped)

    def test_verify_closure_rejects_missing_source_mapping(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            targets = self._write_targets(root, ["Example.Tests.MissingTests.Absent"])
            source_root = root / "tests" / "Hexalith.Memories.IntegrationTests"
            source_root.mkdir(parents=True)
            results = self._write_trx(root, [])

            with self.assertRaisesRegex(MODULE.VerificationError, "does not resolve exactly once"):
                MODULE.verify_closure(
                    targets,
                    source_root,
                    root / "deferred-work.md",
                    results,
                    expected_count=1,
                    priority_originals=set(),
                )

    def test_verify_closure_rejects_priority_skip(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = "Example.Tests.PriorityTests.MustPass"
            targets = self._write_targets(root, [target])
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class PriorityTests
                {
                    [Fact(Skip = "26.3-EXAMPLE: current blocker. Owner: test owner. Unskip when: seam exists.")]
                    public void MustPass() { }
                }
                """,
            )
            self._write_deferred(root, "26.3-EXAMPLE")
            results = self._write_trx(root, [(target, "NotExecuted")])

            with self.assertRaisesRegex(MODULE.VerificationError, "priority target"):
                MODULE.verify_closure(
                    targets,
                    root / "tests" / "Hexalith.Memories.IntegrationTests",
                    root / "deferred-work.md",
                    results,
                    expected_count=1,
                    priority_originals={target},
                )

    def test_verify_closure_rejects_absent_trx_result(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = "Example.Tests.RealTests.Passes"
            targets = self._write_targets(root, [target])
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class RealTests
                {
                    [Fact]
                    public void Passes() { true.ShouldBeTrue(); }
                }
                """,
            )
            results = self._write_trx(root, [])

            with self.assertRaisesRegex(MODULE.VerificationError, "no TRX result"):
                MODULE.verify_closure(
                    targets,
                    root / "tests" / "Hexalith.Memories.IntegrationTests",
                    root / "deferred-work.md",
                    results,
                    expected_count=1,
                    priority_originals=set(),
                )

    def test_verify_closure_rejects_runnable_no_op_target_even_when_trx_passed(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = "Example.Tests.FalsePassTests.Empty"
            targets = self._write_targets(root, [target])
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class FalsePassTests
                {
                    [Fact]
                    public void Empty() { /* scenario only */ }
                }
                """,
            )
            results = self._write_trx(root, [(target, "Passed")])

            with self.assertRaisesRegex(MODULE.VerificationError, "assertion-free no-op body"):
                MODULE.verify_closure(
                    targets,
                    root / "tests" / "Hexalith.Memories.IntegrationTests",
                    root / "deferred-work.md",
                    results,
                    expected_count=1,
                    priority_originals=set(),
                )

    def test_verify_closure_rejects_nonaccepted_deferred_entry(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = "Example.Tests.DeferredTests.Blocked"
            targets = self._write_targets(root, [target])
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class DeferredTests
                {
                    [Theory(DisplayName = "blocked", Skip = "26.3-EXAMPLE: current blocker. Owner: test owner. Unskip when: seam exists.")]
                    [InlineData(1)]
                    public void Blocked(int value) { }
                }
                """,
            )
            deferred = self._write_deferred(root, "26.3-EXAMPLE", status="resolved")
            results = self._write_trx(root, [(target, "NotExecuted")])

            with self.assertRaisesRegex(MODULE.VerificationError, "status must be accepted"):
                MODULE.verify_closure(
                    targets,
                    root / "tests" / "Hexalith.Memories.IntegrationTests",
                    deferred,
                    results,
                    expected_count=1,
                    priority_originals=set(),
                )

    def test_verify_closure_rejects_deferred_entry_without_owner_or_reopen_trigger(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = "Example.Tests.DeferredTests.Blocked"
            targets = self._write_targets(root, [target])
            self._write_source(
                root,
                """
                namespace Example.Tests;
                public sealed class DeferredTests
                {
                    [Fact(Skip = "26.3-EXAMPLE: current blocker. Owner: test owner. Unskip when: seam exists.")]
                    public void Blocked() { }
                }
                """,
            )
            deferred = root / "deferred-work.md"
            deferred.write_text(
                "- ID: 26.3-EXAMPLE\n- Status: accepted\n- Rationale: missing seam\n",
                encoding="utf-8",
            )
            results = self._write_trx(root, [(target, "NotExecuted")])

            with self.assertRaises(MODULE.VerificationError) as raised:
                MODULE.verify_closure(
                    targets,
                    root / "tests" / "Hexalith.Memories.IntegrationTests",
                    deferred,
                    results,
                    expected_count=1,
                    priority_originals=set(),
                )
            self.assertIn("rationale must name Owner", str(raised.exception))
            self.assertIn("must define a Re-open trigger", str(raised.exception))

    @staticmethod
    def _write_targets(root: Path, rows: list[str]) -> Path:
        path = root / "targets.txt"
        path.write_text("\n".join(rows) + "\n", encoding="utf-8")
        return path

    @staticmethod
    def _write_source(root: Path, source: str) -> Path:
        path = root / "tests" / "Hexalith.Memories.IntegrationTests" / "Targets.cs"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(textwrap.dedent(source), encoding="utf-8")
        return path

    @staticmethod
    def _write_deferred(root: Path, entry_id: str, status: str = "accepted") -> Path:
        path = root / "deferred-work.md"
        path.write_text(
            f"- ID: {entry_id}\n"
            f"- Status: {status}\n"
            "- Re-open trigger: the deterministic seam exists.\n"
            "- Rationale: The seam is currently unavailable. Owner: test owner.\n",
            encoding="utf-8",
        )
        return path

    @staticmethod
    def _write_trx(root: Path, results: list[tuple[str, str]]) -> Path:
        directory = root / "results"
        directory.mkdir(parents=True, exist_ok=True)
        rows = "".join(
            f'<UnitTestResult testName="{name}" outcome="{outcome}" />'
            for name, outcome in results
        )
        (directory / "results.trx").write_text(
            f'<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>{rows}</Results></TestRun>',
            encoding="utf-8",
        )
        return directory


if __name__ == "__main__":
    unittest.main()
