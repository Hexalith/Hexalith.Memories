# Sprint Change Proposal: Implementation Readiness Cleanup

**Date:** 2026-05-17
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `implementation-readiness-report-2026-05-17.md`
**Status:** Approved by JeromePiquot on 2026-05-17
**Change Scope:** Moderate - backlog and planning hygiene; no product definition reset

## 1. Issue Summary

The implementation readiness assessment found strong requirements coverage but weak planning hygiene. The PRD, architecture, epics, and UX documents agree on the product direction, and all 74 functional requirements are covered. The blockers are sequencing and phase-accounting problems that could cause implementation to start with hidden prerequisites or non-MVP work counted as MVP readiness.

Primary issues:

- Foundation Slice 0 was described as a prerequisite but was not represented as executable stories before Epic 1.
- Story 7.1 required the MVP CLI to use `Client.Rest`, while architecture places `Hexalith.Memories.Client.Rest` in Phase 1.5.
- FR71 export and later operational hardening work could be mistaken for MVP readiness work.
- UX guidance correctly describes CLI, MCP, and future web surfaces, but needed an explicit phase boundary so web/FrontComposer scope is not treated as MVP implementation scope.

Evidence:

- Readiness report status: `NEEDS WORK`.
- Critical finding: Foundation Slice 0 sequencing ambiguity.
- Major finding: MVP CLI / `Client.Rest` phase conflict.
- Major finding: deferred/export/hardening work must stay out of MVP readiness accounting.
- UX warnings: FrontComposer/Fluent UI and web implementation are future-surface guidance unless explicitly pulled forward.

## 2. Impact Analysis

### Epic Impact

Epic 1 remains valid, but it now has an explicit Foundation Slice 0 prerequisite with executable stories:

- Story 0.1: Tenant Provisioning Minimum Viable Workflow
- Story 0.2: Minimal Case Bootstrap
- Story 0.3: Tenant and Case Validation Guard

Epic 5 remains the full tenant lifecycle epic. Story 5.1 deepens tenant provisioning, but no longer carries the ambiguity of being both a later Epic 5 story and an implicit Epic 1 prerequisite.

Epic 7 remains the MVP developer-experience gate, but Story 7.1 no longer depends on Phase 1.5 `Client.Rest`.

Epic 8 remains the MVP operations epic for health, consistency verification, repair, logging, traces, and metrics. FR71 export is preserved only as Phase 2/non-MVP traceability.

### Story Impact

Added or clarified:

- Added Story 0.1, Story 0.2, and Story 0.3 to make Foundation Slice 0 executable.
- Updated Story 7.1 so MVP CLI uses a minimal direct HTTP/ingress adapter owned by the CLI.
- Moved former Story 8.3 export scope into a Phase 2 backlog placeholder/non-MVP gate.

No story was removed. Historical completed work remains visible, but readiness accounting now distinguishes MVP from post-MVP and Phase 2 work.

### Artifact Conflicts

Resolved:

- PRD package/dependency matrix no longer makes MVP CLI depend on `Client.Rest`.
- Architecture now states that the MVP CLI adapter is local to the CLI, with reusable `Client.Rest` deferred to Phase 1.5.
- Epics now include executable Foundation Slice 0 stories.
- UX now states that CLI-visible and contract-visible evidence semantics are MVP scope, while FrontComposer/Fluent UI web implementation becomes binding only in an approved web phase.
- `sprint-status.yaml` now tracks Foundation Slice 0 and flags export as non-MVP historical work.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The product definition is not broken.
- PRD coverage is complete.
- Architecture decisions already support the intended split.
- The fixes are document and backlog-hygiene changes, not a strategic pivot.

Effort estimate: Low to medium.
Risk level: Low.
Timeline impact: Minimal, provided implementation work treats Story 0.1-0.3 as prerequisite context before any future Epic 1-style data-writing work.

Alternatives considered:

- Rollback: Not justified. The issue is planning clarity, not failed implementation.
- MVP Review: Not required. MVP remains achievable after phase and sequencing cleanup.

## 4. Detailed Change Proposals

### Epics

Foundation Slice 0:

OLD:

```text
Foundation Slice 0 described prerequisite tenant/case behavior, but had no executable stories before Epic 1.
```

NEW:

```text
Story 0.1: Tenant Provisioning Minimum Viable Workflow
Story 0.2: Minimal Case Bootstrap
Story 0.3: Tenant and Case Validation Guard
```

