---
project: memories
date: 2026-07-21
status: proposed
change_scope: minor
mode: batch
trigger: Epic 23 retrospective action item 3
decision: formally-accept-narrowed-blocker
approval: pending
---

# Sprint Change Proposal — AppHost/EventStore validation claim gate

## 1. Issue Summary

Story 23.7 and the Epic 23 retrospective recorded an AppHost/EventStore.Aspire
validation blocker after the integration-test build failed with `CS0234` for
`Hexalith.EventStore.Aspire` and the available environment could not supply a Docker-backed
Redis Stack/FalkorDB run. The same concern was carried into the Epic 24 retrospective: no
future story may call focused evidence “full-stack Redis/FalkorDB” or “Aspire integration”
proof until the blocker is resolved or formally accepted.

Current-main revalidation narrows the issue:

- The historical compile blocker is resolved. The integration project builds in both default
  package mode (`Hexalith.EventStore.Aspire` 3.79.0) and intentional Debug/source mode against
  the root-declared EventStore submodule, with zero warnings and errors.
- The local Docker, Dapr, and Aspire tools are available, but the Memories AppHost provisions
  Redis Stack, FalkorDB, OpenBao, Memories Server, MCP, and access-telemetry resources only.
  `AddHexalithEventStoreSecurity()` adds the local security resources; it does not provision
  the EventStore command gateway. There is no `AddHexalithEventStoreGatewayProject()` or
  `AddHexalithEventStore(...)` call in the AppHost.
- `EventIngestionPipelineIntegrationTests` proves the Memories event-intake boundary by posting
  directly to `/events/ingest` or publishing to the Memories sidecar's `pubsub` component. It
  can prove persisted Redis/search behavior, but it never originates a command/event through a
  running EventStore gateway and therefore cannot prove EventStore-to-Memories full-stack flow.
- Epic 28 already owns the eventual dependency and runtime adoption. Its activation gate is not
  satisfied: EventStore Story 1.20 remains `blocked`, `final_decision` remains `still blocked`,
  and consumer migration remains unauthorized. Implementing the missing topology now would
  bypass that owner-approval gate.

The correct course is therefore to close the obsolete compile portion and **formally accept the
narrowed EventStore-originating validation blocker**. This acceptance does not make the missing
proof available. It makes the claim boundary explicit, names the consequence and compensating
evidence, and routes reopening to existing Story 28.1.

### Revalidation evidence

| Check | Result | Interpretation |
| --- | --- | --- |
| `dotnet build tests/Hexalith.Memories.IntegrationTests/Hexalith.Memories.IntegrationTests.csproj --no-restore -m:1 /nodeReuse:false -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0` | Passed, 0 warnings / 0 errors | Package-mode AppHost/EventStore.Aspire compile blocker is resolved. |
| Same command with `-p:UseHexalithProjectReferences=true` | Passed, 0 warnings / 0 errors on the serial rerun | Source-mode AppHost/EventStore.Aspire compile blocker is resolved. The first simultaneous package/source attempt was invalidated by both builds writing the same `bin/Debug` outputs; it was not a dependency failure. |
| `docker info`, `dapr --version`, `aspire --version` | Docker 29.4.3; Dapr CLI 1.18.0/runtime 1.18.1; Aspire 13.4.6 | Tool availability is not the remaining blocker. |
| AppHost resource inspection | No `eventstore` project/gateway resource | EventStore-originating topology is absent. |
| EventStore Story 1.20 | `status: blocked`; migration non-authorizing | Story 28.1 must remain backlog. |

## 2. Impact Analysis

### Epic and story impact

- Epic 23 and Story 23.7 remain complete. Their focused implementation evidence stays valid at
  the layer it exercised; neither receives retroactive full-stack credit.
- The Epic 22, Epic 23, and Epic 24 AppHost/EventStore carry-forward actions are three views of
  the same blocker. They can close on the formal-acceptance arm of their recorded success
  criteria once they all point to one accepted-debt record.
