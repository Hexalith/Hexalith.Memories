"""Pure owner-run inspection fixtures; no fixture creates live authority."""

from contextlib import ExitStack
from copy import deepcopy
from dataclasses import FrozenInstanceError, replace
from datetime import date, datetime, timedelta, timezone, tzinfo
import hashlib
import json
from pathlib import Path
import sys
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES  # noqa: E402
from access_telemetry_c1_owner_run import (  # noqa: E402
    OwnerRunAnchors, OwnerRunInspection, OwnerRunInspectionError, inspect_owner_run,
)


START = datetime(2026, 10, 12, 10, tzinfo=timezone.utc)
COLLECTION = START + timedelta(hours=72)
REVIEW = COLLECTION + timedelta(hours=24)
EXPIRY = START + timedelta(days=7)
COLLECTION_ACTIONS = ("observe", "fault", "purge", "reclaim")
LATER_ACTIONS = ("close-out", "publish")
ACTIONS = COLLECTION_ACTIONS + LATER_ACTIONS
TIME_FIELDS = ("notBeforeUtc", "collectionEndsAtUtc", "reviewEndsAtUtc", "expiresAtUtc")


def utc(value):
    return value.isoformat(timespec="milliseconds").replace("+00:00", "Z")


def record():
    return {
        "schemaVersion": "hexalith.access-telemetry.c1.owner-run/v1",
        "policyId": "sole-owner-qualification/2026-10-12",
        "policySha256": "a" * 64,
        "ownerPrincipal": "github:user:6775094",
        "assuranceMode": "sole-owner/self-reviewed",
        "environment": "non-production",
        "profileId": "PG-ONPREM-2",
        "profileSha256": "95aa36e8aba8d9914566f6df894c33098739251040dd0cc4b2917f76a994ccdb",
        "workloadSha256": "71903bb8cc1889a015e066b0276fba2c7f073b2bdfc4d3b11225fc79ec6f091f",
        "sessionId": "pg2-owner.2026-10-12_1",
        "sourceCommit": "b" * 40,
        "targetSha256": "c" * 64,
        "custodyRoot": "/srv/qualification/private-evidence",
        "actions": list(ACTIONS),
        "notBeforeUtc": utc(START),
        "collectionEndsAtUtc": utc(COLLECTION),
        "reviewEndsAtUtc": utc(REVIEW),
        "expiresAtUtc": utc(EXPIRY),
        "status": "approved",
    }


def snapshot(value):
    return JsonSnapshot(json.dumps(value, separators=(",", ":"), ensure_ascii=False).encode())


def anchors(retained):
    """Fixture plan anchors, separate from the mutable candidate in each test."""
    return OwnerRunAnchors(retained.sha256, "a" * 64, "pg2-owner.2026-10-12_1",
                           "b" * 40, "c" * 64, "/srv/qualification/private-evidence")


