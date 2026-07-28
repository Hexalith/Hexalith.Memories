"""Fixtures for tools/check-story-review-readiness.py.

Subprocess-driven CLI cases plus white-box parser cases. The C6 and C2 fixtures
are built from the real shapes observed in live artifacts rather than invented,
so parser drift is caught rather than masked: a synthetic-only suite passes
forever while the live format moves away from it.
"""

from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
VERIFIER = REPO_ROOT / "tools" / "check-story-review-readiness.py"


def load_module():
    spec = importlib.util.spec_from_file_location("check_story_review_readiness", VERIFIER)
    module = importlib.util.module_from_spec(spec)
    # Frozen dataclasses resolve themselves by module name during exec_module.
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


V = load_module()


LEDGER_HEADER = (
    "| Date | Phase | Change | Test count | File List reconciliation |\n"
    "| :--- | :---- | :----- | :--------- | :----------------------- |\n"
)


CREATE_ROW = "| 2026-07-01 | create-story | baseline | +0 methods | matched 1/1 vs `abc1234` |\n"
DEV_ROW = "| 2026-07-02 | dev-story | built it | +3 methods | matched 1/1 vs `abc1234` |\n"
REVIEW_ROW = "| 2026-07-03 | code-review | reviewed | +0 methods | matched 1/1 vs `abc1234` |\n"


def default_ledger(status: str) -> str:
    """C2 requires a dev-story row at review/done and a code-review row at done.

    The fixture ledger therefore has to track the status it is paired with, or
    every done-status case fails on C2 instead of the check under test.
    """
    rows = CREATE_ROW
    if status in ("review", "done"):
        rows += DEV_ROW
    if status == "done":
        rows += REVIEW_ROW
    return rows


def story(
    status: str = "in-progress",
    ledger_rows: str | None = None,
    file_list: list[str] | None = None,
    evidence: str = "",
    exclusions: str = "",
    frontmatter: str = "---\nbaseline_commit: abc1234\n---\n",
) -> str:
    ledger_rows = default_ledger(status) if ledger_rows is None else ledger_rows
    listed = "".join(f"- `{path}`\n" for path in (file_list or ["docs/a.md"]))
    return (
        f"{frontmatter}\n# Story 99.9: Fixture\n\nStatus: {status}\n\n"
        f"## Change Log\n\n{LEDGER_HEADER}{ledger_rows}\n"
        f"{evidence}"
        f"### File List\n\n{listed}\n{exclusions}"
    )


class VerifierTestCase(unittest.TestCase):
    """Writes a story artifact into a throwaway artifacts root and runs the CLI."""

    def run_cli(self, *args: str, artifacts: dict[str, str] | None = None, sprint: str | None = None):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "artifacts"
            root.mkdir()
            for name, body in (artifacts or {}).items():
                (root / name).write_text(body, encoding="utf-8")
            sprint_path = Path(tmp) / "sprint-status.yaml"
            sprint_path.write_text(
                sprint if sprint is not None else "development_status:\n  99-9-fixture: in-progress\n",
                encoding="utf-8",
            )
            completed = subprocess.run(
                [
                    sys.executable,
                    str(VERIFIER),
                    "--artifacts-root",
                    str(root),
                    "--sprint-status-file",
                    str(sprint_path),
                    *args,
                ],
                cwd=REPO_ROOT,
                text=True,
                capture_output=True,
                check=False,
            )
            return completed.returncode, completed.stdout


class NoOpTests(VerifierTestCase):
    def test_no_story_key_is_a_noop(self):
        # `correct-course` commits carry no Story: trailer by convention.
        with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False) as handle:
            handle.write("chore: unrelated change\n")
            message = handle.name
        code, out = self.run_cli("--commit-message-file", message)
        self.assertEqual(code, 0)
        self.assertIn("no-op", out)

    def test_artifact_without_any_governed_section_is_a_noop(self):
        body = "---\n---\n\n# Story 99.9\n\nStatus: done\n\nProse only.\n"
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", artifacts={"99-9-fixture.md": body}
        )
        self.assertEqual(code, 0)
        self.assertIn("no-op", out)

    def test_missing_artifact_fails_closed(self):
        code, out = self.run_cli("--story-key", "99-9-fixture")
        self.assertEqual(code, 1)
        self.assertIn("Story artifact not found", out)


