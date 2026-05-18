# Sprint Change Proposal: Readiness Backlog Structure Cleanup

**Date:** 2026-05-17  
**Project:** Hexalith.Memories  
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-17.md`  
**Status:** Approved by JeromePiquot on 2026-05-17  
**Change Scope:** Moderate - backlog reorganization and implementation-handoff clarity; no PRD reset

## 1. Issue Summary

The implementation readiness assessment found that Hexalith.Memories is requirements-complete enough to move toward implementation: all required planning documents exist, all 74 PRD functional requirements are covered or intentionally deferred, UX aligns with PRD and architecture, and all 77 stories have BDD-style acceptance criteria.

The remaining problem is backlog execution risk. The epics file mixes historical, product, foundation, Phase 1.5, and operational-readiness work in ways that can confuse story tooling and future implementers. The highest-risk examples are the alphabetic story key `2.6A`, Foundation Slice 0 being embedded inside Epic 1 while acting as a prerequisite layer, and component-slice stories that need explicit externally observable validation evidence.

This is not a product-definition failure. It is a planning hygiene and handoff-safety correction.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains valid, but Foundation Slice 0 should be promoted from an embedded section into an explicit prerequisite epic: **Epic 0: Tenant and Case Safety Foundation**. Story 1.1 can remain the first executable scaffold story, but stories 0.1, 0.2, and 0.3 should belong to Epic 0 for traceability and status tooling.

Epic 2 remains valid, but Story 2.6A should be normalized before implementation handoff. The recommended target is:

- Story 2.7: Evidence Packet Contract Mapping
- Story 2.8: Benchmark Suite & Thesis Validation

The old `2.6A` key should be retained only as a historical alias in completion notes, sprint-status migration notes, and any implementation artifact references.

Epics 11, 12, 14, and 15 can stay in the same `epics.md` file only because they are already framed as an Engineering/Operational Readiness track. The proposal strengthens the top-level scope boundary so those epics are not mistaken for MVP product capability work.

### Story Impact

Affected stories:

- Story 0.1, 0.2, 0.3: Move under explicit Epic 0.
- Story 1.1: Keep first executable scaffold story and clarify that Epic 0 runs immediately after it.
- Story 1.2, 1.3, 1.4, 1.5, 2.1, 2.2, and 2.4: Add explicit observable validation evidence where missing.
- Story 2.6A: Rename to Story 2.7 and preserve historical alias.
- Story 2.7: Rename to Story 2.8 after Evidence Packet mapping is inserted as numeric Story 2.7.
- Story 8.3: Keep reserved Phase 2 export history and explicitly validate story-status tooling against this intentional gap.
- Story 15.6: Use the actual `.gitmodules` list for Story 1.1 submodule drift correction: Hexalith.Commons, Hexalith.EventStore, Hexalith.AI.Tools, Hexalith.Tenants, and Hexalith.FrontComposer.

### Artifact Conflicts

PRD: No requirement change is needed. Add no new FRs. MVP remains achievable.

Architecture: No technology or component redesign is needed. The contract-first Evidence Packet and tenant/case isolation architecture remain valid.

UX: No redesign is needed. The UX warning is scope control: keep Fluent UI, FrontComposer, and future web-surface components out of MVP implementation unless a later sprint change explicitly pulls them forward.

Epics: Requires targeted edits to top-level scope language, Epic 0 structure, Story 2 numeric sequencing, component-story validation evidence, and minor operational-story guardrails.

Sprint status: Requires key migration for `foundation-slice-0`, `2-6a-evidence-packet-contract-mapping`, and `2-7-benchmark-suite-and-thesis-validation` if the proposal is approved.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- No requirements are missing.
- No architecture or UX blocker exists.
- No rollback is useful because the issue is planning structure, not failed implementation.
- The current artifacts already contain partial mitigations, so a narrow cleanup is lower risk than a broad replan.
- Normalized story identity will reduce future parser, status, and implementation-artifact drift.

Effort estimate: Low to medium.  
Risk level: Low.  
Timeline impact: Minimal if performed before assigning the next implementation batch.

Alternatives considered:

- **Leave `2.6A` and require parsers to support suffixes:** workable, but it keeps the readiness finding open and spreads a special case into tooling.
- **Fold Foundation Slice 0 into Epic 1 with Story 1.2+ renumbering:** avoids Epic 0, but creates more churn across completed Epic 1 references.
- **Move Epics 11-15 out of `epics.md`:** cleaner product backlog, but loses useful lifecycle continuity. Strengthened lifecycle labeling is enough for now.
- **MVP review:** not needed because MVP scope remains intact.

## 4. Detailed Change Proposals

### Proposal A: Add Explicit Readiness Scope Boundary

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: Before `## Epic List`

OLD:

```text
## Epic List
```

NEW:

