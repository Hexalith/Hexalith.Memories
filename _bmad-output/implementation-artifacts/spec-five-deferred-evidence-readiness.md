---
title: 'Close five deferred evidence readiness gaps'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete DW-601, DW-602, DW-603, DW-607 and DW-615 through the existing story review-readiness gate. Given an artifact at review or done, when an evidence row claims completion, then require a recognized completed state and a real ISO calendar date in its Completion date column or inline review-status text. Given a blocked disposition, when review is claimed, then require named owner, consequence and reopen-trigger values; at done require a date as well. Given an unknown, pending or malformed state, when review or done is claimed, then reject it with a row-specific C6 diagnostic. Preserve the existing below-review skip and both evidence-status header spellings.

Given committed or staged renames, when cumulative or prepared CI paths are collected, then retain both source and destination paths for exact File List reconciliation. Keep the existing newline path interface, represent rename endpoints as separate exact paths, and preserve the documented default-branch cumulative skip. Given configured hook and CI entry points, when isolated fixtures execute them, then demonstrate both passing records and refusal propagation; the hook derives cumulative changes and CI uses its prepared range.

Reuse current policy, parsing and fixture infrastructure without a new dependency or runtime campaign. Document the executable evidence vocabulary and rename representation; resolve exactly the five selected ledger entries with verification evidence. Historical artifact violations exposed by stricter checks remain visible rather than being silently grandfathered. Independently review the implementation, then commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Route assessment: one bounded developer-tool correction; no unresolved intent, new runtime API, dependency, data mutation or deployment. Commit/push explicitly authorized. No application/AppHost changes, so verification uses tooling fixtures.
- Baseline: clean main at `35ef40e3fdcc7a490e04cf49e6a532fd5bf32285`, fast-forwarded to the fetched remote before edits. Read repository guidance, declared AI.Tools baseline/Git rules, EditorConfig/attributes, build configuration, contributor guidance, architecture constraints and canonical story-phase-ledger policy.
- Code map: `tools/check-story-review-readiness.py` owns C6 table metadata, labelled blocker parsing and Git-derived C1 paths. Extend the existing blocker parser with configurable required fields. Retain status/date column context without changing public CLI inputs. Completed states: complete, completed, reviewed, verified, passed, applied. Accountable dispositions: blocked, waived, deferred, accepted, not applicable, n/a. Match states at the start with a token boundary; permit existing dated explanatory suffixes.
- Existing `tests/tooling/story_review_readiness/story_review_readiness_test.py` provides CLI, disposable Git and live-format fixtures. Add evidence and rename regressions there, and executable entry-point fixtures in the same discovery directory. Execute the actual hook and extracted CI shell commands in disposable repositories with unrelated gates isolated. Never mutate the working repository in fixtures.
- `.githooks/commit-msg` and `.github/workflows/ci.yml` collect shared changed paths; add `--no-renames` there and in checker Git diffs to preserve both endpoints. `CONTRIBUTING.md` explains the resulting path-set and evidence contract. Update only the five selected dispositions in deferred-work.md and this spec trace.
- Baseline command: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 81/81 test methods passed in 13.394s, exit 0. All changes are freeform work with no sprint-status row. Implementation/review evidence follows below.

- Implemented the five gaps with reusable per-cell labelled-field parsing, evidence table column context, ISO calendar checks, explicit status vocabularies and rename-safe Git inventories. Dedicated Completion date and inline dates are accepted; arbitrary proof dates do not qualify. Existing ledger checks retain their full field requirements.
- Verification: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'` — 97/97 methods passed in 29.605s, exit 0. Comparable discovery 81 -> 97 (+16 methods). Initial execution exposed the expected historical-inventory assertion and one transient subprocess failure with empty stdout; stderr is now retained by the CLI fixture and the full rerun passed.
- Shared collector compatibility: story_scope suite 71/71 methods passed in 9.147s; tenant_isolation_evidence suite 41/41 passed in 4.596s. Both use the same `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s tests/tooling/<directory> -p '*_test.py'` invocation. `git diff --check` passes.
- The live C6 guard now records ten historically violating artifacts (previously two). Additional failures reflect incomplete blockers, noncanonical status prose, missing dates and an existing unescaped shell-pipe cell. No historical evidence record or sprint status was rewritten, and no new executed-proof claim was made.
- Resolved only DW-601, DW-602, DW-603, DW-607 and DW-615 with the implementation and retained-fixture evidence above. Independent review and final verification follow.

