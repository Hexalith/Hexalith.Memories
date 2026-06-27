# Sprint Change Proposal: Implementation Readiness Course Correction

**Date:** 2026-06-27
**Project:** Hexalith.Memories
**Trigger:** Implementation Readiness Assessment `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-27.md`
**Status:** Approved by Jerome on 2026-06-27 and applied
**Mode:** Batch
**Change Scope:** Moderate - backlog and planning-artifact correction; no PRD, architecture, or UX reset

## 1. Issue Summary

The 2026-06-27 implementation readiness assessment found that the planning artifacts are substantially complete but not ready to execute as written. The report found 14 issues and marked two as critical:

1. The minimum CI preflight needed before Epic 1 data-writing work is defined as a dependency on Story 11.1, which lives in a later operational-readiness epic.
2. Stories 18.5 and 18.6 are explicitly mutually dependent, so they are not independently completable as separate stories.

The issues are planning and sequencing defects, not product or architecture failures. The PRD remains traceable, every PRD functional requirement is mapped, and the architecture already supports the intended CI-first and downstream-consumer-contract direction.

Important repository-state note: `_bmad-output/implementation-artifacts/sprint-status.yaml` currently marks Epics 0-18 as `done`, including Stories 11.1, 18.5, and 18.6. This proposal therefore preserves implementation history and recommends targeted planning corrections instead of reopening completed implementation stories solely for artifact hygiene.

## 2. Impact Analysis

### Epic Impact

Epic 0 needs to explicitly own the minimum build/test CI preflight because it is part of the executable foundation path before Epic 1 data writes. This should be represented as a migrated Story 0.4 rather than a forward dependency into Epic 11.

Epic 1 remains valid. The change removes its dependency on a future operational story and makes its prerequisite path self-contained: Epic 0 complete, then Epic 1 product-capability stories.

Epic 11 remains valid as an operational-readiness epic. Story 11.1 should become the full CI quality pipeline story that extends the minimum preflight, rather than the source of the Epic 1 preflight prerequisite.

Epic 18 remains valid as downstream consumer integration hardening, but Stories 18.5 and 18.6 need non-circular contract language. Because both are already tracked and completed, preserve the story keys and status history; correct the story text so Story 18.6 can stand alone as the stability contract, and Story 18.5 consumes that contract when lookup implementation or future rework is considered.

### Story Impact

Add a planning story:

- Story 0.4: Minimum Build/Test CI Preflight.

Amend existing stories:

- Story 11.1: remove "source of Epic 1 preflight" responsibility; keep broader CI pipeline and release-readiness extension responsibilities.
- Story 18.5: remove circular dependency wording and depend only on an already-published or separately completed stability contract.
- Story 18.6: remove dependency on Story 18.5 as required completion proof; publish source-identity and `MemoryUnitId` stability guidance that is useful even before a lookup endpoint exists.

### Artifact Conflicts

PRD: No change required. The contributor journey already describes clean build, tests, and CI as infrastructure expectations, and the test strategy already separates unit, integration, and contract tests.

Architecture: No redesign required. Architecture Decision D17 already says the GitHub Actions minimum build/test gate comes first, then full semantic release. The only optional architecture cleanup is to align the "First Implementation Steps" list with Story 0.4 wording.

UX: No change required. The critical issues do not affect user journeys, screens, or evidence UX.

Epics: Required changes. `epics.md` must add Story 0.4 and replace the 18.5/18.6 mutual-dependency wording.

Sprint status: Required after approval. Add `0-4-minimum-build-test-ci-preflight: done` as a historical migration from Story 11.1. No status transition is needed for 18.5 or 18.6.

### Technical Impact

No code change is required by this proposal. The implementation records indicate the affected work is already done. The correction is to make future readiness, sprint-status rollups, and story automation reflect the real prerequisite shape.

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The critical issues can be fixed by moving prerequisite ownership and removing circular story wording.
- PRD scope, MVP boundaries, architecture, and UX remain valid.
- Rollback would create unnecessary churn because the implementation status already records the relevant stories as done.
- MVP review is not needed because no product goal or requirement has changed.

Effort estimate: Low to medium.
Risk level: Low if completed as planning-history correction; medium if completed by renumbering or merging completed story history.
Timeline impact: Minimal. The action is an artifact update, not a code delivery effort.

## 4. Detailed Change Proposals

### Proposal A: Add Story 0.4 for Minimum Build/Test CI Preflight

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: `Selected Implementation Scope`

OLD:

```text
1. Story 0.0: Project Scaffolding & Single-Command Boot (historical alias: Story 1.1)
2. Story 0.1: Tenant Provisioning Minimum Viable Workflow
3. Story 0.2: Minimal Case Bootstrap
4. Story 0.3: Tenant and Case Validation Guard
5. Epic 1 data-writing ingestion/search stories

Minimum build/test CI is an early enabling prerequisite for any greenfield or restarted implementation sequence. The historical CI story remains Story 11.1, but implementation handoff must treat the minimum build/test gate as active before broad product work begins. Semantic release, NuGet publishing, branch protection hardening, and release operations remain in the Engineering/Operational Readiness track.
```

NEW:

```text
1. Story 0.0: Project Scaffolding & Single-Command Boot (historical alias: Story 1.1)
2. Story 0.1: Tenant Provisioning Minimum Viable Workflow
3. Story 0.2: Minimal Case Bootstrap
4. Story 0.3: Tenant and Case Validation Guard
5. Story 0.4: Minimum Build/Test CI Preflight
6. Epic 1 data-writing ingestion/search stories

Minimum build/test CI is part of the executable foundation path and is tracked as Story 0.4. Semantic release, NuGet publishing, branch protection hardening, release operations, and extended CI quality hardening remain in the Engineering/Operational Readiness track.
```

Rationale: Epic 1 should not depend on a future Epic 11 story to be independently startable. Story 0.4 makes the prerequisite local to the foundation path.

### Proposal B: Replace the Current Pre-Implementation CI Gate Text

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: `Pre-Implementation CI Preflight Gate (2026-05-19)`

OLD:

```text
Before any Epic 1.x product-capability story writes data, the minimum build/test CI subset declared in Story 11.1 must be in place — specifically: pull-request build, restore + `dotnet build` with `TreatWarningsAsErrors=true`, and the test-tier execution that covers Tier-1 unit tests. This is the implementation reader's preflight; the rest of Story 11.1 (semantic release hardening, branch protection, release run integrity) remains in the Engineering/Operational Readiness Track.

If the preflight subset is not in place, do not start Story 1.2 onward. Either land the preflight subset of Story 11.1 first, or open a sprint change to defer the preflight requirement explicitly.
```

NEW:

```text
Before any Epic 1.x product-capability story writes data, Story 0.4 must be complete. Story 0.4 is the minimum build/test CI preflight: pull-request build, restore, `dotnet build` with `TreatWarningsAsErrors=true`, and Docker-free Tier-1 unit/contract test execution.

If Story 0.4 is not complete, do not start Story 1.2 onward. Either complete Story 0.4 first, or open a sprint change to defer the preflight requirement explicitly. Story 11.1 extends this foundation into the full GitHub Actions quality pipeline; it is no longer the source of the Epic 1 prerequisite.
```

Rationale: Keeps the valid CI gate while removing the forward dependency into Epic 11.

### Proposal C: Add the Story 0.4 Text

Artifact: `_bmad-output/planning-artifacts/epics.md`
Location: after Story 0.3

NEW:

```text
### Story 0.4: Minimum Build/Test CI Preflight

As a maintainer preparing the first data-writing product stories,
I want a minimum automated build and Docker-free test gate in place,
So that Epic 1 work cannot proceed on an unbuildable or untested foundation.

**Acceptance Criteria:**

**Given** a pull request is opened against `main`
**When** the minimum CI preflight runs
**Then** the repository restores and builds `Hexalith.Memories.slnx` with `TreatWarningsAsErrors=true`
**And** the build uses the SDK pinned by `global.json`
**And** package versions remain centrally managed.

**Given** Docker is not available on a contributor machine
**When** the Docker-free test command runs
**Then** Tier-1 unit and contract tests execute without a Dapr sidecar
**And** Docker-required tests are skipped by filter or isolated in a separate lane with clear guidance.

**Given** the CI preflight reports status
**When** a build or test lane fails, is cancelled, or runs zero matching tests
**Then** the lane fails visibly and exposes diagnostics sufficient for implementation handoff.

**Validation Evidence Required:** Story completion must name the build command, Docker-free test command, CI check names if available, and the evidence location. This story may be satisfied by migrated evidence from Story 11.1 if the minimum gate already exists.
```

Rationale: Creates an independently completable foundation story with explicit evidence.

### Proposal D: Reframe Story 11.1 as the Extended CI Pipeline

Artifact: `_bmad-output/planning-artifacts/epics.md`
Story: `11.1 - GitHub Actions Build & Test Pipeline`

OLD:

```text
**Sequencing note:** The minimum build/test gate represented by this story is an early enabling prerequisite for greenfield implementation. Semantic release, NuGet publish automation, release preflight, and branch-protection hardening remain later Epic 11/12 operational-readiness work.
```

NEW:

```text
**Sequencing note:** The minimum Epic 1 preflight is owned by Story 0.4. Story 11.1 extends that foundation into the full GitHub Actions quality pipeline, including stable check names, integration-fast behavior, diagnostic artifacts, branch-protection documentation, and contributor/maintainer CI parity.
```

