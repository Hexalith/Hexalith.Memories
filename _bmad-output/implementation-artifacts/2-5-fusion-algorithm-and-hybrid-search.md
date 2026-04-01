# Story 2.5: Fusion Algorithm & Hybrid Search

Status: ready-for-dev

## Story

As a developer,
I want to search memory units across all available axes in a single hybrid query with configurable axis selection,
So that I get the best possible results by combining syntactic, semantic, and graph relevance signals.

## Acceptance Criteria

1. **Given** a hybrid search query with all three axes enabled
   **When** the fusion algorithm executes
   **Then** it calls all three search backends in parallel
   **And** results are merged using the pure function `Fuse(List<ScoredResult>[], FusionWeights) -> RankedResults`
   **And** the composite score is a weighted average of normalized axis scores
   **And** the function has no backend calls or hidden state — all dependencies (corpus statistics, normalization parameters) are injected

2. **Given** the same query executed twice against the same data
   **When** fusion scores are computed
   **Then** identical composite scores are produced (NFR25: deterministic)
   **And** result ordering within the same score tier may vary

3. **Given** a search query with axis selection control (FR18)
   **When** I specify `axes=syntactic,semantic` (excluding graph)
   **Then** only BM25 and vector search are executed
   **And** the fusion algorithm operates on the available axes only
   **And** the graph axis is architecturally optional — disabling it is a config change, not a rearchitecture
   **And** unknown axis names in the `axes` parameter return HTTP 400 with an error message

4. **Given** a hybrid search is executed
   **When** results are returned
   **Then** p95 latency is <1s at 10 concurrent queries/tenant with 10K memory units (NFR3)

5. **Given** one search backend is unavailable during hybrid search
   **When** the remaining axes return results
   **Then** the system returns partial results with a `degraded: true` flag indicating which axes were unavailable
   **And** `unavailableAxes` only lists axes that were enabled but *failed* — axes intentionally excluded via the `axes` parameter or missing required parameters (e.g., graph without `graphStartNodeId`) do NOT appear in `unavailableAxes` and do NOT set `degraded: true`

6. **Given** a hybrid search with `offset` and `maxResults` parameters
   **When** fused results are returned
   **Then** pagination is applied after fusion and sorting — `offset` skips that many results and `maxResults` limits the page size
   **And** `TotalCount` reflects the total number of deduplicated fused results before pagination

## Tasks / Subtasks

- [ ] Task 1: Create `FusionWeights` record in Contracts (AC: 1, 3)
  - [ ] 1.1 Create `src/Hexalith.Memories.Contracts/V1/FusionWeights.cs` as `public sealed record` with properties: `double SyntacticWeight` (default 0.4), `double SemanticWeight` (default 0.4), `double GraphWeight` (default 0.2). Use `init` setters. Include validation: all weights must be >= 0.0 and at least one weight must be > 0.0
  - [ ] 1.2 Add `FusionWeights` to `MemoriesJsonContext` for AOT serialization support

- [ ] Task 2: Create `HybridSearchResult` record in Contracts (AC: 1, 5)
  - [ ] 2.1 Create `src/Hexalith.Memories.Contracts/V1/HybridSearchResult.cs` as `public sealed record` with properties:
    - `IReadOnlyList<FusedScoredResult> Results` — ranked fused results
    - `long TotalCount` — total across all axes before dedup
    - `bool Degraded` — true if any axis failed
    - `IReadOnlyList<string> UnavailableAxes` — list of axis names that were unavailable (e.g., `["graph"]`)
    - `string Query` — echo of the query
  - [ ] 2.2 Create `FusedScoredResult` record in the same file with properties:
    - `string MemoryUnitId`
    - `double CompositeScore` — the final fused score in [0.0, 1.0]
    - `string ContentSnippet`
    - `string SourceUri`
    - `SourceType SourceType`
    - `double? SyntacticScore` — normalized, null if axis not queried or not found
    - `double? SemanticScore` — normalized, null if axis not queried or not found
    - `double? GraphScore` — normalized, null if axis not queried or not found
  - [ ] 2.3 Add `HybridSearchResult` and `FusedScoredResult` to `MemoriesJsonContext`

