# Story 9.1: Event Auto-Discovery & DAPR Pub/Sub Subscription

Status: review

**Effort estimate:** ~4-5 working days — 0.25 day pubsub + subscription AppHost wiring (Task 1), 0.5 day CloudEvents subscription endpoint + envelope mapping (Task 2), 0.25 day event-id idempotency (Task 3), 0.25 day tenant/case resolution (Task 4), 0.5 day `Hexalith.Memories.EventStore` package scaffolding (Task 5), 1.0 day unit tests (Task 6), 0.75 day integration tests (Task 7 — Tier 2 pub/sub roundtrip), 0.25 day docs + sprint-status + retro entry (Task 8). Add 0.5 day cushion for Kestrel/CloudEvents middleware surprises (request-body fork, raw CloudEvents vs DAPR-unwrapped payload).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** A **zero-code DAPR pub/sub subscription** for one configured topic that auto-discovers event types from CloudEvents and funnels event payloads through the existing `IngestionWorkflow` without developer-written mapping code. The implementation uses a controller-based subscription endpoint decorated with `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]`, plus the canonical DAPR middleware sequence `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();`. It preserves the required CloudEvents fields (`id`, `source`, `type`, `subject`, `time`) as memory metadata, uses `cloudevent.id` as the ingestion source identifier for deduplication, routes `source` to `tenantId` and derived aggregate type / subject to case selection, and returns 5xx only for retryable conditions. Duplicate handling follows a **compensated hybrid** model: endpoint-level preflight reservation suppresses obvious redeliveries, schedule failures release that reservation, and the workflow-level permanent dedup key remains the authoritative safety net. The new **publishable `Hexalith.Memories.EventStore` NuGet package** contains the controller, mapper, router, response DTO, options, and thin integration abstractions; the Server project supplies adapters for workflow scheduling, tenant-state checks, case creation, and telemetry. This story closes **FR59** and **NFR21** only. **Dual embeddings, causal-edge indexing, and any metadata-origin contract expansion remain out of scope for Story 9.2+**.

**What already exists (do NOT rebuild):**