Rationale: Tenant/case foundations must be explicitly executable before ingestion, indexing, search, or graph writes.

Story 7.1:

OLD:

```text
Then it uses the REST API via infrastructure ingress (Client.Rest package).
```

NEW:

```text
Then it uses the minimal direct HTTP/ingress adapter owned by the CLI for the thesis-validation command set.
And it does not depend on the Phase 1.5 Client.Rest package to satisfy MVP Gate 3.
```

Rationale: Resolves the MVP CLI / Phase 1.5 package conflict without changing CLI command semantics.

Former Story 8.3:

OLD:

```text
Story 8.3 appeared inside MVP Operations while being labeled Phase 2.
```

NEW:

```text
FR71 export is a Phase 2 backlog placeholder/non-MVP gate and is excluded from MVP readiness accounting.
```

Rationale: Preserves traceability while preventing MVP accounting confusion.

### PRD

OLD:

```text
Client libraries in MVP included Client.Rest, and Hexalith.Memories.Cli depended on Client.Rest.
```

NEW:

```text
MVP CLI uses a minimal direct HTTP/ingress adapter inside the CLI.
Client and Client.Rest are reusable Phase 1.5 client packages.
```

Rationale: Aligns PRD implementation dependencies with architecture phase order.

### Architecture

OLD:

```text
Client.Rest was Phase 1.5, but CLI communication language could be read as requiring the reusable REST client package.
```

NEW:

```text
The MVP CLI owns a small direct HTTP/ingress adapter. Phase 1.5 introduces or extracts reusable Client.Rest.
```

Rationale: Keeps MVP Gate 3 independent from a Phase 1.5 package.

### UX Design

OLD:

```text
CLI, MCP, and web UI were all described as first-class surfaces without an explicit MVP phase fence.
```

NEW:

```text
The UX document is full-horizon guidance. MVP scope is CLI-first plus shared Evidence Packet/state grammar; MCP/EventStore are Phase 1.5, and FrontComposer/Fluent UI web composition is future work unless pulled forward.
```

Rationale: Prevents web-surface guidance from becoming accidental MVP scope.

### Sprint Status

OLD:

```text
Foundation Slice 0 was not represented in sprint status, and export had no local non-MVP accounting note.
```

NEW:

```text
foundation-slice-0: done
0-1-tenant-provisioning-minimum-viable-workflow: done
0-2-minimal-case-bootstrap: done
0-3-tenant-and-case-validation-guard: done
# Phase 2 / non-MVP historical work; excluded from MVP readiness accounting.
8-3-data-export: done
```

Rationale: Makes the prerequisite slice visible to tracking while preserving historical export status.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is readiness assessment, not one implementation story.
- 1.2 Core problem: Done. Planning hygiene and phase-accounting defect.
- 1.3 Supporting evidence: Done. Report identifies 1 critical issue, 5 major issues, 3 minor concerns, and 3 UX warnings.
- 2.1-2.5 Epic impact: Done. Epics 1, 5, 7, 8, and UX-facing future scope are affected.
- 3.1 PRD conflict: Done. CLI dependency corrected.
- 3.2 Architecture conflict: Done. MVP CLI adapter clarified.
- 3.3 UX conflict: Done. Full-horizon vs MVP phase boundary clarified.
- 3.4 Secondary artifacts: Done. `sprint-status.yaml` updated.
- 4.1 Direct adjustment: Viable. Low risk.
- 4.2 Rollback: Not viable. No rollback needed.
- 4.3 MVP review: Not required. Scope remains achievable.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1-6.2 Review: Done.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: Done.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Treat this proposal as the planning correction record and keep readiness accounting aligned to the new phase boundaries.
- Developer agent: When implementing or reviewing MVP readiness, verify Story 0.1-0.3 prerequisites before any data-writing work and do not require `Client.Rest` for MVP CLI validation.
- Architect: Preserve Phase 1.5 client package extraction as additive, not a blocker for MVP Gate 3.
- UX Designer: Keep Evidence Packet/state grammar binding for MVP contracts; treat FrontComposer/Fluent UI web patterns as future-phase implementation guidance.

Success criteria:

- Foundation Slice 0 is visible and executable in epics and sprint status.
- MVP CLI no longer has a Phase 1.5 `Client.Rest` dependency.
- FR71 export and operational hardening work are not counted as MVP readiness.
- UX web guidance is phase-fenced.
- A follow-up readiness check reports no critical sequencing issue and no CLI/client phase conflict.
