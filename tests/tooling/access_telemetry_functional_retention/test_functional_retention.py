"""Historical VM orchestration and reusable semantic regressions; offline only."""

from contextlib import redirect_stdout
from copy import deepcopy
from datetime import datetime, timedelta, timezone
import hashlib
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import tarfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as functional
import verify_access_telemetry_lifecycle as full
from access_telemetry_producer_common import _qualification_record_ids


class Clock:
    def __init__(self):
        self.elapsed = 0

    def utc(self):
        return 1800000000000 + int(self.elapsed * 1000)

    def sleep(self, seconds):
        self.elapsed += seconds


class OfflineTarget:
    offline_only = True

    def __init__(self, clock):
        self.clock = clock
        self.calls = []
        self.emitted = {}
        self.emit_failure = 0
        self.launch_failure = 0
        self.fail = None
        self.purge_missing_index = False
        self.cleanup_failure = False

    def __getattr__(self, name):
        def action(*arguments):
            self.calls.append((name, arguments))
            if self.fail == name:
                raise functional.FunctionalRetentionError("offline-injected-failure")
            if name == "launch" and self.launch_failure:
                self.launch_failure -= 1
                raise functional.TransportUncertain("offline-readiness-transient")
            if name == "emit":
                run, segment, timestamp = arguments
                self.emitted[segment] = (run, timestamp)
                if self.emit_failure:
                    self.emit_failure -= 1
                    raise functional.TransportUncertain("offline-emit-transport-uncertainty")
                return {"runId": run, "segmentId": segment, "writer": "memories-offline-only", "startedUtcMs": timestamp, "finishedUtcMs": timestamp + 1000, "emitFinishedUtcMs": timestamp + 1000, "acknowledgedUtcMs": timestamp + 2000, "attempted": 125, "enqueued": 125, "acknowledged": 125, "persisted": 125, "transactionAcknowledgements": 125, "conflicted": 0, "rejected": 0, "dropped": 0, "recordIds": _qualification_record_ids(run, segment), "resultCount": 375, "rejectionBreakdown": {"sanitizerUnavailable": 0, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {}, "sanitizerUnavailableBootstrapStages": {}, "lifecycleHealth": "Healthy", "lifecycleReason": "None", "bootstrap": {"attempts": 1, "stage": "Published", "failureStage": None, "failureKind": None, "firstPublishedUtcMs": timestamp - 1000}}}
            if name == "read_cohort":
                run, segment = arguments
                _, timestamp = self.emitted[segment]
                return {"consistency": "strong", "records": [{"recordId": record, "envelopeHash": hashlib.sha256(record.encode()).hexdigest(), "emittedUtcMs": timestamp + ordinal, "expiresUtcMs": timestamp + ordinal + 3600000, "acceptedUtcMs": timestamp + 1000} for ordinal, record in enumerate(_qualification_record_ids(run, segment))]}
            if name == "observe_purge":
                run, segment = arguments
                ids = _qualification_record_ids(run, segment)
                return {"consistency": "strong", "absentRecordIds": ids, "removedIndexIds": [] if self.purge_missing_index else ids, "reminderSequenceBefore": 0, "reminderSequenceAfter": 1, "progressObservedUtcMs": self.clock.utc(), "actorDeletedRecordIds": [], "ttlAbsentRecordIds": ids}
            if name == "terminal_api_readback" and self.cleanup_failure:
                return False
            return True
        return action


class FunctionalTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.root.chmod(0o700)
        self.now = datetime(2026, 10, 16, 12, tzinfo=timezone.utc)
        clock = patch.object(functional, "datetime", wraps=datetime)
        frozen = clock.start()
        frozen.now.side_effect = lambda tz=None: self.now.astimezone(tz) if tz is not None else self.now.replace(tzinfo=None)
        self.addCleanup(clock.stop)
        self.plan_path = self.root / "plan.json"
        self.transport_validation = patch.object(functional, "_validate_transport")
        self.transport_validation.start()
        self.addCleanup(self.transport_validation.stop)
        self.plan = {"schemaVersion": functional.PLAN_SCHEMA, "checkpoint": "functional-retention", "policyId": functional.POLICY, "ownerPrincipal": functional.OWNER, "assuranceMode": functional.ASSURANCE, "profileSha256": functional.STORY_27_4_PROFILE_SHA256, "workloadSha256": functional.STORY_27_4_WORKLOAD_SHA256, "custodyRoot": str(self.root), "sourceRoot": str(ROOT), "sourceFiles": {"fixture": "offline-only"}, "target": deepcopy(functional.TARGET), "limits": deepcopy(functional.LIMITS), "functional": deepcopy(functional.FUNCTIONAL), "window": {"notBefore": "2026-10-16T11:00:00Z", "latestStart": "2026-10-18T06:45:00Z", "expires": "2026-10-18T11:00:00Z"}, "decisionPath": str(self.root / "decision.json"), "statusPath": str(self.root / "status.json"), "preparedInputs": [], "blockers": ["actual-guest-setup-integration-incomplete"], "ready": False}
        self.plan["transport"] = {"offline-fixture": True}
        catalog = self.root / "asset-catalog.json"
        digest = self.write(catalog, functional.artifact_catalog())
        self.plan["preparedInputs"] = [{"path": str(catalog), "sha256": digest, "bytes": catalog.stat().st_size}]
        self.bind()

    def write(self, path, value):
        path.write_text(json.dumps(value))
        path.chmod(0o600)
        return hashlib.sha256(path.read_bytes()).hexdigest()

    def bind(self):
        self.digest = self.write(self.plan_path, self.plan)
        decision = {"schemaVersion": "hexalith.access-telemetry.functional-retention-decision/v1", "decision": "approved", "ownerPrincipal": functional.OWNER, "assuranceMode": functional.ASSURANCE, "planSha256": self.digest, "recordedAtUtc": "2026-10-16T11:00:00Z", "restorationApproved": True}
        self.decision_digest = self.write(Path(self.plan["decisionPath"]), decision)
        self.status = {"planSha256": self.digest, "decisionSha256": self.decision_digest, "executableAuthority": True, "withdrawn": False, "budgetConsumed": False}
        self.write(Path(self.plan["statusPath"]), self.status)

    def authority(self):
        with patch.object(functional, "source_files", return_value={"fixture": "offline-only"}):
            return functional.current_authority(self.plan_path, self.digest, source_root=ROOT, custody_root=self.root, now=self.now)

    def exercise(self, target=None, authority=None):
        clock = target.clock if target else Clock()
        target = target or OfflineTarget(clock)
        result = functional.exercise_offline_sequence(target, authority or (lambda: None), run_id="functional-offline", utc_ms=clock.utc, monotonic=lambda: clock.elapsed, sleep=clock.sleep)
        return target, result

    def test_exact_owner_binding_inspection_is_only_preparation(self):
        self.assertFalse(self.authority()["ready"])

    def test_missing_decision_denies_before_process_spawn(self):
        Path(self.plan["decisionPath"]).unlink()
        with patch("subprocess.Popen") as spawn, redirect_stdout(io.StringIO()) as output:
            result = functional.run_functional_retention(plan_path=self.plan_path, approved_plan_sha256=self.digest, source_root=ROOT, custody_root=self.root)
        self.assertEqual(1, result)
        spawn.assert_not_called()
        self.assertEqual(0, json.loads(output.getvalue())["targetCalls"])

    def test_changed_withdrawn_consumed_and_malformed_status_refuse(self):
        for name, value in (("withdrawn", True), ("executableAuthority", False), ("budgetConsumed", True), ("decisionSha256", "f" * 64), ("withdrawn", 0)):
            with self.subTest(name=name, value=value):
                self.write(Path(self.plan["statusPath"]), {**self.status, name: value})
                with self.assertRaises(functional.FunctionalRetentionError):
                    self.authority()

    def test_expired_future_and_latest_start_refuse(self):
        for now in (self.now - timedelta(days=1), self.now + timedelta(days=2), datetime(2026, 10, 18, 7, tzinfo=timezone.utc)):
            with self.subTest(now=now):
                self.now = now
                with self.assertRaises(functional.FunctionalRetentionError):
                    self.authority()

    def test_changed_plan_source_profile_and_unsafe_input_refuse(self):
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.current_authority(self.plan_path, "f" * 64, source_root=ROOT, custody_root=self.root, now=self.now)
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.current_authority(self.plan_path, self.digest, source_root=ROOT, custody_root=self.root, now=self.now)
        for key, value in (("profileSha256", "7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe"), ("target", {**functional.TARGET, "context": "jpiquot@local"}), ("limits", {**functional.LIMITS, "launchAttempts": 3}), ("target", {**functional.TARGET, "apiPort": 6443})):
            with self.subTest(key=key):
                altered = {**self.plan, key: value}
                with self.assertRaises(functional.FunctionalRetentionError):
                    functional.validate_plan(altered, source_root=ROOT)
        self.plan["command"] = ["/bin/sh", "-c", "anything"]
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.validate_plan(self.plan, source_root=ROOT)

    def test_symlink_and_hardlink_and_public_control_refuse(self):
        path = Path(self.plan["statusPath"])
        original = path.read_bytes()
        path.unlink()
        outside = self.root / "outside.json"
        outside.write_bytes(original)
        outside.chmod(0o600)
        path.symlink_to(outside)
        with self.assertRaises(functional.FunctionalRetentionError):
            self.authority()
        path.unlink()
        os.link(outside, path)
        with self.assertRaises(functional.FunctionalRetentionError):
            self.authority()
        path.unlink()
        path.write_bytes(original)
        path.chmod(0o644)
        with self.assertRaises(functional.FunctionalRetentionError):
            self.authority()

    def test_all_fixed_stages_and_real_time_order_are_exercised_offline(self):
        target, result = self.exercise()
        self.assertEqual("offline-orchestration-passed", result["status"])
        self.assertFalse(result["qualificationCredit"])
        names = [call[0] for call in target.calls]
        for name in ("preflight", "claim_single_run_budget", "launch", "guest_identity", "setup", "startup_probes", "acquire_owned_gate", "observe_purge", "terminal_api_readback", "stopped_readback"):
            self.assertIn(name, names)
        emits = [args for name, args in target.calls if name == "emit"]
        self.assertEqual(1800000, emits[1][2] - emits[0][2])
        self.assertGreaterEqual(target.clock.elapsed, 3600.124)
        self.assertLess(names.index("terminal_api_readback"), max(index for index, name in enumerate(names) if name == "stop_owned_guest"))

    def test_transport_retry_keeps_same_cohort_identity_and_clock(self):
        target = OfflineTarget(Clock())
        target.emit_failure = 1
        target.launch_failure = 1
        target, result = self.exercise(target)
        self.assertEqual("offline-orchestration-passed", result["status"])
        requests = [args for name, args in target.calls if name == "emit"]
        self.assertEqual(requests[0], requests[1])
        self.assertEqual(3, len(requests))
        self.assertEqual([1, 2], [args[0] for name, args in target.calls if name == "launch"])

    def test_actions_crossing_wait_timestamp_never_request_negative_sleep(self):
        target = OfflineTarget(Clock())
        durations = []
        def floor():
            target.clock.elapsed += 10
            return True
        def renew():
            target.clock.elapsed += 10
            return True
        def sleep(seconds):
            self.assertGreaterEqual(seconds, 0)
            durations.append(seconds)
            target.clock.sleep(seconds)
        target.resource_floors, target.renew_owned_gate = floor, renew
        result = functional.exercise_offline_sequence(target, lambda: None, run_id="functional-offline", utc_ms=target.clock.utc, monotonic=lambda: target.clock.elapsed, sleep=sleep)
        self.assertEqual("offline-orchestration-passed", result["status"])
        self.assertIn(0, durations)

    def test_wall_clock_steps_reject_without_earning_retention_or_separation(self):
        for offset in (3600000, -3600000):
            with self.subTest(offset=offset):
                target = OfflineTarget(Clock())
                steps = []
                def utc():
                    return target.clock.utc() + (offset if steps else 0)
                def sleep(seconds):
                    target.clock.elapsed += 1.21
                    steps.append(seconds)
                result = functional.exercise_offline_sequence(target, lambda: None, run_id="functional-offline", utc_ms=utc, monotonic=lambda: target.clock.elapsed, sleep=sleep)
                self.assertEqual("rejected", result["status"])
                self.assertIn("clock-discontinuity", result["reason"])
                self.assertLess(target.clock.elapsed, 2)
                self.assertEqual(["cohort-a"], list(target.emitted))
                self.assertIn("stop_owned_guest", [name for name, _ in target.calls])

    def test_monotonic_elapsed_is_required_even_with_small_permitted_clock_offset(self):
        target = OfflineTarget(Clock())
        offset = 0
        emitted_mono = []
        emit = target.emit
        def emission(*args):
            emitted_mono.append(target.clock.elapsed)
            return emit(*args)
        def utc():
            return target.clock.utc() + offset
        def sleep(seconds):
            nonlocal offset
            offset = 900
            target.clock.sleep(seconds)
        target.emit = emission
        result = functional.exercise_offline_sequence(target, lambda: None, run_id="functional-offline", utc_ms=utc, monotonic=lambda: target.clock.elapsed, sleep=sleep)
        self.assertEqual("offline-orchestration-passed", result["status"])
        self.assertGreaterEqual(emitted_mono[1] - emitted_mono[0], 1800)
        self.assertGreaterEqual(target.clock.elapsed - emitted_mono[0], 3600.124)

    def test_startup_can_take_121_seconds_and_uncertainty_is_never_replayed(self):
        for uncertain in (False, True):
            with self.subTest(uncertain=uncertain):
                target = OfflineTarget(Clock())
                calls = []
                def startup():
                    calls.append(True)
                    target.clock.elapsed += 121
                    if uncertain:
                        raise functional.TransportUncertain("offline-startup-uncertain")
                    return True
                target.startup_probes = startup
                _, result = self.exercise(target)
                self.assertEqual("rejected" if uncertain else "offline-orchestration-passed", result["status"])
                self.assertEqual(1, len(calls))
                self.assertIn("stop_owned_guest", [name for name, _ in target.calls])

    def test_semantic_launch_error_never_retries(self):
        target = OfflineTarget(Clock())
        target.fail = "launch"
        target, result = self.exercise(target)
        self.assertEqual("rejected", result["status"])
        self.assertEqual(1, sum(name == "launch" for name, _ in target.calls))

    def test_missing_index_cannot_turn_ttl_absence_into_pass(self):
        target = OfflineTarget(Clock())
        target.purge_missing_index = True
        _, result = self.exercise(target)
        self.assertEqual("rejected", result["status"])
        self.assertLess(target.clock.elapsed, 4600)

    def test_cleanup_failure_is_incomplete_and_attempts_guest_stop(self):
        target = OfflineTarget(Clock())
        target.cleanup_failure = True
        _, result = self.exercise(target)
        self.assertEqual("incomplete", result["status"])
        self.assertIn("stop_owned_guest", [name for name, _ in target.calls])

    def test_mid_run_withdrawal_stops_test_and_restores_owned_resources(self):
        target = OfflineTarget(Clock())
        def authority():
            if target.clock.elapsed >= 1800:
                raise functional.FunctionalRetentionError("functional-withdrawn")
        _, result = self.exercise(target, authority)
        self.assertEqual("rejected", result["status"])
        self.assertEqual(1, sum(name == "emit" for name, _ in target.calls))
        self.assertIn("disable_owned_gate", [name for name, _ in target.calls])

    def test_newer_record_hash_expiry_identity_and_strong_read_are_required(self):
        target = OfflineTarget(Clock())
        target.emit("functional-offline", "cohort-b", target.clock.utc())
        value = target.read_cohort("functional-offline", "cohort-b")
        expected = functional.validate_cohort(value, run_id="functional-offline", segment="cohort-b", emitted_ms=target.clock.utc())
        for name, new in (("envelopeHash", "f" * 64), ("expiresUtcMs", 1), ("recordId", "foreign")):
            altered = deepcopy(value)
            altered["records"][0][name] = new
            with self.assertRaises(functional.FunctionalRetentionError):
                functional.validate_cohort(altered, run_id="functional-offline", segment="cohort-b", emitted_ms=target.clock.utc(), expected=expected)
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.validate_cohort({**value, "consistency": "eventual"}, run_id="functional-offline", segment="cohort-b", emitted_ms=target.clock.utc())

    def test_production_driver_or_live_pass_cannot_select_offline_seam(self):
        target = OfflineTarget(Clock())
        target.offline_only = False
        with self.assertRaises(functional.FunctionalRetentionError):
            self.exercise(target)
        self.assertEqual([], target.calls)

    def test_latest_start_only_limits_initial_admission(self):
        self.now = datetime(2026, 10, 18, 7, tzinfo=timezone.utc)
        with patch.object(functional, "source_files", return_value={"fixture": "offline-only"}):
            with self.assertRaises(functional.FunctionalRetentionError):
                functional.current_authority(self.plan_path, self.digest, source_root=ROOT, custody_root=self.root, now=self.now)
            self.assertFalse(functional.current_authority(self.plan_path, self.digest, source_root=ROOT, custody_root=self.root, now=self.now, initial_admission=False)["ready"])

    def test_unchanged_reminder_and_old_progress_are_pending_within_grace(self):
        target = OfflineTarget(Clock())
        target.emit("functional-offline", "cohort-a", target.clock.utc())
        records = functional.validate_cohort(target.read_cohort("functional-offline", "cohort-a"), run_id="functional-offline", segment="cohort-a", emitted_ms=target.clock.utc())
        target.clock.elapsed = 3601
        progress = target.observe_purge("functional-offline", "cohort-a")
        progress["reminderSequenceAfter"] = 0
        progress["progressObservedUtcMs"] -= 100000
        self.assertFalse(functional.validate_purge(progress, records, now_ms=target.clock.utc()))
        target.clock.elapsed = 4500
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.validate_purge(progress, records, now_ms=target.clock.utc())

    def test_real_workload_response_contract_and_no_loss_are_required(self):
        target = OfflineTarget(Clock())
        response = target.emit("functional-offline", "cohort-a", target.clock.utc())
        self.assertEqual("memories-offline-only", functional.validate_accounting(response, "functional-offline", "cohort-a", target.clock.utc()))
        self.assertNotIn("accepted", response)
        self.assertNotIn("emittedUtcMs", response)
        for key, value in (("transactionAcknowledgements", 124), ("acknowledged", 124), ("enqueued", 126), ("conflicted", 1), ("resultCount", 0)):
            with self.subTest(key=key):
                with self.assertRaises(functional.FunctionalRetentionError):
                    functional.validate_accounting({**response, key: value}, "functional-offline", "cohort-a", target.clock.utc())
        rejected = {**response, "enqueued": 0, "acknowledged": 0, "persisted": 0, "transactionAcknowledgements": 0, "rejected": 125,
                    "rejectionBreakdown": {**response["rejectionBreakdown"], "sanitizerUnavailable": 125, "sanitizerUnavailableLifecycleReasons": {"RemoteValidationPending": 125},
                                           "sanitizerUnavailableBootstrapStages": {"NotStarted": 125}, "lifecycleHealth": "Degraded", "lifecycleReason": "RemoteValidationPending",
                                           "bootstrap": {"attempts": 0, "stage": "NotStarted", "failureStage": None, "failureKind": None, "firstPublishedUtcMs": None}}}
        with self.assertRaisesRegex(functional.FunctionalRetentionError, "functional-accounting-loss"):
            functional.validate_accounting(rejected, "functional-offline", "cohort-a", target.clock.utc())
        breakdown = response["rejectionBreakdown"]
        bootstrap = breakdown["bootstrap"]
        missing = object()
        negatives = [("missing", missing, "functional-accounting-schema"), ("extra", {**breakdown, "rawResponse": "private"}, "functional-accounting-rejection-breakdown"),
                     ("sanitationFailedOnPass", {**breakdown, "sanitationFailed": 1}, "functional-accounting-rejection-breakdown"),
                     ("reasonOnPass", {**breakdown, "sanitizerUnavailableLifecycleReasons": {"RemoteValidationPending": 1}}, "functional-accounting-rejection-breakdown"),
                     ("stageOnPass", {**breakdown, "sanitizerUnavailableBootstrapStages": {"NotStarted": 1}}, "functional-accounting-rejection-breakdown"),
                     ("unknownReason", {**breakdown, "lifecycleReason": "Private"}, "functional-accounting-rejection-breakdown"),
                     ("unknownHealth", {**breakdown, "lifecycleHealth": "Private"}, "functional-accounting-rejection-breakdown"),
                     ("boolean", {**breakdown, "sanitizerUnavailable": False}, "functional-accounting-rejection-breakdown")]
        # Non-dict breakdowns, each single missing field and out-of-range counts are refused.
        negatives += [("nonDict-" + type(value).__name__, value, "functional-accounting-rejection-breakdown") for value in (None, [], "private", 0)]
        negatives += [("missingField-" + field, {key: item for key, item in breakdown.items() if key != field}, "functional-accounting-rejection-breakdown") for field in sorted(breakdown)]
        negatives += [("range-" + field + "-" + str(value), {**breakdown, field: value}, "functional-accounting-rejection-breakdown")
                      for field in ("sanitizerUnavailable", "sanitationFailed") for value in (-1, 126, 10**100)]
        negatives += [("range-" + field, {**breakdown, field: {name: count}}, "functional-accounting-rejection-breakdown")
                      for field, name in (("sanitizerUnavailableLifecycleReasons", "RemoteValidationPending"), ("sanitizerUnavailableBootstrapStages", "Secret")) for count in (0, -1, 126)]
        # Bootstrap progress: shape and allow-list validity only.
        negatives += [("bootstrapNonDict-" + type(value).__name__, {**breakdown, "bootstrap": value}, "functional-accounting-rejection-breakdown") for value in (None, [], "Published")]
        negatives += [("bootstrapMissing-" + field, {**breakdown, "bootstrap": {key: item for key, item in bootstrap.items() if key != field}}, "functional-accounting-rejection-breakdown") for field in sorted(bootstrap)]
        negatives += [("bootstrap-" + field + "-" + repr(value), {**breakdown, "bootstrap": {**bootstrap, field: value}}, "functional-accounting-rejection-breakdown")
                      for field, values in (("attempts", (-1, True, 2**31, 1.0, None, "1")), ("stage", ("Private", None, 1, "published")),
                                            ("failureStage", ("Private", 1, "", "NotStarted", "Published", "Unobserved")), ("failureKind", ("Private", "None", 1)),
                                            ("firstPublishedUtcMs", (0, -5, True, "1", 2**53 + 1, 1.5))) for value in values]
        negatives += [("bootstrapExtra", {**breakdown, "bootstrap": {**bootstrap, "message": "private"}}, "functional-accounting-rejection-breakdown")]
        for name, value, code in negatives:
            with self.subTest(breakdown=name):
                candidate = {key: item for key, item in response.items() if key != "rejectionBreakdown"} if value is missing else {**response, "rejectionBreakdown": value}
                with self.assertRaisesRegex(functional.FunctionalRetentionError, "^" + code + "$"):
                    functional.validate_accounting(candidate, "functional-offline", "cohort-a", target.clock.utc())
        # Bootstrap fields never turn a correct 125/125 segment into a failure beyond shape and allow-list validity.
        for name, variant in (("unobserved", {"attempts": 0, "stage": "Unobserved", "failureStage": None, "failureKind": None, "firstPublishedUtcMs": None}),
                              ("failedEarlier", {"attempts": 7, "stage": "Published", "failureStage": "Secret", "failureKind": "DaprApi", "firstPublishedUtcMs": 1}),
                              ("refreshInFlight", {"attempts": 2**31 - 1, "stage": "Secret", "failureStage": "Secret", "failureKind": "Other", "firstPublishedUtcMs": 2**53})):
            with self.subTest(passingBootstrap=name):
                self.assertEqual("memories-offline-only", functional.validate_accounting({**response, "rejectionBreakdown": {**breakdown, "bootstrap": variant}}, "functional-offline", "cohort-a", target.clock.utc()))

    def test_bounded_breakdown_drops_out_of_range_and_unknown_values(self):
        raw = {"sanitizerUnavailable": 126, "sanitationFailed": -1, "sanitizerUnavailableLifecycleReasons": {"RemoteValidationPending": 0, "None": 126, "Private": 1, "Expired": 3},
               "sanitizerUnavailableBootstrapStages": {"Secret": 0, "Published": 126, 1: 1, "RemoteValidation": True, "Configuration": 2}, "lifecycleHealth": "Private",
               "lifecycleReason": None, "bootstrap": {"attempts": -1, "stage": "Secret", "failureStage": None, "failureKind": "Private", "firstPublishedUtcMs": 0, "extra": 1}, "extra": "private"}
        self.assertEqual({"sanitizerUnavailableLifecycleReasons": {"Expired": 3}, "sanitizerUnavailableBootstrapStages": {"Configuration": 2},
                          "bootstrap": {"stage": "Secret", "failureStage": None}}, functional.bounded_rejection_breakdown(raw))
        self.assertFalse(functional.rejection_breakdown_complete(raw, {"rejected": 125}))
        for value in (None, [], "private", 1):
            self.assertEqual({}, functional.bounded_rejection_breakdown(value))
            self.assertEqual({}, functional.bounded_bootstrap_progress(value))
            self.assertFalse(functional.rejection_breakdown_complete(value, {"rejected": 125}))

    def test_breakdown_completeness_requires_internally_consistent_bootstrap_progress(self):
        consistent = {"sanitizerUnavailable": 125, "sanitationFailed": 0, "sanitizerUnavailableLifecycleReasons": {"RemoteValidationPending": 125},
                      "sanitizerUnavailableBootstrapStages": {"Secret": 125}, "lifecycleHealth": "Degraded", "lifecycleReason": "RemoteValidationPending",
                      "bootstrap": {"attempts": 2, "stage": "Secret", "failureStage": "Secret", "failureKind": "Transport", "firstPublishedUtcMs": None}}
        self.assertTrue(functional.rejection_breakdown_complete(consistent, {"rejected": 125}))
        for name, bootstrap in (("kindWithoutStage", {"failureStage": None}), ("stageWithoutKind", {"failureKind": None}),
                                ("noAttemptButFailure", {"attempts": 0}), ("noAttemptButPublished", {"attempts": 0, "failureStage": None, "failureKind": None, "firstPublishedUtcMs": 1}),
                                ("publishedWithoutTime", {"stage": "Published"})):
            with self.subTest(bootstrap=name):
                candidate = {**consistent, "bootstrap": {**consistent["bootstrap"], **bootstrap}}
                self.assertEqual(candidate, functional.bounded_rejection_breakdown(candidate))
                self.assertFalse(functional.rejection_breakdown_complete(candidate, {"rejected": 125}))

    def test_rejection_breakdown_allow_lists_match_the_server_enums(self):
        import re
        contracts = ROOT / "src" / "Hexalith.Memories.AccessTelemetry.Contracts"
        server = ROOT / "src" / "Hexalith.Memories.Server" / "Telemetry" / "AccessTelemetryLifecycle"
        def members(path):
            body = path.read_text(encoding="utf-8").split("{", 1)[1].rsplit("}", 1)[0]
            return {match for match in re.findall(r"^\s*([A-Z][A-Za-z0-9]*)\s*(?:=\s*\d+\s*)?,?\s*$", body, re.MULTILINE)}
        def unobserved(path):
            return set(re.findall(r'public const string Unobserved = "([A-Za-z]+)";', path.read_text(encoding="utf-8")))
        # Read the Server's own marker instead of hard-coding it; both C# declarations must agree.
        markers = unobserved(server / "AccessTelemetryQualificationRejectionBreakdown.cs") | unobserved(server / "AccessTelemetryLifecycleBootstrapSnapshot.cs")
        self.assertEqual(1, len(markers))
        marker = next(iter(markers))
        reasons, health = members(contracts / "AccessTelemetryReason.cs"), members(contracts / "AccessTelemetryHealthState.cs")
        stages, kinds = members(server / "AccessTelemetryLifecycleBootstrapStage.cs"), members(server / "AccessTelemetryLifecycleBootstrapFailureKind.cs")
        self.assertTrue(reasons and health and stages and kinds)
        self.assertEqual(reasons | {marker}, set(functional._REJECTION_REASONS))
        self.assertEqual(health | {marker}, set(functional._LIFECYCLE_HEALTH))
        self.assertEqual(stages | {marker}, set(functional._BOOTSTRAP_STAGES))
        self.assertEqual(stages - {"NotStarted", "Published"}, set(functional._BOOTSTRAP_FAILURE_STAGES))
        self.assertEqual(kinds, set(functional._BOOTSTRAP_FAILURE_KINDS))

    def test_budget_claim_is_exclusive_process_bound_and_survives_withdrawal(self):
        with patch.object(functional, "source_files", return_value={"fixture": "offline-only"}), patch.object(functional.FunctionalAuthority, "_metadata", return_value={}):
            grant = functional.FunctionalAuthority(self.plan_path, self.digest, ROOT, self.root)
            self.assertTrue(grant.claim())
            with self.assertRaises(functional.FunctionalRetentionError):
                functional.FunctionalAuthority(self.plan_path, self.digest, ROOT, self.root)()
            with self.assertRaises(functional.FunctionalRetentionError):
                grant.claim()
            self.write(Path(self.plan["statusPath"]), {**self.status, "withdrawn": True})
            with self.assertRaises(functional.FunctionalRetentionError):
                grant()
            grant.finalize()
        status = json.loads(Path(self.plan["statusPath"]).read_bytes())
        self.assertTrue(status["withdrawn"])
        self.assertTrue(status["budgetConsumed"])
        self.assertFalse(status["executableAuthority"])
        self.assertTrue((self.root / "run.claim").is_file())

    def test_matching_rewritten_decision_and_status_cannot_reset_admitted_grant(self):
        with patch.object(functional, "source_files", return_value={"fixture": "offline-only"}), patch.object(functional.FunctionalAuthority, "_metadata", return_value={}):
            grant = functional.FunctionalAuthority(self.plan_path, self.digest, ROOT, self.root)
            grant()
            path = Path(self.plan["decisionPath"])
            decision = json.loads(path.read_bytes())
            decision["recordedAtUtc"] = "2026-10-16T11:01:00Z"
            digest = self.write(path, decision)
            self.write(Path(self.plan["statusPath"]), {**self.status, "decisionSha256": digest})
            with self.assertRaises(functional.FunctionalRetentionError):
                grant()

    def test_tick_checks_fresh_controls_and_real_prepared_metadata_without_rehash(self):
        self.plan["sourceFiles"] = {}
        self.bind()
        with patch.object(functional, "source_files", return_value={}):
            grant = functional.FunctionalAuthority(self.plan_path, self.digest, ROOT, self.root)
            grant()
        with patch.object(functional, "current_authority", side_effect=AssertionError("tick must not rehash")):
            self.assertEqual(self.plan, grant.tick())
            catalog = self.root / "asset-catalog.json"
            catalog.write_bytes(catalog.read_bytes() + b" ")
            with self.assertRaises(functional.FunctionalRetentionError):
                grant.tick()

    def test_fixed_catalog_cannot_be_replaced_by_another_hash_bound_catalog(self):
        catalog = self.root / "asset-catalog.json"
        changed = functional.artifact_catalog()
        changed["downloads"][0]["url"] = "https://foreign.invalid/substitution"
        digest = self.write(catalog, changed)
        self.plan["preparedInputs"] = [{"path": str(catalog), "sha256": digest, "bytes": catalog.stat().st_size}]
        self.bind()
        with self.assertRaises(functional.FunctionalRetentionError):
            self.authority()

    def test_withdrawal_during_final_metadata_check_cannot_be_cached_as_active(self):
        self.plan["sourceFiles"] = {}
        self.bind()
        reads = 0
        def metadata(_grant, _plan):
            nonlocal reads
            reads += 1
            if reads == 2:
                self.write(Path(self.plan["statusPath"]), {**self.status, "withdrawn": True})
            return {}
        with patch.object(functional, "source_files", return_value={}), patch.object(functional.FunctionalAuthority, "_metadata", metadata):
            grant = functional.FunctionalAuthority(self.plan_path, self.digest, ROOT, self.root)
            with self.assertRaises(functional.FunctionalRetentionError):
                grant()
            self.assertFalse(grant.admitted)

    def test_malformed_observation_and_each_cleanup_error_still_attempt_owned_stop(self):
        for exception in (KeyError("field"), TypeError("shape"), AttributeError("shape"), subprocess.SubprocessError("child")):
            with self.subTest(exception=type(exception).__name__):
                target = OfflineTarget(Clock())
                target.read_cohort = unittest.mock.Mock(side_effect=exception)
                target.disable_owned_gate = unittest.mock.Mock(side_effect=exception)
                _, result = self.exercise(target)
                self.assertEqual("incomplete", result["status"])
                self.assertIn("stop_owned_guest", [name for name, _ in target.calls])
                self.assertIn("release_owned_lease", [name for name, _ in target.calls])

    def test_startup_failure_still_attempts_owned_gate_cleanup(self):
        target = OfflineTarget(Clock())
        target.fail = "startup_probes"
        _, result = self.exercise(target)
        self.assertEqual("rejected", result["status"])
        names = [name for name, _ in target.calls]
        self.assertLess(names.index("acquire_owned_gate"), names.index("startup_probes"))
        self.assertIn("disable_owned_gate", names)
        self.assertIn("terminal_api_readback", names)

    def test_read_retries_are_bounded_with_two_and_five_second_delays(self):
        target = OfflineTarget(Clock())
        original = target.read_cohort
        attempts = []
        def read(*args):
            attempts.append(target.clock.elapsed)
            if len(attempts) < 3:
                raise functional.TransportUncertain("offline-read-transient")
            return original(*args)
        target.read_cohort = read
        _, result = self.exercise(target)
        self.assertEqual("offline-orchestration-passed", result["status"])
        self.assertEqual([0, 2, 7], attempts[:3])

    def test_functional_result_is_refused_by_full_predecessor_terminal_and_closeout(self):
        _, packet = self.exercise()
        with self.assertRaises(full.EvidenceValidationError):
            full._validate_predecessor(packet, ROOT, self.root)
        with self.assertRaises(full.EvidenceValidationError):
            full._validate_terminal_bundle(packet, ROOT, self.root)
        with self.assertRaises(full.EvidenceValidationError):
            full._validate_close_out_packet_hash(packet, "functional")

    def test_qemu_serial_is_on_frontend_and_combined_deadline_is_explicit(self):
        argv = functional.fixed_boot_argv()
        self.assertIn("15300s", argv)
        self.assertEqual(2, sum(value == "-device" and "virtio-blk-pci" in argv[index + 1] for index, value in enumerate(argv)))
        for index, value in enumerate(argv):
            if value == "-drive":
                self.assertNotIn("serial=", argv[index + 1])
        self.assertIn("restrict=on", " ".join(argv))
        self.assertNotIn("bootstrap-supervision", " ".join(argv))
        self.assertNotIn("boot-only", " ".join(argv))

    def test_image_build_provenance_commands_select_only_pinned_local_debug_archives(self):
        commands = functional.source_image_build_commands(ROOT)
        self.assertEqual({"server", "lifecycle", "clock"}, set(commands))
        for name, command in commands.items():
            self.assertIn("-p:UseHexalithProjectReferences=true", command)
            self.assertIn("-p:LocalSourceContainerArchive=true", command)
            self.assertIn("-p:ContainerImageFormat=OCI", command)
            self.assertIn(f"-p:ContainerArchiveOutputPath=/tmp/story-27-4-{name}-functional-oci.tar.gz", command)
            self.assertEqual(1, sum("ContainerBaseImage=" in value and "@sha256:" in value for value in command))
            self.assertFalse(any("ContainerRegistry=" in value for value in command))

    def test_strong_probe_uses_actual_runtime_shard_and_correct_dapr_token(self):
        keys = functional.fixed_record_keys("functional-offline", "cohort-a")
        self.assertEqual(125, len(set(keys)))
        for key in keys:
            _, shard, record_id = key.split("/")
            self.assertEqual(int(shard), hashlib.sha256(record_id.encode("ascii")).digest()[0] & 63)
        script = functional.fixed_state_probe_script("functional-offline", "cohort-a", 1800000000000)
        self.assertEqual(125, script.count("consistency=strong"))
        self.assertIn("$DAPR_API_TOKEN", script)
        self.assertNotIn("$APP_API_TOKEN", script)
        self.assertNotIn("--post-data", script)
        self.assertNotIn("DELETE", script)
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.fixed_state_probe_script("shell;evil", "cohort-a", 1800000000000)

    def test_retry_and_cleanup_patch_cannot_drop_uid_version_or_owned_holder(self):
        patch = json.loads(functional.owned_lease_patch(uid="resource-uid", resource_version="1234", holder="run-owner", release=True))
        self.assertEqual(["/metadata/uid", "/metadata/resourceVersion", "/spec/holderIdentity"], [item["path"] for item in patch[:3]])
        self.assertEqual("", patch[3]["value"])
        with self.assertRaises(functional.FunctionalRetentionError):
            functional.owned_lease_patch(uid="foreign/url", resource_version="1234", holder="run-owner", release=True)


class AssetFetchTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.root.chmod(0o700)
        disk = patch.object(functional.shutil, "disk_usage", return_value=unittest.mock.Mock(free=1000 * 1024**3))
        disk.start()
        self.addCleanup(disk.stop)

    def test_catalog_is_fixed_and_preserves_approved_core_image_pins(self):
        catalog = functional.artifact_catalog()
        self.assertEqual("v1.35.9+k3s1", catalog["k3sVersion"])
        self.assertEqual(5, len(catalog["downloads"]))
        self.assertEqual(10, len(catalog["images"]))
        self.assertEqual("5a5a84b19854a9ffaa54082c166ff4ec27473a361e496e5ea167f298f2da9722", next(item["sha256"] for item in catalog["images"] if item["id"] == "postgresql"))
        catalog["downloads"][0]["url"] = "https://foreign.invalid/changed"
        self.assertNotEqual(catalog, functional.artifact_catalog())

    def test_bound_download_checks_actual_hash_readback_and_denial_before_contact(self):
        class Response(io.BytesIO):
            headers = {"Content-Length": "6"}
        payload = b"pinned"
        destination = self.root / "artifact"
        authority = unittest.mock.Mock()
        with patch("urllib.request.urlopen", return_value=Response(payload)) as network:
            functional._download_asset("https://example.invalid/fixed-fixture", destination, hashlib.sha256(payload).hexdigest(), 6, root=self.root, authority=authority)
        self.assertEqual(payload, destination.read_bytes())
        self.assertEqual(1, network.call_count)
        self.assertEqual(0, destination.stat().st_mode & 0o077)
        with patch("urllib.request.urlopen") as network:
            with self.assertRaises(functional.FunctionalRetentionError):
                functional._download_asset("https://example.invalid/fixed-fixture", self.root / "other", "f" * 64, 6, root=self.root, authority=unittest.mock.Mock(side_effect=functional.FunctionalRetentionError("functional-withdrawn")))
        network.assert_not_called()

    def test_hash_mismatch_or_output_limit_never_retries_or_publishes(self):
        class Response(io.BytesIO):
            headers = {"Content-Length": "6"}
        for maximum in (3, 6):
            with self.subTest(maximum=maximum), patch("urllib.request.urlopen", return_value=Response(b"pinned")) as network:
                with self.assertRaises(functional.FunctionalRetentionError):
                    functional._download_asset("https://example.invalid/fixed-fixture", self.root / "artifact", "f" * 64, maximum, root=self.root, authority=lambda: None)
                self.assertEqual(1, network.call_count)
                self.assertFalse((self.root / "artifact").exists())
                self.assertEqual([], list(self.root.glob("*.partial-*")))


    def test_disk_floor_is_checked_for_each_new_block_and_never_retried(self):
        class Response(io.BytesIO):
            headers = {"Content-Length": "6"}
        high = unittest.mock.Mock(free=functional.LIMITS["hostDiskFreeBytes"] + 6)
        low = unittest.mock.Mock(free=functional.LIMITS["hostDiskFreeBytes"] + 5)
        destination = self.root / "floor-bounded-artifact"
        with patch.object(functional.shutil, "disk_usage", side_effect=[high, high, low]), patch("urllib.request.urlopen", return_value=Response(b"pinned")) as network:
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "host-disk-floor"):
                functional._download_asset("https://example.invalid/fixed-fixture", destination, hashlib.sha256(b"pinned").hexdigest(), 6, root=self.root, authority=lambda: None)
        self.assertEqual(1, network.call_count)
        self.assertFalse(destination.exists())
        self.assertEqual([], list(self.root.glob("*.partial-*")))