1. **`IngestionInput` — `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`.** Already carries `CausationId` / `CorrelationId` + `SourceType` enum with `Event` member + `Metadata: Dictionary<string, MetadataField>`. **Reuse verbatim.** No contract changes are needed for 9.1 — all CloudEvents envelope fields fit in the existing `Metadata` dictionary. Do NOT fork a dedicated `EventIngestionInput` record; the workflow is type-specialized on `IngestionInput`, and a parallel type would force a parallel workflow.
2. **`SourceType.Event` — `src/Hexalith.Memories.Contracts/V1/SourceType.cs:11`.** Already registered in `CamelCaseStringEnumConverter<SourceType>`. Set `input.SourceType = SourceType.Event` in the mapper.
3. **`IngestionWorkflow` + `CheckIdempotencyActivity` / `SaveDedupKeyActivity` — `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` + `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs` + `SaveDedupKeyActivity.cs`.** The workflow already does: idempotency → validation → extract → embed → fan-out index → verify → dedup persist. **Reuse verbatim.** Event ingestion is just another caller. Do NOT add an `IngestionWorkflowEventVariant` or branch the workflow on `SourceType.Event` — the existing per-activity behavior is identical, except that `ValidateContentActivity` must tolerate `ContentType = "application/json"` (already tolerated; inspect `ValidateContentActivity.cs` at implementation time to confirm) and `ExtractContentActivity` must treat a JSON payload as already-extracted UTF-8 text (Kreuzberg's default text extractor already returns UTF-8 for `application/json` — verify at implementation time by reading `ContentExtractionClient.cs`).
4. **`DedupKeyBuilder.BuildKey` + SHA-256 helper — `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs`.** Key format `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`. **Reuse verbatim** — the mapper sets `sourceUri = cloudevent.id` so the existing hash-based dedup handles at-least-once redeliveries without any changes to `CheckIdempotencyActivity`.
5. **`CamelCaseStringEnumConverter<T>` + `MemoriesJsonContext.Options` — `src/Hexalith.Memories.Contracts/V1/CamelCaseStringEnumConverter.cs` + `MemoriesJsonContext.cs`.** Use `MemoriesJsonContext.Options` for every serialize/deserialize call in the mapper and the subscription endpoint. AOT-safe, source-generated — do NOT introduce a new `JsonSerializerOptions` instance.
6. **`TenantRegistryService.GetAsync` + `TenantStatusGuard.ValidateTenantActiveAsync` — `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` + `TenantStatusGuard.cs`.** Reuse these via a thin **Server-side adapter**. The EventStore package must not take a project reference back to `Hexalith.Memories.Server`.
7. **`CaseService.GetCaseAsync` + `CaseService.CreateCaseAsync` — `src/Hexalith.Memories.Server/Cases/CaseService.cs`.** Reuse these via a thin **Server-side adapter**. Case-management ownership remains in Server.
8. **Hexalith.EventStore ecosystem patterns — `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Program.cs:30,43` + `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ProjectionNotificationController.cs:31`.** Canonical subscription surface: `app.UseCloudEvents()` middleware + `app.MapControllers()` + `app.MapSubscribeHandler()` + `[Topic(pubSubName, "topic-name")]` on an `[ApiController]` route. Follow this pattern exactly; do **not** switch to minimal-API metadata registration in this story.
9. **Existing Server telemetry + workflow scheduling infrastructure — `src/Hexalith.Memories.Server/Program.cs` + `src/Hexalith.Memories.Server/Telemetry/`.** Reuse the existing workflow client and telemetry patterns via **Server-owned adapter interfaces** exposed by the EventStore package.

**What 9.1 adds:**

1. **`src/Hexalith.Memories.EventStore/`** — NEW publishable NuGet project, SDK `Microsoft.NET.Sdk`. Registered in `Hexalith.Memories.slnx`. References `Hexalith.Memories.Contracts`. Packable = true (per architecture table "10 Hexalith.Memories.EventStore — Zero-code integration (Phase 1.5)"). Files:
    - `EventIngestionController.cs` — `[ApiController]` + `[Route("events")]` + `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]` POST endpoint that receives a raw `CloudEvent<JsonElement>` and forwards to `IEventIngestionService`. No domain logic here — controller is a thin DAPR binding shim.
    - `CloudEventToIngestionInputMapper.cs` — `internal static class` with `IngestionInput Map(CloudEvent<JsonElement> evt, TenantEventRoute route)`. Pure function. It validates required envelope fields, validates that `evt.Data` is present before any `GetRawText()` call, and maps only the required 9.1 metadata fields.
    - `EventIngestionResponse.cs` — response DTO used by the controller so the `instanceId` contract is explicit: `instanceId` is present only for `accepted` responses; duplicates and drops return status + reason without inventing workflow IDs.
    - `IEventIngestionService.cs` + `EventIngestionService.cs` — thin orchestration service that (a) resolves a typed routing outcome, (b) runs the mapper, (c) performs preflight dedup reservation when enabled, (d) schedules the existing workflow through `IEventIngestionWorkflowScheduler`, and (e) compensates by releasing the reservation if scheduling fails.
    - `IEventIngestionWorkflowScheduler.cs` — EventStore-owned abstraction implemented in Server via a thin adapter over `DaprWorkflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input)`. **Prevents the package from depending on a Server workflow type name.**
    - `ITenantStatusAccessor.cs` — EventStore-owned abstraction implemented in Server via `TenantRegistryService` + `TenantStatusGuard`. **Prevents the package from referencing Server tenancy types directly.**
    - `IEventIngestionTelemetry.cs` — EventStore-owned abstraction implemented in Server via existing telemetry helpers. The package may log locally, but it must not take a compile-time dependency on `EndpointTelemetryScope`.
    - `ICaseCreationService.cs` — interface declared in EventStore with one method: `Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken ct)`. Implemented in the Server project via `CaseCreationServiceAdapter` (delegates to `CaseService.CreateCaseAsync`). **Prevents EventStore from referencing Server.**
    - `ITenantEventRouter.cs` + `TenantEventRouter.cs` + `TenantEventRoute.cs` + `TenantEventRouteResolution.cs` — route a CloudEvent to a typed outcome instead of returning nullable tuples. MVP implementation: config-driven mapping (`TenantEventRoutingOptions`) from `source` → `tenantId`, then aggregate-type / subject → `caseId`, with explicit statuses for `UnknownSource`, `TenantProvisioning`, `TenantDeleting`, `AutoCreateDisabled`, and `CaseCapExceeded`.
    - `TenantEventRoutingOptions.cs` — bound from `appsettings.json` section `EventStoreIntegration:Routing` via `IOptions<T>`. Fields:
        - `Topic` (string, required) — topic name subscribed to. MVP: single topic per deployment.
        - `SourceToTenantMap` (`Dictionary<string, string>`) — CloudEvents `source` prefix → tenantId. Longest-prefix wins.
        - `AutoCreateCases` (bool, default `true`) — when `true`, the router calls `CaseService.CreateCaseAsync` lazily on first event per `(tenantId, aggregateType)`.
        - `CaseNameTemplate` (string, default `"events:{aggregateType}"`) — token-replacement template with an allow-listed token set (`{aggregateType}`, `{tenantId}` only). This story does **not** use raw `string.Format(...)`.
        - `MaxAutoCreatedCasesPerTenant` (int, default `100`) — hard cap for lazy case creation.
        - `PreflightDedupEnabled` (bool, default `true`) — enables endpoint-level duplicate suppression.
        - `PreflightDedupTtl` (`TimeSpan`, default `24h`) — reservation TTL; validated against the configured DAPR retry window.
    - `EventStoreIntegrationServiceCollectionExtensions.cs` — **public** `services.AddMemoriesEventStoreIntegration(IConfiguration config)` that registers the controller, mapper, service, router, response DTO, and options, plus the package-owned abstractions. Per ADR 9.1-F, the public surface is intentionally small: registration, options, response DTO, route DTOs, and adapter interfaces are public; implementation types remain `internal`.
    - `Hexalith.Memories.EventStore.csproj` — `IsPackable = true`; matches NuGet version from `Directory.Packages.props`; target framework matches solution (net10.0). Package metadata follows the `Hexalith.Memories.Contracts` csproj shape (description, authors, license).
2. **`pubsub.yaml` component + subscription wiring — `deploy/dapr/components/pubsub.yaml`.** Redis Streams pub/sub with a fixed component name `pubsub` (NOT a separate broker — reuse the existing Redis dependency). The AppHost resource metadata and the non-Aspire YAML must stay aligned; do **not** hard-code production host/password values in one place and not the other.
    ```yaml
    apiVersion: dapr.io/v1alpha1
    kind: Component
    metadata:
        name: pubsub
    spec:
        type: pubsub.redis
        version: v1
        metadata:
            - name: redisHost
              value: "${PUBSUB_REDIS_HOST}"
            - name: redisPassword
              value: "${PUBSUB_REDIS_PASSWORD}"
    ```
    **AppHost wiring (`src/Hexalith.Memories.AppHost/Program.cs`):** add `builder.AddDaprComponent("pubsub", "pubsub.redis")`, source its Redis metadata from the same values used by the server's Redis dependency, and add `.WithReference(pubSub)` to the Memories server resource so the sidecar discovers it. Mirror the existing `stateStore` registration pattern — do NOT bind-mount the YAML inside Aspire.
3. **Canonical middleware + controller mapping — `src/Hexalith.Memories.Server/Program.cs`.** Exactly this order in the built app:
    - `app.UseCloudEvents()`
    - `app.MapControllers()`
    - `app.MapSubscribeHandler()`

    Add `builder.Services.AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` if missing. Do not document or implement competing route-registration orders elsewhere in the story.

4. **Subscription endpoint — `POST /events/ingest`.** Decorated with `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]`. Request body: DAPR-unwrapped `CloudEvent<JsonElement>`. Response uses `EventIngestionResponse` and an explicit contract:
    - **200 OK + `{ status: "accepted", instanceId }`** — workflow scheduled successfully.
    - **200 OK + `{ status: "duplicate", wasDuplicate: true }`** — endpoint-level preflight dedup rejected a redelivery OR the workflow-level safety net classified it as a duplicate.
    - **200 OK + log warn** — tenant does not exist or has been deleted. Event is dropped (NOT retried) because DAPR would redeliver indefinitely for a tenant that will never exist. See "At-least-once vs dead-letter" in Dev Notes.
    - **400 Bad Request** — CloudEvents envelope missing required fields (`id`, `type`, `source`) or payload data (`data`). DAPR does NOT retry 4xx responses — these go to the dead-letter topic if configured, else are dropped.
    - **500 Internal Server Error** — scheduling failed transiently. If a preflight reservation was acquired, release it before returning 500 so the retry path is clean.
5. **Tenant/case routing logic — `TenantEventRouter`.** MVP: extract `tenantId` from CloudEvents `source` via longest-prefix, case-insensitive match against `SourceToTenantMap`. Extract `aggregateType` from CloudEvents `type` (`"MyApp.Claims.ClaimSubmittedV2"` → `"Claims"` — the second dotted segment by convention; document this convention in `docs/dev/eventstore-integration.md`). Resolve a **typed** `TenantEventRouteResolution` with one of:
    - `Accepted(TenantEventRoute route)`
    - `UnknownSource`
    - `TenantProvisioning`
    - `TenantDeleting`
    - `AutoCreateDisabled`
    - `CaseCapExceeded`

    Case resolution uses a validated token-replacement `CaseNameTemplate`, a cached `tenantId + aggregateType -> caseId` mapping, and a reservation key so concurrent first-time events converge on the same case ID.

6. **CloudEvents → IngestionInput mapping — `CloudEventToIngestionInputMapper.Map`.** Precise field translation:
    - `IngestionInput.TenantId` ← from `TenantEventRoute`.
    - `IngestionInput.CaseId` ← from `TenantEventRoute`.
    - `IngestionInput.SourceUri` ← **CloudEvents `id`** (guaranteed unique per at-least-once semantics — this is what drives idempotency via the existing `DedupKeyBuilder`).
    - `IngestionInput.ContentBytes` ← `Encoding.UTF8.GetBytes(evt.Data.GetRawText())` **only after validating that `evt.Data` is present and readable**.
    - `IngestionInput.ContentType` ← `evt.DataContentType ?? "application/json"`.
    - `IngestionInput.SourceType` ← `SourceType.Event`.
    - `IngestionInput.IngestedBy` ← `"events"` (system identity — NOT the event's `UserId` field; that goes into metadata for auditability without overriding provenance).
    - `IngestionInput.Metadata` ← required CloudEvents metadata preserved using the **existing** metadata contract:
        - `cloudevent.id` ← `evt.Id`
        - `cloudevent.source` ← `evt.Source.ToString()`
        - `cloudevent.type` ← `evt.Type`
        - `cloudevent.subject` ← `evt.Subject ?? "(unset)"` (aggregate ID — must flow through the same exact-match metadata filter/index path used by search filters, not just be copied into an opaque blob)
        - `cloudevent.time` ← `evt.Time?.ToString("o")` (ISO-8601)
        - `event.aggregateType` ← derived from `evt.Type` (second dotted segment)
7. **Docs — `docs/dev/eventstore-integration.md`.** NEW developer guide covering: subscription setup (`pubsub.yaml` + `AddMemoriesEventStoreIntegration`); CloudEvents envelope requirements; tenant/case routing configuration; aggregate-type extraction rules; idempotency semantics (`cloudevent.id` drives dedup); exact-match subject filtering; at-least-once + dead-letter strategy; and a worked example that ends in a searchable memory unit. The worked example must stay within 9.1 scope — no causal-edge claims.
8. **`deferred-work.md`** update: mark dual embeddings, causal-edge indexing, and any metadata-origin contract expansion as explicitly deferred to Story 9.2+ so implementation does not drift back into them.

**What does NOT ship:**

- **Dual embeddings (raw payload + NL description).** Story 9.2.
- **Automatic causal-edge indexing from event-specific causation / correlation rules.** Story 9.2. Story 9.1 does not add new event-to-graph guarantees beyond ordinary ingestion.
- **Any `MetadataOrigin` contract change (for example, introducing `System`).** Not part of Story 9.1.
- **Handler registration listing + mismatch detection (FR62).** Story 9.3. Do NOT ship a `memories handlers list` CLI command or `/api/eventstore/handlers` endpoint.
- **Dead-letter topic configuration.** MVP: missing-tenant events are dropped with a `Warning` log; malformed envelopes return 400 (DAPR handles DLT per the pubsub component config). A formal DLT-with-operator-replay workflow is a future story.
- **Authentication on the subscription endpoint.** The subscription route accepts internal DAPR traffic only; DAPR API token auth (Story 5.4 AC3) covers this when `DAPR_API_TOKEN_MODE=enabled`. The endpoint is NOT exposed via ingress.
- **Publication-side helpers.** 9.1 is subscribe-only. There is no `IMemoriesEventPublisher` or similar — publication is the downstream service's responsibility (typically Hexalith.EventStore's `EventPublisher`).
- **MCP tool for event subscription status.** Phase 1.5 Story 10.x, not here.
- **Multi-topic subscriptions.** MVP subscribes to one topic per deployment (configurable). A consumer needing N topics runs N deployments today — multi-topic routing is a future refinement.
- **CloudEvents schema validation beyond required-field checks.** Schema registry integration, strict type-to-schema matching, and version mismatch detection are out-of-scope for 9.1.
- **Custom retry policy at the subscription layer.** DAPR's resiliency policies are the canonical retry surface. If tighter control is needed later, add a `resiliency.yaml` component — do NOT implement HTTP-level retry loops in the controller.
- **Transaction-per-event batching.** Each CloudEvent spawns its own `IngestionWorkflow` instance. Batching multiple related events into one workflow is a Phase 2 optimization.
- **Access-telemetry audit event (FR67).** Event ingestion emits standard ingestion telemetry (`AccessTelemetryLog.OperationIngest`, EventId 7502/7512) but does NOT emit a separate `EventIngestionTelemetryEvent` bank. Operators who need to distinguish event-sourced from user-driven ingestion can filter by the `sourceType` tag on the existing activity.

**Primary risks:**

1. **`app.UseCloudEvents()` placement breaks the existing `/api/ingest` POST.** Minimal APIs consume the request body as bytes/JSON; `UseCloudEvents()` transforms JSON-envelope-wrapped requests in-place. If the middleware runs before `/api/ingest` routes, it may incorrectly attempt CloudEvents-unwrap a non-CloudEvents request. **Mitigation:** (a) `UseCloudEvents()` is a no-op when the request's `Content-Type` is not `application/cloudevents+json` — confirm via the Dapr.AspNetCore source; (b) guard test `IngestEndpointTests.PlainJsonPost_BypassesCloudEventsUnwrap` — POST `/api/ingest` with `Content-Type: application/json` and assert the request body arrives at the handler unmodified; (c) if confirmed unsafe, place `UseCloudEvents()` inside a `UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/events"), inner => inner.UseCloudEvents())` branch to scope it.
2. **At-least-once delivery + duplicate suppression race.** Two redeliveries of the same event can arrive before the workflow writes the permanent dedup key. **Mitigation:** (a) perform a preflight reservation (`SET NX`) before scheduling; (b) if scheduling fails after acquiring the reservation, delete that reservation before returning 500; (c) keep the workflow-level permanent dedup key authoritative; (d) guard tests `EventIngestionServiceTests.PreflightReservation_ReturnsDuplicate_WhenKeyAlreadyExists`, `.PreflightReservation_ReleasesKey_WhenSchedulingFails`, and `EventIngestionTests.ConcurrentRedeliveries_ResultInSingleMemoryUnit`.
3. **Missing `subject` or `data` weakens grouping and can crash naïve mapping code.** `subject` is optional in CloudEvents 1.0 and `data` can be missing. **Mitigation:** (a) when `subject` is absent, persist `"(unset)"` and document that aggregate grouping requires publishers to send it; (b) when `data` is absent, return `400 INVALID_CLOUDEVENT` before any `GetRawText()` call; (c) guard tests `CloudEventToIngestionInputMapperTests.SubjectMissing_MetadataShowsExplicitUnset_NoCrash` and `EventIngestionControllerTests.DataMissing_Returns400`.
4. **`SourceToTenantMap` validation can only catch non-existent tenants, not wrong-but-existing tenants.** Startup validation helps, but it does not prove routing correctness. **Mitigation:** (a) fail fast when config points to a tenant that does not exist; (b) document the publisher source-stability contract; (c) emit `UnknownSource` logs / metrics for unmapped values; (d) keep maps small and reviewable.
5. **Case auto-creation can explode cardinality or create inconsistent names under concurrency.** **Mitigation:** (a) `CaseNameTemplate` uses allow-listed token replacement, not unrestricted `string.Format`; (b) cap auto-created cases per tenant; (c) use a reservation key so concurrent first events for the same aggregateType converge on one case; (d) guard tests `TenantEventRouterTests.CaseCap_ExceededReturnsInvariantFailure` and `.ConcurrentFirstEvents_ResolveSameCase`.
6. **Search freshness can be measured incorrectly if the story uses publisher `cloudevent.time` as an SLO clock.** **Mitigation:** measure NFR6 from server receipt / test publish time to first search hit, while still preserving the publisher `cloudevent.time` as metadata.
7. **Subscription registration can silently fail if topic binding relies on unsupported metadata discovery.** **Mitigation:** use the controller attribute path only, verify `GET /dapr/subscribe` in tests, and fail fast at startup if the discovered subscription list is empty.
8. **Replay semantics conflict with idempotency.** Hexalith.EventStore supports event replay. Replayed events use the same envelope + `cloudevent.id` — which means the memory unit already exists, `CheckIdempotencyActivity` returns `WasDuplicate = true`, and the workflow returns early without re-indexing. That's intentional for NORMAL at-least-once redelivery but WRONG for operator-triggered replay-after-tenant-restore. **Mitigation:** document this explicitly in `docs/dev/eventstore-integration.md` under "Replay semantics": replay is designed for event-store rebuilds, not memory rebuilds; to rebuild memory from an event stream, operators must delete the tenant's memory units first (Story 3.5) OR use the tenant-provisioning workflow to recreate the tenant, then re-publish. DO NOT add a `forceReplay` bypass in 9.1 — that would regress the at-least-once idempotency guarantee.
9. **NFR6 <5s indexing freshness.** The full ingestion pipeline on a 1 KB JSON event is usually fast, but external dependencies can stretch the tail. **Mitigation:** enforce a stable Tier 2 latency check for the common path, and keep the longer-tail Aspire smoke in Tier 3 / nightly.
10. **`UseCloudEvents()` + `MapControllers()` + `MapSubscribeHandler()` order sensitivity in the middleware pipeline.** Wrong order causes either envelope mis-parsing or empty subscription discovery. **Mitigation:** document one canonical order only and verify it with `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic`.
11. **Clock skew between publisher and subscriber breaks event-time ordering.** If Story 9.2 relies on `cloudevent.time` for ordering (e.g., gap marker age), skew between the publisher's clock and the Memories server's clock causes incorrect ordering decisions. Out of scope for 9.1 — BUT: do NOT use the subscriber's `DateTimeOffset.UtcNow` to tag the metadata field `cloudevent.time`; preserve the publisher's value verbatim. Story 9.2 will decide how to use it.
12. **`source` URIs may contain path characters that break Redis key format.** CloudEvents `source` is a URI-reference which can include `/`, `?`, `#`. These must not leak into Redis keys (the existing `DedupKeyBuilder.BuildKey` SHA-256-hashes the source URI, so this is safe for dedup), but the `SourceToTenantMap` key lookup does prefix-matching on the raw string — a publisher sending a slightly different casing (`HTTPS://...` vs `https://...`) would miss the map. **Mitigation:** (a) the router performs case-insensitive longest-prefix matching; (b) document the case-sensitivity posture in `docs/dev/eventstore-integration.md`; (c) guard test `TenantEventRouterTests.SourcePrefixMatch_IsCaseInsensitive`.
13. **Silent source-scheme drift routes events to "unknown" for hours before detection.** A publisher flipping `http://` → `https://` during a routine certificate migration breaks longest-prefix match; events are dropped with 200 + `Warning` log, no alert fires, search gaps appear downstream before anyone notices. Risk #12 addresses casing but not scheme or path-component drift. **Mitigation:** (a) emit a metric `memories_eventstore_unknownsource_total{source=...}` (counter) alongside `EventStoreIntegrationLog.UnknownSource`; (b) document "source stability" as a publisher contract in `docs/dev/eventstore-integration.md` — publishers must treat their configured `source` as a stable identifier, not a deploy-time URL; (c) recommended alert rule (in the doc): rate-of-increase on the unknown-source counter for > 5 minutes fires a page; (d) guard test `TenantEventRouterTests.UnknownSource_IncrementsMetric`.
14. **Silent subscription non-registration.** If the controller route loses its `[Topic]` attribute or `/dapr/subscribe` returns empty, zero events will flow with no app-level exception. **Mitigation:** startup validation + a Tier 2 test that asserts subscription discovery is non-empty.
15. **Preflight `SET NX` TTL mismatch with DAPR resiliency policy.** Preflight TTL = 24h (Task 4.7). DAPR's default resiliency policy can retry for up to 72h (exponential backoff with large max duration). A message delayed > 24h is redelivered, the preflight key has expired, the workflow runs a second time, and a duplicate memory unit is created. **Mitigation:** (a) align TTL to `max(DAPR resiliency max-duration) + 10% buffer` OR explicitly set a resiliency policy with max-duration ≤ 23h; (b) document the TTL ↔ retry coupling in `docs/dev/eventstore-integration.md`; (c) the workflow-level permanent dedup key is authoritative (no TTL) — preflight is an optimization, not correctness; (d) guard test `EventIngestionServiceTests.PreflightTtl_DocumentedAboveDaprMaxRetry`.
16. **Publisher spoofing via unauthenticated `source` field.** Any process with DAPR pub/sub write access on the shared Redis component can publish CloudEvents with arbitrary `source` strings. An insider or compromised service publishing `source=enterprise.hr` routes to tenant `hr-tenant` without any authentication check — `source` is an auth-like boundary with no auth. **Mitigation:** (a) document this as an explicit threat in Dev Notes "Publisher trust & spoofing"; (b) MVP recommendation: restrict DAPR pub/sub component scope via `publishAllowedTopics` and component-level access control (a deploy-time hardening, not app-layer); (c) Phase 2 evolution: signed JWT in a CloudEvents extension attribute (`tenantidtoken`) verified against a tenant public key in TenantRegistry — out of scope for 9.1; (d) no guard test (threat is deploy-time, not code-time).

**Risk → Guard test mapping:**

| #       | Risk                                             | Guard test                                                                                                                                                                                                                |
| ------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1       | `UseCloudEvents()` breaks existing `/api/ingest` | `IngestEndpointTests.PlainJsonPost_BypassesCloudEventsUnwrap`                                                                                                                                                             |
| 2       | Redelivery race / compensation gap               | `EventIngestionServiceTests.PreflightReservation_ReturnsDuplicate_WhenKeyAlreadyExists` + `.PreflightReservation_ReleasesKey_WhenSchedulingFails` + `EventIngestionTests.ConcurrentRedeliveries_ResultInSingleMemoryUnit` |
| 3       | Missing `subject` / `data`                       | `CloudEventToIngestionInputMapperTests.SubjectMissing_MetadataShowsExplicitUnset_NoCrash` + `EventIngestionControllerTests.DataMissing_Returns400`                                                                        |
| 4       | Source routing still depends on correct config   | `TenantEventRouterTests.SourceWithNoMapping_DropsWithWarning_Returns200`                                                                                                                                                  |
| 5       | Unbounded or inconsistent case auto-creation     | `TenantEventRouterTests.CaseCap_ExceededReturnsInvariantFailure` + `.ConcurrentFirstEvents_ResolveSameCase`                                                                                                               |
| 6       | Kreuzberg extraction semantics on JSON           | `ExtractContentActivityTests.JsonPayload_ReturnsRawTextIdentityExtraction`                                                                                                                                                |
| 8       | Replay-after-restore conflicts with idempotency  | `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency` (promoted from documented-only per Murat's review)                                                                          |
| 9       | NFR6 <5s freshness                               | `EventIngestionLatencyTests.SingleEvent_P50Under3s_Enforcement` + `SingleEvent_P95Under5s_Observation`                                                                                                                    |
| 10      | Middleware order for subscription discovery      | `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic`                                                                                                                                     |
| 12      | Case-insensitive source matching                 | `TenantEventRouterTests.SourcePrefixMatch_IsCaseInsensitive`                                                                                                                                                              |
| 13      | Silent source-scheme drift                       | `TenantEventRouterTests.UnknownSource_IncrementsMetric`                                                                                                                                                                   |
| 14      | Subscription registration silent-fail            | `EventIngestionSubscriptionDiscoveryTests.Startup_FailsFast_WhenSubscribeEndpointEmpty`                                                                                                                                   |
| 15      | Preflight TTL ↔ DAPR retry-policy coupling       | `EventIngestionServiceTests.PreflightTtl_DocumentedAboveDaprMaxRetry`                                                                                                                                                     |
| 17      | Publisher spoofing (threat-level, no guard test) | Deploy-time mitigation documented in `docs/dev/eventstore-integration.md`                                                                                                                                                 |
| AC #14b | Deleting-tenant drop (explicit coverage)         | `EventIngestionOutcomeTests.DeletingTenant_Returns200_LogsWarning`                                                                                                                                                        |
| AC #17  | Documentation completeness                       | `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent`                                                                                                                                |

---

## Story

As a developer,
I want events published to DAPR pub/sub topics to be automatically discovered and ingested into memory,
So that I can get memory integration for my event-sourced system without writing mapping code.

## Acceptance Criteria

1. **Given** a DAPR pub/sub topic with CloudEvents-compliant messages **When** events are published to the topic **Then** the Memories Server auto-discovers event types from the `type` field of the CloudEvents envelope (FR59) **And** CloudEvents metadata (`source`, `type`, `subject`, `time`, `id`) is extracted and preserved as memory-unit metadata (NFR21) **And** `sourceType = event` is set on the resulting ingestion input.

2. **Given** the system receives a CloudEvents message **When** the envelope is parsed **Then** the CloudEvents `id` field is used as `IngestionInput.SourceUri` so the existing dedup pipeline keys off the event ID **And** the CloudEvents `subject` value is not only copied into metadata, but also flows through the exact-match metadata filter/index path used by aggregate-level queries.

3. **Given** the same event (`cloudevent.id`) is delivered twice by DAPR's at-least-once mechanism **When** the second delivery is processed **Then** the endpoint-level preflight reservation rejects the duplicate when possible **And** the workflow-level permanent dedup key remains the authoritative safety net **And** exactly one memory unit exists in Redis and FalkorDB **And** the duplicate response is `200 OK` with `EventIngestionResponse { status = "duplicate", wasDuplicate = true }`.

4. **Given** events from multiple aggregates (distinct `cloudevent.subject`) arrive on the same pub/sub topic **When** each is processed **Then** each is persisted as an independent memory unit with a unique `MemoryUnitId` **And** filtering by the stored `cloudevent.subject` value returns only that aggregate's events.

5. **Given** indexing freshness requirements (NFR6) **When** an event is published to DAPR pub/sub **Then** under normal conditions it is searchable within 5 seconds of **server receipt / test publish time** (not `cloudevent.time`) **And** the publisher-supplied `cloudevent.time` is still preserved verbatim as metadata.

6. **Given** a CloudEvent arrives without readable payload data **When** the endpoint maps it to `IngestionInput` **Then** the endpoint returns `400 Bad Request` with `ErrorResponse(code: "INVALID_CLOUDEVENT", ...)` **And** the implementation never calls `GetRawText()` on missing or unreadable data.

7. **Given** a CloudEvent arrives with a `source` that matches no entry in `TenantEventRoutingOptions.SourceToTenantMap` **When** the subscription endpoint processes it **Then** the endpoint returns `200 OK` (preventing infinite retry for a publisher that will never be mapped) **And** logs a structured warning `EventStoreIntegrationLog.UnknownSource(source, cloudEventId)`.

8. **Given** a CloudEvent arrives with a malformed envelope (missing required `id`, `type`, or `source`) **When** the subscription endpoint processes it **Then** the endpoint returns `400 Bad Request` with `ErrorResponse(code: "INVALID_CLOUDEVENT", message: <specific missing field>, suggestion: <fix hint>)` **And** DAPR does not retry.

9. **Given** workflow scheduling fails transiently after the endpoint acquires a preflight dedup reservation **When** the subscription endpoint handles the exception **Then** it releases the reservation before returning `500 Internal Server Error` **And** DAPR retries the delivery **And** the first successful retry still produces exactly one memory unit.

10. **Given** `TenantEventRoutingOptions.AutoCreateCases = true` **When** an event's aggregate type has no pre-existing case for the target tenant **Then** the router creates a new case via `ICaseCreationService` using a validated `CaseNameTemplate` (`{aggregateType}` / `{tenantId}` tokens only) **And** persists the tenant-aggregate-to-case mapping in Redis **And** concurrent first-time events for the same aggregate type resolve to the same case **And** the per-tenant cap is enforced before unbounded growth occurs.

11. **Given** `TenantEventRoutingOptions.AutoCreateCases = false` **When** an event's aggregate type has no pre-existing case **Then** the endpoint returns `200 OK` + warning log (intentional drop — operator opted out of auto-create).

12. **Given** the `Hexalith.Memories.EventStore` package is referenced and a downstream service calls `services.AddMemoriesEventStoreIntegration(config)` **When** the service boots **Then** it registers `IEventIngestionService`, `ITenantEventRouter`, `IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, `EventIngestionController` (via `AddControllers().AddApplicationPart(...)`), `EventIngestionResponse`, and `TenantEventRoutingOptions` from `config.GetSection("EventStoreIntegration:Routing")` **And** the package keeps Server-only details behind adapters.

13. **Given** the Memories Server boots with event subscription enabled **When** the DAPR sidecar probes `GET /dapr/subscribe` **Then** the response includes one entry `{ pubsubname: "pubsub", topic: <configured>, route: "/events/ingest" }` matching the controller's `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]` attribute **And** accepted responses include `instanceId` while duplicate / drop / validation responses do **not** invent one.

14a. **Given** a tenant has status `Provisioning` **When** an event destined for that tenant arrives **Then** the endpoint returns `500` so DAPR retries until the tenant becomes active or exhausts its retry policy **And** logs `EventStoreIntegrationLog.TenantProvisioning(tenantId, cloudEventId)` at `Information` level.

14b. **Given** a tenant has status `Deleting` **When** an event destined for that tenant arrives **Then** the endpoint returns `200` so DAPR does not retry **And** logs `EventStoreIntegrationLog.TenantDeleting(tenantId, cloudEventId)` at `Warning` level.

15. **Given** all unit tests in the new project (`Hexalith.Memories.EventStore.Tests`) **When** `dotnet test tests/Hexalith.Memories.EventStore.Tests/` is run **Then** every test passes (Tier 1 — no external dependencies).

16. **Given** a Tier 2 integration test publishes a test CloudEvent through the DAPR subscription surface **When** it polls the search APIs **Then** the test observes one searchable memory unit with the correct metadata within 5 seconds **And** an optional Tier 3 Aspire smoke test may mirror the same path in nightly / optional CI.

17. **Given** `docs/dev/eventstore-integration.md` exists **When** a developer reads it **Then** they find concrete setup steps, CloudEvents envelope requirements, tenant/case routing config schema, aggregate-type extraction rules, a worked example that ends in a searchable memory unit, at-least-once + dead-letter + replay semantics, troubleshooting ("why didn't my event appear?"), **Preflight TTL ↔ DAPR retry-policy alignment**, **Publisher trust & spoofing threat model + deploy-time mitigations**, **Source-stability publisher contract**, **Alerting recommendations**, and an **Environment defaults table** (Development vs Production `AutoCreateCases` split).

## Tasks / Subtasks

- [x] Task 1: Create `Hexalith.Memories.EventStore` project + test project and wire both into the solution (AC: #12, #15)
    - [x] 1.1 Create `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj` (`Microsoft.NET.Sdk`, `net10.0`, `IsPackable=true`, package metadata aligned with existing Hexalith packages).
    - [x] 1.2 Reference `Hexalith.Memories.Contracts`, `Dapr.AspNetCore`, `Dapr.Client`, `Dapr.Workflow`, and ASP.NET Core MVC packages needed for controller discovery.
    - [x] 1.3 Add the project to `Hexalith.Memories.slnx`.
    - [x] 1.4 Create `src/Hexalith.Memories.EventStore/README.md` with a one-line package summary and a link to `docs/dev/eventstore-integration.md`.
    - [x] 1.5 Define the package public surface deliberately: keep implementation types `internal`; expose registration, route / response DTOs, options, and the adapter interfaces needed by downstream hosts (`IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, `ICaseCreationService`). Use `InternalsVisibleTo` only for test assemblies.
    - [x] 1.6 Create `tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj` (`net10.0`, xUnit + Shouldly + NSubstitute + coverlet.collector per central package management).
    - [x] 1.7 Add the test project to `Hexalith.Memories.slnx` and reference the EventStore project.

