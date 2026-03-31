# Story 2.1: Syntactic Search (BM25 via RediSearch)

Status: ready-for-dev

## Story

As a developer,
I want to search memory units by text terms using BM25 ranking within a tenant,
So that I can find memory units that contain specific keywords or phrases.

## Acceptance Criteria

1. **Given** a tenant with indexed memory units containing varied content
   **When** I execute a syntactic search with query terms (e.g., "claim denied")
   **Then** results are returned ranked by BM25 relevance score
   **And** each result includes a content snippet, raw BM25 score, SourceUri, and SourceType (FR24)
   **And** results are scoped to the specified tenant only

2. **Given** a syntactic search is executed
   **When** results are returned
   **Then** p95 latency is <200ms at 10 concurrent queries/tenant with 10K memory units (NFR1)

3. **Given** a search query that matches no memory units
   **When** results are returned
   **Then** an empty result set is returned with zero results count
   **And** no error is thrown

4. **Given** a tenant with no indexed memory units
   **When** a syntactic search is executed
   **Then** an empty result set is returned with a clear indication that no memory units exist

## Tasks / Subtasks

- [ ] Task 1: Create search contracts in `Contracts/V1/` (AC: 1, 3, 4)
  - [ ] 1.1 Create `SearchQuery.cs` sealed record (TenantId, Query, CaseId?, MaxResults, Offset)
  - [ ] 1.2 Create `ScoredResult.cs` sealed record (MemoryUnitId, Score, ContentSnippet, SourceUri, SourceType, Axis?)
  - [ ] 1.3 Create `SearchResult.cs` sealed record (Results list, TotalCount, Query echo)
  - [ ] 1.4 Register all three types AND `IReadOnlyList<ScoredResult>` in `MemoriesJsonSourceGenerationContext`
  - [ ] 1.5 Add serialization round-trip tests for all new contracts

- [ ] Task 2: Implement `SyntacticSearchService` in `Server/Search/` (AC: 1, 2, 3, 4)
  - [ ] 2.1 Create `SyntacticSearchService.cs` using NRedisStack `ft.SearchAsync()` (async — do NOT use sync `ft.Search()`)
  - [ ] 2.2 Build FT.SEARCH query from `SearchQuery` input with tenant-scoped index
  - [ ] 2.3 Map RediSearch `Document` results to `ScoredResult` records (set `Axis = "syntactic"`)
  - [ ] 2.4 Add query input sanitization: escape RediSearch special characters in user query terms
  - [ ] 2.5 Handle missing index gracefully (empty result, not exception)
  - [ ] 2.6 Extract pure testable `internal static` methods: `MapDocumentToScoredResult`, `BuildQueryString`, `EscapeRedisQuery` (internal — tests access via `InternalsVisibleTo`). Inline the "Unknown Index name" check directly in `SearchAsync()` — it's a 3-line guard, not worth a separate method.
  - [ ] 2.7 Add unit tests for all extracted pure functions

- [ ] Task 3: Add REST search endpoint (AC: 1, 3, 4)
  - [ ] 3.1 Add `GET /api/search` endpoint in `Program.cs` accepting query parameters
  - [ ] 3.2 Validate required fields (tenantId, query), cap MaxResults at 100, return `ErrorResponse` for invalid input
  - [ ] 3.3 Wire `SyntacticSearchService` via DI (singleton)

- [ ] Task 4: Integration tests with real Redis Stack (AC: 1, 2, 3, 4)
  - [ ] 4.1 Create `SyntacticSearchIntegrationTests` in IntegrationTests project
  - [ ] 4.2 Seed test data via `IndexSyntacticActivity` (reuse existing indexing infrastructure)
  - [ ] 4.3 Test BM25 ranking: multi-term query returns results ordered by relevance
  - [ ] 4.4 Test empty results: query with no matches returns empty set
  - [ ] 4.5 Test tenant isolation: query for tenant A does not return tenant B results
  - [ ] 4.6 Test missing index: query against non-existent tenant returns empty set
  - [ ] 4.7 Test special characters: query with hyphens/parentheses does not throw parse error
  - [ ] 4.8 Test offset pagination: query with offset=10, verify results start from 11th document
  - [ ] 4.9 Test broad-match query: single common word matching many docs returns capped results correctly
  - [ ] 4.10 Latency smoke test: 10 concurrent queries against seeded 100+ docs, assert <200ms p95 — mark with `[Trait("Category", "Performance")]` to allow separate CI scheduling
  - [ ] 4.11 Test query injection prevention: user input containing `@sourceType:{file}` is escaped and does NOT act as field filter
  - [ ] 4.12 Test CaseId injection prevention: caseId containing `} @content:{secret` does not inject a content filter