```text
## Implementation Readiness Boundary

MVP implementation readiness covers Epic 0 through Epic 8:
- Epic 0: Tenant and Case Safety Foundation
- Epic 1-8: MVP thesis, tenant isolation, CLI developer experience, and operations gates

Phase 1.5 fast-follow covers Epic 9 and Epic 10.

Epics 11-15 are Engineering/Operational Readiness work. They may remain in this file for lifecycle continuity, but they require explicit sprint selection before implementation and are judged by delivery safety, release integrity, maintainer/operator outcomes, and validation evidence.

Future web UI, FrontComposer, and Fluent UI implementation remain out of MVP unless a later approved sprint change pulls them forward.

## Epic List
```

Rationale: Makes the implementation boundary visible before readers hit the story list.

### Proposal B: Promote Foundation Slice 0 to Epic 0

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: `Epic List` and detailed story section

OLD:

```text
### Phase: MVP - Foundation Slice 0 (Tenant and Case Bootstrap)

...

### Epic 1: First Tenant-Scoped Memory Ingestion and Search
...

### MVP Foundation Slice 0: Tenant and Case Bootstrap
...
### Story 0.1: Tenant Provisioning Minimum Viable Workflow
### Story 0.2: Minimal Case Bootstrap
### Story 0.3: Tenant and Case Validation Guard
```

NEW:

```text
### Phase: MVP - Foundation Gate

### Epic 0: Tenant and Case Safety Foundation
Before any ingestion, indexing, search, or graph story writes data, the system must support an active tenant with physically isolated backend infrastructure and an active case within that tenant.
**Sequencing:** Story 1.1 remains the first executable scaffold story. Epic 0 runs immediately after Story 1.1 and before Epic 1 data-writing stories.
**FRs covered:** FR26, FR38, FR44
**NFRs covered:** NFR8

### Phase: MVP - Gate 1 (Three-Axis Validation)

### Epic 1: First Tenant-Scoped Memory Ingestion and Search
...

## Epic 0: Tenant and Case Safety Foundation
...
### Story 0.1: Tenant Provisioning Minimum Viable Workflow
### Story 0.2: Minimal Case Bootstrap
### Story 0.3: Tenant and Case Validation Guard
```

Rationale: Preserves the existing `0.x` story keys while making the prerequisite layer explicit and tool-friendly.

### Proposal C: Normalize Story 2 Numeric Keys

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: Epic 2

OLD:

```text
### Story 2.6A: Evidence Packet Contract Mapping

**Historical numbering note:** Story key `2.6A` is intentionally preserved because implementation artifacts and sprint status may already reference it. Story-key parsers must allow alphabetic suffixes.

...

### Story 2.7: Benchmark Suite & Thesis Validation
```

NEW:

```text
### Story 2.7: Evidence Packet Contract Mapping

**Historical alias:** This story was previously tracked as `2.6A`. Existing implementation artifacts and sprint-status history may keep the old key as an alias, but new story tooling and future references should use `2.7`.

...

### Story 2.8: Benchmark Suite & Thesis Validation
```

Rationale: Removes the special alphabetic story key before future tooling depends on it.

Sprint-status migration:

```text
OLD:
2-6a-evidence-packet-contract-mapping: backlog
2-7-benchmark-suite-and-thesis-validation: done

NEW:
2-7-evidence-packet-contract-mapping: backlog
2-8-benchmark-suite-and-thesis-validation: done
```

### Proposal D: Add Story Key Policy

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: Near `Implementation Readiness Boundary`

NEW:

```text
### Story Key Policy

New story keys must use numeric `Epic.Story` format. Alphabetic suffixes are allowed only as historical aliases during migration and must not be introduced for new work unless story tooling explicitly supports them and a sprint change approves the exception.
```

Rationale: Prevents recurrence of the `2.6A` problem while still respecting existing historical references.

### Proposal E: Add Observable Outcomes to Component-Slice Stories

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Sections: Story 1.3, 1.4, 1.5, 2.1, 2.2

OLD:

```text
Acceptance criteria prove component behavior, but not every component-slice story explicitly names a user-visible or contract-visible validation artifact.
```

NEW:

```text
**Validation Evidence Required:** Completion evidence must include at least one externally observable proof point: contract test output, CLI-visible behavior, integration harness output, serialized JSON example, or Aspire trace evidence. Pure internal implementation completion is not sufficient.
```

Apply story-specific proof points:

- Story 1.3: extraction fixture results for text, markdown, and PDF plus Aspire trace evidence.
- Story 1.4: embedding contract test with provider/model/dimension metadata and secret-redaction verification.
- Story 1.5: tenant-scoped indexing integration evidence across RediSearch, Redis Vector, and FalkorDB.
- Story 2.1: CLI or API search output showing tenant-scoped BM25 results and empty-state behavior.
- Story 2.2: semantic search output showing vector result ordering and provider metadata.

