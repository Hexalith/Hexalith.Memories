# Sprint Change Proposal: Readiness Quality Follow-Up

**Date:** 2026-06-27
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-27.md`
**Status:** Approved by Jerome on 2026-06-27 and applied
**Mode:** Batch
**Change Scope:** Moderate - planning-artifact correction and readiness-accounting cleanup; no PRD scope reset and no product code change required by this proposal

## 1. Issue Summary

The 2026-06-27 implementation readiness assessment found that the PRD functional requirement set is fully traceable: FR1-FR74 all map to epics, stories, or explicit backlog placeholders. The project is not blocked by missing product requirements.

The remaining readiness risk is planning precision. Several artifacts still allow readers or automation to over-count MVP scope, blur story ownership, or copy historical technical-story patterns. The current repository state also shows that some findings from the report have already been remediated after or alongside the assessment:

- Story 0.4 exists and is tracked as `done` in `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Story 17.6 exists, is tracked as `done`, and records conformance evidence for FrontComposer / Fluent UI Blazor V5 hardening.
- `epics.md` already contains an Implementation Readiness Boundary and an operational-readiness acceptance-criteria guard.

This proposal therefore recommends targeted artifact edits, not a replan.

## 2. Impact Analysis

### Epic Impact

Epic 0 remains the MVP foundation path. Story 0.4 is already present, but its validation-evidence wording should be tightened so imported Story 11.1 evidence is recorded as historical evidence, not as an apparent future dependency.

Epic 1 remains valid, but Story 1.2 needs the same explicit "do not reopen as a broad technical slice" guard already applied to Stories 1.5 and 1.6.

Epic 2 and Epic 5 need clearer ownership for degraded-backend behavior. Story 2.5 should own fusion behavior when available axes are passed to the search layer. Story 5.6 should remain the canonical FR66 / NFR18 system-wide degradation policy owner.

Epic 7 and Epic 8 need CLI ownership wording cleanup. The implemented inspection command is `memories consistency inspect --tenant <tenant-id> --id <unit-id>`, owned by Epic 8 operational consistency work, not a root-level `memories inspect --id` command owned by Epic 7's MVP CLI essentials.

Epic 17 is now acceptable as a future web RCL track because Story 17.6 is done and conformance-tested. The remaining caveat is that host/browser/assistive-technology validation remains deferred and must not be implied by Story 17.6.

### Artifact Conflicts

PRD: No change required. The PRD already separates MVP, Phase 1.5, Phase 2, and Ongoing responsibilities.

Architecture: Change required. The Requirements Coverage table still says FR1-FR70 and FR72-FR74 are covered in MVP, which incorrectly includes Phase 1.5 FRs. It also marks NFR20-NFR21 protocol conformance as MVP-critical even though the PRD tags them P1.5.

Epics: Change required. Story 0.4, Story 2.5, Story 5.6, Story 8.2, and the Epic 1 historical-slice note need small wording updates. The Phase 2 FR71 placeholder and operational checkpoint stories need explicit activation/execution wording, not new scope.

UX Design: No content change required. Story 17.6 evidence should be treated as the active governance proof for current web conformance. Future host-level accessibility validation remains a known deferred gap.

Sprint Status: No status change required by this proposal. The relevant Story 0.4 and Story 17.6 entries are already present and `done`.

### Technical Impact

No code change is required to resolve the readiness findings covered here. One finding was checked against implementation artifacts: the consistency inspection CLI already exists as `memories consistency inspect --tenant <tenant-id> --id <unit-id>`.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The PRD does not need scope reduction.
- No rollback is justified.
- The remaining issues are wording, phase accounting, and ownership clarity.
- Some report findings are already closed in current artifacts, so the safest path is reconciliation, not broad rewriting.

Effort estimate: Low to medium.

Risk level: Low if the change is limited to planning artifacts; medium if implementation automation depends on exact old wording.

Timeline impact: Minimal. This is a planning cleanup before broad implementation-readiness signoff.

## 4. Detailed Change Proposals

### Proposal A: Correct Architecture FR Phase Accounting

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `Requirements Coverage`

OLD:

```text
**Functional Requirements: 73/74 covered in MVP. 1 deferred.**

| Status | FRs | Note |
|---|---|---|
| Covered (MVP) | FR1-70, FR72-74 | All architecturally supported |
| Deferred (Phase 2) | FR71 (export) | `TenantExportService.cs` added in Phase 2 |
```

NEW:

```text
**Functional Requirements: 74/74 architecturally traceable. Active MVP scope is phase-filtered.**

| Status | FRs | Note |
|---|---|---|
| Active MVP readiness | FR1-FR22, FR24-FR53, FR55-FR57, FR63-FR70, FR72-FR74 | Covered by Epic 0-Epic 8 MVP scope |
| Phase 1.5 fast-follow | FR23, FR54, FR58-FR62 | MCP and EventStore integration scope; architecturally additive, not MVP-counted |
| Deferred (Phase 2) | FR71 (export) | Portable export remains non-MVP unless an approved sprint change pulls it forward |
```

Rationale: Architecture should separate architectural support from MVP completion accounting.

### Proposal B: Correct Architecture NFR Phase Accounting

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Sections: `Cross-Cutting Concerns Identified`, item 7; `Requirements Coverage`

OLD:

```text
7. **Serialization & Protocol Conformance (NFR20-21) [MVP-critical]:** JSON everywhere. CloudEvents envelope for DAPR pub/sub. MCP protocol specification conformance for tool schemas, typed parameters, structured errors. Contract tests for serialization round-trips.
```

NEW:

```text
7. **Serialization & Protocol Conformance (phase-split):** JSON contract serialization and source-generated round-trip tests are MVP-critical. MCP protocol conformance (NFR20) and DAPR CloudEvents publisher compatibility for EventStore ingestion (NFR21) are Phase 1.5 fast-follow scope; the MVP architecture must keep these additions additive, not count them as MVP gate completion.
```

OLD:

```text
**Non-Functional Requirements: 30/31 covered in MVP. 1 deferred by design.**

| Status | NFRs | Note |
|---|---|---|
| Covered (MVP) | NFR1-10, NFR12-31 | All architecturally supported |
| Deferred (Phase 1.5) | NFR11 (ingress auth) | Matches D8 (TenantAuthorizationMiddleware) |
```

NEW:

```text
**Non-Functional Requirements: 31/31 architecturally traceable. Active MVP scope is phase-filtered.**

| Status | NFRs | Note |
|---|---|---|
| Active MVP readiness | NFR1-NFR4, NFR8, NFR16-NFR17, NFR24-NFR26, NFR30-NFR31 | MVP gate and thesis-validation NFRs |
| Phase 1.5 fast-follow | NFR6, NFR11, NFR20-NFR21 | Event freshness, ingress auth, MCP protocol conformance, CloudEvents publisher compatibility |
| Ongoing / operational hardening | NFR5, NFR7, NFR9-NFR10, NFR12-NFR15, NFR18-NFR19, NFR22-NFR23, NFR27-NFR29 | Required but not all MVP gate-blocking; implemented through MVP and operational-readiness tracks as selected |
```

Rationale: This matches the PRD phase tags and avoids overstating active MVP readiness.

### Proposal C: Tighten Story 0.4 Evidence Wording

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `0.4 - Minimum Build/Test CI Preflight`

OLD:

```text
**Validation Evidence Required:** Story completion must name the build command, Docker-free test command, CI check names if available, and the evidence location. This story may be satisfied by migrated evidence from Story 11.1 if the minimum gate already exists.
```

NEW:

```text
**Validation Evidence Required:** Story completion must name the build command, Docker-free test command, CI check names if available, and the evidence location. If the minimum gate already exists, Story 0.4 may cite evidence originally produced under Story 11.1 as imported historical evidence. That citation does not create a dependency on future Story 11.1 completion.
```

Rationale: Keeps the useful history while removing forward-dependency ambiguity.

### Proposal D: Split Degraded-Backend Ownership Between Stories 2.5 and 5.6

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `2.5 - Fusion Algorithm & Hybrid Search`

OLD:

```text
**Given** one search backend is unavailable during hybrid search
**When** the remaining axes return results
**Then** the system returns partial results with a `degraded: true` flag indicating which axes were unavailable
```

NEW:

```text
**Given** hybrid search receives axis-availability information from the system degradation policy
**When** one axis is unavailable and the remaining axes return results
**Then** the fusion layer combines only the available axes
**And** the response carries degraded search metadata naming the excluded axis.

**Ownership note:** Story 2.5 owns search-layer fusion behavior over available axes. Story 5.6 owns the system-wide backend availability policy, health detection, chaos/degradation verification, and FR66/NFR18 degraded-service contract.
```

Story: `5.6 - Graceful Degradation on Backend Failure`

Add after the story summary:

```text
**Ownership note:** Story 5.6 is the canonical owner of FR66 and NFR18 degraded-service behavior. Search stories consume the availability/degradation result; they do not define backend health policy independently.
```

Rationale: Avoids duplicate ownership while keeping both search behavior and system policy testable.

### Proposal E: Correct the Consistency Inspect CLI Command

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `8.2 - Consistency Verification & Repair`

OLD:

```text
**Given** a per-unit consistency inspection
**When** I run `memories inspect --id <unit-id>`
**Then** the system queries all three backends for that specific unit's state
```

NEW:

```text
**Given** a per-unit consistency inspection
**When** I run `memories consistency inspect --tenant <tenant-id> --id <unit-id>`
**Then** the system queries all three backends for that specific unit's state
```

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `Cross-Cutting Concerns Identified`, item 9

OLD:

```text
Per-memory-unit consistency inspection (`memories inspect --id <unit-id>`) queries all three backends for a single unit's state — essential for developer debugging.
```

NEW:

```text
Per-memory-unit consistency inspection (`memories consistency inspect --tenant <tenant-id> --id <unit-id>`) queries all three backends for a single unit's state — essential for operator and developer debugging. This command is owned by Epic 8 operational consistency work, not by the root MVP CLI essentials list in Epic 7.
```

Rationale: Aligns the planning text with the implemented CLI group and keeps Epic 7 scope from expanding accidentally.

### Proposal F: Make Historical Technical-Slice Guards Binding for Story 1.2

Artifact: `_bmad-output/planning-artifacts/epics.md`

Location: `Implementation Readiness Amendment (2026-05-18)` and Story 1.2

OLD:

```text
**Implementation Readiness Amendment (2026-05-18):** Stories 1.2 through 1.5 are accepted as historical technical slices, but they must not be used as a pattern for future work without an observable proof gate.
```

NEW:

```text
**Implementation Readiness Amendment (2026-05-18; reaffirmed 2026-06-27):** Stories 1.2, 1.5, and 1.6 are accepted as historical broad technical or bundled infrastructure slices, but they are not valid patterns for future story creation. Any reopened, reimplemented, or analogous work must be split into independently demonstrable vertical stories with CLI/API/contract/trace/integration evidence. Internal classes, mocks, or green unit tests alone are not sufficient completion evidence.
```

Add before Story 1.2 acceptance criteria:

```text
**Historical Scope Guard:** Do not reopen Story 1.2 as a single broad contract/model implementation unit. If contract rework resumes, split it into separate numeric stories such as memory-unit contract shape, graph-edge contract shape, Evidence Packet/error contract mapping, and downstream serialization/consumer fixture proof.
```

Rationale: Stories 1.5 and 1.6 already have explicit historical guards. Story 1.2 should have the same protection.

### Proposal G: Clarify Operational Checkpoint and FR71 Activation Rules

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Engineering/Operational Readiness Track`

Add:

```text
Operational checkpoint stories may remain umbrella tracking stories, but each checkpoint must be implemented, reviewed, and evidenced as a separately verifiable slice. A checkpoint cannot be marked complete only because the umbrella story is complete.
```

Section: `Phase 2 Backlog Placeholders`

Add to the FR71 placeholder:

```text
Activation rule: when FR71 becomes active, create a normal Story 8.3 story file and sprint-status entry before implementation. Until then, this placeholder remains `reserved-non-mvp` and must not be counted as a missing MVP story or as active MVP readiness.
```

Rationale: Preserves current placeholders while making future execution rules explicit.

### Proposal H: Preserve Story 17.6 as the Web UX Governance Gate

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/17-6-frontcomposer-fluent-ui-blazor-v5-conformance-hardening.md`
- `_bmad-output/implementation-artifacts/epic-17-retro-2026-06-24.md`

