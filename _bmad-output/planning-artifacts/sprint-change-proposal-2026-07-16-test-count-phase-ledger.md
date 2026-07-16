---
project: memories
date: 2026-07-16
status: approved
change_scope: minor
mode: batch
trigger: "Recurring dev/QA/review test-count drift and File List omissions"
approved_by: Administrator
approved_at: 2026-07-16
---

# Sprint Change Proposal — Phase-by-Phase Test Count and File List Ledger

## 1. Issue Summary

Story-bound BMAD workflows do not enforce one cumulative, phase-by-phase record of test-count changes and File List reconciliation across `create-story`, `dev-story`, QA gap-closure, and code review. Individual story instructions sometimes request the information, but the requirement is repeated as prose and is not consistently carried across phase boundaries.

The failure mode is established:

- Story 18.1 recorded `IntegrationTests` at 237 methods after development, then QA added two tests and raised the actual total to 239. Senior review had to repair a stale `237 / +1` record, add the missing QA row, and add two omitted files to the File List.
- The Epic 18 retrospective identifies stale test counts and File List omissions as recurring tracking-metadata drift caused by QA adding tests after the Dev Agent Record was written.
- Story 20.6 later omitted four changed files from its Dev Agent File List; review repaired the omissions.
- The generated create-story template currently ends at `### File List` and does not create a Change Log. Dev-story checks only for a summary, QA automation does not require a governing story update, and code review has no dedicated count/File List reconciliation layer.

The current approach therefore improves only when a story author remembers earlier retrospective advice. It does not provide an update-safe workflow control.

## 2. Impact Analysis

### Epic impact

- No product epic is invalidated, reopened, added, removed, or resequenced.
- Completed stories remain historical evidence and are not edited.
- Future story execution benefits from the control regardless of epic number.
- `sprint-status.yaml` requires no proposal-time change because no epic or story status changes.

### Story impact

- No existing story specification changes.
- Newly created stories gain a canonical Change Log before entering `ready-for-dev`.
- Story-bound development, QA gap-closure, and review runs must append their own ledger row and reconcile the File List before advancing status.
- Standalone QA work without a governing story continues to use its test summary, but must explicitly state that story-ledger reconciliation is not applicable. A run presented as story gap-closure must resolve the story or halt.

### Artifact conflicts

- **PRD:** No conflict or amendment. Product capabilities, MVP gates, and success metrics are unchanged.
- **Epics:** No scope or acceptance-criteria amendment. This is a cross-story execution control.
- **Architecture:** No product architecture amendment. The change affects repository-local BMAD workflow governance only.
- **UX:** Not applicable; no product interface or user journey changes.
- **Spec:** No planning `spec-*` artifact requires amendment.
- **CI/CD:** The existing CI job already runs `tests/tooling/bmad_customization`; only its fixture content changes.

### Technical and process impact

The implementation changes committed `_bmad/custom` overrides, not generated `.agents/skills/**` files. This follows the repository's existing update-safe customization pattern and prevents a BMad refresh from deleting the control.

Affected artifacts:

1. New `_bmad/custom/story-phase-ledger.md`.
2. Updated `_bmad/custom/bmad-create-story.toml`.
3. New `_bmad/custom/bmad-dev-story.toml`.
4. New `_bmad/custom/bmad-qa-generate-e2e-tests.toml`.
5. Updated `_bmad/custom/bmad-code-review.toml`.
6. Updated `tests/tooling/bmad_customization/bmad_customization_test.py`.
7. Updated `_bmad-output/process-notes/story-creation-lessons.md`.

Generated `.agents/skills/**` files, PRD, epics, architecture, UX, sprint status, and CI workflow files remain unchanged.

## 3. Recommended Approach

### Selected path: Direct Adjustment

Add one shared phase-ledger policy, inject it through the four team-owned skill customizations, and verify the resolved customizations in the existing CI fixture.

This is preferred because:

- the product plan remains valid;
- no rollback simplifies the issue;
- an MVP or epic replan would not address the process-control failure;
- one policy prevents four workflows from developing incompatible count semantics;
- repository-owned overrides survive skill refreshes;
- executable resolver fixtures detect missing or malformed workflow injection.

### Effort, risk, and timeline

