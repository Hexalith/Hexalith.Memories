"""Exact private sidecars, offline dependency barriers and fresh race refusals."""

from contextlib import ExitStack
from copy import deepcopy
from dataclasses import FrozenInstanceError, fields
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

import access_telemetry_c1_retained_files as retained_files
from access_telemetry_c1_capture_streams import C115CommandStreams, C115StreamInspection
from access_telemetry_c1_interchange import InterchangeFormatError, JsonSnapshot
from test_capture_streams import bind, changed, encoded, fixture, many_fixture, ONE_MIB, THIRTY_TWO_MIB


class CaptureRetainedFilesTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.custody = Path(temporary.name) / "private-evidence"
        self.custody.mkdir(mode=0o700)
        self.packet = self.custody / "capture.json"
        self.sidecar = self.custody / "capture.json.streams"
        self.sidecar.mkdir(mode=0o700)
        self.value, self.streams = fixture()
        self.prepare()

    def write(self, path, raw):
        if path.exists():
            path.chmod(0o600)
        fd = os.open(path, os.O_CREAT | os.O_TRUNC | os.O_WRONLY | os.O_NOFOLLOW, 0o600)
        try:
            remaining = memoryview(raw)
            while remaining:
                remaining = remaining[os.write(fd, remaining):]
        finally:
            os.close(fd)
        path.chmod(0o400)

    def read(self, path):
        fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW)
        try:
            chunks = []
            while chunk := os.read(fd, ONE_MIB):
                chunks.append(chunk)
            return b"".join(chunks)
        finally:
            os.close(fd)

    def prepare(self, *, packet_raw=None, manifest_raw=None):
        for path in self.sidecar.iterdir():
            path.unlink()
        self.packet_raw = encoded(self.value) + b"\n" if packet_raw is None else packet_raw
        self.manifest = {
            "schemaVersion": retained_files.STREAM_MANIFEST_SCHEMA,
            "captureSha256": hashlib.sha256(self.packet_raw).hexdigest(),
            "receipts": [{"index": index, "purpose": command["purpose"],
                          "stdoutSha256": command["stdoutSha256"], "stderrSha256": command["stderrSha256"]}
                         for index, command in enumerate(self.value["commands"])],
        }
        self.write(self.packet, self.packet_raw)
        self.write(self.sidecar / "manifest.json", encoded(self.manifest) if manifest_raw is None else manifest_raw)
        for index, stream in enumerate(self.streams):
            self.write(self.sidecar / f"{index:04d}.stdout", stream.stdout)
            self.write(self.sidecar / f"{index:04d}.stderr", stream.stderr)

    def inspect(self, **kwargs):
        # Filesystem access is restricted to the descriptor implementation. The
        # wrapper must never execute commands, contact targets, infer the clock,
        # consult configuration, or fall back to high-level path reads.
        attempts = {}

        def deny(name):
            def rejected(*args, **options):
                attempts[name] = attempts.get(name, 0) + 1
                raise AssertionError("offline wrapper dependency attempt")
            return rejected

        targets = ("builtins.open", "io.open", "os.system", "socket.socket", "socket.create_connection",
                   "subprocess.run", "subprocess.Popen", "subprocess.check_output", "time.time", "time.monotonic",
                   "os.getenv", "pathlib.Path.resolve", "pathlib.Path.read_bytes")
        try:
            with ExitStack() as barriers:
                for target in targets:
                    barriers.enter_context(patch(target, side_effect=deny(target)))
                return retained_files.inspect_c115_retained_files(
                    kwargs.get("packet_path", str(self.packet)), custody_root=kwargs.get("custody_root", str(self.custody)))
        finally:
            self.assertEqual({}, attempts)

    def refuses(self, *, code=None, **kwargs):
        with self.assertRaises(InterchangeFormatError) as caught:
            self.inspect(**kwargs)
        error = caught.exception
        if code is not None:
            self.assertEqual(code, str(error))
        self.assertRegex(str(error), r"\A[a-z0-9-]+\Z")
        self.assertIsNone(error.__context__)
        self.assertIsNone(error.__cause__)
        self.assertTrue(error.__suppress_context__)
        self.assertNotIn("private-canary", repr(error))
        self.assertFalse(hasattr(error, "inspection"))
        return error

    def test_complete_one_two_three_pods_return_exact_frozen_inspection(self):
        for count in (1, 2, 3):
            with self.subTest(count=count):
                self.value, self.streams = fixture(tuple("lifecycle-" + str(index + 1) for index in range(count)))
                self.value, self.streams = changed(self.value, self.streams, 0,
                                                   stdout=b"\xe2\x80\x83jpiquot@local\r\n\xc2\x85", stderr=b"notice \r\n")
                self.prepare()
                result = self.inspect()
                self.assertIs(type(result), C115StreamInspection)
                self.assertEqual(result.capture.raw, self.packet_raw)
                self.assertEqual(result.streams, self.streams)
                self.assertEqual(result.pod_count, count)
                self.assertEqual(result.command_count, count * 3 + 3)
                self.assertTrue(any(stream.stderr == b"" for stream in result.streams))
                self.assertEqual(tuple(field.name for field in fields(result)),
                                 ("capture", "streams", "pod_count", "pod_names", "command_count"))
                self.assertFalse(result.capture.value["productionGatePassed"])
                self.assertEqual(result.capture.value["gateStatus"], "not-evaluated")
                self.assertEqual(result.capture.value["independentDisposition"], "pending")
                with self.assertRaises(FrozenInstanceError):
                    result.pod_count = 99
                self.assertNotIn("jpiquot", repr(result))

    def test_missing_surplus_and_misnumbered_pairs_and_manifest_refuse(self):
        for kind in ("manifest", "stdout", "stderr", "extra", "misnumbered", "directory"):
            with self.subTest(kind=kind):
                self.prepare()
                if kind in ("manifest", "stdout", "stderr"):
                    (self.sidecar / ("manifest.json" if kind == "manifest" else "0000." + kind)).unlink()
                elif kind == "extra":
                    self.write(self.sidecar / "unlisted.stdout", b"safe")
                elif kind == "misnumbered":
                    (self.sidecar / "0000.stdout").rename(self.sidecar / "0.stdout")
                else:
                    (self.sidecar / "0000.stdout").unlink()
                    (self.sidecar / "0000.stdout").mkdir(mode=0o700)
                self.refuses()
                if kind == "directory":
                    (self.sidecar / "0000.stdout").rmdir()

    def test_cross_packet_sidecar_and_packet_byte_hash_drift_refuse(self):
        self.manifest["captureSha256"] = "0" * 64
        self.write(self.sidecar / "manifest.json", encoded(self.manifest))
        self.refuses(code="c115-files-capture-hash-mismatch")
        self.prepare()
        self.write(self.packet, self.packet_raw + b" ")
        self.refuses(code="c115-files-capture-hash-mismatch")

    def test_manifest_has_closed_fields_schema_and_exact_ordered_receipts(self):
        mutations = (
            lambda m: m.update(extra=False), lambda m: m.pop("receipts"),
            lambda m: m.update(schemaVersion="other/v1"), lambda m: m.update(receipts={}),
            lambda m: m["receipts"].pop(), lambda m: m["receipts"].append(deepcopy(m["receipts"][0])),
            lambda m: m["receipts"].reverse(), lambda m: m["receipts"][0].update(index=True),
            lambda m: m["receipts"][0].update(index=1), lambda m: m["receipts"][0].update(purpose="other-purpose"),
            lambda m: m["receipts"][0].update(stdoutSha256="0" * 64),
            lambda m: m["receipts"][0].update(stderrSha256="invalid"),
            lambda m: m["receipts"][0].update(extra=False), lambda m: m["receipts"][0].pop("purpose"),
        )
        for index, mutate in enumerate(mutations):
            with self.subTest(index=index):
                self.prepare()
                mutate(self.manifest)
                self.write(self.sidecar / "manifest.json", encoded(self.manifest))
                self.refuses()

    def test_invalid_manifest_json_utf8_duplicates_and_budget_are_content_free(self):
        for raw in (b"\xffprivate-canary", b'{"private-canary":', b'{"a":0,"a":1}', b"x" * (ONE_MIB + 1)):
            with self.subTest(raw=raw[:16]):
                self.write(self.sidecar / "manifest.json", raw)
                self.refuses()
        self.prepare()
        manifest_raw = encoded(self.manifest)
        self.write(self.sidecar / "manifest.json", manifest_raw + b" " * (ONE_MIB - len(manifest_raw)))
        self.inspect()

    def test_hash_tampered_streams_refuse_even_with_complete_set(self):
        for suffix in ("stdout", "stderr"):
            for index in (0, len(self.streams) - 1):
                with self.subTest(suffix=suffix, index=index):
                    self.prepare()
                    self.write(self.sidecar / f"{index:04d}.{suffix}", b"safe changed bytes")
                    self.refuses(code="c115-stream-hash-mismatch")

    def test_rehashed_unsafe_utf8_secret_and_semantic_contradictions_refuse(self):
        for raw, code in ((b"\xff", "c115-stream-invalid-utf8"),
                          (b"password=private-canary", "c115-stream-secret-shaped-content"),
                          (b"unmatched-context", "c115-stream-observation-mismatch")):
            for suffix in ("stdout", "stderr") if code != "c115-stream-observation-mismatch" else ("stdout",):
                with self.subTest(suffix=suffix, code=code):
                    self.value, self.streams = fixture()
                    self.value, self.streams = changed(self.value, self.streams, 0, **{suffix: raw})
                    self.prepare()
                    self.refuses(code=code)

    def test_each_artifact_limit_is_checked_before_oversized_reads(self):
        for path in (self.packet, self.sidecar / "manifest.json", self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"):
            with self.subTest(path=path.name):
                self.prepare()
                self.write(path, b" " * (ONE_MIB + 1))
                inode = path.stat().st_ino
                real_read = os.read

                def read(fd, count):
                    self.assertNotEqual(os.fstat(fd).st_ino, inode, "oversized artifact was read")
                    return real_read(fd, count)

                with patch.object(retained_files.os, "read", side_effect=read):
                    self.refuses(code="c115-files-artifact-byte-budget-exceeded")

    def test_exact_packet_and_stream_limits_preserve_untrimmed_bytes(self):
        self.value, self.streams = changed(self.value, self.streams, 0,
                                           stdout=self.streams[0].stdout + b" " * (ONE_MIB - len(self.streams[0].stdout)),
                                           stderr=b"x" * ONE_MIB)
        packet_raw = encoded(self.value)
        self.prepare(packet_raw=packet_raw + b" " * (ONE_MIB - len(packet_raw)))
        result = self.inspect()
        self.assertEqual(len(result.capture.raw), ONE_MIB)
        self.assertEqual(len(result.streams[0].stdout), ONE_MIB)
        self.assertEqual(len(result.streams[0].stderr), ONE_MIB)

    def test_exact_aggregate_limit_and_repeated_occurrence_charging(self):
        self.value, self.streams = many_fixture(5)
        remaining = THIRTY_TWO_MIB - len(encoded(self.value) + b"\n") - sum(len(stream.stdout) for stream in self.streams)
        expanded = []
        for stream in self.streams:
            stderr_size = min(remaining, ONE_MIB)
            remaining -= stderr_size
            stdout_padding = min(remaining, ONE_MIB - len(stream.stdout))
            remaining -= stdout_padding
            expanded.append(C115CommandStreams(stream.purpose, stream.stdout + b" " * stdout_padding, b"x" * stderr_size))
        self.assertEqual(remaining, 0)
        self.streams = tuple(expanded)
        self.value = bind(self.value, self.streams)
        self.prepare()
        self.assertEqual(len(self.packet_raw) + sum(len(s.stdout) + len(s.stderr) for s in self.streams), THIRTY_TWO_MIB)
        self.inspect()
        self.value, self.streams = changed(self.value, self.streams, -1, stderr=self.streams[-1].stderr + b"x")
        self.prepare()
        self.refuses(code="c115-files-aggregate-byte-budget-exceeded")
        self.value, self.streams = many_fixture(11)
        self.streams = tuple(C115CommandStreams(stream.purpose, stream.stdout, b"x" * ONE_MIB) for stream in self.streams)
        self.value = bind(self.value, self.streams)
        self.prepare()
        self.refuses(code="c115-files-aggregate-byte-budget-exceeded")

    def test_every_file_and_private_directory_requires_owner_only_custody(self):
        for path in (self.custody, self.sidecar, self.packet, self.sidecar / "manifest.json",
                     self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"):
            mode = path.stat().st_mode & 0o777
            for added in (0o040, 0o004, 0o010, 0o001):
                with self.subTest(path=path.name, added=added):
                    path.chmod(mode | added)
                    self.refuses(code="c115-files-not-private")
                    path.chmod(mode)
        original = os.geteuid()
        with patch.object(retained_files.os, "geteuid", return_value=original + 1):
            self.refuses(code="c115-files-not-private")

    def test_symlink_hardlink_fifo_and_directory_artifacts_refuse_without_reads(self):
        for path in (self.packet, self.sidecar / "manifest.json", self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"):
            for kind in ("symlink", "hardlink", "fifo", "directory"):
                with self.subTest(path=path.name, kind=kind):
                    self.prepare()
                    outside = self.custody.parent / "outside-artifact"
                    self.write(outside, self.read(path))
                    path.unlink()
                    if kind == "symlink":
                        path.symlink_to(outside)
                    elif kind == "hardlink":
                        os.link(outside, path)
                    elif kind == "fifo":
                        os.mkfifo(path, mode=0o600)
                    else:
                        path.mkdir(mode=0o700)
                    self.refuses()
                    path.rmdir() if kind == "directory" else path.unlink()
                    outside.unlink()

    def test_symlink_ancestors_and_noncanonical_or_outside_paths_refuse(self):
        for path in (str(self.packet) + "/../capture.json", "relative/capture.json", str(self.packet).replace("/capture", "//capture"),
                     str(self.custody.parent / "outside.json"), None):
            with self.subTest(path=path):
                self.refuses(packet_path=path)
        alias = self.custody.parent / "alias"
        alias.symlink_to(self.custody, target_is_directory=True)
        self.refuses(packet_path=str(alias / "capture.json"), custody_root=str(alias))
        self.sidecar.rename(self.custody / "moved-streams")
        self.sidecar.symlink_to(self.custody / "moved-streams", target_is_directory=True)
        self.refuses()

    def test_bytes_names_permissions_and_extra_files_changed_during_inspection_refuse(self):
        original = retained_files.inspect_c115_retained_streams
        for path in (self.packet, self.sidecar / "manifest.json", self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"):
            for kind in ("bytes", "replacement", "permissions"):
                with self.subTest(path=path.name, kind=kind):
                    self.prepare()

                    def inspect(*args):
                        result = original(*args)
                        if kind == "bytes":
                            self.write(path, self.read(path) + b" ")
                        elif kind == "replacement":
                            replacement = self.custody / "replacement"
                            self.write(replacement, self.read(path))
                            os.replace(replacement, path)
                        else:
                            path.chmod(0o440)
                        return result

                    with patch.object(retained_files, "inspect_c115_retained_streams", side_effect=inspect):
                        self.refuses()
                    path.chmod(0o400)
        self.prepare()

        def extra(*args):
            result = original(*args)
            self.write(self.sidecar / "surplus.stderr", b"safe")
            return result

        with patch.object(retained_files, "inspect_c115_retained_streams", side_effect=extra):
            self.refuses()

    def test_packet_sidecar_and_private_ancestor_movement_during_inspection_refuses(self):
        original = retained_files.inspect_c115_retained_streams
        for folder in (self.sidecar, self.custody):
            moved = folder.with_name(folder.name + "-moved")

            def inspect(*args):
                result = original(*args)
                folder.rename(moved)
                return result

            with self.subTest(folder=folder.name), patch.object(retained_files, "inspect_c115_retained_streams", side_effect=inspect):
                self.refuses()
            moved.rename(folder)

    def test_exact_byte_readback_detects_same_size_drift_with_coarse_metadata(self):
        original = retained_files.inspect_c115_retained_streams
        self.value, self.streams = changed(self.value, self.streams, 0, stderr=b"safe-note-A")
        self.prepare()
        path = self.sidecar / "0000.stderr"

        def inspect(*args):
            result = original(*args)
            self.write(path, b"safe-note-B")
            return result

        with patch("access_telemetry_c1_owner_dispatch._file_metadata",
                   side_effect=lambda metadata: (metadata.st_dev, metadata.st_ino, metadata.st_mode, metadata.st_nlink,
                                                 metadata.st_uid, metadata.st_gid, metadata.st_size)), \
                patch.object(retained_files, "inspect_c115_retained_streams", side_effect=inspect):
            self.refuses(code="c115-files-artifact-bytes-drifted")

    def test_substitution_during_initial_read_refuses_and_closes_all_descriptors(self):
        for path in (self.packet, self.sidecar / "manifest.json", self.sidecar / "0000.stdout", self.sidecar / "0000.stderr"):
            with self.subTest(path=path.name):
                self.prepare()
                identity = path.stat().st_ino
                real_read, real_open, real_close = os.read, os.open, os.close
                opened, closed = [], []
                replacement = self.custody / "replacement"
                self.write(replacement, self.read(path))
                replaced = False

                def read(fd, count):
                    nonlocal replaced
                    raw = real_read(fd, count)
                    if os.fstat(fd).st_ino == identity and not replaced:
                        replaced = True
                        os.replace(replacement, path)
                    return raw

                def open_file(*args, **kwargs):
                    fd = real_open(*args, **kwargs)
                    opened.append(fd)
                    return fd

                def close_file(fd):
                    closed.append(fd)
                    return real_close(fd)

                with patch.object(retained_files.os, "read", side_effect=read), \
                        patch.object(retained_files.os, "open", side_effect=open_file), \
                        patch.object(retained_files.os, "close", side_effect=close_file):
                    self.refuses()
                self.assertCountEqual(opened, closed)
                self.inspect()

    def test_filesystem_and_cleanup_failures_and_effective_user_drift_have_no_exception_chain(self):
        for operation in ("open", "stat", "fstat", "read", "scandir", "lseek"):
            for error_type in (OSError, NotImplementedError, TypeError):
                with self.subTest(operation=operation, error_type=error_type.__name__), \
                        patch.object(retained_files.os, operation, side_effect=error_type("private-canary")):
                    self.refuses(code="c115-files-filesystem-unavailable")
        with patch.object(retained_files.os, "geteuid", side_effect=(os.geteuid(), os.geteuid() + 1)):
            self.refuses(code="c115-files-effective-user-drifted")
        real_close = os.close

        def close(fd):
            real_close(fd)
            raise OSError("private-canary cleanup")

        with patch.object(retained_files.os, "close", side_effect=close):
            self.refuses(code="c115-files-filesystem-unavailable")

    def test_each_invocation_reads_current_files_without_cached_inspection(self):
        self.inspect()
        (self.sidecar / "0000.stderr").unlink()
        self.refuses()
        self.prepare()
        self.inspect()
        self.write(self.sidecar / "0000.stderr", b"changed between calls")
        self.refuses(code="c115-stream-hash-mismatch")

    def test_legacy_or_blocked_packets_never_gain_retained_inspection(self):
        for mutate in (lambda value: value.update(schemaVersion="hexalith.access-telemetry.c1.evidence/v1"),
                       lambda value: value.update(gate="C1.16"),
                       lambda value: value.update(profileId="PG-ONPREM-1"),
                       lambda value: value.update(producerStatus="blocked")):
            self.value, self.streams = fixture()
            mutate(self.value)
            self.prepare()
            self.refuses()


if __name__ == "__main__":
    unittest.main()