Rationale: Keeps enabling stories acceptable by requiring demonstrable progress.

### Proposal F: Constrain "Implemented or Documented" Acceptance Patterns

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Section: Engineering/Operational Readiness Track

NEW:

```text
Acceptance criteria that allow "implemented, documented, accepted, or carried forward" are allowed only in Engineering/Operational Readiness stories. MVP product capability stories must deliver working behavior or explicit testable validation, not documentation-only completion, unless a separate sprint change approves a deferral.
```

Rationale: Preserves legitimate governance triage while protecting product stories from soft completion criteria.

### Proposal G: Validate Intentional Story Gaps

Artifact: `_bmad-output/planning-artifacts/epics.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml`  
Section: Epic 8 / sprint status comments

NEW:

```text
Story 8.3 is reserved for Phase 2 export history and is excluded from MVP readiness accounting. Story-status and story-file-scope tooling must either tolerate this intentional reserved key or explicitly map it as non-MVP historical work.
```

Rationale: The `8.3` gap is acceptable only if tooling behavior is explicit.

### Proposal H: Align Story 1.1 Submodule References

Artifact: `_bmad-output/planning-artifacts/epics.md`  
Sections: Story 1.1 and Story 15.6

OLD:

```text
Story 1.1 names Hexalith.Commons and Hexalith.EventStore as submodules.
```

NEW:

```text
Root-level submodule validation must match `.gitmodules`:
- Hexalith.Commons
- Hexalith.EventStore
- Hexalith.AI.Tools
- Hexalith.Tenants
- Hexalith.FrontComposer

Nested submodules must not be initialized or updated unless explicitly requested.
```

Rationale: Aligns planning with repository reality and preserves the root-level-only submodule policy.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is the 2026-05-17 implementation readiness assessment.
- 1.2 Core problem: Done. Backlog structure and story identity cleanup needed before implementation handoff.
- 1.3 Supporting evidence: Done. Report lists 5 major cleanup items, 3 minor concerns, and 3 UX scope warnings.
- 2.1 Current epic impact: Done. Epic 1 and Epic 2 need structural cleanup.
- 2.2 Required epic changes: Done. Add Epic 0, normalize Story 2 keys, strengthen readiness boundary.
- 2.3 Remaining epic review: Done. Epics 11-15 remain as operational-readiness track with guardrails.
- 2.4 Invalidated epics/new epics: Done. No existing epic invalidated; new Epic 0 is a formalization of existing Foundation Slice 0.
- 2.5 Order/priority: Done. Story 1.1 remains first executable scaffold story; Epic 0 follows before data-writing stories.
- 3.1 PRD conflicts: Done. No PRD requirement changes required.
- 3.2 Architecture conflicts: Done. No architecture redesign required.
- 3.3 UX conflicts: Done. UX remains aligned; future web-surface scope must remain fenced.
- 3.4 Other artifacts: Done. `sprint-status.yaml` requires key migration if approved.
- 4.1 Direct adjustment: Viable. Low/medium effort, low risk.
- 4.2 Rollback: Not viable. No implementation rollback would help.
- 4.3 PRD MVP review: Not required. MVP remains achievable.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done in this draft.
- 6.1 Checklist review: Done.
- 6.2 Proposal accuracy: Done against current readiness report and current planning artifacts.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: Done. Epic 0 and Story 2 key migrations were applied to `sprint-status.yaml`.
- 6.5 Handoff plan: Done below.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Apply the epics and sprint-status backlog hygiene edits after approval.
- Developer agent: Preserve historical aliases when touching implementation artifacts. Do not reopen completed stories solely because their IDs are being normalized.
- Architect: Confirm that Epic 0 formalization does not alter the tenant/case ownership architecture.
- UX Designer: No immediate UX redesign. Keep Fluent UI, FrontComposer, and future web UI work out of MVP until explicitly selected.

Success criteria:

- `epics.md` has an explicit implementation readiness boundary.
- Foundation Slice 0 is represented as Epic 0, with stories 0.1-0.3 clearly owned by that epic.
- Story `2.6A` is replaced by numeric Story 2.7, and the benchmark story becomes Story 2.8.
- Historical aliases exist where needed so prior references remain understandable.
- Component-slice stories require externally observable validation evidence.
- Operational-readiness acceptance criteria are clearly separated from product-capability acceptance criteria.
- Story 8.3's Phase 2 reservation is documented for tooling.
- Submodule references match `.gitmodules` and preserve the no-recursive-submodule rule.

## 7. Approval Record

JeromePiquot approved this proposal for implementation on 2026-05-17. The backlog structure cleanup was applied to:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

No PRD, architecture, UX, or code changes are required by this proposal unless later review finds stale cross-references after the story-key migration.
