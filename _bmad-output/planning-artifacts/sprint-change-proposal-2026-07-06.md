---
change_trigger: "Preserve TenantProvisioningWorkflow as the sole owner of tenant infrastructure creation during Epic 1 and Epic 5 rework"
mode: batch
status: approved-applied
project: Hexalith.Memories
date: 2026-07-06
scope_classification: moderate
recommended_path: direct-adjustment
---

# Sprint Change Proposal: Tenant Provisioning Ownership Guard

Date: 2026-07-06
Project: Hexalith.Memories
Prepared for: Jerome

## 1. Issue Summary

The Epic 0 retrospective created an open action item: preserve `TenantProvisioningWorkflow` as the sole owner of tenant infrastructure creation during Epic 1 and Epic 5 rework.

The immediate concern is not that the planning artifacts lack this invariant. They already state it in the PRD, epics, and architecture. The risk is rework drift: historical Epic 1 indexing and ingestion stories are broad, and future agents may copy old implementation patterns or local test setup helpers that create RediSearch, Redis Vector, or FalkorDB tenant infrastructure from feature paths.

This proposal keeps the existing architecture and PRD direction intact and adds an explicit rework gate to the affected epic/story areas. The gate makes ownership reviewable whenever Epic 1 indexing/search/graph work or Epic 5 tenant lifecycle work is reopened.

Evidence:

- PRD implementation sequencing says `TenantProvisioningWorkflow` owns physically isolated tenant infrastructure creation before ingestion/indexing writes.
- Epic 1 Story 1.5 already says indexing uses tenant infrastructure already provisioned by `TenantProvisioningWorkflow`.
- Epic 5 Story 5.1 already says `TenantProvisioningWorkflow` is the sole owner of RediSearch, Redis Vector, and FalkorDB tenant infrastructure creation.
- Architecture marks `TenantProvisioningWorkflow` as the sole tenant index/database creation workflow with saga rollback.
- Story 23.7 implementation evidence shows the current code has `ITenantIndexReadinessVerifier`, `TenantIndexNotProvisionedException`, and source-guard tests preventing per-document `FT.CREATE` in indexing hot paths.

## 2. Impact Analysis

### Epic Impact

Epic 1 is affected because indexing, ingestion orchestration, and graph write paths consume tenant infrastructure. Future Epic 1 rework must remain a consumer of already-provisioned infrastructure, not a creator of tenant indexes or graph databases.

Epic 5 is affected because it owns tenant lifecycle semantics. Future Epic 5 rework may change provisioning internals only within `TenantProvisioningWorkflow` and its tenant provisioning/deletion/verification activities.

Epic 23 is supporting evidence, not the main target. Story 23.7 already removed the A34 hot-path drift pattern, but its epic text frames the work mainly as performance/log-noise cleanup. The ownership invariant should be preserved as a review gate for future Epic 1 and Epic 5 work.

No new epic is needed. No completed epic needs to be reopened.

### Story Impact

Affected historical or future story areas:

- Story 1.5 or any split successor for syntactic, semantic, or graph indexing.
- Story 1.6 or any split successor for ingestion orchestration.
- Story 5.1 or any rework of tenant provisioning, verification, rollback, or registry activation.
- Any future CLI, MCP, search, or graph story that touches tenant index/database availability.

Current action item remains open as an ongoing review invariant. It should not be marked done merely because the current code contains guards; future related rework must keep checking it.

### Artifact Conflicts

PRD: No conflict. No PRD change required.

Architecture: No conflict. Architecture already assigns ownership to `TenantProvisioningWorkflow`.

Epics: Minor wording change recommended so the invariant survives story rework and review without requiring readers to chase older sprint-change proposals.

UX: Not applicable. This is backend lifecycle ownership, not a UI/UX change.

Sprint status: No structural status change required. The existing Epic 0 action item should remain open until the team decides whether ongoing review invariants are tracked as open actions or converted into a standing checklist.

### Technical Impact

No immediate production code change is required. The current code already has relevant guard evidence:

