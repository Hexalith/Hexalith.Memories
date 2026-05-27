# Sprint Change Proposal: Foundation and CI Readiness Correction

**Date:** 2026-05-17
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-17.md`
**Status:** Approved by JeromePiquot on 2026-05-17 and applied
**Change Scope:** Moderate - backlog restructuring and sequencing cleanup; no product requirement reset
**Supersedes for these two critical findings:** earlier 2026-05-17 readiness cleanup proposals that preserved `Story 1.1 -> Epic 0 -> remaining Epic 1` as the implementation path.

## 1. Issue Summary

The readiness assessment found that PRD, architecture, UX, and epics are present and materially aligned. All 74 PRD functional requirements are traced, and there is no product-substance coverage gap.

The blocker is implementation sequencing. The planning set still made the first executable story appear as Story 1.1, then Epic 0, then the remaining Epic 1 work. That creates a circular-looking foundation path for humans, story tooling, and implementation agents. The same report also found that the minimum CI build/test gate sits in Epic 11 even though it is an early greenfield prerequisite.

## 2. Impact Analysis

### Epic Impact

Epic 0 is now the complete foundation path:

- Story 0.0: Project Scaffolding & Single-Command Boot
- Story 0.1: Tenant Provisioning Minimum Viable Workflow
- Story 0.2: Minimal Case Bootstrap
- Story 0.3: Tenant and Case Validation Guard

Epic 1 now starts only after Epic 0 is complete and is scoped to data-writing ingestion/search behavior rather than scaffold/foundation setup.

Epic 11 remains the Engineering/Operational Readiness epic for CI/CD, semantic release, NuGet publishing, branch protection hardening, and release operations. Its minimum build/test slice is explicitly treated as an early enabling prerequisite for any greenfield or restarted implementation sequence.

### Story Impact

- Story 1.1 has been reclassified in planning as Story 0.0 with historical alias Story 1.1.
- The existing completed story file and sprint-status key keep the `1-1` identifier for traceability.
- Sprint status now includes `0-0-project-scaffolding-and-single-command-boot: done` and documents the alias.
- Story 11.1 remains complete, but its minimum build/test gate is no longer hidden behind the later operational epic ordering.

### Artifact Impact

PRD:

- Implementation sequencing now calls out scaffold, minimum build/test feedback, tenant provisioning, case bootstrap, and validation guards as the foundation path.

Architecture:

- Decision D17 now distinguishes the early minimum build/test gate from later semantic release and release-hardening work.

Epics:

- Added a selected implementation scope block.
- Reframed Epic 0 as the complete foundation path.
- Moved the scaffold story into Epic 0 as Story 0.0 while retaining the historical Story 1.1 alias.
- Reframed Epic 1 as dependent on completed Epic 0 foundation.

Sprint status:

- Updated the foundation sequence comment and added the Story 0.0 completed key.

UX:

- No direct UX change required. The readiness issue is planning sequence, not product interaction design.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- No PRD requirement is missing.
- No product strategy change is needed.
- The problem is structural readability and implementation handoff safety.
- A clean foundation path removes the circular-looking `Story 1.1 -> Epic 0 -> remaining Epic 1` sequence without reopening completed work.
- Keeping Story 1.1 as a historical alias avoids breaking completed story files and sprint history.

Effort estimate: Low to medium.
Risk level: Low.
Timeline impact: Minimal if the corrected sequence is used for future handoffs.

Alternatives considered:

- Rollback: Not justified because this is planning structure, not failed implementation.
- Full MVP review: Not required because requirements coverage and product alignment are intact.
- Renumber every historical artifact: Not recommended because it would churn completed story files and status history without improving implementation safety.

## 4. Detailed Change Proposals

### Foundation Sequence

Artifact: Epics
Section: Implementation readiness boundary and Epic 0

OLD:

```text
Story 1.1 is the first executable scaffold story.
Epic 0 runs immediately after Story 1.1 and before Epic 1 data-writing stories.
```

NEW:

```text
Epic 0 is the complete foundation path:
1. Story 0.0: Project Scaffolding & Single-Command Boot (historical alias: Story 1.1)
2. Story 0.1: Tenant Provisioning Minimum Viable Workflow
3. Story 0.2: Minimal Case Bootstrap
4. Story 0.3: Tenant and Case Validation Guard
5. Epic 1 data-writing ingestion/search stories
```

Rationale: Removes the circular-looking handoff path and makes foundation execution readable from story numbers.

### Scaffold Story Reclassification

Artifact: Epics and implementation story file
Section: Story 0.0

OLD:

```text
## Epic 1: First Tenant-Scoped Memory Ingestion and Search
### Story 1.1: Set Up Initial Project from Aspire Starter Template
```

NEW:

```text
## Epic 0: Tenant and Case Safety Foundation
### Story 0.0: Project Scaffolding & Single-Command Boot
Historical alias: Story 1.1.
```

Rationale: Puts the scaffold in the foundation epic without losing completed-work traceability.

### Epic 1 Scope

Artifact: Epics
Section: Epic 1 summary

OLD:

```text
Epic 1 delivers scaffold, tenant/case foundation dependencies, and ingestion/search behavior.
```

NEW:

```text
Epic 1 depends on completed Epic 0 and delivers ingestion/search behavior: IngestionWorkflow, Contracts V1 ingestion/search contracts, Redis/FalkorDB indexing, Kreuzberg extraction, embedding provider configuration, provenance, and causal metadata indexing.
```

Rationale: Separates foundation setup from product data-writing behavior.

### Early Minimum CI Gate

Artifacts: PRD, architecture, epics, sprint status
Section: Implementation sequencing / D17 / Epic 11

OLD:

```text
Epic 11: CI/CD & Automated Quality Pipeline appears after MVP and Phase 1.5 product work.
```

NEW:

```text
Minimum build/test CI is an early enabling prerequisite for any greenfield or restarted implementation sequence.
Semantic release, NuGet publishing, branch protection hardening, and release operations remain in the Engineering/Operational Readiness track.
```

Rationale: Keeps release automation in its operational track while making the minimum feedback gate part of the early foundation path.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is the implementation readiness assessment, not a failed implementation story.
- 1.2 Core problem: Done. Planning sequencing defect and misplaced early CI prerequisite.
- 1.3 Supporting evidence: Done. Readiness report identifies 2 critical blockers and confirms no FR coverage gap.
- 2.1 Current epic impact: Done. Epic 0 and Epic 1 needed restructuring.
- 2.2 Required epic changes: Done. Scaffold moved into Epic 0 as Story 0.0; Epic 1 now depends on Epic 0.
- 2.3 Remaining epic review: Done. Epic 11 CI placement clarified.
- 2.4 Invalidated epics/new epics: Done. No epic invalidated and no new epic required.
- 2.5 Order/priority: Done. Foundation path now precedes Epic 1; minimum CI gate is early.
- 3.1 PRD conflicts: Done. Sequencing language updated.
- 3.2 Architecture conflicts: Done. D17 updated.
- 3.3 UX conflicts: N/A. No UX behavior change.
- 3.4 Other artifacts: Done. Sprint status updated.
- 4.1 Direct adjustment: Viable. Low/medium effort, low risk.
- 4.2 Rollback: Not viable. No rollback needed.
- 4.3 MVP review: Not required. MVP remains achievable.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1 Proposal review: Done.
- 6.2 Accuracy check: Done.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: Done.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Use the corrected foundation path for future story selection and implementation handoff.
- Developer agent: Treat Story 0.0 as completed via the historical Story 1.1 artifact; do not reopen completed code solely for renumbering.
- Release maintainer: Keep minimum build/test CI available before broad implementation; keep semantic release and NuGet publishing in the operational track.
- Architect: Preserve the distinction between early feedback gate and full release automation.

Success criteria:

- The first executable path is readable as Story 0.0, 0.1, 0.2, 0.3, then Epic 1.
- Epic 1 no longer contains scaffold/foundation setup as its first story.
- Story 1.1 remains only as a historical alias.
- Minimum CI build/test is visible as an early prerequisite.
- No product requirements or FR traceability are reset.

## 7. Approval Record

JeromePiquot approved this Sprint Change Proposal for implementation on 2026-05-17.
