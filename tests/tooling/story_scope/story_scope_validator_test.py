import os
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-story-file-scope.py"


# Frozen snapshot of a real `## File Scope` shape (taken from
# 12-2-forbidden-default-tolerances-checklist on 2026-05-01). Lives inline so
# the parser-drift fixture is not affected by edits to live story artifacts.
FROZEN_STORY_FIXTURE = """\
# Story 12.2: Forbidden-Default Tolerances Checklist

Status: review

## File Scope

Allowed files for this story:

- `CONTRIBUTING.md` - UPDATE. Document the forbidden-default tolerances pattern.
- `_bmad-output/implementation-artifacts/12-2-forbidden-default-tolerances-checklist.md` - UPDATE Dev Agent Record.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` - UPDATE only through the BMad workflow.

Read/verify only:

- `_bmad-output/implementation-artifacts/epic-11-retro-2026-04-30.md`
- `tools/test-release.ps1`

Forbidden by default:

- `src/**/*.cs`
- `tests/**/*.cs`
- `tools/publish-nuget.ps1`
- submodule contents
"""


def run_validator(*args, cwd=REPO_ROOT):
    return subprocess.run(
        [sys.executable, str(TOOL), *args],
        cwd=cwd,
        text=True,
        capture_output=True,
        check=False,
    )


def section_block(stdout: str, header: str) -> list[str]:
    """Return the bullet lines under `header:` until the next section header."""
    lines = stdout.splitlines()
    block: list[str] = []
    in_section = False
    for line in lines:
        if line.strip() == header:
            in_section = True
            continue
        if in_section:
            if not line.startswith("  - "):
                break
            block.append(line[4:])
    return block