- **Effort:** Low — policy, four small customization changes, one Python fixture update, one process lesson.
- **Implementation risk:** Low — no production code or generated skill changes.
- **Operational risk:** Low — enforcement applies only to story recordkeeping and status gates.
- **Timeline impact:** No backlog resequencing; expected as one focused developer change.
- **Scope classification:** Minor; direct Developer-agent implementation.

## 4. Detailed Change Proposals

### 4.1 Shared phase-ledger policy

Artifact: `_bmad/custom/story-phase-ledger.md` (new)

**OLD**

No centralized policy exists. Story-specific prose uses inconsistent phase labels and count formats. Some records state only a suite total, some state only a delta, and QA additions may not propagate into the File List.

**NEW**

Create an update-safe policy with these invariants:

1. Every story contains this table before `ready-for-dev`:

   ```markdown
   ## Change Log

   | Date | Phase | Change | Test count | File List reconciliation |
   | :--- | :---- | :----- | :--------- | :----------------------- |
   ```

2. Canonical phase names are exactly:

   - `create-story`
   - `dev-story`
   - `qa-gap-closure`
   - `code-review`

3. Each `Test count` cell records:

   - phase delta: tests added and removed, including a zero delta;
   - cumulative story delta from the create-story baseline;
   - affected assembly/project discovery totals before and after when available;
   - whether the runner reported methods, cases, or another explicitly named unit;
   - the evidence command, or an exact blocked-evidence record.

4. Count rules:

   - never infer counts from prose, task checkboxes, or the number of edited files;
   - use runner discovery/list output where supported;
   - if a test is strengthened without changing discovery count, record `phase delta +0` and describe the strengthened behavior in `Change`;
   - if evidence cannot run, record the exact command, blocker, owner, consequence, and reopen trigger instead of inventing a number.

5. Phase semantics:

   - `create-story`: record `actual +0`, planned tests when quantified, and baseline discovery totals or the reason baseline discovery was not run;
   - `dev-story`: record actual added/removed tests, cumulative delta, post-development discovery totals, and commands;
   - `qa-gap-closure`: calculate its delta from the dev row, recompute the cumulative delta/totals, and update all prior stale narrative references;
   - `code-review`: independently verify the cumulative counts against live discovery and the reviewed diff; record a zero or non-zero review-patch delta.

6. File List reconciliation:

   - compare the phase's in-scope changed-file set against the story File List;
   - include every added, modified, deleted, or renamed implementation, test, documentation, summary, and tracked workflow artifact within the story scope;
   - do not absorb unrelated dirty-worktree changes;
   - list any policy-approved excluded session/generated artifact with an explicit reason;
   - record `matched N/N`, followed by any exclusions, in the phase row.

7. Status gate:

   - a story cannot advance to `review` or `done` while counts disagree, a required phase row is missing, or the File List does not match the in-scope changed-file set.

**Rationale:** One cumulative contract gives all phases the same vocabulary, authoritative evidence source, and fail-closed handoff behavior.

### 4.2 Create-story customization

Artifact: `_bmad/custom/bmad-create-story.toml` (update)

**OLD**

The customization loads only the historical-slice policy and lessons, then adds `HISTORICAL_SLICE_GUARD`. The generated story template has no Change Log section.

**NEW**

- Add `file:{project-root}/_bmad/custom/story-phase-ledger.md` to `workflow.persistent_facts`.
- Append exactly one `STORY_PHASE_LEDGER:` activation directive requiring the workflow to:
  - create the canonical Change Log table if absent;
  - append the `create-story` row;
  - record actual `+0`, planned test count/range when known, baseline discovery evidence or a precise not-run reason;
  - reconcile the initial File List for story-creation artifacts;
  - fail closed before `ready-for-dev` and sprint-status mutation if the row or table is missing.
- Preserve the existing historical-slice directive unchanged.

**Rationale:** The ledger must exist before later phases can calculate deltas reliably.

### 4.3 Dev-story customization

Artifact: `_bmad/custom/bmad-dev-story.toml` (new)

**OLD**

Dev-story requires a File List and generic Change Log summary, but does not require an exact phase delta, cumulative count, discovery total, or reconciliation result.

**NEW**

