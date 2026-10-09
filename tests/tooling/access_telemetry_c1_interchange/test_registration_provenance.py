"""Isolated registration and local Git fixtures; no owner or target authority."""

from __future__ import annotations

from copy import deepcopy
from dataclasses import FrozenInstanceError, replace
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))

from access_telemetry_c1_interchange import InterchangeFormatError  # noqa: E402
from access_telemetry_c1_producer_bindings import (  # noqa: E402
    inspect_registry, lookup_deployed_binding,
)
from access_telemetry_c1_source_provenance import corroborate_local_sources  # noqa: E402
import access_telemetry_c1_source_provenance as provenance  # noqa: E402
from test_producer_bindings import (  # noqa: E402
    PATHS, fixture_capture, fixture_sources, registry_entry, snapshot,
)


def git(root, *args):
    return subprocess.check_output(("git", *args), cwd=root, stderr=subprocess.DEVNULL).decode().strip()


def reference(path, snap):
    return {"path": path, "sha256": hashlib.sha256(snap.raw).hexdigest(), "byteLength": len(snap.raw)}


class RegistrationProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="isolated-c1-registration-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        git(self.root, "init", "-q")
        (self.root / ".gitattributes").write_text("* text eol=crlf\n", encoding="utf-8")
        for source in fixture_sources():
            target = self.root / source.path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(source.executed_bytes)
        for path in ("fixtures/stories/isolated-done-label.md", "fixtures/never-executed-verifier.py"):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(("# ISOLATED OFFLINE FIXTURE: " + path + "\r\n").encode())
        git(self.root, "add", ".")
        git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
            "commit", "-qm", "fixture: isolated sources")
        self.commit = git(self.root, "rev-parse", "HEAD")
        self.sources = tuple(replace(source, source_commit=self.commit) for source in fixture_sources())
        self.capture_value = fixture_capture(self.sources)
        self.capture_value["sourceCommit"] = self.commit
        self.capture = snapshot(self.capture_value)
        self.command_value = {
            "schemaVersion": "hexalith.access-telemetry.c1.fixture-command-contract/v1",
            "gate": "C1.15", "profileId": "PG-ONPREM-2", "producerPath": PATHS[0],
            "executable": "kubectl", "purposes": ["current-context", "lifecycle-pods", "daprd-version",
                                           "metadata", "alpha-opt-in", "lifecycle-pods-recheck"],
            "readOnly": True,
        }
        self.role_value = {
            "schemaVersion": "hexalith.access-telemetry.c1.fixture-review-role-policy/v1",
            "gate": "C1.15", "profileId": "PG-ONPREM-2",
            "reviewerRole": "independent-security-reviewer", "producerExcluded": True,
        }
        self._refresh()

    def _refresh(self, *, statement_update=None, command_update=None, role_update=None, entry_update=None):
        self.command_value = dict(self.command_value, **(command_update or {}))
        self.role_value = dict(self.role_value, **(role_update or {}))
        self.command = snapshot(self.command_value)
        self.role = snapshot(self.role_value)
        self.entry = registry_entry()
        self.entry["commandContract"] = reference("fixtures/command.json", self.command)
        self.entry["reviewRolePolicy"] = reference("fixtures/roles.json", self.role)
        self.entry.update(entry_update or {})
        statement = {key: deepcopy(value) for key, value in self.entry.items() if key != "registrationReceipt"}
        statement.update(schemaVersion="hexalith.access-telemetry.c1.fixture-registration/v1",
                         sourceCommit=self.commit)
        statement.update(statement_update or {})
        self.statement = snapshot(statement)
        self.entry["registrationReceipt"] = reference("fixtures/registration.json", self.statement)
        self.registry = inspect_registry(snapshot([self.entry]))

    def verify(self):
        return corroborate_local_sources(self.registry, self.capture, self.sources, self.commit,
                                         self.statement, self.command, self.role, str(self.root))

    def refuse(self, code=None):
        with self.assertRaises(InterchangeFormatError) as caught:
            self.verify()
        if code is not None:
            self.assertEqual(str(caught.exception), code)
        self.assertLess(len(str(caught.exception)), 80)
        self.assertNotIn("ISOLATED", str(caught.exception))

    def test_exact_fixture_corroborates_all_sources_without_deployed_entry(self):
        with patch("socket.socket", side_effect=AssertionError("network")), \
                patch("urllib.request.urlopen", side_effect=AssertionError("network")), \
                patch("os.system", side_effect=AssertionError("target")):
            result = self.verify()
        self.assertEqual(result.source_commit, self.commit)
        self.assertEqual(result.paths, tuple(sorted(PATHS)))
        self.assertEqual(len(result.git_blob_oids), 19)
        self.assertEqual(result.registration.registration_sha256, self.statement.sha256)
        self.assertEqual(result.registration.command_sha256, self.command.sha256)
        self.assertEqual(result.registration.role_policy_sha256, self.role.sha256)
        self.assertFalse(hasattr(result, "accepted"))
        with self.assertRaises(FrozenInstanceError):
            result.source_commit = "changed"
        with self.assertRaises(InterchangeFormatError) as caught:
            lookup_deployed_binding("C1.15", "PG-ONPREM-2", self.registry)
        self.assertEqual(str(caught.exception), "deployed-producer-binding-unavailable")

    def test_statement_and_material_drift_refuse_before_any_git_or_file_call(self):
        for update in ({"registeredStory": "fixtures/other.md"}, {"gate": "C1.16"},
                       {"sourceCommit": "f" * 40}, {"producerPath": PATHS[1]},
                       {"verifierSchema": "other"}, {"cleanupRequired": True}):
            with self.subTest(update=update):
                self._refresh(statement_update=update)
                with patch("access_telemetry_c1_source_provenance._git", side_effect=AssertionError("git")), \
                        patch("access_telemetry_c1_source_provenance._open_directory",
                              side_effect=AssertionError("filesystem")):
                    self.refuse()
        self._refresh(command_update={"executable": "sh"})
        with patch("access_telemetry_c1_source_provenance._git", side_effect=AssertionError("git")):
            self.refuse()
        self._refresh(command_update={"executable": "kubectl"}, role_update={"producerExcluded": False})
        with patch("access_telemetry_c1_source_provenance._git", side_effect=AssertionError("git")):
            self.refuse()
        self._refresh(role_update={"producerExcluded": True})
        for name in ("registrationReceipt", "commandContract", "reviewRolePolicy"):
            with self.subTest(reference=name):
                entry = deepcopy(self.entry)
                entry[name]["sha256"] = "f" * 64
                registry = inspect_registry(snapshot([entry]))
                with patch("access_telemetry_c1_source_provenance._git", side_effect=AssertionError("git")):
                    with self.assertRaises(InterchangeFormatError) as caught:
                        corroborate_local_sources(registry, self.capture, self.sources, self.commit,
                                                  self.statement, self.command, self.role, str(self.root))
                self.assertEqual(str(caught.exception), "reference-digest-mismatch")

    def test_wrong_revision_dirty_and_untracked_sources_refuse(self):
        self._refresh()
        (self.root / PATHS[0]).write_bytes(b"changed\r\n")
        self.refuse("local-repository-dirty")
        git(self.root, "checkout", "--", PATHS[0])
        (self.root / "untracked.txt").write_text("untracked", encoding="utf-8")
        self.refuse("local-repository-dirty")
        (self.root / "untracked.txt").unlink()
        git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
            "commit", "--allow-empty", "-qm", "fixture: newer head")
        self.refuse("local-head-commit-mismatch")

    def test_symlink_foreign_blob_mode_and_attribute_refuse(self):
        self._refresh()
        source = self.root / PATHS[0]
        source.unlink()
        # A link to identical bytes still fails descriptor-relative O_NOFOLLOW.
        alias = self.root / "source-alias"
        alias.write_bytes(self.sources[0].executed_bytes)
        source.symlink_to(alias)
        actual_git = provenance._git
        def clean_status(root_fd, *args):
            if args[0] == "status":
                return b""
            return actual_git(root_fd, *args)
        with patch.object(provenance, "_git", side_effect=clean_status):
            self.refuse("local-source-unavailable")
        source.unlink()
        alias.unlink()
        source.write_bytes(self.sources[0].executed_bytes)
        # A valid blob object for a different tracked path cannot substitute.
        foreign_oid = git(self.root, "rev-parse", f"{self.commit}:{PATHS[1]}")
        def foreign_tree(root_fd, *args):
            if args[0] == "ls-tree" and args[-1] == PATHS[0]:
                return f"100644 blob {foreign_oid}\t{PATHS[0]}\0".encode()
            return actual_git(root_fd, *args)
        with patch.object(provenance, "_git", side_effect=foreign_tree):
            self.refuse("local-source-blob-mismatch")
        (self.root / ".git" / "info" / "attributes").write_text(
            f"{PATHS[0]} filter=fixture-filter\n", encoding="utf-8")
        self.refuse("local-source-attributes-unsupported")
        (self.root / ".git" / "info" / "attributes").unlink()
        source.chmod(0o755)
        with patch.object(provenance, "_git", side_effect=clean_status):
            self.refuse("local-source-executed-bytes-mismatch")
        source.chmod(0o644)
        alias = self.root / ".git" / "info" / "source-alias"
        os.link(source, alias)
        self.refuse("local-source-not-regular-bounded")
        alias.unlink()

    def test_missing_git_blob_object_refuses(self):
        self._refresh()
        oid = git(self.root, "rev-parse", f"{self.commit}:{PATHS[0]}")
        object_path = self.root / ".git" / "objects" / oid[:2] / oid[2:]
        self.assertTrue(object_path.is_file())
        object_path.unlink()
        self.refuse("local-git-unavailable")

    def test_replacement_commit_cannot_change_selected_tree(self):
        self._refresh()
        (self.root / PATHS[0]).write_bytes(b"substituted\r\n")
        git(self.root, "add", PATHS[0])
        git(self.root, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
            "commit", "-qm", "fixture: replacement tree")
        replacement = git(self.root, "rev-parse", "HEAD")
        git(self.root, "reset", "--hard", self.commit)
        git(self.root, "replace", self.commit, replacement)
        replaced_oid = git(self.root, "rev-parse", f"{self.commit}:{PATHS[0]}")
        self.assertNotEqual(replaced_oid, self.capture_value["producerSources"][0]["gitBlobOid"])
        self.assertEqual(self.verify().source_commit, self.commit)

    def test_skip_worktree_and_assume_unchanged_refuse_even_when_status_is_clean(self):
        self._refresh()
        source = self.root / PATHS[0]
        for flag, clear in (("--skip-worktree", "--no-skip-worktree"),
                            ("--assume-unchanged", "--no-assume-unchanged")):
            with self.subTest(flag=flag):
                git(self.root, "update-index", flag, "--", PATHS[0])
                source.write_bytes(b"hidden dirty bytes\r\n")
                self.assertEqual(git(self.root, "status", "--porcelain=v1", "--untracked-files=all"), "")
                self.refuse("local-repository-index-flags-unsupported")
                git(self.root, "update-index", clear, "--", PATHS[0])
                git(self.root, "checkout", "--", PATHS[0])

    def test_hidden_dirty_attributes_refuse_with_all_inspected_paths_normal(self):
        self._refresh()
        attributes = self.root / ".gitattributes"
        for flag, clear in (("--skip-worktree", "--no-skip-worktree"),
                            ("--assume-unchanged", "--no-assume-unchanged")):
            with self.subTest(flag=flag):
                git(self.root, "update-index", flag, "--", ".gitattributes")
                attributes.write_text("* text eol=crlf filter=hidden\n", encoding="utf-8")
                self.assertEqual(git(self.root, "status", "--porcelain=v1", "--untracked-files=all"), "")
                for path in (*PATHS, self.entry["registeredStory"], self.entry["verifierPath"]):
                    self.assertTrue(git(self.root, "ls-files", "-v", "--", path).startswith("H "))
                self.refuse("local-repository-index-flags-unsupported")
                git(self.root, "update-index", clear, "--", ".gitattributes")
                git(self.root, "checkout", "--", ".gitattributes")

    def test_object_alternates_refuse_before_git(self):
        self._refresh()
        alternates = self.root / ".git" / "objects" / "info" / "alternates"
        alternates.write_text("/isolated/other-object-store\n", encoding="utf-8")
        with patch.object(provenance, "_git", side_effect=AssertionError("git called")):
            self.refuse("local-object-alternates-unsupported")

    def test_missing_story_and_verifier_at_commit_refuse(self):
        for field in ("registeredStory", "verifierPath"):
            with self.subTest(field=field):
                self._refresh(entry_update={field: "fixtures/missing-file.py"})
                self.refuse("local-auxiliary-source-missing")

    def test_story_and_verifier_working_paths_are_safely_read(self):
        self._refresh()
        for field in ("registeredStory", "verifierPath"):
            with self.subTest(field=field):
                path = self.root / self.entry[field]
                original = path.read_bytes()
                path.unlink()
                path.symlink_to(self.root / PATHS[0])
                actual_git = provenance._git
                def clean_status(root_fd, *args):
                    if args[0] == "status":
                        return b""
                    return actual_git(root_fd, *args)
                with patch.object(provenance, "_git", side_effect=clean_status):
                    self.refuse("local-source-unavailable")
                path.unlink()
                path.write_bytes(original)

    def test_bounded_child_output_is_reaped(self):
        real_popen = subprocess.Popen
        children = []
        def oversized_child(command, **kwargs):
            child = real_popen([sys.executable, "-c", "import os; os.write(1, b'x' * 2097152)"],
                               stdout=kwargs["stdout"], stderr=kwargs["stderr"])
            children.append(child)
            return child
        root_fd = os.open(self.root, os.O_RDONLY | os.O_DIRECTORY)
        try:
            with patch.object(provenance.subprocess, "Popen", side_effect=oversized_child):
                with self.assertRaises(InterchangeFormatError) as caught:
                    provenance._git(root_fd, "status", "--porcelain=v1")
            self.assertEqual(str(caught.exception), "local-git-output-budget-exceeded")
            self.assertEqual(len(children), 1)
            self.assertIsNotNone(children[0].poll())
        finally:
            os.close(root_fd)

    def test_stalled_child_is_killed_at_timeout_boundary(self):
        real_popen = subprocess.Popen
        children = []
        def stalled_child(command, **kwargs):
            child = real_popen([sys.executable, "-c", "import time; time.sleep(30)"],
                               stdout=kwargs["stdout"], stderr=kwargs["stderr"])
            children.append(child)
            return child
        root_fd = os.open(self.root, os.O_RDONLY | os.O_DIRECTORY)
        try:
            with patch.object(provenance.subprocess, "Popen", side_effect=stalled_child), \
                    patch.object(provenance.select, "select", return_value=([], [], [])):
                with self.assertRaises(InterchangeFormatError) as caught:
                    provenance._git(root_fd, "status", "--porcelain=v1")
            self.assertEqual(str(caught.exception), "local-git-unavailable")
            self.assertEqual(len(children), 1)
            self.assertIsNotNone(children[0].poll())
        finally:
            os.close(root_fd)

    def test_child_git_commands_and_environment_are_closed(self):
        self._refresh()
        real_popen = subprocess.Popen
        invocations = []
        def guarded_child(command, **kwargs):
            self.assertEqual(command[0], "git")
            allowed = {"rev-parse", "status", "ls-files", "ls-tree", "cat-file", "check-attr"}
            self.assertEqual(len(allowed.intersection(command)), 1)
            self.assertTrue({"fetch", "clone", "remote", "push"}.isdisjoint(command))
            self.assertIn("protocol.allow=never", command)
            environment = kwargs["env"]
            self.assertEqual(environment["GIT_NO_REPLACE_OBJECTS"], "1")
            self.assertEqual(environment["GIT_NO_LAZY_FETCH"], "1")
            self.assertEqual(environment["GIT_CONFIG_NOSYSTEM"], "1")
            self.assertEqual(environment["GIT_TERMINAL_PROMPT"], "0")
            self.assertNotIn("GIT_ALTERNATE_OBJECT_DIRECTORIES", environment)
            invocations.append(tuple(command))
            return real_popen(command, **kwargs)
        with patch.object(provenance.subprocess, "Popen", side_effect=guarded_child):
            self.verify()
        self.assertGreater(len(invocations), 20)

    def test_owner_execute_only_and_root_trailing_space(self):
        self._refresh()
        source = self.root / PATHS[0]
        source.chmod(0o654)
        actual_git = provenance._git
        def clean_status(root_fd, *args):
            if args[0] == "status":
                return b""
            return actual_git(root_fd, *args)
        with patch.object(provenance, "_git", side_effect=clean_status):
            self.assertEqual(self.verify().source_commit, self.commit)
        source.chmod(0o644)
        trailing_root = self.root / "checkout "
        trailing_root.mkdir()
        for child in list(self.root.iterdir()):
            if child != trailing_root:
                shutil.move(str(child), str(trailing_root / child.name))
        self.root = trailing_root
        self.assertEqual(self.verify().repository_root, str(trailing_root))


if __name__ == "__main__":
    unittest.main()
