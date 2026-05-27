# Sprint Change Proposal: Implementation Readiness Cleanup

**Date:** 2026-05-17
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-17.md`
**Status:** Approved by JeromePiquot on 2026-05-17
**Change Scope:** Moderate - backlog and planning hygiene; no product definition reset

## 1. Issue Summary

The implementation readiness assessment found that the planning set is strong on requirements coverage but still needs cleanup before a low-ambiguity implementation handoff. PRD, architecture, epics, and UX are present and aligned at the product level, and all 74 PRD functional requirements are traced into active or intentionally deferred work.

The remaining risk is backlog shape: phase boundaries, operational-readiness labeling, greenfield CI sequencing, release package inventory consistency, and a small number of story-quality issues could cause implementers to treat non-MVP work as MVP work or validate against conflicting acceptance criteria.

Primary issues from the readiness report:

- Epic 7 has a quickstart phase conflict: the summary says full quickstart polish is Phase 1.5, while Story 7.4 places guided quickstart and the under-30-minute onboarding gate in MVP Gate 3.
- Epics 11, 12, 14, and 15 are mostly engineering/release-hardening work and should be consistently labeled as an Engineering/Operational Readiness track.
- CI/CD appears late for a greenfield sequence; a minimal build/test gate should be treated as an early enabling story or explicit historical prerequisite.
- Story 11.2 and Story 12.1 disagree on the release package inventory.
- Story 1.6 is unusually large and should be split for future implementation reuse, or explicitly preserved as historical completed scope with smaller verification slices.
- Epic 8 story shape needs cleanup or explicit reframing for telemetry test/instrumentation work.

## 2. Impact Analysis

### Epic Impact

Epic 7 remains valid but needs a precise phase rule:

- MVP must include a README-driven quickstart that proves NFR31: first successful search in under 30 minutes.
- The interactive `memories quickstart` command should be Phase 1.5 CLI polish unless explicitly pulled forward.
- Empty-state text may mention `memories quickstart` as future/optional guidance only if the command exists; otherwise the MVP text should point to the README quickstart.

Epics 11, 12, 14, and 15 remain valuable but should be treated as an Engineering/Operational Readiness track. They do not need to be removed from `epics.md`, but their track label and quality criteria should be explicit in both the summary and detailed sections.

Epic 11 should be split in interpretation:

- Minimal CI/build/test gate is an early enabling prerequisite for any greenfield or restarted implementation sequence.
- Semantic release, NuGet publish automation, branch protection, and release hardening can remain in Epic 11/12.

Epic 1 remains completed, but Story 1.6 should not be reused as a future implementation template in its current size. Any future rework should split it into smaller vertical slices.

Epic 8 remains valid, and current local docs already contain personas for Story 8.4 and Story 8.5. The remaining cleanup is to formally frame Story 8.4 as release-manager verification value and Story 8.5 as operator-observable Redis trace value, not as test-only work.

### Story Impact

Required story-level changes:

- Update Story 7.4 so README quickstart and help completeness are MVP, while interactive `memories quickstart` is Phase 1.5 unless explicitly pulled forward.
- Add or identify an early CI prerequisite story for minimum build/test validation if this plan is used as a from-scratch implementation sequence.
- Normalize Story 11.2 and Story 12.1 around the authoritative release inventory in `tools/release-packages.json`.
- Add a note to Story 1.6 that future reimplementation should split happy path, failure/compensation visibility, and restart/idempotency hardening into separate slices.
- Preserve the Story 8.4 and 8.5 persona cleanup already present locally and add a short outcome summary if implementers still find those stories too instrumentation-heavy.

### Artifact Conflicts

PRD:

- The PRD already distinguishes MVP CLI essentials from Phase 1.5 CLI expansion, but the journey/empty-state language still mentions guided quickstart. It should state that the MVP guarantee is README quickstart under 30 minutes, while interactive quickstart is optional Phase 1.5 polish.
- The package section says "10 packages - 8 published NuGet + 2 internal Aspire projects", but current release tooling lists 7 published packages plus 3 non-packable projects. This should be normalized to the tooling source of truth.

Architecture:

- Architecture already states that full CLI polish, including `quickstart`, is Phase 1.5. It should cross-reference the README quickstart as the MVP NFR31 validation path to avoid conflict with Story 7.4.
- Architecture package inventory should reference `tools/release-packages.json` as the release package source of truth.

Epics:

- Epic 7 needs the quickstart phase clarification.
- Epics 11, 12, 14, and 15 need consistent Engineering/Operational Readiness labels.
- Story 11.2 and Story 12.1 need one package list.
- Story 1.6 needs sizing guidance for future work.
- Story 8.4 and Story 8.5 should keep their persona lines and be framed around operator/release-manager outcomes.

Sprint status:

- If a new early CI prerequisite story is added, track it as done if it reflects already-completed historical work, or backlog if it is still missing.
- No completed story should be retroactively reopened solely for planning hygiene; add follow-up stories or notes instead.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The product definition is not broken.
- Requirements coverage is complete.
- No critical blockers were found.
- Most fixes are phase-accounting and backlog-shape corrections.
- Several findings are already partially addressed in the local artifact set, so a targeted cleanup is safer than a replan.

Effort estimate: Low to medium.
Risk level: Low.
Timeline impact: Minimal if the cleanup is completed before assigning the next implementation batch.

Alternatives considered:

- Rollback: Not justified. These are planning-quality defects, not failed implementation outputs.
- MVP review: Not required. MVP remains achievable after phase and release-inventory cleanup.
- Major replan: Not warranted. The existing PRD, architecture, UX, and epics remain directionally consistent.

## 4. Detailed Change Proposals

### Epic 7 Quickstart Phase Boundary

Story: Story 7.4 - Quickstart & Documentation
Section: Acceptance Criteria

OLD:

```text
Given the `memories quickstart` command
When executed
Then it provides an interactive guided setup: verify prerequisites (Docker, DAPR), boot the stack, create a tenant, create a case, ingest a sample document, run a search (FR57)
And each step provides clear instructions and validates success before proceeding
```

NEW:

```text
Given the MVP README quickstart
When a developer follows it on a clean machine with Docker installed
Then they can boot the stack, create or select a tenant and case, ingest a sample document, and complete a first successful search in under 30 minutes (NFR31)
And the steps use only MVP CLI essentials.

