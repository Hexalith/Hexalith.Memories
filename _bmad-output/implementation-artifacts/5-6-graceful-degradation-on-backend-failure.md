# Story 5.6: Graceful Degradation on Backend Failure

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## TL;DR

**What ships:** close the remaining gaps so every read path honors the "partial failure → degraded service, not total failure" contract (NFR18 / FR66). Concretely: (a) add backend-failure catches to the **single-axis** `axis=syntactic` / `axis=semantic` read paths on `/api/search`, (b) add a `RedisConnectionException` catch to the dedicated graph endpoints (`axis=graph` in `/api/search` and `/api/tenants/{tenantId}/traverse`) returning **503 `GRAPH_UNAVAILABLE`**, (c) promote the hybrid "all enabled axes unavailable" case from an empty success to a **503 `ALL_BACKENDS_UNAVAILABLE`**, (d) assert the existing `IngestionWorkflow` retry policy behavior (FR9 / NFR22), (e) document & test auto-recovery via `StackExchange.Redis` auto-reconnect. **No new contracts, no new services, no new workflows.**

**What does NOT ship:** circuit-breaker library (Polly), backend-health polling endpoint, outage dashboard, retry count tuning, new error-reporting infrastructure, CLI degradation display (Epic 7), predictive failover. No changes to `HybridSearchService` core logic — it already handles per-axis degradation; this story only plugs the **endpoint-level** gaps.

**Primary risks:** regressing the hybrid happy-path, breaking `axis=graph` error semantics (existing `TimeoutException`→504 must stay), misclassifying `RedisServerException` ("no such index") as unavailability (that is `Missing`/empty result, not a backend failure). Auto-recovery appears automatic via the multiplexer — do NOT add manual reconnect logic.

## Breaking Changes (Pre-Gate-2 MVP)

1. **`axis=graph` on `/api/search`**: no behavior change on success paths. New 503 `GRAPH_UNAVAILABLE` response is additive — previously this path would propagate `RedisConnectionException` as an unhandled 500. Moving from 500 → 503 with a typed error code is a **refinement**, not a contract break for any current caller (no caller branches on 500 body shape).
2. **`axis=syntactic` / `axis=semantic` (non-hybrid) on `/api/search`**: same refinement. New 503 `BACKEND_UNAVAILABLE` replaces unhandled 500 on `RedisConnectionException` / transient Redis errors. Embedding-specific 503 responses (`EmbeddingApiException`, `EmbeddingRateLimitException`, `SemanticSearchDimensionMismatchException`) **stay as they are** — do not collapse.
3. **`/api/tenants/{tenantId}/traverse`**: new 503 `GRAPH_UNAVAILABLE` response on `RedisConnectionException`. Existing `TraversalResult` return shape is unchanged for the success path.
4. **Hybrid search total-failure response**: when **every enabled axis** becomes unavailable at runtime, `/api/search?axis=hybrid` now returns **503 `ALL_BACKENDS_UNAVAILABLE`** instead of `200 OK { results: [], degraded: true, unavailableAxes: [...] }`. Empty results with `degraded: true` is a valid partial state when at least one axis succeeded; it is misleading when no axis ran at all. Callers relying on the empty-OK shape must handle 503.

## Story

As a developer,
I want the system to return partial results when a backend is unavailable and a clear, typed error when no backend can serve the request,
so that I get the best possible answer during infrastructure issues and never see an unhandled 500 or an empty-but-successful response masking total failure.

## Acceptance Criteria

1. **Given** Redis Vector is unavailable (connection exception or LOADING/BUSY) but RediSearch and FalkorDB are healthy, **when** `/api/search?axis=hybrid` is executed with `axes=syntactic,semantic,graph` **and** a valid `graphStartNodeId`, **then** HTTP `200 OK` is returned with a `HybridSearchResult` body in which `results` contain fused entries from syntactic + graph only, `degraded=true`, `unavailableAxes=["semantic"]`, and the response structure matches `HybridSearchResult` (FR66, NFR18). Reaching the same outcome via `axes=syntactic,semantic` (no graph) yields `unavailableAxes=["semantic"]` and results from syntactic only.

2. **Given** FalkorDB is unavailable (`RedisConnectionException` on the `"falkordb"` multiplexer) but Redis Stack (RediSearch + Redis Vector) is healthy, **when** (a) `/api/search?axis=hybrid` is executed, **then** `200 OK` with results from syntactic + semantic only, `degraded=true`, `unavailableAxes=["graph"]`; **and when** (b) `/api/search?axis=graph&startNodeId=...` **or** `/api/tenants/{tenantId}/traverse?startNodeId=...` is executed, **then** HTTP `503` is returned with `ErrorResponse` body `{"code":"GRAPH_UNAVAILABLE","message":"Graph backend is unavailable.","suggestion":"Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status."}`.

3. **Given** all three backends are unavailable (both multiplexers throw `RedisConnectionException`), **when** any read path is attempted, **then**:
    - `/api/search?axis=hybrid` returns HTTP `503` with body `{"code":"ALL_BACKENDS_UNAVAILABLE","message":"All enabled search backends are unavailable.","suggestion":"Check infrastructure status (Redis Stack, FalkorDB). The service auto-recovers when backends reconnect; retry the request."}` and the response body includes an `unavailableAxes` field listing every enabled axis. **Do not return `200 OK` with an empty results array.**
    - `/api/search?axis=syntactic` (or `axis=semantic`) returns HTTP `503` with body `{"code":"BACKEND_UNAVAILABLE","message":"Search backend is unavailable.","suggestion":"Retry the request; the backend auto-recovers when Redis reconnects."}`.
    - `/api/search?axis=graph` and `/api/tenants/{tenantId}/traverse` return the 503 `GRAPH_UNAVAILABLE` response from AC2.

4. **Given** a backend recovers after being unavailable, **when** subsequent requests are made, **then** the system resumes serving all available axes **without any manual operator action**. This is delegated to `StackExchange.Redis`'s built-in auto-reconnect behavior on `IConnectionMultiplexer` — the multiplexer reopens the connection on the next operation. No explicit reconnect logic is added. **Verification scope:** unit-level tests assert no residual "degraded" state sticks to the request-scoped search path (the catch block is stateless; a fresh mock configuration on the second call succeeds). **True multiplexer auto-reconnect is NOT verified at unit level** — it is an assumed library behavior documented in the `StackExchange.Redis` readme. End-to-end verification is deferred to the Aspire integration fixture (Task 8.1) where a restarted container validates the real-world path.

5. **Given** partial backend failure during ingestion (transient `RedisConnectionException`, `EmbeddingApiException`, `EmbeddingRateLimitException`, or any activity exception that is not a `SemanticSearchDimensionMismatchException`), **when** the `IngestionWorkflow` encounters the outage during `ExtractContentActivity`, `GenerateEmbeddingActivity`, `IndexSyntacticActivity`, `IndexSemanticActivity`, or `IndexGraphActivity`, **then** the DAPR Workflow retry policy configured on the workflow (`maxNumberOfAttempts=5`, `firstRetryInterval=2s`, `backoffCoefficient=1.5`, `maxRetryInterval=5min`) handles the retry with exponential backoff (FR9, NFR22). After max retries are exhausted the workflow moves the memory unit to `failed` status with `FailureDetails` populated — the workflow does **not** fail permanently until those retries run out. A regression test **pins** the `WorkflowRetryPolicy` values so a future diff cannot silently lower them.

6. **Given** any degraded-path response (AC1, AC2, AC3), **when** the response is produced, **then** a structured log at `Warning` level is emitted via `[LoggerMessage]` with `tenantId`, `axis`, `reason` (e.g. `"RedisConnectionException"`, `"LOADING"`, `"GraphConnectionException"`), and `degradationType` (`"per-axis"` | `"total"`). This is reused from the existing `HybridSearchService` axis-failure warnings where possible; new log events are added only for the endpoint-level catches introduced in this story.

## Tasks / Subtasks

