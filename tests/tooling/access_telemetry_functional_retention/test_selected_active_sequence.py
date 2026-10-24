"""Offline real selected sequence with controlled UTC/monotonic, HTTP and SQL; no live credit."""
from contextlib import redirect_stdout
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import threading
import types
import unittest
from unittest.mock import patch
from urllib.error import HTTPError
from urllib.parse import unquote, urlsplit
import xml.etree.ElementTree as xml

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as f
from access_telemetry_producer_common import _qualification_record_ids

# A correct segment whose bootstrap failed once before publishing must still pass: bootstrap is shape-checked only.
def pass_bootstrap(start):
    return {"attempts": 3, "stage": "Published", "failureStage": "RemoteValidation", "failureKind": "Transport", "firstPublishedUtcMs": start - 30000}


_PERSISTENT_DENIAL = {"sanitizerUnavailable": 125, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {"CapabilityUnproven": 125},
                      "sanitizerUnavailableBootstrapStages": {"RemoteValidation": 125}, "lifecycleHealth": "Degraded", "lifecycleReason": "CapabilityUnproven",
                      "bootstrap": {"attempts": 4, "stage": "RemoteValidation", "failureStage": "RemoteValidation", "failureKind": "RemoteDenied", "firstPublishedUtcMs": None}}
# Fully allow-listed, non-zero breakdowns; only the first is internally consistent with 125 rejected records.
CONSISTENT_REJECTIONS = {
    "breakdown-consistent-rejection": _PERSISTENT_DENIAL,
    "breakdown-reason-sum-mismatch": {**_PERSISTENT_DENIAL, "sanitizerUnavailableLifecycleReasons": {"CapabilityUnproven": 124}},
    "breakdown-stage-sum-mismatch": {**_PERSISTENT_DENIAL, "sanitizerUnavailableBootstrapStages": {"RemoteValidation": 100}},
    "breakdown-exceeds-rejected": _PERSISTENT_DENIAL,
    # Every field is individually valid, but no completed attempt cannot carry a failure.
    "breakdown-bootstrap-inconsistent": {**_PERSISTENT_DENIAL, "bootstrap": {**_PERSISTENT_DENIAL["bootstrap"], "attempts": 0}},
}


