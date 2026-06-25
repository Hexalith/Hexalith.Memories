---
baseline_commit: 377df3f
---
# Story 18.8: Cross-Module Dapr Event Intake Contract and Verification

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

| Field | Value |
| :---- | :---- |
| Epic | 18 - Downstream Consumer Integration Contract Hardening |
| Story key | `18-8-cross-module-dapr-event-intake-contract-and-verification` |
| Origin | Sprint Change Proposal 2026-06-24 - Dapr sidecar event intake for Hexalith modules |
| Lifecycle track | Engineering / Operational Readiness - Downstream Consumer Integration Hardening. **Not MVP-counted.** |
| Release impact | **None expected.** Contract docs + focused tests/smoke evidence over an existing sidecar-managed path. Use `docs:` / `test:` commits unless preflight proves a genuine implementation gap. No new public client API, no package-version change, no `tools/release-packages.json` edit, and no direct submodule mutation. |
| Deliverable | A published, drift-guarded cross-module event-intake contract proving that Hexalith modules publish CloudEvents to Dapr `pubsub` + `MEMORIES_EVENTSTORE_TOPIC`, the Memories Server sidecar discovers `/events/ingest` via `/dapr/subscribe`, `SourceToTenantMap` routes at least `hexalith/tenants` and `hexalith/parties`, duplicate deliveries stay idempotent, unknown sources drop without retry, `/process` remains absent, and the one-topic-per-deployment limitation is explicit. |
| Coupling | Builds on Stories 18.2 and 18.3 docs/guards plus Story 9.1/9.2 event-ingest implementation. Does **not** reopen Epic 9 and does **not** implement multi-topic routing. |
| Parties-side follow-up | Parties and future Hexalith modules publish domain event streams through Dapr pub/sub and update ACL/runbook references away from `/process` or REST ingestion. |

## Story

As an operator integrating Hexalith modules with Memories,
I want the Memories Server Dapr sidecar to be the documented and tested subscriber for module CloudEvents,
so that Tenants, Parties, and future Hexalith modules can publish events without direct REST coupling or per-module ingestion code.

## Acceptance Criteria

1. **Sidecar subscription discovery is proven.** Given a downstream Hexalith module publishes a CloudEvent to the configured Dapr `pubsub` component and `MEMORIES_EVENTSTORE_TOPIC`, when the Memories Server sidecar discovers subscriptions, then `/dapr/subscribe` exposes `pubsubname=pubsub`, the configured topic, and route `/events/ingest`.

2. **Two Hexalith module source prefixes route through the shared topic.** Given two module source prefixes, for example `hexalith/tenants` and `hexalith/parties`, when events are published on the shared topic, then `SourceToTenantMap` routes each source prefix to the configured tenant without direct REST ingestion calls.

3. **ACL operation surface rejects `/process`.** Given an operator authors Dapr access-control policy, when they inspect the published operation surface, then the documented allowed operation is `POST /events/ingest` through pub/sub delivery and the docs explicitly state that `/process` is not part of the Memories event-ingest surface.

4. **Duplicate Dapr deliveries are idempotent.** Given the same CloudEvent is delivered more than once by Dapr, when the event reaches Memories, then existing preflight and workflow idempotency produce one memory unit and duplicate deliveries do not create additional units.

5. **Unknown source prefix drops without retry and is diagnosable.** Given a module publishes to an unknown source prefix, when the event reaches Memories, then the endpoint returns the existing non-retry drop outcome and handler mismatch/unknown-source diagnostics identify the missing route.

6. **One-topic limitation and workaround are published.** Given the current one-topic-per-deployment limitation, when docs are updated, then they explain the supported shared-topic pattern and the separate-deployment workaround for independent topics; multi-topic routing remains deferred.

7. **Focused validation evidence exists.** Given this story completes, when focused validation runs, then tests or documented smoke evidence prove sidecar subscription discovery, source-prefix routing for at least two synthetic Hexalith modules, and duplicate-safe delivery.

## Tasks / Subtasks