- Load the shared phase-ledger policy as a persistent fact.
- Add one `STORY_PHASE_LEDGER:` activation directive requiring the workflow, before setting status to `review`, to:
  - read the create-story row;
  - obtain actual post-development discovery counts;
  - append a `dev-story` row with phase and cumulative deltas plus evidence;
  - reconcile every in-scope changed file against the File List;
  - update any stale count references in permitted Dev Agent Record sections;
  - halt status advancement if the row, evidence, counts, or File List are inconsistent.

**Rationale:** Dev hands QA an explicit, reproducible baseline instead of an unstructured summary.

### 4.4 QA gap-closure customization

Artifact: `_bmad/custom/bmad-qa-generate-e2e-tests.toml` (new)

**OLD**

QA writes `tests/test-summary.md`, but its base workflow does not discover or update the governing story, its Change Log, or its File List.

**NEW**

- Load the shared phase-ledger policy as a persistent fact.
- Add one `STORY_PHASE_LEDGER:` activation directive with two explicit modes:
  - **Story-bound gap-closure:** resolve the story from the explicit request or sprint context; capture the dev-row baseline; after test generation, append `qa-gap-closure`, recalculate cumulative counts/totals, update stale count prose, add all QA-touched tests/docs/summaries to the File List, and halt if reconciliation fails.
  - **Standalone QA:** when no governing story exists and the work is not represented as story gap-closure, keep the test summary as the authoritative artifact and state `story phase ledger: N/A — standalone QA`.
- A run represented as story gap-closure must halt before changes if it cannot resolve a governing story.

**Rationale:** QA is the phase demonstrated to invalidate development counts and introduce File List omissions.

### 4.5 Code-review customization

Artifact: `_bmad/custom/bmad-code-review.toml` (update)

**OLD**

The customization adds the historical-slice review layer. Review may repair tracking drift incidentally, but no layer systematically checks phase rows, cumulative arithmetic, live discovery evidence, or File List completeness.

**NEW**

- Load the shared phase-ledger policy as a persistent fact.
- Append one `STORY_PHASE_LEDGER:` activation directive requiring full/spec review to append a `code-review` row after review actions and before status synchronization.
- Add a keyed review layer:

  ```toml
  [[workflow.review_layers]]
  id = "story-phase-ledger"
  name = "Story Phase Ledger Auditor"
  when = 'Only when {review_mode} = "full".'
  ```

- The layer audits:
  - presence and order of required phase rows;
  - phase and cumulative count arithmetic;
  - counts against available live discovery evidence;
  - reviewed-diff paths against the story File List;
  - stale count statements elsewhere in permitted story-record sections;
  - exact blocked-evidence records when commands could not run.
- Route unambiguous ledger/File List repairs to `patch`; route ambiguous scope or exclusion decisions to `decision_needed`.
- Prevent `done` when a count mismatch, missing row, or unaccounted in-scope file remains.
- Preserve the existing historical-slice layer and all default review layers.

**Rationale:** Review becomes an independent verifier rather than the first phase to notice drift manually.

### 4.6 Resolved-customization fixtures

Artifact: `tests/tooling/bmad_customization/bmad_customization_test.py` (update)

**OLD**

Two tests verify the historical-slice customizations for create-story and code-review only.

**NEW**

- Add constants for the phase-ledger policy fact and `STORY_PHASE_LEDGER:` marker.
- Resolve all four affected workflows.
- Assert each resolved workflow contains the policy fact and exactly one ledger directive.
- Assert create-story retains the historical-slice facts/directive.
- Assert code-review contains exactly one `story-phase-ledger` layer alongside the four defaults and the historical-slice layer.
- Assert the review layer references `{spec_file}`, `{diff_output}`, the shared policy, count reconciliation, File List reconciliation, and fail-closed routing.
- Assert QA contains both story-bound and standalone semantics and fails closed for unresolved story gap-closure.
- Assert dev-story gates transition to `review` on successful reconciliation.

The implementing developer records the fixture test-count delta in the implementation change's own phase ledger; the proposal does not predeclare a false exact count before test design is finalized.

**Rationale:** CI already executes this fixture, so no CI workflow modification is needed.

### 4.7 Durable process lesson

Artifact: `_bmad-output/process-notes/story-creation-lessons.md` (update)

**OLD**

The lessons ledger explains that team rules belong in `_bmad/custom` rather than generated skills, but does not define the cross-phase count/File List contract.

