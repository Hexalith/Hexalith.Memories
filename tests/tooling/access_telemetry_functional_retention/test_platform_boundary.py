"""Current Platform route refusal checks; no live lifecycle boundary is claimed."""

from contextlib import ExitStack, redirect_stdout
from copy import deepcopy
import builtins
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as functional

spec = importlib.util.spec_from_file_location("platform_lifecycle_cli", ROOT / "tools/verify-access-telemetry-lifecycle.py")
cli = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cli)


class PlatformBoundaryTests(unittest.TestCase):
    def invoke_without_effects(self, arguments):
        # Nothing may enter retained VM code, acquire old authority, read a
        # credential/input, stage a file, contact an endpoint or spawn a child.
        with ExitStack() as stack:
            forbidden = [stack.enter_context(patch(target)) for target in (
                "access_telemetry_functional_retention.FunctionalAuthority",
                "access_telemetry_functional_retention._historical_prepare_vm_bundle",
                "access_telemetry_functional_retention._historical_run_vm_retention",
                "access_telemetry_functional_retention._run_sequence",
                "subprocess.Popen", "subprocess.run", "urllib.request.urlopen",
                "socket.create_connection", "os.open", "pathlib.Path.open",
                "pathlib.Path.mkdir", "pathlib.Path.read_bytes", "pathlib.Path.read_text",
                "pathlib.Path.write_bytes", "pathlib.Path.write_text",
            )]
            with redirect_stdout(io.StringIO()) as output:
                exit_code = cli.main(["--checkpoint", "functional-retention", *arguments])
            result = json.loads(output.getvalue())
            for operation in forbidden:
                operation.assert_not_called()
        self.assertEqual(1, exit_code)
        self.assertEqual("blocked", result["status"])
        self.assertEqual("Hexalith.Platform", result["environmentOwner"])
        self.assertFalse(result["ready"])
        self.assertFalse(result["executableAuthority"])
        self.assertFalse(result["qualificationCredit"])
        self.assertEqual(0, result["targetProcessSpawns"])
        self.assertEqual(0, result["targetCalls"])
        return result

    def test_current_readiness_names_owning_contract_gaps_without_inputs(self):
        result = self.invoke_without_effects([])
        self.assertCountEqual(
            [item["id"] for item in result["blockers"]],
            ["platform-tool-source-alignment", "platform-runtime-artifact",
             "platform-startup-observations", "platform-real-functional-outcome"],
        )
        self.assertTrue(all(item["owner"] and item["source"] and item["gap"] and item["reopen"] for item in result["blockers"]))
        self.assertEqual("functional-platform-runner-integration-required", result["reason"])
        self.assertCountEqual(
            [item["id"] for item in result["recordedBlockers"]],
            ["platform-v2-test-profile", "platform-descriptor-result",
             "platform-test-lifetime", "platform-postgresql-profile",
             "platform-local-capability-binding", "platform-tool-source-alignment"],
        )
        blockers = {item["id"]: item for item in result["recordedBlockers"]}
        self.assertIn("HXR028", blockers["platform-v2-test-profile"]["gap"])
        self.assertIn("15 minutes", blockers["platform-test-lifetime"]["gap"])
        self.assertIn("state.redis", blockers["platform-postgresql-profile"]["gap"])
        self.assertIn("KVM profile", blockers["platform-local-capability-binding"]["gap"])
        self.assertIn("directly declared candidate", blockers["platform-tool-source-alignment"]["gap"])
        self.assertTrue(all(item["owner"] and item["source"] and item["implementationConstraint"] and item["nextImplementation"] and item["reopen"] for item in blockers.values()))
        observation = result["blockerObservation"]
        self.assertEqual("2026-10-16T15:26:54.112019Z", observation["observedUtc"])
        self.assertEqual("b2890d20575356239555df5b113e3100c1d594c1", observation["platformRevision"])
        self.assertEqual("63dda8fc14882aba383888afd7e6505afab1f345", observation["buildsRevision"])
        self.assertEqual("c285b84fddac7dda1efaf4435b8de18694282583", observation["directMemoriesRevision"])
        self.assertEqual("286d4907d2a8701c42b8cebfa2b664e28608b941708a21323038e623a36b9be6", observation["inspectionReceiptSha256"])
        self.assertTrue(observation["reinspectionRequired"])
        self.assertEqual("prepared-source; actual startup and tool/artifact alignment unverified", result["blockerStatus"])
        foreign = self.invoke_without_effects(["--repository-root", "/foreign/uninspected-source"])
        for field in ("blockers", "reason", "blockerStatus"):
            self.assertEqual(result[field], foreign[field])
        self.assertEqual(observation, foreign["blockerObservation"])
        self.assertIn("no supplied source root", foreign["blockerObservation"]["scope"])

    def test_old_preparation_argument_never_creates_or_overwrites_a_bundle(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            absent = root / "absent"
            existing = root / "approved-unused-bundle"
            existing.mkdir()
            original = {"status.json": b'{"budgetConsumed":false,"executableAuthority":false}', "actual-owner-decision.json": b"retained actual decision", "plan.json": b"approved legacy VM plan"}
            for name, raw in original.items():
                (existing / name).write_bytes(raw)
            for destination in (absent, existing):
                self.invoke_without_effects(["--prepare-functional-bundle", str(destination)])
            self.assertFalse(absent.exists())
            self.assertEqual(set(original), {path.name for path in existing.iterdir()})
            self.assertEqual(original, {name: (existing / name).read_bytes() for name in original})

    def test_legacy_approved_plan_cannot_reach_vm_or_change_unused_budget(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            plan = root / "plan.json"
            plan.write_text(json.dumps({"schemaVersion": functional.PLAN_SCHEMA, "target": functional.TARGET, "ready": True, "blockers": [], "statusPath": str(root / "status.json")}))
            status = root / "status.json"
            status.write_text('{"withdrawn":false,"executableAuthority":true,"budgetConsumed":false}')
            before = {item.name: item.read_bytes() for item in root.iterdir()}
            self.invoke_without_effects(["--functional-plan", str(plan), "--approved-plan-sha256", hashlib.sha256(plan.read_bytes()).hexdigest(), "--evidence-root", str(root)])
            self.assertEqual(before, {item.name: item.read_bytes() for item in root.iterdir()})

    def test_insufficient_existing_readiness_and_negative_variants_never_become_authority(self):
        # Insufficient negative sample derived from CompositionReadiness and
        # CompositionDocumentStore's JsonSerializerDefaults.Web/camelCase shape.
        # This handwritten object is not a serialized interoperability test.
        # The owning contract lacks identity, ownership, lifetime and outcome
        # fields; variants add unsupported claims that cannot supply authority.
        readiness = {"schema": "hexalith.g4-run-readiness.v1", "runId": "local-run", "eventStoreEndpoint": "http://127.0.0.1:10001/", "uiEndpoint": None, "resources": [{"name": "memories", "kind": "project", "state": "Healthy"}]}
        variants = {
            "existing-contract": readiness,
            "foreign-run": {**readiness, "runId": "foreign"},
            "partial-startup": {**readiness, "resources": [{"name": "memories", "kind": "project", "state": "Starting"}]},
            "stale": {**readiness, "startupDurationSeconds": 86400},
            "wrong-candidate": {**readiness, "sourceMappingHash": "f" * 64},
            "redis-substitution": {**readiness, "backend": "state.redis"},
            "insufficient-lifetime": {**readiness, "lifetimeSeconds": 900},
            "ownership-conflict": {**readiness, "ownerRunId": "foreign"},
            "withdrawn": {**readiness, "outcome": "Withdrawn"},
            "cleanup-failed": {**readiness, "cleanup": "incomplete"},
            "failure-retained": {**readiness, "outcome": "failed", "retention": "debugging"},
            "caller-claims-success": {**readiness, "outcome": "passed", "cleanup": "complete"},
            "malformed": b'{"schema":',
            "missing": None,
        }
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "unsupported.json"
            for name, candidate in variants.items():
                with self.subTest(name=name):
                    if path.exists():
                        path.unlink()
                    if candidate is not None:
                        path.write_bytes(candidate if isinstance(candidate, bytes) else json.dumps(deepcopy(candidate)).encode())
                    self.invoke_without_effects(["--functional-plan", str(path), "--approved-plan-sha256", "a" * 64, "--evidence-root", directory])

    def test_retained_operational_callables_and_nonoffline_sequence_refuse(self):
        with patch.object(functional, "FunctionalAuthority") as authority, patch("subprocess.Popen") as spawn:
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "historical-vm-route-retired"):
                functional._historical_run_vm_retention(plan_path=Path("missing"), approved_plan_sha256="a" * 64, source_root=ROOT, custody_root=Path("missing"))
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "historical-vm-route-retired"):
                functional._historical_prepare_vm_bundle(Path("missing"), ROOT)
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "historical-vm-route-retired"):
                functional._run_sequence(object(), lambda: None, run_id="legacy", utc_ms=lambda: 0, monotonic=lambda: 0, sleep=lambda _: None)
            authority.assert_not_called()
            spawn.assert_not_called()

    def test_historical_target_helpers_are_not_imported_by_current_route(self):
        historical = {"access_telemetry_functional_target", "access_telemetry_functional_guest"}
        attempted = []
        real_import = builtins.__import__

        def restricted_import(name, *args, **kwargs):
            if name in historical:
                attempted.append(name)
                raise ImportError("historical-import-refused")
            return real_import(name, *args, **kwargs)

        with patch.dict(sys.modules):
            for name in historical | {"access_telemetry_functional_retention", "fresh_platform_lifecycle_cli"}:
                sys.modules.pop(name, None)
            with patch("builtins.__import__", side_effect=restricted_import), patch("subprocess.Popen") as spawn, patch("subprocess.run") as run, patch("urllib.request.urlopen") as network, patch("socket.create_connection") as connect:
                # Prove the restriction traps actual attempted imports, then
                # load the helper and CLI fresh while that same guard is active.
                for name in sorted(historical):
                    with self.assertRaisesRegex(ImportError, "historical-import-refused"):
                        builtins.__import__(name)
                fresh_helper = importlib.import_module("access_telemetry_functional_retention")
                fresh_spec = importlib.util.spec_from_file_location("fresh_platform_lifecycle_cli", ROOT / "tools/verify-access-telemetry-lifecycle.py")
                fresh_cli = importlib.util.module_from_spec(fresh_spec)
                fresh_spec.loader.exec_module(fresh_cli)
                self.assertIsNot(functional, fresh_helper)
                self.assertIsNot(cli, fresh_cli)
                with redirect_stdout(io.StringIO()) as output:
                    exit_code = fresh_cli.main(["--checkpoint", "functional-retention"])
                self.assertEqual(1, exit_code)
                self.assertFalse(json.loads(output.getvalue())["ready"])
                self.assertEqual(sorted(historical), attempted)
                self.assertTrue(historical.isdisjoint(sys.modules))
                for operation in (spawn, run, network, connect):
                    operation.assert_not_called()


if __name__ == "__main__":
    unittest.main()