class C6EvidenceRowTests(VerifierTestCase):
    # The real 26-5 shape: `| pending | - |` under a done story.
    REAL_26_5 = (
        "### Evidence Table\n\n"
        "| Checkpoint | Owner | Validation command/artifact | Review status | Completion date |\n"
        "|---|---|---|---|---|\n"
        "| Audit-anchor preflight | Developer | Revision reconciliation | pending | - |\n"
        "| Capacity planning | Developer | `capacity-planning.md` | pending | - |\n\n"
    )
    # The real 22-2 shape: capitalised `Pending` with an empty completion cell.
    REAL_22_2 = (
        "### Evidence Table\n\n"
        "| Evidence item | Owner | Required proof | Review status | Completion date |\n"
        "|---|---|---|---|---|\n"
        "| A9 current-state proof | Dev | Test or code proof | Pending | |\n\n"
    )
    VERIFIED = (
        "### Evidence Table\n\n"
        "| Evidence item | Owner | Required proof | Review status | Completion date |\n"
        "|---|---|---|---|---|\n"
        "| A9 current-state proof | Dev | Test or code proof | Verified | 2026-07-05 |\n\n"
    )

    def assert_story(self, status, evidence, expected_code, sprint_status=None):
        sprint = f"development_status:\n  99-9-fixture: {sprint_status or status}\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status=status, evidence=evidence)},
            sprint=sprint,
        )
        self.assertEqual(code, expected_code, out)
        return out

    def test_done_with_lowercase_pending_rows_fails(self):
        out = self.assert_story("done", self.REAL_26_5, 1)
        self.assertIn("C6: evidence row 'Audit-anchor preflight'", out)
        self.assertIn("C6: evidence row 'Capacity planning'", out)

    def test_done_with_capitalised_pending_and_empty_date_fails(self):
        out = self.assert_story("done", self.REAL_22_2, 1)
        self.assertIn("C6: evidence row 'A9 current-state proof'", out)

    def test_in_progress_with_pending_rows_passes(self):
        # 27-3 carries 14 pending rows while in-progress. That is correct, not drift.
        self.assert_story("in-progress", self.REAL_26_5, 0)

    def test_ready_for_dev_with_pending_rows_passes(self):
        # 31-2's five pending rows at ready-for-dev must not fail.
        self.assert_story("ready-for-dev", self.REAL_26_5, 0)

    def test_done_with_verified_rows_passes(self):
        self.assert_story("done", self.VERIFIED, 0)

    def test_blocked_row_is_accepted_at_done(self):
        blocked = self.REAL_26_5.replace("| pending | - |", "| blocked - owner named | - |")
        self.assert_story("done", blocked, 0)

    def test_review_state_spelling_is_honoured(self):
        renamed = self.REAL_26_5.replace("Review status", "Review state")
        out = self.assert_story("done", renamed, 1)
        self.assertIn("C6", out)

    def test_artifact_without_evidence_table_skips_c6(self):
        out = self.assert_story("done", "", 0)
        self.assertIn("C6", out)
        self.assertIn("skipped", out)


class C6ParserTests(unittest.TestCase):
    """White-box checks against the live artifacts C6 was designed from."""

    ARTIFACTS = REPO_ROOT / "_bmad-output" / "implementation-artifacts"

    def test_live_26_5_has_ten_pending_rows(self):
        text = V.read_text_lf(self.ARTIFACTS / "26-5-operational-runbook-set.md")
        failures, tables = V.check_evidence_rows(text, "done")
        # 26-5 carries the checkpoint table twice: once as the live table and
        # once restated inside the completion record.
        self.assertEqual(tables, 2)
        self.assertEqual(len(failures), 10)

    def test_live_22_2_has_five_pending_rows(self):
        text = V.read_text_lf(self.ARTIFACTS / "22-2-bounded-cancellable-graph-traversal.md")
        failures, _ = V.check_evidence_rows(text, "done")
        self.assertEqual(len(failures), 5)

    def test_live_27_3_pending_rows_do_not_fire_below_review(self):
        text = V.read_text_lf(self.ARTIFACTS / "27-3-production-adapter-and-deployment-profile.md")
        failures, tables = V.check_evidence_rows(text, "in-progress")
        self.assertGreater(tables, 0)
        self.assertEqual(failures, [])


