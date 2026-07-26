---
date: 2026-07-26
status: approved
trigger: "Align the Memories module CI/CD on the EventStore or Tenants CI/CD model"
scope_classification: Moderate
mode: Incremental
reference_baseline: "EventStore structure, supplemented by compatible Tenants coverage and consumer-validation inputs"
artifacts_modified:
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/implementation-artifacts/sprint-status.yaml
implementation_artifacts_affected:
  - .github/workflows/ci.yml
  - .github/workflows/commitlint.yml
  - .github/workflows/release.yml
  - .github/workflows/recover-partial-release.yml
  - .releaserc.json
  - tools/release-packages.json
  - tools/release-packages.schema.json
  - tools/release and validation scripts
  - tests/tooling release and CI fixtures
  - docs/dev/branch-protection.md
  - docs/dev/release-runbook.md
external_dependency: Hexalith.Builds
---

# Sprint Change Proposal — Memories CI/CD Alignment

**Date:** 2026-07-26  
**Project:** Hexalith.Memories  
**Prepared by:** Correct Course workflow  
**Approver:** Administrator  
**Status:** Approved

---

## 1. Issue Summary

### Problem statement

Memories implements a mature but predominantly repository-local CI/CD pipeline. EventStore and
Tenants now use the reusable workflows and guarded release contract defined by
Hexalith.Builds. Memories should align with that standard without discarding the module-specific
evidence required by its broader runtime, test, deployment, and four-image release surface.

The planning correction must not reopen historical Epics 11 or 26. Both are complete. Backlog
Epic 30 already owns the four-image release and partial-recovery lane, so it is the correct
operational-readiness home for this alignment.

### Trigger and discovery context

The Administrator requested alignment on either the EventStore or Tenants CI/CD model. Repository
inspection shows that EventStore is the closer structural reference:

- EventStore delegates standard build and test work to `domain-ci.yml` while retaining separate
  module-specific integration and advisory workflows.
- Tenants delegates a larger proportion of its domain tests, coverage, consumer validation, and
  scheduled performance work directly to `domain-ci.yml`.
- Both use an operator-dispatched release, exact-current-green-`main` preflight, protected
  `production` environment, and `domain-release.yml` pinned to an immutable Hexalith.Builds SHA.
- Memories has additional story-scope, tenant-negative-evidence, tooling, web E2E, fast/slow
  integration, deployment-verification, benchmark, four-image, and partial-recovery requirements.

Therefore, the selected alignment is the EventStore-shaped shared-core plus companion-lane model,
using Tenants inputs only where they preserve equivalent evidence.

### Evidence

| Surface | Memories today | EventStore/Tenants and Builds standard | Finding |
| :--- | :--- | :--- | :--- |
| Standard CI | Local checkout, initialization, restore, build, and test orchestration | Reusable `domain-ci.yml@main` caller | Standard mechanics should move to the shared workflow. |
| Specialized verification | Story scope, tenant evidence, Python/PowerShell fixtures, web E2E, live Dapr, deployment and benchmark lanes | EventStore retains companion integration/advisory workflows | Keep these local and explicit unless Builds gains an equivalent reusable input. |
| Release trigger | Every push to `main` | Intentional `workflow_dispatch` | Automatic publication conflicts with `ci-cd-standards.md`. |
| Source authorization | Release rebuilds and retests after a push | Unprotected exact-current-green-`main` preflight before protected release | Adopt the guarded source contract; do not duplicate exact-source CI tests. |
| Shared release identity | Local release job and mutable shared action references | `domain-release.yml` pinned to the same exact SHA passed as `builds-execution-sha` | Adopt after the multi-container prerequisite is satisfied. |
| Package inventory schema | `packages[].packageId` | Shared publication preflight reads `packages[].id` | Migrate the manifest, schema, tooling, recovery, and fixtures atomically. |
| Container identity | Four repositories: `memories`, `memories-mcp`, `memories-access-telemetry`, `memories-access-telemetry-clock` | Current preflight freezes one `container_repository` and uses a single-use container phase | Hexalith.Builds must be generalized before Memories can safely adopt the reusable release. |
| Partial publication | Repository-owned summaries, issue alert, and recovery workflow | Shared release uploads immutable evidence but is proven by current consumers only for one container | Preserve recovery and extend shared evidence for an exact four-image release unit. |
| Commit validation | Pull-request validation does not cover the complete title-edit/main-push contract | PR opened/synchronized/reopened/edited plus push `main`, with explicit PR title input | Align commitlint triggers and inputs. |

