"""Exercise story-scope path handling through the validator and Git hook."""

import importlib.util
import ntpath
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path, PureWindowsPath
from unittest import mock


ROOT = Path(__file__).resolve().parents[3]
VALIDATOR = ROOT / "tools/check-story-file-scope.py"
STORY = "spec-fixture"


class StoryScopePathsTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory(prefix="story scope ")
        self.addCleanup(directory.cleanup)
        self.repo = Path(directory.name)
        self.environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
        self.environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        self.artifacts = self.repo / "_bmad-output/implementation-artifacts"
        self.artifacts.mkdir(parents=True)
        (self.artifacts / "sprint-status.yaml").write_text("development_status: {}\n", encoding="utf-8")

    def run_command(self, *arguments, env=None, check=False):
        return subprocess.run(
            arguments, cwd=self.repo, env=self.environment if env is None else env,
            text=True, capture_output=True, check=check, timeout=20,
        )

    def validate(self, *arguments):
        return self.run_command(sys.executable, str(VALIDATOR), *arguments)

    def write_scope(self, body):
        story = self.artifacts / f"{STORY}.md"
        story.write_text("## File Scope\n\n" + body, encoding="utf-8")
        return story

    def test_ownerless_automation_is_a_no_op_but_invalid_or_conflicting_owners_fail(self):
        args = ("--branch-name", "dependabot/npm_and_yarn/package-1.2.3", "--changed-file", "package-lock.json")
        ownerless = self.validate(*args)
        self.assertEqual(0, ownerless.returncode, ownerless.stdout)
        self.assertIn("No story key resolved; story-scope check is a no-op.", ownerless.stdout)
        invalid = self.validate(*args, "--story-key", "invalid-owner")
        self.assertEqual(1, invalid.returncode, invalid.stdout)
        self.assertIn("is not a valid story key", invalid.stdout)
        message = self.repo / "message.txt"
        message.write_text("test: Verify owner conflict\n\nStory-Key: spec-other\n", encoding="utf-8")
        conflict = self.validate("--branch-name", STORY, "--commit-message-file", str(message),
                                 "--changed-file", "docs/file.md")
        self.assertEqual(1, conflict.returncode, conflict.stdout)
        self.assertIn("Conflicting story keys: trailer=spec-other, branch=spec-fixture", conflict.stdout)

    def test_blank_list_is_a_no_op_but_nonblank_paths_cannot_normalize_to_empty(self):
        changed = self.repo / "changes.txt"
        changed.write_text("\n \t\n", encoding="utf-8")
        result = self.validate("--changed-files-file", str(changed))
        self.assertEqual(0, result.returncode, result.stdout)
        self.assertIn("No changed files", result.stdout)

        for value in (".", "../..", "docs/..", "``"):
            with self.subTest(value=value):
                changed.write_text(f"\n{value}\n", encoding="utf-8")
                result = self.validate("--changed-files-file", str(changed))
                self.assertEqual(1, result.returncode, result.stdout)
                self.assertIn("does not identify a repository file", result.stdout)
        direct = self.validate("--changed-file", "docs/..")
        self.assertEqual(1, direct.returncode, direct.stdout)

    def test_legacy_allowed_labels_warn_about_bare_bullets(self):
        for label in ("Expected files to add or edit:", "Allowed to modify:"):
            with self.subTest(label=label):
                story = self.write_scope(f"{label}\n- `docs/allowed.md`\n- docs/bare.md\n")
                allowed = self.validate("--story-key", STORY, "--changed-file", "docs/allowed.md")
                self.assertEqual(0, allowed.returncode, allowed.stdout)
                self.assertIn(f"{story.relative_to(self.repo).as_posix()}:5:", allowed.stderr)
                self.assertIn("ignored allowed-scope bullet", allowed.stderr)
                denied = self.validate("--story-key", STORY, "--changed-file", "docs/bare.md")
                self.assertEqual(1, denied.returncode, denied.stdout)
                self.assertIn("Out-of-scope files:\n  - docs/bare.md", denied.stdout)

    def test_windows_diagnostics_omit_cross_drive_and_unc_anchors(self):
        spec = importlib.util.spec_from_file_location("test_story_scope_validator", VALIDATOR)
        module = importlib.util.module_from_spec(spec)
        with (
            mock.patch.dict(sys.modules, {spec.name: module}),
            mock.patch.object(sys, "path", [str(ROOT / "tools"), *sys.path]),
        ):
            spec.loader.exec_module(module)
        cases = {
            "C:/repo/docs/file.md": "docs/file.md",
            "D:/stories/file.md": "<external>/stories/file.md",
            "//server/share/stories/file.md": "<external>/stories/file.md",
        }
        relpath = ntpath.relpath
        with (
            mock.patch.object(module.Path, "cwd", return_value=PureWindowsPath("C:/repo")),
            mock.patch.object(os.path, "relpath", side_effect=relpath),
        ):
            for value, expected in cases.items():
                with self.subTest(value=value):
                    self.assertEqual(expected, module.display_path(PureWindowsPath(value)))

    def test_all_three_checkers_reject_a_malformed_nul_input(self):
        artifact = self.artifacts / f"{STORY}.md"
        artifact.write_text("Status: in-progress\n\n### File List\n\n- `docs/file.md`\n", encoding="utf-8")
        changed = self.repo / "changes.nul"
        changed.write_bytes(b"docs/file.md\x00docs/other.md")
        scripts = (VALIDATOR, ROOT / "tools/check-tenant-isolation-evidence.py",
                   ROOT / "tools/check-story-review-readiness.py")
        for script in scripts:
            with self.subTest(script=script.name):
                result = self.run_command(sys.executable, str(script), "--story-key", STORY,
                                          "--changed-files0-file", str(changed))
                self.assertEqual(1, result.returncode, result.stdout + result.stderr)
                self.assertIn("nonempty NUL-terminated Git records", result.stdout)

    def test_missing_nul_path_list_uses_relative_diagnostics(self):
        missing = self.repo / "missing paths.nul"
        result = self.validate("--changed-files0-file", str(missing))
        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("Cannot read Git changed paths from missing paths.nul", result.stdout)
        self.assertNotIn(str(self.repo), result.stdout)

    def test_readiness_uses_the_supplied_index_snapshot_for_cumulative_validation(self):
        self.run_command("git", "init", f"--initial-branch={STORY}", check=True)
        self.run_command("git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                         "commit", "--allow-empty", "-m", "test: Establish a baseline", check=True)
        baseline = self.run_command("git", "rev-parse", "HEAD", check=True).stdout.strip()
        artifact = self.artifacts / f"{STORY}.md"
        artifact.write_text(
            f"---\nstatus: in-progress\nbaseline_commit: {baseline}\n---\n"
            "\n### File List\n\n- `docs/file.md`\n", encoding="utf-8",
        )
        changed = self.repo / "changes.nul"
        changed.write_bytes(b"docs/file.md\x00")
        (self.repo / "later-stage.txt").write_text("new index content\n", encoding="utf-8")
        self.run_command("git", "add", "--", "later-stage.txt", check=True)
        result = self.run_command(sys.executable, str(ROOT / "tools/check-story-review-readiness.py"),
                                  "--branch-name", STORY, "--derive-cumulative",
                                  "--changed-files0-file", str(changed))
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("C1: File List exactly matches all 1 in-scope changed paths.", result.stdout)

    @unittest.skipIf(os.name == "nt", "Uses POSIX hooks and filenames with newlines")
    def test_pre_commit_checks_literal_staged_names_with_a_python_fallback(self):
        self.run_command("git", "init", f"--initial-branch={STORY}", check=True)
        self.write_scope("Allowed files for this story:\n- `docs/**`\n")
        hooks = self.repo / ".githooks"
        hooks.mkdir()
        shutil.copy2(ROOT / ".githooks/pre-commit", hooks / "pre-commit")
        shutil.copy2(ROOT / ".githooks/python-interpreter.sh", hooks / "python-interpreter.sh")
        (self.repo / "tools").symlink_to(ROOT / "tools", target_is_directory=True)
        bin_path = self.repo / "bin"
        bin_path.mkdir()
        (bin_path / "git").symlink_to(shutil.which("git"))
        (bin_path / "sh").symlink_to(shutil.which("sh"))
        fallback = bin_path / "python"
        fallback.write_text("#!/bin/sh\nexit 1\n", encoding="utf-8")
        fallback.chmod(0o755)
        environment = dict(self.environment, PATH=str(bin_path))

        missing = self.run_command(".githooks/pre-commit", env=environment)
        self.assertEqual(1, missing.returncode, missing.stdout + missing.stderr)
        self.assertIn("Git hooks require Python 3.9 or newer", missing.stderr)
        fallback.unlink()
        fallback.symlink_to(sys.executable)

        allowed_path = "docs/café space\nname.md"
        denied_path = "src/café space\nname.cs"
        for path in (allowed_path, denied_path):
            target = self.repo / path
            target.parent.mkdir()
            target.write_text("fixture\n", encoding="utf-8")
        self.run_command("git", "add", "--", allowed_path, check=True)
        allowed = self.run_command(".githooks/pre-commit", env=environment)
        self.assertEqual(0, allowed.returncode, allowed.stdout + allowed.stderr)
        self.assertIn(f"  - {allowed_path!r}", allowed.stdout)

        self.run_command("git", "add", "--", denied_path, check=True)
        denied = self.run_command(".githooks/pre-commit", env=environment)
        self.assertEqual(1, denied.returncode, denied.stdout + denied.stderr)
        self.assertIn(f"Forbidden-default files (no override; D5-class):\n  - {denied_path!r}", denied.stdout)