class OwnerRunInspectionTests(unittest.TestCase):
    def setUp(self):
        self.value = record()
        self.retained = snapshot(self.value)
        self.anchors = anchors(self.retained)

    def inspect(self, value=None, *, approved=None, now=START, withdrawn=False, action="observe"):
        retained = self.retained if value is None else snapshot(value)
        plan = self.anchors if value is None else replace(self.anchors, record_sha256=retained.sha256)
        return inspect_owner_run(retained, plan if approved is None else approved,
                                 now=now, withdrawn=withdrawn, action=action)

    def refuses(self, value=None, *, code=None, **kwargs):
        with self.assertRaises(OwnerRunInspectionError) as caught:
            self.inspect(value, **kwargs)
        if code is not None:
            self.assertEqual(code, str(caught.exception))
        self.assertRegex(str(caught.exception), r"\Aowner-run-[a-z-]+\Z")
        return caught.exception

    def test_collection_actions_return_exact_bytes_and_self_review_disclosure(self):
        for action in COLLECTION_ACTIONS:
            with self.subTest(action=action):
                result = self.inspect(action=action)
                self.assertIs(type(result), OwnerRunInspection)
                self.assertEqual(self.retained.raw, result.snapshot.raw)
                self.assertEqual(hashlib.sha256(self.retained.raw).hexdigest(), result.record_sha256)
                self.assertEqual("github:user:6775094", result.owner_principal)
                self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)
                self.assertEqual(action, result.requested_action)
                self.assertEqual(START, result.inspected_at_utc)
                self.assertIsNot(self.retained, result.snapshot)
                self.assertFalse(hasattr(result, "accepted_gate"))
                self.assertFalse(hasattr(result, "execute"))

    def test_later_actions_are_inspectable_at_collection_end(self):
        for action in LATER_ACTIONS:
            with self.subTest(action=action):
                self.assertEqual(action, self.inspect(now=COLLECTION, action=action).requested_action)

    def test_former_and_foreign_profile_hashes_refuse_even_with_rebound_approval_digest(self):
        for profile_hash in ("7f9f69322353cb22ec1254f1d486ee12337c9a9d579dbc80d6d842d32b339efe",
                             "d" * 64):
            with self.subTest(profile=profile_hash):
                self.refuses({**self.value, "profileSha256": profile_hash},
                             code="owner-run-artifact-literal-mismatch")

    def test_one_action_never_implies_another(self):
        for included in ACTIONS:
            value = {**self.value, "actions": [included]}
            phase = START if included in COLLECTION_ACTIONS else COLLECTION
            self.assertEqual(included, self.inspect(value, now=phase, action=included).requested_action)
            for requested in set(ACTIONS) - {included}:
                with self.subTest(included=included, requested=requested):
                    self.refuses(value, now=phase, action=requested, code="owner-run-action-not-included")

    def test_fixed_identity_and_scope_cannot_be_reappointed_by_record(self):
        changes = {
            "schemaVersion": "hexalith.access-telemetry.c1.owner-run/v2",
            "policyId": "sole-owner-qualification/2026-10-13",
            "ownerPrincipal": "github:user:6775095",
            "assuranceMode": "independently-reviewed",
            "environment": "production",
            "profileId": "PG-ONPREM-1",
            "profileSha256": "dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14",
            "workloadSha256": "d" * 64,
        }
        for name, changed in changes.items():
            with self.subTest(field=name):
                self.refuses({**self.value, name: changed})

    def test_each_outside_record_anchor_must_match(self):
        for name, changed in (
            ("record_sha256", "d" * 64), ("policy_sha256", "e" * 64),
            ("session_id", "other-session"), ("source_commit", "d" * 40),
            ("target_sha256", "f" * 64), ("custody_root", "/srv/other-evidence"),
        ):
            with self.subTest(anchor=name):
                self.refuses(approved=replace(self.anchors, **{name: changed}))
        for name in ("policySha256", "sessionId", "sourceCommit", "targetSha256", "custodyRoot"):
            value = deepcopy(self.value)
            value[name] = {"policySha256": "d" * 64, "sessionId": "other-session",
                           "sourceCommit": "d" * 40, "targetSha256": "d" * 64,
                           "custodyRoot": "/srv/other-evidence"}[name]
            with self.subTest(candidate=name):
                self.refuses(value, code="owner-run-anchor-mismatch")

    def test_changed_raw_bytes_refuse_even_when_parsed_values_are_identical(self):
        changed = JsonSnapshot(self.retained.raw + b"\n")
        with self.assertRaisesRegex(OwnerRunInspectionError, "^owner-run-record-digest-mismatch$"):
            inspect_owner_run(changed, self.anchors, now=START, withdrawn=False, action="observe")

    def test_cached_snapshot_fields_are_recomputed_from_bounded_raw_bytes(self):
        changed = snapshot(self.value)
        object.__setattr__(changed, "sha256", "d" * 64)
        object.__setattr__(changed, "value", {"status": "withdrawn"})
        result = inspect_owner_run(changed, self.anchors, now=START, withdrawn=False, action="observe")
        self.assertEqual(self.anchors.record_sha256, result.record_sha256)
        self.assertEqual("approved", result.snapshot.value["status"])
        object.__setattr__(changed, "raw", b" " * (MAX_ARTIFACT_BYTES + 1))
        with self.assertRaisesRegex(OwnerRunInspectionError, "^owner-run-artifact-byte-budget-exceeded$"):
            inspect_owner_run(changed, self.anchors, now=START, withdrawn=False, action="observe")

    def test_missing_or_unknown_fields_refuse(self):
        for name in self.value:
            value = deepcopy(self.value)
            del value[name]
            with self.subTest(missing=name):
                self.refuses(value)
        for name in ("approval", "acceptedGate", "execute", "authorization"):
            with self.subTest(extra=name):
                self.refuses({**self.value, name: "Bearer private-canary"})

    def test_wrong_root_snapshot_and_anchor_types_refuse(self):
        for value in (None, True, 3, "approved", [], [self.value]):
            retained = snapshot(value)
            with self.subTest(root=type(value).__name__), self.assertRaises(OwnerRunInspectionError):
                inspect_owner_run(retained, replace(self.anchors, record_sha256=retained.sha256),
                                  now=START, withdrawn=False, action="observe")
        for retained, approved in ((self.retained.raw, self.anchors), (self.value, self.anchors),
                                   (None, self.anchors), (self.retained, None),
                                   (self.retained, self.anchors.__class__.__name__)):
            with self.subTest(snapshot=type(retained).__name__, anchors=type(approved).__name__), \
                    self.assertRaisesRegex(OwnerRunInspectionError, "^owner-run-snapshot-and-anchors-required$"):
                inspect_owner_run(retained, approved, now=START, withdrawn=False, action="observe")

    def test_bool_integer_null_or_array_cannot_stand_in_for_string_fields(self):
        for name in set(self.value) - {"actions"}:
            for changed in (True, False, 0, None, []):
                with self.subTest(field=name, value=changed):
                    self.refuses({**self.value, name: changed})

    def test_anchor_types_and_canonical_digest_and_commit_spellings_refuse(self):
        for name in self.anchors.__dataclass_fields__:
            for changed in (True, None, 1, []):
                with self.subTest(anchor=name, value=changed):
                    self.refuses(approved=replace(self.anchors, **{name: changed}))
        for name in ("policySha256", "profileSha256", "workloadSha256", "targetSha256", "sourceCommit"):
            length = 40 if name == "sourceCommit" else 64
            for changed in ("A" * length, "g" * length, "a" * (length - 1), "a" * length + "\n"):
                with self.subTest(field=name, value=changed):
                    self.refuses({**self.value, name: changed})
        for name in ("record_sha256", "policy_sha256", "target_sha256"):
            self.refuses(approved=replace(self.anchors, **{name: "A" * 64}))

    def test_session_bounds_alphabet_and_secret_safety(self):
        for session in ("a", "Z" + "._-0123456789" * 9 + "a" * 10):
            self.assertLessEqual(len(session), 128)
            value = {**self.value, "sessionId": session}
            retained = snapshot(value)
            self.inspect(value, approved=replace(self.anchors, record_sha256=retained.sha256,
                                                  session_id=session))
        for session in ("", "a" * 129, ".run", "-run", "_run", "run/one", "run one",
                        "café", "run\n", "run-token", "authorization", "secret-canary"):
            with self.subTest(session=session):
                self.refuses({**self.value, "sessionId": session})

    def test_custody_is_only_a_bounded_canonical_absolute_posix_path(self):
        for path in ("/", "relative", "~/private", "//srv/evidence", "/srv//evidence",
                     "/srv/./evidence", "/srv/../evidence", "/srv/evidence/", "/srv\\evidence",
                     "/srv/\x00evidence", "/srv/\nevidence", "/" + "x" * 512):
            with self.subTest(path=path):
                self.refuses({**self.value, "custodyRoot": path})
                self.refuses(approved=replace(self.anchors, custody_root=path))
        path = "/" + "x" * 511
        value = {**self.value, "custodyRoot": path}
        retained = snapshot(value)
        self.inspect(value, approved=replace(self.anchors, record_sha256=retained.sha256, custody_root=path))

    def test_timestamp_spelling_and_calendar_values_refuse(self):
        invalid = ("2026-10-12T10:00:00Z", "2026-10-12T10:00:00.00Z",
                   "2026-10-12T10:00:00.0000Z", "2026-10-12T10:00:00.000+00:00",
                   "2026-10-12T10:00:00.000+01:00", "2026-10-12t10:00:00.000Z",
                   "2026-02-30T10:00:00.000Z", "2026-10-12T24:00:00.000Z",
                   "2026-10-12T10:60:00.000Z", "2026-10-12T10:00:60.000Z",
                   "0000-10-12T10:00:00.000Z", "2026-10-12T10:00:00.000Z\n")
        for name in TIME_FIELDS:
            for instant in invalid:
                with self.subTest(field=name, instant=instant):
                    self.refuses({**self.value, name: instant})

    def test_window_order_and_maximum_durations_are_enforced(self):
        for name, instant in (
            ("collectionEndsAtUtc", START), ("collectionEndsAtUtc", START - timedelta(milliseconds=1)),
            ("collectionEndsAtUtc", COLLECTION + timedelta(milliseconds=1)),
            ("reviewEndsAtUtc", COLLECTION), ("reviewEndsAtUtc", COLLECTION - timedelta(milliseconds=1)),
            ("reviewEndsAtUtc", REVIEW + timedelta(milliseconds=1)),
            ("expiresAtUtc", REVIEW - timedelta(milliseconds=1)),
            ("expiresAtUtc", EXPIRY + timedelta(milliseconds=1)),
        ):
            with self.subTest(field=name, instant=instant):
                self.refuses({**self.value, name: utc(instant)}, code="owner-run-window-invalid")
        self.inspect()
        self.inspect({**self.value, "expiresAtUtc": utc(REVIEW)})

    def test_start_collection_review_and_expiry_boundaries_preserve_microseconds(self):
        microsecond = timedelta(microseconds=1)
        self.refuses(now=START - microsecond, code="owner-run-outside-use-window")
        self.inspect(now=START)
        for action in COLLECTION_ACTIONS:
            self.inspect(now=COLLECTION - microsecond, action=action)
            self.refuses(now=COLLECTION, action=action, code="owner-run-action-outside-phase")
        for action in LATER_ACTIONS:
            self.refuses(now=COLLECTION - microsecond, action=action, code="owner-run-action-outside-phase")
            self.inspect(now=COLLECTION, action=action)
            self.inspect(now=REVIEW - microsecond, action=action)
            for instant in (REVIEW, EXPIRY):
                self.refuses(now=instant, action=action, code="owner-run-outside-use-window")

    def test_now_must_be_a_real_aware_utc_datetime(self):
        for now in (None, True, 0, "2026-10-12T10:00:00.000Z", date(2026, 10, 12),
                    START.replace(tzinfo=None), START.replace(tzinfo=timezone(timedelta(hours=1)))):
            with self.subTest(now=now):
                self.refuses(now=now, code="owner-run-aware-utc-required")
        self.inspect(now=START.replace(tzinfo=timezone(timedelta(0), "UTC-plan")))

    def test_custom_timezone_is_refused_before_any_callback(self):
        class CallerTimezone(tzinfo):
            def __init__(self, throws):
                self.throws = throws
                self.offset = timedelta(0)
                self.calls = 0

            def utcoffset(self, value):
                self.calls += 1
                if self.throws:
                    raise RuntimeError("timezone-private-canary")
                return self.offset

            def dst(self, value):
                self.calls += 1
                raise RuntimeError("timezone-private-canary")

            def tzname(self, value):
                self.calls += 1
                raise RuntimeError("timezone-private-canary")

        for throws in (False, True):
            zone = CallerTimezone(throws)
            now = START.replace(tzinfo=zone)
            error = self.refuses(now=now, code="owner-run-aware-utc-required")
            self.assertNotIn("timezone-private-canary", repr(error))
            self.assertEqual(0, zone.calls)
            zone.offset = timedelta(hours=1)
            self.refuses(now=now, code="owner-run-aware-utc-required")
            self.assertEqual(0, zone.calls)

    def test_retained_timestamp_is_immutable_canonical_utc(self):
        named_utc = timezone(timedelta(0), "UTC-plan")
        supplied = START.replace(tzinfo=named_utc, microsecond=123456, fold=1)
        result = self.inspect(now=supplied)
        self.assertIs(result.inspected_at_utc.tzinfo, timezone.utc)
        self.assertEqual(0, result.inspected_at_utc.fold)
        self.assertEqual(supplied, result.inspected_at_utc)
        self.assertEqual("2026-10-12T10:00:00.123456+00:00", result.inspected_at_utc.isoformat())
        self.assertIsNot(supplied, result.inspected_at_utc)
        with self.assertRaises(AttributeError):
            result.inspected_at_utc.tzinfo.offset = timedelta(hours=1)
        with self.assertRaises(FrozenInstanceError):
            result.inspected_at_utc = supplied.replace(tzinfo=timezone(timedelta(hours=1)))
        self.assertEqual("2026-10-12T10:00:00.123456+00:00", result.inspected_at_utc.isoformat())

    def test_unknown_or_withdrawn_current_status_always_refuses(self):
        for withdrawn in (None, 0, 1, "false", "approved", [], {}):
            with self.subTest(withdrawn=withdrawn):
                self.refuses(withdrawn=withdrawn, code="owner-run-current-status-unavailable")
        self.refuses(withdrawn=True, code="owner-run-withdrawn")
        self.inspect(withdrawn=False)

    def test_pending_rejected_withdrawn_or_aliased_record_status_refuses(self):
        for status in ("pending", "rejected", "withdrawn", "APPROVED", " approved", "unknown", ""):
            with self.subTest(status=status):
                self.refuses({**self.value, "status": status})

    def test_actions_require_nonempty_unique_closed_string_array(self):
        for actions in ([], None, True, "observe", {}, ["observe", "observe"],
                        ["observe", "execute"], ["OBSERVE"], ["observe", True],
                        ["observe", None], ["observe", []], ["observe"] * 7):
            with self.subTest(actions=actions):
                self.refuses({**self.value, "actions": actions})
        for action in ("execute", "OBSERVE", "observe ", "", None, True, 1, []):
            with self.subTest(request=action):
                self.refuses(action=action)

    def test_decoded_secret_content_is_refused_without_echo_or_partial_result(self):
        for value in (
            "/srv/Bearer owner-private-canary",
            "/srv/password=owner-private-canary",
            '/srv/{"token":"owner-private-canary"}',
            '/srv/{"name":"authorization","value":"owner-private-canary"}',
            '/srv/{"nested":"Bearer owner-private-canary"}',
        ):
            error = self.refuses({**self.value, "custodyRoot": value})
            self.assertNotIn("owner-private-canary", repr(error))
            self.assertFalse(hasattr(error, "inspection"))
            error = self.refuses(approved=replace(self.anchors, custody_root=value))
            self.assertNotIn("owner-private-canary", repr(error))

    def test_result_anchors_raw_bytes_and_nested_record_are_immutable(self):
        result = self.inspect()
        with self.assertRaises(FrozenInstanceError):
            result.requested_action = "publish"
        with self.assertRaises(FrozenInstanceError):
            self.anchors.session_id = "replacement"
        with self.assertRaises(FrozenInstanceError):
            result.snapshot.raw = b"changed"
        with self.assertRaises(TypeError):
            result.snapshot.value["status"] = "withdrawn"
        with self.assertRaises(TypeError):
            result.snapshot.value["actions"][0] = "publish"
        self.value["status"] = "withdrawn"
        self.value["actions"].clear()
        self.assertEqual("approved", result.snapshot.value["status"])
        self.assertEqual(ACTIONS, result.snapshot.value["actions"])

    def test_positive_and_negative_inspection_make_no_dependency_calls(self):
        changed = snapshot({**self.value, "status": "withdrawn"})
        changed_plan = replace(self.anchors, record_sha256=changed.sha256)
        def forbidden(*args, **kwargs):
            raise AssertionError("owner-run inspection called a dependency")
        targets = (
            "builtins.open", "io.open", "os.open", "os.system", "socket.socket",
            "socket.create_connection", "subprocess.run", "subprocess.Popen",
            "subprocess.check_output", "pathlib.Path.open", "pathlib.Path.stat",
            "pathlib.Path.lstat", "pathlib.Path.resolve", "pathlib.Path.read_bytes",
            "pathlib.Path.read_text", "pathlib.Path.write_bytes", "pathlib.Path.write_text",
            "time.time", "time.monotonic",
        )
        with ExitStack() as stack:
            for target in targets:
                stack.enter_context(patch(target, side_effect=forbidden))
            result = self.inspect()
            self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)
            self.refuses(withdrawn=True)
            self.refuses(now=REVIEW, action="publish")
            self.refuses(approved=replace(self.anchors, record_sha256="d" * 64))
            with self.assertRaises(OwnerRunInspectionError):
                inspect_owner_run(changed, changed_plan, now=START, withdrawn=False, action="observe")


if __name__ == "__main__":
    unittest.main()