class StoryScopeValidatorTests(unittest.TestCase):
    def write_message(self, text: str) -> Path:
        handle = tempfile.NamedTemporaryFile(
            mode="w",
            suffix=".txt",
            delete=False,
            encoding="utf-8",
        )
        try:
            handle.write(textwrap.dedent(text))
        finally:
            handle.close()
        path = Path(handle.name)
        self.addCleanup(lambda: path.unlink(missing_ok=True))
        return path

    def write_changed_files(self, *paths: str) -> Path:
        handle = tempfile.NamedTemporaryFile(
            mode="w",
            suffix=".txt",
            delete=False,
            encoding="utf-8",
        )
        try:
            handle.write("\n".join(paths))
        finally:
            handle.close()
        path = Path(handle.name)
        self.addCleanup(lambda: path.unlink(missing_ok=True))
        return path

    def fixture_artifacts(self, body: str, story_key: str = "12-2-forbidden-default-tolerances-checklist") -> tempfile.TemporaryDirectory:
        temp = tempfile.TemporaryDirectory()
        (Path(temp.name) / f"{story_key}.md").write_text(
            textwrap.dedent(body),
            encoding="utf-8",
        )
        self.addCleanup(temp.cleanup)
        return temp

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

    def test_branch_and_trailer_agreement_passes(self):
        message = self.write_message(
            """
            docs: align story trailer with branch

            Story: 12-3-story-file-scope-enforcement
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

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        # Trailer wins over branch when they agree.
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
        self.assertIn("docs/dev/story-scope.md", section_block(result.stdout, "Audited Scope-Override entries:"))

    def test_out_of_scope_file_fails_without_override(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "docs/dev/story-scope.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("docs/dev/story-scope.md", section_block(result.stdout, "Out-of-scope files:"))

    def test_d5_style_source_touch_without_override_fails_loudly(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "src/Hexalith.Memories.Server/Search/SemanticSearchService.cs",
        )

        self.assertNotEqual(result.returncode, 0)
        # The plain D5 case (no override at all) must report the forbidden-default
        # diagnostic, not a generic out-of-scope message. Story 12.2's closure
        # note assigned 12.3 ownership of this prevention.
        self.assertIn(
            "src/Hexalith.Memories.Server/Search/SemanticSearchService.cs",
            section_block(result.stdout, "Forbidden-default files (no override; D5-class):"),
        )

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
        self.assertIn(
            "src/Hexalith.Memories.Server/Search/SemanticSearchService.cs",
            section_block(result.stdout, "Forbidden-default files cannot be authorized by Scope-Override:"),
        )

    def test_bare_submodule_pointer_change_is_forbidden_default(self):
        # 12.2 closure note: 12.3 owns prevention of submodule-pointer leaks.
        # The literal submodule-pointer tree entry has no trailing slash.
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "Hexalith.EventStore",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(
            "Hexalith.EventStore",
            section_block(result.stdout, "Forbidden-default files (no override; D5-class):"),
        )

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

    def test_duplicate_consistent_story_trailers_pass(self):
        # Real tooling commonly emits both Story: and Story-Key: with the same
        # value (e.g. amend cycles). Failing closed on agreement is over-strict.
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

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)

    def test_conflicting_story_and_story_key_trailers_fail(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            Story-Key: 12-2-forbidden-default-tolerances-checklist
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Conflicting Story/Story-Key trailers", result.stdout)

    def test_multiple_keys_in_single_story_trailer_value_fails(self):
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement and 12-4-baseline-failures-sweep
            """
        )

        result = run_validator(
            "--commit-message-file",
            str(message),
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Multiple story keys in single trailer", result.stdout)

    def test_unparseable_explicit_story_key_fails_closed(self):
        result = run_validator(
            "--story-key",
            "not-a-valid-key",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("--story-key value is not a valid story key", result.stdout)

    def test_vague_scope_overrides_fail(self):
        for override in ["*", ".", "src", "src/**", "repo-wide cleanup", ""]:
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
                self.assertIn(changed, section_block(result.stdout, "Out-of-scope files:"))

    def test_path_normalization_makes_windows_and_posix_inputs_equivalent(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            r".\\tools\\..\\tools\\check-story-file-scope.py",
            "--changed-file",
            "./tests//tooling/story_scope/story_scope_validator_test.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)

        in_scope = section_block(result.stdout, "In-scope changed files:")
        # Both inputs must normalize to their POSIX-style canonical form.
        self.assertIn("tools/check-story-file-scope.py", in_scope)
        self.assertIn("tests/tooling/story_scope/story_scope_validator_test.py", in_scope)
        # And no backslash- or `..`-bearing form should leak through.
        for entry in in_scope:
            self.assertNotIn("\\", entry)
            self.assertNotIn("..", entry.split("/"))

    def test_zero_changed_files_is_explicit_no_op_success(self):
        result = run_validator()

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("No changed files; story-scope check is a no-op", result.stdout)

    def test_unresolvable_changed_files_input_fails(self):
        result = run_validator(
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-files-file",
            "/nonexistent/changed-files.txt",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Changed-files input does not exist", result.stdout)

    def test_frozen_story_fixture_is_parseable(self):
        # Pin the parser against a snapshot of a real story shape so future
        # template drift is caught here even if live story files are edited.
        temp = self.fixture_artifacts(FROZEN_STORY_FIXTURE)

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-2-forbidden-default-tolerances-checklist",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("CONTRIBUTING.md", section_block(result.stdout, "In-scope changed files:"))

    def test_parser_rejects_missing_allowed_scope(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Read/verify only:

            - `CONTRIBUTING.md`
            """,
            story_key="12-3-story-file-scope-enforcement",
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

    def test_parser_ignores_fenced_code_blocks_inside_file_scope(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `real/path.md` - real authoritative entry

            Example template (illustrative only):

            ```markdown
            Allowed files for this story:

            - `illusion.md` - must NOT be parsed as authoritative
            ```
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        # `illusion.md` would only be allowed if the fence was ignored.
        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "illusion.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("illusion.md", section_block(result.stdout, "Out-of-scope files:"))

    def test_parser_rejects_indented_sub_bullets_as_authoritative(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/parent.py` - main file
              - sub-bullet referencing `tools/forbidden_sibling.py` for context only
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/forbidden_sibling.py",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/forbidden_sibling.py", section_block(result.stdout, "Out-of-scope files:"))

    def test_parser_rejects_bare_unbackticked_bullet_as_path(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/check-story-file-scope.py` - canonical entry
            - tools/inferred-from-prose.py - bare token, must not be parsed
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/inferred-from-prose.py",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/inferred-from-prose.py", section_block(result.stdout, "Out-of-scope files:"))

    def test_glob_star_does_not_cross_path_separators(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/*.py` - shallow allow
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/sub/nested.py",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("tools/sub/nested.py", section_block(result.stdout, "Out-of-scope files:"))

    def test_story_key_regex_does_not_capture_letter_prefixed_digit_run(self):
        # `feat/abc123-12-3-foo` must not resolve to `123-12-3-foo`.
        result = run_validator(
            "--branch-name",
            "feat/abc123-12-3-foo",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("No story key resolved", result.stdout)


if __name__ == "__main__":
    unittest.main()
