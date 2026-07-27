# Sprint Change Proposal — 2026-07-27

**Status:** approved 2026-07-27 by Administrator
**Author:** Developer (correct-course), for Administrator approval
**Triggering story:** `27-3-production-adapter-and-deployment-profile` (Epic 27)
**Executes:** `DW 27.3-CR14`, `DW 27.3-CR15`, `DW 27.3-CR16`
**Scope classification:** Moderate — backlog reorganization plus two governed-record corrections. No PRD, MVP, UX, or architecture-decision change.

---

## 1. Issue Summary

The 2026-07-26 Story 27.3 code review (chunk 3b) resolved seven decisions with the
Administrator. Three of them could not be executed by the reviewing workflow, because each
requires an approved course correction rather than a review patch. All three were recorded
as open deferred-work entries whose reopen triggers fire **before Story 27.3 advances to
`done`** (CR14, CR15) or **before Story 30.1, 30.2 or 31.1 is set `ready-for-dev`** (CR16).

They remained open through four subsequent `dev-story` invocations. The fourth
(2026-07-27) confirmed there is no repo-side work left in Story 27.3 that a development
session may perform: every `[Review][Patch]` and `[Review][Defer]` row is closed, and the
three unchecked rows are exactly these `[Review][Action]` items. This proposal executes
them.

### 1.1 CR14 — one profile, three records, two different values

`PG-ONPREM-1` is the immutable Production adapter profile that Story 27.3's AC1 captures
and AC4's approvals bind to. Its `maxConns` value is recorded three times and does not
agree:

| Record | `maxConns` | Status |
| :----- | :--------- | :----- |
| `deploy/kubernetes/base/dapr/access-telemetry-store.yaml` (shipped manifest) | `"40"` | current |
| `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md` (immutable component block) | `"64"` | stale |
| `sprint-change-proposal-2026-07-20-story-27-3-on-prem-postgresql-18-4.md` (approved) | `"64"` | stale |

`40` is the correct value. `64` cannot work: the ADR two-writer probe runs two lifecycle
replicas and each Dapr sidecar opens its own pool, so `2 x 64 = 128` exceeds PostgreSQL
`max_connections=100`. The 2026-07-26 `dev-story` session lowered the manifest to `40`
(`2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93`) and bound the arithmetic with
`ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`.

Story 27.3 Task 1 states that **any substitution changes the profile hash and requires
another approved course correction**. The profile ID string
(`…-openebs-local-retain-400g-v1`) does not encode `maxConns`, so Task 1's own substitution
guard cannot detect the drift. Until this correction lands, AC1's "immutable profile" and
AC4's "hash-bound approvals" bind an object that three governed records describe
differently.

**Evidence.** Recomputed live on 2026-07-27 from the reviewed canonical profile:

```bash
python3 -c "
import sys; sys.path.insert(0,'tools')
import verify_access_telemetry_lifecycle as v
m = v.canonical_pg_onprem_profile().manifest()
print(m['profile_sha256']); print(m['mutation_manifest_sha256'])"
```

```
dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14
2983ccdebedbd12e34bb1aec363335eb825301ce92d1c4ed87f8956d9c176b84
```

The executable side of the repository is already coherent at `40`: the canonical profile in
`tools/verify_access_telemetry_lifecycle.py`, the pinning fixture
`test_canonical_pg_onprem_profile_hash_is_pinned`, and the live evidence packet's
`reviewed_canonical_profile_sha256` all carry `dc19485835a0…`. **Only the two approval
records are stale.**

### 1.2 CR15 — an independently shipping lane no acceptance criterion declares

`.github/workflows/ci.yml` runs a `production-deployment-verification` job: it installs
kind, Dapr and kubectl, builds the four release OCI archives, renders and applies the
production manifests to a disposable cluster, runs `tools/verify-production-deployment.ps1`,
validates the result with `tools/validate-production-deployment-evidence.ps1`, and uploads
the evidence artifact. The job has no `needs:` dependency and is **not** gated on C1.

