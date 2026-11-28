"""Exercise artifact selection and validation gates through the readiness CLI."""

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


VALIDATOR = Path(__file__).resolve().parents[3] / "tools/check-story-review-readiness.py"
KEY = "99-9-fixture"
ORIGINAL = f"spec-{KEY}.md"
CHECKPOINTS = (
    "\n### Checkpoints\n\n"
    "| Checkpoint | Review state |\n"
    "| ---------- | ------------ |\n"
)
CREATE_STORY_LEDGER = (
    "\n### Change Log\n\n"
    "| Date | Phase | Change | Test count | File List reconciliation |\n"
    "| --- | --- | --- | --- | --- |\n"
    "| 2026-11-26 | create-story | Define fixture | 1 discovered | Matched 1/1 |\n"
)
REVIEW_LEDGER = CREATE_STORY_LEDGER + (
    "| 2026-11-27 | dev-story | Implement fixture | 1 passed | Matched 1/1 |\n"
)
VERIFIED_CHECKPOINT = CHECKPOINTS + "| Verification | Verified 2026-11-27 |\n"


class StoryArtifactResolutionTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory(prefix="story-artifact-")
        self.addCleanup(directory.cleanup)
        self.repo = Path(directory.name)
        self.artifacts = self.repo / "_bmad-output/implementation-artifacts"
        self.artifacts.mkdir(parents=True)
        self.environment = {
            name: value for name, value in os.environ.items() if not name.startswith(("GIT_", "GITHUB_"))
        }
        self.environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        self.write_artifact("sprint-status.yaml", f"development_status:\n  {KEY}: in-progress\n")
        self.write_artifact(ORIGINAL, "# Obsolete spec\n")

    def write_artifact(self, name, text):
        (self.artifacts / name).write_text(text, encoding="utf-8")

    def write_follow_up(self, number, path, *, status="in-progress", story_key=KEY, evidence=""):
        name = f"spec-{KEY}-{number}.md"
        self.write_artifact(
            name,
            f"---\nstatus: '{status}'\nstory_key: '{story_key}'\n---\n"
            f"\n### File List\n\n- `{path}`\n{evidence}",
        )
        return name

    def command(self, *args, check=False, text=True):
        return subprocess.run(
            args, cwd=self.repo, env=self.environment, check=check,
            capture_output=True, text=text, timeout=20,
        )

    def validate(self, *args):
        return self.command(sys.executable, str(VALIDATOR), *args)

    def stage_changed_paths(self, *paths):
        self.command("git", "init", "--quiet", check=True)
        for path in paths:
            target = self.repo / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("fixture\n", encoding="utf-8")
        self.command("git", "add", "--", *paths, check=True)
        changed = self.repo / "changed-files.nul"
        changed.write_bytes(self.command(
            "git", "diff", "--cached", "--no-renames", "--name-only", "-z", "--",
            check=True, text=False,
        ).stdout)
        return "--story-key", Path(ORIGINAL).stem, "--changed-files0-file", str(changed)

    def write_file_list(self, rows, exclusions=""):
        text = "Status: in-progress\n\n### File List\n\n" + rows + "\n"
        if exclusions:
            text += "\n### File List Exclusions\n\n" + exclusions + "\n"
        self.write_artifact(ORIGINAL, text)

    def assert_selected(self, result, name):
        self.assertIn(f"Story artifact: _bmad-output/implementation-artifacts/{name}\n", result.stdout)

    def test_latest_active_numbered_spec_is_checked_instead_of_obsolete_original(self):
        self.write_artifact(f"{KEY}.md", "# Obsolete story\n")
        self.write_follow_up(2, "docs/earlier.md")
        active = self.write_follow_up(10, "docs/current.md")
        self.write_follow_up(11, "docs/draft.md", status="draft")
        self.write_follow_up(12, "docs/historical.md", status="done")
        self.write_follow_up(13, "docs/other-story.md", story_key=f"{KEY}-13")
        self.write_follow_up("14-not-a-revision", "docs/near-match.md")

        passed = self.validate("--story-key", KEY, "--changed-file", "docs/current.md")
        self.assertEqual(0, passed.returncode, passed.stdout + passed.stderr)
        self.assert_selected(passed, active)
        self.assertIn("C1: File List exactly matches all 1 in-scope changed paths.", passed.stdout)

        failed = self.validate("--story-key", KEY, "--changed-file", "docs/earlier.md")
        self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
        self.assert_selected(failed, active)
        self.assertIn("C1: 'docs/earlier.md' changed but is not in the File List.", failed.stdout)

    def test_ambiguous_revision_fails_closed_and_an_explicit_spec_key_pins_the_artifact(self):
        pinned = self.write_follow_up(2, "docs/pinned.md")
        alias = self.write_follow_up("02", "docs/alias.md")

        ambiguous = self.validate("--story-key", KEY)
        self.assertEqual(1, ambiguous.returncode, ambiguous.stdout + ambiguous.stderr)
        self.assertIn("Ambiguous active numbered specs", ambiguous.stdout)
        self.assertIn(pinned, ambiguous.stdout)
        self.assertIn(alias, ambiguous.stdout)

        exact = self.validate("--story-key", Path(pinned).stem, "--changed-file", "docs/pinned.md")
        self.assertEqual(0, exact.returncode, exact.stdout + exact.stderr)
        self.assert_selected(exact, pinned)
        self.assertIn("Story review readiness validation passed.", exact.stdout)

    def test_original_remains_the_fallback_without_an_active_follow_up(self):
        self.write_follow_up(2, "docs/historical.md", status="done")
        result = self.validate("--story-key", KEY)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assert_selected(result, ORIGINAL)

    def test_branch_and_index_invocations_keep_exact_artifact_precedence(self):
        self.command("git", "init", "--quiet", check=True)
        self.command("git", "add", "--", str(self.artifacts), check=True)
        self.write_follow_up(2, "docs/worktree.md")

        for args in (("--branch-name", KEY), ("--story-key", KEY, "--staged-story")):
            with self.subTest(args=args):
                result = self.validate(*args)
                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assert_selected(result, ORIGINAL)

    def test_numbered_spec_retains_its_own_checkpoint_history(self):
        self.write_artifact(ORIGINAL, CHECKPOINTS + "| Obsolete checkpoint | pending |\n")
        active = self.write_follow_up(
            2, "docs/current.md", evidence=CHECKPOINTS + "| Current checkpoint | pending |\n",
        )
        self.command("git", "init", "--quiet", check=True)
        self.command("git", "add", "--", str(self.artifacts), check=True)
        self.command(
            "git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.test",
            "commit", "-qm", "test: Record prior checkpoints", check=True,
        )
        self.write_follow_up(2, "docs/current.md")

        result = self.validate("--story-key", KEY, "--changed-file", "docs/current.md")
        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assert_selected(result, active)
        self.assertIn("C6: evidence row 'current checkpoint' was removed", result.stdout)
        self.assertNotIn("obsolete checkpoint", result.stdout)

    def test_review_statuses_use_the_same_registered_story_gates(self):
        self.write_artifact("sprint-status.yaml", f"development_status:\n  {KEY}: review\n")
        for status in ("review", "in-review"):
            with self.subTest(status=status):
                active = self.write_follow_up(
                    2, "docs/current.md", status=status, evidence=REVIEW_LEDGER + VERIFIED_CHECKPOINT,
                )
                result = self.validate("--story-key", KEY, "--changed-file", "docs/current.md")

                self.assertEqual(0, result.returncode, result.stdout + result.stderr)
                self.assert_selected(result, active)
                self.assertIn(f"Declared status: {status}", result.stdout)
                self.assertIn("C1: File List exactly matches all 1 in-scope changed paths.", result.stdout)
                self.assertIn("C6: 1 evidence table(s) have valid evidence states", result.stdout)
                self.assertIn("Story review readiness validation passed.", result.stdout)

    def test_in_review_reconciles_standalone_spec_file_list_with_supplied_diff(self):
        name = self.write_follow_up(
            2, "docs/current.md", status="in-review", evidence=REVIEW_LEDGER + VERIFIED_CHECKPOINT,
        )
        changed = self.repo / "changed-files.txt"
        changed.write_text("docs/current.md\n", encoding="utf-8")
        args = ("--story-key", Path(name).stem, "--changed-files-file", str(changed))

        passed = self.validate(*args)
        self.assertEqual(0, passed.returncode, passed.stdout + passed.stderr)
        self.assertIn("C1: File List exactly matches all 1 in-scope changed paths.", passed.stdout)

        changed.write_text("docs/other.md\n", encoding="utf-8")
        failed = self.validate(*args)
        self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
        self.assertIn("C1: 'docs/other.md' changed but is not in the File List.", failed.stdout)
        self.assertIn("C1: 'docs/current.md' is in the File List but not in the in-scope changed set.", failed.stdout)
        self.assertNotIn("C3:", failed.stdout)

        changed.write_text("", encoding="utf-8")
        empty = self.validate(*args)
        self.assertEqual(1, empty.returncode, empty.stdout + empty.stderr)
        self.assertIn("C1: the changed set is empty for a governed story.", empty.stdout)

    @unittest.skipIf(os.name == "nt", "Uses POSIX filenames with literal backslashes and newlines")
    def test_file_list_matches_literal_git_paths_and_conventional_paths(self):
        arguments = self.stage_changed_paths(
            "docs/with\\slash.md", "docs/new\nline café`.md", "docs/bare.md", "docs/ordinary.md",
            "docs/prefixed.md",
        )
        rows = "\n".join((
            r'* Git path: `"docs/with\\slash.md"`',
            r'* Git path: `"docs/new\nline caf\u00e9\u0060.md"`',
            '- docs/bare.md — documentation',
            r'- `.\docs\ordinary.md` — documentation',
            '- Git path: `docs/prefixed.md` — documentation',
        ))
        self.write_file_list(rows)

        passed = self.validate(*arguments)
        self.assertEqual(0, passed.returncode, passed.stdout + passed.stderr)
        self.assertIn("C1: File List exactly matches all 5 in-scope changed paths.", passed.stdout)

        for alias_rows in (
            rows.replace(r'with\\slash.md', 'with/slash.md'),
            rows.replace(r'new\nline', r'new\\nline'),
        ):
            with self.subTest(rows=alias_rows):
                self.write_file_list(alias_rows)
                failed = self.validate(*arguments)
                self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
                self.assertIn("changed but is not in the File List.", failed.stdout)
                self.assertIn("is in the File List but not in the in-scope changed set.", failed.stdout)

    @unittest.skipIf(os.name == "nt", "Uses a POSIX filename with a literal backslash")
    def test_file_list_exclusion_matches_only_its_literal_git_path(self):
        arguments = self.stage_changed_paths("docs/with\\slash.md", "docs/with/slash.md", "docs/other.md")
        rows = "- `docs/with/slash.md`"
        literal = r'* Git path: `"docs/with\\slash.md"`'
        metadata = " — owner: Another Story; concurrent work"
        conventional = "\n- `docs/other.md`" + metadata
        self.write_file_list(rows, literal + metadata + conventional)
        passed = self.validate(*arguments)
        self.assertEqual(0, passed.returncode, passed.stdout + passed.stderr)
        self.assertIn("C1: File List exactly matches all 1 in-scope changed paths.", passed.stdout)

        self.write_file_list(rows, literal.replace(r'with\\slash.md', 'with/slash.md') + metadata + conventional)
        failed = self.validate(*arguments)
        self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
        self.assertIn(r"C1: 'docs/with\\slash.md' changed but is not in the File List.", failed.stdout)

    def test_invalid_literal_git_paths_fail_in_either_section(self):
        arguments = self.stage_changed_paths("docs/ordinary.md")
        malformed = (
            '* Git path:',
            '* Git path: "docs/ordinary.md"',
            '* Git path: `"docs/ordinary.md',
            '* Git path: `["docs/ordinary.md"]`',
            r'* Git path: `"docs/\q.md"`',
            r'* Git path: `"docs/\ud800.md"`',
            r'* Git path: `"docs/\u0000.md"`',
            '* Git path: `""`',
            '* Git path: `"/docs/ordinary.md"`',
            '* Git path: `"./docs/ordinary.md"`',
            '* Git path: `"docs//ordinary.md"`',
            '* Git path: `"docs/sub/../ordinary.md"`',
            r'* Git path: `"docs\/ordinary.md"`',
            r'* Git path: `"docs/\u006frdinary.md"`',
            '* Git path: `"docs/ordinary.md"`extra',
        )
        ordinary = "- `docs/ordinary.md`"
        for section in ("File List", "File List Exclusions"):
            for row in malformed:
                with self.subTest(section=section, row=row):
                    if section == "File List":
                        self.write_file_list(ordinary + "\n" + row)
                    else:
                        self.write_file_list(ordinary, row + " — owner: Another Story; concurrent work")
                    failed = self.validate(*arguments)
                    self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
                    self.assertIn(f"C1: invalid literal Git path in {section}:", failed.stdout)

        self.write_file_list(ordinary + '\n* Git path: `"docs/ordinary.md"` extra')
        failed = self.validate(*arguments)
        self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
        self.assertIn("C1: invalid literal Git path in File List:", failed.stdout)

    def test_literal_exclusion_requires_owner_and_reason_after_the_path(self):
        arguments = self.stage_changed_paths("docs/ordinary.md")
        row = '* Git path: `"docs/owner: Misleading; reason.md"`'
        for metadata, missing in (("", "names no owner"), (" — owner: Another Story", "gives no reason")):
            with self.subTest(metadata=metadata):
                self.write_file_list("- `docs/ordinary.md`", row + metadata)
                failed = self.validate(*arguments)
                self.assertEqual(1, failed.returncode, failed.stdout + failed.stderr)
                self.assertIn(f"File List Exclusions entry {missing}", failed.stdout)

    def test_in_review_rejects_missing_file_list_and_unfinished_evidence(self):
        self.write_artifact("sprint-status.yaml", f"development_status:\n  {KEY}: review\n")
        self.write_artifact(
            ORIGINAL,
            "---\nstatus: 'in-review'\n---\n" + CREATE_STORY_LEDGER
            + CHECKPOINTS + "| Verification | pending |\n",
        )

        result = self.validate("--story-key", KEY, "--changed-file", "docs/current.md")

        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("C1: a non-empty File List is required", result.stdout)
        self.assertIn("C2: status is 'in-review' but no dev-story ledger row exists.", result.stdout)
        self.assertIn(
            "C6: evidence row 'Verification' has an unresolved or unrecognized review state 'pending'", result.stdout,
        )
        self.assertNotIn("C3:", result.stdout)
        self.assertNotIn("C4:", result.stdout)

    def test_in_review_rejects_missing_ledger_and_sprint_mismatch(self):
        self.write_follow_up(2, "docs/current.md", status="in-review")

        result = self.validate("--story-key", KEY, "--changed-file", "docs/current.md")

        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("C2: a phase ledger is required for this governed 'in-review' artifact.", result.stdout)
        self.assertIn("C4: story declares status 'in-review' but sprint-status records 'in-progress'.", result.stdout)

    def test_status_only_numbered_spec_requires_file_list_and_ledger_in_bmad_review(self):
        name = f"spec-{KEY}-2.md"
        self.write_artifact("sprint-status.yaml", f"development_status:\n  {KEY}: review\n")
        args = ("--story-key", KEY, "--changed-file", "docs/current.md")

        self.write_artifact(name, "---\nstatus: 'review'\n---\n")
        legacy = self.validate(*args)
        self.assert_selected(legacy, name)
        self.assertEqual(0, legacy.returncode, legacy.stdout + legacy.stderr)
        self.assertIn("check is a no-op.", legacy.stdout)

        self.write_artifact(name, "---\nstatus: 'in-review'\n---\n")
        result = self.validate(*args)
        self.assert_selected(result, name)
        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("C1: a non-empty File List is required", result.stdout)
        self.assertIn("C2: a phase ledger is required for this governed 'in-review' artifact.", result.stdout)

    def test_review_status_near_match_remains_invalid(self):
        name = self.write_follow_up(2, "docs/current.md", status="in-reveiw", evidence=REVIEW_LEDGER)

        result = self.validate("--story-key", Path(name).stem, "--changed-file", "docs/current.md")

        self.assertEqual(1, result.returncode, result.stdout + result.stderr)
        self.assertIn("C3: status 'in-reveiw' is not one of", result.stdout)