- [ ] Task 3: Create `FusionEngine` static class — the pure fusion function (AC: 1, 2)
  - [ ] 3.1 Create `src/Hexalith.Memories.Server/Search/FusionEngine.cs` as `internal static class`
  - [ ] 3.2 Implement the main Fuse method:
    ```csharp
    internal static IReadOnlyList<FusedScoredResult> Fuse(
        IReadOnlyList<ScoredResult>? syntacticResults,
        IReadOnlyList<ScoredResult>? semanticResults,
        IReadOnlyList<ScoredResult>? graphResults,
        FusionWeights weights,
        int documentCount,
        double averageDocumentLength)
    ```
    **Algorithm:**
    1. Build a `Dictionary<string, FusionAccumulator>` keyed by `MemoryUnitId` across all non-null result lists
    2. For each syntactic result: normalize score via `ScoreNormalizer.NormalizeBm25(score, documentCount, averageDocumentLength)` and store
    3. For each semantic result: normalize via `ScoreNormalizer.NormalizeCosine(score)` and store
    4. For each graph result: score is already normalized (graph search returns normalized proximity scores via `ScoreNormalizer.NormalizeGraphProximity`) — store as-is
    5. Compute composite score per memory unit as weighted average over **active axes only**: `sum(weight_i * score_i) / sum(weight_i)` where `i` iterates only over axes that produced a score for that unit. This prevents units found by fewer axes from being penalized by division over all weights
    6. Populate `FusedScoredResult` with per-axis scores (null if that axis didn't return the unit)
    7. Sort descending by `CompositeScore`. Ties broken by `MemoryUnitId` (lexicographic) for determinism (NFR25)
    8. Return the sorted list
  - [ ] 3.3 The method is a pure function: no I/O, no injected services, no state — all data passed as parameters. `documentCount` and `averageDocumentLength` are from `CorpusStatisticsActor` (resolved by the caller, not by the fusion function)
  - [ ] 3.4 Guard: if all three result lists are null/empty, return empty list. If `weights` has all-zero weights for active axes, return empty list
  - [ ] 3.5 Content snippet, SourceUri, SourceType: take from the first axis that provided the result (prefer syntactic > semantic > graph). All three axes pull content from the same Redis hash source — the preference order is for determinism, not quality

- [ ] Task 4: Create `HybridSearchService` class (AC: 1, 3, 4, 5, 6)
  - [ ] 4.1 Create `src/Hexalith.Memories.Server/Search/HybridSearchService.cs` as `internal sealed partial class` (partial for `[LoggerMessage]` source generators)
  - [ ] 4.2 Constructor accepts **delegate functions** for each axis search, not direct service references. This enables unit testing without mocking sealed classes:
    ```csharp
    internal sealed partial class HybridSearchService(
        Func<SearchQuery, Task<SearchResult>> syntacticSearchFunc,
        Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> semanticSearchFunc,
        Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> graphSearchFunc,
        IActorProxyFactory actorProxyFactory,
        ILogger<HybridSearchService> logger)
    ```
    The DI registration (Task 5) wires these delegates to the concrete service methods.
  - [ ] 4.3 Main method signature:
    ```csharp
    internal async Task<HybridSearchResult> SearchAsync(
        SearchQuery query,
        TenantEmbeddingConfig? embeddingConfig,
        string? graphStartNodeId,
        int graphDepth,
        FusionWeights weights,
        IReadOnlySet<string> enabledAxes,
        CancellationToken cancellationToken)
    ```
  - [ ] 4.4 Implementation flow:
    1. Build a list of `Task<SearchResult?>` for each enabled axis. Only launch axes present in `enabledAxes` (valid values: `"syntactic"`, `"semantic"`, `"graph"`)
    2. For semantic: requires `embeddingConfig` (if null and axis enabled, log warning and skip axis, add to unavailable)
    3. For graph: requires `graphStartNodeId` (if null and axis enabled, skip axis — graph participation in hybrid search requires a starting node). **Note:** Graph axis in hybrid mode uses pure graph traversal (no inner search delegate), returning proximity-scored results that are fused with other axes. This is the "optional fusion scorer" role (D2), distinct from graph-scoped search (which post-filters other axis results)
    4. Execute enabled searches in parallel via `Task.WhenAll`
    5. Catch exceptions per-axis: wrap each axis call in try/catch. On failure, set that axis result to null, add axis name to `unavailableAxes`, log error. Do NOT let one axis failure crash the entire hybrid search (NFR18: partial results). **Important distinction:** `unavailableAxes` only tracks axes that were enabled and *failed*. Axes excluded by the caller (not in `enabledAxes`) or missing required parameters (semantic without embeddingConfig, graph without startNodeId) are *intentionally excluded* — they do NOT set `degraded = true` and do NOT appear in `unavailableAxes`
    6. Fetch `CorpusStatistics` from actor proxy — needed for BM25 normalization:
       ```csharp
       ICorpusStatisticsActor statsActor = actorProxyFactory
           .CreateActorProxy<ICorpusStatisticsActor>(
               new ActorId(query.TenantId),
               nameof(CorpusStatisticsActor));
       CorpusStatistics stats = await statsActor.GetStatisticsAsync();
       ```
       If `stats.DocumentCount == 0`, BM25 normalization will return 0.0 for all scores — this is correct behavior (stats not yet meaningful). If the actor call itself fails, catch and treat syntactic scores as raw (log warning, set documentCount=0, averageDocumentLength=0)
    7. Call `FusionEngine.Fuse(...)` with collected results, weights, and corpus stats
    8. Apply pagination: skip `query.Offset`, take `query.MaxResults` from the fused sorted list
    9. Construct and return `HybridSearchResult` with degraded flag and unavailable axes

- [ ] Task 5: Register `HybridSearchService` in Program.cs (AC: 1)
  - [ ] 5.1 Add DI registration following existing pattern (explicit factory), wiring delegates to concrete service methods:
    ```csharp
    builder.Services.AddSingleton<HybridSearchService>(sp =>
    {
        var syntactic = sp.GetRequiredService<SyntacticSearchService>();
        var semantic = sp.GetRequiredService<SemanticSearchService>();
        var graph = sp.GetRequiredService<GraphScopedSearch>();
        return new HybridSearchService(
            query => syntactic.SearchAsync(query),
            (query, config, ct) => semantic.SearchAsync(query, config, ct),
            // innerSearch: null -> pure graph traversal mode (fusion scorer, not graph-scoped search)
            (query, startNode, depth, ct) => graph.SearchAsync(query, startNode, depth, innerSearch: null, ct),
            sp.GetRequiredService<IActorProxyFactory>(),
            sp.GetRequiredService<ILogger<HybridSearchService>>());
    });
    ```

- [ ] Task 6: Add `axis=hybrid` endpoint routing in Program.cs (AC: 1, 3, 4, 5, 6)
  - [ ] 6.1 In the existing `/api/search` endpoint handler, add a new branch for `axis == "hybrid"`:
    - Parse `axes` query parameter (comma-separated, e.g., `axes=syntactic,semantic,graph`). Default: all three axes enabled
    - Parse optional `graphStartNodeId` and `depth` (existing params)
    - Construct `FusionWeights` with default weights (0.4, 0.4, 0.2). Weights are NOT user-configurable via query params in MVP — hardcoded defaults, tunable per-tenant later
    - Parse `enabledAxes` from the `axes` query parameter into a `HashSet<string>`
    - Fetch `TenantEmbeddingConfig` from `TenantConfigurationActor` (existing pattern) if semantic axis enabled
    - Call `HybridSearchService.SearchAsync(...)` and return `HybridSearchResult`
  - [ ] 6.2 Validate `axes` parameter: reject unknown axis names with 400 Bad Request
  - [ ] 6.3 Return HTTP 200 with `HybridSearchResult` (includes degraded flag if applicable)

- [ ] Task 7: Unit tests for `FusionEngine` (AC: 1, 2)
  - [ ] 7.1 Create `tests/Hexalith.Memories.Server.Tests/Search/FusionEngineTests.cs`
  - [ ] 7.2 Test: all three axes with known scores -> expected composite scores (manually computed weighted average)
  - [ ] 7.3 Test: syntactic + semantic only (graph null) -> composite uses only two-axis weights
  - [ ] 7.4 Test: single axis only (other two null) -> composite equals normalized single-axis score
  - [ ] 7.5 Test: same memory unit appearing in multiple axes -> merged with per-axis scores populated
  - [ ] 7.6 Test: memory unit appearing in only one axis -> other axis scores are null, composite uses single-axis weight
  - [ ] 7.7 Test: determinism — same inputs produce identical output ordering (NFR25)
  - [ ] 7.8 Test: tie-breaking — two units with same composite score ordered by MemoryUnitId lexicographically
  - [ ] 7.9 Test: empty inputs (all null) -> empty result list
  - [ ] 7.10 Test: BM25 normalization applied correctly — raw BM25 score of 10.0 with docCount=1000, avgDocLen=200 produces expected normalized value in composite
  - [ ] 7.11 Test: cosine passthrough — cosine score of 0.85 appears unchanged in FusedScoredResult.SemanticScore
  - [ ] 7.12 Test: graph scores passed through (already normalized by GraphScopedSearch)
  - [ ] 7.13 Test: content snippet taken from syntactic result when available (preferred source)
  - [ ] 7.14 Test: content snippet falls back to semantic then graph when syntactic not available for that unit
  - [ ] 7.15 Test: all-zero weights for active axes -> returns empty list (no division by zero)
  - [ ] 7.16 Test: composite score always in [0.0, 1.0] range with Property-based testing using random inputs (10 iterations)
  - [ ] 7.17 Test: BM25 raw score = double.NaN -> normalized to 0.0 by ScoreNormalizer, does not poison composite score (poison pill prevention)
  - [ ] 7.18 Test: BM25 raw score = double.PositiveInfinity -> normalized to 0.0, does not produce NaN/Infinity composite

- [ ] Task 8: Unit tests for `HybridSearchService` (AC: 3, 5, 6)
  - [ ] 8.1 Create `tests/Hexalith.Memories.Server.Tests/Search/HybridSearchServiceTests.cs`
  - [ ] 8.2 Inject fake `Func<>` delegates (lambda stubs) and mock `IActorProxyFactory` via NSubstitute. No sealed-class mocking needed — the delegate constructor pattern makes this straightforward
  - [ ] 8.3 Test: all three axes enabled -> all three delegate functions called
  - [ ] 8.4 Test: `enabledAxes = {"syntactic", "semantic"}` -> only syntactic and semantic delegates called, graph delegate NOT called
  - [ ] 8.5 Test: syntactic delegate throws exception -> `degraded=true`, `unavailableAxes=["syntactic"]`, remaining axes still return correct fused results with recomputed composite scores
  - [ ] 8.6 Test: semantic enabled but `embeddingConfig` is null -> semantic intentionally excluded (NOT in unavailableAxes, NOT degraded)
  - [ ] 8.7 Test: graph enabled but `graphStartNodeId` is null -> graph intentionally excluded (NOT in unavailableAxes, NOT degraded)
  - [ ] 8.8 Test: corpus stats actor called with correct tenantId
  - [ ] 8.9 Test: pagination — offset=5, maxResults=3 correctly slices fused results; TotalCount reflects pre-pagination count

- [ ] Task 9: Contract tests for new records (AC: 1)
  - [ ] 9.1 Add serialization round-trip tests for `FusionWeights`, `HybridSearchResult`, `FusedScoredResult` in `tests/Hexalith.Memories.Contracts.Tests/V1/`
  - [ ] 9.2 Follow existing pattern from `ScoredResultTests.cs` — serialize to JSON, deserialize back, verify equality
  - [ ] 9.3 Test: `FusionWeights` default constructor produces expected 0.4/0.4/0.2 values — prevents accidental default changes

## Dev Notes

### Implementation Overview

This story creates the **fusion layer** that composes the three individual search axes (Stories 2.1-2.3) and the normalization primitives (Story 2.4) into a single hybrid search endpoint. You are building:

1. A `FusionEngine` static class with a pure `Fuse()` function — the architectural center of gravity (D-arch)
2. A `HybridSearchService` that orchestrates parallel axis calls, handles degradation, and applies fusion
3. Contract types for fusion weights and hybrid results
4. A new `axis=hybrid` route in the existing `/api/search` endpoint

**This story does NOT implement explain mode** (per-axis breakdown display) — that's Story 2.6. However, the `FusedScoredResult` type includes per-axis score fields that Story 2.6 will expose.

**This story does NOT implement benchmark validation** — that's Story 2.7. However, the fusion function being a pure function makes it directly testable by benchmarks.

### Fusion Algorithm Design

The fusion function is a **weighted average** with per-unit axis-aware scoring:

```csharp
// For each memory unit found across any axis:
double compositeScore = sum(weight_i * normalizedScore_i) / sum(weight_i)
// where i iterates ONLY over axes that found this specific unit
```

**Why weighted average over active axes only:** A memory unit found by syntactic search (BM25=0.8) but not by semantic or graph should score `0.8 * 0.4 / 0.4 = 0.8`, not `0.8 * 0.4 / 1.0 = 0.32`. Dividing by the sum of active-axis weights for that specific unit ensures units found by fewer axes aren't arbitrarily penalized. The weight itself already expresses the relative importance of each axis.

**Default weights (0.4 syntactic, 0.4 semantic, 0.2 graph):** These are initial values for thesis validation. Story 2.7 benchmarks will determine if they need adjustment. Weights are not user-tunable via API in MVP — they are hardcoded constants in the service. Per-tenant weight tuning is a Growth-phase feature.

**Why weighted average, not Reciprocal Rank Fusion (RRF):** RRF (`1/(k+rank)`) is a common alternative that uses only rank position, discarding score magnitude. Weighted average was chosen because (a) the architecture already invests in score normalization (Story 2.4) — discarding magnitude would waste that work, (b) score magnitude carries signal (a cosine score of 0.95 vs 0.71 is meaningful), and (c) weighted average is the architecture-prescribed approach (D9, NFR24). If benchmarks (Story 2.7) show weighted average underperforms RRF, the pure function can be swapped without changing any consumer code.

**Boosting paradox — fewer axes can produce higher scores:** A unit found only by semantic (score 0.9) gets composite `0.9 * 0.4 / 0.4 = 0.9`, while a unit found by all three axes with moderate scores (0.3, 0.6, 0.4) gets `(0.4*0.3 + 0.4*0.6 + 0.2*0.4) / 1.0 = 0.44`. The single-axis unit outranks the multi-axis unit. This is intentional — the formula rewards signal strength, not breadth. If benchmarks show breadth should be rewarded, a coverage bonus term can be added: `compositeScore + coverageBonus * (axesFound / totalAxes)`. No change needed now — validate with Story 2.7.

**Cross-tenant weight non-transferability:** Because BM25 normalization is corpus-adaptive (Story 2.4), the same raw BM25 score normalizes differently across tenants. Default weights may work differently across tenants with very different corpus characteristics. This is acceptable for MVP — benchmark validation (Story 2.7) will flag if weights need per-tenant tuning.

### Deduplication Strategy

Memory units may appear in multiple axis results (e.g., found by both BM25 and vector search). The `Fuse()` function deduplicates by `MemoryUnitId`:

- Build a dictionary keyed by `MemoryUnitId`
- Accumulate per-axis normalized scores for each unit
- Content snippet, SourceUri, SourceType taken from the first axis that provided the result (prefer syntactic > semantic > graph)

### Graph Axis in Hybrid Mode

Graph axis participation in hybrid search uses **pure graph traversal** (mode 1 from Story 2.3), NOT graph-scoped search (mode 2). This means:

- Graph traversal from `graphStartNodeId` up to `depth`, returning proximity-scored results
- These results are fused with syntactic/semantic results by `MemoryUnitId`
- If `graphStartNodeId` is not provided, graph axis is silently excluded from fusion (it cannot contribute without a starting point)

This is the "optional fusion scorer" role from architecture decision D2. The graph axis can be disabled entirely via the `axes` parameter — it's architecturally optional.

**Practical reality:** In typical usage, hybrid search operates as syntactic+semantic fusion. Graph participation is the exception, not the norm — it requires the caller to already know a relevant graph node ID. The primary value of graph is standalone traversal (Story 2.3) and graph-scoped search, not fusion scoring. Expect most hybrid queries to be two-axis.

### Degradation Mode

**Critical distinction: failed vs. intentionally excluded.**

`degraded = true` and `unavailableAxes` are ONLY set when an enabled axis *fails at runtime* (throws an exception). They are NOT set when:
- An axis is excluded via the `axes` parameter (intentional caller choice)
- Graph axis is enabled but `graphStartNodeId` is null (missing prerequisite — silently excluded)
- Semantic axis is enabled but `embeddingConfig` is null (missing prerequisite — silently excluded)

When a backend is unavailable (throws exception during search), the system:
1. Catches the exception per-axis (does not propagate)
2. Adds the axis name to `unavailableAxes`
3. Sets `degraded = true` on the response
4. Fuses remaining axes normally (fewer axes = fewer inputs to Fuse)
5. Logs the error with structured logging

This satisfies NFR18 (partial backend failure -> degraded, not total failure) and FR66 (partial results with indication of excluded axes).

### Determinism (NFR25)

The fusion function produces deterministic scores because:
- All normalization functions are pure (ScoreNormalizer)
- Weighted average is a deterministic computation
- Tie-breaking uses lexicographic ordering on MemoryUnitId (deterministic)
- No randomness, no floating-point non-determinism (same inputs -> same IEEE 754 results)

Note: result ordering within the same composite score tier may vary ONLY if MemoryUnitIds are identical (impossible by definition). In practice, all orderings are fully deterministic.

### Existing Search Service Patterns to Follow

**SyntacticSearchService** (`Search/SyntacticSearchService.cs`):
- Constructor: `(IConnectionMultiplexer redis, ILogger<T> logger)` via keyed DI
- Returns `SearchResult` with `ScoredResult[]` where `Axis = "syntactic"`, Score = raw BM25
- Index: `{tenantId}:memories:idx`

**SemanticSearchService** (`Search/SemanticSearchService.cs`):
- Constructor: `(IConnectionMultiplexer redis, EmbeddingClient embeddingClient, ILogger<T> logger)`
- Returns `SearchResult` with `ScoredResult[]` where `Axis = "semantic"`, Score = cosine similarity [0,1]
- Requires `TenantEmbeddingConfig` parameter (fetched from `TenantConfigurationActor` by caller)
- Enriches results from syntactic hashes via batch pipeline

**GraphScopedSearch** (`Search/GraphScopedSearch.cs`):
- Constructor: `(IConnectionMultiplexer falkordb, IConnectionMultiplexer redis, IGraphQueryBuilder queryBuilder, ILogger<T> logger)`
- Returns `SearchResult` with `ScoredResult[]` where `Axis = "graph"`, Score = proximity (already normalized)
- Two modes: pure traversal (no inner search) and graph-scoped inner search
- For hybrid fusion, use **pure traversal** mode: call `SearchAsync(query, startNodeId, depth, innerSearch: null, cancellationToken)`

**ScoreNormalizer** (`Search/ScoreNormalizer.cs`):
- `NormalizeBm25(double rawScore, int documentCount, double averageDocumentLength)` -> [0.0, 1.0]
- `NormalizeCosine(double cosineScore)` -> [0.0, 1.0] (passthrough with clamp)
- `NormalizeGraphProximity(int hopDistance)` -> [0.0, 1.0] (already called inside GraphScopedSearch)

**Important:** Graph scores from `GraphScopedSearch` are already normalized (the service calls `ScoreNormalizer.NormalizeGraphProximity` internally via `ComputeProximityScore`). The fusion function should NOT re-normalize graph scores — take them as-is. Cosine scores from `SemanticSearchService` are already converted from distance to similarity in [0,1] — the `NormalizeCosine` passthrough is a defensive clamp. Only BM25 raw scores require active normalization using corpus statistics.

### CorpusStatistics Actor Proxy Pattern

```csharp
// Get corpus stats for BM25 normalization
ICorpusStatisticsActor statsActor = actorProxyFactory
    .CreateActorProxy<ICorpusStatisticsActor>(
        new ActorId(query.TenantId),
        nameof(CorpusStatisticsActor));
CorpusStatistics stats = await statsActor.GetStatisticsAsync();
// stats.DocumentCount, stats.AverageDocumentLength
```

If `stats.DocumentCount == 0` (empty index or stats not yet refreshed), `ScoreNormalizer.NormalizeBm25` returns 0.0 for all scores. The fusion function handles this gracefully — syntactic scores contribute nothing, and the composite score reflects only the remaining axes.

**Cold start note:** The first hybrid search after actor activation may produce semantic-only results until `CorpusStatisticsActor` refreshes stats (dueTime is `TimeSpan.Zero`, so within seconds). This is transient and self-healing — subsequent queries will include BM25 scores once the timer fires.

### Endpoint Routing Pattern

The existing `/api/search` endpoint in Program.cs (lines ~197-392) handles axis routing via the `axis` query parameter. Add a new branch:

```
axis=hybrid (NEW)
  Required: tenantId, query
  Optional: axes (comma-separated, default: "syntactic,semantic,graph")
  Optional: graphStartNodeId, depth (for graph axis participation)
  Optional: caseId, maxResults, offset

  Flow:
  1. Parse and validate axes parameter
  2. Fetch TenantEmbeddingConfig if semantic in axes
  3. Call HybridSearchService.SearchAsync(...)
  4. Return HybridSearchResult as JSON
```

### Project Structure Notes

New files follow existing conventions:
```
src/Hexalith.Memories.Contracts/V1/
  FusionWeights.cs              # NEW — fusion weight configuration
  HybridSearchResult.cs         # NEW — fusion response with degradation info
  FusedScoredResult.cs          # NEW — per-result fusion scores (may be in same file as HybridSearchResult)

src/Hexalith.Memories.Server/Search/
  FusionEngine.cs               # NEW — pure static fusion function
  HybridSearchService.cs        # NEW — orchestrator for parallel axis calls + fusion

tests/Hexalith.Memories.Server.Tests/Search/
  FusionEngineTests.cs          # NEW — pure function tests
  HybridSearchServiceTests.cs   # NEW — orchestration tests with mocks

tests/Hexalith.Memories.Contracts.Tests/V1/
  FusionWeightsTests.cs         # NEW — serialization round-trip
  HybridSearchResultTests.cs    # NEW — serialization round-trip
```

All files use:
- ITANEO copyright header
- `namespace Hexalith.Memories.{Project}.{Folder};` file-scoped namespace
- `sealed` classes (no inheritance)
- `partial class` only if using `[LoggerMessage]` source generators
- `internal` for implementation classes, `public` for contracts/interfaces

### Testing Standards

- **Framework:** xUnit 2.9.3
- **Assertions:** Shouldly (`result.ShouldBe(expected)`, `result.ShouldBeInRange(0.0, 1.0)`)
- **Tolerance for doubles:** `result.ShouldBe(expected, tolerance: 0.001)` for floating-point comparisons
- **Mocking strategy (resolved):**
  - `FusionEngine.Fuse()` — pure function, no mocking needed. Test exhaustively with direct assertions
  - `HybridSearchService` — constructor accepts `Func<>` delegates (not concrete sealed classes). Tests inject lambda stubs that return canned `SearchResult` data or throw exceptions for degradation scenarios. No NSubstitute needed for search services
  - `IActorProxyFactory` — mock via NSubstitute to return a substitute `ICorpusStatisticsActor` that returns known `CorpusStatistics`
- **Determinism test (NFR25):** Run Fuse 100 times with same inputs, verify identical output each time
- **Latency test (NFR3):** <1s hybrid latency at 10 concurrent queries is an integration/performance concern — deferred to Story 2.7 benchmark suite or integration test suite. Not in scope for unit tests

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.5 — lines 699-732]
- [Source: _bmad-output/planning-artifacts/prd.md#FR17, FR18, FR19, FR63, FR66]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR3 (<1s hybrid latency), NFR24-26 (algorithmic quality)]
- [Source: _bmad-output/planning-artifacts/architecture.md — D2 (graph dual-role), D9 (fusion = pure function), NFR25 (determinism)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Components — FusionEngine is a function not interface, CorpusStatisticsProvider facade]
- [Source: src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs — normalization functions]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs — BM25 search pattern]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs — vector search pattern]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs — graph traversal, ComputeProximityScore]
- [Source: src/Hexalith.Memories.Server/Actors/ICorpusStatisticsActor.cs — actor proxy interface]
- [Source: src/Hexalith.Memories.Server/Program.cs — DI registration pattern, endpoint routing]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs — AOT serialization context]