`epics.md:5137` assigns the three tools to Story 27.3. Story 27.3 declares no acceptance
criterion, no task, and no checkpoint for the lane. The approved 2026-07-26 correction
simultaneously froze Story 27.3's acceptance criteria, so adding a checkpoint alone would
prove an outcome that no acceptance criterion declares.

**Consequence beyond the bookkeeping:** every C1 gate is blocked on five external inputs
outside the repository. This lane is not. Declaring it gives Story 27.3 its first
independently provable outcome and its first checkpoint a development session can actually
close.

### 1.3 CR16 — the split reproduced the shape it was executed to cure

The 2026-07-26 correction split Story 27.3's bundled scope into Story 30.1 and Story 31.1.
Both new stories reproduce the anti-template shape that `story-scope-guard.md` forbids:

- **Story 30.1** carries seven `Given/When/Then` blocks spanning dispatch hardening,
  manifest migration, four-image publication, partial-release recovery, cutover parity,
  rollback, and registry authorization; names eight "separate reviewable checkpoints" with
  no owner, evidence command, review state or completion state; and gates all of it on an
  unshipped external Hexalith.Builds revision. This is precisely the pattern that let
  independently shippable lanes accumulate under a blocked umbrella in Story 27.3.
- **Story 31.1** bundles OpenBao platform hardening and the runtime `secretstore` migration —
  two independently deployable outcomes — with no checkpoint table at all.

`epics.md:555` and `story-scope-guard.md:30-31` do not bind while both are `backlog`, so
nothing is currently violated. Selecting either as written re-creates the violation, which
is why the reopen trigger is `ready-for-dev`, not `done`.

---

## 2. Impact Analysis

### 2.1 Epic impact

| Epic | Impact | Can it still complete as planned? |
| :--- | :----- | :-------------------------------- |
| Epic 27 | Story 27.3 gains one acceptance criterion (AC6) and one checkpoint (C2). AC1-AC5 unchanged. The ADR profile authority is corrected. | Yes — and strictly better: 27.3 gains a provable outcome. |
| Epic 30 | Story 30.1 splits into four stories (30.1, 30.3, 30.4, 30.5). Story 30.2 unchanged. The external Hexalith.Builds activation gate narrows to the three stories that actually need it. | Yes — same scope, four provable slices. |
| Epic 31 | Story 31.1 splits into two stories (31.1, 31.2). | Yes — same scope, two provable slices. |
| Epic 29 | None. Epic 29 owns Aspire/AppHost-local OpenBao topology; the boundary in `epics.md:5184` is unchanged. | Yes |
| Epic 27.4 | None. Remains `backlog` behind Story 27.3. | Yes |

No epic is added, removed, or resequenced relative to another epic. Epic execution order
`epic-29 → epic-30 → epic-31` is unchanged.

### 2.2 Story impact

**Story 27.3** — additive only. AC1-AC5, Tasks 0-1, and checkpoints C0-C1 are untouched;
the C1 gate evidence table is untouched. AC6 and checkpoint C2 are added. Story 27.3 stays
`in-progress`; C2 is added as `pending` / `not complete` and claims no evidence.

**Story 30.1** → four stories, no scope added or dropped:

| New story | Absorbs from old 30.1 | Independently demonstrable outcome |
| :-------- | :-------------------- | :--------------------------------- |
| 30.1 Guarded release dispatch and shared caller adoption | GWT 1, 2, 3 | A dispatch that cannot publish from a non-tip SHA, an unprotected environment, or an unpinned Builds SHA — and a manifest migrated to `id` with nine IDs intact |
| 30.3 Four-image publication contract | GWT 4, 7 | Four images published from four declared mappings, each verified against platforms and health, with the registry authorization mode recorded |
| 30.4 Partial-release recovery | GWT 5 | A publish that failed mid-way recovers exactly the missing members, never overwriting or retagging |
| 30.5 Release cutover parity and rollback | GWT 6 | Proven parity with the old path, removal of the old path only after parity, and a rollback that changes no published version or mutable tag |

**Story 31.1** → two stories:

| New story | Absorbs from old 31.1 | Independently demonstrable outcome |
| :-------- | :-------------------- | :--------------------------------- |
| 31.1 OpenBao platform hardening and documentation | GWT 1, 3 | The deployed platform documented at its exact configuration, its smoke test runnable, and its single-node limitations surfaced as accepted with owner, consequence, compensating controls and reopen trigger |
| 31.2 Runtime Dapr secret-store migration | GWT 2, 4 | `secretstore.yaml` on `hashicorp.vault` with both scopes, every remaining Kubernetes Secret justified, and no OpenBao SDK or leaked secret in any product project |

### 2.3 File-ownership reassignment

The 2026-07-26 correction transferred ten paths to Story 30.1. The split redistributes them.
No path returns to Story 27.3 and none is orphaned:

| Path | New owner | Reason |
| :--- | :-------- | :----- |
| `.github/workflows/recover-partial-release.yml` | 30.4 | Recovery workflow |
| `tools/complete-partial-release.ps1` | 30.4 | Recovery tooling |
| `tools/publish-containers.ps1` | 30.3 | Builds the four OCI archives |
| `tools/verify-container-registry.ps1` | 30.3 | Registry authorization proof |
| `tests/tooling/publish_containers/*` (4 suites) | 30.3 | Guards the four-image contract |
| `docs/dev/release-runbook.md` | 30.3 | Four-image expansion; 30.5 appends its cutover and rollback sections |
| `tests/Hexalith.Memories.Cli.Tests/Ci/CiTestInventoryTests.cs` | 30.1 | Release/CI inventory contract. Story 27.3's declared cross-story edit under the 2026-07-26 Administrator decision is unaffected |

### 2.4 Artifact conflicts

| Artifact | Conflict | Action |
| :------- | :------- | :----- |
| PRD (`prd.md`) | None — no reference to Epic 30, Epic 31, or deployment verification | No change |
| Architecture (`architecture.md`) | `architecture.md:227` describes Story 27.3 as qualifying `PG-ONPREM-1`; it records no `maxConns` and no story-level breakdown of Epic 30/31 | No change |
| UX specifications | Not applicable — no user-facing surface | N/A |
| `docs/dev/adr-27.1-001-...md` | **Conflict.** Immutable component block pins `maxConns: "64"` | Corrected to `"40"` by this proposal |
| `sprint-change-proposal-2026-07-20-...md` | **Conflict.** Pins `maxConns: "64"` | **Not edited.** Approved dated proposals are append-only; superseded by reference, per the 2026-07-26 Administrator decision |
| `sprint-change-proposal-2026-07-19-...md` | Also pins `"64"`, for the superseded pre-on-premises profile | **Not edited.** Already superseded by the 2026-07-20 correction |
| `epics.md` | Story 27.3 AC set; Epic 30 and Epic 31 story definitions | Updated by this proposal |
| `sprint-status.yaml` | Missing rows for the new stories; `story_execution_order` incomplete | Updated by this proposal |
| `27-3-production-adapter-and-deployment-profile.md` | Missing AC6 and checkpoint C2 | Updated by this proposal |
| `deferred-work.md` | CR14, CR15, CR16 `open` | Resolved by this proposal |
| CI/CD (`ci.yml`) | None — the lane already exists and runs | No change |

### 2.5 Technical impact

None. This proposal changes no product code, no test, no deployment manifest, and no
pipeline. The shipped `maxConns: "40"` is already correct and already guarded; this
correction makes the *approval records* agree with it. The recomputed `profile_sha256` is
not a new value — it is the value the executable canonical profile has carried since the
2026-07-26 review; this proposal makes it the named, approved hash.

---

## 3. Recommended Approach

**Selected path: Option 1 — Direct Adjustment.**

| Option | Verdict | Reasoning |
| :----- | :------ | :-------- |
| 1. Direct Adjustment | **Viable, selected** | Effort Low, risk Low. All three items are already decided by the Administrator; only execution was deferred. Nothing is re-litigated. |
| 2. Potential Rollback | Not viable | Nothing needs reverting. The only implementation change in scope (`maxConns` 64 → 40) is the fix, not the defect. |
| 3. PRD MVP Review | Not viable, not needed | Epics 27, 30 and 31 are all `mvpReadiness: excluded` operational-readiness tracks. The MVP is untouched. |