### Current shared-release incompatibilities

Two incompatibilities are fail-closed prerequisites, not reasons to fork shared logic into
Memories:

1. `Hexalith.Builds/Github/publish-containers/publication_preflight.py` reads `packages[].id`,
   while the Memories source-of-truth manifest and its fixtures use `packageId`.
2. The current publication identity contains one `container_repository`, and the container phase
   is single-use. The installed publisher iterates mappings, but the frozen identity and phase
   evidence cannot represent four distinct repositories without collision or identity mismatch.

The first is resolved in Memories through an atomic manifest-contract migration. The second must
be resolved in the repository that owns the reusable release contract: Hexalith.Builds.

---

## 2. Impact Analysis

### Epic impact

| Epic | Current status | Impact |
| :--- | :--- | :--- |
| Epic 11 — CI/CD & Automated Quality Pipeline | `done` | Historical record remains closed and unchanged. |
| Epic 26 — Test, Deployment & Operational Readiness | `done` | Existing coverage, benchmark, deployment, and release evidence remains authoritative and must not be weakened. |
| Epic 30 — CI/CD Pipeline Ownership and Alignment | `backlog` | Expanded and renamed to own CI/CD alignment. Existing four-image and recovery scope is preserved in Story 30.1; new Story 30.2 owns CI alignment. |

No product epic, MVP scope, feature requirement, or user journey changes.

### Story impact

| Story | Current status | Change |
| :--- | :--- | :--- |
| 30.1 Four-Image Container Release and Partial-Recovery Pipeline | `backlog` | Retains its approved release/recovery scope and gains the guarded shared-release target, Hexalith.Builds multi-container activation gate, manifest migration, rehearsal, and rollback contract. |
| 30.2 Shared CI Core and Module-Specific Verification Lanes | new, `backlog` | Owns reusable CI adoption, local-lane boundaries, commitlint alignment, required-check mapping, and evidence-equivalence proof. |

Story 30.2 must execute before Story 30.1 because the guarded release preflight depends on a
successful exact-source push run of the aligned `ci.yml` workflow.

### Planning artifact impact

| Artifact | Impact |
| :--- | :--- |
| `epics.md` | Rename and expand Epic 30; add Story 30.2; strengthen Story 30.1 without removing its approved recovery scope. |
| `architecture.md` | Reconcile D17 with the current reusable-workflow, exact-source, protected-environment, and intentional-release standard. |
| `sprint-status.yaml` | Register Story 30.2 and explicit execution order `30.2 -> 30.1`; keep Epic 30 `backlog`. |
| `prd.md` | No change. Product requirements and MVP scope are unaffected. |
| `ux-design-specification.md` | No change. No user-interface behavior is affected. |

### Technical impact

Implementation will affect GitHub Actions callers, semantic-release configuration, package
inventory tooling, release/recovery fixtures, branch-protection documentation, and the release
runbook. It also requires an owner-approved change in Hexalith.Builds before release cutover.

The correction does not authorize reducing or deleting tenant-negative evidence, web E2E, fast
integration, production-deployment verification, coverage, nightly slow integration, benchmark,
package-topology, registry-authorization, or partial-recovery gates.

### Operational and external impact

- A maintainer must configure or confirm the protected `production` GitHub environment.
- Branch protection or repository rulesets must be updated only after stable replacement check
  names are known and proven.
- The existing Zot challenge-response/write-authorization risk must be re-verified. A registry-side
  failure remains a named external blocker and must not be bypassed.
