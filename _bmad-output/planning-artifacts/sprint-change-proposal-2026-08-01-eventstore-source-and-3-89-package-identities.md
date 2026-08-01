# Sprint Change Proposal — Governed EventStore Source and 3.89 Package Identities

- Date: `2026-08-01`
- Project: `memories`
- Trigger: Administrator request to use the latest compatible revisions of root-declared submodules
- Processing mode: Incremental
- Prepared through: `bmad-correct-course`
- Decision owner: Administrator
- Final decision: **Approved by the Administrator on 2026-08-01**
- Scope classification: **Moderate** — Epic 28 story split, one architecture decision, sprint-registry changes, two governed dependency identities, and one independent one-shot gitlink refresh
- Superproject baseline: `b9ae7b9d11a11cd239ff5aef7c552cd393da7b99`
- Existing same-date proposal protection: this descriptive filename is used because `sprint-change-proposal-2026-08-01.md` already exists and must not be overwritten

---

## 1. Issue Summary

The initial live comparison showed all seven root-declared submodule checkouts and gitlinks at their
then-current upstream `main` revisions. Artifact review exposed a governing exception: Epic 28 does
not authorize Memories source mode to follow an arbitrary EventStore tip. EventStore Story 1.20
authorizes source migration to the exact tested runtime SHA
`fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`.

The current EventStore gitlink, checkout, and upstream `main` are
`77d6f47743453d542d96dbe088d5eef7cd05284b`. That revision is 105 commits beyond the approved
runtime and changes runtime and delivery paths, so it cannot be treated as documentation-only or as
equivalent to the approved source identity.

The original Epic 28 story also requires package mode to use the unrecoverable Story 1.20 proof
packages at `999.1.20-proof.fa2d1c9910f8`. Current checks confirm the Client and Aspire proof-package
URLs return HTTP 404. The Administrator selected the published EventStore `3.89.0` package identity
instead. EventStore tag `v3.89.0` points to
`c590590bc581a3f72ef6e67148eda988ba4b8fe6`, all thirteen EventStore package IDs in the shared
catalog are published, and Hexalith.Builds
`10af541e7b2a5a4664be37c9495930844e0954a8` centrally exposes `3.89.0`.

Source and package modes therefore adopt separately governed compatible identities:

- source: exact Story 1.20 tested runtime `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`;
- package: published EventStore `3.89.0`, supplied only through Hexalith.Builds `10af541e...`.

This is a deliberate replacement of the original exact source/package convergence premise. It must
be explicit in architecture and validated in separate source and package lanes.

### 1.1 Live root-submodule disposition

The remotes were refreshed directly from each root-declared repository during this proposal. No
nested submodule was initialized or updated.

| Root-declared submodule | Recorded root gitlink | Refreshed checkout / `origin/main` | Disposition |
| --- | --- | --- | --- |
| `references/Hexalith.AI.Tools` | `859d53b792e1db9562b0ff66dbc335f418a688be` | same | No change |
| `references/Hexalith.Builds` | `418db1d65f67f31fe2f575cf779759d7eb12b4d6` | `10af541e7b2a5a4664be37c9495930844e0954a8` | Advance root gitlink under Story 28.2 |
| `references/Hexalith.Commons` | `d968f9fd7c592976edfca83a45e6596b3cfc3770` | same | No change |
| `references/Hexalith.EventStore` | `77d6f47743453d542d96dbe088d5eef7cd05284b` | same | Move to approved source SHA `fa2d1c99...` under Story 28.1 |
| `references/Hexalith.FrontComposer` | `da5d637ca3cc2099d0d808cc0ab1c366cd1e0e85` | `a746cde4bd128399522f895a7ac7f077c4ee64da` | Advance root gitlink through a separate one-shot trace |
| `references/Hexalith.PolymorphicSerializations` | `6fe872c4f6f726890df6f17f09118954e7b7969f` | same | No change |
| `references/Hexalith.Tenants` | `3503890facf40926c8fd477ffd21918f5c3f181c` | same | No change |