**Rationale.** These are governance corrections with a decided outcome. The only genuine
design judgement was the Epic 30 split granularity, resolved by the Administrator on
2026-07-27 in favour of four single-outcome stories, on the ground that a two-story split
would leave Story 30.1 carrying five `Given/When/Then` blocks and re-trigger the same
finding at review.

**Timeline impact:** none negative. The correction *removes* blockage in two places: it
gives Story 27.3 a provable checkpoint that does not wait on the five external C1 inputs,
and it narrows the external Hexalith.Builds activation gate from all of Epic 30's release
scope to only the three stories that genuinely need multi-container support.

---

## 4. Detailed Change Proposals

### 4.1 CR14 — ADR immutable component block

**Artifact:** `docs/dev/adr-27.1-001-access-telemetry-lifecycle.md`, `### Required Dapr component configuration`

```
OLD:
    - name: maxConns
      value: "64"

NEW:
    - name: maxConns
      value: "40"
```

**Rationale:** `2 x 64 = 128` exceeds PostgreSQL `max_connections=100` under the ADR
two-writer probe. `40` is the value that fits and is what ships.

**Added immediately after the component block:**

> **`maxConns` corrected 2026-07-27 by approved Sprint Change Proposal 2026-07-27.** This
> block previously pinned `maxConns: "64"`. Two lifecycle replicas each open their own
> sidecar pool, so `"64"` demands 128 connections against `max_connections=100` and
> exhausts the server during the ADR two-writer probe. `"40"` yields
> `2 x 40 + 3 superuser-reserved + 10 evidence sessions = 93` and is the shipped value in
> `deploy/kubernetes/base/dapr/access-telemetry-store.yaml`, guarded by
> `ProductionDeploymentArtifactsTests.ProductionOverlay_AccessTelemetryConnectionPoolFitsPostgreSqlMaxConnections`.
> This is an approved profile change, not a documentation repair: it supersedes the
> `maxConns` pinning in Sprint Change Proposal 2026-07-20, which remains append-only and is
> not edited.
>
> **Authoritative profile hash.** The approved `PG-ONPREM-1` profile hashes to
> `profile_sha256 dc19485835a050395cf73238524d98d735dd84540cdb7cb938512e73c2a63d14` and
> `mutation_manifest_sha256 2983ccdebedbd12e34bb1aec363335eb825301ce92d1c4ed87f8956d9c176b84`.
> The artifact carrying the hash is `canonical_pg_onprem_profile()` in
> `tools/verify_access_telemetry_lifecycle.py`; it is pinned by
> `tests/tooling/access_telemetry_lifecycle/test_adapter_profile.py::AdapterProfileTests::test_canonical_pg_onprem_profile_hash_is_pinned`.
> The hash covers the canonical profile object — identity, capabilities and workload —
> not the rendered Kubernetes manifests and not the running cluster state. AC4's
> hash-bound approvals bind to this value.

### 4.2 CR15 — Story 27.3 acceptance criterion and checkpoint

**Artifact 1:** `_bmad-output/planning-artifacts/epics.md`, Story 27.3 — append AC6:

```
NEW:
6. The kind-based production-deployment-verification lane renders and applies the
   production manifests to a disposable cluster from the four release OCI archives,
   produces verification evidence, and validates that evidence — with any failed render,
   apply, health, or evidence-validation step failing the lane. This criterion is
   independent of AC1-AC5: it neither requires nor unblocks C1.
```

**Artifact 2:** `27-3-production-adapter-and-deployment-profile.md` — the same AC6 in
`## Acceptance Criteria`, in the story's `Given/When/Then` form, plus a new checkpoint row:

```
| C2 - Disposable production-deployment verification | Story 27.3 owner + CI owner |
The `production-deployment-verification` job in `.github/workflows/ci.yml` at a named
run ID and commit SHA, with its uploaded `production-deployment-evidence` artifact; the
job must report success with no skipped render, apply, health, or evidence-validation
step | pending | not complete | — |
```