## Dev Notes

### Implementation Overview

This story adds the **first search capability** to the project. You are building:
1. Three new V1 contracts (`SearchQuery`, `ScoredResult`, `SearchResult`)
2. One service (`SyntacticSearchService`) that executes RediSearch `FT.SEARCH` queries
3. One REST endpoint (`GET /api/search`)

The RediSearch indexes and hash data already exist from Story 1.5's `IndexSyntacticActivity` — you are **only reading** from them, not writing.

### Request-to-Response Flow

```
1. HTTP GET /api/search?tenantId=...&query=...
2. Endpoint validates tenantId (TenantIdGuard) + query (not empty), clamps MaxResults
3. SyntacticSearchService.SearchAsync(SearchQuery) called
   3a. EscapeRedisQuery(query) — neutralize special chars + pipe
   3b. BuildQueryString(escapedTerms, caseId?) — add @caseId TAG filter if present
   3c. Build Query object: WithScores(), Limit(), Dialect(2), ReturnFields()
   3d. ft.SearchAsync("{tenantId}:memories:idx", query)
   3e. Catch RedisServerException "Unknown Index name" → return empty SearchResult
   3f. For each Document: MapDocumentToScoredResult() — parse ID, fields, truncate content
4. Return SearchResult (results list, totalCount, query echo)
5. Endpoint returns Results.Ok(searchResult) → JSON via MemoriesJsonContext.Options
```

### SyntacticSearchService Method Signature

```csharp
public sealed class SyntacticSearchService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SyntacticSearchService> _logger;

    public SyntacticSearchService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<SyntacticSearchService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query) { ... }

    internal static ScoredResult MapDocumentToScoredResult(Document doc, string tenantId) { ... }
    internal static string BuildQueryString(string searchTerms, string? caseId) { ... }
    internal static string EscapeRedisQuery(string input) { ... }
}
```

### Architecture: Where Search Lives

Per architecture decision D9, search code goes in `Server/Search/` namespace `Hexalith.Memories.Server.Search`:

```
src/Hexalith.Memories.Server/
  Search/
    SyntacticSearchService.cs    # NEW — FT.SEARCH executor
src/Hexalith.Memories.Contracts/V1/
    SearchQuery.cs               # NEW — search input
    ScoredResult.cs              # NEW — single ranked result
    SearchResult.cs              # NEW — search response envelope
```

The `Server/Search/` folder does **not exist yet** — this story creates it. This is the first file in this namespace.

**Future story context (do NOT build now):** Stories 2.2-2.6 will add `SemanticSearchService`, `GraphScopedSearch`, `SearchService` (orchestrator), `Bm25Normalizer`, and `FusionAlgorithm`. Design contracts and service to be composable for future fusion — but do NOT build fusion infrastructure now (D9: no premature abstractions).

**Known refactoring point:** The architecture doc targets a `Redis/Syntactic/RediSearchQueryExecutor.cs` in a separate `Hexalith.Memories.Redis` project. For Story 2.1, keep everything in `Server/Search/` per D9. Story 2.5 (fusion) is the natural extraction point.

**Endpoint evolution:** `/api/search` is designed to be forward-compatible. Story 2.5 will add `?axes=syntactic,semantic,graph` parameter. Do NOT create `/api/search/syntactic` — keep one unified search endpoint.

### RediSearch Index Schema (Already Exists)

`IndexSyntacticActivity` (Story 1.5, file: `src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs`) creates:

- **Index name:** `{tenantId}:memories:idx`
- **Hash key pattern:** `{tenantId}:mu:{memoryUnitId}`
- **Index type:** HASH with prefix `{tenantId}:mu:`

**Indexed TEXT fields (searchable via FT.SEARCH):**
| Field | Weight | Content |
|-------|--------|---------|
| `content` | 1.0 | Main content body |
| `sourceUriText` | 0.25 | Source URI as searchable text |
| `sourceTypeText` | 0.25 | Source type as searchable text |
| `metadataText` | 0.25 | Flattened metadata key/value pairs |