class C2LedgerTests(VerifierTestCase):
    def test_bold_matched_cell_is_accepted(self):
        # Live cells write `Matched **27/27**`. A pattern anchored straight after
        # "matched" breaks on the bold markers; this is the regression guard.
        rows = "| 2026-07-01 | code-review | did work | +0 | Matched **27/27** cumulative paths |\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress", ledger_rows=rows)},
        )
        self.assertEqual(code, 0, out)

    def test_placeholder_test_count_fails(self):
        rows = "| 2026-07-01 | dev-story | did work | TBD | matched 1/1 |\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress", ledger_rows=rows)},
        )
        self.assertEqual(code, 1)
        self.assertIn("placeholder", out)

    def test_review_without_dev_story_row_fails(self):
        rows = "| 2026-07-01 | create-story | baseline | +0 | matched 1/1 |\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="review", ledger_rows=rows)},
            sprint="development_status:\n  99-9-fixture: review\n",
        )
        self.assertEqual(code, 1)
        self.assertIn("no dev-story ledger row", out)

    def test_newest_row_without_reconciliation_fails(self):
        rows = "| 2026-07-01 | dev-story | did work | +2 methods | see notes |\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress", ledger_rows=rows)},
        )
        self.assertEqual(code, 1)
        self.assertIn("matched N/N", out)

    def test_blocked_evidence_record_is_accepted(self):
        rows = (
            "| 2026-07-01 | dev-story | did work | +2 | discovery blocked; owner named, "
            "reopen trigger recorded |\n"
        )
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress", ledger_rows=rows)},
        )
        self.assertEqual(code, 0, out)


class C3StatusTests(VerifierTestCase):
    def test_unknown_status_fails(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="almost-done")},
        )
        self.assertEqual(code, 1)
        self.assertIn("C3", out)

    def test_each_valid_status_is_recognised(self):
        for status in V.VALID_STATUSES:
            with self.subTest(status=status):
                self.assertIn(status, V.VALID_STATUSES)
                text = story(status=status)
                self.assertEqual(V.parse_status(text), status)


class C4SprintSyncTests(VerifierTestCase):
    def test_status_mismatch_fails(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="review")},
            sprint="development_status:\n  99-9-fixture: in-progress\n",
        )
        self.assertEqual(code, 1)
        self.assertIn("C4", out)

    def test_missing_row_fails(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress")},
            sprint="development_status:\n  other-key: done\n",
        )
        self.assertEqual(code, 1)
        self.assertIn("no development_status row", out)

    def test_crlf_sprint_status_parses_clean(self):
        # sprint-status.yaml is eol=lf but materialises CRLF in the worktree.
        # An unstripped \r produced false mismatches during design.
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress")},
            sprint="development_status:\r\n  99-9-fixture: in-progress\r\n",
        )
        self.assertEqual(code, 0, out)

    def test_crlf_story_file_parses_clean(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress").replace("\n", "\r\n")},
        )
        self.assertEqual(code, 0, out)


class C1FileListTests(VerifierTestCase):
    def test_changed_but_unlisted_fails(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            "--changed-file",
            "src/undeclared.cs",
            artifacts={"99-9-fixture.md": story(status="in-progress", file_list=["docs/a.md"])},
        )
        self.assertEqual(code, 1)
        self.assertIn("src/undeclared.cs", out)

    def test_declared_exclusion_is_honoured(self):
        exclusions = (
            "### File List Exclusions\n\n"
            "- `src/other.cs` — owner: Another Story; concurrent work, not credited here\n"
        )
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            "--changed-file",
            "src/other.cs",
            artifacts={
                "99-9-fixture.md": story(
                    status="in-progress", file_list=["docs/a.md"], exclusions=exclusions
                )
            },
        )
        self.assertEqual(code, 0, out)

    def test_exclusion_without_owner_is_rejected(self):
        exclusions = "### File List Exclusions\n\n- `src/other.cs` — concurrent work\n"
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "docs/a.md",
            artifacts={
                "99-9-fixture.md": story(
                    status="in-progress", file_list=["docs/a.md"], exclusions=exclusions
                )
            },
        )
        self.assertEqual(code, 1)
        self.assertIn("names no owner", out)

    def test_empty_changed_set_fails_closed(self):
        # The sibling gate's vacuous pass is the failure this inverts: an empty
        # set on a governed story proves nothing and must not exit 0.
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            artifacts={"99-9-fixture.md": story(status="in-progress")},
        )
        self.assertEqual(code, 1)
        self.assertIn("fail closed", out)

    def test_path_normalisation(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            "--changed-file",
            "./docs//a.md",
            artifacts={"99-9-fixture.md": story(status="in-progress", file_list=["docs/a.md"])},
        )
        self.assertEqual(code, 0, out)

    def test_done_status_skips_c1(self):
        code, out = self.run_cli(
            "--story-key",
            "99-9-fixture",
            artifacts={"99-9-fixture.md": story(status="done")},
            sprint="development_status:\n  99-9-fixture: done\n",
        )
        self.assertEqual(code, 0, out)
        self.assertIn("C1", out)
        self.assertIn("skipped", out)


