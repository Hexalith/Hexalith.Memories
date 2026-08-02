---
title: "AppHost / EventStore.Aspire integration validation correction"
date: 2026-08-02
status: implemented
decision: formally-accept-narrowed-runtime-topology-gap
approval: approved-2026-08-02
workflow: bmad-correct-course
trigger_action: "Resolve or formally accept the AppHost/EventStore.Aspire integration validation blocker before treating full-stack Redis/FalkorDB proof as available"
classification: minor
---

# Sprint Change Proposal — AppHost / EventStore.Aspire Integration Validation

## 1. Executive Summary

Formally accept the remaining **EventStore-originating runtime-topology and approved-identity gap** as
`23.7-APPHOST-EVENTSTORE-FULLSTACK`, while recording the historical package/source compile blocker as
resolved.

This acceptance closes the three duplicated resolve-or-accept sprint actions, but it does **not** make
EventStore-to-Memories full-stack proof available. Current evidence supports the narrower statement that
the Memories-owned Aspire topology exercises Redis Stack, FalkorDB, the Memories Dapr sidecar, direct
`POST /events/ingest`, and Dapr pub/sub ingestion. It does not provision or exercise an `eventstore`
gateway resource.

Story 28.1 remains the implementation owner for resolving the accepted gap. Its plan must now recognize
that EventStore Story 1.20 has authorized consumer migration, while still requiring Memories to adopt the
exact approved source/package identities and converge Dapr component ownership before claiming the
EventStore-originating path.

No production code, test behavior, epic ordering, or UX scope changes in this correction.

## 2. Trigger and Current Diagnosis

### 2.1 Trigger

Epic 23 retrospective action:

> Resolve or formally accept the AppHost/EventStore.Aspire integration validation blocker before
> treating full-stack Redis/FalkorDB proof as available.

Equivalent open actions exist under Epics 22 and 24.

### 2.2 What changed since the 2026-07-21 draft

The July proposal is not approval authority and was never applied. Its central external-gate premise is
now stale:

- EventStore Story 1.20 now records `final_decision: available`,
  `authorize_consumer_migration: true`, and the approved 40-hex runtime identity
  `fa2d1c9910f8976553adb33dcdb1c9ff2ea75594`.
- The historical duplicate-assembly compile concern is resolved: the Memories integration project builds
  cleanly in both package and source modes.
- The Memories AppHost still calls only `AddHexalithEventStoreSecurity()`; it does not call
  `AddHexalithEventStoreGatewayProject()`, `AddHexalithEventStorePlatformProjects()`, or
  `AddHexalithEventStore(...)`.
- The current EventStore checkout (`30810727cb91f5886cb2aa13601680a23b18bcc0`) and restored package-mode
  assets (`Hexalith.EventStore.Aspire` and `Hexalith.EventStore.Client` `3.89.0`) do not equal Story
  1.20's exact approved source/package pins (`fa2d1c99...` and
  `999.1.20-proof.fa2d1c9910f8`).
- The current EventStore Aspire helper exposes a gateway-only composition, but it also adds Dapr resources
  named `statestore` and `pubsub`. Memories already owns resources with those names, so integration needs
  an explicit shared-component ownership design rather than a blind helper call.
- The focused Aspire ingestion lane passes 2/2. Its test inputs are direct `/events/ingest` and Dapr
  pub/sub publish to Memories; no EventStore gateway produces the event.

### 2.3 Root cause

The original blocker mixed three different evidence questions:

1. **Compile compatibility** — now resolved for the current package and source graphs.
2. **Memories infrastructure depth** — proven for the focused Redis/FalkorDB + Dapr intake lane.
3. **EventStore-to-Memories system boundary** — still unavailable because the AppHost has no EventStore
   gateway resource and Memories has not adopted Story 1.20's exact approved identities.

The correction separates those claims. A test tier describes infrastructure depth; it does not, by
itself, identify every producer or service boundary traversed.

## 3. Change-Impact Analysis

### 3.1 Epic impact