- [x] Task 1: Single-axis backend failure handling in `/api/search` (AC: #3 bullet 2)
    - [x] 1.1 In `src/Hexalith.Memories.Server/Program.cs` around the `axis=syntactic` branch (`SyntacticSearchService.SearchAsync` call — default path near line 1513 **and** the graph-scoped inner search around line 1449), wrap in `try { ... } catch (RedisConnectionException or RedisTimeoutException ex) { return ...503 BACKEND_UNAVAILABLE with Retry-After: 5 header; } catch (RedisServerException ex) when (IsTransientRedisError(ex)) { return ...same 503; }`. **Three catches, not one**: (a) `RedisConnectionException` — connection dropped; (b) `RedisTimeoutException` — command timed out on a live connection; (c) `RedisServerException` whose message contains `"LOADING"`, `"BUSY"`, or `"OOM"` (helper: `internal static bool IsTransientRedisError(RedisServerException ex) => ex.Message.Contains("LOADING", OrdinalIgnoreCase) || ex.Message.Contains("BUSY", OrdinalIgnoreCase) || ex.Message.Contains("OOM", OrdinalIgnoreCase);`). **Do NOT** catch `RedisServerException` with `"no such index"` / `"Unknown Index name"` — those are handled internally by `SyntacticSearchService` and produce empty results (not failures). The `when` filter must explicitly exclude those message patterns OR the helper must be pattern-specific.
    - [x] 1.2 Same treatment for the `axis=semantic` non-hybrid path around line 1472 (after the existing `EmbeddingApiException` / `EmbeddingRateLimitException` / `SemanticSearchDimensionMismatchException` catches). Order the new catches **after** those — the embedding-specific ones are more specific and give better error messages. The three-catch set from 1.1 (`RedisConnectionException`, `RedisTimeoutException`, transient `RedisServerException`) reaches this layer only when the Redis Vector index query itself fails (not the embedding generation). Same `Retry-After: 5` header.
    - [x] 1.3 **Do NOT introduce a helper method** for the repeated `Results.Json(new ErrorResponse(...), statusCode: 503)` block — inline it. Four call sites, identical shape, no benefit from a helper (anti-pattern #3 below). If the count grows past ~6, revisit and extract.
    - [x] 1.4 Use the existing `SearchEndpointErrorResponseFactory` pattern only if a new factory method reads cleanly — the existing factory creates 503 responses keyed to embedding failure modes. A generic `CreateBackendUnavailable(Exception ex)` would be reasonable; add it to the factory if adoption exceeds two call sites. Otherwise inline (see 1.3).
    - [x] 1.5 Add structured `[LoggerMessage]` warning on the catch (`LogBackendUnavailable(logger, axis, tenantId, reason)` — reuse the `HybridSearchService` log category if convenient, or declare a static partial on a lightweight `SearchEndpointDegradationLogger` if Program.cs cannot host partial methods). Minimum fields: `tenantId`, `axis`, `reason` (exception type name).

- [x] Task 2: Graph backend failure handling (AC: #2, AC: #3 bullet 3)
    - [x] 2.1 In `/api/search?axis=graph` branch (`Program.cs` ~line 1253), add the three-catch transient set (`RedisConnectionException`, `RedisTimeoutException`, transient `RedisServerException` via `IsTransientRedisError`) **before** the existing `catch (TimeoutException)` — the `TimeoutException` catch currently produces `504 GRAPH_TIMEOUT` which is semantically different (traversal query too slow on a live backend, not backend unreachable). The new catches return `Results.Json(new ErrorResponse("GRAPH_UNAVAILABLE", "Graph backend is unavailable.", "Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status."), statusCode: 503)` and add `Retry-After: 5` header. **Disambiguation:** `TimeoutException` (504) = live FalkorDB, query exceeded deadline → caller should reduce depth. `RedisTimeoutException` (503) = FalkorDB's multiplexer timed out at the command layer → caller should retry.
    - [x] 2.2 In `/api/tenants/{tenantId}/traverse` endpoint (`Program.cs` ~line 1525), wrap `traversalService.TraverseAsync(...)` in the same three-catch transient set (per 2.1). Same error body + `Retry-After: 5` header.
    - [x] 2.3 Graph-scoped inner search (`axis=syntactic`/`semantic` + `startNodeId`) around `Program.cs:1449` — the `graphScopedSearch.SearchAsync` with an inner search callback can fail from either the **FalkorDB traversal portion** or the **inner Redis Stack call**. **Distinguish the source**, do NOT merge:

        Refactor the call so the graph traversal and the inner search run in clearly-scoped sub-blocks (the current `GraphScopedSearch.SearchAsync` wraps both internally — see `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`). Two options:
        - **Option A (preferred, small):** add an outer `try/catch` on the `graphScopedSearch.SearchAsync` call that catches transient exceptions and inspects the `IConnectionMultiplexer` identity on the exception's `StackExchange.Redis` internals **if reliably possible**; where not, fall back to endpoint pessimism: since the first step of `GraphScopedSearch.SearchAsync` is always the FalkorDB traversal (it scopes the inner search to the graph result set), a transient exception thrown before the inner callback fires is **graph-origin** — return `GRAPH_UNAVAILABLE`. Once the inner callback is invoked, any transient exception is **redis-origin** — return `BACKEND_UNAVAILABLE`. Model this by recording a `bool innerSearchStarted = false` in the endpoint and flipping it to `true` at the top of the inner-search lambda; the catch inspects the flag to choose the error code.

        - **Option B (fallback):** keep a single catch returning a new `PARTIAL_STACK_UNAVAILABLE` code with message naming both FalkorDB and Redis. Less precise but avoids the flag-inspection pattern. **Only use if Option A proves fragile during implementation.**

        Same three-catch transient set (`RedisConnectionException`, `RedisTimeoutException`, transient `RedisServerException`). `Retry-After: 5` header on all 503 responses.

    - [x] 2.4 **Do NOT alter `GraphTraversalService` / `GraphScopedSearch`** internal catch blocks. They already translate `RedisServerException` ("graph not found") into empty results — that's correct (FR66-style semantics: missing data ≠ unavailable infrastructure). Changing them would move the boundary and regress 5-4 behavior.
    - [x] 2.5 Log with `[LoggerMessage]` on the graph 503 catches — minimum fields: `tenantId`, `startNodeId`, `reason`.

- [x] Task 3: Hybrid total-failure promotion (AC: #1, #3 bullet 1)
    - [x] 3.1 In `HybridSearchService.SearchAsync` (the internal `async Task<HybridSearchResult>` overload, `HybridSearchService.cs`), **after** the pagination block builds `unavailableAxisList` and before the final `return new HybridSearchResult { ... }`, compute the count of enabled axes that actually attempted execution. "Enabled and attempted" means: `enabledAxes.Contains(axis)` AND the axis was not skipped due to missing inputs (e.g., semantic skipped when `embeddingConfig is null`, graph skipped when `graphStartNodeId` is null). **Skipped-for-missing-inputs is NOT a failure** — it's a non-execution, by configuration. Do not count it toward total-failure.
    - [x] 3.2 Introduce a new boolean `AllEnabledAxesUnavailable` (property on `HybridSearchResult`, **nullable** `bool?` — `null` when the concept does not apply, e.g., no axes enabled; `true` when every enabled-and-attempted axis landed in `unavailableAxes`; `false` when at least one axis produced a result). Add to `HybridSearchResult.cs`, register in `MemoriesJsonContext` (already present — confirm). **Do not repurpose `Degraded`** — `Degraded=true` means "≥1 axis failed" (partial); `AllEnabledAxesUnavailable=true` means "every attempted axis failed" (total). The two are orthogonal semantic signals.
    - [x] 3.3 In `Program.cs`'s hybrid branch (~line 1343, immediately after `HybridSearchResult hybridResult = await hybridSearchService.SearchAsync(...)`), inspect `hybridResult.AllEnabledAxesUnavailable == true`. If true, bypass the enrichment/explain steps and return `Results.Json(new ErrorResponse("ALL_BACKENDS_UNAVAILABLE", "All enabled search backends are unavailable.", "Check infrastructure status (Redis Stack, FalkorDB). The service auto-recovers when backends reconnect; retry the request."), statusCode: 503)`. Include the `unavailableAxes` list in the response payload — extend `ErrorResponse` only if it does not already support arbitrary data (**check first**; if not, include the list in the `message` or `suggestion` field as a joined string, do NOT create a new error response record — anti-pattern #4). **Preferred:** keep `ErrorResponse` untouched and append `unavailableAxes` comma-list into the message, e.g. `"All enabled search backends are unavailable: syntactic, semantic, graph."`.
    - [x] 3.4 **Edge case:** if the caller passed `axes=` with only axes that get skipped (semantic without embedding config, graph without startNodeId), `enabledAndAttemptedAxes` is empty. Return the existing empty-OK result (not 503). Rationale: this is a caller misconfiguration, not a backend outage. Zero-attempted ≠ total-failure. Test this explicitly.
    - [x] 3.5 Update the `HybridSearchService` unit tests (`HybridSearchServiceTests.cs`) to assert `AllEnabledAxesUnavailable` across combinations: all axes fail → true; one axis fails → false; no axes fail → false; all axes skipped (not attempted) → null.

- [x] Task 4: Ingestion workflow retry-policy regression guard (AC: #5)
    - [x] 4.1 In `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` (or create a companion `IngestionWorkflowRetryPolicyTests.cs`), add a test that reads the **compile-time** constants `_mainRetryAttempts = 5` and `_compensationRetryAttempts = 3` from `IngestionWorkflow`. Use `typeof(IngestionWorkflow).GetField("_mainRetryAttempts", BindingFlags.NonPublic | BindingFlags.Static)` + `GetRawConstantValue()` and assert values. **Rationale:** prevents a "let's drop to 3 retries to speed up failure" diff from silently weakening NFR22.
    - [x] 4.2 Add a second test asserting `firstRetryInterval=2s`, `backoffCoefficient=1.5`, `maxRetryInterval=5min` on the main retry options. These are captured at workflow run-time; since `WorkflowTaskOptions` is built inside `RunAsync` you cannot read it without a workflow context. Pragmatic approach: **the test asserts on a helper method** — extract a `static WorkflowTaskOptions CreateMainRetry()` (mirroring the existing `CreateCompensationRetry()` at line 285) if one does not exist, then assert via reflection on the returned `RetryPolicy`. Only extract if doing so is a 3-line refactor; if it's more than that, fall back to an **XML-doc comment** on the workflow file pinning the values and a grep-based test that `2s`, `1.5`, `5min` strings appear in the workflow source. Prefer the refactor.
    - [x] 4.3 Add a test asserting that on a non-retryable condition (e.g., `SemanticSearchDimensionMismatchException`), the workflow **still** retries the full policy — DAPR does not support selective non-retry in the current SDK. Document this as known-accepted waste in Dev Notes (5 retries of a dimension mismatch will all fail identically).
    - [x] 4.4 Add a `[Fact(Skip = "Requires Aspire AppHost fixture")]` integration test scaffolding in `tests/Hexalith.Memories.IntegrationTests/Workflows/IngestionRetryIntegrationTests.cs`. Scenario: inject a transient `EmbeddingClient` mock that fails the first 3 calls with `EmbeddingApiException` then succeeds, start an ingestion, assert the workflow completes successfully and the memory unit is indexed. Deferred per 5-1/5-2/5-3/5-4/5-5 Aspire-fixture pattern.

        **Tracker reference:** this skipped test is tracked for unskip under **Epic 6 (Ingestion Pipeline Resilience & Operations)**, specifically Story 6.3 (Retry, Failure Visibility & Re-Ingestion) — the natural owner of end-to-end retry-under-failure validation. When Epic 6 lands an Aspire-fixture harness for ingestion, this file is the first candidate to unskip. Add a one-line comment in the `[Fact(Skip = ...)]` attribute: `"Requires Aspire AppHost fixture — unskip with Story 6.3 retry validation harness"`. Without the tracker anchor the test risks rotting silently.

- [x] Task 5: Auto-recovery test coverage (AC: #4)
    - [x] 5.1 In `HybridSearchServiceTests.cs` or `SearchRecoveryTests.cs`, add a test: configure the `semanticSearchFunc` mock to throw `RedisConnectionException` on first call, succeed on second call. Invoke `SearchAsync` twice (fresh invocation each time — the service is a singleton but each `SearchAsync` call is independent). Assert: first call returns `degraded=true, unavailableAxes=["semantic"]`; **second call returns `degraded=false, unavailableAxes=[]`** with populated semantic results. This validates no residual "degraded" state persists across requests.
    - [x] 5.2 Same pattern for `/api/search?axis=syntactic`: first call surfaces 503 `BACKEND_UNAVAILABLE`, second call returns 200 with results. Can be unit-level (mock `SyntacticSearchService`).
    - [x] 5.3 **(deferred to Phase 2)** — the originally planned "documentation-only test" asserting `GetDatabase()` call pattern has low ROI (architecture-style test with fuzzy value). Skipped by team consensus during 5.6 review. If a future refactor changes `IConnectionMultiplexer` lifetime from keyed singleton to scoped/transient, auto-reconnect breaks silently — at that point, add a **DI-lifetime assertion test** (`services.GetRequiredKeyedService<IConnectionMultiplexer>("redis")` invoked twice returns the same instance) in place of a call-pattern assertion. Do NOT implement in 5.6.
    - [x] 5.4 **LOADING / BUSY classification test** (risk-based addition per review): in `HybridSearchServiceTests` or `SearchEndpointDegradationTests`, assert that a Redis axis responding with `RedisServerException` whose message contains `"LOADING"` or `"BUSY"` is classified as **unavailable** (populated into `unavailableAxes` for hybrid; 503 `BACKEND_UNAVAILABLE` for single-axis). This protects the load-bearing distinction between `"no such index"` (empty result, valid) and `"LOADING"` (unavailable). Dev Notes call this out as the intended behavior; the test makes it enforceable.

- [x] Task 6: Error response consistency + log events (AC: #6)
    - [x] 6.1 Add `[LoggerMessage(EventId = 5601, Level = Warning, ...)]` partial methods for new endpoint-level degradation events. Suggested event IDs (pinned for dashboard/alert wiring later):
        - `5601` — single-axis backend unavailable (`axis`, `tenantId`, `reason`).
        - `5602` — graph backend unavailable (`tenantId`, `startNodeId`, `reason`).
        - `5603` — hybrid total failure (`tenantId`, `unavailableAxes`, `enabledAxes`).
    - [x] 6.2 Host the `[LoggerMessage]` partial methods on a new `internal static partial class SearchEndpointDegradationLog` in `src/Hexalith.Memories.Server/Search/` — Program.cs cannot host partial methods because it is a file-scoped top-level-statements file. One file, minimal; this is not a premature abstraction (avoid anti-pattern #3) because `[LoggerMessage]` requires a partial class host.
    - [x] 6.3 Register the new event IDs in the story's "Reference: Log Events" section below so the operations runbook can pick them up.

- [x] Task 7: Unit tests (AC: #1–#5)
    - [x] 7.1 `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` additions:
        - Semantic fails, syntactic + graph succeed → `degraded=true`, `unavailableAxes=["semantic"]`, `AllEnabledAxesUnavailable=false`, fused results from 2 axes. (AC1)
        - Graph fails, syntactic + semantic succeed → `degraded=true`, `unavailableAxes=["graph"]`, `AllEnabledAxesUnavailable=false`. (AC2 hybrid)
        - All three enabled, all fail → `degraded=true`, `AllEnabledAxesUnavailable=true`, `unavailableAxes` contains all three. (AC3 hybrid)
        - Two enabled, one skipped (no embeddingConfig, no startNodeId), one succeeds → `degraded=false`, `AllEnabledAxesUnavailable=false` (the skipped axis is not counted as failed). (AC3.4 edge)
        - All enabled, all skipped (no inputs) → `AllEnabledAxesUnavailable` is **null** (concept does not apply). (AC3.4 edge)
        - Recovery: `semanticSearchFunc` throws once then succeeds → second call has no failures. (AC4)
        - `CorpusStatisticsActor` throws `DaprException` mid-syntactic-flow → `unavailableAxes=["syntactic"]`, syntactic axis excluded from fusion, other axes unaffected. (Edge case added per review)
    - [x] 7.2 `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs` (new):
        - `axis=syntactic` with `RedisConnectionException` → 503 `BACKEND_UNAVAILABLE` + `Retry-After: 5` header. (AC3 bullet 2)
        - `axis=syntactic` with `RedisTimeoutException` → 503 `BACKEND_UNAVAILABLE` + `Retry-After: 5`. (AC3 bullet 2, review addition)
        - `axis=syntactic` with `RedisServerException("LOADING")` / `("BUSY")` / `("OOM")` → 503 `BACKEND_UNAVAILABLE` (parameterized `[Theory]`). **Regression guard** that `RedisServerException("no such index")` does NOT hit the 503 catch — it returns 200 with empty results (`SyntacticSearchService` internal behavior). (Task 5.4, review addition)
        - `axis=semantic` with `RedisConnectionException` (after embedding succeeds) → 503 `BACKEND_UNAVAILABLE` + `Retry-After: 5`.
        - `axis=semantic` with `EmbeddingApiException` → 503 `EMBEDDING_UNAVAILABLE` (existing factory, regression). Confirm ordering: embedding-specific catch first, transient-Redis catches after.
        - `axis=graph` with `RedisConnectionException` / `RedisTimeoutException` → 503 `GRAPH_UNAVAILABLE` + `Retry-After: 5` (parameterized). (AC2)
        - `axis=graph` with `TimeoutException` → 504 `GRAPH_TIMEOUT` (regression — distinguished from `RedisTimeoutException`). (Review addition: semantic disambiguation)
        - `axis=hybrid` with all axes throwing → 503 `ALL_BACKENDS_UNAVAILABLE` + `Retry-After: 5`, message lists all axes. (AC3 bullet 1)
        - `axis=hybrid` with two of three axes throwing → 200 OK with `degraded=true`, `AllEnabledAxesUnavailable=false`. (AC1/AC2)
        - `/api/tenants/{id}/traverse` with `RedisConnectionException` → 503 `GRAPH_UNAVAILABLE` + `Retry-After: 5`. (AC2)
        - Graph-scoped inner (`axis=syntactic` + `startNodeId`) with failure **before** inner-search lambda fires → 503 `GRAPH_UNAVAILABLE`. (Task 2.3 Option A branch)
        - Graph-scoped inner (`axis=syntactic` + `startNodeId`) with failure **inside** inner-search lambda → 503 `BACKEND_UNAVAILABLE`. (Task 2.3 Option A branch)
    - [x] 7.3 **Extend existing `IngestionWorkflowTests.cs`** (do NOT create a new test file — 3 tests don't justify it per review consensus) with retry-policy pin tests per Task 4: `_mainRetryAttempts == 5`, `_compensationRetryAttempts == 3`, retry policy fields (coefficient / intervals) pinned. (AC5)
    - [x] 7.4 `HybridSearchResultSerializationTests.cs` addition: round-trip `AllEnabledAxesUnavailable=true` and `=null` via `MemoriesJsonContext`. Mirror the pattern used in `TenantSummary` serialization tests.
    - [x] 7.5 **`ErrorResponse` body round-trip tests** (risk-based addition per review): for each new 503 code introduced by this story (`BACKEND_UNAVAILABLE`, `GRAPH_UNAVAILABLE`, `ALL_BACKENDS_UNAVAILABLE`), assert the response body deserializes to an `ErrorResponse` with the exact `code` / non-empty `message` / non-empty `suggestion` fields via `MemoriesJsonContext`. Catches accidental code-string drift (e.g., `BACKEND_UNAVAILABLE` → `INVALID_INPUT`) and ensures dashboards/alerts keyed on these code strings stay wired. File: `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs` or a separate `SearchEndpointErrorResponseTests.cs` — whichever reads cleaner.

- [x] Task 8: Integration tests (AC: #1–#5) — all `[Fact(Skip = "Requires Aspire AppHost fixture")]` per 5-1/5-2/5-3/5-4/5-5 deferral pattern
    - [x] 8.1 `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs` (new) with scenarios:
        - Stop the Redis Vector container → hybrid search still returns 200 with degraded result.
        - Stop the FalkorDB container → hybrid search degrades to syntactic+semantic; `/traverse` returns 503.
        - Stop all Redis Stack + FalkorDB → hybrid returns 503 `ALL_BACKENDS_UNAVAILABLE`.
        - Restart the stopped container → next request returns non-degraded result (auto-recovery).
        - Transient ingestion failure (inject 3 failures then allow success) → workflow completes without moving the unit to `failed`.

### Review Findings

- [x] \[Review\]\[Patch\] Narrow the new backend-unavailable catch scopes to the actual search operations; post-search enrichment failures are currently misclassified or can still bubble as 500 [src/Hexalith.Memories.Server/Program.cs:1131]
- [x] \[Review\]\[Patch\] `AllEnabledAxesUnavailable` currently treats actor/Dapr-side pre-unavailability as backend outage and can incorrectly promote hybrid requests to `ALL_BACKENDS_UNAVAILABLE` [src/Hexalith.Memories.Server/Program.cs:1225]
- [x] \[Review\]\[Patch\] The new degradation logs omit the `degradationType` field required by AC6, and the graph/total-failure events do not expose the full requested diagnostic fields [src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs:51]
- [x] \[Review\]\[Patch\] `SearchEndpointDegradationTests` does not exercise endpoint status routing or `Retry-After: 5` assertions, so the highest-risk delegate logic in `Program.cs` remains unverified [tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs:17]
- [x] \[Review\]\[Patch\] `IngestionWorkflowTests` still lacks the Task 4.3 regression test proving a non-retryable exception path still consumes the configured workflow retry policy [tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs:717]

## Dev Notes

### First Principles Framing

**What this story IS:** a **closure story** for the Gate 2 reliability contract (NFR18 / FR66). The architecture already established "partial backend failure → degraded service, not total failure" in Story 2.5 (`HybridSearchService` with `Degraded`/`UnavailableAxes`) and Story 1.6 (`IngestionWorkflow` with DAPR retry policies). 5.6 wires the last gaps: endpoint-level exception catches on non-hybrid paths + total-failure promotion + regression pins.

**What this story IS NOT:**

- NOT a circuit breaker. No Polly, no retry budgets, no bulkhead. DAPR Workflow retry handles the ingestion path; per-request exception translation is sufficient for the read path. Circuit breakers are Phase 2 if benchmarks show backend retry storms.
- NOT a backend health dashboard. `TenantMetricsService.GetIndexStatusAsync` (5.5) already exposes per-tenant backend health on demand. Aspire Dashboard shows service-level health via `/healthz`. A cross-tenant backend-health endpoint is Phase 2.
- NOT an outage-aware fallback strategy. We do not choose a different axis because another is down; we just surface the degradation and the caller decides.
- NOT an SLA-grade resilience story. Fuzz testing, chaos testing, and failure injection frameworks are Phase 2. MVP bar: deterministic behavior on single-backend outage, observable via logs and response fields.

**Mental model for the dev agent:**

- AC1 (hybrid partial fail) = **already implemented** in `HybridSearchService`; add tests + `AllEnabledAxesUnavailable` signal.
- AC2 (graph-specific endpoints) = add `catch (RedisConnectionException)` → 503 `GRAPH_UNAVAILABLE`.
- AC3 (total failure) = two paths: (a) hybrid promotes to 503 when all enabled axes unavailable (new `AllEnabledAxesUnavailable` field); (b) single-axis paths catch `RedisConnectionException` → 503 `BACKEND_UNAVAILABLE`.
- AC4 (auto-recovery) = **no code**; rely on `IConnectionMultiplexer`. Add tests to prove no residual state sticks.
- AC5 (ingestion retry) = **already implemented**; add regression pinning tests.
- AC6 (structured logging) = add `[LoggerMessage]` events 5601/5602/5603 for the new endpoint-level catches.

**If you find yourself adding a Polly circuit breaker, a custom health-poller, a retry-count tuning endpoint, a fallback axis selector, a "graceful-mode" config flag, or a new error response record — STOP. You're over-scoping.**

### Dependencies

- **Story 2.5 (Fusion Algorithm & Hybrid Search):** Required — provides `HybridSearchService`, `HybridSearchResult`, `FusionEngine`. This story extends `HybridSearchResult` with `AllEnabledAxesUnavailable`. Status: done.
- **Story 2.3 (Graph-scoped search):** Required — provides `GraphScopedSearch`, `GraphTraversalService`. This story wraps their calls in endpoint-level catches. Status: done.
- **Story 1.6 (Ingestion Workflow Orchestration):** Required — provides `IngestionWorkflow` with the retry policy we pin. Status: done.
- **Story 5.4 (Tenant Context Enforcement):** Required — provides `TenantStatusGuard.ValidateTenantActiveAsync` and `ToHttpResult` routing which every endpoint in this story reuses. Status: done.
- **Story 5.5 (Tenant Configuration & Listing):** No direct code dependency. 5.5 added `TenantMetricsService.GetIndexStatusAsync` which is **related but not invoked** by 5.6 (5.5 is operator-facing tenant health; 5.6 is request-path degradation). Do NOT reuse or call `TenantMetricsService` from the read path — adds ~3 backend calls to every search request (unacceptable hot-path cost).

### Architecture Compliance

- **NFR18 (partial backend failure → degraded):** Directly satisfied by AC1, AC2, AC3.
- **NFR19 (failed ingestion units never silently dropped):** Already satisfied by `IngestionWorkflow`'s `AttachFailureDetails` + `failed` status. AC5 pins the retry config that governs how long the unit stays in retry vs. moves to `failed`.
- **NFR22 (exponential backoff + jitter on transient failures):** DAPR Workflow retry policy provides the exponential backoff portion (`backoffCoefficient=1.5`); jitter is NOT provided by the current policy — this is a known-accepted MVP gap (see "Known MVP Limitations"). AC5 does not add jitter; it pins what exists.
- **FR66 (partial results + unavailable-axis indicator):** Satisfied by `HybridSearchResult.UnavailableAxes` (already exists) and the new `AllEnabledAxesUnavailable` signal.
- **D9 (`IGraphQueryBuilder`):** No change — graph query construction is unaffected; only the callers are wrapped in catches.
- **Reliability pillar (architecture.md:36):** "Partial backend failure → degraded service, not total failure" — this is the thesis statement 5.6 closes. Gate 2 sign-off requires all three AC3 paths (single-axis, graph-only, hybrid-total) to return a typed 503, not an unhandled 500 or a misleading 200.

### Existing Infrastructure to Reuse

| Component                                                   | Location                                              | Usage in This Story                                                                                                                                |
| ----------------------------------------------------------- | ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `HybridSearchService`                                       | `Server/Search/HybridSearchService.cs`                | Already handles per-axis failure; extend return value with `AllEnabledAxesUnavailable`; do NOT change core execution logic.                        |
| `HybridSearchResult`                                        | `Contracts/V1/HybridSearchResult.cs`                  | Add `bool? AllEnabledAxesUnavailable` property.                                                                                                    |
| `SearchEndpointErrorResponseFactory`                        | `Server/Search/SearchEndpointErrorResponseFactory.cs` | Existing patterns for 503 embedding errors. Extend with `CreateBackendUnavailable(Exception)` if call-site count justifies (≥2).                   |
| `ErrorResponse`                                             | `Contracts/V1/ErrorResponse.cs`                       | `(code, message, suggestion)` shape. Reuse as-is; do NOT extend the record.                                                                        |
| `IngestionWorkflow`                                         | `Server/Workflows/IngestionWorkflow.cs`               | Retry policy already configured at lines 22-23, 40-45, 285-290. Pin with tests. Extract `CreateMainRetry()` only if it's a 3-line refactor.        |
| `IConnectionMultiplexer` (keyed `"redis"` and `"falkordb"`) | DI in `Program.cs`                                    | Already configured with auto-reconnect semantics. Do NOT add manual reconnect logic.                                                               |
| `TenantStatusGuard.ToHttpResult`                            | `Server/Tenants/TenantStatusGuard.cs`                 | For tenant-validation errors; degradation errors are NOT tenant-status errors — use plain `Results.Json(new ErrorResponse(...), statusCode: 503)`. |
| `GraphTraversalService` / `GraphScopedSearch`               | `Server/Graph/`                                       | Already handle internal "graph not found" → empty result via `RedisServerException` catch. Do NOT modify.                                          |
| `MemoriesJsonContext`                                       | `Contracts/V1/MemoriesJsonContext.cs`                 | Register extended `HybridSearchResult` shape (new property is automatic on a `record` — double-check AOT metadata emits it).                       |

### Current Endpoint State (Baseline)

**Existing and reused as-is (verified during story authoring):**

- `/api/search?axis=hybrid` — `HybridSearchService` already returns `degraded`/`unavailableAxes`. Endpoint does NOT currently promote total-failure to 503; that's Task 3.
- `/api/search?axis=syntactic` — calls `SyntacticSearchService.SearchAsync`. Internal `RedisServerException` for "no such index" is handled (empty result). `RedisConnectionException` is **NOT** handled at the endpoint — currently surfaces as unhandled 500. That's the Task 1 gap.
- `/api/search?axis=semantic` — handles `EmbeddingApiException` / `EmbeddingRateLimitException` / `SemanticSearchDimensionMismatchException` at 503/500. `RedisConnectionException` on Redis Vector query is **NOT** handled. Task 1.
- `/api/search?axis=graph` — handles `TimeoutException` → 504. `RedisConnectionException` from FalkorDB is **NOT** handled. Task 2.1.
- `/api/search?axis=syntactic` or `semantic` + `startNodeId` (graph-scoped inner search) — handles embedding-specific errors + timeout. `RedisConnectionException` **NOT** handled. Task 2.3.
- `/api/tenants/{tenantId}/traverse` — does NOT handle any backend exceptions. Task 2.2.

**Modified in this story:**

- `/api/search?axis=hybrid`: adds 503 `ALL_BACKENDS_UNAVAILABLE` branch.
- `/api/search?axis=syntactic`: adds 503 `BACKEND_UNAVAILABLE` on `RedisConnectionException`.
- `/api/search?axis=semantic`: adds 503 `BACKEND_UNAVAILABLE` after existing embedding-specific catches.
- `/api/search?axis=graph`: adds 503 `GRAPH_UNAVAILABLE` before the existing `TimeoutException` → 504 catch.
- `/api/tenants/{tenantId}/traverse`: adds 503 `GRAPH_UNAVAILABLE`.

**New in this story:**

- `HybridSearchResult.AllEnabledAxesUnavailable` property.
- `SearchEndpointDegradationLog` partial class (log event IDs 5601, 5602, 5603).
- Possibly `SearchEndpointErrorResponseFactory.CreateBackendUnavailable(Exception)` (conditional — extract only if ≥2 callers after Task 1/2).

### Code Patterns

**Endpoint-level catch template (inline at call site):**

```csharp
try
{
    SearchResult result = await syntacticService.SearchAsync(query).ConfigureAwait(false);
    // enrichment + explain + return Results.Ok(result)
}
catch (RedisConnectionException ex)
{
    SearchEndpointDegradationLog.LogBackendUnavailable(logger, "syntactic", tenantId, ex.GetType().Name);
    return Results.Json(
        new ErrorResponse(
            "BACKEND_UNAVAILABLE",
            "Search backend is unavailable.",
            "Retry the request; the backend auto-recovers when Redis reconnects."),
        statusCode: 503);
}
```

**Ordering rule for `axis=semantic` catches:** `EmbeddingApiException` → `EmbeddingRateLimitException` → `SemanticSearchDimensionMismatchException` → `TimeoutException` → `RedisConnectionException`. The embedding-specific ones must be first (more specific). `RedisConnectionException` is last — it catches Redis Vector query failures after the embedding step succeeded.

**Graph endpoint catch template:**

```csharp
try
{
    TraversalResult result = await traversalService.TraverseAsync(tenantId, startNodeId, clampedDepth, caseId, parsedEdgeTypes, cancellationToken);
    return Results.Ok(result);
}
catch (RedisConnectionException ex)
{
    SearchEndpointDegradationLog.LogGraphUnavailable(logger, tenantId, startNodeId, ex.GetType().Name);
    return Results.Json(
        new ErrorResponse(
            "GRAPH_UNAVAILABLE",
            "Graph backend is unavailable.",
            "Retry the request; graph auto-recovers when FalkorDB reconnects. Check infrastructure status."),
        statusCode: 503);
}
```

### Auto-Recovery Mechanism (AC4)

`StackExchange.Redis.IConnectionMultiplexer` auto-reconnects on the next operation after a transient failure. This is the library's default behavior — documented in the StackExchange.Redis readme under "Reconnecting after a failure". Key properties:

1. The multiplexer is registered as a **keyed singleton** in `Program.cs` (`"redis"` and `"falkordb"`). It persists for the application lifetime.
2. On a connection drop, subsequent `GetDatabase()` / `ExecuteAsync()` calls re-establish the connection (internal reconnect with backoff).
3. The search services call `_redis.GetDatabase()` **per method invocation** (not cached in a field). Each search request pays the multiplexer's connection-check overhead but gets a fresh/reconnected `IDatabase`.
4. **No code changes required** in 5.6 to enable auto-recovery. The existing infrastructure already supports it. AC4 is satisfied by _verification_ (tests + doc) not _implementation_.

**Caveat:** if the multiplexer itself is disposed or replaced, auto-reconnect cannot work. Do NOT `Dispose()` the multiplexer in any request-path code. (It is disposed at app shutdown by DI.)

### Total-Failure Semantics (AC3)

**Why 503 and not 200-empty for hybrid total-failure?**

HTTP semantics matter: `200 OK { results: [], degraded: true }` tells the caller "we ran your query successfully and there are no matching results." That is a false signal when **no query ran at all** because every backend was down. The caller's retry logic, observability dashboards, and business logic should treat "no backends ran" as an outage, not a zero-result query.

**Why `AllEnabledAxesUnavailable` as `bool?` instead of a separate sentinel?**

Three states exist:

- `true` — at least one axis was attempted, all attempted axes failed → 503.
- `false` — at least one axis was attempted and succeeded → 200 (possibly with `degraded=true`).
- `null` — no axis was attempted (all skipped due to caller misconfiguration) → 200 with empty results, `degraded=false` (not 503, because the absence of input is not an outage).

A nullable bool encodes the "not-applicable" state cleanly; adding a separate enum would be abstraction-for-one-caller (anti-pattern #3).

### Ingestion Workflow Retry Policy (AC5) — Pinned Values

From `IngestionWorkflow.cs` (current, verified):

```csharp
private const int _compensationRetryAttempts = 3;  // line 22
private const int _mainRetryAttempts = 5;          // line 23

var retryOptions = new WorkflowTaskOptions(
    new WorkflowRetryPolicy(
        maxNumberOfAttempts: _mainRetryAttempts,    // 5
        firstRetryInterval: TimeSpan.FromSeconds(2),
        backoffCoefficient: 1.5,                    // line 44 (verify; pinned by test)
        maxRetryInterval: TimeSpan.FromMinutes(5)));
```

**Total max wait time** (worst case, all retries exhausted): `2 + 3 + 4.5 + 6.75 + 10.125 = 26.4s` per attempt window, plus activity execution time. For a memory unit hitting the full retry budget on a 30s-timeout activity, the unit can spend up to ~5 minutes in retry before moving to `failed`. This is within NFR19 bounds (no silent drops; the user sees `failed` state). **Do not lower** `_mainRetryAttempts` below 5 — the 5-retry budget was selected to cover transient embedding provider rate limits (retry-after 60s windows × 2-3 cycles).

**Compensation retries = 3** is independently configured for cleanup activities (`CleanupSyntacticActivity` etc.) where idempotency is guaranteed and fewer retries are acceptable.

### Error Codes

New error codes introduced by this story:

- `BACKEND_UNAVAILABLE` (503) — single-axis (syntactic/semantic) Redis Stack outage on the read path.
- `GRAPH_UNAVAILABLE` (503) — FalkorDB outage on graph read endpoints.
- `ALL_BACKENDS_UNAVAILABLE` (503) — hybrid search when every enabled axis failed.
- `PARTIAL_STACK_UNAVAILABLE` (503) — **fallback only** (Task 2.3 Option B) — graph-scoped inner search when the failing multiplexer cannot be cleanly identified. Prefer Option A (flag-based disambiguation to `GRAPH_UNAVAILABLE` / `BACKEND_UNAVAILABLE`); introduce this code only if Option A proves fragile during implementation. If introduced, register in `ErrorResponse` unit tests (Task 7.5).

**All 503 responses from this story MUST carry a `Retry-After: 5` header** to mitigate reconnect-storm by giving conformant clients a backoff signal. Use `Results.Json(...)` combined with middleware or a result-writer to set the header (or the simpler `httpContext.Response.Headers.Append("Retry-After", "5")` call immediately before returning the `IResult`).

Reused from 5-1 through 5-5:

- `DAPR_UNAVAILABLE` (503) — for any DAPR sidecar / state-store / actor-proxy failure. No overlap; DAPR outage is distinct from backend outage.
- `GRAPH_TIMEOUT` (504) — query too slow (graph too dense). **Not** a backend outage; keep this code.
- `EMBEDDING_UNAVAILABLE` (503) / `EMBEDDING_RATE_LIMITED` (503) — from `SearchEndpointErrorResponseFactory`. Kept as more-specific alternatives to `BACKEND_UNAVAILABLE` for the embedding layer.

Unchanged: `INVALID_INPUT` (400), `TENANT_*` codes from 5-4/5-5.

### Anti-Patterns to Avoid

1. **Do NOT add a _request-layer_ circuit breaker (Polly or similar).** `StackExchange.Redis`'s internal short-circuit (after N consecutive failures the multiplexer stops attempting commands until reconnect) **IS** a circuit breaker — we just don't control it from the application layer, and that is deliberate. Adding Polly creates a second retry/breaker surface that races the DAPR Workflow retry policy and the multiplexer's internal one — triple retries waste request budget and amplify thundering-herd under sustained outage.
2. **Do NOT poll backend health on a timer.** `IConnectionMultiplexer.IsConnected` is available but reading it preemptively creates a time-of-check-to-time-of-use gap that misleads callers. Handle failure at call time; let the exception be the signal.
3. **Do NOT create a `BackendAvailabilityService`.** `TenantMetricsService.GetIndexStatusAsync` (from 5.5) already exposes per-tenant health on demand for the **operator-facing** path. Inventing a second service for the **request-time** path is speculative complexity; inline catches are the right granularity for MVP.
4. **Do NOT extend `ErrorResponse`.** `(code, message, suggestion)` is the pinned shape for every error in this codebase. If `unavailableAxes` needs to surface on the 503 body, join it into `message` (`"All enabled search backends are unavailable: syntactic, semantic, graph."`). A structured list on the error body requires a contract change rippling across CLI (Epic 7) and is not worth the churn pre-Gate-2.
5. **Do NOT retry inside the endpoint delegate.** The read-path endpoint is request-scoped. Retries belong in the DAPR Workflow (ingestion) or in the caller's client (CLI, Epic 7). Retries inside the request handler double-charge latency budget without adding resilience.
6. **Do NOT catch `Exception` broadly.** Catch specific types: `RedisConnectionException`, `RedisServerException` (only for specific subtypes — "LOADING", "BUSY"; do NOT catch "no such index" which is a valid empty signal). Broad `catch (Exception)` swallows `OperationCanceledException` and breaks cancellation.
7. **Do NOT catch `OperationCanceledException`.** Always let cancellation propagate — the caller knows.
8. **Do NOT introduce a fallback axis selector.** If FalkorDB is down, `axis=graph` returns 503 — do NOT silently substitute `axis=syntactic`. The caller's intent was graph traversal; substituting ranks ≠ traversal is silently wrong.
9. **Do NOT modify `GraphScopedSearch.SearchAsync` internal catches.** They correctly translate "graph not found" → empty result (5-1 provisioning behavior). Adding 503-raising logic there leaks HTTP semantics into a domain service.
10. **Do NOT repurpose `Degraded` to mean "total failure."** It means "≥1 axis failed, ≥1 axis succeeded." Total failure gets its own signal (`AllEnabledAxesUnavailable=true`).
11. **Do NOT lower `_mainRetryAttempts`** (AC5 pin test enforces this).
12. **Do NOT add jitter to the retry policy** as part of this story. `WorkflowRetryPolicy` does not expose a jitter parameter in the current DAPR SDK; emulating jitter via activity-level sleep is a workaround best scoped to Epic 6 (Ingestion Pipeline Resilience).
13. **Do NOT add a configuration flag** to toggle degraded mode on/off. It's always on — partial results is the design, not a feature.

### Known MVP Limitations

- **No jitter on retry policy (thundering-herd risk):** DAPR Workflow retry policy does not expose a jitter parameter. Under Redis-restart or FalkorDB-restart conditions with many in-flight workflows, every retry across all tenants lands on the same lockstep cadence (2s → 3s → 4.5s → 6.75s → 10.125s). **Explicit risk profile:** a 100-tenant deployment with N active workflows each hitting `firstRetryInterval=2s` simultaneously creates a spike of ~100·N commands at T+2s post-recovery, potentially re-crashing the just-recovered backend. Mitigated partially by: per-tenant rate limiting (5.5), 5-minute cap on retry interval, and the multiplexer's internal short-circuit. **Not** mitigated at the workflow retry layer. Full jitter is Epic 6 (Ingestion Pipeline Resilience).
- **Post-recovery corpus-stats skew (FR22-adjacent silent failure):** after a Redis restart, the `CorpusStatisticsActor` state may be flushed or stale. The first N hybrid-search requests post-recovery will compute BM25 normalization against incorrect `DocumentCount` / `AverageDocumentLength` values, producing subtly wrong fusion rankings. The response will NOT carry `degraded=true` because the backend **is** reachable — the data is merely stale. This is an architecture-level silent-failure mode (see architecture.md silent-failure-modes table, row "Fusion score distribution skew"). Out of scope for 5.6; add to operator runbook: "after Redis restart, expect 5-10 min of ranking drift while corpus stats rehydrate."
- **Log storm on sustained outage:** `[LoggerMessage]` events 5601/5602/5603 fire on every degraded request. During a 30-minute outage with a 100 req/s workload, expect ~180k log events — this can saturate downstream log pipelines (Aspire Dashboard, OTEL exporters) and create back-pressure that further slows requests. Out of scope for 5.6; candidate mitigation for Epic 8 (Observability): per-tenant log sampling OR a `LoggerExtensions` wrapper that dedupes identical events within a 60s window. Document in the ops runbook: "sustained 503s → check log pipeline back-pressure before assuming application-layer fault."
- **No backend-specific retry policies:** all activities use the same `_mainRetryAttempts=5`. Embedding API rate limits may need a longer budget (60s+ retry-after windows); indexing backends need a shorter budget. Deferred to Epic 6 per-activity tuning.
- **No total-failure escalation path:** a 503 `ALL_BACKENDS_UNAVAILABLE` is returned per-request; there is no operator alert, no automatic traffic shedding, no dashboard. Operator observability is via logs (event IDs 5601/5602/5603) and `/healthz` only.
- **`GraphScopedSearch` inner-search path lumps two failure domains:** when `axis=syntactic` + `startNodeId`, a failure in FalkorDB (graph traversal) and a failure in Redis Stack (syntactic filter) both look like `RedisConnectionException` — the current catch returns `BACKEND_UNAVAILABLE` generically. Distinguishing requires inspecting the multiplexer on the exception (fragile). Acceptable; log the exception fully so ops can tell.
- **No circuit-breaker "short-circuit" after N consecutive failures:** every request retries the multiplexer. In a sustained outage this creates per-request latency (connection timeout \* axis count). StackExchange.Redis internally does short-circuit `IsConnected` checks after a few failures, so the actual cost is low but non-zero. Full circuit-breaker is Phase 2.
- **Auto-recovery is not observable in-response:** after an outage, the next successful request returns `degraded=false` but there is no "backend X reconnected" signal. Ops see recovery via the absence of 5601/5602/5603 events. Deferred: an availability-change event channel (Epic 8).
- **Test coverage for network partition is simulated:** mocks throw `RedisConnectionException` to simulate backend outage. True network partition testing requires container-level network manipulation (toxiproxy, chaos mesh) — deferred to Phase 2 resilience validation.
- **Legacy memory units with stale metadata during partial failure:** if the graph is unavailable, a hybrid result has no graph-score component. `FusedScoredResult.GraphScore=null` is the signal. Callers must not require a non-null graph score; the explain metadata (5.5) indicates which axes contributed.

### Edge Cases

- **Backend returns `LOADING` / `BUSY` error:** treated as **unavailable** for this story (Redis is warming up, not serving). Internally `HybridSearchService.ExecuteAxisAsync` already catches as generic `Exception` (line 323) and marks the axis unavailable. Endpoint-level catches do not specifically distinguish LOADING from connection failure; both surface as 503 `BACKEND_UNAVAILABLE`. (Contrast with 5.5 `IndexHealth.Degraded` — 5.5 is operator observability, not request-time failure.)
- **`CorpusStatisticsActor` failure during hybrid search:** already handled at `HybridSearchService.cs:163-177` (catch wraps `GetStatisticsAsync`, marks `syntactic` unavailable, nulls the syntactic result). Add explicit test coverage (Task 7.1): mock actor throws `DaprException` → expect `unavailableAxes=["syntactic"]`, fused results exclude syntactic axis, `Degraded=true`. Do NOT add a new catch — the existing one is correct.
- **Caller requests `axes=semantic` but provides no embedding config (tenant config actor unreachable):** `preUnavailableAxes.Add("semantic")` already handled by Program.cs line 1338. Result: 200 OK with `degraded=true, unavailableAxes=["semantic"]`, results from other axes. NOT treated as total failure.
- **Caller requests `axes=graph` only and FalkorDB is down:** hybrid path — `AllEnabledAxesUnavailable=true`, return 503 `ALL_BACKENDS_UNAVAILABLE` with message "...: graph". Alternative: caller should have used `axis=graph` which returns 503 `GRAPH_UNAVAILABLE` (more specific). Both are correct; the 503 is the key signal.
- **Transient failure during the enrichment step** (`EnrichResultWithCaseAttributionAsync`, `EnrichResultWithAnnotationCountsAsync`): currently propagates. In this story's scope, do NOT add catches to enrichment — enrichment failures reflect metadata lookup issues, not axis availability. An enrichment failure producing a 500 is a separate concern (track under Epic 8 observability).
- **Partial results with exactly one remaining axis:** `degraded=true, unavailableAxes=["semantic","graph"]`, results from syntactic only. Valid. `AllEnabledAxesUnavailable=false` (syntactic attempted and succeeded).
- **All axes skipped for missing inputs (e.g., `axes=semantic,graph` but no embedding config + no startNodeId):** `enabledAxes={semantic,graph}`, both skipped pre-execution. No axis attempted. `AllEnabledAxesUnavailable=null`. Response: 200 OK with empty results and `degraded=false`. This is a caller misconfiguration, NOT an outage.
- **Cancellation mid-flight:** `OperationCanceledException` propagates through all catches. Response: standard ASP.NET 499/client-aborted. No degradation logging.
- **Multiplexer reports `IsConnected=false` between requests:** multiplexer's next `GetDatabase()` call triggers reconnect. If the reconnect succeeds, the request proceeds normally. If it fails, it raises `RedisConnectionException` which the endpoint catches. No special handling needed.
- **Graph endpoint with invalid startNodeId after graph unavailability:** graph endpoint returns 503 `GRAPH_UNAVAILABLE` first (connection check); input validation on `startNodeId` only reaches the query when connected. This ordering is correct — infrastructure failure takes precedence over input validity.
- **DAPR sidecar failure during ingestion workflow:** handled by existing `DaprException` catches; outside this story's scope. If the sidecar is down the workflow is inert; DAPR auto-resumes workflows on sidecar recovery.

### Previous Story Learnings (from 5-4, 5-5)

- `TenantStatusGuard.ToHttpResult` is the tenant-validation router; **do NOT reuse it for backend errors**. Backend unavailability is a different concern — use plain `Results.Json(new ErrorResponse(...), statusCode: 503)`. (From 5-4/5-5 precedent: separate concerns keep error semantics clean.)
- `[LoggerMessage]` event IDs are pinned for dashboard stability; 5501 was claimed by 5.5 (tenant operational log). This story pins 5601/5602/5603. Future stories claim 5701+.
- DAPR actor state-store bypass (5.5 Amendment N) is a Phase 2 optimization; this story does NOT touch actor state.
- The `CapturingLogger<TCategory>` test fixture (see `TenantContextEnforcementTests`) is the established pattern for asserting `[LoggerMessage]` calls. Reuse for `SearchEndpointDegradationLog` assertions.
- Test-fixture factory pattern (`IndexInputFactory` in 5.5) is the template if 5.6 needs a `HybridSearchResultFactory`. Unlikely — assertions are on field values, not fixture construction.
- Aspire-fixture integration tests use `[Fact(Skip = "Requires Aspire AppHost fixture")]`; this is the established pattern (5-1 through 5-5). Follow it for Task 8. Do not block on unskipping.
- Pre-existing test failures in `SaveDedupKeyActivityTests` (2 tests) are documented on baseline `b33cd71`; ignore them when assessing 5.6 regression bar.
- Run full test suite before and after — 5-5 left ~1087+ tests passing (5-4 baseline 1051 + ~36 from 5.5); keep that bar. New expected count: ~1087 + ~20 from 5.6.

### Git Intelligence

Recent commits show:

- `b33cd71` — "Add DAPR configuration and tenant mismatch monitoring." Related to 5-4 `TenantMismatchMonitor`; unrelated to 5-6 degradation paths. Do NOT accidentally repurpose `TenantMismatchMonitor` as a "generic degradation counter."
- `9cd3b97` — `TenantStatusGuard.ToHttpResult` helper (5-4). See "Previous Story Learnings" — do NOT reuse for backend errors.
- `912a3ab` — serialization tests for tenant isolation results. Mirror this test pattern for `HybridSearchResultSerializationTests` addition (Task 7.4).
- `5bb2655` / `acbcffe` — unit tests for tenant provisioning/deletion activities (5-1/5-2). Mirror structure for `IngestionWorkflowRetryPolicyTests`.
- `e5b8062` — confidence promotion + gap detection (Epic 4). Unrelated.

### Project Structure Notes

**New files:**

- `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs` (static partial class with `[LoggerMessage]` methods for events 5601/5602/5603; also hosts `IsTransientRedisError` helper per Task 1.1)
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs`
- `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs` (`[Fact(Skip)]`)
- `tests/Hexalith.Memories.IntegrationTests/Workflows/IngestionRetryIntegrationTests.cs` (`[Fact(Skip)]` per Task 4.4)

**Modified files:**

- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs` — add `bool? AllEnabledAxesUnavailable` property.
- `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs` — verify AOT metadata picks up the new property (records auto-register, but run an AOT build to confirm).
- `src/Hexalith.Memories.Server/Search/HybridSearchService.cs` — compute and populate `AllEnabledAxesUnavailable` in the final `HybridSearchResult` construction.
- `src/Hexalith.Memories.Server/Program.cs` — add catches to `/api/search?axis=syntactic`, `axis=semantic` (non-hybrid and graph-scoped-inner), `axis=graph`, `axis=hybrid` (total-failure promotion), and `/api/tenants/{tenantId}/traverse`. Wire up log events via `SearchEndpointDegradationLog`.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — **optional** extraction of `CreateMainRetry()` helper to make Task 4.2 assertion straightforward (only if it's a 3-line refactor per Task 4.2 guidance).
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — add retry-policy pin tests (Task 7.3; no new file per review consensus).
- `src/Hexalith.Memories.Server/Search/SearchEndpointErrorResponseFactory.cs` — **optional** `CreateBackendUnavailable(Exception)` helper (only if ≥2 call sites materialize per Task 1.4).
- `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` — add AC1/AC2/AC3/AC4 tests per Task 7.1.
- `tests/Hexalith.Memories.Server.Tests/Contracts/HybridSearchResultSerializationTests.cs` — serialization round-trip for new property.

### Definition of Done

1. All unit tests (Task 7) pass — **at least 27 new tests** covering: `HybridSearchService` degradation combinations incl. `CorpusStatisticsActor` failure (7 — Task 7.1), endpoint-level 503 routing incl. LOADING/BUSY/OOM parameterized theory, `RedisTimeoutException`, graph-scoped inner disambiguation (12 — Task 7.2), `IngestionWorkflow` retry policy pins (3 — Task 7.3), `HybridSearchResult` serialization (2 — Task 7.4), `ErrorResponse` body round-trip per new 503 code (3 — Task 7.5). Auto-recovery tests (Tasks 5.1–5.2) fold into Task 7.1 / 7.2 counts where they naturally sit.
2. `/api/search?axis=syntactic` with `RedisConnectionException` returns 503 `BACKEND_UNAVAILABLE` (not unhandled 500).
3. `/api/search?axis=graph` and `/api/tenants/{tenantId}/traverse` with `RedisConnectionException` return 503 `GRAPH_UNAVAILABLE`.
4. `/api/search?axis=hybrid` with all enabled axes failing returns 503 `ALL_BACKENDS_UNAVAILABLE`; with ≥1 axis succeeding returns 200 OK with `degraded=true`.
5. `IngestionWorkflow` retry policy values (`_mainRetryAttempts=5`, intervals) are pinned by regression tests.
6. Auto-recovery test passes — second request after transient `RedisConnectionException` succeeds without residual degradation.
7. `[LoggerMessage]` events 5601/5602/5603 are emitted on the new 503 paths with `tenantId`, `axis`/`startNodeId`, `reason`.
8. Full test suite passes at ≥ baseline count (no new regressions).
9. Integration tests (Task 8) all `[Fact(Skip)]` — acceptable per established deferral pattern. **Gate 2 final sign-off explicitly depends on the Aspire integration-test harness landing and Task 8.1 + Task 4.4 scenarios being unskipped.** Track this as an epic-5 retrospective exit item: Epic 5 retrospective MUST capture the deferred-fixture debt so it is re-surfaced before Gate 2 is declared closed. Five sequential stories (5-1 through 5-5) have deferred Aspire fixtures; 5.6 is the sixth and final Epic-5 deferral. Stop the accumulation here.
10. Every AC has at least one direct test; every anti-pattern has an explicit test or a Dev Notes justification.
11. Every 503 response from this story (Tasks 1–3) carries `Retry-After: 5`. Verified by assertions in Task 7.2.
12. Status set to `review` before handoff to `code-review` workflow.

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 5, Story 5.6 (FR66)]
- [Source: _bmad-output/planning-artifacts/prd.md — FR9, FR66, NFR17, NFR18, NFR19, NFR22]
- [Source: _bmad-output/planning-artifacts/architecture.md — Reliability pillar, Partial backend failure section, Silent failure modes table, WorkflowRetryPolicy guidance]
- [Source: \_bmad-output/implementation-artifacts/5-5-tenant-configuration-and-listing.md — `TenantMetricsService.GetIndexStatusAsync` for operator-facing backend health, `[LoggerMessage]` event ID discipline, deferral pattern for integration tests]
- [Source: _bmad-output/implementation-artifacts/5-4-tenant-context-enforcement.md — `TenantStatusGuard.ToHttpResult` routing; error-response shape (`code`, `message`, `suggestion`)]
- [Source: _bmad-output/implementation-artifacts/2-5-fusion-algorithm-and-hybrid-search.md — `HybridSearchService` per-axis degradation contract]
- [Source: src/Hexalith.Memories.Server/Search/HybridSearchService.cs — already handles per-axis failures, lines 89-209; extend return with `AllEnabledAxesUnavailable`]
- [Source: src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs — record to extend]
- [Source: src/Hexalith.Memories.Server/Program.cs — endpoint catches to add: lines 1230-1274 (axis=graph), 1343-1368 (axis=hybrid), 1397-1469 (graph-scoped inner), 1472-1511 (axis=semantic), 1513 (axis=syntactic default), 1525-1580 (traverse)]
- [Source: src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs — retry policy at lines 22-23, 40-45, 285-290 — pin via regression tests]
- [Source: src/Hexalith.Memories.Server/Search/SearchEndpointErrorResponseFactory.cs — existing 503 helpers for embedding failures]
- [Source: src/Hexalith.Memories.Server/HealthChecks/DaprStateStoreHealthCheck.cs — reference for connection-probe pattern (do NOT reuse at request time)]
- [Source: src/Hexalith.Memories.Server/Graph/GraphTraversalService.cs — internal `RedisServerException` → empty result (keep as-is)]

### Reference: Log Events

| EventId | Level   | Message                                                                                                 | Fields                                       | Emitted When                                                                                    |
| ------- | ------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| 5601    | Warning | `"Search backend {Axis} unavailable for tenant {TenantId}: {Reason}"`                                   | `axis`, `tenantId`, `reason`                 | `axis=syntactic` / `axis=semantic` / graph-scoped inner path catches `RedisConnectionException` |
| 5602    | Warning | `"Graph backend unavailable for tenant {TenantId}, startNode={StartNodeId}: {Reason}"`                  | `tenantId`, `startNodeId`, `reason`          | `axis=graph` or `/traverse` catches `RedisConnectionException`                                  |
| 5603    | Warning | `"Hybrid search total failure for tenant {TenantId}: all enabled axes unavailable ({UnavailableAxes})"` | `tenantId`, `unavailableAxes`, `enabledAxes` | Hybrid path where `AllEnabledAxesUnavailable == true`                                           |

Existing (retained, emitted by `HybridSearchService` per-axis catches):

- `HybridSearchService.LogAxisExecutionFailure` — Warning, "Axis {AxisName} failed during execution — marking as unavailable."
- `HybridSearchService.LogSemanticSkipped` / `LogGraphSkipped` — Warning, axis skipped due to missing input.
- `HybridSearchService.LogCorpusStatsFailure` — Warning, corpus stats actor failure.
- `HybridSearchService.LogAxisDroppedFromFusion` — Warning, axis returned stale/unenrichable hits.

## Dev Agent Record

### Agent Model Used

claude-opus-4-6 (1M context)

### Debug Log References

- Build: `dotnet build src/Hexalith.Memories.Server` → succeeded after reordering `RedisTimeoutException` catches before `TimeoutException` in the graph-scoped inner search paths (RedisTimeoutException derives from TimeoutException — more specific must come first).
- Full unit test run: `dotnet test tests/Hexalith.Memories.Server.Tests` → 811 passed, 2 pre-existing failures in `SaveDedupKeyActivityTests` (documented baseline in Dev Notes).
- Contracts tests: `dotnet test tests/Hexalith.Memories.Contracts.Tests` → 274/274 passing.
- `Hexalith.Memories.IntegrationTests` does not build on baseline `30f86c2` due to pre-existing Aspire/CommunityToolkit.Dapr package conflict in AppHost (CS0311 on `IDaprSidecarResource`). Confirmed pre-existing by stashing 5.6 changes and reproducing the same error. New `DegradationIntegrationTests.cs` and `IngestionRetryIntegrationTests.cs` scaffolds are `[Fact(Skip)]` per deferral pattern; they do not need to execute and will be unskipped under Story 6.3.

### Completion Notes List

- **AC1 — Hybrid partial failure (per-axis degraded 200):** already implemented in `HybridSearchService` prior to this story; added regression tests covering semantic-fails / graph-fails combinations with `AllEnabledAxesUnavailable=false` assertion.
- **AC2 — Graph backend failure (503 GRAPH_UNAVAILABLE):** added three-catch transient set (`RedisConnectionException`, `RedisTimeoutException`, transient `RedisServerException`) on `axis=graph`, `/api/tenants/{tenantId}/traverse`, and the graph-scoped inner-search paths in Program.cs. Disambiguation note: `TimeoutException` (504 GRAPH_TIMEOUT) is retained and semantically distinct from `RedisTimeoutException` (503 GRAPH_UNAVAILABLE) — comments in code explain. Catch order inverted (Redis-specific before base `TimeoutException`) to avoid CS0160.
- **AC3 — Total failure (503 ALL_BACKENDS_UNAVAILABLE):** added `bool? HybridSearchResult.AllEnabledAxesUnavailable`. Computed in `HybridSearchService` from an `attemptedAxes` set that tracks which enabled axes actually ran (skipped-for-missing-inputs is NOT counted as an attempt). Endpoint checks `AllEnabledAxesUnavailable == true` immediately after the service call and bypasses enrichment to return 503 with `unavailableAxes` joined into the message per Dev Notes anti-pattern #4 (do not extend `ErrorResponse`).
- **AC3 — Single-axis 503 BACKEND_UNAVAILABLE:** added three-catch transient set on the non-hybrid syntactic (default) and semantic branches in Program.cs. Embedding-specific catches (`EmbeddingApiException` etc.) stay as they were and are still first (more specific).
- **AC3 — Graph-scoped inner-search disambiguation (Task 2.3 Option A):** chose flag-based approach — `innerSearchStarted = false` flipped to `true` at the top of the inner-search lambda. Catches inspect the flag: pre-lambda failure = graph-origin → `GRAPH_UNAVAILABLE`; post-lambda failure = redis-origin → `BACKEND_UNAVAILABLE`. No dependency on inspecting multiplexer internals on the exception. Option B (`PARTIAL_STACK_UNAVAILABLE`) not needed.
- **AC4 — Auto-recovery:** no code added (confirmed `IConnectionMultiplexer` auto-reconnect via keyed singleton per Dev Notes). Added two tests: (a) transient-then-success on `HybridSearchService` proves no residual degradation state sticks; (b) a test that a `RedisConnectionException` then success on repeat invocations yields `Degraded=false` on the recovered call.
- **AC5 — Ingestion retry policy pin:** extracted `CreateMainRetry()` helper mirroring existing `CreateCompensationRetry()`. Corrected `backoffCoefficient` from 2.0 → 1.5 to match AC5 pinned values (Dev Notes worst-case math — `2 + 3 + 4.5 + 6.75 + 10.125 ≈ 26.4 s` — confirms 1.5 is the intended value; the 2.0 value in baseline was a drift). Added four pin tests reading constants via reflection and asserting `WorkflowRetryPolicy` fields. Pin test for compensation policy (3 attempts / 1 s / 2.0 / 30 s) added for symmetry.
- **AC6 — Structured logging:** new `SearchEndpointDegradationLog` partial class hosts `[LoggerMessage]` event IDs 5601, 5602, 5603 plus the `IsTransientRedisError` helper (per Dev Notes Project Structure — one file, minimal, since `[LoggerMessage]` requires a partial class host and Program.cs cannot host them).
- **Retry-After: 5 header:** all 503 responses from Tasks 1–3 emit `Retry-After: 5` via the `AppendRetryAfter(HttpContext)` helper. Header append is guarded by `HasStarted` and duplicate-key checks so retries can be set once per request.
- **Deferred integration tests (Task 8 + Task 4.4):** `DegradationIntegrationTests.cs` (5 `[Fact(Skip)]` scenarios) and `IngestionRetryIntegrationTests.cs` (1 `[Fact(Skip)]` scenario). Skip reason references Story 6.3 as the unskip tracker per Task 4.4 guidance — stops accumulation of deferred-fixture debt. Epic 5 retrospective should capture this as a Gate 2 exit item.
- **Pre-existing baseline failures:** `SaveDedupKeyActivityTests` (2 tests) were already failing on `b33cd71` per story Dev Notes — not introduced by 5.6 and out of scope. Pre-existing `Hexalith.Memories.AppHost` compile error (CS0311 on `IDaprSidecarResource`) also predates 5.6 (confirmed by stash-and-rebuild); integration tests could not be built either before or after this story.
- **Test count:** Server.Tests 811 passing → was ~788 before 5.6 (= 813 total − 2 baseline failures); 5.6 adds ~23 net new tests (HybridSearchService 7, SearchEndpointDegradationTests 13, IngestionWorkflow 4). Contracts.Tests 274 passing → was 272 before (+ 2 from `HybridSearchResultSerializationTests`).

### File List

**Modified**

- `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs` — added `bool? AllEnabledAxesUnavailable` property with `[JsonIgnore(Condition = WhenWritingNull)]`.
- `src/Hexalith.Memories.Server/Program.cs` — wired `ILogger<Program>` and `HttpContext` into `/api/search` and `/api/tenants/{tenantId}/traverse`; added transient-Redis catches (three-catch set) on axis=graph, axis=semantic (non-hybrid and graph-scoped-inner), axis=syntactic (default and graph-scoped-inner), and `/traverse`; added hybrid total-failure 503 promotion; added `BuildBackendUnavailableResponse`, `BuildGraphUnavailableResponse`, `AppendRetryAfter` static helpers.
- `src/Hexalith.Memories.Server/Search/HybridSearchService.cs` — added `attemptedAxes` tracking, computed `AllEnabledAxesUnavailable` (`null` / `true` / `false`), included pre-unavailable axes as attempted, populated the new property in the returned `HybridSearchResult`.
- `src/Hexalith.Memories.Server/Workflows/IngestionWorkflow.cs` — extracted `CreateMainRetry()` internal helper; corrected `backoffCoefficient` to 1.5 to match AC5-pinned value; promoted `CreateCompensationRetry()` from private to internal for testability.
- `tests/Hexalith.Memories.Server.Tests/Workflows/IngestionWorkflowTests.cs` — added four retry-policy pin tests (main & compensation, constants & field values).
- `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs` — added seven tests covering `AllEnabledAxesUnavailable` semantics, auto-recovery across two invocations, and LOADING/BUSY/OOM classification.
- `tests/Hexalith.Memories.Contracts.Tests/V1/HybridSearchResultSerializationTests.cs` — added two round-trip tests for `AllEnabledAxesUnavailable=true` and `=null` (omit-when-null assertion).

**New**

- `src/Hexalith.Memories.Server/Search/SearchEndpointDegradationLog.cs` — `[LoggerMessage]` event hosts (5601/5602/5603) and `IsTransientRedisError` classifier.
- `tests/Hexalith.Memories.Server.Tests/Endpoints/SearchEndpointDegradationTests.cs` — 13 tests covering `IsTransientRedisError` (LOADING/BUSY/OOM vs. missing-index vs. other), log-event dispatch (event IDs, level), and round-trip assertions for the three new 503 error codes.
- `tests/Hexalith.Memories.IntegrationTests/Search/DegradationIntegrationTests.cs` — 5 `[Fact(Skip)]` scenarios (hybrid degrade, total failure, auto-recovery, single-axis 503). Tracker: unskip under Story 6.3 resilience harness.
- `tests/Hexalith.Memories.IntegrationTests/Ingestion/IngestionRetryIntegrationTests.cs` — 1 `[Fact(Skip)]` scenario (transient-then-success through the DAPR retry policy). Tracker: unskip under Story 6.3.

**Sprint status**

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `5-6-graceful-degradation-on-backend-failure` transitioned `ready-for-dev → in-progress → review`.

### Change Log

| Date       | Change                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        | Author                      |
| ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| 2026-04-14 | Story 5.6 implementation — endpoint-level backend-failure catches (503 BACKEND_UNAVAILABLE / GRAPH_UNAVAILABLE / ALL_BACKENDS_UNAVAILABLE with Retry-After: 5), `AllEnabledAxesUnavailable` signal on `HybridSearchResult`, `SearchEndpointDegradationLog` (events 5601/5602/5603), `IngestionWorkflow.CreateMainRetry()` extraction + pin tests, graph-scoped inner disambiguation via `innerSearchStarted` flag. `IngestionWorkflow` `backoffCoefficient` corrected 2.0 → 1.5 to match AC5. 23 new unit tests, 6 deferred `[Fact(Skip)]` integration scaffolds (unskip tracker: Story 6.3). | Claude Opus 4.6 (dev agent) |
