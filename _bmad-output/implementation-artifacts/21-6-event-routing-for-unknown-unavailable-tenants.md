---
baseline_commit: e64459b
---

# Story 21.6: Event Routing for Unknown/Unavailable Tenants

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a maintainer,
I want events for unknown or unavailable tenants to be retried or dead-lettered,
so that rollout ordering or transient tenant states cannot silently blackhole traffic.

## Acceptance Criteria

1. Given `EventIngestionController.OnEvent` currently returns HTTP 200 for `TenantNotFound` and `TenantDeleting`, when a routed event resolves to a missing tenant, a deleting tenant, or an unavailable tenant status, then Dapr receives a retry-driving response instead of an ACK/drop. The default repository implementation should return HTTP 500 for these outcomes because no dead-letter subscription is configured in-repo. Closes A27.

2. Given Dapr pub/sub treats 2xx responses without explicit `RETRY` as success and retries non-404 non-2xx responses, when Story 21.6 completes, then unit tests prove `TenantNotFound` and `TenantDeleting` outcomes map to retry behavior while intentional permanent drops remain HTTP 200: `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded`.

3. Given `TenantEventRouter` maps `EventStoreTenantStatus.Unavailable` into the same route-resolution path as `Deleting`, when the tenant registry reports `Failed`, `CompensationFailed`, or another non-operational status, then the event is not acknowledged as a successful drop. It must share the retry/dead-letter behavior for unavailable tenants and preserve telemetry/log status visibility.

4. Given Dapr at-least-once delivery can redeliver late or duplicate events after a tenant later becomes active, when the retried event is eventually accepted, then existing duplicate safety is preserved: preflight dedup still reserves/releases correctly, workflow instance IDs remain based on `EventStoreDedupKey.Build(tenantId, caseId, cloudEventId)`, curated search-index events still bypass generic dedup only after successful tenant routing, and Story 21.5 route-cache revalidation is not weakened.

5. Given operator docs currently state `tenant-not-found` and `tenant-deleting` are non-retry drops, when this story completes, then `docs/dev/eventstore-integration.md`, troubleshooting guidance, and any drift-guarded route/deployment docs that mention event-intake semantics are updated to reflect the retry/dead-letter posture without changing the published operation path (`POST /events/ingest`) or the `unknown-source` non-retry contract.

## Tasks / Subtasks

- [x] Task 1 - Re-run the A27 anchor preflight before editing (AC: 1, 2, 3)
  - [x] Confirm `EventIngestionController.OnEvent` still maps `TenantNotFound` and `TenantDeleting` to `Ok(result.Response)`.
  - [x] Confirm `EventIngestionService.MapNonAcceptedResolution` still returns `EventIngestionOutcome.TenantNotFound` and `EventIngestionOutcome.TenantDeleting` with `EventIngestionResponse.Drop(...)`.
  - [x] Confirm `TenantEventRouter.ResolveAsync` still maps `EventStoreTenantStatus.Deleting` and `EventStoreTenantStatus.Unavailable` to `TenantEventRouteResolution.TenantDeleting(...)`.
  - [x] Confirm `TenantStatusAccessorAdapter` still maps `TenantStatus.Failed` / `CompensationFailed` to `EventStoreTenantStatus.Unavailable`.

- [x] Task 2 - Change only the retry posture for unknown/unavailable tenants (AC: 1, 2, 3)
  - [x] Map `EventIngestionOutcome.TenantNotFound` to HTTP 500 in `EventIngestionController.OnEvent`.
  - [x] Map `EventIngestionOutcome.TenantDeleting` to HTTP 500 in `EventIngestionController.OnEvent`; this also covers `Unavailable` statuses because the router currently funnels them through `TenantDeleting`.
  - [x] Keep `EventIngestionOutcome.TenantProvisioning` and `ScheduleFailed` as HTTP 500.
  - [x] Keep `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded` as HTTP 200 intentional drops; these are operator/configuration outcomes, not transient tenant lifecycle states.
  - [x] Do not rename `/events/ingest`, `EventIngestionController.PubSubName`, `MEMORIES_EVENTSTORE_TOPIC`, or public response JSON fields.

