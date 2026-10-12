"""Private file custody fixtures; no successful result establishes live authority."""

from contextlib import ExitStack
from copy import deepcopy
import os
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

from access_telemetry_c1_interchange import MAX_ARTIFACT_BYTES  # noqa: E402
from access_telemetry_c1_owner_custody import (  # noqa: E402
    OwnerRunCustodyError, dispatch_owner_run_files,
)
from access_telemetry_c1_owner_run import (  # noqa: E402
    OwnerRunInspection, OwnerRunInspectionError, inspect_owner_run,
)
from test_owner_dispatch import wire, sha256  # noqa: E402
from test_owner_run import START, COLLECTION, REVIEW, EXPIRY, ACTIONS, record  # noqa: E402


def metadata_copy(metadata, **changes):
    """Consistent stat fixture with explicit native nanosecond fields."""
    fields = ("st_dev", "st_ino", "st_mode", "st_nlink", "st_uid", "st_gid", "st_size",
              "st_mtime_ns", "st_ctime_ns")
    return SimpleNamespace(**{**{name: getattr(metadata, name) for name in fields}, **changes})


class OwnerRunCustodyTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.custody = Path(self.temporary.name).resolve() / "private-evidence"
        self.custody.mkdir(mode=0o700)
        self.plan_path = self.custody / "plan.json"
        self.record_path = self.custody / "record.json"
        self.status_path = self.custody / "status.json"
        self.value = {**record(), "custodyRoot": str(self.custody)}
        self.plan = {
            "recordSha256": sha256(wire(self.value)), "policySha256": "a" * 64,
            "sessionId": self.value["sessionId"], "sourceCommit": "b" * 40,
            "targetSha256": "c" * 64, "custodyRoot": str(self.custody),
            "statusPath": str(self.status_path), "recordPath": str(self.record_path),
        }
        self.prepare()

    def write_private(self, path, raw):
        path.write_bytes(raw)
        path.chmod(0o600)

    def forbid_artifact_reads(self, *paths):
        """Fail if os.read touches any denied artifact's actual device/inode."""
        identities = {(path.stat().st_dev, path.stat().st_ino) for path in paths}
        real_read, real_fstat = os.read, os.fstat

        def read(descriptor, count):
            metadata = real_fstat(descriptor)
            self.assertNotIn((metadata.st_dev, metadata.st_ino), identities,
                             "denied artifact was read")
            return real_read(descriptor, count)

        return read

    def prepare(self, *, plan_raw=None, record_raw=None, status_raw=None):
        record_raw = wire(self.value) if record_raw is None else record_raw
        self.plan["recordSha256"] = sha256(record_raw)
        self.plan_bytes = wire(self.plan) if plan_raw is None else plan_raw
        self.approved = sha256(self.plan_bytes)
        self.status = {
            "planSha256": self.approved, "recordSha256": self.plan["recordSha256"],
            "sessionId": self.plan["sessionId"], "withdrawn": False,
        }
        self.write_private(self.plan_path, self.plan_bytes)
        self.write_private(self.record_path, record_raw)
        self.write_private(self.status_path, wire(self.status) if status_raw is None else status_raw)

    def dispatch(self, *, plan_path=None, approved=None, custody_root=None, now=START, action="observe"):
        return dispatch_owner_run_files(
            str(self.plan_path) if plan_path is None else plan_path,
            self.approved if approved is None else approved,
            custody_root=str(self.custody) if custody_root is None else custody_root,
            now=now, action=action,
        )

    def refuses(self, *, code=None, inspected=False, **kwargs):
        with patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", wraps=inspect_owner_run) as inspect, \
                self.assertRaises((OwnerRunCustodyError, OwnerRunInspectionError)) as caught:
            self.dispatch(**kwargs)
        if code is not None:
            self.assertEqual(code, str(caught.exception))
        self.assertRegex(str(caught.exception), r"\Aowner-(?:custody|run)-[a-z0-9-]+\Z")
        self.assertEqual(int(inspected), inspect.call_count)
        self.assertIsNone(caught.exception.__cause__)
        self.assertIsNone(caught.exception.__context__)
        self.assertTrue(caught.exception.__suppress_context__)
        self.assertNotIn("private-canary", repr(caught.exception))
        self.assertFalse(hasattr(caught.exception, "inspection"))
        self.assertFalse(hasattr(caught.exception, "execute"))
        return caught.exception

    def test_each_call_reads_all_current_files_and_returns_only_existing_inspection(self):
        real_read = os.read
        for call in range(2):
            with self.subTest(call=call), patch("access_telemetry_c1_owner_custody.os.read", wraps=real_read) as read:
                result = self.dispatch()
            self.assertIs(type(result), OwnerRunInspection)
            self.assertEqual(wire(self.value), result.snapshot.raw)
            self.assertEqual(self.plan["recordSha256"], result.record_sha256)
            self.assertEqual(START, result.inspected_at_utc)
            self.assertEqual("observe", result.requested_action)
            self.assertEqual("github:user:6775094", result.owner_principal)
            self.assertEqual("sole-owner/self-reviewed", result.assurance_mode)
            self.assertEqual(12, read.call_count)  # Initial read and exact-byte readback, each with EOF.
            for authority in ("execute", "accepted_gate", "checkpoint", "publication", "a41_closed"):
                self.assertFalse(hasattr(result, authority))

    def test_record_changes_between_calls_are_not_cached(self):
        self.dispatch()
        self.write_private(self.record_path, wire({**self.value, "actions": ["purge"]}))
        self.refuses(code="owner-custody-record-digest-mismatch")
        self.value["actions"] = ["purge"]
        self.prepare()
        self.refuses(code="owner-run-action-not-included", inspected=True)
        self.assertEqual("purge", self.dispatch(action="purge").requested_action)

    def test_withdrawal_and_missing_status_between_calls_are_not_cached(self):
        self.dispatch()
        replacement = self.custody / "replacement.json"
        self.write_private(replacement, wire({**self.status, "withdrawn": True}))
        os.replace(replacement, self.status_path)
        self.refuses(code="owner-run-withdrawn", inspected=True)
        self.write_private(self.status_path, wire(self.status))
        self.dispatch()
        self.status_path.unlink()
        self.refuses(code="owner-custody-filesystem-unavailable")

    def test_existing_actions_windows_pg2_policy_and_self_review_checks_are_preserved(self):
        for action in ACTIONS:
            now = START if action in ACTIONS[:4] else COLLECTION
            self.assertEqual(action, self.dispatch(now=now, action=action).requested_action)
        for now, action, code in (
            (COLLECTION, "observe", "owner-run-action-outside-phase"),
            (START, "publish", "owner-run-action-outside-phase"),
            (REVIEW, "publish", "owner-run-outside-use-window"),
            (EXPIRY, "publish", "owner-run-outside-use-window"),
            (START, "execute", "owner-run-action-unknown"),
        ):
            with self.subTest(now=now, action=action):
                self.refuses(now=now, action=action, code=code, inspected=True)
        for field, value in (("profileId", "PG-ONPREM-1"), ("policyId", "other-policy"),
                             ("assuranceMode", "independent"), ("environment", "production")):
            original = self.value[field]
            self.value[field] = value
            self.prepare()
            with self.subTest(field=field):
                self.refuses(code="owner-run-artifact-literal-mismatch", inspected=True)
            self.value[field] = original

    def test_plan_exact_bytes_are_pinned_to_the_outside_digest(self):
        self.refuses(approved="d" * 64, code="owner-custody-plan-digest-mismatch")
        self.write_private(self.plan_path, self.plan_bytes + b"\n")
        with patch("access_telemetry_c1_owner_custody.os.open", wraps=os.open) as opened:
            self.refuses(code="owner-custody-plan-digest-mismatch")
        self.assertFalse(any(call.args[0] in ("record.json", "status.json") for call in opened.call_args_list))

    def test_plan_requires_record_path_without_changing_seven_field_byte_contract(self):
        del self.plan["recordPath"]
        self.prepare()
        self.refuses(code="owner-custody-record-binding-required")
        from access_telemetry_c1_interchange import JsonSnapshot
        from access_telemetry_c1_owner_dispatch import dispatch_owner_run
        with patch("access_telemetry_c1_owner_custody.os.lseek", side_effect=AssertionError("byte API sought a file")):
            result = dispatch_owner_run(JsonSnapshot(wire(self.value)), self.plan_bytes, self.approved,
                                        now=START, action="observe")
        self.assertIs(type(result), OwnerRunInspection)

    def test_caller_custody_root_must_match_the_exact_approved_plan(self):
        self.plan["custodyRoot"] = str(self.custody.parent)
        self.prepare()
        self.refuses(code="owner-custody-root-mismatch")

    def test_paths_and_digest_require_closed_types_and_canonical_spellings_before_io(self):
        for argument in ("plan_path", "custody_root"):
            for value in (None, True, Path(self.custody), "/", "relative", "//tmp/private",
                          "/tmp/./private", "/tmp/../private", "/tmp//private", "/tmp/private/",
                          "/tmp\\private", "/tmp/\x00private", "/tmp/\nprivate", "/" + "x" * 512,
                          "/tmp/Bearer private-canary"):
                # None in dispatch() means its fixture default, so call the API directly.
                kwargs = {"plan_path": str(self.plan_path), "custody_root": str(self.custody),
                          "approved_plan_sha256": self.approved, "now": START, "action": "observe"}
                kwargs[argument] = value
                with self.subTest(argument=argument, value=value), \
                        patch("access_telemetry_c1_owner_custody.os.open") as opened, \
                        self.assertRaises(OwnerRunCustodyError) as caught:
                    dispatch_owner_run_files(**kwargs)
                opened.assert_not_called()
                self.assertIsNone(caught.exception.__context__)
        for digest in (True, "A" * 64, "d" * 63, "d" * 64 + "\n"):
            with patch("access_telemetry_c1_owner_custody.os.open") as opened:
                self.refuses(approved=digest)
                opened.assert_not_called()

    def test_record_path_requires_canonical_type_and_spelling(self):
        for value in (None, True, [], "/", "relative", "/tmp//record", "/tmp/./record",
                      "/tmp/../record", str(self.record_path) + "/", "/tmp\\record", "/tmp/\x00record"):
            self.plan["recordPath"] = value
            self.prepare()
            with self.subTest(value=value):
                self.refuses()

    def test_outside_and_prefix_sibling_paths_are_refused_without_outside_reads(self):
        outside = self.custody.parent / "outside.json"
        with patch("access_telemetry_c1_owner_custody.os.read") as read:
            self.refuses(plan_path=str(outside), code="owner-custody-plan-outside-custody")
            read.assert_not_called()
        for field in ("recordPath", "statusPath"):
            for path in (self.custody, outside,
                         self.custody.parent / (self.custody.name + "-other") / "file.json"):
                original = self.plan[field]
                self.plan[field] = str(path)
                self.prepare()
                with self.subTest(field=field, path=path), \
                        patch("access_telemetry_c1_owner_custody.os.open", wraps=os.open) as opened:
                    self.refuses()
                self.assertFalse(any(call.args[0] in ("outside.json", "file.json") for call in opened.call_args_list))
                self.plan[field] = original

    def test_plan_record_and_status_paths_must_be_distinct(self):
        for field, path in (("recordPath", self.plan_path), ("statusPath", self.plan_path),
                            ("statusPath", self.record_path)):
            original = self.plan[field]
            self.plan[field] = str(path)
            self.prepare()
            with self.subTest(field=field, path=path):
                self.refuses(code="owner-custody-distinct-paths-required")
            self.plan[field] = original

    def test_non_private_mode_on_custody_directory_or_any_file_refuses_before_its_read(self):
        for path in (self.custody, self.plan_path, self.record_path, self.status_path):
            original = path.stat().st_mode & 0o777
            for permission in (0o004, 0o020, 0o010, 0o040, 0o001, 0o002):
                path.chmod(original | permission)
                denied = (self.plan_path, self.record_path, self.status_path) if path == self.custody else (path,)
                read = self.forbid_artifact_reads(*denied)
                with self.subTest(path=path.name, permission=oct(permission)), \
                        patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                    self.refuses(code="owner-custody-not-private")
                path.chmod(original)

    def test_foreign_owner_on_custody_directories_and_files_refuses(self):
        real_fstat = os.fstat
        for path in (self.custody, self.plan_path, self.record_path, self.status_path):
            identity = (path.stat().st_dev, path.stat().st_ino)

            def foreign(descriptor):
                metadata = real_fstat(descriptor)
                if (metadata.st_dev, metadata.st_ino) == identity:
                    values = list(metadata)
                    values[4] = os.geteuid() + 1
                    return os.stat_result(values)
                return metadata

            denied = (self.plan_path, self.record_path, self.status_path) if path == self.custody else (path,)
            read = self.forbid_artifact_reads(*denied)
            with self.subTest(path=path.name), \
                    patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=foreign), \
                    patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                self.refuses(code="owner-custody-not-private")

    def test_effective_os_uid_is_custody_evidence_only_and_changes_refuse(self):
        with patch("access_telemetry_c1_owner_custody.os.geteuid", return_value=os.geteuid() + 1):
            self.refuses(code="owner-custody-not-private")
        uid = os.geteuid()
        with patch("access_telemetry_c1_owner_custody.os.geteuid", side_effect=[uid, uid + 1]):
            self.refuses(code="owner-custody-effective-user-drifted", inspected=True)

    def test_nested_private_directories_work_and_non_private_nested_directory_refuses(self):
        for field in ("recordPath", "statusPath"):
            folder = self.custody / field
            folder.mkdir(mode=0o700)
            path_attribute = "record_path" if field == "recordPath" else "status_path"
            old = getattr(self, path_attribute)
            new = folder / old.name
            old.rename(new)
            setattr(self, path_attribute, new)
            self.plan[field] = str(new)
        self.prepare()
        self.dispatch()
        for folder in (self.record_path.parent, self.status_path.parent):
            folder.chmod(0o750)
            self.refuses(code="owner-custody-not-private")
            folder.chmod(0o700)

    def test_hard_link_to_any_file_is_refused_without_reading_it(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            link = self.custody / "hard-link.json"
            os.link(path, link)
            read = self.forbid_artifact_reads(path)
            with self.subTest(path=path.name), \
                    patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                self.refuses(code="owner-custody-single-link-required")
            link.unlink()

    def test_symlink_files_are_refused_even_for_inside_or_outside_targets(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            for target_parent in (self.custody, self.custody.parent):
                target = target_parent / "retained.json"
                path.rename(target)
                path.symlink_to(target)
                with self.subTest(path=path.name, target=target), \
                        patch("access_telemetry_c1_owner_custody.os.open", wraps=os.open) as opened:
                    self.refuses(code="owner-custody-filesystem-unavailable")
                self.assertFalse(any(call.args[0] == "retained.json" for call in opened.call_args_list))
                path.unlink()
                target.rename(path)

    def test_symlink_root_and_ancestors_are_not_followed(self):
        moved = self.custody.parent / "moved"
        self.custody.rename(moved)
        self.custody.symlink_to(moved, target_is_directory=True)
        with patch("access_telemetry_c1_owner_custody.os.read") as read:
            self.refuses(code="owner-custody-filesystem-unavailable")
            read.assert_not_called()
        self.custody.unlink()
        moved.rename(self.custody)
        folder = self.custody / "nested"
        folder.mkdir(mode=0o700)
        self.record_path.rename(folder / self.record_path.name)
        alias = self.custody / "alias"
        alias.symlink_to(folder, target_is_directory=True)
        self.plan["recordPath"] = str(alias / self.record_path.name)
        self.prepare()
        self.refuses(code="owner-custody-filesystem-unavailable")

    def test_missing_directories_regular_files_or_nonblocking_fifo_refuse(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            raw = path.read_bytes()
            path.unlink()
            with self.subTest(path=path.name, kind="missing"):
                self.refuses()
            path.mkdir(mode=0o700)
            with self.subTest(path=path.name, kind="directory"):
                self.refuses()
            path.rmdir()
            os.mkfifo(path, mode=0o600)
            with self.subTest(path=path.name, kind="fifo"):
                self.refuses()
            path.unlink()
            self.write_private(path, raw)

    def test_malformed_duplicate_secret_and_status_binding_errors_are_sanitized(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            original = path.read_bytes()
            for raw in (b"", b'{"private-canary":', b"\xff", b"null", b"[]",
                        b'{"withdrawn":false,"withdrawn":true}'):
                self.write_private(path, raw)
                approved = sha256(raw) if path == self.plan_path else self.approved
                with self.subTest(path=path.name, raw=raw):
                    self.refuses(approved=approved)
            self.write_private(path, original)
        for field, value in (("withdrawn", None), ("withdrawn", "false"),
                             ("planSha256", "d" * 64), ("recordSha256", "d" * 64),
                             ("sessionId", "other-session"), ("sessionId", "Bearer private-canary")):
            self.write_private(self.status_path, wire({**self.status, field: value}))
            with self.subTest(field=field, value=value):
                self.refuses()
        self.write_private(self.status_path, wire(self.status))
        self.value["actions"] = ["Bearer private-canary"]
        self.prepare()
        self.refuses(inspected=True, code="owner-run-artifact-secret-shaped-content")

    def test_every_required_plan_binding_and_closed_value_is_still_checked(self):
        original = deepcopy(self.plan)
        for field in original:
            for value in (None, True, [], {}):
                changed = {**original, field: value}
                self.plan = deepcopy(original)
                self.prepare(plan_raw=wire(changed))
                with self.subTest(field=field, value=value):
                    self.refuses()
            changed = {key: value for key, value in original.items() if key != field}
            self.plan = deepcopy(original)
            self.prepare(plan_raw=wire(changed))
            with self.subTest(missing=field):
                self.refuses()
        self.plan = original

    def test_exact_limit_each_file_is_accepted_with_bounded_reads(self):
        record_raw = wire(self.value)
        record_raw += b" " * (MAX_ARTIFACT_BYTES - len(record_raw))
        self.plan["recordSha256"] = sha256(record_raw)
        plan_raw = wire(self.plan)
        plan_raw += b" " * (MAX_ARTIFACT_BYTES - len(plan_raw))
        self.prepare(plan_raw=plan_raw, record_raw=record_raw)
        status_raw = wire(self.status)
        self.write_private(self.status_path, status_raw + b" " * (MAX_ARTIFACT_BYTES - len(status_raw)))
        real_read = os.read
        counts = []
        with patch("access_telemetry_c1_owner_custody.os.read", side_effect=lambda fd, n: (counts.append(n), real_read(fd, n))[1]):
            result = self.dispatch()
        self.assertEqual(MAX_ARTIFACT_BYTES, len(result.snapshot.raw))
        self.assertEqual([MAX_ARTIFACT_BYTES + 1, 1] * 6, counts)

    def test_each_oversize_file_consumes_only_its_budget_then_refuses(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            original = path.read_bytes()
            identity = path.stat().st_ino
            self.write_private(path, b" " * (MAX_ARTIFACT_BYTES * 2 + 512))
            for chunk_limit in (MAX_ARTIFACT_BYTES + 1, 65536):
                consumed = 0
                real_read = os.read

                def read(fd, count):
                    nonlocal consumed
                    if os.fstat(fd).st_ino == identity:
                        self.assertLess(consumed, MAX_ARTIFACT_BYTES + 1)
                        self.assertLessEqual(count, MAX_ARTIFACT_BYTES + 1 - consumed)
                        raw = real_read(fd, min(count, chunk_limit))
                        consumed += len(raw)
                        return raw
                    return real_read(fd, count)

                with self.subTest(path=path.name, chunk_limit=chunk_limit), \
                        patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                    self.refuses(code="owner-custody-artifact-byte-budget-exceeded")
                self.assertEqual(MAX_ARTIFACT_BYTES + 1, consumed)
            self.write_private(path, original)

    def test_early_eof_and_short_read_rewrite_refuse_before_inspection(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            original = path.read_bytes()
            identity = path.stat().st_ino
            self.write_private(path, original + b" " * 100)
            real_read = os.read
            calls = 0

            def read(fd, count):
                nonlocal calls
                if os.fstat(fd).st_ino == identity:
                    calls += 1
                    return real_read(fd, len(original)) if calls == 1 else b""
                return real_read(fd, count)

            with self.subTest(path=path.name), patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                self.refuses(code="owner-custody-current-status-drifted")
            self.write_private(path, original)

    def test_replacement_removal_or_rewrite_of_each_file_during_inspection_returns_nothing(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            for mutation in ("replace", "remove", "rewrite", "mode", "hard-link"):
                self.prepare()
                raw = path.read_bytes()
                replacement = self.custody / "replacement.json"
                link = self.custody / "link.json"
                # Pre-create the replacement before descriptors snapshot directories.
                self.write_private(replacement, raw)

                def inspect(*args, **kwargs):
                    result = inspect_owner_run(*args, **kwargs)
                    if mutation == "replace":
                        os.replace(replacement, path)
                    elif mutation == "remove":
                        path.unlink()
                    elif mutation == "rewrite":
                        path.write_bytes(raw + b"\n")
                    elif mutation == "mode":
                        path.chmod(0o640)
                    else:
                        os.link(path, link)
                    return result

                with self.subTest(path=path.name, mutation=mutation), \
                        patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                        self.assertRaises(OwnerRunCustodyError) as caught:
                    self.dispatch()
                self.assertIsNone(caught.exception.__context__)
                self.assertFalse(hasattr(caught.exception, "inspection"))
                if replacement.exists():
                    replacement.unlink()
                if link.exists():
                    link.unlink()
                self.write_private(path, raw)

    def test_same_length_rewrite_and_restored_mode_during_inspection_refuse(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            before = path.stat()
            raw = path.read_bytes()

            def inspect(*args, **kwargs):
                result = inspect_owner_run(*args, **kwargs)
                path.write_bytes(raw[:-1] + b" ")  # A same-length content change.
                path.chmod(0o640)
                path.chmod(0o600)
                os.utime(path, ns=(before.st_atime_ns, before.st_mtime_ns + 1))
                return result

            with self.subTest(path=path.name), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    self.assertRaises(OwnerRunCustodyError):
                self.dispatch()
            self.write_private(path, raw)

    def test_file_rewrite_during_final_ancestor_checks_is_revalidated_before_return(self):
        real_stat = os.stat
        inspected = False
        changed = False
        raw = self.status_path.read_bytes()

        def inspect(*args, **kwargs):
            nonlocal inspected
            result = inspect_owner_run(*args, **kwargs)
            inspected = True
            return result

        def metadata(path, *args, **kwargs):
            nonlocal changed
            result = real_stat(path, *args, **kwargs)
            if inspected and not changed and path == "/":
                changed = True
                self.status_path.write_bytes(raw + b" ")
            return result

        with patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                patch("access_telemetry_c1_owner_custody.os.stat", side_effect=metadata), \
                self.assertRaisesRegex(OwnerRunCustodyError, "^owner-custody-current-status-drifted$"):
            self.dispatch()
        self.assertTrue(changed)

    def test_same_size_rewrite_with_repeated_ctime_and_restored_mtime_refuses_each_artifact(self):
        real_stat, real_fstat = os.stat, os.fstat
        for path in (self.plan_path, self.record_path, self.status_path):
            raw = path.read_bytes()
            before = path.stat()
            identity = (before.st_dev, before.st_ino)
            replacement = (raw.replace(b"false", b"true ") if path == self.status_path
                           else raw.replace(b"b" * 40, b"d" * 40))
            self.assertEqual(len(raw), len(replacement))
            self.assertNotEqual(raw, replacement)

            def coarse(metadata):
                if (metadata.st_dev, metadata.st_ino) == identity:
                    return metadata_copy(metadata, st_mtime_ns=before.st_mtime_ns,
                                         st_ctime_ns=before.st_ctime_ns)
                return metadata

            def inspect(*args, **kwargs):
                result = inspect_owner_run(*args, **kwargs)
                path.write_bytes(replacement)
                os.utime(path, ns=(before.st_atime_ns, before.st_mtime_ns))
                return result

            with self.subTest(path=path.name), \
                    patch("access_telemetry_c1_owner_custody.os.stat", side_effect=lambda *a, **k: coarse(real_stat(*a, **k))), \
                    patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=lambda fd: coarse(real_fstat(fd))), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    self.assertRaisesRegex(OwnerRunCustodyError, "^owner-custody-artifact-bytes-drifted$") as caught:
                self.dispatch()
            self.assertIsNone(caught.exception.__context__)
            self.write_private(path, raw)

    def test_late_withdrawal_during_plan_or_record_final_checks_is_read_back_last(self):
        real_stat, real_fstat = os.stat, os.fstat
        for checked_path in (self.plan_path, self.record_path):
            before = self.status_path.stat()
            raw = self.status_path.read_bytes()
            status_identity = (before.st_dev, before.st_ino)
            inspected = False
            changed = False

            def coarse(metadata):
                if (metadata.st_dev, metadata.st_ino) == status_identity:
                    return metadata_copy(metadata, st_mtime_ns=before.st_mtime_ns,
                                         st_ctime_ns=before.st_ctime_ns)
                return metadata

            def inspect(*args, **kwargs):
                nonlocal inspected
                result = inspect_owner_run(*args, **kwargs)
                inspected = True
                return result

            def named(path, *args, **kwargs):
                nonlocal changed
                metadata = real_stat(path, *args, **kwargs)
                if inspected and not changed and path == checked_path.name:
                    changed = True
                    self.status_path.write_bytes(raw.replace(b"false", b"true "))
                    os.utime(self.status_path, ns=(before.st_atime_ns, before.st_mtime_ns))
                return coarse(metadata)

            with self.subTest(checked_path=checked_path.name), \
                    patch("access_telemetry_c1_owner_custody.os.stat", side_effect=named), \
                    patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=lambda fd: coarse(real_fstat(fd))), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    self.assertRaisesRegex(OwnerRunCustodyError, "^owner-custody-artifact-bytes-drifted$"):
                self.dispatch()
            self.assertTrue(changed)
            self.write_private(self.status_path, raw)

    def test_required_file_and_private_directory_nanoseconds_must_exist_as_integers(self):
        real_stat, real_fstat = os.stat, os.fstat
        for path in (self.custody, self.plan_path, self.record_path, self.status_path):
            identity = (path.stat().st_dev, path.stat().st_ino)
            names = ("st_ctime_ns",) if path == self.custody else ("st_mtime_ns", "st_ctime_ns")
            for name in names:
                for value in ("absent", None, True, False, 1.5, "123"):
                    def unavailable(metadata):
                        if (metadata.st_dev, metadata.st_ino) != identity:
                            return metadata
                        changed = metadata_copy(metadata)
                        if value == "absent":
                            delattr(changed, name)
                        else:
                            setattr(changed, name, value)
                        return changed

                    denied = (self.plan_path, self.record_path, self.status_path) if path == self.custody else (path,)
                    read = self.forbid_artifact_reads(*denied)
                    with self.subTest(path=path.name, name=name, value=value), \
                            patch("access_telemetry_c1_owner_custody.os.stat", side_effect=lambda *a, **k: unavailable(real_stat(*a, **k))), \
                            patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=lambda fd: unavailable(real_fstat(fd))), \
                            patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                        self.refuses(code="owner-custody-freshness-metadata-unavailable")

    def test_distinct_single_link_device_inode_alias_is_refused_before_status_read(self):
        real_stat, real_fstat = os.stat, os.fstat
        status_before = self.status_path.stat()
        status_identity = (status_before.st_dev, status_before.st_ino)
        record_metadata = self.record_path.stat()
        self.assertEqual(1, record_metadata.st_nlink)
        self.assertEqual(1, status_before.st_nlink)
        read = self.forbid_artifact_reads(self.status_path)

        def alias(metadata):
            if (metadata.st_dev, metadata.st_ino) == status_identity:
                return record_metadata
            return metadata

        with patch("access_telemetry_c1_owner_custody.os.stat", side_effect=lambda *a, **k: alias(real_stat(*a, **k))), \
                patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=lambda fd: alias(real_fstat(fd))), \
                patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
            self.refuses(code="owner-custody-distinct-files-required")

    def test_restored_permissions_require_directory_ctime_for_root_and_each_artifact_chain(self):
        for path_attribute, field in (("plan_path", None), ("record_path", "recordPath"),
                                      ("status_path", "statusPath")):
            folder = self.custody / path_attribute
            folder.mkdir(mode=0o700)
            old = getattr(self, path_attribute)
            new = folder / old.name
            old.rename(new)
            setattr(self, path_attribute, new)
            if field is not None:
                self.plan[field] = str(new)
        self.prepare()
        real_stat, real_fstat = os.stat, os.fstat
        for directory in (self.custody, self.plan_path.parent, self.record_path.parent, self.status_path.parent):
            before = directory.stat()
            identity = (before.st_dev, before.st_ino)
            lost_privacy = False

            def changed(metadata):
                if lost_privacy and (metadata.st_dev, metadata.st_ino) == identity:
                    return metadata_copy(metadata, st_ctime_ns=before.st_ctime_ns + 1)
                return metadata

            def inspect(*args, **kwargs):
                nonlocal lost_privacy
                result = inspect_owner_run(*args, **kwargs)
                directory.chmod(0o750)
                directory.chmod(0o700)
                lost_privacy = True
                return result

            with self.subTest(directory=directory.name), \
                    patch("access_telemetry_c1_owner_custody.os.stat", side_effect=lambda *a, **k: changed(real_stat(*a, **k))), \
                    patch("access_telemetry_c1_owner_custody.os.fstat", side_effect=lambda fd: changed(real_fstat(fd))), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    self.assertRaisesRegex(OwnerRunCustodyError, "^owner-custody-current-status-drifted$"):
                self.dispatch()
            self.assertTrue(lost_privacy)
            self.assertEqual(wire(self.value), self.record_path.read_bytes())
            self.assertEqual(self.plan_bytes, self.plan_path.read_bytes())
            self.assertEqual(wire(self.status), self.status_path.read_bytes())

    def test_all_descriptors_are_open_through_inspection_and_closed_on_success_or_refusal(self):
        real_open, real_close = os.open, os.close
        active = set()
        opened = []

        def open_file(*args, **kwargs):
            fd = real_open(*args, **kwargs)
            active.add(fd)
            opened.append(fd)
            return fd

        def close_file(fd):
            active.remove(fd)
            real_close(fd)

        def inspect(*args, **kwargs):
            self.assertEqual(len(opened), len(active))
            self.assertGreater(len(active), 3)
            for fd in active:
                os.fstat(fd)
            return inspect_owner_run(*args, **kwargs)

        with patch("access_telemetry_c1_owner_custody.os.open", side_effect=open_file), \
                patch("access_telemetry_c1_owner_custody.os.close", side_effect=close_file), \
                patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect):
            self.dispatch()
        self.assertFalse(active)
        opened.clear()
        with patch("access_telemetry_c1_owner_custody.os.open", side_effect=open_file), \
                patch("access_telemetry_c1_owner_custody.os.close", side_effect=close_file):
            self.refuses(approved="d" * 64)
        self.assertFalse(active)

    def test_custody_ancestor_replacement_or_outside_symlink_during_inspection_refuses(self):
        moved = self.custody.parent / "moved"
        for mutation in ("replace", "symlink"):
            def inspect(*args, **kwargs):
                result = inspect_owner_run(*args, **kwargs)
                self.custody.rename(moved)
                if mutation == "symlink":
                    self.custody.symlink_to(moved, target_is_directory=True)
                else:
                    self.custody.mkdir(mode=0o700)
                return result

            with self.subTest(mutation=mutation), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    patch("access_telemetry_c1_owner_custody.os.stat", wraps=os.stat) as checked, \
                    self.assertRaises(OwnerRunCustodyError):
                self.dispatch()
            for call in checked.call_args_list:
                self.assertIs(False, call.kwargs["follow_symlinks"])
            if mutation == "symlink":
                self.custody.unlink()
            elif mutation == "replace":
                self.custody.rmdir()
            if moved.exists():
                moved.rename(self.custody)

    def test_ancestor_above_custody_and_nested_record_status_ancestor_movement_refuse(self):
        for path_attribute, field in (("record_path", "recordPath"), ("status_path", "statusPath")):
            folder = self.custody / field
            folder.mkdir(mode=0o700)
            old = getattr(self, path_attribute)
            new = folder / old.name
            old.rename(new)
            setattr(self, path_attribute, new)
            self.plan[field] = str(new)
        self.prepare()
        for folder in (self.record_path.parent, self.status_path.parent, self.custody.parent):
            moved = folder.parent / (folder.name + "-moved")

            def inspect(*args, **kwargs):
                result = inspect_owner_run(*args, **kwargs)
                folder.rename(moved)
                return result

            with self.subTest(folder=folder.name), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                    self.assertRaises(OwnerRunCustodyError):
                self.dispatch()
            moved.rename(folder)

    def test_unrelated_sibling_change_above_custody_does_not_invalidate_private_inputs(self):
        sibling = self.custody.parent / "unrelated.json"

        def inspect(*args, **kwargs):
            result = inspect_owner_run(*args, **kwargs)
            sibling.write_bytes(b"unrelated")
            return result

        with patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect):
            self.assertEqual("observe", self.dispatch().requested_action)
        sibling.unlink()

    def test_unrelated_sibling_directory_creation_and_removal_above_custody_are_allowed(self):
        sibling = self.custody.parent / "unrelated-directory"
        for mutation in ("create", "remove"):
            if mutation == "remove":
                sibling.mkdir()

            def inspect(*args, **kwargs):
                result = inspect_owner_run(*args, **kwargs)
                if mutation == "create":
                    sibling.mkdir()
                else:
                    sibling.rmdir()
                return result

            with self.subTest(mutation=mutation), \
                    patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect):
                self.assertEqual("observe", self.dispatch().requested_action)
            if sibling.exists():
                sibling.rmdir()

    def test_non_following_traversal_refuses_symlink_ancestor_above_custody_without_reads(self):
        alias = self.custody.parent.parent / (self.custody.parent.name + "-alias")
        alias.symlink_to(self.custody.parent, target_is_directory=True)
        self.addCleanup(alias.unlink)
        with patch("access_telemetry_c1_owner_custody.os.read") as read:
            self.refuses(plan_path=str(alias / self.custody.name / self.plan_path.name),
                         custody_root=str(alias / self.custody.name),
                         code="owner-custody-filesystem-unavailable")
            read.assert_not_called()

    def test_nested_plan_directory_is_private_and_its_movement_during_dispatch_refuses(self):
        folder = self.custody / "plans"
        folder.mkdir(mode=0o700)
        self.plan_path.rename(folder / self.plan_path.name)
        self.plan_path = folder / self.plan_path.name
        self.dispatch()
        folder.chmod(0o750)
        self.refuses(code="owner-custody-not-private")
        folder.chmod(0o700)
        moved = self.custody / "moved-plans"

        def inspect(*args, **kwargs):
            result = inspect_owner_run(*args, **kwargs)
            folder.rename(moved)
            return result

        with patch("access_telemetry_c1_owner_dispatch.inspect_owner_run", side_effect=inspect), \
                self.assertRaises(OwnerRunCustodyError):
            self.dispatch()

    def test_plan_or_record_replacement_while_reading_is_not_used_for_dispatch(self):
        for path in (self.plan_path, self.record_path, self.status_path):
            raw = path.read_bytes()
            identity = path.stat().st_ino
            replacement = self.custody / "replacement.json"
            self.write_private(replacement, raw)
            real_read = os.read
            changed = False

            def read(fd, count):
                nonlocal changed
                value = real_read(fd, count)
                if os.fstat(fd).st_ino == identity and not changed:
                    changed = True
                    os.replace(replacement, path)
                return value

            with self.subTest(path=path.name), patch("access_telemetry_c1_owner_custody.os.read", side_effect=read):
                self.refuses(code="owner-custody-current-status-drifted")
            self.dispatch()

    def test_filesystem_errors_unsupported_operations_and_cleanup_errors_have_no_exception_chain(self):
        for operation in ("open", "fstat", "read", "stat", "geteuid"):
            for error_type in (OSError, NotImplementedError, TypeError):
                error = error_type("private-canary filesystem data")
                error.__cause__ = ValueError("private-canary cause")
                error.__context__ = RuntimeError("private-canary context")
                with self.subTest(operation=operation, error=error_type.__name__), \
                        patch("access_telemetry_c1_owner_custody.os." + operation, side_effect=error):
                    self.refuses(code="owner-custody-filesystem-unavailable")
        for error_type in (OSError, NotImplementedError, TypeError):
            with patch("access_telemetry_c1_owner_custody.os.lseek",
                       side_effect=error_type("private-canary seek")):
                self.refuses(code="owner-custody-filesystem-unavailable", inspected=True)
        real_close = os.close

        def close(fd):
            real_close(fd)
            raise OSError("private-canary close")

        with patch("access_telemetry_c1_owner_custody.os.close", side_effect=close):
            self.refuses(code="owner-custody-filesystem-unavailable", inspected=True)
        with patch("access_telemetry_c1_owner_custody.os.O_NOFOLLOW", None):
            self.refuses(code="owner-custody-filesystem-unavailable")

    def test_positive_and_negative_calls_never_use_process_network_target_or_hidden_clock(self):
        def forbidden(*args, **kwargs):
            raise AssertionError("custody wrapper called a forbidden dependency")

        with ExitStack() as stack:
            for target in ("builtins.open", "io.open", "os.system", "socket.socket", "socket.create_connection",
                           "subprocess.run", "subprocess.Popen", "subprocess.check_output", "time.time",
                           "time.monotonic", "pathlib.Path.resolve", "pathlib.Path.read_bytes"):
                stack.enter_context(patch(target, side_effect=forbidden))
            self.dispatch()
            self.refuses(approved="d" * 64)
            self.refuses(now=REVIEW, action="publish", inspected=True)


if __name__ == "__main__":
    unittest.main()
