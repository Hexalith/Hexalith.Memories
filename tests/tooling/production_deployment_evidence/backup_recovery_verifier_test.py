import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from typing import Sequence


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools" / "verify-backup-recovery.py"
SPEC = importlib.util.spec_from_file_location("verify_backup_recovery", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VERIFIER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VERIFIER)


HEALTHY_INFO = """loading:0
aof_enabled:1
aof_last_write_status:ok
aof_last_bgrewrite_status:ok
"""


class FakeRunner:
    def __init__(
        self,
        *,
        summary: str = "acme\ttenant\t3\t1\t2",
        redis_info: str = HEALTHY_INFO,
        falkor_info: str = HEALTHY_INFO,
        memory_units: int = 3,
        semantic_chunks: int = 4,
        missing_semantic_units: int = 0,
        cases: int = 2,
        graph_edges: int = 4,
    ) -> None:
        self.summary = summary
        self.redis_info = redis_info
        self.falkor_info = falkor_info
        self.memory_units = memory_units
        self.semantic_chunks = semantic_chunks
        self.missing_semantic_units = missing_semantic_units
        self.cases = cases
        self.graph_edges = graph_edges
        self.commands: list[list[str]] = []

    def __call__(self, command: Sequence[str]) -> str:
        values = list(command)
        self.commands.append(values)
        joined = " ".join(values)
        if values[0] == "jq":
            return self.summary
        if "get pvc data-redis-stack-0" in joined or "get pvc data-falkordb-0" in joined:
            return "Bound"
        if "INFO persistence" in joined:
            return self.redis_info if "redis-stack-0" in joined else self.falkor_info
        if "missing_semantic_units" in joined:
            return str(self.missing_semantic_units)
        if ":mu:*" in joined:
            return str(self.memory_units)
        if ":vec:*" in joined:
            return str(self.semantic_chunks)
        if ":case:*" in joined:
            return str(self.cases)
        if "GRAPH.QUERY" in joined:
            return f'count(r)\n{self.graph_edges}\nCached execution: 0\n'
        raise AssertionError(f"Unexpected command: {values}")


class BackupRecoveryVerifierTests(unittest.TestCase):
    def make_export(self, root: Path) -> Path:
        path = root / "tenant-export.json"
        path.write_text('{"fixture":true}\n', encoding="utf-8")
        return path

    def verify(self, root: Path, runner: FakeRunner):
        return VERIFIER.verify_recovery(
            namespace="hexalith-memories",
            tenant_id="acme",
            export_path=self.make_export(root),
            runner=runner,
        )

    def test_verified_recovery_emits_sanitized_exact_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner()

            evidence = self.verify(Path(temp), runner)

            self.assertEqual("verified", evidence["status"])
            self.assertEqual(4, evidence["expected"]["totalGraphEdges"])
            self.assertEqual(4, evidence["actual"]["totalGraphEdges"])
            self.assertEqual(4, evidence["actual"]["semanticChunks"])
            self.assertEqual(0, evidence["actual"]["memoryUnitsMissingSemanticChunks"])
            all_commands = "\n".join(" ".join(command) for command in runner.commands)
            self.assertIn("REDISCLI_AUTH", all_commands)
            self.assertNotIn("redis-cli -a", all_commands)
            self.assertNotIn("password", json.dumps(evidence).lower())

    def test_unhealthy_aof_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(redis_info=HEALTHY_INFO.replace("aof_enabled:1", "aof_enabled:0"))

            with self.assertRaisesRegex(VERIFIER.VerificationError, "aof_enabled"):
                self.verify(Path(temp), runner)

    def test_missing_semantic_chunks_fails_closed(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(semantic_chunks=2)

            with self.assertRaisesRegex(VERIFIER.VerificationError, "semantic-chunk count"):
                self.verify(Path(temp), runner)

    def test_missing_per_unit_semantic_coverage_fails_despite_sufficient_total_chunks(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(semantic_chunks=4, missing_semantic_units=2)

            with self.assertRaisesRegex(VERIFIER.VerificationError, "no active semantic chunk"):
                self.verify(Path(temp), runner)

    def test_graph_count_includes_rebuilt_contains_edges(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(graph_edges=1)

            with self.assertRaisesRegex(VERIFIER.VerificationError, "exported edges plus rebuilt CONTAINS"):
                self.verify(Path(temp), runner)

    def test_case_scope_export_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(summary="acme\tcase\t3\t1\t1")

            with self.assertRaisesRegex(VERIFIER.VerificationError, "consolidated tenant-scope"):
                self.verify(Path(temp), runner)

    def test_wrong_tenant_export_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            runner = FakeRunner(summary="other\ttenant\t3\t1\t2")

            with self.assertRaisesRegex(VERIFIER.VerificationError, "does not match"):
                self.verify(Path(temp), runner)

    def test_evidence_write_is_atomic(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            output = root / "evidence" / "result.json"

            VERIFIER.write_evidence(output, {"status": "verified"})

            self.assertEqual({"status": "verified"}, json.loads(output.read_text(encoding="utf-8")))
            self.assertFalse(output.with_suffix(".json.part").exists())


if __name__ == "__main__":
    unittest.main()
