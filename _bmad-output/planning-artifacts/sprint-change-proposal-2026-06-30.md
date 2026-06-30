---
project: Hexalith.Memories
date: 2026-06-30
workflow: bmad-correct-course
change_trigger: Active deferred-work entries and Epic 18 residual action items need explicit backlog homes.
mode: Batch
status: approved
prepared_for: Jerome
scope_classification: Moderate
approved_by: Jerome
approved_at: 2026-06-30T11:07:12+02:00
---

# Sprint Change Proposal - Deferred Work Backlog Homes

## 1. Issue Summary

The triggering issue is that `_bmad-output/implementation-artifacts/deferred-work.md` still contains active `open` and `carried-forward` entries after Epic 15 and Epic 18 are marked done in `sprint-status.yaml`.

The register is valuable, but it is now mixing three different states:

- Completed work with evidence, such as `MEM-2` through `MEM-7`.
- Accepted risks with re-open triggers, such as several Story 15.1 and scaffolding entries.
- Active residuals that need either a story id, an explicit accepted-debt decision, or a re-open trigger tied to a real planning event.

The clearest current examples are:

- `MEM-2-ASPIRATE` and `MEM-3-OPENAPI`, both carried forward without assigned story ids.
- Epic 18 retrospective Action Item 4: assign backlog homes to aspirate emission, OpenAPI generation, real-Redis race evidence, Dapr-sidecar pub/sub smoke evidence, and the Story 18.4 token-anchoring edge.
- The nine `15.2-RV*` provider-registry review items marked `open`.
- The `15.1-RV*` release-preflight review items marked `carried-forward`.
- `12.4-RV20`, the strict release baseline replay evidence candidate.
- `MEM-1`, still carried forward for residual AppHost/public-surface stability concerns.

The problem is not that all of these must be implemented immediately. The problem is that "active but unscheduled" entries can be lost after the owning epics close.

## 2. Impact Analysis

### Epic Impact

Epic 15 remains done. It should not be reopened just because post-triage entries still exist.

Epic 18 remains done. The consumer integration stories were completed and the retrospective correctly recorded residuals. Reopening Epic 18 would blur completed history.

Epic 2 remains in progress because Story 2.7 is still in `review`, but the deferred-register issue does not require changing Epic 2 scope.

Recommended epic-level change: add a new Engineering/Operational Readiness epic, provisionally **Epic 19: Deferred Register Backlog Home and Residual Hardening**. This gives active residuals a forward-looking backlog container without mutating completed epic history.

### Story Impact

New story anchors should be added under Epic 19:

- **Story 19.1: Deferred Register Active-Entry Classification Sweep**
- **Story 19.2: Downstream Contract Artifact Generation Decisions**
- **Story 19.3: Release Preflight and Baseline Evidence Residual Sweep**
- **Story 19.4: Provider Registry and Migration Residual Sweep**

Existing stories should not be reopened. Their history should be referenced from the new stories.

### Artifact Conflicts

PRD: no product-scope change. This is operational-readiness governance, not a product capability change.

Architecture: no immediate architecture change is required. Story 19.1 should include the existing Epic 18 action item to reconcile stale architecture anchors against live code.

UX: no UI/UX specification change is required. The issue is backlog/state governance.

Implementation artifacts: `deferred-work.md` and `sprint-status.yaml` need updates after approval. Those updates should assign explicit story homes or accepted-debt decisions to active residuals.

### Technical Impact

No runtime code change is required by the proposal itself. Later Epic 19 stories may touch:

- `tools/release-preflight.ps1`
- `tools/test-release.ps1`
- `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs`
- `tests/tooling/release_preflight/release_preflight_test.py`
- provider registry and migration files under `src/Hexalith.Memories.Server`
- deployment/route docs and generated contract artifacts
- focused integration evidence for Redis and Dapr sidecar behavior