- [x] **Task 0 - Preflight: re-verify live anchors before editing.** (AC: 1-7)
  - [x] Confirm `src/Hexalith.Memories.Server/Program.cs` still registers `AddServerEventStoreIntegration(builder.Configuration)`, uses `UseCloudEvents()`, maps controllers, and calls `MapSubscribeHandler()` in that order.
  - [x] Confirm `src/Hexalith.Memories.EventStore/EventIngestionController.cs` still exposes `[Route("events")]` + `[HttpPost("ingest")]`, `PubSubName == "pubsub"`, and `TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"`.
  - [x] Confirm `docs/dev/eventstore-integration.md`, `docs/operations/deployment-configuration.md`, and `docs/operations/route-surface.md` already contain the June 24 contract text before deciding what to change. Do not rewrite stable sections just to restate existing content.
  - [x] Confirm `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs` still has `SubscribeHandler_ExposesEventStoreTopicBinding` and `/process` negative coverage.
  - [x] Confirm `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs` still has two-prefix coverage for `hexalith/tenants` and `hexalith/parties`.
  - [x] Confirm `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs` still proves the Dapr pub/sub publish path and duplicate replay behavior; treat it as optional slow evidence unless the implementation scope requires running integration tests.
  - [x] If any anchor moved since baseline `377df3f`, update this story and implementation plan before editing.

- [x] **Task 1 - Publish or tighten the cross-module event-intake contract.** (AC: 1,2,3,5,6)
  - [x] Update `docs/dev/eventstore-integration.md` only where needed to make the document title/intro and routing examples explicitly say **EventStore / Hexalith module event integration**, not EventStore-only.
  - [x] Ensure the docs state the canonical flow exactly: `Hexalith module -> Dapr pub/sub component -> Memories Server sidecar -> POST /events/ingest -> EventIngestionService -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)`.
  - [x] Include a concrete shared-topic `SourceToTenantMap` example with at least:
    - `hexalith/tenants` -> a configured tenant such as `tenant-events` or `tenants-index`;
    - `hexalith/parties` -> a configured tenant such as `party-events`.
  - [x] Preserve the existing source-prefix rules: longest-prefix wins, matching is case-insensitive, `source` is stable publisher identity, unknown source returns `unknown-source` / EventId 9110 and does not retry.
  - [x] Preserve the known limitation: one topic per deployment; independent topics require separate Memories deployments until multi-topic routing is approved.

- [x] **Task 2 - Keep operations docs cohesive with the developer contract.** (AC: 1,3,6)
  - [x] In `docs/operations/route-surface.md`, verify or add the ACL-facing statement that pub/sub delivery is `POST /events/ingest`, discovery is `/dapr/subscribe`, and `/process` does not exist.
  - [x] In `docs/operations/deployment-configuration.md`, verify or add the deployment-facing statement for `pubsub`, `MEMORIES_EVENTSTORE_TOPIC`, `EventStoreIntegration:Routing:SourceToTenantMap`, and Server sidecar ports.
  - [x] Keep these docs as concise cross-links to `docs/dev/eventstore-integration.md`; do not duplicate the full routing semantics in every document.

- [x] **Task 3 - Strengthen focused tests where existing proof is review-only.** (AC: 1,2,3,5,6,7)
  - [x] Extend `DocumentationCompletenessTests` or add a new contract test to assert `docs/dev/eventstore-integration.md` contains the exact cross-module phrases: `Hexalith modules`, `hexalith/tenants`, `hexalith/parties`, `SourceToTenantMap`, `shared-topic pattern`, `separate Memories deployments per topic`, `POST /events/ingest`, `/dapr/subscribe`, and the explicit `/process` refutation.
  - [x] If not already sufficient after preflight, extend `MiddlewareOrderTests.SubscribeHandler_ExposesEventStoreTopicBinding` to assert `pubsubname=pubsub`, topic `memories-events`, and route `/events/ingest` in the Dapr subscription payload. The current route may appear with or without a leading slash; keep the test tolerant of both shapes.
  - [x] If not already sufficient after preflight, add/extend EventStore tests proving an unknown `source` produces `EventIngestionOutcome.UnknownSource`, HTTP 200 from the controller, no workflow scheduling, and a diagnosable status string.
  - [x] Keep these tests unit/in-process unless implementation changes require integration coverage. Do not make Docker/Aspire a new requirement for the default verification path.