- Original-code sensitivity probe: nine selected current regression methods executed against the unchanged baseline checker/hook/CI files yielded 43 expected failing subcases and zero runner errors in 2.788s. Isolated mutations removing the hook cumulative flag and adding it to CI were both caught. Follow-up mutations removing CI branch/message metadata arguments and injecting `if: false` or `continue-on-error: true` were also caught; all used disposable copies.
- Review patches strengthened owner agreement and duplicate-column refusal, excluded template/scheduled/metadata dates, rejected explicit incomplete status clauses, and retained ISO timestamps. Git collectors now set `core.quotepath=false`; existing rename methods now cover accented and Japanese endpoints. Entry-point fixtures now cover independent metadata sources and the configured CI execution/failure policy.
- Post-review verification: the retained readiness suite passed 103/103 methods in 20.183s, exit 0. Discovery 81 -> 103: implementation +16 and review +6, cumulative +22 methods. Existing rename methods were strengthened without changing their discovery count. All five ledger resolutions now cite this result. Actionlint, hook shell syntax, commitlint policy fixtures (3/3), exact eight-file scope and whitespace checks also pass.

- Final follow-up: review confirmed all seven original patches, then found an inline-date false negative introduced by metadata filtering. Explicit `completion date:` and `completed:` values now qualify independently of unrelated metadata. The final retained suite passed 104/104 methods in 17.113s (81 -> 104, implementation +16, review +7, cumulative +23). Five selected ledger dispositions cite this final result.
- Independent scoped recheck reports no known issue remaining after four focused tests and nine direct date controls. No configured review layer was skipped and no review finding was deferred. Fresh fetch confirmed HEAD and origin/main still match the canonical baseline; Actionlint, shell syntax, pre-commit commitlint and whitespace validation pass. Repository-managed hooks will run for the commit.

## File Scope

Allowed files for this story:

- `tools/check-story-review-readiness.py`
- `tests/tooling/story_review_readiness/story_review_readiness_test.py`
- `tests/tooling/story_review_readiness/readiness_entrypoints_test.py`
- `.githooks/commit-msg`
- `.github/workflows/ci.yml`
- `CONTRIBUTING.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-five-deferred-evidence-readiness.md`

## Review Triage Log

- Medium, patched: a named column owner could disagree with an explicit owner label, and duplicate owner columns overwrote one another. Contradictions and duplicate owner columns now fail; matching and explicit-owner-with-placeholder-column controls pass.
- Medium, patched: only the first review-status column was checked. Multiple recognized status columns now fail as ambiguous, including a hidden pending state and even duplicate completed declarations.
- Medium, patched: template and scheduled dates counted as completion. Angle-bracket dates, planned/TBD dates and dates in labelled metadata no longer qualify; real dates, leap days and ISO timestamps retain passing controls.
- Medium, patched: a completed prefix hid an explicit pending/blocked clause. Initial incomplete qualifiers and semicolon/dash/parenthesized incomplete clauses now fail; ordinary explanatory suffixes remain supported.
- Medium, patched: Git Unicode path quoting corrupted rename endpoints. Checker, hook and CI collectors disable Unicode quoting; committed/staged and PR/push fixtures execute accented and Japanese paths with source-omission refusal.
- Medium, patched: agreeing branch/trailer fixtures did not detect lost metadata forwarding. Both entry points now execute branch-only, trailer-only, conflict and bypass cases; removing either CI metadata argument fails the isolated mutation probe.
- Medium, patched: extracting only CI run commands missed disabled/soft-failed enforcement. A configuration contract now pins the existing PR/non-main-push job condition and rejects step conditions and continue-on-error at step/job level. Mutations disabling the step or suppressing its failures are caught. Actual GitHub scheduling is not executed locally.

- Medium, patched: follow-up review found metadata filtering rejected explicit inline completion-date labels. Labelled completion dates now qualify while template, scheduled and reopen-trigger-only dates still fail; completed and blocked positive regressions pass. Independent recheck found no remaining issue in that scope.