**Rationale:** the lane ships independently of C1 and is unaccountable to any acceptance
criterion until this lands. The evidence is named as the **CI job**, not a local command,
because `kind` is not installable in every development environment (it is absent from the
current one) while `docker` alone cannot satisfy the lane.

**Explicit non-changes:** AC1-AC5 are untouched. AC5's fail-closed rule is unchanged and
continues to keep Production writes disabled, Story 27.3 `in-progress`, Story 27.4
`backlog`, and A41 open. C2 passing does not advance C1, and C1 being blocked does not
excuse C2. The forward dependency already recorded in Story 27.3 stands, with its owner
updated: the four `.tar.gz` archives this lane consumes are produced by
`tools/publish-containers.ps1`, now **Story 30.3**-owned; Story 30.3 must not regress this
lane.

### 4.3 CR16 — Epic 30 split (1 story → 4)

**Artifact:** `_bmad-output/planning-artifacts/epics.md`, Epic 30.

**Activation gate, narrowed.** The gate currently on Story 30.1 —

> Story 30.1 must not enter implementation until an owner-approved Hexalith.Builds revision
> supports a frozen multi-container publication identity, repeated per-container
> verification without phase collisions, and evidence sufficient for partial-release
> recovery.

— describes a multi-container need. It **moves to Stories 30.3, 30.4 and 30.5**. Story 30.1
(dispatch hardening plus manifest migration) needs the shared caller, not multi-container
support, and is therefore no longer externally blocked.

| Story | Title | Owner | Status | AC blocks |
| :---- | :---- | :---- | :----- | :-------- |
| 30.1 | Guarded release dispatch and shared caller adoption | Memories Maintainer | backlog | dispatch preflight; release-job inputs; `packageId` → `id` migration |
| 30.2 | Shared CI core and module-specific verification lanes | Memories Maintainer + Hexalith.Builds Maintainer | backlog | **unchanged** |
| 30.3 | Four-image publication contract | Memories Maintainer | backlog | four mappings + platform/health verification; registry authorization mode and named blocker |
| 30.4 | Partial-release recovery | Memories Maintainer | backlog | evidence-driven recovery of only the missing members |
| 30.5 | Release cutover parity and rollback | Memories Maintainer | backlog | dry run, rehearsal, parity, old-path removal, rollback |

Each new story carries an **Implementation evidence** requirement naming checkpoints that
each have an owner, an evidence command or artifact, a review state, and a completion state
— the `story-scope-guard.md` condition the old Story 30.1 failed.

The carried zot-registry known risk moves to Story 30.3 with its acceptance criterion.

**Execution order** (`story_execution_order.epic-30`):
`30-2 → 30-1 → 30-3 → 30-4 → 30-5`.
Reason: shared CI alignment first (guarded release requires exact-source green CI); then
dispatch and manifest, which the publication contract consumes; then four-image
publication, which recovery must be able to resume; then cutover, which rehearses all of it.

### 4.4 CR16 — Epic 31 split (1 story → 2)

**Artifact:** `_bmad-output/planning-artifacts/epics.md`, Epic 31.

| Story | Title | Owner | Status | AC blocks |
| :---- | :---- | :---- | :----- | :-------- |
| 31.1 | OpenBao platform hardening and documentation | Memories Maintainer + security reviewer | backlog | topology documented at exact deployed configuration + runnable smoke test; single-node seal and ingress limitations surfaced as accepted with owner, consequence, compensating controls, reopen trigger |
| 31.2 | Runtime Dapr secret-store migration to `hashicorp.vault` | Memories Maintainer + security reviewer | backlog | `secretstore.yaml` on `hashicorp.vault` with both scopes; every remaining Kubernetes Secret justified; no OpenBao SDK, endpoint, or credential in any product project and no secret in logs, telemetry, CLI output, or snapshots |