- [x] **Task 4 - Optional slow smoke evidence for the actual Dapr publish path.** (AC: 4,7)
  - [x] Reuse the existing integration test shape in `EventIngestionPipelineIntegrationTests.PublishViaDaprPubSub_ShouldBecomeSearchableWithinFiveSeconds_AndIgnoreDuplicateReplay` if slow infrastructure is available.
  - [x] If the sandbox or CI lane cannot run Aspire/Dapr integration tests, document that limitation in the Dev Agent Record and rely on the existing integration test plus the in-process unit/contract guards.
  - [x] Do not add a new direct REST module-ingestion path as a workaround. REST `/api/ingest` remains for external content ingestion, not Hexalith module event streams.

- [x] **Task 5 - Verify and finalize.** (AC: 1-7)
  - [x] Build and run the focused default-lane tests, at minimum:
    ```bash
    dotnet build tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
      -class Hexalith.Memories.Server.Tests.EventStoreIntegration.MiddlewareOrderTests
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
      -class Hexalith.Memories.Server.Tests.EventStoreIntegration.DocumentationCompletenessTests
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
      -class Hexalith.Memories.Server.Tests.Deployment.RouteSurfaceContractTests
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.Server.Tests/bin/Debug/net10.0/Hexalith.Memories.Server.Tests.dll \
      -class Hexalith.Memories.Server.Tests.Deployment.DeploymentConfigurationContractTests
    ```
  - [x] Build and run the EventStore unit tests if Task 3 edits that project:
    ```bash
    dotnet build tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll \
      -class Hexalith.Memories.EventStore.Tests.TenantEventRouterTests
    DiffEngine_Disabled=true dotnet exec \
      tests/Hexalith.Memories.EventStore.Tests/bin/Debug/net10.0/Hexalith.Memories.EventStore.Tests.dll \
      -class Hexalith.Memories.EventStore.Tests.EventIngestionServiceTests
    ```
  - [x] Record any optional integration-test run separately. Do not block completion only because Docker/Aspire is unavailable, but do not claim the optional smoke passed unless it ran.
  - [x] Update this story's Dev Agent Record, File List, Completion Notes, and Change Log with exact test counts and any skipped optional evidence.

## Dev Notes

### Scope and intent

This is a **contract + verification hardening** story over an event-intake path that already exists. The risk is not that Memories lacks a Dapr subscription endpoint; the risk is downstream drift: modules may publish to undocumented topics, bypass the sidecar with REST calls, configure ACLs for `/process`, or fail to understand duplicate/unknown-source behavior.

The implementation should therefore be preflight-first and additive. Many story requirements are already partly satisfied by the June 24 amendments and prior tests. If the preflight finds the docs/tests already satisfy an item, strengthen only the missing proof or close the story with evidence; do not churn stable docs.

### Current behavior to preserve (verified while drafting)