**TAG fields (exact match/filter):**
- `sourceUri` — exact match filter
- `sourceType` — exact match filter
- `contentHash` — dedup lookup
- `caseId` — case scoping filter
- `embeddingProvider` — provider filter

**Non-indexed stored fields:** `metadataJson`, `ingestedBy`, `ingestedAt`, `lastUpdated`

[Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs, lines 46-65]

### NRedisStack FT.SEARCH API (v1.3.0)

Use `db.FT()` which returns `SearchCommands` (extends `SearchCommandsAsync`). **Always use `SearchAsync()` (async)** — synchronous `Search()` blocks thread pool threads and will violate NFR1 under concurrency.

```csharp
IDatabase db = _redis.GetDatabase();
SearchCommands ft = db.FT();
var query = new Query(queryString)
    .WithScores()              // Required for BM25 scores — each Document gets .Score property
    .Limit(offset, maxResults)
    .Dialect(2)                // Explicit dialect 2 for consistent special-char handling
    .ReturnFields("content", "sourceUri", "sourceType", "metadataJson", "ingestedBy", "ingestedAt");
RedisSearchResult result = await ft.SearchAsync($"{tenantId}:memories:idx", query);
```

**Note:** Loading the `content` field is intentional — it's needed for `ContentSnippet` truncation. This is bounded by `MaxResults` (max 100 documents), not the full corpus.

**Critical implementation details:**

1. **Namespace collision:** NRedisStack has its own `NRedisStack.Search.SearchResult`. Use alias:
   ```csharp
   using RedisSearchResult = NRedisStack.Search.SearchResult;
   ```
   Our contract is `Hexalith.Memories.Contracts.V1.SearchResult`.

2. **BM25 scores:** Call `query.WithScores()` BEFORE executing search. Each `Document` then gets a `.Score` property (double). Typical range: ~0.5 to ~25.0 for matching documents. Non-matching docs don't appear at all.

3. **Document ID parsing:** `Document.Id` is the full hash key (e.g., `"tenant1:mu:abc123"`). Extract MemoryUnitId by stripping the known prefix. No fallback needed — every document from the index has prefix `{tenantId}:mu:` by definition (the index is created with that prefix filter):
   ```csharp
   string prefix = $"{tenantId}:mu:";
   string memoryUnitId = document.Id[prefix.Length..];
   ```

4. **Field access:** `document.GetProperties()` returns key-value pairs. Access via `document["fieldName"]` which returns `RedisValue`.

5. **Missing index:** Throws `RedisServerException` containing `"Unknown Index name"`. Catch and return empty result — an empty/new tenant is valid.

6. **Case-scoped TAG filter:** TAG values with special characters (hyphens, dots) must use curly brace syntax. **Critical: escape the caseId value** before embedding to prevent TAG filter injection (e.g., a caseId of `"} @content:{secret"` would break the query structure):
   ```csharp
   string escapedCaseId = EscapeRedisQuery(caseId);
   string queryText = caseId is not null
       ? $"@caseId:{{{escapedCaseId}}} {escapedSearchTerms}"
       : escapedSearchTerms;
   ```

7. **Query tokenization:** Pass the escaped query string directly to RediSearch — it handles tokenization, stemming, and stop-word removal internally. Do NOT split the query into terms yourself.

8. **Special characters in queries:** RediSearch dialect 2 treats `-`, `(`, `)`, `@`, `!`, `{`, `}`, `|` as syntax. `|` is the TAG OR operator — unescaped pipes in caseId would break case isolation. Escape all:
   ```csharp
   static string EscapeRedisQuery(string input)
       => Regex.Replace(input, @"[-@!{}()\[\]^~*?:\\\"'|]", @"\$0");
   ```

9. **SourceType parsing from hash:** Hash stores `sourceType` as camelCase string (e.g., `"file"`). Parse:
   ```csharp
   Enum.TryParse<SourceType>(value, ignoreCase: true, out var result)
   ```
   Fallback to `SourceType.File` for unrecognized values (forward compatibility). `File` is chosen as the safest default because it's the most common source type and has no special processing implications. Do NOT throw — new source types added in future stories must not break existing search.

10. **ContentSnippet truncation:** Hash `content` field stores full text. Truncate to nearest space before 200 chars, append `"..."` if truncated. Truncate at word boundary (avoids UTF-8 issues).

11. **Axis field:** Set `Axis = "syntactic"` on all results. Stories 2.2-2.3 set `"semantic"` and `"graph"`. Story 2.5 uses this for score origins.

