---
title: 'Track test counts in the story phase Change Log'
type: 'feature'
created: '2026-07-16'
status: 'done'
baseline_commit: 'c28a1d8ce0459abb713df9f029a028efa578702d'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-test-count-phase-ledger.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story test counts and File Lists drift when development, QA gap-closure, and review update different prose records. The approved proposal is committed, but its enforcement artifacts are absent.

**Approach:** Add one update-safe phase-ledger policy and inject it through committed create-story, dev-story, QA, and code-review customizations, with resolver fixtures and a durable process lesson.

## Boundaries & Constraints

**Always:** Use committed `_bmad/custom` overrides; preserve existing historical-slice directives and review layers unchanged; use exact phase names `create-story`, `dev-story`, `qa-gap-closure`, and `code-review`; record runner-derived phase/cumulative counts, named units, commands or precise blockers, and cumulative in-scope File List reconciliation; fail closed before `ready-for-dev`, `review`, or `done` when required evidence disagrees.

**Ask First:** Any change to the customization resolver, generated skill source, CI workflow, canonical phase names, or policy-approved interpretation of ambiguous story scope/exclusions.

**Never:** Edit `.agents/skills/**`, product code, PRD, epics, architecture, UX, sprint status, completed historical stories, or unrelated dirty-worktree files; infer counts from prose, checkboxes, or file totals; mix discovery units in arithmetic.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Story lifecycle | Create baseline, dev changes tests, optional QA adds tests/files, review patches | Ordered rows retain per-unit phase and cumulative deltas; each row reconciles the cumulative story-scoped File List | Status advancement halts on missing rows, stale totals, or unaccounted files |
| Strengthened test | Existing test behavior changes without discovery-count change | Row records `phase delta +0` and describes strengthened behavior | Never claim an added test |
| Blocked discovery | Runner cannot list/discover tests | Row records command, blocker, owner, consequence, and reopen trigger | Never invent a count |
| Standalone QA | No governing story and work is not represented as story gap-closure | Test summary states `story phase ledger: N/A — standalone QA` | Represented gap-closure with unresolved story halts before edits |

</frozen-after-approval>

## Code Map