- No release version or tag may be reused after any partial publication.

---

## 3. Recommended Approach

### Selected path

**Direct Adjustment — EventStore-shaped hybrid alignment.**

Use Hexalith.Builds for standard mechanics, keep Memories-specific verification and recovery as
explicit module-owned lanes, and extend Hexalith.Builds where the shared release contract cannot
represent a four-image publication.

### Options evaluated

| Option | Assessment |
| :--- | :--- |
| Copy EventStore wholesale | Rejected. Its structure is the closest match, but its current one-container release and smaller module-specific surface do not satisfy Memories unchanged. |
| Copy Tenants wholesale | Rejected. Its domain-ci-heavy topology is too narrow for Memories' web, Dapr, deployment, benchmark, four-image, and recovery evidence. |
| Keep the bespoke Memories pipeline | Rejected. It preserves current behavior but leaves standard mechanics duplicated and automatic publication noncompliant with the shared release policy. |
| EventStore-shaped hybrid with Builds prerequisite | Selected. It centralizes common mechanics, preserves local evidence, and fixes missing reusable capability at its owning boundary. |

### Rationale

1. EventStore establishes the correct ownership shape: reusable core plus explicit companion lanes.
2. Tenants demonstrates useful shared coverage and consumer-validation inputs, but those inputs are
   adopted only when their script and evidence contracts fit Memories.
3. Completed operational history remains closed. The backlog Epic 30 absorbs the new work without
   rewriting Epics 11 or 26.
4. Publication changes are fail-closed. The release caller does not switch until the exact
   multi-container Builds revision, nine-package schema, rehearsal, and recovery evidence pass.
5. Reusable-workflow gaps are fixed in Hexalith.Builds, avoiding another local copy that would drift.

### Effort, risk, and timeline

| Dimension | Assessment |
| :--- | :--- |
| Scope classification | **Moderate** — two backlog stories plus one cross-repository prerequisite. |
| Effort | **Medium** — approximately 4–7 engineering days, excluding external registry remediation and review wait time. |
| Technical risk | **Medium-High for release**, **Medium for CI** — publication is irreversible, while CI caller migration is readily reversible. |
| Product/MVP risk | **None** — no product requirement or MVP-readiness change. |
| Critical dependency | Owner-approved Hexalith.Builds multi-container publication contract. |

---

## 4. Detailed Change Proposals

All four changes below were reviewed incrementally and received final approval from the
Administrator on 2026-07-26. The planning-artifact changes were then applied.

### Edit 1 — Reframe Epic 30

**File:** `_bmad-output/planning-artifacts/epics.md`

**OLD:**

```md
## Epic 30: Container Release Pipeline Ownership

The four-image container release and partial-recovery pipeline is owned, tested, and documented as an independently demonstrable release lane rather than as incidental scope inside an adapter-qualification story.

**Lifecycle label:** Operational Readiness / Release Engineering.
```

**NEW:**

```md
## Epic 30: CI/CD Pipeline Ownership and Alignment

Memories adopts the EventStore-shaped Hexalith CI/CD model: reusable Hexalith.Builds workflows own standard build, test, and release mechanics, while Memories retains only named module-specific verification and recovery lanes. Tenants supplies compatible coverage and consumer-validation patterns where those shared inputs fit. The existing four-image container release and partial-recovery scope remains independently owned inside this epic.

**Lifecycle label:** Operational Readiness / CI/CD Engineering.

**Alignment target:** EventStore's shared-core plus companion-lane structure, with Tenants-style coverage and consumer validation where supported. Alignment must not weaken tenant-negative evidence, web E2E, integration, deployment, benchmark, package-inventory, or partial-release recovery gates.
```

Apply the approved title and summary consistently in the Epic List and full Epic 30 body. Preserve
the existing scope origin and approved DW 27.3-CR5 provenance.

### Edit 2 — Add Story 30.2

**File:** `_bmad-output/planning-artifacts/epics.md`

**NEW:**

