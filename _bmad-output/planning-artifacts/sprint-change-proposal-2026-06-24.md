---
project: Hexalith.Memories
date: 2026-06-24
workflow: bmad-correct-course
change_trigger: Memories Server should use a Dapr sidecar to manage events from other Hexalith modules.
mode: Batch
status: approved
approved_by: Jerome
approved_at: 2026-06-24T14:34:08+02:00
---

# Sprint Change Proposal - Dapr Sidecar Event Intake for Hexalith Modules

## 1. Issue Summary

The requested course correction is that the Memories Server should be the Dapr sidecar-managed event subscriber for events emitted by other Hexalith modules. Other modules should publish CloudEvents to the configured Dapr pub/sub component and topic; the Memories Server sidecar should discover the subscription and deliver those events to the server's `/events/ingest` endpoint.

The current code already has most of this behavior:

- `src/Hexalith.Memories.Server/Program.cs` registers Dapr client, workflow, actors, `UseCloudEvents()`, `MapControllers()`, and `MapSubscribeHandler()`.
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs` exposes `POST /events/ingest` with environment-backed Dapr topic metadata from `MEMORIES_EVENTSTORE_TOPIC`.
- `src/Hexalith.Memories.EventStore/EventIngestionService.cs` parses CloudEvents, routes by `source` prefix to tenants, performs preflight dedup, and schedules `IngestionWorkflow`.
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` attaches a Dapr sidecar and `pubsub` component to a consuming AppHost topology.
- `docs/dev/eventstore-integration.md` documents one topic per deployment and the `/events/ingest` Dapr subscription surface.

The gap is artifact and backlog clarity: the sprint artifacts do not state strongly enough that cross-Hexalith-module event ingestion is server-owned, sidecar-managed, and verified as a downstream integration contract. Epic 9 is already marked done, so this should be handled as Epic 18 operational/integration hardening rather than reopening Epic 9 implementation scope.

## 2. Impact Analysis

### Epic Impact

- Epic 9 remains done. It already delivered the core Dapr pub/sub subscription and EventStore integration surface.
- Epic 18 should absorb the new hardening requirement because it is the active backlog area for downstream consumer integration contracts.
- Epic 17 UI work is not directly affected. Operator/event diagnostics already flow through handler registry, mismatch reports, and future Operator Console patterns.

### Story Impact

- Story 9.1 needs a completion/amendment note, not implementation reopening: the server-side Dapr subscription is the canonical path for Hexalith module event intake.
- Story 18.2 should include the module-event configuration contract: shared pub/sub component, topic, source-prefix routing, ports, and required environment variables.
- Story 18.3 should include the Dapr operation surface: `/dapr/subscribe` reports `pubsubname=pubsub`, configured topic, and route `/events/ingest`; no `/process` operation exists.
- Add Story 18.8 for concrete verification of cross-module Dapr event intake.

### Artifact Conflicts

- PRD: no MVP scope change. The existing Phase 1.5 EventStore integration promise remains valid, but the wording should generalize from EventStore-only to Hexalith module CloudEvents where appropriate.
- Architecture: add an explicit cross-module event flow: Hexalith module -> Dapr pub/sub -> Memories Server sidecar -> `/events/ingest` -> `IngestionWorkflow`.
- UX: no immediate artifact change. The existing handler list, mismatch detection, trust-state, and Operator Console guidance cover the user-facing diagnostics.
- Docs: update `docs/dev/eventstore-integration.md` and Epic 18 docs stories to make the module event contract discoverable.

### Technical Impact

- No new persistence mechanism. Domain state remains persisted through existing ingestion workflows and Hexalith.EventStore-aligned event sourcing rules.
- No direct REST push requirement for other modules. REST remains for external ingestion/search surfaces; cross-module event intake uses Dapr pub/sub.
- No multi-topic implementation is required now. Current limitation remains one topic per deployment; modules can publish to the shared configured topic, or operators can run separate Memories deployments per topic until multi-topic routing is approved.
- Verification should cover subscription discovery, route shape, topic/source mapping, duplicate-safe behavior, and at least two synthetic Hexalith module sources.

## 3. Recommended Approach

Use Direct Adjustment with moderate scope.

Rationale:

- The core implementation exists and aligns with PRD FR59-FR62, NFR6, and NFR21.
- The risk is consumer drift: downstream modules may bypass the sidecar path, publish to undocumented topics, or configure Dapr ACLs against the wrong operation path.
- The least disruptive fix is to add an Epic 18 hardening story and amend the relevant completed planning/story text. Do not reopen Epic 9 or replan the MVP.