12. **Stale index entries:** RediSearch doesn't auto-remove entries when hash is deleted. `Document.GetProperties()` may return null/empty fields. Check for required fields (`content`, `sourceUri`, `sourceType`) — skip documents with missing required fields rather than crashing. Log a warning for skipped stale entries.

### Contract Design

Follow existing patterns established in Epic 1 contracts (see `ErrorResponse.cs`, `IndexInput.cs`, `MemoryUnit.cs`):
- `sealed record` with `required` properties (required fields first, nullable last)
- No external NuGet dependencies in Contracts project
- XML doc summaries on public types
- Register in `MemoriesJsonSourceGenerationContext` (file: `src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs`)
- Enum serialization as camelCase strings via `CamelCaseStringEnumConverter<T>`
- Copyright header: `// <copyright file="..." company="ITANEO">` — use on all new files (matches `MemoriesJsonContext.cs`, `IngestionInput.cs`, `TenantEmbeddingConfig.cs`)
- **Source-gen registration:** When adding `SearchResult` with `IReadOnlyList<ScoredResult>`, ALSO register `[JsonSerializable(typeof(IReadOnlyList<ScoredResult>))]` in `MemoriesJsonSourceGenerationContext`. Without this, the generic collection falls through to reflection-based serialization (works in dev but breaks AOT/trimming). Follow the pattern of `[JsonSerializable(typeof(Dictionary<string, MetadataField>))]` already in the context.

**SearchQuery:**
```csharp
public sealed record SearchQuery
{
    public required string TenantId { get; init; }
    public required string Query { get; init; }
    public string? CaseId { get; init; }
    public int MaxResults { get; init; } = 10;
    public int Offset { get; init; } = 0;
}
```

**ScoredResult** (reused by all axes in future stories):
```csharp
public sealed record ScoredResult
{
    public required string MemoryUnitId { get; init; }
    public required double Score { get; init; }
    public required string ContentSnippet { get; init; }
    public required string SourceUri { get; init; }
    public required SourceType SourceType { get; init; }
    public string? Axis { get; init; }
}
```

**SearchResult:**
```csharp
public sealed record SearchResult
{
    public required IReadOnlyList<ScoredResult> Results { get; init; }
    public required long TotalCount { get; init; }
    public required string Query { get; init; }
}
```

### REST Endpoint Pattern

Follow existing minimal API pattern from `Program.cs` (lines 91-108). No controllers — use `app.MapGet()`:

```csharp
app.MapGet("/api/search", async (
    SyntacticSearchService searchService,
    [FromQuery] string tenantId,
    [FromQuery] string query,
    [FromQuery] string? caseId,
    [FromQuery] int maxResults = 10,
    [FromQuery] int offset = 0) =>
{
    // Validate required params
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT",
            "Both 'tenantId' and 'query' are required.",
            "Provide tenantId and query as query parameters."));
    }

    // Validate tenantId format
    try { TenantIdGuard.Validate(tenantId); }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new ErrorResponse("INVALID_TENANT_ID", ex.Message,
            "TenantId must contain only alphanumeric characters and hyphens."));
    }

    int clampedMaxResults = Math.Clamp(maxResults, 1, 100);
    int clampedOffset = Math.Max(offset, 0);

    var searchResult = await searchService.SearchAsync(
        new SearchQuery
        {
            TenantId = tenantId,
            Query = query,
            CaseId = caseId,
            MaxResults = clampedMaxResults,
            Offset = clampedOffset,
        });

    return Results.Ok(searchResult);
    // Note: RedisServerException for "Unknown Index name" is handled inside SearchAsync()
    // (returns empty result). All other Redis exceptions propagate as 500 — correct for
    // infrastructure failures (OOM, replication, connection). Do NOT catch broadly here.
});
```

Register `SyntacticSearchService` as **singleton** in DI — it only holds an `IConnectionMultiplexer` reference (same as indexing activities). Inject Redis via keyed services:

```csharp
builder.Services.AddSingleton<SyntacticSearchService>(sp =>
    new SyntacticSearchService(
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
        sp.GetRequiredService<ILogger<SyntacticSearchService>>()));
```

### Tenant Isolation — Critical

All queries MUST be scoped to the tenant's index. The index name `{tenantId}:memories:idx` enforces isolation at the RediSearch level — each tenant has a physically separate index. Validate `tenantId` with `TenantIdGuard.Validate()` before querying (same pattern as `IndexSyntacticActivity.cs`, line 37).

