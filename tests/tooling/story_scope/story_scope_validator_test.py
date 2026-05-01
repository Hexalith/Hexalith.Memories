import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-story-file-scope.py"


def run_validator(*args, cwd=REPO_ROOT):
    return subprocess.run(
        [sys.executable, str(TOOL), *args],
        cwd=cwd,
        text=True,
        capture_output=True,
        check=False,
    )


class StoryScopeValidatorTests(unittest.TestCase):
    def make_artifacts(self, body: str) -> tempfile.TemporaryDirectory:
        temp = tempfile.TemporaryDirectory()
        artifacts = Path(temp.name)
        (artifacts / "12-3-story-file-scope-enforcement.md").write_text(
            textwrap.dedent(body),
            encoding="utf-8",
        )
        self.addCleanup(temp.cleanup)
        return temp

    def write_message(self, text: str) -> Path:
        path = Path(self._testMethodName + ".txt")
        path.write_text(textwrap.dedent(text), encoding="utf-8")
        self.addCleanup(lambda: path.unlink(missing_ok=True))
        return path

    def test_branch_name_story_key_discovery_allows_in_scope_file(self):
        result = run_validator(
            "--branch-name",
            "feature/12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/check-story-file-scope.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("Selected story key: 12-3-story-file-scope-enforcement", result.stdout)

    def test_story_trailer_discovery_allows_in_scope_file(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("Story source: trailer", result.stdout)

    def test_scope_override_trailer_can_cover_specific_non_forbidden_path(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            Scope-Override: docs/dev/story-scope.md - companion operator note
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "docs/dev/story-scope.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("Audited Scope-Override entries:", result.stdout)

    def test_out_of_scope_file_fails_without_override(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "docs/dev/story-scope.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Out-of-scope files:", result.stdout)
        self.assertIn("docs/dev/story-scope.md", result.stdout)

    def test_d5_style_source_touch_fails_even_with_override(self):
        message = self.write_message(
            """
            ci: update story scope validator

            Story: 12-3-story-file-scope-enforcement
            Scope-Override: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs - emergency fix
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "src/Hexalith.Memories.Server/Search/SemanticSearchService.cs",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Forbidden-default files cannot be authorized", result.stdout)

    def test_branch_and_trailer_conflict_fails_closed(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-2-forbidden-default-tolerances-checklist
            """
        )

        result = run_validator(
            "--branch-name",
            "feature/12-3-story-file-scope-enforcement",
            "--commit-message-file",
            str(message),
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Conflicting story keys", result.stdout)

    def test_multiple_story_trailers_fail(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            Story-Key: 12-3-story-file-scope-enforcement
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Multiple Story/Story-Key trailers", result.stdout)

    def test_vague_scope_overrides_fail(self):
        for override in ["*", ".", "src", "src/**", "repo-wide cleanup"]:
            with self.subTest(override=override):
                message = self.write_message(
                    f"""
                    docs: update scope guidance

                    Story: 12-3-story-file-scope-enforcement
                    Scope-Override: {override}
                    """
                )

                result = run_validator(
                    "--commit-message-file",
                    str(message),
                    "--changed-file",
                    "docs/dev/story-scope.md",
                )

                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Invalid Scope-Override", result.stdout)

    def test_exact_override_does_not_authorize_sibling_child_suffix_or_partial_paths(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            Scope-Override: docs/dev/story-scope.md - companion operator note
            """
        )

        for changed in [
            "docs/dev/story-scope-extra.md",
            "docs/dev/story-scope.md/child.txt",
            "other/docs/dev/story-scope.md",
            "docs/dev/story-scope.md.bak",
        ]:
            with self.subTest(changed=changed):
                result = run_validator(
                    "--commit-message-file",
                    str(message),
                    "--changed-file",
                    changed,
                )

                self.assertNotEqual(result.returncode, 0)
                self.assertIn(changed, result.stdout)

    def test_path_normalization_handles_windows_and_posix_inputs(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            r".\\tools\\..\\tools\\check-story-file-scope.py",
            "--changed-file",
            "./tests//tooling/story_scope/story_scope_validator_test.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)

    def test_zero_changed_files_is_explicit_no_op_success(self):
        result = run_validator()

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("No changed files; story-scope check is a no-op", result.stdout)

    def test_real_current_story_file_scope_pattern_is_parseable(self):
        result = run_validator(
            "--story-key",
            "12-2-forbidden-default-tolerances-checklist",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)

    def test_parser_rejects_missing_allowed_scope(self):
        temp = self.make_artifacts(
            """
            # Story 12.3

            ## File Scope

            Read/verify only:

            - `CONTRIBUTING.md`
            """
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("empty allowed scope", result.stdout)


if __name__ == "__main__":
    unittest.main()
