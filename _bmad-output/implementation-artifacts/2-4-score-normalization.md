# Story 2.4: Score Normalization

Status: done

## Story

As a developer,
I want all search axis scores normalized to 0.0-1.0 before fusion,
So that scores from different axes are comparable and the fusion algorithm produces meaningful composite rankings.

## Acceptance Criteria

1. **Given** a raw BM25 score from RediSearch (unbounded range)
   **When** normalization is applied
   **Then** the score is normalized to 0.0-1.0 using saturation normalization against corpus statistics
   **And** the `CorpusStatisticsActor` per tenant provides: document count and average document length (bytes per document from RediSearch index metadata)
   **And** the normalization function is a pure function: `NormalizeBm25(rawScore, corpusStats) -> float` with known inputs producing known outputs

2. **Given** a cosine similarity score from Redis Vector
   **When** normalization is applied
   **Then** the score is passed through unchanged (native 0.0-1.0 range)

3. **Given** a graph proximity score
   **When** normalization is applied
   **Then** the score is computed via inverse hop distance with decay function, producing 0.0-1.0
   **And** the decay function is documented and deterministic

4. **Given** the `CorpusStatisticsActor` for a tenant
   **When** corpus statistics are queried
   **Then** methods `GetDocumentCount()` and `GetAverageDocumentLength()` return cached values (term frequency excluded — not consumed by the saturation normalization formula)
   **And** statistics are refreshed via timer
   **And** actor state is persisted before every response (not batch-persisted on deactivation)

5. **Given** normalization unit tests with known inputs
   **When** each normalization function is executed
   **Then** outputs match expected values exactly (NFR24)

## Tasks / Subtasks

- [x] Task 1: Create `ScoreNormalizer` pure static class (AC: 1, 2, 3)
    - [x] 1.1 Create `src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs` as `internal static class`
    - [x] 1.2 Implement `NormalizeBm25(double rawScore, int documentCount, double averageDocumentLength)` using saturation formula: `rawScore / (rawScore + k)` where `k = Math.Log2(documentCount + 1) * (averageDocumentLength / 100.0)`, clamped to [0.0, 1.0]. Guard: if `!double.IsFinite(rawScore) || rawScore <= 0.0 || documentCount <= 0 || averageDocumentLength <= 0.0`, return 0.0. The `IsFinite` check prevents NaN/Infinity from propagating through fusion as poison pills.
    - [x] 1.3 Implement `NormalizeCosine(double cosineScore)` as passthrough: guard `if (!double.IsFinite(cosineScore)) return 0.0`, then `Math.Clamp(cosineScore, 0.0, 1.0)`. The `IsFinite` guard is critical because `Math.Clamp(NaN, 0, 1)` returns NaN, not 0.
    - [x] 1.4 Implement `NormalizeGraphProximity(int hopDistance)` — guard: `ArgumentOutOfRangeException.ThrowIfNegative(hopDistance)` then `Math.Clamp(1.0 / (1.0 + hopDistance), 0.0, 1.0)`. This centralizes the formula; `GraphScopedSearch.ComputeProximityScore` should call `ScoreNormalizer.NormalizeGraphProximity` to avoid duplication. Negative hop distance would produce `1/0` = Infinity which clamps to 1.0 — a silent wrong answer scored as "same node".
    - [x] 1.5 All three methods are pure functions: no I/O, no state, deterministic

- [x] Task 2: Create `CorpusStatistics` record (AC: 4)
    - [x] 2.1 Create `src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs` as `public sealed record` with properties: `int DocumentCount`, `double AverageDocumentLength`, `DateTimeOffset LastRefreshedAt`. Located in `Server/Actors/` (not Contracts). Made public (not internal) because DAPR actor proxy requires public interface return types — `ICorpusStatisticsActor.GetStatisticsAsync()` returns `CorpusStatistics`.