Rationale: Story 11.1 remains meaningful, but it no longer blocks earlier product capability stories by definition.

### Proposal E: Update Sprint Status for the Migrated CI Preflight

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`
Section: Epic 0

OLD:

```yaml
  epic-0: done
  0-0-project-scaffolding-and-single-command-boot: done
  0-1-tenant-provisioning-minimum-viable-workflow: done
  0-2-minimal-case-bootstrap: done
  0-3-tenant-and-case-validation-guard: done
  epic-0-retrospective: optional
```

NEW:

```yaml
  epic-0: done
  0-0-project-scaffolding-and-single-command-boot: done
  0-1-tenant-provisioning-minimum-viable-workflow: done
  0-2-minimal-case-bootstrap: done
  0-3-tenant-and-case-validation-guard: done
  0-4-minimum-build-test-ci-preflight: done
  epic-0-retrospective: optional
```

Add a status comment near the header:

```text
# 2026-06-27: Story 0.4 added as a planning-history migration of the minimum build/test CI preflight from Story 11.1. No new implementation story file required; completion evidence remains in 11-1-github-actions-build-and-test-pipeline.md.
```

Rationale: Keeps the register aligned with the new Epic 0 prerequisite without reopening completed CI work.

### Proposal F: Replace the Epic 18 Mutual-Dependency Note

Artifact: `_bmad-output/planning-artifacts/epics.md`
Section: Epic 18 preamble

OLD:

```text
**Sequencing note:** Stories 18.5 (source-URI-keyed lookup) and 18.6 (`MemoryUnitId` stability contract) are mutually dependent in their contract text — 18.5 reuses and depends on the dedup-record lifetime that 18.6 documents, and 18.6 names 18.5 as the authoritative resolution path. Land them in the same change (or 18.6's contract before/with 18.5's endpoint) so the lookup endpoint is never published against an unstated stability guarantee.
```

NEW:

```text
**Sequencing correction:** Stories 18.5 and 18.6 must not be treated as mutually dependent. Story 18.6 is the independent `MemoryUnitId` and source-URI dedup-record stability contract. Story 18.5 is the lookup endpoint that consumes that contract. If either story is reopened or reimplemented, complete or refresh Story 18.6's contract first, then apply Story 18.5's endpoint work. Preserve the existing numeric keys and completed story history for traceability.
```

Rationale: Resolves the circular dependency without merging or renumbering completed story history.

### Proposal G: Amend Story 18.5 to Consume the Stability Contract

Artifact: `_bmad-output/planning-artifacts/epics.md`
Story: `18.5 - Source-URI-Keyed Memory-Unit Lookup Endpoint`
Section: Acceptance Criteria

OLD:

```text
**Given** the dedup record `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}` already maps source URI -> `MemoryUnitId` internally,
**When** the lookup is implemented,
**Then** it reuses that existing mapping as the authoritative index where possible rather than introducing a parallel store, and documents the dependency on the dedup record's lifetime (see Story 18.6).
```

NEW:

```text
**Given** the Story 18.6 stability contract states the lifetime and failure modes of the source-URI dedup record `dedup:{tenantId}:{caseId}:{SHA256(sourceUri)}`,
**When** the lookup is implemented or reworked,
**Then** it reuses that existing mapping as the authoritative index where possible rather than introducing a parallel store,
**And** if the stability contract is missing or stale, Story 18.6 must be completed or refreshed before the endpoint is accepted.
```

Rationale: Story 18.5 now has a strict prerequisite rather than a mutual dependency.

### Proposal H: Amend Story 18.6 to Stand Alone

Artifact: `_bmad-output/planning-artifacts/epics.md`
Story: `18.6 - MemoryUnitId Stability Contract`
Section: Acceptance Criteria

OLD:

```text
**Given** loss of the dedup record (eviction, TTL expiry, or contract change) would re-mint an id,
**When** the contract is published,
**Then** it documents that failure mode and recommends the keyed lookup (Story 18.5) as the authoritative resolution path so consumers need not maintain a growing local id list, and notes the conditions under which a consumer should dedup by `sourceUri` instead.
```

NEW:

```text
**Given** loss of the dedup record (eviction, TTL expiry, or contract change) would re-mint an id,
**When** the contract is published,
**Then** it documents that failure mode,
**And** it tells consumers to retain `(tenantId, caseId, sourceUri)` as the durable source identity for long-lived correlation,
**And** it states that consumers should resolve the current `MemoryUnitId` through the source-URI keyed lookup when that endpoint is available, or dedup by `sourceUri` when it is not.
```

Rationale: Story 18.6 becomes independently completable documentation and guardrail work. It no longer requires Story 18.5 to exist before it delivers value.

### Proposal I: Optional Architecture Cleanup

Artifact: `_bmad-output/planning-artifacts/architecture.md`
Section: `First Implementation Steps`

OLD:

```text
5. CI pipeline: `.github/workflows/ci.yml` + `release.yml`
```

NEW:

```text
5. Minimum build/test CI preflight: `.github/workflows/ci.yml` with restore, build, and Docker-free unit/contract tests before Epic 1 data-writing work; release automation remains later operational-readiness scope.
```

Rationale: Architecture D17 already has the correct decision. This small cleanup aligns the implementation-step wording with the proposed Story 0.4 split.

## 5. Checklist Results

- 1.1 Triggering story: [x] Done. Trigger is the 2026-06-27 readiness report, not a single implementation story.
- 1.2 Core problem: [x] Done. Issue type is failed planning dependency shape: forward dependency plus mutual story dependency.
- 1.3 Supporting evidence: [x] Done. Evidence is the readiness report's CR-1 and CR-2 findings, current `epics.md` text, and current `sprint-status.yaml`.
- 2.1 Current epic impact: [x] Done. Epic 0, Epic 1, Epic 11, and Epic 18 are affected.
- 2.2 Required epic-level changes: [x] Done. Add Story 0.4, reframe Story 11.1, and correct Epic 18 sequencing.
- 2.3 Remaining epics review: [x] Done. Other major recommendations are noted but do not block this correction.
- 2.4 Invalidated epics/new epics: [x] Done. No epic is invalidated; no new epic is required.
- 2.5 Order/priority: [x] Done. Story 0.4 must be in the foundation path before Epic 1 data-writing work.
- 3.1 PRD conflicts: [x] Done. No PRD change required.
- 3.2 Architecture conflicts: [x] Done. No architecture redesign required; optional wording cleanup proposed.
- 3.3 UX conflicts: [N/A] Skip. No UX behavior or UI scope affected.
- 3.4 Other artifacts: [x] Done. `sprint-status.yaml` added the migrated Story 0.4 done row.
- 4.1 Direct adjustment: [x] Viable. Low/medium effort, low risk.
- 4.2 Potential rollback: [N/A] Not viable. Completed story rollback is unnecessary.
- 4.3 PRD MVP review: [N/A] Not required. MVP goals remain intact.
- 4.4 Recommended path: [x] Done. Direct Adjustment.
- 5.1 Issue summary: [x] Done.
- 5.2 Epic impact and artifact adjustment needs: [x] Done.
- 5.3 Recommended path with rationale: [x] Done.
- 5.4 PRD MVP impact and action plan: [x] Done. No MVP scope change; action is artifact correction.
- 5.5 Agent handoff plan: [x] Done.
- 6.1 Checklist review: [x] Done.
- 6.2 Proposal accuracy: [x] Done against the current artifacts loaded on 2026-06-27.
- 6.3 User approval: [x] Done. Jerome approved by replying `c` on 2026-06-27.
- 6.4 Sprint status update: [x] Done. Added `0-4-minimum-build-test-ci-preflight: done`.
- 6.5 Handoff confirmation: [x] Done. Moderate-scope handoff remains Product Owner / Developer with architecture cleanup applied.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Handoff recipients:

- Product Owner / Developer: approve the proposal, then apply the `epics.md` and `sprint-status.yaml` edits.
- Developer agent: preserve completed implementation story files; do not reopen Story 11.1, 18.5, or 18.6 solely for planning correction.
- Architect: optionally apply the architecture implementation-step cleanup; no architecture redesign is needed.
- Scrum/planning maintainer: keep sprint-status history explicit by adding Story 0.4 as a done migration row, not as new active implementation work.

Success criteria:

- Epic 1 no longer has a forward dependency into Epic 11 for minimum build/test CI.
- Story 0.4 exists in Epic 0 and has clear acceptance criteria and migrated completion evidence.
- Story 11.1 is framed as extended CI quality pipeline work, not the source of the Epic 1 preflight prerequisite.
- Stories 18.5 and 18.6 no longer contain circular completion language.
- Existing completed story and sprint-status history remains traceable.
- No PRD, UX, or architecture redesign is introduced by this correction.

## 7. Approval Record

Jerome approved this Sprint Change Proposal on 2026-06-27 by replying `c`.

Applied changes:

1. Updated `_bmad-output/planning-artifacts/epics.md` with Proposals A-D and F-H.
2. Updated `_bmad-output/implementation-artifacts/sprint-status.yaml` with Proposal E.
3. Updated `_bmad-output/planning-artifacts/architecture.md` with Proposal I.
4. Updated this proposal with approval and applied-artifact status.