```md
### Story 30.2: Shared CI Core and Module-Specific Verification Lanes

**Status:** backlog. **Owner:** Memories Maintainer + Hexalith.Builds Maintainer.

As a maintainer,
I want Memories CI aligned to the shared Hexalith.Builds contract,
So that standard checks remain consistent across modules without losing Memories-specific evidence.

**Acceptance Criteria:**

**Given** pull requests and pushes to `main`,
**When** `ci.yml` runs,
**Then** its standard restore, Release build, warnings-as-errors, and compatible per-project test work is delegated to `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`
**And** test projects and platform selection are explicit rather than inferred.

**Given** Memories has verification that the reusable workflow does not model,
**When** the pipeline is reorganized,
**Then** story-file scope, tenant-negative evidence, tooling fixtures, release-package topology, web E2E, fast integration, and production-deployment verification remain named local jobs or companion workflows
**And** nightly slow integration and benchmark lanes remain intact.

**Given** consumer validation, coverage, or package validation cannot use an existing shared input without weakening evidence,
**When** alignment is implemented,
**Then** the missing reusable capability is added to Hexalith.Builds or retained locally with a documented exception
**And** shared workflow logic is not copied into Memories.

**Given** commit and pull-request title validation,
**When** a pull-request title is opened, synchronized, reopened, or edited, or a commit reaches `main`,
**Then** commitlint runs with the pull-request title supplied explicitly and enforces the repository Conventional Commit contract.

**Given** the aligned pipeline is proposed for required-check adoption,
**When** old and new lanes are compared,
**Then** every existing required gate has equivalent or stronger executable evidence, stable check names are documented for branch protection, TRX and coverage evidence remain downloadable, and duplicate work is removed only after equivalence is proven.

**Implementation evidence:** The story file must contain a lane-by-lane migration table naming the old owner, new owner, trigger, required-check name, validation command or artifact, and rollback path.
```

### Edit 3 — Align Story 30.1 to guarded shared release

**File:** `_bmad-output/planning-artifacts/epics.md`

Keep the Story 30.1 title, known Zot risk, existing provenance, and production-deployment scope
boundary. Replace its story statement and acceptance criteria with:

```md
**Activation gate:** Story 30.1 must not enter implementation until an owner-approved Hexalith.Builds revision supports a frozen multi-container publication identity, repeated per-container verification without phase collisions, and evidence sufficient for partial-release recovery. The current single `container_repository` identity and single-use container phase do not satisfy the four-image contract.

As a maintainer,
I want the nine-package and four-image release aligned to the guarded Hexalith release workflow,
So that publication uses one auditable shared contract while preserving deterministic partial recovery.

**Acceptance Criteria:**

**Given** an operator intentionally dispatches a release from `main`,
**When** the release caller starts,
**Then** an unprotected preflight proves the dispatch SHA is the current `main` tip with successful exact-source push CI
**And** the release job uses a protected `production` environment, `cancel-in-progress: false`, and `domain-release.yml` pinned to the same approved 40-character Hexalith.Builds SHA passed as `builds-execution-sha`
**And** ordinary pushes to `main` never publish a release.

**Given** exact-source CI already tested the release candidate,
**When** the reusable release job is invoked,
**Then** `test-projects` remains empty to avoid duplicate release compute
**And** `expected-package-count` is fixed at `9`
**And** any failed source, package-count, environment, destination-absence, or Builds-identity proof stops before publication.

**Given** the shared publication preflight reads `packages[].id`,
**When** Memories adopts the shared release,
**Then** `tools/release-packages.json`, its schema, validators, pack scripts, recovery tooling, and fixtures migrate atomically from `packageId` to `id`
**And** the canonical inventory remains exactly the existing nine package IDs.

**Given** the approved multi-container Hexalith.Builds contract,
**When** semantic-release publishes version `${nextRelease.version}`,
**Then** the caller supplies exactly these mappings:

- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj|memories`
- `src/Hexalith.Memories.Mcp/Hexalith.Memories.Mcp.csproj|memories-mcp`
- `src/Hexalith.Memories.AccessTelemetry/Hexalith.Memories.AccessTelemetry.csproj|memories-access-telemetry`
- `src/Hexalith.Memories.AccessTelemetry.Clock/Hexalith.Memories.AccessTelemetry.Clock.csproj|memories-access-telemetry-clock`