class BypassTests(VerifierTestCase):
    def write_message(self, body: str) -> str:
        handle = tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False)
        handle.write(body)
        handle.close()
        return handle.name

    def test_bypass_with_reason_passes(self):
        message = self.write_message(
            "fix: thing\n\nStory: 99-9-fixture\n"
            "Story-Review-Readiness-Bypass: platform outage, tracked as DW-1\n"
        )
        code, out = self.run_cli(
            "--commit-message-file",
            message,
            artifacts={"99-9-fixture.md": story(status="done")},
        )
        self.assertEqual(code, 0)
        self.assertIn("bypassed", out)

    def test_bypass_without_reason_fails(self):
        message = self.write_message(
            "fix: thing\n\nStory: 99-9-fixture\nStory-Review-Readiness-Bypass:\n"
        )
        code, out = self.run_cli(
            "--commit-message-file",
            message,
            artifacts={"99-9-fixture.md": story(status="done")},
        )
        self.assertEqual(code, 1)
        self.assertIn("non-empty reason", out)


class LivenessTests(unittest.TestCase):
    """Guards against the parser drifting away from the live artifact format.

    A synthetic-only suite passes forever while the real format moves. These
    assert the measured 2026-07-28 state of the repository.
    """

    ARTIFACTS = REPO_ROOT / "_bmad-output" / "implementation-artifacts"

    def evidence_bearers(self) -> list[Path]:
        bearers = []
        for path in sorted(self.ARTIFACTS.glob("*.md")):
            text = V.read_text_lf(path)
            if V.find_evidence_tables(text):
                bearers.append(path)
        return bearers

    def test_evidence_table_bearers_are_discovered(self):
        # 14 artifacts carried a Review status/state column on 2026-07-28. A drop
        # means the parser stopped recognising the table shape.
        self.assertGreaterEqual(len(self.evidence_bearers()), 14)

    def test_every_ledger_bearer_parses(self):
        found = 0
        for path in sorted(self.ARTIFACTS.glob("*.md")):
            rows = V.find_ledger(V.read_text_lf(path))
            if rows is None:
                continue
            found += 1
            self.assertTrue(rows, f"{path.name} has a ledger header but no parsed rows")
        self.assertGreaterEqual(found, 8)

    def test_only_the_two_known_stale_artifacts_violate_c6(self):
        statuses = V.parse_sprint_status(self.ARTIFACTS / "sprint-status.yaml")
        violating = []
        for path in self.evidence_bearers():
            status = statuses.get(path.stem)
            if status is None:
                continue
            failures, _ = V.check_evidence_rows(V.read_text_lf(path), status)
            if failures:
                violating.append(path.stem)
        self.assertEqual(
            sorted(violating),
            ["22-2-bounded-cancellable-graph-traversal", "26-5-operational-runbook-set"],
            "C6 violations changed; retrofit the new artifact or update this guard.",
        )

    def test_live_status_lines_parse_for_every_governed_artifact(self):
        statuses = V.parse_sprint_status(self.ARTIFACTS / "sprint-status.yaml")
        for path in self.evidence_bearers():
            if path.stem not in statuses:
                continue
            with self.subTest(artifact=path.name):
                self.assertIsNotNone(
                    V.parse_status(V.read_text_lf(path)),
                    f"{path.name} declares no parseable Status: line",
                )


if __name__ == "__main__":
    unittest.main()
