# Sprint Change Proposal: Sequencing and Framing Follow-Up

**Date:** 2026-05-17
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-17.md`
**Status:** Approved by JeromePiquot on 2026-05-17 and applied
**Change Scope:** Moderate - backlog sequencing, epic framing, and traceability hygiene; no product definition reset

## 1. Issue Summary

The implementation readiness assessment found that the planning set remains strong on product coverage: PRD, architecture, epics, and UX are present; all 74 PRD functional requirements are traced; and UX aligns with architecture and PRD phase boundaries.

The remaining blocker is implementation sequencing and planning hygiene. The epics still present Foundation Slice 0 tenant/case stories before the Aspire starter-template scaffold story they depend on. Additional findings ask for sharper user/operator value framing, stronger validation evidence on enabling stories, and explicit traceability rules for historical numbering and FR71 export deferral.

Evidence:

- Readiness report status: `NEEDS WORK`.
- Critical finding: `Story 0.1`, `Story 0.2`, and `Story 0.3` are ordered before `Story 1.1: Set Up Initial Project from Aspire Starter Template`.
- Major findings: Epic 1 is infrastructure-heavy in framing; operational epics need maintainer/operator outcome framing; enabling stories need concrete validation evidence.
- Minor findings: Story numbering irregularities (`2.6A`, missing detailed `8.3`) need either normalization or explicit historical preservation; FR71 must remain Phase 2 and excluded from MVP readiness accounting.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains valid but should be renamed and resequenced for implementation clarity.

- `Story 1.1` must be the first executable story in the implementation sequence because it creates the solution scaffold, contracts project, test structure, AppHost, ServiceDefaults, and composition root that Foundation Slice 0 relies on.
- Foundation Slice 0 remains the first data-safety prerequisite after scaffolding. It still gates ingestion, indexing, search, graph traversal, CLI, and later MCP data access.
- Epic 1's title should lead with the developer outcome: first tenant-scoped ingestion/search. Infrastructure remains implementation detail, not the epic identity.

Epics 11, 12, 14, and 15 remain valid as Engineering/Operational Readiness work. They should be judged by release safety, maintainer/operator decisions, and validation evidence rather than by product-feature delivery.

No epic is obsolete. No new major epic is needed.

### Story Impact

Affected story changes:

- Move or explicitly sequence `Story 1.1` before Foundation Slice 0 stories.
- Keep `Story 0.1`, `Story 0.2`, and `Story 0.3` immediately after scaffold and before any data-writing stories.
- Add validation-evidence expectations to enabling stories, especially `Story 1.2`, `Story 2.4`, `Story 11.1`, and `Story 11.2`.
- Add preflight checks to Epics 14 and 15 so deferred IDs, retrospective references, and review artifacts are verified before implementation starts.
- Preserve historical numbering where existing implementation artifacts depend on it, with explicit parser/traceability guidance.

### Artifact Conflicts

PRD:

- No PRD requirement changes are needed.
- Add or preserve explicit language that FR71 is Phase 2 traceability only and is not MVP implementation scope.

Architecture:

- No technology decision changes are needed.
- The existing Aspire Empty + Incremental Projects starter-template decision reinforces the sequencing fix.

Epics:

- Requires the primary edits: story order, Epic 1 title/framing, operational-readiness framing, validation evidence, numbering hygiene, and FR71 accounting.

UX:

- No UX change required. UX remains aligned and already phase-fenced.

Sprint status:

- If implementation artifacts already depend on historical keys, preserve keys and add explanatory comments rather than renumbering completed or in-progress work.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The product direction is not broken.
- Requirements coverage is complete.
- Architecture already supports the intended scaffold-first sequence.
- The critical issue is presentation/order, not a missing capability.
- Historical story IDs appear in sprint status and implementation artifacts, so targeted edits are safer than broad renumbering.

Effort estimate: Low to medium.
Risk level: Low.
Timeline impact: Minimal if corrected before the next implementation-readiness rerun.

Alternatives considered:

- Rollback: Not justified. The issue is planning sequence clarity, not failed code.
- MVP scope review: Not required. MVP remains achievable.
- Full replan: Not warranted. Existing artifacts are close; narrow cleanup is sufficient.

## 4. Detailed Change Proposals

### Story Sequencing

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Epic 1 detailed story order

OLD:

```text
### MVP Foundation Slice 0: Tenant and Case Bootstrap
...
### Story 0.1: Tenant Provisioning Minimum Viable Workflow
### Story 0.2: Minimal Case Bootstrap
### Story 0.3: Tenant and Case Validation Guard
### Story 1.1: Set Up Initial Project from Aspire Starter Template
```

NEW:

```text
### Story 1.1: Set Up Initial Project from Aspire Starter Template

