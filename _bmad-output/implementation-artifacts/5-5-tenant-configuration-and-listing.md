# Story 5.5: Tenant Configuration & Listing

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** operator-facing tenant listing with per-backend counts + index health, a composed configuration view, a `PATCH /api/tenants/{id}` endpoint limited to `displayName`, and FR70 propagation of `embeddingModel` onto memory units. Rate-limit updates use the **existing** `PUT /api/tenants/{id}/embedding-config` path — PATCH does not touch actor state.

**What does NOT ship:** audit persistence (logs only), reindex workflow (`reindexRequired` is advisory), caching, pagination, CLI (Epic 7), customer-facing change visibility, identity-based authorization (Phase 1.5).

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

2. **Given** an existing tenant, **when** the operator calls `GET /api/tenants/{tenantId}/configuration`, **then** the response includes: the full `TenantEmbeddingConfig` (`provider`, `model`, `dimensions`, `rateLimitPerMinute`, `apiSecretKeyName`, `reindexRequired`), per-backend `indexStatus` (ready / missing / degraded / unknown), `lastActivityAt`, `memoryUnitCount`, `createdAt`, `displayName`, `status` (FR45). For unknown tenants the response is `404` `TENANT_NOT_FOUND`; `ValidateTenantExistsAsync` guard is applied first. **Note on `apiSecretKeyName`:** this is the _name/identifier_ of the secret in the secret store (e.g., `"google-embedding-api-key"`), not the secret value. It is safe to return. The contract XML doc on `TenantEmbeddingConfig.ApiSecretKeyName` must state this explicitly so future readers don't mistake it for a sensitive field.

3. **Given** an existing tenant in `Active` status, **when** the operator calls `PATCH /api/tenants/{tenantId}` with an updated `displayName`, **then** (a) the new name is persisted on the registry entry, (b) the updated `TenantSummary` is returned, (c) an **operational log** entry at `Information` level is emitted via `[LoggerMessage]` source generator with the fields `tenantId`, `actor`, `field`, `oldValue`, `newValue`, `occurredAt`, `durationMs` (FR42). **Rate-limit updates** flow through the existing `PUT /api/tenants/{tenantId}/embedding-config` endpoint — PATCH handles registry fields only. This scoping is deliberate (corroborated by Occam + three-way self-consistency derivation): conflating two persistence targets in one endpoint introduces a cross-store partial-failure mode for zero user benefit.

    **Terminology note:** the PRD says "recorded in the tenant's audit trail"; in MVP this is an **operational log** (not a compliance-grade, tamper-evident audit trail). A durable tenant audit store is Phase 2. The chosen field names (`tenantId`, `actor`, `field`, `oldValue`, `newValue`, `occurredAt`, `durationMs`) are **pinned to match the anticipated Phase 2 audit event contract** so the migration is a one-for-one field remap rather than a rewrite. The `TenantStatusGuard.ValidateTenantActiveAsync` guard applies — non-Active tenants are rejected with the existing `TENANT_*` 404/409 error codes. `DaprException` is caught and surfaced as 503 `DAPR_UNAVAILABLE` (mirrors the POST `/api/tenants` pattern).

4. **Given** a configuration change that would create data inconsistency (e.g., changing `embedding dimensions`, `provider`, or `model` without reindex), **when** the change is submitted to `PUT /api/tenants/{tenantId}/embedding-config` without `forceReindex=true`, **then** the system rejects it with HTTP `409 Conflict`, error code `EMBEDDING_CONFIG_BREAKING_CHANGE`, and a response body that names the affected fields, the current values, and the proposed values (FR43). The operator must re-submit with `forceReindex=true` to acknowledge the reindex requirement and proceed.

5. **Given** per-tenant rate limit ceilings are configured, **when** the `EmbeddingRateLimiterActor` enforces limits during ingestion, **then** it uses the tenant's currently-configured `rateLimitPerMinute` value from `TenantConfigurationActor` (FR69). Changes made via `PUT /api/tenants/{tenantId}/embedding-config` propagate to the rate limiter on the next embedding request (the limiter's `SetCeilingAsync` is called on each `GenerateEmbeddingActivity` invocation — this is already the established pattern).

6. **Given** a memory unit is ingested, **when** it is indexed, **then** both the embedding `provider` AND the embedding `model` used are recorded as durable fields on the memory unit, visible in `GET /api/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}` responses (FR70). This enables future auditing of which vectors were generated by which (provider, model) pair.

## Tasks / Subtasks

