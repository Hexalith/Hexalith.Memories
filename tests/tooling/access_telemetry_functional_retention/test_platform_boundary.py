"""Current Platform route refusal checks; no live lifecycle boundary is claimed."""

from contextlib import ExitStack, redirect_stderr, redirect_stdout
from copy import deepcopy
from datetime import datetime, timedelta, timezone
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
    def invoke_without_effects(self, arguments, *, forbidden_output=()):
        # Nothing may enter retained VM code, acquire old authority, read a
        # credential/input, stage a file, contact an endpoint or spawn a child.
        with ExitStack() as stack:
            forbidden = [stack.enter_context(patch(target)) for target in (
                "access_telemetry_functional_retention.FunctionalAuthority",
                "access_telemetry_functional_retention._historical_prepare_vm_bundle",
                "access_telemetry_functional_retention._historical_run_vm_retention",
                "access_telemetry_functional_retention._run_sequence",
                "subprocess.Popen", "subprocess.run", "urllib.request.urlopen",
                "socket.create_connection", "os.open", "builtins.open", "io.open", "pathlib.Path.open",
                "pathlib.Path.mkdir", "pathlib.Path.read_bytes", "pathlib.Path.read_text",
                "pathlib.Path.write_bytes", "pathlib.Path.write_text",
            )]
            with redirect_stdout(io.StringIO()) as output, redirect_stderr(io.StringIO()) as errors:
                exit_code = cli.main(["--checkpoint", "functional-retention", *arguments])
            stdout, stderr = output.getvalue(), errors.getvalue()
            result = json.loads(stdout)
            for operation in forbidden:
                operation.assert_not_called()
        self.assertEqual("", stderr)
        for value in forbidden_output:
            self.assertNotIn(value, stdout)
            self.assertNotIn(value, stderr)
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
            [item["id"] for item in result["recordedBlockers"]],
            ["platform-descriptor-result", "platform-postgresql-profile",
             "platform-local-capability-binding", "platform-tool-source-alignment"],
        )
        blockers = {item["id"]: item for item in result["recordedBlockers"]}
        self.assertIn("writes both as null", blockers["platform-descriptor-result"]["implementationConstraint"])
        self.assertIn("HEXALITH_G4_TEST_REPORT_PATH", blockers["platform-descriptor-result"]["implementationConstraint"])
        self.assertIn("missing, malformed and failed reports", blockers["platform-descriptor-result"]["nextImplementation"])
        self.assertIn("state.redis", blockers["platform-postgresql-profile"]["gap"])
        self.assertIn("KVM profile", blockers["platform-local-capability-binding"]["gap"])
        self.assertIn("86400-second retention", blockers["platform-local-capability-binding"]["gap"])
        self.assertIn("consistent 3600-second retention", blockers["platform-local-capability-binding"]["nextImplementation"])
        self.assertIn("15-minute MaximumGateLifetime", blockers["platform-local-capability-binding"]["implementationConstraint"])
        self.assertIn("fresh expiry/withdrawal checks", blockers["platform-local-capability-binding"]["nextImplementation"])
        self.assertIn("matching starting Memories source", blockers["platform-tool-source-alignment"]["gap"])
        self.assertIn("verifier requires --checkpoint", blockers["platform-tool-source-alignment"]["implementationConstraint"])
        self.assertIn("fixed selected entry/checkpoint through the existing verifier", blockers["platform-tool-source-alignment"]["nextImplementation"])
        self.assertTrue(all(item["owner"] and item["source"] and item["implementationConstraint"] and item["nextImplementation"] and item["reopen"] for item in blockers.values()))
        self.assertCountEqual(
            [item["id"] for item in result["recordedSupportedContracts"]],
            ["platform-v2-test-selection", "platform-test-lifetime",
             "platform-test-outcomes", "platform-direct-candidate-baseline"],
        )
        supported = {item["id"]: item for item in result["recordedSupportedContracts"]}
        self.assertTrue(all(item["owner"] and item["source"] and item["support"] for item in supported.values()))
        self.assertIn("python3", supported["platform-v2-test-selection"]["support"])
        self.assertIn("14400", supported["platform-test-lifetime"]["support"])
        self.assertIn("900", supported["platform-test-lifetime"]["support"])
        self.assertIn("owner withdrawal", supported["platform-test-outcomes"]["support"])
        self.assertIn("not loaded-artifact evidence", supported["platform-direct-candidate-baseline"]["support"])
        self.assertNotIn("HXR028", json.dumps(result["recordedBlockers"]))
        self.assertNotIn("native test lane is bounded to 15 minutes", json.dumps(result["recordedBlockers"]))
        self.assertNotIn("helper is absent", json.dumps(result["recordedBlockers"]))
        observation = result["blockerObservation"]
        self.assertEqual("2026-10-16T17:23:28.947458Z", observation["observedUtc"])
        self.assertEqual("40ede27ab6f6ccde5628b5ad644c968d155d757e", observation["platformRevision"])
        self.assertEqual("a573a793e0d32db3304c3779ad062edaa51c6441", observation["buildsRevision"])
        self.assertEqual("2a2daacaf39f08503a3f8bd93c665912a68d4abd", observation["directMemoriesRevision"])
        self.assertEqual("6b46089d0dcee22e802d3f10d3a5a46206c54b65319ddf75510efd34653277f6", observation["inspectionReceiptSha256"])
        self.assertTrue(observation["reinspectionRequired"])
        self.assertEqual("prepared-source; actual startup and tool/artifact alignment unverified", result["blockerStatus"])
        self.assertCountEqual([item["id"] for item in result["blockers"]], ["platform-tool-source-alignment", "platform-runtime-artifact", "platform-startup-observations", "platform-real-functional-outcome"])
        self.assertTrue(all(item["owner"] and item["source"] and item["gap"] and item["reopen"] for item in result["blockers"]))
        self.assertEqual("functional-platform-runner-integration-required", result["reason"])
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
        # Readiness alone lacks the required observation/loaded-identity binding.
        # Selected lifetime and outcome metadata now exist in other owning
        # contracts; adding claims here cannot supply the missing observations.
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

    def test_selected_runner_handoff_metadata_does_not_supply_missing_observations(self):
        # Handwritten NativeTestHandoff/NativeTestExecutor/source-mapping values
        # cover the existing selected envelope, including optional peer/UI.
        # Required retention observations remain absent. This is unconditional
        # refusal, not serialized interoperability, expiry or report validation.
        requested_at = datetime(2026, 10, 16, 17, 23, 28, 947458, tzinfo=timezone.utc)
        deadline = requested_at + timedelta(seconds=14400)
        credential_expiry = deadline.replace(microsecond=0) + timedelta(seconds=bool(deadline.microsecond))
        handoff = {
            "HEXALITH_G4_RUN_ID": "selected-run",
            "HEXALITH_G4_EVENTSTORE_URL": "http://127.0.0.1:20012/",
            "HEXALITH_G4_EVENTSTORE_PEER_URL": "http://127.0.0.1:20013/",
            "HEXALITH_G4_UI_URL": "http://127.0.0.1:20014/",
            "HEXALITH_G4_TENANT": "selected-run-tenant",
            "HEXALITH_G4_RESOURCE_NAMESPACE": "selected-run-resources",
            "HEXALITH_G4_DOMAINS": "memories",
            "HEXALITH_G4_TEST_NAME": "functional-retention",
            "HEXALITH_G4_TEST_OWNER": "memories",
            "HEXALITH_G4_TEST_MANIFEST_HASH": "A" * 64,
            "HEXALITH_G4_TEST_ENTRY_HASH": "B" * 64,
            "HEXALITH_G4_TEST_LIFETIME_SECONDS": "14400",
            "HEXALITH_G4_TEST_STOP_SECONDS": "900",
            "HEXALITH_G4_EXECUTION_DEADLINE_UTC": deadline.isoformat(),
            "HEXALITH_G4_CREDENTIAL_EXPIRY_UTC": credential_expiry.isoformat(),
            "HEXALITH_G4_TEST_MODE": "source",
            "HEXALITH_G4_PLAN_PATH": "/unread/plan.json",
            "HEXALITH_G4_READINESS_PATH": "/unread/readiness.json",
            "HEXALITH_G4_SOURCE_MAPPING_PATH": "/unread/source-mapping/mapping.json",
            "HEXALITH_G4_TEST_REPORT_PATH": "/unread/native-tests/native.trx",
            "HEXALITH_G4_ACCESS_TOKEN": "offline-token-never-retained",
            "CustomBeforeDirectoryBuildProps": "/unread/source-mapping/SourceMapping.targets",
            "CustomAfterMicrosoftCommonProps": "/unread/source-mapping/SourceMapping.props",
            "CustomBeforeDirectoryBuildTargets": "/unread/source-mapping/SourceMapping.before-targets.props",
            "CustomAfterDirectoryBuildTargets": "/unread/source-mapping/SourceMapping.late.props",
            "AfterMicrosoftNETSdkTargets": "/unread/source-mapping/SourceMapping.final.targets",
            "HEXALITH_SOURCE_MAPPING_HASH": "C" * 64,
            "Configuration": "Debug",
        }
        with patch.dict("os.environ", handoff):
            self.invoke_without_effects([], forbidden_output=(handoff["HEXALITH_G4_ACCESS_TOKEN"], "/unread/"))

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
                with redirect_stdout(io.StringIO()) as output, redirect_stderr(io.StringIO()) as errors:
                    exit_code = fresh_cli.main(["--checkpoint", "functional-retention"])
                self.assertEqual(1, exit_code)
                self.assertFalse(json.loads(output.getvalue())["ready"])
                self.assertEqual("", errors.getvalue())
                self.assertEqual(sorted(historical), attempted)
                self.assertTrue(historical.isdisjoint(sys.modules))
                for operation in (spawn, run, network, connect):
                    operation.assert_not_called()


if __name__ == "__main__":
    unittest.main()
