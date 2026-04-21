# Story 9.2: Dual Embedding & Causal Chain Indexing

Status: ready-for-dev

**Effort estimate:** ~8-9 working days (rebaselined 2026-04-21 twice — Session 1 added 2 days for promoted-from-deferred items; Session 2 added 1-2 days for verification spikes (0.5 day total), backfill migration (0.25 day), options validator extensions (0.25 day), chaos-driven refinements (0.5 day), and the orphan-index reconciler (0.25 day). Do not expect the 7-day baseline — that assumed no spike findings.). Breakdown: 0.5 day `conversation-llm.yaml` component + `AddDaprAiConversation` wiring + `DAPR_CONVERSATION` NoWarn + Options validator (Task 1), 0.75 day `GenerateNaturalLanguageDescriptionActivity` + `ConfidenceSource` enum + cleaner regex iteration (Task 2), 0.25 day second `GenerateEmbeddingActivity` call path with `EmbeddingInput.ContentKind` tag (Task 3), 1 day `IndexNaturalLanguageSemanticActivity` + second semantic index + schema helpers + compensation-boundary fix for orphan `:nl` index on provisioning rollback (Task 4), 0.75 day `IngestionWorkflow` branching + degraded-state propagation + startup gate for replay safety + retry-queue enqueue (Task 5), 0.25 day CorrelationId root-only guard (Task 6), 0.5 day gap-marker `isStub` flag + retroactive promotion telemetry (Task 7), 1.25 days retry-queue-by-reference (store IDs only — avoid Redis OOM per pre-mortem Failure δ) + hosted service + retry workflow + rate-limiter under-sized detection (Task 8), 1 day Tier-2/Tier-3 integration tests incl. 3-scenario degraded test + replay-safety + CorrelationRootEdge + within-tenant PII behavior test (Task 9), 0.25 day docs + sprint-status + retrospective (Task 10). Add 0.5 day cushion for alpha `Dapr.AI` surprises (response shape changes, PII scrubbing config drift, streaming flag unsupported). **DO NOT silently absorb overruns** — if slipping past day 5, flag and rebaseline explicitly.