**And** every image is verified against its declared platforms and workload-appropriate health contract
**And** Memories-specific production-deployment asset generation remains in the caller rather than being copied into Hexalith.Builds.

**Given** publication stops after any subset of packages or images succeeds,
**When** recovery is authorized,
**Then** `.github/workflows/recover-partial-release.yml` consumes immutable release evidence, proves the exact source/version/inventory, skips already-published members, and publishes only the missing members
**And** recovery never overwrites, retags, or silently treats an ambiguous destination response as success.

**Given** the existing automatic release and custom publisher,
**When** migration is cut over,
**Then** a dry run and controlled release rehearsal prove nine-package, four-image, GitHub Release asset, registry authorization, failure, and recovery parity
**And** the old path is removed only after parity succeeds
**And** rollback restores the prior caller without changing a published version or mutable tag.

**Implementation evidence:** The story file must track the Hexalith.Builds prerequisite SHA, manifest migration, four-image identity, release rehearsal, partial-failure exercise, recovery result, protected-environment configuration, and rollback proof as separate reviewable checkpoints.
```

### Edit 4 — Reconcile architecture and sprint governance

#### Architecture D17

**File:** `_bmad-output/planning-artifacts/architecture.md`

Replace the D17 decision row with:

```md
| D17 | CI/CD pipeline | Hexalith.Builds reusable CI core plus module-specific verification lanes and intentional guarded release | Pull requests and pushes to `main` use `domain-ci.yml@main` for compatible standard build/test work. Memories-specific tenant evidence, tooling, web E2E, integration, deployment, benchmark, and recovery lanes remain local and explicit. Publication is operator-dispatched from the exact current green `main` source, enters a protected environment, and invokes `domain-release.yml` pinned to an approved immutable Hexalith.Builds SHA. | Shared workflow callers, module-specific workflow lanes, commit conventions, branch protection, package/container publication, recovery, CONTRIBUTING.md |
```

Replace the D17 summary row with:

```md
| D17 | Reusable GitHub Actions CI + guarded semantic release | Consistent evidence, intentional publication, recoverable multi-artifact release | Engineering/Operational Readiness |
```

#### Sprint execution and status

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

Add under `story_execution_order`:

```yaml
  epic-30:
    reason: "The guarded release requires a successful exact-source ci.yml contract, so shared CI alignment must complete before release migration."
    order:
      - 30-2-shared-ci-core-and-module-specific-verification-lanes
      - 30-1-four-image-container-release-and-partial-recovery-pipeline
```

Update the Epic 30 status block to:

```yaml
  # Epic 30: CI/CD Pipeline Ownership and Alignment
  # Expanded by the approved Memories CI/CD alignment course correction.
  # Story 30.2 precedes 30.1 because guarded release requires exact-source green CI.
  epic-30: backlog
  30-1-four-image-container-release-and-partial-recovery-pipeline: backlog
  30-2-shared-ci-core-and-module-specific-verification-lanes: backlog
  epic-30-retrospective: optional