- `_bmad/custom/story-phase-ledger.md` -- shared ledger schema, evidence semantics, repeated-phase rules, and status gates.
- `_bmad/custom/bmad-create-story.toml` -- create-story baseline and pre-ready gate while retaining the historical-slice guard.
- `_bmad/custom/bmad-dev-story.toml` -- development evidence and pre-review gate.
- `_bmad/custom/bmad-qa-generate-e2e-tests.toml` -- story-bound versus standalone QA behavior.
- `_bmad/custom/bmad-code-review.toml` -- independent ledger auditor and post-patch/pre-status final row.
- `tests/tooling/bmad_customization/bmad_customization_test.py` -- resolved-workflow and synthetic lifecycle fixture.
- `_bmad-output/process-notes/story-creation-lessons.md` -- durable cross-phase handoff lesson.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad/custom/story-phase-ledger.md` -- define the canonical table, same-unit arithmetic, optional/repeated phase behavior, blocked evidence, cumulative File List matching, and fail-closed gates.
- [x] `_bmad/custom/bmad-create-story.toml` -- load the policy and carry one ledger obligation through baseline creation and both ready-for-dev writes.
- [x] `_bmad/custom/bmad-dev-story.toml` -- load the policy and require discovery, stale-reference repair, ledger append, and File List reconciliation before review status changes.
- [x] `_bmad/custom/bmad-qa-generate-e2e-tests.toml` -- load the policy and enforce story-bound gap-closure or explicit standalone mode.
- [x] `_bmad/custom/bmad-code-review.toml` -- preserve existing layers, add one full-review auditor, and append/reconcile the final row after actions but before status sync.
- [x] `tests/tooling/bmad_customization/bmad_customization_test.py` -- resolve all four workflows and cover exact-one injection, preserved layers, phase gates, two QA modes, and dev-to-QA-to-review count/File List propagation.
- [x] `_bmad-output/process-notes/story-creation-lessons.md` -- add L10 explaining cumulative phase handoffs and update-safe ownership.
- [x] `_bmad-output/implementation-artifacts/spec-track-test-counts-in-story-phase-change-log.md` -- dogfood the implementation phase by recording the development ledger evidence and cumulative File List; the review workflow owns the later `code-review` row.

**Acceptance Criteria:**
- Given any affected resolved workflow, when customization is resolved, then it contains the shared policy fact and exactly one `STORY_PHASE_LEDGER:` directive.
- Given create-story and code-review resolution, when the new overrides merge, then all historical-slice behavior and the four default review layers remain present exactly once.
- Given full code review, when ledger findings are triaged and patches finish, then unambiguous repairs route to patch, ambiguous scope routes to decision-needed, and `done` is blocked until the final row and File List reconcile.
- Given the focused fixture command, when it completes, then all discovered unittest methods pass and the synthetic dev-to-QA-to-review scenario proves updated cumulative counts and File List membership.
- Given the completed diff, when paths are inspected, then no generated skill, CI, product-planning, sprint-status, or unrelated worktree file is changed.

## Change Log

| Date | Phase | Change | Test count | File List reconciliation |
| :--- | :---- | :----- | :--------- | :----------------------- |
| 2026-07-16 | create-story | Established the implementation baseline from the approved proposal and independent repository audit. | actual phase `+0`; cumulative story `+0`; baseline and observed final `2` Python unittest methods passed; unit: test methods; runner/scope: Python unittest discovery in `tests/tooling/bmad_customization`, pattern `*_test.py`; evidence: `python3 -m unittest discover -s tests/tooling/bmad_customization -p '*_test.py' -v` | matched 1/1; comparison baseline `c28a1d8ce0459abb713df9f029a028efa578702d`; evidence: scoped name-status inspection; excluded `_bmad-output/implementation-artifacts/sprint-status.yaml` (user-owned Epic 26 completion alignment) |
| 2026-07-16 | dev-story | Added the shared policy, four resolved-workflow obligations, the full-review auditor, synthetic lifecycle and edge-case fixtures, and durable L10 guidance; strengthened the existing review-layer test to prove the pre-triage auditor does not require its not-yet-appended current review row. | actual phase `+6`; verification hardening delta `+0`; create baseline `2`; cumulative story `+6`; external same-suite `+3` owned by `_bmad-output/implementation-artifacts/spec-strengthen-story-creation-review-historical-slice-templates.md`; observed phase `5 -> 11`; observed final `11`; unit: Python unittest methods; runner/scope: Python unittest discovery in `tests/tooling/bmad_customization`, pattern `*_test.py`; equation `2 + 6 + 3 = 11`; evidence: `python3 -m unittest discover -s tests/tooling/bmad_customization -p '*_test.py' -v` | matched 8/8; comparison baseline `c28a1d8ce0459abb713df9f029a028efa578702d`; evidence: scoped File List/name-status inspection; excluded `_bmad-output/implementation-artifacts/spec-repository-line-ending-policy.md` and its repository-wide normalization (concurrent line-ending workflow), plus `_bmad-output/implementation-artifacts/spec-strengthen-story-creation-review-historical-slice-templates.md` and its shared fixture/lesson deltas (concurrent historical-slice workflow) |
| 2026-07-16 | code-review | Patched review findings for comparable repeated-phase arithmetic, explicit external drift, policy adoption, writable record boundaries, reproducible rename/File List evidence, complete chunk gating, QA/review contract assertions, untracked-file whitespace checks, and dogfood arithmetic verification. | actual phase `+1`; create baseline `2`; cumulative story `+7`; external same-suite `+5` owned by `_bmad-output/implementation-artifacts/spec-strengthen-story-creation-review-historical-slice-templates.md` (`+3` before development and `+2` during review); observed phase `11 -> 14`; observed final `14`; unit: Python unittest methods; runner/scope: Python unittest discovery in `tests/tooling/bmad_customization`, pattern `*_test.py`; equation `2 + 7 + 5 = 14`; evidence: `python3 -m unittest discover -s tests/tooling/bmad_customization -p '*_test.py' -v` | matched 8/8; comparison baseline `c28a1d8ce0459abb713df9f029a028efa578702d`; evidence: scoped File List/name-status inspection after patches; excluded `_bmad-output/implementation-artifacts/spec-repository-line-ending-policy.md` and its repository-wide normalization (concurrent line-ending workflow), plus `_bmad-output/implementation-artifacts/spec-strengthen-story-creation-review-historical-slice-templates.md`, `tests/tooling/bmad_customization/historical_slice_guard_test.py`, and their shared fixture/lesson deltas (concurrent historical-slice workflow) |

## File List

- `_bmad/custom/story-phase-ledger.md`
- `_bmad/custom/bmad-create-story.toml`
- `_bmad/custom/bmad-dev-story.toml`
- `_bmad/custom/bmad-qa-generate-e2e-tests.toml`
- `_bmad/custom/bmad-code-review.toml`
- `tests/tooling/bmad_customization/bmad_customization_test.py`
- `_bmad-output/process-notes/story-creation-lessons.md`
- `_bmad-output/implementation-artifacts/spec-track-test-counts-in-story-phase-change-log.md`

## Spec Change Log

- 2026-07-16 -- The sequential completion audit found that a task requiring a future review-phase write could not truthfully close before Step 3 ended. The task now closes on the implemented development row and reconciled File List; the frozen lifecycle expectation, shared policy, review directive, and Design Notes still require the post-action `code-review` row. This avoids either a false incomplete implementation task or a prematurely fabricated review row. KEEP the independent post-patch review evidence and fail-closed `done` gate.

## Design Notes

Activation directives are carried obligations because customization activation occurs before story or diff discovery. The review layer audits existing rows and emits findings before triage; the parent directive appends the current `code-review` row only after selected patches and before status determination. QA rows are optional unless gap-closure occurred, repeated phase cycles append new rows, and arithmetic is performed only within the same named discovery unit.

## Verification

**Commands:**
- `python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py" -v` -- expected: every discovered unittest method passes with no failures, errors, or skips.
- `for skill in bmad-create-story bmad-dev-story bmad-qa-generate-e2e-tests bmad-code-review; do python3 _bmad/scripts/resolve_customization.py --skill ".agents/skills/$skill" --key workflow; done` -- expected: valid JSON for all four workflows, one ledger directive each, and preserved default/historical layers.
- `git diff --check` -- expected: no whitespace errors in this change.
- `git diff --no-index --check /dev/null _bmad/custom/story-phase-ledger.md` -- expected: exit 1 for a clean full-file addition and no whitespace diagnostics.
- `git diff --no-index --check /dev/null _bmad/custom/bmad-dev-story.toml` -- expected: exit 1 for a clean full-file addition and no whitespace diagnostics.
- `git diff --no-index --check /dev/null _bmad/custom/bmad-qa-generate-e2e-tests.toml` -- expected: exit 1 for a clean full-file addition and no whitespace diagnostics.
- `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-track-test-counts-in-story-phase-change-log.md` -- expected: exit 1 for a clean full-file addition and no whitespace diagnostics.

