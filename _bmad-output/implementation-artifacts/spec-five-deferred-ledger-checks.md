---
title: 'Close five deferred phase-ledger checks'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close DW-608, DW-609, DW-610, DW-612 and DW-613 through the existing story readiness checker's phase-ledger gate. These five related gaps let governed artifacts advance without a creation baseline, a ledger, accountable blocker details, a plausible phase sequence, or meaningful Date/Change cells.

Given a governed review/done artifact, when the ledger or create-story row is absent, then C2 fails. Given canonical ledger rows, when creation does not precede development or QA/review precedes development, then C2 fails; repeated development, review and course-correction work remains valid. Given a canonical row, when Date or Change is a placeholder, then C2 fails. Given a newest reconciliation cell without matched N/N, when its blocked-evidence record lacks a non-placeholder command, blocker, owner, consequence or reopen trigger, then C2 fails with actionable details. Complete structured blocker records and normal matched records pass.

Preserve ungoverned-prose no-ops, existing statuses, default-branch cumulative-comparison behavior, optional QA/course-correction phases, and evidence-row checks. Do not impose a new date format or a global monotonic phase ranking: live ledgers legitimately alternate development, review and course correction. Update exactly the five deferred dispositions with verification evidence. The user authorized completing five deferred items and committing/pushing changes on main. Runtime campaigns, deployment, unrelated findings and submodule changes are outside this work.

</frozen-after-approval>

## Implementation Notes

- Plan assessment: no unresolved user intent; a bounded correction to the existing checker and subprocess fixtures, contributor guidance, this trace and five register entries. No new public API, dependency or runtime mutation. Commit/push is explicitly authorized.
- Baseline revision: `66c3c72b3814f2438603d2530a9c94d9b3110305`; clean `main` after the user's existing changes were pushed.
- Code map: `tools/check-story-review-readiness.py` owns `check_ledger`, `validate` C2 routing and canonical phase constants. Reuse placeholder normalization and existing table/status parsing. `_bmad/custom/story-phase-ledger.md` supplies the baseline, chronological repeated phases and blocked-evidence requirements; `CONTRIBUTING.md` describes the executable subset.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` contains subprocess CLI fixtures and live-format guards. Baseline command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 62/62 methods passed in 6.995s, exit 0. Add focused refusal and positive compatibility controls for the five gaps; update isolated row fixtures to include valid prerequisites.
- Investigation included independent read-only verification of all five gaps and 31 live ledger shapes. Repeat phases and later correct-course rows are legitimate. No application code or host composition changes are involved, so no runtime host launch is needed.

- Implemented C2 lifecycle prerequisites, review/done ledger and creation-baseline refusal, Date/Change placeholder checks and labelled blocker parsing. Labelled fields must occur once with non-placeholder values; emphasis, mixed-case labels, re-open spelling and command semicolons are supported. Contributor guidance documents the syntax and proof limits.
- Original-checker regression command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'` before checker edits — 72 methods, 97 failing negative subcases in 22.024s (2 missing-baseline, 6 missing-ledger, 4 phase-order, 60 Date/Change, 25 blocker-field/marker cases). All failures reproduce the intended gaps, with no runner errors. Baseline checker was the unchanged source at `66c3c72b3814f2438603d2530a9c94d9b3110305`.
- First implementation verification with the same suite plus `-v`: 72/72 methods passed in 50.308s. Two further positive/duplicate-field regression methods were then added; final verification is recorded below.

- Final pre-review verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 74/74 methods passed in 39.004s, exit 0. Comparable discovery: 62 -> 74, phase/cumulative delta +12 methods. The existing live C6 inventory remains unchanged. `git diff --check` passes. Exactly DW-608, DW-609, DW-610, DW-612 and DW-613 now record resolved dispositions, baseline-sensitive regressions and reopen triggers.

- Independent blind review returned five verified findings; all were patched within the planned checker/test scope. Masking Markdown code spans preserves metadata-like command text; generic metadata boundaries prevent unknown labels from disguising placeholder values. Affirmative matched records must start the cell (optional Cumulative prefix), explicit template markers fail C2, and done requires a final review after subsequent development/QA/course-correction work. Added seven regression methods and corrected the earlier done-status blocker fixture to place its complete blocker record in the final review row.

## Review Triage Log

- Medium, patched: stripping code delimiters before tokenization misread `python3 -c 'print("; owner: diagnostic")'` as a duplicate owner and could supply missing fields from command content. Offset-preserving code-span masking and positive/negative single/multiple-backtick controls cover both directions.
- Medium, patched: incidental matched text in command values or the word unmatched bypassed blocker checks. Anchored affirmative matching rejects both, while cumulative and code-formatted result controls still pass. Count arithmetic and evidence execution remain independent audit responsibilities.
- Medium, patched: explicit `<name>`, `<date>` and ellipsis template values passed non-placeholder checks. C2 now rejects full angle-bracket templates and ellipses across ledger/blocker fields, without imposing a date format.
- Medium, patched: unknown metadata labels merged into required values, letting `owner: TBD; note: follow up` pass. All metadata labels delimit fields outside code spans; the required owner stays a placeholder and fails, with a regression for each required label.
- Medium, patched: the done-status positive blocker fixture ended with unreviewed development. Done now requires the last canonical row to be code-review; repeated phases remain valid with a final review, and the fixture now records its complete blocker on that review row.

- Final post-review verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 81/81 methods passed in 48.117s, exit 0. Comparable discovery 62 -> 81: implementation +12 and review +7 methods, cumulative +19. All original format/no-op and live C6 guards still pass. `git diff --check` passes. All five selected register resolutions cite the final 81-method evidence; none of the five review findings was deferred.

- Independent reviewer rechecked all five patches and confirmed no unresolved defects in that scope, reporting 47 targeted read-only checks. Full retained-suite evidence remains the 81/81 method result above. No review finding was deferred. Commit message validated with repository commitlint before staging/commit; the active commit hook and post-commit validation will also run.