- `src/Hexalith.Memories.Server/Program.cs` registers Dapr client/workflows/actors, calls `AddServerEventStoreIntegration(builder.Configuration)`, then maps `UseCloudEvents()`, `MapControllers()`, and `MapSubscribeHandler()`. This is the canonical sidecar discovery path.
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs` exposes `POST /events/ingest` via `[Route("events")]` and `[HttpPost("ingest")]`, with `PubSubName = "pubsub"` and `TopicEnvVar = "MEMORIES_EVENTSTORE_TOPIC"`.
- `EventIngestionService` parses CloudEvents, resolves tenant/case through `ITenantEventRouter`, uses `EventStoreDedupKey.Build(route.TenantId, route.CaseId, envelope.Id)`, reserves preflight dedup when enabled, schedules `IngestionWorkflow` with the dedup key as the instance id, returns duplicate without scheduling, and maps unknown-source / tenant-not-found / tenant-deleting / auto-create-disabled / case-cap-exceeded to non-retry drops.
- `TenantEventRouter` matches `source` against `TenantEventRoutingOptions.SourceToTenantMap` using case-insensitive longest-prefix matching. Existing unit coverage includes `hexalith/tenants` and `hexalith/parties` routing to different tenants.
- `docs/dev/eventstore-integration.md` already documents `SourceToTenantMap`, `MEMORIES_EVENTSTORE_TOPIC`, `/dapr/subscribe`, `POST /events/ingest`, `/process` absence, the shared-topic pattern, separate deployments per topic, unknown-source alert EventId 9110, and preflight TTL alignment. The task is to make sure those claims are complete for cross-module consumers and guarded against drift.
- `docs/operations/deployment-configuration.md` and `docs/operations/route-surface.md` already carry Story 18.2/18.3 views of the same event-intake surface. Keep them cohesive with the developer doc.

### Files likely to touch

- `docs/dev/eventstore-integration.md` - UPDATE only if the cross-module contract text/examples are incomplete.
- `docs/operations/deployment-configuration.md` - UPDATE only if the deployment view lacks a needed cross-link or literal.
- `docs/operations/route-surface.md` - UPDATE only if the ACL view lacks a needed cross-link or `/process` refutation.
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` - likely UPDATE to add explicit cross-module literals.
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs` - UPDATE only if subscription discovery proof needs tightening.
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs` / `EventIngestionServiceTests.cs` / `EventIngestionControllerTests.cs` - UPDATE only if preflight shows routing, duplicate, or unknown-source proof is missing.
- `src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs` - UPDATE only if consumer AppHost guidance/defaults are actually wrong. Its parameterized defaults (`serverName = "memories"`, `daprHttpPort = 3502`, `daprGrpcPort = 50002`, `eventStoreTopic = "memories-events"`) differ from the in-repo AppHost defaults by design; do not "fix" them unless a real consumer-helper bug is found.

### What not to change

- Do **not** reopen Epic 9 or rewrite Story 9.1 implementation scope.
- Do **not** add multi-topic routing to a single Memories deployment.
- Do **not** add a direct REST module-event-ingestion path; cross-module event streams use Dapr pub/sub.
- Do **not** mutate `Hexalith.EventStore`, `Hexalith.Tenants`, `Hexalith.FrontComposer`, or other submodule contents.
- Do **not** add package references, package versions in `.csproj`, or `tools/release-packages.json` changes.
- Do **not** change `PubSubName`, `TopicEnvVar`, `/events/ingest`, or `/dapr/subscribe` without treating it as a consumer-breaking contract change.

### Testing strategy

Default-lane proof should be unit/in-process:

- `MiddlewareOrderTests` proves ASP.NET Core maps `/events/ingest`, `UseCloudEvents()` does not break plain JSON ingestion, `/dapr/subscribe` emits the Dapr subscription metadata, and `/process` is not mapped.
- `DocumentationCompletenessTests` protects the developer event-intake doc from losing required contract text.
- `RouteSurfaceContractTests` protects the ACL operation surface and `/process` refutation.
- `DeploymentConfigurationContractTests` protects the operator deployment literals (`pubsub`, topic env var, ports, source map).
- `TenantEventRouterTests` protects source-prefix routing for multiple synthetic modules.
- `EventIngestionServiceTests` protects duplicate, unknown-source, and scheduler/no-scheduler outcomes.

Optional slow proof:

- `EventIngestionPipelineIntegrationTests.PublishViaDaprPubSub_ShouldBecomeSearchableWithinFiveSeconds_AndIgnoreDuplicateReplay` proves the actual Dapr sidecar publish path and duplicate replay behavior. Run only when Docker/Aspire/Dapr infrastructure is available.

### Previous story intelligence