Given the interactive `memories quickstart` command
When CLI Phase 1.5 polish is in scope
Then it provides guided setup with prerequisite checks, stack boot guidance, tenant and case creation, sample ingestion, and first search.
And it is not required to satisfy MVP Gate 3 unless explicitly pulled forward by a later sprint change.
```

Rationale: Keeps NFR31 as an MVP hard gate while preserving the richer interactive command as Phase 1.5 polish.

### Empty-State Quickstart Text

Artifact: PRD / Epics
Section: Empty search guidance and Story 7.3

OLD:

```text
Run `memories quickstart` for a guided setup.
```

NEW:

```text
Follow the README quickstart for a guided setup.
If the Phase 1.5 `memories quickstart` command is installed, run `memories quickstart`.
```

Rationale: Prevents MVP output from referencing a command that is explicitly Phase 1.5 unless it has been pulled forward.

### Engineering/Operational Readiness Track

Artifact: Epics
Section: Epic 11, Epic 12, Epic 14, Epic 15 summaries

OLD:

```text
Epics 11, 12, 14, and 15 appear in the same epic list as product capability epics.
```

NEW:

```text
Engineering/Operational Readiness Track:
- Epic 11: CI/CD & Automated Quality Pipeline
- Epic 12: First Release & Operations Foundation
- Epic 14: Deferred Work Hardening and Operational Readiness
- Epic 15: Carry-Forward Operational Risk Closure

These epics are judged by delivery safety, release integrity, maintainer/operator outcomes, and validation evidence, not by the same product-capability standard used for feature epics.
```

Rationale: Retains the work while making the quality standard explicit.

### Early Minimal CI Gate

Artifact: Epics / Sprint Status
Section: Epic 11 or Foundation Slice 0

OLD:

```text
Epic 11 appears after MVP and Phase 1.5 product capabilities.
```

NEW:

```text
Minimum build/test CI is an enabling prerequisite for greenfield implementation.
Semantic release, NuGet publishing, release preflight, and branch-protection hardening remain in Epic 11/12.
```

Rationale: Satisfies greenfield sequencing expectations without moving the entire release automation epic earlier.

### Release Package Inventory

Stories: Story 11.2 and Story 12.1
Section: Acceptance Criteria

OLD:

```text
Story 11.2: all 8 published NuGet packages are built and published: Contracts, Client, Client.Rest, Server, Redis, Cli, Mcp, EventStore.

Story 12.1: pack-release.ps1 produces 7 packages, then lists Contracts, Client.Rest, Redis, Cli, Mcp, EventStore, Telemetry.
```

NEW:

```text
The authoritative release inventory is `tools/release-packages.json`.
As of 2026-05-17 it lists 7 published packages:
- Hexalith.Memories.Contracts
- Hexalith.Memories.Client.Rest
- Hexalith.Memories.Redis
- Hexalith.Memories.Cli
- Hexalith.Memories.Mcp
- Hexalith.Memories.EventStore
- Hexalith.Memories.Telemetry