- [x] Task 3: Create `ICorpusStatisticsActor` interface (AC: 4)
    - [x] 3.1 Create `src/Hexalith.Memories.Server/Actors/ICorpusStatisticsActor.cs` extending `IActor`
    - [x] 3.2 Define methods: `Task<int> GetDocumentCountAsync()`, `Task<double> GetAverageDocumentLengthAsync()`, `Task<CorpusStatistics> GetStatisticsAsync()` (convenience method returning full snapshot)
    - [x] 3.3 Follow exact pattern of `ITenantConfigurationActor` (same namespace, same XML doc style)

- [x] Task 4: Create `CorpusStatisticsActor` implementation (AC: 4)
    - [x] 4.1 Create `src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs` as `internal sealed partial class` extending `Actor, ICorpusStatisticsActor`
    - [x] 4.2 Constructor: `(ActorHost host, [FromKeyedServices("redis")] IConnectionMultiplexer redis, ILogger<CorpusStatisticsActor> logger)` — inject Redis to query RediSearch index info
    - [x] 4.3 State key: `"corpusStats"`, type: `CorpusStatistics`
    - [x] 4.4 `OnActivateAsync()`: register timer `"RefreshCorpusStats"` with `dueTime: TimeSpan.Zero` (refresh immediately on activation), `period: TimeSpan.FromMinutes(5)`
    - [x] 4.5 Timer callback `RefreshStatsCallbackAsync()`: query RediSearch via raw `db.ExecuteAsync("FT.INFO", indexName)` and parse via `ParseFtInfoResult(RedisResult)` (see Task 4.10). Use `Id.GetId()` for tenantId. Handle missing index gracefully (set DocumentCount=0, AverageDocumentLength=0). Handle `RedisConnectionException` by logging a warning and retaining previous state — do not let a transient connection failure wipe cached stats or deactivate the actor
    - [x] 4.10 Extract `internal static CorpusStatistics ParseFtInfoResult(RedisResult raw, DateTimeOffset refreshedAt)` as a pure function in `CorpusStatisticsActor`. Parses the flat key-value `RedisResult[]` array to extract `num_docs` and `doc_table_size_mb`. Uses `Resp2Type` (not deprecated `Type`) for type checking. Skips non-BulkString keys gracefully to survive Redis version upgrades that change the response format. Testable without Redis — accepts a mock `RedisResult` array
    - [x] 4.6 Persist state via `StateManager.SetStateAsync("corpusStats", stats)` BEFORE returning from every public method and after refresh — per architecture requirement D24 (state persisted before every response)
    - [x] 4.7 `GetDocumentCountAsync()`: return cached `DocumentCount` from state. If no state exists yet (first call before timer fires), trigger refresh inline. If refresh still returns docCount=0 (empty index), consumers should treat BM25 normalization as unavailable rather than normalizing all scores to 0.0 — the `CorpusStatistics.DocumentCount == 0` signal indicates stats are not yet meaningful
    - [x] 4.8 `GetAverageDocumentLengthAsync()`: return cached `AverageDocumentLength` from state
    - [x] 4.9 `GetStatisticsAsync()`: return full `CorpusStatistics` snapshot

- [x] Task 5: Register actor in Program.cs (AC: 4)
    - [x] 5.1 Add `options.Actors.RegisterActor<CorpusStatisticsActor>()` in the existing `AddActors` block (after `TenantConfigurationActor`)

- [x] Task 6: Refactor `GraphScopedSearch.ComputeProximityScore` to use `ScoreNormalizer` (AC: 3)
    - [x] 6.1 Change `GraphScopedSearch.ComputeProximityScore(int hopDistance)` body to delegate to `ScoreNormalizer.NormalizeGraphProximity(hopDistance)` — keeps the existing public API surface but centralizes the formula
    - [x] 6.2 Verify existing `GraphScopedSearchTests` still pass (formula unchanged, only call site moved)

