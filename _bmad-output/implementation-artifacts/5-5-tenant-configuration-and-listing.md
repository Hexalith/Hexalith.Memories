# Story 5.5: Tenant Configuration & Listing

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** operator-facing tenant listing with per-backend counts + index health, a composed configuration view, a `PATCH` endpoint for `displayName` / `rateLimitPerMinute`, and FR70 propagation of `embeddingModel` onto memory units.

**What does NOT ship:** audit persistence (logs only), reindex workflow (`reindexRequired` is advisory), caching, pagination, CLI (Epic 7), customer-facing change visibility.

**Primary risks:** contract change on `GET /api/tenants`, first direct-write-to-registry-from-endpoint (pattern shift vs. 5-1 through 5-4 workflows), FR70 schema addition on `MemoryUnit`/`IndexInput`/Redis hash.

## Breaking Changes (Pre-Gate-2 MVP)

1. **`GET /api/tenants` response shape changes** from `TenantInfo[]` → `TenantSummary[]`. Acceptable because no production consumers exist pre-Gate-2 and `TenantSummary` is a field superset. Integration tests in the Aspire fixture must be rewritten. See Task 1.1 + "Current Endpoint State (Baseline)" section for rationale.
2. **`IndexInput.EmbeddingModel` added as `required`** — any internal constructor of `IndexInput` that does not supply it will fail to compile. Grep confirmed the single construction site is in the ingestion workflow; Task 4.5 threads the model through. No external callers affected (internal contract).

## Story

As an operator,
I want to list tenants with usage/health details, view each tenant's full configuration, and update rate limits and display name after creation,
so that I can effectively manage the multi-tenant environment without having to inspect infrastructure backends directly.

## Acceptance Criteria

1. **Given** a multi-tenant deployment with one or more registered tenants, **when** the operator calls `GET /api/tenants`, **then** every tenant is returned with: `id`, `displayName`, `status`, `createdAt`, `memoryUnitCount`, per-backend `indexSizes` (RediSearch key count, Redis Vector key count, FalkorDB node count), `reindexRequired` (from the tenant's embedding config), and `lastActivityAt` (nullable) (FR41). Missing / unavailable backend counts are reported as `null` with a `backendAvailability` flag rather than failing the whole request.

2. **Given** an existing tenant, **when** the operator calls `GET /api/tenants/{tenantId}/configuration`, **then** the response includes: the full `TenantEmbeddingConfig` (`provider`, `model`, `dimensions`, `rateLimitPerMinute`, `apiSecretKeyName`, `reindexRequired`), per-backend `indexStatus` (ready / missing / degraded / unknown), `lastActivityAt`, `memoryUnitCount`, `createdAt`, `displayName`, `status` (FR45). For unknown tenants the response is `404` `TENANT_NOT_FOUND`; `ValidateTenantExistsAsync` guard is applied first. **Note on `apiSecretKeyName`:** this is the *name/identifier* of the secret in the secret store (e.g., `"google-embedding-api-key"`), not the secret value. It is safe to return. The contract XML doc on `TenantEmbeddingConfig.ApiSecretKeyName` must state this explicitly so future readers don't mistake it for a sensitive field.

3. **Given** an existing tenant in `Active` status, **when** the operator calls `PATCH /api/tenants/{tenantId}` with an updated `displayName` and/or `rateLimitPerMinute`, **then** (a) non-breaking changes are applied immediately, (b) the updated values are returned in the response, (c) an **operational log** entry at `Information` level is emitted via `[LoggerMessage]` source generator with the fields `tenantId`, `actor`, `field`, `oldValue`, `newValue`, `occurredAt` (FR42). **Terminology note:** the PRD says "recorded in the tenant's audit trail"; in MVP this is an **operational log** (not a compliance-grade, tamper-evident audit trail). A durable tenant audit store is Phase 2. The chosen field names (`tenantId`, `actor`, `field`, `oldValue`, `newValue`, `occurredAt`) are **pinned to match the anticipated Phase 2 audit event contract** so the migration is a one-for-one field remap rather than a rewrite. The `TenantStatusGuard.ValidateTenantActiveAsync` guard applies — non-Active tenants are rejected with the existing `TENANT_*` 404/409 error codes.

4. **Given** a configuration change that would create data inconsistency (e.g., changing `embedding dimensions`, `provider`, or `model` without reindex), **when** the change is submitted to `PUT /api/tenants/{tenantId}/embedding-config` without `forceReindex=true`, **then** the system rejects it with HTTP `409 Conflict`, error code `EMBEDDING_CONFIG_BREAKING_CHANGE`, and a response body that names the affected fields, the current values, and the proposed values (FR43). The operator must re-submit with `forceReindex=true` to acknowledge the reindex requirement and proceed.

5. **Given** per-tenant rate limit ceilings are configured, **when** the `EmbeddingRateLimiterActor` enforces limits during ingestion, **then** it uses the tenant's currently-configured `rateLimitPerMinute` value from `TenantConfigurationActor` (FR69). Changes made via `PUT /api/tenants/{tenantId}/embedding-config` or `PATCH /api/tenants/{tenantId}` propagate to the rate limiter on the next embedding request (the limiter's `SetCeilingAsync` is called on each `GenerateEmbeddingActivity` invocation — this is already the established pattern).

6. **Given** a memory unit is ingested, **when** it is indexed, **then** both the embedding `provider` AND the embedding `model` used are recorded as durable fields on the memory unit, visible in `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` responses (FR70). This enables future auditing of which vectors were generated by which (provider, model) pair.

## Tasks / Subtasks