| Area | Impact | Disposition |
| --- | --- | --- |
| Epic 22 | One duplicate resolve-or-accept action remains open. | Close by formal acceptance of the narrowed blocker. No delivered ingestion result is reclassified. |
| Epic 23 | Triggering action remains open. | Close by formal acceptance; retain current Redis/FalkorDB evidence only with an explicit Memories-owned boundary. |
| Epic 24 | One duplicate future-claim guard remains open. | Close by formal acceptance; preserve the guard through the accepted blocker and architecture taxonomy. |
| Epic 28 / Story 28.1 | Activation narrative is stale because Story 1.20 now authorizes migration. | Record the prerequisite as satisfied, keep the story in backlog until selected, and bind blocker resolution to exact identity adoption plus real EventStore-originating proof. |
| Other epics | No sequencing or scope change. | No change. |

### 3.2 Artifact impact

| Artifact | Required change |
| --- | --- |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Convert the existing unstructured AppHost `eventstore` entry into accepted blocker `23.7-APPHOST-EVENTSTORE-FULLSTACK`, preserving its Story 21.2 provenance and adding current evidence, resolution criteria, and reopen triggers. |
| `_bmad-output/implementation-artifacts/sprint-status.yaml` | Close the Epic 22/23/24 duplicate actions by formal acceptance and refresh the Epic 28 activation comment. Do not change Epic 28 or Story 28.1 from `backlog`. |
| `_bmad-output/planning-artifacts/architecture.md` | Add a claim taxonomy that distinguishes compile, Memories-owned infrastructure, event-intake, and EventStore-originating evidence. |
| `docs/dev/eventstore-integration.md` | Add a visible evidence boundary near the integration overview. |
| `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs` | Correct the class summary so it describes both direct and Dapr-publish tests and explicitly excludes an EventStore gateway claim. Test behavior remains unchanged. |
| `_bmad-output/planning-artifacts/epics.md` | Refresh Epic 28's activation state and strengthen Story 28.1's closure proof for the accepted blocker. |

### 3.3 PRD, architecture, and UX consistency

- **PRD:** no requirement conflict. The PRD already assigns AppHost local orchestration ownership and
  places EventStore integration in Phase 1.5.
- **Architecture:** the technical direction remains valid, but its tier language needs a system-boundary
  qualifier to prevent evidence inflation.
- **UX:** no user journey, interaction, accessibility, or visual behavior changes.
- **Technical feasibility:** formal acceptance is immediately feasible. Resolution is feasible only as a
  separately selected implementation slice because it changes AppHost resource composition, dependency
  identity, and executable full-stack verification.

### 3.4 Scope classification

**Minor.** The correction adds a governed claim boundary, closes duplicated resolve-or-accept actions,
and refreshes one existing backlog story. It adds no epic or story and does not reorder the roadmap.

## 4. Recommended Path

Choose the action's **formal acceptance** branch now:

1. Mark compile compatibility resolved.
2. Accept only the remaining EventStore-originating topology and approved-identity gap.
3. Preserve current focused Aspire evidence as valid for the Memories-owned Redis/FalkorDB + Dapr intake
   boundary.
4. Prohibit `EventStore-to-Memories`, unqualified `full-stack EventStore`, or equivalent claims until
   Story 28.1 closes the accepted blocker with executable evidence.
5. Keep Story 28.1 in backlog until explicitly selected; external activation is now satisfied, but
   implementation is not.

Immediate runtime resolution is not part of this correction because a naive integration would create
competing `statestore` and `pubsub` ownership. Story 28.1 must deliberately select or introduce a single
component owner and prove that every existing Memories sidecar continues to use the intended Redis
resources.

## 5. Incremental Edit Proposals

The following edits are proposed individually. Approval of this proposal authorizes all six unless an
edit is explicitly revised or skipped.

### Edit 1 — Formalize the accepted deferred-work item

**File:** `_bmad-output/implementation-artifacts/deferred-work.md`

Replace the existing Story 21.2 bullet beginning ``eventstore` service not yet provisioned in the AppHost
topology` with a structured record equivalent to:

```markdown
- **23.7-APPHOST-EVENTSTORE-FULLSTACK - accepted.** Current package/source compilation and the
  Memories-owned Aspire Redis/FalkorDB + Dapr ingestion lane pass, but the AppHost does not provision an
  `eventstore` gateway resource and current source/package identities do not match EventStore Story
  1.20's exact approved pins. The focused event-ingestion lane publishes directly to Memories and is not
  EventStore-to-Memories proof.

  - ID: 23.7-APPHOST-EVENTSTORE-FULLSTACK
  - Status: accepted
  - Source story: story-21.2 dev; Epic 23 retrospective corrective action
  - Target artifact: `_bmad-output/planning-artifacts/epics.md` (Story 28.1)
  - Resolution criteria: adopt the exact owner-approved EventStore source/package identities; compose one
    `eventstore` gateway resource with unambiguous `statestore`/`pubsub` ownership; run a real
    EventStore-originating publish through Dapr into Memories; prove persisted/searchable Redis and
    FalkorDB outcomes plus ignored duplicate replay; attach tenant-isolation negative evidence.
  - Re-open trigger: Story 28.1 is selected; any story or review claims EventStore-to-Memories or
    unqualified full-stack EventStore proof; or the AppHost adds an `eventstore` resource without closing
    every resolution criterion.
```

Preserve the original Story 21.2 rationale as provenance inside the accepted record; do not create a
second duplicate entry.

### Edit 2 — Close duplicated sprint actions and refresh Epic 28 commentary

**File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

Change the three matching actions under Epics 22, 23, and 24 from `open` to `done`, each with a dated
comment equivalent to:

```yaml
status: done  # 2026-08-02: Current package/source compile validation is green; the narrower missing EventStore-originating gateway and exact-identity proof is formally accepted as 23.7-APPHOST-EVENTSTORE-FULLSTACK. Memories-owned Redis/FalkorDB + Dapr intake evidence remains valid, but this closure does not make EventStore-to-Memories full-stack proof available.
```

Refresh the Epic 28 comment so it says Story 1.20's external activation prerequisite is satisfied as of
2026-08-02, while Epic 28 and Story 28.1 remain `backlog` pending explicit selection and implementation.
Do not change their status values.

### Edit 3 — Add the evidence claim taxonomy

**File:** `_bmad-output/planning-artifacts/architecture.md`

Add this rule next to the existing test-tier definitions:

```markdown
**Evidence claim boundary:** A tier identifies infrastructure depth, not every service boundary
traversed. Evidence must name both the tier and the exercised system boundary.

- Package/source build evidence proves compile and dependency-graph compatibility only.
- Memories Aspire evidence may claim the concrete resources observed, including Redis Stack,
  FalkorDB, OpenBao, and Memories Dapr sidecars.
- Direct `POST /events/ingest` or Dapr publish to the Memories `pubsub` subscription proves the
  Memories event-intake contract; it does not prove an EventStore producer or gateway.
- EventStore-to-Memories full-stack evidence requires an AppHost-provisioned `eventstore` resource,
  Story 1.20-aligned source/package identity, an EventStore-originating event, persisted/searchable
  Redis and FalkorDB outcomes, duplicate replay proof, and tenant-isolation negative evidence.
```

### Edit 4 — Publish the developer-facing claim boundary

**File:** `docs/dev/eventstore-integration.md`

Add an **Evidence claim boundary** subsection after the overview:

- explain that `Hexalith.Memories.EventStore` names the adapter package but does not prove an
  EventStore gateway participated;
- identify the two current focused paths: direct `/events/ingest` and Dapr publish to Memories;
- state that the current AppHost does not provision `eventstore`;
- link the unavailability of EventStore-originating proof to accepted blocker
  `23.7-APPHOST-EVENTSTORE-FULLSTACK` and Story 28.1;
- retain the current setup and route documentation unchanged.

### Edit 5 — Correct the integration-test class summary

**File:** `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`

Replace the class summary with wording equivalent to:

```csharp
/// <summary>End-to-end coverage for the Memories event-intake surface. Exercises both direct
/// <c>POST /events/ingest</c> and Dapr pub/sub publish into the Memories subscription, then proves
/// workflow completion, searchable persistence, subject filtering, and duplicate suppression against
/// the Memories-owned Aspire Redis/FalkorDB topology. The fixture does not provision an EventStore
/// gateway resource, so this class is not EventStore-to-Memories full-stack evidence.</summary>
```