## Suggested Review Order

**Shared ledger contract**

- Start with the canonical schema and append-only phase model.
  [`story-phase-ledger.md:7`](../../_bmad/custom/story-phase-ledger.md#L7)

- Verify comparable discovery, external drift, and blocked-evidence arithmetic.
  [`story-phase-ledger.md:24`](../../_bmad/custom/story-phase-ledger.md#L24)

- Check adoption, writable boundaries, and repeated-phase ownership.
  [`story-phase-ledger.md:58`](../../_bmad/custom/story-phase-ledger.md#L58)

- Confirm reproducible File List equality and fail-closed status gates.
  [`story-phase-ledger.md:91`](../../_bmad/custom/story-phase-ledger.md#L91)

**Phase enforcement**

- Creation establishes evidence before either ready-for-dev mutation.
  [`bmad-create-story.toml:14`](../../_bmad/custom/bmad-create-story.toml#L14)

- Development captures comparable snapshots before review status changes.
  [`bmad-dev-story.toml:11`](../../_bmad/custom/bmad-dev-story.toml#L11)

- QA separates governed gap-closure from explicit standalone operation.
  [`bmad-qa-generate-e2e-tests.toml:11`](../../_bmad/custom/bmad-qa-generate-e2e-tests.toml#L11)

- Review finalizes only complete post-patch evidence and chunks.
  [`bmad-code-review.toml:14`](../../_bmad/custom/bmad-code-review.toml#L14)

- The independent auditor verifies pre-triage ledger and diff consistency.
  [`bmad-code-review.toml:41`](../../_bmad/custom/bmad-code-review.toml#L41)

**Verification and handoff**

- Resolver coverage pins exactly one directive across all workflows.
  [`bmad_customization_test.py:92`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L92)

- Synthetic cycles prove QA propagation and repeated-phase arithmetic.
  [`bmad_customization_test.py:186`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L186)

- Dogfood validation catches story/external count-equation drift.
  [`bmad_customization_test.py:231`](../../tests/tooling/bmad_customization/bmad_customization_test.py#L231)

- The implementation ledger records phase counts and named exclusions.
  [`spec-track-test-counts-in-story-phase-change-log.md:68`](spec-track-test-counts-in-story-phase-change-log.md#L68)

- L10 preserves the cross-phase ownership rationale for future refreshes.
  [`story-creation-lessons.md:39`](../process-notes/story-creation-lessons.md#L39)