**NEW**

Add `L10 - Story phase records are cumulative handoff contracts`:

- create-story establishes the baseline;
- dev-story records actual implementation delta;
- QA gap-closure recalculates rather than appending stale prose;
- review independently verifies counts and File List completeness;
- all enforcement lives in committed customizations and resolver fixtures;
- generated `.agents/skills/**` files remain untouched.

**Rationale:** Future workflow changes retain the reason behind the enforcement rather than only its mechanics.

## 5. Implementation Handoff

### Scope and recipient

- **Classification:** Minor.
- **Route to:** Developer agent.
- **Coordination:** No backlog reorganization, Product Manager decision, or Architect decision required.

### Developer responsibilities

1. Implement the shared policy and four update-safe customizations exactly as approved.
2. Extend the existing resolver fixture without editing generated skill files.
3. Resolve each affected workflow and inspect the merged JSON.
4. Run:

   ```bash
   python3 -m unittest discover -s tests/tooling/bmad_customization -p "*_test.py"
   ```

5. Demonstrate one synthetic story lifecycle or fixture scenario where QA adds tests after dev and review sees the updated cumulative count and File List.
6. Record the implementation's own test counts and File List reconciliation using the new policy.

### Success criteria

- Each of the four resolved workflows loads the same shared policy and contains exactly one ledger directive.
- A newly created story contains a canonical `create-story` row before `ready-for-dev`.
- Dev-story cannot advance to `review` without an exact development row and File List reconciliation.
- Story-bound QA gap-closure appends its row, recalculates cumulative totals, and adds every QA-touched file.
- Full code review independently audits the ledger/File List, appends its row, and cannot mark the story `done` with unresolved drift.
- Standalone QA remains supported and explicitly marks story-ledger reconciliation not applicable.
- Existing historical-slice customizations and default review layers remain intact.
- The customization fixture passes in the existing CI job.
- No generated `.agents/skills/**`, PRD, epics, architecture, UX, or sprint-status file is changed.

## 6. Checklist Completion and Approval State

### Understand the trigger and context

- [x] 1.1 Trigger identified: Story 18.1 and the Epic 18 retrospective provide the canonical incident; Story 20.6 confirms recurrence.
- [x] 1.2 Core problem defined: a repeated prose convention failed to become an update-safe cross-phase control.
- [x] 1.3 Evidence collected: stale counts and File List omissions with review repairs.

### Epic impact assessment

- [x] 2.1 Existing epics remain completable/complete as planned.
- [N/A] 2.2 No epic-level scope change.
- [x] 2.3 Future epics benefit without specification changes.
- [N/A] 2.4 No obsolete or new product epic.
- [N/A] 2.5 No priority or sequence change.

### Artifact conflict analysis

- [N/A] 3.1 No PRD conflict.
- [N/A] 3.2 No product architecture conflict.
- [N/A] 3.3 No UX conflict.
- [x] 3.4 Workflow governance, customization fixtures, and durable process notes require changes.

### Path forward

- [x] 4.1 Direct Adjustment viable — low effort, low risk.
- [N/A] 4.2 Rollback not viable or useful.
- [N/A] 4.3 MVP review not required.
- [x] 4.4 Direct Adjustment selected.

### Proposal and handoff

- [x] 5.1 Issue summary complete.
- [x] 5.2 Impact analysis complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP unaffected; action plan defined.
- [x] 5.5 Developer handoff defined.
- [x] 6.1 Applicable checklist items reviewed.
- [x] 6.2 Proposal checked for consistency and actionable changes.
- [x] 6.3 Explicit final user approval received from Administrator on 2026-07-16.
- [N/A] 6.4 No sprint-status epic/story changes.
- [x] 6.5 Minor-scope implementation handoff routed to the Developer agent.

## 7. Handoff Execution Log

| Date | Scope | Routed to | Deliverables | Status |
| :--- | :---- | :-------- | :----------- | :----- |
| 2026-07-16 | Minor | Developer agent | Approved shared ledger policy; four workflow customization edits; resolver-fixture updates; durable process lesson; validation command and success criteria | Ready for implementation |

Approval condition: implementation must preserve generated `.agents/skills/**` files and apply the change only through committed `_bmad/custom` overrides and their verification fixture.
