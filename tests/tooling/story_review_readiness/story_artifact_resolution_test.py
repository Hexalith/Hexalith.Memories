"""Exercise numbered follow-up resolution through the readiness CLI."""

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

    def command(self, *args, check=False):
        return subprocess.run(
            args, cwd=self.repo, env=self.environment, check=check,
            capture_output=True, text=True, timeout=20,
        )

    def validate(self, *args):
        return self.command(sys.executable, str(VALIDATOR), *args)

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