- [x] Task 7: Unit tests for `ScoreNormalizer` (AC: 1, 2, 3, 5)
    - [x] 7.1 Create `tests/Hexalith.Memories.Server.Tests/Search/ScoreNormalizerTests.cs`
    - [x] 7.2 Test `NormalizeBm25`: rawScore=0 -> 0.0
    - [x] 7.3 Test `NormalizeBm25`: rawScore=5.0, docCount=1000, avgDocLen=200 -> expected value (compute manually from formula)
    - [x] 7.4 Test `NormalizeBm25`: rawScore=100.0 (very high) -> close to 1.0 (saturation)
    - [x] 7.5 Test `NormalizeBm25`: output always in [0.0, 1.0] for any positive rawScore
    - [x] 7.6 Test `NormalizeBm25`: docCount=0 -> returns 0.0
    - [x] 7.7 Test `NormalizeBm25`: monotonicity — higher rawScore always produces higher normalized score (same corpus stats)
    - [x] 7.8 Test `NormalizeCosine`: score=0.91 -> 0.91 (passthrough)
    - [x] 7.9 Test `NormalizeCosine`: score=0.0 -> 0.0
    - [x] 7.10 Test `NormalizeCosine`: score=1.0 -> 1.0
    - [x] 7.11 Test `NormalizeCosine`: score=1.001 (floating-point overshoot) -> clamped to 1.0
    - [x] 7.12 Test `NormalizeGraphProximity`: hopDistance=0 -> 1.0
    - [x] 7.13 Test `NormalizeGraphProximity`: hopDistance=1 -> 0.5
    - [x] 7.14 Test `NormalizeGraphProximity`: hopDistance=2 -> 0.333 (tolerance 0.001)
    - [x] 7.15 Test `NormalizeGraphProximity`: hopDistance=3 -> 0.25
    - [x] 7.16 Test `NormalizeBm25`: rawScore=-5.0 (negative) -> 0.0
    - [x] 7.17 Test `NormalizeGraphProximity`: hopDistance=1000 (very large) -> value > 0.0 and < 1.0
    - [x] 7.18 Test `NormalizeBm25`: rawScore=double.NaN -> 0.0 (IsFinite guard)
    - [x] 7.19 Test `NormalizeBm25`: rawScore=double.PositiveInfinity -> 0.0 (IsFinite guard)
    - [x] 7.20 Test `NormalizeCosine`: score=double.NaN -> 0.0 (IsFinite guard)
    - [x] 7.21 Test `NormalizeGraphProximity`: hopDistance=-1 -> throws `ArgumentOutOfRangeException`

- [x] Task 8: Unit tests for `CorpusStatisticsActor` (AC: 4)
    - [x] 8.1 Create `tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs`
    - [x] 8.2 Test: `GetDocumentCountAsync` returns value from state
    - [x] 8.3 Test: `GetAverageDocumentLengthAsync` returns value from state
    - [x] 8.4 Test: `GetStatisticsAsync` returns full snapshot
    - [x] 8.5 Test: `ParseFtInfoResult` with valid `RedisResult[]` array containing `num_docs=100` and `doc_table_size_mb=0.5` -> returns `CorpusStatistics { DocumentCount=100, AverageDocumentLength=5242.88 }`
    - [x] 8.6 Test: `ParseFtInfoResult` with empty/malformed `RedisResult[]` -> returns `CorpusStatistics { DocumentCount=0, AverageDocumentLength=0 }` (graceful degradation, no exception)
    - [x] 8.7 Note: Full actor lifecycle tests (timer, Redis query) are integration-level — defer to Story 2.5 or a dedicated integration test story if needed. Unit tests focus on pure state-based behavior and the `ParseFtInfoResult` pure function

### Review Findings

- [x] [Review][Patch] Persist cached corpus statistics before every public response [src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs:36]
- [x] [Review][Patch] Guard BM25 normalization against non-finite and overflowed corpus inputs [src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs:23]
- [x] [Review][Patch] Validate and fully parse `doc_table_size_mb` values from FT.INFO [src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs:105]
- [x] [Review][Patch] Harden corpus-stat refresh error handling for missing-index variants and Redis timeouts [src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs:149]

## Dev Notes

### Implementation Overview

