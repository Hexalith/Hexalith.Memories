---
status: approved
date: 2026-05-18
project: Hexalith.Memories
trigger_report: implementation-readiness-report-2026-05-17.md
change_scope: moderate
approved_by: Jerome
approved_on: 2026-05-18
affected_artifacts:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/implementation-artifacts/1-2-memory-unit-domain-model-and-contracts.md
  - _bmad-output/implementation-artifacts/1-3-content-extraction-via-kreuzberg.md
  - _bmad-output/implementation-artifacts/1-4-embedding-generation.md
  - _bmad-output/implementation-artifacts/1-5-three-backend-indexing.md
  - _bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md
---

# Sprint Change Proposal: Epic 1 Observable Proof Guard

## 1. Issue Summary

The 2026-05-17 Implementation Readiness Assessment marked the artifact set as `NEEDS WORK` with 0 critical blockers, 2 major issues, and 4 warning/minor concerns.

The remaining major issues are:

1. Epic 1 contains technical/component stories that can be completed without proving user-visible or developer-observable behavior.
2. Story 1.6 is too large for future rework and must be treated as historical scope unless split first.

This is not a requirements coverage problem. All PRD functional requirements are mapped. The risk is execution quality: an implementer could close or copy early Epic 1 technical slices based only on internal classes, mocks, or isolated unit tests, leaving the tenant-scoped ingestion/search journey unproven.

## 2. Checklist Findings

### Trigger and Context

- [x] Triggering artifact: `implementation-readiness-report-2026-05-17.md`.
- [x] Issue type: planning quality and implementation-readiness correction.
- [x] Evidence: readiness report identifies Epic 1 technical slicing and Story 1.6 size as the two remaining major issues.

### Epic Impact

- [x] Epic 1 can remain complete as historical scope.
- [x] No new epic is required.
- [x] No epic needs removal, renumbering, or resequencing.
- [x] Future Epic 1 rework must include observable proof gates for technical slices.

### Artifact Impact

- [x] PRD impact: none. MVP goals and functional requirements remain unchanged.
- [x] Architecture impact: none. The existing architecture already supports contracts, activities, workflows, telemetry, CLI/API surfaces, and integration proofs.
- [x] UX impact: none. The Evidence Packet and trust-loop UX remain aligned.
- [x] Story artifact impact: Epic 1 story files need explicit future-rework evidence guards.
- [N/A] Sprint status impact: no stories were added, removed, renumbered, or status-changed.

### Path Forward

Selected path: Direct Adjustment.

Effort: Low.

Risk: Low.

Rationale: The readiness issue is best resolved by adding explicit proof and split guards to existing planning artifacts. Rollback is not useful because the stories are historical and complete. MVP review is not needed because scope and requirements remain valid.

## 3. Recommended Approach

Apply a documentation-level course correction:

1. Add an Epic 1 implementation-readiness amendment requiring observable proof for Stories 1.2 through 1.5 and analogous future technical slices.
2. Add story-level future-rework proof requirements to Stories 1.2, 1.3, 1.4, and 1.5.
3. Add a Story 1.6 historical-scope guard requiring split-before-rework.
4. Leave `sprint-status.yaml` unchanged because no story status or backlog structure changes.

This is a moderate planning correction because it affects implementation handoff rules, but it does not change product scope, architecture, or active story status.

## 4. Detailed Change Proposals

### Epic 1 Amendment

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `## Epic 1: First Tenant-Scoped Memory Ingestion and Search`

OLD:

```text
Developer can boot the stack, provision or select an active tenant and case, ingest local content, and see it persisted and searchable across text, vector, and graph axes with tenant-safe provenance and typed graph edges.
```

NEW:

```text
Developer can boot the stack, provision or select an active tenant and case, ingest local content, and see it persisted and searchable across text, vector, and graph axes with tenant-safe provenance and typed graph edges.

Implementation Readiness Amendment (2026-05-18): Stories 1.2 through 1.5 are accepted as historical technical slices, but they must not be used as a pattern for future work without an observable proof gate. Any reopened, reimplemented, or analogous technical slice in Epic 1 must close with at least one developer-visible, contract-visible, CLI/API-visible, trace-visible, or integration-harness proof that demonstrates how the slice advances the tenant-scoped ingestion/search journey. Internal classes, mocks, or green unit tests alone are not sufficient completion evidence.
```