### Previous Story Intelligence (Story 2.4)

From Story 2.4 implementation:
- `ScoreNormalizer` is `internal static class` with three pure functions — reuse directly, do not duplicate
- `CorpusStatisticsActor` caches per-tenant stats (docCount, avgDocLength) with 5-minute refresh timer
- Actor ID = tenant ID — call via `actorProxyFactory.CreateActorProxy<ICorpusStatisticsActor>(new ActorId(tenantId), nameof(CorpusStatisticsActor))`
- `CorpusStatistics` is a `public sealed record` (made public for DAPR proxy interface)
- `GraphScopedSearch.ComputeProximityScore` delegates to `ScoreNormalizer.NormalizeGraphProximity` — graph scores in search results are already normalized
- Build error lesson: DAPR actor proxy requires public interface return types
- Build error lesson: Use `Resp2Type` not deprecated `Type` for StackExchange.Redis
- `[FromKeyedServices("redis")]` and `[FromKeyedServices("falkordb")]` are the keyed DI patterns
- `sealed partial class` with `[LoggerMessage]` for structured logging
- Full test suite as of Story 2.4: 336 tests, 0 failures

### Git Intelligence

Recent commits show sequential search axis implementations:
- `81057a3` feat: Implement GraphScopedSearch for traversing FalkorDB and enriching results from Redis
- `5c39312` feat: Implement Semantic Search Service with KNN vector search capabilities
- `0d104b7` feat: Implement Syntactic Search Service with BM25 ranking and related data models

Pattern: Each search service is a standalone `internal sealed class` registered as singleton via explicit factory in Program.cs. The hybrid search service follows this same pattern but composes the other three.

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