Effort estimate: 2-4 working days.

Risk level: Medium. The code change may be small, but the blast radius includes deployment docs, Dapr ACL policy authors, downstream AppHosts, and integration tests.

## 4. Detailed Change Proposals

### PRD Amendment

Section: `Phase 1.5 - Fast-Follow`, EventStore Integration row.

OLD:

```markdown
EventStore Integration (DAPR pub/sub, auto-discovery, dual embedding, causal chains)
```

NEW:

```markdown
EventStore / Hexalith Module Event Integration (Dapr pub/sub through the Memories Server sidecar, CloudEvents auto-discovery, dual embedding, causal chains)

The Memories Server is the sidecar-managed event subscriber. Hexalith modules publish CloudEvents to the configured Dapr pub/sub topic; the server sidecar delivers them to `/events/ingest`, where source-prefix routing maps events to tenant/case memory. Modules should not bypass this path with direct REST pushes for event streams.
```

Justification: This preserves the zero-code EventStore promise while making the broader Hexalith-module intake model explicit.

### Architecture Amendment

Section: `Data Flow`.

OLD:

```markdown
Ingest: CLI/MCP -> Controller -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)
```

NEW:

```markdown
Event ingest: Hexalith module -> Dapr pub/sub component -> Memories Server Dapr sidecar -> POST /events/ingest -> EventIngestionService -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)

Content ingest: CLI/MCP/REST -> Controller -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)
```

Justification: Separates event-stream ingestion from user/content ingestion and makes sidecar ownership explicit.

### Epic 9 Amendment

Story: `9.1 Event Auto-Discovery & DAPR Pub/Sub Subscription`.

Section: Completion / Scope Clarification.

OLD:

```markdown
MVP subscribes to one topic per deployment (configurable). A consumer needing N topics runs N deployments today - multi-topic routing is a future refinement.
```

NEW:

```markdown
MVP subscribes to one topic per deployment (configurable). For Hexalith module integration, modules publish CloudEvents to the configured Dapr pub/sub topic and set stable `source` prefixes so `SourceToTenantMap` can route them. A consumer needing N independent topics runs N Memories deployments today - multi-topic routing is a future refinement.

The Memories Server Dapr sidecar is the event-subscription owner. Other Hexalith modules should not call Memories REST ingestion directly for domain event streams; they publish to Dapr pub/sub and let the Memories sidecar deliver to `/events/ingest`.
```

Justification: Avoids reopening done code while recording the intended integration boundary.

### Epic 18 Amendment

Add a new story.