class SelectedActiveSequenceTests(unittest.TestCase):
    def run_sequence(self, variant="pass"):
        with tempfile.TemporaryDirectory() as directory:
            base = Path(directory)
            root = base / "run"
            candidate = base / "platform" / "references" / "Hexalith.Memories"
            for path in (root / "native-tests", root / "source-mapping", root / "platform-components" / "retention", candidate / "tools", candidate / "module", candidate / "bin"):
                path.mkdir(parents=True, mode=0o700)
            root.chmod(0o700)
            for path in root.rglob("*"):
                if path.is_dir(): path.chmod(0o700)
            clock = types.SimpleNamespace(seconds=1792144800.0, monotonic=100.0)
            def now():
                return datetime.fromtimestamp(clock.seconds, timezone.utc)
            class Utc(datetime):
                @classmethod
                def now(cls, tz=None):
                    return datetime.fromtimestamp(clock.seconds, tz)
            def write(path, value):
                path.write_text(json.dumps(value, separators=(",", ":")))
                path.chmod(0o600)
            entry = candidate / "tools/verify-access-telemetry-lifecycle.py"
            entry.write_text("# inert fixture entry\n")
            manifest = candidate / "module/hexalith.module-manifest.v2.json"
            manifest.write_text("{}")
            mapping = {"Mode": "source", "Root": str(candidate.parent.parent), "ActiveModule": "Hexalith.Platform", "Entries": [{"Identity": "Hexalith.Memories", "Origin": "source", "Path": str(candidate)}], "HasGit": False, "PlatformIdentity": None, "IdentityFindings": []}
            mapping_hash = f._sha(json.dumps(mapping, separators=(",", ":")).encode())
            write(root / "source-mapping/mapping.json", mapping)
            pins = {item["id"]: item["reference"] for item in f.artifact_catalog()["images"]}
            profile = {"purpose": "local-functional-retention", "runId": "offline-run", "sourceMappingHash": mapping_hash, "componentType": "state.postgresql/v2", "runtimeImage": pins["daprd"], "postgresqlImage": pins["postgresql"], "sslMode": "verify-full", "retentionSeconds": 3600, "qualificationCredit": False}
            raw_profile = json.dumps(profile, separators=(",", ":"))
            loaded = {}
            names = {"memories": "Hexalith.Memories.Server.dll", "memories-access-telemetry": "Hexalith.Memories.AccessTelemetry.dll", "memories-access-telemetry-clock": "Hexalith.Memories.AccessTelemetry.Clock.dll"}
            for resource, assembly in names.items():
                path = candidate / "bin" / assembly
                payload = b"a" * 1048576 + b"checked binary tail" if variant in {"large-artifact", "large-tail-drift"} else b"offline mapped fixture bytes"
                path.write_bytes(payload)
                loaded[resource + "/bin/" + assembly] = f._sha(path.read_bytes())
            if variant == "large-tail-drift":
                path.write_bytes(payload[:-1] + b"!")
            selection = {"moduleId": "memories", "name": "functional-retention", "entry": "tools/verify-access-telemetry-lifecycle.py", "mode": "source", "daprRuntimeVersion": "1.18.1", "executionTimeoutSeconds": 14400, "stopTimeoutSeconds": 900, "executionDeadlineUtc": (now() + timedelta(hours=4)).isoformat(), "manifestHash": f._sha(manifest.read_bytes()).upper(), "entryHash": f._sha(entry.read_bytes()).upper()}
            plan = {"schema": "hexalith.g4-run-plan.v1", "runId": "offline-run", "selectedTest": selection, "sourceMapping": {"root": str(candidate.parent.parent)}, "sourceMappingHash": mapping_hash}
            gate_path = root / "platform-components/retention/gate.json"
            access_path = gate_path.with_name("access.json")
            observation = {"isComplete": True, "runId": "offline-run", "moduleId": "memories", "runtimeArtifactVerified": True, "profileJson": raw_profile, "profileHash": f._sha(raw_profile.encode()), "observedBackendIdentity": pins["postgresql"] + "/180006|t|TLSv1.3|telemetry_observer|on/sha256:" + "f" * 64, "observedCandidateHash": f._sha(json.dumps([{"Key": k, "Value": v} for k, v in sorted(loaded.items())], separators=(",", ":")).encode()), "observedRuntimeHash": "c" * 64, "loadedArtifactHashes": loaded, "serviceRetentionSeconds": {name: 3600 for name in names}, "observedAtUtc": now().isoformat(), "accessPath": str(access_path), "gatePath": str(gate_path), "postgresqlContainerId": "d" * 64, "endpoints": {name + suffix: "http://127.0.0.1:12345" for name in names for suffix in ("", "/dapr")}}
            readiness = {"schema": "hexalith.g4-run-readiness.v1", "runId": "offline-run", "selectedTest": selection, "resources": [{"state": "Healthy"}], "testObservation": observation}
            write(root / "plan.json", plan)
            write(root / "readiness.json", readiness)
            write(access_path, {name: {"APP_API_TOKEN": "fixture-app-secret", "DAPR_API_TOKEN": "fixture-dapr-secret"} for name in names})
            def gate():
                write(gate_path, {"schemaVersion": 1, "state": "disabled" if variant == "withdrawal" and clock.monotonic > 1000 else "enabled", "profileSha256": observation["profileHash"], "expiresUtcMs": int(min(clock.seconds + 600, datetime.fromisoformat(selection["executionDeadlineUtc"]).timestamp()) * 1000)})
            gate()
            def sleep(seconds):
                clock.seconds += seconds
                clock.monotonic += seconds
                if variant == "deadline" and clock.monotonic > 1000:
                    clock.seconds += 14400
                    clock.monotonic += 14400
                gate()
            records, emitted = {}, {}
            record_keys = {segment: f.fixed_record_keys("offline-run", segment) for segment in ("cohort-a", "cohort-b")}
            def iso(milliseconds):
                return datetime.fromtimestamp(milliseconds / 1000, timezone.utc).isoformat()
            calls = []
            def http(_, request, **kwargs):
                calls.append(request.full_url)
                headers = {key.lower(): value for key, value in request.headers.items()}
                self.assertIn(headers["dapr-api-token"], {"fixture-app-secret", "fixture-dapr-secret"})
                path = unquote(urlsplit(request.full_url).path)
                if path.endswith("/fixed-workload"):
                    segment = headers["x-hexalith-qualification-segment"]
                    start = int(headers["x-hexalith-qualification-emitted-utc-ms"])
                    emitted[segment] = start
                    ids = _qualification_record_ids("offline-run", segment)
                    for ordinal, (key, identity) in enumerate(zip(record_keys[segment], ids)):
                        records[key] = {"recordId": identity, "envelopeHash": "e" * 64, "emittedAtUtc": iso(start + ordinal), "expiresAtUtc": iso(start + ordinal + 3600000), "acceptedAtUtc": iso(start), "payload": {"message": "original"}}
                    value = {"runId": "offline-run", "segmentId": segment, "writer": "writer-1", "recordIds": ids, "startedUtcMs": start, "finishedUtcMs": start, "emitFinishedUtcMs": start, "acknowledgedUtcMs": start, "resultCount": 1, **{key: 125 for key in ("attempted", "enqueued", "acknowledged", "persisted", "transactionAcknowledgements")}, **{key: 0 for key in ("conflicted", "rejected", "dropped")},
                             # A passing segment may carry any valid bootstrap history, including an earlier failure.
                             "rejectionBreakdown": {"sanitizerUnavailable": 0, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {}, "sanitizerUnavailableBootstrapStages": {},
                                                    "lifecycleHealth": "Healthy", "lifecycleReason": "None", "bootstrap": pass_bootstrap(start)}}
                    rejected_counters = {"enqueued": 0, "acknowledged": 0, "persisted": 0, "transactionAcknowledgements": 0, "rejected": 125}
                    if variant == "pre-enqueue-rejection":
                        value.update(rejected_counters,
                                     rejectionBreakdown={"sanitizerUnavailable": 125, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {"CapabilityUnproven": 120, "RemoteValidationPending": 5, "fixture-response-secret": 1},
                                                         "sanitizerUnavailableBootstrapStages": {"NotStarted": 120, "Configuration": 5, "fixture-response-secret": 1},
                                                         "lifecycleHealth": "Degraded", "lifecycleReason": "CapabilityUnproven", "rawResponse": "fixture-response-secret",
                                                         "bootstrap": {"attempts": 0, "stage": "Configuration", "failureStage": None, "failureKind": None, "firstPublishedUtcMs": None, "rawResponse": "fixture-response-secret"}})
                    elif variant in CONSISTENT_REJECTIONS:
                        value.update(rejected_counters, rejectionBreakdown=deepcopy(CONSISTENT_REJECTIONS[variant]))
                        if variant == "breakdown-exceeds-rejected":
                            value.update(enqueued=25, acknowledged=25, persisted=25, transactionAcknowledgements=25, rejected=100)
                    elif variant == "breakdown-private":
                        value["rejectionBreakdown"] = {"sanitizerUnavailable": "fixture-response-secret", "sanitationFailed": 10**100, "sanitizerUnavailableLifecycleReasons": ["fixture-response-secret"],
                                                       "sanitizerUnavailableBootstrapStages": "fixture-response-secret", "lifecycleHealth": {"credential": "fixture-response-secret"},
                                                       "lifecycleReason": "fixture-response-secret", "bootstrap": ["fixture-response-secret"]}
                    elif variant == "breakdown-bootstrap-private":
                        value["rejectionBreakdown"]["bootstrap"] = {"attempts": True, "stage": "fixture-response-secret", "failureStage": 3, "failureKind": "fixture-response-secret",
                                                                    "firstPublishedUtcMs": "fixture-response-secret", "rawResponse": "fixture-response-secret"}
                    elif variant == "breakdown-nonempty-on-pass":
                        value["rejectionBreakdown"] = {**value["rejectionBreakdown"], "sanitationFailed": 1}
                    elif variant == "accounting-loss":
                        value.update(acknowledged=124, persisted=124, transactionAcknowledgements=124, rejected=1)
                    elif variant == "accounting-private":
                        value.update(persisted="fixture-response-secret", rawResponse={"credential": "fixture-response-secret"})
                    elif variant == "accounting-bounds":
                        value.update(attempted=True, enqueued=-1, acknowledged=126, persisted=10**100, transactionAcknowledgements=1.5, conflicted="fixture-response-secret")
                    elif variant == "accounting-foreign":
                        value["runId"] = "fixture-response-secret"
                elif "/actors/" in path:
                    if variant == "fresh-actor" and not emitted:
                        response = io.BytesIO(b"")
                        response.status = 204
                        return response
                    purged = "cohort-a" in emitted and clock.seconds * 1000 >= emitted["cohort-a"] + 3600124
                    value = {"ReminderSequence": 2 if purged else 1, "LastPurgeUnixMilliseconds": int(clock.seconds * 1000) if purged else 0}
                else:
                    key = path.split("/access-telemetry-store/", 1)[1]
                    value = records.get(key)
                    a_expired = "cohort-a" in emitted and clock.seconds * 1000 >= emitted["cohort-a"] + 3600124
                    if key in record_keys["cohort-a"] and a_expired:
                        value = None
                    if key in record_keys["cohort-b"] and a_expired:
                        if variant == "b-loss": value = None
                        if variant == "b-mutation": value = {**value, "payload": {"message": "mutated"}}
                response = io.BytesIO(json.dumps(value).encode())
                response.status = 200
                return response
            sql_calls = []
            def sql(argv, **kwargs):
                sql_calls.append(argv)
                self.assertEqual("127.0.0.1", argv[argv.index("-h") + 1])
                self.assertIn("PGSSLMODE=verify-full", argv)
                self.assertEqual(f.fixed_physical_bound_query("offline-run"), argv[-1])
                if variant == "sql-latency":
                    clock.seconds += 10
                    clock.monotonic += 10
                rows = []
                # Normal cleanup already removed physical tuples before the first post-expiry poll.
                if "cohort-a" in emitted and clock.seconds * 1000 < emitted["cohort-a"] + 3600000:
                    for ordinal, key in enumerate(record_keys["cohort-a"]):
                        row = {"recordId": records[key]["recordId"], "record": records[key], "expiresUtcMs": emitted["cohort-a"] + ordinal + 3600000 + 1000}
                        rows.append(json.loads(json.dumps(row)))
                    if variant == "missing-ttl": rows.pop()
                    if variant == "early-ttl": rows[0]["expiresUtcMs"] -= 2000
                    if variant == "late-ttl": rows[0]["expiresUtcMs"] += 900000
                    if variant == "row-drift": rows[0]["record"]["payload"]["message"] = "foreign"
                result = {"serverVersionNum": "180006", "ssl": variant != "non-tls", "tlsVersion": "TLSv1.3", "role": "telemetry_observer", "readOnly": "on", "observedUtcMs": int(clock.seconds * 1000), "rows": rows}
                return types.SimpleNamespace(stdout=json.dumps(result).encode(), stderr=b"")
            report = root / "native-tests/native.trx"
            if variant == "persist-failure": write(report.with_name("functional-observations.json"), {"occupied": True})
            if variant == "accounting-occupied": write(report.with_name("cohort-a-accounting.json"), {"occupied": True})
            outside = base / "outside"
            if variant == "symlink-output":
                outside.mkdir(mode=0o700)
                (root / "native-tests").rmdir()
                (root / "native-tests").symlink_to(outside, target_is_directory=True)
            env = {"HEXALITH_G4_TEST_NAME": "functional-retention", "HEXALITH_G4_TEST_OWNER": "memories", "HEXALITH_G4_TEST_MODE": "source", "HEXALITH_G4_RUN_ID": "offline-run", "HEXALITH_G4_PLAN_PATH": str(root / "plan.json"), "HEXALITH_G4_READINESS_PATH": str(root / "readiness.json"), "HEXALITH_G4_TEST_REPORT_PATH": str(report), "HEXALITH_G4_TEST_MANIFEST_HASH": selection["manifestHash"], "HEXALITH_G4_TEST_ENTRY_HASH": selection["entryHash"], "HEXALITH_SOURCE_MAPPING_HASH": mapping_hash}
            original_execute = f._PlatformRetentionConsumer.execute
            def execute(consumer):
                observed = original_execute(consumer)
                if variant == "final-return-drift":
                    artifact = Path("/", *consumer.loaded_inputs[0].parts)
                    artifact.write_bytes(artifact.read_bytes() + b"changed-after-observation-return")
                return observed
            with patch.dict(os.environ, env, clear=True), patch.object(f, "__file__", str(candidate / "tools/access_telemetry_functional_retention.py")), patch.object(f, "datetime", Utc), patch.object(f.time, "time", lambda: clock.seconds), patch.object(f.time, "monotonic", lambda: clock.monotonic), patch.object(f.time, "sleep", sleep), patch("urllib.request.OpenerDirector.open", http), patch.object(f.subprocess, "run", sql), patch.object(f._PlatformRetentionConsumer, "execute", execute), redirect_stdout(io.StringIO()) as output:
                result = f.run_selected_platform_retention()
            self.assertNotIn("fixture-app-secret", output.getvalue())
            self.assertNotIn("fixture-dapr-secret", output.getvalue())
            status = json.loads(output.getvalue())
            captures = sorted((root / "native-tests").glob("cohort-*-accounting.json"))
            if variant == "accounting-occupied":
                self.assertEqual({"occupied": True}, json.loads(captures[0].read_text()))
            else:
                self.assertEqual(0 if variant in {"large-tail-drift", "symlink-output"} else len(emitted), len(captures))
                for capture in captures:
                    raw = capture.read_text()
                    self.assertNotIn("fixture-response-secret", raw)
                    self.assertNotIn("fixture-app-secret", raw)
                    self.assertNotIn("fixture-dapr-secret", raw)
                    snapshot = json.loads(raw)
                    self.assertEqual({"runId", "segmentId", "observedUtcMs", "counters", "rejectionBreakdown", "rejectionBreakdownComplete"}, set(snapshot))
                    # Diagnostic only: dropped fields, missing captures or inconsistent counts are disclosed, never repaired.
                    self.assertIs(snapshot["rejectionBreakdownComplete"], variant not in {"pre-enqueue-rejection", "breakdown-private", "breakdown-bootstrap-private", "breakdown-nonempty-on-pass", "accounting-foreign",
                                                                                          "breakdown-reason-sum-mismatch", "breakdown-stage-sum-mismatch", "breakdown-exceeds-rejected",
                                                                                          "breakdown-bootstrap-inconsistent"})
                    self.assertEqual("offline-run", snapshot["runId"])
                    self.assertEqual(capture.stem.removesuffix("-accounting"), snapshot["segmentId"])
                    self.assertEqual(0o600, capture.stat().st_mode & 0o777)
                    self.assertGreaterEqual(snapshot["observedUtcMs"], emitted[snapshot["segmentId"]])
                    self.assertTrue(all(type(value) is int and 0 <= value <= 125 for value in snapshot["counters"].values()))
                    if variant == "accounting-loss":
                        self.assertEqual({"attempted": 125, "enqueued": 125, "acknowledged": 124, "persisted": 124, "transactionAcknowledgements": 124, "conflicted": 0, "rejected": 1, "dropped": 0}, snapshot["counters"])
                    elif variant == "accounting-private":
                        self.assertNotIn("persisted", snapshot["counters"])
                        self.assertNotIn("rawResponse", raw)
                    elif variant == "accounting-bounds":
                        self.assertEqual({"rejected": 0, "dropped": 0}, snapshot["counters"])
                    elif variant == "accounting-foreign":
                        self.assertEqual({}, snapshot["counters"])
                        self.assertEqual({}, snapshot["rejectionBreakdown"])
                    elif variant == "pre-enqueue-rejection":
                        self.assertEqual({"attempted": 125, "enqueued": 0, "acknowledged": 0, "persisted": 0, "transactionAcknowledgements": 0, "conflicted": 0, "rejected": 125, "dropped": 0}, snapshot["counters"])
                        self.assertEqual({"sanitizerUnavailable": 125, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {"CapabilityUnproven": 120, "RemoteValidationPending": 5},
                                          "sanitizerUnavailableBootstrapStages": {"NotStarted": 120, "Configuration": 5},
                                          "lifecycleHealth": "Degraded", "lifecycleReason": "CapabilityUnproven",
                                          "bootstrap": {"attempts": 0, "stage": "Configuration", "failureStage": None, "failureKind": None, "firstPublishedUtcMs": None}}, snapshot["rejectionBreakdown"])
                        self.assertNotIn("rawResponse", raw)
                    elif variant in CONSISTENT_REJECTIONS:
                        # Retained exactly; only the diagnostic flag discloses an inconsistent sum or excess over the rejections.
                        self.assertEqual(CONSISTENT_REJECTIONS[variant], snapshot["rejectionBreakdown"])
                        self.assertEqual(100 if variant == "breakdown-exceeds-rejected" else 125, snapshot["counters"]["rejected"])
                    elif variant == "breakdown-private":
                        self.assertEqual({}, snapshot["rejectionBreakdown"])
                    elif variant == "breakdown-bootstrap-private":
                        self.assertEqual({}, snapshot["rejectionBreakdown"]["bootstrap"])
                        self.assertEqual({}, snapshot["rejectionBreakdown"]["sanitizerUnavailableBootstrapStages"])
                    elif variant in {"pass", "accounting-loss"}:
                        self.assertEqual({"sanitizerUnavailable": 0, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {}, "sanitizerUnavailableBootstrapStages": {},
                                          "lifecycleHealth": "Healthy", "lifecycleReason": "None", "bootstrap": pass_bootstrap(emitted[snapshot["segmentId"]])},
                                         snapshot["rejectionBreakdown"])
            if variant in {"pass", "sql-latency", "fresh-actor", "large-artifact"}:
                self.assertEqual(0, result, status)
                self.assertEqual("functional-observations-passed", status["status"])
                detail = json.loads(report.with_name("functional-observations.json").read_text())
                self.assertEqual(125, len(detail["ttlEvidence"]["preExpiry"]["rows"]))
                self.assertEqual([], detail["ttlEvidence"]["lastObservation"]["rows"])
                self.assertEqual([], detail["purge"]["actorDeletedRecordIds"])
                self.assertEqual(125, len(detail["purge"]["ttlAbsentRecordIds"]))
                self.assertGreaterEqual(detail["cohortClocks"]["cohort-b"]["monotonicSeconds"] - detail["cohortClocks"]["cohort-a"]["monotonicSeconds"], 1800)
                self.assertTrue(calls and len(sql_calls) >= 3)
                self.assertEqual([], detail["ttlEvidence"]["preflight"]["rows"])
                absent = variant == "fresh-actor"
                self.assertEqual({"httpStatus": 204 if absent else 200, "statePresent": not absent, "reminderSequence": 0 if absent else 1,
                                  "sequenceSource": "state-contract-initial-zero" if absent else "stored-state"}, detail["initialActorState"])
                self.assertEqual(0 if absent else 1, detail["purge"]["reminderSequenceBefore"])
                self.assertGreater(detail["purge"]["reminderSequenceAfter"], detail["purge"]["reminderSequenceBefore"])
                self.assertGreater(detail["purge"]["progressObservedUtcMs"], 0)
                first_expiry = min(row["record"]["expiresAtUtc"] for row in detail["ttlEvidence"]["preExpiry"]["rows"])
                self.assertLess(detail["ttlEvidence"]["preExpiry"]["observedUtcMs"], int(datetime.fromisoformat(first_expiry).timestamp() * 1000))
            else:
                self.assertEqual(1, result, status)
                self.assertEqual("rejected", status["status"])
                reasons = {"accounting-loss": "functional-accounting-loss", "pre-enqueue-rejection": "functional-accounting-loss", **{name: "functional-accounting-loss" for name in CONSISTENT_REJECTIONS}, "breakdown-private": "functional-accounting-rejection-breakdown", "breakdown-bootstrap-private": "functional-accounting-rejection-breakdown", "breakdown-nonempty-on-pass": "functional-accounting-rejection-breakdown", "accounting-private": "functional-accounting-schema", "accounting-bounds": "functional-accounting-loss", "accounting-foreign": "functional-accounting-identity", "accounting-occupied": "functional-selected-input-refused", "large-tail-drift": "functional-selected-loaded-artifact-drift", "final-return-drift": "functional-selected-loaded-artifact-drift", "b-loss": "functional-selected-record-missing", "b-mutation": "functional-selected-canonical-cohort-changed", "missing-ttl": "functional-selected-ttl-bound-missing", "early-ttl": "functional-selected-ttl-bound-drift", "late-ttl": "functional-selected-ttl-bound-drift", "row-drift": "functional-selected-ttl-bound-drift", "non-tls": "functional-selected-postgres-session", "deadline": "functional-selected-expiry-or-clock-step", "withdrawal": "functional-selected-gate-withdrawn-or-expired", "persist-failure": "functional-selected-input-refused", "symlink-output": "functional-selected-native-output-refused"}
                self.assertEqual(reasons[variant], status["reason"])
                if variant == "final-return-drift":
                    self.assertFalse(report.with_name("functional-observations.json").exists())
            if variant == "symlink-output": self.assertEqual([], list(outside.iterdir()))
            else:
                counters = xml.parse(report).find("{*}ResultSummary/{*}Counters")
                self.assertEqual("1" if variant in {"pass", "sql-latency", "fresh-actor", "large-artifact"} else "0", counters.attrib["passed"])
                if result:
                    self.assertEqual(status["reason"], xml.parse(report).find("{*}Results/{*}UnitTestResult/{*}Output/{*}ErrorInfo/{*}Message").text)
            return status

    def test_accounting_failure_retains_bounded_rejection_counters_before_validation(self):
        self.run_sequence("accounting-loss")

    def test_accounting_capture_excludes_untrusted_fields_values_and_foreign_identities(self):
        for variant in ("accounting-private", "accounting-bounds", "accounting-foreign"):
            with self.subTest(variant=variant):
                self.run_sequence(variant)

    def test_pre_enqueue_rejection_retains_bounded_breakdown_and_still_fails_accounting(self):
        self.run_sequence("pre-enqueue-rejection")

    def test_rejection_breakdown_values_are_allow_listed_and_must_be_consistent(self):
        for variant in ("breakdown-private", "breakdown-bootstrap-private", "breakdown-nonempty-on-pass"):
            with self.subTest(variant=variant):
                self.run_sequence(variant)

    def test_consistent_nonzero_breakdown_is_complete_and_still_fails_accounting(self):
        self.run_sequence("breakdown-consistent-rejection")

    def test_inconsistent_breakdown_is_retained_but_disclosed_incomplete(self):
        for variant in ("breakdown-reason-sum-mismatch", "breakdown-stage-sum-mismatch", "breakdown-exceeds-rejected", "breakdown-bootstrap-inconsistent"):
            with self.subTest(variant=variant):
                self.run_sequence(variant)

    def test_accounting_capture_cannot_overwrite_an_existing_observation(self):
        self.run_sequence("accounting-occupied")

    def test_loaded_binary_over_json_limit_is_fully_hashed_and_tail_drift_refused(self):
        self.run_sequence("large-artifact")
        self.run_sequence("large-tail-drift")

    def test_real_selected_sequence_reconciles_cleanup_before_first_poll(self):
        self.run_sequence()

    def test_real_selected_sequence_captures_ttl_before_sql_latency_crosses_last_checkpoint(self):
        self.run_sequence("sql-latency")

    def test_real_selected_sequence_records_fresh_http_204_baseline_and_later_real_progress(self):
        self.run_sequence("fresh-actor")

    def test_final_observation_return_rechecks_loaded_inputs_before_success_is_recorded(self):
        self.run_sequence("final-return-drift")

    def test_actor_absence_requires_initial_http_204_with_empty_body(self):
        consumer = object.__new__(f._PlatformRetentionConsumer)
        consumer.check = lambda: None
        consumer.access = {"memories-access-telemetry": {"DAPR_API_TOKEN": "fixture-dapr-secret"}}
        consumer.observation = {"endpoints": {"memories-access-telemetry/dapr": "http://127.0.0.1:12345"}}
        for status, body, initial, accepted in ((204, b"", True, True), (204, b"", False, False),
                                                (200, b"", True, False), (200, b"null", True, False),
                                                (204, b"null", True, False), (200, b"{}", True, False)):
            response = io.BytesIO(body)
            response.status = status
            with self.subTest(status=status, body=body, initial=initial), patch("urllib.request.OpenerDirector.open", return_value=response):
                if accepted:
                    self.assertIsNone(consumer.actor(initial=initial))
                else:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-actor-state"):
                        consumer.actor(initial=initial)
        with HTTPError("http://127.0.0.1:12345", 404, "Not Found", {}, None) as failure, patch("urllib.request.OpenerDirector.open", side_effect=failure):
            with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-request-failed"):
                consumer.actor(initial=True)

    def test_initial_zero_does_not_supply_later_sequence_or_purge_progress(self):
        consumer = object.__new__(f._PlatformRetentionConsumer)
        records = {"record-a": {"expiresUtcMs": 1000}}
        for sequence, progress, expected in ((0, 1000, False), (1, 0, False), (1, 1000, True)):
            with self.subTest(sequence=sequence, progress=progress), patch.object(consumer, "request", return_value=(200, json.dumps({"ReminderSequence": sequence, "LastPurgeUnixMilliseconds": progress}).encode())):
                state = consumer.actor()
                observation = {"consistency": "strong", "absentRecordIds": ["record-a"], "removedIndexIds": ["record-a"],
                               "reminderSequenceBefore": 0, "reminderSequenceAfter": state["ReminderSequence"],
                               "progressObservedUtcMs": state["LastPurgeUnixMilliseconds"], "actorDeletedRecordIds": [], "ttlAbsentRecordIds": ["record-a"]}
                self.assertEqual(expected, f.validate_purge(observation, records, now_ms=1000))
                if not expected:
                    with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-purge-deadline"):
                        f.validate_purge(observation, records, now_ms=901000)

    def test_active_sql_uses_pinned_postgresql_v2_column_and_schema_contract(self):
        # Dapr v1.18.1/go.mod pins components-contrib v1.18.0. Its
        # state/postgresql/v2/postgresql.go CREATE TABLE defines value bytea
        # and expires_at timestamptz; metadata.go TableName appends "state".
        # https://raw.githubusercontent.com/dapr/components-contrib/v1.18.0/state/postgresql/v2/postgresql.go
        # https://raw.githubusercontent.com/dapr/components-contrib/v1.18.0/state/postgresql/v2/metadata.go
        query = f.fixed_physical_bound_query("offline-run")
        self.assertIn("FROM access_telemetry.lifecycle_state WHERE key IN (", query)
        self.assertIn("convert_from(value,'UTF8')::jsonb", query)
        self.assertIn("extract(epoch FROM expires_at)", query)
        self.assertNotIn("expiredate", query)
        self.assertIn("expiredate", f.fixed_expired_physical_query("offline-run", "cohort-a"))

    def test_strong_reads_keep_fixed_sixteen_concurrency_and_key_order(self):
        consumer = object.__new__(f._PlatformRetentionConsumer)
        consumer.segments = {"cohort-a": 0}
        consumer.check = lambda: None
        barrier = threading.Barrier(16)
        lock = threading.Lock()
        active = maximum = admitted = 0
        keys = f.fixed_record_keys("offline-run", "cohort-a")
        def request(resource, path, **kwargs):
            nonlocal active, maximum, admitted
            self.assertEqual("memories-access-telemetry/dapr", resource)
            self.assertTrue(path.endswith("?consistency=strong&metadata.partitionKey=access-telemetry"))
            key = unquote(urlsplit(path).path).split("/access-telemetry-store/", 1)[1]
            self.assertIn(key, keys)
            with lock:
                active += 1
                admitted += 1
                ordinal = admitted
                maximum = max(maximum, active)
            if ordinal <= 16:
                barrier.wait(timeout=5)
            with lock:
                active -= 1
            return key
        consumer.request = request
        observed = consumer.states("offline-run", "cohort-a")
        self.assertEqual(16, maximum)
        self.assertEqual(125, admitted)
        self.assertEqual(list(keys), [item["key"] for item in observed])
        self.assertEqual(list(keys), [item["value"] for item in observed])

    def test_strong_read_withdrawal_admits_no_later_batch(self):
        consumer = object.__new__(f._PlatformRetentionConsumer)
        consumer.segments = {"cohort-a": 0}
        withdrawn = threading.Event()
        barrier = threading.Barrier(16)
        admitted = []
        def check():
            f._require(not withdrawn.is_set(), "selected-gate-withdrawn-or-expired")
        consumer.check = check
        def request(resource, path, **kwargs):
            admitted.append(path)
            barrier.wait(timeout=5)
            withdrawn.set()
            return None
        consumer.request = request
        with self.assertRaisesRegex(f.FunctionalRetentionError, "functional-selected-gate-withdrawn-or-expired"):
            consumer.states("offline-run", "cohort-a")
        self.assertEqual(16, len(admitted))

    def test_real_selected_sequence_refuses_behavior_clock_and_custody_failures(self):
        for variant in ("b-loss", "b-mutation", "missing-ttl", "early-ttl", "late-ttl", "row-drift", "non-tls", "deadline", "withdrawal", "persist-failure", "symlink-output"):
            with self.subTest(variant=variant):
                self.run_sequence(variant)


if __name__ == "__main__":
    unittest.main()