The Builds and FrontComposer checkouts were advanced by the Administrator or another concurrent
workspace actor while this workflow was running. They are preserved as user-owned changes. This
proposal did not perform those checkout moves and does not absorb them into one story.

### 1.2 Core problem classification

- **Trigger category:** dependency identity drift plus an implementation-blocking planning conflict.
- **Source conflict:** latest EventStore `main` is not the exact Story 1.20 authorized source SHA.
- **Package conflict:** the exact Story 1.20 proof packages are unavailable, while the selected
  supported package identity is now `3.89.0` through an updated shared catalog.
- **Story-shape conflict:** the existing Story 28.1 combines independently demonstrable source and
  package outcomes across six Given/When/Then gates without the required checkpoint table.
- **Repository-scope constraint:** only root-declared gitlinks may move; submodule contents and
  nested submodules remain out of scope.

---

## 2. Impact Analysis

### 2.1 Epic and story impact

| Item | Assessment |
| --- | --- |
| Epic 28 | **Changed.** Replace exact source/package convergence with separately governed compatible source and package identities. |
| Existing Story 28.1 | **Split.** It is anti-template: source adoption and package adoption are independent outcomes. |
| New Story 28.1 | Exact approved EventStore source identity adoption. |
| New Story 28.2 | Published EventStore `3.89.0` package identity adoption through Builds `10af541e...`. |
| Epic 27 / Story 27.3 | No scope or status change. Dependency moves are external deltas and must remain named exclusions in its cumulative ledger. |
| Epic 29 | No scope change. AppHost topology is preserved and source/package validation remains additive. |
| Epic 30 | No scope change. Builds `10af541e...` remains a package-catalog dependency, not authority to change Epic 30 release ownership. |
| Epic 31 / Story 31.2 | No scope or status change. Start `dev-story` after Epic 28 dependency adoption so its implementation baseline includes the final gitlinks. |
| FrontComposer refresh | Independent one-shot completion trace; no Epic 28 or sprint-status row. |

### 2.2 Artifact conflicts

| Artifact | Conflict | Approved disposition |
| --- | --- | --- |
| `prd.md` | None. Zero-code EventStore integration and the DAPR topology remain intact. | No change |
| `architecture.md` | No decision currently permits source `fa2d1c99...` and package `3.89.0` as separate compatible identities. | Add D32 and extend the decision registry to D1-D32 |
| `epics.md` | Story 28.1 bundles source and package outcomes and depends on unavailable proof packages. | Replace Epic 28 story content with Stories 28.1 and 28.2 |
| `sprint-status.yaml` | Only one Epic 28 story is registered and no execution order exists. | Replace the row and add `story_execution_order.epic-28` |
| `ux-design-specification.md` | No user-interface or interaction change. | No change |
| Active implementation story files | No ownership transfer is required. | No edits; sequence Story 31.2 after dependency adoption |

### 2.3 Technical impact

The implementation phase will own only these dependency and consumer-boundary changes:

1. EventStore gitlink `77d6f477...` -> `fa2d1c99...`.
2. Builds gitlink `418db1d6...` -> `10af541e...`.
3. FrontComposer gitlink `da5d637c...` -> `a746cde4...` in a separate one-shot.
4. `Hexalith.Memories.Server` gains the missing conditional EventStore Client source project edge,
   complementing the existing AppHost Aspire source project edge.
5. Executable identity guards prevent silent EventStore pin drift, mixed EventStore graphs, local
   package-version overrides, and nested-submodule scope expansion.

No EventStore, Builds, or FrontComposer submodule content is edited by the Memories changes.

### 2.4 Current compatibility evidence

The following evidence was executed against the refreshed user-owned Builds and FrontComposer
checkouts before proposal drafting:

| Evidence | Result |
| --- | --- |
| `dotnet restore Hexalith.Memories.slnx --force-evaluate` | Exit 0 |
| `dotnet build Hexalith.Memories.slnx --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| Release assets — Server | `Hexalith.EventStore.Client/3.89.0` and `Contracts/3.89.0`, both `type=package` |
| Release assets — AppHost | `Hexalith.EventStore.Aspire/3.89.0`, `type=package` |
| `Hexalith.Memories.EventStore.Tests` | 129 passed, 0 failed, 0 skipped |
| Focused Server EventStore/deployment surface | 62 passed, 0 failed, 0 skipped |
| `Hexalith.Memories.Web.Tests` | 492 passed, 0 failed, 0 skipped |
| Thirteen cataloged EventStore `3.89.0` NuGet URLs | HTTP 200 for every ID |

A temporary Debug/source build against EventStore `fa2d1c99...` also passed with 0 warnings and
0 errors, but that is not Story 28.1 completion evidence: the current Server still resolves Client
from package while AppHost resolves Aspire from source. Story 28.1 must first correct that mixed graph
and then re-run the source lane.

---

## 3. Recommended Approach

**Selected: Direct Adjustment.** Split Epic 28 into bounded source and package stories, add D32,
update the sprint registry, and route the unrelated FrontComposer pointer through a one-shot trace.

| Option | Verdict | Reason |
| --- | --- | --- |
| Direct adjustment | **Selected** | Preserves the PRD, makes both identity domains executable, and keeps independent dependency outcomes separate. |
| Pointer-only rollback | Rejected | Pinning only EventStore leaves a mixed source graph, lacks drift protection, and does not resolve package identity. |
| PRD/MVP review | Rejected | No FR, NFR, or UX commitment is dropped; Epic 28 is operational dependency adoption. |

### 3.1 Decisions ratified incrementally

1. The Administrator selected the governed EventStore source correction rather than closing the
   initial latest-tip request as a no-op.
2. EventStore source identity is the exact Story 1.20 tested runtime `fa2d1c99...`.
3. EventStore package identity is the published version `3.89.0`, not `3.88.0` and not the
   unrecoverable proof version.
4. Hexalith.Builds `10af541e...` is the sole package-version authority for that identity.
5. Source and package identities are compatible-but-distinct evidence domains; neither substitutes
   for the other.
6. The original Story 28.1 is split into two stories; no decision-only third story remains necessary
   after Builds published the `3.89.0` catalog update.
7. FrontComposer latest-tip adoption remains independent from Epic 28.

### 3.2 Risk assessment

| Risk | Mitigation |
| --- | --- |
| Source `fa2d1c99...` and package `3.89.0` behave differently | Separate pristine-lane dependency graphs, builds, focused contracts, and zero-code topology tests; never claim byte-identical convergence. |
| Automation advances EventStore away from the approved source SHA | Exact gitlink/checkout drift guard in ordinary CI; fail before claiming Story 28.1 completion. |
| A local override hides a stale Builds catalog | Guard zero `Version`, `VersionOverride`, local `PackageVersion`, or fallback authority for EventStore packages. |
| Builds or FrontComposer pointer changes are absorbed into the wrong story | Separate Story 28.2 and one-shot ownership, with explicit File Scope and gitlink evidence. |
| Active Story 31.2 receives baseline drift | Start its implementation after Epic 28 adoption; retain current `ready-for-dev` status until then. |
| Nested dependency work expands silently | Root `.gitmodules` inventory and non-recursive commands are mandatory; nested initialization/update fails the story. |

---

## 4. Detailed Change Proposals

### 4.1 `_bmad-output/planning-artifacts/epics.md`

Replace the current Epic 28 description and Story 28.1 with the following governed structure.

#### Epic 28: Governed EventStore Source and Package Identity Adoption

Memories adopts separately governed compatible EventStore identities in Debug/source and
Release/package modes while preserving the existing zero-code DAPR ingestion contract. Source and
package evidence are independent: success in one mode never substitutes for the other, and Epic 28
is complete only when both stories are done.

**Lifecycle label:** Operational Readiness / EventStore Dependency Adoption.

**Sequencing:** Story 28.1 precedes Story 28.2. Story 31.2 implementation starts only after Story
28.2 so its baseline includes the final dependency gitlinks.

##### Story 28.1: Adopt Owner-Approved EventStore Source Identity

**Status:** backlog. **Owner:** Memories Maintainer + EventStore Maintainer.

As a Memories maintainer,
I want Debug/source mode pinned to the exact owner-approved EventStore runtime identity,
So that source debugging is reproducible and cannot drift to an unapproved runtime.

**Acceptance criteria:**

1. Given EventStore Story 1.20 authorization, when activation is evaluated, then
   `final_decision: available`, `authorize_consumer_migration: true`, the exact 40-hex
   `tested_runtime_sha`, and named owner approvals are reverified from the current proof packet;
   otherwise the story remains backlog and no gitlink changes.
2. Given the approved SHA `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`, when source adoption is
   implemented, then the root gitlink and checkout both equal that SHA, EventStore content is clean,
   and only Memories-root-declared submodules are initialized.
3. Given explicit `UseHexalithProjectReferences=true`, when the Debug/source graph is restored, then
   EventStore Client and Aspire resolve as projects rooted at that exact checkout, every selected
   EventStore project is version-aligned, and zero EventStore package edge remains.
4. Given Memories' zero-code EventStore integration, when the source identity changes, then
   `AddMemoriesServerServices()` -> `AddServerEventStoreIntegration()` ->
   `AddMemoriesEventStoreIntegration()`, `UseCloudEvents()`, `MapControllers()`,
   `MapSubscribeHandler()`, `/events/ingest`, `pubsub`, and `MEMORIES_EVENTSTORE_TOPIC` remain intact.
5. Given the exact pin, when CI and focused validation run, then an executable drift guard rejects a
   different gitlink or checkout, Debug/source restore and build pass, focused EventStore and Server
   contracts pass, and the DAPR ingestion lane proves persisted/searchable output with duplicate
   replay ignored. A compatibility failure routes to a separate approved correction.

**Implementation evidence:** the story file carries a checkpoint table with owner, exact command or
artifact, review state, and completion state for authorization, gitlink/checkout, source graph,
zero-code topology, build/test/integration, and drift-guard rows.

**Owned implementation paths:** `references/Hexalith.EventStore` (gitlink only),
`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`, and a focused dependency-identity
guard under `tests/`. `Directory.Build.props`, AppHost topology, EventStore submodule contents, and
nested submodules are read/verify-only unless a later approved finding widens scope.

##### Story 28.2: Adopt Published EventStore 3.89.0 Package Identity

**Status:** backlog. **Owner:** Memories Maintainer + Hexalith.Builds Maintainer.

**Activation gate:** Story 28.1 is done, and Builds
`10af541e7b2a5a4664be37c9495930844e0954a8` remains reachable from Builds `origin/main` and exposes
`HexalithEventStoreVersion` `3.89.0` for every centrally cataloged EventStore package.

As a Memories release maintainer,
I want Release/package mode to consume EventStore `3.89.0` exclusively through Hexalith.Builds,
So that published builds use one available, reproducible, and centrally governed package identity.

**Acceptance criteria:**

1. Given Builds `10af541e...`, when package identity is adopted, then the root Builds gitlink and
   checkout equal that commit, its single `HexalithEventStoreVersion` is `3.89.0`, all EventStore
   catalog entries use it, and the consumed packages are retrievable from the configured public
   source.
2. Given Release/package intent, when restore is forced in an isolated lane, then every resolved
   `Hexalith.EventStore*` asset is `type=package` at `3.89.0`, zero EventStore project edge remains,
   and no Memories-local version authority or override exists.
3. Given the selected package identity, when its bytes are recorded, then every consumed EventStore
   package is signature-verified where supported and its downloaded SHA-256 is persisted with the
   evaluated `project.assets.json` evidence. Historical Story 1.20 proof hashes are not reused.
4. Given the zero-code integration contract, when package mode builds and tests, then the same
   registration, middleware, route, topic, component, and duplicate-replay invariants from Story
   28.1 remain green.
5. Given package validation, when completion is claimed, then Release restore/build, focused
   EventStore/Server contracts, release-package topology validation, and the required DAPR
   integration evidence pass without EventStore project edges or a local version override.

**Implementation evidence:** the story file carries a checkpoint table for Builds identity, package
availability/bytes, evaluated dependency graph, zero-code topology, and build/test/integration rows.

**Owned implementation paths:** `references/Hexalith.Builds` (gitlink only) and focused
dependency-identity evidence/guards under `tests/` or the story evidence directory. Builds submodule
content and nested submodules are never edited by the Memories story.

### 4.2 `_bmad-output/planning-artifacts/architecture.md`

Extend the decision registry to D1-D32 and add:

#### D32 — Staged EventStore Consumer Identity Adoption

EventStore source and package identities are separate consumer evidence domains. Memories
Debug/source mode uses the exact Story 1.20 owner-approved tested runtime
`fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`. Memories Release/package mode uses published
EventStore `3.89.0`, bound to EventStore tag commit
`c590590bc581a3f72ef6e67148eda988ba4b8fe6` and supplied centrally by Hexalith.Builds
`10af541e7b2a5a4664be37c9495930844e0954a8`.

The identities are required to be behaviorally compatible but are not claimed to be byte-identical
or source-equivalent. Each mode has its own pristine restore, evaluated dependency graph, build,
focused contract evidence, and integration evidence. A pass in one mode grants no evidence in the
other.

The historical `999.1.20-proof.fa2d1c9910f8` packages are unavailable and remain audit history;
they are not build inputs and their hashes are never applied to `3.89.0`. EventStore package versions
come only from the selected Builds catalog. Memories-local `Version`, `VersionOverride`, local
`PackageVersion`, fallback properties, submodule-content edits, and nested submodule updates are
forbidden. Any future source or package identity change requires equivalent owner-approved evidence
and cannot be inferred from a moving branch tip.

### 4.3 `_bmad-output/implementation-artifacts/sprint-status.yaml`

Replace the current Epic 28 rows with:

```yaml
epic-28: backlog
28-1-adopt-owner-approved-eventstore-source-identity: backlog
28-2-adopt-published-eventstore-3-89-0-package-identity: backlog
epic-28-retrospective: optional
```

Add:

```yaml
story_execution_order:
  epic-28:
    reason: "Source identity is adopted independently before package-mode adoption validates the published 3.89.0 catalog through the approved Builds revision."
    order:
      - 28-1-adopt-owner-approved-eventstore-source-identity
      - 28-2-adopt-published-eventstore-3-89-0-package-identity
