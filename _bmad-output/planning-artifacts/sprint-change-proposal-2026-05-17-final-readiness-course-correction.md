# Sprint Change Proposal: Final Readiness Course Correction

**Date:** 2026-05-17
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-17.md`
**Status:** Approved by JeromePiquot on 2026-05-17 and applied
**Change Scope:** Moderate - implementation handoff clarification; no PRD, architecture, or UX reset

## 1. Issue Summary

The 2026-05-17 implementation readiness assessment found no critical blockers. PRD, architecture, epics, and UX are present and traceable, and all 74 PRD functional requirements are covered or explicitly deferred.

The remaining issue is handoff ambiguity. Several early Epic 1 stories are component-oriented and must not be accepted as complete without observable proof. Story 1.6 is intentionally historical completed scope and must be split if future rework touches it. Tenant provisioning appears in both Story 0.1 and Story 5.1, so implementers need a clear ownership rule. The UX document is aligned, but FrontComposer and web UI scope must stay future-phase unless explicitly pulled forward.

## 2. Impact Analysis

### Epic Impact

Epic 0 remains the foundation path. Story 0.1 establishes the minimum active-tenant path required before any Epic 1 data-writing work.

Epic 1 remains valid for tenant-scoped ingestion and search, but Stories 1.2 through 1.5 must require contract-visible, CLI/API-visible, trace-visible, or integration-harness evidence. Current `epics.md` already includes validation evidence requirements for Stories 1.2, 1.3, 1.4, and 1.5.

Story 1.6 remains historical completed scope. Current `epics.md` already contains the binding sizing note requiring any future reimplementation or major rework to split happy path, failure/compensation visibility, and restart/idempotency hardening.

Epic 5 remains the full tenant lifecycle epic. Story 5.1 owns tenant infrastructure lifecycle semantics for RediSearch, Redis Vector, and FalkorDB. Story 0.1 consumes that ownership as the MVP prerequisite slice rather than creating a competing tenant-provisioning path.

### Artifact Impact

PRD: No requirement change is needed.

Architecture: No component redesign is needed.

UX: No redesign is needed. Keep the current full-horizon wording, with MVP implementation limited to CLI-visible and contract-visible evidence semantics.

Epics: Preserve the existing observable evidence requirements. If further cleanup is approved, add explicit boundary text that Story 0.1 is the minimum prerequisite slice and Story 5.1 is the full lifecycle owner.

Sprint status: No status transition is required. Do not reopen completed stories solely for planning clarification.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- Requirements coverage is complete.
- No architecture or UX blocker exists.
- Most readiness concerns are already partially addressed in the current artifact set.
- The remaining risk is implementer interpretation, not product definition.
- Rollback and MVP review would add churn without improving readiness.

Effort estimate: Low.
Risk level: Low.
Timeline impact: Minimal.

## 4. Detailed Change Proposals

### Proposal A: Preserve Epic 1 Observable Proof Requirements

Artifact: `_bmad-output/planning-artifacts/epics.md`
Stories: 1.2, 1.3, 1.4, 1.5

Current state:

```text
Each technical-leaning story already includes "Validation Evidence Required" language.
```

Decision:

```text
Keep these requirements binding. A story may not be accepted as complete on internal implementation alone; completion evidence must include contract tests, schema-compatible JSON, CLI/API-visible output, trace evidence, or integration-harness output as applicable.
```

Rationale: Prevents "green component stories, unproven user workflow."

### Proposal B: Treat Story 1.6 as Historical Scope Only

Artifact: `_bmad-output/planning-artifacts/epics.md`
Story: 1.6 - Ingestion Workflow Orchestration

Current state:

```text
Story 1.6 is historical completed scope. Future reimplementation or major rework must split it into smaller vertical stories: happy-path local file ingestion orchestration; failure, compensation, and failed-unit visibility; restart recovery, idempotency, and duplicate detection hardening.
```

Decision:

```text
Keep this note binding. Do not reopen Story 1.6 as a single future work item. Create new smaller stories if orchestration rework is needed.
```

Rationale: Avoids repeating an epic-sized story shape while preserving completed-work history.

### Proposal C: Clarify Tenant Provisioning Ownership

Artifact: `_bmad-output/planning-artifacts/epics.md`
Stories: 0.1 and 5.1

Proposed addition to Story 0.1:

```text
**Ownership Boundary:** Story 0.1 is the minimum executable prerequisite proving an active tenant exists before Epic 1 data-writing work. It must use the same `TenantProvisioningWorkflow` ownership model as Story 5.1 and must not introduce a separate tenant infrastructure creation path.
```

Proposed addition to Story 5.1:

```text
**Ownership Boundary:** Story 5.1 is the canonical full tenant lifecycle story for provisioning semantics, rollback behavior, verification, and lifecycle ownership. If Story 0.1 has already implemented the complete workflow, Story 5.1 should verify, extend, or mark the full lifecycle criteria as satisfied rather than duplicating divergent provisioning logic.
```

Rationale: Makes the minimum-versus-full lifecycle relationship explicit.

### Proposal D: Preserve UX/Web Scope Fence

Artifacts: `_bmad-output/planning-artifacts/epics.md`, `_bmad-output/planning-artifacts/ux-design-specification.md`

Current state:

```text
Future Fluent UI / FrontComposer web UI guidance remains future-phase design guidance unless a later sprint change explicitly pulls web UI work into scope.
```

Decision:

```text
Keep web and FrontComposer implementation out of MVP execution. MVP UX acceptance remains CLI-visible and contract-visible: scope, source, confidence, axis breakdown, omitted details, degraded state, and recovery actions.
```

Rationale: UX is aligned, but the implementation phase boundary must stay intact.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is the 2026-05-17 readiness assessment.
- 1.2 Core problem: Done. Handoff ambiguity after readiness review.
- 1.3 Supporting evidence: Done. Report lists 0 critical blockers, 3 major issues, and 3 warnings/minor concerns.
- 2.1 Current epic impact: Done. Epic 1 and Epic 5 need handoff clarity.
- 2.2 Required epic changes: Done. Preserve evidence requirements; clarify tenant provisioning boundary.
- 2.3 Remaining epic review: Done. Future web scope remains fenced.
- 2.4 Invalidated epics/new epics: Done. No epic invalidated; no new epic required.
- 2.5 Order/priority: Done. Epic 0 remains prerequisite before Epic 1.
- 3.1 PRD conflicts: Done. No PRD change required.
- 3.2 Architecture conflicts: Done. No architecture redesign required.
- 3.3 UX conflicts: Done. No UX redesign required; preserve phase boundary.
- 3.4 Other artifacts: Done. Sprint status requires no changes unless new stories are created later.
- 4.1 Direct adjustment: Viable. Low effort, low risk.
- 4.2 Rollback: Not viable. No rollback needed.
- 4.3 PRD MVP review: Not required. MVP remains achievable.
- 4.4 Recommended path: Done. Direct Adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1 Checklist review: Done.
- 6.2 Proposal accuracy: Done against current planning artifacts.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: N/A. No story status changes proposed.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Approve and apply the Story 0.1/5.1 boundary wording if desired.
- Developer agent: Treat Epic 1 technical stories as incomplete without observable proof evidence; do not reopen Story 1.6 as a single large future work item.
- Architect: Preserve `TenantProvisioningWorkflow` as the sole tenant infrastructure owner.
- UX Designer: No immediate redesign. Keep FrontComposer and Fluent UI implementation out of MVP until explicitly selected.

Success criteria:

- Epic 1 technical stories require observable proof, not internal implementation only.
- Story 1.6 future rework is split before implementation begins.
- Story 0.1 and Story 5.1 have a clear minimum-prerequisite versus full-lifecycle ownership boundary.
- Web/FrontComposer implementation remains future-phase unless pulled forward by an approved sprint change.
- No completed implementation story is reopened solely for planning hygiene.

## 7. Approval Record

JeromePiquot approved this Sprint Change Proposal for implementation on 2026-05-17.

Applied changes:

- Added Story 0.1 ownership boundary text to `_bmad-output/planning-artifacts/epics.md`.
- Added Story 5.1 ownership boundary text to `_bmad-output/planning-artifacts/epics.md`.

No sprint-status changes were required.