- `ITenantIndexReadinessVerifier` verifies existing tenant indexes and treats missing indexes as provisioning inconsistencies.
- Indexing activities use readiness verification instead of creating tenant indexes.
- `TenantProvisioningWorkflow` still registers and orchestrates provisioning activities.
- Guard tests assert indexing hot paths do not issue per-document `FT.CREATE`.

The future implementation rule is stricter than a code-style preference: feature paths may validate, read, write, and fail clearly, but they must not create or mutate tenant infrastructure lifecycle state.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale:

- Rollback is not useful because current implementation hardening already moved in the correct direction.
- MVP scope does not need reduction.
- The needed change is a persistent review gate on future rework, not a new product feature.

Effort estimate: Low for planning edits; medium ongoing review attention during related rework.

Risk level: Medium if left implicit, low after the rework gate is added.

Timeline impact: No material delay. Apply as a planning amendment before any Epic 1 or Epic 5 rework story is created or reopened.

## 4. Detailed Change Proposals

### Epics: Story 1.5 Rework Gate

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Story 1.5: Three-Backend Indexing

OLD:

```markdown
**Historical Scope Guard:** Do not reopen Story 1.5 as a single implementation unit. If indexing rework resumes, create separate numeric stories for the documented slices before implementation starts, keep each slice independently testable against tenant-scoped infrastructure, and require observable proof per slice (CLI-visible, contract-visible, or integration-harness output) - internal unit tests alone are not sufficient completion evidence.

**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.
```

NEW:

```markdown
**Historical Scope Guard:** Do not reopen Story 1.5 as a single implementation unit. If indexing rework resumes, create separate numeric stories for the documented slices before implementation starts, keep each slice independently testable against tenant-scoped infrastructure, and require observable proof per slice (CLI-visible, contract-visible, or integration-harness output) - internal unit tests alone are not sufficient completion evidence.

**Rework Ownership Gate:** Any Epic 1 indexing rework must prove it only validates or verifies tenant infrastructure readiness before writing. It must not call `FT.CREATE`, create Redis Vector indexes, create FalkorDB tenant databases, or otherwise create/mutate tenant infrastructure lifecycle state from ingestion, indexing, search, graph, CLI, or MCP paths. Missing or incompatible tenant infrastructure is a `TENANT_NOT_PROVISIONED` or equivalent structured validation/operational inconsistency, not a trigger for on-demand resource creation.

**Readiness tooling guard:** Do not create a new implementation story file that reuses this broad historical story key for new work. New implementation must use newly numbered split stories with externally observable completion evidence.
```

Rationale: Makes the ownership invariant explicit where future Epic 1 indexing rework will be copied from.

### Epics: Story 5.1 Rework Gate

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Story 5.1: Tenant Provisioning Workflow

OLD:

```markdown
**Ownership Boundary:** Story 5.1 is the canonical full tenant lifecycle story for provisioning semantics, rollback behavior, verification, and lifecycle ownership. If Story 0.1 has already implemented the complete workflow, Story 5.1 should verify, extend, or mark the full lifecycle criteria as satisfied rather than duplicating divergent provisioning logic.
```

NEW:

```markdown
**Ownership Boundary:** Story 5.1 is the canonical full tenant lifecycle story for provisioning semantics, rollback behavior, verification, and lifecycle ownership. If Story 0.1 has already implemented the complete workflow, Story 5.1 should verify, extend, or mark the full lifecycle criteria as satisfied rather than duplicating divergent provisioning logic.

**Rework Ownership Gate:** Any Epic 5 tenant lifecycle rework may change tenant infrastructure creation only inside `TenantProvisioningWorkflow` and its tenant provisioning, verification, rollback, or deletion activities. It must update schema definitions, provisioning/deletion activities, verification behavior, and lifecycle tests together. Feature paths such as ingestion, indexing, search, graph, CLI, and MCP remain consumers of active tenant infrastructure and must fail clearly when required infrastructure is missing or inactive.
```

Rationale: Keeps Epic 5 as the lifecycle owner while preventing future feature-path shortcuts.

### Sprint Status Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`
Section: `action_items`

No edit recommended in this proposal. Keep the action item open until the team decides whether this is tracked as a standing review invariant or closed after the epics rework gates are applied.