**Note:** `TenantIdGuard` is declared `internal static partial class` in namespace `Hexalith.Memories.Server.Activities.Indexing`. It IS accessible from `Server.Search` because both are in the same assembly (`Hexalith.Memories.Server`). Requires `using Hexalith.Memories.Server.Activities.Indexing;`.

[Source: src/Hexalith.Memories.Server/Activities/Indexing/TenantIdGuard.cs]

### Error Handling

- **Missing index** (tenant never had data indexed): Catch `RedisServerException` containing `"Unknown Index name"`, return empty `SearchResult` with `TotalCount = 0`. Do NOT throw — an empty tenant is valid.
- **Invalid query syntax / other Redis server errors**: Let `RedisServerException` propagate as 500 from the endpoint. Do NOT catch broadly — a broad catch masks infrastructure failures (OOM, replication errors) as query errors. The "Unknown Index name" case is the only `RedisServerException` handled internally by `SearchAsync()`.
- **Redis connection failure** (`RedisConnectionException`, `RedisTimeoutException`): Let exception propagate (infrastructure failure, retriable by caller).
- **Stale index entries:** Skip documents with missing required fields (`content`, `sourceUri`, `sourceType`) rather than crashing. Log warning. **Important:** `document["fieldName"]` returns `RedisValue` (a struct, never null). Check via `.IsNullOrEmpty` property, NOT C# null check.

### Testing Strategy

**Test framework:** xUnit `[Fact]` + Shouldly assertions (`result.ShouldBe(expected)`, `Should.Throw<T>()`) + NSubstitute for mocks (`Substitute.For<T>()`). Do NOT use FluentAssertions or Moq.

**Tier 1 — Contracts.Tests (unit, no dependencies):**
- File pattern: `tests/Hexalith.Memories.Contracts.Tests/V1/{Type}SerializationTests.cs`
- Follow exact pattern from `ErrorResponseSerializationTests.cs`: `RoundTrip_ShouldProduceIdenticalObject()` and `PropertyNames_ShouldBeCamelCase()`
- Serialize/deserialize via `MemoriesJsonContext.Options`
- Test `SearchQuery` default values (MaxResults=10, Offset=0)
- Test `ScoredResult` with nullable `Axis` field (null serializes correctly)
- Test `SearchResult` with empty `Results` list

**Tier 2 — Server.Tests (unit, pure logic focus):**
- File: `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`
- Extract testable **static pure functions** from `SyntacticSearchService`:
  - `MapDocumentToScoredResult(Document doc, string tenantId)` — hash key parsing, field extraction, SourceType parsing, content truncation, Axis tagging
  - `BuildQueryString(string searchTerms, string? caseId)` — case-scoped TAG filter construction, CaseId escaping
  - `EscapeRedisQuery(string input)` — RediSearch special character escaping
- The "Unknown Index name" check is inlined in `SearchAsync()` (3-line guard, not worth extracting)
- Test cases:
  - MemoryUnitId extraction: `"tenant1:mu:abc123"` -> `"abc123"` (prefix strip, no fallback)
  - SourceType parsing: `"file"` -> `SourceType.File`, `"event"` -> `SourceType.Event` (case-insensitive)
  - SourceType fallback: unknown value -> `SourceType.File`
  - ContentSnippet truncation: 500-char input -> 200-char output at word boundary
  - ContentSnippet short: 100-char input -> unchanged, no `"..."`
  - Empty results: zero documents -> empty list with TotalCount=0
  - Missing index: `SearchAsync()` with non-existent tenant -> empty result (inlined "Unknown Index name" guard)
  - BM25 score positive: matching document has Score > 0.0
  - Case-scoped query: with caseId -> `"@caseId:{case\-1} escaped terms"`, without -> `"escaped terms"`
  - CaseId injection prevention: caseId `"} @content:{secret"` -> escaped before TAG embedding, does NOT inject content filter
  - Query escaping: `"claim-denied"` -> `"claim\-denied"`, `"@admin"` -> `"\@admin"`
  - All-special-char query: `"---"` after escaping -> return empty results (not error)
  - Query injection prevention: input `"@sourceType:{file}"` -> escaped to `"\@sourceType\:\{file\}"`, does NOT act as field filter
  - Null/missing field handling: document with `sourceType.IsNullOrEmpty` -> skipped, not crash
  - Missing content field: document with `content.IsNullOrEmpty` -> skipped (most likely stale entry scenario)