class SourceArchiveGuardTests(unittest.TestCase):
    def invoke(self, *properties, target="GuardSourceContainerArchive"):
        result = subprocess.run([shutil.which("dotnet"), "msbuild", str(ROOT / "src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj"), "-nologo", "-t:" + target, "-p:UseHexalithProjectReferences=true", "-p:Configuration=Debug", *properties], cwd=ROOT, capture_output=True, text=True, timeout=30)
        return result

    def test_only_absolute_local_debug_archive_is_allowed(self):
        args = ["-p:LocalSourceContainerArchive=true", "-p:ContainerArchiveOutputPath=/tmp/functional-guard-test.tar"]
        result = self.invoke(*args)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        for additional in (("-p:ContainerRegistry=example.invalid",), ("-p:RegistryUrl=example.invalid",), ("-p:Configuration=Release",), ("-p:ContainerArchiveOutputPath=relative.tar",), ("-p:EnableContainer=false",), ("-p:LocalSourceContainerArchive=false",)):
            with self.subTest(additional=additional):
                self.assertNotEqual(0, self.invoke(*args, *additional).returncode)

    def test_package_pack_and_release_source_guards_are_preserved(self):
        self.assertNotEqual(0, self.invoke("-p:LocalSourceContainerArchive=true", "-p:ContainerArchiveOutputPath=/tmp/functional-guard-test.tar", target="FailPackWithHexalithProjectReferences").returncode)
        self.assertNotEqual(0, self.invoke("-p:Configuration=Release", target="FailReleaseWithHexalithProjectReferences").returncode)

    def test_local_archive_property_cannot_select_package_daemon_or_registry_output(self):
        for options in (("-p:UseHexalithProjectReferences=false", "-p:LocalSourceContainerArchive=true"), ("-p:UseHexalithProjectReferences=false", "-p:FunctionalRetentionArchive=true"), ("-p:UseHexalithProjectReferences=false", "-p:LocalSourceContainerArchive=true", "-p:ContainerArchiveOutputPath=/tmp/functional-guard-test.tar")):
            with self.subTest(options=options):
                self.assertNotEqual(0, self.invoke(*options).returncode)

    def hook(self, target, *properties):
        with tempfile.TemporaryDirectory(prefix="functional-msbuild-hook-") as temporary:
            folder = Path(temporary)
            marker = folder / "marker"
            project = folder / "fixture.proj"
            project.write_text('<Project><PropertyGroup><EnableContainer>true</EnableContainer></PropertyGroup>'
                               f'<Import Project="{ROOT / "Directory.Build.props"}" />'
                               f'<Import Project="{ROOT / "Directory.Build.targets"}" />'
                               + ''.join(f'<Target Name="{name}"><WriteLinesToFile File="{marker}" Lines="{name}" /></Target>' for name in ("Publish", "PublishContainer", "Pack", "GenerateNuspec")) + '</Project>')
            result = subprocess.run([shutil.which("dotnet"), "msbuild", str(project), "-nologo", "-t:" + target, "-p:UseHexalithProjectReferences=true", "-p:Configuration=Debug", *properties], cwd=ROOT, capture_output=True, text=True, timeout=30)
            return result, marker.exists()

    def test_real_beforetargets_hooks_refuse_before_publication_markers(self):
        valid = ("-p:LocalSourceContainerArchive=true", "-p:ContainerArchiveOutputPath=/tmp/functional-hook.tar")
        for target in ("Publish", "PublishContainer"):
            result, marked = self.hook(target, *valid)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertTrue(marked)
            for invalid in ((), ("-p:LocalSourceContainerArchive=true",), (*valid, "-p:ContainerRegistry=remote.invalid"), (*valid, "-p:Configuration=Release"), (*valid, "-p:UseHexalithProjectReferences=false"), ("-p:UseHexalithProjectReferences=false", "-p:FunctionalRetentionArchive=true")):
                with self.subTest(target=target, invalid=invalid):
                    result, marked = self.hook(target, *invalid)
                    self.assertNotEqual(0, result.returncode)
                    self.assertFalse(marked)
        for target in ("Pack", "GenerateNuspec"):
            result, marked = self.hook(target, *valid)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse(marked)