Implementation sequence note: Story 1.1 is the first executable story. It creates the solution scaffold, AppHost, ServiceDefaults, contracts/test structure, and composition root required by every later workflow, guard, storage, and CLI story.

### MVP Foundation Slice 0: Tenant and Case Bootstrap
...
### Story 0.1: Tenant Provisioning Minimum Viable Workflow
### Story 0.2: Minimal Case Bootstrap
### Story 0.3: Tenant and Case Validation Guard
```

Rationale: Fixes the critical forward dependency without changing historical story IDs.

### Epic 1 User-Value Framing

Artifact: `_bmad-output/planning-artifacts/epics.md`
Sections: Epic list and detailed Epic 1 heading

OLD:

```text
### Epic 1: Foundation, Ingestion & Graph Edge Indexing
Developer can boot the full stack ... This epic establishes the entire infrastructure spine: Aspire AppHost, DAPR Workflows, Contracts, Redis, FalkorDB, Kreuzberg, git submodules, and the IndexGraphActivity.
```

NEW:

```text
### Epic 1: First Tenant-Scoped Memory Ingestion and Search
Developer can boot the stack, provision or select a tenant and case, ingest local content, and see it persisted and searchable across text, vector, and graph axes with tenant-safe provenance and graph edges.

Implementation foundation delivered by this epic includes Aspire AppHost, DAPR workflows, Contracts V1, Redis, FalkorDB, Kreuzberg, `references/` submodule validation, and graph indexing activities.
```

Rationale: Leads with the developer-visible outcome while preserving implementation details.

### Operational-Readiness Framing

Artifact: `_bmad-output/planning-artifacts/epics.md`
Sections: Engineering/Operational Readiness Track, Epics 11, 12, 14, and 15

OLD:

```text
Epics 11, 12, 14, and 15 appear as operational epics, but some titles and stories can still read as process containers.
```

NEW:

```text
Engineering/Operational Readiness Track:
These epics are accepted only when they produce maintainer/operator decisions and validation evidence: CI checks, release run evidence, package inventory proof, deferred-ID resolution records, runbook updates, or explicit accepted-risk entries.

Before implementing Epics 14 or 15, verify every referenced deferred ID, retrospective item, review finding, and sprint-change proposal path still exists. If a reference is stale, update the story before implementation begins.
```

Rationale: Prevents hardening epics from becoming miscellaneous technical buckets.

### Enabling Story Validation Evidence

Artifact: `_bmad-output/planning-artifacts/epics.md`
Sections: Stories 1.2, 2.4, 11.1, and 11.2

OLD:

```text
Enabling stories define technical foundations, but validation evidence is unevenly emphasized.
```

NEW:

```text
Validation evidence required:
- Story 1.2 must produce contract serialization tests and schema-compatible JSON examples.
- Story 2.4 must produce deterministic normalization tests covering BM25, cosine, graph proximity, and repeated-query stability.
- Story 11.1 must produce a named CI run/check evidence path for build, unit/contract tests, and integration-fast behavior.
- Story 11.2 must produce release dry-run or publish evidence tied to `tools/release-packages.json`.
```

Rationale: Keeps legitimate technical enablers tied to observable proof.

### Numbering Hygiene

Artifact: `_bmad-output/planning-artifacts/epics.md` and `_bmad-output/implementation-artifacts/sprint-status.yaml`
Sections: Story numbering notes

OLD:

```text
Story 2.6A appears between 2.6 and 2.7.
Epic 8 detailed stories jump from 8.2 to 8.4 while sprint status retains 8-3-data-export.
```

NEW:

```text
Historical numbering note:
Story key `2.6A` is intentionally preserved because implementation and sprint-status artifacts may already reference it. Story-key parsers must allow alphabetic suffixes.

