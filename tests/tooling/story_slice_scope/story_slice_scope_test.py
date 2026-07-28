"""Contract tests for tools/check-story-slice-scope.py.

The failing fixtures are reconstructions of two shapes this repository actually
produced and recorded, not invented examples. Both are described verbatim in
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-27-profile-hash-deployment-ac-and-epic-splits.md`
section 1.3 and in the `epics.md` split notes at Stories 30.1 and 31.1. The
original files no longer exist -- the 2026-07-27 split replaced them -- so their
shape is rebuilt inline here from the recorded description.

The passing fixtures pin the false positives found while calibrating the gate
against live stories, so a future parser change cannot silently reintroduce them.
"""

import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-story-slice-scope.py"
STORY_DIR = "_bmad-output/implementation-artifacts"

# Live stories that satisfied the policy at the time the gate was calibrated
# (2026-07-28). They are the gate's regression baseline: a parser change that
# starts failing any of them is a false positive, not a new finding.
CALIBRATION_STORIES = (
    "27-1-access-telemetry-retention-ownership-decision",
    "27-2-bounded-retention-ttl-and-purge-implementation",
    "27-3-production-adapter-and-deployment-profile",
    "29-1-openbao-backed-apphost-secret-topology",
    "31-1-openbao-platform-hardening-and-documentation",
    "31-2-runtime-dapr-secret-store-migration",
)

# Reconstruction of the pre-split Story 31.1, recorded as bundling "OpenBao
# platform hardening and the runtime `secretstore` migration -- two
# independently deployable outcomes -- with no checkpoint table at all".
PRE_SPLIT_31_1 = """\
# Story 31.1: OpenBao Platform Hardening and Runtime Secret Store Migration

Status: backlog

Builds on Story 27.3 and Story 29.1.

## Acceptance Criteria

1. **Then** the OpenBao platform is hardened. Checkpoint C1.
2. **Then** the seal configuration is recorded. Checkpoint C2.
3. **Then** the runtime `secretstore` component is migrated. Checkpoint C3.
4. **Then** the bootstrap Secret is provisioned. Checkpoint C4.
5. **Then** the RBAC reader roles are applied. Checkpoint C5.
6. **Then** the operations document is republished. Checkpoint C6.
7. **Then** the deployed profile is verified. Checkpoint C7.
8. **Then** rollback is proven. Checkpoint C8.
"""

# Reconstruction of the pre-split Story 30.1, recorded as naming "eight
# 'separate reviewable checkpoints' with no owner, evidence command, review
# state or completion state".
PRE_SPLIT_30_1 = """\
# Story 30.1: Guarded Release Dispatch, Manifest Migration and Publication

Status: backlog

Supersedes part of Story 27.3.

## Checkpoints

The following are separate reviewable checkpoints:

| Checkpoint | Description |
| :--------- | :---------- |
| C1 | Guarded dispatch hardening |
| C2 | Manifest migration |
| C3 | Four-image publication |
| C4 | Partial-release recovery |
| C5 | Cutover parity |
| C6 | Rollback |
| C7 | Registry authorization |
| C8 | Shared caller adoption |
"""

COMPLIANT_STORY = """\
# Story 40.1: A Narrow Slice

Status: backlog

Reuses a pattern first written for Story 26.1.

### Historical Context Classification

| Source | Classification | Permitted use |
| :----- | :------------- | :------------ |
| Story 26.1 | current-narrow-pattern | Assertion mechanism only, re-verified against current source |
| Story 27.3 whole checkpoint shape | anti-template | Its gate table must not be copied |

### Slice Proof

One independently demonstrable outcome: the documentation guard passes.
"""


def run_gate(*args):
    return subprocess.run(
        [sys.executable, str(TOOL), *args],
        cwd=REPO_ROOT,
        text=True,
        capture_output=True,
        check=False,
    )


class StorySliceScopeGateTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)
        (self.root / STORY_DIR).mkdir(parents=True)
        self.addCleanup(self._tmp.cleanup)

    def write_story(self, key: str, body: str) -> str:
        path = f"{STORY_DIR}/{key}.md"
        (self.root / path).write_text(body, encoding="utf-8")
        return path

    def check(self, paths, *, message: str = "", extra=()):
        changed = self.root / "changed.txt"
        changed.write_text("\n".join(paths) + "\n", encoding="utf-8")
        args = [
            "--repo-root",
            str(self.root),
            "--changed-files-file",
            str(changed),
        ]
        if message:
            message_file = self.root / "message.txt"
            message_file.write_text(message, encoding="utf-8")
            args += ["--commit-message-file", str(message_file)]
        return run_gate(*args, *extra)

    # --- recorded failing shapes -------------------------------------------------

    def test_pre_split_31_1_shape_is_rejected(self):
        path = self.write_story("31-1-bundled", PRE_SPLIT_31_1)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("no checkpoint table", result.stdout)

    def test_pre_split_30_1_shape_is_rejected(self):
        path = self.write_story("30-1-umbrella", PRE_SPLIT_30_1)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)
        # Eight gates in a two-column table that promises no owner, evidence,
        # review or completion state is not a checkpoint evidence table.
        self.assertIn("no checkpoint table", result.stdout)

    def test_unclassified_row_is_rejected(self):
        body = COMPLIANT_STORY.replace(
            "| Story 26.1 | current-narrow-pattern |", "| Story 26.1 | reusable |"
        )
        path = self.write_story("40-1-unclassified", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("carries none of", result.stdout)

    def test_bare_anti_template_label_is_rejected(self):
        body = COMPLIANT_STORY.replace(
            "| Story 27.3 whole checkpoint shape | anti-template | Its gate table must not be copied |",
            "| Story 27.3 whole checkpoint shape | anti-template |  |",
        )
        path = self.write_story("40-1-bare", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("states no permitted use", result.stdout)

    def test_registered_story_without_record_is_rejected(self):
        path = self.write_story(
            "40-2-norecord",
            "# Story 40.2\n\nStatus: backlog\n\nFollows Story 26.1.\n",
        )
        result = self.check([path, "_bmad-output/planning-artifacts/epics.md"])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Historical Context Classification", result.stdout)
        self.assertIn("Slice Proof", result.stdout)

    def test_half_populated_checkpoint_row_is_rejected(self):
        body = COMPLIANT_STORY + textwrap.dedent(
            """
            ### Checkpoints

            | Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state |
            | :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- |
            | C1 | Maintainer | `pytest -k c1` | reviewed | complete |
            | C2 | Maintainer |  |  | complete |
            """
        )
        path = self.write_story("40-3-halfrow", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Checkpoint C2 row", result.stdout)

    # --- calibrated false positives ----------------------------------------------

    def test_qualified_classification_is_accepted(self):
        """Story 31.1's real row carries two labels and is correct.

        `current-narrow-pattern (whole-story shape is anti-template)` states that
        a mechanism is reused while the surrounding story shape is not. Requiring
        exactly one label would reject the most careful row in the repository.
        """
        body = COMPLIANT_STORY.replace(
            "| Story 26.1 | current-narrow-pattern | Assertion mechanism only, re-verified against current source |",
            "| Story 26.1 | current-narrow-pattern (whole-story shape is `anti-template`) | Assertion mechanism only |",
        )
        path = self.write_story("40-4-qualified", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_decision_table_starting_with_a_gate_id_is_not_a_checkpoint_table(self):
        """Story 31.1 carries `| Decision | Resolution |` with a `C7 unsigned` row."""
        body = COMPLIANT_STORY + textwrap.dedent(
            """
            #### Review resolutions

            | Decision | Resolution |
            | :------- | :--------- |
            | C7 unsigned | Closed by an approved time-bounded waiver |
            """
        )
        path = self.write_story("40-5-decision", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_discovery_table_starting_with_a_gate_id_is_not_a_checkpoint_table(self):
        """Story 27.3 carries `| Checkpoint | Exact Python discovery command | ... |`."""
        body = COMPLIANT_STORY + textwrap.dedent(
            """
            #### Discovery

            | Checkpoint | Exact Python discovery command | Required case inventory |
            | :--------- | :----------------------------- | :---------------------- |
            | C1 | `python3 -m unittest discover` | `SomeTests.test_one` |
            """
        )
        path = self.write_story("40-6-discovery", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_transferred_gate_mentions_do_not_demand_rows(self):
        """Story 27.3 names C1.1-C1.14 because they moved to Story 27.5."""
        body = COMPLIANT_STORY + textwrap.dedent(
            """
            #### Transfers

            The gates C1.1, C1.2, C1.3, C1.4, C1.5, C1.6, C1.7, C1.8 transfer to
            Story 27.5, keeping their identifiers.

            | Checkpoint | Accountable owner | Required evidence artifact and command | Review state | Completion state |
            | :--------- | :---------------- | :------------------------------------- | :----------- | :--------------- |
            | C2 | Maintainer | `pytest -k c2` | reviewed | complete |
            """
        )
        path = self.write_story("40-7-transferred", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 0, result.stdout)

    # --- binding point -----------------------------------------------------------

    def test_legacy_edit_without_registration_is_not_asked_for_a_record(self):
        path = self.write_story(
            "12-4-legacy",
            "# Story 12.4\n\nStatus: done\n\nRelated to Story 11.2.\n",
        )
        result = self.check([path])
        self.assertEqual(result.returncode, 0, result.stdout)

    def test_partial_record_is_completed_even_without_registration(self):
        """A story that started the record must finish it."""
        path = self.write_story(
            "12-5-partial",
            "# Story 12.5\n\nFollows Story 11.2.\n\n### Slice Proof\n\nOne outcome.\n",
        )
        result = self.check([path])
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Historical Context Classification", result.stdout)

    # --- structural safety -------------------------------------------------------

    def test_record_inside_a_code_fence_does_not_satisfy_the_gate(self):
        body = textwrap.dedent(
            """\
            # Story 41.1

            Follows Story 26.1.

            ```markdown
            ### Historical Context Classification

            | Source | Classification | Permitted use |
            | :----- | :------------- | :------------ |
            | Story 26.1 | current-narrow-pattern | example only |

            ### Slice Proof

            example
            ```
            """
        )
        path = self.write_story("41-1-fenced", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)

    def test_record_inside_an_html_comment_does_not_satisfy_the_gate(self):
        body = (
            "# Story 41.2\n\nFollows Story 26.1.\n\n"
            "<!--\n### Historical Context Classification\n\n### Slice Proof\n-->\n"
        )
        path = self.write_story("41-2-commented", body)
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 1, result.stdout)

    def test_self_reference_alone_does_not_trigger_the_record(self):
        path = self.write_story(
            "42-1-selfref",
            "# Story 42.1\n\nStatus: backlog\n\nStory 42.1 delivers one outcome.\n",
        )
        result = self.check([path, f"{STORY_DIR}/sprint-status.yaml"])
        self.assertEqual(result.returncode, 0, result.stdout)

    # --- bypass and no-op --------------------------------------------------------

    def test_trailer_bypass_requires_a_reason(self):
        path = self.write_story("31-1-bundled", PRE_SPLIT_31_1)
        with_reason = self.check(
            [path, f"{STORY_DIR}/sprint-status.yaml"],
            message="fix: something\n\nStory-Slice-Scope: not-applicable - no scope authored\n",
        )
        self.assertEqual(with_reason.returncode, 0, with_reason.stdout)

        without_reason = self.check(
            [path, f"{STORY_DIR}/sprint-status.yaml"],
            message="fix: something\n\nStory-Slice-Scope: not-applicable\n",
        )
        self.assertEqual(without_reason.returncode, 1, without_reason.stdout)

    def test_bare_invocation_reports_a_no_op_rather_than_a_silent_pass(self):
        result = run_gate("--repo-root", str(self.root))
        self.assertEqual(result.returncode, 0)
        self.assertIn("no-op, not a pass", result.stdout)

    def test_missing_changed_files_input_fails_rather_than_passing(self):
        result = run_gate(
            "--repo-root", str(self.root), "--changed-files-file", "does-not-exist.txt"
        )
        self.assertEqual(result.returncode, 1)

    def test_unrelated_change_is_a_clean_no_op(self):
        result = self.check(["src/Some/File.cs", "README.md"])
        self.assertEqual(result.returncode, 0, result.stdout)
        self.assertIn("no governed story file changed", result.stdout)

    # --- live calibration lock ---------------------------------------------------

    def test_live_reference_stories_still_pass(self):
        for key in CALIBRATION_STORIES:
            story = REPO_ROOT / STORY_DIR / f"{key}.md"
            if not story.is_file():
                self.skipTest(f"{key} not present")
            with self.subTest(story=key):
                result = run_gate("--story-key", key, "--require-record")
                self.assertEqual(result.returncode, 0, result.stdout)


if __name__ == "__main__":
    unittest.main()