Optional later refinement:

```yaml
    status: in-progress
    evidence: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-06.md"
```

Rationale: Avoids modifying the existing dirty sprint-status file during this workflow and avoids prematurely closing an ongoing guardrail.

## 5. Implementation Handoff

Change scope: Moderate.

Routing:

- Architect: confirm the rework gate language preserves the intended lifecycle boundary and does not block legitimate provisioning internals.
- Developer: apply the two epics wording changes after approval; do not change production code unless a follow-up review finds a concrete drift.
- Test Architect: keep or extend source guards around indexing hot paths when new Epic 1 or Epic 5 rework touches RediSearch, Redis Vector, FalkorDB, readiness verification, or provisioning activities.
- Product Owner: keep the action item visible until the guard is either applied as epics text or moved into a standing story-review checklist.

Success criteria:

- No Epic 1 rework story can create tenant indexes/databases from ingestion, indexing, search, graph, CLI, or MCP paths.
- No Epic 5 rework creates a second tenant infrastructure lifecycle owner.
- Missing or inactive infrastructure fails clearly with structured validation or operational inconsistency, never silent creation.
- Tests or source guards continue to prove indexing hot paths do not issue lifecycle creation commands.

## 6. Checklist Execution Notes

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | [x] Done | Epic 0 retrospective action item 2. |
| 1.2 Core problem defined | [x] Done | Rework drift could reintroduce duplicate infrastructure ownership. |
| 1.3 Evidence gathered | [x] Done | PRD, epics, architecture, Epic 0 retro, Story 23.7 evidence, and code guard search reviewed. |
| 2.1 Current epic evaluated | [x] Done | Epic 0 stays complete; Epic 1 and Epic 5 rework need persistent guard text. |
| 2.2 Epic-level changes determined | [x] Done | Add Story 1.5 and Story 5.1 rework ownership gates. |
| 2.3 Remaining epics reviewed | [x] Done | Epic 23 is supporting evidence; no new epic needed. |
| 2.4 Future epic invalidation checked | [x] Done | No future epic invalidated. |
| 2.5 Epic order/priority checked | [x] Done | No resequencing needed. |
| 3.1 PRD conflicts checked | [x] Done | PRD already aligned; no change. |
| 3.2 Architecture conflicts checked | [x] Done | Architecture already aligned; no change. |
| 3.3 UX conflicts checked | [N/A] Skip | No UI/UX behavior changes. |
| 3.4 Other artifacts checked | [x] Done | Sprint status action item exists; no edit recommended before approval. |
| 4.1 Direct Adjustment evaluated | [x] Viable | Best path. |
| 4.2 Rollback evaluated | [x] Not viable | Current code already hardened in the right direction. |
| 4.3 MVP Review evaluated | [x] Not viable | MVP scope and PRD remain valid. |
| 4.4 Recommended path selected | [x] Done | Direct Adjustment. |
| 5.1 Issue summary created | [x] Done | Included above. |
| 5.2 Impact documented | [x] Done | Included above. |
| 5.3 Rationale documented | [x] Done | Included above. |
| 5.4 MVP impact/action plan defined | [x] Done | No MVP scope change; apply epics wording after approval. |
| 5.5 Handoff plan established | [x] Done | Architect, Developer, Test Architect, Product Owner. |
| 6.1 Checklist reviewed | [x] Done | All applicable sections addressed. |
| 6.2 Proposal accuracy checked | [x] Done | Current artifact and code evidence cross-checked. |
| 6.3 User approval | [x] Done | Approved by Jerome on 2026-07-06. |
| 6.4 sprint-status.yaml update | [N/A] Skip | Optional update skipped to avoid touching the pre-existing dirty sprint-status file; action item remains open as an ongoing review invariant. |
| 6.5 Next steps confirmed | [x] Done | Epics wording applied; future related rework must preserve the ownership gate. |

## 7. Approval

- [x] Approved by Jerome - 2026-07-06
- [x] Epics wording applied - Story 1.5 and Story 5.1 rework ownership gates added
- [N/A] Optional sprint-status action evidence/status update skipped; existing action item remains open