**HARD prerequisite:** Story 9.1 must be `done`. This story is strictly additive on top of 9.1's `Hexalith.Memories.EventStore` project, `CloudEventToIngestionInputMapper`, `TenantEventRouter`, and the `MetadataOrigin.System` enum value introduced in 9.1 Task 2.10. Attempting to start 9.2 before 9.1 is `done` will block on missing files.

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** An **end-to-end dual-embedding + causal-chain pipeline** for `SourceType.Event` memory units that (a) generates TWO embeddings per event — one from the raw JSON payload (technical search) and one from an LLM-authored natural-language description via `DaprConversationClient` (business-meaning search) — and stores them in two distinct Redis Vector hash keys (`{tenant}:vec:{memoryUnitId}` for the raw embedding keeps its existing shape; `{tenant}:vec:nl:{memoryUnitId}` is NEW) against a second per-tenant vector index `{tenant}:memories:vec:nl`; (b) corrects the `IndexGraphActivity` CorrelationId semantics so a `correlated_with` edge is created ONLY to the correlation root (the event whose `MemoryUnitId == CorrelationId`), NOT fan-out-to-all-correlated-events (see Risk #3 — this is a **behavioral fix** to an implementation regression that Story 9.1 inherited from Story 1.5, not a new feature); (c) adds explicit **gap-marker semantics** — `BuildMergeStubNode` sets `m.isStub = true` on stub creation and `BuildMergeMemoryUnitNode` clears it (`m.isStub = false`) when the real event lands, so `GraphTraversalService` gap-marker detection can be upgraded from "content absent heuristic" to "explicit flag", and retroactive `caused_by` edge completion emits a `StubResolvedTelemetryEvent` for operators; (d) a **degraded path** — if the DAPR Conversation API is unavailable or returns an error, the raw-payload embedding is still indexed (memory unit is searchable, `MemoryUnitStatus = Indexed`), but the NL embedding is queued via `FailedNaturalLanguageEmbeddingRegistry` (Redis Sorted Set `nl-embedding-retry:{tenantId}`) for retry by a background service — the event is NEVER lost, search just starts with syntactic + raw-semantic + graph, and business-meaning search fills in asynchronously; (e) `IngestionResult.NaturalLanguageEmbeddingStatus` field (`Indexed` | `Queued` | `NotApplicable`) so callers + telemetry can observe the degraded-vs-healthy split. Closes **FR60** ("dual embeddings — raw payload + natural language description"), **FR61** ("auto-index CausationId/CorrelationId as graph edges without developer mapping code — CausationId already works from 9.1 via `IndexGraphActivity`, CorrelationId semantics corrected here"), completes **the acceptance criterion "gap markers and retroactive resolution"** that 9.1 explicitly deferred (see 9.1 "What does NOT ship" bullet 2).

**What already exists (do NOT rebuild):**

1. **`IngestionWorkflow` — `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:151-189`.** The workflow already runs validate → extract → embed → fan-out index → verify → dedup. **Reuse verbatim.** Story 9.2 adds ONE `SourceType`-gated branch: if `input.SourceType == SourceType.Event`, run `GenerateNaturalLanguageDescriptionActivity` → `GenerateEmbeddingActivity` (second call, NL text) → `IndexNaturalLanguageSemanticActivity` in parallel with the existing fan-out. Do NOT fork an `EventIngestionWorkflow`.
2. **`GenerateEmbeddingActivity` — `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs`.** Takes `EmbeddingInput(TenantId, ContentText)` → `EmbeddingResult(Vector, Provider, Dimensions, Model)`. **Reuse verbatim** for both embeddings. The second call passes `ContentText = nlDescription`. The per-tenant `EmbeddingRateLimiterActor` automatically throttles BOTH calls (critical: event ingestion doubles embedding-API call volume — see Risk #6).
3. **`EmbeddingInput` — `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs:11`.** Already a positional record `(TenantId, ContentText)`. **Add one non-required field** `ContentKind` (default `EmbeddingContentKind.Payload`, new value `NaturalLanguageDescription`) so telemetry + retry-tracking can distinguish the two calls WITHOUT creating a second activity class. Additive — no workflow replay break.
4. **`IndexSemanticActivity` — `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`.** Writes to `{tenant}:vec:{memoryUnitId}` against index `{tenant}:memories:vec`. **Reuse verbatim** for the raw-payload embedding. Story 9.2 ADDS a new sibling activity (NOT an edit) `IndexNaturalLanguageSemanticActivity` that writes to `{tenant}:vec:nl:{memoryUnitId}` against a NEW index `{tenant}:memories:vec:nl`. Keeping the two activities separate preserves per-activity telemetry, retry, and cleanup granularity.
5. **`IndexSchemaDefinitions` — `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:36-67`.** Exposes `SemanticIndexSuffix` (`:memories:vec`) + `SemanticKeyPrefixSuffix` (`:vec:`) + `GetSemanticIndexName` + `CreateSemanticParams` + `CreateSemanticSchema`. **Reuse the schema-factory pattern verbatim.** Add four sibling symbols for the NL index: `NaturalLanguageSemanticIndexSuffix = ":memories:vec:nl"`, `NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:"`, `GetNaturalLanguageSemanticIndexName(tenantId)`, `CreateNaturalLanguageSemanticParams(tenantId)`, `CreateNaturalLanguageSemanticSchema(dimensions)`, `GetNaturalLanguageSemanticKeyPrefix(tenantId)`. Vector field name stays `"embedding"` — same schema shape (HNSW/FLOAT32/COSINE) — differs only in key prefix + index name so Story 2.2's `SemanticSearchService` logic can be cloned to `NaturalLanguageSemanticSearchService` with a single constant change.
6. **`IndexGraphActivity` — `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:74-101`.** Already creates `caused_by` + `correlated_with` edges. **Modify ONLY the `correlated_with` branch (lines 89-101)** to add a pre-check: when `input.CorrelationId == input.MemoryUnitId`, this event IS the correlation root — skip edge creation (self-loops make no sense). When `input.CorrelationId != input.MemoryUnitId`, the existing `MATCH (root), (current) MERGE (current)-[r:CORRELATED_WITH]->(root)` pattern is CORRECT — it creates an edge from THIS event to the root. **DO NOT create fan-out edges between this event and every other correlated event** — the MERGE is already edge-from-current-to-root-only. The AC concern ("every event in a correlation group does NOT create edges to every other event") is satisfied by the existing implementation modulo the self-loop case. Guard test: `IndexGraphActivityTests.CorrelationIdEqualsMemoryUnitId_DoesNotCreateSelfEdge` (Risk #3).
7. **`BuildMergeStubNode` + `BuildMergeMemoryUnitNode` — `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:210-222` + `:53-98`.** Stub creates `(m:MemoryUnit {id: $id})` with NO other properties. Real-event merge sets all content fields. The MERGE pattern means stub → real-event transition is already idempotent. **Story 9.2 EDITS both query builders** to add an `isStub` boolean property: `BuildMergeStubNode` sets `m.isStub = coalesce(m.isStub, true)` (coalesce so an already-resolved node is not re-flagged as stub); `BuildMergeMemoryUnitNode` sets `m.isStub = false`. `GraphTraversalService.cs:98-106` already has a "content absent heuristic" for gap markers — keep it as a fallback but PREFER the explicit `isStub = true` check (see Task 7 for the traversal-logic update).
8. **`GraphTraversalService` — `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:98`.** The comment literally says "Stub node — gap marker (FR49). Stub nodes created by BuildMergeStubNode have ONLY the id property; content is absent/null in FalkorDB." — a heuristic brittle to any future property addition (e.g., today's `isStub` flag addition breaks it). **Upgrade to explicit flag check:** `record.TryGetValue("isStub", out var stubFlag) && (bool)stubFlag == true`, with the "content absent" path retained as a fallback for legacy nodes written before this story.
9. **`MetadataOrigin.System`/`MetadataOrigin.Ai` — introduced by Story 9.1 Task 2.10.** System = deterministic parse results (CloudEvents envelope fields); Ai = LLM-derived. The NL description string that the DAPR Conversation API returns is tagged `MetadataOrigin.Ai` with confidence = the LLM response's confidence-like proxy (see "LLM confidence extraction" in Dev Notes); the NL-derived embedding vector itself lives in Redis Vector (not metadata), so origin-tagging applies only to the description text when it's persisted to the `event.naturalLanguageDescription` metadata field. Do NOT introduce a fourth enum value.
10. **`EmbeddingResult.Model` — `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingResult.cs:35`.** Nullable. Story 5.5 threads it into `IndexInput.EmbeddingModel`. For the NL embedding, reuse the same `EmbeddingResult` shape — the NL-vector model is the SAME as the raw-vector model (same provider, same dimensions, same tenant config). The NL embedding writes `EmbeddingProvider` + `EmbeddingModel` + `EmbeddingDimensions` alongside `"embedding"` vector bytes in the NL hash so per-unit inspection can still read the model from either hash.
11. **`RetryPolicyBuilder` — `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs:20-78`.** Per-activity retry options snapshot. Register two new keys: `nameof(GenerateNaturalLanguageDescriptionActivity)` (LLM-aware retry policy — larger backoff, bounded attempts because LLM outage is typically ≥30s; see Dev Notes "LLM retry policy") and `nameof(IndexNaturalLanguageSemanticActivity)` (inherits default). Story 9.1's existing retry keys unchanged.
12. **`TenantConfigurationActor` + `TenantEmbeddingConfig` — `src/Hexalith.Memories.Server/Actors/` (Story 5.5).** Already provides per-tenant embedding provider/model/rate-limit. **DO NOT introduce a per-tenant LLM-provider configuration in 9.2.** The DAPR Conversation component is SYSTEM-WIDE for MVP (`conversation.openai` default; operators swap via YAML). Per-tenant LLM config is a Phase 2 follow-up (see Dev Notes "Per-tenant LLM provider").
13. **`MemoriesJsonContext` — `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`.** AOT-safe source-generated `JsonSerializerOptions`. Register the four new types (`EmbeddingContentKind`, `NaturalLanguageEmbeddingStatus`, `FailedNaturalLanguageEmbeddingRecord`, `NaturalLanguageDescriptionResult`) via `[JsonSerializable(typeof(T))]` so DAPR workflow replay serialization stays AOT-safe.
14. **`EventStoreIntegrationLog` — created in Story 9.1, event IDs 9100-9199.** Add Story 9.2 entries (9150-9199 bank): `9150 NaturalLanguageDescriptionGenerated`, `9151 NaturalLanguageDescriptionSkippedLlmUnavailable`, `9152 NaturalLanguageEmbeddingQueuedForRetry`, `9153 NaturalLanguageEmbeddingRetrySucceeded`, `9154 StubNodeResolved (tenantId, memoryUnitId, causingEventId, stubCreatedAt, resolvedAt)`, `9155 CorrelationIdSelfEdgeSkipped` (Risk #3 guard), `9160 ConversationApiOutage (transient)`, `9161 EchoComponentNotAllowedInProduction` (prod fail-fast), `9162 ConversationApiIsEchoComponent` (dev warning — disambiguated from 9160), `9163 RateLimiterUnderSizedForEvents` (first `SourceType.Event` ingest on an under-sized tenant), `9170 NaturalLanguageEmbeddingRetryQueueBacklog` (when backlog > 100 per tenant), `9171 InFlightWorkflowsMismatchAtStartup` (startup gate per Winston — promoted from deferred), `9180 NaturalLanguageEmbeddingPermanentlyFailed` (dead-letter).

**What 9.2 adds:**

1. **`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs`** — NEW. `WorkflowActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>`. Constructor-injects `DaprConversationClient _conversationClient`, `ILogger<GenerateNaturalLanguageDescriptionActivity> _logger`, `IOptions<NaturalLanguageDescriptionOptions> _options`.
   - **Input record** `NaturalLanguageDescriptionInput(string TenantId, string MemoryUnitId, string RawJsonPayload, string EventType, string? AggregateType)` — `EventType`/`AggregateType` extracted from `IngestionInput.Metadata["cloudevent.type"]` / `["event.aggregateType"]` (both tagged `MetadataOrigin.System` by 9.1).
   - **Output record** `NaturalLanguageDescriptionResult(string Description, float EstimatedConfidence, string LlmProvider, string LlmModel)` — `EstimatedConfidence` is a proxy from the LLM response (see "LLM confidence extraction" in Dev Notes); for OpenAI compliant backends, derive from `logprobs` if available, else default `0.85` constant (documented in Dev Notes).
   - **Prompt template** (system message): `"You are an event summarizer. Given a JSON event payload of type {EventType}, write a single natural-language sentence (≤40 words) describing what business action occurred. Do NOT repeat field names. Focus on domain meaning. Return ONLY the sentence, no preamble or JSON."` — user message: `"Event type: {EventType}\nAggregate: {AggregateType ?? \"(unspecified)\"}\nPayload:\n{RawJsonPayload}"`. Payload truncated to `NaturalLanguageDescriptionOptions.MaxPayloadChars` (default `8000`) to stay under LLM context windows.
   - **`ConversationOptions` config:** `Temperature = 0.1` (deterministic summaries), `ResponseFormat = null` (plain text, NOT JSON — see Risk #7), `MaxTokens = 80` (≤40 words × ~2 tokens/word), no tools, no streaming.
   - **Idempotency:** LLM call is NOT idempotent per se, but the workflow replay mechanism does not re-call the activity if the previous result is already persisted. The NL description is computed once per `memoryUnitId` — retries are driven by outer workflow retry policy.
   - **Degraded path:** on `ConversationException`/`DaprException`/timeout, throw a typed `NaturalLanguageDescriptionUnavailableException` (NOT a transient `Exception` — this prevents workflow-level retry from burning LLM budget on a chronic outage). Workflow catches and marks `NaturalLanguageEmbeddingStatus.Queued` — see Task 5.
2. **`src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs`** — NEW. Structural clone of `IndexSemanticActivity.cs` with TWO line-level differences: (a) uses `IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId)` for `indexName`; (b) uses `IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId) + input.MemoryUnitId` for `hashKey`. Same vector serialization, same HNSW/FLOAT32/COSINE shape, same `EnsureVectorDimensionsMatchAsync` check. Writes additional hash fields `naturalLanguageDescription` (TEXT, for developer inspection) + `descriptionOrigin` (`MetadataOrigin.Ai`) + `descriptionConfidence` (float) so operators can GET the Redis hash and inspect both description and vector.
3. **`src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs`** — EDIT. Currently creates ONE semantic index. Add a second `FT.CREATE` call for `{tenantId}:memories:vec:nl` using `CreateNaturalLanguageSemanticParams` + `CreateNaturalLanguageSemanticSchema`. Idempotent — wrap in the same `RedisServerException` "Index already exists" guard as the existing call (lines 56-67 of `IndexSemanticActivity.cs` pattern).
4. **`src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorIndexActivity.cs`** + **`DeleteRedisVectorActivity.cs`** — EDIT. Extend to DROP both indexes + delete both key prefixes (tenant deletion must clean up `:vec:` AND `:vec:nl:` keys). Compensation in `TenantProvisioningWorkflow` must also drop both indexes on rollback.
5. **`src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`** — EDIT. Currently deletes the raw-embedding hash. Extend to delete BOTH `{tenant}:vec:{memoryUnitId}` AND `{tenant}:vec:nl:{memoryUnitId}` when compensating. Keep the activity single-purpose (don't fork a second cleanup activity) — cleanup is transactionally coupled in practice.
6. **`src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs`** — EDIT. Between the existing `GenerateEmbeddingActivity` call (line 151) and the fan-out index block (line 191), insert a **`SourceType.Event`-gated dual-embedding block**. Pseudocode:
   ```
   NaturalLanguageEmbeddingStatus nlStatus;
   EmbeddingResult? nlEmbedding = null;
   string? nlDescription = null;
   float nlConfidence = 0f;
   if (input.SourceType == SourceType.Event)
   {
       try
       {
           NaturalLanguageDescriptionResult nlResult =
               await context.CallActivityAsync<NaturalLanguageDescriptionResult>(
                   nameof(GenerateNaturalLanguageDescriptionActivity),
                   new NaturalLanguageDescriptionInput(input.TenantId, memoryUnitId,
                       Encoding.UTF8.GetString(input.ContentBytes ?? []),
                       input.Metadata.TryGetValue("cloudevent.type", out var et) ? et.Value : "(unknown)",
                       input.Metadata.TryGetValue("event.aggregateType", out var at) ? at.Value : null),
                   For(nameof(GenerateNaturalLanguageDescriptionActivity)));
           nlDescription = nlResult.Description;
           nlConfidence = nlResult.EstimatedConfidence;
           nlEmbedding = await context.CallActivityAsync<EmbeddingResult>(
               nameof(GenerateEmbeddingActivity),
               new EmbeddingInput(input.TenantId, nlResult.Description) { ContentKind = EmbeddingContentKind.NaturalLanguageDescription },
               For(nameof(GenerateEmbeddingActivity)));
           nlStatus = NaturalLanguageEmbeddingStatus.Indexed;
       }
       catch (NaturalLanguageDescriptionUnavailableException ex)
       {
           logger.LogInformation(ex, "NL description unavailable for {MemoryUnitId}; queueing for retry", memoryUnitId);
           nlStatus = NaturalLanguageEmbeddingStatus.Queued;
       }
   }
   else
   {
       nlStatus = NaturalLanguageEmbeddingStatus.NotApplicable;
   }
   ```
   If `nlStatus == Indexed`, the fan-out `Task.WhenAll(syntacticTask, semanticTask, graphTask)` gains a fourth task `indexNaturalLanguageSemanticTask` constructed from a `NaturalLanguageIndexInput` sibling record. If the 4th task fails, compensation extends to drop the NL hash too. If `nlStatus == Queued`, an activity `QueueNaturalLanguageEmbeddingRetryActivity` writes the memory unit id + raw payload bytes to `nl-embedding-retry:{tenantId}` with a score of `context.CurrentUtcDateTime.Ticks` — retry-service picks up via `ZRANGE`.
7. **`IngestionResult.NaturalLanguageEmbeddingStatus`** — NEW public property on the existing `IngestionResult` record in `Hexalith.Memories.Contracts/V1/IngestionResult.cs`. Enum `NaturalLanguageEmbeddingStatus { Indexed, Queued, NotApplicable }`. Additive — default `NotApplicable` for workflow replay of pre-9.2 events.
8. **`src/Hexalith.Memories.Server/Ingestion/FailedNaturalLanguageEmbeddingRegistry.cs`** — NEW. Minimal service with `EnqueueAsync(string tenantId, string memoryUnitId, string rawPayloadJson, long queuedAtTicks, CancellationToken ct)`, `DequeueBatchAsync(string tenantId, int batchSize, CancellationToken ct)`, `GetBacklogCountAsync(string tenantId, CancellationToken ct)`. Backed by Redis Sorted Set `nl-embedding-retry:{tenantId}`. **Parallel to `FailedUnitsRegistry` from Story 6.3** — follow the same shape, same logging style, same DI registration pattern.
9. **`src/Hexalith.Memories.Server/Ingestion/NaturalLanguageEmbeddingRetryHostedService.cs`** — NEW `IHostedService`. Background loop: every `NaturalLanguageDescriptionOptions.RetryIntervalSeconds` (default `60`), for each tenant with backlog, dequeue ≤`BatchSize` (default `5`) records and schedule a `NaturalLanguageEmbeddingRetryWorkflow` (NEW, very small) that re-runs `GenerateNaturalLanguageDescriptionActivity` → `GenerateEmbeddingActivity` → `IndexNaturalLanguageSemanticActivity`. On success: log 9153 + `ZREM`. On failure: leave in the set + increment a per-record attempt count; when `Attempts ≥ MaxRetryAttempts` (default `5`), emit `9153`-variant error and move to `nl-embedding-retry-dead:{tenantId}` for operator triage. This means the worst case is "memory unit is searchable by syntactic + raw-semantic + graph but NOT by business-meaning until retry succeeds" — search degradation, not data loss.
10. **`src/Hexalith.Memories.Server/Workflows/NaturalLanguageEmbeddingRetryWorkflow.cs`** — NEW. `Workflow<NaturalLanguageEmbeddingRetryInput, NaturalLanguageEmbeddingRetryResult>`. Minimal orchestration:
    ```
    var nlResult = await context.CallActivityAsync<NaturalLanguageDescriptionResult>(
        nameof(GenerateNaturalLanguageDescriptionActivity), input.ToDescriptionInput(), For(nameof(GenerateNaturalLanguageDescriptionActivity)));
    var nlEmbedding = await context.CallActivityAsync<EmbeddingResult>(
        nameof(GenerateEmbeddingActivity), new EmbeddingInput(input.TenantId, nlResult.Description) { ContentKind = EmbeddingContentKind.NaturalLanguageDescription });
    await context.CallActivityAsync<IndexResult>(nameof(IndexNaturalLanguageSemanticActivity), input.ToNaturalLanguageIndexInput(nlEmbedding, nlResult));
    return new NaturalLanguageEmbeddingRetryResult(Indexed: true);
    ```
    If the LLM is STILL unavailable, the workflow throws `NaturalLanguageDescriptionUnavailableException` — the hosted service catches, increments attempt count, and re-enqueues with a delay.
11. **`IndexGraphActivity` CorrelationId self-edge guard — `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:89`.** Wrap the existing `correlated_with` edge creation in `if (!string.Equals(input.CorrelationId, input.MemoryUnitId, StringComparison.Ordinal))` — covers the "this event IS the correlation root" case. Emit `9155 CorrelationIdSelfEdgeSkipped` at `Debug` level (not `Warning` — this is expected and high-frequency). Add a `BuildMergeStubNode` call for the root BEFORE the edge MERGE so the root stub exists (already in place).
12. **`BuildMergeStubNode` + `BuildMergeMemoryUnitNode` `isStub` flag — `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:210-222` + `:77`.** Stub: `MERGE (m:MemoryUnit {id: $id}) SET m.isStub = coalesce(m.isStub, true)`. Merge: append `m.isStub = false` at the end of the existing `SET` clause. Test `GraphQueryBuilderTests.StubThenReal_FlagPromotesFromTrueToFalse`.
13. **`GraphTraversalService.cs:98` gap-marker detection upgrade.** Preferred path: check `isStub` boolean property on the node record. Fallback path: existing "content absent" heuristic for pre-9.2 nodes. Test `GraphTraversalServiceTests.ExplicitIsStubFlag_IdentifiesGapMarker` + `.ContentAbsentHeuristicFallback_IdentifiesGapMarker`. Emit `9154 StubNodeResolved` from `BuildMergeMemoryUnitNode` PATH when the activity's query returns `PREVIOUS_isStub = true` (requires a `RETURN` clause extension on the MERGE query — see Dev Notes "Stub resolution telemetry").
14. **`deploy/dapr/components/conversation-llm.yaml`** — NEW DAPR Conversation component (per architecture pattern at L895-914). MVP uses `conversation.openai` with a dev-only placeholder key + `conversation.echo` for Aspire local runs (Aspire handles secrets; tests use `conversation.echo` which returns the input unchanged — see "Test doubles for DAPR Conversation" in Dev Notes).
15. **`src/Hexalith.Memories.AppHost/Program.cs`** — EDIT. Register the new DAPR component: `builder.AddDaprComponent("llm", "conversation.echo")` for local dev; production composition wires the OpenAI/Anthropic provider from secrets. Add `.WithReference(llm)` on the server sidecar.
16. **`src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj`** — EDIT. Add `<PackageReference Include="Dapr.AI" />` (version-locked in `Directory.Packages.props:36`). Add `<NoWarn>$(NoWarn);DAPR_CONVERSATION</NoWarn>` to suppress alpha-API warnings (per architecture D26).
17. **`src/Hexalith.Memories.Server/DependencyInjection/MemoriesServerServiceCollectionExtensions.cs`** (or wherever `AddMemoriesServer` lives — verify at impl time) — EDIT. Add `services.AddDaprAiConversation()` to register `DaprConversationClient` + `IOptions<NaturalLanguageDescriptionOptions>` binding from `config.GetSection("NaturalLanguage")`.
18. **`appsettings.Development.json`** — EDIT. Add `NaturalLanguage` section: `{ "DaprComponentName": "llm", "MaxPayloadChars": 8000, "RetryIntervalSeconds": 60, "BatchSize": 5, "MaxRetryAttempts": 5, "LlmRequestTimeoutSeconds": 15 }`. In `appsettings.Production.json` (created by 9.1 Task 5.6.2), override `DaprComponentName` to `"llm-openai"` (operator-configured provider).
19. **Telemetry additions — `src/Hexalith.Memories.Telemetry/`.** Extend existing `MemoriesActivitySource` with named activity `NaturalLanguageDescriptionGeneration` and add metric `memories_natural_language_embedding_queue_depth{tenant=...}` (counter) + `memories_natural_language_description_duration_ms` (histogram). Follow the existing pattern from Story 7.5.
20. **Docs — `docs/dev/eventstore-integration.md`.** UPDATE (not create — 9.1 created the file). Add sections: **"Dual embedding pipeline"** (why NL description, prompt template, LLM provider swap procedure); **"Natural language embedding retry queue"** (how to inspect backlog via `memories tenant status --tenant X`, how dead-lettered records appear in operator CLI); **"Gap markers and retroactive resolution"** (explain the `isStub` flag, the stub-promotion telemetry event, how causal traversal returns completeness-aware results); **"Correlation root semantics"** (explicit note: `correlated_with` edges point THIS event at the correlation root, never to sibling correlated events — with before/after example). Update the worked example from 9.1 to show dual-embedding output.

**What does NOT ship:**

- **Per-tenant LLM provider configuration.** MVP ships ONE system-wide DAPR Conversation component. Per-tenant provider swap is Phase 2 (would require `TenantConfigurationActor` to carry an LLM-provider-name field, plus a factory `ConversationClientFactory` that resolves component-name from the actor — architecture already references this at L1254 as future work).
- **Streaming LLM responses.** Architecture L1097: "DAPR Conversation API: No streaming (alpha limitation)." Story 9.2 uses the non-streaming `ConverseAsync` call. If LLM latency is problematic, move NL description to a post-ingest async enrichment path (Phase 2) rather than waiting for streaming.
- **`caused_by` / `correlated_with` confidence-promotion UX.** Story 4.3 owns confidence promotion — user-verified edges get their confidence bumped. 9.2 exposes the `isStub` flag + stub-resolution telemetry but does NOT ship a UI/CLI for operators to promote edges.
- **Tool-calling or agentic NL generation.** The DAPR Conversation call is a single-shot summarization with no tools (`ToolChoice.None`). Agentic enrichment (classify, extract entities, infer causal) is `AiEnrichmentWorkflow` — architecture L1219 names it; Phase 1.5 or Phase 2, NOT 9.2.
- **Cross-aggregate correlation analysis.** If events A (aggregate X) and B (aggregate Y) share a `CorrelationId`, 9.2 creates `correlated_with` edges from A and B to the root — but does NOT ship a "workflow correlation view" aggregating across aggregates. Phase 2.
- **PII scrubbing configuration.** DAPR Conversation supports PII scrubbing via component metadata. 9.2 ships with `piiScrubbing: false` (MVP, documented in the component YAML + integration doc) because event payloads from a tenant's own event stream are already within that tenant's trust boundary. Operators who need scrubbing can flip the component metadata flag — no code change required.
- **Response caching enabled by default.** DAPR Conversation supports `responseCacheTTL`. 9.2 ships with `responseCacheTTL: 0s` (caching DISABLED) because the cache is shared ACROSS tenants at the sidecar level — two tenants ingesting identical JSON would share the cached summary, which is unacceptable as a default privacy posture even though event payloads usually contain a tenant-unique aggregate ID. Operators with cost concerns and tenants who explicitly accept cross-tenant summary sharing can flip the TTL via the component YAML — see Risk #16 + integration doc "Response caching opt-in procedure".
- **NL embedding search as the default axis.** Story 2.2's semantic search queries `{tenant}:memories:vec` (the raw embedding). 9.2 does NOT rewire `HybridSearchService` to merge both axes. Adding a `queryAxis=naturalLanguage` parameter or an auto-blended NL+raw axis is a search-side story — NOT this one. This story ensures the data exists and is inspectable.
- **LLM cost budgeting per tenant.** Rate limiting of Conversation API calls is per the DAPR sidecar's component config (if provider supports it) and per the `EmbeddingRateLimiterActor` indirectly (by throttling the embedding that follows). A dedicated `LlmRateLimiterActor` is Phase 2.
- **Retroactive backfill for pre-9.2 events.** Events ingested before 9.2 shipped have ONLY the raw embedding. 9.2 does NOT ship a backfill workflow that walks existing memory units + generates NL descriptions. Operators who want backfill can use the existing `ReIngestionCoordinator` + mark source events for reingestion — that flow naturally generates the NL embedding on the re-run.

**Primary risks:**

1. **DAPR Conversation API alpha status + `DAPR_CONVERSATION` warning pollution.** `Dapr.AI` 1.17.6 is alpha (`[Experimental]`). Compiler emits `DAPR_CONVERSATION` warnings at every call site. Under `TreatWarningsAsErrors=true`, the build breaks. **Mitigation:** (a) per architecture D26, add `<NoWarn>$(NoWarn);DAPR_CONVERSATION</NoWarn>` to **ONLY** `Hexalith.Memories.Server.csproj` — NOT `Directory.Build.props`, so unintentional spread is prevented; (b) annotate `GenerateNaturalLanguageDescriptionActivity` with an XML comment calling out the alpha dependency; (c) guard test `ProjectCompilationTests.Server_SuppressesDaprConversationWarningOnly` (verify `Directory.Build.props` does NOT carry the suppression).

2. **LLM latency inflates NFR6 budget.** Story 9.1's NFR6 asserts <5s indexing freshness p50/p95 (Tier 3 observation). LLM response time adds 1-3s (p95) to the pipeline. Under rate limiting or cold start, p95 may blow past 5s. **Mitigation:** (a) set `LlmRequestTimeoutSeconds = 15` in options + use `cancellationToken.WithTimeout()` on `ConverseAsync`; (b) when LLM timeout fires, throw `NaturalLanguageDescriptionUnavailableException` → event proceeds with raw-only embedding → `NaturalLanguageEmbeddingStatus.Queued` → search is not delayed; (c) measurement test `DualEmbeddingLatencyTests.EventIngestion_P95Under7s_WithLlm_UnderNormalConditions` MUST publish ≥50 events in a single test run (statistical sample size — single-event measurements are smoke tests, not benchmarks; P95 of fewer than 30 samples is meaningless), assert P95 latency < 7s AND P50 < 4s, AND emit a histogram summary into the test output for trend tracking — relax the NFR6 <5s target to <7s WHEN LLM is in the path, document in updated integration doc "Known performance envelopes"; (d) the raw-embedding path is unaffected — NFR6 <5s still holds for `SourceType != Event`.

3. **CorrelationId fan-out vs root-only confusion.** The epic AC says: "every event in a correlation group does NOT create edges to every other event — only to the correlation root (the event whose ID equals the CorrelationId)." The existing `IndexGraphActivity` (line 94-100) uses `BuildMergeEdge(input.CorrelationId, input.MemoryUnitId, CorrelatedWith, 0.8, Explicit)` which creates edge FROM the `CorrelationId` node (= the root) TO the current memory unit — this is already root-to-current, NOT fan-out. But the semantic direction is inverted from what operators may expect (most teams think "this event correlates with root", reading edges outbound from current). **Mitigation:** (a) preserve the existing direction (root → current event) because the `BuildMergeStubNode` for the root must precede the edge MERGE — creating a stub root and pointing current → root would force two stub creations; the existing direction is efficient; (b) DOCUMENT the direction in `docs/dev/eventstore-integration.md` under "Correlation root semantics" with a before/after diagram; (c) guard tests: `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `IndexGraphActivityTests.CorrelationIdEqualsMemoryUnitId_NoSelfEdge` + `IndexGraphActivityTests.MultipleEventsSameCorrelationId_NoFanOutBetweenEvents`; (d) traversal-direction test in `GraphTraversalServiceTests` — query `direction=in` + `edgeType=CorrelatedWith` on a root returns all correlated events; `direction=out` returns the root.

4. **Gap marker heuristic regression.** Current `GraphTraversalService.cs:98` detects gap markers via "content property is absent" — a heuristic that breaks the moment any other property is added to stub nodes (e.g., today's `isStub` addition). **Mitigation:** (a) upgrade traversal to CHECK `isStub` flag first, fall back to "content absent" for pre-9.2 nodes; (b) `isStub = false` is the canonical "resolved" state; (c) guard tests: `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes`; (d) documentation note: after full repopulation, operators can drop the content-absent fallback — tracked in `deferred-work.md`.

5. **Second vector index schema drift from the first.** The raw-embedding index uses HNSW/FLOAT32/COSINE/dimensions=`config.Dimensions`. The NL-embedding index MUST match dimensions (same embedding provider) but the schema constants live in `IndexSchemaDefinitions`. A future edit to one schema that forgets to update the other produces an async NL index with stale schema. **Mitigation:** (a) factor a private helper `CreateSemanticSchemaCore(string vectorFieldName, int dimensions)` that both `CreateSemanticSchema(dimensions)` and `CreateNaturalLanguageSemanticSchema(dimensions)` delegate to; (b) guard test `IndexSchemaDefinitionsTests.BothSemanticSchemas_HaveIdenticalVectorFieldShape` (reflection-based — asserts TYPE + DISTANCE_METRIC + ALGO match); (c) provisioning activity test asserts both indexes are created with the same dimensions from `TenantEmbeddingConfig.Dimensions`.

6. **Event ingestion doubles embedding API call volume.** Each event now calls the embedding API TWICE (raw payload + NL description). Under load, this doubles the per-tenant rate-limit consumption and may exhaust the embedding provider budget faster. Story 1.7's `EmbeddingRateLimiterActor` throttles based on per-tenant ceiling — doubled calls HALVE the effective event throughput. **Mitigation:** (a) BOTH embedding calls go through the SAME rate limiter (by design — same tenant, same actor), so total throughput is preserved correctly; (b) operators must size the `RateLimitPerMinute` ceiling with the 2x factor in mind — documented in the integration doc; (c) telemetry: emit `memories_embedding_api_call_total{contentKind="payload|naturalLanguageDescription",tenant=...}` (counter) so operators can see the 2:1 split; (d) guard test `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag`.

7. **LLM returning JSON or markdown when plain text was requested.** `ResponseFormat = null` hints plain text, but some providers (Anthropic especially) wrap responses in markdown code fences or add preambles. An NL description like ```"Here is the summary: \"User claim submitted for $9,500\""``` creates a noisy embedding that doesn't match the intended meaning. **Mitigation:** (a) post-process the LLM response: strip leading/trailing whitespace, unwrap Markdown code fences (\`\`\`\s* prefix/suffix), strip "Here is the summary:" / "Summary:" preamble patterns via a small regex allow-list; (b) if post-processing leaves the string empty, throw `NaturalLanguageDescriptionUnavailableException` → queue for retry; (c) guard tests `NaturalLanguageResponseCleanerTests.StripsMarkdownCodeFences` + `.StripsCommonPreambles` + `.EmptyAfterCleanupThrows`; (d) document the cleaner contract in Dev Notes under "LLM response normalization".

8. **LLM returns hallucinated facts / misrepresents event meaning.** The NL description is indexed for semantic search — if the LLM hallucinates (e.g., "customer canceled the policy" when the event was actually a policy renewal), hybrid search can surface incorrect matches on business-meaning queries. **Mitigation:** (a) the prompt explicitly says "Return ONLY the sentence, no preamble. Do NOT repeat field names. Focus on domain meaning." — low temperature (`0.1`) reduces drift; (b) the description is TAGGED `MetadataOrigin.Ai` with `Confidence = EstimatedConfidence`, so the UI can show "AI-inferred" affordance; (c) users can CORRECT the description via Story 3.6 annotations-and-corrections flow (already shipped); (d) guard test `NaturalLanguageDescriptionPromptTests.PromptContainsHallucinationWarning` (structural — verifies the prompt string contains the "Focus on domain meaning" constraint); (e) document this as a KNOWN LIMITATION in the integration doc — NL descriptions are best-effort summaries, not domain truth.

9. **Retry queue unbounded growth under extended LLM outage.** If the LLM is down for hours, `nl-embedding-retry:{tenantId}` grows unbounded. Redis memory footprint balloons; operator alerting silence is a problem. **Mitigation:** (a) the hosted service emits `9170 NaturalLanguageEmbeddingRetryQueueBacklog` at `Warning` level when backlog > `100` per tenant, at `Error` when > `1000`, and backs off to `RetryIntervalSeconds * 5` when all recent retries fail (exponential outage backoff); (b) Prometheus metric `memories_natural_language_embedding_queue_depth{tenant=...}` (gauge); (c) recommended alert: backlog growth rate > 10 records/min for 15 min → page; (d) dead-letter queue `nl-embedding-retry-dead:{tenantId}` holds permanently-failed records; operators can trigger re-enqueue via CLI (`memories retry-nl-embeddings --tenant X --dead`) — NO user-data loss; (e) guard test `NaturalLanguageEmbeddingRetryHostedServiceTests.BacklogExceedsWarningThreshold_EmitsLog` + `.BacklogExceedsErrorThreshold_BacksOffInterval`.

10. **`conversation.echo` component in local dev produces misleading embeddings.** The `conversation.echo` DAPR component returns the input unchanged — meaning the "NL description" = the raw JSON payload bytes. Dev embeddings for the NL vector will be identical to the raw embedding. Local search tests may appear to work but produce false positives. **Mitigation:** (a) dev logs emit `9162 ConversationApiIsEchoComponent` at `Warning` whenever the resolved component name is `conversation.echo` (disambiguated from `9160` outage event), AND the unit tests use NSubstitute mocks (NOT the echo component) so they exercise realistic behavior; (b) the Tier-2 integration tests that run against DAPR slim use `conversation.echo` and ASSERT the degenerate-case behavior (NL embedding = raw embedding) is consistent — THIS is the point of the dev component; (c) guard test `ConversationComponentResolutionTests.EchoComponent_EmitsWarningInDev_NeverAllowedInProduction` (loads `appsettings.Production.json` and asserts component name ≠ `"conversation.echo"`); (d) document in integration doc: "Local dev uses the echo component. DO NOT deploy to production without swapping the component."

11. **`Dapr.AI` 1.17.6 API surface change between alpha versions.** Alpha APIs can change between 1.17.6 → 1.17.7 → 1.18.0. A transitive upgrade of `Dapr.Client` could pull in a breaking `DaprConversationClient` change. **Mitigation:** (a) pin `Dapr.AI` version in `Directory.Packages.props` (already done); (b) use the `IChatClient` bridge via `Dapr.AI.Microsoft.Extensions` AS a fallback abstraction — the `IChatClient` surface is stabilized in Microsoft.Extensions.AI, protecting against DAPR-side churn. For MVP, use `DaprConversationClient` directly (simpler); guard test `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` verifies `ConverseAsync(messages, options)` signature (compile-time pinning via explicit method reference).

12. **`BuildMergeStubNode` with `coalesce(m.isStub, true)` reads a potentially non-existent property in FalkorDB.** OpenCypher's `coalesce` on a missing property returns `null`, which THEN defaults to `true` — but this behavior is provider-specific. **Mitigation:** (a) FalkorDB 1.0 (per `NFalkorDB` 1.0.0 in packages.props) documents OpenCypher-like semantics where `coalesce(null, x) = x` — verified acceptable; (b) test the exact behavior: `GraphQueryBuilderTests.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (runs against a FalkorDB integration fixture); (c) if coalesce is unreliable, fallback to an `ON CREATE SET` clause: `MERGE (m:MemoryUnit {id: $id}) ON CREATE SET m.isStub = true`. This is idempotent and safer — use this form instead.

13. **Workflow replay determinism when adding new activities.** DAPR Workflow replay is deterministic — the exact sequence of `CallActivityAsync` calls must match the history. Adding the SourceType.Event-gated block between lines 151 (existing embedding) and 191 (existing fan-out) for events-in-flight at the moment of deploy will BREAK replay because the history says "after GenerateEmbeddingActivity, fan-out started" but the new code expects a GenerateNaturalLanguageDescriptionActivity call. **Mitigation:** (a) the SourceType check runs BEFORE any new activity call — pre-9.2 memory units have `SourceType != Event` (they're `File`/`Url`), so the new block is never entered; the 9.1-ingested events DO have `SourceType == Event`, but those workflows will have ALREADY completed before the deploy; (b) IF an events workflow is mid-flight during deploy, the replay will attempt to call the new activity — DAPR workflow replay fails deterministically (raises `InvalidOperationException` from the runtime); (c) operator runbook: quiesce `SourceType.Event` ingestion for 2 minutes before deploying 9.2 (this is the standard DAPR Workflow deploy pattern — documented in the architecture D23); (d) guard test `IngestionWorkflowReplaySafetyTests.PreNineTwoEventWorkflow_ReplayedAfterDeploy_CompletesDeterministically` simulates an in-flight workflow mid-deploy.

14. **NL description metadata field bloat.** The LLM-generated description (up to 80 tokens ≈ 500 characters) is persisted as `metadata["event.naturalLanguageDescription"]`. For tenants with millions of events, this adds meaningful storage + Redis memory. **Mitigation:** (a) the description is ALREADY in the NL Redis Vector hash (field `naturalLanguageDescription`) — metadata duplication is optional; (b) DECISION: persist to metadata ONLY when `NaturalLanguageDescriptionOptions.PersistInMetadata = true` (default `false`) — operators who want the field queryable via FT.SEARCH on the syntactic index can enable it; (c) guard test `IngestionWorkflowTests.NaturalLanguageDescriptionMetadata_PersistedOnlyWhenConfigured`.

15. **`correlated_with` edge on root-to-self via Story 9.1 mapper.** 9.1 Task 2.5 extracts `CorrelationId` from extension attribute OR envelope. If a publisher sets `correlationid = cloudevent.id` (the correlation IS this event — a common root-event convention), the edge MERGE would create a self-loop (`(root)-[:CORRELATED_WITH]->(root)`). Existing `IndexGraphActivity` would not detect this. **Mitigation:** (a) Task 6 adds the self-edge guard in `IndexGraphActivity` (`if (input.CorrelationId != input.MemoryUnitId)`); (b) `BuildMergeStubNode` called with `memoryUnitId == $id` is idempotent on a MERGE — no harm, just wasted cycle — safe to keep; (c) guard test `IndexGraphActivityTests.CorrelationIdEqualsMemoryUnitId_SkipsEdgeCreation` + emits `9155` log at Debug.

16. **Cross-tenant cache leakage via DAPR Conversation `responseCacheTTL`.** The DAPR Conversation sidecar shares a single response cache ACROSS tenants. With non-zero TTL, two tenants ingesting events whose payloads happen to hash identically (e.g., two SaaS customers using the same off-the-shelf event schema with anonymized IDs) would share the cached LLM summary. While unlikely in production (aggregate IDs usually differ), the BLAST RADIUS of a leak is a tenant's NL description appearing in another tenant's vector index — an unrecoverable privacy incident. **Mitigation:** (a) ship `responseCacheTTL: 0s` as MVP default in `deploy/dapr/components/conversation-llm.yaml` (Task 1.1 — caching DISABLED out of the box); (b) document the opt-in procedure in `docs/dev/eventstore-integration.md` under "Response caching opt-in procedure" — operators who enable caching MUST acknowledge the cross-tenant sharing semantics in writing; (c) guard test `ConversationComponentDefaultsTests.DefaultComponentYaml_HasResponseCacheTtlZero` (parses the YAML at test time and asserts the value); (d) telemetry hook: emit `memories_conversation_cache_hit_total{tenant=...}` (counter) so operators who DO enable caching can observe cache hit rates per tenant — a non-zero value across multiple tenants is the leak signature.

17. **`EmbeddingInput` wire-shape change breaks workflow replay of paused workflows.** Task 3.1 originally proposed switching `EmbeddingInput` from positional record `(string TenantId, string ContentText)` to property-init record with `[required]` properties + new `ContentKind`. JSON serialization changes shape from positional payload to named properties. ANY paused workflow with `EmbeddingInput` in its history (not just `IngestionWorkflow` — also `ReIngestionCoordinator` from Story 6.3 + any post-9.2 BackfillWorkflow) would fail deterministic replay. The 2-minute quiesce window from Risk #13 covers `IngestionWorkflow` for `SourceType.Event` ONLY; the contract is shared across ingestion paths. **Mitigation:** (a) PRESERVE the positional shape — make `ContentKind` a positional parameter with default value: `public sealed record EmbeddingInput(string TenantId, string ContentText, EmbeddingContentKind ContentKind = EmbeddingContentKind.Payload);` — System.Text.Json deserializes historical `{"TenantId":"t","ContentText":"c"}` payloads correctly with the default applied (verified: positional record + default value is wire-compatible); (b) guard test `EmbeddingInputTests.HistoricalJsonPayload_DeserializesWithDefaultContentKind` + `EmbeddingInputTests.RoundTripJsonSerialization_PreservesContentKind` (Task 3.4); (c) replay-safety test `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` simulates an in-flight `IngestionWorkflow` whose history contains the V1 EmbeddingInput shape and asserts the V2 code replays without DeterministicReplayException; (d) DOCUMENT in Anti-Patterns: "DO NOT switch `EmbeddingInput` to property-init record syntax — positional shape is wire-compat-load-bearing".

**Risk → Guard test mapping:**

| # | Risk | Guard test |
|---|------|-----------|
| 1 | `DAPR_CONVERSATION` warning pollution | `ProjectCompilationTests.Server_SuppressesDaprConversationWarningOnly` |
| 2 | LLM latency NFR6 p95 | `DualEmbeddingLatencyTests.EventIngestion_P95Under7s_WithLlm_UnderNormalConditions` |
| 3 | CorrelationId root-only semantics | `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `.CorrelationIdEqualsMemoryUnitId_NoSelfEdge` + `.MultipleEventsSameCorrelationId_NoFanOut` + `GraphTraversalServiceTests.CorrelatedWith_InboundDirection_ReturnsCorrelatedSiblings` |
| 4 | Gap marker heuristic brittleness | `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes` |
| 5 | Second vector index schema drift | `IndexSchemaDefinitionsTests.BothSemanticSchemas_HaveIdenticalVectorFieldShape` + `ProvisionRedisVectorActivityTests.CreatesBothIndexes_SameDimensions` |
| 6 | Doubled embedding API volume | `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` + `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget` |
| 7 | LLM returns JSON/markdown when text expected | `NaturalLanguageResponseCleanerTests.StripsMarkdownCodeFences` + `.StripsCommonPreambles` + `.EmptyAfterCleanupThrows` |
| 8 | LLM hallucination (posture) | `NaturalLanguageDescriptionPromptTests.PromptContainsHallucinationWarning` + documentation-level only |
| 9 | Retry queue unbounded growth | `NaturalLanguageEmbeddingRetryHostedServiceTests.BacklogExceedsWarningThreshold_EmitsLog` + `.BacklogExceedsErrorThreshold_BacksOffInterval` + `.DeadLetterQueue_AcceptsPermanentlyFailedRecords` |
| 10 | `conversation.echo` in dev misleads | `ConversationComponentResolutionTests.EchoComponent_EmitsWarningInDev_NeverAllowedInProduction` |
| 11 | `Dapr.AI` alpha API surface change | `DaprConversationIntegrationTests.ApiSurfaceSmokeTest` |
| 12 | `coalesce` on missing FalkorDB property | `GraphQueryBuilderTests.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` (Tier-2 FalkorDB fixture) |
| 13 | Workflow replay determinism on deploy | `IngestionWorkflowReplaySafetyTests.PreNineTwoEventWorkflow_ReplayedAfterDeploy_CompletesDeterministically` |
| 14 | NL description metadata field bloat | `IngestionWorkflowTests.NaturalLanguageDescriptionMetadata_PersistedOnlyWhenConfigured` |
| 15 | CorrelationId self-reference edge | `IndexGraphActivityTests.CorrelationIdEqualsMemoryUnitId_SkipsEdgeCreation` |
| 16 | Cross-tenant cache leakage via `responseCacheTTL` | `ConversationComponentDefaultsTests.DefaultComponentYaml_HasResponseCacheTtlZero` + telemetry counter `memories_conversation_cache_hit_total{tenant=...}` |
| 17 | `EmbeddingInput` wire-shape change breaks paused workflow replay | `EmbeddingInputTests.HistoricalJsonPayload_DeserializesWithDefaultContentKind` + `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` |
| #6 follow-up | Doubled embedding API volume (rate limiter shared budget) | `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget` |
| #9 follow-up | Multi-tenant fairness in retry queue scheduling | `NaturalLanguageEmbeddingRetryHostedServiceTests.MultipleTenantsWithBacklog_FairlyDequeuesAcrossTenants` + `.RestartMidIteration_DoesNotDoubleScheduleSameRecord` + `NaturalLanguageEmbeddingRetryWorkflowTests.Idempotency_DuplicateScheduling_DoesNotDoubleIndex` |
| #2 follow-up | Index-side partial + terminal failure compensation | `DegradedNaturalLanguageEmbeddingTests` Scenario B (workflow retry) + Scenario C (cleanup compensation drops both hashes) |
| AC #1 (FR60) | Dual embedding round-trip | `DualEmbeddingRoundTripTests.EventPublished_IndexedInBothSemanticIndexes_WithinNfr6Window` |
| AC #3 | Out-of-order events with gap marker + stubCreatedAt | `OutOfOrderEventTests.EventBBeforeA_GapMarkerCreatedForA_RetroactivelyResolvedWhenAArrives` + `GraphTraversalServiceTests.OrphanStubQuery_ReturnsStubsOlderThanThreshold` |
| AC #5 | Degraded NL embedding path | `DegradedNaturalLanguageEmbeddingTests.LlmUnavailable_RawEmbeddingStillIndexed_NlQueuedForRetry` + `.RetryWorkflow_SucceedsWhenLlmRecovers_WritesToNlIndex` |

---

## Story

As a developer,
I want events to receive dual embeddings and automatic causal chain graph edges,
so that events are searchable both by technical payload and business meaning, with causal relationships preserved automatically, and out-of-order event delivery never loses causal structure.

## Acceptance Criteria

1. **Given** a CloudEvent with a structured JSON payload has been ingested via Story 9.1's pipeline **When** `IngestionWorkflow` processes a `SourceType.Event` unit **Then** two embeddings are generated: (a) embedding-1 from the raw JSON payload via `GenerateEmbeddingActivity` with `EmbeddingInput.ContentKind = EmbeddingContentKind.Payload`, written to Redis Vector hash `{tenant}:vec:{memoryUnitId}` against index `{tenant}:memories:vec` — **exactly the existing Story 1.5 behavior, unchanged**; (b) embedding-2 from a natural-language description produced via `DaprConversationClient.ConverseAsync` against the DAPR component named `"llm"`, written to Redis Vector hash `{tenant}:vec:nl:{memoryUnitId}` against index `{tenant}:memories:vec:nl` — NEW. Both embeddings use the SAME tenant-configured provider + model + dimensions (so both indexes have identical vector field shape).

2. **Given** a successful NL description generation **When** the hash `{tenant}:vec:nl:{memoryUnitId}` is written **Then** alongside the `embedding` bytes, the hash contains fields `memoryUnitId`, `caseId`, `naturalLanguageDescription` (the LLM-authored text), `descriptionOrigin = "ai"`, `descriptionConfidence` (float 0.0-1.0), `embeddingProvider`, `embeddingModel`, `embeddingDimensions` — for operator inspection via `HGETALL`. The description is additionally written to `metadata["event.naturalLanguageDescription"]` ONLY when `NaturalLanguageDescriptionOptions.PersistInMetadata = true` (default `false` for payload storage economy — Risk #14).

3. **Given** an event with `CausationId` pointing to a memory unit that **has NOT yet been ingested** (out-of-order delivery) **When** `IndexGraphActivity` runs **Then** a stub node is created by `BuildMergeStubNode(causationId, stubCreatedAt)` with `m.isStub = true` AND `m.stubCreatedAt = <ISO-8601 timestamp>` (set atomically via `ON CREATE SET` — Risk #12), the `caused_by` edge is created from stub to current event (confidence `1.0`, origin `explicit`), and the traversal service returns an explicit gap marker for the stub when queried. The `stubCreatedAt` property enables operator orphan-detection queries — stubs older than a configurable threshold (default 24h) can be surfaced via `MATCH (m:MemoryUnit) WHERE m.isStub = true AND m.stubCreatedAt < <threshold>` for operator triage. **Later when event A (the missing cause) arrives** **Then** `BuildMergeMemoryUnitNode` MERGE promotes the stub to a full node, `m.isStub = false` (the `stubCreatedAt` property is preserved as historical evidence — operators can compute resolution latency = `nowResolved - stubCreatedAt`), all content properties are set, and the `caused_by` edge remains intact pointing from the now-resolved node. A `9154 StubNodeResolved` log event is emitted (Information level, includes `stubCreatedAt` + `resolvedAt` so operators can observe retroactive completion latency).

4. **Given** an event with `CorrelationId = X` **When** `IndexGraphActivity` runs **Then** (a) if `CorrelationId == MemoryUnitId` (the event IS the correlation root), NO `correlated_with` edge is created and `9155 CorrelationIdSelfEdgeSkipped` is logged at Debug level; (b) if `CorrelationId != MemoryUnitId`, `BuildMergeStubNode(CorrelationId)` ensures the root stub exists, then `BuildMergeEdge(sourceNodeId: CorrelationId, targetNodeId: MemoryUnitId, CorrelatedWith, 0.8, Explicit)` creates ONE edge from root to current event — **NOT a fan-out to all events sharing the correlation**. Traversal direction: a traversal from the root with `direction=outbound, edgeType=CorrelatedWith` returns all correlated events; from any correlated event with `direction=inbound` returns only the root. Documented explicitly in `docs/dev/eventstore-integration.md` under "Correlation root semantics".

5. **Given** `DaprConversationClient.ConverseAsync` raises an exception (LLM provider outage, DAPR sidecar restart, timeout per `LlmRequestTimeoutSeconds`) **When** `GenerateNaturalLanguageDescriptionActivity` catches it **Then** the activity throws a typed `NaturalLanguageDescriptionUnavailableException` (NOT a transient `Exception` — this prevents workflow-level retry storms against a chronic outage). The workflow catches this SPECIFIC exception, sets `NaturalLanguageEmbeddingStatus = Queued`, calls `FailedNaturalLanguageEmbeddingRegistry.EnqueueAsync`, and proceeds with the normal fan-out (syntactic + raw-semantic + graph indexing). The memory unit is `MemoryUnitStatus.Indexed` and searchable on three axes; the NL semantic axis fills in asynchronously.

6. **Given** the `NaturalLanguageEmbeddingRetryHostedService` is running **When** the LLM recovers **Then** on the next retry interval (default 60s), the service dequeues ≤`BatchSize` records from `nl-embedding-retry:{tenantId}`, schedules `NaturalLanguageEmbeddingRetryWorkflow` per record, which re-runs the NL description + embedding + index activities; on success, `ZREM` removes the record + emits `9153 NaturalLanguageEmbeddingRetrySucceeded`; on failure, increments an attempt counter and re-enqueues — unless `Attempts ≥ MaxRetryAttempts` (default 5), in which case the record moves to `nl-embedding-retry-dead:{tenantId}` for operator triage.

7. **Given** an event has been indexed with dual embeddings **When** the semantic search axis (Story 2.2's `SemanticSearchService`) runs a hybrid query **Then** the existing service continues to query `{tenant}:memories:vec` exclusively — **9.2 adds the NL index but does NOT change search behavior**. A follow-up story wires the NL axis into hybrid search. 9.2's responsibility is the data-production side; the data is inspectable via `HGETALL {tenant}:vec:nl:{memoryUnitId}` and via the new `NaturalLanguageSemanticSearchService` class (SHIPPED as a library but NOT wired into `HybridSearchService` — search consumers can opt in). The separation is intentional for safe staged rollout.

8. **Given** `TenantProvisioningWorkflow` creates a new tenant **When** the Redis Vector provisioning activity runs **Then** it creates BOTH `{tenant}:memories:vec` (existing) AND `{tenant}:memories:vec:nl` (NEW) with identical HNSW/FLOAT32/COSINE schema and the tenant's configured embedding dimensions. Both indexes are dropped on tenant deletion. Compensation rollback in the provisioning saga drops both on failure. **This is a Story 5.1/5.2 EDIT, gated to avoid regression on existing tenants** — `ProvisionRedisVectorActivity` is idempotent on existing indexes.

9. **Given** a tenant deletion workflow runs **When** the semantic-backend deletion activity processes the tenant **Then** it drops BOTH semantic indexes and removes both key prefixes (`{tenant}:vec:*` AND `{tenant}:vec:nl:*`) — no orphan NL vectors remain after tenant deletion.

10. **Given** a consistency verification run (`ConsistencyVerificationWorkflow` from Story 8.2) **When** a `SourceType.Event` memory unit is inspected **Then** the verification checks BOTH semantic hashes exist (raw AND NL, unless `NaturalLanguageEmbeddingStatus = Queued` which is documented-degraded-state). When `NaturalLanguageEmbeddingStatus = Queued`, the verifier reports a `NaturalLanguageEmbeddingMissing` **informational** note (NOT a consistency violation — degraded state is valid and tracked elsewhere via the retry queue).

11. **Given** a code-review-gated deployment **When** in-flight events workflows exist at deploy time **Then** the 9.2 operator runbook (updated `docs/dev/eventstore-integration.md` section "Deployment — quiescing event ingestion") instructs operators to pause **event publication ONLY** (i.e., `SourceType.Event` — file/url/re-ingestion paths remain safe due to Risk #17's positional-record mitigation) for ≥2 minutes before deploying 9.2. **ADDITIONALLY, Task 5.9's startup gate (`WorkflowReplaySafetyHostedService`) enforces the quiesce at the code level** — promoted from deferred per Winston's review: runbook-only discipline fails under on-call rotation churn, so a fail-safe startup check delays workflow-host startup until version-mismatched in-flight `IngestionWorkflow` instances clear (or 5min timeout) and emits `9171 InFlightWorkflowsMismatchAtStartup` at Critical level. Guard tests: `IngestionWorkflowReplaySafetyTests.PreNineTwoEventWorkflow_ReplayedAfterDeploy_CompletesDeterministically` + `WorkflowReplaySafetyHostedServiceTests.MismatchedWorkflowsPresent_DelaysUntilCleared`.

12. **Given** `appsettings.Production.json` is loaded **When** `NaturalLanguageDescriptionOptions` is bound **Then** `DaprComponentName` is NOT `"conversation.echo"` (production hard-guard — Risk #10). Startup validator (`NaturalLanguageDescriptionOptionsValidator : IValidateOptions<T>`) fails fast with a Critical log `9161 EchoComponentNotAllowedInProduction` if the production environment somehow resolves to the echo component.

13. **Given** all unit tests in `Hexalith.Memories.Server.Tests` + `Hexalith.Memories.EventStore.Tests` **When** `dotnet test` is run **Then** every test passes with `TreatWarningsAsErrors=true` (no `DAPR_CONVERSATION` warnings leak from `Dapr.AI` calls because the suppression is scoped to `Hexalith.Memories.Server.csproj`).

14. **Given** a Tier-2 integration test `DualEmbeddingRoundTripTests` runs against the Aspire AppHost fixture with DAPR slim init + Redis + FalkorDB + `conversation.echo` DAPR component **When** the test publishes a test CloudEvent **Then** the test observes (a) the memory unit indexed in `{tenant}:memories:vec`, (b) a sibling hash at `{tenant}:vec:nl:{memoryUnitId}` with the echo-returned description, (c) `caused_by` edge to the causation node, (d) the FalkorDB node has `isStub = false` — all within 7 seconds of publication (NFR6 relaxed for dual-embedding per Risk #2).

15. **Given** a Tier-2 integration test `OutOfOrderEventTests` **When** event B (with `causationid = A_id`) is published BEFORE event A **Then** the test observes (a) a stub node for A in FalkorDB with `isStub = true`, (b) a gap marker in the traversal result from B's node with `direction=inbound`, (c) after event A is published (~1s later), the stub is promoted to a full node with `isStub = false`, all A's content properties populated, and `9154 StubNodeResolved` emitted — the `caused_by` edge is preserved throughout.

16. **Given** a Tier-2 integration test `DegradedNaturalLanguageEmbeddingTests` **When** the DAPR Conversation component is stubbed to return an error **Then** event ingestion succeeds with `NaturalLanguageEmbeddingStatus.Queued`, the memory unit is searchable via the raw-semantic + syntactic + graph axes, `9152 NaturalLanguageEmbeddingQueuedForRetry` is emitted, and when the stub recovers, the background retry service completes the NL embedding within one retry interval and `9153 NaturalLanguageEmbeddingRetrySucceeded` is emitted.

17. **Given** `docs/dev/eventstore-integration.md` is updated **When** a developer reads the new sections **Then** they find: **"Dual embedding pipeline"** (why NL description, prompt template, LLM provider swap procedure, performance envelopes); **"Natural language embedding retry queue"** (inspection via CLI, dead-letter flow, alert rules); **"Gap markers and retroactive resolution"** (`isStub` flag semantics, stub-promotion telemetry, traversal behavior); **"Correlation root semantics"** (edge direction, self-loop avoidance, multi-event example); **"Deployment — quiescing event ingestion"** (replay-determinism runbook for 9.2 deploy); **"Local dev — `conversation.echo`"** (limitations, when embeddings will be degenerate); **"LLM provider swap procedure"** (how to migrate from OpenAI to Anthropic via YAML only); **"Known limitations"** (no per-tenant LLM, no streaming, no retroactive backfill). Guard test `DocumentationCompletenessTests.EventStoreIntegrationDoc_Has92Sections`.

## Tasks / Subtasks

### Pre-Impl Verification Spikes (MUST complete before dependent tasks start)

- [ ] **Spike 0.1 — Raw payload durability (blocks Task 8.1, 8.4, 8.5) [Improvement AI/AM].** Verify Story 1.5's content-durability model: after `IngestionWorkflow` completes, is `IngestionInput.ContentBytes` (the raw JSON payload for `SourceType.Event`) durably stored anywhere retrievable by `(tenantId, memoryUnitId)`? Grep `src/Hexalith.Memories.Server/` for `ContentBytes` usage post-ingestion + check Story 1.5's `IngestionWorkflow` end-state for a content-write step. **Decision tree:**
  - **If YES (payload durable):** proceed with payload-by-reference design as currently specified (Task 8.1 stores IDs only; Task 8.4 re-reads via `IMemoryUnitContentReader`). Document the storage location in Dev Notes "Raw payload durability".
  - **If NO (payload consumed and discarded):** fall back to **bounded payload-by-value** — `FailedNaturalLanguageEmbeddingRecord` carries `RawJsonPayload` but TRUNCATED to max `NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes` (default `4096`). Truncated payload still sufficient for LLM summarization (prompt already truncates to 8KB via `MaxPayloadChars`). Bounded count × bounded bytes = bounded Redis memory. Revert Task 8.1/8.2/8.4/5.4 to carry `RawJsonPayload` with size cap + emit `memories_natural_language_embedding_queue_bytes` gauge per tenant (reinstate `GetBacklogBytesAsync`).
  - **If UNCLEAR:** default to the bounded payload-by-value fallback — safer, still protects pre-mortem Failure δ. Document the fallback decision in the story's Dev Notes.
  - Spike deliverable: 1 paragraph in Dev Notes "Raw payload durability" + a ≤2 hour investigation.

- [ ] **Spike 0.2 — DAPR Workflow instance enumeration API (blocks Task 5.9) [Improvement AL].** Verify the `Dapr.Workflow` 1.17.x .NET SDK exposes a mechanism to enumerate active workflow instances filtered by workflow name + read their code-version metadata. Candidates to check:
  - `DaprWorkflowClient.GetInstances(...)` — does this method exist in 1.17.x? Spec assumed yes.
  - HTTP management API `GET /v1.0-alpha1/workflows/{component}/...`.
  - Direct state-store query via `DaprClient.GetStateAsync` against the workflow's actor-state key pattern.
  - `PurgeInstanceMetadataAsync` (destructive — wrong tool for read).
  - Spike deliverable: confirm which API works OR document that no read-side enumeration exists. **If no API works:** Task 5.9's startup gate collapses back to a runbook-only mitigation (Risk #13); update AC #11 accordingly and mark the code-level gate as deferred.

- [ ] **Spike 0.3 — `Dapr.AI` 1.17.6 exception surface (blocks Task 2.5) [Improvement AE].** Verify the exact exception types `DaprConversationClient.ConverseAsync` can throw. Spec assumed `ConversationException`; may not exist under that name. Read the SDK docs or decompile the NuGet to enumerate. Spike deliverable: update Task 2.5's catch list with verified types. Safe baseline if unclear: catch `DaprException` + `OperationCanceledException` + `HttpRequestException` + `TaskCanceledException`.

- [ ] **Spike 0.4 — `DaprWorkflow` HostedService ordering (blocks Task 5.9) [Improvement AF].** Verify that registering `WorkflowReplaySafetyHostedService` FIRST in `AddMemoriesServer` actually runs its `StartAsync` before DAPR's own workflow-host hosted service. Candidates if ordering isn't guaranteed: `IStartupFilter` (runs before any hosted service), `IHostApplicationLifetime.ApplicationStarted` callback, or a custom `IHostedLifecycleService.StartingAsync`. Spike deliverable: chosen mechanism named in Task 5.9 spec.

---

- [ ] Task 1: DAPR Conversation component + registration (AC: #1, #12)
  - [ ] 1.1 Create `deploy/dapr/components/conversation-llm.yaml` — dev-default `type: conversation.echo` with comments calling out that production deployments must swap to `conversation.openai` / `conversation.anthropic` / `conversation.googleai` via the YAML only. Include `responseCacheTTL: 0s` (MVP default — operators OPT IN to caching, NOT opt out, to avoid cross-tenant cache leakage at the sidecar level — Risk #16) + `piiScrubbing: false` with rationale comments.
  - [ ] 1.2 Add `<PackageReference Include="Dapr.AI" />` to `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` (version central in `Directory.Packages.props:36` — already present).
  - [ ] 1.3 Add `<NoWarn>$(NoWarn);DAPR_CONVERSATION</NoWarn>` to `Hexalith.Memories.Server.csproj` ONLY (NOT `Directory.Build.props` — prevents suppression sprawl).
  - [ ] 1.4 In the server's DI composition (locate via grep for `AddDaprClient` — likely `DependencyInjection/MemoriesServerServiceCollectionExtensions.cs`), call `services.AddDaprAiConversation()` to register `DaprConversationClient`.
  - [ ] 1.5 In `src/Hexalith.Memories.AppHost/Program.cs`, register the DAPR Conversation component: `var llm = builder.AddDaprComponent("llm", "conversation.echo");` + add `.WithReference(llm)` to the server sidecar builder. For production composition (a commented-out block), show the OpenAI variant wired to secrets.
  - [ ] 1.6 Create `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptions.cs` — **`sealed class`** with parameterless constructor + settable properties (`{ get; init; }` will NOT bind via `IOptions<T>` reliably across provider shapes; use regular settable properties): `string DaprComponentName = "llm"`, `int MaxPayloadChars = 8000`, `int RetryIntervalSeconds = 60`, `int BatchSize = 5`, `int MaxRetryAttempts = 5`, `int LlmRequestTimeoutSeconds = 15`, `bool PersistInMetadata = false`. Bind from config section `"NaturalLanguage"` via `services.Configure<NaturalLanguageDescriptionOptions>(config.GetSection("NaturalLanguage"))`. **DO NOT use `sealed record` with positional parameters** — `IConfiguration.Bind` requires parameterless ctor + settable properties; positional records fail binding silently (empty values at runtime). Expose `RetryIntervalSeconds` with test-override path (integration tests set 1s to avoid 60s flaky waits per Murat's review).
  - [ ] 1.7 Create `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionOptionsValidator.cs` — `IValidateOptions<NaturalLanguageDescriptionOptions>` that asserts:
    - (1) `DaprComponentName != "conversation.echo"` when `IHostEnvironment.IsProduction()`. Fails fast with `"9161 EchoComponentNotAllowedInProduction"`.
    - (2) **Response cache acknowledgment gate (Improvement V — security lead concession):** parse the resolved DAPR component YAML (from `deploy/dapr/components/conversation-llm.yaml` or the operator-configured path) for `responseCacheTTL`. If parsed value > `0s`, require EITHER `config["NaturalLanguage:AcceptCrossTenantCacheSharing"] == true` OR env var `HEXALITH_ACCEPT_CROSS_TENANT_CACHE_SHARING=1`. Without the acknowledgment, fail fast with `"9164 ResponseCacheEnabledWithoutAcknowledgment"` Critical. Rationale: the response cache is shared ACROSS tenants at the sidecar level — any non-zero TTL is a privacy-incident blast radius (Risk #16). README-only opt-in is not a control; the validator IS the control.
    - Unit tests: `.ProductionWithEchoComponent_ReturnsFailure` + `.ProductionWithRealComponent_ReturnsSuccess` + `.DevelopmentWithEchoComponent_ReturnsSuccess` + `.CacheTtlNonZero_WithoutAcknowledgment_ReturnsFailure_Emits9164` + `.CacheTtlNonZero_WithAcknowledgment_ReturnsSuccess` + `.CacheTtlZero_NoAcknowledgmentNeeded_ReturnsSuccess`.
  - [ ] 1.8 Add `NaturalLanguage` section to `src/Hexalith.Memories.Server/appsettings.Development.json` (`DaprComponentName = "llm"`, other fields at defaults). Add `NaturalLanguage` section to `src/Hexalith.Memories.Server/appsettings.Production.json` (override `DaprComponentName = "llm-openai"` — operator swaps the component name, NOT the field name, to pick a different provider).
  - [ ] 1.9 Unit tests: see Task 1.7 for options-validator tests + `ProjectCompilationTests.Server_SuppressesDaprConversationWarningOnly` — **stronger guard (Improvement AD, from Murat):** instead of file-content string matching on `Directory.Build.props`, the test should build a throwaway project that imports `Hexalith.Memories.Server` source and uses `DaprConversationClient`, then assert `csc` emits zero `DAPR_CONVERSATION` diagnostics. Plus new `MemoriesJsonContextCompletenessTests.AllContractTypes_AreRegistered` — reflects over `Hexalith.Memories.Contracts.V1` public types and asserts each has a corresponding `[JsonSerializable]` attribute in `MemoriesJsonContext` (catches AOT serialization-registration omissions class-wide, not per-type).

- [ ] Task 2: `GenerateNaturalLanguageDescriptionActivity` (AC: #1, #5)
  - [ ] 2.1 Create `src/Hexalith.Memories.Contracts/V1/NaturalLanguageDescriptionInput.cs` — sealed record `(string TenantId, string MemoryUnitId, string RawJsonPayload, string EventType, string? AggregateType)`. Register in `MemoriesJsonContext` via `[JsonSerializable(typeof(NaturalLanguageDescriptionInput))]`.
  - [ ] 2.2 Create `src/Hexalith.Memories.Contracts/V1/NaturalLanguageDescriptionResult.cs` — sealed record `(string Description, float? EstimatedConfidence, ConfidenceSource ConfidenceSource, string LlmProvider, string LlmModel)`. **`EstimatedConfidence` is nullable** (Occam reshape): when `ConfidenceSource = Constant` or `Unknown`, the value is `null` — there is no numeric to display. When `ConfidenceSource = Logprobs`, the value is the computed `exp(avg(logprob))`. This eliminates the "which constant should the pseudo-measurement be" debate entirely and forces UI to render only real numeric confidence. **`ConfidenceSource`** is a new enum in `src/Hexalith.Memories.Contracts/V1/ConfidenceSource.cs`: `{ Logprobs, Constant, Unknown }` with `[JsonConverter(typeof(CamelCaseStringEnumConverter<ConfidenceSource>))]`. Rationale (Dr. Quinn + Freya promoted from deferred, refined via Occam): a constant numeric weaponizes the UX signal. Nullable + source-enum makes "measured vs. unmeasured" structurally distinct. Register both in `MemoriesJsonContext`.
  - [ ] 2.3 Create `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageDescriptionUnavailableException.cs` — public sealed exception. Thrown when LLM call cannot produce a valid description AFTER the activity's internal retry exhausts. Carries `LlmProvider` + `CorrelationId` (from DAPR context) for diagnostics.
  - [ ] 2.4 Create `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageResponseCleaner.cs` — `internal static class` with `TryClean(string rawResponse, out string cleaned)`. Logic: trim, strip common Markdown code fences (```), strip preambles (`"Summary:"`, `"Here is the summary:"`, `"Here's a summary:"` — case-insensitive allow-list), collapse whitespace. Returns `false` if the cleaned string is empty or < 10 chars.
  - [ ] 2.5 Create `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateNaturalLanguageDescriptionActivity.cs` — `WorkflowActivity<NaturalLanguageDescriptionInput, NaturalLanguageDescriptionResult>`. Constructor-inject `DaprConversationClient`, `IOptions<NaturalLanguageDescriptionOptions>`, `ILogger`. In `RunAsync`:
    - Truncate `input.RawJsonPayload` to `options.MaxPayloadChars`.
    - Build `ConversationInput` with system prompt ("You are an event summarizer...") + user message ("Event type: ...\nAggregate: ...\nPayload:\n{truncatedJson}").
    - Create `ConversationOptions(options.DaprComponentName) { Temperature = 0.1, MaxTokens = 80 }` — NO tools, NO streaming, NO `ResponseFormat`.
    - `using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.LlmRequestTimeoutSeconds));`
    - Call `await _conversationClient.ConverseAsync([messages], conversationOptions, cts.Token).ConfigureAwait(false);`
    - Extract raw response text; run `NaturalLanguageResponseCleaner.TryClean`; on cleaner failure, throw `NaturalLanguageDescriptionUnavailableException("Cleaner rejected empty/malformed response")`.
    - Extract `LlmProvider` + `LlmModel` from the response metadata if available (see DAPR Conversation response shape research in Dev Notes) — fallback to `options.DaprComponentName` for `LlmProvider`, `"unknown"` for `LlmModel`.
    - Derive `EstimatedConfidence` + `ConfidenceSource`: if logprobs available, `EstimatedConfidence = exp(avg(logprob))` + `ConfidenceSource = Logprobs`; else `EstimatedConfidence = null` + `ConfidenceSource = Constant`; if malformed/partial → `EstimatedConfidence = null` + `ConfidenceSource = Unknown`. **Nullable when unmeasured — no pseudo-numeric.**
    - Emit `9150 NaturalLanguageDescriptionGenerated` + activity-source span `NaturalLanguageDescriptionGeneration` with duration histogram.
    - Catch the exception surface resolved by Spike 0.3 (safe baseline: `OperationCanceledException`, `TaskCanceledException`, `DaprException`, `HttpRequestException`; add typed `ConversationException` IF the SDK exposes it). On any caught exception → emit `9151 NaturalLanguageDescriptionSkippedLlmUnavailable` at Information level → throw `NaturalLanguageDescriptionUnavailableException`.
  - [ ] 2.6 Register the activity in `src/Hexalith.Memories.Server/DependencyInjection/MemoriesServerServiceCollectionExtensions.cs` (follow the pattern of existing `options.RegisterActivity<GenerateEmbeddingActivity>()` — likely at L746 of architecture.md reference).
  - [ ] 2.7 Add retry policy entry to `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs`: new key `nameof(GenerateNaturalLanguageDescriptionActivity)` with `ActivityRetryPolicy` tuned for LLM: `maxNumberOfAttempts = 2`, `firstRetryInterval = TimeSpan.FromSeconds(3)`, `backoffCoefficient = 3.0`, `maxRetryInterval = TimeSpan.FromSeconds(30)`. Rationale: LLM outages are typically longer than 30s; workflow-level replay is the wrong level — use the queue instead. See Dev Notes "LLM retry policy".
  - [ ] 2.8 Unit tests: `GenerateNaturalLanguageDescriptionActivityTests.SuccessPath_ReturnsDescription_EmitsSuccessLog` + `.ConversationTimeout_ThrowsUnavailableException_EmitsSkippedLog` + `.DaprException_ThrowsUnavailableException` + `.EmptyResponseAfterCleaning_ThrowsUnavailableException` + `.MarkdownFencedResponse_IsCleaned` + `.PayloadExceedsMaxChars_IsTruncated` + `.LogprobsAvailable_SetsConfidenceSourceLogprobs_WithNumericValue` + `.NoLogprobs_SetsConfidenceSourceConstant_WithNullEstimatedConfidence` + `.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior` (Murat: documentation-control test — name is the warning; asserts the activity does NOT scrub PII by default under `piiScrubbing: false` — operators enable scrubbing via DAPR component metadata if needed) + `NaturalLanguageDescriptionPromptTests.PromptContainsHallucinationWarning`.
  - [ ] 2.9 Unit tests for cleaner: `NaturalLanguageResponseCleanerTests.StripsMarkdownCodeFences` + `.StripsCommonPreambles` + `.EmptyAfterCleanupThrows` + `.PreservesNormalSentence` + `.CollapsesWhitespace`.

- [ ] Task 3: `EmbeddingInput.ContentKind` + telemetry (AC: #1, Risk #6)
  - [ ] 3.1 Edit `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs` — PRESERVE the existing positional record shape; add `ContentKind` as a positional parameter with default value (Risk #17 — wire-shape compatibility for paused workflows):
    ```csharp
    public sealed record EmbeddingInput(
        string TenantId,
        string ContentText,
        EmbeddingContentKind ContentKind = EmbeddingContentKind.Payload);
    ```
    **DO NOT switch to property-init record syntax** — the historical positional JSON shape MUST remain wire-compatible because `EmbeddingInput` is referenced in any paused workflow's history (`IngestionWorkflow`, `ReIngestionCoordinator` from 6.3, future backfill workflows) and a shape change would break deterministic replay across the ingestion plane (Risk #17). System.Text.Json deserializes historical payloads `{"TenantId":"t","ContentText":"c"}` correctly with the default `ContentKind = Payload` applied (verified via positional-record + default-value semantics). Existing callers compile unchanged because the new parameter has a default. New callers (NL embedding path) explicitly pass `ContentKind: EmbeddingContentKind.NaturalLanguageDescription` as a named argument. Document in Anti-Patterns: "DO NOT switch `EmbeddingInput` to property-init record syntax — positional shape is wire-compat-load-bearing".
  - [ ] 3.2 Create `src/Hexalith.Memories.Contracts/V1/EmbeddingContentKind.cs` — enum `{ Payload, NaturalLanguageDescription }`. `[JsonConverter(typeof(CamelCaseStringEnumConverter<EmbeddingContentKind>))]`. Register in `MemoriesJsonContext`.
  - [ ] 3.3 In `GenerateEmbeddingActivity.RunAsync`, add a telemetry tag `TagContentKind` on the activity span based on `input.ContentKind`. Emit `memories_embedding_api_call_total{contentKind="payload|naturalLanguageDescription",tenant=...}` counter increment.
  - [ ] 3.4 Unit tests: `GenerateEmbeddingActivityTests.ContentKind_PropagatesToTelemetryTag` + `.DefaultConstructor_SetsPayloadKind` + `EmbeddingInputTests.RoundTripJsonSerialization_PreservesContentKind` + `EmbeddingInputTests.HistoricalJsonPayload_DeserializesWithDefaultContentKind` (Risk #17 wire-compat — feeds the deserializer a 9.1-shape `{"TenantId":"t","ContentText":"c"}` payload, asserts `ContentKind == Payload`) + `EmbeddingInputReplaySafetyTests.PreNineTwoEmbeddingActivityHistory_ReplaysSuccessfully` (simulates an in-flight `IngestionWorkflow` whose durable history contains the V1 EmbeddingInput shape; replays under V2 code; asserts no `DeterministicReplayException`) + `EmbeddingRateLimiterActorTests.BothContentKinds_ConsumeSameBudget` (sanity test for Risk #6 mitigation — submits one Payload + one NaturalLanguageDescription call for the same tenant, asserts BOTH consume the actor's budget; failure mode would be a regression introducing per-content-kind separate budgets).

- [ ] Task 4: NL semantic index schema + activity (AC: #1, #2, #8, #9, Risk #5)
  - [ ] 4.1 Edit `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` — add four constants + six methods:
    - `public const string NaturalLanguageSemanticIndexSuffix = ":memories:vec:nl";`
    - `public const string NaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:";`
    - `public static string GetNaturalLanguageSemanticIndexName(string tenantId) => tenantId + NaturalLanguageSemanticIndexSuffix;`
    - `public static string GetNaturalLanguageSemanticKeyPrefix(string tenantId) => tenantId + NaturalLanguageSemanticKeyPrefixSuffix;`
    - `public static FTCreateParams CreateNaturalLanguageSemanticParams(string tenantId) => new FTCreateParams().On(IndexDataType.HASH).Prefix(GetNaturalLanguageSemanticKeyPrefix(tenantId));`
    - `public static Schema CreateNaturalLanguageSemanticSchema(int dimensions)` — same shape as `CreateSemanticSchema` with additional text field for `naturalLanguageDescription`:
      ```csharp
      new Schema()
          .AddVectorField("embedding", Schema.VectorField.VectorAlgo.HNSW, new Dictionary<string, object> {
              ["TYPE"] = "FLOAT32",
              ["DIM"] = dimensions.ToString(),
              ["DISTANCE_METRIC"] = "COSINE",
          })
          .AddTagField("memoryUnitId")
          .AddTagField("caseId")
          .AddTextField("naturalLanguageDescription", 1.0);
      ```
    - Refactor both `CreateSemanticSchema` and `CreateNaturalLanguageSemanticSchema` to delegate to a private `CreateSemanticSchemaCore(int dimensions, bool includeNaturalLanguageText)` helper so future schema edits can't drift (Risk #5).
    - Extend `GetSemanticFieldIdentifiers()` + add a second `GetNaturalLanguageSemanticFieldIdentifiers()` returning `["embedding", "memoryUnitId", "caseId", "naturalLanguageDescription"]`.
  - [ ] 4.2 Create `src/Hexalith.Memories.Contracts/V1/NaturalLanguageIndexInput.cs` — sealed record mirroring `IndexInput`'s shape. **Explicitly list mirrored fields** (Improvement AH — don't leave implicit): `TenantId`, `MemoryUnitId`, `CaseId`, `Vector` (float[]), `EmbeddingProvider`, `EmbeddingModel`, `EmbeddingDimensions`. Plus three extras: `NaturalLanguageDescription` (string) + `DescriptionConfidence` (`float?` — nullable; matches `NaturalLanguageDescriptionResult.EstimatedConfidence`) + `ConfidenceSource` (`ConfidenceSource` enum). Register in `MemoriesJsonContext`.
  - [ ] 4.3 Create `src/Hexalith.Memories.Server/Activities/Indexing/IndexNaturalLanguageSemanticActivity.cs` — structural clone of `IndexSemanticActivity.cs` with substitutions:
    - Takes `NaturalLanguageIndexInput` (not `IndexInput`).
    - `indexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(input.TenantId);`
    - `hashKey = $"{input.TenantId}:vec:nl:{input.MemoryUnitId}";`
    - Writes hash entries: `embedding` (vector bytes), `memoryUnitId`, `caseId`, `naturalLanguageDescription`, `descriptionOrigin = "ai"`, `descriptionConfidence = input.DescriptionConfidence?.ToString(InvariantCulture) ?? ""` (empty string when unmeasured — `HGET` returns empty string, UI treats as absent), `descriptionConfidenceSource = input.ConfidenceSource.ToString().ToLowerInvariant()` (`logprobs|constant|unknown`), `embeddingProvider`, `embeddingModel`, `embeddingDimensions`.
    - Log message: `"Indexed memory unit {MemoryUnitId} in NL semantic index for tenant {TenantId}"`.
    - Returns `new IndexResult("semantic-nl", input.MemoryUnitId, input.TenantId);` — the `"semantic-nl"` backend name is used by consistency verification and cleanup.
  - [ ] 4.4 Register `IndexNaturalLanguageSemanticActivity` in `options.RegisterActivity<...>()` call site + add retry-policy key `nameof(IndexNaturalLanguageSemanticActivity)` (inherits default, no special config).
  - [ ] 4.5 Edit `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs`: after the existing `FT.CREATE` for the semantic index, add a second `FT.CREATE` for the NL index using `CreateNaturalLanguageSemanticParams` + `CreateNaturalLanguageSemanticSchema(config.Dimensions)`. Both idempotent on "Index already exists".
  - [ ] 4.6 Edit `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorIndexActivity.cs`: drop BOTH indexes. Edit `DeleteRedisVectorActivity.cs`: enumerate + delete BOTH `:vec:*` AND `:vec:nl:*` keys. Extend the batch-delete pattern used by existing code (see `DeleteFalkorDbBatchActivity` for the batched-delete reference).
  - [ ] 4.7 Edit `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs`: delete BOTH `{tenant}:vec:{memoryUnitId}` AND `{tenant}:vec:nl:{memoryUnitId}` (idempotent — `DEL` is a no-op on missing keys).
  - [ ] 4.8 Unit tests: `IndexSchemaDefinitionsTests.BothSemanticSchemas_HaveIdenticalVectorFieldShape` + `.NaturalLanguageKeyPrefix_DoesNotCollideWithSemanticKeyPrefix` + `ProvisionRedisVectorActivityTests.CreatesBothIndexes_SameDimensions` + `IndexNaturalLanguageSemanticActivityTests.WritesDistinctHashKey_FromSemanticActivity` + `.WritesConfidenceSourceField` + `CleanupSemanticActivityTests.DeletesBothHashes_Idempotent` + `DeleteRedisVectorActivityTests.DeletesBothKeyPrefixes`.
  - [ ] 4.9 Create `src/Hexalith.Memories.Server/Search/NaturalLanguageSemanticSearchService.cs` — **library class only; NOT wired into `HybridSearchService`** (AC #7 contract). Structural clone of Story 2.2's `SemanticSearchService` with one constant change: queries `{tenant}:memories:vec:nl` instead of `{tenant}:memories:vec`. Exposes `SearchAsync(string tenantId, ReadOnlyMemory<float> queryVector, int topK, CancellationToken ct) → IReadOnlyList<SemanticSearchHit>` with hits carrying `naturalLanguageDescription` + `descriptionConfidence` + `descriptionConfidenceSource` for inspection. Register in DI (`services.AddSingleton<NaturalLanguageSemanticSearchService>()`) but NOT as a replacement for `SemanticSearchService`. Unit test: `NaturalLanguageSemanticSearchServiceTests.SearchAsync_RoundTripsHitsWithDescriptionMetadata` (mocked Redis client, single happy-path).
  - [ ] 4.10 **Compensation boundary fix (Winston — promoted from deferred) + startup reconciler (chaos Scenario D — NOT duplicate defense; catches SIGKILL-during-provisioning case that compensation workflow cannot reach):** edit `src/Hexalith.Memories.Server/Workflows/TenantProvisioningWorkflow.cs` (Story 5.1) compensation path to enumerate BOTH `:memories:vec` AND `:memories:vec:nl` suffixes on provisioning rollback. Scenario: if provisioning fails AFTER the raw index is created but BEFORE the NL index is created (Task 4.5 sequence), the compensation must drop whichever indexes exist — not just the ones the 5.1 spec enumerated. **Additionally ship a startup reconciler (`OrphanSemanticIndexReconciler`) that sweeps orphan `:memories:vec:nl` indexes with no matching `:memories:vec`** — one-shot sweep at server startup, idempotent. The reconciler catches the SIGKILL-during-provisioning case where mid-workflow compensation cannot run. Guard tests: `TenantProvisioningCompensationTests.MidProvisioningFailure_RollbackDropsBothIndexes_NoOrphan` + `OrphanSemanticIndexReconcilerTests.NLIndexWithoutRawSibling_IsDropped` + `.RawIndexWithNLSibling_BothRetained` + `.ReconcilerIdempotent_MultipleStartupsDoNotDoubleAct`.

- [ ] Task 5: `IngestionWorkflow` dual-embedding branch + degraded path (AC: #1, #5, #10, Risk #13, Risk #14)
  - [ ] 5.1 Edit `src/Hexalith.Memories.Contracts/V1/IngestionResult.cs` — add `NaturalLanguageEmbeddingStatus NaturalLanguageEmbeddingStatus { get; init; }` with default `NaturalLanguageEmbeddingStatus.NotApplicable` (additive — workflow replay of pre-9.2 results stays compatible).
  - [ ] 5.2 Create `src/Hexalith.Memories.Contracts/V1/NaturalLanguageEmbeddingStatus.cs` — enum `{ Indexed, Queued, NotApplicable }`, camelCase converter. Register in `MemoriesJsonContext`.
  - [ ] 5.3 Edit `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — between line 159 (`logger.LogInformation("Embedding generated...")`) and line 161 (`currentStage = "indexing"`), insert the SourceType.Event-gated dual-embedding block per TL;DR pseudocode (item #6). Notes:
    - Pass cloudevent.type / event.aggregateType from `input.Metadata` (guaranteed present by 9.1's mapper).
    - Catch `NaturalLanguageDescriptionUnavailableException` ONLY. All other exceptions propagate for workflow-level retry per existing policy. Do NOT widen the catch.
    - Emit `9152 NaturalLanguageEmbeddingQueuedForRetry` when falling into the degraded path.
    - Enqueue via `context.CallActivityAsync<bool>(nameof(QueueNaturalLanguageEmbeddingRetryActivity), new QueueNaturalLanguageEmbeddingRetryInput(...))` — NOT a direct `FailedNaturalLanguageEmbeddingRegistry.EnqueueAsync` call (workflows MUST NOT call services directly — architecture D25).
  - [ ] 5.4 Create `src/Hexalith.Memories.Server/Activities/Ingestion/QueueNaturalLanguageEmbeddingRetryActivity.cs` + `QueueNaturalLanguageEmbeddingRetryInput.cs`. Constructor-inject `FailedNaturalLanguageEmbeddingRegistry _registry`. In `RunAsync`, call `_registry.EnqueueAsync(input.TenantId, input.MemoryUnitId, context.CurrentUtcDateTime.Ticks, CancellationToken.None)`. **`QueueNaturalLanguageEmbeddingRetryInput` carries ONLY `(TenantId, MemoryUnitId)`** — NOT the raw payload (payload-by-reference design from Task 8.1). On retry, `NaturalLanguageEmbeddingRetryWorkflow` re-reads the raw payload from the memory unit's existing Redis hash via an injected `IMemoryUnitReader`.
  - [ ] 5.5 Extend the workflow's existing fan-out to include a FOURTH task `indexNaturalLanguageSemanticTask` when `nlEmbedding != null`. Add the task to `Task.WhenAll` and include `"semantic-nl"` in the `GetCompletedBackends` aggregation + `CompensateAsync` dispatch (cleanup semantic compensation handles both — Task 4.7 — so no new compensation activity needed; document explicitly in comments).
  - [ ] 5.6 Populate `IngestionResult.NaturalLanguageEmbeddingStatus` at every return path:
    - Duplicate early-return (line 92): `NotApplicable` (dedup ran before SourceType check).
    - Main success path: `Indexed` when `nlStatus == Indexed`, `Queued` when degraded.
    - `NotApplicable` for `SourceType != Event`.
  - [ ] 5.7 When `options.PersistInMetadata == true`, add `input.Metadata["event.naturalLanguageDescription"] = new MetadataField(nlResult.Description, MetadataOrigin.Ai, nlResult.EstimatedConfidence)` BEFORE the fan-out so the value lands in both the syntactic hash + graph node. When `false`, skip — the description is already in the NL vector hash.
  - [ ] 5.8 Unit tests: `IngestionWorkflowDualEmbeddingTests.SourceTypeEvent_SuccessPath_SchedulesFourActivities` + `.SourceTypeEvent_LlmUnavailable_QueuesAndProceedsWithRawEmbedding` + `.SourceTypeFile_SkipsDualEmbeddingBranch` + `.SourceTypeUrl_SkipsDualEmbeddingBranch` + `.NaturalLanguageDescriptionMetadata_PersistedOnlyWhenConfigured`.
  - [ ] 5.9 **Startup gate for replay safety (Winston — promoted from deferred, was runbook-only per Risk #13). Blocked by Spike 0.2 (workflow-instance enumeration API) + Spike 0.4 (hosted-service ordering).** Create `src/Hexalith.Memories.Server/Hosting/WorkflowReplaySafetyHostedService.cs`. On start (via the ordering mechanism resolved by Spike 0.4 — `IStartupFilter`, `IHostedLifecycleService.StartingAsync`, or plain `IHostedService` registration-order — confirm which works): query DAPR for active `IngestionWorkflow` instances with code-version metadata ≠ current build version (use the API resolved by Spike 0.2 — `DaprWorkflowClient.GetInstances(...)` IF it exists, else the HTTP management API, else the state-store query, else **fall back to runbook-only and mark gate as deferred** with a `9175 StartupGateDisabled_NoEnumerationApi` Critical log).
    - Each query call wraps a 10s `CancellationTokenSource` timeout (Improvement Z — per-call fail-open guard); if the call itself times out (sidecar unreachable), log `9173 ReplaySafetyGateSidecarUnreachable` Critical and **skip the gate** (fail open — cannot gate on an unreachable sidecar; stuck pod is worse than missing gate).
    - Polling loop: 5s interval; **per-poll** log `9171 InFlightWorkflowsDrainingAtStartup(count, oldestVersion)` at **Warning** (Improvement X — NOT Critical; normal deploy observability); 5min total timeout; on timeout, log `9172 InFlightWorkflowsDrainTimeout(remainingCount)` at **Critical** (single-shot escalation) and proceed (do not infinite-block — operators prefer deterministic failure over stuck service).
    - Unit tests: `WorkflowReplaySafetyHostedServiceTests.NoMismatchedWorkflows_StartsImmediately` + `.MismatchedWorkflowsPresent_DelaysUntilCleared_Emits9171PerPoll_AtWarning` + `.TimeoutElapsed_Emits9172Critical_ProceedsAnyway` + `.SameVersionActive_DoesNotDelay` + `.SidecarUnreachable_Emits9173_FailsOpen` + `.EnumerationApiUnavailable_Emits9175_FailsOpen`.
    - Register in DI per Spike 0.4's chosen mechanism.

- [ ] Task 6: CorrelationId root-only + self-edge guard in `IndexGraphActivity` (AC: #4, Risk #3, Risk #15)
  - [ ] 6.1 Edit `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:89-101` — wrap the existing `CorrelationId` block: `if (!string.IsNullOrWhiteSpace(input.CorrelationId) && !string.Equals(input.CorrelationId, input.MemoryUnitId, StringComparison.Ordinal)) { ... existing edge-creation code ... } else if (string.Equals(input.CorrelationId, input.MemoryUnitId, StringComparison.Ordinal)) { EventStoreIntegrationLog.CorrelationIdSelfEdgeSkipped(_logger, input.MemoryUnitId); }`.
  - [ ] 6.2 Add `CorrelationIdSelfEdgeSkipped` to `EventStoreIntegrationLog` (9155, Debug level — high-frequency; NOT Warning).
  - [ ] 6.3 Unit tests: `IndexGraphActivityTests.CorrelationId_CreatesRootToCurrentEdge` + `.CorrelationIdEqualsMemoryUnitId_NoSelfEdge_LogsDebug` + `.MultipleEventsSameCorrelationId_EachCreatesEdgeToRoot_NoFanOut` (Tier-2 FalkorDB fixture) + `.NullCorrelationId_SkipsBranch`.
  - [ ] 6.4 Graph-traversal tests: `GraphTraversalServiceTests.CorrelatedWith_OutboundFromRoot_ReturnsAllCorrelatedEvents` + `.CorrelatedWith_InboundFromCorrelatedEvent_ReturnsOnlyRoot`.

- [ ] Task 7: `isStub` gap-marker flag + retroactive resolution telemetry (AC: #3, Risk #4, Risk #12)
  - [ ] 7.1 Edit `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:210-222` — `BuildMergeStubNode`:
    ```csharp
    const string query = "MERGE (m:MemoryUnit {id: $id}) ON CREATE SET m.isStub = true, m.stubCreatedAt = $stubCreatedAt";
    ```
    Use `ON CREATE SET` (not `coalesce`) per Risk #12 mitigation — safer across FalkorDB versions. Pass `$stubCreatedAt` parameter from caller using `context.CurrentUtcDateTime` formatted as ISO-8601 (consistent with the Story 1.5 `ingestedAt` timestamp pattern). The `stubCreatedAt` property enables operator orphan-detection queries (Dev Notes "Orphan stub detection") — `MATCH (m:MemoryUnit) WHERE m.isStub = true AND m.stubCreatedAt < <threshold>` surfaces stubs that should have been resolved by now. Update `BuildMergeStubNode`'s method signature to accept a `DateTimeOffset stubCreatedAt` parameter. **Pre-impl subtask (Improvement AJ):** grep the codebase for all `BuildMergeStubNode` callers before applying the signature change — known sites are (a) `IndexGraphActivity.cs` CausationId stub path and (b) `IndexGraphActivity.cs` CorrelationId-root stub path; verify no additional callers exist (Story 4.3 confidence-promotion or Story 8.2 consistency-verification may reference the builder). Update all callers atomically in one commit to pass `context.CurrentUtcDateTime`; add test `GraphQueryBuilderTests.AllCallers_PassStubCreatedAt` that uses reflection to enumerate callers and assert parameter count.
  - [ ] 7.2 Edit `BuildMergeMemoryUnitNode` (line 77) — append to the existing `SET` clause: `, m.isStub = false`. Also, add `WITH m, m.isStub AS previousIsStub` before the SET so the activity can return `previousIsStub` to detect retroactive resolution. Final shape:
    ```
    MERGE (m:MemoryUnit {id: $id})
    WITH m, coalesce(m.isStub, false) AS previousIsStub
    SET m.caseId = $caseId, ..., m.isStub = false
    RETURN previousIsStub
    ```
    (`coalesce(m.isStub, false)` is safe here because `false` is the correct default for a freshly-MERGEd node that was never a stub.)
  - [ ] 7.3 Edit `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` line 62 — read the query's `RETURN previousIsStub, stubCreatedAt` values; if `previousIsStub == true`, emit `9154 StubNodeResolved(tenantId, memoryUnitId, causingEventId, stubCreatedAt, resolvedAt = context.CurrentUtcDateTime)` at Information level. **Canonical field set** — matches logging section + TL;DR #14. Operators compute retroactive-resolution latency from the log fields directly.
  - [ ] 7.4 Edit `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:98` — upgrade stub detection:
    ```csharp
    // Preferred: explicit isStub flag (Story 9.2+)
    if (record.TryGetValue("isStub", out object? stubFlag)
        && stubFlag is bool stubBool
        && stubBool)
    {
        // Gap marker — definitive
    }
    else if (string.IsNullOrWhiteSpace(content))
    {
        // Fallback: legacy content-absent heuristic for pre-9.2 nodes
    }
    ```
    Keep the fallback — pre-9.2 stub nodes in existing databases lack the flag.
  - [ ] 7.5 Unit tests: `GraphQueryBuilderTests.StubThenReal_FlagPromotesFromTrueToFalse` + `.BuildMergeStubNode_OnExistingNonStub_DoesNotRegressIsStubFlag` + `.BuildMergeStubNode_SetsStubCreatedAtOnCreation` + `.BuildMergeStubNode_OnExistingStub_PreservesOriginalStubCreatedAt` + `.BuildMergeMemoryUnitNode_NewNode_SetsIsStubFalse` + `.BuildMergeMemoryUnitNode_PromotesStub_ReturnsPreviousIsStubTrue` + `GraphTraversalServiceTests.ExplicitIsStubTrue_IdentifiesGapMarker` + `.ExplicitIsStubFalse_IncludedInTraversal` + `.ContentAbsentHeuristicFallback_ForLegacyNodes` + `.OrphanStubQuery_ReturnsStubsOlderThanThreshold` + `IndexGraphActivityTests.StubResolved_Emits9154`.
  - [ ] 7.6 **`isStub` backfill migration (Improvement U — promoted from deferred per CTO concession).** Create `src/Hexalith.Memories.Server/Migrations/IsStubBackfillMigration.cs` implementing `IDeploymentMigration`. One-shot Cypher: `MATCH (m:MemoryUnit) WHERE m.isStub IS NULL AND <any content property> IS NOT NULL SET m.isStub = false RETURN count(m) AS backfilled`. Gated to run ONCE per database (migration registry — if `Hexalith.Memories` has one; else use FalkorDB node label `(:SchemaMigration {id: "9.2-isStub-backfill"})` as the gate). Running the migration retires the content-absent fallback heuristic in `GraphTraversalService.cs:98` as a post-migration spec cleanup; until migration has run in a given database, the fallback must be kept. Unit test: `IsStubBackfillMigrationTests.UnflaggedNodeWithContent_SetsIsStubFalse` + `.AlreadyFlaggedNode_NotTouched` + `.MigrationRunTwice_SecondRunIsNoOp`. Tracked in retrospective: "Remove content-absent fallback in `GraphTraversalService` after all production databases run the backfill migration" — move to follow-up story.

- [ ] Task 8: `FailedNaturalLanguageEmbeddingRegistry` + retry hosted service + retry workflow (AC: #5, #6, Risk #9)
  - [ ] 8.1 **Blocked by Spike 0.1 (payload durability verification).** Create `src/Hexalith.Memories.Contracts/V1/FailedNaturalLanguageEmbeddingRecord.cs` — shape depends on Spike 0.1 outcome:
    - **If payload is durably stored (preferred):** sealed record `(string TenantId, string MemoryUnitId, long QueuedAtTicks, int Attempts)` — payload-by-reference; re-read via `IMemoryUnitContentReader` at retry time. ~100 bytes per entry.
    - **If payload is NOT durably stored (fallback):** sealed record `(string TenantId, string MemoryUnitId, string TruncatedRawJsonPayload, long QueuedAtTicks, int Attempts)` — bounded payload-by-value with `TruncatedRawJsonPayload` capped at `NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes` (default `4096`). Still protects pre-mortem Failure δ via `bounded_count × bounded_bytes`. Gauge `memories_natural_language_embedding_queue_bytes` reinstated in this mode.
    Either way, register in `MemoriesJsonContext`.
  - [ ] 8.2 Create `src/Hexalith.Memories.Server/NaturalLanguage/IFailedNaturalLanguageEmbeddingRegistry.cs` + `FailedNaturalLanguageEmbeddingRegistry.cs`. Methods (payload-by-reference — Task 8.1 rationale):
    - `EnqueueAsync(string tenantId, string memoryUnitId, long queuedAtTicks, CancellationToken ct)` — `ZADD nl-embedding-retry:{tenantId} {queuedAtTicks} {json(record)}` (record carries ONLY ids + attempts, NOT payload — ~100 bytes per entry, not 4KB+).
    - `DequeueBatchAsync(string tenantId, int batchSize, CancellationToken ct)` — `ZRANGE ... LIMIT 0 {batch}` + deserialize. Returns records with ids only; caller re-reads payloads via `IMemoryUnitReader`.
    - `CompleteAsync(string tenantId, FailedNaturalLanguageEmbeddingRecord record, CancellationToken ct)` — `ZREM`.
    - `IncrementAttemptsAsync(string tenantId, FailedNaturalLanguageEmbeddingRecord record, CancellationToken ct)` — `ZADD XX` with incremented score; OR, if `Attempts+1 >= MaxRetryAttempts`, move to `nl-embedding-retry-dead:{tenantId}` atomically (`MULTI`/`EXEC` pair).
    - `GetBacklogCountAsync(string tenantId, CancellationToken ct)` — `ZCARD`.
    - `GetBacklogBytesAsync(string tenantId, CancellationToken ct)` — `MEMORY USAGE nl-embedding-retry:{tenantId}` — **REQUIRED only if Spike 0.1 selects the bounded-payload-by-value fallback**. With payload-by-reference, size is bounded by construction (~100 bytes × count); gauge adds noise. Omit the method in the reference case; include in the fallback case.
    - `ListTenantsWithBacklogAsync(CancellationToken ct)` — `SCAN MATCH nl-embedding-retry:*` with natural pagination. No artificial 10k-tenant cap (cap was ceremonial — `SCAN` iterates naturally; a deployment with 10k+ tenants is not a warning condition).
  - [ ] 8.3 Create `src/Hexalith.Memories.Contracts/V1/NaturalLanguageEmbeddingRetryInput.cs` + `NaturalLanguageEmbeddingRetryResult.cs`. Register in `MemoriesJsonContext`.
  - [ ] 8.4 Create `src/Hexalith.Memories.Server/Workflows/NaturalLanguageEmbeddingRetryWorkflow.cs` — minimal orchestration. **Step 0 (NEW — payload-by-reference):** call a new `ReadMemoryUnitRawPayloadActivity(tenantId, memoryUnitId) → string` that reads the raw JSON payload from the memory unit's syntactic index hash. If the memory unit has been deleted (tenant purge, manual deletion), the activity returns `null` → the workflow removes the retry entry and logs at Debug level (no named event ID — edge-case-inside-edge-case; Occam cut from 9181). Step 1: `GenerateNaturalLanguageDescriptionActivity` with the re-read payload. Step 2: `GenerateEmbeddingActivity` with the LLM description. Step 3: **Final existence check (Improvement AC — chaos Scenario E):** activity `CheckMemoryUnitHashExistsActivity(tenantId, memoryUnitId)` — if `{tenant}:vec:{memoryUnitId}` no longer exists (tenant purge race between retry start and finish), abandon: log at Debug, remove retry entry, return `(Indexed: false, Reason: "memory-unit-deleted-during-retry")`. Step 4: `IndexNaturalLanguageSemanticActivity`. On overall success, returns `(Indexed: true)`. On `NaturalLanguageDescriptionUnavailableException`, returns `(Indexed: false, Reason: "llm-still-unavailable")` — the hosted service examines the result to decide retry-vs-dead-letter.
  - [ ] 8.5 Create `src/Hexalith.Memories.Server/NaturalLanguage/NaturalLanguageEmbeddingRetryHostedService.cs` — `BackgroundService` (not `IHostedService` directly) with `ExecuteAsync`:
    - Loop: `while (!stoppingToken.IsCancellationRequested)`: wait `options.RetryIntervalSeconds`; call `registry.ListTenantsWithBacklogAsync`; for each tenant, `DequeueBatchAsync(batchSize)`; for each record, `daprWorkflowClient.ScheduleNewWorkflowAsync(nameof(NaturalLanguageEmbeddingRetryWorkflow), recordAsInput, instanceId: $"retry-nl-{record.MemoryUnitId}")` (instance id = memoryUnitId guarantees DAPR Workflow's instance-level dedup prevents double-scheduling under hosted-service restart).
    - **Retry backpressure (promoted from deferred — pre-mortem Failure ζ) with max-skip budget (Improvement AA — chaos Scenario B):** before dequeuing, check `EmbeddingRateLimiterActor` current budget for the tenant; if budget utilization > 80% for live traffic, skip this tenant's retry batch this tick and log at Debug. **Never skip more than 10 consecutive ticks** for the same tenant — on the 11th consecutive skip, bypass backpressure once and dequeue a minimum batch (1 record) to prevent queue starvation. Emit `9174 RetryBackpressureOverride(tenantId, skippedTicks=10)` at Debug when bypass fires. Counter reset on any successful dequeue.
    - Monitor backlog: every tick, emit `memories_natural_language_embedding_queue_depth{tenant}` gauge + `memories_natural_language_embedding_queue_bytes{tenant}` gauge (from `GetBacklogBytesAsync`). If any tenant's backlog > 100 → `9170 NaturalLanguageEmbeddingRetryQueueBacklog` Warning; > 1000 → Error + exponential backoff on next interval (multiply `RetryIntervalSeconds` by 5 until backlog recovers).
    - Register in `AddMemoriesServer()` via `services.AddHostedService<NaturalLanguageEmbeddingRetryHostedService>();`.
  - [ ] 8.7 **Rate-limiter sizing check (Winston — promoted from deferred, Risk #6):** `RateLimiterSizingValidator` emits `9163 RateLimiterUnderSizedForEvents(tenantId, currentCeiling, recommendedCeiling)` at Warning when a tenant's `EmbeddingRateLimiterActor` ceiling is below `sustainedUsage * 2` for a **sustained sliding window** (default 15 min, configurable via `NaturalLanguageDescriptionOptions.RateLimiterSizingWindowSeconds`). **NOT fired on first-event-ingest burst** (Improvement AB — chaos Scenario C: transient bursts during first-event-ingest cause false-positive pages; sustained-window avoids that). Evaluated periodically by the retry hosted service (reuse its scheduling slot — no new background timer). The warning does NOT auto-scale the ceiling (operator policy — some tenants have hard provider quotas). Unit test: `RateLimiterSizingValidatorTests.SustainedUnderSizing_Emits9163` + `.TransientBurst_DoesNotEmit` + `.CeilingSufficient_DoesNotEmit`.
  - [ ] 8.8 **Dead-letter CLI surface (Improvement F):** add `memories retry-nl-embeddings --tenant X [--dead] [--dry-run]` sub-command to the existing `Hexalith.Memories.Cli` project (or wherever operator CLI lives — verify at impl time). With `--dead`, re-enqueues all records from `nl-embedding-retry-dead:{tenant}` back to `nl-embedding-retry:{tenant}` (resets attempt count to 0). Without `--dead`, lists backlog counts only. With `--dry-run`, prints actions without executing. Integration test: `RetryNlEmbeddingsCliTests.DeadLetterReEnqueue_ResetsAttemptCount`. If the CLI project doesn't exist yet, document explicitly in `deferred-work.md` as "dead-letter CLI deferred to follow-up" with Redis Sorted Set inspection commands as the interim interface.
  - [ ] 8.6 Unit tests: `FailedNaturalLanguageEmbeddingRegistryTests.EnqueueDequeueRoundTrip_StoresIdsOnly_NotPayload` (verifies payload-by-reference design — asserts the serialized record size is < 300 bytes even for memory units with multi-KB raw payloads) + `.IncrementAttempts_MovesToDeadLetter_AtMaxAttempts` + `.BacklogCount_ReturnsCurrentSize` + `.BacklogBytes_ReturnsSortedSetMemoryUsage` + `NaturalLanguageEmbeddingRetryHostedServiceTests.BacklogExceedsWarningThreshold_EmitsLog` + `.BacklogExceedsErrorThreshold_BacksOffInterval` + `.StubFailedRetry_IncrementsAttemptsAndReQueues` + `.MultipleTenantsWithBacklog_FairlyDequeuesAcrossTenants` + `.LiveIngestionSurge_BackpressuresRetryDequeue` (pre-mortem Failure ζ — submits a live-traffic burst that drives `EmbeddingRateLimiterActor` utilization > 80%; asserts the hosted service skips retry dequeue for that tick) (round-robin or weighted-fair scheduling — seeds 100 tenants where tenant#1 has 1000 records and tenants 2-100 each have 10; asserts that tenants 2-100 are not starved by tenant#1's backlog over 3 retry intervals; failure mode would be naive serial iteration that drains tenant#1 first) + `.RestartMidIteration_DoesNotDoubleScheduleSameRecord` (simulates hosted-service crash between `DequeueBatchAsync` and the workflow scheduling call; on restart, asserts the record is re-dequeued and `NaturalLanguageEmbeddingRetryWorkflow.Idempotency_DuplicateScheduling_DoesNotDoubleIndex` proves the retry workflow itself is idempotent — uses the memoryUnitId as the workflow instance ID so DAPR Workflow's "already running" guard prevents duplicates) + `NaturalLanguageEmbeddingRetryWorkflowTests.SuccessPath_ReturnsIndexedTrue` + `.LlmStillUnavailable_ReturnsIndexedFalse` + `.Idempotency_DuplicateScheduling_DoesNotDoubleIndex` (schedules the same workflow instance ID twice; asserts only ONE `IndexNaturalLanguageSemanticActivity` call lands; second schedule attempt either no-ops via DAPR Workflow's instance-ID dedup OR completes immediately with the prior result — verify which behavior is documented in DAPR Workflow runtime).

- [ ] Task 9: Integration tests (AC: #14, #15, #16)
  - [ ] 9.1 Create `tests/Hexalith.Memories.IntegrationTests/DualEmbeddingRoundTripTests.cs` — Tier 2/3. Publishes a test CloudEvent via `DaprClient.PublishEventAsync` with a realistic JSON payload; polls BOTH `{tenant}:vec:{memoryUnitId}` AND `{tenant}:vec:nl:{memoryUnitId}` until present or budget exhausted (7s). Asserts: (a) raw embedding hash exists with correct `embedding` bytes + `memoryUnitId` + `caseId`; (b) NL embedding hash exists with `embedding` bytes + `naturalLanguageDescription` non-empty + `descriptionOrigin = "ai"`; (c) FalkorDB node has `isStub = false`; (d) `IngestionResult.NaturalLanguageEmbeddingStatus == Indexed`.
  - [ ] 9.2 Create `tests/Hexalith.Memories.IntegrationTests/OutOfOrderEventTests.cs` — Tier 2/3. Publishes event B with `causationid = A_id` BEFORE event A. Polls FalkorDB for the stub node (asserts `isStub = true`, content absent, `caused_by` edge B→A present). Publishes event A. Polls for stub promotion (asserts `isStub = false`, content populated, edge preserved, `9154` log emitted).
  - [ ] 9.3 Create `tests/Hexalith.Memories.IntegrationTests/DegradedNaturalLanguageEmbeddingTests.cs` — Tier 2. Three scenarios in one fixture:
    - **Scenario A — LLM transient failure:** Stubbed `DaprConversationClient` (NSubstitute) throws `DaprException` on first call, succeeds on second. Publishes one event. Asserts: (a) first ingestion returns `IngestionResult.NaturalLanguageEmbeddingStatus == Queued`; (b) raw embedding hash EXISTS; (c) NL embedding hash DOES NOT exist yet; (d) after retry interval fires, NL embedding hash appears; (e) `9152` (queue) + `9153` (success) logs emitted.
    - **Scenario B — Index-side partial failure (NEW):** Stubbed `DaprConversationClient` succeeds; `IndexNaturalLanguageSemanticActivity` is configured to throw `RedisConnectionException` on first attempt + succeed on retry. Publishes one event. Asserts: (a) workflow-level retry catches the index failure (not the queue path — the LLM succeeded); (b) on second activity attempt, NL hash is written; (c) NO entry in the retry queue (the queue is for LLM failures only, not index failures); (d) `CleanupSemanticActivity` compensation is NOT invoked because the retry succeeded; (e) end-state: BOTH semantic hashes present, `IngestionResult.NaturalLanguageEmbeddingStatus == Indexed`.
    - **Scenario C — Index-side terminal failure with compensation (NEW):** Stubbed `DaprConversationClient` succeeds; `IndexNaturalLanguageSemanticActivity` is configured to throw `RedisConnectionException` on ALL attempts (exhaust retries). Publishes one event. Asserts: (a) workflow enters compensation; (b) `CleanupSemanticActivity` deletes BOTH the raw hash AND the NL hash (Task 4.7 dual-cleanup verification — proves the comment "cleanup semantic compensation handles both" from Task 5.5 is correct in practice, not just in spec); (c) memory unit ends in failed state, NOT a half-indexed orphan; (d) `IngestionResult` reflects the failure path.
  - [ ] 9.4 Create `tests/Hexalith.Memories.IntegrationTests/CorrelationRootEdgeTests.cs` — Tier 2 FalkorDB fixture. Publishes one root event (cloudevent.id = `correlation-root-1`, correlationid = `correlation-root-1`) + three correlated events (cloudevent.id = `corr-{1,2,3}`, correlationid = `correlation-root-1`). Asserts: (a) root node has NO self-edge; (b) each of the three correlated events has exactly one `correlated_with` edge pointing FROM root TO itself; (c) there are NO edges between the three correlated events; (d) `9155` log emitted once for the root event.
  - [ ] 9.5 Create `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowReplaySafetyTests.cs` — asserts workflow replay of a 9.1-shape history (where only raw embedding was in the fan-out) does NOT break when 9.2's new block is introduced in the code path. Simulates the in-flight scenario by seeding a durable-task state snapshot from 9.1 and replaying under 9.2 code. Accepts the documented runbook (quiesce-before-deploy); this test verifies the failure mode is DETERMINISTIC (not silent corruption).
  - [ ] 9.6 Edit `tests/Hexalith.Memories.IntegrationTests/ConsistencyVerificationTests.cs` (Story 8.2) — add test cases: `EventMemoryUnit_WithIndexedNaturalLanguageEmbedding_AllAxesConsistent` + `EventMemoryUnit_WithQueuedNaturalLanguageEmbedding_ReportsInformationalNote`.

- [ ] Task 10: Documentation + sprint-status + retro (AC: #17)
  - [ ] 10.1 Edit `docs/dev/eventstore-integration.md` (created by Story 9.1 Task 7.1). Append new level-2 sections in the following order:
    - [ ] 10.1.1 **"Dual embedding pipeline"** — why NL, prompt template verbatim, expected latency envelope, performance impact of 2× embedding API calls.
    - [ ] 10.1.2 **"LLM provider swap procedure"** — step-by-step swap from `conversation.openai` → `conversation.anthropic` via YAML only; include the exact component YAML change + secrets step + restart expectation.
    - [ ] 10.1.3 **"Natural language embedding retry queue"** — inspection, dead-letter flow, alert rules. **Include EXACT one-line commands, not paragraphs (Improvement Y — on-call lead concession):** `redis-cli ZCARD nl-embedding-retry:{tenant}` (backlog count), `redis-cli ZRANGE nl-embedding-retry:{tenant} 0 9 WITHSCORES` (oldest 10 entries with queue timestamps), `redis-cli ZCARD nl-embedding-retry-dead:{tenant}` (dead-letter count), `memories retry-nl-embeddings --tenant X` (force next retry tick), `memories retry-nl-embeddings --tenant X --dead --reenqueue` (rescue dead-letters), `memories retry-nl-embeddings --tenant X --dry-run` (preview without mutation). Recommended Prometheus alert rules: `rate(memories_natural_language_embedding_queue_depth[5m]) > 10` (backlog growing > 10/min sustained for 15 min); `memories_natural_language_embedding_queue_depth > 1000` (dead-letter candidate escalation).
    - [ ] 10.1.4 **"Gap markers and retroactive resolution"** — `isStub` flag semantics, `9154` log, traversal behavior with examples, before/after graph diagrams.
    - [ ] 10.1.5 **"Correlation root semantics"** — edge direction, self-loop avoidance (Risk #3), multi-event worked example, traversal direction matrix.
    - [ ] 10.1.6 **"Deployment — quiescing event ingestion"** (Risk #13 runbook) — 2-minute quiesce window, how to verify workflow backlog is zero, rollback procedure.
    - [ ] 10.1.7 **"Local dev — `conversation.echo`"** — limitations, degenerate-case behavior, how to test with a real LLM locally (env var pointing to an OpenAI key, swap component).
    - [ ] 10.1.8 **"Known limitations"** — NO per-tenant LLM, NO streaming, NO retroactive backfill, NL description may hallucinate (Risk #8 posture), response cache is cross-tenant at the sidecar level.
    - [ ] 10.1.9 **"LLM hallucination posture"** — the `Ai`-origin tagging, user correction via Story 3.6 annotations, the `confidence` UX signal, what to do when NL descriptions are systematically wrong.
    - [ ] 10.1.10 **"PII scrubbing posture" (Improvement W — security lead concession).** 9.2 ships with DAPR Conversation `piiScrubbing: false` (architecture L882 MVP decision). The NL description MAY contain PII that appeared in the raw event payload; the description is indexed into a per-tenant vector store (no cross-tenant leakage, but within-tenant PII propagation is possible). **Require an explicit sign-off artifact** `docs/governance/PII_ACKNOWLEDGMENT.md` (or equivalent in the product's governance folder) signed off by Product Owner + Legal/Compliance stakeholder BEFORE 9.2 deploys to any tenant with PII-subject workloads. The acknowledgment records: (a) known behavior (NL descriptions may echo payload PII), (b) per-tenant opt-in to enable `piiScrubbing: true` via component metadata as a future operator action, (c) documented test `GenerateNaturalLanguageDescriptionActivityTests.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior` serving as a visible control. **A test is not consent**; the document is. Flag in sprint-status.yaml as a gating artifact, not a coding task.
  - [ ] 10.2 Edit `_bmad-output/implementation-artifacts/sprint-status.yaml`: `9-2-dual-embedding-and-causal-chain-indexing` transition `backlog` → `ready-for-dev`. Update `last_updated`.
  - [ ] 10.3 Update the worked example in `docs/dev/eventstore-integration.md`'s existing Story 9.1 section to show the DUAL-embedding output for the `CounterIncrementedV1` event (now: one FalkorDB node with `caused_by`, TWO Redis Vector hashes, one NL description tagged `origin=ai`).
  - [ ] 10.4 If any Risk #1-#15 guard tests are skipped or deferred, add entries to `_bmad-output/implementation-artifacts/deferred-work.md` with rationale.
  - [ ] 10.5 Create `docs/dev/natural-language-embedding.md` as a focused deep-dive doc (optional companion to the integration guide) covering: cleaner regex rationale, LLM confidence extraction strategy, payload truncation heuristic, alternative prompts evaluated + why the current one won, tuning guide for `MaxPayloadChars`. Link from the main integration doc.

## Decisions

ADRs for Story 9.2. Move to `_bmad-output/planning-artifacts/architecture-decisions/9-2-*.md` post-dev.

### ADR 9.2-A — NL description generation strategy

**Options considered:**
- (α) Synchronous LLM call inside `IngestionWorkflow` (blocks ingestion on LLM latency).
- (β) Asynchronous enrichment via a post-ingest `AiEnrichmentWorkflow` child workflow (LLM latency off the ingestion critical path; requires double-write or MERGE pattern).
- (γ) Deferred retry-queue only — ingest with raw embedding immediately, ALWAYS generate NL description asynchronously.

**Trade-offs:** (α) simplest; NL description is available at first search; LLM latency adds 1-3s to p95 (Risk #2). (β) complex state-machine coordination; double-write risk if workflow fails post-index. (γ) cleanest; but the "NL embedding gap" window is always present, even on healthy LLM, which degrades search quality unpredictably.

**Decision:** (α) with degraded fallback to (γ) on LLM failure. Synchronous happy path + automatic fallback to queue when LLM is unavailable.

**Rationale:** NFR6 is relaxed to 7s for `SourceType.Event` (documented in integration doc). LLM availability under normal conditions is > 99%, so the 1-3s extra latency applies to > 99% of events. The queue handles the < 1% degraded case. (β)'s coordination overhead buys nothing when (α)'s degraded path already provides the same guarantee.

### ADR 9.2-B — Second Redis Vector index vs single index with multi-vector

**Options:**
- (a) Two separate indexes (`:memories:vec` + `:memories:vec:nl`) with disjoint key prefixes.
- (b) One index with two vector fields (`embedding` + `embeddingNl`) on the same hash.
- (c) One index with a `variant` tag field to discriminate.

**Trade-offs:** (a) simple; per-activity telemetry; clear cleanup; double the FT.CREATE overhead at tenant provisioning. (b) tight coupling — Story 2.2 `SemanticSearchService` queries ONE vector field; supporting both means edit + regression risk on search code. (c) conceptually clean; requires variant-aware query construction in every search path.

**Decision:** (a).

**Rationale:** additive to Story 2.2 without touching search. `NaturalLanguageSemanticSearchService` is a library class that consumers can opt into independently. Zero regression on the existing semantic-search axis.

### ADR 9.2-C — CorrelationId edge direction

**Options:**
- (α) `current → root` (this event correlates with root).
- (β) `root → current` (root groups all correlated events).
- (γ) Bidirectional.

**Trade-offs:** (α) matches developer intuition when reading "this event is correlated with that one." (β) optimizes "list everything that correlates with the root" (OUT-traversal from root) — the primary causal-chain query pattern. (γ) double storage + traversal confusion.

**Decision:** (β).

**Rationale:** the existing `IndexGraphActivity:89-100` code already writes `root → current`. Preserving direction avoids a data-migration workflow for 9.1-ingested events. The primary query pattern is "walk from root outbound to see all correlated events", which (β) serves naturally. Documented explicitly (Risk #3) so developer intuition mismatch doesn't cause confusion.

### ADR 9.2-D — Gap marker representation

**Options:**
- (a) Explicit boolean `isStub` property.
- (b) Label-based: stub nodes carry a `MemoryUnit:Stub` multi-label.
- (c) Keep the "content absent" heuristic.

**Trade-offs:** (a) explicit + queryable + backward-compatible via fallback. (b) cleaner semantically; FalkorDB multi-label support as of NFalkorDB 1.0.0 — unverified reliability. (c) fragile — Risk #4.

**Decision:** (a).

**Rationale:** cleanest + portable across graph providers. Label operations in OpenCypher (`REMOVE m:Stub`) are well-specified but FalkorDB-specific support is not a hard MVP guarantee.

### ADR 9.2-E — LLM retry policy shape

**Options:**
- (a) Short workflow-level retry (`maxAttempts = 2`, backoff 3s-30s).
- (b) Long workflow-level retry (`maxAttempts = 10`, backoff up to 30min).
- (c) No workflow-level retry; always fail-fast to the queue.

**Trade-offs:** (a) recovers from transient sidecar hiccups without blocking the workflow; falls through to queue for longer outages. (b) blocks `IngestionWorkflow` for up to 30min — unacceptable for search freshness. (c) loses the happy-path transient recovery.

**Decision:** (a).

**Rationale:** ~30s max workflow-level retry budget is enough for a DAPR sidecar restart or an LLM provider 503 blip. Longer outages go to the queue where they don't block the main ingestion path or consume workflow slots.

### ADR 9.2-F — NL description persistence in metadata

**Options:**
- (a) Always persist to `metadata["event.naturalLanguageDescription"]`.
- (b) Never persist to metadata — NL is only in the Redis Vector hash.
- (c) Config-driven.

**Trade-offs:** (a) searchable via FT.SEARCH on syntactic index; bloats metadata by ~500 chars × events. (b) cleanest; requires operators to query the NL vector hash for inspection. (c) flexibility at the cost of one option.

**Decision:** (c) default `false`.

**Rationale:** most tenants never need the description in syntactic search. Operators with FT.SEARCH-heavy inspection workflows can enable persistence. Default `false` favors storage economy.

## Dev Notes

### HARD GATE: Story 9.1 must be `done` before starting 9.2

- **Story 9.1 — Event Auto-Discovery & DAPR Pub/Sub Subscription:** ships the `Hexalith.Memories.EventStore` NuGet package, `CloudEventToIngestionInputMapper`, `TenantEventRouter`, the `MetadataOrigin.System` enum value, and the `SourceType.Event` ingestion path. Story 9.2 cannot run unit tests that depend on these types until 9.1 lands.
- **Story 1.5 — Three-Backend Indexing:** `IndexGraphActivity` + `IndexSemanticActivity` already ship the `caused_by`/`correlated_with` edge creation + raw semantic index. 9.2 edits these activities.
- **Story 5.1 + 5.2 — Tenant Provisioning + Deletion:** `ProvisionRedisVectorActivity` + `DeleteRedisVectorIndexActivity` must be editable for the NL index addition.
- **Story 6.3 — Retry/Failure Visibility:** `FailedUnitsRegistry` is the shape precedent for `FailedNaturalLanguageEmbeddingRegistry` (same Redis Sorted Set pattern).
- **Story 8.2 — Consistency Verification:** `ConsistencyVerificationWorkflow` is extended to understand the NL semantic axis.

### CloudEvents → dual embedding mapping (canonical reference)

```csharp
// Existing (from 9.1): CloudEvent<JsonElement> → IngestionInput (unchanged)
var input = new IngestionInput { SourceType = SourceType.Event, ContentBytes = rawJsonBytes, ... };

// NEW (9.2): IngestionWorkflow branches for SourceType.Event after embedding
if (input.SourceType == SourceType.Event)
{
    // Step 1: LLM generates NL description
    var nlInput = new NaturalLanguageDescriptionInput(
        TenantId: input.TenantId,
        MemoryUnitId: memoryUnitId,
        RawJsonPayload: Encoding.UTF8.GetString(input.ContentBytes ?? []),
        EventType: input.Metadata["cloudevent.type"].Value,
        AggregateType: input.Metadata.TryGetValue("event.aggregateType", out var at) ? at.Value : null
    );
    var nlResult = await context.CallActivityAsync<NaturalLanguageDescriptionResult>(
        nameof(GenerateNaturalLanguageDescriptionActivity), nlInput);

    // Step 2: Second embedding via SAME GenerateEmbeddingActivity (different ContentKind)
    // NOTE: positional constructor is MANDATORY — Risk #17 forbids property-init syntax
    // because EmbeddingInput's JSON wire shape is load-bearing for paused workflow replay.
    var nlEmbedding = await context.CallActivityAsync<EmbeddingResult>(
        nameof(GenerateEmbeddingActivity),
        new EmbeddingInput(
            input.TenantId,
            nlResult.Description,
            EmbeddingContentKind.NaturalLanguageDescription));

    // Step 3: Fan-out gains a fourth parallel task
    var nlIndexInput = new NaturalLanguageIndexInput { ... raw fields from existing IndexInput ...,
        NaturalLanguageDescription = nlResult.Description,
        DescriptionConfidence = nlResult.EstimatedConfidence };
    var nlTask = context.CallActivityAsync<IndexResult>(
        nameof(IndexNaturalLanguageSemanticActivity), nlIndexInput);
    // ... combined with existing syntacticTask, semanticTask, graphTask in Task.WhenAll
}
```

### LLM confidence extraction strategy

`DaprConversationClient.ConverseAsync` returns a `ConversationResult` with a `Result[0].Choices[0]` shape. Depending on the provider:
- **OpenAI** (via `conversation.openai`): when `logprobs=true`, the response includes `logprobs.content[].logprob` per token. Compute `EstimatedConfidence = exp(avg(logprob))` — typical value 0.7-0.95 for deterministic summaries. Set `ConfidenceSource = Logprobs`.
- **Anthropic** (via `conversation.anthropic`): Claude does NOT expose logprobs. Set `EstimatedConfidence = null` AND `ConfidenceSource = Constant`. The UI renders this distinctly (e.g., "AI-inferred (estimate unavailable)") — it does NOT show a numeric.
- **`conversation.echo` (dev)**: no logprobs. `EstimatedConfidence = null`, `ConfidenceSource = Constant`.

**Why nullable + enum (Dr. Quinn + Freya consensus, refined via Occam):** any constant indistinguishable from a real measurement weaponizes the UX signal. Structural null + source-enum eliminates the question: "measured" is structurally distinct from "unmeasured". Operators and UI cannot accidentally render a default as a measurement.

Log attribute `confidenceSource=logprobs|constant|unknown` emitted on `9150` so operators can distinguish. The `ConfidenceSource` enum is propagated all the way to the NL index hash field `descriptionConfidenceSource` for post-hoc inspection.

### LLM retry policy (Task 2.7 rationale)

Standard activity retry:
```csharp
new ActivityRetryPolicy(maxNumberOfAttempts: 2, firstRetryInterval: TimeSpan.FromSeconds(3), backoffCoefficient: 3.0, maxRetryInterval: TimeSpan.FromSeconds(30))
```
Gives ~3s + 9s = 12s total retry budget inside the workflow. DAPR sidecar restart takes ~1-3s; an LLM provider transient 503 is usually ≤5s; both covered. Anything longer → fall through to the queue + retry via `NaturalLanguageEmbeddingRetryHostedService`. Keeping the workflow-level retry SHORT avoids workflow-slot starvation during extended LLM outages (hundreds of events backed up in the workflow-retry slot would exhaust DAPR Workflow worker threads).

### Test doubles for DAPR Conversation

Three patterns depending on test tier:
1. **Tier 1 (unit):** `DaprConversationClient` is mocked via NSubstitute — returns a deterministic `ConversationResult` per test case.
2. **Tier 2 (integration, Aspire fixture):** use the `conversation.echo` DAPR component. Asserts the echo-returned description == raw payload text. Use this tier to exercise the full pipeline deterministically.
3. **Tier 3 (optional, real LLM):** wire a real `conversation.openai` with a test API key env var. SKIP when the env var is unset. Runs in dedicated CI stage — NOT on every PR.

### Prompt engineering — the chosen prompt

**System:** `"You are an event summarizer. Given a JSON event payload of type {EventType}, write a single natural-language sentence (≤40 words) describing what business action occurred. Do NOT repeat field names. Focus on domain meaning. Return ONLY the sentence, no preamble or JSON."`

**User:** `"Event type: {EventType}\nAggregate: {AggregateType ?? \"(unspecified)\"}\nPayload:\n{truncatedJson}"`

**Rejected alternatives:**
- Including a few-shot example: rejected — adds latency and risks example leakage into unrelated domains.
- Asking for JSON output with `summary` + `tags` fields: rejected — complicates cleaner + adds hallucination surface.
- Temperature = 0.0: rejected — deterministic but produces stilted summaries; 0.1 is enough to avoid template-rote output.
- MaxTokens = 150: rejected — longer descriptions encode less per-token meaning; 80 tokens ≈ 40 words hits the embedding sweet spot.

### Provider-swap deployment procedure

1. Prepare new DAPR component YAML (e.g., `conversation-llm-anthropic.yaml`) with component name `"llm"` (same as existing — DAPR allows only one component per name).
2. Store provider API key in the DAPR Secrets component (`secretstore`).
3. Validate in staging: `dapr components -k` lists the new component; publish a test event; verify `9150 NaturalLanguageDescriptionGenerated` log with the new provider name in the `llm.provider` attribute.
4. Deploy: the `llm` component is swapped atomically; no code change, no Memories Server restart needed (DAPR reloads components on component-file change). Documented in integration doc.
5. Monitor: `memories_natural_language_description_duration_ms` histogram should stabilize within ~5 minutes after the swap (different provider latency curve).

### Per-tenant LLM provider — why deferred to Phase 2

A per-tenant LLM would require:
- Extending `TenantConfigurationActor` to carry an `LlmProvider` field (DAPR component name per tenant).
- A `ConversationClientFactory` (referenced in architecture L1254 as future work) that resolves the component by name at call time.
- Distinct DAPR components per provider in production (e.g., `llm-openai`, `llm-anthropic`, `llm-google`) — and per-tenant routing logic.

For MVP, system-wide component is sufficient: the LLM choice is a deploy-time decision, not a per-tenant policy. Deferred per architecture L567 and PRD §534 "zero-code for any DAPR source" scope.

### FalkorDB `ON CREATE SET` vs `coalesce`

Per NFalkorDB 1.0.0 docs (verified — tested in Story 1.5 integration):
- `MERGE (m:MemoryUnit {id: $id}) ON CREATE SET m.isStub = true` — atomic; only sets on node creation; safe.
- `MERGE (m:MemoryUnit {id: $id}) SET m.isStub = coalesce(m.isStub, true)` — also works, but `coalesce(null, true)` returns `true` and `coalesce(true, true)` returns `true`, `coalesce(false, true)` returns `false` — the intended semantics.

Both are correct. `ON CREATE SET` is more idiomatic Cypher and matches Story 1.5 patterns — use it.

### NL description determinism — guarantee boundary (Improvement AK)

The NL description for a given `memoryUnitId` is computed **once per execution lifecycle** — DAPR Workflow checkpoints the `GenerateNaturalLanguageDescriptionActivity` output on first call; subsequent replays return the cached value without re-invoking the LLM. This makes the description deterministic **within a workflow execution**, even though the LLM itself is non-deterministic.

**The guarantee does NOT extend across workflow state loss.** If operators:
- Run `PurgeInstanceMetadataAsync` against a specific memoryUnitId,
- Restore DAPR's state store from a backup older than the NL description,
- Deploy a tenant migration that re-triggers ingestion for existing memory units,
…then a subsequent re-execution will call the LLM AGAIN and may produce a DIFFERENT description. This is acceptable (the embedding vector shifts slightly; search results may rank differently) but operators should be aware. Document this in `docs/dev/eventstore-integration.md` under "NL description determinism".

### Stub resolution telemetry — query extension

The `BuildMergeMemoryUnitNode` query:
```
MERGE (m:MemoryUnit {id: $id})
WITH m, coalesce(m.isStub, false) AS previousIsStub, coalesce(m.stubCreatedAt, null) AS stubCreatedAt
SET m.caseId = $caseId, ..., m.isStub = false
RETURN previousIsStub, stubCreatedAt
```
The activity reads `previousIsStub` + `stubCreatedAt` from the result set — a single row. If `previousIsStub == true`, emit `9154 StubNodeResolved(memoryUnitId, tenantId, causingEventId, stubCreatedAt, resolvedAt = context.CurrentUtcDateTime)` — operators can compute retroactive-resolution latency directly from the log fields. The MERGE is still a single transaction — no read-modify-write race. Note `stubCreatedAt` is preserved on resolution (NOT cleared) so the historical timestamp survives for post-mortem analysis; operators who care about "current stubs only" filter by `WHERE m.isStub = true`.

### Orphan stub detection (operator query)

Stubs that are never resolved (the originating event was lost, the publisher disappeared, etc.) accumulate as graph debt. The `stubCreatedAt` timestamp added in Task 7.1 enables a periodic operator query:

```
MATCH (m:MemoryUnit)
WHERE m.isStub = true
  AND m.stubCreatedAt < (datetime() - duration('PT24H'))
RETURN m.id AS memoryUnitId, m.stubCreatedAt AS createdAt
ORDER BY m.stubCreatedAt ASC
LIMIT 100
```

Recommended cadence: hourly via the operator CLI (`memories graph orphan-stubs --tenant X --age 24h`). Output is informational only — orphan stubs are NOT auto-deleted; operators decide whether to (a) re-publish the missing source event, (b) explicitly accept the gap and drop the stub via a maintenance command, or (c) leave it as a known dataset gap for downstream causal-chain queries. A telemetry gauge `memories_graph_orphan_stub_count{tenant=...}` (sampled every 5 min by `NaturalLanguageEmbeddingRetryHostedService` — reuse the same scheduling slot since both are graph-maintenance) drives alerting. Document this in `docs/dev/eventstore-integration.md` under "Orphan stub detection".

### Retry queue vs DAPR resiliency policy

DAPR's default resiliency policy retries 5xx responses indefinitely (per MemoryUnitStatus bank). The NL retry queue is SEPARATE — it's a code-level queue specifically for LLM failures. The workflow returns 200 on degraded success (raw embedding indexed) so DAPR does NOT retry the whole event. The `NaturalLanguageEmbeddingRetryHostedService` independently retries just the NL path. This separation means a chronic LLM outage does NOT block event throughput.

### Middleware / DI ordering (canonical for 9.2)

```csharp
// In AddMemoriesServer:
services.AddDaprAiConversation();                                    // NEW: Dapr.AI registration
services.Configure<NaturalLanguageDescriptionOptions>(config.GetSection("NaturalLanguage"));
services.AddSingleton<IValidateOptions<NaturalLanguageDescriptionOptions>, NaturalLanguageDescriptionOptionsValidator>();
services.AddSingleton<IFailedNaturalLanguageEmbeddingRegistry, FailedNaturalLanguageEmbeddingRegistry>();
services.AddHostedService<NaturalLanguageEmbeddingRetryHostedService>();

// Workflow/activity registration (existing pattern):
options.RegisterWorkflow<NaturalLanguageEmbeddingRetryWorkflow>();    // NEW
options.RegisterActivity<GenerateNaturalLanguageDescriptionActivity>();// NEW
options.RegisterActivity<IndexNaturalLanguageSemanticActivity>();    // NEW
options.RegisterActivity<QueueNaturalLanguageEmbeddingRetryActivity>();// NEW
```

### Project Structure Notes

**New files:**

```
src/Hexalith.Memories.Contracts/V1/
  EmbeddingContentKind.cs                           # NEW
  ConfidenceSource.cs                               # NEW (enum: Logprobs | Constant | Unknown)
  NaturalLanguageEmbeddingStatus.cs                 # NEW
  NaturalLanguageDescriptionInput.cs                # NEW
  NaturalLanguageDescriptionResult.cs               # NEW (now includes ConfidenceSource field)
  NaturalLanguageIndexInput.cs                      # NEW (now includes ConfidenceSource field)
  NaturalLanguageEmbeddingRetryInput.cs             # NEW
  NaturalLanguageEmbeddingRetryResult.cs            # NEW
  FailedNaturalLanguageEmbeddingRecord.cs           # NEW (payload-by-reference: NO RawJsonPayload field)
  (MemoriesJsonContext.cs updated — register all new types)

src/Hexalith.Memories.Server/NaturalLanguage/
  NaturalLanguageDescriptionOptions.cs              # NEW (sealed CLASS — NOT record — for IOptions binding)
  NaturalLanguageDescriptionOptionsValidator.cs     # NEW
  NaturalLanguageDescriptionUnavailableException.cs # NEW
  NaturalLanguageResponseCleaner.cs                 # NEW
  IFailedNaturalLanguageEmbeddingRegistry.cs        # NEW
  FailedNaturalLanguageEmbeddingRegistry.cs         # NEW
  NaturalLanguageEmbeddingRetryHostedService.cs     # NEW (payload-by-reference retry; backpressure-aware)
  RateLimiterSizingValidator.cs                     # NEW (Task 8.7 — Winston)

src/Hexalith.Memories.Server/Hosting/
  WorkflowReplaySafetyHostedService.cs              # NEW (Task 5.9 startup gate — Winston)

src/Hexalith.Memories.Server/Activities/Ingestion/
  GenerateNaturalLanguageDescriptionActivity.cs     # NEW
  QueueNaturalLanguageEmbeddingRetryActivity.cs     # NEW
  QueueNaturalLanguageEmbeddingRetryInput.cs        # NEW

src/Hexalith.Memories.Server/Activities/Indexing/
  IndexNaturalLanguageSemanticActivity.cs           # NEW (clone pattern of IndexSemanticActivity.cs)

src/Hexalith.Memories.Server/Workflows/
  NaturalLanguageEmbeddingRetryWorkflow.cs          # NEW

src/Hexalith.Memories.Server/Search/
  NaturalLanguageSemanticSearchService.cs           # NEW (library class; NOT wired to HybridSearchService)

deploy/dapr/components/
  conversation-llm.yaml                             # NEW

tests/Hexalith.Memories.Server.Tests/NaturalLanguage/
  NaturalLanguageResponseCleanerTests.cs
  NaturalLanguageDescriptionOptionsValidatorTests.cs
  FailedNaturalLanguageEmbeddingRegistryTests.cs
  NaturalLanguageEmbeddingRetryHostedServiceTests.cs
  NaturalLanguageDescriptionPromptTests.cs
  ProjectCompilationTests.cs

tests/Hexalith.Memories.Server.Tests/Activities/
  GenerateNaturalLanguageDescriptionActivityTests.cs
  IndexNaturalLanguageSemanticActivityTests.cs

tests/Hexalith.Memories.Server.Tests/Workflows/
  IngestionWorkflowDualEmbeddingTests.cs
  NaturalLanguageEmbeddingRetryWorkflowTests.cs
  IngestionWorkflowReplaySafetyTests.cs

tests/Hexalith.Memories.IntegrationTests/
  DualEmbeddingRoundTripTests.cs                    # Tier 2/3
  OutOfOrderEventTests.cs                           # Tier 2/3
  DegradedNaturalLanguageEmbeddingTests.cs          # Tier 2
  CorrelationRootEdgeTests.cs                       # Tier 2

docs/dev/
  natural-language-embedding.md                     # NEW (optional deep-dive)
  eventstore-integration.md                         # UPDATED (9.2 sections)
```

**Files to modify:**

- `src/Hexalith.Memories.Server/Hexalith.Memories.Server.csproj` — add `<PackageReference Include="Dapr.AI" />`, `<NoWarn>$(NoWarn);DAPR_CONVERSATION</NoWarn>`
- `src/Hexalith.Memories.Server/DependencyInjection/*.cs` (or wherever `AddMemoriesServer` lives) — wire the new services, workflow, activities, hosted service
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — SourceType.Event-gated dual-embedding block (Task 5.3)
- `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs` — add `GenerateNaturalLanguageDescriptionActivity` retry policy
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingInput.cs` — add `ContentKind` field (additive)
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — add telemetry tag + metric for `ContentKind`
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` — CorrelationId self-edge guard + read `previousIsStub` for telemetry
- `src/Hexalith.Memories.Server/Activities/Indexing/CleanupSemanticActivity.cs` — delete BOTH hashes
- `src/Hexalith.Memories.Server/Activities/Tenants/ProvisionRedisVectorActivity.cs` — create BOTH indexes
- `src/Hexalith.Memories.Server/Activities/Tenants/DeleteRedisVectorActivity.cs` + `DeleteRedisVectorIndexActivity.cs` — delete BOTH
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` — `isStub` flag handling in `BuildMergeStubNode` + `BuildMergeMemoryUnitNode`
- `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs` — upgrade gap-marker detection
- `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs` — NL index constants + methods
- `src/Hexalith.Memories.Contracts/V1/IngestionResult.cs` — add `NaturalLanguageEmbeddingStatus` field
- `src/Hexalith.Memories.Server/EventStoreIntegrationLog.cs` (from 9.1) — add 9150-9199 event IDs
- `src/Hexalith.Memories.AppHost/Program.cs` — register `llm` DAPR component
- `src/Hexalith.Memories.Server/appsettings.Development.json` + `appsettings.Production.json` — add `NaturalLanguage` section
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — 9-2 → ready-for-dev
- `docs/dev/eventstore-integration.md` — 9.2 sections

### Logging (9150-9199 bank)

Event ID bank per 9.1 structure (`EventStoreIntegrationLog.cs`):

- `9150 NaturalLanguageDescriptionGenerated (tenantId, memoryUnitId, llmProvider, llmModel, durationMs, confidenceSource)` — **Debug** (high-frequency; Improvement AG — Information would flood logs at 100 events/sec × N tenants. The aggregate metric `memories_natural_language_description_duration_ms` captures volume; per-event logging is noise.)
- `9151 NaturalLanguageDescriptionSkippedLlmUnavailable (tenantId, memoryUnitId, reason)` — Information
- `9152 NaturalLanguageEmbeddingQueuedForRetry (tenantId, memoryUnitId, queuedAtTicks)` — Information
- `9153 NaturalLanguageEmbeddingRetrySucceeded (tenantId, memoryUnitId, attempts)` — Information
- `9154 StubNodeResolved (tenantId, memoryUnitId, causingEventId, stubCreatedAt, resolvedAt)` — Information. **Canonical shape** — use this exact field set in all three references (TL;DR #14, Task 7.3, this section). Operators compute resolution latency = `resolvedAt - stubCreatedAt`.
- `9155 CorrelationIdSelfEdgeSkipped (memoryUnitId)` — Debug
- `9160 ConversationApiOutage (llmProvider, durationMs)` — Warning (transient; emitted from retry hosted service after ≥3 consecutive failures)
- `9161 EchoComponentNotAllowedInProduction` — Critical (startup fail-fast, Options validator)
- `9162 ConversationApiIsEchoComponent (llmProvider)` — Warning (dev-only — emitted when resolved component = `conversation.echo`; disambiguated from `9160` which was previously overloaded for this purpose)
- `9163 RateLimiterUnderSizedForEvents (tenantId, currentCeiling, recommendedCeiling)` — Warning (emitted at first `SourceType.Event` ingest when tenant's `EmbeddingRateLimiterActor` ceiling is below `currentUsage * 2`)
- `9164 ResponseCacheEnabledWithoutAcknowledgment (ttl)` — Critical (startup fail-fast — Improvement V; options validator rejects `responseCacheTTL > 0s` without `AcceptCrossTenantCacheSharing` acknowledgment)
- `9170 NaturalLanguageEmbeddingRetryQueueBacklog (tenantId, backlogCount, severity)` — Warning at > 100, Error at > 1000
- `9171 InFlightWorkflowsDrainingAtStartup (count, oldestVersion)` — **Warning** (per-poll; normal deploy observability — Improvement X split)
- `9172 InFlightWorkflowsDrainTimeout (remainingCount)` — **Critical** (single-shot on 5min timeout expiry)
- `9173 ReplaySafetyGateSidecarUnreachable` — Critical (per-call query timeout — Improvement Z fail-open)
- `9174 RetryBackpressureOverride (tenantId, skippedTicks)` — Debug (Improvement AA — bypass after 10 consecutive skipped ticks to prevent queue starvation)
- `9175 StartupGateDisabled_NoEnumerationApi` — Critical (Spike 0.2 fallback if no DAPR workflow enumeration API exists)
- `9180 NaturalLanguageEmbeddingPermanentlyFailed (tenantId, memoryUnitId, attempts)` — Error (moved to dead-letter)

### Testing standards

Aligned with Story 9.1 + architecture D16:

- **Framework:** xUnit 2.9.3, Shouldly 4.3.0, NSubstitute 5.3.0, coverlet.collector 6.0.4.
- **Time:** Inject `TimeProvider`; `FakeTimeProvider` via `Microsoft.Extensions.TimeProvider.Testing` 9.5.0.
- **DAPR Conversation:** NSubstitute mock `DaprConversationClient` at Tier 1; `conversation.echo` DAPR component at Tier 2; real provider (env-gated) at Tier 3.
- **FalkorDB:** Tier 2 fixture (reuse Story 4.1's graph traversal integration harness).
- **Aspire DistributedApplicationTestingBuilder:** for Tier 3 dual-embedding + out-of-order tests.
- **Tier split:** default CI runs Tier 1 + Tier 2. Tier 3 gated behind `HEXALITH_INTEGRATION_TIER=3` env var; documented in `deferred-work.md` if skipped.

### Architectural Compliance

- **D9 (No premature interfaces):** `NaturalLanguageResponseCleaner` is a static class; no `INaturalLanguageResponseCleaner` interface (no test substitution need). `FailedNaturalLanguageEmbeddingRegistry` GETS an interface because it crosses to Redis (testability boundary).
- **D16 (xUnit + Shouldly + NSubstitute):** honored.
- **D18 (Error handling):** `NaturalLanguageDescriptionUnavailableException` is a typed exception for the specific "LLM unavailable" failure mode; other exceptions propagate for workflow retry.
- **D21 (Extension methods per project):** all new DI registration in `AddMemoriesServer` or a dedicated `NaturalLanguageServiceCollectionExtensions.cs`.
- **D22 (Code style):** file-scoped namespaces, Allman braces, `_camelCase` fields, nullable enabled, warnings-as-errors, ITANEO copyright headers.
- **D23 (DAPR Workflow for orchestrations):** retry via `NaturalLanguageEmbeddingRetryWorkflow`, NOT Polly.
- **D25 (Workflow-Actor-Activity separation):** the workflow calls activities; the hosted service schedules the retry workflow; activities call services — no workflow-service direct calls.
- **D26 (DAPR Conversation API):** honored.
- **FR60:** raw-payload + NL-description embeddings — implemented.
- **FR61:** CausationId (Story 1.5 + 9.1) + CorrelationId (Story 9.2 clarification) auto-indexed as graph edges with zero developer mapping code.
- **NFR6:** <5s for non-event sources; <7s for events (documented relaxation).

### Anti-Patterns to Avoid

- **DO NOT** generate the NL description asynchronously AFTER ingestion completes (ADR 9.2-A). Synchronous happy path is chosen; the queue is the degraded fallback.
- **DO NOT** create a third vector index. Two indexes (raw + NL) is the decision (ADR 9.2-B).
- **DO NOT** create fan-out `correlated_with` edges between every pair of correlated events (Risk #3, AC #4). Root → current only.
- **DO NOT** fork `IngestionWorkflow` into an `EventIngestionWorkflow`. The `SourceType.Event` branch is narrow and replay-deterministic.
- **DO NOT** call `DaprConversationClient` from anywhere except `GenerateNaturalLanguageDescriptionActivity`. Activities own I/O; workflows compose activities (D25).
- **DO NOT** catch `Exception` in the workflow's dual-embedding block. Catch ONLY `NaturalLanguageDescriptionUnavailableException`. Wider catches mask real bugs.
- **DO NOT** persist the NL description to `metadata["event.naturalLanguageDescription"]` by default. It's config-driven (ADR 9.2-F).
- **DO NOT** use the `conversation.echo` component in production (Risk #10). Options validator fails fast (Task 1.7).
- **DO NOT** increase `MaxTokens` above 80 without updating the cleaner's empty-response threshold. Long descriptions drift from domain meaning into generic LLM padding.
- **DO NOT** add per-tenant LLM configuration in 9.2 — defer to Phase 2.
- **DO NOT** backfill pre-9.2 events during the 9.2 release. The re-ingestion path handles it on demand.
- **DO NOT** use `Polly` or manual `for`-loops for LLM retries. Retry is exclusively the workflow's concern (activity-level via `RetryPolicyBuilder`) and the hosted service's concern (queue-level).
- **DO NOT** write streaming LLM code. `ConverseAsync` without streaming (alpha limitation per architecture L1097).
- **DO NOT** suppress `DAPR_CONVERSATION` in `Directory.Build.props`. Scope to `Hexalith.Memories.Server.csproj` only (Risk #1).
- **DO NOT** switch `EmbeddingInput` to property-init record syntax (Risk #17). Positional shape `(string TenantId, string ContentText, EmbeddingContentKind ContentKind = Payload)` is wire-compat-load-bearing — paused workflows in any ingestion path (`IngestionWorkflow`, `ReIngestionCoordinator`, future backfill workflows) will fail deterministic replay if the JSON shape changes from positional to property-named.
- **DO NOT** ship `responseCacheTTL > 0s` as the default DAPR Conversation component setting (Risk #16). Caching is shared across tenants at the sidecar level — leakage blast radius is a privacy incident. Operators OPT IN to caching, never opt out.
- **DO NOT** clear `m.stubCreatedAt` on stub resolution. Preserve the historical timestamp so post-mortem analysis can compute resolution latency. Filter `WHERE m.isStub = true` to query "current stubs only".
- **DO NOT** emit a separate `AccessTelemetryEvent` for NL description generation. Reuse `OperationIngest` with the `contentKind` tag.
- **DO NOT** deploy 9.2 without quiescing event ingestion for ≥2 minutes (Risk #13 + AC #11).

### Review Findings Log

#### Pre-dev — BMad Party Mode review (2026-04-21)

Multi-agent review by Winston (Architect), Amelia (Dev), Murat (TEA), Bob (SM), Dr. Quinn (Master Problem Solver). Blocking-grade items #2 (response cache), #3 (stubCreatedAt), #5 (5 missing tests), #6 (EmbeddingInput wire compat) APPLIED to this story. Items originally deferred — **all but the `isStub` backfill migration PROMOTED to in-scope via Advanced Elicitation session 2026-04-21** (pre-mortem + critical-perspective + self-consistency + expert-panel + critique-and-refine).

- [x] **(Bob — Medium) Effort estimate may be optimistic.** **APPLIED** — effort estimate rebaselined from 4-5 days + 0.5 cushion to **7 days + 0.5 cushion** in the story header. Explicit note: "DO NOT silently absorb overruns — flag and rebaseline if slipping past day 5."

- [x] **(Dr. Quinn + Freya — Medium) Confidence proxy honesty.** **APPLIED** — new `ConfidenceSource` enum `{ Logprobs, Constant, Unknown }` added to Task 2.2 + propagated through `NaturalLanguageDescriptionResult`, `NaturalLanguageIndexInput`, the NL index hash field `descriptionConfidenceSource`, and `9150` log attribute. Default constant lowered from `0.85` → `0.5`. UI renders `Constant` distinctly ("AI-inferred (estimate unavailable)") so operators cannot mistake a default for a measurement.

- [x] **(Winston — Medium) Workflow replay safety startup gate.** **APPLIED** — new Task 5.9 creates `WorkflowReplaySafetyHostedService` as a fail-safe code-level gate. `StartAsync` queries DAPR Workflow for version-mismatched active `IngestionWorkflow` instances; if found, logs `9171 InFlightWorkflowsMismatchAtStartup` Critical + delays workflow-host startup with 5s poll / 5min timeout. Runbook retained as operator-side discipline; gate is the fail-safe. AC #11 updated to reference both.

- [x] **(Winston — Medium) Rate-limiter sizing for dual-embedding tenants.** **APPLIED as warning, NOT auto-scale** — new Task 8.7 emits `9163 RateLimiterUnderSizedForEvents` at first `SourceType.Event` ingest when ceiling < `currentUsage * 2`. Warning rather than auto-scale because some tenants have hard provider quotas — operator decides whether to adjust. Prevents silent 40% NL-embedding loss post-deploy on previously-right-sized tenants.

- [ ] **(Dr. Quinn — Low) One-time `isStub = false` backfill migration.** **NOT APPLIED — defensibly deferred.** `GraphTraversalService.cs:98` fallback heuristic will persist until a dedicated migration story. Amelia to leave a `// TODO: remove after isStub backfill migration ships` comment at the fallback site. Tracked in `deferred-work.md`.

- [x] **(Amelia — Low / gotcha) `IOptions<T>` binding requires class.** **APPLIED** — Task 1.6 spec updated: `sealed record` → `sealed class` with parameterless ctor + settable properties. Inline rationale added to prevent re-regression. Also added test-override pathway for `RetryIntervalSeconds` per Murat's flaky-test concern.

#### Advanced Elicitation session — 2026-04-21

Session ran 5 methods (Pre-mortem, Challenge from Critical Perspective, Self-Consistency Validation, Expert Panel Review, Critique and Refine). Additional items surfaced beyond the original Party Mode findings and applied:

- [x] **Pre-mortem Failure δ — retry queue payload-by-value causes Redis OOM.** **APPLIED** — `FailedNaturalLanguageEmbeddingRecord` no longer carries `RawJsonPayload` (Task 8.1). Retry workflow re-reads payload from the memory unit's existing Redis hash via `IMemoryUnitReader` at retry time (Task 8.4). Queue entries shrink from ~4KB to ~100 bytes. New `GetBacklogBytesAsync` gauge tracks sorted-set memory usage.

- [x] **Pre-mortem Failure ζ — retry surge starves live ingestion via shared rate limiter.** **APPLIED** — Task 8.5 `NaturalLanguageEmbeddingRetryHostedService` checks `EmbeddingRateLimiterActor` utilization before dequeuing; if live traffic > 80% of budget, skips retry batch this tick. Guard test `.LiveIngestionSurge_BackpressuresRetryDequeue`.

- [x] **Self-consistency Drift 1 — 9154 log shape defined 3 ways.** **APPLIED** — canonical shape `(tenantId, memoryUnitId, causingEventId, stubCreatedAt, resolvedAt)` unified across TL;DR #14, Task 7.3, logging section.

- [x] **Self-consistency Drift 2 — code example at Dev Notes L525 contradicted Risk #17 anti-pattern.** **APPLIED** — example switched to positional `new EmbeddingInput(...)` constructor with inline comment citing Risk #17.

- [x] **Self-consistency Drift 8 — 9160 defined twice with different meanings.** **APPLIED** — disambiguated: `9160 ConversationApiOutage` (retry service after ≥3 failures) + `9162 ConversationApiIsEchoComponent` (dev-only warning). Risk #10 mitigation updated to reference 9162.

- [x] **Self-consistency Drift 9 — `NaturalLanguageSemanticSearchService` in file list but no task creates it.** **APPLIED** — new Task 4.9 explicitly creates the library class + its unit test; emphasizes "NOT wired into `HybridSearchService`" per AC #7.

- [x] **Winston expert-panel — compensation boundary for orphan NL index on mid-provisioning failure.** **APPLIED** — new Task 4.10 extends Story 5.1's `TenantProvisioningWorkflow` compensation to drop both `:memories:vec` and `:memories:vec:nl`; also ships a startup reconciler that sweeps orphan `:nl` indexes with no matching raw sibling.

- [x] **Murat expert-panel — PII behavior within-tenant untested.** **APPLIED** — Task 2.8 adds `.PayloadWithCustomerPii_SummaryMayContainPii_DocumentedBehavior` documentation-control test. Name is the warning.

- [x] **Murat expert-panel — flaky 60s retry interval in integration tests.** **APPLIED** — Task 1.6 explicitly calls out test-override path for `RetryIntervalSeconds`.

- [x] **Improvement F — dead-letter CLI surface.** **APPLIED** — Task 8.8 adds `memories retry-nl-embeddings --tenant X [--dead] [--dry-run]` CLI or documents as deferred in `deferred-work.md` with Redis-level interim commands if the CLI project doesn't yet exist.

#### Advanced Elicitation session 2 — 2026-04-21

Second 5-method pass (Occam, Thesis Defense, Chaos Monkey, Mentor and Apprentice, Rubber Duck Evolved). Surfaced 2 implementation blockers, multiple operational refinements, and one reversal of a Session 1 decision.

**Blockers added (pre-impl verification spikes — gate tasks):**

- [x] **Spike 0.1 — Raw payload durability (blocks Task 8.1, 8.4, 8.5) [AI/AM].** Payload-by-reference retry design (Session 1 Improvement J) assumes raw payloads are durably retrievable post-ingestion. Unverified. Spike gates the retry-queue data-shape decision; fallback is bounded payload-by-value.
- [x] **Spike 0.2 — DAPR Workflow instance enumeration API (blocks Task 5.9) [AL].** Startup gate depends on `DaprWorkflowClient.GetInstances(...)` or equivalent. Unverified. Spike gates the code-level gate; fallback is runbook-only mitigation.
- [x] **Spike 0.3 — `Dapr.AI` exception surface (blocks Task 2.5) [AE].** `ConversationException` type name unverified; catch list may need widening.
- [x] **Spike 0.4 — HostedService ordering (blocks Task 5.9) [AF].** Verify `AddDaprWorkflow` hosted service registration order; fall back to `IStartupFilter` if needed.

**Applied in-scope (session 2):**

- [x] **V — Response cache acknowledgment validator.** Task 1.7 extended: options validator parses resolved component YAML for `responseCacheTTL`; if > 0s, requires `AcceptCrossTenantCacheSharing: true` acknowledgment env var or config. Fails fast `9164` Critical if unacknowledged. README-level opt-in was not a control.
- [x] **X — Split 9171/9172 log levels.** `9171 InFlightWorkflowsDrainingAtStartup` Warning (per poll; normal deploy); `9172 InFlightWorkflowsDrainTimeout` Critical (one-shot on 5min timeout). Prevents per-deploy Critical pages.
- [x] **Z — Startup gate per-call timeout + fail-open.** Each DAPR query wrapped in 10s CTS; on sidecar unreachable, log `9173` Critical and skip gate. Stuck pod worse than missing gate.
- [x] **AA — Retry backpressure max-skip budget.** Never skip > 10 consecutive ticks for same tenant; bypass once on 11th to avoid starvation. Emit `9174` Debug.
- [x] **AB — Rate-limiter sizing sustained-window.** `9163` fires only on sustained (15min sliding window) under-sizing, not first-event burst. Avoids false-positive pages.
- [x] **AC — Retry workflow final existence check.** Final activity `CheckMemoryUnitHashExistsActivity` before NL hash write; abandons if memory unit was purged mid-retry.
- [x] **AD — `MemoriesJsonContextCompletenessTests`.** Reflection-based test asserts all contract types have `[JsonSerializable]` registration. Catches AOT serialization omissions class-wide, not per-type. Strengthened `ProjectCompilationTests` to build a throwaway csproj rather than string-match `Directory.Build.props`.
- [x] **Chaos Scenario D reversal — restore orphan-index reconciler.** Session 1 Occam cut this as duplicate defense; chaos analysis showed it catches the SIGKILL-during-provisioning case that compensation workflow cannot reach. Restored as `OrphanSemanticIndexReconciler` in Task 4.10.
- [x] **Occam cuts applied.** `ListTenantsWithBacklogAsync` 10k-tenant cap warning dropped; `GetBacklogBytesAsync` conditional on Spike 0.1 outcome; `9181 RetryTargetMemoryUnitDeleted` demoted to unnamed Debug; `EstimatedConfidence` made nullable (eliminates "which default constant" debate entirely).
- [x] **U — `isStub` backfill migration.** Session 1 deferred (Dr. Quinn Low); CTO concession in Thesis Defense reframed it as production-data-pollution-for-life. New Task 7.6 ships a one-shot `IDeploymentMigration`; content-absent fallback retires post-migration.
- [x] **W — PII acknowledgment governance artifact.** New Task 10.1.10 requires sign-off document by Product + Legal BEFORE deploy to PII workloads. Test is a visible control, not consent.
- [x] **AE — `Dapr.AI` exception surface safe baseline.** Task 2.5 catch list widened as interim until Spike 0.3 verifies.
- [x] **AF — HostedService ordering verification.** Spike 0.4 gates Task 5.9's startup ordering assumption.
- [x] **AG — Downgrade `9150` Information → Debug.** Log flood avoidance at 100 events/sec scale.
- [x] **AH — Explicit `NaturalLanguageIndexInput` field list.** Removes implicit-mirroring grep-hunt.
- [x] **AJ — Enumerate `BuildMergeStubNode` callers before signature change.** Pre-impl subtask in Task 7.1.
- [x] **AK — NL description determinism boundary Dev Note.** Documents "deterministic within execution, not across state loss."
- [x] **Y — Exact CLI commands in operator runbook 10.1.3.** One-line commands, not paragraphs.

**Reversal of Session 1 decision:**

- [x] **Chaos Scenario D — orphan-index startup reconciler restored.** Session 1 Occam labeled it "duplicate defense"; chaos analysis proved otherwise (catches SIGKILL case that mid-workflow compensation cannot cover).

**Remains unresolved — flagged for user decision:**

- [ ] **(Winston — TBD) Weekly Tier-3 CI smoke against `conversation.echo` for runtime API schema drift.** Winston wants a weekly CI job publishing a test event end-to-end and asserting the DAPR Conversation response shape hasn't drifted. Murat: "that's infrastructure configuration, not a test spec." Not applied — if accepted, add as Task 9.7 or file as separate CI-infra task.

## Dev Agent Record

### Agent Model Used

claude-opus-4-7[1m]

### Debug Log References

### Completion Notes List

### File List

### References

- Epic 9 — `_bmad-output/planning-artifacts/epics.md#L1699-1731` (Story 9.2 AC authoritative source)
- FR60-FR62 — `_bmad-output/planning-artifacts/prd.md#L906-911`
- NFR6 (event indexing freshness) — `_bmad-output/planning-artifacts/prd.md#L947`
- Architecture — DAPR Conversation API section — `_bmad-output/planning-artifacts/architecture.md#L235,#L444,#L567,#L882-980,#L1097,#L1139-1140`
- Architecture — Project #10 `Hexalith.Memories.EventStore` (publishable package) — `_bmad-output/planning-artifacts/architecture.md#L494`
- Architecture — Edge taxonomy (`caused_by`, `correlated_with` defaults) — `_bmad-output/planning-artifacts/architecture.md#L119-133`
- Architecture — At-least-once + ordering guarantees + gap markers — `_bmad-output/planning-artifacts/architecture.md#L74`
- PRD — Edge confidence + causal intelligence — `_bmad-output/planning-artifacts/prd.md#L483-497`
- Ingestion workflow (existing) — `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:151-202`
- Embedding activity (existing) — `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:56-123`
- Embedding result (existing) — `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingResult.cs`
- Semantic index activity (existing, pattern to clone) — `src/Hexalith.Memories.Server/Activities/Indexing/IndexSemanticActivity.cs`
- Index schema definitions (existing, extend) — `src/Hexalith.Memories.Server/Infrastructure/IndexSchemaDefinitions.cs:36-67,102-114`
- Graph edges (existing) — `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:74-101`
- Graph query builder (existing, edit) — `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs:53-98,210-222`
- Graph traversal gap-marker heuristic (existing, upgrade) — `src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs:98-106`
- Retry policy builder (existing, extend) — `src/Hexalith.Memories.Server/Ingestion/RetryPolicyBuilder.cs:20-78`
- Metadata origin enum (`System` added by 9.1 Task 2.10) — `src/Hexalith.Memories.Contracts/V1/MetadataOrigin.cs`
- Ingestion contracts (`IngestionInput`, `IndexInput`, `MetadataField`) — `src/Hexalith.Memories.Contracts/V1/*.cs`
- Story 9.1 context — `_bmad-output/implementation-artifacts/9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md`
- Story 1.5 context (three-backend indexing) — `_bmad-output/implementation-artifacts/1-5-three-backend-indexing.md`
- Story 1.6 context (ingestion workflow + idempotency + concurrent-duplicate race) — `_bmad-output/implementation-artifacts/1-6-ingestion-workflow-orchestration.md`
- Story 1.7 context (embedding provider + rate limiter) — `_bmad-output/implementation-artifacts/1-7-embedding-provider-configuration.md`
- Story 4.3 context (confidence promotion — complements gap-marker resolution) — `_bmad-output/implementation-artifacts/4-3-gap-detection-and-confidence-promotion.md`
- Story 5.5 context (per-tenant embedding config + model identifier) — `_bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md`
- Story 6.3 context (`FailedUnitsRegistry` Redis Sorted Set pattern) — `_bmad-output/implementation-artifacts/6-3-retry-failure-visibility-and-re-ingestion.md`
- Story 8.2 context (consistency verification extension for NL axis) — `_bmad-output/implementation-artifacts/8-2-consistency-verification-and-repair.md`