- [x] Task 1: Enriched tenant listing endpoint (AC: #1)
    - [x] 1.1 Create a new contract `Hexalith.Memories.Contracts.V1.TenantSummary` as a `public sealed record` with: `Id`, `DisplayName`, `Status`, `CreatedAt`, `long? MemoryUnitCount`, `TenantIndexSizes IndexSizes`, `TenantIndexStatus IndexStatus`, `bool ReindexRequired`, `DateTimeOffset? LastActivityAt`. `MemoryUnitCount` is nullable so a Redis-unavailable case can report `null` instead of a misleading zero. **Availability is conveyed by `IndexHealth.Unknown` on each axis in `IndexStatus`** — no separate `TenantBackendAvailability` record (Amendment P: fold availability into the status enum, which already has `Unknown`). **Do NOT modify `TenantInfo`** — it's the canonical minimal tenant record used by workflows and actors. `TenantSummary` is an enriched view for operator listing. **Contract change is listed in the top-of-file "Breaking Changes" section.**
    - [x] 1.2 Create `Hexalith.Memories.Contracts.V1.TenantIndexSizes` record with `long? RediSearchKeyCount`, `long? RedisVectorKeyCount`, `long? FalkorDbNodeCount`. Nulls indicate the corresponding backend was unavailable (signalled in parallel by `IndexHealth.Unknown` in `IndexStatus`).
    - [x] 1.3 **(removed per Amendment P)** — no `TenantBackendAvailability` record. Availability is derivable from `IndexStatus.<backend> == IndexHealth.Unknown` OR `IndexSizes.<backend> == null`.
    - [x] 1.4 Register `TenantSummary`, `TenantIndexSizes`, `TenantIndexStatus`, and `IndexHealth` enum in `MemoriesJsonContext.cs` for AOT-friendly serialization. Follow the existing `[JsonSerializable(typeof(...))]` pattern.
    - [x] 1.5 Create `TenantMetricsService` in `Server/Tenants/` that computes the enriched fields for a single tenant given a tenantId. Methods:
        - `GetMemoryUnitCountAsync(string tenantId, CancellationToken ct)` — counts Redis keys matching `{tenantId}:mu:*` via `SCAN` (bounded `COUNT` hint ≈ 1000, do NOT use `KEYS`). Return `long?` (null if Redis unavailable).
        - `GetIndexSizesAsync(string tenantId, CancellationToken ct)` — returns `(TenantIndexSizes, TenantIndexStatus)`. Queries RediSearch index stats (`FT.INFO {tenantId}:syntactic` → `num_docs`), Redis Vector index stats (`FT.INFO {tenantId}:semantic` → `num_docs`), FalkorDB node count (`MATCH (n) RETURN count(n)` — constant infra query, acceptable per D9 audit, mirror `VerifyTenantActivity` pattern). Each backend failure is caught internally and reported as `null` count + `IndexHealth.Unknown` status. Do NOT throw.

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

    - [x] 1.6 Update `GET /api/tenants` endpoint in `Program.cs` to project `TenantInfo` → `TenantSummary` in parallel (use `Task.WhenAll` to fetch per-tenant enrichment concurrently). Endpoint should tolerate per-tenant enrichment failure — a tenant whose metrics cannot be fetched still appears in the list with nulls for `IndexSizes.*` and `IndexHealth.Unknown` on each axis in `IndexStatus`.

        **Read-through state-store bypass (DAPR optimization):** For the list endpoint, source `ReindexRequired` and the embedding config via `DaprClient.GetStateAsync<TenantEmbeddingConfig>("statestore", key: <actor-state-key>, ct)` **directly**, NOT through `ITenantConfigurationActor.GetEmbeddingConfigAsync()`. Actor-proxy fan-out across N tenants triggers N actor activations, each serialized by the actor runtime — catastrophic for list latency. The actor's state key (`TenantConfigurationActor` uses `StateName = "embeddingConfig"` in its state manager) is readable directly from the underlying DAPR state store. **Caveats:** (a) state-store reads bypass actor validation in `TryGetStoredEmbeddingConfigAsync` — if a stored config is corrupted, the bypass returns raw data; the endpoint must treat unreadable config as `null` and set `reindexRequired=false`, `provider/model=null` in the summary; (b) keep actor proxy for single-tenant `GET /configuration` and for `PUT`. Document this bypass with an inline comment referencing this story's Dev Notes.

        **Key-format verification subtask (Amendment N):** DAPR prefixes actor state keys internally (`<app-id>||<actor-type>||<actor-id>||<state-name>` or similar, depending on version and state store). Before implementing the bypass, run `dapr state get --store-name statestore --key <candidate>` or inspect Redis directly with `KEYS *embeddingConfig*` against a known-configured tenant on the dev Aspire AppHost and **record the exact key format in the Dev Agent Record**. Do not hard-code a guessed format — verify once, then pin. If the format is DAPR-version-dependent, add a fallback to the actor-proxy path and log `Warning` on bypass-miss (cold fan-out degradation is better than silent-null).

        `LastActivityAt` sourced per amendment A (O(1) Redis read — see Task 2.3 below).

    - [x] 1.7 **Performance guard:** listing N tenants triggers ~N×4 backend calls (Redis SCAN + 2×FT.INFO + FalkorDB node count + actor read for config). Additionally, the FalkorDB `MATCH (n) RETURN count(n)` query is **O(|V|)** — a full-graph scan per tenant. For MVP scale (tenant count < ~100, graph size bounded by a few hundred thousand nodes) this is acceptable; optimization via caching with TTL or a materialized metric is explicitly deferred. Add a one-line code comment marking this. Do NOT add caching now (anti-pattern: speculative complexity).

- [x] Task 2: Tenant configuration view endpoint (AC: #2)
    - [x] 2.1 Create contract `Hexalith.Memories.Contracts.V1.TenantConfigurationView` with fields: `Id`, `DisplayName`, `Status`, `CreatedAt`, `DateTimeOffset? LastActivityAt`, `long? MemoryUnitCount`, `TenantEmbeddingConfig EmbeddingConfig`, `TenantIndexStatus IndexStatus`. **Return the full `TenantEmbeddingConfig` directly** — do NOT introduce a duplicate projection record. Rationale: `apiSecretKeyName` is a non-sensitive secret-name reference (not the secret value); maintaining two near-identical shapes is debt. Update the XML doc on `TenantEmbeddingConfig.ApiSecretKeyName` to clarify its non-sensitive nature. (Availability is derivable from `IndexStatus.<backend> == IndexHealth.Unknown` per Amendment P.)
    - [x] 2.2 Create `Hexalith.Memories.Contracts.V1.TenantIndexStatus` record with `IndexHealth RediSearch`, `IndexHealth RedisVector`, `IndexHealth FalkorDb`. Define `IndexHealth` enum with four values:
        - `Ready` — backend responded, index exists, `num_docs` (or node count) present.
        - `Missing` — backend responded, but the expected index/graph is absent (e.g., `FT.INFO` returns "no such index"; `GRAPH.LIST` doesn't include the tenant id). This is the signal for "provisioning incomplete" or "index dropped after deletion".
        - `Degraded` — backend responded, but the response indicates reduced capability: (a) the response payload is well-formed but `num_docs`/node-count field is absent or unparseable, OR (b) the server returns a `LOADING`/`BUSY` error response. **Not** used for timeouts or connection failures — those are `Unknown`.
        - `Unknown` — backend unreachable (connection timeout, `RedisConnectionException`, DAPR sidecar 503). Indicates availability failure, not data state.

        Register enum in `MemoriesJsonContext` with `CamelCaseStringEnumConverter`.

    - [x] 2.3 Extend `TenantMetricsService`:
        - `GetIndexStatusAsync(string tenantId, CancellationToken ct)` — for each backend: `Ready` if `FT.INFO` / `GRAPH.LIST` returns an index with the expected name, `Missing` if the call succeeds but the index is not found, `Degraded` if the call returns a partial / error response, `Unknown` if the backend is entirely unreachable. Must NEVER throw.
        - `GetLastActivityAtAsync(string tenantId, CancellationToken ct)` — returns the tenant's last-activity timestamp. **Implementation:** read `HGET {tenantId}:metadata lastActivityAt`. Value is stored as `Ticks.ToString(InvariantCulture)`; parse back with `long.TryParse` then `new DateTimeOffset(ticks, TimeSpan.Zero)`. Returns `null` if the field does not exist (fresh tenant, never ingested) or if Redis is unavailable. The write side lives in `IndexSyntacticActivity` (Task 4.6) AFTER the memory-unit hash succeeds (Amendment L).
            - **DO NOT** bound-sample over `{tenantId}:mu:*` keys; that produces wrong values for tenants with more memory units than the sample cap.
            - **DO NOT** route this through DAPR state store — it's a hot per-ingest write path; Redis direct write is simpler and cheaper.
            - **Hash field, not top-level string** (Amendment T): participates in the tenant-metadata group; deploy-doc must enforce `noeviction` policy so the field is not evicted under memory pressure.
            - Failure to write the timestamp during ingest is not a blocking error — log a `Warning` but do not fail the ingestion workflow. A stale `lastActivityAt` is acceptable; a failed ingest is not.
    - [x] 2.4 Add endpoint `GET /api/tenants/{tenantId}/configuration` in `Program.cs`:
        - Validate via `ValidateTenantId` (format) + `TenantStatusGuard.ValidateTenantExistsAsync` (registry). Route errors through `TenantStatusGuard.ToHttpResult`.
        - Fetch `TenantEmbeddingConfig` via existing `ITenantConfigurationActor.GetEmbeddingConfigAsync()`.
        - Fetch metrics via `TenantMetricsService`. Compose `TenantConfigurationView`. Return `Results.Ok`.
    - [x] 2.5 Register `TenantMetricsService` as singleton DI in `Program.cs` (stateless, reads from Redis/FalkorDB via keyed DI — `"redis"` for Redis, `"falkordb"` for FalkorDB) near existing `TenantStatusGuard` / `TenantRegistryService` registrations. Do NOT create a new DI extension method (anti-pattern: premature abstraction for a single registration).

- [x] Task 3: Update display name via PATCH (AC: #3)

    **Scope note (Amendment Q):** PATCH handles **`displayName` only**. Rate-limit updates flow through the existing `PUT /api/tenants/{tenantId}/embedding-config` endpoint — that path already validates, enforces breaking-change detection, and persists to the `TenantConfigurationActor`. Merging both concerns into one PATCH introduced a cross-store partial-failure mode (registry write + actor write) for no user benefit. Keep them separate.
    - [x] 3.1 Create contract `Hexalith.Memories.Contracts.V1.TenantUpdateInput` with a single field `string DisplayName` (non-null; the validator rejects empty/whitespace). Named record kept for API doc clarity and future extension (e.g., `description`, `tags` — but those are NOT in 5.5 scope).
    - [x] 3.2 Add a public method `TenantRegistryService.UpdateTenantDisplayNameAsync(string tenantId, string actor, string displayName, CancellationToken ct)` that uses the existing ETag-based CAS pattern (see `RegisterOrGetTenantEntryAsync` for the template — `GetStateAndETagAsync` + `TrySaveStateAsync` + retry loop capped at `MaxTenantRegistrationRetries` = 3). Preserve all other registry entry fields. Wrap the operation in a `Stopwatch` to capture `durationMs` for the log.

        Emit a `[LoggerMessage]` entry at `Information`, with **pinned field names** (for Phase 2 `ITenantAuditStore` compatibility):

        ```csharp
        [LoggerMessage(EventId = 5501, Level = LogLevel.Information,
          Message = "Tenant operational log: {TenantId} field={Field} oldValue={OldValue} newValue={NewValue} actor={Actor} occurredAt={OccurredAt:o} durationMs={DurationMs}")]
        private static partial void LogTenantFieldUpdated(
            ILogger logger, string tenantId, string field, string oldValue, string newValue,
            string actor, DateTimeOffset occurredAt, long durationMs);
        ```

        **Field-name contract (do not rename):** `tenantId`, `field`, `oldValue`, `newValue`, `actor`, `occurredAt`, `durationMs`. Names match the anticipated Phase 2 audit event record so migration is a one-to-one remap.

        **`actor` field population (Amendment R):** `"operator"` is a degenerate placeholder; the field must carry actionable signal. In MVP, populate it as `$"operator@{remoteIpOrNull ?? "unknown"}"` using `HttpContext.Connection.RemoteIpAddress?.ToString()`. This is NOT authentication — it's a weak attribution hint that beats a constant string. Identity-middleware-provided principal (Phase 1.5's `TenantAuthorizationMiddleware`) replaces this later with no log-signature change.

    - [x] 3.3 Add endpoint `PATCH /api/tenants/{tenantId}` in `Program.cs`:
        - Validate via `ValidateTenantId` + `TenantStatusGuard.ValidateTenantActiveAsync` (must be Active — `ToHttpResult` routes errors).
        - Reject `null` body → `400 INVALID_INPUT`.
        - Validate `DisplayName`: non-empty, length 1-100 chars, no control characters. Inline `ArgumentException.ThrowIfNullOrWhiteSpace` + length check. Do NOT create a new `DisplayNameValidator` class (anti-pattern: one-time helper).
        - Catch `Dapr.DaprException` → 503 `DAPR_UNAVAILABLE` with suggestion "Check service health via /healthz and retry." (mirrors POST `/api/tenants` pattern from 5-1).
        - Return `200 OK` with the updated `TenantSummary` (reuse Task 1's projection).
        - **Verify PATCH routing works on Aspire:** ASP.NET Core Minimal APIs default to standard HTTP methods; `MapPatch` is supported. After implementation, smoke-test `PATCH /api/tenants/{id}` against the running Aspire fixture — if a reverse proxy strips PATCH, fall back to `POST /api/tenants/{id}/updates` with the same `TenantUpdateInput` body. Document the chosen verb in the Dev Agent Record.
    - [x] 3.4 **Operational log scope:** Do NOT create a dedicated `TenantAuditStore` / `TenantAuditEvent` persistence layer. Consistent with 5-4's "Access telemetry is structured logging only in MVP. Dedicated audit store is Phase 2."

- [x] Task 4: Embedding model field propagation (AC: #6, FR70)
    - [x] 4.1 Add `public string? EmbeddingModel { get; init; }` to `Hexalith.Memories.Contracts.V1.MemoryUnit` (nullable for backward compatibility — legacy memory units won't have it).
    - [x] 4.2 Add `public required string EmbeddingModel { get; init; }` to `Hexalith.Memories.Contracts.V1.IndexInput` (required — new ingestions must supply it). **Migration audit:** before compile, grep all `new IndexInput` (and `IndexInput { ... }` initializers) across `src/` AND `tests/` — every call site (production workflow AND any hand-crafted test fixture in `Hexalith.Memories.Server.Tests`, `Hexalith.Memories.IntegrationTests`, `Hexalith.Memories.Contracts.Tests`) must supply `EmbeddingModel`. Compiler will flag missing ones but the audit ensures test fixtures get realistic values (e.g., `"gemini-embedding-001"`) not placeholder strings.
    - [x] 4.3 Add `EmbeddingModel` to `EmbeddingResult` — **verified to exist** at the output of `GenerateEmbeddingActivity` (`src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs:17` declares `WorkflowActivity<EmbeddingInput, EmbeddingResult>`). Add `public string? EmbeddingModel { get; init; }` (nullable to avoid breaking any DAPR-replayed historical workflow state). Locate the record via grep `EmbeddingResult` in the Contracts or Server project and register the new field in `MemoriesJsonContext` if the existing record is already registered there.
    - [x] 4.4 Update `GenerateEmbeddingActivity.RunAsync` to populate `EmbeddingModel` on its output (read from `config.Model`, already loaded for rate-limit ceiling).
    - [x] 4.5 Update the ingestion workflow (grep for where `IndexInput` is constructed — likely `IngestionWorkflow` or `IngestMemoryUnitActivity`) to pass `EmbeddingModel` through to `IndexInput`.
    - [x] 4.6 Update `IndexSyntacticActivity.RunAsync` to:
        - Persist `embeddingModel` as a `HashEntry` on the memory-unit Redis hash, alongside the existing `embeddingProvider` entry.
        - **Write the tenant last-activity key** (Amendment A, ordering per Amendment L): **AFTER the memory-unit hash write succeeds** (not before), fire `db.HashSetAsync($"{input.TenantId}:metadata", "lastActivityAt", input.IngestedAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture), flags: CommandFlags.FireAndForget)`. Do NOT await blocking; the activity's success must not depend on this write succeeding. Wrap in a try-catch that logs `Warning` on `RedisException` and swallows. **Ordering rationale (L):** writing `lastActivityAt` before the hash would advertise activity that never happened if the hash write subsequently fails — observability lies are worse than missing data.

        **Eviction-policy note (Amendment T):** the key is written without TTL. Use a **hash field** (`{tenantId}:metadata` HSET) rather than a top-level string so it participates in the tenant-metadata group under Redis policy. The Redis deployment documentation must state that tenant namespaces MUST use `maxmemory-policy noeviction` (or `volatile-*` policies that only evict TTL'd keys) so `lastActivityAt` is not silently lost under memory pressure. Add a deploy-doc TODO marker in the code comment and file a follow-up under Epic 8 (Observability & System Health) if no deploy-doc tracking exists yet.

    - [x] 4.7 Update `CaseService.GetMemoryUnitAsync` (and any other Redis-hash-to-`MemoryUnit` mapper) to read the `embeddingModel` field. Missing field (legacy data) → `null` (not a mismatch; legacy data pre-dates FR70).
    - [x] 4.8 Update `IndexGraphActivity` if it persists provider/dimensions on graph nodes — add model there too for consistency. If graph nodes don't persist these fields, skip (only syntactic hash matters per FR70).
    - [x] 4.9 **Legacy compatibility:** existing memory units indexed before this change have no `embeddingModel` field. The `GET memory-unit` response simply returns `null` for them. Do NOT attempt to backfill — migration is out of scope. Add a one-line comment in `CaseService.GetMemoryUnitAsync` reader explaining this.

- [x] Task 5: Unit tests (AC: #1, #2, #3, #4, #5, #6)
    - [x] 5.1 Create `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`. Use NSubstitute for `IConnectionMultiplexer` / `IDatabase` and for the FalkorDB client. Coverage:
        - `GetMemoryUnitCountAsync`: returns correct count from mock SCAN; returns `null` when Redis throws `RedisConnectionException`.
        - `GetIndexSizesAsync`: **parameterized test covering all 2³=8 backend-availability combinations** (RediSearch up/down × Redis Vector up/down × FalkorDB up/down). For each combination assert: (a) no exception thrown, (b) available backends report populated counts, (c) unavailable backends report `null` counts with `backendAvailability.<backend>=false`, (d) method always returns a fully-formed tuple. Use `[Theory]` + `[InlineData]` or `MemberData`.
        - `GetIndexStatusAsync`: returns `Ready` when `FT.INFO` returns a parseable `num_docs`, `Missing` when `FT.INFO` returns "no such index", `Degraded` when response is well-formed but `num_docs` is absent / unparseable, `Degraded` on `LOADING`/`BUSY` response, `Unknown` on `RedisConnectionException`.
    - [x] 5.2 Create `tests/Hexalith.Memories.Server.Tests/Tenants/TenantConfigurationEndpointTests.cs`:
        - `PATCH /api/tenants/{id}` with unknown tenant → 404 `TENANT_NOT_FOUND`.
        - `PATCH` with non-Active tenant → **parameterized `[Theory]` covering every `TENANT_*` code** (`TENANT_DELETING`, `TENANT_PROVISIONING`, `TENANT_FAILED`, `TENANT_UNAVAILABLE`) asserts `ToHttpResult` produces 409 and that `TENANT_NOT_FOUND` produces 404. This is the **mutation-guard test** protecting 5-4's `ToHttpResult` bug fix — any change that routes a non-not-found code to 404 or vice versa fails here.
        - `PATCH` with null body → 400 `INVALID_INPUT`.
        - `PATCH` with empty/whitespace `displayName` → 400 `INVALID_INPUT`.
        - `PATCH` with `displayName` > 100 chars → 400 `INVALID_INPUT`.
        - `PATCH` with valid `displayName` updates registry entry (verify via mock `TenantRegistryService.UpdateTenantDisplayNameAsync` received with correct `actor` value containing remote IP).
        - `PATCH` returns the updated `TenantSummary` projection (not a bare `TenantInfo`).
        - `PATCH` operational log captured — `[LoggerMessage]` event 5501 received with fields `tenantId`, `field="displayName"`, `oldValue`, `newValue`, `actor`, `occurredAt`, `durationMs`.
        - `PATCH` with DAPR sidecar unavailable (mock throws `DaprException`) → 503 `DAPR_UNAVAILABLE`.
        - `GET /api/tenants/{id}/configuration` for unknown tenant → 404.
        - `GET /api/tenants/{id}/configuration` returns composed `TenantConfigurationView` with the full `TenantEmbeddingConfig` record embedded + metrics.
        - `GET /api/tenants` returns `TenantSummary[]` with backend availability signal (via `IndexHealth.Unknown` on each axis), `reindexRequired` field, and `lastActivityAt` populated or null; one tenant's backend failure does NOT fail the whole list.
    - [x] 5.3 Create `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs` additions (or extend existing test class if present):
        - `SetEmbeddingConfigAsync` with only `RateLimitPerMinute` changed (provider/model/dimensions unchanged) → no breaking-change exception, value persisted.
        - Confirm `EmbeddingProviderDefaults.GetBreakingChangeFields` does NOT list `rateLimitPerMinute` in breaking fields (existing contract — test that it stays that way to protect AC3's rate-limit update path).
    - [x] 5.4 FR69 regression test in `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` (create or extend):
        - Mock `ITenantConfigurationActor.GetEmbeddingConfigAsync()` to return a config with `RateLimitPerMinute = 500`.
        - Assert `IEmbeddingRateLimiterActor.SetCeilingAsync(500)` received before `TryConsumeAsync`.
    - [x] 5.5 FR70 test in `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs`:
        - `RunAsync` with `input.EmbeddingModel = "gemini-embedding-001"` → verify Redis `HashSetAsync` received a `HashEntry("embeddingModel", "gemini-embedding-001")`.
        - Legacy-hash-read test in `CaseServiceTests` (or the TenantContextEnforcementTests mismatch pattern): hash without `embeddingModel` field → `MemoryUnit.EmbeddingModel` is `null`, no exception.

- [x] Task 6: Integration tests (AC: #1, #2, #3, #4, #6)
    - [x] 6.1 Create `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`. Most scenarios use `[Fact(Skip = "Requires Aspire AppHost fixture")]` consistent with 5-1 / 5-2 / 5-3 / 5-4 deferral pattern. Required before Gate 2 sign-off.

        **Exception — FR70 golden path unskipped:** the AC6 end-to-end test (last scenario in 6.2) asserting `embeddingModel` propagation through the full ingest → index → read path **must NOT be skipped if any ingestion-path integration fixture already runs in CI**. If no such fixture exists, fall back to a unit-level test in `IngestionWorkflowTests` that asserts `IndexInput.EmbeddingModel` is populated from `EmbeddingResult.EmbeddingModel`. Rationale: FR70 is the one new durable field and the primary regression risk; it should not be blocked behind the Aspire deferral.

    - [x] 6.2 Scenarios:
        - List tenants against a real Redis + FalkorDB → response includes `memoryUnitCount`, non-null `indexSizes`, `reindexRequired`, and `lastActivityAt` for an active tenant with indexed data.
        - List tenants with one backend stopped → response still returns the tenant; `backendAvailability.RedisVector=false`; other counts populated.
        - `GET /api/tenants/{id}/configuration` end-to-end returns composed view with the full `TenantEmbeddingConfig` (including `apiSecretKeyName` — verify XML doc clarifies non-sensitive nature) and `IndexStatus=Ready` on all three backends.
        - `PATCH /api/tenants/{id}` with `displayName` → subsequent `GET /api/tenants/{id}` reflects new name; log capture contains the `Information` operational-log entry with `oldValue`/`newValue`/`durationMs`.
        - `PUT /api/tenants/{id}/embedding-config` with `rateLimitPerMinute=200` (non-breaking change) → subsequent `GET /api/tenants/{id}/embedding-config` reflects new ceiling; next ingestion observes new rate limit at the `EmbeddingRateLimiterActor` on the next `GenerateEmbeddingActivity` invocation.
        - `PUT /api/tenants/{id}/embedding-config` with changed `dimensions` and `forceReindex=false` → 409 `EMBEDDING_CONFIG_BREAKING_CHANGE`; with `forceReindex=true` → 200 and `reindexRequired=true`; subsequent `GET /api/tenants` shows `reindexRequired=true` on that tenant's summary.
        - **[FR70 golden — unskip per 6.1]** Ingest one memory unit end-to-end → `GET memory-unit` response includes `embeddingProvider=google` AND `embeddingModel=gemini-embedding-001`; Redis hash inspection shows both `embeddingProvider` and `embeddingModel` fields persisted.

    ### Review Findings
    - [x] [Review][Defer] Keep the current actor-proxy fallback in `GET /api/tenants` instead of requiring the Task 1.6 state-store bypass before sign-off [src/Hexalith.Memories.Server/Program.cs:1829] — deferred by review decision. Reason: state-store key format is not empirically verified yet, so the actor fallback is the safer MVP path for now.
    - [x] [Review][Patch] PATCH display-name path can return 500 on CAS exhaustion or a late missing-tenant race [src/Hexalith.Memories.Server/Program.cs:467]
    - [x] [Review][Patch] Configuration endpoint null-forgives a second registry read and can 500 after concurrent deletion [src/Hexalith.Memories.Server/Program.cs:434]
    - [x] [Review][Patch] Configuration endpoint does not translate actor/config retrieval failures into structured availability responses [src/Hexalith.Memories.Server/Program.cs:437]
    - [x] [Review][Patch] Display-name update can still write a `Deleting` tenant after the pre-write Active check races with DELETE [src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs:285]
    - [x] [Review][Patch] Replay fallback stores `provider:model` in `EmbeddingModel` instead of the model identifier [src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs:129]
    - [x] [Review][Patch] Graph indexing still omits `embeddingModel` even though graph nodes persist provider and dimensions [src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs:44]
    - [x] [Review][Patch] Story-promised endpoint and metric coverage is incomplete, leaving new runtime branches largely untested [tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs:1]
    - [x] [Review][Defer] Breaking-change conflict response still returns `EmbeddingConfigChangeRequired` instead of the pinned `EMBEDDING_CONFIG_BREAKING_CHANGE` contract [src/Hexalith.Memories.Server/Program.cs:1888] — deferred, pre-existing

## Dev Notes

### First Principles Framing

**What this story IS:** Exposing existing tenant state (configuration, index sizes, activity counts) to operators via HTTP endpoints; adding the missing FR70 field (`embeddingModel`) on memory units; allowing display name and rate-limit updates post-creation.

**What this story IS NOT:**

- NOT an audit store. Structured logging is the MVP audit mechanism (consistent with 5-4 decision).
- NOT a new validation framework. Reuse `EmbeddingProviderDefaults.Validate` and `TenantStatusGuard`.
- NOT a reindexing workflow. Reindex on config change is out of scope — the existing `forceReindex` acknowledgment flag is sufficient for MVP; full reindex workflow is tracked elsewhere.
- NOT metric backends. `memoryUnitCount` / `indexSizes` are computed on demand, not exposed via OpenTelemetry. Caching / OTEL is Phase 2.

**Mental model for the dev agent:**

- AC1 (list) = _enriched projection_ over `TenantRegistryService.ListTenantsAsync` + three backend count calls.
- AC2 (view config) = _fan-out_ across `TenantRegistryService` + `TenantConfigurationActor` + `TenantMetricsService`.
- AC3 (update) = _two-step write_ (registry entry for name, config actor for rate limit). No distributed transaction.
- AC4 (breaking-change guard) = _already built_. Extend tests; do NOT rewrite.
- AC5 (rate limit ceiling) = _already wired in `GenerateEmbeddingActivity`_. Add regression test.
- AC6 (FR70 model tracking) = _schema addition_ — `EmbeddingModel` on `MemoryUnit`, `IndexInput`, and Redis hash; thread through ingestion.

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

| Component                                   | Location                                        | Usage in This Story                                                                                         |
| ------------------------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `TenantRegistryService`                     | `Server/Tenants/TenantRegistryService.cs`       | `ListTenantsAsync`, `GetTenantEntryAsync`, new `UpdateTenantDisplayNameAsync` (ETag CAS pattern)            |
| `TenantStatusGuard`                         | `Server/Tenants/TenantStatusGuard.cs`           | `ValidateTenantExistsAsync` (GET /configuration), `ValidateTenantActiveAsync` (PATCH), `ToHttpResult`       |
| `TenantConfigurationActor`                  | `Server/Actors/TenantConfigurationActor.cs`     | `GetEmbeddingConfigAsync`, `SetEmbeddingConfigAsync`                                                        |
| `EmbeddingProviderDefaults`                 | `Server/Ingestion/EmbeddingProviderDefaults.cs` | `Validate`, `GetBreakingChangeFields`                                                                       |
| `EmbeddingConfigChangeException`            | `Server/Ingestion/...`                          | Already raised by actor; already surfaced as 409 in `PUT embedding-config`                                  |
| `IConnectionMultiplexer` keyed `"redis"`    | DI                                              | Redis SCAN + FT.INFO                                                                                        |
| `IConnectionMultiplexer` keyed `"falkordb"` | DI                                              | FalkorDB GRAPH.QUERY                                                                                        |
| `IGraphQueryBuilder` / `GraphQueryBuilder`  | `Server/Graph/`                                 | For graph node count (optional; constant query is acceptable per D9)                                        |
| `MemoriesJsonContext`                       | `Contracts/V1/`                                 | Register all new contracts for AOT serialization                                                            |
| `ErrorResponse`                             | `Contracts/V1/ErrorResponse.cs`                 | Standard error response format `(code, message, suggestion)`                                                |
| `ValidateTenantId`                          | `Program.cs` static helper                      | Format validation                                                                                           |
| `ITenantConfigurationActor`                 | `Server/Actors/`                                | Already DI-registered via actor runtime                                                                     |
| `EmbeddingRateLimiterActor.SetCeilingAsync` | `Server/Actors/`                                | Already called from `GenerateEmbeddingActivity.cs:58` with `config.RateLimitPerMinute` — FR69 already wired |

### Current Endpoint State (Baseline)

**Existing and reused as-is:**

- `GET /api/tenants` — returns `TenantInfo[]`; this story **replaces the return type** with `TenantSummary[]` (see Breaking Changes at top of file).
- `GET /api/tenants/{tenantId}` — returns basic `TenantInfo`. **Kept minimal deliberately** — pre-Gate-2 no external consumer needs the basic form, but the provisioning/deletion workflow endpoints (`provision-status`, `deletion-status`) return workflow state that callers cross-reference with basic tenant info. Keeping the minimal GET avoids binding those workflow-adjacent lookups to the richer `TenantConfigurationView` which triggers N+3 backend calls. If a future audit shows no such callers, collapse in a later story. Do NOT collapse now as part of 5.5 — out of scope.
- `GET /api/tenants/{tenantId}/embedding-config` — returns `TenantEmbeddingConfig`; kept as-is. This is the **write-shape** endpoint (same shape accepted by PUT). Story 5.5's `GET /configuration` returns the same `TenantEmbeddingConfig` record embedded in `TenantConfigurationView` — single contract shape reused (no projection).
- `PUT /api/tenants/{tenantId}/embedding-config` — accepts `TenantEmbeddingConfig` + `forceReindex` query param; already returns 409 on breaking change. **Covers AC4 entirely — no code change needed**, only tests.

**New in this story:**

- `GET /api/tenants/{tenantId}/configuration` — composed `TenantConfigurationView` (embedding + metrics + status).
- `PATCH /api/tenants/{tenantId}` — `displayName` only. Rate-limit updates go through existing `PUT /api/tenants/{tenantId}/embedding-config`.

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

- **No audit persistence:** Structured logs only. Phase 2 adds `ITenantAuditStore`. The `[LoggerMessage]` field names (`tenantId`, `field`, `oldValue`, `newValue`, `actor`, `occurredAt`, `durationMs`) are pinned to match the anticipated audit event contract.
- **`actor` field in operational log is weak (Amendment R):** populated as `"operator@{remoteIp}"`. This is a hint, not an authenticated principal. Identity-aware middleware (Phase 1.5, D8) replaces this without changing the log signature.
- **TOCTOU between PATCH and DELETE (Amendment M):** a `PATCH /api/tenants/{id}` may commit via ETag CAS milliseconds before the deletion workflow updates registry status. The `PATCH` succeeds but the value is lost when deletion completes. Not a data leakage vector; concurrent admin operations are rare. Acceptable for MVP.
- **PATCH is not workflow-replayable (Amendment O):** `PATCH /api/tenants/{id}` writes the registry synchronously from the endpoint. A crash mid-PATCH may or may not have committed; operator retries are idempotent (same `displayName` → same result). DAPR workflow saga semantics do NOT apply to this path — by design per the "Architectural Pattern Shift" section.
- **`reindexRequired` has no MVP resolution path (Amendment S):** the tenant summary / configuration view exposes the flag, but no endpoint triggers a reindex in MVP. Operator-facing runbook resolution: `DELETE /api/tenants/{id}` + re-provision + re-ingest. An automated reindex workflow is Phase 2 (tracked separately, not scoped here).
- **Mid-batch rate-limit drop dynamics (Amendment V):** dropping `rateLimitPerMinute` via `PUT /api/tenants/{id}/embedding-config` during an in-flight ingestion batch stalls the batch until the per-minute rate bucket refills at the new ceiling. Batches pre-sized against the old ceiling may take significantly longer than estimated. Operator guidance: lower rate limits between batches, not during.
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

claude-opus-4-6 (Claude Opus 4.6, 1M context)

### Debug Log References

- Pre-existing build failure in `src/Hexalith.Memories.AppHost/Program.cs` (CommunityToolkit.Aspire.Hosting.Dapr `IDaprSidecarResource` / `WithEnvironment` generic constraint) — verified present on baseline `b33cd71` before any 5-5 changes via `git stash`. Not in scope; noted here so reviewers don't mistake it for a regression.
- Pre-existing test failures in `SaveDedupKeyActivityTests` (2 tests, unrelated to 5-5) — verified present on baseline via `git stash`.
- DAPR actor state-store key format (Task 1.6 / Amendment N) **not empirically verified** in this session. The list endpoint therefore uses the actor-proxy parallel fan-out path (each tenant has a distinct actor instance, so cross-tenant concurrency does not serialize under the single-threaded-per-actor model). This matches the story's explicit "fallback to actor-proxy path" branch — the state-store read-through bypass is deferred as a documented Phase 2 optimization. Acceptable for MVP tenant counts (&lt; ~100).

### Completion Notes List

- **Code-review patch set (post-review fixes):** extracted `TenantEndpointHandlers` so tenant endpoint branches are directly testable; `GET /api/tenants/{tenantId}/configuration` now handles concurrent deletion and Dapr/config retrieval failures with structured `404` / `503` responses; `PATCH /api/tenants/{tenantId}` now turns late delete/update races into `TENANT_DELETING` / `TENANT_UPDATE_CONFLICT` instead of leaking 500s. `TenantRegistryService.UpdateTenantDisplayNameAsync` now rechecks tenant status inside the CAS loop before writing.
- **FR70 follow-up from review:** ingestion replay fallback now derives the model identifier from compound provider strings when historical workflow payloads predate `EmbeddingResult.Model`; graph indexing now persists `embeddingModel` alongside provider/dimensions by threading the value through `IGraphQueryBuilder` / `GraphQueryBuilder` / `IndexGraphActivity`.
- **Focused verification after review fixes:** `dotnet test tests/Hexalith.Memories.Server.Tests/Hexalith.Memories.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~TenantConfigurationEndpointTests|FullyQualifiedName~TenantMetricsServiceTests|FullyQualifiedName~TenantRegistryServiceTests|FullyQualifiedName~IngestionWorkflowTests|FullyQualifiedName~IndexGraphActivityTests|FullyQualifiedName~GraphQueryBuilderTests|FullyQualifiedName~GenerateEmbeddingActivityTests|FullyQualifiedName~IndexSyntacticActivityTests|FullyQualifiedName~CaseServiceTests|FullyQualifiedName~TenantConfigurationActorTests"` → **267 passed, 0 failed**.
- **Task 4 (FR70, AC6):** `MemoryUnit.EmbeddingModel` nullable (legacy null), `IndexInput.EmbeddingModel` required (compile-time migration check; audited 1 production + 4 test call sites). `EmbeddingResult.Model` init-only nullable for DAPR-replay compatibility. `IngestionWorkflow` threads model through to `IndexInput` (fallback to compound provider string if replayed payload predates the field). `IndexSyntacticActivity` persists `embeddingModel` hash entry AND writes `{tenantId}:metadata` `lastActivityAt` fire-and-forget AFTER the MU-hash write (Amendment L ordering, Amendment T eviction note). `CaseService.ParseMemoryUnitFromHash` reads the field with null fallback for pre-FR70 data. No backfill (Anti-Pattern #9).
- **Task 1 (AC1, FR41):** New contracts `TenantSummary`, `TenantIndexSizes`, `TenantIndexStatus`, `IndexHealth` (enum) — all registered in `MemoriesJsonContext`. `IndexHealth` uses `CamelCaseStringEnumConverter<T>`. No `TenantBackendAvailability` record (Amendment P). `GET /api/tenants` now returns `TenantSummary[]`; parallel per-tenant enrichment via `Task.WhenAll`; per-tenant failure tolerated. `TenantMetricsService` created with `GetMemoryUnitCountAsync` (SCAN, never KEYS), `GetIndexSizesAsync` (reuses existing `IndexSchemaDefinitions.TryGetDocumentCount` — no duplicate parser, simpler than inlining the FT.INFO parse snippet from Task 1.5), and `GetLastActivityAtAsync` (HGET `{tenantId}:metadata` with `InvariantCulture` Ticks). Performance-guard comment documents O(|V|) FalkorDB scan per tenant for MVP.
- **Task 2 (AC2, FR45):** `TenantConfigurationView` embeds the full `TenantEmbeddingConfig` directly (Amendment C). `TenantEmbeddingConfig.ApiSecretKeyName` XML doc now explicitly states it is the secret name, not the value. `GET /api/tenants/{tenantId}/configuration` composes `ValidateTenantExistsAsync` + actor-proxy + metrics. `TenantMetricsService` registered as singleton inline (Anti-Pattern #10: no dedicated DI extension).
- **Task 3 (AC3, FR42):** `TenantUpdateInput` (display-name only — Amendment Q). `TenantRegistryService.UpdateTenantDisplayNameAsync` uses existing ETag-CAS retry loop (max 3). `[LoggerMessage]` **EventId 5501** carries `tenantId`/`field`/`oldValue`/`newValue`/`actor`/`occurredAt`/`durationMs` — names pinned for Phase 2 audit-store migration (Amendment J). `PATCH /api/tenants/{tenantId}` validates inline (no `DisplayNameValidator` class — Anti-Pattern #10), enforces `ValidateTenantActiveAsync`, populates `actor` as `"operator@{remoteIp}"` (Amendment R), catches `Dapr.DaprException` → 503 (Amendment K), returns the updated `TenantSummary`.
- **Task 5 (Unit tests):** 36+ new tests across 8 files. Key tests: `TenantMetricsServiceTests` 2³ backend-availability `[Theory]` + individual Missing/Loading/Unparseable/unavailable paths + `GetLastActivityAtAsync` 4-way coverage + `GetMemoryUnitCountAsync` unavailability; `TenantConfigurationEndpointTests` **ToHttpResult mutation-guard `[Theory]`** over every `TENANT_*` code (protects 5-4's fix); `TenantConfigurationActorTests` new `GetBreakingChangeFields_RateLimitOnlyDelta_ShouldReturnEmptyList` contract-guard; `GenerateEmbeddingActivityTests` FR69 `SetCeilingAsync(500)` + FR70 `EmbeddingResult.Model` assertions; `IndexSyntacticActivityTests` FR70 hash-field assertion; `CaseServiceTests` new field read + legacy null read; `TenantRegistryServiceTests` 4 new `UpdateTenantDisplayNameAsync` tests with `ListLogger` log capture asserting `EventId=5501` and all pinned fields.
- **Task 6 (Integration tests):** `TenantConfigurationIntegrationTests.cs` (10 `[Fact(Skip)]` scenarios matching 5-1/5-2/5-3/5-4 deferral pattern). FR70 golden path has a **unit-level fallback in `IngestionWorkflowTests`** per Task 6.1 — `RunAsync_ShouldPropagateEmbeddingModelFromEmbeddingResultToIndexInput` asserts the workflow threads `EmbeddingResult.Model` into `IndexInput.EmbeddingModel`; runs in CI without the Aspire fixture.
- **Test results:** `Hexalith.Memories.Server.Tests` — **774 passed**, 2 failed (pre-existing `SaveDedupKeyActivityTests`, verified on baseline); `Hexalith.Memories.Contracts.Tests` — **272 passed**. Regression bar held (5-4 established 1051+ tests passing; this story adds ~36 new passing tests).
- **PATCH verb routing smoke-test (Task 3.3):** deferred — requires a running Aspire fixture not available in this session. The minimal-API `MapPatch` is the ASP.NET Core default; no known reverse-proxy fronting the dev AppHost. If a future environment strips PATCH, the story documents a POST-with-subresource fallback.
- **Definition of Done:** every checkbox [x]; ACs 1–6 implemented; review patch findings resolved or deferred; tests added; File List complete; Change Log updated; Status set to `done`.

### File List

**New files:**

- `src/Hexalith.Memories.Contracts/V1/TenantSummary.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantIndexSizes.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantIndexStatus.cs`
- `src/Hexalith.Memories.Contracts/V1/IndexHealth.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantConfigurationView.cs`
- `src/Hexalith.Memories.Contracts/V1/TenantUpdateInput.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantMetricsService.cs`
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs`
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs`
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Tenants/TenantConfigurationIntegrationTests.cs`

**Modified files:**

- `src/Hexalith.Memories.Contracts/V1/MemoryUnit.cs` — added nullable `EmbeddingModel` (FR70).
- `src/Hexalith.Memories.Contracts/V1/IndexInput.cs` — added required `EmbeddingModel` (FR70).
- `src/Hexalith.Memories.Contracts/V1/TenantEmbeddingConfig.cs` — XML doc on `ApiSecretKeyName` clarifies non-sensitive nature.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — registered Story 5.5 contracts + `IndexHealth` enum.
- `src/Hexalith.Memories.Server/Program.cs` — replaced `GET /api/tenants` with enriched projection; added `GET /api/tenants/{tenantId}/configuration`, `PATCH /api/tenants/{tenantId}`, `BuildTenantSummaryAsync` helper; registered `TenantMetricsService` singleton.
- `src/Hexalith.Memories.Server/Tenants/TenantRegistryService.cs` — new `UpdateTenantDisplayNameAsync` + `[LoggerMessage]` EventId 5501.
- `src/Hexalith.Memories.Server/Tenants/TenantEndpointHandlers.cs` — extracted testable tenant endpoint logic for summary/configuration/patch flows and structured Dapr-unavailable handling.
- `src/Hexalith.Memories.Server/Activities/Ingestion/EmbeddingResult.cs` — added nullable `Model`.
- `src/Hexalith.Memories.Server/Activities/Ingestion/GenerateEmbeddingActivity.cs` — populates `Model = config.Model`.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — threads `EmbeddingModel` with safe fallback.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs` — persists `embeddingModel` hash entry; fire-and-forget `{tenantId}:metadata lastActivityAt` write.
- `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` — threads `embeddingModel` through the graph merge contract.
- `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` — persists `embeddingModel` on memory-unit graph nodes.
- `src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs` — passes `EmbeddingModel` into graph indexing.
- `src/Hexalith.Memories.Server/Cases/CaseService.cs` — reads `embeddingModel` hash field with null fallback.
- `tests/Hexalith.Memories.TestHelpers/Factories/IndexInputFactory.cs` — default `EmbeddingModel`.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSyntacticActivityTests.cs` — test-input set `EmbeddingModel`; new FR70 hash-field test.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexSemanticActivityTests.cs` — test-input set `EmbeddingModel`.
- `tests/Hexalith.Memories.Server.Tests/Activities/Indexing/IndexGraphActivityTests.cs` — test-input set `EmbeddingModel`; asserts graph merge receives it.
- `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` — updated merge-node assertions for persisted `embeddingModel`.
- `tests/Hexalith.Memories.Server.Tests/Activities/Ingestion/GenerateEmbeddingActivityTests.cs` — new FR69 SetCeilingAsync assertion + FR70 Model assertion.
- `tests/Hexalith.Memories.Server.Tests/Actors/TenantConfigurationActorTests.cs` — new rate-limit-only breaking-fields contract test.
- `tests/Hexalith.Memories.Server.Tests/Cases/CaseServiceTests.cs` — 2 new embeddingModel tests (new field + legacy null).
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantRegistryServiceTests.cs` — 4 new `UpdateTenantDisplayNameAsync` tests + `ListLogger`.
- `tests/Hexalith.Memories.Server.Tests/Tenants/TenantMetricsServiceTests.cs` — expanded count coverage, including successful memory-unit enumeration.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/TenantConfigurationEndpointTests.cs` — added runtime branch coverage for concurrent delete/conflict/Dapr-unavailable paths and typed-result execution harness.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — sets `EmbeddingResult.Model` in happy-path setup; new FR70 propagation assertion (unit-level fallback for the Task 6.1 deferred integration golden path).
- `tests/Hexalith.Memories.Contracts.Tests/V1/IndexInputSerializationTests.cs` — `CreateFullInput` sets `EmbeddingModel`.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `5-5-tenant-configuration-and-listing: done`.

## Change Log

| Date       | Change                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2026-04-14 | Story 5.5 context created via `bmad-create-story`; ready-for-dev.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 2026-04-14 | Party-mode review amendments applied: `TenantSummary` gains `reindexRequired`/`lastActivityAt`; `apiSecretKeyName` excluded from view via new `TenantEmbeddingConfigView`; `IndexHealth` states fully defined (`Ready`/`Missing`/`Degraded`/`Unknown`); AC3 wording clarifies structured-log audit mechanism; `FT.INFO` parse snippet inlined in Task 1.5; O(\|V\|) cost of FalkorDB node count documented in Task 1.7; 2³ backend-availability property test in Task 5.1; `ToHttpResult` mutation-guard theory in Task 5.2; FR70 golden end-to-end test unskipped (or unit-level fallback) in Task 6.1; breaking changes hoisted to top-of-file section.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 2026-04-14 | Advanced-elicitation amendments (methods 1–5 applied, A–J): (A) `lastActivityAt` via dedicated `{tenantId}:lastActivityAt` Redis key written fire-and-forget from `IndexSyntacticActivity` — replaces bounded-sample; (B) AC3 "audit trail" reworded to "operational log"; (C) dropped `TenantEmbeddingConfigView` projection — configuration view now embeds the full `TenantEmbeddingConfig`, `apiSecretKeyName` documented as non-sensitive; (D) `FT.INFO` parse snippet hardened with `TryParse` guard and string/integer dual handling; (E) read-through state-store bypass documented for list-endpoint config enrichment (avoids N actor activations); (F) PATCH routing smoke-test subtask added to Task 3.4 with POST fallback; (G) `IndexInput` constructor migration audit across src/ + tests/ added to Task 4.2; (H) TL;DR block added at top of file; (I) "Architectural Pattern Shift" section documenting first direct-registry-write-from-endpoint vs. the 5-1–5-4 workflow pattern; (J) `[LoggerMessage]` field names pinned (`tenantId`, `field`, `oldValue`, `newValue`, `actor`, `occurredAt`) to match anticipated Phase 2 audit contract.                                                                                                                             |
| 2026-04-14 | Advanced-elicitation round 2 (chaos / Occam / self-consistency / Feynman / hindsight, K–V): (K) `PATCH` endpoint catches `DaprException` → 503 `DAPR_UNAVAILABLE`; (L) `lastActivityAt` write re-ordered to AFTER the memory-unit hash write (no advertising of activity that didn't happen); (M) TOCTOU between PATCH and DELETE documented in Known MVP Limitations; (N) state-store key-format verification subtask added to read-through bypass (dapr state get / Redis KEYS inspection, pin the format, actor-proxy fallback); (O) "PATCH is not workflow-replayable" note added to Known MVP Limitations; (P) `TenantBackendAvailability` record dropped — availability folded into `IndexHealth.Unknown` per axis; (Q) **`PATCH` scoped to `displayName` only** — rate-limit updates remain on existing `PUT /embedding-config` (three-way self-consistency + Occam); (R) `actor` field populated as `"operator@{remoteIp}"` instead of constant string; (S) `reindexRequired` resolution-path gap documented; (T) `lastActivityAt` stored as Redis **hash field** under `{tenantId}:metadata` with `noeviction` deploy-doc guidance; (U) `durationMs` added to operational log template via `Stopwatch`; (V) mid-batch rate-limit-drop dynamics documented in Known MVP Limitations. |
| 2026-04-14 | Story 5.5 implementation complete (Tasks 1–6). Tasks 4 + 1 + 2 + 3 delivered per Dev Notes priority; Task 5 added 36+ unit tests; Task 6 integration tests use `[Fact(Skip)]` deferral pattern with FR70 golden-path unit-level fallback in `IngestionWorkflowTests`. FR41, FR42, FR43, FR45, FR69, FR70 satisfied. ACs 1–6 implemented. `Hexalith.Memories.Server.Tests`: **774 passing** (+ 2 pre-existing unrelated `SaveDedupKeyActivityTests` failures confirmed via baseline `git stash`). `Hexalith.Memories.Contracts.Tests`: 272 passing. Status → `review`.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 2026-04-14 | Code-review follow-up completed for Story 5.5. Resolved all 7 `[Review][Patch]` findings by extracting `TenantEndpointHandlers`, hardening tenant PATCH/configuration race and Dapr error handling, rechecking tenant status inside the registry CAS loop, correcting FR70 replay fallback model derivation, and persisting `embeddingModel` through graph indexing. Expanded focused endpoint/metrics/workflow/graph tests and verified with a focused server-test slice: **267 passed, 0 failed**. Status → `done`.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