This story creates the **score normalization layer** that makes all three search axis scores comparable before fusion (Story 2.5). You are building:

1. A `ScoreNormalizer` static class with three pure normalization functions (BM25 saturation, cosine passthrough, graph proximity)
2. A `CorpusStatisticsActor` DAPR actor that caches per-tenant RediSearch index statistics
3. A `CorpusStatistics` record for actor state

**This story does NOT implement fusion** — it prepares the normalization primitives that Story 2.5 will compose into a fusion algorithm. The search endpoints continue to return raw scores in this story. Normalization happens at fusion time (Story 2.5 calls normalizers with injected corpus stats), NOT inside individual search services. `SyntacticSearchService`, `SemanticSearchService`, and `GraphScopedSearch` remain unchanged — they return raw scores and consumers decide whether to normalize.

**Downstream awareness — embedding model changes:** If the tenant's embedding model is changed (e.g., `text-embedding-004` -> a future model), cosine score distributions may shift significantly. After any model change, benchmarks (Story 2.7) must be re-run to validate that fusion weighting still produces effective results.

### BM25 Saturation Normalization Formula

Raw BM25 scores from RediSearch are unbounded (typically 0-30+, but can be higher for rare terms in large corpora). The saturation normalization formula maps them to [0.0, 1.0]:

```csharp
internal static double NormalizeBm25(double rawScore, int documentCount, double averageDocumentLength)
{
    if (!double.IsFinite(rawScore) ||
        rawScore <= 0.0 ||
        documentCount <= 0 ||
        !double.IsFinite(averageDocumentLength) ||
        averageDocumentLength <= 0.0)
        return 0.0;

    // Corpus-adaptive saturation constant:
    // - log2(docCount+1) scales with corpus size (more docs = higher threshold)
    // - avgDocLen/100 scales with document length (longer docs = higher raw BM25)
    double k = Math.Log2((double)documentCount + 1.0) * (averageDocumentLength / 100.0);
    if (!double.IsFinite(k) || k <= 0.0)
        return 0.0;

    return Math.Clamp(rawScore / (rawScore + k), 0.0, 1.0);
}
```

**Why saturation normalization:** The `score / (score + k)` formula produces a natural S-curve:

- Zero raw score -> 0.0
- Raw score equal to k -> 0.5
- Very high raw scores asymptote toward 1.0 (never exceed it)
- Monotonically increasing — preserves relative ordering

**Why corpus-adaptive k:** A fixed k works poorly across different corpus sizes. A corpus with 10 documents has different BM25 score ranges than one with 100K documents. Using `log2(docCount+1) * avgDocLen/100` as k adapts the saturation threshold to the corpus, ensuring mid-range BM25 scores normalize to ~0.5 regardless of corpus characteristics.

**Initial formula, subject to calibration:** This saturation formula is an initial design choice. Story 2.7 (Benchmark Suite) will validate whether the resulting normalized score distribution produces effective fusion results. If benchmarks show poor discrimination (scores clustering too tightly), the formula or `k` derivation may need adjustment. The architecture is designed for this — the pure function can be swapped without changing any consumer code. One specific refinement to evaluate: **query-adaptive normalization** — using per-query IDF (inverse document frequency) to adjust `k` for each query rather than using a single corpus-level `k`. Common-term queries (e.g., "the claim") produce uniformly low BM25 scores that compress to a narrow band under corpus-level k, while rare-term queries produce high scores. Query-adaptive k could improve discrimination at the cost of additional complexity and a per-query RediSearch lookup.

**Cross-tenant weight non-transferability:** Because `k` is corpus-adaptive, the same raw BM25 score normalizes differently across tenants with different corpus sizes. This means fusion weights tuned for one tenant may not be optimal for another. Story 2.5 (Fusion) should treat weights as per-tenant or use corpus-size-independent defaults.

### Cosine Similarity Passthrough

Redis Vector returns cosine distance in [0.0, 2.0]. `SemanticSearchService.ConvertDistanceToSimilarity()` already converts this to similarity in [0.0, 1.0] via `1.0 - distance`. The normalizer is a defensive clamp:

```csharp
internal static double NormalizeCosine(double cosineScore)
    => Math.Clamp(cosineScore, 0.0, 1.0);
```

### Graph Proximity (Existing Formula)

Already implemented in Story 2.3's `GraphScopedSearch.ComputeProximityScore`:

```csharp
internal static double NormalizeGraphProximity(int hopDistance)
    => Math.Clamp(1.0 / (1.0 + hopDistance), 0.0, 1.0);
```

Hop 0 -> 1.0, Hop 1 -> 0.5, Hop 2 -> 0.333, Hop 3 -> 0.25. This formula is deterministic and documented (NFR24).

### Cosine Score Distribution Caveat

Cosine similarity passthrough assumes the embedding model produces well-distributed scores across [0.0, 1.0]. In practice, some models (including `text-embedding-004`) produce scores clustered in 0.7-0.95 for related content, compressing the effective range to ~0.25. This means cosine scores carry less discriminative power than their 0-1 range suggests. No change needed for Story 2.4 — the passthrough is correct. But Story 2.7 (Benchmarks) should evaluate whether cosine scores need range stretching (e.g., min-max within observed range) to improve fusion effectiveness.

### CorpusStatisticsActor Pattern

Follow the exact pattern of `TenantConfigurationActor` (same namespace, constructor shape, state management):

```csharp
internal sealed class CorpusStatisticsActor : Actor, ICorpusStatisticsActor
{
    private const string StateName = "corpusStats";
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CorpusStatisticsActor> _logger;

    public CorpusStatisticsActor(
        ActorHost host,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CorpusStatisticsActor> logger)
        : base(host) { ... }

    protected override async Task OnActivateAsync()
    {
        await RegisterTimerAsync(
            "RefreshCorpusStats",
            nameof(RefreshStatsCallbackAsync),
            null,
            dueTime: TimeSpan.Zero,      // Refresh immediately on activation
            period: TimeSpan.FromMinutes(5));
    }
}
```

**Getting corpus stats from RediSearch:**

Use raw `FT.INFO` command via `ExecuteAsync` (primary approach — avoids dependency on `NRedisStack.InfoResult` property names which vary across versions):

```csharp
IDatabase db = _redis.GetDatabase();
string indexName = $"{Id.GetId()}:memories:idx"; // Actor ID = tenant ID

try
{
    RedisResult raw = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);
    // FT.INFO returns a flat array of key-value pairs: [key1, val1, key2, val2, ...]
    RedisResult[] items = (RedisResult[])raw!;
    int docCount = 0;
    double docTableSizeMB = 0.0;

    for (int i = 0; i < items.Length - 1; i += 2)
    {
        string key = (string)items[i]!;
        if (key == "num_docs") docCount = (int)items[i + 1];
        else if (key == "doc_table_size_mb") docTableSizeMB = (double)items[i + 1];
    }

    double avgDocLen = docCount > 0
        ? (docTableSizeMB * 1024 * 1024) / docCount
        : 0.0;
}
catch (RedisServerException ex) when (ex.Message.Contains("Unknown Index name"))
{
    // Index doesn't exist yet — return zero stats
}
```

**Why raw parsing over `InfoResult`:** `NRedisStack.Search.InfoResult` property names may differ across NRedisStack versions, causing silent failures where properties return default values. Raw parsing against known Redis protocol field names (`num_docs`, `doc_table_size_mb`) is stable and version-independent.

**Extract parsing to pure function:** The FT.INFO parsing logic should be extracted to `internal static CorpusStatistics ParseFtInfoResult(RedisResult raw, DateTimeOffset refreshedAt)` — this makes it unit-testable without Redis. The flat key-value array structure (`[key1, val1, key2, val2, ...]`) may contain nested arrays for some fields; the parser should skip non-string keys gracefully rather than throwing `InvalidCastException`.