Both stories carry the same **Implementation evidence** checkpoint requirement. Epic 31's
scope boundary against Epic 29 (`epics.md:5184`) is unchanged and restated on both stories.

**Execution order** (`story_execution_order.epic-31`): `31-1 → 31-2`.
Reason: the migration must land against a documented, hardened platform whose accepted
limitations are already on record, so that the security reviewer evaluates the runtime
change against a known baseline.

### 4.5 Sprint status

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

Added to `development_status` (all `backlog`):
`30-3-four-image-publication-contract`, `30-4-partial-release-recovery`,
`30-5-release-cutover-parity-and-rollback`, `31-2-runtime-dapr-secret-store-migration`.

Renamed: `30-1-four-image-container-release-and-partial-recovery-pipeline` →
`30-1-guarded-release-dispatch-and-shared-caller-adoption`;
`31-1-openbao-platform-hardening-and-runtime-secret-store-migration` →
`31-1-openbao-platform-hardening-and-documentation`.

`story_execution_order.epic-30` extended to five entries; `story_execution_order.epic-31`
added.

No status advances. Story 27.3 stays `in-progress`; Story 27.4 stays `backlog`; every
Epic 30 and Epic 31 story stays `backlog`; `20.5-A41-ACCESS-TELEMETRY-RETENTION` stays open.

### 4.6 Deferred work

`DW 27.3-CR14`, `DW 27.3-CR15`, `DW 27.3-CR16` move from `open` to `resolved`, each citing
this proposal and the artifacts changed.

---

## 5. Implementation Handoff

**Scope classification: Moderate** — backlog reorganization plus two governed-record
corrections.

| Recipient | Responsibility |
| :-------- | :------------- |
| Developer (this correct-course session) | Apply every edit in Section 4 on approval: ADR, `epics.md`, Story 27.3 file, `sprint-status.yaml`, `deferred-work.md`. Run the ADR structure guard, the deployment-artifacts guard, and the `access_telemetry_lifecycle` lane to prove nothing regressed. |
| Product Owner | Confirm the Epic 30 and Epic 31 story boundaries before any of them is set `ready-for-dev`. Still outstanding and **not** addressed here: nominate the independent AC4 security approver for Story 27.3. |
| `create-story` | Create story files for 30.3, 30.4, 30.5 and 31.2 when each is selected. This proposal registers them; it does not author their story files. |
| `dev-story` (Story 27.3) | Prove checkpoint C2 against a named CI run. This is Story 27.3's first checkpoint not blocked by the five external C1 inputs. |

**Success criteria for this correction:**

1. `maxConns` reads `"40"` in the shipped manifest and the ADR, and the approved
   `profile_sha256` is named in the ADR with a statement of what it covers.
2. Story 27.3 declares AC6 and checkpoint C2, both `pending` / `not complete`, claiming no
   evidence.
3. Epic 30 has five stories and Epic 31 has two, each with one independently demonstrable
   outcome and a checkpoint requirement carrying owner, evidence, review state, and
   completion state.
4. `sprint-status.yaml` registers every new story as `backlog`, with execution order for
   both epics, and no status advanced.
5. `DW 27.3-CR14`, `CR15`, `CR16` are `resolved`.
6. The ADR structure guard, `ProductionDeploymentArtifactsTests`, and the
   `access_telemetry_lifecycle` Python lane all pass after the edits.

**Explicitly out of scope:** enabling Production lifecycle writes; any C1 gate; mutating
`20.5-A41-ACCESS-TELEMETRY-RETENTION`; editing any approved dated sprint change proposal;
advancing any story status; authoring the new story files.

---

## 6. Approval

- [x] Administrator approved this Sprint Change Proposal on 2026-07-27. All edits in Section 4 were applied in the same session.

Recorded decision inputs: the Epic 30 split granularity (four stories) was selected by the
Administrator on 2026-07-27. The `maxConns` value, the superseding-correction route, and
the decision to keep the deployment-verification lane inside Story 27.3 were all decided by
the Administrator on 2026-07-26 during Story 27.3 code review and are recorded in
`DW 27.3-CR14`, `CR15`, and `CR16`.