Story key `8.3` is reserved for Phase 2 FR71 export history and is excluded from MVP readiness. The detailed MVP Epic 8 sequence intentionally continues with `8.4` and `8.5`.
```

Rationale: Avoids breaking existing artifact references while making traceability machine-readable.

### FR71 Phase 2 Traceability

Artifact: `_bmad-output/planning-artifacts/epics.md`, readiness reports, and sprint status comments

OLD:

```text
FR coverage is 100%, with FR71 deferred.
```

NEW:

```text
FR coverage is 100% traceable. MVP implementation scope is not 100% of FR1-FR74 because FR71 portable export is explicitly deferred to Phase 2 unless a later approved sprint change pulls it forward.
```

Rationale: Prevents readiness readers from treating explicit deferral as an MVP implementation commitment.

## 5. Checklist Results

- 1.1 Triggering story: Done. Trigger is readiness report finding CR-1, not a failed implementation story.
- 1.2 Core problem: Done. Sequencing/order violation plus backlog framing and traceability hygiene.
- 1.3 Supporting evidence: Done. Report identifies 1 critical, 3 major, and 2 minor issues.
- 2.1 Current epic impact: Done. Epic 1 remains valid with scaffold-first sequence.
- 2.2 Required epic changes: Done. Rename/reframe Epic 1; clarify operational track; add preflight checks.
- 2.3 Remaining epics: Done. Epics 11, 12, 14, and 15 need value/evidence framing.
- 2.4 Invalidated epics/new epics: Done. None invalidated; no new epic required.
- 2.5 Order/priority: Done. Story 1.1 must precede Foundation Slice 0 execution.
- 3.1 PRD conflicts: Done. No requirement change; FR71 phase hygiene only.
- 3.2 Architecture conflicts: Done. No decision change; architecture supports the scaffold-first sequence.
- 3.3 UX conflicts: Done. No UX edit required.
- 3.4 Other artifacts: Done. Sprint status comments should preserve historical keys if edits are approved.
- 4.1 Direct adjustment: Viable. Low/medium effort, low risk.
- 4.2 Rollback: Not viable. No rollback needed.
- 4.3 MVP review: Not required. MVP remains achievable.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1-6.2 Proposal review: Done.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: Done. Historical keys are preserved with scaffold-first and FR71 phase-accounting comments.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Apply the epics and sprint-status documentation edits after approval.
- Developer agent: Preserve historical story IDs; do not reopen completed stories solely for planning cleanup.
- Architect: Confirm no architecture decision changes are needed beyond respecting the existing Aspire Empty + Incremental Projects starter-template decision.
- Release/operations maintainer: Ensure Epics 14 and 15 start with deferred-ID and review-artifact existence checks.

Success criteria:

- `Story 1.1` appears or is explicitly marked as the first executable story before Foundation Slice 0 stories.
- Epic 1 is framed around first tenant-scoped ingestion/search, with infrastructure as implementation support.
- Epics 11, 12, 14, and 15 carry maintainer/operator outcome and validation-evidence expectations.
- Enabling stories name concrete validation artifacts.
- Story numbering irregularities are either normalized or explicitly preserved; this proposal recommends preservation.
- FR71 remains visibly Phase 2 and excluded from MVP readiness accounting.
- A follow-up readiness check no longer reports CR-1.

## 7. Approval Record

JeromePiquot approved this proposal on 2026-05-17. The planning artifact edits were applied to `_bmad-output/planning-artifacts/epics.md`, and sprint-status comments were updated without reopening completed stories.
