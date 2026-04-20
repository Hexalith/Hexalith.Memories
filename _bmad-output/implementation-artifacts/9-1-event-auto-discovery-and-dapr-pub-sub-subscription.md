# Story 9.1: Event Auto-Discovery & DAPR Pub/Sub Subscription

Status: ready-for-dev

**Effort estimate:** ~4-5 working days — 0.25 day pubsub + subscription AppHost wiring (Task 1), 0.5 day CloudEvents subscription endpoint + envelope mapping (Task 2), 0.25 day event-id idempotency (Task 3), 0.25 day tenant/case resolution (Task 4), 0.5 day `Hexalith.Memories.EventStore` package scaffolding (Task 5), 1.0 day unit tests (Task 6), 0.75 day integration tests (Task 7 — Tier 2 pub/sub roundtrip), 0.25 day docs + sprint-status + retro entry (Task 8). Add 0.5 day cushion for Kestrel/CloudEvents middleware surprises (request-body fork, raw CloudEvents vs DAPR-unwrapped payload).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** A **zero-code DAPR pub/sub subscription** that auto-discovers event types and funnels every CloudEvents-compliant message published to a configured pub/sub topic through the existing `IngestionWorkflow`. Ships (a) the `pubsub.yaml` Redis-Streams component + subscription wiring in the AppHost; (b) a minimal `POST /events/ingest` subscription endpoint decorated with `[Topic]` (via `app.UseCloudEvents()` + `app.MapSubscribeHandler()`) that accepts a raw `CloudEvent<JsonElement>`; (c) a `CloudEventToIngestionInputMapper` that preserves CloudEvents `id`/`source`/`type`/`subject`/`time` as `MetadataOrigin.System`-tagged metadata fields on the resulting memory unit (a new enum value introduced by Task 2.10 — `MetadataOrigin.Ai` is reserved for Story 9.2's LLM-derived NL description, per Risk #16) and uses CloudEvents `id` as the ingestion source URI so the existing dedup activity deduplicates at-least-once redeliveries; (d) a `TenantEventRoutingConfig`-driven mapping from CloudEvents `source` → `tenantId` and from CloudEvents `type`/`subject` → `caseId`; (e) a **new publishable `Hexalith.Memories.EventStore` NuGet package** (per architecture D9 / project #10) that hosts the subscription endpoint + mapper + DI extension so downstream services can reference it; (f) an `IngestedBy` system identity `"events"` for every event-sourced ingestion; (g) non-200 return on transient workflow-scheduler failure so DAPR retries per at-least-once guarantee. Closes **FR59** ("auto-discover event types published to DAPR pub/sub topics") and **NFR21** ("handle CloudEvents envelope format"); lays the plumbing for **FR60-FR62** (dual embeddings — Story 9.2; handler registration listing — Story 9.3).

**What already exists (do NOT rebuild):**

1. **`IngestionInput` — `src/Hexalith.Memories.Contracts/V1/IngestionInput.cs`.** Already carries `CausationId` / `CorrelationId` + `SourceType` enum with `Event` member + `Metadata: Dictionary<string, MetadataField>`. **Reuse verbatim.** No contract changes are needed for 9.1 — all CloudEvents envelope fields fit in the existing `Metadata` dictionary. Do NOT fork a dedicated `EventIngestionInput` record; the workflow is type-specialized on `IngestionInput`, and a parallel type would force a parallel workflow.
2. **`SourceType.Event` — `src/Hexalith.Memories.Contracts/V1/SourceType.cs:11`.** Already registered in `CamelCaseStringEnumConverter<SourceType>`. Set `input.SourceType = SourceType.Event` in the mapper.
3. **`IngestionWorkflow` + `CheckIdempotencyActivity` / `SaveDedupKeyActivity` — `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` + `src/Hexalith.Memories.Server/Activities/Ingestion/CheckIdempotencyActivity.cs` + `SaveDedupKeyActivity.cs`.** The workflow already does: idempotency → validation → extract → embed → fan-out index → verify → dedup persist. **Reuse verbatim.** Event ingestion is just another caller. Do NOT add an `IngestionWorkflowEventVariant` or branch the workflow on `SourceType.Event` — the existing per-activity behavior is identical, except that `ValidateContentActivity` must tolerate `ContentType = "application/json"` (already tolerated; inspect `ValidateContentActivity.cs` at implementation time to confirm) and `ExtractContentActivity` must treat a JSON payload as already-extracted UTF-8 text (Kreuzberg's default text extractor already returns UTF-8 for `application/json` — verify at implementation time by reading `ContentExtractionClient.cs`).
4. **`DedupKeyBuilder.BuildKey` + SHA-256 helper — `src/Hexalith.Memories.Server/Activities/Ingestion/DedupKeyBuilder.cs`.** Key format `dedup:{tenantId}:{caseId}:{sha256(sourceUri)}`. **Reuse verbatim** — the mapper sets `sourceUri = cloudevent.id` so the existing hash-based dedup handles at-least-once redeliveries without any changes to `CheckIdempotencyActivity`.
5. **`IndexGraphActivity` — `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:74-101`.** Already creates `caused_by` + `correlated_with` edges from `IndexInput.CausationId` / `CorrelationId` via `BuildMergeStubNode` + `BuildMergeEdge`. **This is already the "auto-index CausationId/CorrelationId as graph edges" guarantee from FR61.** Story 9.1 threads the CloudEvents-unwrapped CausationId/CorrelationId into `IngestionInput`; no changes needed to `IndexGraphActivity`. Confidence-promotion for out-of-order events + gap markers is Story 9.2 — do NOT pre-solve.
6. **`CamelCaseStringEnumConverter<T>` + `MemoriesJsonContext.Options` — `src/Hexalith.Memories.Contracts/V1/CamelCaseStringEnumConverter.cs` + `MemoriesJsonContext.cs`.** Use `MemoriesJsonContext.Options` for every serialize/deserialize call in the mapper and the subscription endpoint. AOT-safe, source-generated — do NOT introduce a new `JsonSerializerOptions` instance.
7. **`TenantRegistryService.GetAsync` + `TenantStatusGuard.ValidateTenantActiveAsync` — `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` + `TenantStatusGuard.cs`.** Reuse verbatim to reject events destined for an unknown/inactive tenant before scheduling the workflow (return non-200 so DAPR retries if the tenant is provisioning; return 200 + log drop if the tenant is intentionally absent — see Dev Notes "At-least-once vs dead-letter").
8. **`CaseService.GetCaseAsync` + `CaseService.CreateCaseAsync` — `src/Hexalith.Memories.Server/Cases/CaseService.cs`.** Reuse for the "case per tenant+aggregate-type" auto-creation path (see Task 4). Do NOT write case-management code; the existing `CaseService` is the single owner of case creation + listing.
9. **Hexalith.EventStore ecosystem patterns — `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Program.cs:30,43` + `src/submodules/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ProjectionNotificationController.cs:31`.** Canonical subscription surface: `app.UseCloudEvents()` middleware + `app.MapSubscribeHandler()` + `[Topic(pubSubName, "topic-name")]` attribute on an `[ApiController]` route. **Follow this pattern exactly** — the EventStore submodule is the shared-ecosystem reference. Do NOT introduce a raw `[Route]`-only minimal API controller without the `[Topic]` attribute; DAPR's subscription-discovery relies on the attribute.
10. **`Program.cs` ingestion endpoint + `DaprWorkflowClient.ScheduleNewWorkflowAsync` — `src/Hexalith.Memories.Server/Program.cs:240-305`.** The REST `/api/ingest` path already schedules `IngestionWorkflow`. The event-subscription endpoint uses the **same `DaprWorkflowClient`** — just with an event-sourced `IngestionInput`. Mirror the error-handling shape (`EndpointTelemetryScope` pattern).
11. **`MemoriesActivitySource` + `AccessTelemetryLog` — `src/Hexalith.Memories.Server/Telemetry/`.** Emit an OpenTelemetry activity for each CloudEvent ingestion using `MemoriesActivitySource.IngestRequest` with `TagOperation = AccessTelemetryLog.OperationIngest` and `TagSourceType = "event"`. Reuse `EndpointTelemetryScope` verbatim — the subscription endpoint is just another ingestion surface.

**What 9.1 adds:**

1. **`src/Hexalith.Memories.EventStore/`** — NEW publishable NuGet project, SDK `Microsoft.NET.Sdk`. Registered in `Hexalith.Memories.slnx`. References `Hexalith.Memories.Contracts`. Packable = true (per architecture table "10 Hexalith.Memories.EventStore — Zero-code integration (Phase 1.5)"). Files:
   - `EventIngestionController.cs` — `[ApiController]` + `[Route("events")]` + `[Topic(...)]` POST endpoint that receives a raw `CloudEvent<JsonElement>` and forwards to `IEventIngestionService`. No domain logic here — controller is a thin DAPR binding shim.
   - `CloudEventToIngestionInputMapper.cs` — static mapper class: `(IngestionInput Input, string TenantId) Map(CloudEvent<JsonElement> evt, ITenantEventRouter router, TimeProvider timeProvider)`. Pure function — NO `DateTimeOffset.UtcNow`; takes `TimeProvider` so tests can pin the clock. Returns `null`/throws `InvalidOperationException` with an explicit reason if required fields are missing so the controller returns a typed 400 — see "CloudEvents → IngestionInput mapping" in Dev Notes.
   - `IEventIngestionService.cs` + `EventIngestionService.cs` — thin service that (a) runs the mapper, (b) calls `TenantStatusGuard.ValidateTenantActiveAsync`, (c) calls `DaprWorkflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input)`. **Does NOT call `DaprClient` directly.** DI-constructor-injected.
   - `ICaseCreationService.cs` — interface declared in EventStore with one method: `Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken ct)`. Implemented in the Server project via `CaseCreationServiceAdapter` (delegates to `CaseService.CreateCaseAsync`). **Prevents EventStore from referencing Server.**
   - `ITenantEventRouter.cs` + `TenantEventRouter.cs` — routes a CloudEvent to a `(tenantId, caseId)` tuple. MVP implementation: config-driven mapping (`TenantEventRoutingOptions`) — topic/source → tenantId; event-type prefix → caseId OR auto-create case per `{tenantId}:{aggregateType}`. See "Tenant + case routing" in Dev Notes for the decision matrix.
   - `TenantEventRoutingOptions.cs` — bound from `appsettings.json` section `EventStoreIntegration:Routing` via `IOptions<T>`. Fields:
     - `PubSubName` (string, default `"pubsub"`) — DAPR pub/sub component name.
     - `Topic` (string, required) — topic name subscribed to. MVP: single topic per deployment.
     - `SourceToTenantMap` (`Dictionary<string, string>`) — CloudEvents `source` prefix → tenantId. Longest-prefix wins.
     - `AutoCreateCases` (bool, default `true`) — when `true`, the router calls `CaseService.CreateCaseAsync` lazily on first event per `(tenantId, aggregateType)`.
     - `CaseIdTemplate` (string, default `"events:{aggregateType}"`) — string.Format template used as the case name when auto-creating.
   - `EventStoreIntegrationServiceCollectionExtensions.cs` — **public** `services.AddMemoriesEventStoreIntegration(IConfiguration config)` that registers the controller, mapper, service, router, and binds `TenantEventRoutingOptions`; also self-registers `TryAddSingleton<TimeProvider>(TimeProvider.System)` so Server consumers do not silently NRE (Risk #18). Extension-method-per-project per Architecture D21. Per ADR 9.1-F, this is one of only **three public types** in the package (with `TenantEventRoutingOptions` and `TenantEventRoute`); all other types are `internal`.
   - `Hexalith.Memories.EventStore.csproj` — `IsPackable = true`; matches NuGet version from `Directory.Packages.props`; target framework matches solution (net10.0). Package metadata follows the `Hexalith.Memories.Contracts` csproj shape (description, authors, license).
2. **`pubsub.yaml` component + subscription wiring — `deploy/dapr/components/pubsub.yaml`.** Redis Streams pub/sub (NOT a separate broker — reuse the existing Redis container):
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
         value: "127.0.0.1:6379"
       - name: redisPassword
         value: ""
   ```
   **AppHost wiring (`src/Hexalith.Memories.AppHost/Program.cs`):** add a second `builder.AddDaprComponent("pubsub", "pubsub.redis")` call, chain `.WithMetadata("redisHost", "127.0.0.1:6379")` + `.WithMetadata("redisPassword", string.Empty)`, and add `.WithReference(pubSub)` to the Memories server resource so the sidecar discovers it. Mirror the existing `stateStore` registration pattern — do NOT bind-mount the YAML; the AppHost creates components in-process.
3. **`app.UseCloudEvents()` + `app.MapSubscribeHandler()` + `app.MapControllers()` — `src/Hexalith.Memories.Server/Program.cs`.** Three additions in the middleware chain:
   - `app.UseCloudEvents()` — BEFORE `app.MapControllers()` and BEFORE any route mapping that reads the body. Required for DAPR to unwrap CloudEvents envelopes.
   - `app.MapSubscribeHandler()` — registers DAPR's `/dapr/subscribe` endpoint that daprd probes at startup to discover `[Topic]` bindings. Must be called before `app.Run()`.
   - `app.MapControllers()` — if not already called. Verify at implementation time (the current Server uses minimal APIs for ingest; controllers are still registered via `AddControllers` in the DI container but `MapControllers` may not be mapped). Add `builder.Services.AddControllers()` registration if missing, and add `app.MapControllers()` registration before `app.MapDefaultEndpoints()`.
4. **Subscription endpoint — `POST /events/ingest`.** Decorated with `[Topic(pubSubName: "pubsub", name: "{from-options}")]`. Request body: DAPR-unwrapped `CloudEvent<JsonElement>`. Response:
   - **200 OK** — workflow scheduled successfully OR duplicate detected (idempotency early-return by `CheckIdempotencyActivity`).
   - **200 OK + log warn** — tenant does not exist or has been deleted. Event is dropped (NOT retried) because DAPR would redeliver indefinitely for a tenant that will never exist. See "At-least-once vs dead-letter" in Dev Notes.
   - **400 Bad Request** — CloudEvents envelope missing required fields (`id`, `type`, `source`) OR mapper could not resolve tenant. DAPR does NOT retry 4xx responses — these go to the dead-letter topic if configured, else are dropped. Log at `Error` level.
   - **500 Internal Server Error** — `DaprWorkflowClient.ScheduleNewWorkflowAsync` throws transient (sidecar restart, Redis hiccup). DAPR retries per the at-least-once contract.
5. **Tenant/case routing logic — `TenantEventRouter`.** MVP: extract `tenantId` from CloudEvents `source` via longest-prefix match against `SourceToTenantMap`. Extract `aggregateType` from CloudEvents `type` (`"MyApp.Claims.ClaimSubmittedV2"` → `"Claims"` — the second dotted segment by convention; **documented as "sources must follow the `{domain}.{aggregateType}.{eventName}{version}` CloudEvents type convention" in `docs/dev/eventstore-integration.md`**). Resolve `caseId` via:
   - `GetOrCreateCaseForAggregateAsync(tenantId, aggregateType, ct)` — reads `TenantId-to-AggregateType-to-CaseId` from Redis (key `events:caseMap:{tenantId}:{aggregateType}`); if absent, calls `CaseService.CreateCaseAsync` with `Name = string.Format(CaseIdTemplate, aggregateType)` and persists the new `caseId` to the Redis map. Idempotent — two concurrent events with the same aggregateType must resolve to the same `caseId` (use `SET NX`).
6. **CloudEvents → IngestionInput mapping — `CloudEventToIngestionInputMapper.Map`.** Precise field translation:
   - `IngestionInput.TenantId` ← from `ITenantEventRouter.ResolveTenantId(evt)`.
   - `IngestionInput.CaseId` ← from `ITenantEventRouter.ResolveCaseIdAsync(tenantId, evt, ct)`.
   - `IngestionInput.SourceUri` ← **CloudEvents `id`** (guaranteed unique per at-least-once semantics — this is what drives idempotency via the existing `DedupKeyBuilder`).
   - `IngestionInput.ContentBytes` ← `Encoding.UTF8.GetBytes(evt.Data.GetRawText())` — the raw JSON payload, preserved byte-for-byte so Story 9.2's "raw payload embedding" has a stable input.
   - `IngestionInput.ContentType` ← `evt.DataContentType ?? "application/json"`.
   - `IngestionInput.SourceType` ← `SourceType.Event`.
   - `IngestionInput.IngestedBy` ← `"events"` (system identity — NOT the event's `UserId` field; that goes into metadata for auditability without overriding provenance).
   - `IngestionInput.Metadata` ← system-origin (`MetadataOrigin.System`, confidence `1.0`) fields — see Task 2.10 for enum introduction; `MetadataOrigin.Ai` is reserved for Story 9.2 LLM-derived fields (Risk #16):
     - `cloudevent.id` ← `evt.Id`
     - `cloudevent.source` ← `evt.Source.ToString()`
     - `cloudevent.type` ← `evt.Type`
     - `cloudevent.subject` ← `evt.Subject` (aggregate ID — **CRITICAL for AC #7 filtering**)
     - `cloudevent.time` ← `evt.Time?.ToString("o")` (ISO-8601)
     - `cloudevent.specversion` ← `evt.SpecVersion` (for forward-compat — e.g., future `1.1` detection)
     - `event.aggregateType` ← derived from `evt.Type` (second dotted segment)
     - `event.userId` ← from the event envelope if present (see Dev Notes "CausationId / CorrelationId extraction"); confidence `1.0`.
   - `IngestionInput.CausationId` ← extracted from either (a) CloudEvents extension attribute `causationid` (preferred — follows CloudEvents ext conventions) OR (b) event envelope `CausationId` property when the payload is a Hexalith.EventStore `EventEnvelope` shape (see Dev Notes). `null` when neither present.
   - `IngestionInput.CorrelationId` ← same dual-source rule: extension attribute `correlationid` preferred; fallback to envelope field. `null` when neither present.
7. **Docs — `docs/dev/eventstore-integration.md`.** NEW developer guide covering: subscription setup (pubsub.yaml + `AddMemoriesEventStoreIntegration`); CloudEvents envelope requirements (required + optional fields); tenant/case routing configuration; aggregateType extraction rules; idempotency semantics (CloudEvents `id` drives dedup); at-least-once + dead-letter strategy; test harness (how to publish a test event via `DaprClient.PublishEventAsync` or curl into the subscription endpoint). Include a worked example: Hexalith.EventStore `CounterIncrementedV1` event → memory unit with graph edges.
8. **`deferred-work.md`** update: mark "Story 9.1 — dual embeddings" + "Story 9.1 — handler registration" as already-documented-in-Story-9.2/9.3 so we don't accidentally cross-implement.

**What does NOT ship:**

- **Dual embeddings (raw payload + NL description).** Ship only the raw-payload path; Story 9.2 adds DAPR Conversation-API-generated natural-language embedding. Do NOT add an `EnrichEventActivity` or any LLM wiring in 9.1.
- **Gap markers + retroactive `caused_by` resolution.** The out-of-order event case (B arrives before A) is Story 9.2's AC. Story 9.1 leaves gap handling to `IndexGraphActivity`'s existing `BuildMergeStubNode` (which creates a stub with no other data). The stub-to-real merge when event A arrives later works because `BuildMergeMemoryUnitNode` is `MERGE` + property-set — but Story 9.2 adds explicit gap-marker semantics + confidence-promotion UX. Do NOT implement that here.
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
2. **At-least-once delivery + idempotency race.** Two redeliveries of the same event arrive concurrently on different workflow instances; both pass `CheckIdempotencyActivity` before either calls `SaveDedupKeyActivity`. **Mitigation:** the existing duplicate race is DOCUMENTED in Story 1.6's "Known race window" — the architecture accepts it because `IndexSyntacticActivity` / `IndexSemanticActivity` / `IndexGraphActivity` are MERGE/HSET operations (idempotent on the memory unit ID), and two workflows writing the same data yields identical state. Verify this still holds for event ingestion: the `MemoryUnitId` is workflow-instance-scoped (`context.InstanceId`), so TWO workflow instances for the SAME `cloudevent.id` would create TWO distinct memory units. **This is a behavioral divergence from file ingestion's dedup model** — file ingestion keys dedup by `(tenantId, caseId, sourceUri)`; event ingestion uses `sourceUri = cloudevent.id`, which is globally unique, so the first workflow to `SaveDedupKeyActivity` wins. Add a guard test `EventIngestionTests.ConcurrentRedeliveries_ResultInSingleMemoryUnit` that fires 5 parallel redeliveries of the same CloudEvent and asserts exactly one memory unit exists in Redis + FalkorDB + exactly one `IngestionResult.WasDuplicate=false` response. If the guard fails, the mitigation is to promote the dedup write into a Lua `SET NX` before scheduling the workflow (fast, deterministic; file ingestion's deferred SaveDedupKeyActivity is unchanged).
3. **Missing `subject` field on CloudEvents breaks AC #2 + AC #4.** The AC says "CloudEvents metadata (source, type, subject, time, id) is extracted and preserved" — but `subject` is OPTIONAL in CloudEvents 1.0. **Mitigation:** (a) when `subject` is null/empty, set `metadata["cloudevent.subject"] = "(unset)"` and log at `Information` level; (b) ACs explicitly scoped to "CloudEvents-compliant messages" that include `subject` — document the expectation in `docs/dev/eventstore-integration.md` that `subject` MUST be present for grouping to work; (c) guard test `CloudEventToIngestionInputMapperTests.SubjectMissing_MetadataShowsExplicitUnset_NoCrash`.
4. **Tenant routing via `source` prefix creates a single-point-of-configuration failure.** A typo in `SourceToTenantMap` sends every event to the wrong tenant — a data-integrity catastrophe. **Mitigation:** (a) the router validates every configured tenantId against `TenantRegistryService.GetAsync` at startup (fails-fast if any entry points to a non-existent tenant); (b) emit a warning `EventStoreIntegrationLog.UnknownTenant` if a runtime CloudEvent's `source` matches no entry (drops + 200 response per "At-least-once vs dead-letter"); (c) guard test `TenantEventRouterTests.SourceWithNoMapping_DropsWithWarning_Returns200`.
5. **Case auto-creation produces unbounded cases.** If an attacker or misconfigured publisher sends events with thousands of distinct aggregate types, the router auto-creates thousands of cases. **Mitigation:** (a) cap auto-creation at `MaxAutoCreatedCasesPerTenant = 100` (config-bound); (b) exceeding the cap returns 400 on the subscription endpoint (DAPR does NOT retry) + emits `EventStoreIntegrationLog.CaseCreationCapExceeded`; (c) guard test `TenantEventRouterTests.CaseCap_ExceededReturnsInvariantFailure`; (d) the cap is per-tenant, soft-configurable, and documented in `docs/dev/eventstore-integration.md` as "raise this only if your domain has > 100 aggregate types".
6. **`ContentBytes` = raw JSON bypasses Kreuzberg extraction but `ExtractContentActivity` still runs.** The existing workflow calls `ExtractContentActivity` regardless of SourceType. Kreuzberg on a JSON blob returns the JSON as text — which is fine for BM25 but loses the semantic structure. **Mitigation:** (a) verify at implementation time that `ExtractContentActivity` short-circuits for `application/json` or returns the input bytes as UTF-8 text (read `ContentExtractionClient.cs`); (b) if it does NOT short-circuit, add a fast-path that simply sets `extraction.ExtractedContent = Encoding.UTF8.GetString(input.ContentBytes)` when `ContentType = "application/json"` — this is additive and narrow; (c) guard test `ExtractContentActivityTests.JsonPayload_ReturnsRawTextIdentityExtraction`.
7. **`EventEnvelope` from Hexalith.EventStore vs generic CloudEvents payload.** The Hexalith.EventStore `EventPublisher.cs` wraps its `EventEnvelope` inside the CloudEvents payload — so `evt.Data` contains a Hexalith-specific shape with `CausationId`/`CorrelationId` properties. Generic publishers (Marten, Wolverine, Axon) may put `CausationId` in CloudEvents extension attributes instead. **Mitigation:** (a) the mapper tries BOTH sources — first CloudEvents extension attributes (`evt.GetExtensionAttribute<string>("causationid")`), then falls back to `evt.Data.TryGetProperty("causationId", ...)`; (b) neither present → `CausationId = null`, which is already handled by `IndexGraphActivity` (skips the optional edge); (c) guard tests for each path: `CloudEventToIngestionInputMapperTests.ExtensionAttribute_PreferredOverEnvelopeField` + `CloudEventToIngestionInputMapperTests.EnvelopeField_UsedWhenExtensionAbsent` + `CloudEventToIngestionInputMapperTests.NeitherPresent_NullCausationId`.
8. **Replay semantics conflict with idempotency.** Hexalith.EventStore supports event replay. Replayed events use the same envelope + `cloudevent.id` — which means the memory unit already exists, `CheckIdempotencyActivity` returns `WasDuplicate = true`, and the workflow returns early without re-indexing. That's intentional for NORMAL at-least-once redelivery but WRONG for operator-triggered replay-after-tenant-restore. **Mitigation:** document this explicitly in `docs/dev/eventstore-integration.md` under "Replay semantics": replay is designed for event-store rebuilds, not memory rebuilds; to rebuild memory from an event stream, operators must delete the tenant's memory units first (Story 3.5) OR use the tenant-provisioning workflow to recreate the tenant, then re-publish. DO NOT add a `forceReplay` bypass in 9.1 — that would regress the at-least-once idempotency guarantee.
9. **NFR6 <5s indexing freshness.** The full ingestion pipeline (validate → extract → embed → index → verify → dedup) on a 1 KB JSON event is ~1-3s in dev; under load (embedding provider latency, cold FalkorDB connection) the p95 can exceed 5s. **Mitigation:** (a) MVP measurement test `EventIngestionLatencyTests.SingleEvent_P95IndexingFreshness_Under5s` in the Tier 2 integration suite; (b) if p95 fails, the remediation is Story 9.2's LLM-generated NL description + dual embedding which doesn't help latency — so escalate to an embedding provider review; (c) document the NFR assumption ("under normal conditions" means embedding provider is NOT rate-limited) in `docs/dev/eventstore-integration.md`.
10. **`UseCloudEvents()` + `MapSubscribeHandler()` order sensitivity in the middleware pipeline.** Wrong order causes either (a) CloudEvents envelope not unwrapped (handler receives the wrapped envelope, deserialization fails) or (b) subscription-handler discovery returns empty (DAPR fails to register the topic at startup). **Mitigation:** (a) follow the EventStore submodule's exact order: `app.UseCloudEvents()` after auth middleware, BEFORE `app.MapControllers()`; `app.MapSubscribeHandler()` AFTER `app.MapControllers()`; (b) guard test: run the full Aspire AppHost fixture, observe `GET http://localhost:<memories-server-app-port>/dapr/subscribe` returns the subscription entry for "pubsub" + the configured topic; (c) test `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic`.
11. **Clock skew between publisher and subscriber breaks event-time ordering.** If Story 9.2 relies on `cloudevent.time` for ordering (e.g., gap marker age), skew between the publisher's clock and the Memories server's clock causes incorrect ordering decisions. Out of scope for 9.1 — BUT: do NOT use the subscriber's `DateTimeOffset.UtcNow` to tag the metadata field `cloudevent.time`; preserve the publisher's value verbatim. Story 9.2 will decide how to use it.
12. **`source` URIs may contain path characters that break Redis key format.** CloudEvents `source` is a URI-reference which can include `/`, `?`, `#`. These must not leak into Redis keys (the existing `DedupKeyBuilder.BuildKey` SHA-256-hashes the source URI, so this is safe for dedup), but the `SourceToTenantMap` key lookup does prefix-matching on the raw string — a publisher sending a slightly different casing (`HTTPS://...` vs `https://...`) would miss the map. **Mitigation:** (a) the router performs case-insensitive longest-prefix matching; (b) document the case-sensitivity posture in `docs/dev/eventstore-integration.md`; (c) guard test `TenantEventRouterTests.SourcePrefixMatch_IsCaseInsensitive`.
13. **Silent source-scheme drift routes events to "unknown" for hours before detection.** A publisher flipping `http://` → `https://` during a routine certificate migration breaks longest-prefix match; events are dropped with 200 + `Warning` log, no alert fires, search gaps appear downstream before anyone notices. Risk #12 addresses casing but not scheme or path-component drift. **Mitigation:** (a) emit a metric `memories_eventstore_unknownsource_total{source=...}` (counter) alongside `EventStoreIntegrationLog.UnknownSource`; (b) document "source stability" as a publisher contract in `docs/dev/eventstore-integration.md` — publishers must treat their configured `source` as a stable identifier, not a deploy-time URL; (c) recommended alert rule (in the doc): rate-of-increase on the unknown-source counter for > 5 minutes fires a page; (d) guard test `TenantEventRouterTests.UnknownSource_IncrementsMetric`.
14. **`MapSubscribeHandler` silent non-registration (Task 4.4 fallback path).** If dynamic endpoint metadata via `WithMetadata(new TopicAttribute(...))` is NOT read by `Dapr.AspNetCore` 1.17.6's subscription-discovery (only class-level `[Topic]` attributes are), `GET /dapr/subscribe` returns empty, daprd never wires the topic, and zero events flow with NO error — "nothing is wrong with nothing happening." **Mitigation:** (a) a startup `IHostedService` asserts `GET /dapr/subscribe` contains ≥1 entry matching the configured topic, and calls `IHostApplicationLifetime.StopApplication()` with a Critical log (`EventStoreIntegrationLog.SubscriptionRegistrationFailed`) if empty — fail-fast over fail-silent; (b) the new spike (Task 4.0 — see Tasks) empirically verifies which path works before production code is written; (c) guard test `EventIngestionSubscriptionDiscoveryTests.Startup_FailsFast_WhenSubscribeEndpointEmpty`.
15. **Preflight `SET NX` TTL mismatch with DAPR resiliency policy.** Preflight TTL = 24h (Task 4.7). DAPR's default resiliency policy can retry for up to 72h (exponential backoff with large max duration). A message delayed > 24h is redelivered, the preflight key has expired, the workflow runs a second time, and a duplicate memory unit is created. **Mitigation:** (a) align TTL to `max(DAPR resiliency max-duration) + 10% buffer` OR explicitly set a resiliency policy with max-duration ≤ 23h; (b) document the TTL ↔ retry coupling in `docs/dev/eventstore-integration.md`; (c) the workflow-level permanent dedup key is authoritative (no TTL) — preflight is an optimization, not correctness; (d) guard test `EventIngestionServiceTests.PreflightTtl_DocumentedAboveDaprMaxRetry`.
16. **`MetadataOrigin.Ai` misused for envelope-derived fields corrupts downstream UX.** The Memories UI treats `origin = ai` as "LLM-derived — user should verify." Tagging `cloudevent.id`/`type`/`subject`/`aggregateType` with `MetadataOrigin.Ai` labels deterministic parse results as "AI-generated," producing spurious "verify this?" affordances for every event-sourced memory unit. **Mitigation:** (a) use a non-AI origin (`MetadataOrigin.System` if it exists in `Hexalith.Memories.Contracts`; else introduce it as part of Task 2.10); (b) `Ai` origin is reserved for LLM inference (Story 9.2 NL description); (c) guard test `CloudEventToIngestionInputMapperTests.EnvelopeFields_UseSystemOrigin_NotAi`.
17. **Publisher spoofing via unauthenticated `source` field.** Any process with DAPR pub/sub write access on the shared Redis component can publish CloudEvents with arbitrary `source` strings. An insider or compromised service publishing `source=enterprise.hr` routes to tenant `hr-tenant` without any authentication check — `source` is an auth-like boundary with no auth. **Mitigation:** (a) document this as an explicit threat in Dev Notes "Publisher trust & spoofing"; (b) MVP recommendation: restrict DAPR pub/sub component scope via `publishAllowedTopics` and component-level access control (a deploy-time hardening, not app-layer); (c) Phase 2 evolution: signed JWT in a CloudEvents extension attribute (`tenantidtoken`) verified against a tenant public key in TenantRegistry — out of scope for 9.1; (d) no guard test (threat is deploy-time, not code-time).
18. **`TimeProvider` DI registration omission produces NRE at first event.** The mapper takes `TimeProvider` to avoid `DateTimeOffset.UtcNow`, but `TimeProvider.System` is NOT registered in DI by default. If a dev adds the EventStore project reference but forgets the `builder.Services.AddSingleton(TimeProvider.System)` line, the first runtime event throws `NullReferenceException` deep in the controller → service → mapper call chain — a silent misconfiguration that passes unit tests (tests inject a fake `TimeProvider`). **Mitigation:** (a) Task 5.2's `AddSingleton(TimeProvider.System)` is a primary bullet, not parenthetical; (b) `AddMemoriesEventStoreIntegration` self-registers `TimeProvider.System` via `TryAddSingleton<TimeProvider>(TimeProvider.System)` so Server consumers don't need to; (c) guard test `EventStoreIntegrationServiceCollectionExtensionsTests.RegistersTimeProvider_WhenNotAlreadyRegistered`.

**Risk → Guard test mapping:**

| # | Risk | Guard test |
|---|------|-----------|
| 1 | `UseCloudEvents()` breaks existing `/api/ingest` | `IngestEndpointTests.PlainJsonPost_BypassesCloudEventsUnwrap` |
| 2 | At-least-once redelivery race | `EventIngestionServiceTests.PreflightSetNx_ReturnsDuplicate_WhenKeyAlreadyExists` + `.PreflightSetNx_SchedulesWorkflow_WhenKeyAcquired` + `.PreflightSetNx_FailsOpen_OnRedisOutage` + `EventIngestionTests.ConcurrentRedeliveries_ResultInSingleMemoryUnit` (Tier 3 stress — validates the whole path) |
| 3 | Missing `subject` field | `CloudEventToIngestionInputMapperTests.SubjectMissing_MetadataShowsExplicitUnset_NoCrash` |
| 4 | Typo in `SourceToTenantMap` routes to wrong tenant | `TenantEventRouterTests.SourceWithNoMapping_DropsWithWarning_Returns200` |
| 5 | Unbounded case auto-creation | `TenantEventRouterTests.CaseCap_ExceededReturnsInvariantFailure` |
| 6 | Kreuzberg extraction semantics on JSON | `ExtractContentActivityTests.JsonPayload_ReturnsRawTextIdentityExtraction` |
| 7 | `CausationId` dual-source extraction | `CloudEventToIngestionInputMapperTests.ExtensionAttribute_PreferredOverEnvelopeField` + `.EnvelopeField_UsedWhenExtensionAbsent` + `.NeitherPresent_NullCausationId` |
| 8 | Replay-after-restore conflicts with idempotency | `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency` (promoted from documented-only per Murat's review) |
| 9 | NFR6 <5s p95 freshness | `EventIngestionLatencyTests.SingleEvent_P50Under3s_Enforcement` + `SingleEvent_P95Under5s_Observation` (split — p50 enforcement fails build, p95 observation emits metric only; see Task 6.9) |
| 10 | Middleware order for subscription discovery | `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic` |
| 12 | Case-insensitive source matching | `TenantEventRouterTests.SourcePrefixMatch_IsCaseInsensitive` |
| 13 | Silent source-scheme drift | `TenantEventRouterTests.UnknownSource_IncrementsMetric` |
| 14 | Subscription registration silent-fail | `EventIngestionSubscriptionDiscoveryTests.Startup_FailsFast_WhenSubscribeEndpointEmpty` |
| 15 | Preflight TTL ↔ DAPR retry-policy coupling | `EventIngestionServiceTests.PreflightTtl_DocumentedAboveDaprMaxRetry` |
| 16 | `MetadataOrigin.Ai` misuse for envelope fields | `CloudEventToIngestionInputMapperTests.EnvelopeFields_UseSystemOrigin_NotAi` |
| 17 | Publisher spoofing (threat-level, no guard test) | Deploy-time mitigation documented in `docs/dev/eventstore-integration.md` |
| 18 | `TimeProvider` DI omission | `EventStoreIntegrationServiceCollectionExtensionsTests.RegistersTimeProvider_WhenNotAlreadyRegistered` |
| AC #14b | Deleting-tenant drop (explicit coverage) | `EventIngestionOutcomeTests.DeletingTenant_Returns200_LogsWarning` |
| AC #17 | Documentation completeness | `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasAllRequiredSections` |

---

## Story

As a developer,
I want events published to DAPR pub/sub topics to be automatically discovered and ingested into memory,
So that I can get memory integration for my event-sourced system without writing mapping code.

## Acceptance Criteria

1. **Given** a DAPR pub/sub topic with CloudEvents-compliant messages **When** events are published to the topic **Then** the Memories Server auto-discovers event types from the `type` field of the CloudEvents envelope (FR59) **And** CloudEvents metadata (`source`, `type`, `subject`, `time`, `id`) is extracted and preserved as memory unit metadata entries with `origin = system` and `confidence = 1.0` (NFR21; `system` introduced in Task 2.10 — see Risk #16) **And** `sourceType = event` is set on the resulting memory unit.

2. **Given** the system receives a CloudEvents message **When** the envelope is parsed **Then** the CloudEvents `id` field is used as the `IngestionInput.SourceUri` so the existing `DedupKeyBuilder.BuildKey(tenantId, caseId, cloudEventId)` + `CheckIdempotencyActivity` flow deduplicates **And** the CloudEvents `subject` field (aggregate ID) is persisted in the memory unit's metadata as `cloudevent.subject` for filtering-by-aggregate queries.

3. **Given** the same event (identical `cloudevent.id`) is delivered twice by DAPR's at-least-once mechanism **When** the second delivery is processed **Then** `CheckIdempotencyActivity` returns `IsDuplicate = true` for the second workflow instance **And** the second workflow returns early with `WasDuplicate = true` **And** exactly one memory unit exists in Redis (syntactic + vector) and FalkorDB graph **And** the duplicate response is 200 OK so DAPR does not retry.

4. **Given** events from multiple event-sourced aggregates (distinct `cloudevent.subject`) arrive on the same pub/sub topic **When** each is processed **Then** each is persisted as an independent memory unit with a unique `MemoryUnitId` **And** each memory unit's `metadata["cloudevent.subject"]` carries the originating aggregate ID **And** a FalkorDB search filtered by `cloudevent.subject = <aggregateId>` returns only that aggregate's events.

5. **Given** indexing freshness requirements (NFR6) **When** an event is published to DAPR pub/sub **Then** under normal conditions (embedding provider NOT rate-limited, FalkorDB responsive, DAPR sidecar healthy) it is searchable via hybrid search within 5 seconds of publication (measured as `cloudevent.time` → first successful `SearchService` hit).

6. **Given** a CloudEvent arrives with `CausationId` or `CorrelationId` (either as a CloudEvents extension attribute `causationid`/`correlationid` OR on the envelope payload) **When** the event is ingested **Then** `IngestionInput.CausationId` / `CorrelationId` are populated correctly **And** `IndexGraphActivity` creates `caused_by` and/or `correlated_with` edges via `BuildMergeStubNode` + `BuildMergeEdge` (this behavior is already implemented in Story 1.5 — 9.1's scope is threading the values through the mapper, NOT modifying `IndexGraphActivity`).

7. **Given** a CloudEvent arrives with a `source` that matches no entry in `TenantEventRoutingOptions.SourceToTenantMap` **When** the subscription endpoint processes it **Then** the endpoint returns 200 OK (prevents infinite DAPR retry for a publisher that will never be mapped) **And** logs a structured `Warning`-level entry `EventStoreIntegrationLog.UnknownSource(source, cloudEventId)` so operators can correct the mapping without losing the event-ID for future investigation.

8. **Given** a CloudEvent arrives with a malformed envelope (missing required `id`, `type`, or `source`) **When** the subscription endpoint processes it **Then** the endpoint returns 400 Bad Request with `ErrorResponse(code: "INVALID_CLOUDEVENT", message: <specific missing field>, suggestion: <fix hint>)` **And** DAPR does not retry (4xx).

9. **Given** `DaprWorkflowClient.ScheduleNewWorkflowAsync` fails transiently (sidecar restart mid-request) **When** the subscription endpoint handles the exception **Then** it returns 500 Internal Server Error **And** DAPR retries the delivery per its at-least-once contract **And** the first successful retry produces exactly one memory unit (verified via `cloudevent.id` dedup).

10. **Given** `TenantEventRoutingOptions.AutoCreateCases = true` **When** an event's aggregateType has no pre-existing case for the target tenant **Then** the router creates a new case via `CaseService.CreateCaseAsync` with `Name = string.Format(CaseIdTemplate, aggregateType)` (default `"events:{aggregateType}"`) **And** persists the tenant-aggregate-to-case mapping in Redis so subsequent events for the same aggregateType go to the same case **And** concurrent first-time events for the same aggregateType resolve to the same case (verified via `SET NX`).

11. **Given** `TenantEventRoutingOptions.AutoCreateCases = false` **When** an event's aggregateType has no pre-existing case **Then** the endpoint returns 200 + logs `Warning` (event dropped — operator has explicitly opted out of auto-create).

12. **Given** the `Hexalith.Memories.EventStore` package is published and a downstream service calls `services.AddMemoriesEventStoreIntegration(config)` **When** the service boots **Then** it registers: `IEventIngestionService`, `ITenantEventRouter`, `CloudEventToIngestionInputMapper`, `EventIngestionController` (via `AddControllers().AddApplicationPart`), and binds `TenantEventRoutingOptions` from `config.GetSection("EventStoreIntegration:Routing")` **And** the subscription endpoint is discoverable by DAPR at `GET /dapr/subscribe`.

13. **Given** the Memories Server boots with event-subscription enabled **When** the DAPR sidecar probes `GET /dapr/subscribe` **Then** the response includes one entry `{ pubsubname: "pubsub", topic: <configured>, route: "/events/ingest" }` matching the controller's `[Topic]` attribute **And** publishing a test event via `DaprClient.PublishEventAsync("pubsub", <topic>, testPayload, metadata: { cloudevent.id, cloudevent.source, cloudevent.type })` results in one memory unit indexed within 5 seconds (Tier 2 test).

14a. **Given** a tenant has status `Provisioning` (not yet `Active`) **When** an event destined for that tenant arrives **Then** the endpoint returns 500 so DAPR retries until the tenant becomes active OR exhausts its retry policy **And** logs `EventStoreIntegrationLog.TenantProvisioning(tenantId, cloudEventId)` at `Information` level.

14b. **Given** a tenant has status `Deleting` **When** an event destined for that tenant arrives **Then** the endpoint returns 200 so DAPR does NOT retry (events for a tenant-in-teardown are intentionally dropped) **And** logs `EventStoreIntegrationLog.TenantDeleting(tenantId, cloudEventId)` at `Warning` level.

15. **Given** all unit tests in the new project (`Hexalith.Memories.EventStore.Tests`) **When** `dotnet test tests/Hexalith.Memories.EventStore.Tests/` is run **Then** every test passes (Tier 1 — no external deps).

16. **Given** a Tier 2 integration test `EventIngestionRoundTripTests` runs against the Aspire AppHost fixture with DAPR slim init + Redis **When** the test publishes a test CloudEvent via `DaprClient.PublishEventAsync` and polls search APIs **Then** the test observes the event as a memory unit with correct metadata, graph edges (if CausationId/CorrelationId present), and 200 search hits within 5 seconds.

17. **Given** `docs/dev/eventstore-integration.md` exists **When** a developer reads it **Then** they find: setup steps (AddMemoriesEventStoreIntegration + pubsub.yaml + appsettings section), CloudEvents envelope requirements, tenant/case routing config schema, aggregateType extraction rules, a worked example (Hexalith.EventStore `CounterIncrementedV1` → memory unit with `caused_by` edge), at-least-once + dead-letter + replay semantics, troubleshooting ("why didn't my event appear?"), **Preflight TTL ↔ DAPR retry-policy alignment** (Risk #15), **Publisher trust & spoofing threat model + deploy-time mitigations** (Risk #17), **Source-stability publisher contract** (Risk #13), **Alerting recommendations** (Risks #5, #13, #14 + preflight fail-open), and **Environment defaults table** (ADR 9.1-C Dev vs Production `AutoCreateCases` split).

## Tasks / Subtasks

- [ ] Task 1: Create `Hexalith.Memories.EventStore` project + wire into solution (AC: #12)
  - [ ] 1.1 Create `src/Hexalith.Memories.EventStore/Hexalith.Memories.EventStore.csproj` (`Microsoft.NET.Sdk`, `net10.0`, `IsPackable=true`, description + authors per existing Contracts csproj shape)
  - [ ] 1.2 Reference `Hexalith.Memories.Contracts`, `Dapr.AspNetCore`, `Dapr.Client`, `Dapr.Workflow`, `Microsoft.AspNetCore.Mvc.Core`
  - [ ] 1.3 Add project to `Hexalith.Memories.slnx` (solution folder `src`)
  - [ ] 1.4 Create empty `README.md` at the project root (required by NuGet publishing flow; content: one-line summary + link to `docs/dev/eventstore-integration.md`)
  - [ ] 1.5 Add `InternalsVisibleTo("Hexalith.Memories.EventStore.Tests")` + `InternalsVisibleTo("Hexalith.Memories.Server")` + `InternalsVisibleTo("Hexalith.Memories.IntegrationTests")` + `InternalsVisibleTo("DynamicProxyGenAssembly2")` to the csproj. **Additional `InternalsVisibleTo` entries are required because per ADR 9.1-F the MVP public API surface is intentionally minimal** — only `AddMemoriesEventStoreIntegration`, `TenantEventRoutingOptions`, and `TenantEventRoute` are `public`; all other types (`ITenantEventRouter`, `IEventIngestionService`, `EventIngestionController`, `CloudEventToIngestionInputMapper`, `EventIngestionOutcome`, `ICaseCreationService`, `AggregateTypeExtractor`, `EventStoreIntegrationLog`) are `internal`. The Server + test assemblies need `InternalsVisibleTo` to consume them.
  - [ ] 1.6 Create `tests/Hexalith.Memories.EventStore.Tests/Hexalith.Memories.EventStore.Tests.csproj` (`Microsoft.NET.Sdk`, `net10.0`, `IsPackable=false`) with xUnit 2.9.3 + Shouldly 4.3.0 + NSubstitute 5.3.0 + coverlet.collector references per Directory.Packages.props
  - [ ] 1.7 Add test project to solution + add ProjectReference to `Hexalith.Memories.EventStore.csproj` + `Hexalith.Memories.Contracts.csproj`

- [ ] Task 2: Author `CloudEventToIngestionInputMapper` (AC: #1, #2, #6)
  - [ ] 2.1 Create `src/Hexalith.Memories.EventStore/CloudEventToIngestionInputMapper.cs` — **`internal` sealed static class** (per ADR 9.1-F public-API-surface decision; exposed to Server + tests via `InternalsVisibleTo`) with `Map(CloudEvent<JsonElement> evt, ITenantEventRouter router, TimeProvider timeProvider, CancellationToken ct) : Task<IngestionInput>` returning an `IngestionInput` ready for `ScheduleNewWorkflowAsync`
  - [ ] 2.2 Validate required CloudEvents fields: `id`, `type`, `source` — throw `InvalidOperationException("cloudevent.<field> missing")` on any absence (caught by controller → 400 response)
  - [ ] 2.3 Extract `aggregateType` from `evt.Type` (second dotted segment: `"MyApp.Claims.ClaimSubmittedV2"` → `"Claims"`); when `evt.Type` has fewer than 3 segments, use the whole type as aggregateType
  - [ ] 2.4 Call `router.ResolveTenantAndCaseAsync(evt, ct)` → `(tenantId, caseId)` tuple
  - [ ] 2.5 Extract `CausationId`/`CorrelationId` via dual path: first try `evt.GetExtensionAttribute<string>("causationid")` / `"correlationid"`; if null, try `evt.Data.TryGetProperty("causationId", out var el) && el.ValueKind == JsonValueKind.String` — store `el.GetString()`; else `null`
  - [ ] 2.6 Populate `IngestionInput.Metadata` with **system-origin** fields (all `MetadataOrigin.System`, confidence `1.0f` — NOT `MetadataOrigin.Ai`; see Task 2.10 + Risk #16):
    - `cloudevent.id`, `cloudevent.source`, `cloudevent.type`, `cloudevent.subject` (or `"(unset)"` when null), `cloudevent.time` (ISO-8601 when present, else `"(unset)"`), `cloudevent.specversion`, `event.aggregateType`
  - [ ] 2.7 Populate `IngestionInput.ContentBytes = Encoding.UTF8.GetBytes(evt.Data.GetRawText())`, `ContentType = evt.DataContentType ?? "application/json"`, `SourceType = SourceType.Event`, `SourceUri = evt.Id`, `IngestedBy = "events"`
  - [ ] 2.8 Unit-test every branch via NSubstitute mocks: present `subject` + absent `subject`; extension `causationid` + envelope `CausationId` + neither; CloudEvents 1.0 + future 1.1; malformed `type` (no dots) falls back to whole type as aggregateType
  - [ ] 2.9 DO NOT call `DateTimeOffset.UtcNow` inside the mapper — take a `TimeProvider`; tests inject `TimeProvider.System` (or a fake for determinism)

  - [ ] 2.10 **Introduce `MetadataOrigin.System` in `Hexalith.Memories.Contracts` (Risk #16 mitigation).** Current enum values: `Human`, `Ai`. Add a third value `System` for envelope-derived/parse-result fields that are deterministic (not AI-inferred). Steps:
    - Edit `src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs` — add `System` value (additive, non-breaking).
    - Verify existing consumers in `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`, `src/Hexalith.Memories.Server/Cases/CaseService.cs`, and `src/Hexalith.Memories.Cli/Output/Formatters/*` handle the new value gracefully (switch-exhaustiveness / fallthrough — no panics on unknown enum).
    - Update the `CamelCaseStringEnumConverter<MetadataOrigin>` JSON serialization test to include `System` ↔ `"system"` round-trip.
    - Update `docs/dev/metadata-origin.md` (if it exists; else inline in the contracts XML doc comment) to document the semantic: `Human` = user-supplied; `System` = deterministic parse/derivation (envelope fields, filename heuristics); `Ai` = LLM-inferred, user-verifiable.
    - DO NOT rename `Ai` → anything else (that would be breaking). The `Ai` value stays reserved for Story 9.2.
    - Guard test: `CloudEventToIngestionInputMapperTests.EnvelopeFields_UseSystemOrigin_NotAi` asserts all envelope-derived metadata fields carry `MetadataOrigin.System`.

- [ ] Task 3: Author `TenantEventRouter` + `TenantEventRoutingOptions` (AC: #7, #10, #11)
  - [ ] 3.1 Create `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs` — sealed record with `PubSubName` (default `"pubsub"`), `Topic` (required), `SourceToTenantMap` (`Dictionary<string, string>`), `AutoCreateCases` (default `true` in `Development`, `false` in `Production` — see Task 5.6), `CaseIdTemplate` (default `"events:{aggregateType}"`), `MaxAutoCreatedCasesPerTenant` (default `100`), `PreflightDedupEnabled` (default `true` — see Task 4.7), `PreflightDedupTtl` (default `TimeSpan.FromHours(24)` — see Task 4.7 + Risk #15)
  - [ ] 3.2 Create `src/Hexalith.Memories.EventStore/ITenantEventRouter.cs` interface: `Task<TenantEventRoute?> ResolveRouteAsync(CloudEvent<JsonElement> evt, CancellationToken ct)` returning `(string TenantId, string CaseId)` or `null` when the event must be dropped
  - [ ] 3.3 Create `src/Hexalith.Memories.EventStore/TenantEventRouter.cs` implementing the interface:
    - Resolve tenantId: longest-prefix, case-insensitive match on `evt.Source.ToString()` against `SourceToTenantMap.Keys`. No match → return `null` + log `EventStoreIntegrationLog.UnknownSource`
    - Validate tenant: call `TenantStatusGuard.ValidateTenantActiveAsync(tenantId, ct)`. Non-active tenant: log + return `null`; controller maps this to 200 (intentional drop) per AC #14 — pass the `TenantStatus` back so the controller can distinguish `Deleting` (drop, 200) from `Provisioning` (retry, 500)
    - Resolve aggregateType via mapper (Task 2.3 helper — promote to a shared `AggregateTypeExtractor.Extract(string cloudEventType)` static method so both mapper and router use the same logic)
    - Resolve caseId: Redis key `events:caseMap:{tenantId}:{aggregateType}`; `GET` it; if present return; if absent AND `AutoCreateCases = true`, call `ICaseCreationService.CreateCaseAsync` via the injected interface (NOT `CaseService` directly — see Task 3.6 for the dependency-inversion rationale), store the new caseId via `SET NX` (if `SET NX` fails due to concurrent creation, re-`GET` to pick up the winner's caseId), enforce `MaxAutoCreatedCasesPerTenant` cap before create (use `SCARD events:autoCases:{tenantId}` + `SADD`); if absent AND `AutoCreateCases = false`, return `null`
  - [ ] 3.4 Startup validation hook: implement `IHostedService` that runs once at startup — iterates `SourceToTenantMap.Values` and calls `TenantRegistryService.GetAsync` on each; fails fast (`app.Services.GetRequiredService<IHostLifetime>()` stop) if any tenant doesn't exist. Log `EventStoreIntegrationLog.RoutingConfigValidated(tenantCount)` on success.
  - [ ] 3.5 Unit tests: `TenantEventRouterTests.SourcePrefixMatch_IsCaseInsensitive`, `.SourceWithNoMapping_DropsWithWarning`, `.InactiveTenant_DroppedForDeleting_RetriesForProvisioning`, `.AutoCreateOff_MissingCase_ReturnsNull`, `.AutoCreateOn_ConcurrentFirstEvents_ResolveSameCase`, `.CaseCap_ExceededReturnsInvariantFailure`, `.StartupValidation_UnknownTenantInMap_StopsApp`

  - [ ] 3.6 **Introduce `ICaseCreationService` to break reverse dependency.** `TenantEventRouter` lives in `Hexalith.Memories.EventStore`; `CaseService` lives in `Hexalith.Memories.Server`. A direct reference would force `EventStore → Server`, which reverses the intended package direction (Server consumes EventStore as a NuGet). Mitigation:
    - Define `ICaseCreationService` in `src/Hexalith.Memories.EventStore/ICaseCreationService.cs` with a single method: `Task<string> CreateCaseAsync(string tenantId, string caseName, CancellationToken ct)` returning the new caseId.
    - `CaseService` in the Server project implements `ICaseCreationService` via a thin adapter (`CaseCreationServiceAdapter.cs` in `src/Hexalith.Memories.Server/Cases/`) that delegates to the existing `CaseService.CreateCaseAsync` — NO new domain logic in the adapter.
    - `AddMemoriesEventStoreIntegration` does NOT register `ICaseCreationService` (EventStore doesn't know about `CaseService`); the Server registers the adapter in its own `ConfigureServices`: `services.AddScoped<ICaseCreationService, CaseCreationServiceAdapter>()`.
    - Unit test: `CaseCreationServiceAdapterTests.CreateCaseAsync_DelegatesToCaseService` (Server test project).

- [ ] Task 4: Author `EventIngestionService` + `EventIngestionController` (AC: #2, #3, #5, #7, #8, #9, #13, #14a, #14b)
  - [ ] 4.0 **Verification spike (≤ 30 min, MUST run before 4.1).** Stand up a throwaway minimal-API sample referencing `Dapr.AspNetCore` 1.17.6 and empirically verify which `[Topic]` binding path `MapSubscribeHandler` actually reads. Test matrix: (a) class-level `[Topic("pubsub","fixed-name")]` on controller → confirmed baseline; (b) class-level `[Topic("pubsub","$(ENV_VAR)")]` with env-var substitution → verify substitution happens at attribute-read time; (c) `endpoints.MapPost("/events/ingest", ...).WithMetadata(new TopicAttribute("pubsub","from-options"))` on a minimal API → **this is the one Task 4.4 is uncertain about**. Run daprd, `GET /dapr/subscribe`, record which paths produce an entry. **Deliverable:** one-paragraph finding written into the story's `## Decisions` section under ADR 9.1-A. If path (c) fails and path (b) works, use env-var substitution for MVP (revise Task 4.4 accordingly). If both fail, escalate — the story cannot proceed until topic binding is confirmed.
  - [ ] 4.1 Create `src/Hexalith.Memories.EventStore/IEventIngestionService.cs` + `EventIngestionService.cs`: constructor-injects `ITenantEventRouter`, `TimeProvider`, `DaprWorkflowClient`, `ILogger<EventIngestionService>`; method `IngestAsync(CloudEvent<JsonElement> evt, CancellationToken ct) : Task<EventIngestionOutcome>` returning an enum `{ Accepted, Duplicate, TenantUnknown, TenantDeleting, TenantProvisioning, InvalidEnvelope, TransientFailure, AutoCreateDisabled, CaseCapExceeded }`
  - [ ] 4.2 Inside `IngestAsync`: call `CloudEventToIngestionInputMapper.Map` → `IngestionInput`; call `DaprWorkflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), input)`; wrap in try/catch with specific handling for `DaprException`/`InvalidOperationException`/`TenantStatusException` returning the appropriate outcome
  - [ ] 4.3 Create `src/Hexalith.Memories.EventStore/EventIngestionController.cs` — `[ApiController]` + `[Route("events")]`. Single endpoint `[HttpPost("ingest")]` + `[Topic("pubsub", "<configured topic>")]`. Input: `[FromBody] CloudEvent<JsonElement>` (CloudEvents middleware unwraps envelope). Calls `_eventIngestionService.IngestAsync`; maps the outcome enum to HTTP response per the table:
    | Outcome | HTTP | Body | DAPR retry |
    |---|---|---|---|
    | Accepted | 200 | `{ instanceId }` | No (success) |
    | Duplicate | 200 | `{ instanceId, wasDuplicate: true }` | No |
    | TenantUnknown | 200 | `{ droppedReason: "unknown tenant" }` | No (intentional drop) |
    | TenantDeleting | 200 | `{ droppedReason: "tenant deleting" }` | No |
    | TenantProvisioning | 500 | `ErrorResponse("TENANT_PROVISIONING", ...)` | Yes |
    | InvalidEnvelope | 400 | `ErrorResponse("INVALID_CLOUDEVENT", ...)` | No (4xx) |
    | TransientFailure | 500 | `ErrorResponse("TRANSIENT_FAILURE", ...)` | Yes |
    | AutoCreateDisabled | 200 | `{ droppedReason: "auto-create off" }` | No |
    | CaseCapExceeded | 400 | `ErrorResponse("CASE_CAP_EXCEEDED", ...)` | No (explicit config guard) |
  - [ ] 4.4 **Topic binding — decided design (NOT a spike):** register the subscription programmatically via endpoint metadata so the topic name is config-driven. In `EventStoreIntegrationServiceCollectionExtensions.AddMemoriesEventStoreIntegration`, resolve `IOptions<TenantEventRoutingOptions>` at `UseEndpoints` time and call `endpoints.MapPost("/events/ingest", handler).WithMetadata(new TopicAttribute(options.PubSubName, options.Topic))`. The controller's `[HttpPost("ingest")]` handler stays; the `[Topic(...)]` attribute is REMOVED from the controller class and attached dynamically via `WithMetadata`. DAPR's `/dapr/subscribe` discovery reads the endpoint metadata — confirmed pattern in `Dapr.AspNetCore` 1.17.6 (`MapSubscribeHandler` enumerates endpoints and their `TopicAttribute` metadata regardless of whether the attribute was declared or attached). If implementation reveals `MapSubscribeHandler` only reads class-level attributes on controllers, fall back to reading `MEMORIES_EVENTSTORE_TOPIC` env var and apply via `[Topic("pubsub", "$(MEMORIES_EVENTSTORE_TOPIC)")]` at the controller — env-var substitution IS supported by Dapr.AspNetCore at attribute-read time.
  - [ ] 4.5 Emit telemetry via `EndpointTelemetryScope` (reuse from Server); `TagSourceType = "event"`; eventIds use existing `7502`/`7512` bank (AC ingestion telemetry)
  - [ ] 4.6 Unit tests: `EventIngestionServiceTests` — every outcome branch; controller tests `EventIngestionControllerTests` — every HTTP mapping via minimal API integration (use `WebApplicationFactory<Program>`-style test or route the controller through a test harness)

  - [ ] 4.7 **Pre-schedule dedup `SET NX` (Risk #2 mitigation — feature-flagged, default-on).** Before `EventIngestionService.IngestAsync` calls `DaprWorkflowClient.ScheduleNewWorkflowAsync`, perform an atomic `SET NX` against the dedup key **when `TenantEventRoutingOptions.PreflightDedupEnabled = true` (default)**:
    - Compute `dedupKey = DedupKeyBuilder.BuildKey(tenantId, caseId, evt.Id)` (reuses existing builder — no key-format divergence).
    - `bool acquired = await _redis.StringSetAsync(dedupKey, workflowInstanceIdPlaceholder, expiry: preflightTtl, when: When.NotExists);`
    - `preflightTtl` is configured via `TenantEventRoutingOptions.PreflightDedupTtl` (default `TimeSpan.FromHours(24)`). **MUST be ≥ DAPR resiliency policy max-retry-duration + 10% buffer** (Risk #15) — validated at startup by `TenantEventRoutingOptionsValidator`.
    - If `acquired == false` → return `EventIngestionOutcome.Duplicate` immediately (200 + `wasDuplicate: true`). Do NOT schedule the workflow.
    - If `acquired == true` → schedule the workflow. The workflow's existing `CheckIdempotencyActivity` + `SaveDedupKeyActivity` remain as a secondary safety net (covers the narrow case where the endpoint-level SET NX TTL expires before the workflow writes the final dedup key). The workflow-level permanent dedup key is **authoritative** — preflight is a compute-saving optimization, not correctness.
    - Rationale: eliminates the "two workflow instances concurrently doing extract → embed → index before either writes the dedup key" cost. This is ~20 lines at the endpoint layer and removes ~1-3 seconds of duplicate work per redelivery race. **Default-on** because DAPR at-least-once redelivery rates under load make Story 1.6's "accepted MVP race" untenable for event ingestion. **Feature-flagged** because the optimization couples the subscription endpoint to Redis availability (with fail-open) and adds a maintenance surface — operators can disable it if Redis becomes a bottleneck.
    - When `PreflightDedupEnabled = false`: skip the `SET NX` call entirely, proceed directly to `ScheduleNewWorkflowAsync`, and rely on workflow-level `CheckIdempotencyActivity`/`SaveDedupKeyActivity` for correctness (matches Story 1.6's posture).
    - Inject `IConnectionMultiplexer` (or an `IDedupKeyPreflight` abstraction over it) into `EventIngestionService`.
    - Unit test: `EventIngestionServiceTests.PreflightSetNx_ReturnsDuplicate_WhenKeyAlreadyExists`; `PreflightSetNx_SchedulesWorkflow_WhenKeyAcquired`; `PreflightSetNx_FailsOpen_OnRedisOutage` (falls through to scheduling with a warning log — the workflow's own dedup covers correctness; this trades a performance-optimization for availability when Redis is temporarily unreachable); `PreflightSetNx_Disabled_SkipsRedisCall` (verifies the feature flag short-circuits); `PreflightTtl_DocumentedAboveDaprMaxRetry` (validator test — startup fails fast if TTL < DAPR max-retry-duration).

- [ ] Task 5: Wire into Memories Server + AppHost (AC: #12, #13)
  - [ ] 5.1 Add `ProjectReference` to `Hexalith.Memories.EventStore` from `Hexalith.Memories.Server.csproj`
  - [ ] 5.2 In `src/Hexalith.Memories.Server/Program.cs`, register (order matters — primary bullets, not parenthetical):
    - [ ] 5.2.1 `builder.Services.AddControllers().AddApplicationPart(typeof(EventIngestionController).Assembly)` — assembly-scanning registration of the controller.
    - [ ] 5.2.2 `builder.Services.AddMemoriesEventStoreIntegration(builder.Configuration)` — binds options + registers router/service/mapper. **Self-registers `TryAddSingleton<TimeProvider>(TimeProvider.System)` internally** (Risk #18 mitigation) so consumers do not silently NRE on first event.
    - [ ] 5.2.3 `builder.Services.AddScoped<ICaseCreationService, CaseCreationServiceAdapter>()` — Server-side implementation of the EventStore-declared interface (see Task 3.6).
    - [ ] 5.2.4 Verify `TimeProvider` is resolvable after boot: add a startup assertion in `RoutingConfigValidationHostedService` that `IServiceProvider.GetRequiredService<TimeProvider>()` succeeds; fail-fast with `EventStoreIntegrationLog.TimeProviderMissing` if not.
  - [ ] 5.3 In `Program.cs` middleware chain (after `app.MapDefaultEndpoints()`, before any minimal-API `MapPost`): add `app.UseCloudEvents()`; then `app.MapControllers()`; then `app.MapSubscribeHandler()` (order per Hexalith.EventStore reference)
  - [ ] 5.4 In `src/Hexalith.Memories.AppHost/Program.cs`: add `IResourceBuilder<IDaprComponentResource> pubSub = builder.AddDaprComponent("pubsub", "pubsub.redis").WithMetadata("redisHost", "127.0.0.1:6379").WithMetadata("redisPassword", string.Empty)`; extend `server.WithDaprSidecar(sidecar => ... .WithReference(pubSub))`
  - [ ] 5.5 Create `deploy/dapr/components/pubsub.yaml` with the component definition shown in "What 9.1 adds" section above — needed for non-Aspire deployments (Kubernetes, manual DAPR runs)
  - [ ] 5.6 Add environment-specific `EventStoreIntegration:Routing` config (per ADR 9.1-C — default split by environment):
    - [ ] 5.6.1 `appsettings.Development.json` section: dev-default topic name (`"hexalith.memories.events"`), empty `SourceToTenantMap` (documented empty-map semantics: "when empty, ALL events are dropped with `UnknownSource` log — configure SourceToTenantMap to enable routing"), **`AutoCreateCases: true`** (zero-code DX for local exploration), `PreflightDedupEnabled: true`.
    - [ ] 5.6.2 `appsettings.Production.json` section (NEW file — create if it doesn't exist): **`AutoCreateCases: false`** (production favors explicit case provisioning), `PreflightDedupEnabled: true`, `MaxAutoCreatedCasesPerTenant: 100`, `PreflightDedupTtl: "23:00:00"` (23h — tuned to be less than DAPR's default 24h retry-policy max to make the TTL↔retry coupling explicit per Risk #15).
    - [ ] 5.6.3 Document the environment split in `docs/dev/eventstore-integration.md` with a table showing which defaults apply where.
    - [ ] 5.6.4 Guard test `TenantEventRoutingOptionsValidatorTests.ProductionDefaults_AutoCreateCases_IsFalse` — loads `appsettings.Production.json` via `ConfigurationBuilder` and asserts `AutoCreateCases == false`. Prevents accidental re-introduction of auto-create in production.
  - [ ] 5.7 Wire `Hexalith.Memories.EventStore` types into `MemoriesJsonContext`: `[JsonSerializable(typeof(TenantEventRoutingOptions))]` + `[JsonSerializable(typeof(TenantEventRoute))]` — AOT compatibility

- [ ] Task 6: Integration tests (AC: #5, #13, #16)
  - [ ] 6.1 Create `tests/Hexalith.Memories.IntegrationTests/EventIngestionRoundTripTests.cs` — Tier 3 Aspire-hosted test using `DistributedApplicationTestingBuilder`
  - [ ] 6.2 Test `EventIngestionRoundTripTests.PublishSingleEvent_IngestsAsMemoryUnit_Under5Seconds` — publishes via `DaprClient.PublishEventAsync("pubsub", topic, payload, metadata)` with `cloudevent.id` + `cloudevent.source` + `cloudevent.type` + `cloudevent.subject` metadata; polls `GET /api/search` until the memory unit appears or the 5-second budget is exhausted
  - [ ] 6.3 Test `EventIngestionRoundTripTests.PublishWithCausationId_CreatesCausedByEdge` — publishes two events where B's `causationid` extension = A's `cloudevent.id`; asserts `caused_by` edge exists in FalkorDB via `GET /api/tenants/{tenant}/cases/{case}/memory-units/{id}/traverse?direction=in&edgeType=causedBy`
  - [ ] 6.4 Test `EventIngestionRoundTripTests.ConcurrentRedeliveries_ResultInSingleMemoryUnit` — publishes the same event 5 times in parallel; asserts exactly one memory unit exists
  - [ ] 6.5 Test `EventIngestionSubscriptionDiscoveryTests.DaprSubscribeEndpoint_ListsConfiguredTopic` — performs `GET http://localhost:<app-port>/dapr/subscribe` and asserts one entry matching the configured topic
  - [ ] 6.6 (Tier 2 skip-path) If the Aspire fixture is not available, ship a minimum Tier 2 test `EventIngestionMinimalTests` that uses `WebApplicationFactory<Program>` + a stubbed `DaprWorkflowClient` and exercises the controller + mapper end-to-end — Tier 3 integration remains optional per Story 1.6's skip pattern

  - [ ] 6.7 Test `EventIngestionOutcomeTests.DeletingTenant_Returns200_LogsWarning` (AC #14b coverage — previously implicit; now explicit). Arrange: stub `TenantStatusGuard` to return `TenantStatus.Deleting`; publish an event. Assert: HTTP 200, `EventStoreIntegrationLog.TenantDeleting` emitted at `Warning` level, no workflow scheduled.

  - [ ] 6.8 Test `EventIngestionReplayAfterRestoreTests.ReplayedEvent_AfterTenantRestore_BlockedByIdempotency` — documents-only risk #8 promoted to a proveable negative test. Arrange: ingest an event; simulate tenant teardown + restore (delete the tenant's memory units but NOT the dedup key); re-publish the same CloudEvent. Assert: workflow returns early (`wasDuplicate: true`), no memory unit is created. Proves the documented behavior ("replay requires operator to delete dedup keys") holds.

  - [ ] 6.9 **Latency test split — enforcement vs observation.** Rewrite `EventIngestionLatencyTests.SingleEvent_P95IndexingFreshness_Under5s` as TWO assertions: (a) p50 < 3s — ENFORCEMENT; fails the build if exceeded. (b) p95 < 5s — OBSERVATION; emits a warning log + metric but does NOT fail the build. Rationale: p95 assertions under CI load are flaky; enforce only the budget that correlates with dev-environment steady-state, observe the one that correlates with NFR6's under-load guarantee. Document the split in `docs/dev/eventstore-integration.md` under "Known test-reliability trade-offs".

  - [ ] 6.10 Test `DocumentationCompletenessTests.EventStoreIntegrationDoc_HasAllRequiredSections` — markdown-lint-style check asserting `docs/dev/eventstore-integration.md` contains level-2 headers for the sections enumerated in AC #17 (Setup, CloudEvents envelope requirements, Routing config schema, aggregateType extraction, Worked example, At-least-once + dead-letter + replay semantics, Troubleshooting, Preflight TTL ↔ DAPR retry alignment, Publisher trust & spoofing, Source-stability publisher contract + unknown-source alerting). Runs as a unit test (no external deps); parses the markdown file from the repo root.

  - [ ] 6.11 Test `MiddlewareOrderTests.CloudEventsScopedOrGlobalNoOpOnPlainJson` — verifies that `app.UseCloudEvents()` is a no-op for requests with `Content-Type: application/json` (non-`cloudevents+json`). Arrange: minimal `WebApplicationFactory<Program>` harness; send `POST /api/ingest` with a plain-JSON body containing binary payload bytes. Assert: the request body reaches the ingest handler unmodified (byte-for-byte equality). Closes Risk #1 with a CI-level assertion rather than a conceptual note.

  - [ ] 6.12 Test `EventIngestionSubscriptionDiscoveryTests.Startup_FailsFast_WhenSubscribeEndpointEmpty` — arrange: start the Server with a deliberately-broken topic binding (e.g., empty topic name, no `[Topic]` attribute on any controller). Assert: the `RoutingConfigValidationHostedService` (or an equivalent `IHostedService`) detects that `GET /dapr/subscribe` returns `[]`, logs `EventStoreIntegrationLog.SubscriptionRegistrationFailed` at `Critical`, and calls `IHostApplicationLifetime.StopApplication()`. Closes Risk #14 (silent non-registration) with fail-fast startup behavior.

  - [ ] 6.13 Test `EventStoreIntegrationServiceCollectionExtensionsTests.RegistersTimeProvider_WhenNotAlreadyRegistered` — arrange: bare `ServiceCollection`, call `AddMemoriesEventStoreIntegration`. Assert: `IServiceProvider.GetRequiredService<TimeProvider>()` returns non-null (resolves to `TimeProvider.System`). Also test `PreservesExistingTimeProviderRegistration` — when `TimeProvider` is pre-registered (e.g., a fake in tests), `TryAddSingleton` does NOT overwrite it. Closes Risk #18.

- [ ] Task 7: Documentation (AC: #17)
  - [ ] 7.1 Create `docs/dev/eventstore-integration.md` with the sections listed in AC #17 **PLUS** the following additional sections added during 9.1 refinement:
    - [ ] 7.1.1 **"Preflight TTL ↔ DAPR retry-policy alignment"** — document that `PreflightDedupTtl` MUST be ≥ DAPR resiliency policy max-retry-duration, explain the Risk #15 failure mode (delayed redelivery producing duplicates if TTL expires), include a worked example comparing 24h TTL vs custom 72h DAPR resiliency policy.
    - [ ] 7.1.2 **"Publisher trust & spoofing"** — mirror the Dev Notes threat-model section; explicitly state `source` is NOT an authentication boundary; list the 4 deploy-time mitigations (component scope restrictions, network segmentation, DAPR API token, operational alerting) as REQUIRED, not optional; reference Phase 2 JWT extension as a future evolution.
    - [ ] 7.1.3 **"Source-stability publisher contract"** — publishers MUST treat their configured `source` as a stable identifier, not a deploy-time URL; scheme/path drift (Risk #13) causes silent routing failures; include example of a safe `source` (e.g., `urn:hexalith:enterprise:claims`) vs a brittle one (`https://claims.prod.hexalith.io/`).
    - [ ] 7.1.4 **"Alerting recommendations"** — required alert rules: (a) `rate(memories_eventstore_unknownsource_total[5m]) > 0` for > 5 min → page (Risk #13); (b) new-aggregate-type observed (first time ever, per tenant) → notify (Risk #5 backstop); (c) `memories_eventstore_preflight_failopen_total` > 0 → investigate Redis health (Task 4.7); (d) subscription-discovery empty at startup → fail build/deploy (Risk #14).
    - [ ] 7.1.5 **"Environment defaults table"** — showing Development (`AutoCreateCases=true`) vs Production (`AutoCreateCases=false`) per ADR 9.1-C, with rationale.
  - [ ] 7.2 Add a section "Known limitations" that explicitly documents: single-topic subscription, no dead-letter replay workflow, auto-created case cap, replay-vs-idempotency semantics, clock-skew-in-cloudevent.time, preflight TTL coupling, publisher-source trust assumption.
  - [ ] 7.3 Add a section "Testing your integration" with a curl-equivalent: `dapr publish --publish-app-id memories-server --pubsub pubsub --topic <configured> --data '{"claimId":"c-123"}' --metadata '{"cloudevent.id":"evt-1","cloudevent.type":"MyApp.Claims.ClaimSubmittedV1","cloudevent.source":"MyApp/claims","cloudevent.subject":"c-123"}'` followed by verification via `memories search --tenant <t> --case events:Claims "claimId"`
  - [ ] 7.4 Link the doc from `README.md` under the "Integration guides" section (or create that section if it doesn't exist)

- [ ] Task 8: Sprint status + retro entry (AC: #12)
  - [ ] 8.1 Update `_bmad-output/implementation-artifacts/sprint-status.yaml`: `epic-9` → `in-progress`; `9-1-event-auto-discovery-and-dapr-pub-sub-subscription` → `ready-for-dev` (this task completed by the create-story workflow)
  - [ ] 8.2 After dev completion (when dev runs `dev-story`): update `last_updated` to the current date + a one-line summary of what landed
  - [ ] 8.3 If any Risk #1-#12 guard tests are skipped or deferred, add entries to `_bmad-output/implementation-artifacts/deferred-work.md` with rationale

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

```csharp
// CloudEvent<JsonElement> → IngestionInput
var input = new IngestionInput
{
    TenantId = route.TenantId,
    CaseId = route.CaseId,
    SourceUri = evt.Id,                                                 // dedup key
    ContentBytes = Encoding.UTF8.GetBytes(evt.Data!.Value.GetRawText()),
    ContentType = evt.DataContentType ?? "application/json",
    SourceType = SourceType.Event,
    IngestedBy = "events",
    // All envelope-derived fields use MetadataOrigin.System (new enum value — Task 2.10).
    // MetadataOrigin.Ai is reserved for Story 9.2's LLM-generated NL description (Risk #16).
    Metadata = new Dictionary<string, MetadataField>
    {
        ["cloudevent.id"]          = new(evt.Id, MetadataOrigin.System, 1.0f),
        ["cloudevent.source"]      = new(evt.Source.ToString(), MetadataOrigin.System, 1.0f),
        ["cloudevent.type"]        = new(evt.Type, MetadataOrigin.System, 1.0f),
        ["cloudevent.subject"]     = new(evt.Subject ?? "(unset)", MetadataOrigin.System, 1.0f),
        ["cloudevent.time"]        = new(evt.Time?.ToString("o") ?? "(unset)", MetadataOrigin.System, 1.0f),
        ["cloudevent.specversion"] = new(evt.SpecVersion ?? "1.0", MetadataOrigin.System, 1.0f),
        ["event.aggregateType"]    = new(aggregateType, MetadataOrigin.System, 1.0f),
    },
    CausationId = ExtractCausation(evt),        // dual-source: ext attr, then envelope
    CorrelationId = ExtractCorrelation(evt),    // dual-source
};
```

### Tenant + case routing (MVP decision matrix)

| Config state | CloudEvent `source` resolves in `SourceToTenantMap`? | Tenant active? | `AutoCreateCases`? | Case mapping exists? | Router result | HTTP response |
|---|---|---|---|---|---|---|
| — | No | — | — | — | `null` | 200 + `UnknownSource` log |
| — | Yes | No (Deleting) | — | — | `null` | 200 + `TenantDeleting` log |
| — | Yes | No (Provisioning) | — | — | `null` + `retryable=true` | 500 + DAPR retries |
| — | Yes | Yes | — | Yes | `(tenantId, caseId)` | 200 (schedule workflow) |
| — | Yes | Yes | true | No | `(tenantId, newCaseId)` + auto-create | 200 |
| — | Yes | Yes | false | No | `null` + `AutoCreateDisabled` log | 200 |
| — | Yes | Yes | true | No (cap exceeded) | `null` + `CaseCapExceeded` log | 400 |

### CausationId / CorrelationId extraction

Two sources — extension attributes (preferred, CloudEvents-native) and envelope payload fields (Hexalith.EventStore pattern). Extraction order:

```csharp
static string? ExtractCausation(CloudEvent<JsonElement> evt)
{
    // 1. CloudEvents extension attribute (ecosystem-neutral)
    if (evt.TryGetExtensionAttribute<string>("causationid", out var ext) && !string.IsNullOrWhiteSpace(ext))
        return ext;

    // 2. Envelope field (Hexalith.EventStore shape)
    if (evt.Data is { ValueKind: JsonValueKind.Object } data
        && data.TryGetProperty("causationId", out var field)
        && field.ValueKind == JsonValueKind.String)
        return field.GetString();

    return null;
}
```

**Why this order matters:** extension attributes are first-class CloudEvents metadata (preserved by every CloudEvents-compliant broker); envelope fields are Hexalith-EventStore-specific. A generic publisher (Marten, Wolverine) will use the extension; Hexalith.EventStore uses the envelope. Supporting both = "zero-code for any DAPR source" (PRD §534).

### Publisher trust & spoofing (threat model — Risk #17)

**CloudEvents `source` is an auth-like boundary with no authentication** in the MVP design. Any process with DAPR pub/sub write access on the shared Redis pub/sub component can publish messages with an arbitrary `source` string, which the `TenantEventRouter` then uses to resolve the target `tenantId`. This means:

- **Insider threat / credential compromise.** A compromised service or an insider with pub/sub write access can inject events tagged `source=enterprise.hr` into tenant `hr-tenant` without any cryptographic proof of origin.
- **Cross-tenant contamination.** If two tenants share the same physical pub/sub broker, mis-configuration of `SourceToTenantMap` can route one tenant's events to another tenant's memory scope.
- **No in-app remediation in 9.1.** Story 9.1 explicitly does NOT authenticate publisher identity at the application layer. Adding app-layer auth (e.g., JWT-on-extension-attribute) would contradict the PRD §534 "zero-code for any DAPR source" goal and is deferred to Phase 2.

**MVP mitigations (deploy-time, not code-time):**

1. **DAPR pub/sub component scope restrictions.** Configure the pubsub component with `publishAllowedTopics` / `subscribeAllowedTopics` + per-publisher scopes, so only explicitly-authorized application IDs can publish to the Memories topic. This is the primary control surface.
2. **Network segmentation.** The Memories Server + its DAPR sidecar should not be on the same pub/sub bus as untrusted publishers. Use separate DAPR pub/sub components (or separate Redis instances) per trust zone.
3. **DAPR API token auth (Story 5.4 AC3).** When `DAPR_API_TOKEN_MODE=enabled`, daprd-to-app calls are token-authenticated. Does NOT authenticate the original publisher — but does prevent direct HTTP injection into the subscription endpoint from outside the DAPR plane.
4. **Operational alerting.** Monitor `EventStoreIntegrationLog.UnknownSource` rate (Risk #13); a spike signals either misconfiguration OR an attacker probing source-to-tenant mappings.

**Phase 2 evolution (documented here, NOT implemented in 9.1):** a signed JWT in a CloudEvents extension attribute (`tenantidtoken`), verified at the subscription endpoint against a tenant public key published by `TenantRegistryService`. Requires key-distribution infrastructure + publisher SDK changes — out of scope for zero-code MVP.

**What we document to operators:** in `docs/dev/eventstore-integration.md` under "Publisher trust & spoofing", explicitly state that `source` is NOT an authentication boundary and list the four MVP mitigations above as REQUIRED deploy-time controls, not optional hardening.

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
```

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

```
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

### Completion Notes List

### File List

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