## 3. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The core plan and completed epics remain valid.
- No rollback is useful; the issue is planning signal and residual ownership.
- MVP scope does not need review.
- A new operational-readiness epic is cleaner than reopening done epics or scattering unscheduled work across old story files.

Effort estimate: Medium. Story 19.1 is low effort; Stories 19.2 through 19.4 may range from documentation/test cleanup to implementation work depending on the decisions made.

Risk level: Medium. The main risk is over-promoting low-value accepted risks into mandatory implementation. Story 19.1 must explicitly separate "schedule now" from "accept until trigger".

## 4. Detailed Change Proposals

### Epics: Add Epic 19 to the Epic List

Section: `Epic List`

OLD:

```markdown
### Epic 18: Downstream Consumer Integration Contract Hardening
Maintainers can give the first external consumer of Hexalith.Memories (the `Hexalith.Parties` project) a stable, documented, and race-safe integration contract, closing seven cross-repository asks (MEM-1 ... MEM-7) raised during the Parties `bmad-correct-course` intake on 2026-05-27. Three asks (MEM-1, MEM-4, MEM-7) were partly satisfied by the current `main`; the stories close only the verified residual gap.
```

NEW:

```markdown
### Epic 18: Downstream Consumer Integration Contract Hardening
Maintainers can give the first external consumer of Hexalith.Memories (the `Hexalith.Parties` project) a stable, documented, and race-safe integration contract, closing seven cross-repository asks (MEM-1 ... MEM-7) raised during the Parties `bmad-correct-course` intake on 2026-05-27. Three asks (MEM-1, MEM-4, MEM-7) were partly satisfied by the current `main`; the stories close only the verified residual gap.

### Epic 19: Deferred Register Backlog Home and Residual Hardening
Maintainers can convert active `open` and `carried-forward` deferred-work entries into explicit backlog homes, accepted-debt decisions, or trigger-bound future work without reopening completed epics.
```

Rationale: Epic 19 preserves Epic 15 and Epic 18 completion history while making deferred residual ownership explicit.

### Epics: Add Epic 19 Section

Section: after Epic 18.

ADD:

```markdown
## Epic 19: Deferred Register Backlog Home and Residual Hardening

**Lifecycle label:** Operational Readiness / Deferred Register Governance.

Maintainers can convert active `open` and `carried-forward` entries from `deferred-work.md` and recent retrospective action items into explicit backlog homes, accepted-debt decisions, or trigger-bound future work without reopening completed epics.

**Driven by:** Sprint Change Proposal 2026-06-30 (Deferred Work Backlog Homes).

**Preflight required:** Before implementing any story, re-read `deferred-work.md`, `sprint-status.yaml` action items, and the relevant retrospective that produced each residual. Do not bulk-rewrite historical deferred prose; only migrate active entries that need planning signal.

### Story 19.1: Deferred Register Active-Entry Classification Sweep

As a maintainer,
I want every active `open` or `carried-forward` deferred-work entry to have a current disposition,
So that completed epics do not hide unscheduled operational or consumer-risk work.

**Acceptance Criteria:**

**Given** `deferred-work.md` contains structured entries with `Status: open` or `Status: carried-forward`,
**When** the sweep runs,
**Then** every active structured entry is classified as one of: scheduled story, accepted debt with rationale, carried-forward with explicit trigger and owner, or resolved with evidence.

**Given** Epic 18 retrospective Action Item 4 names parked carry-forwards,
**When** the sweep runs,
**Then** `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`, the real-Redis race evidence, the Dapr-sidecar pub/sub smoke evidence, and the Story 18.4 token-anchoring edge each receive a story id or accepted-debt entry with a re-open trigger.

**Given** active entries from completed Epics 15 and 18 may still be valid,
**When** the sweep updates planning artifacts,
**Then** it references the completed source story but does not reopen the completed epic or alter completed story history.

**Given** `sprint-status.yaml` has retrospective action items,
**When** the sweep completes,
**Then** related action items are updated only when their acceptance condition is actually met.

### Story 19.2: Downstream Contract Artifact Generation Decisions

As a downstream integration maintainer,
I want explicit decisions for generated deployment and route artifacts,
So that consumers know whether to rely on maintained docs, generated manifests, or generated OpenAPI/Swagger output.

**Acceptance Criteria:**

**Given** `MEM-2-ASPIRATE` is carried forward without a story id,
**When** this story runs,
**Then** aspirate or equivalent manifest emission is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

**Given** `MEM-3-OPENAPI` is carried forward without a story id,
**When** this story runs,
**Then** OpenAPI/Swagger generation is either scheduled for implementation, explicitly accepted as not needed, or deferred with an owner, trigger, and target artifact.

**Given** maintained docs already exist for deployment configuration and route surface,
**When** generated artifacts remain deferred,
**Then** the rationale states why the maintained-doc plus drift-guard tests remain sufficient for current consumers.

### Story 19.3: Release Preflight and Baseline Evidence Residual Sweep

As a release maintainer,
I want release-preflight and baseline-evidence carry-forwards reviewed as one release-quality backlog decision,
So that low-value hardening stays trigger-bound and high-value release risks get implementation stories.

**Acceptance Criteria:**

**Given** `12.4-RV20` requests optional strict literal per-SHA replay evidence,
**When** release evidence needs are reviewed,
**Then** the team either creates a strict replay evidence story or records why ancestry-based proof remains sufficient until a release post-mortem or quality story reopens it.

**Given** `15.1-RV1` through `15.1-RV16` are carried forward from release-preflight review,
**When** the sweep runs,
**Then** each entry is grouped into implement-now, accept-until-trigger, or future release-hardening story buckets.

**Given** release tooling changes can affect package publication,
**When** any implement-now item is selected,
**Then** focused validation covers the changed script, workflow, and inventory-test behavior.

### Story 19.4: Provider Registry and Migration Residual Sweep

As a system operator,
I want provider-registry and migration-marker residual risks reviewed against current code,
So that embedding-provider expansion and live migration do not inherit stale assumptions.

**Acceptance Criteria:**

**Given** `15.2-RV1` through `15.2-RV9` are marked `open`,
**When** the sweep runs,
**Then** each item is either resolved, accepted with rationale, or assigned to a concrete provider-registry follow-up story.

**Given** migration-marker deferred entries from Story 15.3 include concurrency, stale-marker, TTL, and operator-documentation risks,
**When** the sweep runs,
**Then** the team identifies which risks remain trigger-bound and which need a migration-hardening story before the next provider migration investment.

**Given** provider/model casing and registry dispatch appear in both provider and migration paths,
**When** follow-up work is scheduled,
**Then** tests cover both write-time validation and read/runtime comparison paths where practical.
```

Rationale: These stories create concrete ownership without pretending every deferred entry has equal urgency.

### Sprint Status: Add Epic 19 Backlog Entries

Section: `development_status`

ADD after Epic 18 rows:

```yaml
  # Epic 19: Deferred Register Backlog Home and Residual Hardening
  epic-19: backlog
  19-1-deferred-register-active-entry-classification-sweep: backlog
  19-2-downstream-contract-artifact-generation-decisions: backlog
  19-3-release-preflight-and-baseline-evidence-residual-sweep: backlog
  19-4-provider-registry-and-migration-residual-sweep: backlog
  epic-19-retrospective: optional
```

Rationale: The sprint tracker needs explicit backlog rows before create-story/dev-story workflows can pick up the work.

### Deferred Work: Add Backlog Home Rollup

Section: near the active rollup area of `deferred-work.md`.

ADD:

```markdown
## Deferred Register Backlog Home Rollup (2026-06-30)

Sprint Change Proposal 2026-06-30 creates Epic 19 as the backlog home for active deferred-register residuals that outlived completed epics.

- `MEM-2-ASPIRATE` and `MEM-3-OPENAPI` target Story 19.2 unless the story explicitly accepts or reassigns them.
- `12.4-RV20` and `15.1-RV1` through `15.1-RV16` target Story 19.3 unless the story explicitly accepts or reassigns them.
- `15.2-RV1` through `15.2-RV9` and Story 15.3 migration-marker residuals target Story 19.4 unless the story explicitly accepts or reassigns them.
- Other active `open` or `carried-forward` entries are classified by Story 19.1 before implementation is scheduled.
```

Rationale: Reviewers should not have to infer ownership from historical headings.

## 5. Change Analysis Checklist

- [x] 1.1 Triggering story identified: no single active story; trigger is `deferred-work.md` plus Epic 18 retrospective Action Item 4 after Epic 15 and Epic 18 closure.
- [x] 1.2 Core problem defined: active deferred entries lack explicit backlog homes or accepted-debt decisions.
- [x] 1.3 Evidence gathered: `deferred-work.md` active statuses, Epic 18 retrospective, and `sprint-status.yaml` action items.
- [x] 2.1 Current epic assessment: Epics 15 and 18 remain done and should not be reopened.
- [x] 2.2 Epic-level change: add Epic 19.
- [x] 2.3 Remaining epics reviewed: Epic 2 remains in progress; Epic 17 action items overlap but are not invalidated.
- [x] 2.4 No epic invalidation: no planned epic becomes obsolete.
- [x] 2.5 Priority: classify before the next operational-readiness implementation pass.
- [x] 3.1 PRD conflict checked: no product-scope change.
- [x] 3.2 Architecture conflict checked: stale architecture anchors should be handled by Story 19.1 classification, not as a direct architecture rewrite in this proposal.
- [N/A] 3.3 UI/UX conflict checked: no UI behavior change.
- [x] 3.4 Secondary artifacts checked: `deferred-work.md`, `sprint-status.yaml`, retrospectives, release/provider/migration docs and tests.
- [x] 4.1 Direct Adjustment viable: recommended.
- [N/A] 4.2 Rollback not viable: no completed work needs reverting.
- [N/A] 4.3 MVP review not required: MVP scope is unaffected.
- [x] 4.4 Recommended path selected: Direct Adjustment, moderate scope.
- [x] 5.1 Issue summary included.
- [x] 5.2 Epic impact and artifact needs included.
- [x] 5.3 Recommended path included.
- [x] 5.4 MVP impact and action plan included.
- [x] 5.5 Handoff plan included.
- [x] 6.3 User approval received from Jerome on 2026-06-30.
- [x] 6.4 Sprint status update approved for Epic 19 backlog entries.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Reason: This is backlog reorganization and governance across completed epics, with later implementation likely touching release tooling, provider/migration code, docs, and integration evidence. It does not require a PRD or architecture replan.

Route to:

- Product Owner / Developer: approve Epic 19 and add the proposed backlog rows.
- Developer agent: implement Story 19.1 first, then use its classification to decide whether Stories 19.2 through 19.4 need implementation, accepted-debt records, or smaller split stories.
- Architect: review only if Story 19.2 chooses generated manifests/OpenAPI as strategic platform artifacts or Story 19.4 changes provider/migration architecture.

Success criteria:

- Every active `open` or `carried-forward` structured deferred entry has a current disposition.
- `MEM-2-ASPIRATE`, `MEM-3-OPENAPI`, real-Redis race evidence, Dapr-sidecar pub/sub smoke evidence, and the Story 18.4 token-anchoring edge are no longer parked without story ids or accepted-debt decisions.
- Completed Epics 15 and 18 remain historically intact.
- `sprint-status.yaml` action items are updated only when their stated acceptance condition is satisfied.

## 7. Approval

Decision: approved by Jerome on 2026-06-30 at 11:07:12+02:00.

Approved Direct Adjustment: add Epic 19 with Stories 19.1 through 19.4, update sprint status with backlog rows, and add a deferred-register rollup that routes active residuals to Epic 19 classification.