No test body, assertion, category, or fixture behavior changes.

### Edit 6 — Refresh Epic 28 and bind Story 28.1 to blocker closure

**File:** `_bmad-output/planning-artifacts/epics.md`

Make these targeted changes:

1. Replace both stale Epic 28 activation paragraphs with a dated activation-state note: Story 1.20 now
   records the required available/authorized fields and exact identities, so the external gate is
   satisfied; Epic 28 and Story 28.1 remain backlog until explicitly selected.
2. Preserve the fail-closed AC for any future loss or invalidation of authorization.
3. Add an AC binding Story 28.1 to `23.7-APPHOST-EVENTSTORE-FULLSTACK`:

```markdown
**Given** `23.7-APPHOST-EVENTSTORE-FULLSTACK` is accepted,
**When** Story 28.1 claims the blocker resolved,
**Then** the AppHost provisions one `eventstore` gateway resource without duplicate `statestore` or
`pubsub` ownership, a real EventStore-originating publish reaches Memories through Dapr, the resulting
memory is persisted and searchable through Redis and FalkorDB, duplicate replay is ignored, and attached
negative evidence proves no cross-tenant result leakage.
```

4. Keep the existing zero-code Dapr ingestion contract and compatibility fail-closed ACs.
5. Do not create a Story 28.1 implementation file or change its sprint status in this correction.

## 6. Verification Evidence and Epic-Claim Audit

Every current-state claim introduced by the proposed edits was rechecked against the current tree.

