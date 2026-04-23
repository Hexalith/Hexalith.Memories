# Story 9.3: Handler Registration & Mismatch Detection

Status: ready-for-dev

**Revision history:**
- 2026-04-23 (round 1) — Advanced Elicitation Review (pre-mortem + devil's-advocate + ADR + red-team + critique passes). 16 deltas applied: estimate revised 7→9-11d (Δ15); added AC #20-#25 covering NFR latency, kill switch, bounded FAF, SDK-server contract, experimental header, authZ (Δ13, Δ14, Δ3, Δ5, Δ12); new Spike 0.5 for authZ policy (Δ12); new Task 10.0 fixture-smoke spike reordered to run FIRST (critique); new Task 1.5a aggregates-set cardinality cap at 1024 + log event 9142 (Δ10); Task 3.4 hardened with SemaphoreSlim(256) + 2s timeout + whitespace-strict gate (Δ3, Δ11); new `memories.handlers.observations.dropped` counter + cardinality smoke test (Δ4); new `EventStoreObservationOptions` kill switch + 9143 log event (Δ14); 5 ADR deliverables in Task 11.7-11.11 (Δ9); projection-registry cross-check gap surfaced in "What does NOT ship" (Δ1); 7 new deferred-work tracking references in Task 11.6 (Δ1, Δ2, Δ4, Δ7, Δ8); Dev Notes add sections on Redis-Streams-rejected, stale-phrasing-unification, regex-vs-attribute, 3-vs-4-state, --since-flag, experimental-header convention. Unified "stale handler" phrasing to `observedTypes.Count == 0` across Risk #2, AC #4, §10 StaleHandler detection (Δ16). No prior AC / risk wording was deleted — only extended.
- 2026-04-23 (round 3) — Advanced Elicitation Review (Occam's Razor + Meta-prompting + Chaos Monkey + Shark Tank + Hindsight Reflection). 14 deltas applied — **theme: cut, not add**. Reverted Finding C (consumer-by-version list — cross-tenant read architecture violation) and Finding D (`--format explain` formatter — speculative). Merged 9143+9145 log events into single 9143 with `direction` tag. Dropped `Summary.CategoriesExamined` (duplicated metadata). Corrected drop-rate detector to per-drop emission (Risk #7 said do not extend `RollingCounterStore` — prior round contradicted it). Dropped `Story-9.3-PercentageRolloutFlag` from deferred-work (would be a rewrite, not a flag). Hardened `IOptionsMonitor.OnChange` idempotency to compare by value not reference (Chaos Monkey finding). **Semantic fix**: observation store now records on `Accepted` ONLY, not `Duplicate` — Duplicate means "we already recorded this" so counting again over-counts (was a latent bug in spec). Added 500ms timeout on `TenantRegistryService.GetAsync` in partial-snapshot path. Added window-boundary semantics note to Dev Notes. **Meta note — diminishing returns**: Round 1 delivered ~14/16 load-bearing findings (87% signal). Round 2 delivered ~18/31 (58% signal). Round 3 was deliberately cut-oriented (100% signal on those cuts because we had real over-builds to correct). **Future stories should budget 1-2 elicitation rounds, not 3+; the third round is primarily about correcting the second round.**
- 2026-04-23 (round 2) — Advanced Elicitation Review (persona focus group + code-review gauntlet + Feynman + failure-mode + what-if passes). 31 findings applied (Findings A-EE). Key structural changes: **merged ADR-9.3-004 + ADR-9.3-005 into ADR-9.3-004 "Enum minimalism for operator-facing types"** (F — Task 11 drops from 5 ADRs to 4); kill switch upgraded from `IOptions<T>` to `IOptionsMonitor<T>` for hot-reload without restart (Q — AC #21 + Task 3.4); `HandlerRegistryService` uses per-tenant try-catch so one bad tenant degrades gracefully instead of 500ing the endpoint (S — Task 4.3); new `ObservationDropRateElevated` 9144 log when dropped > 0.01% / 1h window (G); new `HasWarnings` + `HasInfo` computed properties + `.summary` field on `HandlerMismatchReport` (L, EE); new Spike 0.6 (submodule `IEventIngestionTelemetry` sweep, M) + Spike 0.7 (named-arg call-site sweep, O); CLI gains `--exclude-stale`, `--only-warning`, `--format explain`, `--no-wrap` (B, D, X); new AC #26-#29 (clock-skew, partial-snapshot, summary-shape, routing-priority); deferred-work list extends with `Story-9.3-PostgresObservationStoreAlternative` (DD), `Story-9.3-PercentageRolloutFlag` (BB), `Story-9.3-ScheduleDescopePlan` (CC); Dev Notes add glossary (Z), semaphore-256 rationale (K), TTL-vs-window independence (I), terminal-segment rationale (J), runbook-URL convention for Suggestion strings (A, C); additional tasks for operator polish, hardening sweeps, guard tests.

**Effort estimate:** ~10-12 working days (Elicitation Review 2026-04-23 rounds 1+2 — revised up from 7-8d baseline; the 7-8d bottom-up breakdown did not account for (a) code-review + CI-review latency (0.5-1d), (b) Tier-2 testcontainers fixture startup (~30s/test) amortized across ~18 integration tests (grew from 15 after round 2), (c) likely merge-conflict rework if 9.1/9.2 rebase during flight, (d) the ADR deliverables in Task 11 (~0.5d, reduced from 5 to 4 ADRs after round 2's merge), (e) the bounded-FAF + kill-switch work in Task 3.4, (f) the aggregates-set cardinality cap in Task 1.5, (g) the runtime-cardinality smoke test in Task 9.5, (h) round-2 additions: operator-polish CLI flags (`--exclude-stale`, `--only-warning`, `--format explain`, `--no-wrap`; ~0.5d), per-tenant try-catch + partial-snapshot shape (~0.25d), drop-rate detector 9144 (~0.25d), Spikes 0.6 + 0.7 sweeps (~0.25d). Base breakdown below is the 7-8d floor; add 3-4d total for rounds 1+2. Breakdown:
- **0.5 day — Pre-impl spikes 0.1–0.4** (MemoriesJsonContextCompletenessTests existence, StackExchange.Redis batch shape, TenantStatusGuard return types, IOptionsMonitor change-notification posture).
- **0.75 day — `IObservedEventTypeStore` + `RedisObservedEventTypeStore`** (Task 1) — includes the aggregates-index SET (Fix #5) + batched-pipeline write (4 ops) + fail-open posture + unit tests.
- **0.5 day — Contract types + AOT registration** (Task 2) — `HandlerRegistrationSnapshot`, `HandlerMismatchReport`, `MemoriesJsonContext` registrations, completeness test (create if missing per Spike 0.1).
- **0.5 day — `IEventIngestionTelemetry` extension** (Task 3) — add `cloudEventType` positional param, thread through 6 call sites in `EventIngestionService`, update `EventIngestionTelemetryAdapter` with fire-and-forget observation write + all test doubles.
- **0.75 day — `HandlerRegistryService`** (Task 4) — includes reconciled `SubscriptionStatus` rule (Fix #1) + empty-pattern data-purity (Fix #3) + 9131 log emission + scoped-lifetime DI + unit tests.
- **1.0 day — `HandlerMismatchDetector`** (Task 5) — UnhandledEventType + StaleHandler + VersionMismatch with terminal-segment stem extraction (Fix #2) + Context copy templates (Fix #4) + ReDoS defenses + 9141 log emission + 8 unit tests.
- **0.5 day — Minimal-API endpoints + DI wiring** (Task 6) — `/api/handlers` + `/api/tenants/{id}/handlers/mismatches` + TenantStatusGuard integration + 4 endpoint integration tests.
- **0.5 day — `MemoriesClient` REST methods** (Task 7) — `ListHandlersAsync` + `GetHandlerMismatchesAsync` with `[Experimental("HXL002")]` + unit tests + consumer-driven contract test (Fix #6).
- **0.75 day — CLI `handlers list` + `handlers mismatches`** (Task 8) — stub replacement in `RootCommandFactory` + both commands + table formatters with sentinel (Fix #3) + 8 unit tests + ADR-7.2-002 byte-stability snapshot.
- **0.5 day — `MemoriesMeter` instruments** (Task 9) — 2 new instruments, `EnsureHandlerGaugeCreated` helper, MetricTagKeyPolicy extension, metrics test extension.
- **1.0 day — Tier-2 integration tests** (Task 10) — `HandlersListIntegrationTests` + `HandlersMismatchIntegrationTests` + property-based `ObservationStoreLostWrites_DetectorConverges*` (Fix #7) against the Aspire AppHost fixture. Fixture startup is ~30s/test — slow iterations.
- **0.5 day — Docs + sprint-status** (Task 11) — §11 in `eventstore-integration.md`, `handlers` subsection in `cli-config.md`, telemetry-substrate separation note, 24h-window rationale (Fix #10), sprint-status.yaml transition.
- **0.25 day cushion** for unforeseen edge cases (CloudEvents `type` convention variants, DI lifetime mismatches discovered at commit time).

**HARD prerequisite:** Story 9.1 must be `done` — this story extends 9.1's `EventIngestionService` telemetry hook + `TenantEventRoutingOptions` surface. Story 9.2 (`review`) and Story 9.1 (`review`) are BOTH independent of 9.3: 9.3 observes the Outcome enum (added by 9.1) and does NOT require dual embeddings (9.2) to be merged — **9.3 lands even if 9.2 slips**. 8.5 (`ready-for-dev`) is parallel-safe: the Redis OTEL instrumentation it adds does not collide with the Redis keys used here (`eventstore:observed:*`).

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** A **read-only handler registry + mismatch detector** that surfaces three operator-facing signals without introducing new runtime behavior on the hot-path:
1. **`GET /api/handlers`** returns an enriched snapshot of every registered DAPR pub/sub subscription configured in `TenantEventRoutingOptions` — one row per `(pubSubName, topic, tenantId, aggregateType)` tuple — with per-tenant `eventsProcessedCount`, `lastEventAt`, `subscriptionStatus` (`active` when the DAPR sidecar has acknowledged the `/dapr/subscribe` probe within the rolling window, else `unknown`), and `observedEventTypes` (distinct `cloudevent.type` strings seen via `EventIngestionService` in the last 24h).
2. **`GET /api/tenants/{tenantId}/handlers/mismatches`** returns categorized mismatches: `UnhandledEventType` (observed on topic but no mapping matched an active tenant), `StaleHandler` (mapped source has received no events in 24h), `VersionMismatch` (e.g., `ClaimSubmittedV2` registered but `ClaimSubmittedV3` arriving — detected by name-stem + trailing `V\d+` comparison). Each mismatch carries `Severity` (`Info` / `Warning`) + an actionable `Suggestion` string.
3. **`memories handlers list` + `memories handlers mismatches` CLI commands** replace the 7.2-era `NotImplementedCommand` stub at `RootCommandFactory.CommandGroups[5]` and route through `MemoriesClient` + `OutputFormatterRouter` for human / JSON / table output — byte-compatible with the existing `tenant list` + `status telemetry` pattern (ADR-7.2-002 parity).

This closes **FR62** only. It does NOT ship any new subscription mechanism, does NOT change `EventIngestionController` routing, does NOT modify the `IngestionWorkflow`, and does NOT add any hot-path guards — the detector runs on demand at query time, not per-event.

**What already exists (do NOT rebuild):**

1. **`TenantEventRoutingOptions` — `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs`.** Already owns `PubSubName`, `Topic`, `SourceToTenantMap` (CloudEvents source-prefix → tenantId, longest-prefix wins, case-insensitive), `AutoCreateCases`, `CaseNameTemplate`, `MaxAutoCreatedCasesPerTenant`, `PreflightDedupEnabled`, `PreflightDedupTtl`. **Reuse verbatim.** 9.3's `HandlerRegistryService` is a pure READER over `IOptionsMonitor<TenantEventRoutingOptions>` — no new config surface.
2. **`EventIngestionService` — `src/Hexalith.Memories.EventStore/EventIngestionService.cs:56-193`.** Already parses the envelope via `CloudEventEnvelopeParser.Parse`, resolves the tenant via `ITenantEventRouter.ResolveAsync`, and emits a single `IEventIngestionTelemetry.RecordIngestion(...)` call per request at the end of every branch (Accepted / Duplicate / UnknownSource / TenantNotFound / TenantProvisioning / TenantDeleting / AutoCreateDisabled / CaseCapExceeded / InvalidCloudEvent / ScheduleFailed). **Reuse verbatim.** 9.3 EXTENDS the `IEventIngestionTelemetry` contract with ONE new positional parameter (`string? cloudEventType`) and threads it through — NOT a new telemetry sink, NOT a parallel pipeline.
3. **`IEventIngestionTelemetry` — `src/Hexalith.Memories.EventStore/IEventIngestionTelemetry.cs`.** Contract shape: `RecordIngestion(tenantId, caseId, cloudEventId, aggregateType, outcome, durationMs)`. **EDIT — add `cloudEventType` as a new positional parameter.** ALL three implementations (`NoOpEventIngestionTelemetry`, `EventIngestionTelemetryAdapter` in Server, any test doubles) update in lock-step. Method is not part of any workflow history, so positional-arg addition is safe.
4. **`EventIngestionTelemetryAdapter` — `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs`.** Already writes per-ingestion records via `AccessTelemetryLog.CreateEvent`. **EDIT** to (a) add `cloudEventType` to `queryParams["cloudEventType"]` for audit-log correlation, and (b) fan out to a new `IObservedEventTypeStore.RecordObservationAsync` call SO THE MAIN AUDIT PATH IS UNAFFECTED if observation persistence fails (fire-and-forget, logged-on-error).
5. **`RedisAggregateCaseMappingStore` — `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs:19`.** Shows the canonical per-tenant Redis-hash pattern. Key shape: `{tenantId}:eventstore:aggregate-case-map`. **Follow this shape verbatim** for the new observation store — keys `{tenantId}:eventstore:observed:{aggregateType}` (sorted set of event types by `lastSeenUnixMs`) + `{tenantId}:eventstore:observed-count:{aggregateType}:{eventType}` (counter hash) to match the existing key-naming ADR (9.1-E).
6. **`MemoriesMeter` — `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs:49-65`.** Already registers the `Hexalith.Memories` meter + `IngestionDocuments` / `IngestionFailures` / `SearchRequests` / `SearchDuration` / `IndexSize` / `PipelineQueueDepth` instruments with pinned tag-key policy (`MetricTagKeyPolicy` at :99). **EDIT** to add exactly two new instruments — `memories.handlers.registered` (observable gauge, tag `tenant_id`) + `memories.handlers.mismatches` (counter, tags `tenant_id` + `severity`). Extend `MetricTagKeyPolicy` in the same commit so `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys` catches drift (Risk #4 — tag-cardinality discipline).
7. **`TelemetrySnapshotCache` + `RollingCounterStore` — `src/Hexalith.Memories.Server/Telemetry/`.** Already provide the per-tenant cached-snapshot substrate + the 5-slot rolling counter store that `TelemetrySummaryService` reads. **DO NOT** extend these for handler state — the observation window is 24h (not 5m), keeping the substrates separate avoids polluting the tight 5m rolling-counter ring and its `MeterListener`-driven fast path (Risk #7). The observation store (Task 1) is a **separate** Redis-backed facility with its own TTL semantics.
8. **`MemoriesClient` — `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:22` + `GetTelemetrySummaryAsync` at :587 (EXPERIMENTAL HXL001).** Follow this pattern verbatim. Add `ListHandlersAsync(CancellationToken)` + `GetHandlerMismatchesAsync(string tenantId, CancellationToken)` both marked `[Experimental("HXL002")]` (the next reserved diagnostic id — reuse HXL001 would conflate Story 7.5 telemetry surface stability with 9.3's). Same HTTP client, same `MemoriesJsonContext.Options` deserialization, same `ErrorResponseDecoder` on non-2xx.
9. **`RootCommandFactory.CommandGroups[5]` — `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:80`.** The tuple `("handlers", "List registered event handlers.", "7.2")` is **already declared** and registered via `NotImplementedCommand.Create(...)` at :145-148. **REPLACE the stub** — remove the `handlers` entry from the `CommandGroups` `foreach` stub-loop, build a real `handlersCommand` above it (mirroring `statusCommand` + `consistencyCommand` shape at :120-132), and add two subcommands: `HandlersListCommand.Build(services)` + `HandlersMismatchesCommand.Build(services)`. **DO NOT** leave the stub in place beside the real command — duplicate subcommand registration throws at Parse time.
10. **`OutputFormatterRouter` + `TenantListCommand` — `src/Hexalith.Memories.Cli/Output/` + `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs`.** Follow this shape verbatim for `HandlersListCommand` (empty-state nudge pattern at :68-90 for "No registered handlers" when the routing map is empty, but WITHOUT the tenant-create CTA — see Task 8.3 for the handlers-specific empty-state copy).
11. **`TelemetrySummaryService` — `src/Hexalith.Memories.Server/Telemetry/TelemetrySummaryService.cs:34-76`.** Follow this shape verbatim for `HandlerRegistryService.GetSnapshotAsync` (single-method reader, DI-injected dependencies, no mutation). Minimal-API wiring mirrors `TenantEndpointHandlers.GetTenantConfigurationAsync` at `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`.
12. **`IOptionsMonitor<TenantEventRoutingOptions>` at `Program.cs:254`.** The `/dapr/subscribe` endpoint already reads it to expose the live subscription contract. **Reuse verbatim** for `HandlerRegistryService` — the SAME `IOptionsMonitor<T>` instance drives both surfaces, guaranteeing reload-coherence.
13. **`EventStoreIntegrationLog` — `src/Hexalith.Memories.EventStore/EventStoreIntegrationLog.cs`.** EventId bank `9100-9199`. **Add 9.3 entries in a new sub-bank 9130-9149** (the "9130+ Information (happy-path ingestion)" doc-comment reservation at :17 is the target range — document the sub-bank partition in the header): `9130 ObservedEventTypeRecorded` (Debug — high-frequency), `9131 HandlerRegistrySnapshotServed` (Information, includes `handlersCount`), `9132 HandlerMismatchDetected` (Information — with `severity` + `category` tags), `9140 ObservedEventTypeStoreWriteFailed` (Warning — Redis outage, we degrade gracefully), `9141 RegexSkippedForPathologicalEventType` (**Fix #8** — Warning, emitted when `VersionMismatch` regex is bypassed due to `RegexMatchTimeoutException` or `eventType.Length > 256`; includes `eventType` (truncated to 128 chars for safe logging) + `reason` tags).
14. **`MemoriesJsonContext` — `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` (AOT source-generated).** Register `HandlerRegistrationSnapshot`, `HandlerRegistration`, `HandlerMismatchReport`, `HandlerMismatch`, `HandlerMismatchSeverity`, `HandlerMismatchCategory`, `HandlerSubscriptionStatus` via `[JsonSerializable(typeof(T))]`. AOT-safe; matches the Story 9.2 spec pattern.

**What 9.3 adds:**

1. **`src/Hexalith.Memories.EventStore/IObservedEventTypeStore.cs`** — NEW. Public interface:
   ```csharp
   public interface IObservedEventTypeStore
   {
       Task RecordObservationAsync(string tenantId, string aggregateType, string eventType, DateTimeOffset observedAt, CancellationToken cancellationToken);
       Task<IReadOnlyList<ObservedEventType>> GetObservedTypesAsync(string tenantId, string aggregateType, TimeSpan window, CancellationToken cancellationToken);
       Task<IReadOnlyList<ObservedEventType>> GetAllObservedTypesAsync(string tenantId, TimeSpan window, CancellationToken cancellationToken);
   }
   public sealed record ObservedEventType(string AggregateType, string EventType, long Count, DateTimeOffset LastSeenAt);
   ```
   Public surface reason: Server + tests need the read side; the EventStore package is the natural owner because the store is co-located with `EventIngestionService`. Implementation type `RedisObservedEventTypeStore` stays `internal` (ADR 9.1-F — same public/internal split as `RedisAggregateCaseMappingStore`).

2. **`src/Hexalith.Memories.EventStore/RedisObservedEventTypeStore.cs`** — NEW. Redis-backed `IObservedEventTypeStore`. Keys (Fix #5 — SCAN replaced with auxiliary per-tenant aggregates SET):
   - **Aggregates index SET** `{tenantId}:eventstore:observed-aggregates` — members = distinct `{aggregateType}` values observed for this tenant. `SADD` on every observation (idempotent). `EXPIRE window * 2` on every write. **Purpose:** replaces the `SCAN {tenantId}:eventstore:observed:*` approach in `GetAllObservedTypesAsync` — the reader does `SMEMBERS {tenantId}:eventstore:observed-aggregates` (O(N) in aggregate count, bounded) and loops via `GetObservedTypesAsync` per member. SCAN is production-safe but O(keyspace); SMEMBERS is O(cardinality). At 1000+ tenants this difference is load-bearing (Fix #5 — Winston's concern).
   - **Sorted set** `{tenantId}:eventstore:observed:{aggregateType}` — member = `{eventType}`, score = `observedAt.ToUnixTimeMilliseconds()`. `ZADD` on every observation (XX=false — insert-or-update score). `ZRANGEBYSCORE` with `(now - window).ToUnixTimeMilliseconds()` trims to the window at query time. `EXPIRE` set to `window * 2` on every write to bound growth.
   - **Counter hash** `{tenantId}:eventstore:observed-count:{aggregateType}` — field = `{eventType}`, value = increment. `HINCRBY 1`. `EXPIRE` set to `window * 2`. **Reason for 3 keys:** aggregates SET answers "which aggregates has this tenant seen" (discovery — replaces SCAN); sorted set answers "was this type seen in the last 24h" (membership + recency); counter hash answers "how many times" (the `Count` field). Three commands on every observation (SADD + ZADD + HINCRBY) → one pipelined `Batch` round-trip (see Task 1.4 — `IDatabase.CreateBatch()` pattern).
   - **[FromKeyedServices("redis")] IConnectionMultiplexer** — same keyed-service pattern as `RedisAggregateCaseMappingStore` at :19.
   - **Fail-open on Redis exception.** If Redis throws on write, log `9140 ObservedEventTypeStoreWriteFailed` at Warning and return — NEVER bubble up to break `EventIngestionService`. Read-side failures throw (the caller of `GetSnapshotAsync` returns a 500 with `ErrorResponse`).

3. **`src/Hexalith.Memories.EventStore/NoOpEventIngestionTelemetry.cs`** — EDIT. Add `cloudEventType` parameter to `RecordIngestion` signature. No-op body unchanged (the method does nothing by design).

4. **`src/Hexalith.Memories.EventStore/IEventIngestionTelemetry.cs`** — EDIT. Signature becomes:
   ```csharp
   void RecordIngestion(
       string tenantId,
       string? caseId,
       string? cloudEventId,
       string? aggregateType,
       string? cloudEventType,
       EventIngestionOutcome outcome,
       long durationMs);
   ```
   **Parameter ordering rationale:** `cloudEventType` slots between `aggregateType` and `outcome` to group the CloudEvents-envelope-derived fields together (`cloudEventId`, `aggregateType`, `cloudEventType`). All callers inside this repo compile-error on the signature change — explicit, desired blast radius.

5. **`src/Hexalith.Memories.EventStore/EventIngestionService.cs`** — EDIT. Thread `envelope.Type` (the CloudEvents `type` header — guaranteed non-null by `CloudEventEnvelopeParser.Parse` on the Accepted-and-Duplicate path; null-coalesced to `null` on the InvalidCloudEvent branch) through EVERY `_telemetry.RecordIngestion(...)` call site. Five existing call sites — one per branch. Reuse the existing `envelope` local; do NOT re-parse.

6. **`src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs`** — EDIT. Add `cloudEventType` to the method signature + `queryParams["cloudEventType"]` in the dictionary. **ADD** a BOUNDED fire-and-forget call to `_observedEventTypeStore.RecordObservationAsync(tenantId, aggregateType, cloudEventType, DateTimeOffset.UtcNow, linkedCt)` at the END of `RecordIngestion` — wrapped in a try/catch that logs via `EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed` on failure. Guard rules (ALL must hold or the observation write is skipped):
   - `outcome is Accepted` — **R3-8 narrowed from "Accepted or Duplicate"**: counting Duplicates inflates `EventsProcessedCount` by retry volume (a single logical event retried 3x would show count=3). Duplicates already signal "we accepted this once" via the earlier Accepted; dropped/failed events are visible in `AccessTelemetryLog`.
   - `!string.IsNullOrWhiteSpace(tenantId)` (Red Team Delta #11 — whitespace gate stricter than IsNullOrEmpty).
   - `tenantId != MemoriesMeter.RejectedTenantTag` (Risk #9).
   - `!string.IsNullOrWhiteSpace(aggregateType)` and `!string.IsNullOrWhiteSpace(cloudEventType)`.
   - **Kill switch (Delta #14 + Finding Q):** `optionsMonitor.CurrentValue.Enabled == true` — reads `IOptionsMonitor<EventStoreObservationOptions>` (NOT `IOptions<T>` — Finding Q correction: `IOptions<T>` is static-at-DI-time and would require a process restart, contradicting the "flip without redeploying" promise). Operators change `EventStoreIntegration:Observation:Enabled = false` in `appsettings.json` at runtime; the `IConfigurationRoot` reloadOnChange fires `IOptionsMonitor` change notifications; the hot path picks up the new value on the NEXT event. **R3-3 merge:** every transition emits **one** log event `9143 ObservationWritesConfigChanged` at Information level with tag `enabled ∈ {true, false}` — the prior Round-2 design had separate 9143/9145 for disable/enable; collapsing into one event-id with a direction tag halves the event-id burn without losing audit granularity.
   - **Bounded fire-and-forget (Delta #3 — hardens Risk #8):** the fire-and-forget task is gated by a process-wide `SemaphoreSlim(maxConcurrency: 256)` — if acquisition fails within `TimeSpan.FromMilliseconds(5)` via `WaitAsync(5ms)`, the observation is DROPPED (counter `memories.handlers.observations.dropped` increments with tag `reason = "backpressure"`) rather than enqueued indefinitely. The linked CancellationTokenSource has a `TimeSpan.FromSeconds(2)` timeout so a slow Redis cannot keep the task alive past two seconds. Net effect: observation writes NEVER tie up thread-pool threads for more than 2s, and the in-flight count is capped at 256. **Do NOT** pass `CancellationToken.None` — use `new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token` linked with `request.CancellationToken` where available.
   
   Constructor-inject `IObservedEventTypeStore` + `IOptions<EventStoreObservationOptions>` + `ILogger<EventIngestionTelemetryAdapter>`. Add the `EventStoreObservationOptions` type under `src/Hexalith.Memories.EventStore/EventStoreObservationOptions.cs` with XML doc calling out the kill-switch use case.

7. **`src/Hexalith.Memories.Contracts/V1/HandlerRegistrationSnapshot.cs`** — NEW. Sealed record used by `GET /api/handlers`:
   ```csharp
   public sealed record HandlerRegistrationSnapshot
   {
       [JsonPropertyName("pubSubName")] public required string PubSubName { get; init; }
       [JsonPropertyName("topic")] public required string Topic { get; init; }
       [JsonPropertyName("asOf")] public required string AsOf { get; init; } // ISO-8601
       [JsonPropertyName("handlers")] public required IReadOnlyList<HandlerRegistration> Handlers { get; init; }
       [JsonPropertyName("subscriptionStatus")] public required HandlerSubscriptionStatus SubscriptionStatus { get; init; }
   }
   public sealed record HandlerRegistration
   {
       [JsonPropertyName("tenantId")] public required string TenantId { get; init; }
       [JsonPropertyName("sourcePrefix")] public required string SourcePrefix { get; init; }
       [JsonPropertyName("eventTypePatterns")] public required IReadOnlyList<string> EventTypePatterns { get; init; } // derived from observed types; [] when none seen yet
       [JsonPropertyName("eventsProcessedCount")] public required long EventsProcessedCount { get; init; } // 24h rolling
       [JsonPropertyName("lastEventAt")] public required string? LastEventAt { get; init; } // ISO-8601 or null
       [JsonPropertyName("observedEventTypes")] public required IReadOnlyList<ObservedEventTypeSummary> ObservedEventTypes { get; init; }
       // Finding S — per-tenant graceful degradation. Set to "OBSERVATION_READ_FAILED" when a specific tenant's Redis call throws; null in the happy path.
       [JsonPropertyName("error")] public string? Error { get; init; }
   }
   public sealed record ObservedEventTypeSummary
   {
       [JsonPropertyName("aggregateType")] public required string AggregateType { get; init; }
       [JsonPropertyName("eventType")] public required string EventType { get; init; }
       [JsonPropertyName("count")] public required long Count { get; init; }
       [JsonPropertyName("lastSeenAt")] public required string LastSeenAt { get; init; }
   }
   public enum HandlerSubscriptionStatus { Active, Unknown, Disabled }
   ```
   **Subscription-status canonical rule (Fix #1 — reconciled across AC #1, TL;DR §7, Task 4.3):**
   - `Disabled` — `TenantEventRoutingOptions.Topic` is empty (the `/dapr/subscribe` probe returns `[]` in this case — see `Program.cs:258`). `Handlers = []`.
   - `Unknown` — `Topic` is non-empty BUT either (a) `SourceToTenantMap` is empty (misconfiguration — subscribed to a topic with no tenant routing), OR (b) the process uptime is under the startup grace window of 2 minutes AND no events have been observed yet.
   - `Active` — `Topic` is non-empty AND `SourceToTenantMap` has at least one entry AND (process uptime ≥ 2 minutes OR at least one observation has been recorded). This is the steady-state healthy reading and is **independent of per-handler `EventsProcessedCount`** — a handler registered with zero traffic is still `Active` from the subscription's POV (traffic-level concerns are reflected per-row via `EventsProcessedCount` / `LastEventAt`, not in the top-level status).

   This is the single rule referenced everywhere — do NOT re-infer from per-handler event counts in Task 4.3.

8. **`src/Hexalith.Memories.Contracts/V1/HandlerMismatchReport.cs`** — NEW. Sealed record used by `GET /api/tenants/{tenantId}/handlers/mismatches`:
   ```csharp
   public sealed record HandlerMismatchReport
   {
       [JsonPropertyName("tenantId")] public required string TenantId { get; init; }
       [JsonPropertyName("asOf")] public required string AsOf { get; init; }
       [JsonPropertyName("windowHours")] public required int WindowHours { get; init; } // 24 default; renamed to `actualWindowHours` prospectively once Δ2 lands (Finding R — invariant-test asserts WindowHours matches detector's runtime window)
       [JsonPropertyName("mismatches")] public required IReadOnlyList<HandlerMismatch> Mismatches { get; init; }
       // Finding EE — positive-confirmation UX. Always populated, even on empty-mismatches responses.
       [JsonPropertyName("summary")] public required HandlerMismatchReportSummary Summary { get; init; }

       // Finding L — computed props for automated monitors that need to short-circuit without parsing the array.
       [JsonIgnore] public bool HasWarnings => Mismatches.Any(m => m.Severity == HandlerMismatchSeverity.Warning);
       [JsonIgnore] public bool HasInfo => Mismatches.Any(m => m.Severity == HandlerMismatchSeverity.Info);
   }
   public sealed record HandlerMismatchReportSummary
   {
       [JsonPropertyName("routesConfigured")] public required int RoutesConfigured { get; init; }
       [JsonPropertyName("observationsChecked")] public required int ObservationsChecked { get; init; }
       // R3-4 removed CategoriesExamined — always contained the same 3 enum values; duplicated metadata.
   }
   public sealed record HandlerMismatch
   {
       [JsonPropertyName("category")] public required HandlerMismatchCategory Category { get; init; }
       [JsonPropertyName("severity")] public required HandlerMismatchSeverity Severity { get; init; }
       [JsonPropertyName("subject")] public required string Subject { get; init; } // "ClaimSubmittedV2" or "MyApp/claims" etc.
       [JsonPropertyName("context")] public required string Context { get; init; } // free-form description
       [JsonPropertyName("suggestion")] public required string Suggestion { get; init; } // actionable next-step
   }
   [JsonConverter(typeof(CamelCaseStringEnumConverter<HandlerMismatchCategory>))]
   public enum HandlerMismatchCategory { UnhandledEventType, StaleHandler, VersionMismatch }
   [JsonConverter(typeof(CamelCaseStringEnumConverter<HandlerMismatchSeverity>))]
   public enum HandlerMismatchSeverity { Info, Warning }
   ```
   All enums use `CamelCaseStringEnumConverter<T>` (Story 1.2 convention). `Severity` is intentionally two-valued — there is no "Error" for mismatches because NO mismatch is action-blocking (Risk #2 — a "critical" severity would be interpreted as paging-worthy and is not; the category itself conveys urgency).

   **`Context` field copy per category (Fix #4 — canonical):** The detector MUST populate `Context` with a stable, structured description so AC #3's "non-empty Context describing where it was observed" is satisfied without dev invention.
   - **`UnhandledEventType`:** `$"Observed {count} event(s) of type '{eventType}' on aggregate '{aggregateType}' in the last {windowHours}h. No SourceToTenantMap entry routes this aggregate to tenant '{tenantId}'. Most recent observation: {lastSeenAt:O}."`
   - **`StaleHandler`:** `$"SourceToTenantMap entry '{sourcePrefix}' → '{tenantId}' has received zero events in the last {windowHours}h. Observation-store last write for this tenant: {mostRecentTenantObservationAt?:O ?? \"never\"}."`
   - **`VersionMismatch`:** `$"Stem '{stem}' observed with {versionCount} concurrent versions in the last {windowHours}h: {string.Join(\", \", versionsWithCounts)}. Total events across versions: {totalCount}."` — where `versionsWithCounts` is e.g., `"V2 (15 events)", "V3 (3 events)"`. (**R3-1 revert** of Round-2 Finding C: the consumer-by-version list was withdrawn because its implementation required cross-tenant reads inside a tenant-scoped endpoint, violating the Epic 5 tenant-isolation invariant. Publishers who need their migration blast radius should call a future dedicated endpoint — tracked as deferred-work `Story-9.3-CrossTenantVersionConsumerLookup`.)
   
   All three templates are stable per-version — changes require a minor-version bump on `HXL002` surface contract.

   **Finding A — runbook URL convention on every `Suggestion` string.** Each suggestion ends with `" See: https://docs.hexalith.dev/memories/runbooks/handler-{categoryKebab}."` where `{categoryKebab}` is the category in kebab-case (`unhandled-event-type`, `stale-handler`, `version-mismatch`). The URL is declarative — Jerome will stand up the runbook pages at landing time; the URL format is PINNED by the `Suggestion` template so the pages can be backfilled without another surface-contract bump. This gives SREs a concrete next step when woken at 2am instead of generic advice.

9. **`src/Hexalith.Memories.Server/Handlers/HandlerRegistryService.cs`** — NEW. `public sealed class`. Constructor-injects `IOptionsMonitor<TenantEventRoutingOptions>`, `IObservedEventTypeStore`, `TenantRegistryService`, `TimeProvider` (singleton — enables deterministic testing). One public method:
   ```csharp
   public async Task<HandlerRegistrationSnapshot> GetSnapshotAsync(CancellationToken ct)
   ```
   Logic:
   - Read `TenantEventRoutingOptions` via `IOptionsMonitor<T>.CurrentValue`.
   - If `options.Topic` is empty → return snapshot with `SubscriptionStatus = Disabled`, `Handlers = []`.
   - Enumerate `SourceToTenantMap` — ONE `HandlerRegistration` per entry.
   - Filter out tenants that are `TenantStatus.Deleting` or `TenantNotFound` (query `TenantRegistryService.GetAsync`).
   - For each remaining, call `_store.GetAllObservedTypesAsync(tenantId, TimeSpan.FromHours(24), ct)` → produce `ObservedEventTypeSummary[]` and aggregate `EventsProcessedCount = sum(count)`, `LastEventAt = max(lastSeenAt)`.
   - `EventTypePatterns` is derived from observed event types (`type`-header values), grouped by `aggregateType`. **Service layer always returns `[]` when none observed (Fix #3 — data purity).** The CLI table formatter is responsible for rendering `[]` as the sentinel `"(none observed in last 24h)"` at the presentation layer (Task 8.5).
   - Set `SubscriptionStatus = Active` when `Topic` is non-empty AND at least one tenant has received at least one event OR the container is less than 2 minutes old (startup grace). Otherwise `Unknown`.
   - Parallelize the per-tenant Redis reads via `Task.WhenAll` — 2N round-trips where N = tenant count, bounded by `SourceToTenantMap.Count`.
   - Emit `9131 HandlerRegistrySnapshotServed` once at the end with `handlersCount` tag.
   - Register in DI as scoped — same lifetime as `TelemetrySummaryService`.

10. **`src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs`** — NEW. `public sealed class`. Constructor-injects `IOptionsMonitor<TenantEventRoutingOptions>`, `IObservedEventTypeStore`, `TimeProvider`. One public method:
    ```csharp
    public async Task<HandlerMismatchReport> DetectAsync(string tenantId, TimeSpan window, CancellationToken ct)
    ```
    Logic (pure-functional after the Redis read):
    - **UnhandledEventType detection:** `IObservedEventTypeStore.GetAllObservedTypesAsync` returns everything seen in the window. If a seen `(aggregateType, eventType)` has **no** routing-map entry pointing at this tenant for that aggregate type, emit `UnhandledEventType` mismatch with `Severity = Warning`, `Suggestion = "Add an EventStoreIntegration:Routing:SourceToTenantMap entry for source starting with '{aggregateType}' OR verify publisher is targeting the configured topic '{options.Topic}'."`. **Note:** because 9.1's routing maps `source` (not `aggregateType`) to tenant, "unhandled" here means "an `aggregateType` was seen that does not match any configured source prefix's aggregateType-naming convention" — document the inference in `docs/dev/eventstore-integration.md` §11.2.
    - **StaleHandler detection (canonical "stale" = `observedTypes.Count == 0` per Risk #2 unification — do NOT re-derive from counters):** For each `SourceToTenantMap` entry routed to this tenant, if the observed-types list returned by `GetAllObservedTypesAsync` is empty within the window, emit `StaleHandler` mismatch with `Severity = Info` (NOT Warning — "no events" is often the correct steady state for low-volume event streams, see Risk #2), `Suggestion = "Handler registered for source '{sourcePrefix}' but no events received in the last {windowHours}h — verify the publisher is online and targeting topic '{options.Topic}'. Low-volume publishers may legitimately go silent; set up a publisher-side heartbeat event if certainty matters."`.
    - **VersionMismatch detection:** Within each aggregateType's observed-types, split each `eventType` on `.` and operate the regex on the terminal segment only (`eventType.Split('.').Last()`) — this is the canonical algorithm (Fix #2). Group by the name-stem regex `^(.+?)(V\d+)$` applied to the terminal segment. If two distinct versions exist (e.g., both `ClaimSubmittedV2` and `ClaimSubmittedV3` observed — OR their FQ'd equivalents `MyApp.Claims.ClaimSubmittedV2` + `MyApp.Claims.ClaimSubmittedV3`) AND counts are non-zero for both, emit `VersionMismatch` mismatch with `Severity = Warning`, `Subject = "{stem}"` (the stem from the terminal segment, e.g., `"ClaimSubmitted"`), `Suggestion = "Multiple versions of '{stem}' observed ({versions}) — review whether all versions are intentional, or whether a publisher is emitting an old version."`. **Stem parsing** uses a compiled `Regex` with timeout=100ms (defense-in-depth against pathological regex inputs — Risk #5).
    - Emit `9132 HandlerMismatchDetected` at Information level once per detected mismatch with tags `severity` + `category`.
    - Return `HandlerMismatchReport` — `Mismatches = []` when nothing is wrong, NOT a 404.

11. **`src/Hexalith.Memories.Server/Program.cs`** — EDIT. Wire two minimal-API endpoints AFTER the existing `/api/tenants/{tenantId}/telemetry/summary` at :2906:
    - `app.MapGet("/api/handlers", async (HttpContext http, HandlerRegistryService svc, CancellationToken ct) => { http.Response.Headers.Append("X-Memories-API-Experimental", "HXL002"); return Results.Ok(await svc.GetSnapshotAsync(ct)); })` — no tenantId path segment because the registry is tenant-plural. **Version header (Delta #5)**: the `X-Memories-API-Experimental: HXL002` response header flags non-SDK consumers that this surface is experimental — paired with the `[Experimental("HXL002")]` compile-time warning on SDK callers, this gives raw-HTTP consumers visibility into the stability posture they silently miss today.
    - `app.MapGet("/api/tenants/{tenantId}/handlers/mismatches", async (HttpContext http, HandlerMismatchDetector detector, TenantStatusGuard guard, string tenantId, CancellationToken ct) => { http.Response.Headers.Append("X-Memories-API-Experimental", "HXL002"); var err = guard.ValidateTenantIdFormat(tenantId); if (err is not null) return Results.BadRequest(err); var tenantCheck = await guard.ValidateTenantActiveAsync(tenantId, ct); if (tenantCheck is not null) return Results.NotFound(tenantCheck); return Results.Ok(await detector.DetectAsync(tenantId, TimeSpan.FromHours(24), ct)); })` — mirror `TenantEndpointHandlers.GetTenantConfigurationAsync` validation shape at :70-100.
    - **AuthZ (Delta #12) — MUST be asserted at impl time, not assumed:** both endpoints fall under the existing server-wide authorization middleware pipeline, which requires the `RequireAuthorization("OperatorAccess")` policy (or equivalent as currently wired for `/api/tenants/{tenantId}/telemetry/summary`). Spike 0.5 (new — added in Task 0 section) verifies the policy name and adds `.RequireAuthorization("OperatorAccess")` to BOTH new endpoints explicitly — do NOT rely on implicit inheritance from a parent group mapping. The `/api/tenants/{tenantId}/handlers/mismatches` endpoint additionally needs tenant-scoping authZ (caller must be authorized FOR `tenantId`, not just globally) — re-use the exact claims check performed on `/api/tenants/{tenantId}/telemetry/summary`. Guard test `HandlerEndpointAuthorizationTests.UnauthenticatedRequest_Returns401` + `.AuthenticatedForDifferentTenant_Returns403` (Tier-2 integration, mirrors the existing tenant-scoped authZ tests).
    - Register in `AddMemoriesServer` (or the Program-level DI section): `services.AddScoped<HandlerRegistryService>()` + `services.AddScoped<HandlerMismatchDetector>()` + `services.TryAddSingleton<IObservedEventTypeStore, RedisObservedEventTypeStore>()` + `services.Configure<EventStoreObservationOptions>(configuration.GetSection("EventStoreIntegration:Observation"))` (Delta #14 — kill-switch binding).

12. **`src/Hexalith.Memories.Client.Rest/MemoriesClient.cs`** — EDIT. Add two methods mirroring `GetTelemetrySummaryAsync` at :587:
    ```csharp
    [Experimental("HXL002")]
    public virtual async Task<HandlerRegistrationSnapshot> ListHandlersAsync(CancellationToken ct)
    [Experimental("HXL002")]
    public virtual async Task<HandlerMismatchReport> GetHandlerMismatchesAsync(string tenantId, CancellationToken ct)
    ```
    Exact path strings: `"api/handlers"` and `$"api/tenants/{Uri.EscapeDataString(tenantId)}/handlers/mismatches"`. Use `MemoriesJsonContext.Options` for deserialization. Throw `MemoriesRemoteException` on non-2xx via `ErrorResponseDecoder.DecodeAsync`.

13. **`src/Hexalith.Memories.Cli/Commands/HandlersListCommand.cs`** — NEW. `public static class HandlersListCommand` mirroring `TenantListCommand` verbatim. `CommandName = "handlers list"` — ADR-7.3-002 command-name for JSON error envelopes. Suppresses `HXL002` at the call site with `#pragma warning disable HXL002` in the `ExecuteAsync` method only (Task 8.6). Empty-state nudge (when `snapshot.Handlers.Count == 0`): `"No handlers registered. Configure EventStoreIntegration:Routing:SourceToTenantMap in appsettings to bind CloudEvents sources to tenants. See docs/dev/eventstore-integration.md §11."`.

14. **`src/Hexalith.Memories.Cli/Commands/HandlersMismatchesCommand.cs`** — NEW. `public static class HandlersMismatchesCommand`. One **REQUIRED** `--tenant` option (mismatches are tenant-scoped). Optional `--severity` filter (`info`, `warning`, omit for both). `CommandName = "handlers mismatches"`. Empty-state nudge when `report.Mismatches.Count == 0`: `"No handler mismatches detected in the last 24h for tenant '{tenantId}' — this is the healthy state."`. When mismatches exist in HUMAN format, render one line per mismatch: `[{severity}] {category}: {subject} — {suggestion}` and set exit code to `CliExitCodes.Success` (this is a report, not a failure).

15. **`src/Hexalith.Memories.Cli/Output/OutputFormatters/`** — EDIT (add). Register a table formatter for `HandlerRegistrationSnapshot` (columns: `tenant`, `source`, `events (24h)`, `last event`, `event types`) and for `HandlerMismatchReport` (columns: `severity`, `category`, `subject`, `suggestion`). Human and JSON formatters come from the generic formatter-router; table needs the explicit column map.

16. **`src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`** — EDIT. At line 74, replace the `CommandGroups` entry `("handlers", "List registered event handlers.", "7.2")` with the REAL `handlers` command group (constant `HandlersCommandDescription`). Mirror the `statusCommand` wiring pattern at :120-124. Remove `handlers` from the `foreach` stub-loop at :145.

17. **`src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`** — EDIT. Add two instruments + tag-key policy:
    ```csharp
    public const string HandlersRegisteredName = "memories.handlers.registered";
    public const string HandlerMismatchesName = "memories.handlers.mismatches";
    // observable gauge: per-tenant count of registered sources
    // counter: per (tenant, severity) mismatch count
    ```
    Observable gauge registration happens inside `HandlerRegistryService` via `MemoriesMeter.EnsureObservableGaugesCreated` — BUT that method's shape only handles two observers (index size + queue depth); ADD a third observer parameter OR create a parallel `EnsureHandlerGaugeCreated(Func<IEnumerable<Measurement<int>>>)` helper (preferred — additive API, no behavior break for Story 7.5 call sites). Extend `MetricTagKeyPolicy`:
    ```csharp
    [HandlersRegisteredName] = new[] { "tenant_id" },
    [HandlerMismatchesName] = new[] { "tenant_id", "severity" },
    ```

18. **Docs — `docs/dev/eventstore-integration.md`.** ADD §11 "Handler registration & mismatch detection" with subsections: **§11.1** "Listing registered handlers" (HTTP + CLI examples, sample output, when to check it); **§11.2** "Mismatch categories" (each of the three with a real-world example — e.g., "When `MyApp.Claims.ClaimSubmittedV3` starts appearing but your subscription only routes `MyApp.Claims.ClaimSubmittedV2`, the detector reports this as a `VersionMismatch` warning"); **§11.3** "Observation window + staleness semantics" (24h default, why `StaleHandler` is Info not Warning, how the Redis store self-expires); **§11.4** "Troubleshooting flows" (two worked examples — "publisher changed topic" + "new event version rolled out without subscription update"). The existing §6 "Alerting recommendations" gets a new bullet pointing to `/api/tenants/{tenantId}/handlers/mismatches` as the operator's poke-during-investigation endpoint.

19. **Docs — `docs/dev/cli-config.md`.** ADD a `handlers` subsection listing `memories handlers list` + `memories handlers mismatches --tenant X [--severity warning]` with sample outputs in all three formats (human, json, table). Follow the existing `tenant list` / `status telemetry` documentation shape.

**What does NOT ship:**

- **New DAPR subscription discovery.** DAPR's `/dapr/subscribe` HTTP probe is the ONLY contract. This story reads the configured contract (from `TenantEventRoutingOptions`) — it does NOT enumerate DAPR sidecar state (no such API is stable in Dapr.Client 1.17.x). `SubscriptionStatus` is inferred from the in-process config, NOT the sidecar.
- **Handler CRUD.** No `POST /api/handlers` or `DELETE /api/handlers/...`. Handlers are declared in `appsettings.json` (`EventStoreIntegration:Routing:SourceToTenantMap`) — changing them is an operator-managed config deploy, not a runtime API. The runbook already exists in `docs/dev/eventstore-integration.md` §3.2 "Routing-config changes must quiesce the stream".
- **Per-handler pause / resume.** No "subscription paused" state exists in the Hexalith pipeline — events either flow or they don't. `HandlerSubscriptionStatus` does NOT have a `Paused` member for this reason.
- **Event-type REGISTRATION.** Nothing in 9.3 asks operators to enumerate expected event types up front. The detector INFERS expected types from `SourceToTenantMap` aggregateType conventions (`MyApp.Claims.ClaimSubmittedV2` → aggregateType `Claims`) — consistent with 9.1's lazy routing posture. A declarative "expected event types per tenant" schema is a future-story concern (phased: Story 10.x or beyond — out of scope).
- **Projection-registry cross-check.** The detector validates observed events against the ROUTING config (`SourceToTenantMap`), NOT against the set of projections/handlers the tenant's application code has bound at runtime. **Known gap:** an event can be "handled from routing's POV" (source prefix matches → tenant resolved → event accepted) while the tenant's application has NO projection bound for that `aggregateType` — the event is silently ignored downstream and this story will NOT report that as a mismatch. A declarative projection registry (attribute-scanned, reflection-verified) is the right future-story solution — logged as deferred work (`deferred-work.md` entry Story-9.3-ProjectionRegistryCrossCheck). Operators who need this today must use the audit-log + `AccessTelemetryLog` to correlate accepted events against downstream projection-write counts.
- **Cross-tenant mismatch detection.** Each `DetectAsync` call is scoped to ONE tenant. The operator multiplexes across tenants via `memories tenant list | jq | xargs memories handlers mismatches --tenant` — there is NO `/api/handlers/mismatches` plural endpoint. Reason: tenant isolation (Epic 5) — mismatches cannot leak between tenants.
- **Historical mismatch archive.** Mismatches are computed on-demand from the rolling 24h observation store. No durable "mismatch ledger" — if the operator needs a ledger, the `AccessTelemetryLog` audit stream already carries `cloudEventType` after this story ships.
- **Automatic remediation.** `Suggestion` strings are read-only. No "auto-register missing handler" button. Remediation is explicitly a config-deploy action (ADR 9.1-C).
- **Redis-absence graceful degradation beyond observation writes.** The `HandlerRegistryService` read path REQUIRES Redis availability. On `RedisConnectionException`, the endpoint returns 500 + `ErrorResponse("REDIS_UNAVAILABLE", "Observation store is unreachable", "Check Redis connectivity via the health endpoint.")`. This is the same posture as `GET /api/tenants/{tenantId}/telemetry/summary`.
- **Observable gauge for observation-store backlog.** The observation store self-expires via Redis `EXPIRE`; there is no monotonically-growing backlog to observe. `memories.handlers.registered` + `memories.handlers.mismatches` are the only new instruments.
- **Telemetry metric integration tests beyond tag-policy pins.** The Tier-2 integration tests exercise the HTTP + CLI read path; metric emission is unit-tested via `MeterListener` in `tests/Hexalith.Memories.Server.Tests/Handlers/` — no Tier-2 OTLP assertion needed (Story 8.4's Tier-3 path covers OTEL span plumbing; 9.3's metrics ride the same pipeline without needing a dedicated proof).

**Primary risks:**

1. **Observation store write amplification doubles Redis RPS on the ingestion hot path.** Every `EventIngestionService` `Accepted`/`Duplicate` outcome now fires a pipelined 2-command Redis write (`ZADD` + `HINCRBY`). At 500 events/sec per tenant, that's 1000 Redis commands/sec added ON TOP OF the existing preflight-dedup `SETNX` — effectively a 3x Redis RPS increase on the EventStore hot path. **Mitigation:** (a) Batch-pipeline the two commands via `IDatabase.CreateBatch()` → one round-trip (verified via `StackExchange.Redis.Batch` pattern, see `RedisAggregateCaseMappingStore` for reference on connection-reuse via `[FromKeyedServices("redis")]`); (b) fail-open on write exception (log 9140 Warning, return — the ingestion path MUST NOT block on observation-store failures); (c) add a deployment-time RedisLatencyAlert in `docs/dev/eventstore-integration.md` §6 ("observation-store write p95 > 10ms over 5 min"); (d) `RedisObservedEventTypeStoreTests.BatchedWrite_ExecutesInSingleRoundTrip` guard test.

2. **"StaleHandler" is Info — operators may not act on low-volume legitimate silence.** An event stream that sees one event per week is NOT stale, but a mismatch detector applying a 24h window will call it stale every Monday. **Canonical definition of "stale" (unified — see §Dev Notes "Stale phrasing unification"):** the `IObservedEventTypeStore.GetAllObservedTypesAsync(tenantId, TimeSpan.FromHours(24))` result is an **empty list** for a `SourceToTenantMap` entry routed to this tenant. No counter comparison, no "zero events" arithmetic — the check is literally `observedTypes.Count == 0`. Use this exact phrasing in §10 spec, AC #4, and every test-name comment. **Mitigation:** (a) hard-pin `Severity = Info` (not Warning) for StaleHandler so no paging rule would ever fire on it; (b) `Suggestion` copy explicitly acknowledges the low-volume case: `"... in the last 24h — verify the publisher is online and targeting topic '{options.Topic}'. Low-volume publishers may legitimately go silent; set up a publisher-side heartbeat event if certainty matters."`; (c) document the Info severity choice in `docs/dev/eventstore-integration.md` §11.2 + in **ADR-9.3-005 (mismatch severity is 2-valued; Error absent)** — see Task 11.11; (d) guard test `HandlerMismatchDetectorTests.EmptyObservedTypes_EmitsInfoSeverity_NotWarning`; (e) **future consideration** — Dev Notes logs an operator-configurable `--since` CLI flag (Story 10.x or beyond) to widen the window for low-volume tenants so the false-positive rate becomes tunable per inspection.

3. **Empty-observed-types UX dead-zone.** If a tenant has a `SourceToTenantMap` entry but no events have been observed yet (new tenant, warm-up period), `EventTypePatterns` is empty → the CLI table renders blank columns. Operators will read this as "handler broken" when it's actually "handler working, hasn't been exercised yet". **Mitigation:** (a) `HandlerRegistryService` synthesizes `EventTypePatterns = ["(none observed in last 24h)"]` (single-element sentinel list) instead of `[]` when observed-types is empty — the sentinel string is rendered verbatim in the table, is UNAMBIGUOUSLY not a real event type (the parens disambiguate), and guarantees every row has content; (b) the JSON format returns `[]` (data purity) — the sentinel is a PRESENTATION concern handled in the `OutputFormatters/HandlerRegistrationTableFormatter.cs`; (c) Risk-adjusted: the human format should show the sentinel (readability); the JSON format shows `[]` (API purity); (d) guard tests `HandlerRegistryServiceTests.EmptyObservedTypes_ReturnsEmptyArrayInJson` + `HandlerRegistrationTableFormatterTests.EmptyEventTypes_RendersSentinel`.

4. **Metric tag-cardinality explosion.** `memories.handlers.mismatches` with tag `severity` is low-cardinality (2 values). `memories.handlers.registered` with tag `tenant_id` scales linearly with tenant count — acceptable for single-digit to hundreds but not thousands. **Mitigation:** (a) pin the tag policy in `MemoriesMeter.MetricTagKeyPolicy` (Story 7.5 pattern — `case_id` and `user` are explicitly excluded per Risk #1 of 7.5); (b) document the tenant-cardinality expectation in `docs/dev/telemetry.md` (cross-link with the existing Story 7.5 section); (c) guard test `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys` extended to cover the new instruments (this test already exists — just adding rows catches drift); (d) **runtime cardinality smoke test (Delta #4)** — new Tier-2 integration test `HandlerMetricsCardinalitySmokeTests.MismatchesMetric_DistinctTagValuesStayBounded`: publishes 200 CloudEvents across 50 distinct `(category, severity)` mismatch combinations, collects emitted metrics via a `MeterListener`, asserts that the distinct-tag-value count on `memories.handlers.mismatches` equals `2 (severities) × 3 (categories) = 6` — never more, never growing with event volume. Complements (c)'s structural check with runtime behavior verification; (e) **future consideration (Delta #4 follow-up)** — at N ≥ 1000 tenants, switch `memories.handlers.registered` from `tenant_id`-tagged gauge to a bucketed summary (0/1-10/10-100/100+ tenants); deferred until a real deployment approaches that tenant count — logged as `deferred-work.md` entry Story-9.3-TenantCardinalityBucketing.

5. **Regex timeout on pathological event-type names.** The `VersionMismatch` detector uses a compiled regex `^(.+?)(V\d+)$`. A malicious publisher could send an event type with 10k characters and a crafted structure to ReDoS the detector. **Mitigation:** (a) Use `RegexOptions.Compiled | RegexOptions.CultureInvariant` with `MatchTimeout = TimeSpan.FromMilliseconds(100)` — Regex is already non-backtracking-intense (`.+?` + `V\d+` is linear), so 100ms is >1000x the expected worst case; (b) on `RegexMatchTimeoutException`, skip the mismatch for THAT type and emit **`9141 RegexSkippedForPathologicalEventType`** at Warning (Fix #8 — dedicated event id, distinct from `9140` which is reserved for Redis observation-store write failures); (c) cap input length at `eventType.Length <= 256` BEFORE the regex runs (CloudEvents `type` is 253 chars max per RFC — any value over that is already suspect) — also emits `9141` with `reason = "length_exceeded"`; (d) guard test `HandlerMismatchDetectorTests.EventTypeOver256Chars_SkippedFromVersionMismatch_EmitsWarning` + `.RegexTimeout_DoesNotThrow_LogsWarning_With9141`.

6. **`CommandGroups` stub-loop removal is load-bearing.** Leaving `("handlers", ..., "7.2")` in `CommandGroups` while ALSO registering a real `handlersCommand` results in a duplicate-subcommand-registration exception at Parse time (System.CommandLine throws). **Mitigation:** (a) Remove the entry from `CommandGroups` in the SAME commit as adding the real command — zero-window; (b) `RootCommandFactoryTests.RootCommand_HasHandlersSubcommand_ButNoStub` — guard test that (i) `root.Subcommands` contains a `handlers` command, (ii) `NotImplementedCommand.IsStub(handlersCmd) == false`, (iii) `handlersCmd.Subcommands.Count >= 2` (list + mismatches); (c) also guard `RootCommandFactoryTests.NoCommandGroupIsRegisteredBothAsRealAndStub` scanning `CommandGroups` ∩ `root.Subcommands.Where(c => !NotImplementedCommand.IsStub(c))` returning empty set.

7. **Rolling 5m store vs rolling 24h store substrate confusion.** Story 7.5 introduced `RollingCounterStore` with pinned 5m/5-slot/1m-bucket shape. A future developer might extend that store to cover handler observation to "save code", breaking the 5m telemetry ring's tight invariants. **Mitigation:** (a) `IObservedEventTypeStore` is a SEPARATE interface in a SEPARATE folder — no tempting "just extend RollingCounterStore" path; (b) `docs/dev/telemetry.md` gets a note: "Handler observation uses a Redis-backed 24h store — intentionally separate from the 5-min in-process counter store so neither evolves to compromise the other's invariants"; (c) ADR addendum to the telemetry ADR (-003) naming the separation decision.

8. **`EventIngestionTelemetryAdapter` fire-and-forget observation records can lose data on sidecar restart OR backpressure the thread pool under slow-Redis conditions.** The observation-record task is NOT awaited in the request path (by design — Risk #1). Two failure modes: (i) process restart between accepted-and-responded and the observation write → observation lost; (ii) degraded Redis (p99 latency = 5s) → unbounded fire-and-forget backlog because `CancellationToken.None` grants infinite lifetime → thread pool saturation → ingestion p99 spikes. **Mitigation:** (a) The ingestion AUDIT LOG via `AccessTelemetryLog.LogIngestAccess` IS awaited and is durable — the observation store is a PERFORMANCE-OPTIMIZED READ CACHE for the mismatch detector, not source-of-truth; (b) document this explicitly in `docs/dev/eventstore-integration.md` §11.3: "Observation store is eventually-consistent with the audit log; losses under restart are tolerable because the detector's rolling window (24h) is long enough to re-populate from steady traffic"; (c) **bounded-FAF (Delta #3 — addresses failure mode (ii)):** the fire-and-forget call is wrapped in a process-wide `SemaphoreSlim(256)` acquire-with-`WaitAsync(5ms)` AND a 2-second `CancellationTokenSource` timeout — see "What 9.3 adds" #6 for the precise pattern. Dropped observations increment `memories.handlers.observations.dropped` (new counter, tag `reason ∈ {"backpressure", "timeout", "redis_error"}`) so operators see the degradation in Grafana before ingestion latency regresses. Guard test `EventIngestionTelemetryAdapterTests.SlowRedis_DropsObservation_WithinTwoSeconds_DoesNotBlockIngestion`; (d) **kill switch (Delta #14 — addresses both failure modes):** `EventStoreIntegration:Observation:Enabled = false` in `appsettings.json` disables the fire-and-forget call entirely — an operator's escape hatch for incidents. Emits `9143 ObservationWritesDisabledByConfig` at Information (once at startup) so the disabled state is auditable; (e) optional future follow-up: rebuild-store-from-audit-log on startup (NOT in scope for 9.3).

9. **Cross-tenant observation-store key contamination.** All observation keys are prefixed with `{tenantId}:eventstore:observed:*`. A bug that synthesizes a wrong tenantId (e.g., `__rejected__` — the telemetry placeholder at `MemoriesMeter.RejectedTenantTag`) could write data under the placeholder prefix, polluting a non-tenant namespace that tenant deletion never cleans. **Mitigation:** (a) Task 6 gates on `tenantId != MemoriesMeter.RejectedTenantTag && !string.IsNullOrEmpty(tenantId) && outcome is Accepted or Duplicate` BEFORE calling the store; (b) guard test `EventIngestionTelemetryAdapterTests.RejectedTenantTag_DoesNotWriteToObservationStore`; (c) `RedisObservedEventTypeStoreTests.AssertTenantIdIsNeverRejectedTag` — defense-in-depth at the store level too (ArgumentException on `tenantId == "__rejected__"`).

10. **AOT serialization omission for new Contract types.** Forgetting to register `HandlerRegistrationSnapshot` in `MemoriesJsonContext` would cause runtime `NotSupportedException` on AOT-published binaries. **Mitigation:** (a) Add `[JsonSerializable(typeof(HandlerRegistrationSnapshot))]` etc. in the SAME commit as creating the type; (b) `MemoriesJsonContextCompletenessTests.AllContractTypes_AreRegistered` (a pattern from Story 9.2's Task 1.9) reflects over `Hexalith.Memories.Contracts.V1` public types and asserts each has a corresponding `[JsonSerializable]` attribute. **Note:** if that test doesn't exist yet (verify at impl time), this story BLOCKS on adding it — one hour of work that protects future stories too.

**Risk → Guard test mapping:**

| # | Risk | Guard test |
|---|------|-----------|
| 1 | Observation-store write RPS amplification | `RedisObservedEventTypeStoreTests.BatchedWrite_ExecutesInSingleRoundTrip` + `RedisObservedEventTypeStoreTests.WriteFailure_LogsWarningAndReturns` |
| 2 | StaleHandler Info-severity semantics | `HandlerMismatchDetectorTests.EmptyObservedTypes_EmitsInfoSeverity_NotWarning` + `HandlerMismatchDetectorTests.StaleHandlerSuggestion_ContainsLowVolumeCaveat` |
| 3 | Empty-observed-types UX dead-zone | `HandlerRegistryServiceTests.EmptyObservedTypes_ReturnsEmptyArrayInJson` + `HandlerRegistrationTableFormatterTests.EmptyEventTypes_RendersSentinel` |
| 4 | Metric tag cardinality discipline | `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys` (extend) |
| 5 | Regex ReDoS on event-type names | `HandlerMismatchDetectorTests.EventTypeOver256Chars_SkippedFromVersionMismatch_EmitsWarning` + `HandlerMismatchDetectorTests.RegexTimeout_DoesNotThrow_LogsWarning` |
| 6 | CommandGroups stub-vs-real collision | `RootCommandFactoryTests.RootCommand_HasHandlersSubcommand_ButNoStub` + `RootCommandFactoryTests.NoCommandGroupIsRegisteredBothAsRealAndStub` |
| 7 | 5m vs 24h substrate confusion | Doc-only (ADR addendum) + code-comment guard in `IObservedEventTypeStore` interface |
| 8 | Fire-and-forget observation loss | **(Fix #7)** Property-based `HandlerRegistryIntegrationTests.ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` — injects a configurable `dropProbability ∈ [0, 0.5]` at the `IObservedEventTypeStore` wrapper, asserts the detector still reports ≥ `(1 - dropProbability) × expected` observations within `2 × window` of steady traffic. Complements (does NOT replace) the existing audit-log durability check. |
| 9 | Cross-tenant key contamination | `EventIngestionTelemetryAdapterTests.RejectedTenantTag_DoesNotWriteToObservationStore` + `RedisObservedEventTypeStoreTests.RejectedTenantTag_ThrowsArgumentException` |
| 10 | AOT Contract type registration | `MemoriesJsonContextCompletenessTests.AllContractTypes_AreRegistered` (create if missing) |
| AC #1 | Enumerated registered handlers with counts + timestamps | `HandlerRegistryServiceTests.ReturnsOneRegistrationPerSourceToTenantMapEntry_WithAggregatedCounts` |
| AC #2 | UnhandledEventType mismatch | `HandlerMismatchDetectorTests.ObservedTypeNotInRoutingMap_ReportedAsUnhandled_Warning` |
| AC #3 | StaleHandler mismatch | `HandlerMismatchDetectorTests.RegisteredSourceWithZeroEvents_ReportedAsStale_Info` |
| AC #4 | VersionMismatch detection | `HandlerMismatchDetectorTests.MultipleVersionsSameStem_ReportedAsVersionMismatch_Warning` |
| AC #5 | CLI categorization + suggestion + severity | `HandlersMismatchesCommandTests.HumanFormat_ShowsSeverityCategorySubjectSuggestion` |
| AC #6 | Handler list + mismatches CLI end-to-end | `HandlersListIntegrationTests.CliListCommand_ExitsZero_WithRegisteredHandlersInJsonFormat` + `HandlersMismatchIntegrationTests.CliMismatchesCommand_OnHealthyTenant_ReportsZeroMismatches` |

---

## Story

As a developer,
I want to list registered event handlers and detect mismatches,
so that I can verify my event-sourced system is fully integrated and catch configuration drift before it causes silent data loss or silent over-ingestion.

## Acceptance Criteria

1. **Given** the Memories Server has one or more `EventStoreIntegration:Routing:SourceToTenantMap` entries configured AND `EventStoreIntegration:Routing:Topic` is non-empty **When** an operator calls `GET /api/handlers` **Then** the response is a `HandlerRegistrationSnapshot` with `PubSubName = "pubsub"`, `Topic` = the configured topic, `SubscriptionStatus = Active` (per the canonical rule in "What 9.3 adds" #7 — Fix #1), `AsOf` = ISO-8601 current UTC, and `Handlers` containing ONE `HandlerRegistration` per `SourceToTenantMap` entry whose routed tenant is NOT deleted/deleting. Each `HandlerRegistration` carries: `TenantId` + `SourcePrefix` + `EventTypePatterns` (distinct `aggregateType` values observed in last 24h — **always `[]` when empty** at the service/JSON layer per Fix #3; the CLI table formatter renders `[]` as the sentinel `"(none observed in last 24h)"`, but the data surface is `[]`) + `EventsProcessedCount` (sum of observed-type counts in the 24h window) + `LastEventAt` (ISO-8601 of the most-recent observation, or `null` when none) + `ObservedEventTypes` (array of `ObservedEventTypeSummary(AggregateType, EventType, Count, LastSeenAt)`).

2. **Given** `EventStoreIntegration:Routing:Topic` is empty (pre-9.1 bootstrap or deliberately-disabled deployment) **When** `GET /api/handlers` is called **Then** the response has `SubscriptionStatus = Disabled` AND `Handlers = []` AND returns 200 OK — **not** a 5xx or 404 (disabled is a valid operational state, not an error).

3. **Given** an operator calls `GET /api/tenants/{tenantId}/handlers/mismatches` for a tenant that has observed at least one event type in the last 24h for an aggregateType NOT covered by any `SourceToTenantMap` entry routed to this tenant **When** the detector runs **Then** the response includes a `HandlerMismatch` with `Category = UnhandledEventType`, `Severity = Warning`, `Subject = "{aggregateType}/{eventType}"`, a non-empty `Context` describing where it was observed, and a `Suggestion` string containing an `EventStoreIntegration:Routing:SourceToTenantMap` action keyword. Log event `9132 HandlerMismatchDetected` is emitted at Information level with tag `category = UnhandledEventType`.

4. **Given** the same endpoint **When** for a `SourceToTenantMap` entry routed to this tenant the `IObservedEventTypeStore.GetAllObservedTypesAsync` result is an empty list within the 24h window (the canonical "stale" condition — see Risk #2 unified definition) **Then** the response includes a `HandlerMismatch` with `Category = StaleHandler`, `Severity = Info` (**NOT Warning** — Risk #2), `Subject = "{sourcePrefix}"`, and a `Suggestion` whose text includes the phrase "low-volume publishers may legitimately go silent" so operators reading the suggestion understand why severity is Info.

5. **Given** the same endpoint **When** within a tenant's observed event-types the detector finds two or more variants sharing a stem but differing only in a trailing `V\d+` suffix (e.g., `ClaimSubmittedV2` and `ClaimSubmittedV3`) with non-zero counts for at least two variants **Then** the response includes a `HandlerMismatch` with `Category = VersionMismatch`, `Severity = Warning`, `Subject = "{stem}"`, and a `Suggestion` containing both version strings and the phrase `review whether all versions are intentional`.

6. **Given** an operator runs `memories handlers list --format json` **When** the CLI successfully resolves `--endpoint` and `--token` **Then** it prints the `HandlerRegistrationSnapshot` as JSON to stdout, exits with `CliExitCodes.Success`, and writes nothing to stderr. The JSON format's `ObservedEventTypes` field is `[]` when empty (**NOT** the human-format sentinel — Risk #3 presentation-vs-data separation) and `EventTypePatterns` is `[]` (data purity).

7. **Given** an operator runs `memories handlers list --format table` on a stack with zero `SourceToTenantMap` entries **When** the CLI prints the table **Then** the output includes the header row AND a stderr nudge line: `"No handlers registered. Configure EventStoreIntegration:Routing:SourceToTenantMap in appsettings to bind CloudEvents sources to tenants. See docs/dev/eventstore-integration.md §11."`. Exit code: `CliExitCodes.Success` (empty state is not an error — ADR-7.2-002 parity with `memories tenant list`).

8. **Given** an operator runs `memories handlers mismatches --tenant acme --severity warning` **When** the server reports 2 Warning + 3 Info mismatches **Then** the CLI output in HUMAN format contains the 2 Warning mismatches only, formatted as `[warning] {category}: {subject} — {suggestion}` one per line, exit code `CliExitCodes.Success`. In JSON format, the full 5-mismatch `HandlerMismatchReport` is returned unfiltered (server-side filter is CLI-render-only — the JSON API is unfiltered so downstream consumers can filter themselves).

9. **Given** a call to `GET /api/tenants/{tenantId}/handlers/mismatches` for a tenant that does NOT exist in `TenantRegistryService` **When** the endpoint runs its guard **Then** it returns HTTP 404 + `ErrorResponse(Code: "TENANT_NOT_FOUND", Message: "Tenant '{tenantId}' is not registered.", Suggestion: "Use GET /api/tenants to list available tenants.")` — same shape as `GET /api/tenants/{tenantId}` at `Program.cs:874`.

10. **Given** `EventIngestionService.ProcessAsync` runs with a valid CloudEvents envelope **When** the outcome is `Accepted` (R3-8 semantic fix: NOT `Duplicate` — Duplicate means the event was previously recorded; counting again would inflate `EventsProcessedCount` by retry volume) AND the mapper produced non-null `aggregateType` + non-null `cloudEventType` AND `tenantId != "__rejected__"` **Then** the Server-side `EventIngestionTelemetryAdapter.RecordIngestion` writes a new observation record to `{tenantId}:eventstore:observed:{aggregateType}` (sorted set, score = unix ms) AND increments `{tenantId}:eventstore:observed-count:{aggregateType}:{eventType}` — both via a SINGLE batched Redis round-trip. If the Redis write fails, log `9140 ObservedEventTypeStoreWriteFailed` at Warning level and RETURN — the ingestion outcome is NOT affected (Risk #8 fire-and-forget posture).

11. **Given** `EventIngestionService` completes a request with outcome `Duplicate` (R3-8), `UnknownSource`, `TenantNotFound`, `TenantProvisioning`, `TenantDeleting`, `AutoCreateDisabled`, `CaseCapExceeded`, `InvalidCloudEvent`, or `ScheduleFailed` **When** `RecordIngestion` is called **Then** the observation store is NOT written. Only `Accepted` outcomes record observations (R3-8 narrowed from the prior "Accepted or Duplicate" after a Chaos Monkey round revealed that Duplicate-counting over-counted on retry storms). Guard test `EventIngestionTelemetryAdapterTests.DuplicateOutcome_DoesNotWriteObservation_R3Dash8`.

12. **Given** `MemoriesMeter.MetricTagKeyPolicy` is audited **When** `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys` runs **Then** it finds `memories.handlers.registered` with expected tag keys `["tenant_id"]` AND `memories.handlers.mismatches` with expected tag keys `["tenant_id", "severity"]`. The observable gauge for `memories.handlers.registered` is registered exactly once via `MemoriesMeter.EnsureHandlerGaugeCreated(...)` (new helper, parallel to the existing `EnsureObservableGaugesCreated`).

13. **Given** the CLI root command is built **When** `RootCommandFactoryTests.RootCommand_HasHandlersSubcommand_ButNoStub` runs **Then** `root.Subcommands` contains exactly one command named `"handlers"`, `NotImplementedCommand.IsStub(handlers) == false`, AND `handlers.Subcommands.Select(c => c.Name)` equals `["list", "mismatches"]`. No duplicate `handlers` command is registered from the `NotImplementedCommand` stub loop (Risk #6 regression guard).

14. **Given** all `Hexalith.Memories.Contracts.V1` types are scanned **When** `MemoriesJsonContextCompletenessTests.AllContractTypes_AreRegistered` runs (create the test if it does not exist yet — Risk #10) **Then** `HandlerRegistrationSnapshot`, `HandlerRegistration`, `ObservedEventTypeSummary`, `HandlerMismatchReport`, `HandlerMismatch`, `HandlerMismatchCategory`, `HandlerMismatchSeverity`, and `HandlerSubscriptionStatus` all have corresponding `[JsonSerializable]` attributes in `MemoriesJsonContext`.

15. **Given** a Tier-2 integration test `HandlersListIntegrationTests` runs against the Aspire AppHost fixture with Redis + a populated `SourceToTenantMap` of `{"acme.events"->"acme-tenant"}` **When** 5 CloudEvents with `source = "acme.events/claims"` and mixed `type` values (`MyApp.Claims.ClaimSubmittedV2` ×3, `MyApp.Claims.ClaimApprovedV2` ×2) are published to the pubsub topic and 2 seconds pass **Then** `memories handlers list --format json` shows ONE registration with `EventsProcessedCount = 5`, `ObservedEventTypes.Length = 2`, and `LastEventAt` within 3 seconds of the test's clock.

16. **Given** the same fixture + a `ClaimSubmittedV3` is published in addition to the V2 events **When** `memories handlers mismatches --tenant acme-tenant` runs **Then** at least ONE `VersionMismatch` with `subject = "ClaimSubmitted"` is present (stem extraction: `ClaimSubmittedV2` + `ClaimSubmittedV3` → stem `ClaimSubmitted`). Exit code 0. In human format, the output line starts with `[warning] versionMismatch: ClaimSubmitted —`.

17. **Given** `docs/dev/eventstore-integration.md` is updated **When** a developer reads the new §11 sections **Then** they find: §11.1 "Listing registered handlers" (HTTP + CLI examples with sample outputs, when to check); §11.2 "Mismatch categories" (one worked example per category); §11.3 "Observation window + staleness semantics" (24h default, Info-vs-Warning rationale, Redis self-expire); §11.4 "Troubleshooting flows" (two worked examples). Guard: `DocumentationCompletenessTests.EventStoreIntegrationDoc_Has93Sections`.

18. **Given** `TreatWarningsAsErrors=true` applies to every project **When** `dotnet test` runs the full solution **Then** no `HXL002`-marked call in `src/` leaks a warning — suppression is scoped to the CLI command files via `#pragma warning disable HXL002` at the narrow call site only (NOT at the project level). `ProjectCompilationTests.Cli_SuppressesHxl002WarningOnlyAtNarrowCallSite` (pattern parallel to Story 9.2 Task 1.9 guard).

19. **Given** `memories handlers list --format human` **When** an operator runs it against a healthy stack **Then** stdout contains (a) ONE line per registered handler in format `{tenantId} {sourcePrefix} events={count} last={lastEventAt or 'never'} types={observedTypes or '(none observed in last 24h)'}`, (b) a trailing empty line, (c) nothing on stderr. ADR-7.2-002 byte-for-byte stability is maintained — the human format is stable across releases within Phase 1.5.

20. **Given** a warm Redis connection + pre-populated observation store for N=100 tenants each with 5 distinct `(aggregateType, eventType)` observations within the window **When** `GET /api/handlers` is called 50 times back-to-back via an in-process HTTP client **Then** the p95 response time is ≤ 500ms measured at the server. Similarly, `GET /api/tenants/{tenantId}/handlers/mismatches` across those 100 tenants (one call per tenant) has p95 ≤ 200ms (smaller bound because detector reads ONE tenant per call). NFR guard test `HandlerEndpointLatencyNfrTests.GetHandlers_AtN100Tenants_P95Under500Ms` runs in the integration tier against the Aspire fixture — if it regresses, revisit the `Task.WhenAll` concurrency pattern in `HandlerRegistryService`. (Delta #13 — without this bound, Tier-2 could pass on a dev laptop and regress in prod.)

21. **Given** `EventStoreIntegration:Observation:Enabled = false` in `appsettings.json` **When** the Server starts AND processes events **Then** (a) at startup, log event `9143 ObservationWritesDisabledByConfig` is emitted once at Information level; (b) NO observation writes are issued to Redis regardless of outcome; (c) `GET /api/handlers` still returns a valid snapshot but all `EventsProcessedCount = 0` and `ObservedEventTypes = []` (consistent with "no observations recorded"); (d) `memories.handlers.observations.dropped` counter does NOT increment (writes are skipped, not dropped). (Delta #14 — operator kill switch.)

22. **Given** the Server is under degraded-Redis conditions where observation writes exceed 2s **When** ingestion runs **Then** (a) the ingestion response latency is UNAFFECTED (fire-and-forget drop path engages); (b) `memories.handlers.observations.dropped` counter increments with tag `reason in {"backpressure","timeout","redis_error"}`; (c) guard test `EventIngestionTelemetryAdapterTests.SlowRedis_DropsObservation_WithinTwoSeconds_DoesNotBlockIngestion` holds end-to-end ingestion p95 below 50ms while observation writes are simulated at 5s latency via a stub `IObservedEventTypeStore`. (Delta #3 — bounded FAF contract.)

23. **Given** the new `HandlerRegistrationSnapshot` or `HandlerMismatchReport` C# type **When** it is serialized on the Server AND deserialized through `MemoriesClient.ListHandlersAsync` / `GetHandlerMismatchesAsync` via a mocked `HttpMessageHandler` in `MemoriesClientHandlersContractTests.cs` **Then** the round-tripped object is structurally equal to the source instance via `BeEquivalentTo`, AND the intermediate JSON bytes render enums as camelCase (e.g., `"unhandledEventType"`). The test proves SDK+Server contract compatibility at `dotnet build` time without requiring the Tier-2 Aspire fixture (Fix #6 — Murat's consumer-driven contract test, hoisted to AC level to make the contract pin explicit).

24. **Given** both endpoints `GET /api/handlers` and `GET /api/tenants/{tenantId}/handlers/mismatches` **When** any caller receives a 2xx response **Then** the HTTP response headers include `X-Memories-API-Experimental: HXL002` — non-SDK consumers (raw-HTTP integrators, cURL users) have visibility of the experimental-surface contract. (Delta #5 — compile-time `[Experimental]` attribute does not help raw-HTTP callers.)

25. **Given** both endpoints **When** an unauthenticated request is made **Then** the response is `401 Unauthorized`. **Given** an authenticated request for `tenantId = "other-tenant"` on the mismatches endpoint **When** the caller lacks authorization for that tenant **Then** the response is `403 Forbidden`. Both endpoints explicitly call `.RequireAuthorization("OperatorAccess")` in Program.cs wiring (per Spike 0.5 finding) — implicit inheritance is NOT relied upon. Guard tests `HandlerEndpointAuthorizationTests.UnauthenticatedRequest_Returns401` + `.AuthenticatedForDifferentTenant_Returns403_OnMismatchesEndpoint`. (Delta #12 — authZ contract made explicit.)

26. **Given** an adversarial scenario where Redis server time is manually reset backwards by 2 hours **When** new observation records are written (with `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` scores) AND the detector subsequently reads via `ZRANGEBYSCORE` **Then** the detector still returns only observations within the intended 24h window — the store uses a single time source consistently on both write and read paths so absolute server-clock correctness is not load-bearing. Guard test `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` — simulates skew via a `TimeProvider` fake on the writer while the reader uses the same fake. (Finding N — clock-skew failure mode.)

27. **Given** the server is serving `GET /api/handlers` for N=100 tenants AND one specific tenant's `GetAllObservedTypesAsync` Redis call throws `RedisConnectionException` **When** the endpoint computes the snapshot **Then** the response is HTTP 200 with the snapshot containing 99 healthy `HandlerRegistration` rows AND ONE row for the failing tenant with `tenantId` set, `EventsProcessedCount = 0`, `ObservedEventTypes = []`, `EventTypePatterns = []`, AND a new `Error` field on the row set to `"OBSERVATION_READ_FAILED"`. The endpoint MUST NOT return 500 because ONE tenant's backend call failed. Guard test `HandlerRegistryServiceTests.PartialTenantFailure_ReturnsPartialSnapshot_NotFiveHundred`. (Finding S — graceful degradation.) Schema impact: `HandlerRegistration` gains an optional `Error` property — `null` in the happy path.

28. **Given** a successful call to `GET /api/tenants/{tenantId}/handlers/mismatches` **When** the response is deserialized **Then** the `HandlerMismatchReport` includes a `Summary` property with `{ routesConfigured: int, observationsChecked: int }` (R3-4 dropped `categoriesExamined`) — operators see what the detector actually examined, not just what it found. The `HasWarnings` + `HasInfo` computed properties are accessible on the SDK shape (either as getters or helpers) for automated-monitor short-circuiting. Empty-mismatches response still returns a `Summary` populated with non-zero `routesConfigured` + `observationsChecked` — positive-confirmation UX. (Findings EE, L.)

29. **Given** the Server is started AND the minimal-API routing table is frozen **When** `EndpointRoutingTests.HandlersEndpointsAreReachable` runs **Then** it asserts via `Endpoints.OfType<RouteEndpoint>()` enumeration that (a) exactly one endpoint exists with pattern `/api/handlers` GET, (b) exactly one endpoint exists with pattern `/api/tenants/{tenantId}/handlers/mismatches` GET, AND (c) neither endpoint is shadowed by a catch-all route. Prevents silent route-priority regressions if a future story adds a catch-all ahead of these. (Finding U.) Also verifies (d) `MemoriesClient` uses the exact path strings `api/handlers` + `api/tenants/{tenantId}/handlers/mismatches` (no embedded version segment beyond the current convention) — Guard `MemoriesClientPathConstantTests.PathStringsMatchServerRoutes`. (Finding V.)

30. **Given** `memories handlers mismatches --tenant X --exclude-stale` **When** the report contains mixed Info (StaleHandler) + Warning mismatches **Then** the CLI output contains only non-StaleHandler entries, exit code 0. **Given** `--only-warning` (shorthand equivalent to `--severity warning`) **When** the report is mixed **Then** only Warning entries render. **Given** `--no-wrap` on a table format **When** column content would wrap **Then** content is truncated with an ellipsis and full content is available in JSON format. (Findings B, X. R3-2 dropped `--format explain` clause.)

## Tasks / Subtasks

### Pre-Impl Verification Spikes (MUST complete before dependent tasks start)

- [ ] **Spike 0.1 — `MemoriesJsonContextCompletenessTests` existence (blocks Task 2 + Task 9) [Risk #10].** Verify whether `tests/Hexalith.Memories.Contracts.Tests/MemoriesJsonContextCompletenessTests.cs` exists (Story 9.2 spec mentioned it). If YES, extend it to include the 8 new types from Task 2. If NO, CREATE it in this story — uses reflection over `typeof(MemoriesJsonContext).Assembly.GetTypes()` filtered to `public + record/enum + under V1 namespace` compared against `MemoriesJsonContext` attribute-registered types. Spike deliverable: 1 paragraph in Dev Notes confirming existence + location + test methodology.

- [ ] **Spike 0.2 — `StackExchange.Redis` batch API shape (blocks Task 1.4) [Risk #1].** Verify the exact batched-pipeline call pattern for `ZADD` + `HINCRBY` on the same key family. Candidates: `IDatabase.CreateBatch()` + `batch.SortedSetAddAsync(...)` + `batch.HashIncrementAsync(...)` + `batch.Execute()`. Spike: check the existing `RedisPreflightDedupStore` + `RedisAggregateCaseMappingStore` for the idiomatic usage in this codebase — match their shape. Spike deliverable: the exact C# pattern in Dev Notes "Observation-store Redis batch shape".

- [ ] **Spike 0.3 — `TenantStatusGuard.ValidateTenantIdFormat` + `ValidateTenantActiveAsync` return shapes (blocks Task 6.2) [DI contract].** Check the exact return types + null semantics of these guards as called from `Program.cs` existing endpoints (e.g., `/api/tenants/{tenantId}/configuration`). Spike: grep for `ValidateTenantActiveAsync` + `ValidateTenantIdFormat` in Server, confirm they return `ErrorResponse?` (null on success) or a different shape. Spike deliverable: the correct null-check pattern in Task 6.2 spec.

- [ ] **Spike 0.4 — `IOptionsMonitor<TenantEventRoutingOptions>` change-notification posture (blocks Task 9 determinism of `HandlerRegistryServiceTests`) [Risk #7].** Verify whether the existing `Program.cs:254` `/dapr/subscribe` handler accepts a fresh `.CurrentValue` on every call or captures it at registration. Confirm that `HandlerRegistryService` reading `.CurrentValue` per-call is the correct pattern. If the answer is unclear, add a `RoutingOptionsChangeNotificationTests.CurrentValueReadsFreshlyAfterConfigReload` guard test.

- [ ] **Spike 0.6 — Submodule `IEventIngestionTelemetry` implementation sweep (NEW — Finding M, blocks Task 3.1) [FMA #1].** Before changing the `IEventIngestionTelemetry` interface signature, sweep the submodule tree for any implementation or consumer. Command: `grep -rn "IEventIngestionTelemetry\b" src/submodules/` + `grep -rn "RecordIngestion" src/submodules/`. Any hit must either (a) be updated in-lock-step with this story, (b) have its method already go through an adapter we control, or (c) trigger an escalation before the signature change. Spike deliverable: the list of submodule consumers found (expected: zero, but verify).

- [ ] **Spike 0.7 — Named-arg call-site audit for `RecordIngestion` (NEW — Finding O, blocks Task 3.3) [FMA #4].** The signature change inserts `cloudEventType` BETWEEN `aggregateType` and `outcome`. If any caller uses named-arg invocation like `RecordIngestion(aggregateType: x, outcome: y)` with positional intervening args, the call compiles through but values are silently passed wrong. Command: `grep -rEn "RecordIngestion\s*\(\s*(tenantId|caseId|cloudEventId|aggregateType|outcome|durationMs)\s*:" src/ tests/`. Every hit must be manually verified before compile. Spike deliverable: the list of named-arg call sites (expected: few or zero in this codebase style, but verify).

- [ ] **Spike 0.5 — Authorization policy name + tenant-scoping mechanism (NEW — Delta #12, blocks Task 6.2) [Red Team gap].** Check how `/api/tenants/{tenantId}/telemetry/summary` at `Program.cs:2906` currently wires authorization — is it `.RequireAuthorization("OperatorAccess")`, `.RequireAuthorization("TenantOperator")`, a custom authorization handler, or inherited implicitly from a parent group? Also verify how tenant-scoping is enforced (claims check vs middleware vs handler). Spike deliverable: the EXACT `.RequireAuthorization(...)` string + the tenant-scoping claims-check snippet to copy into `/api/tenants/{tenantId}/handlers/mismatches`. If there is NO existing tenant-scoping pattern, this spike BLOCKS the story — the mismatches endpoint MUST NOT land without tenant-scoped authZ (Red Team gap #4). If the answer is "there is no tenant-scoping today, the `telemetry/summary` endpoint is globally-scoped operator-only," escalate to the user for a scoping decision BEFORE implementing Task 6.2.

---

- [ ] **Task 1: `IObservedEventTypeStore` + `RedisObservedEventTypeStore`** (AC: #10, #11, Risks #1, #8, #9)
  - [ ] 1.1 Create `src/Hexalith.Memories.EventStore/IObservedEventTypeStore.cs` — public interface + `public sealed record ObservedEventType(string AggregateType, string EventType, long Count, DateTimeOffset LastSeenAt)`. Include XML doc comments calling out the 24h rolling window + fail-open-on-write posture.
  - [ ] 1.2 Create `src/Hexalith.Memories.EventStore/RedisObservedEventTypeStore.cs` — `internal sealed class`. Constructor takes `[FromKeyedServices("redis")] IConnectionMultiplexer redis` + `ILogger<RedisObservedEventTypeStore> logger` — match `RedisAggregateCaseMappingStore.cs:19` pattern.
  - [ ] 1.3 Implement `RecordObservationAsync`:
    - Validate `tenantId != MemoriesMeter.RejectedTenantTag` — throw `ArgumentException` if violated (defense-in-depth per Risk #9).
    - Validate `tenantId`, `aggregateType`, `eventType` non-empty.
    - Use `IDatabase.CreateBatch()` to pipeline (Fix #5 — aggregates SET added):
      - `SetAddAsync(aggregatesIndexKey, aggregateType)` — records the aggregateType in the per-tenant index set.
      - `SortedSetAddAsync(sortedSetKey, eventType, observedAt.ToUnixTimeMilliseconds(), When.Always)`
      - `HashIncrementAsync(counterHashKey, eventType, 1)`
      - `KeyExpireAsync(aggregatesIndexKey, TimeSpan.FromHours(48))`
      - `KeyExpireAsync(sortedSetKey, TimeSpan.FromHours(48))`
      - `KeyExpireAsync(counterHashKey, TimeSpan.FromHours(48))`
    - `batch.Execute()` + `await Task.WhenAll(...)` the returned tasks.
    - Wrap in `try/catch (RedisException)` — on failure, log `9140 ObservedEventTypeStoreWriteFailed` at Warning level + return (fail-open).
  - [ ] 1.4 Implement `GetObservedTypesAsync(tenantId, aggregateType, window, ct)`:
    - Compute `minScore = (now - window).ToUnixTimeMilliseconds()`.
    - `ZRANGEBYSCORE` from `minScore` to `+inf` with scores to get event types + their `lastSeenAt` scores.
    - `HMGET` the counter hash for the same event types to fetch their counts.
    - Pipe-line both via one batch. Return sorted by `LastSeenAt DESC`.
  - [ ] 1.5 Implement `GetAllObservedTypesAsync(tenantId, window, ct)` (Fix #5 — SMEMBERS-driven, NOT SCAN):
    - `SMEMBERS {tenantId}:eventstore:observed-aggregates` → enumerate the aggregateTypes known for this tenant. O(cardinality), bounded by the tenant's own aggregate-type diversity — typically single-digit.
    - For each aggregateType discovered, reuse `GetObservedTypesAsync(tenantId, aggregateType, window, ct)`.
    - Return flat list.
    - **DO NOT** use `SCAN` or `KEYS` — the per-tenant SET is the authoritative index. Guard test `.GetAllObservedTypes_UsesSMEMBERS_NotScanOrKeys` (assert no SCAN command issued via Redis command capture).

  - [ ] 1.5a **Aggregates-set cardinality cap (Delta #10 — Red Team gap).** BEFORE the `SetAddAsync(aggregatesIndexKey, aggregateType)` in `RecordObservationAsync` (1.3), issue `SCARD {tenantId}:eventstore:observed-aggregates` via the same batch → if result ≥ 1024, emit `EventStoreIntegrationLog.ObservationAggregatesSetCardinalityWarning` (**new event id 9142**, Warning level, tags `tenant_id` + `cardinality`) AND SKIP the SADD for this observation (the ZADD + HINCRBY still proceed — we just stop registering new aggregateTypes for this tenant until the 48h EXPIRE resets). Rationale: a malicious or buggy publisher emitting 10k distinct aggregateTypes could otherwise inflate the per-tenant aggregates index without bound, causing `SMEMBERS` in `GetAllObservedTypesAsync` to fan out 10k parallel `GetObservedTypesAsync` calls → connection-pool saturation + tail-latency blowup. Cap at 1024 is O(10× normal per-tenant diversity) — permissive for legit use, tight enough to stop abuse. Guard test `RedisObservedEventTypeStoreTests.AggregatesSetAt1024_StopsRegistering_EmitsWarning_KeepsZADDAndHINCRBY`.
  - [ ] 1.6 Register in `EventStoreIntegrationServiceCollectionExtensions.AddMemoriesEventStoreIntegration` via `services.TryAddSingleton<IObservedEventTypeStore, RedisObservedEventTypeStore>()`.
  - [ ] 1.7 Unit tests in `tests/Hexalith.Memories.EventStore.Tests/RedisObservedEventTypeStoreTests.cs` — use the existing Redis test fixture pattern from `RedisPreflightDedupStoreTests.cs` (testcontainers-based) or a tight in-memory double. Tests:
    - `.BatchedWrite_ExecutesInSingleRoundTrip` (asserts the batch contains both commands via fixture-level command capture).
    - `.WriteFailure_LogsWarningAndReturns` (stubs `IConnectionMultiplexer.GetDatabase()` to throw; asserts log + no exception surfaces).
    - `.RejectedTenantTag_ThrowsArgumentException`.
    - `.GetObservedTypes_Within24hWindow_ReturnsRecentOnly` (writes at `now - 23h` and `now - 25h`; asserts only 23h one returned when window = 24h).
    - `.KeyExpiration_IsSetTo48h_OnEveryWrite` (verify via `OBJECT` or `TTL` assertion).

- [ ] **Task 2: Contract types + AOT registration** (AC: #3, #4, #5, #14, Risk #10)
  - [ ] 2.1 Create `src/Hexalith.Memories.Contracts/V1/HandlerRegistrationSnapshot.cs` — sealed records from the "What 9.3 adds" #7 spec. Use `[JsonPropertyName]` on every property.
  - [ ] 2.2 Create `src/Hexalith.Memories.Contracts/V1/HandlerMismatchReport.cs` — sealed records + enums from "What 9.3 adds" #8. Enums use `[JsonConverter(typeof(CamelCaseStringEnumConverter<T>))]`.
  - [ ] 2.3 Edit `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — add `[JsonSerializable(typeof(HandlerRegistrationSnapshot))]`, `[JsonSerializable(typeof(HandlerRegistration))]`, `[JsonSerializable(typeof(ObservedEventTypeSummary))]`, `[JsonSerializable(typeof(HandlerMismatchReport))]`, `[JsonSerializable(typeof(HandlerMismatch))]`, `[JsonSerializable(typeof(HandlerMismatchCategory))]`, `[JsonSerializable(typeof(HandlerMismatchSeverity))]`, `[JsonSerializable(typeof(HandlerSubscriptionStatus))]`. **Per Spike 0.1 finding:** either extend the existing completeness test OR create `tests/Hexalith.Memories.Contracts.Tests/MemoriesJsonContextCompletenessTests.cs` in this task.
  - [ ] 2.4 Unit tests `HandlerRegistrationSnapshotTests.cs` + `HandlerMismatchReportTests.cs` — round-trip JSON serialization for all shapes + verifies enum `camelCase` output (e.g., `"unhandledEventType"` not `"UnhandledEventType"`).

- [ ] **Task 3: Extend `IEventIngestionTelemetry` + downstream callers** (AC: #10, #11, Risk #9)
  - [ ] 3.1 Edit `src/Hexalith.Memories.EventStore/IEventIngestionTelemetry.cs` — add `string? cloudEventType` as a new positional parameter between `aggregateType` and `outcome`. Update the XML doc comment.
  - [ ] 3.2 Edit `src/Hexalith.Memories.EventStore/NoOpEventIngestionTelemetry.cs` — update method signature; body stays empty.
  - [ ] 3.3 Edit `src/Hexalith.Memories.EventStore/EventIngestionService.cs` — thread `envelope.Type` through EVERY `_telemetry.RecordIngestion(...)` call site. Five sites currently:
    - The InvalidCloudEvent branch (`envelope` is NOT available — pass `null`).
    - The RouteResolutionFailed branch (`envelope.Type` IS available).
    - `MapNonAcceptedResolution` drop-branch call at :111.
    - The Duplicate-reservation branch at :138.
    - The Accepted success path at :164.
    - The ScheduleFailed catch at :188.
    - Verify all 6 sites compile + pass the appropriate `envelope?.Type ?? null`.
  - [ ] 3.4 Edit `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs`:
    - Add `cloudEventType` parameter to `RecordIngestion`.
    - Add `queryParams["cloudEventType"] = cloudEventType`.
    - Constructor-inject `IObservedEventTypeStore`, `IOptionsMonitor<EventStoreObservationOptions>` (Finding Q — NOT `IOptions<T>`; live reload required), `ILogger<EventIngestionTelemetryAdapter>`.
    - Hold a static `SemaphoreSlim` field (shared process-wide) `_observationInFlight = new(initialCount: 256, maxCount: 256)` — the concurrency cap for fire-and-forget observation writes. See Dev Notes "Why 256 for the semaphore cap" for rationale (Finding K).
    - Register an `OnChange` subscription on the `IOptionsMonitor<EventStoreObservationOptions>` to log `9143 ObservationWritesConfigChanged` (R3-3 unified; tag `enabled=true|false`) on each enable/disable transition. The subscription stores the prior-known **value** in a private field (e.g., `private bool? _lastKnownEnabled`) and compares **by value** (`prior != current.Enabled`) — R3-7 hardening against `OnChange` firing multiple times per logical change (filesystem watchers on some platforms notify 2-3x) where reference-equality would false-positive. If comparison says "no change," the subscription returns without emitting the log.
    - After the existing `AccessTelemetryLog.LogIngestAccess` / `LogIngestAccessError` call, add:
      ```csharp
      // Delta #14 — kill switch. Disabled = skip entirely; transition logging is handled by the OnChange subscription above.
      if (!_observationOptionsMonitor.CurrentValue.Enabled) return;

      // R3-8 semantic fix: record ONLY on Accepted. Duplicate means "we already recorded this" — double-counting
      // the same logical event inflates EventsProcessedCount by retry volume, which is misleading.
      // Delta #11 — whitespace-strict; Risk #9 — reject the __rejected__ tenant; also null cloudEventType.
      if (outcome != EventIngestionOutcome.Accepted
          || string.IsNullOrWhiteSpace(tenantId)
          || tenantId == MemoriesMeter.RejectedTenantTag
          || string.IsNullOrWhiteSpace(aggregateType)
          || string.IsNullOrWhiteSpace(cloudEventType))
      {
          // Finding P — emit a Debug-level log so a future debugger can see "observation skipped due to whitespace"
          // without digging through the code. Debug is intentional — not Warning — because this is often a legitimate
          // case (e.g., InvalidCloudEvent outcome with null fields).
          _logger.LogDebug("Observation write skipped (outcome={Outcome}, hasTenantId={HasTenant}, hasAggregate={HasAggregate}, hasEventType={HasEventType})",
              outcome, !string.IsNullOrWhiteSpace(tenantId), !string.IsNullOrWhiteSpace(aggregateType), !string.IsNullOrWhiteSpace(cloudEventType));
          return;
      }

      // Delta #3 — bounded fire-and-forget. Drop on backpressure; hard 2s timeout on the task itself.
      _ = Task.Run(async () =>
      {
          var acquireCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5));
          bool acquired = false;
          try
          {
              acquired = await _observationInFlight.WaitAsync(TimeSpan.FromMilliseconds(5), acquireCts.Token).ConfigureAwait(false);
              if (!acquired)
              {
                  MemoriesMeter.ObservationsDropped.Add(1, new KeyValuePair<string, object?>("reason", "backpressure"));
                  return;
              }
              using var writeCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
              try
              {
                  await _observedEventTypeStore
                      .RecordObservationAsync(tenantId, aggregateType, cloudEventType, DateTimeOffset.UtcNow, writeCts.Token)
                      .ConfigureAwait(false);
              }
              catch (OperationCanceledException) when (writeCts.IsCancellationRequested)
              {
                  MemoriesMeter.ObservationsDropped.Add(1, new KeyValuePair<string, object?>("reason", "timeout"));
              }
              catch (RedisException)
              {
                  MemoriesMeter.ObservationsDropped.Add(1, new KeyValuePair<string, object?>("reason", "redis_error"));
                  // Store-level logging already emits 9140 at Warning.
              }
          }
          finally
          {
              if (acquired) _observationInFlight.Release();
          }
      });
      ```
    - Emit `9143 ObservationWritesDisabledByConfig` at Information level ONCE at startup (via a hosted service or a one-shot latch in the first call when options.Value.Enabled is false) — AC #21 clause (a).
    - Register `memories.handlers.observations.dropped` counter in `MemoriesMeter` (see Task 9.1) with tag policy `["reason"]`.
  - [ ] 3.5 Edit any test doubles (`FakeEventIngestionTelemetry`, `RecordingEventIngestionTelemetry`, etc.) to match the new signature — `grep -rn "IEventIngestionTelemetry" tests/` to enumerate.
  - [ ] 3.6 Unit tests `EventIngestionTelemetryAdapterTests.cs`:
    - `.AcceptedOutcomeWithNonNullEventType_WritesToObservationStore`.
    - `.DuplicateOutcome_DoesNotWriteObservation_R3Dash8` (inverted from prior spec — R3-8 semantic fix).
    - `.UnknownSourceOutcome_DoesNotWriteToObservationStore`.
    - `.NullCloudEventType_DoesNotWriteToObservationStore`.
    - `.WhitespaceCloudEventType_DoesNotWriteToObservationStore_EmitsDebugLog` (R3 Finding P).
    - `.RejectedTenantTag_DoesNotWriteToObservationStore`.
    - `.ObservationStoreThrows_AuditLogStillEmitted_NoExceptionSurfaces`.
    - `.OnChange_SameEnabledValueTwice_EmitsLogOnlyOnce_R3Dash7` (R3-7 value-comparison hardening).

- [ ] **Task 4: `HandlerRegistryService`** (AC: #1, #2, #9)
  - [ ] 4.1 Create `src/Hexalith.Memories.Server/Handlers/` folder (mirrors `Tenants/` + `Telemetry/` conventions).
  - [ ] 4.2 Create `src/Hexalith.Memories.Server/Handlers/HandlerRegistryService.cs` — public sealed class from "What 9.3 adds" #9. Constructor: `IOptionsMonitor<TenantEventRoutingOptions> options, IObservedEventTypeStore store, TenantRegistryService tenantRegistry, TimeProvider timeProvider, ILogger<HandlerRegistryService> logger`.
  - [ ] 4.3 Implement `GetSnapshotAsync(CancellationToken ct)`:
    - Read `options.CurrentValue`.
    - If `Topic` is empty → return `new HandlerRegistrationSnapshot { SubscriptionStatus = HandlerSubscriptionStatus.Disabled, Handlers = [], ... }`.
    - Build per-tenant tasks: `options.SourceToTenantMap.GroupBy(kvp => kvp.Value)` then `Task.WhenAll` over groups. **Finding S — graceful per-tenant degradation:** EACH per-tenant task is wrapped in its own try-catch; if the per-tenant Redis read throws, the task returns a SENTINEL "error" `HandlerRegistration` (with `Error = "OBSERVATION_READ_FAILED"`, `EventsProcessedCount = 0`, `ObservedEventTypes = []`) instead of propagating. The outer `Task.WhenAll` then completes successfully with a mix of healthy + error rows. An operator seeing ONE error row in a 100-tenant snapshot can drill into that specific tenant rather than being faced with a 500 that hides 99 healthy tenants. Log `9146 TenantObservationReadFailed` at Warning per failed tenant (tags `tenant_id` + `exception_type`).
    - For each tenant group:
      - Call `tenantRegistry.GetAsync(tenantId, linkedCt)` with a per-tenant `CancellationTokenSource(TimeSpan.FromMilliseconds(500))` linked to `ct` (R3-9 hardening against a hanging `TenantRegistryService`). If cancelled, surface the row with `Error = "TENANT_STATUS_CHECK_FAILED"` and skip the Redis read.
      - If the GetAsync returns `null` or `Status in (Deleting, Deleted)`, SKIP the registration (do NOT surface deleted tenants' handler state).
      - Call `_store.GetAllObservedTypesAsync(tenantId, TimeSpan.FromHours(24), ct)` INSIDE the try-catch described above.
      - For each `(sourcePrefix → tenantId)` entry in this tenant group, synthesize ONE `HandlerRegistration`:
        - `EventTypePatterns = observedTypes.Select(o => o.AggregateType).Distinct().ToList()` — **always a plain `List<string>`, `[]` when empty (Fix #3 — data purity at the service layer).** The sentinel string `"(none observed in last 24h)"` is a CLI TABLE FORMATTER concern ONLY (Task 8.5), never the service response.
        - `EventsProcessedCount = observedTypes.Sum(o => o.Count)`.
        - `LastEventAt = observedTypes.MaxBy(o => o.LastSeenAt)?.LastSeenAt.ToString("O")`.
        - `ObservedEventTypes = observedTypes.Select(o => new ObservedEventTypeSummary { ... }).ToList()`.
    - `SubscriptionStatus` — apply the canonical rule from "What 9.3 adds" #7 reconciled block (Fix #1). Pseudocode:
      ```csharp
      if (string.IsNullOrEmpty(options.Topic)) return HandlerSubscriptionStatus.Disabled;
      if (options.SourceToTenantMap.Count == 0) return HandlerSubscriptionStatus.Unknown;
      var uptime = _timeProvider.GetUtcNow() - _processStartedAt; // injected
      if (uptime < TimeSpan.FromMinutes(2) && !anyObservationRecorded) return HandlerSubscriptionStatus.Unknown;
      return HandlerSubscriptionStatus.Active;
      ```
      `EventsProcessedCount == 0` on a handler does NOT flip status to Unknown — that is per-row traffic information, not subscription state.
    - Emit `9131 HandlerRegistrySnapshotServed` once at the end with `handlersCount`.
  - [ ] 4.4 Register in `Program.cs` (or the Server DI composition root) as scoped.
  - [ ] 4.5 Unit tests `HandlerRegistryServiceTests.cs`:
    - `.EmptyTopic_ReturnsDisabledStatus_NoHandlers`.
    - `.MultipleEntriesPointingToSameTenant_CollapseToOneRegistration`.
    - `.DeletedTenant_IsExcludedFromHandlers`.
    - `.ObservedCountsAggregatedPerTenant`.
    - `.EmptyObservedTypes_ReturnsEmptyArrayInJson` (service layer returns `[]` — sentinel is table-formatter concern).
    - `.LastEventAtIsIsoFormatted_OrNullWhenNoObservations`.
    - `.SubscriptionStatusActive_WhenAtLeastOneTenantHasEvents_AndTopicConfigured`.

- [ ] **Task 5: `HandlerMismatchDetector`** (AC: #3, #4, #5, Risks #2, #5)
  - [ ] 5.1 Create `src/Hexalith.Memories.Server/Handlers/HandlerMismatchDetector.cs` — public sealed class. Constructor: `IOptionsMonitor<TenantEventRoutingOptions> options, IObservedEventTypeStore store, TimeProvider timeProvider, ILogger<HandlerMismatchDetector> logger`.
  - [ ] 5.2 Declare compiled regex constant:
    ```csharp
    private static readonly Regex VersionStemRegex = new(
        pattern: @"^(.+?)(V\d+)$",
        options: RegexOptions.Compiled | RegexOptions.CultureInvariant,
        matchTimeout: TimeSpan.FromMilliseconds(100));
    ```
    **Finding T — wrap ALL regex invocations in the detector**, not just the VersionMismatch scan. If a future edit adds regex-based detection to UnhandledEventType or StaleHandler, the try-catch surface must already exist. Implement as a private helper `TryMatchVersionStem(string eventType, out Match? match)` that returns false on `RegexMatchTimeoutException`, emits 9141, and catches `ArgumentException` for invalid regex input as a separate defensive case. All call sites go through this helper.
  - [ ] 5.3 Implement `DetectAsync(string tenantId, TimeSpan window, CancellationToken ct)`:
    - Validate inputs.
    - Call `_store.GetAllObservedTypesAsync(tenantId, window, ct)`.
    - Read `options.CurrentValue.SourceToTenantMap`; filter to entries routed to `tenantId`.
    - **StaleHandler scan:** for each source-prefix routed to this tenant, if observed-types list is empty, emit one `StaleHandler` with the Info-severity suggestion text. Cap result to N Stale mismatches where N = routed-source count (bounded).
    - **UnhandledEventType scan:** for each observed type whose `aggregateType` does NOT match any routed source-prefix's aggregateType-naming convention (hint: grep on aggregateType substring within the source-prefix; document conservatively in §11.2), emit `UnhandledEventType` with Warning severity.
    - **VersionMismatch scan:** for each observed type, FIRST split on `.` and take the terminal segment (`lastSegment = eventType.Split('.').Last()`) — the regex operates on the terminal segment ONLY (Fix #2 — canonical per Dev Notes §"Mismatch detector stem-extraction edge cases"). THEN group by regex-extracted stem from that terminal segment; for groups where `count >= 2` distinct versions AND all have `Count > 0`, emit ONE VersionMismatch per group. CAP: `eventType.Length <= 256` BEFORE regex evaluation (Risk #5 ReDoS guard). `Subject` on the mismatch is the stem FROM THE TERMINAL SEGMENT (e.g., `MyApp.Claims.ClaimSubmittedV2` + `MyApp.Claims.ClaimSubmittedV3` → `Subject = "ClaimSubmitted"`, NOT `"MyApp.Claims.ClaimSubmitted"`).
    - Emit `9132 HandlerMismatchDetected` once per detected mismatch with tags.
    - Return `HandlerMismatchReport`.
  - [ ] 5.4 Register in Program.cs as scoped.
  - [ ] 5.5 Unit tests `HandlerMismatchDetectorTests.cs`:
    - `.ObservedTypeNotInRoutingMap_ReportedAsUnhandled_Warning`.
    - `.RegisteredSourceWithZeroEvents_ReportedAsStale_Info`.
    - `.StaleHandlerSuggestion_ContainsLowVolumeCaveat` (string assertion on suggestion text).
    - `.MultipleVersionsSameStem_ReportedAsVersionMismatch_Warning`.
    - `.SingleVersionOnly_NoVersionMismatchReported`.
    - `.EventTypeOver256Chars_SkippedFromVersionMismatch_EmitsWarning`.
    - `.RegexTimeout_DoesNotThrow_LogsWarning` (inject an adversarial event-type via synthetic test harness).
    - `.EmptyObservedTypes_EmitsInfoSeverity_NotWarning` (Risk #2 regression guard).

- [ ] **Task 6: Minimal-API endpoints** (AC: #1, #2, #9)
  - [ ] 6.1 Per Spike 0.3 finding, verify `TenantStatusGuard.ValidateTenantIdFormat` + `ValidateTenantActiveAsync` shapes.
  - [ ] 6.2 Edit `src/Hexalith.Memories.Server/Program.cs` — AFTER existing `/api/tenants/{tenantId}/telemetry/summary` at :2906:
    ```csharp
    app.MapGet("/api/handlers", async (HandlerRegistryService svc, CancellationToken ct) =>
        Results.Ok(await svc.GetSnapshotAsync(ct)));

    app.MapGet("/api/tenants/{tenantId}/handlers/mismatches", async (
        HandlerMismatchDetector detector,
        TenantStatusGuard guard,
        string tenantId,
        CancellationToken ct) =>
    {
        // ... use the exact shape discovered in Spike 0.3
        return Results.Ok(await detector.DetectAsync(tenantId, TimeSpan.FromHours(24), ct));
    });
    ```
  - [ ] 6.3 Register services in the DI composition (same file / AddMemoriesServer extension) — scoped lifetimes.
  - [ ] 6.4 Integration tests `HandlerEndpointIntegrationTests.cs` (use existing `WebApplicationFactory`/Testcontainers fixture pattern):
    - `.GetHandlers_ReturnsSnapshotShape`.
    - `.GetHandlersMismatches_UnknownTenant_Returns404`.
    - `.GetHandlersMismatches_InvalidTenantIdFormat_Returns400`.
    - `.GetHandlers_Disabled_WhenTopicEmpty`.

- [ ] **Task 7: `MemoriesClient` REST methods** (AC: #6, #18)
  - [ ] 7.1 Edit `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs` — add:
    ```csharp
    [Experimental("HXL002")]
    public virtual async Task<HandlerRegistrationSnapshot> ListHandlersAsync(CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync("api/handlers", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ErrorResponse error = await ErrorResponseDecoder.DecodeAsync(response, ct).ConfigureAwait(false);
            throw new MemoriesRemoteException(response.StatusCode, error);
        }
        HandlerRegistrationSnapshot? snapshot = await response.Content
            .ReadFromJsonAsync<HandlerRegistrationSnapshot>(MemoriesJsonContext.Options, ct)
            .ConfigureAwait(false);
        return snapshot ?? throw CreateInvalidResponseException(response.StatusCode, "Server returned 2xx with empty handler snapshot body.");
    }

    [Experimental("HXL002")]
    public virtual async Task<HandlerMismatchReport> GetHandlerMismatchesAsync(string tenantId, CancellationToken ct)
    {
        // ... same pattern with path $"api/tenants/{Uri.EscapeDataString(tenantId)}/handlers/mismatches"
    }
    ```
  - [ ] 7.2 Confirm `HXL002` diagnostic is defined (if HXL001 exists as the only current diagnostic, check `Directory.Build.props` or the `Experimental` attribute's behavior — the `[Experimental("HXL002")]` attribute automatically emits a custom-ID warning without needing a separate declaration; verify by compiling).
  - [ ] 7.3 Unit tests `MemoriesClientHandlersTests.cs` — use `HttpMessageHandler` test double to return canned JSON + assert deserialization. Include `.NonSuccessStatusCode_ThrowsMemoriesRemoteException`.
  - [ ] 7.4 **Consumer-driven contract test (Fix #6 — Murat's gap):** Add `MemoriesClientHandlersContractTests.cs` that:
    - Constructs a server-shape `HandlerRegistrationSnapshot` + `HandlerMismatchReport` instance in C# (the Server's canonical shape).
    - Serializes via `JsonSerializer.Serialize(instance, MemoriesJsonContext.Options)` → bytes.
    - Pipes those bytes through a mocked `HttpMessageHandler` into `MemoriesClient.ListHandlersAsync` / `GetHandlerMismatchesAsync`.
    - Asserts the round-tripped object is structurally equal (field-by-field) to the original via `FluentAssertions.BeEquivalentTo` or equivalent.
    - Also asserts enum camelCase rendering (`"unhandledEventType"`, not `"UnhandledEventType"`) survives the round-trip.
    - **Purpose:** catches `MemoriesJsonContext` registration drift + enum-converter omissions BEFORE Tier-2 integration — the JSON contract between server and client is proven at `dotnet build` time, not at commit time.

- [ ] **Task 8: CLI `handlers list` + `handlers mismatches`** (AC: #6, #7, #8, #13, #19)
  - [ ] 8.1 Create `src/Hexalith.Memories.Cli/Commands/HandlersListCommand.cs` — mirror `TenantListCommand.cs` verbatim structure. `CommandName = "handlers list"`. `#pragma warning disable HXL002` scoped around the `client.ListHandlersAsync` call. **Finding W — exit-code map:** on `MemoriesRemoteException` return `CliExitCodes.NetworkError = 4` (parallel to other CLI commands), on valid-empty return `Success = 0`, on argument-validation failures return `CliExitCodes.ArgumentError = 2`. Specified in code comment at the catch site so the map survives future refactors. **R3-2 revert of Round-2 Finding D:** `--format explain` formatter was speculative and has been removed from scope. Non-developer operators are expected to use the existing `table` format with glossary support (Dev Notes "Glossary" section) — the Hindsight Reflection finding validated that real users learn `table` in a day regardless. **Finding X — `--no-wrap`:** the table formatter accepts a `--no-wrap` option which truncates each column with an ellipsis at an OS-detected terminal width (`Console.WindowWidth`) and sets `stderr` note "use --format json for full content".
  - [ ] 8.2 Empty-state nudge in `HandlersListCommand` — write to stderr (for table) or stdout (for human) per the `TenantListCommand` convention: `"No handlers registered. Configure EventStoreIntegration:Routing:SourceToTenantMap in appsettings to bind CloudEvents sources to tenants. See docs/dev/eventstore-integration.md §11."`. JSON format: skip the nudge (consumers detect empty array).
  - [ ] 8.3 Create `src/Hexalith.Memories.Cli/Commands/HandlersMismatchesCommand.cs`:
    - REQUIRED `--tenant` option (`IsRequired = true`).
    - OPTIONAL `--severity` option with allowed values `info`, `warning`.
    - OPTIONAL `--only-warning` flag (Finding B — shorthand equivalent to `--severity warning`; convenient common filter).
    - OPTIONAL `--exclude-stale` flag (Finding B — suppress `StaleHandler` category entries; SRE noise-reduction during investigation).
    - Server-side returns the unfiltered report; filter CLIENT-SIDE for HUMAN + TABLE output (AC #8 + #30). JSON output returns the full unfiltered report.
    - Empty-state human nudge: `"No handler mismatches detected in the last 24h for tenant '{tenantId}' — this is the healthy state. Summary: {summary.routesConfigured} routes configured, {summary.observationsChecked} observations examined."` to stdout. (Finding EE — positive-confirmation UX; the summary fields come from the new `HandlerMismatchReportSummary`.)
    - When mismatches exist in HUMAN format, render: `[{severity.ToLowerInvariant()}] {category.ToLowerInvariant()}: {subject} — {suggestion}` one per line. Render the summary line after the last mismatch: `"({n} mismatch(es) across {summary.categoriesExamined.Count} categor(ies)) examined."`.
    - **Finding W — exit-code map:** `CliExitCodes.NetworkError = 4` on `MemoriesRemoteException`; `CliExitCodes.Success = 0` on any successful response regardless of mismatch count (mismatches are NOT errors).
  - [ ] 8.4 Create `src/Hexalith.Memories.Cli/Output/OutputFormatters/HandlerRegistrationTableFormatter.cs` + `HandlerMismatchTableFormatter.cs` — register in the `OutputFormatterRouter` composition (same extension method as the existing tenant + telemetry formatters).
  - [ ] 8.5 Table formatter for `HandlerRegistrationSnapshot` implements the sentinel-for-empty-observed-types rule (Risk #3) — transforms `EventTypePatterns = []` into `"(none observed in last 24h)"` for rendering. JSON formatter does NOT.
  - [ ] 8.6 Edit `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs`:
    - Add `HandlersCommandDescription` constant.
    - REMOVE the `("handlers", ...)` tuple from `CommandGroups` at :80.
    - Build the real `handlersCommand` at the location parallel to `statusCommand` at :120-124:
      ```csharp
      var handlersCommand = new Command("handlers", HandlersCommandDescription);
      handlersCommand.Subcommands.Add(HandlersListCommand.Build(services));
      handlersCommand.Subcommands.Add(HandlersMismatchesCommand.Build(services));
      handlersCommand.SetAction(_ => handlersCommand.Parse("--help").Invoke());
      root.Subcommands.Add(handlersCommand);
      ```
  - [ ] 8.7 Unit tests `HandlersListCommandTests.cs` + `HandlersMismatchesCommandTests.cs` + `RootCommandFactoryTests.cs` extensions:
    - `.RootCommand_HasHandlersSubcommand_ButNoStub` (Risk #6).
    - `.NoCommandGroupIsRegisteredBothAsRealAndStub`.
    - `.HandlersList_JsonFormat_EmitsSerializedSnapshot`.
    - `.HandlersList_EmptyRegistry_EmitsNudgeToStderr_InTableFormat`.
    - `.HandlersList_HumanFormat_IsStableAcrossReleases` (byte-for-byte snapshot test — ADR-7.2-002 parity).
    - `.HandlersMismatches_HumanFormat_ShowsSeverityCategorySubjectSuggestion`.
    - `.HandlersMismatches_SeverityFilter_WorksInHumanFormat_NotInJson`.
    - `.HandlersMismatches_EmptyReport_EmitsHealthyMessageToStdout`.

- [ ] **Task 9: `MemoriesMeter` instrument additions** (AC: #12, #22, Risk #4)
  - [ ] 9.1 Edit `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs`:
    - Add `HandlersRegisteredName`, `HandlerMismatchesName`, and `ObservationsDroppedName = "memories.handlers.observations.dropped"` constants (Delta #3).
    - Add `HandlerMismatches` counter property with `Instance.CreateCounter<long>(...)` — tags: `tenant_id`, `severity`.
    - Add `ObservationsDropped` counter — tag: `reason` (values: `"backpressure"`, `"timeout"`, `"redis_error"`). Note: no `tenant_id` tag here because dropped observations are a STORE-side concern, not tenant-scoped — and attaching `tenant_id` would re-introduce the same cardinality problem Risk #4 mitigates.
    - Add a PARALLEL `EnsureHandlerGaugeCreated(Func<IEnumerable<Measurement<int>>> handlerRegisteredObserver)` method (additive — do NOT change the existing `EnsureObservableGaugesCreated` signature).
    - Extend `MetricTagKeyPolicy` dictionary with:
      ```csharp
      [HandlersRegisteredName] = new[] { "tenant_id" },
      [HandlerMismatchesName] = new[] { "tenant_id", "severity" },
      [ObservationsDroppedName] = new[] { "reason" },
      ```
  - [ ] 9.2 Call `MemoriesMeter.EnsureHandlerGaugeCreated(...)` from `Program.cs` alongside the existing `MemoriesMeter.EnsureObservableGaugesCreated(...)` call at :274. The observer delegates to a method on `HandlerRegistryService` that returns a per-tenant count of registered sources. **Finding Y — force-create counters at this same call site** so `MeterListener` subscribers attached after startup still see the `ObservationsDropped` instrument — lazy creation on first Add can hide the counter from late-subscribing listeners.
  - [ ] 9.3 Emit `MemoriesMeter.HandlerMismatches.Add(1, ...)` from inside `HandlerMismatchDetector.DetectAsync` per detected mismatch (one emission per mismatch).
  - [ ] 9.4 Unit tests `MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys` — ensure the extended map covers all three new instruments (including `ObservationsDropped`).
  - [ ] 9.5a **Per-drop Warning log (Finding G — new log event 9144; R3-5 simplification).** The Round-2 proposal extended `RollingCounterStore` for a 1h aggregation window, which directly conflicted with Risk #7's "DO NOT extend `RollingCounterStore` — substrate separation is load-bearing." **Simplified design:** emit `9144 ObservationDropped` at Warning level ONCE per drop with tags `reason ∈ {"backpressure","timeout","redis_error"}` + `tenant_id`. Operators aggregate at the log sink (Grafana/Loki natively supports 1h window aggregation via PromQL / LogQL `rate()`). Zero in-process state; zero substrate-sharing; zero Risk-#7 violation. Guard test `EventIngestionTelemetryAdapterTests.ObservationDrop_Emits9144_AtWarning_WithReasonAndTenantTags`.

  - [ ] 9.5 **Runtime cardinality smoke test (Delta #4 — AC #22 dependency).** Create `tests/Hexalith.Memories.IntegrationTests/Handlers/HandlerMetricsCardinalitySmokeTests.cs`:
    - `.MismatchesMetric_DistinctTagValuesStayBounded` — publishes 200 CloudEvents across configurations that would emit 6 distinct `(category, severity)` combinations (3 categories × 2 severities). Uses a `MeterListener` subscribed to `MemoriesMeter.Instance` to capture every emission's tag bag. Asserts `distinctTagBags.Count == 6` AND `distinctTagBags.All(b => b.ContainsKey("severity") && b.ContainsKey("tenant_id"))`. Runs in Tier-2 with the Aspire fixture — 30s startup acceptable because it's run once per CI.
    - `.ObservationsDropped_OnlyCarriesReasonTag` — inject a slow `IObservedEventTypeStore` stub that takes 5s, publish 500 events, assert the emitted dropped-counter samples carry ONLY the `reason` tag and values are in `{"backpressure","timeout"}`.

- [ ] **Task 10: Tier-2 integration tests** (AC: #15, #16, #20)

  **Execution order (Critique Δ — smoke first):** Task 10.0 (fixture smoke) MUST run as the FIRST concrete coding step after the 4 pre-impl spikes complete — BEFORE Tasks 1-9. The Aspire-fixture startup is ~30s per test and has historically surfaced fixture-shape surprises (Story 8.2, 8.4) that would require Tasks 1-9 to be reworked. Front-loading the smoke de-risks the fixture contract; if Task 10.0 fails, rework happens at hour 1 instead of day 8.

  - [ ] 10.0 **Fixture-smoke spike (NEW — Critique Δ).** Before any other Task-10 work, scaffold a minimal `HandlersFixtureSmokeTests.cs` that (a) spins up the Aspire AppHost fixture, (b) publishes ONE CloudEvent via the pubsub driver (use the existing `EventIngestionPipelineIntegrationTests` helper), (c) asserts it arrives in the observation store (`SMEMBERS {tenantId}:eventstore:observed-aggregates` returns exactly 1 member). This test PROVES the fixture wiring for the observation store before 200+ events worth of assertions are built against it. Budget: 0.25d. Move on to 10.1+ only when this passes green.

  - [ ] 10.1 Create `tests/Hexalith.Memories.IntegrationTests/Handlers/HandlersListIntegrationTests.cs` — use the Aspire AppHost fixture (same pattern as Story 9.1's integration tests).
    - Publish 5 CloudEvents to the pubsub topic, 3×`MyApp.Claims.ClaimSubmittedV2` + 2×`MyApp.Claims.ClaimApprovedV2`, all `source = "acme.events/claims"`.
    - Wait ≤3 seconds (polling for observation-store population) — use `TestRetryHelper` or the existing `PollingHelper` if present in the integration-tests project.
    - Invoke `memories handlers list --format json` via the existing CLI test runner (or call `MemoriesClient.ListHandlersAsync` directly — CHOOSE ONE and document).
    - Assert `EventsProcessedCount == 5`, `ObservedEventTypes.Length == 2`, `LastEventAt within 3s of now`.
  - [ ] 10.2 Create `tests/Hexalith.Memories.IntegrationTests/Handlers/HandlersMismatchIntegrationTests.cs` — same fixture:
    - Publish V2 events + ONE `ClaimSubmittedV3`.
    - `memories handlers mismatches --tenant acme-tenant` output contains `VersionMismatch` with `subject = "ClaimSubmitted"`.
    - `.HealthyTenant_ReportsZeroMismatches` — publish only one version, assert empty report.
    - `.StaleHandler_AfterNoPublications_ReportedAsInfo` — configure routing for an unused tenant, no events, assert 1 Info mismatch.
  - [ ] 10.3 **Property-based `ObservationStoreLostWrites_DetectorConvergesWithinTwoWindows` (Fix #7 — Risk #8 guard test):** Wrap `IObservedEventTypeStore` in a `DroppyObservedEventTypeStore` decorator that discards a configurable `dropProbability` of `RecordObservationAsync` calls. Publish N=200 events over a short window, invoke `HandlerRegistryService.GetSnapshotAsync`, assert `observedCount ≥ (1 - dropProbability) * N * 0.9` (allowing 10% test-harness jitter). Test is parameterized over `dropProbability ∈ {0.0, 0.1, 0.3}`. Purpose: prove the convergence property, not merely the fire-and-forget mechanic.

  - [ ] 10.4 **NFR latency test (Delta #13 — AC #20).** `HandlerEndpointLatencyNfrTests.GetHandlers_AtN100Tenants_P95Under500Ms` — pre-populate 100 tenants × 5 observations each in the Aspire fixture, issue 50 back-to-back `GET /api/handlers` requests via `HttpClient`, collect durations, assert p95 ≤ 500ms. Similarly `.GetMismatches_AtN100Tenants_P95Under200Ms`. If flakey in CI, budget an additional warm-up loop (discard first 5 samples). These NFRs lock the `Task.WhenAll` concurrency pattern in `HandlerRegistryService` and flag regressions before prod.

  - [ ] 10.5 **AuthZ integration tests (Delta #12 — AC #25).** `HandlerEndpointAuthorizationTests.cs`:
    - `.UnauthenticatedRequest_Returns401_OnBothEndpoints` — issues requests with no bearer token.
    - `.AuthenticatedForDifferentTenant_Returns403_OnMismatchesEndpoint` — authenticated as `tenant-A` caller, calls `/api/tenants/tenant-B/handlers/mismatches`, expects 403.
    - `.AuthenticatedOperatorAccess_Returns200` — sanity check the positive path.
    Verifies the `.RequireAuthorization("OperatorAccess")` policy was actually applied to the new endpoints — a missing policy attachment would silently downgrade security without breaking existing tests.

  - [ ] 10.6 **Kill-switch integration test (Delta #14 — AC #21).** `HandlerObservationKillSwitchIntegrationTests.DisabledByConfig_NoRedisWrites_9143Logged` — boots the fixture with `EventStoreIntegration:Observation:Enabled=false`, publishes 10 events, asserts (a) Redis observation keys do not exist, (b) `9143 ObservationWritesDisabledByConfig` appears in the log sink exactly once, (c) `GET /api/handlers` returns `EventsProcessedCount=0` for all handlers.

  - [ ] 10.7 **Bounded-FAF integration test (Delta #3 — AC #22).** `EventIngestionTelemetryAdapterSlowRedisTests.SlowRedis_DropsObservation_WithinTwoSeconds_DoesNotBlockIngestion` — swaps in a `SlowObservedEventTypeStore` decorator (5s artificial delay per write), publishes 200 events via the ingestion endpoint, asserts (a) ingestion response p95 < 50ms (unblocked), (b) `memories.handlers.observations.dropped` counter sum ≈ 200 (all dropped), (c) the process thread-pool queue length never exceeds 256 during the run (captured via `ThreadPool.ThreadCount` polling).

  - [ ] 10.8 **Clock-skew test (AC #26 — Finding N).** `RedisObservedEventTypeStoreTests.ServerClockSkew_DoesNotPoisonWindow` — injects a `TimeProvider` fake used by BOTH writer and reader; skews forward 2h then backward; asserts the writes at time T are still returned by a read at time T + 23h with the same skewed time source. Proves the store is self-consistent regardless of absolute clock correctness.

  - [ ] 10.9 **Partial-snapshot test (AC #27 — Finding S).** `HandlerRegistryServiceTests.PartialTenantFailure_ReturnsPartialSnapshot_NotFiveHundred` — stubs `IObservedEventTypeStore.GetAllObservedTypesAsync` to throw `RedisConnectionException` for ONE tenant out of 3; asserts the returned `HandlerRegistrationSnapshot` has 3 rows, the failing tenant's row has `Error = "OBSERVATION_READ_FAILED"`, and log `9146 TenantObservationReadFailed` appears exactly once.

  - [ ] 10.10 **Summary shape test (AC #28 — Findings EE, L; R3-4 adjusted).** `HandlerMismatchDetectorTests.Summary_PopulatedEvenOnEmptyMismatches` — publishes no events, calls detector, asserts `report.Summary.RoutesConfigured == configuredMapSize` and `report.Summary.ObservationsChecked == 0`. Also `.HasWarnings_FalseOnEmpty_TrueOnWarning` + `.HasInfo_TrueOnInfoOnly`.

  - [ ] 10.11 **Endpoint-routing test (AC #29 — Findings U, V).** `EndpointRoutingTests.HandlersEndpointsAreReachable` enumerates `Endpoints.OfType<RouteEndpoint>()` and asserts both new endpoints present with the expected patterns AND no catch-all shadows them. `MemoriesClientPathConstantTests.PathStringsMatchServerRoutes` asserts the SDK client's hardcoded path strings match the server's endpoint patterns (via reflection on the Program.cs routes OR a test-only registered `IEndpointConventionBuilder` snapshot).

  - [ ] 10.12 **CLI operator-polish test (AC #30 — Findings B, X; R3-2 removed explain-format test).** `HandlersMismatchesCommandTests.ExcludeStaleFlag_SuppressesStaleHandlers` + `.OnlyWarningFlag_EquivalentToSeverityWarning` + `HandlersListCommandTests.TableFormat_NoWrap_TruncatesWithEllipsis`.

  - [ ] 10.13 **R3-1 removed** — the VersionMismatch consumer-list test (Round-2 Finding C) is withdrawn along with the feature itself. No replacement test.

- [ ] **Task 11: Documentation + sprint-status** (AC: #17)
  - [ ] 11.1 Edit `docs/dev/eventstore-integration.md` — add §11 "Handler registration & mismatch detection" with subsections §11.1–§11.4 per "What 9.3 adds" #18 spec. Cross-link from §6 "Alerting recommendations".
  - [ ] 11.2 Edit `docs/dev/cli-config.md` — add `handlers` subsection with `memories handlers list` + `memories handlers mismatches` examples in all three formats.
  - [ ] 11.3 Edit `docs/dev/telemetry.md` — add note on the 5m-vs-24h-substrate separation (Risk #7) AND the 24h window hardcode rationale (Fix #10 — cross-link to Dev Notes "Why the 24h observation window is hardcoded").
  - [ ] 11.4 Unit test `DocumentationCompletenessTests.EventStoreIntegrationDoc_Has93Sections` — asserts all four subsection headers exist.
  - [ ] 11.5 Update `_bmad-output/implementation-artifacts/sprint-status.yaml` — move `9-3-handler-registration-and-mismatch-detection` from `backlog` → `in-progress` (developer updates this on story-execution start; create-story workflow sets `ready-for-dev`).
  - [ ] 11.6 Update `_bmad-output/implementation-artifacts/deferred-work.md` — log all deferrals surfaced by this story with tracking references:
    - **`Story-9.3-ObservationWindowConfig` (Fix #10):** 24h-window operator-configurability deferred.
    - **`Story-9.3-ProjectionRegistryCrossCheck` (Delta #1):** declarative projection registry + reflection-verified cross-check against observed events — the "event accepted but silently ignored by projection" gap. Blocked until operator-driven demand.
    - **`Story-9.3-SinceFlagForLowVolume` (Delta #2):** `--since` CLI flag to widen the observation window for low-volume tenants.
    - **`Story-9.3-TenantCardinalityBucketing` (Delta #4):** switch `memories.handlers.registered` from `tenant_id`-tagged gauge to bucketed summary at N ≥ 1000 tenants.
    - **`Story-9.3-VersionMismatchAttributeApproach` (Delta #7):** replace regex-based `VersionMismatch` with publisher-declared version attribute (`[EventType("ClaimSubmitted", Version=2)]`) — eliminates ReDoS surface entirely. Blocked until publisher contract change is acceptable.
    - **`Story-9.3-SubscriptionStatusConfigured` (Delta #8):** 4-state `HandlerSubscriptionStatus` enum (add `Configured` between `Unknown` and `Active`) to disambiguate "routing is set up" from "routing is working." API contract change — blocked until next major of HXL002.
    - **`Story-9.3-ObservationStoreRebuildFromAuditLog` (Risk #8):** rebuild observation store from `AccessTelemetryLog` on startup to recover from sidecar-restart observation loss.
    - **`Story-9.3-PostgresObservationStoreAlternative` (Finding DD):** investigate using an `AccessTelemetryLog`-backed Postgres VIEW in place of the dedicated Redis observation store — eliminates Redis write amplification (Risk #1) and sidecar-restart loss (Risk #8) in one move. Blocked until (a) `AccessTelemetryLog` backing is confirmed as Postgres and (b) a read-latency benchmark of the VIEW-based approach shows acceptable p95.
    - **`Story-9.3-PercentageRolloutFlag` — R3-6 withdrawn.** A tenant-sample-rate flag is a rewrite (not a feature flag); scope is disproportionate to the theoretical benefit. Rely on the kill switch (Delta #14) as the operational escape hatch; skip progressive rollout.
    - **`Story-9.3-ScheduleDescopePlan` (Finding CC):** if a blocker surfaces during dev that threatens the 10-12d window, graceful de-scope = ship registry endpoint alone (3d), defer mismatch detector to 9.4 (5d). FR62 stays open until 9.4. Dev agent: exercise this escape hatch only if ≥3d of effort has been lost to unforeseen issues and spikes show more to come.
    - **`Story-9.3-CrossTenantVersionConsumerLookup` (R3-1 withdrawal):** a dedicated endpoint for publisher-owners to see "which tenants consume each version of my event type." Requires explicit cross-tenant read permissions (operator-scope authZ). Deferred because the simpler tenant-scoped `VersionMismatch` detection satisfies the operational need inside Story 9.3.
    - **`Story-9.3-PostLaunchCategoryReview` (R3-14):** measure 3 months of post-launch `memories.handlers.mismatches` counter data tagged by category; drop categories showing near-zero operator-acknowledgement or >95% false-positive rate. Target review: 2026-09.
  - [ ] 11.7 **ADR-9.3-001: Observation store uses 3 Redis keys (SET + ZSET + HASH), not Redis Streams.** Create `docs/dev/adrs/adr-9.3-001-observation-store-key-shape.md`. Options: (a) 3-key pattern [CHOSEN], (b) Redis Stream with MAXLEN trim, (c) Postgres append-only table. Trade-offs: (a) familiar to team (mirrors `RedisAggregateCaseMappingStore`), batchable to 1 round-trip, required Fix #5; (b) 1 round-trip, built-in XRANGE windowing, but NEW Redis surface, team less familiar; (c) durable + queryable, but cross-backend. Decision: (a) for consistency with 9.1 + team familiarity — revisit at scale >10k events/sec/tenant.
  - [ ] 11.8 **ADR-9.3-002: `IObservedEventTypeStore` is a separate interface from `RollingCounterStore`.** Create `docs/dev/adrs/adr-9.3-002-telemetry-substrate-separation.md`. Rationale: 5m/5-slot in-process ring (7.5) has different invariants (bounded memory, MeterListener fast path) than 24h Redis-backed store (9.3); coupling them binds future changes. Formalizes what Risk #7 mitigation only hinted at.
  - [ ] 11.9 **ADR-9.3-003: Experimental diagnostic ID allocation — HXL002 for 9.3.** Create `docs/dev/adrs/adr-9.3-003-experimental-diagnostic-id-allocation.md`. Include the convention table: HXL001 (7.5 telemetry summary), HXL002 (9.3 handler registry), HXL003+ (reserved for future). Rationale: separate IDs per surface prevent stabilization-bottleneck coupling. Cross-reference `docs/dev/experimental-apis.md` (create if not present).
  - [ ] 11.10 **ADR-9.3-004 (Finding F — merged): Enum minimalism for operator-facing types.** Create `docs/dev/adrs/adr-9.3-004-operator-enum-minimalism.md`. Consolidates two negative-space decisions: (a) `HandlerSubscriptionStatus` is 3-state (`Active/Unknown/Disabled`) — `Paused` deliberately absent because Hexalith has no pause semantics; adding it would create consumer expectation of a capability that does not exist. (b) `HandlerMismatchSeverity` is 2-valued (`Info/Warning`) — `Error` deliberately absent because no mismatch is action-blocking at the ingestion level; an `Error` severity would be interpreted by downstream paging rules as page-worthy and is not. Merged rationale: "operator-facing enums pay a compounding cost per value" — each additional state is a new `switch` case downstream, a new rendering rule in the CLI, a new test scenario. Minimalism preserves clarity and avoids premature expressiveness. Cross-references Risk #2 canonical "stale" definition + Delta #8 deferred `Story-9.3-SubscriptionStatusConfigured` tracking ref. **(Rationale for merging: Daniel's critique — documenting negative-space is valuable, but 5 ADRs for a read-only feature is gold-plating; one "operator enum minimalism" ADR covers both decisions.)**
  - [ ] 11.11 **ADR-path-validity guard test (Finding H).** Create `tests/Hexalith.Memories.Docs.Tests/AdrsReferenceValidFilePathsTests.cs` — for each ADR under `docs/dev/adrs/`, regex-extract file-path references (anything matching `src/**.cs`, `tests/**.cs`, or inline code-path strings), assert each path exists on disk at test time. A `9.3-001` ADR citing `RedisAggregateCaseMappingStore.cs:19` would fail if that file is renamed without updating the ADR. Prevents ADR rot. Runs in every CI.

## Dev Notes

### Key architectural anchors

- **FR62 — scope bounded.** This story implements FR62 ("Developer can list registered event handlers and detect handler registration mismatches") — the LAST open FR in Epic 9. Epic 9 closes after this story ships. FR59/60/61 ship via 9.1 + 9.2 — do NOT duplicate their concerns here.
- **Read-side-only discipline.** Every new capability (`/api/handlers`, `/api/tenants/{id}/handlers/mismatches`, both CLI commands) is a PURE READ. There is NO write side in this story — no handler-registration endpoint, no runtime subscription mutation. The routing configuration (`EventStoreIntegration:Routing:SourceToTenantMap` in `appsettings.json`) IS the source of truth; changes to it follow the existing runbook at `docs/dev/eventstore-integration.md` §3.2.
- **Observation substrate separation.** 9.3 introduces `IObservedEventTypeStore` (24h rolling, Redis-backed). DO NOT conflate with Story 7.5's `RollingCounterStore` (5m rolling, in-process). The two substrates share no types. Future work that wants to unify them should propose an explicit ADR.

### Observation-store key schema

All keys are `{tenantId}:eventstore:*`-prefixed to match Story 9.1's key-naming ADR (9.1-E) and remain cleanly bounded by tenant-deletion workflows. If tenant-deletion Task 5.x cleans `{tenantId}:*` globally, these keys are covered — verify at impl time by grepping `TenantDeletionWorkflow` + `DeleteRedisVectorActivity` for `KEYS` / `SCAN` patterns.

- **Aggregates index SET (Fix #5):** `{tenantId}:eventstore:observed-aggregates` — members = distinct `{aggregateType}` strings this tenant has observed. Populated via `SADD` in the observation batch; read via `SMEMBERS` by `GetAllObservedTypesAsync`. Replaces the SCAN-based discovery pattern.
- **Sorted set:** `{tenantId}:eventstore:observed:{aggregateType}` — member = `{eventType}`, score = observedAt unix ms.
- **Counter hash:** `{tenantId}:eventstore:observed-count:{aggregateType}` — field = `{eventType}`, value = increment count (single hash per aggregate — `HINCRBY` is atomic per-field, no cross-eventType contention).
- **TTL:** `48h` on every write (2x the 24h window — generous headroom for detector queries that run at the tail of a window). Refreshed on every observation so active handlers never expire.

### Mismatch detector stem-extraction edge cases

- `ClaimSubmittedV2` → stem `ClaimSubmitted` + version `V2`. ✓
- `ClaimSubmitted` (no version suffix) → NO match — not a version-mismatch candidate.
- `ClaimSubmittedV2a` → NO match (suffix must be `V\d+$` fully). Considered NOT a version mismatch (an "a" suffix likely means alpha or variant).
- `ClaimSubmittedV12` → stem `ClaimSubmitted` + version `V12`. ✓ (multi-digit versions supported).
- `MyApp.Claims.ClaimSubmittedV2` (full CloudEvents type) → the regex operates on the TERMINAL segment of the type string; split on `.` and apply regex to `[^.]+$` part. Specifically: `lastSegment = eventType.Split('.').Last()`, then `regex.Match(lastSegment)`. Document this behavior in `§11.2`.
- `V2` alone (degenerate) → stem is empty → SKIP (bogus input).

### CLI empty-state copy conventions

Follow the `TenantListCommand.WriteEmptyTenantsNudge` shape at `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs:68-90` verbatim:
- **JSON format:** no nudge (empty array is the signal).
- **Table format:** nudge to stderr — supports `2>/dev/null` suppression for scripts.
- **Human format:** nudge to stdout (append after the "No handlers found." line).

### Parameter-passing order in `IEventIngestionTelemetry` — why the new param is between aggregateType and outcome

Grouping CloudEvents-envelope-derived fields (`cloudEventId`, `aggregateType`, `cloudEventType`) together makes the signature semantically clustered: envelope-fields → ingestion-outcome-fields. Alternative orderings (append-at-end, after-durationMs) fragment the envelope group. When future stories extend the contract further (say, adding `source` or `subject` as positional), the clustered group makes the pattern obvious.

### Why the 24h observation window is hardcoded (Fix #10)

The window is **hardcoded at `TimeSpan.FromHours(24)` at the endpoint layer** for this story. Rationale:

1. **Shared-nothing between tenants.** The observation store is per-tenant (key prefix `{tenantId}:eventstore:observed-*`) and TTL-self-expiring at 48h. Making the window configurable per-tenant complicates the TTL (TTL MUST exceed the widest possible window across all tenants sharing the Redis instance), and global-config makes the knob awkward — not obviously owned by any tenant.
2. **Alternate windows can be computed client-side.** The detector already emits all observations; downstream queries could bucket them differently. Making 24h the server-side inference default does not preclude client-side re-aggregation.
3. **24h is the smallest window that survives a weekend** for low-volume streams (Friday-afternoon event → Monday-morning inspection). Shorter would false-positive on weekly patterns; longer would delay mismatch detection for high-volume streams.

**Deferred follow-up — log as `deferred-work.md` entry when this story lands:** "Story 10.x or beyond — make the observation window operator-configurable via `EventStoreIntegration:Routing:ObservationWindow` (default 24h). Requires TTL-widening of `RedisObservedEventTypeStore` keys to `2 * max(configuredWindows)` + a `/api/handlers/observation-window` diagnostic endpoint for operators to verify the live value. Blocked until an operator explicitly requests non-24h windows in a real deployment."

### Why `HXL002` and not reuse of `HXL001`

Story 7.5's `HXL001` applies to `TelemetrySummary` surface. Conflating `HandlerRegistrationSnapshot` + `HandlerMismatchReport` under `HXL001` would mean a future stabilization of ONE surface forces a co-stabilization of the OTHER (or per-type suppression gymnastics). Separate diagnostics give each surface its own lifecycle. `HXL002` is the next reserved diagnostic in the Hexalith experimental-id allocation (verify at impl time — see `docs/dev/experimental-apis.md` if it exists). Formalized in **ADR-9.3-003** (Task 11.9).

### Rejected alternative — Redis Streams for the observation substrate (Delta #6)

**Considered:** Replace the 3-key pattern (`SET + ZSET + HASH`) with a single Redis Stream per aggregate (`{tenantId}:eventstore:observed-stream:{aggregateType}`) using `XADD` + `XTRIM MAXLEN ~ window-sized` for the window + `XLEN` for the counter + `XRANGE` for the recency query.

**Pro:** one command per observation (vs three); built-in window-trim semantics via `MAXLEN`; eliminates the `SMEMBERS` discovery problem entirely (`SCAN` over stream keys is still needed to discover aggregates, but `XINFO GROUPS` or a Stream-per-tenant pattern could replace that).

**Con (chosen reason to reject):** (1) Streams is a NEW Redis surface for this team — existing stores (`RedisAggregateCaseMappingStore`, `RedisPreflightDedupStore`) use classic data structures; a Streams introduction increases the cognitive-load and test-surface area at a point where 9.1 through 9.3 are stabilizing. (2) MAXLEN is "approximate" without the `=` modifier and costs more latency with it — our per-batch write-amplification is already Risk #1; introducing a latency-sensitive new pattern multiplies the risk without proportionate gain. (3) The 3-key pattern's `SMEMBERS` issue is already solved by Fix #5 (aggregates-index SET) — the remaining trade-off is "one extra command per write," which is absorbed by `IDatabase.CreateBatch()`.

**Decision:** 3-key pattern chosen. Decision revisited if (a) team adds Streams-using code elsewhere (reducing the cognitive-load argument) OR (b) observation write-amplification exceeds 10× Redis RPS budget in prod. Formalized as **ADR-9.3-001** (Task 11.7).

### Stale phrasing unification (Delta #16)

Across the spec, "stale handler" is defined exactly ONE way: **`IObservedEventTypeStore.GetAllObservedTypesAsync(tenantId, TimeSpan.FromHours(24)).Result.Count == 0`** for a given `SourceToTenantMap` entry routed to this tenant. All other phrasings ("no events received," "zero events in 24h," "traffic is zero") are informal paraphrases that MUST NOT be used to re-derive the check in code. Risk #2, AC #4, §10 StaleHandler detection, and `HandlerMismatchDetectorTests` all refer to this canonical phrasing.

### Version-mismatch detection — regex vs attribute approach (Delta #7)

The current story uses a regex (`^(.+?)(V\d+)$`) on the terminal segment of the event-type name to extract the stem + version. This is the pragmatic approach because it requires ZERO publisher contract change.

**Future approach — publisher-declared version attribute:** `[EventType("ClaimSubmitted", Version = 2)]` on the event record, surfaced via `ReflectionTypeLoader` at startup. Detection becomes an O(1) dictionary lookup (`stems[type] = {version, count}`) with no ReDoS surface, no length cap, no regex timeout, no dedicated event id 9141. Why deferred: requires coordinating a convention change with every publisher repo in the ecosystem — a cross-cutting coordination cost that is disproportionate to the VersionMismatch feature's incremental value. Logged as `deferred-work.md` entry `Story-9.3-VersionMismatchAttributeApproach` and tracked in **ADR-9.3-001 / addendum**.

### Subscription-status — why 3-state not 4-state (Delta #8)

The current enum is `{Active, Unknown, Disabled}`. An alternative 4-state enum `{Configured, Active, Unknown, Disabled}` would disambiguate "routing is set up but has never seen events" (Configured) from "routing is set up and has seen events" (Active). The 3-state choice is intentional for 9.3:
- Adding `Configured` expands the operator's mental model (now 4 states to reason about) without solving an actual reported incident.
- The `EventsProcessedCount` + `LastEventAt` per-row data already surfaces "seen events" information.
- The ADR-9.3-004 decision is explicitly revisitable; adding `Configured` is a non-breaking ENUM extension at the JSON layer (new value) but IS a breaking change at the C# `enum` level (downstream `switch` statements need a new case).

If operator feedback post-landing indicates the 3-state model is ambiguous, Story 10.x adds `Configured` as the 4th state. Logged as `Story-9.3-SubscriptionStatusConfigured` deferred-work.

### `--since` CLI flag for low-volume tenants (Delta #2 — deferred)

Low-volume publishers (e.g., one event per week) will trigger `StaleHandler` Info mismatches every day — the noise ratio makes Info-filtered dashboards lose signal. The planned-but-deferred answer is `memories handlers mismatches --tenant X --since 7d` which widens the observation window to 7 days (or an operator-chosen duration) for this invocation ONLY. Why deferred: requires the observation store's TTL to be widened to `2 × max(window)` globally OR a dedicated "expanded-window store" — either path is a material cost. Tracked as `Story-9.3-SinceFlagForLowVolume`.

### Hindsight-affirmed decisions (R3-12, R3-13)

Two Round-2 additions were reviewed under Hindsight Reflection and AFFIRMED as load-bearing:

- **`HandlerRegistration.Error` field (R3-12 / Finding S):** simulated 2027 postmortem concluded that operators presented with 10 error-rows + 90 healthy-rows drilled into exactly the 10 failing tenants — far better UX than a blanket 500 that hides the healthy majority. Keep the field.
- **Kill switch (R3-13 / Delta #14):** simulated 2027 Q3 Redis degradation incident: kill switch took 30 seconds to disable vs 20 minutes for a rollback deploy. Keep the switch.

The remaining Round-2 additions that did NOT survive Hindsight Reflection are the cuts captured in R3-1, R3-2, R3-4.

### Three mismatch categories — ship-then-measure posture (R3-14)

The story ships all three categories (`UnhandledEventType`, `StaleHandler`, `VersionMismatch`) at once. **The Shark Tank + Hindsight rounds flagged a risk:** `StaleHandler` may turn out to be noise for low-volume tenants (false-positive frequency per weekly-publishing pattern) and `VersionMismatch` may fire rarely with low operator-intervention value. **Acceptance posture:** ship all three, instrument via `memories.handlers.mismatches` counter (tagged by category), measure post-launch. If 3 months of telemetry shows that a category has either (a) near-zero operator acknowledgement (no one drills into its rows) or (b) >95% false-positive rate (StaleHandler likely candidate), open a follow-up story to drop or relocate that category to a separate endpoint. Do NOT pre-emptively cut categories based on speculation — cut based on measured telemetry. The three-category decision is therefore EXPLICITLY REVISITABLE and tracked via `deferred-work.md` entry `Story-9.3-PostLaunchCategoryReview` (target: 2026-09 or later).

### Observation window boundary semantics (R3-10)

The 24h window is **inclusive-start, exclusive-end relative to "now."** An observation at timestamp T is included in a read at time (now) if `T ≥ now - 24h` — tightly: `ZRANGEBYSCORE minScore=(now - 86_400_000ms) maxScore=+inf`. Edge cases:
- Observation at T, read at T + 24h - 1ms → **included**.
- Observation at T, read at T + 24h exactly → **included** (minScore comparison is `>=`).
- Observation at T, read at T + 24h + 1ms → **excluded**.

**Boundary flap warning:** if an operator runs `memories handlers mismatches` twice at T + 24h and T + 24h + 2ms, they may see a StaleHandler appear in the second run that wasn't in the first. This is correct behavior, not a bug, but document it in §11.3 so operators reading back-to-back snapshots understand the ±ms transition.

### TTL vs observation window — independent mechanisms (Finding I)

Reading the spec, a dev may conflate two separate mechanisms:

1. **Redis TTL (48h)** — the store's SELF-CLEANUP. Every write refreshes the TTL to `now + 48h` via `KeyExpireAsync`. If a tenant stops emitting events entirely, its keys self-expire after 48h with no external cleanup required. TTL bounds the store's growth but does NOT implement the "window" semantics.

2. **Observation window (24h)** — the CUTOFF applied at READ TIME via `ZRANGEBYSCORE (now - window) → +inf`. The window selects which observations are "current" for detection purposes. It is NOT a deletion mechanism; older-than-window entries still exist in Redis (until TTL expires them at 48h) — they are simply excluded from detector output.

Why two mechanisms? TTL protects the store from unbounded growth if a tenant becomes inactive; the window gives operators a stable "recent" reading regardless of underlying TTL jitter. The 2× relationship (TTL = 2 × window) provides headroom so a query at T + 24h - 1ms still sees data that would have been in-window at T.

### Why the stem comes from the TERMINAL segment (Finding J)

Event type strings follow CloudEvents convention: `{namespace}.{aggregate}.{eventName}` (e.g., `MyApp.Claims.ClaimSubmittedV2`). The version attaches to the EVENT NAME, not the namespace. Applying the stem regex to the namespace or aggregate portion would (a) find nothing useful (namespaces don't version with `V\d+`), (b) risk false-positive matches on coincidental prefix-suffix patterns. The terminal-segment split makes the regex operate on the portion that actually carries version semantics. Documented here because it is NOT obvious from reading the regex alone why the `.Split('.').Last()` step exists.

### Why 256 for the observation-write semaphore (Finding K)

The `SemaphoreSlim(256)` cap is a heuristic chosen by the following reasoning:

1. **.NET thread-pool defaults:** `ThreadPool.GetMinThreads()` returns `(Environment.ProcessorCount, Environment.ProcessorCount)` at cold start and grows lazily. 256 concurrent observation writes is O(8-16× ProcessorCount) on typical 16-32 core boxes — enough overhead to absorb transient bursts but not so large that we mask pathological Redis slowness.
2. **Expected steady-state RPS:** if ingestion runs at 1000 events/sec with p95 observation-write latency of 5ms, the expected in-flight concurrency is ~5. 256 is 50× headroom.
3. **Escalation threshold:** if observed p95 observation-write latency degrades to 256ms (i.e., 256 in-flight × 1s = the cap is saturated), dropped-counter starts firing and Finding G's 9144 surfaces the issue.

**Revisit the cap if:** (a) production load benchmarks show >1000 events/sec per pod sustained, (b) dropped-counter emits more than 0.01%/1h persistently (9144 is the signal), (c) Redis write latency baseline shifts significantly. **Do not** make the cap config-driven until one of these triggers — YAGNI.

### Glossary (Finding Z)

For operators and non-specialist devs reading this spec:

- **CloudEvents:** standardized event envelope format (CNCF spec). A CloudEvent has required headers (`id`, `source`, `type`, `specversion`) plus optional extension headers.
- **Event type / `type`:** CloudEvents `type` header (e.g., `MyApp.Claims.ClaimSubmittedV2`). Identifies the SEMANTIC meaning of the event; distinct from the transport-level topic.
- **Source prefix:** the CloudEvents `source` header (URL-like, e.g., `https://publisher.acme.com/claims`). Routing maps match against the START of this string.
- **`SourceToTenantMap`:** `Dictionary<string, string>` — key = source-prefix (longest-prefix match wins), value = tenant-id. Configured in `appsettings.json`.
- **Aggregate / aggregateType:** DDD term. The domain object the event relates to (e.g., `Claim`). Extracted from the CloudEvents `type` by convention (the segment before the final `.`).
- **Terminal segment / stem:** The FINAL `.`-separated segment of an event type; e.g., `ClaimSubmittedV2` in `MyApp.Claims.ClaimSubmittedV2`. The STEM is that segment with its trailing `V\d+` suffix stripped (`ClaimSubmitted`).
- **Observation store:** Redis-backed 24h rolling counter of which event types the system has seen per tenant. Populated fire-and-forget from `EventIngestionService`; read by `HandlerRegistryService` + `HandlerMismatchDetector`.
- **Dapr pub/sub:** Dapr's message-broker abstraction. `TenantEventRoutingOptions.PubSubName` + `.Topic` configure which broker + which channel.
- **Fire-and-forget:** a Task is started without awaiting; the ingestion response returns immediately, the task finishes asynchronously. Used deliberately here for observation writes so ingestion latency is never gated on the store's write latency.
- **HXL001 / HXL002:** Hexalith experimental-API diagnostic IDs. Code marked `[Experimental("HXL002")]` emits a compile-time warning unless the consumer explicitly suppresses the diagnostic — the warning signals "this API may change without a major-version bump."

### API surface experimental-header convention (Delta #5)

Both new endpoints return `X-Memories-API-Experimental: HXL002` in the response headers on every 2xx response. This gives raw-HTTP consumers (`curl`, Postman, bespoke integrations) visibility of the stability posture they silently miss today (the `[Experimental("HXL002")]` attribute only fires a compile-time warning on SDK callers). If 9.4 graduates `HXL002` to stable, the header is removed in the same release. Convention extension candidate: `X-Memories-API-Experimental: HXL001,HXL002` when a single endpoint touches multiple experimental surfaces.

### References

- Epic 9 Story 9.3: `_bmad-output/planning-artifacts/epics.md:1733-1757` [Source: epics.md#Story 9.3]
- FR62 source of truth: `_bmad-output/planning-artifacts/epics.md:99` + :291
- Story 9.1 shipped: `src/Hexalith.Memories.EventStore/` (this is the package to extend — reference `9-1-event-auto-discovery-and-dapr-pub-sub-subscription.md` status = `review`)
- `EventIngestionService`: `src/Hexalith.Memories.EventStore/EventIngestionService.cs:56-193`
- `TenantEventRoutingOptions`: `src/Hexalith.Memories.EventStore/TenantEventRoutingOptions.cs`
- `IEventIngestionTelemetry`: `src/Hexalith.Memories.EventStore/IEventIngestionTelemetry.cs`
- `EventIngestionTelemetryAdapter`: `src/Hexalith.Memories.Server/EventStoreIntegration/EventIngestionTelemetryAdapter.cs`
- `RedisAggregateCaseMappingStore` (pattern to follow): `src/Hexalith.Memories.EventStore/RedisAggregateCaseMappingStore.cs`
- `MemoriesMeter`: `src/Hexalith.Memories.Telemetry/MemoriesMeter.cs:49-108`
- `TelemetrySummaryService` (shape to mirror): `src/Hexalith.Memories.Server/Telemetry/TelemetrySummaryService.cs:34-76`
- `RootCommandFactory` (CLI root to edit): `src/Hexalith.Memories.Cli/Commands/RootCommandFactory.cs:80` (CommandGroups entry to remove) + :120-132 (wiring pattern to copy)
- `TenantListCommand` (CLI command to mirror): `src/Hexalith.Memories.Cli/Commands/TenantListCommand.cs`
- `MemoriesClient.GetTelemetrySummaryAsync` (client pattern + EXPERIMENTAL attribute usage): `src/Hexalith.Memories.Client.Rest/MemoriesClient.cs:587-612`
- Program.cs endpoint wiring location: `src/Hexalith.Memories.Server/Program.cs:2906` (insert after telemetry summary endpoint)
- `TenantEndpointHandlers.GetTenantConfigurationAsync` (minimal-API guard pattern to mirror): `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs:70-100`
- Story 9.2 spec (patterns for `MemoriesJsonContextCompletenessTests` + `DocumentationCompletenessTests`): `_bmad-output/implementation-artifacts/9-2-dual-embedding-and-causal-chain-indexing.md`
- EventStore integration docs (where §11 is added): `docs/dev/eventstore-integration.md`
- CLI config docs (where `handlers` subsection is added): `docs/dev/cli-config.md`

## Dev Agent Record

### Agent Model Used

_To be filled in by the dev agent when implementation begins._

### Debug Log References

_To be populated during implementation._

### Completion Notes List

_To be populated during implementation. Expected entries:_
- Spike 0.1 outcome: whether `MemoriesJsonContextCompletenessTests` existed or was created here.
- Spike 0.2 outcome: the chosen `IDatabase.CreateBatch()` pattern.
- Spike 0.3 outcome: the exact `TenantStatusGuard` null-check shape.
- Spike 0.4 outcome: whether an `IOptionsMonitor<T>` change-notification guard test was added.
- Any follow-ups discovered that belong in `deferred-work.md`.

### File List

_To be populated during implementation._
