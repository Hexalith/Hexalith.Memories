"""Actual-file identity guards; offline checks confer no runtime evidence."""

from contextlib import ExitStack
from datetime import datetime, timedelta, timezone
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import access_telemetry_functional_retention as f


class SelectedLoadedInputTests(unittest.TestCase):
    def setUp(self):
        self.stack = ExitStack()
        self.addCleanup(self.stack.close)
        self.root = Path(self.stack.enter_context(tempfile.TemporaryDirectory()))
        self.root.chmod(0o700)
        self.artifact = self.root / "bin" / "bound.dll"
        self.artifact.parent.mkdir()
        self.artifact.write_bytes(b"MZ-A")
        self.digest = f._hash_file(self.artifact)[0]
        held = f._open_held_file(str(self.artifact), self.stack)
        self.assertEqual(self.digest, f._hash_file(self.artifact)[0])
        held.revalidate()
        self.before = self.artifact.stat()
        self.consumer = object.__new__(f._PlatformRetentionConsumer)
        self.consumer.root = self.root
        self.consumer.loaded_inputs = (held,)
        self.consumer.started, self.consumer.wall = time.monotonic(), time.time()
        self.consumer.deadline = datetime.now(timezone.utc) + timedelta(hours=2)
        self.consumer.plan_bytes = self.consumer.readiness_bytes = b"{}"
        gate = self.root / "gate.json"
        self.consumer.observation = {"gatePath": str(gate), "profileHash": "a" * 64}
        for path, raw in ((self.root / "plan.json", b"{}"), (self.root / "readiness.json", b"{}"),
                          (gate, json.dumps({"schemaVersion": 1, "state": "enabled", "profileSha256": "a" * 64,
                                             "expiresUtcMs": int(time.time() * 1000) + 600000}).encode())):
            path.write_bytes(raw)
            path.chmod(0o600)

    def test_unchanged_loaded_input_is_revalidated_without_rehashing(self):
        with patch.object(f, "_hash_file", side_effect=AssertionError("repeated hashing is not allowed")):
            self.consumer.check()
            self.artifact.read_bytes()  # Access-time changes do not invalidate the bound input.
            self.consumer.check()

    def test_sibling_files_and_subdirectories_do_not_freeze_unbound_inputs(self):
        sibling = self.artifact.parent / "unrelated-notes.md"
        sibling.write_text("new notes")
        (self.artifact.parent / "unrelated-output").mkdir()
        self.consumer.check()
        sibling.write_text("changed notes")
        self.consumer.check()

    def test_unchanged_parent_identity_does_not_freeze_directory_permissions(self):
        self.artifact.parent.chmod(0o750)
        self.consumer.check()

    def test_atomic_replacement_with_identical_bytes_and_mtime_is_refused(self):
        replacement = self.artifact.with_name("replacement.dll")
        replacement.write_bytes(self.artifact.read_bytes())
        os.utime(replacement, ns=(self.before.st_atime_ns, self.before.st_mtime_ns))
        os.replace(replacement, self.artifact)
        self.assertEqual(self.digest, f._hash_file(self.artifact)[0])
        self.assertNotEqual(self.before.st_ino, self.artifact.stat().st_ino)
        self.refused()

    def test_same_size_in_place_rewrite_is_refused(self):
        time.sleep(0.02)
        self.artifact.write_bytes(b"MZ-B")
        self.assertEqual(self.before.st_ino, self.artifact.stat().st_ino)
        self.assertEqual(self.before.st_size, self.artifact.stat().st_size)
        self.refused()

    def test_restoring_mtime_after_in_place_rewrite_does_not_hide_ctime_drift(self):
        time.sleep(0.02)
        self.artifact.write_bytes(b"MZ-B")
        os.utime(self.artifact, ns=(self.before.st_atime_ns, self.before.st_mtime_ns))
        self.assertEqual(self.before.st_mtime_ns, self.artifact.stat().st_mtime_ns)
        self.assertNotEqual(self.before.st_ctime_ns, self.artifact.stat().st_ctime_ns)
        self.refused()

    def test_parent_path_replacement_is_refused_even_if_the_old_file_is_held(self):
        previous = self.artifact.parent.with_name("previous-bin")
        self.artifact.parent.rename(previous)
        self.artifact.parent.mkdir()
        self.artifact.write_bytes(b"MZ-A")
        self.refused()

    def refused(self):
        with self.assertRaisesRegex(f.FunctionalRetentionError, "^functional-selected-loaded-artifact-drift$"):
            self.consumer.check()


if __name__ == "__main__":
    unittest.main()