Rationale: Establishes the guard once at the epic level so future story creation and implementation handoff inherit it.

### Story 1.2 Proof Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-2-memory-unit-domain-model-and-contracts.md`

Change: Add a future-rework evidence gate requiring representative JSON payloads, serialization round-trip output, and one downstream consumer/API/CLI/integration proof.

Rationale: Contracts are technical by nature, so the observable proof must be contract-visible rather than UI-visible.

### Story 1.3 Proof Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-3-content-extraction-via-kreuzberg.md`

Change: Add a future-rework evidence gate requiring text, markdown, and PDF fixture extraction results plus activity/API/CLI/trace/integration evidence.

Rationale: Extraction should prove content crosses a system boundary, not only that a client method works.

### Story 1.4 Proof Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-4-embedding-generation.md`

Change: Add a future-rework evidence gate requiring provider/model/dimension output, boundary evidence, secret redaction, and rate-limit/retry recovery proof.

Rationale: Embedding generation is only useful if its output is visible to ingestion/search flow and safe credential handling is proven.

### Story 1.5 Proof Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-5-three-backend-indexing.md`

Change: Add a future-rework evidence gate requiring proof that one memory unit is discoverable through RediSearch, Redis Vector, and FalkorDB under the correct tenant, plus negative tenant-isolation evidence.

Rationale: This directly addresses the "green technical stories, unproven user workflow" risk.

### Story 1.6 Historical Scope Guard

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md`

OLD:

```text
Story 1.6 is historical completed scope. Future reimplementation or major rework must split it into smaller vertical stories: happy-path local file ingestion orchestration; failure, compensation, and failed-unit visibility; restart recovery, idempotency, and duplicate detection hardening.
```

NEW:

```text
Story 1.6 is historical completed scope and must not be reopened as one implementation unit. Any future reimplementation, major refactor, or behavioral expansion of this area must first split the work into smaller numeric stories before development starts.

Required future split:

1. Happy-path local file ingestion orchestration from validated tenant/case context through indexed result.
2. Failure, retry exhaustion, compensation, and failed-unit visibility.
3. Restart recovery, idempotency, and duplicate detection hardening.

Each future slice must include observable API, CLI, trace, or integration-harness proof. Internal workflow tests alone are not sufficient evidence for future work.
```

Rationale: Turns the existing sizing warning into an enforceable implementation handoff rule.

## 5. MVP and Scope Impact

MVP scope is unchanged.

Epics 0-8 remain the MVP execution boundary for readiness accounting. Epics 9-10 remain Phase 1.5. Epics 11-15 remain operational-readiness history unless explicitly selected by a sprint change.

FR71 remains an explicit Phase 2 deferral and is not affected by this proposal.

## 6. Implementation Handoff

Scope classification: Moderate planning correction.

Handoff recipients:

- Product Owner / planning owner: preserve these proof gates during future story creation and backlog grooming.
- Developer agent: when touching Epic 1 technical areas, produce observable proof artifacts before marking work done.
- Reviewer / readiness assessor: reject future Epic 1 technical-slice closure when evidence is internal-only.

Success criteria:

1. `epics.md` contains an Epic 1 observable-proof amendment.
2. Stories 1.2 through 1.5 contain explicit future-rework proof gates.
3. Story 1.6 contains a historical-scope guard with required split categories.
4. `sprint-status.yaml` remains unchanged because no story status changed.

## 7. Approval

Approved by Jerome on 2026-05-18.

Approval disposition: proceed with the documentation-level course correction as applied. Future Epic 1 technical-slice work must follow the observable-proof gates, and Story 1.6 must be split before any future rework.

## 8. Final Disposition

Applied on 2026-05-18 as a documentation-level course correction. No code, architecture, PRD, or sprint-status changes were required.