- **Do NOT mock `IConnectionMultiplexer` -> `IDatabase` -> `SearchCommands` chain** — that's brittle. Tier 3 covers it.

**Tier 3 — IntegrationTests (real Redis Stack via Testcontainers):**
- File: `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`
- Use `[Collection("RedisStack")]` with existing `RedisStackFixture` (file: `tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs`)
- Seed data by constructing `IndexSyntacticActivity` with the fixture's `Connection` and calling `RunAsync()` directly (same pattern as `IndexSyntacticIntegrationTests.cs`)
- Use `IndexInputFactory.Create()` from `TestHelpers` project for test data
- Test cases:
  - BM25 ranking: seed 3+ docs with varied relevance, assert ordering
  - Tenant isolation: seed identical docs under two unique tenant IDs, query one, assert zero cross-leak
  - Case scoping: seed docs with different `caseId`, query with `@caseId` TAG filter, assert filtering
  - Empty results: query with gibberish term against seeded docs, assert empty
  - Missing index: query against never-seeded tenant, assert empty (not exception)
  - Special characters: query with hyphens, parentheses doesn't throw
  - Offset pagination: seed 15+ docs, query with offset=10, maxResults=5, assert correct skip
  - Broad-match: seed many docs with common word, assert MaxResults cap respected
  - Latency smoke: 10 concurrent queries via `Task.WhenAll()`, assert p95 <200ms

### Performance Considerations (NFR1: <200ms p95)

- RediSearch FT.SEARCH on 10K documents is inherently fast (<50ms typical)
- Use `ReturnFields()` to select only needed fields — do NOT load full content unnecessarily
- Default result size: 10 (configurable up to 100 via MaxResults)
- No additional processing needed for raw BM25 results (normalization is Story 2.4)

### What NOT To Build

- **Score normalization** — Story 2.4 handles BM25 -> 0.0-1.0 normalization. Return raw BM25 scores.
- **Fusion/hybrid search** — Story 2.5. This story is syntactic-only.
- **Explain mode** — Story 2.6. No per-axis breakdown yet.
- **Pagination tokens** — Use simple offset/limit for now.
- **Semantic or graph search** — Stories 2.2, 2.3. Completely separate.
- **CorpusStatisticsActor** — Story 2.4. Not needed for raw BM25 results.
- **No new interfaces** — D9: concrete `SyntacticSearchService` class, no `ISyntacticSearchService` until a second implementation exists.
- **Tenant authorization** — Epic 5. MVP relies on trusted callers. `TenantIdGuard.Validate()` checks format only, not permission.
- **SummarizeFields()** — While NRedisStack `Query.SummarizeFields()` could provide server-side snippets, manual truncation is simpler and predictable. Consider upgrading in a future story if snippet quality matters.

### Project Structure Notes

- **New folder:** `src/Hexalith.Memories.Server/Search/` — first file in this namespace
- **Contracts:** `src/Hexalith.Memories.Contracts/V1/` alongside existing records
- **Server tests:** `tests/Hexalith.Memories.Server.Tests/Search/SyntacticSearchServiceTests.cs`
- **Contract tests:** `tests/Hexalith.Memories.Contracts.Tests/V1/SearchQuerySerializationTests.cs`, `ScoredResultSerializationTests.cs`, `SearchResultSerializationTests.cs`
- **Integration tests:** `tests/Hexalith.Memories.IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`

### Code Conventions (from existing codebase)

- File-scoped namespaces, Allman braces, 4-space indent
- Copyright header: `// <copyright file="FileName.cs" company="ITANEO">`
- `sealed record` with `required init` for mandatory fields
- `field ??= []` for collection initialization
- Async suffix on async methods
- `_camelCase` for private fields
- Nullable enabled globally, warnings as errors
- Activities: no exception catching, no CancellationToken (propagate to workflow)
- JSON: `MemoriesJsonContext.Options` for all serialization

### Previous Story Intelligence

