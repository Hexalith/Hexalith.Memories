"""Local owner dispatch fixtures; successful inspection creates no authority."""

from contextlib import ExitStack
from copy import deepcopy
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

from access_telemetry_c1_interchange import JsonSnapshot, MAX_ARTIFACT_BYTES  # noqa: E402
from access_telemetry_c1_owner_dispatch import (  # noqa: E402
    OwnerRunDispatchError, dispatch_owner_run,
)
from access_telemetry_c1_owner_run import (  # noqa: E402
    OwnerRunInspection, OwnerRunInspectionError, inspect_owner_run,
)
from test_owner_run import (  # noqa: E402
    START, COLLECTION, REVIEW, EXPIRY, COLLECTION_ACTIONS, LATER_ACTIONS, record,
)


def wire(value):
    """Independent fixture encoding; exact bytes are approved outside the record."""
    return json.dumps(value, separators=(",", ":"), ensure_ascii=False).encode()


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


class OwnerRunDispatchTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.custody = Path(self.temporary.name).resolve() / "private-evidence"
        self.custody.mkdir()
        self.status_path = self.custody / "current.json"
        self.value = record()
        self.value["custodyRoot"] = str(self.custody)
        self.retained = JsonSnapshot(wire(self.value))
        self.plan = {
            "recordSha256": sha256(self.retained.raw),
            "policySha256": "a" * 64,
            "sessionId": "pg2-owner.2026-10-12_1",
            "sourceCommit": "b" * 40,
            "targetSha256": "c" * 64,
            "custodyRoot": str(self.custody),
            "statusPath": str(self.status_path),
        }
        self.retain_plan()

    def retain_plan(self):
        self.plan_bytes = wire(self.plan)
        self.approved_plan_sha256 = sha256(self.plan_bytes)
        self.status = {
            "planSha256": self.approved_plan_sha256,
            "recordSha256": self.plan["recordSha256"],
            "sessionId": self.plan["sessionId"],
            "withdrawn": False,
        }
        self.write_status()

    def write_status(self, value=None):
        self.status_path.write_bytes(wire(self.status if value is None else value))

    def dispatch(self, *, retained=None, plan_bytes=None, approved=None, now=START, action="observe"):
        return dispatch_owner_run(
            self.retained if retained is None else retained,
            self.plan_bytes if plan_bytes is None else plan_bytes,
            self.approved_plan_sha256 if approved is None else approved,
            now=now, action=action,
        )

    def refuses(self, *, code=None, inspected=False, **kwargs):
        with patch("access_telemetry_c1_owner_dispatch.inspect_owner_run",
                   wraps=inspect_owner_run) as inspect, \
                self.assertRaises((OwnerRunDispatchError, OwnerRunInspectionError)) as caught:
            self.dispatch(**kwargs)
        if code is not None:
            self.assertEqual(code, str(caught.exception))
        self.assertRegex(str(caught.exception), r"\Aowner-(?:dispatch|run)-[a-z0-9-]+\Z")
        self.assertEqual(int(inspected), inspect.call_count)
        self.assertNotIn("private-canary", repr(caught.exception))
        self.assertFalse(hasattr(caught.exception, "inspection"))
        self.assertFalse(hasattr(caught.exception, "execute"))
        self.assertTrue(caught.exception.__suppress_context__)
        self.assertIsNone(caught.exception.__cause__)
        self.assertIsNone(caught.exception.__context__)
        return caught.exception

    def test_matching_fresh_status_returns_only_one_inspection_and_self_review(self):
        for action in COLLECTION_ACTIONS:
            with self.subTest(action=action):
                result = self.dispatch(action=action)
                self.assertIs(type(result), OwnerRunInspection)
                self.assertEqual(action, result.requested_action)
                self.assertEqual(self.retained.raw, result.snapshot.raw)
                self.assertEqual(self.plan["recordSha256"], result.record_sha256)
                self.assertEqual("github:user:6775094", result.owner_principal)
                self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)
                for authority in ("accepted_gate", "checkpoint", "execute", "qualification_authorized",
                                  "production_gate_passed", "a41_closed"):
                    self.assertFalse(hasattr(result, authority))

    def test_later_actions_return_inspection_without_close_out_or_publication(self):
        for action in LATER_ACTIONS:
            with self.subTest(action=action):
                self.assertIs(type(self.dispatch(now=COLLECTION, action=action)), OwnerRunInspection)
                self.refuses(now=START, action=action, inspected=True,
                             code="owner-run-action-outside-phase")

    def test_other_plan_fields_are_ignored_and_never_expand_action_authority(self):
        self.value["actions"] = ["observe"]
        self.retained = JsonSnapshot(wire(self.value))
        self.plan.update(recordSha256=sha256(self.retained.raw), resources=["*"], verbs=["*"],
                         actions=["publish", "purge"], cleanup={"execute": True},
                         qualificationAuthorized=True, acceptedGate="C1.1", withdrawn=False,
                         profileId="PG-ONPREM-1", schemaVersion="institutional-receipt/v1")
        self.retain_plan()
        self.assertEqual("observe", self.dispatch().requested_action)
        for action in ("publish", "purge"):
            self.refuses(action=action, inspected=True, code="owner-run-action-not-included")

    def test_plan_digest_mismatch_refuses_before_status_io_or_inspection(self):
        with patch("access_telemetry_c1_owner_dispatch.os.open") as opened:
            self.refuses(approved="d" * 64, code="owner-dispatch-plan-digest-mismatch")
            self.refuses(plan_bytes=self.plan_bytes + b"\n", code="owner-dispatch-plan-digest-mismatch")
        opened.assert_not_called()

    def test_missing_malformed_or_wrong_type_plan_and_outside_digest_refuse(self):
        for plan_bytes in (None, True, self.plan, bytearray(self.plan_bytes), b"", b"{", b"\xff"):
            with self.subTest(plan_type=type(plan_bytes).__name__):
                with self.assertRaises(OwnerRunDispatchError):
                    dispatch_owner_run(self.retained, plan_bytes, self.approved_plan_sha256,
                                       now=START, action="observe")
        for approved in (None, True, 1, "A" * 64, "d" * 63, "d" * 64 + "\n"):
            with self.subTest(digest_type=type(approved).__name__):
                with self.assertRaises(OwnerRunDispatchError):
                    dispatch_owner_run(self.retained, self.plan_bytes, approved,
                                       now=START, action="observe")

    def test_plan_requires_every_binding_and_an_object_root(self):
        for field in self.plan:
            changed = deepcopy(self.plan)
            del changed[field]
            raw = wire(changed)
            with self.subTest(missing=field):
                self.refuses(plan_bytes=raw, approved=sha256(raw),
                             code="owner-dispatch-plan-bindings-required")
        for value in (None, True, 1, "approved", [], [self.plan]):
            raw = wire(value)
            with self.subTest(root_type=type(value).__name__):
                self.refuses(plan_bytes=raw, approved=sha256(raw),
                             code="owner-dispatch-plan-bindings-required")

    def test_binding_fields_require_closed_types_and_canonical_spellings(self):
        for field in self.plan:
            for value in (None, True, False, 0, [], {}):
                changed = {**self.plan, field: value}
                raw = wire(changed)
                with self.subTest(field=field, value=value):
                    self.refuses(plan_bytes=raw, approved=sha256(raw))
        for field in ("recordSha256", "policySha256", "targetSha256", "sourceCommit"):
            length = 40 if field == "sourceCommit" else 64
            for value in ("A" * length, "g" * length, "a" * (length - 1), "a" * length + "\n"):
                raw = wire({**self.plan, field: value})
                with self.subTest(field=field, spelling=value):
                    self.refuses(plan_bytes=raw, approved=sha256(raw))
        for value in ("", ".session", "run/one", "café", "a" * 129, "run-token", "secret-canary"):
            raw = wire({**self.plan, "sessionId": value})
            with self.subTest(session=value):
                self.refuses(plan_bytes=raw, approved=sha256(raw))

    def test_record_digest_is_recomputed_and_drift_refuses_before_status_read(self):
        for raw in (self.retained.raw + b"\n", wire({**self.value, "sessionId": "other-session"})):
            with self.subTest(raw_length=len(raw)), \
                    patch("access_telemetry_c1_owner_dispatch.os.open") as opened:
                self.refuses(retained=JsonSnapshot(raw), code="owner-dispatch-record-digest-mismatch")
                opened.assert_not_called()
        object.__setattr__(self.retained, "sha256", "d" * 64)
        object.__setattr__(self.retained, "value", {"status": "withdrawn"})
        self.assertEqual(self.plan["recordSha256"], self.dispatch().record_sha256)

    def test_missing_or_wrong_type_record_refuses_without_inspection(self):
        for value in (None, True, self.value, self.retained.raw):
            with self.subTest(record_type=type(value).__name__), \
                    self.assertRaisesRegex(OwnerRunDispatchError, "^owner-dispatch-record-snapshot-required$"):
                dispatch_owner_run(value, self.plan_bytes, self.approved_plan_sha256,
                                   now=START, action="observe")

    def test_each_retained_plan_anchor_must_match_the_bound_record(self):
        for field, value in (("policySha256", "d" * 64), ("sessionId", "other-session"),
                             ("sourceCommit", "d" * 40), ("targetSha256", "d" * 64)):
            original = self.plan[field]
            self.plan[field] = value
            self.retain_plan()
            with self.subTest(field=field):
                self.refuses(inspected=True, code="owner-run-anchor-mismatch")
            self.plan[field] = original
        self.plan["custodyRoot"] = str(self.custody.parent)
        self.retain_plan()
        self.refuses(inspected=True, code="owner-run-anchor-mismatch")

    def test_status_key_drift_refuses_before_inspection(self):
        for field, value in (("planSha256", "d" * 64), ("recordSha256", "d" * 64),
                             ("sessionId", "other-session")):
            with self.subTest(field=field):
                self.write_status({**self.status, field: value})
                self.refuses(code="owner-dispatch-status-binding-mismatch")

    def test_status_requires_exact_fields_and_matching_closed_types(self):
        for field in self.status:
            changed = deepcopy(self.status)
            del changed[field]
            self.write_status(changed)
            with self.subTest(missing=field):
                self.refuses(code="owner-dispatch-artifact-field-set")
        for extra in ("execute", "acceptedGate", "authorization", "qualificationAuthorized"):
            self.write_status({**self.status, extra: True})
            with self.subTest(extra=extra):
                self.refuses(code="owner-dispatch-artifact-field-set")
        for field in ("planSha256", "recordSha256", "sessionId"):
            for value in (None, True, 1, [], {}):
                self.write_status({**self.status, field: value})
                with self.subTest(field=field, value=value):
                    self.refuses()

    def test_unknown_or_non_boolean_withdrawal_is_never_coerced_to_false(self):
        for value in (None, 0, 1, "false", "approved", [], {}):
            self.write_status({**self.status, "withdrawn": value})
            with self.subTest(value=value):
                self.refuses(code="owner-dispatch-current-status-unavailable")
        self.write_status({**self.status, "withdrawn": True})
        self.refuses(inspected=True, code="owner-run-withdrawn")

    def test_later_dispatch_reads_changed_withdrawal_and_missing_state_again(self):
        first = self.dispatch()
        self.write_status({**self.status, "withdrawn": True})
        self.refuses(inspected=True, code="owner-run-withdrawn")
        self.write_status({**self.status, "withdrawn": False})
        self.assertEqual(first.requested_action, self.dispatch().requested_action)
        self.status_path.unlink()
        self.refuses(code="owner-dispatch-current-status-unavailable")

    def test_status_replacement_during_read_refuses_obsolete_open_bytes(self):
        replacement = self.custody / "replacement.json"
        replacement.write_bytes(wire({**self.status, "withdrawn": True}))
        real_read = os.read
        changed = False

        def read(descriptor, count):
            nonlocal changed
            raw = real_read(descriptor, count)
            if not changed:
                changed = True
                os.replace(replacement, self.status_path)
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")
        self.refuses(inspected=True, code="owner-run-withdrawn")

    def test_status_unlink_during_read_refuses_obsolete_open_bytes(self):
        real_read = os.read
        changed = False

        def read(descriptor, count):
            nonlocal changed
            raw = real_read(descriptor, count)
            if not changed:
                changed = True
                self.status_path.unlink()
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")

    def test_short_read_and_same_length_in_place_rewrite_refuse_mixed_old_bytes(self):
        before = self.status_path.stat()
        original = wire(self.status)
        replacement = original.replace(b"false", b"true ")
        self.assertEqual(len(original), len(replacement))
        real_read = os.read
        chunks = []

        def read(descriptor, count):
            raw = real_read(descriptor, min(count, len(original) - 1))
            chunks.append(raw)
            if len(chunks) == 1:
                # The first short read has already consumed False. The remainder
                # can still complete valid obsolete JSON despite this rewrite.
                self.status_path.write_bytes(replacement)
                os.utime(self.status_path, ns=(before.st_atime_ns, before.st_mtime_ns + 1))
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")
        self.assertEqual(original, b"".join(chunks))
        self.assertEqual(before.st_ino, self.status_path.stat().st_ino)

    def test_early_eof_cannot_accept_a_valid_prefix_of_the_named_status_file(self):
        raw = wire(self.status)
        self.status_path.write_bytes(raw + b" " * 100)
        real_read = os.read
        calls = 0

        def read(descriptor, count):
            nonlocal calls
            calls += 1
            return real_read(descriptor, len(raw)) if calls == 1 else b""

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")

    def test_custody_directory_replacement_during_read_refuses_old_traversal(self):
        moved = self.custody.parent / "moved-custody"
        real_read = os.read
        changed = False

        def read(descriptor, count):
            nonlocal changed
            raw = real_read(descriptor, count)
            if not changed:
                changed = True
                self.custody.rename(moved)
                self.custody.mkdir()
                self.write_status()
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")
        self.assertEqual("observe", self.dispatch().requested_action)

    def test_opened_custody_ancestor_movement_during_read_refuses(self):
        ancestor = self.custody.parent / "ancestor"
        ancestor.mkdir()
        self.custody.rename(ancestor / self.custody.name)
        self.custody = ancestor / self.custody.name
        self.status_path = self.custody / "current.json"
        self.value["custodyRoot"] = str(self.custody)
        self.retained = JsonSnapshot(wire(self.value))
        self.plan.update(recordSha256=sha256(self.retained.raw), custodyRoot=str(self.custody),
                         statusPath=str(self.status_path))
        self.retain_plan()
        moved = ancestor.parent / "moved-ancestor"
        real_read = os.read
        changed = False

        def read(descriptor, count):
            nonlocal changed
            raw = real_read(descriptor, count)
            if not changed:
                changed = True
                ancestor.rename(moved)
                ancestor.mkdir()
                self.custody.mkdir()
                self.write_status()
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.refuses(code="owner-dispatch-current-status-drifted")

    def test_custody_replaced_by_outside_symlink_during_read_is_never_followed(self):
        moved = self.custody.parent / "moved-custody"
        outside = self.custody.parent / "outside"
        outside.mkdir()
        (outside / "current.json").write_bytes(wire(self.status))
        real_read = os.read
        changed = False

        def read(descriptor, count):
            nonlocal changed
            raw = real_read(descriptor, count)
            if not changed:
                changed = True
                self.custody.rename(moved)
                self.custody.symlink_to(outside, target_is_directory=True)
            return raw

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read), \
                patch("access_telemetry_c1_owner_dispatch.os.open", wraps=os.open) as opened, \
                patch("access_telemetry_c1_owner_dispatch.os.stat", wraps=os.stat) as checked:
            self.refuses(code="owner-dispatch-current-status-drifted")
        self.assertEqual(1, sum(call.args[0] == "current.json" for call in opened.call_args_list))
        for call in checked.call_args_list:
            self.assertIs(False, call.kwargs["follow_symlinks"])

    def test_filesystem_failures_and_unsupported_operations_leave_no_exception_chain(self):
        for operation in ("open", "fstat", "read", "stat"):
            for error_type in (OSError, NotImplementedError):
                internal = error_type("private-canary filesystem data")
                internal.__cause__ = ValueError("private-canary cause")
                internal.__context__ = RuntimeError("private-canary context")
                with self.subTest(operation=operation, error_type=error_type.__name__), \
                        patch("access_telemetry_c1_owner_dispatch.os." + operation, side_effect=internal):
                    self.refuses(code="owner-dispatch-current-status-unavailable")

    def test_parser_refusals_drop_internal_documents_and_exception_chains(self):
        invalid = b'{"private-canary":'
        self.refuses(plan_bytes=invalid, approved=sha256(invalid), code="owner-dispatch-invalid-json")
        self.status_path.write_bytes(invalid)
        self.refuses(code="owner-dispatch-invalid-json")
        self.write_status()
        object.__setattr__(self.retained, "raw", invalid)
        self.refuses(code="owner-dispatch-invalid-json")
        self.retained = JsonSnapshot(wire({**self.value, "actions": ["Bearer private-canary"]}))
        self.plan["recordSha256"] = sha256(self.retained.raw)
        self.retain_plan()
        self.refuses(inspected=True, code="owner-run-artifact-secret-shaped-content")

    def test_missing_unreadable_or_non_regular_status_refuses(self):
        with patch("access_telemetry_c1_owner_dispatch.os.open",
                   side_effect=PermissionError("private-canary")):
            self.refuses(code="owner-dispatch-current-status-unavailable")
        with patch("access_telemetry_c1_owner_dispatch.os.read",
                   side_effect=OSError("private-canary")):
            self.refuses(code="owner-dispatch-current-status-unavailable")
        self.status_path.unlink()
        self.refuses(code="owner-dispatch-current-status-unavailable")
        self.status_path.mkdir()
        self.refuses(code="owner-dispatch-current-status-unavailable")
        self.status_path.rmdir()
        os.mkfifo(self.status_path)
        self.refuses(code="owner-dispatch-current-status-unavailable")

    def test_status_parse_failures_and_bounds_refuse_without_inspection(self):
        for raw in (b"", b"{", b"\xff", b"true", b"null", b"[]", b'"false"',
                    b"\xef\xbb\xbf" + wire(self.status),
                    b'{' + wire(self.status)[1:-1] + b',"withdrawn":false}',
                    b" " * (MAX_ARTIFACT_BYTES + 1)):
            self.status_path.write_bytes(raw)
            with self.subTest(raw_length=len(raw)):
                self.refuses()

    def test_plan_and_record_bounds_retain_snapshot_fail_closed_behavior(self):
        raw = b" " * (MAX_ARTIFACT_BYTES + 1)
        self.refuses(plan_bytes=raw, approved=sha256(raw),
                     code="owner-dispatch-artifact-byte-budget-exceeded")
        object.__setattr__(self.retained, "raw", raw)
        self.refuses(code="owner-dispatch-artifact-byte-budget-exceeded")

    def test_oversize_status_reads_consume_only_the_budget_with_no_later_reads(self):
        self.status_path.write_bytes(b" " * (2 * MAX_ARTIFACT_BYTES + 512))
        real_read = os.read
        for chunk_limit in (MAX_ARTIFACT_BYTES + 1, 65536):
            consumed = 0
            requested = []

            def read(descriptor, count):
                nonlocal consumed
                self.assertLess(consumed, MAX_ARTIFACT_BYTES + 1, "read attempted after byte budget")
                self.assertLessEqual(count, MAX_ARTIFACT_BYTES + 1 - consumed)
                requested.append(count)
                raw = real_read(descriptor, min(count, chunk_limit))
                consumed += len(raw)
                return raw

            with self.subTest(chunk_limit=chunk_limit), \
                    patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
                self.refuses(code="owner-dispatch-artifact-byte-budget-exceeded")
            self.assertEqual(MAX_ARTIFACT_BYTES + 1, consumed)
            self.assertEqual(1 if chunk_limit > MAX_ARTIFACT_BYTES else 17, len(requested))

    def test_valid_exact_limit_status_document_is_read_and_inspected(self):
        raw = wire(self.status)
        raw += b" " * (MAX_ARTIFACT_BYTES - len(raw))
        self.assertEqual(MAX_ARTIFACT_BYTES, len(raw))
        self.status_path.write_bytes(raw)
        real_read = os.read
        consumed = []
        requested = []

        def read(descriptor, count):
            requested.append(count)
            chunk = real_read(descriptor, count)
            consumed.append(len(chunk))
            return chunk

        with patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read):
            self.assertEqual("observe", self.dispatch().requested_action)
        self.assertEqual([MAX_ARTIFACT_BYTES + 1, 1], requested)
        self.assertEqual([MAX_ARTIFACT_BYTES, 0], consumed)

    def test_noncanonical_status_or_custody_paths_refuse_before_io(self):
        for field in ("statusPath", "custodyRoot"):
            for path in ("/", "relative", "//tmp/evidence", "/tmp//evidence", "/tmp/./evidence",
                         "/tmp/../evidence", str(self.status_path) + "/", "/tmp\\evidence",
                         "/tmp/\x00evidence", "/tmp/\nevidence", "/" + "x" * 512):
                raw = wire({**self.plan, field: path})
                with self.subTest(field=field, path=path), \
                        patch("access_telemetry_c1_owner_dispatch.os.open") as opened:
                    self.refuses(plan_bytes=raw, approved=sha256(raw))
                    opened.assert_not_called()

    def test_outside_and_prefix_sibling_status_paths_refuse_before_io(self):
        for path in (self.custody, self.custody.parent / "current.json",
                     self.custody.parent / (self.custody.name + "-other") / "current.json"):
            raw = wire({**self.plan, "statusPath": str(path)})
            with self.subTest(path=str(path)), \
                    patch("access_telemetry_c1_owner_dispatch.os.open") as opened:
                self.refuses(plan_bytes=raw, approved=sha256(raw),
                             code="owner-dispatch-status-outside-custody")
                opened.assert_not_called()

    def test_symlink_file_is_refused_even_when_target_is_inside_custody(self):
        target = self.custody / "retained.json"
        self.status_path.rename(target)
        self.status_path.symlink_to(target)
        self.refuses(code="owner-dispatch-current-status-unavailable")

    def test_symlink_to_outside_status_is_never_followed(self):
        target = self.custody.parent / "outside.json"
        self.status_path.rename(target)
        self.status_path.symlink_to(target)
        with patch("access_telemetry_c1_owner_dispatch.os.read") as read:
            self.refuses(code="owner-dispatch-current-status-unavailable")
            read.assert_not_called()

    def test_symlink_directory_and_custody_root_are_refused(self):
        folder = self.custody / "retained"
        folder.mkdir()
        self.status_path.rename(folder / "current.json")
        link = self.custody / "alias"
        link.symlink_to(folder, target_is_directory=True)
        self.plan["statusPath"] = str(link / "current.json")
        raw = wire(self.plan)
        self.refuses(plan_bytes=raw, approved=sha256(raw),
                     code="owner-dispatch-current-status-unavailable")
        alias = self.custody.parent / "custody-alias"
        alias.symlink_to(self.custody, target_is_directory=True)
        self.plan.update(custodyRoot=str(alias), statusPath=str(alias / "retained" / "current.json"))
        raw = wire(self.plan)
        self.refuses(plan_bytes=raw, approved=sha256(raw),
                     code="owner-dispatch-current-status-unavailable")

    def test_canonical_nested_status_path_remains_in_custody(self):
        folder = self.custody / "state"
        folder.mkdir()
        self.status_path.rename(folder / "current.json")
        self.status_path = folder / "current.json"
        self.plan["statusPath"] = str(self.status_path)
        self.retain_plan()
        self.assertEqual("observe", self.dispatch().requested_action)

    def test_closed_actions_preserve_existing_inspector_refusals(self):
        self.value["actions"] = ["observe"]
        self.retained = JsonSnapshot(wire(self.value))
        self.plan["recordSha256"] = sha256(self.retained.raw)
        self.retain_plan()
        self.refuses(action="fault", inspected=True, code="owner-run-action-not-included")
        self.refuses(now=COLLECTION, inspected=True, code="owner-run-action-outside-phase")
        for instant in (REVIEW, EXPIRY):
            self.refuses(now=instant, inspected=True, code="owner-run-outside-use-window")

    def test_pg1_other_versions_and_institutional_records_are_not_selected(self):
        for field, value in (("schemaVersion", "hexalith.access-telemetry.c1.predecessor/v2"),
                             ("schemaVersion", "hexalith.access-telemetry.c1.disposition/v1"),
                             ("profileId", "PG-ONPREM-1"), ("profileSha256", "d" * 64),
                             ("workloadSha256", "d" * 64)):
            self.retained = JsonSnapshot(wire({**self.value, field: value}))
            self.plan["recordSha256"] = sha256(self.retained.raw)
            self.retain_plan()
            with self.subTest(field=field, value=value):
                self.refuses(inspected=True, code="owner-run-artifact-literal-mismatch")

    def test_secret_shaped_binding_or_status_content_refuses_without_echo(self):
        for field, value in (("sessionId", "private-canary-token"),
                             ("custodyRoot", "/srv/Bearer private-canary"),
                             ("statusPath", "/srv/password=private-canary")):
            raw = wire({**self.plan, field: value})
            with self.subTest(field=field):
                self.refuses(plan_bytes=raw, approved=sha256(raw))
        self.write_status({**self.status, "sessionId": "Bearer private-canary"})
        self.refuses()

    def test_status_read_finishes_immediately_before_inspector_and_never_contacts_target(self):
        observed = []
        real_read = os.read

        def read(descriptor, count):
            observed.append("status-read")
            return real_read(descriptor, count)

        def inspect(*args, **kwargs):
            self.assertEqual("status-read", observed[-1])
            observed.append("inspect")
            self.assertIs(False, kwargs["withdrawn"])
            return inspect_owner_run(*args, **kwargs)

        def forbidden(*args, **kwargs):
            raise AssertionError("owner dispatch contacted a target")

        with ExitStack() as stack:
            for target in ("os.system", "socket.socket", "socket.create_connection", "subprocess.run",
                           "subprocess.Popen", "subprocess.check_output", "time.time", "time.monotonic"):
                stack.enter_context(patch(target, side_effect=forbidden))
            stack.enter_context(patch("access_telemetry_c1_owner_dispatch.os.read", side_effect=read))
            stack.enter_context(patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect))
            result = self.dispatch()
        self.assertEqual("inspect", observed[-1])
        self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)


if __name__ == "__main__":
    unittest.main()