- Story 18.7 completed as docs/test only and used a strong pattern: preflight first, publish a consumer contract, add drift guards over doc claims, record exact test counts, and avoid production code changes when the preflight found zero drift.
- Story 18.6 published a stability contract and resolved the doc/test proof gap without changing public behavior; mirror its direct, source-cited contract style.
- Story 18.3 published `docs/operations/route-surface.md` and added route-surface drift guards. Reuse and extend that surface rather than creating a second operation-surface document.
- Story 18.2 published `docs/operations/deployment-configuration.md` and added deployment literal guards. Keep the source-of-truth split: deployment literals live there, routing semantics live in `docs/dev/eventstore-integration.md`.
- Recent commits are story-scoped and conventional: `test(story-18.3)`, `feat(story-18.4)`, `feat(story-18.5)`, `docs(story-18.6)`, `docs(story-18.7)`. This story should normally be `docs:` / `test:`.

### Project Structure Notes

- This story aligns with existing structure: developer docs in `docs/dev`, operator docs in `docs/operations`, in-process server tests under `tests/Hexalith.Memories.Server.Tests`, EventStore package tests under `tests/Hexalith.Memories.EventStore.Tests`, and optional slow Dapr/Aspire evidence under `tests/Hexalith.Memories.IntegrationTests`.
- No UX/UI work is involved.
- No data-persistence change is expected. If implementation unexpectedly touches persisted domain behavior, re-read `Hexalith.AI.Tools/hexalith-state-instructions.md` before editing.
- The repo uses .NET 10 / C# 14, Dapr 1.18.4, xUnit v3, Shouldly, and NSubstitute. Package versions are centralized in `Directory.Packages.props`; do not add versions to project files.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-18.8] - story statement, acceptance criteria, target artifacts, and out-of-scope boundaries.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-24.md] - approved Direct Adjustment, canonical event flow, no-REST/no-multi-topic constraints, and success criteria.
- [Source: _bmad-output/planning-artifacts/prd.md#Phase-1.5-Fast-Follow] - EventStore / Hexalith module event integration and sidecar ownership wording.
- [Source: _bmad-output/planning-artifacts/architecture.md#Data-Flow] - event-ingest flow through Dapr pub/sub, Memories Server sidecar, `/events/ingest`, and `IngestionWorkflow`.
- [Source: docs/dev/eventstore-integration.md] - existing EventStore / module event-ingest developer contract, setup, routing, replay/idempotency, unknown-source, one-topic limitation, and troubleshooting.
- [Source: docs/operations/deployment-configuration.md] - deployment literals for `pubsub`, `MEMORIES_EVENTSTORE_TOPIC`, `EventStoreIntegration:Routing:SourceToTenantMap`, and sidecar ports.
- [Source: docs/operations/route-surface.md] - ACL operation surface, `POST /events/ingest`, `/dapr/subscribe`, and `/process` refutation.
- [Source: src/Hexalith.Memories.Server/Program.cs] - Dapr/event-store wiring, middleware order, and `MapSubscribeHandler()`.
- [Source: src/Hexalith.Memories.EventStore/EventIngestionController.cs] - `PubSubName`, `TopicEnvVar`, route attributes, and outcome-to-HTTP mapping.
- [Source: src/Hexalith.Memories.EventStore/EventIngestionService.cs] - CloudEvent parse, route, preflight dedup, workflow schedule, duplicate/drop/retry outcomes.
- [Source: src/Hexalith.Memories.EventStore/TenantEventRouter.cs] - source-prefix routing semantics and case resolution.
- [Source: src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs] - `PubSubName`, `Topic`, `SourceToTenantMap`, auto-create and dedup options.
- [Source: src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs] - downstream AppHost helper and sidecar/topic defaults.
- [Source: tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs] - in-process `/events/ingest`, `/dapr/subscribe`, and `/process` proof.
- [Source: tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs] - two-module source-prefix routing proof.
- [Source: tests/Hexalith.Memories.EventStore.Tests/EventIngestionServiceTests.cs] - duplicate/unknown-source/drop/retry proof.
- [Source: tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs] - optional slow Dapr pub/sub duplicate-safe end-to-end proof.
- [Source: _bmad-output/implementation-artifacts/18-7-memories-client-mockability-stability-contract.md] - immediate previous story pattern for docs/test-only contract hardening and exact test-count close-out.
- [Source: _bmad-output/project-context.md] - repo technology stack, Dapr/idempotency rules, docs placement, testing conventions, release rules, and submodule policy.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- `dotnet build tests/Hexalith.Memories.Server.Tests/...` → Build succeeded, 0 warnings, 0 errors.
- `dotnet build tests/Hexalith.Memories.EventStore.Tests/...` → Build succeeded, 0 warnings, 0 errors.
- RED proof: `DocumentationCompletenessTests` failed on `should contain "hexalith/tenants"` before the doc edit.
- GREEN: `DocumentationCompletenessTests` passed after the doc edit.

### Completion Notes List

**Approach: preflight-first, additive, docs/test only.** No production C# changed. Preflight (Task 0) confirmed every live anchor matches baseline `377df3f`:

- `Program.cs` registers `AddServerEventStoreIntegration(builder.Configuration)` (line 332) and maps `UseCloudEvents()` → `MapControllers()` → `MapSubscribeHandler()` in that order (lines 351-353).
- `EventIngestionController` still exposes `[Route("events")]` + `[HttpPost("ingest")]`, `PubSubName == "pubsub"`, `TopicEnvVar == "MEMORIES_EVENTSTORE_TOPIC"`.
- The two ops docs (`route-surface.md`, `deployment-configuration.md`) already carry the `POST /events/ingest`, `/dapr/subscribe`, `/process`-refutation, `pubsub`, `MEMORIES_EVENTSTORE_TOPIC`, `SourceToTenantMap`, and sidecar-port statements and cross-link to `eventstore-integration.md` §1.3–§1.6 — **no change needed (Task 2 was verify-only)**.

**Drift found and closed (the only real gap):** the developer doc framed the contract as EventStore-only and lacked the exact canonical flow string and the `hexalith/tenants` / `hexalith/parties` literal example.

- **Task 1** — `docs/dev/eventstore-integration.md`: reframed title/intro as cross-module ("EventStore / Hexalith Module Event Integration"); added the exact canonical flow `Hexalith module -> Dapr pub/sub component -> Memories Server sidecar -> POST /events/ingest -> EventIngestionService -> DaprWorkflowClient.ScheduleNewWorkflowAsync(IngestionWorkflow)`; added a concrete shared-topic `SourceToTenantMap` JSON example with `hexalith/tenants -> tenant-events` and `hexalith/parties -> party-events`; preserved the existing longest-prefix/case-insensitive/stable-source/unknown-source-9110-no-retry rules and the one-topic-per-deployment limitation. Additive only (41 insertions, 1 deletion = the title); the pre-existing Story 18.1 LF block was left byte-untouched.
- **Task 3 bullet 1** — extended `DocumentationCompletenessTests` with drift guards for `Hexalith modules`, `hexalith/tenants`, `hexalith/parties` (the other required literals — `SourceToTenantMap`, `shared-topic pattern`, `separate Memories`/`deployments per topic`, `POST /events/ingest`, `/dapr/subscribe`, `/process` refutation — were already asserted).
- **Task 3 bullet 2** — preflight found `MiddlewareOrderTests.SubscribeHandler_ExposesEventStoreTopicBinding` already asserts `pubsubname=pubsub`, topic `memories-events`, and route `events/ingest` (slash-tolerant), plus `ProcessRoute_IsNotMappedAsEventIngestionSurface` negative coverage. **No change needed.**
- **Task 3 bullet 3** — preflight found `EventIngestionServiceTests.ProcessAsync_UnknownSource_ReturnsDropNoInstanceId` proves `EventIngestionOutcome.UnknownSource` + `StatusUnknownSource` + no scheduling, and `EventIngestionControllerTests` `[InlineData(EventIngestionOutcome.UnknownSource)]` proves HTTP 200 (`OkObjectResult`). **No change needed.**

**Task 4 — optional slow Dapr/Aspire smoke: NOT RUN (environment limitation).** This sandbox has no Dapr sidecar / Docker / Aspire (the in-process `MiddlewareOrderTests` logged `Connection refused` to the sidecar, confirming none is present). `EventIngestionPipelineIntegrationTests.PublishViaDaprPubSub_ShouldBecomeSearchableWithinFiveSeconds_AndIgnoreDuplicateReplay` was therefore **not executed**; AC4 (duplicate-safe delivery) is covered in the default lane by `EventIngestionServiceTests` duplicate/no-scheduling cases plus the existing (unchanged) integration test that runs when infra is available. No direct REST module-ingestion workaround was added.

**Test evidence (focused default lane, `DiffEngine_Disabled=true dotnet exec` on xUnit v3 dlls):**

- `Hexalith.Memories.Server.Tests` — focused contract classes (Middleware + Documentation + RouteSurface + DeploymentConfiguration): **22 passed, 0 failed**.
- `Hexalith.Memories.Server.Tests` — **full suite: 1942 passed, 0 failed, 1 skipped** (pre-existing unrelated skip).
- `Hexalith.Memories.EventStore.Tests` — TenantEventRouter + EventIngestionService + EventIngestionController: **44 passed, 0 failed**.
- `Hexalith.Memories.EventStore.Tests` — **full suite: 94 passed, 0 failed**.

All seven ACs satisfied by published contract text + drift guards + existing in-process proofs; only the optional slow Dapr smoke (Task 4) was environment-skipped as permitted.

### File List

- `docs/dev/eventstore-integration.md` — MODIFIED (cross-module title/intro, canonical flow string, `hexalith/tenants`/`hexalith/parties` shared-topic `SourceToTenantMap` example).
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs` — MODIFIED (added `Hexalith modules`, `hexalith/tenants`, `hexalith/parties` drift guards).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — MODIFIED (story status `ready-for-dev` → `in-progress` → `review`).
- `_bmad-output/implementation-artifacts/18-8-cross-module-dapr-event-intake-contract-and-verification.md` — MODIFIED (this story: task checkboxes, Dev Agent Record, Change Log, Status).
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CrossModuleEventIntakeE2ETests.cs` — ADDED (QA generate-e2e-tests gap-fill: 3 Tier-2 in-process HTTP E2E tests over `/events/ingest` — two-module shared-topic routing (AC2/AC7), unknown-source non-retry drop (AC5), and duplicate-safe delivery (AC4/AC7)).
- `_bmad-output/implementation-artifacts/tests/test-summary-18-8-cross-module-event-intake.md` — ADDED (QA generate-e2e-tests summary: gaps discovered/applied, per-AC coverage, and test counts).

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated story-automator review) · **Date:** 2026-06-25 · **Outcome: Approve.**

**Method.** Adversarial validation of every story claim against the live tree: cross-referenced git changes vs the File List, validated all 7 ACs against actual source, audited each `[x]` task, then *built and ran* the suites rather than trusting the recorded counts.

**Git vs File List — consistent.** All source/test/doc changes the story claims are present in git (`docs/dev/eventstore-integration.md` M, `DocumentationCompletenessTests.cs` M, `CrossModuleEventIntakeE2ETests.cs` + test summary added). The only undeclared working-tree changes are `.claude/scheduled_tasks.lock` and a `_bmad-output/story-automator/` orchestration log — both excluded from review scope (automation/runtime artifacts, not application source). No "claimed but absent" or "changed but undocumented source" discrepancies.

**AC validation — all covered.** AC1 (`MiddlewareOrderTests` `/dapr/subscribe` discovery), AC2 (`TenantEventRouterTests` unit + new E2E two-prefix routing), AC3 (`RouteSurfaceContractTests` + middleware `/process` 404 + doc refutation), AC4 (service duplicate unit + new E2E duplicate-safe), AC5 (service unknown-source unit + new E2E 200-drop with **EventId 9110 fired live** — observed in the run log), AC6 (`DocumentationCompletenessTests` one-topic limitation), AC7 (the +3 E2E close the routing/duplicate halves at the HTTP surface). Each `[x]` task verified against real anchors — e.g. controller maps `UnknownSource → Ok` (HTTP 200) at `EventIngestionController.cs:91`, and the service returns `Duplicate()` without scheduling on a preflight `Duplicate` reservation (`EventIngestionService.cs:156`).

**Claims independently re-run (this review):**
- `Hexalith.Memories.Server.Tests` build: succeeded, **0 warnings** (warnings-as-errors gate green).
- `CrossModuleEventIntakeE2ETests`: **3 passed / 0 failed**.
- Focused contract set (Middleware + Documentation + RouteSurface + DeploymentConfiguration): **22 passed / 0 failed** (25 with the +3 E2E).
- Full `Hexalith.Memories.Server.Tests`: **1945 passed, 0 failed, 1 skipped** (matches claim exactly).
- Full `Hexalith.Memories.EventStore.Tests`: **94 passed, 0 failed** (matches claim).
- Spot-checks: new doc `SourceToTenantMap` JSON example uses the real binding path `EventStoreIntegration:Routing:{PubSubName,Topic,SourceToTenantMap}` (`TenantEventRoutingOptions.cs:9`); intro anchor `#16-route-surface-for-hexalith-modules` resolves to the real `### 1.6` heading; all three review files are CRLF per `.editorconfig`.

**Findings.** 0 Critical, 0 High, 0 Medium. **1 Low (no fix applied):** in `TwoHexalithModulePrefixes_…`, the `parties.InstanceId.ShouldNotBe(tenants.InstanceId)` assertion is also satisfied by the two events' differing CloudEvent ids, so it does not *isolate* routing as the sole cause of distinctness — but the test still proves the end-to-end accept→route→schedule path with source-keyed router mocks and `Received(2)`, and routing isolation is proven at the unit level in `TenantEventRouterTests`. Left as-is: strengthening a green, correct test was judged churn not worth the regression risk in an autonomous pass.

**Decision.** Status → **done**; sprint-status synced `18-8-… → done`.

## Change Log

| Date | Version | Description | Author |
| :--- | :------ | :---------- | :----- |
| 2026-06-25 | 0.1 | Story drafted via create-story workflow. Status -> ready-for-dev. | Bob (SM) |
| 2026-06-25 | 0.2 | Implemented via dev-story (docs/test only). Published cross-module event-intake contract in `eventstore-integration.md` (canonical flow + `hexalith/tenants`/`hexalith/parties` shared-topic example) and added matching drift guards to `DocumentationCompletenessTests`. Preflight confirmed ops docs + middleware/router/service/controller tests already satisfy their ACs (no change). Optional Dapr/Aspire smoke environment-skipped. Server.Tests 1942 pass / EventStore.Tests 94 pass. Status -> review. | Amelia (Dev) |
| 2026-06-25 | 0.3 | QA via generate-e2e-tests (gap-fill, no production source changed). Added `CrossModuleEventIntakeE2ETests` (3 Tier-2 HTTP E2E tests) closing the AC7 gap where two-module shared-topic routing (AC2), the unknown-source non-retry drop (AC5), and duplicate-safe delivery (AC4) were proven only at the unit level — now driven end-to-end through `/events/ingest` (unknown-source test also fires the real EventId 9110 warning live). Server.Tests 1945 pass (+3) / EventStore.Tests 94 pass; focused contract set 25 pass (+3); 0 warnings. Summary at `tests/test-summary-18-8-cross-module-event-intake.md`. Status stays review. | Claude (QA) |
| 2026-06-25 | 1.0 | Adversarial senior-developer review (story-automator). Re-ran all suites: Server.Tests 1945 pass/1 skip, EventStore.Tests 94 pass, focused contract set 22 (+3 E2E = 25), build 0 warnings — every recorded count reproduced. AC1–AC7 all covered; git vs File List consistent; doc config example + anchor + CRLF verified. 0 Critical/High/Medium, 1 Low (non-blocking E2E assertion-rigor note, left as-is). Outcome Approve. Status -> done; sprint-status synced. | Jérôme Piquot (AI Review) |
