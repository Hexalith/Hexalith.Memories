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
    """C2 requires creation/development at review/done and final review at done.

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
    listed = "".join(f"- `{path}`\n" for path in (["docs/a.md"] if file_list is None else file_list))
    return (
        f"{frontmatter}\n# Story 99.9: Fixture\n\nStatus: {status}\n\n"
        f"## Change Log\n\n{LEDGER_HEADER}{ledger_rows}\n"
        f"{evidence}"
        f"### File List\n\n{listed}\n{exclusions}"
    )


class VerifierTestCase(unittest.TestCase):
    """Writes a story artifact into a throwaway artifacts root and runs the CLI."""

    def run_cli(
        self, *args: str, artifacts: dict[str, str] | None = None,
        sprint: str | None = None, cwd: Path | None = None,
    ):
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
                cwd=REPO_ROOT if cwd is None else cwd,
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


class OwnershipResolutionTests(VerifierTestCase):
    def write_message(self, body: str) -> Path:
        handle = tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8")
        try:
            handle.write(body)
        finally:
            handle.close()
        path = Path(handle.name)
        self.addCleanup(lambda: path.unlink(missing_ok=True))
        return path

    def test_spec_key_resolves_from_cli_trailer_and_branch(self):
        spec_key = "spec-readiness-fixture"
        artifact = story(status="done")
        message = self.write_message(
            f"test: verify standalone spec readiness\n\nStory-Key: {spec_key}\n"
        )
        cases = (
            ("cli", ("--story-key", spec_key)),
            ("trailer", ("--commit-message-file", str(message))),
            ("branch", ("--branch-name", f"fix/{spec_key}")),
        )

        for expected_source, source_args in cases:
            with self.subTest(source=expected_source):
                code, out = self.run_cli(
                    *source_args,
                    artifacts={f"{spec_key}.md": artifact},
                    sprint="development_status:\n",
                )

                self.assertEqual(code, 0, out)
                self.assertIn(f"Selected story key: {spec_key}", out)
                self.assertIn(f"Story source: {expected_source}", out)

    def test_spec_trailer_conflicting_with_numeric_story_branch_fails(self):
        message = self.write_message(
            "test: verify standalone spec readiness\n\n"
            "Story-Key: spec-readiness-fixture\n"
        )

        code, out = self.run_cli(
            "--branch-name",
            "feature/99-9-fixture",
            "--commit-message-file",
            str(message),
        )

        self.assertEqual(code, 1, out)
        self.assertIn("Conflicting story keys", out)
        self.assertIn("trailer=spec-readiness-fixture", out)
        self.assertIn("branch=99-9-fixture", out)

    def test_extended_spec_key_does_not_fall_back_to_shorter_artifact(self):
        code, out = self.run_cli(
            "--branch-name",
            "fix/spec-readiness-fixture-extra",
            artifacts={"spec-readiness-fixture.md": story(status="done")},
            sprint="development_status:\n",
        )

        self.assertEqual(code, 1, out)
        self.assertIn("spec-readiness-fixture-extra.md", out)

    def test_malformed_spec_key_fails_from_cli_trailer_and_branch(self):
        message = self.write_message(
            "test: reject malformed standalone spec key\n\n"
            "Story-Key: spec-readiness-fixture.extra\n"
        )
        cases = (
            ("cli", ("--story-key", "spec-readiness-fixture.extra")),
            ("trailer", ("--commit-message-file", str(message))),
            ("branch", ("--branch-name", "fix/spec-readiness-fixture.extra")),
        )

        for source, source_args in cases:
            with self.subTest(source=source):
                code, out = self.run_cli(*source_args)

                self.assertEqual(code, 1, out)
                self.assertIn("valid standalone spec key", out)

    def test_incomplete_spec_key_fails_from_cli_trailer_and_branch(self):
        for malformed in ("spec-", "spec-readiness-fixture-"):
            message = self.write_message(
                f"test: reject incomplete standalone spec key\n\nStory-Key: {malformed}\n"
            )
            cases = (
                ("cli", ("--story-key", malformed)),
                ("trailer", ("--commit-message-file", str(message))),
                ("branch", ("--branch-name", f"fix/{malformed}")),
            )

            for source, source_args in cases:
                with self.subTest(malformed=malformed, source=source):
                    code, out = self.run_cli(*source_args)

                    self.assertEqual(code, 1, out)
                    self.assertIn("valid standalone spec key", out)

    def test_duplicate_spec_key_fails_from_cli_trailer_and_branch(self):
        duplicate = "spec-readiness-fixture/spec-readiness-fixture"
        message = self.write_message(
            "test: reject duplicate standalone spec key\n\n"
            f"Story-Key: {duplicate}\n"
        )
        cases = (
            ("cli", ("--story-key", duplicate)),
            ("trailer", ("--commit-message-file", str(message))),
            ("branch", ("--branch-name", f"fix/{duplicate}")),
        )

        for source, source_args in cases:
            with self.subTest(source=source):
                code, out = self.run_cli(*source_args)

                self.assertEqual(code, 1, out)
                self.assertIn("multiple story keys", out.lower())

    def test_unicode_spec_key_does_not_alias_ascii_artifact(self):
        code, out = self.run_cli(
            "--story-key",
            "spec-Key",
            artifacts={"spec-key.md": story(status="done")},
            sprint="development_status:\n",
        )

        self.assertEqual(code, 1, out)
        self.assertIn("valid standalone spec key", out)

    def test_bypass_does_not_hide_owner_conflict(self):
        message = self.write_message(
            "test: reject conflicting owner under bypass\n\n"
            "Story-Key: 99-9-fixture\n"
            "Story-Review-Readiness-Bypass: verifier-only change\n"
        )

        code, out = self.run_cli(
            "--branch-name",
            "fix/spec-readiness-fixture",
            "--commit-message-file",
            str(message),
        )

        self.assertEqual(code, 1, out)
        self.assertIn("Conflicting story keys", out)


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

    def test_missing_status_cell_fails_at_review_and_done(self):
        for status in ("review", "done"):
            for evidence in (
                self.VERIFIED.replace(
                    "| A9 current-state proof | Dev | Test or code proof | Verified | 2026-07-05 |",
                    "| Truncated proof | Dev | Test or code proof |",
                ),
                self.VERIFIED.replace(
                    "| A9 current-state proof | Dev | Test or code proof | Verified | 2026-07-05 |",
                    "| Truncated proof | Dev | Test or code proof | 2026-10-22 |",
                ),
                "| Gate | Review state |\n|---|---|\n| Truncated proof |\n",
            ):
                with self.subTest(status=status, evidence=evidence):
                    out = self.assert_story(status, evidence, 1)
                    self.assertIn("C6: evidence row 'Truncated proof' is truncated", out)

    def test_placeholder_only_rows_are_not_discarded_as_separators(self):
        for status in ("review", "done"):
            for row in ("| - |", "| - | - | - | - | - |"):
                with self.subTest(status=status, row=row):
                    evidence = self.VERIFIED.rstrip() + "\n" + row + "\n"
                    out = self.assert_story(status, evidence, 1)
                    self.assertIn("C6: evidence row '-'", out)

    def test_missing_status_cell_is_allowed_below_review(self):
        evidence = "| Gate | Review status |\n|---|---|\n| Truncated proof |\n"
        for status in ("in-progress", "ready-for-dev"):
            with self.subTest(status=status):
                self.assert_story(status, evidence, 0)


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
    BLOCKED_RECORD = (
        "command: `python3 tools/discover.py`; blocker: runner unavailable; "
        "owner: Developer; consequence: count agreement remains open; "
        "reopen trigger: runner restored"
    )

    def assert_ledger(self, rows, expected_code, status="review"):
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
            artifacts={"99-9-fixture.md": story(status=status, ledger_rows=rows)},
            sprint=f"development_status:\n  99-9-fixture: {status}\n",
        )
        self.assertEqual(code, expected_code, out)
        return out

    def test_review_and_done_without_create_story_row_fail(self):
        for status in ("review", "done"):
            with self.subTest(status=status):
                rows = default_ledger(status).replace(CREATE_ROW, "")
                out = self.assert_ledger(rows, 1, status)
                self.assertIn("no create-story ledger row", out)

    def test_governed_review_and_done_require_ledger(self):
        for status in ("review", "done"):
            for section in ("file-list", "evidence", "both"):
                with self.subTest(status=status, section=section):
                    body = f"# Fixture\n\nStatus: {status}\n\n"
                    if section in ("file-list", "both"):
                        body += "### File List\n\n- `docs/a.md`\n\n"
                    if section in ("evidence", "both"):
                        body += C6EvidenceRowTests.VERIFIED
                    code, out = self.run_cli(
                        "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
                        artifacts={"99-9-fixture.md": body},
                        sprint=f"development_status:\n  99-9-fixture: {status}\n",
                    )
                    self.assertEqual(code, 1, out)
                    self.assertIn("C2:", out)
                    self.assertIn("phase ledger is required", out)

    def test_missing_ledger_below_review_keeps_existing_behavior(self):
        for status in ("backlog", "ready-for-dev", "in-progress"):
            with self.subTest(status=status):
                body = f"# Fixture\n\nStatus: {status}\n\n### File List\n\n- `docs/a.md`\n"
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
                    artifacts={"99-9-fixture.md": body},
                    sprint=f"development_status:\n  99-9-fixture: {status}\n",
                )
                self.assertEqual(code, 0, out)
                self.assertIn("ledger checks skipped", out)

    def test_empty_ledger_cannot_replace_canonical_rows(self):
        out = self.assert_ledger("", 1)
        self.assertIn("no ledger row carries a canonical phase", out)

    def test_out_of_order_canonical_phases_fail(self):
        qa = DEV_ROW.replace("dev-story", "qa-gap-closure")
        correction = DEV_ROW.replace("dev-story", "correct-course")
        for rows in (
            DEV_ROW + CREATE_ROW,
            CREATE_ROW + REVIEW_ROW + DEV_ROW,
            CREATE_ROW + qa + DEV_ROW,
            correction + CREATE_ROW + DEV_ROW,
        ):
            with self.subTest(rows=rows):
                out = self.assert_ledger(rows, 1)
                self.assertIn("C2: phase order", out)

    def test_repeated_phases_and_late_course_corrections_pass(self):
        qa = DEV_ROW.replace("dev-story", "qa-gap-closure")
        correction = DEV_ROW.replace("dev-story", "correct-course")
        rows = CREATE_ROW + CREATE_ROW + correction + DEV_ROW + qa + REVIEW_ROW
        rows += DEV_ROW + correction + qa + REVIEW_ROW + REVIEW_ROW
        self.assert_ledger(rows, 0, "done")

    def test_date_and_change_placeholders_fail_on_every_canonical_phase(self):
        for phase in V.CANONICAL_PHASES:
            for column, label in ((0, "Date"), (2, "Change")):
                for placeholder in ("", "-", "—", "**TBD**", "`N/A`", "todo"):
                    with self.subTest(phase=phase, column=label, placeholder=placeholder):
                        cells = ["2026-07-03", phase, "work recorded", "+0 methods", "matched 1/1"]
                        cells[column] = placeholder
                        row = "| " + " | ".join(cells) + " |\n"
                        out = self.assert_ledger(CREATE_ROW + DEV_ROW + row, 1)
                        self.assertIn(f"{phase} row leaves '{label}' as a placeholder", out)

    def test_nonplaceholder_historical_date_format_is_preserved(self):
        rows = (CREATE_ROW + DEV_ROW + REVIEW_ROW).replace("2026-07-03", "2026-09-02/03")
        self.assert_ledger(rows, 0, "done")

    def test_bare_blocker_markers_do_not_replace_accountable_records(self):
        for value in ("blocker", "blocked", "not run", "could not run", "reopen trigger recorded"):
            with self.subTest(value=value):
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", value)
                out = self.assert_ledger(CREATE_ROW + row, 1)
                self.assertIn("blocked-evidence", out)

    def test_blocked_record_requires_each_nonplaceholder_accountability_field(self):
        fields = self.BLOCKED_RECORD.split("; ")
        for field in fields:
            label = field.split(":", 1)[0]
            for replacement in (None, f"{label}: ", f"{label}: **TBD**", f"{label}: `N/A`"):
                with self.subTest(label=label, replacement=replacement):
                    record = "; ".join(
                        item if item != field else replacement
                        for item in fields if item != field or replacement is not None
                    )
                    row = DEV_ROW.replace("matched 1/1 vs `abc1234`", record)
                    out = self.assert_ledger(CREATE_ROW + row, 1)
                    self.assertIn("blocked-evidence", out)
                    self.assertIn(label, out)

    def test_duplicate_blocked_record_fields_are_rejected(self):
        for field in self.BLOCKED_RECORD.split("; "):
            with self.subTest(field=field):
                record = self.BLOCKED_RECORD + "; " + field
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", record)
                out = self.assert_ledger(CREATE_ROW + row, 1)
                self.assertIn("blocked-evidence", out)
                self.assertIn(field.split(":", 1)[0], out)

    def test_blocked_record_accepts_formatting_and_command_semicolons(self):
        record = self.BLOCKED_RECORD.replace(
            "command: `python3 tools/discover.py`",
            "**Command**: `prepare; python3 tools/discover.py`",
        ).replace("reopen trigger:", "Re-open trigger:")
        row = DEV_ROW.replace("matched 1/1 vs `abc1234`", record)
        self.assert_ledger(CREATE_ROW + row, 0)

    def test_command_code_spans_cannot_create_metadata_fields(self):
        for command in (
            '`python3 -c \'print("; owner: diagnostic")\'`',
            '``python3 -c \'print("`marker`; owner: diagnostic")\'``',
        ):
            with self.subTest(command=command):
                record = self.BLOCKED_RECORD.replace("`python3 tools/discover.py`", command)
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", record)
                self.assert_ledger(CREATE_ROW + row, 0)

    def test_command_code_spans_cannot_supply_missing_metadata(self):
        record = (
            'command: `python3 -c \'print("; owner: Developer; consequence: open; '
            'reopen trigger: runner restored")\'`; blocker: unavailable'
        )
        row = DEV_ROW.replace("matched 1/1 vs `abc1234`", record)
        out = self.assert_ledger(CREATE_ROW + row, 1)
        for label in ("owner", "consequence", "reopen trigger"):
            self.assertIn(label, out)

    def test_matched_text_in_commands_or_negative_words_is_not_reconciliation(self):
        for value in (
            'command: echo "matched 1/1"; blocker: TBD',
            'command: `echo "matched 1/1"`; blocker: TBD',
            'unmatched 1/1; blocker: TBD',
            'not matched 1/1; blocker: TBD',
        ):
            with self.subTest(value=value):
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", value)
                out = self.assert_ledger(CREATE_ROW + row, 1)
                self.assertIn("blocked-evidence", out)

    def test_affirmative_cumulative_and_code_formatted_reconciliations_pass(self):
        for value in ("**Cumulative matched 1/1** paths", "`matched 1/1` paths"):
            with self.subTest(value=value):
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", value)
                self.assert_ledger(CREATE_ROW + row, 0)

    def test_explicit_template_markers_are_placeholders(self):
        for placeholder in ("<name>", "<date>", "...", "…"):
            for label in ("command", "blocker", "owner", "consequence", "reopen trigger"):
                with self.subTest(placeholder=placeholder, label=label):
                    fields = [
                        f"{label}: {placeholder}" if field.startswith(label + ":") else field
                        for field in self.BLOCKED_RECORD.split("; ")
                    ]
                    row = DEV_ROW.replace("matched 1/1 vs `abc1234`", "; ".join(fields))
                    self.assertIn("blocked-evidence", self.assert_ledger(CREATE_ROW + row, 1))
            for column in (0, 2):
                with self.subTest(placeholder=placeholder, column=column):
                    cells = ["2026-07-02", "dev-story", "work recorded", "+0 methods", "matched 1/1"]
                    cells[column] = placeholder
                    row = "| " + " | ".join(cells) + " |\n"
                    self.assertIn("placeholder", self.assert_ledger(CREATE_ROW + row, 1))

    def test_unknown_metadata_cannot_disguise_placeholder_required_values(self):
        for label in ("command", "blocker", "owner", "consequence", "reopen trigger"):
            with self.subTest(label=label):
                fields = [
                    f"{label}: TBD; note: follow up" if field.startswith(label + ":") else field
                    for field in self.BLOCKED_RECORD.split("; ")
                ]
                row = DEV_ROW.replace("matched 1/1 vs `abc1234`", "; ".join(fields))
                self.assertIn("blocked-evidence", self.assert_ledger(CREATE_ROW + row, 1))

    def test_done_requires_review_after_latest_governed_work(self):
        for phase in ("dev-story", "qa-gap-closure", "correct-course"):
            with self.subTest(phase=phase):
                latest = DEV_ROW.replace("dev-story", phase)
                rows = CREATE_ROW + DEV_ROW + REVIEW_ROW + latest
                out = self.assert_ledger(rows, 1, "done")
                self.assertIn("final canonical ledger row is not code-review", out)
                self.assert_ledger(rows + REVIEW_ROW, 0, "done")

    def test_bold_matched_cell_is_accepted(self):
        # Live cells write `Matched **27/27**`. A pattern anchored straight after
        # "matched" breaks on the bold markers; this is the regression guard.
        rows = CREATE_ROW + DEV_ROW + "| 2026-07-03 | code-review | did work | +0 | Matched **27/27** cumulative paths |\n"
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
        for status in ("in-progress", "review", "done"):
            with self.subTest(status=status):
                final = REVIEW_ROW if status == "done" else DEV_ROW
                row = final.replace("matched 1/1 vs `abc1234`", self.BLOCKED_RECORD)
                rows = CREATE_ROW + DEV_ROW if status == "done" else CREATE_ROW
                self.assert_ledger(rows + row, 0, status)


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
    def test_listed_but_unchanged_fails(self):
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
            artifacts={"99-9-fixture.md": story(file_list=["docs/a.md", "docs/stale.md"])},
        )
        self.assertEqual(code, 1, out)
        self.assertIn("'docs/stale.md' is in the File List but not in the in-scope changed set", out)

    def test_mismatch_reports_both_directions(self):
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", "docs/new.md",
            artifacts={"99-9-fixture.md": story(file_list=["docs/old.md"])},
        )
        self.assertEqual(code, 1, out)
        self.assertIn("'docs/new.md' changed but is not in the File List", out)
        self.assertIn("'docs/old.md' is in the File List but not in the in-scope changed set", out)

    def test_wildcards_cannot_replace_exact_paths(self):
        for pattern in ("src/**", "src/*.cs", "src/file?.cs", "src/[ab].cs"):
            changed = "src/a.cs" if "[ab]" in pattern else "src/file1.cs"
            with self.subTest(pattern=pattern):
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture", "--changed-file", changed,
                    artifacts={"99-9-fixture.md": story(file_list=[pattern])},
                )
                self.assertEqual(code, 1, out)
                self.assertIn(f"'{changed}' changed but is not in the File List", out)
                self.assertIn(f"'{pattern}' is in the File List but not in the in-scope changed set", out)

    def test_literal_glob_metacharacters_match_identical_paths(self):
        path = "src/literal[ab].cs"
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", path,
            artifacts={"99-9-fixture.md": story(file_list=[path])},
        )
        self.assertEqual(code, 0, out)
        self.assertIn("File List exactly matches all 1 in-scope changed paths", out)

    def test_missing_file_list_fails_for_active_governed_artifacts(self):
        for status in ("in-progress", "review"):
            for governed_body in (
                story(status=status).split("### File List", 1)[0],
                f"Status: {status}\n\n{C6EvidenceRowTests.VERIFIED}",
            ):
                with self.subTest(status=status, body=governed_body):
                    code, out = self.run_cli(
                        "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
                        artifacts={"99-9-fixture.md": governed_body},
                        sprint=f"development_status:\n  99-9-fixture: {status}\n",
                    )
                    self.assertEqual(code, 1, out)
                    self.assertIn("C1: a non-empty File List is required", out)

    def test_empty_file_list_fails_for_active_governed_artifacts(self):
        for status in ("in-progress", "review"):
            with self.subTest(status=status):
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
                    artifacts={"99-9-fixture.md": story(status=status, file_list=[])},
                    sprint=f"development_status:\n  99-9-fixture: {status}\n",
                )
                self.assertEqual(code, 1, out)
                self.assertIn("C1: a non-empty File List is required", out)

    def test_prose_only_artifacts_remain_ungoverned(self):
        for status in ("in-progress", "review", "done"):
            with self.subTest(status=status):
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture",
                    artifacts={"99-9-fixture.md": f"Status: {status}\n\nProse only.\n"},
                )
                self.assertEqual(code, 0, out)
                self.assertIn("no-op", out)

    def test_missing_file_list_outside_active_statuses_skips_c1(self):
        for status in ("backlog", "ready-for-dev", "done"):
            with self.subTest(status=status):
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture",
                    artifacts={"99-9-fixture.md": story(status=status).split("### File List", 1)[0]},
                    sprint=f"development_status:\n  99-9-fixture: {status}\n",
                )
                self.assertEqual(code, 0, out)
                self.assertIn("C1: artifact carries no File List; completeness check skipped", out)

    def test_blank_exclusion_owner_fails(self):
        for owner in (" ", "   ", "\t"):
            with self.subTest(owner=owner):
                exclusions = (
                    "### File List Exclusions\n\n"
                    f"- `src/other.cs` — owner: {owner}; concurrent work\n"
                )
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
                    "--changed-file", "src/other.cs",
                    artifacts={"99-9-fixture.md": story(exclusions=exclusions)},
                )
                self.assertEqual(code, 1, out)
                self.assertIn("File List Exclusions entry names no owner", out)

    def test_named_exclusions_are_removed_before_exact_set_comparison(self):
        exclusions = (
            "### File List Exclusions\n\n"
            "- `src/other.cs` — owner: Another Story; concurrent work\n"
        )
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
            "--changed-file", "src/other.cs",
            artifacts={"99-9-fixture.md": story(
                file_list=["./docs//a.md"], exclusions=exclusions,
            )},
        )
        self.assertEqual(code, 0, out)
        self.assertIn("File List exactly matches all 1 in-scope changed paths", out)

    def test_excluded_path_cannot_also_be_credited_in_file_list(self):
        exclusions = (
            "### File List Exclusions\n\n"
            "- `src/other.cs` — owner: Another Story; concurrent work\n"
        )
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--changed-file", "docs/a.md",
            "--changed-file", "src/other.cs",
            artifacts={"99-9-fixture.md": story(
                file_list=["docs/a.md", "src/other.cs"], exclusions=exclusions,
            )},
        )
        self.assertEqual(code, 1, out)
        self.assertIn("'src/other.cs' is in the File List but not in the in-scope changed set", out)

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


class C1CumulativeTests(VerifierTestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="readiness-git-")
        self.addCleanup(temporary.cleanup)
        self.repo = Path(temporary.name)
        self.git("init", "--initial-branch=main")
        (self.repo / "docs").mkdir()
        self.write_file("docs/unchanged.md")
        self.git("add", ".")
        self.git("commit", "-m", "test: create readiness baseline fixture")
        self.baseline = self.git("rev-parse", "HEAD").strip()

    def git(self, *args):
        result = subprocess.run(
            ["git", "-c", "user.name=Readiness Fixture", "-c",
             "user.email=readiness@example.invalid", "-c", "commit.gpgsign=false", *args],
            cwd=self.repo, text=True, capture_output=True, check=True,
        )
        return result.stdout

    def write_file(self, name):
        (self.repo / name).write_text("fixture\n", encoding="utf-8")

    def fixture_story(self, **kwargs):
        return story(frontmatter=f"---\nbaseline_commit: {self.baseline}\n---\n", **kwargs)

    def test_cumulative_comparison_includes_committed_staged_and_supplied_paths(self):
        self.git("switch", "-c", "fix/99-9-fixture")
        self.write_file("docs/committed.md")
        self.git("add", ".")
        self.git("commit", "-m", "test: record cumulative readiness fixture")
        self.write_file("docs/staged.md")
        self.git("add", ".")
        self.write_file("docs/supplied.md")
        paths = ["docs/committed.md", "docs/staged.md", "docs/supplied.md"]
        for file_list, missing, stale in (
            (paths, None, None),
            (paths[1:], paths[0], None),
            ([paths[0], paths[2]], paths[1], None),
            (paths[:2], paths[2], None),
            (paths + ["docs/unchanged.md"], None, "docs/unchanged.md"),
        ):
            with self.subTest(file_list=file_list):
                code, out = self.run_cli(
                    "--story-key", "99-9-fixture", "--derive-cumulative",
                    "--changed-file", "docs/supplied.md", cwd=self.repo,
                    artifacts={"99-9-fixture.md": self.fixture_story(file_list=file_list)},
                )
                self.assertEqual(code, 1 if missing or stale else 0, out)
                if missing:
                    self.assertIn(f"'{missing}' changed but is not in the File List", out)
                elif stale:
                    self.assertIn(f"'{stale}' is in the File List but not in the in-scope changed set", out)
                else:
                    self.assertIn("File List exactly matches all 3 in-scope changed paths", out)

    def test_default_branch_skips_only_cumulative_comparison(self):
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--derive-cumulative", cwd=self.repo,
            artifacts={"99-9-fixture.md": self.fixture_story(file_list=["docs/stale.md"])},
        )
        self.assertEqual(code, 0, out)
        self.assertIn("C1 SKIPPED: HEAD is on default branch 'main'", out)
        self.assertNotIn("File List exactly matches", out)

    def test_default_branch_skip_cannot_hide_blank_exclusion_owner(self):
        exclusions = (
            "### File List Exclusions\n\n"
            "- `src/other.cs` — owner:   ; concurrent work\n"
        )
        code, out = self.run_cli(
            "--story-key", "99-9-fixture", "--derive-cumulative", cwd=self.repo,
            artifacts={"99-9-fixture.md": self.fixture_story(exclusions=exclusions)},
        )
        self.assertEqual(code, 1, out)
        self.assertIn("File List Exclusions entry names no owner", out)


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