```

Merge this entry into the existing `story_execution_order` mapping; do not introduce a second key.
No active story status changes.

### 4.4 FrontComposer latest-compatible one-shot

Do not add an epic/story sprint row. Author a one-shot implementation trace under
`_bmad-output/implementation-artifacts/` with `route: one-shot` and `status: done` only after:

- the root FrontComposer gitlink equals checkout and `origin/main`
  `a746cde4bd128399522f895a7ac7f077c4ee64da`;
- no FrontComposer submodule content or nested submodule is changed;
- the Release solution build and focused web tests pass on the final clean tree;
- the trace records the exact command, result, pointer delta, and Files list.

The current 0-warning/0-error Release build and 492/492 Web tests are pre-change compatibility
evidence; re-run them after the gitlink is recorded before marking the one-shot done.

### 4.5 Explicit no-change artifacts

- `prd.md`: no change.
- `ux-design-specification.md`: no change.
- Story 27.3, 29.2, 31.1, and 31.2 implementation files: no change.
- Root gitlinks for AI.Tools, Commons, PolymorphicSerializations, and Tenants: no change.

---

## 5. Epic AC Verification

Every planning claim below was re-derived against current source or classified honestly.

| Claim | Verification command or artifact | Verdict | Disposition |
| --- | --- | --- | --- |
| Story 1.20 authorizes migration and pins `fa2d1c99...` | EventStore `1-20-owner-approved-parity-closure-proof-packet.md` frontmatter: `tested_runtime_sha`, `final_decision`, `authorize_consumer_migration`, approval URLs | Confirmed | Story 28.1 activation authority |
| Current EventStore tip is not equivalent to the tested runtime | `git rev-list --count fa2d1c99..77d6f477`; `git diff --shortstat` and runtime/delivery path classification | Confirmed — 105 commits, 682 files | Exact source pin remains required |
| Current Debug/source mode already resolves all EventStore edges from source | Root csproj/props inspection plus temporary source build | **Corrected** — Aspire is source, Client remains package | Story 28.1 owns the conditional Client source edge and zero-package graph proof |
| Story 1.20 proof packages are usable package inputs | NuGet flat-container checks for Client/Aspire proof version | **Corrected** — HTTP 404 | D32 forbids using the proof version as a build input |
| No copy of any proof package exists in every possible historical location | Tenants Story 2.12 negative audit | Historical-reference-only; not fully re-executed here | This proposal does not depend on the universal claim; it selects published `3.89.0` |
| Builds exposes `3.89.0` centrally | `git show 10af541e:Props/Directory.Packages.props` | Confirmed | Story 28.2 identity authority |
| Cataloged EventStore `3.89.0` packages are published | NuGet flat-container HTTP checks for all 13 catalog IDs | Confirmed — 13/13 HTTP 200 | Re-run and persist consumed bytes/hashes during Story 28.2 |
| Memories package graph resolves EventStore `3.89.0` only | `jq` over Server and AppHost Release `project.assets.json` | Confirmed — Client, Contracts, Aspire all `type=package` at `3.89.0` | Current compatibility evidence; re-run on final gitlinks |
| Zero-code DAPR topology still exists | `rg` over registration extensions, `Program.cs`, controller, deploy components, and focused tests | Confirmed | Binding acceptance criteria in both stories |
| Latest FrontComposer checkout is compatible | Release build and `Hexalith.Memories.Web.Tests` | Confirmed — build 0/0, tests 492/492 | Re-run after recording the final gitlink |

No `corrected` claim is left as untracked prose: the source-graph correction is assigned to Story
28.1, and the unavailable proof-package correction is encoded in D32 and Story 28.2.

---

## 6. Historical Context Classification and Slice Proof

### 6.1 Historical context classification

| Prior material | Classification | Use in this proposal |
| --- | --- | --- |
| Story 0.0 root submodule bootstrap | current-narrow-pattern | Reuse only the root-declared, non-recursive initialization safeguard |
| Story 15.6 submodule guard | current-narrow-pattern | Reuse root inventory/missing-marker guard style; it does not prove exact identity |
| Existing Story 28.1 | anti-template | Must not be copied; it bundles source and package outcomes across six gates without checkpoint evidence |
| EventStore/Tenants Story 2.12 re-scope | historical-reference-only | Use its package-retention lesson and audit provenance; its Tenants-scoped AD-22 exception grants no Memories authority |
| Pre-split Stories 30.1 and 31.1 | anti-template | Reinforces splitting independent publication/platform outcomes before story creation |
| Routine prior submodule bump commits | historical-reference-only | Point-in-time provenance only; not acceptance templates |

### 6.2 Slice proof

- Story 28.1 has one independently demonstrable outcome: exact source identity adoption.
- Story 28.2 has one independently demonstrable outcome: published package identity adoption.
- The FrontComposer gitlink refresh is a third independent outcome and is routed as a one-shot.
- Neither Story 28.1 nor Story 28.2 may claim the other's dependency graph or test evidence.
- Both stories require checkpoint tables even though each is narrowed, because dependency identity,
  graph, topology, validation, and drift evidence must remain independently reviewable.
- No story uses Story 0.0, Story 15.6, the original Story 28.1, or Tenants Story 2.12 as a whole-story
  template.

This satisfies the historical-slice guard without suppressing or reclassifying prior broad stories.

---

## 7. Implementation Handoff

### 7.1 Routing

| Recipient | Responsibility |
| --- | --- |
| Product Manager / Scrum Master | Apply the approved Epic 28 split and sprint-status registry/order changes. |
| Architect | Apply and ratify D32 exactly as approved. |
| Story author (`bmad-create-story`) | Create Story 28.1 with the checkpoint table and current Epic AC Verification; after completion, create Story 28.2. |
| Developer (`bmad-dev-story`) | Implement each story from its own clean baseline and declared File Scope; do not edit submodule contents. |
| Developer, separate one-shot route | Record FrontComposer `a746cde4...` root gitlink and its focused final-tree evidence. |
| Reviewer | Verify exact gitlinks, dependency graphs, no nested work, checkpoint completion, and non-crediting of another route's evidence. |

### 7.2 Required implementation order

1. Apply the planning-artifact changes from sections 4.1-4.3.
2. Create and implement Story 28.1.
3. Create and implement Story 28.2.
4. Complete the independent FrontComposer one-shot; it may run before or after Epic 28 only if its
   changed-file set and evidence remain isolated.
5. Rebaseline Story 31.2 implementation against the resulting dependency gitlinks, then start its
   `dev-story` execution.

### 7.3 Final success criteria

1. Root EventStore gitlink and checkout equal `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`.
2. Explicit Debug/source mode resolves every selected EventStore edge as a project from that exact
   checkout and zero EventStore package edge.
3. Root Builds gitlink and checkout equal `10af541e7b2a5a4664be37c9495930844e0954a8`.
4. Release/package mode resolves every selected EventStore asset as `type=package` at `3.89.0`,
   with zero EventStore project edge and no local version authority.
5. Root FrontComposer gitlink and checkout equal
   `a746cde4bd128399522f895a7ac7f077c4ee64da`, with its one-shot evidence complete.
6. AI.Tools, Commons, PolymorphicSerializations, and Tenants gitlinks remain unchanged at the tabled
   revisions.
7. Zero-code EventStore registration, CloudEvents middleware, subscription discovery, route, topic,
   and duplicate-replay contracts remain green in both dependency modes.
8. No nested submodule is initialized or updated; every pointer is a root `160000 commit` entry.
9. Final root worktree changes are fully attributed to Story 28.1, Story 28.2, the FrontComposer
   one-shot, or the approved planning update; unrelated user changes remain preserved.
10. All commit messages, if later authorized, pass commitlint before and after commit and use valid
    Conventional Commit subjects such as `build(deps): adopt EventStore 3.89.0 through Hexalith.Builds`.

### 7.4 Explicitly out of scope

- Editing EventStore, Builds, FrontComposer, or any other submodule content.
- Initializing or updating a nested submodule.
- A Memories-local EventStore package-version override.
- Treating EventStore `origin/main` tip as source authorization.
- Treating the Story 1.20 proof-package hashes as `3.89.0` hashes.
- Reopening or changing the scope/status of Epics 27, 29, 30, or 31.
- Committing, staging, pushing, or publishing during this correct-course workflow.

---

## 8. Checklist Record

| Section | Item | Status |
| --- | --- | --- |
| 1 | Trigger and context | Complete — dependency refresh request plus Epic 28 identity conflict |
| 2 | Epic impact | Complete — Epic 28 split; other active epics preserved |
| 3 | Artifact conflicts | Complete — Architecture, Epics, Sprint Status change; PRD/UX no change |
| 4 | Path options | Complete — Direct Adjustment approved; pointer-only rollback and PRD review rejected |
| 5 | Proposal components | Complete — issue, impact, handoff, success criteria, AC verification, and slice proof included |
| 6 | Final approval | Complete — approved by the Administrator on 2026-08-01 |

### Incremental decision log

- Processing mode: `incremental`.
- Section 1 disposition: continue with governed EventStore correction.
- Section 2 impact assessment: confirmed.
- Section 3 artifact mapping: confirmed.
- Direct Adjustment: approved `[a]`.
- Epic edit: approved, then revised from three stories to two after `3.89.0` was selected and Builds
  `10af541e...` became available; consolidated revision approved `[a]`.
- Architecture D32: `3.88.0` replaced by Administrator-selected `3.89.0`; consolidated revision
  approved `[a]`.
- Sprint-status edit: approved `[a]`.
- Assembled proposal: approved `[a]`.

No planning artifact other than this proposal has been changed by the workflow. No source,
configuration, test, gitlink, submodule content, story status, commit, or external destination has
been mutated by this proposal-writing step.