- [x] Task 3 - Preserve duplicate, late-event, and curated-event safety (AC: 4)
  - [x] Ensure no preflight dedup reservation is taken before a tenant/case route is accepted.
  - [x] Ensure retrying a previously failed `TenantNotFound` / unavailable event does not create a dedup key until the tenant becomes active and a case route exists.
  - [x] Preserve schedule-failure reservation release behavior: if a retry reaches scheduling and scheduling throws, `IPreflightDedupStore.ReleaseAsync` still runs when the reservation was held.
  - [x] Preserve curated `SearchIndexEntryChanged` / `SearchIndexEntryRemoved` behavior: curated events still require tenant routing first and still bypass case-map and generic workflow scheduling only after an active tenant is resolved.
  - [x] Do not remove or bypass Story 21.5's persisted-map revalidation on `TenantEventRouter` cache hits.

- [x] Task 4 - Update tests for the new Dapr outcome contract (AC: 1, 2, 3, 4)
  - [x] Split `EventIngestionControllerTests.OnEvent_DropOutcomes_Return200` so `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded` remain 200 while `TenantNotFound` and `TenantDeleting` assert 500.
  - [x] Add or update service/router tests so `EventStoreTenantStatus.Unavailable` is explicitly covered and documented as retryable at the controller boundary.
  - [x] Keep tests proving `TenantProvisioning` and route-resolution exceptions remain retryable.
  - [x] Keep tests proving `UnknownSource` does not schedule workflows and still does not retry, per Story 18.8.
  - [x] If a Dapr-sidecar/pubsub integration smoke is available, add a focused assertion that the endpoint response code for tenant-not-found is non-2xx; otherwise record the Docker/sidecar blocker and rely on unit-level HTTP mapping tests.

- [x] Task 5 - Update operator/developer documentation (AC: 5)
  - [x] Update `docs/dev/eventstore-integration.md` section 3 table: `Tenant not found` and `Tenant deleting` should no longer say 200/no retry; describe 500 retry and eventual DLT if operators configure Dapr retry + dead-letter topics.
  - [x] Update troubleshooting section 9 so EventIds 9111 and 9112 tell operators to fix tenant rollout/registry state or inspect Dapr retry/DLT, not to treat the event as dropped.
  - [x] Keep `Unknown source` documented as non-retry drop because publisher source drift cannot be fixed by redelivery.
  - [x] Update alerting recommendations if they still classify 9111/9112 purely as drops.
  - [x] Do not add an in-repo dead-letter topic unless the implementation intentionally chooses the DLT path and includes Dapr subscription/component tests; the minimal approved implementation is HTTP 500 retry.

- [x] Task 6 - Validate and record evidence (AC: 1-5)
  - [x] Run focused EventStore tests, at minimum `EventIngestionControllerTests`, `EventIngestionServiceTests`, and `TenantEventRouterTests`.
  - [x] Run docs/route-surface drift-guard tests if any edited documentation is guarded by tests.
  - [x] Run `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`.
  - [x] If normal `dotnet test` is blocked by the known VSTest TCP-listener sandbox issue, use the in-process xUnit runner fallback and record both commands.
  - [x] Update this story's Dev Agent Record, File List, Completion Notes, and Change Log during implementation.

## Dev Notes

Story 21.6 closes audit finding A27. It is a routing-outcome story, not a new event-ingestion feature, not a multi-topic subscription change, and not a dedup-race fix. The implementation should be intentionally narrow: change the Dapr response posture for tenant lifecycle states that may be caused by rollout ordering or transient infrastructure state, while preserving permanent configuration drops and the existing event-ingestion surface. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.6; _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A27]

### Discovery Results

