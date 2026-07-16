---
project: Hexalith.Memories
date: 2026-05-18
trigger: implementation-readiness-report-2026-05-18.md
status: approved
scope: moderate
approved_by: Jerome
approved_on: 2026-05-18
---

# Sprint Change Proposal: Implementation Readiness Course Correction

## 1. Issue Summary

The 2026-05-18 implementation readiness assessment found the planning set close to ready but blocked by story-quality issues: one critical blocker, four major issues, and five minor concerns. Requirements coverage remains clean at 74/74 functional requirements covered, UX and architecture are aligned, and no duplicate-document blocker exists.

The change trigger is not a product pivot. It is a backlog and acceptance-criteria correction so implementation agents can execute stories independently without inheriting impossible semantics, unsafe telemetry expectations, or oversized unchecked work.

## 2. Impact Analysis

Checklist status:

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Trigger story identified | Done | Trigger is the implementation readiness assessment covering Stories 0.0, 3.5, 7.5, 13.2, 13.6, plus minor cleanup items. |
| 1.2 Core problem defined | Done | Existing epics have story-independence, impossible-AC, telemetry minimization, and sizing/checkpoint gaps. |
| 1.3 Evidence gathered | Done | Evidence is recorded in `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-18.md`. |
| 2.1 Current epic impact | Done | Epic 0 needed an epic-level readiness gate so Story 0.0 can close independently. |
| 2.2 Required epic-level changes | Done | Epic 0 Definition of Done was added; no epic renumbering required. |
| 2.3 Remaining epic review | Done | Epic 3, Epic 7, Epic 8 placeholder text, and Epic 13 stories required targeted edits. |
| 2.4 Future epic invalidation | N/A | No planned epic is obsolete. |
| 2.5 Order or priority change | N/A | Existing implementation order remains valid. |
| 3.1 PRD conflicts | Done | No PRD rewrite needed; FR coverage remains intact. |
| 3.2 Architecture conflicts | Done | Story 3.5 now matches the architecture's DAPR Workflow saga/compensation model. |
| 3.3 UX conflicts | Done | Story 7.5 now supports privacy-preserving telemetry aligned with trust and recoverability guidance. |
| 3.4 Other artifact impact | Done | Story-status/scope tooling must treat Story 8.3 as reserved non-MVP. |
| 4.1 Direct adjustment | Viable | Low to medium effort, low risk. |
| 4.2 Rollback | Not viable | No completed implementation needs rollback for these planning corrections. |
| 4.3 PRD MVP review | Not viable | MVP scope remains achievable. |
| 4.4 Path selected | Done | Direct adjustment within existing epics. |
| 5.1-5.5 Proposal components | Done | This document records issue summary, impact, approach, detailed edits, and handoff. |
| 6.1-6.5 Final review and handoff | Done | User approved on 2026-05-18. No sprint-status changes were needed because no stories were added, removed, or renumbered. |

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: the readiness failures are localized to story text and acceptance criteria. The PRD, UX, architecture, and FR coverage do not require a broader replan. Editing the affected stories preserves the current sequence while removing implementation ambiguity.

Scope classification: Moderate.

Timeline impact: Low. These are planning-artifact edits and should reduce implementation churn.

Risk: Low after review. The only meaningful residual risk is that Story 13.2 and Story 13.6 remain large tracked stories; checkpoint gates now make their review and acceptance incremental without forcing story renumbering.

## 4. Detailed Change Proposals

### Story 0.0: Project Scaffolding & Single-Command Boot

Section: Acceptance Criteria / Epic 0 readiness.

OLD:

Story 0.0 included `Foundation Epic Minimum Acceptance Criteria` covering tenant provisioning, minimal case creation, and tenant/case validation guard behavior.

NEW:

The same criteria were moved to `Epic 0 Definition of Done / Readiness Gate`. Story 0.0 now remains limited to scaffold, AppHost boot, ServiceDefaults, contracts/test structure, Redis/FalkorDB/DAPR/Aspire Dashboard, and root-declared `references/` submodule behavior.

Rationale: Story 0.0 is independently completable. Stories 0.1, 0.2, and 0.3 remain responsible for the later foundation behaviors.

### Story 3.5: Memory Unit Deletion & Case Deletion

Section: Case deletion acceptance criteria.

OLD:

`And the operation completes atomically - partial deletion is not acceptable (use DAPR Workflow for orchestration)`

NEW:

Case deletion is orchestrated by DAPR Workflow as a durable saga with retry and compensation steps. Failed backend deletion after retries must record the failed stage, keep the case in `deleting` or `delete_failed` state, and expose retry or repair behavior instead of silently leaving partial state.

Rationale: This matches the architecture's explicit statement that distributed atomic writes/deletes across Redis and FalkorDB are not achievable.

### Story 7.5: Search & Access Telemetry

Section: Audit telemetry acceptance criteria.

OLD:

Audit events included raw query parameters for search.

NEW:

Telemetry includes sanitized, bounded, low-cardinality fields only: timestamp, tenant ID, operation type, case ID, subject identifier, operation state/error code, duration bucket, result count bucket, axis selection, and query length bucket. Raw query text, raw query parameters, metadata filter values, payloads, secrets, and tokens are excluded from normal logs, traces, metrics, and audit telemetry.

Rationale: This preserves FR67 while honoring project rules against sensitive diagnostic leakage.

### Story 13.2: Implement OidcTokenProvider

Section: Story structure.

NEW:

Added implementation checkpoints:

- Checkpoint A: token acquisition and cache core.
- Checkpoint B: invalidation and concurrency.
- Checkpoint C: transport, DI, and redaction hardening.

Rationale: The story can remain one tracked Epic 13 story, but implementation and review must close each checkpoint independently before acceptance.

### Story 13.6: Vector Migration Tool

Section: Story structure.

NEW:

Added implementation checkpoints:

- Checkpoint A: dry-run and preflight.
- Checkpoint B: live migration execution.
- Checkpoint C: interruption, resume, and rollback safety.
- Checkpoint D: operator evidence.

Rationale: The story has multiple failure domains. Checkpoint evidence reduces migration blast-radius risk without forcing an immediate renumbering.

### Minor Cleanup Items

Story 5.3: Reworded awkward BDD phrasing so the verification report directly includes isolation checks.

Story 1.7: Replaced confusing `IEmbeddingProvider` wording with "embedding provider strategy/factory shape".

Story 8.3: Made the reserved/non-MVP tooling instruction explicit with `reserved-non-mvp`.

## 5. Implementation Handoff

Handoff recipients:

- Product Owner / Developer: review and accept the adjusted `epics.md` story text.
- Developer agent: use the updated acceptance criteria for future story implementation.
- Test Architect: when Stories 13.2 or 13.6 are selected, verify checkpoint evidence before story acceptance.

Success criteria:

- Story 0.0 can be implemented and reviewed independently.
- Story 3.5 no longer requires impossible cross-backend atomic semantics.
- Story 7.5 cannot be implemented with raw search telemetry by default.
- Story 13.2 and Story 13.6 have explicit checkpoint review gates.
- Minor readiness concerns are corrected or explicitly mapped.

## 6. Applied Artifact Changes

Updated artifact:

- `_bmad-output/planning-artifacts/epics.md`

No PRD, architecture, UX, or sprint-status changes were required for this correction.