No new story is required because Story 17.6 is already `done` and records:

- `Hexalith.Memories.Web` conformance audit.
- Fluent UI Blazor V5 / FrontComposer component preference.
- Fluent 2 token and legacy-token checks.
- A fail-closed allowlist.
- Web test evidence: full suite 475/475, validation namespace 184/184, conformance hardening/remediation tests 15/15, and `git diff --check` clean.

Add a short note to Epic 17:

```text
**Readiness note:** Story 17.6 is the current conformance gate for the host-less web RCL. It does not close product-route browser, axe, forced-colors, reduced-motion, zoom/reflow, touch, or manual screen-reader validation gaps; those remain future web-host validation work and must be sprint-selected separately.
```

Rationale: The readiness report's web warning should be closed against existing 17.6 evidence while preserving the remaining accessibility validation boundary.

## 5. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / planning maintainer: approve the wording changes and apply the artifact edits to `architecture.md` and `epics.md`.
- Developer agent: after approval, make the artifact-only edits with no product-code changes unless a referenced implementation discrepancy is discovered.
- Test Architect / QA reviewer: re-run implementation readiness or targeted document checks after edits to confirm the nine findings are closed or explicitly classified as already resolved.

Success criteria:

- Architecture no longer counts Phase 1.5 or Phase 2 FRs/NFRs as active MVP completion.
- Story 0.4 no longer reads as dependent on future Story 11.1 completion.
- Story 2.5 and Story 5.6 have non-overlapping degradation ownership.
- Story 8.2 and architecture use the implemented `memories consistency inspect --tenant <tenant-id> --id <unit-id>` command.
- Story 1.2 has the same historical-slice guard as Stories 1.5 and 1.6.
- FR71 remains reserved-non-MVP until explicitly activated.
- Story 17.6 is recognized as done while host/browser accessibility validation remains a separate future web concern.

## Checklist Execution Notes

| Item | Status | Notes |
|---|---|---|
| 1.1 Trigger identified | [x] Done | Trigger is the 2026-06-27 readiness report. |
| 1.2 Core problem defined | [x] Done | Planning precision, phase accounting, and story ownership. |
| 1.3 Evidence gathered | [x] Done | Read readiness report, PRD scope, epics sections, architecture coverage, UX governance, sprint status, and Story 17.6 evidence. |
| 2.1-2.5 Epic impact assessed | [x] Done | Affected epics: 0, 1, 2, 5, 7, 8, 17; no new epic required. |
| 3.1 PRD conflict check | [x] Done | No PRD change required. |
| 3.2 Architecture conflict check | [x] Done | Applied Proposals A, B, and E. |
| 3.3 UX conflict check | [x] Done | Story 17.6 evidence closes current conformance governance; browser/AT gaps remain future work. |
| 3.4 Secondary artifact check | [x] Done | Applied `epics.md` wording edits; no sprint-status change required. |
| 4.1 Direct Adjustment | [x] Viable | Recommended. |
| 4.2 Potential Rollback | [N/A] Skip | No implementation rollback needed. |
| 4.3 PRD MVP Review | [N/A] Skip | MVP scope remains valid. |
| 4.4 Path selected | [x] Done | Direct Adjustment. |
| 5.1-5.5 Proposal components | [x] Done | Included issue summary, impact, approach, action plan, and handoff. |
| 6.1-6.2 Final review | [x] Done | Draft is internally consistent with current artifacts. |
| 6.3 User approval | [x] Done | Jerome approved on 2026-06-27; artifact edits applied. |
| 6.4 Sprint-status update | [N/A] Skip | No sprint-status update proposed; Story 0.4 and Story 17.6 are already tracked. |
| 6.5 Handoff plan | [x] Done | Route to Product Owner / Developer agents after approval. |

## Final Handoff

Approved implementation has been applied to `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/planning-artifacts/epics.md`.

Scope classification: **Moderate**.

Routed to: Product Owner / Developer agents for artifact maintenance and readiness re-check.

Next success check: rerun targeted readiness validation and confirm the nine findings are either closed by these edits or explicitly classified as already resolved by existing Story 0.4 and Story 17.6 evidence.