- Epic 28 and Story 28.1 remain the resolution home. They stay `backlog`; this correction neither
  activates adoption nor changes the EventStore gitlink, package version, or topology.
- No new epic or story is required. Creating a competing topology story would duplicate Story
  28.1 and weaken its owner-authorization gate.
- Epic priority and execution order are unchanged.

### Artifact conflicts

- **PRD:** No FR, NFR, MVP, or product-scope change. Redis/RediSearch/vector/FalkorDB and
  EventStore requirements remain valid. This is an evidence-classification correction.
- **Architecture:** The three-tier test model does not currently define when “full-stack” may be
  used or distinguish Memories event-intake proof from EventStore-originating proof. Add that
  gate.
- **UX:** No screen, flow, component, interaction, responsive, localization, or accessibility
  impact.
- **Epics:** Add a Story 28.1 close-out criterion that resolves the accepted blocker only after
  owner-authorized dependency adoption and persisted end-to-end evidence pass.
- **Developer documentation:** Clarify the validation claim boundary in
  `docs/dev/eventstore-integration.md` and in the integration-test class summary.
- **Implementation tracking:** Add one structured accepted-debt entry in `deferred-work.md` and
  reconcile the three duplicate retrospective actions in `sprint-status.yaml`.
- **Historical records:** Preserve Story 23.7 and retrospective failure text as point-in-time
  evidence. Do not rewrite history to imply the old compile failure never occurred.

### Technical and operational impact

No runtime, contract, package, submodule, deployment, or data change is authorized by this
proposal. Existing evidence may be described precisely as:

- Memories AppHost Redis/FalkorDB integration proof, when a test starts the Memories topology and
  independently inspects the persisted backing state it claims; or
- Memories Dapr event-intake proof, when a test publishes to the Memories `pubsub` component and
  proves the resulting persisted/searchable state.

It may **not** be described as EventStore-to-Memories full-stack proof, EventStore gateway
integration proof, or owner-approved EventStore runtime adoption until the accepted blocker is
reopened and resolved.

## 3. Recommended Approach

Select **Direct Adjustment — formal acceptance of the narrowed blocker**.

- **Effort:** Less than one day for the documentation and tracking edits after approval.
- **Risk:** Low implementation risk; medium evidence-governance risk if claim wording is ignored.
- **Timeline:** No MVP delay. EventStore-originating integration proof remains unavailable until
  Story 28.1 is activated and completed.
- **Compensating controls:** Keep the clean package/source compile guards; retain focused
  EventStore boundary tests; retain real Memories Redis/FalkorDB persisted-state tests and Dapr
  event-intake tests; require exact proof labels and attached tenant-isolation negative evidence
  for scope-sensitive changes.
- **Consequence accepted:** Releases and story close-outs may rely on the narrower evidence above,
  but cannot claim a running EventStore gateway-to-projection path or use that unproven path as a
  production-readiness assertion.
- **Review:** Reassess by 2026-08-21 even if no earlier trigger fires.

Resolving the topology immediately is not viable because EventStore Story 1.20 has not authorized
the source/package identity that Story 28.1 must adopt. Rollback is not viable because restoring
the historical compile failure adds no safety. PRD/MVP review is not warranted because product
goals and scope remain achievable and unchanged.

## 4. Detailed Change Proposals

### 4.1 Record the accepted blocker once

**Artifact:** `_bmad-output/implementation-artifacts/deferred-work.md`

**Old:** No structured entry owns the duplicated retrospective actions. The older Story 21.2
prose notes that the AppHost does not provision `eventstore`, but it does not provide the complete
formal-acceptance fields or current build evidence.

**New — add:**

