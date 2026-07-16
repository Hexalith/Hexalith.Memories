---
project: memories
date: 2026-07-16
status: approved
change_scope: minor
approval: approved
requested_by: Administrator
approved_by: Administrator
---

# Sprint Change Proposal — Reconcile Story 19.4 migration-marker target-consistency action

## 1. Issue Summary

The Epic 19 retrospective action to carry Story 19.4's migration-marker target-consistency cluster into Epic 21 planning is still marked `open` in `sprint-status.yaml` even though Stories 21.9 and 21.10 were created, completed, and explicitly incorporated the cluster.

The cluster consists of Story 19.4 residuals `15.3-RV15`, `15.3-RV16`, and `15.3-RV27`:

- Reject starting a migration when the tenant already has an active marker for another target.
- Reject completion when the active marker does not belong to the completing target.
- Reject resume when the active marker no longer references the requested target.

Repository evidence shows that the requested carry-forward is complete:

- Story 21.9 acceptance criterion 6 and its implementation tasks map the cluster to fail-closed start, resume, completion, abort, locking, TTL, and heartbeat behavior.
- Story 21.10 maps the same active/per-target marker invariants to migration subsystem tests, including target and owner mismatches and the completed-marker end state.
- The Epic 21 retrospective records the carry-forward action as completed.
- A duplicate follow-through record under Epic 20 is already marked `done` with Stories 21.9 and 21.10 as its evidence.

The problem is therefore a tracking inconsistency, not missing planning or implementation.

## 2. Impact Analysis

### Epic and story impact

- Epic 19 remains complete; only its retrospective-action status is stale.
- Stories 21.9 and 21.10 remain complete and unchanged.
- Epic 21 remains complete; its retrospective already confirms the action's completion.
- No story needs to be created, reopened, resequenced, or re-scoped.
- No current or future epic is invalidated or delayed.

### Artifact conflicts

- **PRD:** No conflict or change. Functional and non-functional scope is unaffected.
- **Epics:** No change. The detailed Story 21.9 and 21.10 artifacts already contain the required planning.
- **Architecture:** No conflict or change. The migration-marker consistency behavior is already expressed through the completed story work.
- **UX:** Not applicable; no user flow or interface changes.
- **Sprint tracking:** One stale `open` status in `sprint-status.yaml` must be reconciled to `done`.
- **Implementation and tests:** No changes. The proposal records existing closure evidence only.

### Delivery impact

- **Effort:** Low; one tracking status edit after approval.
- **Risk:** Low; the edit aligns status with completed, independently recorded work.
- **Timeline impact:** None.
- **Release impact:** None.
- **MVP impact:** None; product scope and readiness do not change.

## 3. Recommended Approach

Use **Direct Adjustment**: mark the original Epic 19 retrospective action `done` and attach the existing Story 21.9, Story 21.10, and Epic 21 retrospective evidence in a YAML comment.

Rollback, story reopening, and PRD/MVP re-planning are not warranted because the requested planning and delivery are already complete. Leaving the row open would preserve contradictory tracking state and could cause the same closed action to be carried forward again.

## 4. Detailed Change Proposal

### Sprint tracking reconciliation

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Old text:**

```yaml
  - epic: 19
    action: "Carry the Story 19.4 migration-marker target-consistency cluster into Epic 21 planning when stories 21.9 and 21.10 are created"
    owner: "Winston, Amelia, Murat"
    status: open
```

**Proposed new text:**

```yaml
  - epic: 19
    action: "Carry the Story 19.4 migration-marker target-consistency cluster into Epic 21 planning when stories 21.9 and 21.10 are created"
    owner: "Winston, Amelia, Murat"
    status: done  # 2026-07-05 (Stories 21.9/21.10): 15.3-RV15/RV16/RV27 mapped to fail-closed active-target start/resume/completion behavior and migration coverage; Epic 21 retrospective confirms completion.
```

**Rationale:** The action's success condition was met when Stories 21.9 and 21.10 were planned with the target-consistency cluster and subsequently completed. The Epic 21 retrospective independently confirms closure. Updating the originating row removes the remaining contradiction without changing scope or behavior.

### Explicit no-change decisions

- Do not modify the PRD, architecture, UX specification, or `epics.md`.
- Do not alter Stories 21.9 or 21.10; their current records already provide the required traceability.
- Do not add code or tests; this proposal does not identify an implementation gap.
- Preserve the completed follow-through evidence already recorded in the Epic 21 retrospective.

## 5. Implementation Handoff

**Classification:** Minor.

**Recipient:** Developer or planning-artifact maintainer.

**Responsibilities after explicit approval:**

- Apply only the proposed `status: done` reconciliation and evidence comment in `sprint-status.yaml`.
- Preserve unrelated working-tree and planning-artifact changes.
- Confirm the edited YAML remains parseable.
- Record the approved proposal as the handoff and closure evidence.

**Success criteria:**

- The Epic 19 action is marked `done`.
- Its evidence names Stories 21.9 and 21.10 and the `15.3-RV15`/`RV16`/`RV27` cluster.
- Story, epic, PRD, architecture, UX, code, and test content remains unchanged.
- No open retrospective action continues to claim this completed carry-forward is pending.

## Workflow Execution Log

| Date | Event | Result |
|---|---|---|
| 2026-07-16 | User-requested action confirmed | Complete |
| 2026-07-16 | PRD, epics, architecture, UX, Story 19.4, Stories 21.9/21.10, sprint status, and Epic 21 retrospective reviewed | Complete |
| 2026-07-16 | Path-forward evaluation | Direct Adjustment recommended |
| 2026-07-16 | Change scope classified | Minor |
| 2026-07-16 | Proposal prepared | Approved by Administrator |
| 2026-07-16 | Sprint tracking reconciliation applied and validated | Complete |
| 2026-07-16 | Minor-scope handoff | Complete |

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified: Story 19.4 / Epic 19 retrospective carry-forward action.
- [x] 1.2 Core problem defined: stale action status after the requested Epic 21 planning was completed.
- [x] 1.3 Evidence collected from Story 19.4, Stories 21.9 and 21.10, sprint status, and the Epic 21 retrospective.

### 2. Epic impact assessment

- [x] 2.1 Epic 19 can remain complete as planned.
- [N/A] 2.2 No epic-level scope change is required.
- [x] 2.3 Remaining epics reviewed; no dependency or sequencing impact found.
- [N/A] 2.4 No epic is invalidated and no new epic is needed.
- [N/A] 2.5 No priority or order change is needed.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no conflict or modification required.
- [x] 3.2 Architecture reviewed; no conflict or modification required.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Sprint tracking requires one status reconciliation; story and retrospective artifacts already provide closure evidence.

### 4. Path-forward evaluation

- [x] 4.1 Direct Adjustment is viable with low effort and low risk.
- [N/A] 4.2 Rollback is unnecessary because no incorrect implementation is identified.
- [N/A] 4.3 PRD/MVP review is unnecessary because product scope is unchanged.
- [x] 4.4 Direct Adjustment selected.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed.
- [x] 5.2 Epic and artifact impacts documented.
- [x] 5.3 Recommended path and alternatives documented.
- [x] 5.4 MVP impact and action plan documented.
- [x] 5.5 Minor-scope handoff documented.

### 6. Final review and handoff

- [x] 6.1 Applicable checklist items completed.
- [x] 6.2 Proposal checked against repository evidence.
- [x] 6.3 Explicit approval received from Administrator on 2026-07-16.
- [x] 6.4 Applied and validated the `sprint-status.yaml` reconciliation.
- [x] 6.5 Finalized the minor-scope handoff and closure record.