**Redis connection resilience:** Wrap `ExecuteAsync` in a try/catch for `RedisConnectionException` (transient network failures). On connection failure, log a warning and retain the previous cached state — do not overwrite with zeros or let the exception deactivate the actor.

**Average document length:** RediSearch does not directly expose average document length. Practical approaches:

1. Compute from `DocTableSizeMB / NumDocs` (bytes per document, rough proxy)
2. Sample a few documents and compute average `content.Length`
3. Track incrementally during ingestion (add to indexing activity)

For the saturation normalization, the exact average document length is not critical — it's a scaling factor. Approach 1 (bytes per doc from FT.INFO) is sufficient for MVP. If the value proves too coarse, it can be refined in later stories.

**Important clarification:** `averageDocumentLength` in this story represents **bytes per document from RediSearch index metadata** (via `DocTableSizeMB / NumDocs`), NOT character-count length. The saturation constant `k` uses this as a scaling factor — absolute precision is not required, only corpus-proportional scaling. Document this in code comments to prevent future confusion.

**State persistence requirement (D24):** The architecture mandates state persistence before every response. After computing stats in `RefreshStatsCallbackAsync`, call `StateManager.SetStateAsync` BEFORE the callback returns. In `GetDocumentCountAsync` / `GetAverageDocumentLengthAsync`, read from state — do NOT re-query Redis.

### DI Registration

Add to the existing `AddActors` block in `Program.cs`:

```csharp
builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<EmbeddingRateLimiterActor>();
    options.Actors.RegisterActor<TenantConfigurationActor>();
    options.Actors.RegisterActor<CorpusStatisticsActor>(); // NEW: Story 2.4
    options.ActorIdleTimeout = TimeSpan.FromMinutes(60);
    options.ActorScanInterval = TimeSpan.FromSeconds(30);
    options.ReentrancyConfig = new Dapr.Actors.ActorReentrancyConfig { Enabled = false };
});
```

The `CorpusStatisticsActor` needs `[FromKeyedServices("redis")] IConnectionMultiplexer` in its constructor. DAPR actor DI resolves keyed services automatically from the service provider.

### Refactoring GraphScopedSearch

`GraphScopedSearch.ComputeProximityScore` already implements the graph normalization formula. To avoid duplication:

```csharp
// GraphScopedSearch.cs — change body to delegate
internal static double ComputeProximityScore(int hopDistance)
    => ScoreNormalizer.NormalizeGraphProximity(hopDistance);
```

Existing `GraphScopedSearchTests` for `ComputeProximityScore` remain unchanged — same formula, same expected outputs. The tests validate behavior, not implementation location.

### Project Structure Notes

New files follow existing project conventions:

```
src/Hexalith.Memories.Server/
  Actors/
    ICorpusStatisticsActor.cs     # NEW — follows ITenantConfigurationActor pattern
    CorpusStatisticsActor.cs      # NEW — follows TenantConfigurationActor pattern
  Search/
    ScoreNormalizer.cs            # NEW — pure static functions

src/Hexalith.Memories.Server/
  Actors/
    CorpusStatistics.cs           # NEW — internal actor state record (not in Contracts — purely internal)

tests/Hexalith.Memories.Server.Tests/
  Search/
    ScoreNormalizerTests.cs       # NEW — follows SyntacticSearchServiceTests pattern
  Actors/
    CorpusStatisticsActorTests.cs # NEW
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
- **No mocking needed:** All normalizer tests are pure functions with direct assertions. For initial manual testing before the actor is wired up, a hardcoded k value (e.g., k=10) is acceptable — unit tests use explicit corpus stats parameters, not the actor
- **Actor tests:** Use DAPR test utilities to create actor instances with in-memory state if available; otherwise test the normalization logic separately and trust the actor framework

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.4 — lines 666-698]
- [Source: _bmad-output/planning-artifacts/prd.md#NFR24 — score normalization requirements]
- [Source: _bmad-output/planning-artifacts/architecture.md — DAPR actor patterns, D15 (actor ID = tenant), D24 (state persist before response)]
- [Source: src/Hexalith.Memories.Server/Actors/TenantConfigurationActor.cs — actor implementation pattern]
- [Source: src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs — existing ComputeProximityScore formula]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs — BM25 score return pattern]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs — cosine distance-to-similarity conversion]

### Previous Story Intelligence (Story 2.3)

From Story 2.3 implementation:

- `GraphScopedSearch.ComputeProximityScore` uses `1/(1+hopDistance)` — reuse this formula, do not change it
- `[FromKeyedServices("redis")]` and `[FromKeyedServices("falkordb")]` are the keyed DI patterns for Redis connections
- `sealed partial class` with `[LoggerMessage]` for structured logging
- Integration tests use `CompositeSearchFixture` with `Task.WhenAll` for parallel container startup
- FalkorDB integration: `NFalkorDB.FalkorDB` wrapping `IConnectionMultiplexer.GetDatabase()`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Build error: `CorpusStatistics` was `internal` but returned by `public` interface `ICorpusStatisticsActor.GetStatisticsAsync()` — fixed by making `CorpusStatistics` public (DAPR actor proxy requires public interfaces)
- Build error: `RedisResult.Type` is obsolete in StackExchange.Redis 2.12.4 — replaced with `Resp2Type`

### Completion Notes List

- Created `ScoreNormalizer` with three pure normalization functions: BM25 saturation, cosine passthrough, graph proximity decay
- Created `CorpusStatistics` record for per-tenant corpus stats (doc count, avg doc length, refresh timestamp)
- Created `ICorpusStatisticsActor` interface following `ITenantConfigurationActor` pattern
- Created `CorpusStatisticsActor` with timer-based FT.INFO refresh (5-minute interval), Redis connection resilience, and inline refresh on first access
- Extracted `ParseFtInfoResult` as a pure static function — testable without Redis, handles nested arrays gracefully via `Resp2Type` checks
- Registered `CorpusStatisticsActor` in `Program.cs` AddActors block
- Refactored `GraphScopedSearch.ComputeProximityScore` to delegate to `ScoreNormalizer.NormalizeGraphProximity` — centralizes formula, existing tests unaffected
- 23 unit tests for `ScoreNormalizer` covering all edge cases (NaN, Infinity, negatives, monotonicity, tiny corpora, range bounds, overflow safety)
- 9 unit tests for `CorpusStatisticsActor` covering state retrieval and `ParseFtInfoResult` parsing (valid, empty, null, nested arrays, NaN rejection, bounds checks)
- Full test suite: 336 tests, 0 failures, 0 regressions
- Review-fix validation: 32 targeted tests passed, 0 failures

### File List

New files:

- src/Hexalith.Memories.Server/Search/ScoreNormalizer.cs
- src/Hexalith.Memories.Server/Actors/CorpusStatistics.cs
- src/Hexalith.Memories.Server/Actors/ICorpusStatisticsActor.cs
- src/Hexalith.Memories.Server/Actors/CorpusStatisticsActor.cs
- tests/Hexalith.Memories.Server.Tests/Search/ScoreNormalizerTests.cs
- tests/Hexalith.Memories.Server.Tests/Actors/CorpusStatisticsActorTests.cs

Modified files:

- src/Hexalith.Memories.Server/Program.cs (added CorpusStatisticsActor registration)
- src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs (ComputeProximityScore delegates to ScoreNormalizer)

### Change Log

- 2026-04-01: Story 2.4 implemented — score normalization layer with ScoreNormalizer (3 pure functions), CorpusStatisticsActor (timer-based FT.INFO refresh), and 27 unit tests. GraphScopedSearch.ComputeProximityScore refactored to centralize formula. Full test suite: 336 passed, 0 regressions.
- 2026-04-01: Review fixes applied — cached actor reads now persist before return, BM25 normalization rejects non-finite corpus stats and preserves tiny-corpus scaling, FT.INFO parsing handles invalid numeric payloads more defensively, and targeted validation passed (32 tests).