```markdown
- **23.7-APPHOST-EVENTSTORE-FULLSTACK - accepted.** The historical
  `Hexalith.EventStore.Aspire` compile failure is resolved in package and source modes, but the
  Memories AppHost does not provision an EventStore command gateway and the EventStore owner has
  not authorized Story 28.1 consumer migration. Existing direct `/events/ingest` and Memories
  Dapr-pub/sub tests do not cross that gateway.

  - ID: 23.7-APPHOST-EVENTSTORE-FULLSTACK
  - Status: accepted
  - Source story: 23-7-index-provisioning-ownership
  - Owner: Amelia (implementation) / Winston (architecture)
  - Approver: Administrator, 2026-07-21
  - Affected scope: Any claim of EventStore-to-Memories full-stack, AppHost/EventStore gateway,
    or owner-approved EventStore runtime integration proof through persisted Redis/RediSearch,
    vector, or FalkorDB projections.
  - Target artifacts: Epic 28 / Story 28.1; `src/Hexalith.Memories.AppHost/Program.cs`;
    `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/`.
  - Consequence: Focused compile, unit, direct event-intake, and Memories Redis/FalkorDB evidence
    remain usable only under those exact labels. They cannot close or support a claim that a
    running EventStore gateway produced the persisted projections.
  - Compensating controls: Package/source integration builds remain green; command-boundary and
    duplicate/idempotency tests remain attached; direct Dapr event-intake tests independently
    inspect persisted/searchable state; scope-sensitive changes retain cross-tenant negative
    evidence or an explicit blocker.
  - Re-open trigger: EventStore Story 1.20 records `final_decision: available` and
    `authorize_consumer_migration: true`; Story 28.1 is selected; an AppHost or deployment adds an
    `eventstore` gateway; or any release/story proposes an EventStore-originating full-stack claim.
  - Resolution gate: Story 28.1 adopts the exact owner-approved source/package identities,
    provisions the supported EventStore gateway topology, and attaches a passing proof that starts
    at the EventStore command/event boundary, reaches Memories through Dapr, persists and searches
    Redis syntactic/vector plus FalkorDB graph state, ignores duplicate replay, and includes
    cross-tenant negative evidence.
  - Review-by: 2026-08-21.
```

### 4.2 Reconcile the three retrospective actions

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Old:**

```yaml
  - epic: 22
    action: "Resolve or explicitly accept the AppHost/EventStore duplicate assembly validation blocker before using full-solution build as ingestion close-out evidence"
    owner: "Amelia, Winston"
    status: open
  - epic: 23
    action: "Resolve or formally accept the AppHost/EventStore.Aspire integration validation blocker before treating full-stack Redis/FalkorDB proof as available"
    owner: "Amelia, Winston"
    status: open
  - epic: 24
    action: "Resolve or formally accept the AppHost/EventStore validation-lane blocker before any future story claims full-stack Redis/FalkorDB or Aspire integration proof"
    owner: "Amelia, Winston"
    status: open
```

**New:** Preserve each action text and owner, set each `status` to `done`, and attach the same dated
comment:

```yaml
    status: done  # 2026-07-21: The historical package/source compile blocker is resolved; the narrower missing EventStore-gateway proof is formally accepted as 23.7-APPHOST-EVENTSTORE-FULLSTACK. This closes the action's resolve-or-accept criterion but does not make EventStore-to-Memories full-stack proof available. Reopen and resolve through Story 28.1 only after EventStore Story 1.20 authorizes migration.
```

### 4.3 Add the architecture evidence taxonomy

**Artifact:** `_bmad-output/planning-artifacts/architecture.md`, `### Test Patterns`

**Old:**

```markdown
**Three-tier structure:**
- **Tier 1:** Unit tests (no external deps) — run on every PR
- **Tier 2:** Integration tests (requires DAPR slim init + Redis/FalkorDB)
- **Tier 3:** Aspire e2e (requires full DAPR init + Docker)
```

**New:** Keep the three tiers and append:

```markdown
**Evidence claim boundary:** A tier identifies infrastructure depth, not the system boundary that
was traversed. Name the proven boundary explicitly.

- **Memories AppHost Redis/FalkorDB proof** starts the Memories topology, enters through the named
  Memories API or Dapr surface, and independently verifies every claimed persisted Redis syntactic/
  vector and FalkorDB graph end-state.
- **EventStore-to-Memories full-stack proof** additionally starts and invokes the owner-approved
  EventStore gateway/runtime, follows its Dapr command/event path into Memories, and proves the
  resulting persisted/searchable Redis and FalkorDB state, duplicate replay behavior, and attached
  cross-tenant negative evidence.
- An AppHost compile, `AddHexalithEventStoreSecurity()`, direct `POST /events/ingest`, or direct
  publish to the Memories `pubsub` component does not prove the EventStore-originating boundary.
- An accepted validation blocker records unavailable proof; it never converts narrower green
  evidence into full-stack proof.
```

### 4.4 Clarify the developer-facing integration contract

**Artifact:** `docs/dev/eventstore-integration.md`

**Old:** Section 1.6 documents the canonical module-to-Memories pub/sub route but does not state how
that evidence differs from a provisioned EventStore gateway proof.

**New — add section 1.7 before section 2:**

```markdown
### 1.7 Validation claim boundary

The cross-module contract above can be validated without running the Hexalith.EventStore command
gateway: a publisher may send a CloudEvent directly through Dapr pub/sub to the Memories sidecar.
That is **Memories Dapr event-intake proof**, not EventStore-to-Memories full-stack proof.

EventStore-to-Memories full-stack proof additionally requires an owner-approved EventStore runtime
provisioned as the `eventstore` AppHost/deployment resource and evidence that follows its command/
event boundary through Dapr to independently inspected Redis syntactic/vector and FalkorDB graph
state. Until `23.7-APPHOST-EVENTSTORE-FULLSTACK` is resolved through Story 28.1, do not use the
full-stack label for direct `/events/ingest`, direct Memories-sidecar pub/sub, compile-only, or
mock-only evidence.
```

### 4.5 Make the test's boundary self-describing

**Artifact:**
`tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`

**Old class summary:**

```csharp
/// <summary>End-to-end event-surface coverage for Story 9.1. Publishes a CloudEvents envelope through
/// <c>POST /events/ingest</c>, waits for the workflow to complete, and proves the resulting memory unit is
/// queryable through the search API with exact <c>subject</c> filtering.</summary>
```

**New class summary:**

```csharp
/// <summary>Memories event-intake coverage for Story 9.1. Publishes a CloudEvents envelope through
/// <c>POST /events/ingest</c> or the Memories Dapr pub/sub sidecar, waits for the workflow to complete,
/// and proves the resulting memory unit is queryable. This fixture does not provision or invoke the
/// EventStore command gateway and is not EventStore-to-Memories full-stack proof.</summary>
```

### 4.6 Bind Story 28.1 to blocker closure

**Artifact:** `_bmad-output/planning-artifacts/epics.md`, Story 28.1

**Old:** Story 28.1 requires owner-approved identity adoption and a real Dapr publish producing a
persisted/searchable result, but does not name the accepted validation blocker it supersedes.

**New — append one acceptance criterion:**

```markdown
**Given** `23.7-APPHOST-EVENTSTORE-FULLSTACK` is accepted and prevents EventStore-originating
full-stack claims,
**When** Story 28.1 completes,
**Then** its unchanged owner-approved identity evidence and gateway-originating persisted-state
proof resolve that entry, reconcile the Epic 22/23/24 validation history without rewriting it, and
become the canonical evidence for any later EventStore-to-Memories full-stack claim.
```

## 5. Implementation Handoff

**Classification:** Minor direct adjustment; no product behavior change.

**Recipients:** Amelia (Developer), Winston (Architect), Murat (Test Architect), and Paige
(Technical Writer).

**Responsibilities after approval:**

- Apply only the six documentation/tracking changes above. Do not change EventStore/Builds gitlinks,
  package versions, AppHost runtime resources, or product code under this acceptance.
- Preserve Story 23.7 and retrospective evidence as historical records.
- Keep Epic 28 / Story 28.1 in backlog while EventStore Story 1.20 is non-authorizing.
- Use the evidence taxonomy in future story, review, release, and operations claims.
- Reopen the accepted entry when any listed trigger fires; resolution belongs to Story 28.1.