**From Story 1.5 (Three-Backend Indexing) — DONE:**
- `[FromKeyedServices("redis")] IConnectionMultiplexer redis` pattern for Redis injection
- `TenantIdGuard.Validate()` for tenant ID validation before any Redis operation
- Index creation is idempotent (catches "Index already exists" exception)
- `NRedisStack.Search` namespace for all FT.* operations
- `NRedisStack.RedisStackCommands` for `db.FT()` extension method
- Hash field names: `content`, `sourceUri`, `sourceUriText`, `sourceType`, `sourceTypeText`, `metadataText`, `metadataJson`, `contentHash`, `caseId`, `embeddingProvider`, `ingestedBy`, `ingestedAt`, `lastUpdated`

**From Story 1.6 (Ingestion Workflow) — DONE:**
- REST endpoints use minimal API pattern in `Program.cs` (no controllers)
- Validation returns `ErrorResponse(Code, Message, Suggestion)` for 400 responses
- `Results.Ok()`, `Results.BadRequest()`, `Results.NotFound()` for responses
- `ValidateIngestionRequest()` wraps validation in try/catch -> ErrorResponse pattern
- JSON options configured via `builder.Services.ConfigureHttpJsonOptions()` with `MemoriesJsonContext.Options`

**From Story 1.7 (Embedding Provider Configuration) — READY-FOR-DEV (not yet implemented):**
- `TenantEmbeddingConfig` contract already exists in Contracts/V1
- `EmbeddingClient` is currently registered as typed HttpClient (`AddHttpClient<EmbeddingClient>()`) — Story 1.7 will change to singleton
- This has NO impact on Story 2.1 — search service uses `IConnectionMultiplexer` directly, not `EmbeddingClient`

### Git Intelligence

Recent commits show stable indexing infrastructure:
- `5621fe9` Merge PR #6: Ingestion workflow orchestration (Story 1.6)
- `f1ae9d6` feat: implement ingestion workflow orchestration
- `2253f09` Merge PR #5: Three-backend indexing (Story 1.5)
- `b1db3e9` Unit tests for activities and workflows

All indexing infrastructure is merged to main. Search builds on top of the existing index schema without modifications.

### Dependencies & Imports

The `SyntacticSearchService` needs these imports (all already available in the Server project):
```csharp
using NRedisStack.RedisStackCommands;    // db.FT() extension
using NRedisStack.Search;                 // Query, SearchResult (alias this)
using StackExchange.Redis;                // IConnectionMultiplexer, IDatabase, RedisServerException
using Hexalith.Memories.Contracts.V1;     // SearchQuery, ScoredResult, SearchResult, SourceType
using Hexalith.Memories.Server.Activities.Indexing; // TenantIdGuard
```

NRedisStack 1.3.0 and StackExchange.Redis 2.12.4 are already in `Directory.Packages.props`. No new NuGet packages needed.

### Known Deferred Issues (from deferred-work.md)

- **CaseId not validated for special characters** — TenantId has strict regex, CaseId only checks null/empty. For search, CaseId is used as TAG filter value inside curly braces. This is safe for now but could cause query parse errors with certain special characters in CaseId. Accept for MVP.

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 2, Story 2.1, lines 588-613]
- [Source: _bmad-output/planning-artifacts/architecture.md — D9 (no premature abstractions), lines 555, 638]
- [Source: _bmad-output/planning-artifacts/architecture.md — Search Architecture, lines 584, 1256-1262]
- [Source: _bmad-output/planning-artifacts/architecture.md — Deployment Topology, NRedisStack 1.3.0, line 446]
- [Source: _bmad-output/planning-artifacts/prd.md — FR14, FR20, FR24, NFR1]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs — Index schema, lines 46-88]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/TenantIdGuard.cs — Tenant validation]
- [Source: src/Hexalith.Memories.Server/Program.cs — DI, endpoints, JSON config, lines 40-108]
- [Source: src/Hexalith.Memories.Contracts/V1/MemoriesJsonContext.cs — Source-gen registration]
- [Source: src/Hexalith.Memories.Contracts/V1/ErrorResponse.cs — Error response pattern]
- [Source: src/Hexalith.Memories.Contracts/V1/SourceType.cs — Enum with CamelCaseStringEnumConverter]
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/RedisStackFixture.cs — Testcontainers pattern]
- [Source: tests/Hexalith.Memories.IntegrationTests/Indexing/IndexSyntacticIntegrationTests.cs — Integration test pattern]
- [Source: tests/Hexalith.Memories.Contracts.Tests/V1/ErrorResponseSerializationTests.cs — Serialization test pattern]
- [Source: NRedisStack 1.3.0 docs — FT.SEARCH query API, Query class, WithScores()]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