The following projects are explicitly non-packable:
- Hexalith.Memories.Server
- Hexalith.Memories.AppHost
- Hexalith.Memories.ServiceDefaults
```

Rationale: Aligns stories with release tooling and removes ambiguity around `Client`, `Server`, and `Telemetry`.

### Story 1.6 Sizing

Story: Story 1.6 - Ingestion Workflow Orchestration
Section: Implementation note

OLD:

```text
Story 1.6 covers validation, extraction, embedding, fan-out indexing, consistency verification, compensation, failure status, provenance, DAPR sidecar restart recovery, and duplicate detection.
```

NEW:

```text
Story 1.6 is historical completed scope. Future reimplementation or major rework must split it into smaller vertical stories:
1. Happy-path local file ingestion orchestration.
2. Failure, compensation, and failed-unit visibility.
3. Restart recovery, idempotency, and duplicate detection hardening.
```

Rationale: Avoids reopening completed work while preventing future agents from copying an oversized story shape.

### Epic 8 Story Shape

Stories: Story 8.4 and Story 8.5
Section: Story opening and source/outcome notes

OLD:

```text
Telemetry tests and Redis instrumentation can read as implementation-test work only.
```

NEW:

```text
Story 8.4 outcome: release manager can prove distributed trace and audit-event behavior on real infrastructure before release promotion.
Story 8.5 outcome: operator can attribute Redis-backed search and traversal latency inside the same distributed trace as the originating request.
```

Rationale: Keeps the implementation/testing work but anchors it in release-manager and operator value.

## 5. Checklist Results

- 1.1 Triggering story: N/A. Trigger is readiness assessment, not one implementation story.
- 1.2 Core problem: Done. Planning hygiene and phase-accounting defects.
- 1.3 Supporting evidence: Done. Report identifies no critical blockers, 6 major issues, minor story concerns, and UX/architecture warnings.
- 2.1 Current epic impact: Done. Epic 7 is the main product-epic impact.
- 2.2 Required epic changes: Done. Epic 7 clarification, operational-readiness track labeling, early CI prerequisite.
- 2.3 Remaining epic review: Done. Epics 1, 8, 11, 12, 14, and 15 affected.
- 2.4 Invalidated epics/new epics: Done. No epics invalidated; no major new epic required.
- 2.5 Order/priority: Done. Minimal CI should be treated as early prerequisite; release hardening remains later.
- 3.1 PRD conflicts: Done. Quickstart language and package inventory need normalization.
- 3.2 Architecture conflicts: Done. Architecture needs source-of-truth package inventory reference and README quickstart MVP note.
- 3.3 UX conflicts: Done. UX remains aligned; no direct UX revision required beyond preserving phase fences.
- 3.4 Other artifacts: Done. `sprint-status.yaml`, release tooling references, and implementation handoff notes may need updates.
- 4.1 Direct adjustment: Viable. Low/medium effort, low risk.
- 4.2 Rollback: Not viable. No rollback needed.
- 4.3 MVP review: Not required. MVP remains achievable.
- 4.4 Recommended path: Done. Direct adjustment.
- 5.1-5.5 Proposal components: Done.
- 6.1 Proposal review: Done.
- 6.2 Accuracy check: Done.
- 6.3 User approval: Done. JeromePiquot approved this proposal on 2026-05-17.
- 6.4 Sprint status update: Done. Story 11.1 is documented as the historical early CI/build/test gate; no completed story was reopened.
- 6.5 Handoff plan: Done.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: Apply the proposed backlog and phase-accounting edits to PRD, epics, architecture, and sprint status.
- Developer agent: Do not reopen completed implementation stories solely for planning cleanup. Add follow-up notes or stories where needed.
- Release maintainer: Keep `tools/release-packages.json` as the authoritative package inventory and align Story 11.2/12.1 wording with it.
- Architect: Preserve the distinction between MVP README quickstart validation and Phase 1.5 interactive CLI quickstart.
- UX Designer: No immediate UX redesign required; keep CLI/MCP/web phase fences clear.

Success criteria:

- Epic 7 no longer conflicts on guided quickstart phase.
- NFR31 remains MVP and is validated by README quickstart under 30 minutes.
- `memories quickstart` is not required for MVP unless explicitly pulled forward.
- Epics 11, 12, 14, and 15 are clearly marked as Engineering/Operational Readiness.
- Minimum CI/build/test gate is represented as an early prerequisite or documented historical completion.
- Story 11.2 and Story 12.1 use the same release package inventory as `tools/release-packages.json`.
- Story 1.6 has future sizing guidance without reopening completed work.
- Epic 8 telemetry stories are framed around operator/release-manager outcomes.

## 7. Approval Record

JeromePiquot approved this Sprint Change Proposal for implementation on 2026-05-17.