**Success criteria:**

- The three retrospective actions point to one structured accepted blocker and are closed on their
  formal-acceptance arm.
- No artifact implies that accepting the blocker makes EventStore-originating full-stack proof
  available.
- Architecture, developer docs, tests, epics, deferred work, and sprint tracking use the same
  boundary and reopen gate.
- Existing narrower Memories Redis/FalkorDB and Dapr event-intake evidence remains usable under
  precise labels.
- EventStore Story 1.20 and Story 28.1 remain the sole authorization and resolution path.

## Workflow Execution Log

| Date | Event | Result |
| --- | --- | --- |
| 2026-07-21 | Trigger confirmed from Story 23.7 and Epic 23/24 retrospectives | Complete |
| 2026-07-21 | PRD, epics, architecture, UX, project context, story/retro evidence, deferred register, and current source reviewed | Complete |
| 2026-07-21 | Package-mode IntegrationTests/AppHost build | Passed, 0 warnings / 0 errors |
| 2026-07-21 | Source-mode IntegrationTests/AppHost build | Passed serially, 0 warnings / 0 errors |
| 2026-07-21 | Current AppHost and EventStore Story 1.20 gates inspected | Gateway absent; owner migration still blocked |
| 2026-07-21 | Direct Adjustment selected | Formal acceptance proposed; full-stack proof remains unavailable |
| 2026-07-21 | Batch Sprint Change Proposal written | Complete; approval pending |

## Checklist Record

### 1. Understand the trigger and context

- [x] 1.1 Trigger identified as Story 23.7 / Epic 23 retrospective action 3, carried into
  Epic 24 and related to the earlier Epic 22 compile blocker.
- [x] 1.2 Core problem classified as a validation-lane and evidence-claim limitation.
- [x] 1.3 Historical errors and current package/source, topology, test-boundary, and owner-gate
  evidence collected.

### 2. Epic impact assessment

- [x] 2.1 Epic 23 remains complete; the blocker concerns evidence breadth, not Story 23.7 behavior.
- [x] 2.2 Epic 28 / Story 28.1 is the existing resolution home.
- [x] 2.3 The complete Epic 0-29 outline and dependencies were reviewed.
- [x] 2.4 No epic is invalidated and no new epic is required.
- [x] 2.5 No priority or sequencing change is required; the Story 28.1 activation gate remains.

### 3. Artifact conflict and impact analysis

- [x] 3.1 PRD reviewed; no FR, NFR, MVP, or scope edit is required.
- [x] 3.2 Architecture reviewed; evidence taxonomy and claim boundary require an amendment.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Epics, developer docs, test descriptions, deferred work, sprint actions, and upstream
  EventStore authorization evidence were reviewed and receive bounded changes.

### 4. Path forward evaluation

- [x] 4.1 Direct Adjustment is viable through formal acceptance of the narrowed blocker.
- [N/A] 4.2 Rollback would restore a compile failure and is not viable.
- [N/A] 4.3 PRD/MVP review is unnecessary because product goals and scope are unchanged.
- [x] 4.4 Formal acceptance selected because immediate topology adoption would violate Story 28.1's
  owner-authorization gate.

### 5. Sprint Change Proposal components

- [x] 5.1 Issue summary completed with historical and current evidence.
- [x] 5.2 Epic, artifact, technical, and operational impacts documented.
- [x] 5.3 Recommended path, alternatives, effort, risk, and timeline documented.
- [x] 5.4 Product impact and precise action plan documented.
- [x] 5.5 Minor-scope multi-role handoff and success criteria documented.

### 6. Final review and handoff

- [x] 6.1 All currently applicable checklist items are addressed.
- [x] 6.2 Proposal checked against current repository evidence.
- [ ] 6.3 Explicit approval from Administrator is pending.
- [ ] 6.4 Deferred-work, sprint-status, architecture, epics, docs, and test-comment edits await
  approval.
- [ ] 6.5 Developer/Architect/Test Architect/Technical Writer handoff awaits approval.
