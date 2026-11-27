"""Exercise checkpoint retention and transfers through the story readiness CLI."""

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[3]
VALIDATOR = ROOT / "tools/check-story-review-readiness.py"
STORY = "spec-disposition"
TABLE = (
    "| Checkpoint | Review state | Completion date | Disposition |\n"
    "| ---------- | ------------ | --------------- | ----------- |\n"
)
LEDGER = (
    "| Date | Phase | Change | Test count | File List reconciliation |\n"
    "| ---- | ----- | ------ | ---------- | ------------------------ |\n"
    "| 2026-11-26 | create-story | created | none required | matched 1/1 |\n"
    "| 2026-11-27 | dev-story | documented | none required | matched 1/1 |\n"
    "| 2026-11-27 | code-review | verified | none required | matched 1/1 |\n"
)
TRANSFER = (
    "Decision: approved proposal; Destination: successor story; Owner: release owner; "
    "Consequence: proof remains open; Reopen trigger: successor scope changes"
)


class StoryEvidenceTransferTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory(prefix="story-evidence-")
        self.addCleanup(directory.cleanup)
        self.repo = Path(directory.name)
        self.environment = {
            key: value for key, value in os.environ.items() if not key.startswith(("GIT_", "GITHUB_"))
        }
        self.environment.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        artifacts = self.repo / "_bmad-output/implementation-artifacts"
        artifacts.mkdir(parents=True)
        self.story = artifacts / f"{STORY}.md"
        self.story.with_name("sprint-status.yaml").write_text("development_status: {}\n", encoding="utf-8")
        self.command("git", "init", "--quiet", "--initial-branch=main", check=True)
        self.write_story(TABLE + "| C1 | pending | — | Pending proof |\n", "ready-for-dev")
        self.commit_story("Record the initial checkpoint")

    def commit_story(self, subject):
        self.command("git", "add", "--", str(self.story.parent), check=True)
        self.command(
            "git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.test",
            "commit", "-qm", f"test: {subject}", check=True,
        )
        return self.command("git", "rev-parse", "HEAD", check=True).stdout.strip()

    def use_pull_request_base(self, base):
        event = self.repo / "event.json"
        event.write_text(json.dumps({"pull_request": {"base": {"sha": base}}}), encoding="utf-8")
        self.environment.update(
            GITHUB_ACTIONS="true", GITHUB_EVENT_NAME="pull_request", GITHUB_EVENT_PATH=str(event),
        )

    def command(self, *arguments, check=False):
        return subprocess.run(
            arguments, cwd=self.repo, env=self.environment, check=check,
            capture_output=True, text=True, timeout=20,
        )

    def write_story(self, body, status="done"):
        self.story.write_text(
            f"Status: {status}\n\n" + LEDGER + "\n" + body, encoding="utf-8",
        )

    def validate(self, *arguments):
        return self.command(sys.executable, str(VALIDATOR), "--story-key", STORY, *arguments)

    def test_story_27_3_requires_the_independent_c0_receipt(self):
        key = "27-3-production-adapter-and-deployment-profile"
        actual = (ROOT / f"_bmad-output/implementation-artifacts/{key}.md").read_text(encoding="utf-8")
        checkpoint = actual.split("### Current Binding Checkpoints", 1)[1].splitlines()[:7]
        receipt = actual.split("### C0 Independent Review Receipt", 1)[1].split("### Code Review Evidence", 1)[0]
        self.story = self.story.with_name(f"{key}.md")
        file_list = f"### File List\n\n- `{self.story.relative_to(self.repo).as_posix()}`\n"
        checkpoint = "### Current Binding Checkpoints" + "\n".join(checkpoint) + "\n\n"
        receipt = "### C0 Independent Review Receipt" + receipt
        for status in ("review", "done"):
            self.story.with_name("sprint-status.yaml").write_text(
                f"development_status:\n  {key}: {status}\n", encoding="utf-8",
            )
            for label, changed_checkpoint, changed_receipt in (
                ("valid", checkpoint, receipt),
                ("missing handoff", "", receipt),
                ("historical handoff", checkpoint.replace("Current Binding", "Historical"), receipt),
                ("blocked handoff", checkpoint.replace("reviewed 2026-08-01", "blocked 2026-08-01", 1), receipt),
                ("incomplete handoff", checkpoint.replace("| complete |", "| pending |", 1), receipt),
                ("reopened handoff", checkpoint.replace("| complete |", "| complete; reopened |", 1), receipt),
                ("implementer reviewed", checkpoint.replace("independent reviewer accepted", "implementer accepted"), receipt),
                ("missing receipt", checkpoint, ""),
                ("duplicate receipt", checkpoint, receipt * 2),
                ("fenced receipt", checkpoint, f"```text\n{receipt}```\n"),
                ("commented receipt", checkpoint, f"<!--\n{receipt}-->\n"),
                ("indented receipt", checkpoint, "\n".join("    " + line for line in receipt.splitlines())),
                ("command failed", checkpoint, receipt.replace("| 0 |", "| 1 |", 1)),
                ("execution skipped", checkpoint, receipt.replace("Skipped: 0", "Skipped: 1")),
                ("different assembly", checkpoint, receipt.replace("net10.0", "net11.0", 1)),
                ("wrong command", checkpoint, receipt.replace("dotnet clean", "dotnet build", 1)),
                ("wrong build output", checkpoint, receipt.replace("--verbosity minimal", "-o /elsewhere --verbosity minimal", 1)),
                ("wrong framework", checkpoint, receipt.replace("net10.0", "net99.0")),
            ):
                with self.subTest(status=status, receipt=label):
                    self.write_story(file_list + changed_checkpoint + changed_receipt, status)
                    result = self.command(
                        sys.executable, str(VALIDATOR), "--story-key", key,
                        "--changed-file", str(self.story.relative_to(self.repo)),
                    )
                    self.assertEqual(0 if label == "valid" else 1, result.returncode, result.stdout)
                    if label != "valid":
                        self.assertIn("C6/C0:", result.stdout)

    def test_deleting_the_only_table_is_rejected_at_every_status(self):
        for status in ("ready-for-dev", "done"):
            with self.subTest(status=status):
                self.story.write_text(f"Status: {status}\n", encoding="utf-8")
                result = self.validate()
                self.assertEqual(1, result.returncode, result.stdout)
                self.assertIn("evidence row 'c1' was removed", result.stdout)

    def test_renaming_a_checkpoint_does_not_hide_its_removal(self):
        self.write_story(TABLE + "| C2 | complete | 2026-11-27 | Report |\n")
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c1' was removed", result.stdout)

    def test_completed_checkpoint_cannot_claim_an_administrative_transfer(self):
        self.write_story(TABLE + "| C1 | complete — administrative scope transfer | 2026-11-27 | Report |\n")
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("Use the 'transferred' review state", result.stdout)

    def test_completed_and_transferred_checkpoints_can_pass(self):
        for state, disposition in (("complete", "Executed report"), ("transferred", TRANSFER)):
            with self.subTest(state=state):
                self.write_story(TABLE + f"| C1 | {state} | 2026-11-27 | {disposition} |\n")
                result = self.validate()
                self.assertEqual(0, result.returncode, result.stdout)

    def test_transfer_requires_a_real_date_and_all_accountability_fields(self):
        self.write_story(TABLE + "| C1 | transferred | 2026-02-30 | Destination: TBD; Owner: release owner |\n")
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("consequence, reopen trigger, decision, destination", result.stdout)
        self.assertIn("needs a valid completion date", result.stdout)

    def test_complete_with_structured_transfer_disposition_is_rejected(self):
        self.write_story(TABLE + f"| C1 | complete | 2026-11-27 | {TRANSFER} |\n")
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("Use the 'transferred' review state", result.stdout)

    def test_readiness_uses_the_index_when_the_worktree_has_more_edits(self):
        self.write_story("")
        self.command("git", "add", "--", str(self.story), check=True)
        self.write_story(TABLE + f"| C1 | transferred | 2026-11-27 | {TRANSFER} |\n")
        result = self.validate("--staged-story")
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c1' was removed", result.stdout)
        self.assertEqual(0, self.validate().returncode)

    def test_pending_proposals_can_be_recorded_before_review(self):
        self.write_story(TABLE + "| C1 | pending — scope transfer proposed | — | Awaiting decision |\n", "ready-for-dev")
        self.assertEqual(0, self.validate().returncode)

    def test_pending_or_denied_approval_cannot_satisfy_a_transfer(self):
        cases = (("transferred pending approval", TRANSFER),
                 ("transferred", TRANSFER.replace("approved proposal", "proposal denied")))
        for state, disposition in cases:
            with self.subTest(state=state):
                self.write_story(TABLE + f"| C1 | {state} | 2026-11-27 | {disposition} |\n")
                result = self.validate()
                self.assertEqual(1, result.returncode, result.stdout)

    def test_prose_in_disposition_also_identifies_a_transfer(self):
        self.write_story(TABLE + "| C1 | complete | 2026-11-27 | Administrative scope transfer to Story 27.5 |\n")
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("Use the 'transferred' review state", result.stdout)

    def test_checkpoint_identity_uses_section_and_checkpoint_column(self):
        header = "| Owner | Checkpoint | Review state | Completion date |\n| --- | --- | --- | --- |\n"
        originally = "## First\n" + header + "| team | C1 | pending | — |\n"
        self.write_story(TABLE + "| C1 | pending | — | — |\n\n" + originally, "ready-for-dev")
        self.commit_story("Record a second checkpoint with the same label")

        moved = "## Second\n" + header + "| team | C1 | complete | 2026-11-27 |\n"
        replaced = "## First\n" + header + "| team | C2 | complete | 2026-11-27 |\n"
        self.write_story(TABLE + "| C1 | complete | 2026-11-27 | Report |\n\n" + replaced + "\n" + moved)
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c1' was removed from 'first'", result.stdout)

    def test_manual_and_hosted_checks_compare_against_prior_committed_story(self):
        base = self.command("git", "rev-parse", "HEAD", check=True).stdout.strip()
        self.write_story("")
        head = self.commit_story("Remove the checkpoint")

        result = self.validate("--commit-sha", head)
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c1' was removed", result.stdout)

        self.use_pull_request_base(base)
        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c1' was removed", result.stdout)

    def test_hosted_check_retains_intermediate_work_and_ignores_target_only_checkpoints(self):
        self.command("git", "branch", "feature", check=True)
        self.write_story(TABLE + "| C1 | complete | 2026-11-27 | Report |\n| C2 | pending | — | — |\n")
        target = self.commit_story("Add a checkpoint on the target branch")
        self.command("git", "switch", "feature", check=True)
        self.write_story(TABLE + "| C1 | complete | 2026-11-27 | Report |\n| C3 | pending | — | — |\n")
        self.commit_story("Add the feature checkpoint")
        self.write_story(TABLE + "| C1 | complete | 2026-11-27 | Report |\n")
        self.commit_story("Remove the feature checkpoint")
        self.use_pull_request_base(target)

        result = self.validate()
        self.assertEqual(1, result.returncode, result.stdout)
        self.assertIn("evidence row 'c3' was removed", result.stdout)
        self.assertNotIn("evidence row 'c2' was removed", result.stdout)