```

PRD and UX artifacts remain unchanged.

---

## 5. Implementation Handoff

### Work sequence

1. **Hexalith.Builds prerequisite**
   - Create and approve a Builds-owned change for an ordered multi-container publication identity.
   - Prove one- and multi-container callers, per-repository destination absence, repeated container
     phases without collision, immutable evidence on partial failure, and recovery consumption.
   - Preserve exact reusable-workflow/action/helper SHA identity.
2. **Story 30.2 — CI alignment**
   - Create the story file first.
   - Map every current lane to shared core, local required lane, local companion lane, or nightly lane.
   - Implement the `domain-ci.yml@main` caller and commitlint alignment.
   - Prove evidence equivalence before updating branch protection or removing duplicate work.
3. **Story 30.1 — release alignment**
   - Begin only after the approved Builds prerequisite SHA exists and Story 30.2 is done.
   - Migrate the nine-package manifest contract atomically.
   - Replace automatic publication with manual exact-current-green-`main` guarded release.
   - Pin `domain-release.yml` and `builds-execution-sha` to the same reviewed Builds commit.
4. **Rehearsal and cutover**
   - Exercise dry-run, no-release, success, partial-failure, and recovery paths.
   - Confirm four workload-appropriate image health contracts and all GitHub Release assets.
   - Update branch-protection and runbook documentation after stable checks and release behavior are
     proven.

### Ownership

| Role | Responsibility |
| :--- | :--- |
| Memories Maintainer | Own Stories 30.2 and 30.1, lane mapping, caller workflows, package schema migration, recovery, rehearsal, and documentation. |
| Hexalith.Builds Maintainer | Own reusable multi-container identity, destination proof, evidence, publisher tests, and immutable approved SHA. |
| Test Architect / Reviewer | Review lane equivalence, tenant-negative evidence, coverage/TRX retention, partial-failure injection, and recovery proof. |
| Platform Operations | Configure/approve the protected environment, branch-protection checks, Zot authorization, and external blocker evidence. |
| Product Owner / Scrum Master | Keep Epic 30 outside MVP readiness, enforce `30.2 -> 30.1`, and avoid reopening completed Epics 11 and 26. |

### Success criteria

- Pull requests and pushes to `main` execute the shared CI core and every retained required Memories
  gate with stable documented check names.
- Commitlint covers PR title edits and pushes to `main`, using the explicit pull-request title.
- Nightly slow integration and benchmark coverage remains scheduled and executable.
- No ordinary push publishes a release.
- A release can start only for the exact current green `main` SHA and requires the protected
  `production` environment.
- The caller and nested release tooling resolve to one approved immutable Hexalith.Builds SHA.
- The package identity is exactly nine canonical IDs and the container identity is exactly four
  repositories.
- Successful publication retains immutable package, container, platform/smoke, deployment, and
  GitHub Release evidence.
- Injected partial publication is recoverable without overwriting or republishing completed
  members.
- Current tenant isolation, deployment, coverage, and benchmark gates are not weakened.

### Rollback and failure policy

- CI migration is reversible by restoring the previous caller/job ownership while preserving all
  required checks; branch protection changes occur only after replacement checks pass.
- Release cutover occurs only after rehearsal. Before any publication, the prior caller may be
  restored without changing version identity.
- After any package or image has published, rollback by version reuse, deletion, overwrite, or
  mutable retag is forbidden. The only supported continuation is evidence-driven partial recovery
  or a named external blocker.
- If the current Zot authorization failure reproduces, retain the failure evidence, owner,
  consequence, and reopen trigger. Do not weaken authentication or bypass destination proof.

---

## 6. Correct Course Checklist Summary

| Section | Status | Result |
| :--- | :--- | :--- |
| 1. Trigger and evidence | Complete | User-requested alignment verified against actual Memories, EventStore, Tenants, and Builds workflows. |
| 2. Epic and story impact | Complete | Historical Epics 11/26 remain closed; backlog Epic 30 expands with Story 30.2. |
| 3. Artifact conflict analysis | Complete | Architecture D17 and sprint governance require updates; PRD and UX do not. |
| 4. Path evaluation | Complete | EventStore-shaped hybrid selected; wholesale copies and bespoke status quo rejected. |
| 5. Proposal components | Complete | Epic, stories, architecture, sprint order, dependencies, risks, and rollback are specified. |
| 6. Handoff | Complete | Owners, sequence, evidence, success criteria, and external blockers are defined. |

### Approval record

Approved by the Administrator on 2026-07-26. The Section 4 planning-artifact edits were applied.
This approval does not itself authorize publication, dependency updates, commits, pushes,
branch-protection mutations, protected-environment changes, or out-of-sequence story implementation.