```markdown
### Story 18.8: Cross-Module Dapr Event Intake Contract and Verification

As an operator integrating Hexalith modules with Memories,
I want the Memories Server Dapr sidecar to be the documented and tested subscriber for module CloudEvents,
So that Tenants, Parties, and future Hexalith modules can publish events without direct REST coupling or per-module ingestion code.

Acceptance Criteria:

1. Given a downstream Hexalith module publishes a CloudEvent to the configured Dapr `pubsub` component and `MEMORIES_EVENTSTORE_TOPIC`, when the Memories Server sidecar discovers subscriptions, then `/dapr/subscribe` exposes `pubsubname=pubsub`, the configured topic, and route `/events/ingest`.
2. Given two module source prefixes, for example `hexalith/tenants` and `hexalith/parties`, when events are published on the shared topic, then `SourceToTenantMap` routes each source prefix to the configured tenant without direct REST ingestion calls.
3. Given an operator authors Dapr access-control policy, when they inspect the published operation surface, then the documented allowed operation is `POST /events/ingest` through pub/sub delivery and the docs explicitly state that `/process` is not part of the Memories event-ingest surface.
4. Given the same CloudEvent is delivered more than once by Dapr, when the event reaches Memories, then existing preflight and workflow idempotency produce one memory unit and duplicate deliveries do not create additional units.
5. Given a module publishes to an unknown source prefix, when the event reaches Memories, then the endpoint returns the existing non-retry drop outcome and handler mismatch/unknown-source diagnostics identify the missing route.
6. Given the current one-topic-per-deployment limitation, when docs are updated, then they explain the supported shared-topic pattern and the separate-deployment workaround for independent topics; multi-topic routing remains deferred.
7. Given this story completes, when focused validation runs, then tests or documented smoke evidence prove sidecar subscription discovery, source-prefix routing for at least two synthetic Hexalith modules, and duplicate-safe delivery.

Target artifacts:

- `docs/dev/eventstore-integration.md`
- `docs/operations/*` deployment or route-surface docs
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` if consumer AppHost guidance needs stronger defaults
- `tests/Hexalith.Memories.*` focused tests for subscription discovery/routing where practical
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Out of scope:

- Multi-topic routing in a single Memories deployment
- Direct REST-based module event ingestion
- Mutating Hexalith.EventStore, Hexalith.Tenants, or other submodules
- New persistence mechanisms outside the existing Memories ingestion workflow
```

Justification: Epic 18 is backlog and already owns downstream integration contracts. This story closes the cross-module sidecar contract without disturbing completed Epic 9 work.

### Story 18.2 Amendment

Section: Acceptance Criteria.

ADD:

```markdown
Given Hexalith modules publish events through Dapr pub/sub,
When the deployment contract is published,
Then it documents the shared pub/sub component name (`pubsub`), the required `MEMORIES_EVENTSTORE_TOPIC`, the source-prefix routing map (`EventStoreIntegration:Routing:SourceToTenantMap`), and the Memories Server sidecar ports used for subscription discovery and internal delivery.
```

### Story 18.3 Amendment

Section: Acceptance Criteria.

ADD:

```markdown
Given the Memories Server sidecar manages event delivery,
When the route surface is published,
Then it includes the Dapr subscription discovery contract (`/dapr/subscribe`) and the pub/sub delivery route (`POST /events/ingest`), and it states that domain modules publish CloudEvents to Dapr rather than invoking Memories REST ingestion for event streams.
```

## 5. Change Analysis Checklist

- [x] 1.1 Triggering story identified: completed Epic 9 / Story 9.1 plus Epic 18 downstream integration backlog.
- [x] 1.2 Core problem defined: integration contract ambiguity, not absence of the Dapr sidecar implementation.
- [x] 1.3 Evidence gathered: current code and docs show sidecar, `/events/ingest`, source routing, and one-topic limitation.
- [x] 2.1 Current epic can still stand: Epic 9 remains done.
- [x] 2.2 Epic-level change: add Epic 18 Story 18.8 and amend Stories 18.2/18.3.
- [x] 2.3 Remaining epics reviewed: Epic 17 not directly affected; Epic 18 affected.
- [x] 2.4 No epic invalidation: no planned epic becomes obsolete.
- [x] 2.5 Priority: implement before downstream modules depend on undocumented routes or ACL placeholders.
- [x] 3.1 PRD conflict checked: no MVP conflict; Phase 1.5 wording should clarify Hexalith module event intake.
- [x] 3.2 Architecture conflict checked: add explicit event-ingest data flow.
- [N/A] 3.3 UI/UX conflict checked: no immediate UI change.
- [x] 3.4 Secondary artifacts checked: docs, Dapr component config, ACL docs, tests, sprint status.
- [x] 4.1 Direct Adjustment viable: recommended.
- [N/A] 4.2 Rollback not viable: no completed code needs rollback.
- [N/A] 4.3 MVP review not required: no scope reduction.
- [x] 4.4 Recommended path selected: Direct Adjustment, moderate scope.
- [x] 5.1-5.5 Proposal components included.
- [x] 6.3 User approval received from Jerome on 2026-06-24.
- [x] 6.4 Sprint status updated with Story 18.8 backlog entry.

## 6. Implementation Handoff

Scope classification: Moderate.

Route to: Product Owner / Developer agents.

Responsibilities:

- Product Owner: approve the proposal, add Story 18.8 to `epics.md`, and update `sprint-status.yaml` with backlog status.
- Developer: implement docs/tests and any small AppHost/Aspire helper changes required by Story 18.8.
- Architect, if needed: review any proposed multi-topic extension. Multi-topic routing is not part of this proposal.

Success Criteria:

- Planning artifacts clearly state that Memories Server sidecar owns cross-module event subscription.
- Docs publish the exact Dapr pub/sub topic, source-prefix routing, `/dapr/subscribe`, and `/events/ingest` contract.
- Focused validation proves at least two Hexalith module source prefixes can be routed through Dapr pub/sub without REST coupling.
- Duplicate and unknown-source behaviors remain aligned with existing Story 9.1 idempotency and diagnostics.

## 7. Approval

Decision: approved by Jerome on 2026-06-24.

Approved Direct Adjustment: add Story 18.8 to Epic 18, amend Story 18.2 and Story 18.3 acceptance criteria, clarify PRD/architecture event-intake wording, and update sprint status with the new backlog story.