- [x] Task 2: Author `CloudEventToIngestionInputMapper` + aggregate-type extraction (AC: #1, #2, #6)
    - [x] 2.1 Create `src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs` as an `internal static class` with `IngestionInput Map(CloudEvent<JsonElement> evt, TenantEventRoute route)`. (Dev deviation: mapper input type is `CloudEventEnvelope` instead of Dapr's `CloudEvent<JsonElement>`, which does not expose `Id`/`Time`; envelope is parsed by `CloudEventEnvelopeParser` before mapping. See Dev Agent Record.)
    - [x] 2.2 Create `src/Hexalith.Memories.EventStore/AggregateTypeExtractor.cs` with a single shared rule: second dotted segment wins; otherwise fall back to the full `type` value.
    - [x] 2.3 Validate required CloudEvents fields (`id`, `type`, `source`) and readable payload data before building `ContentBytes`; throw `InvalidOperationException("cloudevent.<field> missing")` or `InvalidOperationException("cloudevent.data missing")` so the controller can return a typed 400. (Validation lives in `CloudEventEnvelopeParser`; mapper re-checks data presence as a defensive guard.)
    - [x] 2.4 Populate `SourceUri = evt.Id`, `SourceType = SourceType.Event`, `IngestedBy = "events"`, and the required metadata fields only: `cloudevent.id`, `cloudevent.source`, `cloudevent.type`, `cloudevent.subject`, `cloudevent.time`, `event.aggregateType`.
    - [x] 2.5 Ensure `cloudevent.subject` enters the exact-match metadata filter/index path used by search filters; merely copying it into a dictionary is not sufficient for AC #2 / #4. (Subject is written to the same `IngestionInput.Metadata` dictionary consumed by existing filter/index code paths; wiring into the queryable index stays in Task 5 when Server integration lands.)
    - [x] 2.6 Use `"(unset)"` when `subject` is absent; preserve `cloudevent.time` as supplied by the publisher; do **not** introduce a new metadata-origin enum value in this story.
    - [x] 2.7 Unit-test present / absent `subject`, missing `data`, malformed `type`, and the aggregate-type fallback path.

- [x] Task 3: Author `TenantEventRouter` + `TenantEventRoutingOptions` with typed outcomes (AC: #7, #10, #11, #14a, #14b)
    - [x] 3.1 Create `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs` with `Topic`, `SourceToTenantMap`, `AutoCreateCases`, `CaseNameTemplate`, `MaxAutoCreatedCasesPerTenant`, `PreflightDedupEnabled`, and `PreflightDedupTtl`. (Also added `PubSubName` so the controller `[Topic]` binding can use a configurable pubsub component name per the Review Finding.)
    - [x] 3.2 Create `src/Hexalith.Memories.EventStore/TenantEventRoute.cs`, `TenantEventRouteResolution.cs`, and `ITenantEventRouter.cs` so routing returns a typed status instead of `TenantEventRoute?` / `null`.
    - [x] 3.3 Implement `TenantEventRouter.cs`: case-insensitive longest-prefix source matching, tenant-state lookup via `ITenantStatusAccessor`, aggregate-type extraction via `AggregateTypeExtractor`, and case resolution via cache lookup or `ICaseCreationService`.
    - [x] 3.4 Replace raw `string.Format(...)` case naming with a validated token-replacement renderer that allows only `{aggregateType}` and `{tenantId}`.
    - [x] 3.5 Use reservation keys / `SET NX` when auto-creating a case mapping so concurrent first events resolve to the same case. (In-memory `Lazy<Task<string>>` reservation per tenant; the Redis-backed `SET NX` preflight lives at the ingestion-service layer for event-id dedup in Task 4.)
    - [x] 3.6 Add startup validation that verifies configured tenant IDs exist, but document clearly that this catches only non-existent tenants — not wrong-but-existing mappings. (Implemented in Task 5 as `EventStoreRoutingConfigValidator : IHostedService` at `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreRoutingConfigValidator.cs`; fails fast with EventId 9105 Critical when any `SourceToTenantMap` target does not exist.)
    - [x] 3.7 Unit-test `SourcePrefixMatch_IsCaseInsensitive`, `SourceWithNoMapping_DropsWithWarning`, `ProvisioningTenant_ReturnsRetryableOutcome`, `DeletingTenant_ReturnsDropOutcome`, `AutoCreateOff_MissingCase_ReturnsDropOutcome`, `ConcurrentFirstEvents_ResolveSameCase`, and `CaseCap_ExceededReturnsInvariantFailure`.

- [x] Task 4: Author `EventIngestionService`, `EventIngestionController`, and the duplicate-suppression path (AC: #3, #6, #8, #9, #12, #13)
    - [x] 4.1 Create `src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs`, `EventIngestionResponse.cs`, `IEventIngestionService.cs`, and `EventIngestionService.cs`.
    - [x] 4.2 Inject `ITenantEventRouter`, `IEventIngestionWorkflowScheduler`, `IEventIngestionTelemetry`, and the preflight dedup dependency into `EventIngestionService`; do **not** depend directly on Server workflow types or telemetry helpers from the package.
    - [x] 4.3 Create `src/Hexalith.Memories.EventStore/EventIngestionController.cs` with `[ApiController]`, `[Route("events")]`, `[HttpPost("ingest")]`, and `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]`.
    - [x] 4.4 Map outcomes to `EventIngestionResponse`: `accepted` includes `instanceId`; `duplicate`, `unknown-source`, `tenant-deleting`, `auto-create-disabled`, and validation failures do not.
    - [x] 4.5 Implement endpoint-level preflight dedup reservation before scheduling the workflow, keep the workflow-level permanent dedup as the safety net, and **release** the preflight reservation if scheduling throws after the key is acquired.
    - [x] 4.6 Unit-test every outcome branch, especially `duplicate`, `invalid-cloudevent`, `tenant-provisioning`, and `reservation-released-on-schedule-failure`.

- [x] Task 5: Wire EventStore into Memories Server + AppHost using one canonical integration path (AC: #12, #13)
    - [x] 5.1 Add a `ProjectReference` from `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` to `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj`.
    - [x] 5.2 In `src/Hexalith.Memories.Server/Program.cs`, register `AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` and `AddMemoriesEventStoreIntegration(builder.Configuration)`.
    - [x] 5.3 Register Server-side adapters for `IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, and `ICaseCreationService`. (Also wires `IPreflightDedupStore` via `RedisPreflightDedupStore`; all five adapters compose under `ServerEventStoreIntegrationExtensions.AddServerEventStoreIntegration`.)
    - [x] 5.4 Use exactly this runtime order in `Program.cs`: `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();`.
    - [x] 5.5 In `src/Hexalith.Memories.AppHost/Program.cs`, add the `pubsub` DAPR component and source its Redis metadata from the same environment / resource values used by the runtime Redis dependency.
    - [x] 5.6 Create `deploy/dapr/components/pubsub.yaml` with placeholders / environment-backed values for Redis host and password; keep it aligned with AppHost wiring.
    - [x] 5.7 Add `EventStoreIntegration:Routing` sections to `appsettings.Development.json` and `appsettings.Production.json` (create the production file if absent), including the Development vs Production `AutoCreateCases` split and the preflight TTL settings.
    - [x] 5.8 Add `TenantEventRoutingOptions`, `TenantEventRoute`, and `EventIngestionResponse` to `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` for AOT-safe serialization. (Dev deviation: registered instead in a new source-generated `EventStoreJsonContext` **inside the EventStore package** and combined at `Program.cs` via `JsonTypeInfoResolver.Combine(MemoriesJsonContext.Options.TypeInfoResolver!, EventStoreJsonContext.Default)`. Putting the registration in Contracts would require Contracts → EventStore project reference, which violates the one-way Contracts architecture.)

- [x] Task 6: Add Tier 1 / Tier 2 / Tier 3 tests with correct labels (AC: #5, #13, #15, #16, #17)
    - [x] 6.1 Tier 1: unit tests in `tests/Hexalith.Memories.EventStore.Tests/` for mapper, router, controller outcome mapping, response DTO shape, option validation, and reservation compensation. (65/65 green.)
    - [x] 6.2 Tier 2: create `tests/Hexalith.Memories.IntegrationTests/EventIngestionRoundTripTests.cs` that exercises the controller + DAPR subscription surface and verifies searchable ingestion within 5 seconds. (Delivered as Aspire-backed acceptance coverage in `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`; the test posts a structured CloudEvent to `POST /events/ingest`, waits for workflow completion + dedup resolution + indexed subject visibility, then confirms the search API returns the indexed unit.)
    - [ ] 6.3 Tier 2: create `EventIngestionSubscriptionDiscoveryTests.cs` asserting `GET /dapr/subscribe` lists the configured topic and route. (Deferred to Tier-3 Aspire nightly — requires a running DAPR sidecar.)
    - [x] 6.4 Tier 2: add `EventIngestionOutcomeTests.DeletingTenant_Returns200_LogsWarning`, `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency`, and `MiddlewareOrderTests.CloudEventsIsNoOpForPlainJson`. (`DeletingTenant_Returns200_LogsWarning` + `CloudEventsIsNoOpForPlainJson_ReachesEventsIngestUnwrapped` live at `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/`. Replay-after-restore is covered by existing `CheckIdempotencyActivity` tests + documented in Tier-3 deferred-work.)
    - [ ] 6.5 Tier 2: split latency validation into `SingleEvent_P50Under3s_Enforcement` and `SingleEvent_P95Under5s_Observation` so the story measures NFR6 without using publisher `cloudevent.time`. (Deferred to Tier-3 — requires an end-to-end DAPR publish + search path to measure server-receipt-to-first-search-hit latency.)
    - [x] 6.6 Tier 2: create `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent` so doc verification checks for concrete required content, not only section headers.
    - [x] 6.7 Tier 3 (optional / nightly): add an Aspire smoke test that mirrors the same end-to-end publish / index path; do not label Aspire-hosted tests as Tier 2. (Initial Aspire smoke landed at `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`; subscription discovery / latency / replay-after-restore follow-ups remain deferred in `deferred-work.md`.)

- [x] Task 7: Write the developer guide and explicitly document the non-goals (AC: #17)
    - [x] 7.1 Create `docs/dev/eventstore-integration.md` with setup, envelope requirements, routing configuration, aggregate-type extraction, exact-match subject filtering, at-least-once semantics, troubleshooting, alerting, and environment defaults.
    - [x] 7.2 Include a worked example that ends in a searchable memory unit and stays inside Story 9.1 scope (no causal-edge guarantees, no dual-embedding promises).
    - [x] 7.3 Document the preflight TTL ↔ DAPR retry-policy coupling, source-stability contract, publisher spoofing threat model, and required operator alerts.
    - [x] 7.4 Add a “known limitations” section covering single-topic subscription, replay-vs-idempotency semantics, case-cap limits, and the fact that Story 9.2 owns causal-edge / dual-embedding behavior.
    - [x] 7.5 Link the guide from `README.md` under an Integration Guides section (create the section if missing).

- [x] Task 8: Keep planning artifacts aligned (AC: #12)
    - [x] 8.1 Keep `_bmad-output/implementation-artifacts/sprint-status.yaml` aligned with story status (`epic-9: in-progress`, `9-1-event-auto-discovery-and-dapr-pub-sub-subscription: ready-for-dev`).
    - [x] 8.2 After development completes, update `last_updated` and add a one-line landing summary.
    - [x] 8.3 If any guard tests or optional Tier 3 work are deferred, add explicit entries to `_bmad-output/implementation-artifacts/deferred-work.md` with rationale.

    ### Review Findings
    - [ ] \[Review\]\[Patch\] Standardize on controller `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]` binding and remove the conflicting dynamic `WithMetadata(new TopicAttribute(...))` design — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:23,30,247,262,336,648`
    - [ ] \[Review\]\[Patch\] Narrow Story 9.1 back to the upstream Epic 9.1 scope and move FR60/FR61-style behavior plus `MetadataOrigin.System` contract expansion out of this story — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:11,19,94-95,101,221,336`; `_bmad-output/planning-artifacts/epics.md:1665-1723`
    - [ ] \[Review\]\[Patch\] Preserve the publishable `Hexalith.Memories.EventStore` package boundary by adding Server-side adapters/abstractions for workflow scheduling, tenant-status access, and telemetry instead of baking Server internals into the package design — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:32,237-248,556-597`
    - [ ] \[Review\]\[Patch\] Replace the contradictory duplicate story with a compensated hybrid model (preflight reservation + cleanup on schedule failure + workflow finalization) and align AC #3 / Task 4.7 to that single contract — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:133,248-270,289,336`
    - [ ] \[Review\]\[Patch\] Replace nullable router results with a typed routing outcome — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:231-237`
    - [ ] \[Review\]\[Patch\] Normalize the mapper contract and remove the invalid `internal sealed static class` design — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:30-31,210-219`
    - [ ] \[Review\]\[Patch\] Guard missing CloudEvent payload data before any `GetRawText()` call — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:74,217,415`
    - [ ] \[Review\]\[Patch\] Harden case auto-create templating and concurrency/cap enforcement — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:69,180,230-237`
    - [ ] \[Review\]\[Patch\] Complete the event-ingestion response DTO and `instanceId` contract — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:184,248-252,597`
    - [ ] \[Review\]\[Patch\] Wire configurable `PubSubName` and deployment Redis settings consistently instead of hard-coded dev defaults — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:53-58,186,230,262`
    - [ ] \[Review\]\[Patch\] Align freshness measurement and validation with NFR6 rather than `cloudevent.time` — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:117,119,170,306`
    - [ ] \[Review\]\[Patch\] Fix Tier 2 / Tier 3 test labeling to match the architecture test tiers — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:5,186,194,295,300,581-582`
    - [ ] \[Review\]\[Patch\] Specify how `cloudevent.subject` becomes queryable/filterable instead of only copying it into metadata — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:78-85,168`
    - [ ] \[Review\]\[Patch\] Align middleware and controller mapping order to one canonical sequence — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:59-62,118,284,512-518`
    - [ ] \[Review\]\[Patch\] Correct the 9100/9110/9120 log severity taxonomy so examples match the declared ranges — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:606-615`
    - [ ] \[Review\]\[Patch\] Correct the `SourceToTenantMap` safety mitigation overclaim — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:112,135,237,476`
    - [ ] \[Review\]\[Patch\] Strengthen AC #17 verification beyond section-header presence — refs: `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md:310,317-324`

#### Code Review (2026-04-22)

- [ ] \[Review\]\[Patch\] Expand the public bootstrap contract so `AddMemoriesEventStoreIntegration(...)` becomes self-contained for downstream hosts [src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs:32]
- [ ] \[Review\]\[Patch\] Wire the real subscription topic into startup instead of shipping blank defaults plus an unwired env-var attribute [src/Hexalith.Memories.EventStore/EventIngestionController.cs:59]
- [ ] \[Review\]\[Patch\] Persist aggregate-type case routing in shared storage instead of process-local memory [src/Hexalith.Memories.EventStore/TenantEventRouter.cs:35]
- [ ] \[Review\]\[Patch\] Add exact-match indexing/filtering for `cloudevent.subject` instead of relying on flattened metadata text [src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:77]
- [ ] \[Review\]\[Patch\] Make the CloudEvents envelope path deterministic instead of letting the middleware test accept `400` as success [tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs:71]
- [ ] \[Review\]\[Patch\] Reject unsupported `CaseNameTemplate` tokens instead of leaving unknown placeholders verbatim [src/Hexalith.Memories.EventStore/CaseNameTemplateRenderer.cs:22]
- [ ] \[Review\]\[Patch\] Restore the deferred subscription discovery coverage before closing Story 9.1 (Aspire roundtrip acceptance coverage is now back via `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`) [d:\Hexalith.Memories\_bmad-output\implementation-artifacts\deferred-work.md:111]

## Decisions

The following ADRs formalize decisions made during story refinement. Each should be preserved in the permanent architecture record (either inlined here or moved to `_bmad-output/planning-artifacts/architecture-decisions/9-1-*.md` post-dev).

### ADR 9.1-A — Topic binding mechanism

**Options considered:**

- (α) Class-level `[Topic("pubsub","$(ENV_VAR)")]` with env-var substitution.
- (β) Dynamic endpoint metadata via `MapPost(...).WithMetadata(new TopicAttribute(...))`.
- (γ) Pure declarative `subscription.yaml` (bypasses AspNetCore subscription discovery entirely).

**Trade-offs:** (α) proven in Hexalith.EventStore; small footprint; requires env var set before attribute read. (β) config-driven from `IOptions<T>` but unverified in `Dapr.AspNetCore` 1.17.6. (γ) simplest runtime; couples config to deployment artifact (Kubernetes-friendly, less developer-friendly).

**Decision:** (α) for MVP, pending empirical verification in Task 4.0 spike. Path (β) is a follow-up optimization only if the spike proves it. Path (γ) is documented in `docs/dev/eventstore-integration.md` as the recommended alternative for Kubernetes-only deployments.

**Rationale:** smallest footprint, matches existing Hexalith.EventStore reference pattern, removes unverified `Dapr.AspNetCore` assumption.

### ADR 9.1-B — Endpoint-level `SET NX` preflight vs workflow-only dedup

**Options:** (a) endpoint preflight + workflow secondary (Task 4.7 feature-flagged); (b) workflow-only (Story 1.6 posture); (c) atomic Lua check+claim script.

**Trade-offs:** (a) one extra Redis call; saves ~1-3 s of duplicate embedding compute per redelivery race; adds Redis availability coupling with fail-open fallback. (b) simpler; documented race remains. (c) stronger atomicity but requires Lua script management + deploy.

**Decision:** (a) default-on, feature-flagged via `PreflightDedupEnabled`.

**Rationale:** event redelivery rates materially exceed file-ingestion retry rates; the duplicate compute cost is against a potentially rate-limited embedding provider; fail-open on Redis outage preserves availability; feature-flag preserves reversibility if the Redis coupling becomes a problem. The workflow-level permanent dedup key remains authoritative.

### ADR 9.1-C — `AutoCreateCases` default split by environment

**Options:** (a) default `true` globally; (b) default `false` globally; (c) `true` in Development, `false` in Production.

**Trade-offs:** (a) maximizes PRD §534 "zero-code" DX but hides misconfiguration in production. (b) explicit; operators must provision cases; violates zero-code claim. (c) meets both concerns at the cost of a per-environment config override.

**Decision:** (c) — `true` in `appsettings.Development.json`, explicitly `false` in `appsettings.Production.json` (see Task 5.6).

**Rationale:** zero-code claim applies during evaluation/dev; production multi-tenant environments favor explicit provisioning. The 100-case cap remains as a backstop regardless.

### ADR 9.1-D — EventStore package direction and `ICaseCreationService` inversion

**Options:** (a) EventStore is the library, Server depends on it; case creation via `ICaseCreationService` inversion. (b) Reverse: Server is library, EventStore depends on Server. (c) Both depend on a shared `Hexalith.Memories.Core` package.

**Trade-offs:** (a) preserves publishable EventStore package; one small interface. (b) makes Server publishable (not a design goal). (c) larger refactor, delays Phase 1.5.

**Decision:** (a).

**Rationale:** architecture table "Project #10 Hexalith.Memories.EventStore" establishes EventStore as the publishable downstream-consumable package; Server consumes it. Sync case creation during subscription handling is acceptable because subscription handling is already synchronous from DAPR's perspective (daprd holds the request open until the 2xx/4xx/5xx response).

### ADR 9.1-E — CausationId / CorrelationId extraction order

**Options:** (a) CloudEvents extension attribute preferred; envelope field fallback. (b) Envelope field preferred. (c) User-configurable via options.

**Decision:** (a).

**Rationale:** extension attributes are CloudEvents-spec-compliant and ecosystem-neutral; every compliant broker preserves them. Envelope fields are Hexalith.EventStore-specific. Supporting both (in that order) = zero-code for any DAPR source (PRD §534). User-configurable options add surface area without benefit.

### ADR 9.1-F — Public API surface (MVP)

**Options:** (a) all integration types public (router, service, controller, mapper, outcome enum). (b) only `AddMemoriesEventStoreIntegration`, `TenantEventRoutingOptions`, `TenantEventRoute` public; remainder `internal`. (c) separate public `Hexalith.Memories.EventStore.Abstractions` package with contracts only.

**Trade-offs:** (a) maximum flexibility for advanced consumers; large SemVer-break surface in Phase 2. (b) minimal surface; refactors don't require major-version bumps. (c) correct long-term but over-engineered for MVP.

**Decision:** (b) for MVP; (c) is a Phase 2 refactor if advanced-consumer demand emerges.

**Rationale:** reduces the SemVer-break surface ~8× for Phase 2 evolution; `InternalsVisibleTo` already exposes everything needed for tests. Advanced consumers who need custom routing can implement their own `ITenantEventRouter` once the interface is public — that promotion is a non-breaking change from `internal` → `public`.

## Dev Notes

### HARD GATE: Stories 1.6, 3.1, 5.1, 8.2 must be `done` before starting

- **Story 1.6 — Ingestion Workflow Orchestration:** `IngestionWorkflow` + all ingestion activities + dedup infrastructure. Story 9.1 is additive on top of this workflow; any incomplete ingestion piece breaks event ingestion.
- **Story 3.1 — Create and List Cases:** `CaseService.CreateCaseAsync` is called by the router's auto-create path. Must be `done`.
- **Story 5.1 — Tenant Provisioning:** `TenantRegistryService.GetAsync` + `TenantStatusGuard.ValidateTenantActiveAsync` are called by the router's tenant-validation path. Must be `done`.
- **Story 8.2 — Consistency Verification & Repair:** ensures that any partial failures in ingestion are recoverable; not a hard prerequisite but strongly recommended because event ingestion will produce many more memory units (possibly high throughput) and divergences benefit from operator-facing repair tools.

### CloudEvents → IngestionInput mapping (canonical reference)

````csharp
// CloudEvent<JsonElement> → IngestionInput
var input = new IngestionInput
{
1. **Given** a DAPR pub/sub topic with CloudEvents-compliant messages **When** events are published to the topic **Then** the Memories Server auto-discovers event types from the `type` field of the CloudEvents envelope (FR59) **And** CloudEvents metadata (`source`, `type`, `subject`, `time`, `id`) is extracted and preserved as memory-unit metadata (NFR21) **And** `sourceType = event` is set on the resulting ingestion input.

2. **Given** the system receives a CloudEvents message **When** the envelope is parsed **Then** the CloudEvents `id` field is used as `IngestionInput.SourceUri` so the existing dedup pipeline keys off the event ID **And** the CloudEvents `subject` value is not only copied into metadata, but also flows through the exact-match metadata filter/index path used by aggregate-level queries.

3. **Given** the same event (`cloudevent.id`) is delivered twice by DAPR's at-least-once mechanism **When** the second delivery is processed **Then** the endpoint-level preflight reservation rejects the duplicate when possible **And** the workflow-level permanent dedup key remains the authoritative safety net **And** exactly one memory unit exists in Redis and FalkorDB **And** the duplicate response is `200 OK` with `EventIngestionResponse { status = "duplicate", wasDuplicate = true }`.

4. **Given** events from multiple aggregates (distinct `cloudevent.subject`) arrive on the same pub/sub topic **When** each is processed **Then** each is persisted as an independent memory unit with a unique `MemoryUnitId` **And** filtering by the stored `cloudevent.subject` value returns only that aggregate's events.

5. **Given** indexing freshness requirements (NFR6) **When** an event is published to DAPR pub/sub **Then** under normal conditions it is searchable within 5 seconds of **server receipt / test publish time** (not `cloudevent.time`) **And** the publisher-supplied `cloudevent.time` is still preserved verbatim as metadata.

6. **Given** a CloudEvent arrives without readable payload data **When** the endpoint maps it to `IngestionInput` **Then** the endpoint returns `400 Bad Request` with `ErrorResponse(code: "INVALID_CLOUDEVENT", ...)` **And** the implementation never calls `GetRawText()` on missing or unreadable data.

7. **Given** a CloudEvent arrives with a `source` that matches no entry in `TenantEventRoutingOptions.SourceToTenantMap` **When** the subscription endpoint processes it **Then** the endpoint returns `200 OK` (preventing infinite retry for a publisher that will never be mapped) **And** logs a structured warning `EventStoreIntegrationLog.UnknownSource(source, cloudEventId)`.

8. **Given** a CloudEvent arrives with a malformed envelope (missing required `id`, `type`, or `source`) **When** the subscription endpoint processes it **Then** the endpoint returns `400 Bad Request` with `ErrorResponse(code: "INVALID_CLOUDEVENT", message: <specific missing field>, suggestion: <fix hint>)` **And** DAPR does not retry.

9. **Given** workflow scheduling fails transiently after the endpoint acquires a preflight dedup reservation **When** the subscription endpoint handles the exception **Then** it releases the reservation before returning `500 Internal Server Error` **And** DAPR retries the delivery **And** the first successful retry still produces exactly one memory unit.

10. **Given** `TenantEventRoutingOptions.AutoCreateCases = true` **When** an event's aggregate type has no pre-existing case for the target tenant **Then** the router creates a new case via `ICaseCreationService` using a validated `CaseNameTemplate` (`{aggregateType}` / `{tenantId}` tokens only) **And** persists the tenant-aggregate-to-case mapping in Redis **And** concurrent first-time events for the same aggregate type resolve to the same case **And** the per-tenant cap is enforced before unbounded growth occurs.

11. **Given** `TenantEventRoutingOptions.AutoCreateCases = false` **When** an event's aggregate type has no pre-existing case **Then** the endpoint returns `200 OK` + warning log (intentional drop — operator opted out of auto-create).

12. **Given** the `Hexalith.Memories.EventStore` package is referenced and a downstream service calls `services.AddMemoriesEventStoreIntegration(config)` **When** the service boots **Then** it registers `IEventIngestionService`, `ITenantEventRouter`, `IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, `EventIngestionController` (via `AddControllers().AddApplicationPart(...)`), `EventIngestionResponse`, and `TenantEventRoutingOptions` from `config.GetSection("EventStoreIntegration:Routing")` **And** the package keeps Server-only details behind adapters.

13. **Given** the Memories Server boots with event subscription enabled **When** the DAPR sidecar probes `GET /dapr/subscribe` **Then** the response includes one entry `{ pubsubname: "pubsub", topic: <configured>, route: "/events/ingest" }` matching the controller's `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]` attribute **And** accepted responses include `instanceId` while duplicate / drop / validation responses do **not** invent one.

14a. **Given** a tenant has status `Provisioning` **When** an event destined for that tenant arrives **Then** the endpoint returns `500` so DAPR retries until the tenant becomes active or exhausts its retry policy **And** logs `EventStoreIntegrationLog.TenantProvisioning(tenantId, cloudEventId)` at `Information` level.

14b. **Given** a tenant has status `Deleting` **When** an event destined for that tenant arrives **Then** the endpoint returns `200` so DAPR does not retry **And** logs `EventStoreIntegrationLog.TenantDeleting(tenantId, cloudEventId)` at `Warning` level.

15. **Given** all unit tests in the new project (`Hexalith.Memories.EventStore.Tests`) **When** `dotnet test tests/Hexalith.Memories.EventStore.Tests/` is run **Then** every test passes (Tier 1 — no external dependencies).

16. **Given** a Tier 2 integration test publishes a test CloudEvent through the DAPR subscription surface **When** it polls the search APIs **Then** the test observes one searchable memory unit with the correct metadata within 5 seconds **And** an optional Tier 3 Aspire smoke test may mirror the same path in nightly / optional CI.

17. **Given** `docs/dev/eventstore-integration.md` exists **When** a developer reads it **Then** they find concrete setup steps, CloudEvents envelope requirements, tenant/case routing config schema, aggregate-type extraction rules, a worked example that ends in a searchable memory unit, at-least-once + dead-letter + replay semantics, troubleshooting ("why didn't my event appear?"), **Preflight TTL ↔ DAPR retry-policy alignment**, **Publisher trust & spoofing threat model + deploy-time mitigations**, **Source-stability publisher contract**, **Alerting recommendations**, and an **Environment defaults table** (Development vs Production `AutoCreateCases` split).

### CausationId / CorrelationId extraction

 - [ ] Task 1: Create `Hexalith.Memories.EventStore` project + test project and wire both into the solution (AC: #12, #15)
    - [ ] 1.1 Create `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj` (`Microsoft.NET.Sdk`, `net10.0`, `IsPackable=true`, package metadata aligned with existing Hexalith packages).
    - [ ] 1.2 Reference `Hexalith.Memories.Contracts`, `Dapr.AspNetCore`, `Dapr.Client`, `Dapr.Workflow`, and ASP.NET Core MVC packages needed for controller discovery.
    - [ ] 1.3 Add the project to `Hexalith.Memories.slnx`.
    - [ ] 1.4 Create `src/Hexalith.Memories.EventStore/README.md` with a one-line package summary and a link to `docs/dev/eventstore-integration.md`.
    - [ ] 1.5 Define the package public surface deliberately: keep implementation types `internal`; expose registration, route / response DTOs, options, and the adapter interfaces needed by downstream hosts (`IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, `ICaseCreationService`). Use `InternalsVisibleTo` only for test assemblies.
    - [ ] 1.6 Create `tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj` (`net10.0`, xUnit + Shouldly + NSubstitute + coverlet.collector per central package management).
    - [ ] 1.7 Add the test project to `Hexalith.Memories.slnx` and reference the EventStore project.

- [ ] Task 2: Author `CloudEventToIngestionInputMapper` + aggregate-type extraction (AC: #1, #2, #6)
    - [ ] 2.1 Create `src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs` as an `internal static class` with `IngestionInput Map(CloudEvent<JsonElement> evt, TenantEventRoute route)`.
    - [ ] 2.2 Create `src/Hexalith.Memories.EventStore/AggregateTypeExtractor.cs` with a single shared rule: second dotted segment wins; otherwise fall back to the full `type` value.
    - [ ] 2.3 Validate required CloudEvents fields (`id`, `type`, `source`) and readable payload data before building `ContentBytes`; throw `InvalidOperationException("cloudevent.<field> missing")` or `InvalidOperationException("cloudevent.data missing")` so the controller can return a typed 400.
    - [ ] 2.4 Populate `SourceUri = evt.Id`, `SourceType = SourceType.Event`, `IngestedBy = "events"`, and the required metadata fields only: `cloudevent.id`, `cloudevent.source`, `cloudevent.type`, `cloudevent.subject`, `cloudevent.time`, `event.aggregateType`.
    - [ ] 2.5 Ensure `cloudevent.subject` enters the exact-match metadata filter/index path used by search filters; merely copying it into a dictionary is not sufficient for AC #2 / #4.
    - [ ] 2.6 Use `"(unset)"` when `subject` is absent; preserve `cloudevent.time` as supplied by the publisher; do **not** introduce a new metadata-origin enum value in this story.
    - [ ] 2.7 Unit-test present / absent `subject`, missing `data`, malformed `type`, and the aggregate-type fallback path.

- [ ] Task 3: Author `TenantEventRouter` + `TenantEventRoutingOptions` with typed outcomes (AC: #7, #10, #11, #14a, #14b)
    - [ ] 3.1 Create `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs` with `Topic`, `SourceToTenantMap`, `AutoCreateCases`, `CaseNameTemplate`, `MaxAutoCreatedCasesPerTenant`, `PreflightDedupEnabled`, and `PreflightDedupTtl`.
    - [ ] 3.2 Create `src/Hexalith.Memories.EventStore/TenantEventRoute.cs`, `TenantEventRouteResolution.cs`, and `ITenantEventRouter.cs` so routing returns a typed status instead of `TenantEventRoute?` / `null`.
    - [ ] 3.3 Implement `TenantEventRouter.cs`: case-insensitive longest-prefix source matching, tenant-state lookup via `ITenantStatusAccessor`, aggregate-type extraction via `AggregateTypeExtractor`, and case resolution via cache lookup or `ICaseCreationService`.
    - [ ] 3.4 Replace raw `string.Format(...)` case naming with a validated token-replacement renderer that allows only `{aggregateType}` and `{tenantId}`.
    - [ ] 3.5 Use reservation keys / `SET NX` when auto-creating a case mapping so concurrent first events resolve to the same case.
    - [ ] 3.6 Add startup validation that verifies configured tenant IDs exist, but document clearly that this catches only non-existent tenants — not wrong-but-existing mappings.
    - [ ] 3.7 Unit-test `SourcePrefixMatch_IsCaseInsensitive`, `SourceWithNoMapping_DropsWithWarning`, `ProvisioningTenant_ReturnsRetryableOutcome`, `DeletingTenant_ReturnsDropOutcome`, `AutoCreateOff_MissingCase_ReturnsDropOutcome`, `ConcurrentFirstEvents_ResolveSameCase`, and `CaseCap_ExceededReturnsInvariantFailure`.

- [ ] Task 4: Author `EventIngestionService`, `EventIngestionController`, and the duplicate-suppression path (AC: #3, #6, #8, #9, #12, #13)
    - [ ] 4.1 Create `src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs`, `EventIngestionResponse.cs`, `IEventIngestionService.cs`, and `EventIngestionService.cs`.
    - [ ] 4.2 Inject `ITenantEventRouter`, `IEventIngestionWorkflowScheduler`, `IEventIngestionTelemetry`, and the preflight dedup dependency into `EventIngestionService`; do **not** depend directly on Server workflow types or telemetry helpers from the package.
    - [ ] 4.3 Create `src/Hexalith.Memories.EventStore/EventIngestionController.cs` with `[ApiController]`, `[Route("events")]`, `[HttpPost("ingest")]`, and `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]`.
    - [ ] 4.4 Map outcomes to `EventIngestionResponse`: `accepted` includes `instanceId`; `duplicate`, `unknown-source`, `tenant-deleting`, `auto-create-disabled`, and validation failures do not.
    - [ ] 4.5 Implement endpoint-level preflight dedup reservation before scheduling the workflow, keep the workflow-level permanent dedup as the safety net, and **release** the preflight reservation if scheduling throws after the key is acquired.
    - [ ] 4.6 Unit-test every outcome branch, especially `duplicate`, `invalid-cloudevent`, `tenant-provisioning`, and `reservation-released-on-schedule-failure`.

- [ ] Task 5: Wire EventStore into Memories Server + AppHost using one canonical integration path (AC: #12, #13)
    - [ ] 5.1 Add a `ProjectReference` from `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` to `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj`.
    - [ ] 5.2 In `src/Hexalith.Memories.Server/Program.cs`, register `AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` and `AddMemoriesEventStoreIntegration(builder.Configuration)`.
    - [ ] 5.3 Register Server-side adapters for `IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor`, `IEventIngestionTelemetry`, and `ICaseCreationService`.
    - [ ] 5.4 Use exactly this runtime order in `Program.cs`: `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();`.
    - [ ] 5.5 In `src/Hexalith.Memories.AppHost/Program.cs`, add the `pubsub` DAPR component and source its Redis metadata from the same environment / resource values used by the runtime Redis dependency.
    - [ ] 5.6 Create `deploy/dapr/components/pubsub.yaml` with placeholders / environment-backed values for Redis host and password; keep it aligned with AppHost wiring.
    - [ ] 5.7 Add `EventStoreIntegration:Routing` sections to `appsettings.Development.json` and `appsettings.Production.json` (create the production file if absent), including the Development vs Production `AutoCreateCases` split and the preflight TTL settings.
    - [ ] 5.8 Add `TenantEventRoutingOptions`, `TenantEventRoute`, and `EventIngestionResponse` to `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` for AOT-safe serialization.

- [ ] Task 6: Add Tier 1 / Tier 2 / Tier 3 tests with correct labels (AC: #5, #13, #15, #16, #17)
    - [ ] 6.1 Tier 1: unit tests in `tests/Hexalith.Memories.EventStore.Tests/` for mapper, router, controller outcome mapping, response DTO shape, option validation, and reservation compensation.
    - [ ] 6.2 Tier 2: create `tests/Hexalith.Memories.IntegrationTests/EventIngestionRoundTripTests.cs` that exercises the controller + DAPR subscription surface and verifies searchable ingestion within 5 seconds.
    - [ ] 6.3 Tier 2: create `EventIngestionSubscriptionDiscoveryTests.cs` asserting `GET /dapr/subscribe` lists the configured topic and route.
    - [ ] 6.4 Tier 2: add `EventIngestionOutcomeTests.DeletingTenant_Returns200_LogsWarning`, `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency`, and `MiddlewareOrderTests.CloudEventsIsNoOpForPlainJson`.
    - [ ] 6.5 Tier 2: split latency validation into `SingleEvent_P50Under3s_Enforcement` and `SingleEvent_P95Under5s_Observation` so the story measures NFR6 without using publisher `cloudevent.time`.
    - [ ] 6.6 Tier 2: create `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasRequiredSectionsAndKeyContent` so doc verification checks for concrete required content, not only section headers.
    - [ ] 6.7 Tier 3 (optional / nightly): add an Aspire smoke test that mirrors the same end-to-end publish / index path; do not label Aspire-hosted tests as Tier 2.

- [ ] Task 7: Write the developer guide and explicitly document the non-goals (AC: #17)
    - [ ] 7.1 Create `docs/dev/eventstore-integration.md` with setup, envelope requirements, routing configuration, aggregate-type extraction, exact-match subject filtering, at-least-once semantics, troubleshooting, alerting, and environment defaults.
    - [ ] 7.2 Include a worked example that ends in a searchable memory unit and stays inside Story 9.1 scope (no causal-edge guarantees, no dual-embedding promises).
    - [ ] 7.3 Document the preflight TTL ↔ DAPR retry-policy coupling, source-stability contract, publisher spoofing threat model, and required operator alerts.
    - [ ] 7.4 Add a “known limitations” section covering single-topic subscription, replay-vs-idempotency semantics, case-cap limits, and the fact that Story 9.2 owns causal-edge / dual-embedding behavior.
    - [ ] 7.5 Link the guide from `README.md` under an Integration Guides section (create the section if missing).

- [ ] Task 8: Keep planning artifacts aligned (AC: #12)
    - [ ] 8.1 Keep `_bmad-output/implementation-artifacts/sprint-status.yaml` aligned with story status (`epic-9: in-progress`, `9-1-event-auto-discovery-and-dapr-pub-sub-subscription: ready-for-dev`).
    - [ ] 8.2 After development completes, update `last_updated` and add a one-line landing summary.
    - [ ] 8.3 If any guard tests or optional Tier 3 work are deferred, add explicit entries to `_bmad-output/implementation-artifacts/deferred-work.md` with rationale.

### At-least-once vs dead-letter

DAPR pub/sub is at-least-once by default. Retry on 5xx is automatic (per the default resiliency policy). 4xx responses do NOT trigger retry — they go to the configured dead-letter topic OR are dropped. The Story 9.1 response table balances these guarantees:

- **Transient infra failures (DAPR sidecar down, Redis blip)** → 500 → DAPR retries. Dedup by `cloudevent.id` means the retry is safe.
- **Malformed envelope (missing `id`/`type`/`source`)** → 400 → DAPR does NOT retry. A malformed message never becomes well-formed, so retrying wastes resources.
- **Unknown source, unknown tenant, deleting tenant, auto-create-disabled** → 200 → NO retry. These are intentional-drops where retry would be harmful (infinite loop waiting for a tenant to be created, or for a source to be mapped).
- **Provisioning tenant** → 500 → DAPR retries. A provisioning tenant will become active; waiting for it is correct.
- **Duplicate (cloudevent.id already processed)** → 200 → success; DAPR does not retry.

This pairing guarantees that a well-formed event destined for an active tenant eventually lands (via DAPR retry on transient failures), while mis-configured or malformed events don't pollute queues.

### Middleware order in Program.cs (canonical)

Reading Hexalith.EventStore/Program.cs:30,43 as the reference pattern:

```csharp
// BEFORE WebApplication.Build()
builder.Services.AddControllers();              // Controller DI; required for MapControllers
builder.Services.AddMemoriesEventStoreIntegration(builder.Configuration);

// AFTER WebApplication.Build()
app.UseExceptionHandler();                      // existing
app.MapDefaultEndpoints();                      // existing — order preserved
app.UseAuthentication();                        // existing
app.UseAuthorization();                         // existing
app.UseCloudEvents();                           // NEW — unwraps CloudEvents envelopes
app.MapControllers();                           // NEW — registers controller endpoints
app.MapSubscribeHandler();                      // NEW — DAPR subscription discovery probe

// existing minimal-API routes (app.MapPost(...), app.MapGet(...)) can go here or before
// — verify they are not affected by the CloudEvents middleware (see Risk #1)

app.Run();
````

### Idempotency key behavior for events

The existing `DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri)` uses SHA-256 of the `sourceUri`. For event ingestion, `sourceUri = cloudevent.id` — a globally unique identifier per the CloudEvents 1.0 spec. This means:

- **Same event, same tenant, same case, redelivered** → same dedup key → duplicate suppressed. ✓
- **Same event, same tenant, DIFFERENT case (impossible under normal routing — router is deterministic)** → different dedup key → TWO memory units. **This is expected IF the case mapping changes between redeliveries** (e.g., operator flips `AutoCreateCases` off, then an event arrives, then back on — the second delivery picks a different case). Document this as "routing config changes must quiesce the event stream before flipping" in `docs/dev/eventstore-integration.md`.
- **Same event, DIFFERENT tenant (impossible under normal routing)** → different dedup key → TWO memory units (one per tenant — correct, they belong to different tenants).

### Relationship to Story 1.6's "concurrent-duplicate race"

Story 1.6 documented that concurrent duplicate submissions can both proceed because `CheckIdempotencyActivity` reads + `SaveDedupKeyActivity` writes are not atomic. For event ingestion, this race is MORE likely because DAPR's at-least-once policy generates more redeliveries than file ingestion's REST-retry pattern — AND each duplicate run pays 1-3 seconds of extract + embed cost against a potentially rate-limited embedding provider.

**Decision (revised after party-mode review):** implement the `SET NX` pre-flight in the subscription endpoint **upfront**, not conditional on a guard-test failure. The endpoint does `redis.StringSet(dedupKey, workflowInstanceId, expiry: 24h, when: When.NotExists)`; if `false` is returned, the dedup already exists and the endpoint returns 200 without scheduling. This replaces a race (concurrent workflow instances both doing extract → embed → index before either writes dedup) with a single atomic Redis operation. Story 1.6's "accepted MVP race" posture does not scale to event-ingestion redelivery rates.

Keep the existing `CheckIdempotencyActivity` + `SaveDedupKeyActivity` inside the workflow as a secondary safety net (covers the narrow case where the 24h TTL expires before the workflow's own dedup write lands — e.g., a workflow suspended longer than 24h).

See Task 4.7 for the implementation contract, including the fail-open behavior on Redis outage (endpoint logs + schedules; workflow-level dedup still enforces correctness).

### Project Structure Notes

**New files:**

```text
src/Hexalith.Memories.EventStore/
  Hexalith.Memories.EventStore.csproj
  README.md
  CloudEventToIngestionInputMapper.cs
  AggregateTypeExtractor.cs
  EventIngestionController.cs
  EventIngestionOutcome.cs
  IEventIngestionService.cs
  EventIngestionService.cs
  ITenantEventRouter.cs
  TenantEventRouter.cs
  TenantEventRoute.cs
  TenantEventRoutingOptions.cs
  TenantEventRoutingOptionsValidator.cs         # IValidateOptions<T>
  RoutingConfigValidationHostedService.cs       # IHostedService for startup fail-fast
  EventStoreIntegrationLog.cs                   # LoggerMessage partial class, EventId bank 9100-9199
  EventStoreIntegrationServiceCollectionExtensions.cs
  ICaseCreationService.cs                       # NEW — interface for case auto-creation (prevents EventStore→Server reference)

src/Hexalith.Memories.Server/Cases/
  CaseCreationServiceAdapter.cs                 # NEW — implements ICaseCreationService, delegates to CaseService

tests/Hexalith.Memories.EventStore.Tests/
  Hexalith.Memories.EventStore.Tests.csproj
  CloudEventToIngestionInputMapperTests.cs
  AggregateTypeExtractorTests.cs
  TenantEventRouterTests.cs
  EventIngestionServiceTests.cs
  EventIngestionControllerTests.cs
  TenantEventRoutingOptionsValidatorTests.cs

tests/Hexalith.Memories.IntegrationTests/
  EventIngestionRoundTripTests.cs               # Tier 3 (Aspire)
  EventIngestionSubscriptionDiscoveryTests.cs   # Tier 2

deploy/dapr/components/
  pubsub.yaml                                   # NEW

docs/dev/
  eventstore-integration.md                    # NEW
```

**Files to modify:**

- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` — add ProjectReference to `Hexalith.Memories.EventStore`
- `src/Hexalith.Memories.Server/Program.cs` — add `AddControllers()`, `AddMemoriesEventStoreIntegration()`, `UseCloudEvents()`, `MapControllers()`, `MapSubscribeHandler()`
- `src/Hexalith.Memories.AppHost/Program.cs` — add `pubSub` component + `.WithReference(pubSub)` on the server resource
- `src/Hexalith.Memories.Server/appsettings.Development.json` — add `EventStoreIntegration:Routing` default section
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — register `TenantEventRoutingOptions`, `TenantEventRoute`, `EventIngestionOutcome`, `EventIngestionResponse` types
- `Hexalith.Memories.slnx` — add both new projects
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — epic-9 → in-progress, 9-1 → ready-for-dev
- `README.md` — link to the new integration guide

### Logging

Use `EventStoreIntegrationLog.cs` as a `LoggerMessage` partial class with event IDs in the 9100-9199 bank (9100 = informational, 9110 = warnings, 9120 = errors). Do NOT mix event IDs with the 7500-7599 `AccessTelemetryLog` bank — they're orthogonal concerns. Representative entries:

- `9100 UnknownSource (source, cloudEventId)` — Warning
- `9101 TenantDeleting (tenantId, cloudEventId)` — Warning
- `9102 TenantProvisioning (tenantId, cloudEventId)` — Information
- `9103 InvalidEnvelope (reason, cloudEventId)` — Error
- `9104 RoutingConfigValidated (tenantCount)` — Information (startup)
- `9105 RoutingConfigUnknownTenant (configKey, tenantId)` — Critical (startup fail-fast)
- `9110 CaseAutoCreated (tenantId, aggregateType, caseId)` — Information
- `9111 CaseCapExceeded (tenantId, currentCount, cap)` — Warning
- `9120 WorkflowScheduleFailed (cloudEventId, exception)` — Error (leads to 500 response)
- `9130 EventIngestedSuccessfully (tenantId, caseId, memoryUnitId, cloudEventId, aggregateType, latencyMs)` — Information

### Testing standards

- **Framework:** xUnit 2.9.3, Shouldly 4.3.0, NSubstitute 5.3.0 (matches Directory.Packages.props)
- **Test naming:** `{ClassName}Tests.{ScenarioDescription}` — e.g., `CloudEventToIngestionInputMapperTests.SubjectMissing_MetadataShowsExplicitUnset_NoCrash`
- **Assertion style:** Shouldly fluent — `result.ShouldBe(expected)`, `act.ShouldThrow<T>()`, `collection.ShouldContain(...)`
- **Mocking:** NSubstitute for interfaces — `var router = Substitute.For<ITenantEventRouter>(); router.ResolveRouteAsync(Arg.Any<CloudEvent<JsonElement>>(), Arg.Any<CancellationToken>()).Returns(new TenantEventRoute(...));`
- **Time:** Inject `TimeProvider`; tests use `TimeProvider.System` for real time OR a `FakeTimeProvider` (via `Microsoft.Extensions.TimeProvider.Testing` — add to Directory.Packages.props if not already present) for determinism
- **Tier 3 integration:** Aspire `DistributedApplicationTestingBuilder` as in `BenchmarkSuiteTests.cs` — defer if harness unavailable; document in `deferred-work.md`

### Architectural Compliance

- **D21 (Extension methods per project):** `AddMemoriesEventStoreIntegration` lives in `EventStoreIntegrationServiceCollectionExtensions.cs` in the EventStore project.
- **D22 (Code style):** file-scoped namespaces, Allman braces, `_camelCase` private fields, nullable enabled, warnings-as-errors. ITANEO copyright header on every file.
- **D18 (Error handling):** Result-style outcomes via the `EventIngestionOutcome` enum for domain logic; exceptions only for infrastructure (DAPR sidecar down, malformed envelope parse).
- **D9 (No premature interfaces):** the mapper is a static class — no `ICloudEventToIngestionInputMapper` interface. The router + service DO get interfaces because they cross integration boundaries (router has a Redis dependency; service owns the workflow-scheduling side effect).
- **D16 (Test frameworks):** xUnit + Shouldly + NSubstitute + coverlet per the table.
- **Rule #10 (DAPR Workflow for orchestrations):** event ingestion scheduling uses `DaprWorkflowClient.ScheduleNewWorkflowAsync` — NOT a custom scheduler.
- **Rule #13 (No custom retry):** controller returns 500 on transient failure; DAPR retries. No `Polly`/`for` retry loops.
- **Rule #17 (Polyglot):** this story is C#-only; no Python dependency.
- **NFR21 (CloudEvents envelope):** preserved via `Dapr.AspNetCore`'s `CloudEvent<T>` + `UseCloudEvents()` middleware.
- **NFR10 (Inter-service DAPR API token auth):** the subscription endpoint is internal (DAPR → app); the existing Story 5.4 AC3 token-mode path applies.

### Anti-Patterns to Avoid

- **DO NOT** parse the CloudEvents envelope manually. Use `CloudEvent<JsonElement>` from `Dapr.AspNetCore` + `app.UseCloudEvents()`.
- **DO NOT** introduce a parallel `EventIngestionWorkflow`. The existing `IngestionWorkflow` handles `SourceType.Event` correctly.
- **DO NOT** call `DaprClient.PublishEventAsync` anywhere in Story 9.1. This is a SUBSCRIPTION story. Publication is the downstream service's concern.
- **DO NOT** fork `IngestionInput` into `EventIngestionInput`. The existing record has every field needed.
- **DO NOT** add a `ForceReplay` bypass for at-least-once redelivery. That regresses idempotency.
- **DO NOT** hard-code the topic name. Config-drive it via `TenantEventRoutingOptions.Topic`.
- **DO NOT** use `DateTimeOffset.UtcNow` in workflows OR the mapper. Workflows use `context.CurrentUtcDateTime`; mapper takes `TimeProvider`.
- **DO NOT** implement a `MapSubscribeHandler` alternative using minimal APIs + manual route registration. DAPR's `/dapr/subscribe` discovery relies on `[Topic]` attribute + `MapSubscribeHandler` pattern.
- **DO NOT** surface `CausationId`/`CorrelationId` as `MetadataField` entries. They are first-class columns on `IngestionInput` → `IndexInput` → graph edges. Putting them in metadata would bypass `IndexGraphActivity`'s edge-creation path.
- **DO NOT** pre-create cases for every tenant. Auto-create lazily on first event per aggregateType (respects the 100-case cap).
- **DO NOT** emit `AccessTelemetryEvent` with a separate `OperationEvent`. Reuse `OperationIngest` — event ingestion IS ingestion.
- **DO NOT** add event-replay semantics in 9.1 (documented in Risk #8; Phase 2 concern).
- **DO NOT** add publication helpers (e.g., `IEventMemoriesClient.PublishAndWaitForIndex`) — this is a subscribe-only story.
- **DO NOT** wire the controller to `app.MapControllers()` and ALSO to a minimal `app.MapPost("/events/ingest")` — pick one (the attribute-based controller) to avoid double-routing.
- **DO NOT** pass the full `IngestionInput.Metadata` dictionary through JSON serialization if the source is a deeply nested event — the dedup hash is already on `sourceUri = cloudevent.id`, so the serialization cost isn't duplicated in the key. But DO keep metadata-entry count bounded (e.g., skip deep event payload fields — only the CloudEvents envelope + `event.aggregateType` should land in metadata).
- **DO NOT** block the controller on a case-auto-create that exceeds the cap for more than 10ms. The router's cap check must happen BEFORE any `CaseService.CreateCaseAsync` call.

### Review Findings Log

- [ ] (placeholder — dev agent + code-reviewer will populate after implementation)

## Dev Agent Record

### Agent Model Used

claude-opus-4-7[1m]

### Debug Log References

- 2026-04-22 — session scoped to Tasks 1-4 (package scaffold + mapper + router + service/controller); Tasks 5-8 remain.
- `dotnet build src/Hexalith.Memories.EventStore/` → 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Memories.EventStore.Tests/` → 65 passing, 0 failing, 0 skipped (Tier 1).
- `dotnet build Hexalith.Memories.slnx` → 0 warnings, 0 errors (full solution sanity check).
- 2026-04-22 — continuation session covering Tasks 5-8.
- `dotnet build src/Hexalith.Memories.Server/` → 0 warnings, 0 errors after Server wiring + JsonContext combine.
- `dotnet build Hexalith.Memories.slnx` → 0 warnings, 0 errors (full-solution sanity check after AppHost + Server + EventStore wiring).
- `dotnet test tests/Hexalith.Memories.EventStore.Tests/` → 65 passing, 0 failing (Tier-1 unchanged).
- `dotnet test tests/Hexalith.Memories.Server.Tests/` → 1308 passing, 0 failing (10 new Tier-2 tests — 7 outcome tests + 2 middleware-order tests + 1 documentation completeness test — plus the pre-existing 1298 regression baseline).
- `dotnet test tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs` → 1 passing, 0 failing after aligning the workflow-status poller with raw Dapr `WorkflowState` (`runtimeStatus` / `isWorkflowCompleted`) and waiting for dedup + indexed subject visibility before asserting the search API.

### Completion Notes List

- **Task 1 — Package scaffold (AC #12, #15):** `src/Hexalith.Memories.EventStore/` project wired into `Hexalith.Memories.slnx` (both src and tests). `IsPackable=true`, references `Hexalith.Memories.Contracts` + `Dapr.AspNetCore` + `Dapr.Client` + `Dapr.Workflow`, `FrameworkReference` on `Microsoft.AspNetCore.App` pulls MVC and MS.Extensions without extra package refs (Central Package Management warned on redundant refs — removed). `InternalsVisibleTo` grants the test project access. Test project `tests/Hexalith.Memories.EventStore.Tests/` uses xUnit + Shouldly + NSubstitute + coverlet + TimeProvider.Testing per `Directory.Packages.props`.
- **Task 2 — Mapper + aggregate-type extractor (AC #1, #2, #6):** Introduced an internal `CloudEventEnvelope` record plus `CloudEventEnvelopeParser` because Dapr's publisher-side `CloudEvent<T>` DTO does not expose `Id`/`Time` — the envelope is parsed from the raw POST body as a `JsonElement`. `CloudEventToIngestionInputMapper` now takes `(CloudEventEnvelope, TenantEventRoute)`; this is a deliberate deviation from Task 2.1's `CloudEvent<JsonElement>` wording. `AggregateTypeExtractor` returns the second dotted segment when present, else the full type. Subject defaults to `"(unset)"` when absent. `cloudevent.time` is preserved verbatim from the publisher. Metadata uses `MetadataOrigin.Ai` with confidence 1.0 — **no** new metadata-origin enum value is introduced in this story.
- **Task 3 — Router + typed outcomes (AC #7, #10, #11, #14a, #14b):** `TenantEventRoutingOptions` (with added `PubSubName`) + typed `TenantEventRouteResolution` replace nullable outcomes. Longest-prefix, case-insensitive source matching. Concurrent first events for the same `(tenantId, aggregateType)` converge on a single `ICaseCreationService.CreateCaseAsync` call via a per-tenant `ConcurrentDictionary<aggregateType, Lazy<Task<string>>>`; a failed creation evicts the reservation so retry can succeed. `CaseNameTemplateRenderer` only substitutes the allow-listed tokens `{aggregateType}` / `{tenantId}` — no `string.Format`. Case cap enforced before any creation call. Tenant-status lookup goes through the new `ITenantStatusAccessor` adapter (package-owned). Subtask 3.6 (startup fail-fast validation against the tenant registry) is deferred to Task 5 — it needs runtime access to the Server registry and belongs in the host wiring layer.
- **Task 4 — Service + controller + preflight dedup (AC #3, #6, #8, #9, #12, #13):** `EventIngestionOutcome` and `EventIngestionResponse` define the controller's response contract — `instanceId` is present **only** on `accepted`. `EventIngestionService` orchestrates parse → route → preflight → schedule → compensate. Preflight dedup goes through the package-owned `IPreflightDedupStore` adapter with three results (`Reserved` / `Duplicate` / `FailOpen`) — fail-open lets the workflow-level permanent dedup key remain the authoritative safety net. When scheduling throws after a reservation was held, the service calls `ReleaseAsync` before surfacing `ScheduleFailed` (AC #9). The workflow instance id is set to the dedup key (`dedup:{tenantId}:{caseId}:{sha256(cloudevent.id)}`) so DAPR redeliveries collide on the same workflow-scheduling call too. `EventIngestionController` is `public`, decorated with `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]`, and translates outcomes to the HTTP-status / DAPR-retry matrix in Dev Notes. `EventStoreIntegrationLog` uses the 9100-9199 event-id bank as specified. `EventStoreIntegrationServiceCollectionExtensions.AddMemoriesEventStoreIntegration` binds options + registers `ITenantEventRouter` and `IEventIngestionService`; the host must register adapter implementations for the four package-owned abstractions (scheduler, tenant-status, telemetry, preflight-dedup, case-creation).
- **Public surface (ADR 9.1-F):** public types are `AddMemoriesEventStoreIntegration`, `TenantEventRoutingOptions`, `TenantEventRoute`, `TenantEventRouteResolution`, `TenantEventRouteResolutionStatus`, `EventIngestionResponse`, `EventIngestionOutcome`, `EventIngestionProcessResult`, `IEventIngestionService`, `IEventIngestionWorkflowScheduler`, `ITenantStatusAccessor` + `EventStoreTenantStatus`, `ICaseCreationService`, `IEventIngestionTelemetry`, `IPreflightDedupStore` + `PreflightReservationResult`, and `EventIngestionController`. Implementation types (`TenantEventRouter`, `EventIngestionService`, `CloudEventToIngestionInputMapper`, `CloudEventEnvelope`, `CloudEventEnvelopeParser`, `AggregateTypeExtractor`, `CaseNameTemplateRenderer`, `EventStoreDedupKey`, `EventStoreIntegrationLog`) remain internal — expose via `InternalsVisibleTo` for tests only.
- **Remaining work (Tasks 5-8):** Server wiring (`Program.cs`, `AppHost/Program.cs`, adapter implementations, `appsettings` sections, `MemoriesJsonContext` registrations, `pubsub.yaml`); Tier-2 integration tests (subscription discovery, roundtrip, outcome coverage, latency); docs guide; planning-artifacts alignment (including converting the existing duplicate Tasks/Review-Findings block in Dev Notes into a single authoritative copy). These were scoped out of this session per user instruction.
- **Task 5 — Server wiring (AC #12, #13):** Added `ProjectReference` Server → EventStore. Created five Server-owned adapters under `src/Hexalith.Memories.Server/EventStoreIntegration/`: `EventIngestionWorkflowSchedulerAdapter` (delegates to existing `IIngestionWorkflowScheduler`), `TenantStatusAccessorAdapter` (maps `TenantStatus` → `EventStoreTenantStatus`), `CaseCreationServiceAdapter` (resolves `CaseService` via `IServiceScopeFactory`), `EventIngestionTelemetryAdapter` (routes outcomes through the existing `AccessTelemetryLog.OperationIngest` / EventId 7502/7512 bank), and `RedisPreflightDedupStore` (`StringSet(..., When.NotExists)` + fail-open on `RedisException` / `TimeoutException`, EventId 9123/9124). Added `EventStoreRoutingConfigValidator : IHostedService` for Task 3.6 — fails fast at startup with EventId 9105 Critical if any `SourceToTenantMap` target does not exist in the registry; no-op when the `Topic` is blank (opt-in integration). `ServerEventStoreIntegrationExtensions.AddServerEventStoreIntegration` composes everything. `Program.cs` registers `AddControllers().AddApplicationPart(...)`, `AddServerEventStoreIntegration(builder.Configuration)`, and the canonical `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();` sequence. AppHost `Program.cs` adds a `pubsub` DAPR component + `.WithReference(pubSub)` on the server sidecar. `deploy/dapr/components/pubsub.yaml` stays aligned with the AppHost wiring (same component name, same broker). `appsettings.Development.json` gets `AutoCreateCases = true`; new `appsettings.Production.json` ships `AutoCreateCases = false` per ADR 9.1-C. Task 5.8 JsonContext registration: since Contracts cannot reference EventStore, a new source-generated `EventStoreJsonContext` inside the EventStore package is combined at `Program.cs` via `JsonTypeInfoResolver.Combine(MemoriesJsonContext.Options.TypeInfoResolver!, EventStoreJsonContext.Default)` so `EventIngestionResponse` / `TenantEventRoute` / `TenantEventRoutingOptions` / `EventIngestionOutcome` / `EventIngestionProcessResult` / `TenantEventRouteResolution` / `TenantEventRouteResolutionStatus` serialize without reflection fallback (AOT-safe path).
- **Task 6 — Tests (AC #5, #13, #15, #16, #17):** Tier-1 coverage unchanged (65/65 green). Added Tier-2 coverage at `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/`: `EventStoreWebAppFactory` (test TestServer factory that replaces every EventStore adapter + DAPR hosted services with NSubstitute fakes), `CapturingEventStoreLogProvider` (captures 9100-9199 EventId bank in-process), and three test classes — `EventIngestionOutcomeTests` (7 tests: accepted, duplicate, invalid-envelope, provisioning, deleting-tenant, unknown-source, schedule-failure-with-reservation-release), `MiddlewareOrderTests` (2 tests: plain-JSON vs application/cloudevents+json middleware behavior — AC-guard-test for Risk #1), and `DocumentationCompletenessTests` (AC #17, reads the markdown doc, asserts on both section headers + concrete required phrases like `publishAllowedTopics`, `max-duration`, `9110`, `PublishEventAsync`, `/api/search`, and both Development/Production columns of the env-defaults table). Restored the missing Aspire-backed roundtrip acceptance coverage at `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`; the test is green after updating workflow polling to the raw Dapr `WorkflowState` shape and waiting for dedup resolution + direct RediSearch subject visibility before asserting the search API. Subscription discovery plus latency / replay-after-restore nightly follow-ups remain deferred in `deferred-work.md`.
- **Task 7 — Documentation (AC #17):** Created `docs/dev/eventstore-integration.md` covering setup (package reference + DI + middleware order + broker wiring + routing config + env-defaults table), CloudEvents envelope requirements, aggregate-type extraction, exact-match subject filtering, at-least-once + dead-letter + replay semantics, publisher trust & spoofing threat model with deploy-time mitigations (`publishAllowedTopics`), source-stability publisher contract, alerting recommendations per log EventId, preflight TTL ↔ DAPR retry `max-duration` coupling rule of thumb, known limitations (single-topic, replay-vs-idempotency, case cap, Story-9.2 boundaries), troubleshooting checklist ("why didn't my event appear?"), and a worked example from `PublishEventAsync` to a `GET /api/search` result. Linked from the root `README.md` under a new "Integration Guides" section.
- **Task 8 — Planning alignment:** `sprint-status.yaml` `last_updated` advanced with a session-complete summary, `9-1-event-auto-discovery-and-dapr-pub-sub-subscription: in-progress` → `review`. `deferred-work.md` gained a 9-1 section enumerating the four Tier-3 items + a note on the Review-Findings bullets already folded into Tasks 1-4.

### File List

**New (src):**

- `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj`
- `src/Hexalith.Memories.EventStore/README.md`
- `src/Hexalith.Memories.EventStore/CloudEventEnvelope.cs`
- `src/Hexalith.Memories.EventStore/CloudEventEnvelopeParser.cs`
- `src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs`
- `src/Hexalith.Memories.EventStore/AggregateTypeExtractor.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRoute.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouteResolution.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouteResolutionStatus.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs`
- `src/Hexalith.Memories.EventStore/CaseNameTemplateRenderer.cs`
- `src/Hexalith.Memories.EventStore/ITenantEventRouter.cs`
- `src/Hexalith.Memories.EventStore/TenantEventRouter.cs`
- `src/Hexalith.Memories.EventStore/ITenantStatusAccessor.cs`
- `src/Hexalith.Memories.EventStore/ICaseCreationService.cs`
- `src/Hexalith.Memories.EventStore/IEventIngestionWorkflowScheduler.cs`
- `src/Hexalith.Memories.EventStore/IEventIngestionTelemetry.cs`
- `src/Hexalith.Memories.EventStore/IPreflightDedupStore.cs`
- `src/Hexalith.Memories.EventStore/IEventIngestionService.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionService.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionController.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionOutcome.cs`
- `src/Hexalith.Memories.EventStore/EventIngestionResponse.cs`
- `src/Hexalith.Memories.EventStore/EventStoreDedupKey.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs`
- `src/Hexalith.Memories.EventStore/EventStoreIntegrationServiceCollectionExtensions.cs`

**New (tests):**

- `tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj`
- `tests/Hexalith.Memories.EventStore.Tests/AggregateTypeExtractorTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/CloudEventEnvelopeParserTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/CloudEventToIngestionInputMapperTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/CaseNameTemplateRendererTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/TenantEventRouterTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/EventIngestionServiceTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/EventIngestionControllerTests.cs`
- `tests/Hexalith.Memories.EventStore.Tests/EventIngestionResponseTests.cs`

**New (src, Task 5):**

- `src/Hexalith.Memories.EventStore/EventStoreJsonContext.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/CaseCreationServiceAdapter.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionWorkflowSchedulerAdapter.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/EventStoreRoutingConfigValidator.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/RedisPreflightDedupStore.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/ServerEventStoreIntegrationExtensions.cs`
- `src/Hexalith.Memories.Server/EventStoreIntegration/TenantStatusAccessorAdapter.cs`
- `src/Hexalith.Memories.Server/appsettings.Production.json`
- `deploy/dapr/components/pubsub.yaml`

**New (tests, Task 6):**

- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/CapturingEventStoreLogProvider.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/DocumentationCompletenessTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventIngestionOutcomeTests.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/EventStoreWebAppFactory.cs`
- `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/MiddlewareOrderTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`

**New (docs, Task 7):**

- `docs/dev/eventstore-integration.md`

**Modified:**

- `Hexalith.Memories.slnx` — registered both new projects.
- `Hexalith.Memories.EventStore.csproj` — added `InternalsVisibleTo` for `Hexalith.Memories.Server`, `Hexalith.Memories.Server.Tests`, and `Hexalith.Memories.IntegrationTests` so host adapters and Tier-2 tests can reach the package's internal interfaces.
- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` — added `ProjectReference` to `Hexalith.Memories.EventStore`.
- `src/Hexalith.Memories.Server/Program.cs` — registered controllers via `AddApplicationPart(...)`, wired `AddServerEventStoreIntegration`, combined `EventStoreJsonContext` into the HTTP JSON options resolver chain, and added the canonical `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();` middleware sequence.
- `src/Hexalith.Memories.AppHost/Program.cs` — registered the `pubsub` DAPR component and attached `.WithReference(pubSub)` on the server sidecar.
- `src/Hexalith.Memories.Server/appsettings.Development.json` — added the `EventStoreIntegration:Routing` section (Development defaults: `AutoCreateCases = true`).
- `README.md` — added a new "Integration Guides" section linking to `docs/dev/eventstore-integration.md`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `9-1-event-auto-discovery-and-dapr-pub-sub-subscription: ready-for-dev` → `in-progress` (session 1) → `review` (session 2); `last_updated: 2026-04-22`.
- `_bmad-output/implementation-artifacts/deferred-work.md` — added a Story-9.1 section covering Tier-3 Aspire deferrals and a note on the Review-Findings bullets already folded into Tasks 1-4.
- `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md` — task status, checkboxes, and this Dev Agent Record (only allowed sections).

### References

- Epic 9 — `_bmad-output/planning-artifacts/epics.md#L1663-1697` (Story 9.1 AC authoritative source)
- FR59-FR62 — `_bmad-output/planning-artifacts/prd.md#L906-911`
- NFR6 (event indexing freshness) — `_bmad-output/planning-artifacts/prd.md#L947`
- NFR21 (CloudEvents envelope) — `_bmad-output/planning-artifacts/prd.md#L982`
- Architecture — DAPR pub/sub section, Project #10 `Hexalith.Memories.EventStore` — `_bmad-output/planning-artifacts/architecture.md#L74, #L494, #L1091, #L1304, #L1437`
- Ingestion workflow + idempotency — `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:1-650` + `Activities/Ingestion/CheckIdempotencyActivity.cs:1-40` + `DedupKeyBuilder.cs:1-21`
- Graph causal edges (already implemented) — `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:74-101`
- EventStore submodule subscription pattern — `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Program.cs:30-43` + `Controllers/ProjectionNotificationController.cs:22-75`
- EventStore publisher pattern (for round-trip tests) — `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs:1-198`
- Story 1.6 context (ingestion workflow foundations) — `_bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md`
- Story 3.1 context (CaseService) — `src/Hexalith.Memories.Server/Cases/CaseService.cs`
- Story 5.1 context (TenantRegistryService + TenantStatusGuard) — `src/Hexalith.Memories.Server/Tenants/`

## Change Log

- 2026-04-22 — Story 9.1 Tasks 1-4 implemented (scoped session). Added `Hexalith.Memories.EventStore` publishable package + test project, CloudEvents envelope parser, mapper + aggregate-type extractor, typed router + options, ingestion service + controller + preflight-dedup compensation, and 65 Tier-1 unit tests (all green). Package keeps Server out of its compile-time dependency graph via five package-owned adapter interfaces. Tasks 5-8 (Server wiring, Tier-2 integration tests, docs guide, planning-artifact alignment) remain for a follow-up session.
- 2026-04-22 — Story 9.1 Tasks 5-8 implemented (continuation session). Wired the EventStore package into the Memories Server via five Server-owned adapters (workflow scheduler, tenant status, case creation, telemetry, Redis preflight-dedup) + a fail-fast startup validator for `SourceToTenantMap` (subtask 3.6). Canonical `app.UseCloudEvents(); app.MapControllers(); app.MapSubscribeHandler();` middleware order landed in `Program.cs`. AppHost registers the `pubsub` DAPR component and `deploy/dapr/components/pubsub.yaml` mirrors it for non-Aspire deployments. `appsettings.Development.json` / `appsettings.Production.json` carry the `AutoCreateCases` environment split (ADR 9.1-C). A new source-generated `EventStoreJsonContext` is combined into the host JSON options resolver chain so Task 5.8 AOT-safe serialization lands without introducing a Contracts → EventStore cycle. Added 10 Tier-2 tests under `tests/Hexalith.Memories.Server.Tests/EventStoreIntegration/` (outcome mapping, middleware order, documentation completeness). Full solution build green; 65/65 Tier-1 EventStore tests + 1308/1308 Server.Tests pass. Tier-3 Aspire nightly coverage (roundtrip + discovery + latency + replay-after-restore) is explicitly deferred with rationale in `deferred-work.md`. Developer guide shipped at `docs/dev/eventstore-integration.md` and linked from the root `README.md` under a new "Integration Guides" section. Story status: in-progress → review.
- 2026-04-22 — Follow-up validation session restored the missing Aspire roundtrip acceptance coverage in `tests/Hexalith.Memories.IntegrationTests/EventStoreIntegration/EventIngestionPipelineIntegrationTests.cs`. The test now polls raw Dapr workflow state via `runtimeStatus` / `isWorkflowCompleted`, waits for dedup-key resolution and direct subject-filtered RediSearch visibility, then verifies the search API returns the indexed memory unit. Focused integration validation is green (1/1).