| Claim | Verification command / source | Verdict |
| --- | --- | --- |
| EventStore Story 1.20 authorizes migration and pins exact identities. | `sed -n '1,135p' references/Hexalith.EventStore/_bmad-output/implementation-artifacts/1-20-owner-approved-parity-closure-proof-packet.md` | **Confirmed:** `available`, `true`, runtime `fa2d1c99...`, package `999.1.20-proof.fa2d1c9910f8`, hashes and owner references present. |
| The current EventStore checkout differs from the approved runtime SHA. | `git -C references/Hexalith.EventStore rev-parse HEAD`; compare with packet `tested_runtime_sha`. | **Confirmed:** `30810727...` != `fa2d1c99...`. |
| Current package-mode Client/Aspire assets differ from the Story 1.20 package pin. | `rg '"Hexalith\\.EventStore\\.(Aspire|Client)/' .../obj/project.assets.json` | **Confirmed:** both resolve to `3.89.0`, not `999.1.20-proof.fa2d1c9910f8`. |
| Package-mode integration-project compilation passes. | `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --no-restore -m:1 /nodeReuse:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | **Confirmed:** exit 0, 0 warnings, 0 errors. |
| Source-mode integration-project compilation passes. | Same build plus `-p:UseHexalithProjectReferences=true`. | **Confirmed:** exit 0, 0 warnings, 0 errors. |
| The focused Aspire intake lane passes against real local infrastructure. | `dotnet test ... --no-build --no-restore -m:1 ... --filter 'FullyQualifiedName~EventIngestionPipelineIntegrationTests'` | **Confirmed:** 2/2 passed; direct ingest and Dapr pub/sub tests. Docker 29.6.1, Dapr CLI 1.18.0/runtime 1.18.1, and Aspire 13.4.6 were available. |
| Memories does not compose an EventStore gateway/platform topology. | `rg 'AddHexalithEventStoreGatewayProject|AddHexalithEventStorePlatformProjects|AddHexalithEventStore\\(' . --glob '!references/**' --glob '!_bmad-output/**' ...` | **Confirmed:** no matches. `Program.cs` calls only `AddHexalithEventStoreSecurity()`. |
| A blind EventStore helper call would duplicate Dapr component ownership. | Compare `AddDaprComponent` / `AddDaprPubSub` calls in Memories `Program.cs` and EventStore `HexalithEventStoreExtensions.cs`. | **Confirmed:** both add resources named `statestore` and `pubsub`. The correction does not prescribe which side becomes owner. |
| Current focused tests do not originate events from an EventStore gateway. | Inspect `EventIngestionPipelineIntegrationTests.cs` and fixture; search AppHost test resources for `eventstore`. | **Confirmed:** inputs are direct `/events/ingest` and Dapr publish to Memories; no gateway resource is provisioned. |
| Three equivalent sprint actions remain open. | `rg -n -C 4 'AppHost/EventStore|full-stack Redis/FalkorDB|validation-lane blocker' .../sprint-status.yaml` | **Confirmed:** Epic 22, 23, and 24 actions are `open`. |
| The July proposal is pending and unapplied. | Inspect its frontmatter; search target artifacts for `23.7-APPHOST-EVENTSTORE-FULLSTACK` and the proposed claim heading. | **Confirmed:** `status: proposed`, `approval: pending`; proposed ID/wording appears only in that proposal. |

No verifiable claim is inherited solely from a prior story or proposal. The historical 2120-test count in
the existing deferred entry is preserved only as historical provenance and is not used as current proof.

## 7. Historical Context Classification and Slice Guard

| Prior artifact | Classification | Permitted influence |
| --- | --- | --- |
| `sprint-change-proposal-2026-07-21-apphost-eventstore-validation.md` | `historical-reference-only` | Reuse the three-action grouping and the idea of a claim taxonomy. Reject its stale activation premise and re-verify every current claim. |
| Story 21.2 deferred AppHost entry | `historical-reference-only` | Preserve why the gap was first recorded and its original ownership boundary; do not treat its historical test count as current evidence. |
| Existing Story 28.1 definition | `current-narrow-pattern` | Reuse only its exact-identity adoption and zero-code intake constraints as the resolution home. Do not create a new story or copy another story's full shape. |

**Independent slice proof:** this correction is independently demonstrable when the accepted blocker is
registered once, the three duplicate actions are closed by reference to it, and architecture/docs/tests
consistently prevent an EventStore-originating overclaim. Runtime resolution remains a separate,
independently demonstrable Story 28.1 outcome and is intentionally not claimed here.

## 8. Correct-Course Checklist Result

| Checklist section | Result |
| --- | --- |
| Trigger and context | Complete — exact user action, current artifacts, old proposal, current code, and executable evidence reviewed. |
| Epic impact | Complete — Epics 22/23/24 actions and Epic 28 resolution ownership identified; no other epic invalidated. |
| Artifact conflicts | Complete — PRD and UX unchanged; architecture/docs/test summary need claim clarification; no code implementation proposed. |
| Path evaluation | Complete — direct runtime resolution is a separate implementation slice; formal acceptance is the smallest valid correction. |
| Proposal components | Complete — issue, impact, six incremental edits, rationale, verification, and handoff are included. |
| Approval and handoff | Complete — explicitly approved and applied on 2026-08-02; narrow verification passed. |

## 9. Success Criteria

After approved edits are applied:

1. `23.7-APPHOST-EVENTSTORE-FULLSTACK` exists exactly once as an accepted deferred-work record.
2. The Epic 22, 23, and 24 actions are `done` only by resolve-or-accept semantics.
3. Current Memories Aspire Redis/FalkorDB + Dapr evidence remains usable with its exact system boundary.
4. No artifact equates direct Memories intake with EventStore-to-Memories proof.
5. Epic 28 records Story 1.20 activation as satisfied without implying Story 28.1 implementation.
6. Story 28.1 cannot close the blocker without exact identity adoption, an AppHost `eventstore` gateway,
   non-duplicated Dapr component ownership, a real producer path, persisted/searchable outcomes, duplicate
   replay handling, and tenant-isolation negative evidence.
7. Narrow verification passes: target searches, YAML readability, `git diff --check`, package/source
   integration-project builds, and the focused two-test Aspire lane when rerun for implementation handoff.

## 10. Approval Gate and Handoff

**Approval status:** approved by the user on 2026-08-02.

**Implementation status:** complete. All six edits were applied without changing Epic 28 or Story 28.1
from `backlog`. YAML parsing and target diff checks passed; package and source integration-project builds
completed with zero warnings and zero errors; the focused Aspire event-intake lane passed 2/2.

Story 28.1 implementation remains a separate backlog selection and was not authorized by approval of
this proposal.

If any edit is rejected, leave its target unchanged and retain the corresponding action as open unless
another approved artifact supplies the same durable resolve-or-accept boundary.