class SourceClosureTests(unittest.TestCase):
    def test_declared_dependency_and_imported_build_drift_are_bound_without_skill_discovery(self):
        with tempfile.TemporaryDirectory(prefix="functional-source-closure-") as temporary:
            root = Path(temporary)
            for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "docs/operations/qualification/pg2-kvm-approved-profile-2026-10-16.json"):
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("{}")
            modules = []
            for name in ("Hexalith.FrontComposer", "Hexalith.Builds"):
                modules.append(f'[submodule "{name}"]\npath = references/{name}\nurl = https://example.invalid/{name}\n')
                base = root / "references" / name
                base.mkdir(parents=True)
                (base / ".git").write_text("offline initialized-root fixture")
            (root / ".gitmodules").write_text("\n".join(modules))
            source = root / "references/Hexalith.FrontComposer/src/Contracts/Contract.cs"
            build = root / "references/Hexalith.Builds/Props/Directory.Packages.props"
            ignored = root / "references/Hexalith.FrontComposer/src/skills/private/helper.py"
            nested = root / "references/Hexalith.FrontComposer/references/Nested/src/Forbidden.cs"
            for path in (source, build, ignored, nested):
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("offline fixture")
            first = functional.source_files(root)
            self.assertIn(source.relative_to(root).as_posix(), first)
            self.assertIn(build.relative_to(root).as_posix(), first)
            self.assertNotIn(ignored.relative_to(root).as_posix(), first)
            self.assertNotIn(nested.relative_to(root).as_posix(), first)
            source.write_text("changed source")
            second = functional.source_files(root)
            self.assertNotEqual(first[source.relative_to(root).as_posix()], second[source.relative_to(root).as_posix()])
            build.write_text("changed shared package input")
            self.assertNotEqual(second[build.relative_to(root).as_posix()], functional.source_files(root)[build.relative_to(root).as_posix()])


class PreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="functional-preparation-offline-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.root.chmod(0o700)
        disk = patch.object(functional.shutil, "disk_usage", return_value=unittest.mock.Mock(free=1000 * 1024**3))
        disk.start()
        self.addCleanup(disk.stop)

    def test_actual_copy_refuses_aggregate_size_before_creating_destination(self):
        source = self.root / "source"
        source.write_bytes(b"12345678")
        destination = self.root / "copy"
        with patch.dict(functional.LIMITS, {"stagingBytes": 10}):
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "staging-budget"):
                functional._copy_prepared_file(source, destination, root=self.root)
        self.assertFalse(destination.exists())

    def test_actual_copy_checks_external_staging_growth_between_chunks(self):
        source = self.root / "source"
        source.write_bytes(b"s" * (2 * 1024**2 + 16))
        staging = self.root / "staging"
        staging.mkdir(mode=0o700)
        destination = staging / "copy"
        guard = functional._preparation_write_guard
        def changing_guard(root, next_bytes=0):
            if destination.exists() and destination.stat().st_size == 1024**2:
                (staging / "external-growth").write_bytes(b"e" * (3 * 1024**2))
            return guard(root, next_bytes)
        with patch.dict(functional.LIMITS, {"stagingBytes": 4 * 1024**2}), patch.object(functional, "_preparation_write_guard", changing_guard):
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "staging-budget"):
                functional._copy_prepared_file(source, destination, root=staging)
        self.assertEqual(1024**2, destination.stat().st_size)

    def test_actual_copy_and_json_refuse_free_floor_before_next_write(self):
        source = self.root / "source"
        source.write_bytes(b"pinned")
        destination = self.root / "copy"
        high = unittest.mock.Mock(free=functional.LIMITS["hostDiskFreeBytes"] + 6)
        low = unittest.mock.Mock(free=functional.LIMITS["hostDiskFreeBytes"] + 5)
        with patch.object(functional.shutil, "disk_usage", side_effect=[high, high, low]):
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "host-disk-floor"):
                functional._copy_prepared_file(source, destination, root=self.root)
        self.assertEqual(0, destination.stat().st_size)
        path = self.root / "bound.json"
        with patch.object(functional.shutil, "disk_usage", return_value=unittest.mock.Mock(free=functional.LIMITS["hostDiskFreeBytes"])):
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "host-disk-floor"):
                functional._preparation_json(self.root, path, {"bounded": True})
        self.assertFalse(path.exists())

    def test_retired_vm_preparation_refuses_before_missing_tools_or_bundle_writes(self):
        bundle = self.root / "functional-preparation-retired"
        with patch("access_telemetry_functional_target.prepare_transport_inputs") as transport:
            with self.assertRaisesRegex(functional.FunctionalRetentionError, "historical-vm-route-retired"):
                functional._historical_prepare_vm_bundle(bundle, ROOT)
        transport.assert_not_called()
        self.assertFalse(bundle.exists())


if __name__ == "__main__":
    unittest.main()