- Loaded `{epics_content}` from `_bmad-output/planning-artifacts/epics.md`; Epic 21 owns consistency, namespace, deletion, routing, dedup, registry, and migration-safety remediation.
- Loaded `{architecture_content}` from `_bmad-output/planning-artifacts/architecture.md`; relevant constraints are EventStore source-of-truth, Dapr Workflow side effects, at-least-once ingestion, tenant isolation, and no silent failed-unit drops.
- Loaded `{prd_content}` from `_bmad-output/planning-artifacts/prd.md`; relevant requirements are Dapr pub/sub EventStore integration, NFR6 event freshness, NFR19 failed units not silently dropped, and NFR21 CloudEvents compatibility.
- Loaded `{ux_content}` from `_bmad-output/planning-artifacts/ux-design-specification.md`; no module UI work is in scope.
- Loaded persistent facts from `_bmad-output/project-context.md` and Hexalith state instructions. This story should not add new domain persistence; if implementation touches durable tenant/domain state, it must continue through Hexalith.EventStore rather than hand-rolled Dapr state writes.
- Loaded previous Stories 21.1-21.5, current A27 code anchors, current eventstore integration docs, Dapr pub/sub docs, and recent commits through `e64459b`.

### Current State and Code Anchors

`EventIngestionController.OnEvent` translates `EventIngestionProcessResult.Outcome` into HTTP responses for Dapr pub/sub delivery. Today it returns 200 for `UnknownSource`, `TenantNotFound`, `TenantDeleting`, `AutoCreateDisabled`, and `CaseCapExceeded`; 500 only for `TenantProvisioning`, `ScheduleFailed`, and unknown fallback outcomes. This is the direct A27 defect for tenant-not-found and deleting/unavailable states. [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs]

`EventIngestionOutcome` documents the old semantics: `TenantNotFound` is "Drop with 200" and `TenantDeleting` is "Drop with 200." Update the comments so future developers do not reintroduce the ACK/drop behavior after the controller mapping is changed. [Source: src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs]

`EventIngestionService.MapNonAcceptedResolution` is the typed route-outcome mapper. It logs `TenantNotFound` with EventId 9112 and `TenantDeleting` with EventId 9111, and returns `EventIngestionResponse.Drop(...)` with no instance ID. It does not schedule workflows or touch dedup for these branches. That no-schedule/no-dedup property should remain; only the controller HTTP mapping needs to drive retry. [Source: src/Hexalith.Memories.EventStore/EventIngestionService.cs; src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs]

`TenantEventRouter.ResolveAsync` performs longest-prefix source routing, tenant-status lookup, curated-event short-circuiting, case-map/cache resolution, optional case auto-creation, and route acceptance. It maps missing registry rows to `TenantNotFound`, `Provisioning` to `TenantProvisioning`, and `Deleting` plus `Unavailable` to `TenantDeleting`. [Source: src/Hexalith.Memories.EventStore/TenantEventRouter.cs]

`TenantStatusAccessorAdapter` maps server `TenantStatus.Failed` and `TenantStatus.CompensationFailed` to `EventStoreTenantStatus.Unavailable`; the router currently funnels that to `TenantDeleting`. Because there is no separate `TenantUnavailable` outcome today, the simplest compliant fix is making `TenantDeleting` retryable at the controller boundary and updating docs/comments to say "deleting/unavailable." [Source: src/Hexalith.Memories.Server/EventStoreIntegration/TenantStatusAccessorAdapter.cs; src/Hexalith.Memories.EventStore/ITenantStatusAccessor.cs]

