import importlib.util
import os
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path
from unittest import mock


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
    """Return the bullet lines under `header:` until the next non-bullet, non-blank line.

    Blank lines do NOT terminate a section so that future validator output that
    interleaves blank lines does not silently mask a section regression.
    """
    lines = stdout.splitlines()
    block: list[str] = []
    in_section = False
    for line in lines:
        if line.strip() == header:
            in_section = True
            continue
        if not in_section:
            continue
        if line.startswith("  - "):
            block.append(line[4:])
            continue
        if line == "":
            continue
        break
    return block


def stdio(result) -> str:
    """Combined stdout + stderr for assertions that must not couple to a sink."""
    return (result.stdout or "") + (result.stderr or "")


def load_tool_module():
    spec = importlib.util.spec_from_file_location("check_story_file_scope", TOOL)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


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

    def test_spec_key_resolves_from_cli_trailer_and_branch(self):
        spec_key = "spec-scope-owner"
        temp = self.fixture_artifacts(
            """
            # Standalone scope owner

            ## File Scope

            Allowed files for this story:

            - `docs/spec-owned.md` - fixture-owned path
            """,
            story_key=spec_key,
        )
        message = self.write_message(
            f"""
            docs: update standalone spec scope

            Story-Key: {spec_key}
            """
        )

        cases = (
            ("cli", ("--story-key", spec_key)),
            ("trailer", ("--commit-message-file", str(message))),
            ("branch", ("--branch-name", f"fix/{spec_key}")),
        )
        for expected_source, source_args in cases:
            with self.subTest(source=expected_source):
                result = run_validator(
                    "--artifacts-root",
                    temp.name,
                    *source_args,
                    "--changed-file",
                    "docs/spec-owned.md",
                )

                self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
                self.assertIn(f"Selected story key: {spec_key}", result.stdout)
                self.assertIn(f"Story source: {expected_source}", result.stdout)

    def test_spec_trailer_conflicting_with_numeric_story_branch_fails_closed(self):
        message = self.write_message(
            """
            docs: update standalone spec scope

            Story-Key: spec-scope-owner
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
        self.assertIn("trailer=spec-scope-owner", result.stdout)
        self.assertIn("branch=12-3-story-file-scope-enforcement", result.stdout)

    def test_extended_spec_key_does_not_fall_back_to_shorter_artifact(self):
        temp = self.fixture_artifacts(
            """
            # Standalone scope owner

            ## File Scope

            Allowed files for this story:

            - `docs/spec-owned.md` - fixture-owned path
            """,
            story_key="spec-scope-owner",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--branch-name",
            "fix/spec-scope-owner-extra",
            "--changed-file",
            "docs/spec-owned.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("spec-scope-owner-extra.md", result.stdout)
        self.assertNotIn("Selected story key: spec-scope-owner\n", result.stdout)

    def test_unowned_main_changed_set_fails_closed(self):
        result = run_validator(
            "--branch-name",
            "main",
            "--changed-file",
            "docs/unowned.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("No story key resolved", result.stdout)

    def test_spec_owner_rejects_mixed_artifact_and_submodule_changes(self):
        spec_key = "spec-scope-owner"
        temp = self.fixture_artifacts(
            """
            # Standalone scope owner

            ## File Scope

            Allowed files for this story:

            - `docs/spec-owned.md` - fixture-owned path
            """,
            story_key=spec_key,
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            spec_key,
            "--changed-file",
            "docs/spec-owned.md",
            "--changed-file",
            "_bmad-output/implementation-artifacts/27-3-other-owner.md",
            "--changed-file",
            "references/Hexalith.Tenants",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(
            "_bmad-output/implementation-artifacts/27-3-other-owner.md",
            section_block(result.stdout, "Out-of-scope files:"),
        )
        self.assertIn(
            "references/Hexalith.Tenants",
            section_block(result.stdout, "Forbidden-default files (no override; D5-class):"),
        )

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
        # 12.4-RV16: agreement must NOT report conflicting keys; otherwise a
        # future conflict-detection regression that exits 0 could co-exist
        # with this test.
        self.assertNotIn("Conflicting story keys", stdio(result))

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
            "references/Hexalith.EventStore",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(
            "references/Hexalith.EventStore",
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
        # 12.4-RV17: assert on combined stdout+stderr so the test does not
        # silently break if the error path moves between sinks.
        self.assertIn("--story-key value is not a valid story key", stdio(result))

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

    # --- STORY_KEY_PATTERN boundary coverage (12.4-RV9) -----------------------

    def test_story_key_regex_rejects_trailing_hyphen(self):
        # `12-3-foo-` has a trailing hyphen; the regex must not extract `12-3-foo`.
        result = run_validator(
            "--branch-name",
            "feature/12-3-foo-",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("No story key resolved", stdio(result))

    def test_story_key_regex_normalizes_uppercase_to_lowercase(self):
        result = run_validator(
            "--branch-name",
            "feature/12-3-STORY-FILE-SCOPE-ENFORCEMENT",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("Selected story key: 12-3-story-file-scope-enforcement", result.stdout)

    def test_story_key_regex_allows_single_letter_third_segment(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3 (single-letter title segment fixture)

            ## File Scope

            Allowed files for this story:

            - `CONTRIBUTING.md` - canonical entry
            """,
            story_key="12-3-a",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-a",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("Selected story key: 12-3-a", result.stdout)

    # --- Multi-key symmetry (12.4-RV7, 12.4-RV8) ------------------------------

    def test_multiple_keys_in_explicit_story_key_value_fails_with_all_keys_reported(self):
        result = run_validator(
            "--story-key",
            "14-1-ci-story-scope-enforcement-hardening and 12-3-story-file-scope-enforcement",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        combined = stdio(result)
        # The diagnostic must enumerate every detected key so the contributor
        # can fix the input without guessing which key was preferred.
        self.assertIn("12-3-story-file-scope-enforcement", combined)
        self.assertIn("14-1-ci-story-scope-enforcement-hardening", combined)
        self.assertIn("multiple story keys", combined.lower())

    def test_multiple_keys_in_branch_name_fails_with_all_keys_reported(self):
        # Use `/` between the two keys: the regex's `(?<![\w-])` / `(?![\w-])`
        # boundaries treat `/` as a separator so two distinct keys are detected.
        # A pure-hyphen chain like `14-1-foo-and-12-3-bar` is greedily matched
        # as ONE key, which is consistent with the regex contract; the
        # multi-key detection focuses on inputs the regex actually surfaces as
        # two keys.
        result = run_validator(
            "--branch-name",
            "feat/14-1-ci-story-scope-enforcement-hardening/12-3-story-file-scope-enforcement",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        combined = stdio(result)
        self.assertIn("12-3-story-file-scope-enforcement", combined)
        self.assertIn("14-1-ci-story-scope-enforcement-hardening", combined)
        self.assertIn("multiple story keys", combined.lower())

    def test_hyphen_and_joined_multiple_keys_in_branch_name_fails_with_all_keys_reported(self):
        result = run_validator(
            "--branch-name",
            "feat/14-1-ci-story-scope-enforcement-hardening-and-12-3-story-file-scope-enforcement",
            "--changed-file",
            "CONTRIBUTING.md",
        )

        self.assertNotEqual(result.returncode, 0)
        combined = stdio(result)
        self.assertIn("12-3-story-file-scope-enforcement", combined)
        self.assertIn("14-1-ci-story-scope-enforcement-hardening", combined)
        self.assertIn("multiple story keys", combined.lower())

    def test_branch_and_trailer_conflict_diagnostic_lists_both_sources(self):
        # The diagnostic must name source=key for every detected source so
        # contributors can fix the branch, trailer, or CLI without guessing.
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
        combined = stdio(result)
        self.assertIn("trailer=12-2-forbidden-default-tolerances-checklist", combined)
        self.assertIn("branch=12-3-story-file-scope-enforcement", combined)

    # --- git interpret-trailers absence (12.4-RV14) ---------------------------

    def test_missing_git_interpret_trailers_reports_clean_validation_error(self):
        # Simulate `git` not being on PATH so subprocess raises FileNotFoundError.
        # The validator must convert that into a ValidationError with an
        # actionable installation/PATH message and no Python stack trace.
        message = self.write_message(
            """
            docs: update scope guidance

            Story: 12-3-story-file-scope-enforcement
            """
        )

        empty_path_env = {**os.environ, "PATH": ""}
        result = subprocess.run(
            [sys.executable, str(TOOL),
             "--commit-message-file", str(message),
             "--changed-file", "CONTRIBUTING.md"],
            cwd=REPO_ROOT,
            text=True,
            capture_output=True,
            check=False,
            env=empty_path_env,
        )

        self.assertNotEqual(result.returncode, 0)
        combined = stdio(result)
        # Actionable hint must appear; raw Python traceback must not.
        self.assertIn("git", combined.lower())
        self.assertIn("interpret-trailers", combined)
        self.assertNotIn("Traceback", combined)
        self.assertNotIn("FileNotFoundError", combined)

    def test_git_without_interpret_trailers_reports_actionable_guidance(self):
        module = load_tool_module()
        completed = subprocess.CompletedProcess(
            args=["git", "interpret-trailers", "--parse"],
            returncode=1,
            stdout="",
            stderr="git: 'interpret-trailers' is not a git command. See 'git --help'.",
        )

        with mock.patch.object(module.subprocess, "run", return_value=completed):
            with self.assertRaises(module.ValidationError) as error:
                module.parse_trailers("Story: 12-3-story-file-scope-enforcement")

        message = str(error.exception)
        self.assertIn("Git with 'interpret-trailers' is required", message)
        self.assertIn("Install Git", message)
        self.assertIn("PATH", message)
        self.assertIn("not a git command", message)

    # --- Code fence edge cases (12.4-RV12) ------------------------------------

    def test_parser_handles_fences_longer_than_three_backticks(self):
        # A 4-backtick fence must not be closed by an inner 3-backtick fence;
        # otherwise an "illusion" allow-list inside an example would leak.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `real/path.md` - real authoritative entry

            Example template (illustrative only):

            ````markdown
            Allowed files for this story:

            - `illusion.md` - must NOT be parsed as authoritative

            ```python
            print("nested code block inside the example")
            ```
            ````
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

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

    def test_parser_does_not_close_fence_on_marker_with_trailing_text(self):
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `real/path.md` - real authoritative entry

            ````markdown
            ````not-a-closer
            Allowed files for this story:

            - `illusion.md` - must NOT be parsed as authoritative
            ````
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

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

    def test_parser_handles_tilde_fence_with_nested_backtick_fence(self):
        # ~~~ outer fence containing ``` look-alike must not be confused.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `real/path.md` - real authoritative entry

            Example (illustrative only):

            ~~~markdown
            Allowed files for this story:

            - `illusion-tilde.md` - must NOT be parsed as authoritative

            ```python
            print("nested backticks inside tilde fence")
            ```
            ~~~
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "illusion-tilde.md",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("illusion-tilde.md", section_block(result.stdout, "Out-of-scope files:"))

    # --- Allow-list termination (12.4-RV13) -----------------------------------

    def test_parser_does_not_terminate_on_bullet_with_trailing_colon_rationale(self):
        # The previous heuristic terminated the allow-list as soon as any
        # `stripped.endswith(":")` line appeared, which dropped legitimate
        # bullets whose rationale ended with a colon.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/parent.py` - main file with rationale ending in colon:
            - `tools/sibling.py` - sibling that must still be parseable

            Read/verify only:

            - `docs/notes.md`
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/sibling.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("tools/sibling.py", section_block(result.stdout, "In-scope changed files:"))

    def test_parser_terminates_on_known_section_label_only(self):
        # Allow-list collection must terminate at "Read/verify only:" /
        # "Forbidden by default:" but stay open on unrelated trailing-colon
        # prose under the File Scope section.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/parent.py` - main entry

            Read/verify only:

            - `docs/notes.md`
            - `tools/leaked-into-allow-list.py`
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        # `tools/leaked-into-allow-list.py` lives under "Read/verify only:".
        # The allow-list must NOT contain it.
        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/leaked-into-allow-list.py",
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(
            "tools/leaked-into-allow-list.py",
            section_block(result.stdout, "Out-of-scope files:"),
        )

    def test_parser_does_not_terminate_on_unrecognized_prose_with_trailing_colon(self):
        # An author note ending in `:` between bullets must not silently
        # truncate the allow-list.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/parent.py` - first entry

            Note about implementation order:

            - `tools/second.py` - second entry, must still be parsed
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/second.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        self.assertIn("tools/second.py", section_block(result.stdout, "In-scope changed files:"))

    # --- Multiple allow-list blocks (12.3-RV15) -------------------------------

    def test_parser_merges_multiple_allowed_files_blocks(self):
        # Some stories repeat "Allowed files for this story:" labels (e.g.,
        # bridging two `## File Scope` paragraphs). Document the merge
        # behavior so future drift fails this test, not silently in CI.
        temp = self.fixture_artifacts(
            """
            # Story 12.3

            ## File Scope

            Allowed files for this story:

            - `tools/first.py` - first block

            Read/verify only:

            - `docs/notes.md`

            Allowed files for this story:

            - `tools/second.py` - second block
            """,
            story_key="12-3-story-file-scope-enforcement",
        )

        result = run_validator(
            "--artifacts-root",
            temp.name,
            "--story-key",
            "12-3-story-file-scope-enforcement",
            "--changed-file",
            "tools/first.py",
            "--changed-file",
            "tools/second.py",
        )

        self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
        in_scope = section_block(result.stdout, "In-scope changed files:")
        self.assertIn("tools/first.py", in_scope)
        self.assertIn("tools/second.py", in_scope)

    # --- Story artifact path diagnostic (12.4-RV18) ---------------------------

    def test_fixture_test_reports_loaded_story_artifact_path(self):
        # A future loader-precedence bug could silently load a different
        # artifact than the fixture. Pin the diagnostic so the test fails
        # if the loaded path drifts.
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
        # Loaded path must match the fixture artifacts root, not a live
        # repository artifact.
        self.assertIn(
            f"Story artifact: {Path(temp.name).as_posix()}/12-2-forbidden-default-tolerances-checklist.md",
            result.stdout,
        )


if __name__ == "__main__":
    unittest.main()