- [ ] Task 1: Enriched tenant listing endpoint (AC: #1)
  - [ ] 1.1 Create a new contract `Hexalith.Memories.Contracts.V1.TenantSummary` as a `public sealed record` with: `Id`, `DisplayName`, `Status`, `CreatedAt`, `long? MemoryUnitCount`, `TenantIndexSizes IndexSizes`, `TenantBackendAvailability BackendAvailability`, `bool ReindexRequired`, `DateTimeOffset? LastActivityAt`. Use `DateTimeOffset?` for any optional timestamps; `MemoryUnitCount` is nullable so a Redis-unavailable case can report `null` instead of a misleading zero. **Do NOT modify `TenantInfo`** — it's the canonical minimal tenant record used by workflows and actors. `TenantSummary` is an enriched view for operator listing. **Contract change is listed in the top-of-file "Breaking Changes" section.**
  - [ ] 1.2 Create `Hexalith.Memories.Contracts.V1.TenantIndexSizes` record with `long? RediSearchKeyCount`, `long? RedisVectorKeyCount`, `long? FalkorDbNodeCount`. Nulls indicate the corresponding backend was unavailable.
  - [ ] 1.3 Create `Hexalith.Memories.Contracts.V1.TenantBackendAvailability` record with `bool RediSearch`, `bool RedisVector`, `bool FalkorDb`.
  - [ ] 1.4 Register both new contracts in `MemoriesJsonContext.cs` for AOT-friendly serialization. Follow the existing `[JsonSerializable(typeof(...))]` pattern.
  - [ ] 1.5 Create `TenantMetricsService` in `Server/Tenants/` that computes the enriched fields for a single tenant given a tenantId. Methods:
    - `GetMemoryUnitCountAsync(string tenantId, CancellationToken ct)` — counts Redis keys matching `{tenantId}:mu:*` via `SCAN` (bounded `COUNT` hint ≈ 1000, do NOT use `KEYS`). Return `long?` (null if Redis unavailable).
    - `GetIndexSizesAsync(string tenantId, CancellationToken ct)` — returns `(TenantIndexSizes, TenantBackendAvailability)`. Queries RediSearch index stats (`FT.INFO {tenantId}:syntactic` → `num_docs`), Redis Vector index stats (`FT.INFO {tenantId}:semantic` → `num_docs`), FalkorDB node count (`MATCH (n) RETURN count(n)` — constant infra query, acceptable per D9 audit, mirror `VerifyTenantActivity` pattern). Each backend failure is caught internally and reported as `null` + `availability=false`. Do NOT throw.

    **`FT.INFO` parse pattern** (no codebase precedent — include this to prevent trial-and-error): `FT.INFO` returns a `RedisResult` array of alternating key/value pairs. Minimal parse shape:
    ```csharp
    // _redis is IConnectionMultiplexer keyed "redis"; db = _redis.GetDatabase()
    RedisResult result;
    try
    {
        result = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);
    }
    catch (RedisServerException ex) when (ex.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase))
    {
        return (Count: null, Availability: true, Health: IndexHealth.Missing);
    }
    catch (RedisConnectionException)
    {
        return (Count: null, Availability: false, Health: IndexHealth.Unknown);
    }
    RedisResult[] pairs = (RedisResult[])result!;
    long? numDocs = null;
    for (int i = 0; i < pairs.Length - 1; i += 2)
    {
        string? fieldName = (string?)pairs[i];
        if (fieldName == "num_docs" || fieldName == "num_records")
        {
            // Redis server versions vary: some return num_docs as a RedisValue integer,
            // some as a string. Guard both via TryParse rather than an unchecked cast.
            string? raw = (string?)pairs[i + 1];
            if (long.TryParse(raw, out long parsed)) { numDocs = parsed; }
            else if (pairs[i + 1].Type == ResultType.Integer) { numDocs = (long)pairs[i + 1]; }
            break;
        }
    }
    IndexHealth health = numDocs.HasValue ? IndexHealth.Ready : IndexHealth.Degraded;
    return (Count: numDocs, Availability: true, Health: health);
    ```
    Note: `num_docs` is the canonical field in newer RediSearch / Redis Stack Vector; `num_records` is the older alias. Handle both. If the field is present but unparseable, report `Degraded` (not `Ready`).
  - [ ] 1.6 Update `GET /api/tenants` endpoint in `Program.cs` to project `TenantInfo` → `TenantSummary` in parallel (use `Task.WhenAll` to fetch per-tenant enrichment concurrently). Endpoint should tolerate per-tenant enrichment failure — a tenant whose metrics cannot be fetched still appears in the list with nulls and `backendAvailability` flags set to false.

    **Read-through state-store bypass (DAPR optimization):** For the list endpoint, source `ReindexRequired` and the embedding config via `DaprClient.GetStateAsync<TenantEmbeddingConfig>("statestore", key: <actor-state-key>, ct)` **directly**, NOT through `ITenantConfigurationActor.GetEmbeddingConfigAsync()`. Actor-proxy fan-out across N tenants triggers N actor activations, each serialized by the actor runtime — catastrophic for list latency. The actor's state key (`TenantConfigurationActor` uses `StateName = "embeddingConfig"` in its state manager) is readable directly from the underlying DAPR state store. **Caveats:** (a) state-store reads bypass actor validation in `TryGetStoredEmbeddingConfigAsync` — if a stored config is corrupted, the bypass returns raw data; the endpoint must treat unreadable config as `null` and set `reindexRequired=false`, `provider/model=null` in the summary; (b) keep actor proxy for single-tenant `GET /configuration` and for `PUT`. Document this bypass with an inline comment referencing this story's Dev Notes.

    `LastActivityAt` sourced per amendment A (O(1) Redis read — see Task 2.3 below).
  - [ ] 1.7 **Performance guard:** listing N tenants triggers ~N×4 backend calls (Redis SCAN + 2×FT.INFO + FalkorDB node count + actor read for config). Additionally, the FalkorDB `MATCH (n) RETURN count(n)` query is **O(|V|)** — a full-graph scan per tenant. For MVP scale (tenant count < ~100, graph size bounded by a few hundred thousand nodes) this is acceptable; optimization via caching with TTL or a materialized metric is explicitly deferred. Add a one-line code comment marking this. Do NOT add caching now (anti-pattern: speculative complexity).

- [ ] Task 2: Tenant configuration view endpoint (AC: #2)
  - [ ] 2.1 Create contract `Hexalith.Memories.Contracts.V1.TenantConfigurationView` with fields: `Id`, `DisplayName`, `Status`, `CreatedAt`, `DateTimeOffset? LastActivityAt`, `long? MemoryUnitCount`, `TenantEmbeddingConfig EmbeddingConfig`, `TenantIndexStatus IndexStatus`, `TenantBackendAvailability BackendAvailability`. **Return the full `TenantEmbeddingConfig` directly** — do NOT introduce a duplicate projection record. Rationale: `apiSecretKeyName` is a non-sensitive secret-name reference (not the secret value); maintaining two near-identical shapes is debt. Update the XML doc on `TenantEmbeddingConfig.ApiSecretKeyName` to clarify its non-sensitive nature.
  - [ ] 2.2 Create `Hexalith.Memories.Contracts.V1.TenantIndexStatus` record with `IndexHealth RediSearch`, `IndexHealth RedisVector`, `IndexHealth FalkorDb`. Define `IndexHealth` enum with four values:
    - `Ready` — backend responded, index exists, `num_docs` (or node count) present.
    - `Missing` — backend responded, but the expected index/graph is absent (e.g., `FT.INFO` returns "no such index"; `GRAPH.LIST` doesn't include the tenant id). This is the signal for "provisioning incomplete" or "index dropped after deletion".
    - `Degraded` — backend responded, but the response indicates reduced capability: (a) the response payload is well-formed but `num_docs`/node-count field is absent or unparseable, OR (b) the server returns a `LOADING`/`BUSY` error response. **Not** used for timeouts or connection failures — those are `Unknown`.
    - `Unknown` — backend unreachable (connection timeout, `RedisConnectionException`, DAPR sidecar 503). Indicates availability failure, not data state.

    Register enum in `MemoriesJsonContext` with `CamelCaseStringEnumConverter`.
  - [ ] 2.3 Extend `TenantMetricsService`:
    - `GetIndexStatusAsync(string tenantId, CancellationToken ct)` — for each backend: `Ready` if `FT.INFO` / `GRAPH.LIST` returns an index with the expected name, `Missing` if the call succeeds but the index is not found, `Degraded` if the call returns a partial / error response, `Unknown` if the backend is entirely unreachable. Must NEVER throw.
    - `GetLastActivityAtAsync(string tenantId, CancellationToken ct)` — returns the tenant's last-activity timestamp. **Implementation: dedicated Redis key `{tenantId}:lastActivityAt` holding an ISO-8601 UTC timestamp (or a ticks-as-long), updated atomically on every successful ingest write in `IndexSyntacticActivity.RunAsync` via `db.StringSetAsync` (fire-and-forget / no await blocking the activity). `GetLastActivityAtAsync` reads via `db.StringGetAsync`. Returns `null` if the key does not exist (fresh tenant or key evicted).
      - **DO NOT** bound-sample over `{tenantId}:mu:*` keys; that produces wrong values for tenants with more memory units than the sample cap.
      - **DO NOT** route this through DAPR state store — it's a hot per-ingest write path; Redis direct write is simpler and cheaper.
      - Failure to write the timestamp during ingest is not a blocking error — log a `Warning` but do not fail the ingestion workflow. A stale `lastActivityAt` is acceptable; a failed ingest is not.
  - [ ] 2.4 Add endpoint `GET /api/tenants/{tenantId}/configuration` in `Program.cs`:
    - Validate via `ValidateTenantId` (format) + `TenantStatusGuard.ValidateTenantExistsAsync` (registry). Route errors through `TenantStatusGuard.ToHttpResult`.
    - Fetch `TenantEmbeddingConfig` via existing `ITenantConfigurationActor.GetEmbeddingConfigAsync()`.
    - Fetch metrics via `TenantMetricsService`. Compose `TenantConfigurationView`. Return `Results.Ok`.
  - [ ] 2.5 Register `TenantMetricsService` as singleton DI in `Program.cs` (stateless, reads from Redis/FalkorDB via keyed DI — `"redis"` for Redis, `"falkordb"` for FalkorDB) near existing `TenantStatusGuard` / `TenantRegistryService` registrations. Do NOT create a new DI extension method (anti-pattern: premature abstraction for a single registration).

- [ ] Task 3: Update display name and rate limit (AC: #3)
  - [ ] 3.1 Create contract `Hexalith.Memories.Contracts.V1.TenantUpdateInput` with nullable fields `string? DisplayName`, `int? RateLimitPerMinute`. Both optional — the operator sends only what they're updating.
  - [ ] 3.2 Add a public method `TenantRegistryService.UpdateTenantDisplayNameAsync(string tenantId, string actor, string displayName, CancellationToken ct)` that uses the existing ETag-based CAS pattern (see `RegisterOrGetTenantEntryAsync` for the template — `GetStateAndETagAsync` + `TrySaveStateAsync` + retry loop capped at `MaxTenantRegistrationRetries` = 3). Preserve all other registry entry fields.

    Emit two `[LoggerMessage]` entries at `Information`, with **pinned field names** (Amendment J — future-proofing for Phase 2 `ITenantAuditStore`):
    ```csharp
    [LoggerMessage(EventId = 5501, Level = LogLevel.Information,
      Message = "Tenant operational log: {TenantId} field={Field} oldValue={OldValue} newValue={NewValue} actor={Actor} occurredAt={OccurredAt:o}")]
    private static partial void LogTenantFieldUpdated(
        ILogger logger, string tenantId, string field, string oldValue, string newValue,
        string actor, DateTimeOffset occurredAt);
    ```
    **Field-name contract (do not rename):** `tenantId`, `field`, `oldValue`, `newValue`, `actor`, `occurredAt`. These names match the anticipated Phase 2 audit event record so migration is a one-to-one map. For this MVP, `actor` is whatever identity is available from the HTTP context; if no identity middleware is wired, pass the literal string `"operator"` — the field exists for future filling without needing to change the log signature later.

    Apply the same field-name contract to the rate-limit update path (Task 3.4) — emit a second `[LoggerMessage]` for `rateLimitPerMinute` changes using the same parameter names.
  - [ ] 3.3 Rate-limit update: do NOT add a separate field on `TenantRegistryEntry`. Update via the existing `PUT /api/tenants/{tenantId}/embedding-config` flow — setting `rateLimitPerMinute` there is a non-breaking change (not in `EmbeddingProviderDefaults.GetBreakingChangeFields`). The `PATCH` endpoint in step 3.4 should delegate the rate-limit field to `ITenantConfigurationActor.SetEmbeddingConfigAsync` preserving the other fields of the current config.
  - [ ] 3.4 Add endpoint `PATCH /api/tenants/{tenantId}` in `Program.cs`:
    - Validate via `ValidateTenantId` + `TenantStatusGuard.ValidateTenantActiveAsync` (must be Active — `ToHttpResult` routes errors).
    - Reject `null` body with `400 INVALID_INPUT`.
    - Reject updates where both `DisplayName` and `RateLimitPerMinute` are null with `400 INVALID_INPUT`, suggestion "Provide at least one of: displayName, rateLimitPerMinute".
    - Validate `DisplayName`: non-empty, length 1-100 chars, no control characters. Reuse the validation from `TenantProvisioningInput` if it already validates display names; if not, inline a minimal `ArgumentException.ThrowIfNullOrWhiteSpace` + length check. Do NOT create a new `DisplayNameValidator` class (anti-pattern: one-time helper).
    - Validate `RateLimitPerMinute`: > 0, ≤ Google provider ceiling. Reuse `EmbeddingProviderDefaults.Validate` by constructing a candidate `TenantEmbeddingConfig` via `current with { RateLimitPerMinute = input.RateLimitPerMinute.Value }` and calling `Validate`.
    - If both fields present, apply them in order: display name first (registry write), then rate limit (actor write). Each is independent; a failure on rate limit should NOT roll back the display name change. Document this explicitly in an inline comment: "No distributed transaction — operator can re-run the failed portion."
    - Return `200 OK` with the updated `TenantSummary` (reuse Task 1's projection).
    - **Verify PATCH routing works on Aspire:** ASP.NET Core Minimal APIs default to standard HTTP methods; `MapPatch` is supported. After implementation, smoke-test by hitting `PATCH /api/tenants/{id}` against the running Aspire fixture — some reverse proxies (including AppHost's default setup) may strip non-GET/POST methods. If PATCH is blocked, fall back to `POST /api/tenants/{id}/updates` with the same `TenantUpdateInput` body. Document the chosen verb in the Dev Agent Record.
  - [ ] 3.5 **Audit trail scope clarification:** the "audit trail" in AC3 is satisfied by structured `Information` log entries using `[LoggerMessage]` source generator. Do NOT create a dedicated `TenantAuditStore` / `TenantAuditEvent` persistence layer. MVP limitation already documented in 5-4 Dev Notes: "No audit trail for tenant access: Access telemetry is structured logging only in MVP. Dedicated audit store is Phase 2." Consistent with that decision.

- [ ] Task 4: Embedding model field propagation (AC: #6, FR70)
  - [ ] 4.1 Add `public string? EmbeddingModel { get; init; }` to `Hexalith.Memories.Contracts.V1.MemoryUnit` (nullable for backward compatibility — legacy memory units won't have it).
  - [ ] 4.2 Add `public required string EmbeddingModel { get; init; }` to `Hexalith.Memories.Contracts.V1.IndexInput` (required — new ingestions must supply it). **Migration audit:** before compile, grep all `new IndexInput` (and `IndexInput { ... }` initializers) across `src/` AND `tests/` — every call site (production workflow AND any hand-crafted test fixture in `Hexalith.Memories.Server.Tests`, `Hexalith.Memories.IntegrationTests`, `Hexalith.Memories.Contracts.Tests`) must supply `EmbeddingModel`. Compiler will flag missing ones but the audit ensures test fixtures get realistic values (e.g., `"gemini-embedding-001"`) not placeholder strings.
  - [ ] 4.3 Add `EmbeddingModel` to `EmbeddingResult` — **verified to exist** at the output of `GenerateEmbeddingActivity` (`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:17` declares `WorkflowActivity<EmbeddingInput, EmbeddingResult>`). Add `public string? EmbeddingModel { get; init; }` (nullable to avoid breaking any DAPR-replayed historical workflow state). Locate the record via grep `EmbeddingResult` in the Contracts or Server project and register the new field in `MemoriesJsonContext` if the existing record is already registered there.
  - [ ] 4.4 Update `GenerateEmbeddingActivity.RunAsync` to populate `EmbeddingModel` on its output (read from `config.Model`, already loaded for rate-limit ceiling).
  - [ ] 4.5 Update the ingestion workflow (grep for where `IndexInput` is constructed — likely `IngestionWorkflow` or `IngestMemoryUnitActivity`) to pass `EmbeddingModel` through to `IndexInput`.
  - [ ] 4.6 Update `IndexSyntacticActivity.RunAsync` to:
    - Persist `embeddingModel` as a `HashEntry` on the memory-unit Redis hash, alongside the existing `embeddingProvider` entry.
    - **Write the tenant last-activity key** (amendment A): after the hash write succeeds, fire `db.StringSetAsync($"{input.TenantId}:lastActivityAt", input.IngestedAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture), flags: CommandFlags.FireAndForget)`. Do NOT await it blocking; the activity's success must not depend on this write succeeding. Wrap in a try-catch that logs `Warning` on `RedisException` and swallows. **Rationale:** the key powers `GetLastActivityAtAsync` for the list/configuration endpoints; a missing key means a tenant shows `null` in the UI, which is fine. A failed ingest because of this write would be catastrophic.
  - [ ] 4.7 Update `CaseService.GetMemoryUnitAsync` (and any other Redis-hash-to-`MemoryUnit` mapper) to read the `embeddingModel` field. Missing field (legacy data) → `null` (not a mismatch; legacy data pre-dates FR70).
  - [ ] 4.8 Update `IndexGraphActivity` if it persists provider/dimensions on graph nodes — add model there too for consistency. If graph nodes don't persist these fields, skip (only syntactic hash matters per FR70).
  - [ ] 4.9 **Legacy compatibility:** existing memory units indexed before this change have no `embeddingModel` field. The `GET memory-unit` response simply returns `null` for them. Do NOT attempt to backfill — migration is out of scope. Add a one-line comment in `CaseService.GetMemoryUnitAsync` reader explaining this.

- [ ] Task 5: Unit tests (AC: #1, #2, #3, #4, #5, #6)
  - [ ] 5.1 Create `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`. Use NSubstitute for `IConnectionMultiplexer` / `IDatabase` and for the FalkorDB client. Coverage:
    - `GetMemoryUnitCountAsync`: returns correct count from mock SCAN; returns `null` when Redis throws `RedisConnectionException`.
    - `GetIndexSizesAsync`: **parameterized test covering all 2³=8 backend-availability combinations** (RediSearch up/down × Redis Vector up/down × FalkorDB up/down). For each combination assert: (a) no exception thrown, (b) available backends report populated counts, (c) unavailable backends report `null` counts with `backendAvailability.<backend>=false`, (d) method always returns a fully-formed tuple. Use `[Theory]` + `[InlineData]` or `MemberData`.
    - `GetIndexStatusAsync`: returns `Ready` when `FT.INFO` returns a parseable `num_docs`, `Missing` when `FT.INFO` returns "no such index", `Degraded` when response is well-formed but `num_docs` is absent / unparseable, `Degraded` on `LOADING`/`BUSY` response, `Unknown` on `RedisConnectionException`.
  - [ ] 5.2 Create `tests/Hexalith.Memories.Server.Tests/Tenants/TenantConfigurationEndpointTests.cs`:
    - `PATCH /api/tenants/{id}` with unknown tenant → 404 `TENANT_NOT_FOUND`.
    - `PATCH` with non-Active tenant → **parameterized `[Theory]` covering every `TENANT_*` code** (`TENANT_DELETING`, `TENANT_PROVISIONING`, `TENANT_FAILED`, `TENANT_UNAVAILABLE`) asserts `ToHttpResult` produces 409 and that `TENANT_NOT_FOUND` produces 404. This is the **mutation-guard test** protecting 5-4's `ToHttpResult` bug fix — any change that routes a non-not-found code to 404 or vice versa fails here.
    - `PATCH` with empty body (both fields null) → 400 `INVALID_INPUT`.
    - `PATCH` with display name updates registry entry (verify via mock `TenantRegistryService.UpdateTenantDisplayNameAsync` received).
    - `PATCH` with rate-limit > Google ceiling → 400 `INVALID_CONFIG` from `EmbeddingProviderDefaults.Validate`.
    - `PATCH` with display name succeeding but rate-limit subsequently failing → verifies display-name change was NOT rolled back (documents the "no distributed transaction" decision).
    - `GET /api/tenants/{id}/configuration` for unknown tenant → 404.
    - `GET /api/tenants/{id}/configuration` returns composed `TenantConfigurationView` with the full `TenantEmbeddingConfig` record embedded + metrics.
    - `GET /api/tenants` returns `TenantSummary[]` with backend availability flags, `reindexRequired` field, and `lastActivityAt` populated or null; one tenant's backend failure does NOT fail the whole list.
  - [ ] 5.3 Create `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs` additions (or extend existing test class if present):
    - `SetEmbeddingConfigAsync` with only `RateLimitPerMinute` changed (provider/model/dimensions unchanged) → no breaking-change exception, value persisted.
    - Confirm `EmbeddingProviderDefaults.GetBreakingChangeFields` does NOT list `rateLimitPerMinute` in breaking fields (existing contract — test that it stays that way to protect AC3's rate-limit update path).
  - [ ] 5.4 FR69 regression test in `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` (create or extend):
    - Mock `ITenantConfigurationActor.GetEmbeddingConfigAsync()` to return a config with `RateLimitPerMinute = 500`.
    - Assert `IEmbeddingRateLimiterActor.SetCeilingAsync(500)` received before `TryConsumeAsync`.
  - [ ] 5.5 FR70 test in `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`:
    - `RunAsync` with `input.EmbeddingModel = "gemini-embedding-001"` → verify Redis `HashSetAsync` received a `HashEntry("embeddingModel", "gemini-embedding-001")`.
    - Legacy-hash-read test in `CaseServiceTests` (or the TenantContextEnforcementTests mismatch pattern): hash without `embeddingModel` field → `MemoryUnit.EmbeddingModel` is `null`, no exception.

- [ ] Task 6: Integration tests (AC: #1, #2, #3, #4, #6)
  - [ ] 6.1 Create `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`. Most scenarios use `[Fact(Skip = "Requires Aspire AppHost fixture")]` consistent with 5-1 / 5-2 / 5-3 / 5-4 deferral pattern. Required before Gate 2 sign-off.

    **Exception — FR70 golden path unskipped:** the AC6 end-to-end test (last scenario in 6.2) asserting `embeddingModel` propagation through the full ingest → index → read path **must NOT be skipped if any ingestion-path integration fixture already runs in CI**. If no such fixture exists, fall back to a unit-level test in `IngestionWorkflowTests` that asserts `IndexInput.EmbeddingModel` is populated from `EmbeddingResult.EmbeddingModel`. Rationale: FR70 is the one new durable field and the primary regression risk; it should not be blocked behind the Aspire deferral.
  - [ ] 6.2 Scenarios:
    - List tenants against a real Redis + FalkorDB → response includes `memoryUnitCount`, non-null `indexSizes`, `reindexRequired`, and `lastActivityAt` for an active tenant with indexed data.
    - List tenants with one backend stopped → response still returns the tenant; `backendAvailability.RedisVector=false`; other counts populated.
    - `GET /api/tenants/{id}/configuration` end-to-end returns composed view with the full `TenantEmbeddingConfig` (including `apiSecretKeyName` — verify XML doc clarifies non-sensitive nature) and `IndexStatus=Ready` on all three backends.
    - `PATCH /api/tenants/{id}` with `displayName` → subsequent `GET /api/tenants/{id}` reflects new name; log capture contains the `Information` audit entry with `oldValue`/`newValue`.
    - `PATCH /api/tenants/{id}` with `rateLimitPerMinute=200` → subsequent `GET /api/tenants/{id}/embedding-config` reflects new ceiling; next ingestion observes new rate limit at the `EmbeddingRateLimiterActor`.
    - `PUT /api/tenants/{id}/embedding-config` with changed `dimensions` and `forceReindex=false` → 409 `EMBEDDING_CONFIG_BREAKING_CHANGE`; with `forceReindex=true` → 200 and `reindexRequired=true`; subsequent `GET /api/tenants` shows `reindexRequired=true` on that tenant's summary.
    - **[FR70 golden — unskip per 6.1]** Ingest one memory unit end-to-end → `GET memory-unit` response includes `embeddingProvider=google` AND `embeddingModel=gemini-embedding-001`; Redis hash inspection shows both `embeddingProvider` and `embeddingModel` fields persisted.

## Dev Notes

### First Principles Framing

**What this story IS:** Exposing existing tenant state (configuration, index sizes, activity counts) to operators via HTTP endpoints; adding the missing FR70 field (`embeddingModel`) on memory units; allowing display name and rate-limit updates post-creation.

**What this story IS NOT:**
- NOT an audit store. Structured logging is the MVP audit mechanism (consistent with 5-4 decision).
- NOT a new validation framework. Reuse `EmbeddingProviderDefaults.Validate` and `TenantStatusGuard`.
- NOT a reindexing workflow. Reindex on config change is out of scope — the existing `forceReindex` acknowledgment flag is sufficient for MVP; full reindex workflow is tracked elsewhere.
- NOT metric backends. `memoryUnitCount` / `indexSizes` are computed on demand, not exposed via OpenTelemetry. Caching / OTEL is Phase 2.

**Mental model for the dev agent:**
- AC1 (list) = *enriched projection* over `TenantRegistryService.ListTenantsAsync` + three backend count calls.
- AC2 (view config) = *fan-out* across `TenantRegistryService` + `TenantConfigurationActor` + `TenantMetricsService`.
- AC3 (update) = *two-step write* (registry entry for name, config actor for rate limit). No distributed transaction.
- AC4 (breaking-change guard) = *already built*. Extend tests; do NOT rewrite.
- AC5 (rate limit ceiling) = *already wired in `GenerateEmbeddingActivity`*. Add regression test.
- AC6 (FR70 model tracking) = *schema addition* — `EmbeddingModel` on `MemoryUnit`, `IndexInput`, and Redis hash; thread through ingestion.

**If you find yourself building a new generic metrics framework, a caching layer, a dedicated audit store, a validation DSL, or an index-reconciliation service — STOP. You're over-scoping.**

### Dependencies

- **Story 5-1 (Tenant Provisioning):** Required — provides `TenantRegistryService`, `TenantStatusGuard`, `TenantConfigurationActor`, `TenantEmbeddingConfig`, `EmbeddingProviderDefaults`. Status: done.
- **Story 5-2 (Tenant Deletion):** No direct dependency.
- **Story 5-3 (Tenant Isolation Verification):** Independent. This story also hits FalkorDB/Redis for stats but that's orthogonal to isolation verification.
- **Story 5-4 (Tenant Context Enforcement):** Done. `PATCH` endpoint reuses `ValidateTenantActiveAsync` + `ToHttpResult` pattern introduced in 5-4. `GET configuration` reuses `ValidateTenantExistsAsync`.
- **Story 1-5 (Three-backend indexing):** Provides `IndexSyntacticActivity`, `IndexSemanticActivity`, `IndexGraphActivity` — where `embeddingModel` must be persisted.
- **Story 1-7 (Embedding Provider Configuration):** Already provides `TenantConfigurationActor` + breaking-change detection. This story extends the read surface.

### Implementation Priority

Implement in this order:
1. **Task 4 first (FR70 — embedding model field)** — touches contracts + indexing activities; lowest risk if done first before endpoints rely on the field.
2. **Task 1 (list endpoint)** — requires `TenantMetricsService` which Task 2 also uses. Build the service here.
3. **Task 2 (configuration view)** — reuses `TenantMetricsService`. Mostly composition.
4. **Task 3 (PATCH endpoint)** — requires `TenantRegistryService.UpdateTenantDisplayNameAsync`. Reuses existing ETag CAS pattern.
5. **Task 5 (unit tests)** — alongside each task. Write tests as you implement each endpoint.
6. **Task 6 (integration tests)** — last, all `[Fact(Skip)]` per 5-1/5-2/5-3/5-4 deferral pattern.

### Architectural Pattern Shift: Direct Registry Write from Endpoint

Stories 5-1 through 5-4 established a pattern: **every `TenantRegistryEntry` mutation goes through a DAPR workflow** (`TenantProvisioningWorkflow`, `TenantDeletionWorkflow`). The endpoint handler schedules the workflow and returns `Accepted`; the workflow calls `TenantRegistryService.UpdateTenantStatusAsync` / `BeginTenantDeletionAsync` as part of an orchestrated saga.

**Story 5.5's `PATCH /api/tenants/{id}` breaks this pattern deliberately.** `UpdateTenantDisplayNameAsync` is called synchronously from the endpoint delegate, writes the registry, and returns `Ok`. Rationale:
- A display-name change has no side effects on backends (no index rename, no saga needed).
- A rate-limit change mutates only the `TenantConfigurationActor` state, not the registry (no saga needed either).
- Modeling every property edit as a workflow would be overkill (anti-pattern: speculative complexity, see #10 below).

**Consequence:** `UpdateTenantDisplayNameAsync` is the first direct-from-endpoint registry mutation in the codebase. Future PRs that touch the registry should ask: "is this a durable, multi-step operation (→ workflow) or a simple property edit (→ direct write)?" If the distinction blurs — e.g., a display-name change later needs to propagate to downstream systems — revisit the pattern and move it to a workflow. The ETag CAS retry loop preserves safety under concurrent writes; it does NOT preserve cross-system consistency.

### Architecture Compliance

- **FR41, FR42, FR43, FR45, FR69, FR70:** Directly satisfied by ACs 1–6.
- **D1 (FalkorDB isolation):** All graph queries for index stats use tenantId as graph/database name. Reuse existing keyed DI (`"falkordb"`).
- **D9 (IGraphQueryBuilder):** Graph node count uses parameterized Cypher via `IGraphQueryBuilder`. `"MATCH (n) RETURN count(n)"` is a constant infrastructure query with no user input — acceptable per 5-4 audit (matches the existing `VerifyTenantActivity` pattern).
- **NFR10 (DAPR API tokens):** No new channel; reuse existing DAPR sidecar config from 5-4.
- **D8 (TenantAuthorizationMiddleware):** Out of scope (Phase 1.5). MVP validates tenant IDs against the registry per 5-4.

### Existing Infrastructure to Reuse

| Component | Location | Usage in This Story |
|-----------|----------|---------------------|
| `TenantRegistryService` | `Server/Tenants/TenantRegistryService.cs` | `ListTenantsAsync`, `GetTenantEntryAsync`, new `UpdateTenantDisplayNameAsync` (ETag CAS pattern) |
| `TenantStatusGuard` | `Server/Tenants/TenantStatusGuard.cs` | `ValidateTenantExistsAsync` (GET /configuration), `ValidateTenantActiveAsync` (PATCH), `ToHttpResult` |
| `TenantConfigurationActor` | `Server/Actors/TenantConfigurationActor.cs` | `GetEmbeddingConfigAsync`, `SetEmbeddingConfigAsync` |
| `EmbeddingProviderDefaults` | `Server/Ingestion/EmbeddingProviderDefaults.cs` | `Validate`, `GetBreakingChangeFields` |
| `EmbeddingConfigChangeException` | `Server/Ingestion/...` | Already raised by actor; already surfaced as 409 in `PUT embedding-config` |
| `IConnectionMultiplexer` keyed `"redis"` | DI | Redis SCAN + FT.INFO |
| `IConnectionMultiplexer` keyed `"falkordb"` | DI | FalkorDB GRAPH.QUERY |
| `IGraphQueryBuilder` / `GraphQueryBuilder` | `Server/Graph/` | For graph node count (optional; constant query is acceptable per D9) |
| `MemoriesJsonContext` | `Contracts/V1/` | Register all new contracts for AOT serialization |
| `ErrorResponse` | `Contracts/V1/ErrorResponse.cs` | Standard error response format `(code, message, suggestion)` |
| `ValidateTenantId` | `Program.cs` static helper | Format validation |
| `ITenantConfigurationActor` | `Server/Actors/` | Already DI-registered via actor runtime |
| `EmbeddingRateLimiterActor.SetCeilingAsync` | `Server/Actors/` | Already called from `GenerateEmbeddingActivity.cs:58` with `config.RateLimitPerMinute` — FR69 already wired |

### Current Endpoint State (Baseline)

**Existing and reused as-is:**
- `GET /api/tenants` — returns `TenantInfo[]`; this story **replaces the return type** with `TenantSummary[]` (see Breaking Changes at top of file).
- `GET /api/tenants/{tenantId}` — returns basic `TenantInfo`. **Kept minimal deliberately** — pre-Gate-2 no external consumer needs the basic form, but the provisioning/deletion workflow endpoints (`provision-status`, `deletion-status`) return workflow state that callers cross-reference with basic tenant info. Keeping the minimal GET avoids binding those workflow-adjacent lookups to the richer `TenantConfigurationView` which triggers N+3 backend calls. If a future audit shows no such callers, collapse in a later story. Do NOT collapse now as part of 5.5 — out of scope.
- `GET /api/tenants/{tenantId}/embedding-config` — returns `TenantEmbeddingConfig`; kept as-is. This is the **write-shape** endpoint (same shape accepted by PUT). Story 5.5's `GET /configuration` returns the same `TenantEmbeddingConfig` record embedded in `TenantConfigurationView` — single contract shape reused (no projection).
- `PUT /api/tenants/{tenantId}/embedding-config` — accepts `TenantEmbeddingConfig` + `forceReindex` query param; already returns 409 on breaking change. **Covers AC4 entirely — no code change needed**, only tests.

**New in this story:**
- `GET /api/tenants/{tenantId}/configuration` — composed `TenantConfigurationView` (embedding + metrics + status).
- `PATCH /api/tenants/{tenantId}` — partial update (`displayName`, `rateLimitPerMinute`).

**Response type change warning:** Changing `GET /api/tenants` from `TenantInfo[]` to `TenantSummary[]` is a contract change. It is acceptable because:
1. No production consumers exist yet (MVP pre-Gate-2).
2. `TenantSummary` is a superset of `TenantInfo` fields.
3. Integration test rewrite required — acceptable pre-release.

If this concern is revisited later, a safe alternative is to keep `GET /api/tenants` returning `TenantInfo[]` and add `GET /api/tenants?view=summary` or `GET /api/tenants/summaries`. Do NOT implement that now unless a concrete consumer breaks.

### Rate Limit Propagation Timing (AC5)

`GenerateEmbeddingActivity.cs:58` currently calls `rateLimiter.SetCeilingAsync(config.RateLimitPerMinute)` on every activity invocation, just before `TryConsumeAsync`. This means a rate-limit change propagates to the actor on the **next embedding request** for that tenant, not instantaneously. For MVP this is acceptable — the lag is bounded by the next ingestion cycle. Do NOT add a push-based ceiling update (e.g., invoking `SetCeilingAsync` from the `PATCH` endpoint). Reasoning:
- The pull pattern is already idempotent — `SetCeilingAsync` is safe to call repeatedly with the same value.
- A push would create a second code path to maintain and race against the pull.
- The "next request" guarantee is sufficient per NFR22 (rate limiting is best-effort under load).

### Redis/FalkorDB Query Notes

- **Memory unit count** uses `SCAN` with `MATCH {tenantId}:mu:*` and a bounded `COUNT` hint (e.g., 1000). Do NOT use `KEYS` — blocks the server. For large tenants, SCAN iterates; the caller's `CancellationToken` bounds total time.
- **RediSearch count** via `FT.INFO {tenantId}:syntactic` response, parse `num_docs` field. `StackExchange.Redis` does not have a typed wrapper — use `ExecuteAsync("FT.INFO", indexName)` and parse the `RedisResult`. Mirror the pattern from `TenantIsolationVerifier` if it reads index stats.
- **FalkorDB node count** via `GRAPH.QUERY {tenantId} "MATCH (n) RETURN count(n)"`. Returns a single-row result. The query is a constant — no `IGraphQueryBuilder` needed per D9 audit (same pattern as `VerifyTenantActivity`).
- **Missing index vs unavailable backend:** if `FT.INFO` returns an error containing "no such index" or similar, classify as `IndexHealth.Missing`. If the Redis call itself throws `RedisConnectionException`, classify as `IndexHealth.Unknown` + `backendAvailability=false`.

### Error Codes

New error codes introduced by this story:
- `INVALID_INPUT` (400) — already in use; reused for empty PATCH body.
- `INVALID_CONFIG` (400) — already raised by `EmbeddingProviderDefaults.Validate`.
- (none new)

Reused from 5-4:
- `TENANT_NOT_FOUND` (404)
- `TENANT_PROVISIONING` / `TENANT_DELETING` / `TENANT_FAILED` / `TENANT_UNAVAILABLE` (409)
- `EMBEDDING_CONFIG_BREAKING_CHANGE` (409) — from `CreateEmbeddingConfigConflictResponse` in `Program.cs`.

### Code Conventions

Per 5-4 Dev Notes (unchanged):
- Sealed partial class for services using `[LoggerMessage]` source generator.
- File-scoped namespaces.
- `ErrorResponse("CODE", "message", "suggestion")` pattern for all error responses.
- Singleton DI for stateless services.
- xUnit + Shouldly + NSubstitute for testing.
- Test naming: `{ClassName}Tests.cs` with descriptive method names.
- Keyed DI: `"redis"` for RediSearch/Vector, `"falkordb"` for FalkorDB.

### Anti-Patterns to Avoid

1. **Do NOT create an audit-store class / persistence layer** — structured logging is the MVP audit mechanism. Consistent with 5-4 decision.
2. **Do NOT add caching** to tenant metrics or listing for "performance". Anti-pattern: speculative complexity. Operator-initiated list is not a hot path. Revisit if benchmarks prove a bottleneck.
3. **Do NOT extend `TenantInfo`** with counts/sizes. It is a workflow-boundary contract. Enriched data lives in new `TenantSummary` / `TenantConfigurationView` records.
4. **Do NOT create a `TenantAuditEvent` record** or `ITenantAuditStore` interface. See #1.
5. **Do NOT add a push-based rate-limit ceiling update** from the PATCH endpoint. The existing pull-on-next-embedding pattern is sufficient. See "Rate Limit Propagation Timing".
6. **Do NOT add ambient `ITenantContext`** — same anti-pattern as 5-4. Explicit tenantId parameters only.
7. **Do NOT implement a reindex workflow** triggered on `forceReindex=true`. That's Phase 2. Setting `ReindexRequired=true` on the config is the MVP behavior — the flag is informational.
8. **Do NOT rewrite `EmbeddingConfigChangeException` / `CreateEmbeddingConfigConflictResponse`** — they correctly satisfy AC4 today. Only add tests.
9. **Do NOT backfill `embeddingModel` on legacy memory units**. Migration is out of scope. Legacy reads return `null`.
10. **Do NOT create `DisplayNameValidator` or `RateLimitValidator` classes** — inline validation is fine for a PATCH endpoint. Anti-pattern: premature abstraction.
11. **Do NOT switch `GET /api/tenants` to streaming / paginated** response. All tenants fit in memory for MVP. Pagination is Phase 2 if the tenant count grows past ~100.
12. **Do NOT use `KEYS` for memory unit count** — always `SCAN`. See Redis docs.

### Previous Story Learnings (from 5-4)

- `TenantStatusGuard.ValidateTenantExistsAsync` and `TenantStatusGuard.ToHttpResult` are now available — use them. Do NOT write a new registry-lookup-404 pattern per endpoint.
- `TenantMismatchMonitor` exists in `Server/Tenants/` — NOT needed here (no cross-tenant concerns introduced by this story). Do not accidentally reuse it as a "generic metric counter".
- `CapturingLogger<TCategory>` test fixture pattern (see `TenantContextEnforcementTests`) can be reused for asserting `[LoggerMessage]` calls on internal-category loggers.
- `DaprException` wrapping: all DAPR-facing endpoint code should catch and return 503 `DAPR_UNAVAILABLE` per 5-4 pattern.
- `RedisConnectionException` / `RedisServerException`: catch at the service boundary; surface as backend-unavailable in metrics rather than throwing.
- Run full test suite before and after — 5-4 left 1051+ tests passing; keep that bar.

### Git Intelligence

Recent commits show:
- `9cd3b97` — `TenantStatusGuard.ToHttpResult` helper (5-4). **Reuse this for all new endpoints.**
- `912a3ab` — serialization tests for tenant isolation check results. **Mirror this test pattern for `TenantSummary` / `TenantConfigurationView` if adding serialization tests.**
- `5bb2655` / `acbcffe` — unit tests for tenant provisioning/deletion activities. **Mirror the structure for `TenantMetricsService` unit tests.**
- `e5b8062` — confidence promotion + gap detection (epic 4). Unrelated.

### Edge Cases

- **Empty tenant list:** `GET /api/tenants` returns `[]`, not 404. `TenantRegistryService.ListTenantsAsync` already returns empty list for missing index.
- **Tenant in `Provisioning` status:** `GET /api/tenants/{id}/configuration` should succeed (existence-only). Backend index stats may return `Missing` — that's correct, not an error.
- **Tenant in `Failed` status:** `GET /api/tenants/{id}/configuration` should succeed; `indexStatus` reflects whatever partial state exists; `lastActivityAt` may be null.
- **Tenant with zero memory units:** `memoryUnitCount=0`, `lastActivityAt=null`, `indexSizes.*=0` (not null — backend responded, just empty).
- **Rate limit set to exactly `GoogleMaxRateLimitPerMinute` (3000):** allowed (inclusive upper bound in `EmbeddingProviderDefaults.Validate`).
- **Rate limit set to 0 or negative via PATCH:** rejected by `EmbeddingProviderDefaults.Validate`.
- **Display name update to same value as current:** no-op at registry level (CAS write with identical payload); still emit audit log for observability.
- **Concurrent PATCH on same tenant:** ETag CAS retry in `UpdateTenantDisplayNameAsync` handles it. After 3 retries → throw `InvalidOperationException` → caller surfaces 500. Acceptable — concurrent admin writes are rare.
- **Missing `embeddingModel` on legacy memory unit hash:** returns `null` on `MemoryUnit.EmbeddingModel`. Not a `TENANT_MISMATCH` (different concept — that's the tenant ID field).
- **Legacy `IndexInput` callers:** there should be none outside `IngestionWorkflow`. If any internal caller omits `EmbeddingModel`, the `required` field will be a compile error — deliberate.

### Gate 2 Sign-off Criteria (Story 5.5)

Gate 2 for Epic 5 requires this story to satisfy:
1. All unit tests (Task 5) pass — at least 25+ new tests covering metrics service, PATCH validation, embedding model propagation, and serialization.
2. `GET /api/tenants` returns `TenantSummary[]` with populated counts on a running Aspire fixture.
3. `PATCH /api/tenants/{id}` updates display name visible immediately via subsequent `GET /api/tenants/{id}`.
4. `PUT /api/tenants/{id}/embedding-config` with breaking change + `forceReindex=false` → 409; with `forceReindex=true` → 200 with `reindexRequired=true`.
5. New memory units carry `embeddingModel=gemini-embedding-001` in Redis hash and in `MemoryUnit` GET responses.
6. Integration tests (Task 6) all `[Fact(Skip)]` — acceptable per 5-1/5-2/5-3/5-4 pattern; required before Gate 2 final sign-off.

### Known MVP Limitations

- **No audit persistence:** Structured logs only. Phase 2 adds `ITenantAuditStore`.
- **No pagination on list:** `GET /api/tenants` returns all tenants. Assumes tenant count < ~100 for MVP.
- **`lastActivityAt` approximation:** MVP reads a bounded sample of memory unit hashes (100 keys). Exact value requires a per-tenant activity timestamp maintained on writes — deferred to Phase 2.
- **No reindex workflow:** `ReindexRequired=true` is a flag, not an executed operation. Operator must manually trigger reindex (currently unavailable — deferred). The flag surfaces the need; execution is Phase 2.
- **Rate limit change lag:** Propagates on next ingestion embedding call per tenant. See "Rate Limit Propagation Timing".
- **Backfill of `embeddingModel` on legacy data:** Not performed. Pre-5.5 memory units return `null`. Backfill is a migration operation, out of scope.
- **Per-tenant metrics cost:** `GET /api/tenants` issues N×3 backend calls for N tenants. Fine for N<100; caching / OTEL metrics is Phase 2.

### Project Structure Notes

New files:
- `src/Hexalith.Memories.Contracts/V1/TenantSummary.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantIndexSizes.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantBackendAvailability.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantIndexStatus.cs`
- `src/Hexalith.Memories.Contracts/V1/IndexHealth.cs` (enum)
- `src/Hexalith.Memories.Contracts/V1/TenantUpdateInput.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantConfigurationEndpointTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`

Modified files:
- `src/Hexalith.Memories.Server/Program.cs` — update `GET /api/tenants`, add `GET /api/tenants/{tenantId}/configuration`, add `PATCH /api/tenants/{tenantId}`, register `TenantMetricsService`.
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` — add `UpdateTenantDisplayNameAsync` + `[LoggerMessage]` event.
- `src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs` — add `EmbeddingModel` nullable field.
- `src/Hexalith.Memories.Contracts/V1/IndexInput.cs` — add `EmbeddingModel` required field.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — register all new contracts and `IndexHealth` enum.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — include model in output.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` — persist `embeddingModel` hash entry.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` — read `embeddingModel` field in `GetMemoryUnitAsync`.
- `src/Hexalith.Memories.Server/Ingestion/IngestionWorkflow.cs` (or wherever `IndexInput` is constructed) — thread `EmbeddingModel` from `EmbeddingResult` to `IndexInput`.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` — add FR69 regression.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs` — add FR70 persistence test.

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.5]
- [Source: _bmad-output/planning-artifacts/prd.md — FR41, FR42, FR43, FR45, FR69, FR70]
- [Source: _bmad-output/planning-artifacts/architecture.md — D1, D9, Per-tenant pipeline actor, Embedding provider configuration]
- [Source: _bmad-output/implementation-artifacts/5-4-tenant-context-enforcement.md — `TenantStatusGuard`, `ToHttpResult`, MVP audit trail decision]
- [Source: src/Hexalith.Memories.Server/Program.cs — existing endpoint registration patterns]
- [Source: src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs — ETag CAS pattern for `UpdateTenantDisplayNameAsync`]
- [Source: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs — embedding config read/write]
- [Source: src/Hexalith.Memories.Server/Ingestion/EmbeddingProviderDefaults.cs — `Validate`, `GetBreakingChangeFields`]
- [Source: src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs#L58 — existing `SetCeilingAsync` call (AC5 already wired)]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs#L76 — existing `embeddingProvider` hash entry (pattern for FR70)]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs — record to extend with `EmbeddingModel`]
- [Source: src/Hexalith.Memories.Contracts/V1/IndexInput.cs — record to extend with `EmbeddingModel`]
- [Source: src/Hexalith.Memories.Contracts/V1/TenantInfo.cs — canonical minimal tenant record (do NOT modify)]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

## Change Log

| Date       | Change                                                                                          |
|------------|-------------------------------------------------------------------------------------------------|
| 2026-04-14 | Story 5.5 context created via `bmad-create-story`; ready-for-dev. |
| 2026-04-14 | Party-mode review amendments applied: `TenantSummary` gains `reindexRequired`/`lastActivityAt`; `apiSecretKeyName` excluded from view via new `TenantEmbeddingConfigView`; `IndexHealth` states fully defined (`Ready`/`Missing`/`Degraded`/`Unknown`); AC3 wording clarifies structured-log audit mechanism; `FT.INFO` parse snippet inlined in Task 1.5; O(\|V\|) cost of FalkorDB node count documented in Task 1.7; 2³ backend-availability property test in Task 5.1; `ToHttpResult` mutation-guard theory in Task 5.2; FR70 golden end-to-end test unskipped (or unit-level fallback) in Task 6.1; breaking changes hoisted to top-of-file section. |
| 2026-04-14 | Advanced-elicitation amendments (methods 1–5 applied, A–J): (A) `lastActivityAt` via dedicated `{tenantId}:lastActivityAt` Redis key written fire-and-forget from `IndexSyntacticActivity` — replaces bounded-sample; (B) AC3 "audit trail" reworded to "operational log"; (C) dropped `TenantEmbeddingConfigView` projection — configuration view now embeds the full `TenantEmbeddingConfig`, `apiSecretKeyName` documented as non-sensitive; (D) `FT.INFO` parse snippet hardened with `TryParse` guard and string/integer dual handling; (E) read-through state-store bypass documented for list-endpoint config enrichment (avoids N actor activations); (F) PATCH routing smoke-test subtask added to Task 3.4 with POST fallback; (G) `IndexInput` constructor migration audit across src/ + tests/ added to Task 4.2; (H) TL;DR block added at top of file; (I) "Architectural Pattern Shift" section documenting first direct-registry-write-from-endpoint vs. the 5-1–5-4 workflow pattern; (J) `[LoggerMessage]` field names pinned (`tenantId`, `field`, `oldValue`, `newValue`, `actor`, `occurredAt`) to match anticipated Phase 2 audit contract. |