`docs/dev/eventstore-integration.md` currently states that `Tenant not found` and `Tenant deleting` return 200/no retry, and troubleshooting tells operators these events are drops. That documentation must change with the behavior. It also states no dead-letter configuration exists in-repo, so the story should not pretend a DLT path exists unless implementation adds and tests one. [Source: docs/dev/eventstore-integration.md#At-least-once-dead-letter-replay-semantics; docs/dev/eventstore-integration.md#Known-limitations]

`deploy/dapr/components/pubsub.yaml` configures a Redis pub/sub component named `pubsub` only. There is no dead-letter topic, retry resiliency, or subscription YAML in this repository. Therefore the default implementation path is HTTP 500 retry and operator docs for optional DLT configuration. [Source: deploy/dapr/components/pubsub.yaml]

### Architecture Constraints

- Dapr pub/sub delivery is at-least-once. Dapr treats HTTP 2xx plus empty payload or `SUCCESS` as success, explicit `RETRY` as retry, `DROP` as drop, 404 as drop, and other non-2xx responses as retry. This story should use the existing controller HTTP-status mapping rather than invent a new transport. [Source: https://v1-18.docs.dapr.io/reference/api/pubsub_api/]
- Dapr supports dead-letter topics when configured with retry policy and `deadLetterTopic`; this repository currently has no DLT config. Do not add untested DLT semantics in code comments. If the implementation chooses DLT instead of 500, it must include subscription/component configuration and tests. [Source: https://v1-18.docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/]
- Preserve Story 18.8's cross-module event-intake contract: modules publish CloudEvents to Dapr, `/dapr/subscribe` discovers the topic, and Dapr delivers to `POST /events/ingest`. Unknown source remains a non-retry drop so source-prefix drift is diagnosed instead of redelivered forever. [Source: docs/dev/eventstore-integration.md#Route-surface-for-Hexalith-modules; _bmad-output/implementation-artifacts/18-8-cross-module-dapr-event-intake-contract-and-verification.md]
- Preserve Story 20 security posture: `/events/ingest` remains an explicit Dapr infrastructure anonymous route; do not broaden anonymous access to `/api/**`. [Source: _bmad-output/planning-artifacts/architecture.md#Security-Architecture; src/Hexalith.Memories.EventStore/EventIngestionController.cs]
- Preserve Story 21.5 route-cache safety: cached routes must still revalidate against `IAggregateCaseMappingStore` before acceptance, and case deletion invalidation must remain intact. [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md]
- Do not solve Story 21.7 in this story. `SaveDedupKeyActivity` race-safety and duplicate workflow-instance handling remain dedicated follow-up scope. [Source: _bmad-output/planning-artifacts/epics.md#Story-21.7]

### Previous Story Intelligence

Story 21.1 ratified EventStore aggregates as the source of truth with Redis/FalkorDB as projections. 21.6 should not introduce new authoritative state or bypass the EventStore command boundary. [Source: _bmad-output/implementation-artifacts/21-1-consistency-model-decision.md]

Story 21.2 moved domain mutations through EventStore command acceptance and projection workflows. 21.6 is upstream of workflow scheduling for rejected tenant states; it should not schedule a workflow until routing resolves an active tenant and case. [Source: _bmad-output/implementation-artifacts/21-2-transactional-multi-backend-mutation.md]

Story 21.5 added aggregate-case-map deletion plus `TenantEventRouter` cache invalidation/revalidation. 21.6 must preserve those tests and avoid converting stale cached routes into accepted routes during retries. [Source: _bmad-output/implementation-artifacts/21-5-deletion-completeness.md]

Epic 20 handoff says Epic 21 data-path changes must preserve security regression guards and treat remediation documentation as a close-out gate. Include docs updates and focused route-outcome tests before marking the story done. [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#action_items]

### Git Intelligence

Recent commits:

- `e64459b chore(story-automator): record story 21.5 completion`
- `c4df92b feat(story-21.5): Deletion Completeness`
- `b0ff9bf feat(story-21.4): Key-Schema Single Source of Truth`
- `1b072f4 feat(story-21.3): Natural-Language Vector Namespace Separation`
- `95048df feat: Implement natural-language vector namespace separation`

The Epic 21 pattern is narrow audit remediation with explicit code anchors, focused tests, and story/doc hygiene. Continue that pattern; do not turn 21.6 into a Dapr topology redesign.

### Latest Technical Notes

- Repo-pinned Dapr packages remain at `1.18.4`. Official Dapr docs identify v1.18 as the latest stable docs line on 2026-07-04, with v1.19 preview visible but not in scope. Do not upgrade Dapr packages in this story. [Source: _bmad-output/project-context.md; https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/]
- Dapr pub/sub application responses support explicit `SUCCESS`, `RETRY`, and `DROP` statuses, but the current controller already uses HTTP status to express retry/drop. Keeping that pattern is lower risk than adding a Dapr-specific response payload model. [Source: https://v1-18.docs.dapr.io/reference/api/pubsub_api/]
- Dapr dead-letter topics are subscription configuration, not an automatic behavior from this codebase. If operators configure retry plus DLT, the 500 path can eventually flow to DLT according to Dapr policy; without that config, Dapr retries according to the broker/resiliency setup. [Source: https://v1-18.docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/]

### Scope Boundaries

- In scope: controller outcome mapping, XML comments/status docs, EventStore unit tests, and eventstore integration docs for tenant-not-found/deleting/unavailable retry posture.
- In scope: clarifying `TenantDeleting` naming/comments if needed so it explicitly covers unavailable tenant states.
- In scope: adding a `TenantUnavailable` outcome only if it materially improves clarity without forcing broad contract churn; the minimal route is to keep existing status names and update HTTP mapping/docs.
- Out of scope: multi-topic subscription, Dapr component ACL redesign, publisher authentication, dead-letter subscription infrastructure unless explicitly chosen and tested, dedup TOCTOU/duplicate workflow instance fixes, tenant registry CAS/rollback integrity, migration marker work, public route renames, and `/api/*` auth changes.
- Out of scope: changing response JSON shape for accepted/duplicate/drop responses unless tests prove an additive field is required.

### Testing Standards

- Use xUnit v3, Shouldly, and NSubstitute. [Source: _bmad-output/project-context.md#Testing-Rules]
- Keep focused tests Docker-free where possible; the core acceptance is controller HTTP mapping and service/router typed outcomes.
- Test Dapr behavior through HTTP status mapping, not by mocking Dapr internals.
- Preserve existing tests for `UnknownSource`, preflight dedup reservation/release, curated search-index event bypass, and route cache revalidation.
- Run focused EventStore tests first, then full solution build. Use the in-process xUnit fallback if normal VSTest is blocked by the known sandbox listener issue.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-21.6 - story statement and acceptance criteria]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-04.md#Epic-21 - approved A27 remediation scope]
- [Source: _bmad-output/planning-artifacts/research/architecture-audit-2026-07-04.md#A27 - ACK/drop finding]
- [Source: _bmad-output/planning-artifacts/architecture.md#Multi-Backend-Consistency - EventStore source-of-truth and Dapr workflow projection model]
- [Source: _bmad-output/project-context.md - repo-wide Dapr, tenant isolation, testing, package, and submodule rules]
- [Source: references/Hexalith.AI.Tools/hexalith-state-instructions.md - persistence rules if durable state is touched]
- [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs - current HTTP outcome mapping]
- [Source: src/Hexalith.Memories.EventStore/EventIngestionService.cs - typed route-outcome mapping and dedup boundary]
- [Source: src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs - current retry/drop comments]
- [Source: src/Hexalith.Memories.EventStore/TenantEventRouter.cs - tenant status routing]
- [Source: src/Hexalith.Memories.EventStore/ITenantStatusAccessor.cs - `Unavailable` status]
- [Source: src/Hexalith.Memories.Server/EventStoreIntegration/TenantStatusAccessorAdapter.cs - server tenant status mapping]
- [Source: src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs - EventIds 9111/9112]
- [Source: tests/Hexalith.Memories.EventStore.Tests/EventIngestionControllerTests.cs - current 200 drop tests]
- [Source: tests/Hexalith.Memories.EventStore.Tests/EventIngestionServiceTests.cs - service route-outcome tests]
- [Source: tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs - router status and cache tests]
- [Source: docs/dev/eventstore-integration.md - current operator semantics to update]
- [Source: deploy/dapr/components/pubsub.yaml - no in-repo DLT configuration]
- [Source: https://v1-18.docs.dapr.io/reference/api/pubsub_api/ - Dapr pub/sub response status and HTTP retry behavior]
- [Source: https://v1-18.docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-deadletter/ - Dapr dead-letter topic configuration]

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-07-04: create-story workflow loaded local BMAD skill, discovery protocol, template, checklist, BMAD config, sprint status, planning artifacts, project-context facts, Hexalith LLM/state instructions, previous Story 21.5, A27 audit anchor, current code anchors, existing eventstore docs, official Dapr pub/sub docs, and recent commits.
- 2026-07-04: story target came from user request `21.6`; sprint status had `21-6-event-routing-for-unknown-unavailable-tenants: backlog` and `epic-21: in-progress`.
- 2026-07-04: no module UI work detected; UX context was discovered but not needed for implementation scope.
- 2026-07-04: checklist validation applied after creation; no blocking gaps remained after selecting the repository's minimal HTTP 500 retry path over unconfigured DLT infrastructure.
- 2026-07-04: dev-story activation resolved customization with no prepend/append steps and persistent `project-context.md` facts; loaded root plus referenced project contexts, sprint status, and full story.
- 2026-07-04: A27 anchor preflight confirmed controller 200 mappings, service drop outcomes, router `Unavailable` -> `TenantDeleting`, and server adapter failed-status -> `Unavailable` mappings before edits.
- 2026-07-04: `dotnet test tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --filter "FullyQualifiedName~EventIngestionControllerTests|FullyQualifiedName~EventIngestionServiceTests|FullyQualifiedName~TenantEventRouterTests"` exited during restore/test setup without actionable diagnostics in this sandbox; used the documented xUnit v3 in-process fallback.
- 2026-07-04: Built focused EventStore tests with `dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --no-restore -m:1 /nodeReuse:false -v:diag`; build succeeded with 0 warnings / 0 errors.
- 2026-07-04: Ran `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.EventIngestionControllerTests -class Hexalith.Memories.EventStore.Tests.EventIngestionServiceTests -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests`; 49 passed, 0 failed.
- 2026-07-04: Built server tests with `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore -m:1 /nodeReuse:false`; build succeeded with 0 warnings / 0 errors.
- 2026-07-04: Ran `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionOutcomeTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.DocumentationCompletenessTests`; 9 passed, 0 failed.
- 2026-07-04: Ran `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`; build succeeded with 0 warnings / 0 errors.
- 2026-07-04: No runnable Dapr-sidecar/pubsub smoke was available in this sandbox; existing `18.8-DAPR-SMOKE` remains accepted infra-lane debt, so Story 21.6 relies on unit/in-process HTTP mapping tests for the non-2xx tenant-not-found proof.
- 2026-07-04: Senior review loaded the story-automator review workflow, checklist, project context, architecture/state rules, and official Dapr pub/sub/dead-letter docs; MCP resources were unavailable, so official Dapr web docs were used as fallback references.
- 2026-07-04: Senior review `dotnet test tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj --filter "FullyQualifiedName~EventIngestionControllerTests|FullyQualifiedName~EventIngestionServiceTests|FullyQualifiedName~TenantEventRouterTests" --no-restore -m:1 /nodeReuse:false` hit the known VSTest TCP listener sandbox error (`SocketException: Permission denied`).
- 2026-07-04: Senior review fallback `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll -class Hexalith.Memories.EventStore.Tests.EventIngestionControllerTests -class Hexalith.Memories.EventStore.Tests.EventIngestionServiceTests -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests`; 49 passed, 0 failed.
- 2026-07-04: Senior review built server tests with `dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore -m:1 /nodeReuse:false`; build succeeded with 0 warnings / 0 errors.
- 2026-07-04: Senior review ran `DiffEngine_Disabled=true dotnet exec tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll -class Hexalith.Memories.Server.Tests.EventStoreIntegration.EventIngestionOutcomeTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.DocumentationCompletenessTests -class Hexalith.Memories.Server.Tests.EventStoreIntegration.CrossModuleEventIntakeE2ETests`; 14 passed, 0 failed.
- 2026-07-04: Senior review ran `dotnet build Hexalith.Memories.slnx -m:1 /nodeReuse:false --no-restore`; build succeeded with 0 warnings / 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 21.6 created as the A27 implementation story after Story 21.5 completed.
- The story selects HTTP 500 retry as the default implementation path because repository Dapr component configuration has no dead-letter topic/subscription today.
- The story explicitly preserves unknown-source non-retry behavior, preflight/permanent dedup boundaries, curated search-index routing behavior, and Story 21.5 route-cache revalidation.
- Implemented the narrow HTTP posture change: `TenantNotFound` and `TenantDeleting` now return HTTP 500 from `EventIngestionController.OnEvent`, while `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded` remain HTTP 200 intentional drops.
- Kept service/router route-resolution behavior intact: tenant lifecycle failures still return typed drop response bodies without scheduling workflows or reserving preflight dedup keys, and unavailable tenant statuses continue to funnel through `TenantDeleting`.
- Updated logs, XML comments, and operator docs to describe tenant-not-found/deleting/unavailable outcomes as retry/DLT-capable lifecycle rejections rather than successful drops.
- Added focused unit/in-process HTTP coverage for tenant-not-found/deleting 500 responses, permanent-drop 200 responses, unavailable tenant routing, no-dedup/no-scheduling before accepted routes, and the guarded eventstore integration documentation.

### File List

- `_bmad-output/implementation-artifacts/21-6-event-routing-for-unknown-unavailable-tenants.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/dev/eventstore-integration.md`
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs`
- `tests/Hexalith.Memories.EventStore.Tests/EventIngestionControllerTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/EventIngestionServiceTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionOutcomeTests.cs`

### Senior Developer Review (AI)

Reviewer: Codex GPT-5 on 2026-07-04

Outcome: Approved after automatic fixes. No critical issues remain.

Findings fixed:

- [MEDIUM] Story File List omitted a changed source test file, `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs`, even though git shows Story 21.6 added HTTP-pipeline coverage for tenant-not-found and tenant-deleting/unavailable retry behavior. Added the file to the File List.
- [LOW] Several edited files had mixed line terminators after implementation edits. Normalized edited source, test, doc, and story artifacts to consistent LF so the repository git whitespace gate stays clean.

Review notes:

- Acceptance Criteria 1-3 are implemented at the HTTP boundary: `TenantNotFound` and `TenantDeleting` return HTTP 500, while `UnknownSource`, `AutoCreateDisabled`, and `CaseCapExceeded` remain HTTP 200.
- Acceptance Criterion 4 is preserved: service/router flow still performs tenant routing before dedup reservation, workflow instance IDs remain based on `EventStoreDedupKey.Build(tenantId, caseId, cloudEventId)`, curated search-index events still require accepted tenant routing before bypassing generic dedup, and Story 21.5 cache revalidation remains in place.
- Acceptance Criterion 5 is covered in `docs/dev/eventstore-integration.md`; no in-repo DLT config was added.
- Git also shows `_bmad-output` automation artifacts and the `references/Hexalith.EventStore` submodule pointer as modified. These were observed but excluded from the application source review surface; the submodule worktree is clean.

### Change Log

- 2026-07-04: Created story context for event routing retry/dead-letter behavior for unknown/unavailable tenants, covering A27 HTTP outcome mapping, Dapr retry semantics, duplicate safety, documentation updates, and focused validation.
- 2026-07-04: Implemented Story 21.6. Changed tenant-not-found/deleting-unavailable controller responses to HTTP 500 retry, preserved permanent drop contracts and dedup/curated/cache safety, updated docs/log comments, added focused tests, and validated with 58 focused tests plus full solution build.
- 2026-07-04: Senior developer review approved Story 21.6 after auto-fixing File List coverage for the changed E2E test and normalizing edited artifacts to consistent line endings.
