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
   **And** each result includes the memory unit summary, raw BM25 score, SourceUri, and SourceType (FR24)
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
  - [ ] 1.4 Register new types in `MemoriesJsonContext`
  - [ ] 1.5 Add serialization round-trip tests for all new contracts

- [ ] Task 2: Implement `SyntacticSearchService` in `Server/Search/` (AC: 1, 2, 3, 4)
  - [ ] 2.1 Create `SyntacticSearchService.cs` using NRedisStack `FT().SearchAsync()`
  - [ ] 2.2 Build FT.SEARCH query from `SearchQuery` input with tenant-scoped index
  - [ ] 2.3 Map `SearchResult` documents back to `ScoredResult` records (set `Axis = "syntactic"`)
  - [ ] 2.4a Add query input sanitization: escape RediSearch special characters in user query terms
  - [ ] 2.4 Handle missing index gracefully (empty result, not exception)
  - [ ] 2.5 Extract pure testable methods: `MapDocumentToScoredResult`, `HandleSearchException`, `BuildQueryString`
  - [ ] 2.6 Add unit tests for extracted pure functions (no Redis mocking needed)

- [ ] Task 3: Add REST search endpoint (AC: 1, 3, 4)
  - [ ] 3.1 Add `GET /api/search` endpoint in `Program.cs` accepting query parameters
  - [ ] 3.2 Validate required fields (tenantId, query), cap MaxResults at 100 (`Math.Clamp(maxResults, 1, 100)`), return `ErrorResponse` for invalid input
  - [ ] 3.3 Wire `SyntacticSearchService` via DI

- [ ] Task 4: Integration tests with Aspire (AC: 1, 2, 3, 4)
  - [ ] 4.1 Create `SyntacticSearchIntegrationTests` in IntegrationTests project
  - [ ] 4.2 Seed test data via `IndexSyntacticActivity` (reuse existing indexing)
  - [ ] 4.3 Test BM25 ranking: multi-term query returns results ordered by relevance
  - [ ] 4.4 Test empty results: query with no matches returns empty set
  - [ ] 4.5 Test tenant isolation: query for tenant A does not return tenant B results
  - [ ] 4.6 Test missing index: query against non-existent tenant returns empty set
  - [ ] 4.7 Test special characters: query with hyphens/parentheses does not throw parse error
  - [ ] 4.8 Test offset pagination: query with offset=10, verify results start from 11th document
  - [ ] 4.9 Test broad-match query: single common word matching many docs returns capped results correctly
  - [ ] 4.10 Latency smoke test: 10 concurrent queries against seeded 100+ docs, assert <200ms p95

## Dev Notes

### Implementation Overview

This story adds the first search capability to the project. You are building: (1) three new V1 contracts (`SearchQuery`, `ScoredResult`, `SearchResult`), (2) one service (`SyntacticSearchService`) that executes RediSearch `FT.SEARCH` queries against the tenant's existing index, and (3) one REST endpoint (`GET /api/search`). The RediSearch indexes and hash data already exist from Story 1.5's ingestion pipeline — you are only reading from them, not writing.

### Architecture: Where Search Lives

Per architecture decision, search code goes in `Server/Search/` namespace `Hexalith.Memories.Server.Search`:

```
src/Hexalith.Memories.Server/
  Search/
    SyntacticSearchService.cs    # NEW — FT.SEARCH executor
src/Hexalith.Memories.Contracts/V1/
    SearchQuery.cs               # NEW — search input
    ScoredResult.cs              # NEW — single ranked result
    SearchResult.cs              # NEW — search response envelope
```

This is the **first search story** in the project. Stories 2.2-2.6 will add `SemanticSearchService`, `GraphScopedSearch`, `SearchService` (orchestrator), `Bm25Normalizer`, and `FusionAlgorithm`. Design contracts and service interface to be composable for future fusion — but do NOT build fusion infrastructure now (Decision D9: no premature abstractions).

**Known refactoring point:** The architecture doc targets a `Redis/Syntactic/RediSearchQueryExecutor.cs` in a separate `Hexalith.Memories.Redis` project. For Story 2.1, keep everything in `Server/Search/` per D9. When Story 2.5 (fusion) needs to orchestrate multiple executors, that is the natural extraction point — extract `SyntacticSearchService` → `RediSearchQueryExecutor` at that time.

**Endpoint evolution:** `/api/search` is designed to be forward-compatible. Story 2.5 will add `?axes=syntactic,semantic,graph` parameter to this same endpoint. Do NOT create `/api/search/syntactic` — keep one unified search endpoint.

### RediSearch Index Already Exists

`IndexSyntacticActivity` (Story 1.5) creates the RediSearch index and populates hashes:
- **Index name:** `{tenantId}:memories:idx`
- **Hash key pattern:** `{tenantId}:mu:{memoryUnitId}`
- **Schema fields (searchable):**
  - `content` (TEXT, weight 1.0) — main content body
  - `sourceUriText` (TEXT, weight 0.25) — source URI as searchable text
  - `sourceTypeText` (TEXT, weight 0.25) — source type as searchable text
  - `metadataText` (TEXT, weight 0.25) — flattened metadata key/value pairs
  - `sourceUri` (TAG) — exact match filter
  - `sourceType` (TAG) — exact match filter
  - `contentHash` (TAG) — dedup lookup
  - `caseId` (TAG) — case scoping filter
  - `embeddingProvider` (TAG) — provider filter
- **Non-indexed stored fields:** `metadataJson`, `ingestedBy`, `ingestedAt`, `lastUpdated`

[Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs]

### NRedisStack FT.SEARCH API

Use the synchronous `ft.Search()` or async pattern via `IDatabase`. NRedisStack 1.3.0 uses **query dialect 2** by default.

```csharp
IDatabase db = _redis.GetDatabase();
var ft = db.FT();
var query = new Query("claim denied")
    .Limit(0, maxResults)
    .SetSortBy("__score", ascending: false)  // BM25 score
    .ReturnFields("content", "sourceUri", "sourceType", "metadataJson", "ingestedBy", "ingestedAt");
SearchResult result = ft.Search($"{tenantId}:memories:idx", query);
```

Key points:
- `ft.Search()` returns `NRedisStack.Search.SearchResult` with `.TotalResults` (long) and `.Documents` collection — **namespace collision warning:** NRedisStack's `SearchResult` will collide with our `Hexalith.Memories.Contracts.V1.SearchResult`. Use a `using` alias in `SyntacticSearchService.cs`: `using RedisSearchResult = NRedisStack.Search.SearchResult;`
- Each `Document` has `.Id` (the full hash key, e.g., `{tenantId}:mu:{memoryUnitId}`) and `.GetProperties()` for field access. **Extract MemoryUnitId** by stripping the prefix: `document.Id[(document.Id.LastIndexOf(':') + 1)..]`
- BM25 score requires **calling `query.WithScores()` before executing the search** — each document then gets a `.Score` property (double)
- **Async availability:** Verify `ft.SearchAsync()` exists in NRedisStack 1.3.0. If not available, wrap the sync call: `await Task.Run(() => ft.Search(indexName, query))`
- **Missing index throws `RedisServerException`** — catch and return empty result
- **Case-scoped TAG filter:** TAG values with special characters (hyphens, dots) must be escaped. Use the query string syntax with curly braces:
  ```csharp
  // Case-scoped query example:
  string queryText = caseId is not null
      ? $"@caseId:{{{caseId}}} {searchTerms}"
      : searchTerms;
  var query = new Query(queryText).WithScores().Limit(offset, maxResults);
  ```
- **Special characters in queries:** RediSearch query dialect 2 treats `-`, `(`, `)`, `@`, `!`, `{`, `}` as syntax. Two approaches:
  - **Escaping (recommended):** Preserves RediSearch stemming and fuzzy matching. Provide an `EscapeRedisQuery()` helper:
    ```csharp
    static string EscapeRedisQuery(string input)
        => Regex.Replace(input, @"[-@!{}()\[\]^~*?:\\\"']", @"\$0");
    ```
  - **Quoting:** Wrapping terms in double quotes forces exact literal matching (no stemming). Simpler but loses BM25 relevance benefits. Not recommended for this story.
- **SourceType parsing from hash:** The hash stores `sourceType` as camelCase string (e.g., `"file"`). In `MapDocumentToScoredResult`, parse via `Enum.TryParse<SourceType>(value, ignoreCase: true, out var result)` with fallback to `SourceType.File` for unrecognized values — do NOT use the JSON converter path for direct field mapping, and do NOT throw on unrecognized values (forward compatibility)
- **ContentSnippet truncation:** The `content` hash field stores full document text (potentially thousands of chars). Truncate to the nearest space before 200 characters in `MapDocumentToScoredResult`, append `"..."` only if truncated. Ensure UTF-8 safe truncation — do not cut in the middle of a multi-byte character (truncate at word boundary avoids this). Alternatively, explore RediSearch `Query.SummarizeFields()` for server-side query-relevant snippets (less data over the wire — check if NRedisStack 1.3.0 supports it)
- **BM25 raw score range:** Typical BM25 scores for matching documents range from ~0.5 to ~25.0 depending on corpus size, term frequency, and document length. Use this for test assertions: matching docs should have `Score > 0.0`; non-matching docs won't appear in results at all
- **Axis field:** Set `Axis = "syntactic"` on all results from this service. Stories 2.2-2.3 will set `"semantic"` and `"graph"` respectively. Story 2.5 (fusion) uses this field to identify score origins

### Contract Design Principles

Follow existing patterns from Epic 1 contracts:
- `sealed record` with `required` properties (required fields first, nullable fields last)
- No external NuGet dependencies in Contracts project
- XML doc summaries on all public types
- Register in `MemoriesJsonContext` source generator
- Enum serialization as camelCase strings via `CamelCaseStringEnumConverter<T>`

**SearchQuery contract:**
```csharp
public sealed record SearchQuery
{
    public required string TenantId { get; init; }
    public required string Query { get; init; }
    public string? CaseId { get; init; }
    public int MaxResults { get; init; } = 10; // Capped at 100 by endpoint validation
    public int Offset { get; init; } = 0; // Zero-based offset for RediSearch Limit(offset, count)
}
```

**ScoredResult contract** (per architecture: reused by all axes in future stories):
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

**SearchResult contract:**
```csharp
public sealed record SearchResult
{
    public required IReadOnlyList<ScoredResult> Results { get; init; }
    public required long TotalCount { get; init; }
    public required string Query { get; init; }
}
```

### REST Endpoint Pattern

Follow existing minimal API pattern from `Program.cs`:

```csharp
app.MapGet("/api/search", async (
    SyntacticSearchService searchService,
    [FromQuery] string tenantId,
    [FromQuery] string query,
    [FromQuery] string? caseId,
    [FromQuery] int maxResults = 10,
    [FromQuery] int offset = 0) =>
{
    // Validate required params, return ErrorResponse if invalid
    // Call searchService.SearchAsync(new SearchQuery { ... })
    // Return Results.Ok(searchResult)
});
```

Register `SyntacticSearchService` as singleton in DI (it only holds `IConnectionMultiplexer` reference).

### Tenant Isolation — Critical

All queries MUST be scoped to the tenant's index. The index name `{tenantId}:memories:idx` enforces isolation at the RediSearch level — each tenant has a physically separate index. Validate `tenantId` with `TenantIdGuard.Validate()` before querying (same as indexing activities).

### Error Handling

- **Missing index** (tenant never had data indexed): Catch `RedisServerException` containing "Unknown Index name", return empty `SearchResult` with `TotalCount = 0`. Do NOT throw — an empty tenant is a valid state.
- **Invalid query syntax**: Let RediSearch error propagate as 400 Bad Request with `ErrorResponse`.
- **Redis connection failure**: Let exception propagate (infrastructure failure, retriable by caller).
- **Stale index entries:** RediSearch does not auto-remove index entries when the underlying hash is deleted (TTL expiry or manual deletion). `Document.GetProperties()` may return null/empty fields. In `MapDocumentToScoredResult`, check for required fields (`content`, `sourceUri`, `sourceType`) — skip documents with missing required fields rather than crashing the mapping loop. Log a warning for skipped stale entries.

### Testing Strategy

**Tier 1 — Contracts.Tests (unit, no dependencies):**
- Serialization round-trip for `SearchQuery`, `ScoredResult`, `SearchResult`
- Use `MemoriesJsonContext.Options` for all serialization
- Test default values (MaxResults=10, Offset=0)
- Test nullable fields (CaseId null serializes correctly)

**Tier 2 — Server.Tests (unit, pure logic focus):**
- Extract testable pure functions from `SyntacticSearchService`:
  - `MapDocumentToScoredResult(Document doc, string tenantId) → ScoredResult` — hash key parsing, field extraction, SourceType enum parsing, content truncation, Axis tagging
  - `HandleSearchException(RedisServerException ex) → SearchResult` — missing index detection, empty result generation
  - `BuildQueryString(string searchTerms, string? caseId) → string` — case-scoped TAG filter construction, special char escaping
  - `EscapeRedisQuery(string input) → string` — RediSearch special character escaping
- Test result mapping: known `Document` properties → correct `ScoredResult` values (including `Axis = "syntactic"`)
- Test MemoryUnitId extraction from hash key: `"tenant1:mu:abc123"` → `"abc123"`
- Test SourceType parsing: `"file"` → `SourceType.File`, `"event"` → `SourceType.Event` (case-insensitive)
- Test ContentSnippet truncation: 500-char input → 200-char output
- Test empty results: zero documents → empty list with TotalCount=0
- Test missing index: `RedisServerException("Unknown Index name")` → empty result (not exception)
- Test case-scoped query building: with and without caseId
- Test query escaping: `"claim-denied"` → `"claim\-denied"`, `"@admin"` → `"\@admin"`
- Test null/missing field handling: document with null `sourceType` → skipped, not crash
- **Do NOT mock the full Redis `IConnectionMultiplexer` → `IDatabase` → `SearchCommands` chain** — that's brittle and what Tier 3 integration tests validate

**Tier 3 — IntegrationTests (Aspire, real Redis Stack):**
- Use `AspireIngestionPipelineFixture` or create search-specific fixture
- Seed data by calling the ingestion endpoint or directly via `IndexSyntacticActivity`
- Verify BM25 ranking order with known content (seed 3+ docs, assert ordering)
- Verify tenant isolation: seed identical docs under two tenants, query one, assert zero cross-leak
- Verify case scoping with `@caseId` TAG filter
- Verify special character handling: query with hyphens, parentheses doesn't throw
- Verify offset pagination: seed 15+ docs, query with offset=10 maxResults=5, assert correct skip
- Verify broad-match: single common word matching many docs, assert MaxResults cap respected
- Latency smoke test: 10 concurrent queries, assert p95 <200ms

### Performance Considerations (NFR1: <200ms p95)

- RediSearch FT.SEARCH on 10K documents is inherently fast (<50ms typical)
- Do NOT load full content in results — use `ReturnFields` to select only needed fields
- Limit default result size to 10 (configurable via `MaxResults`)
- No additional processing needed for raw BM25 results (normalization is Story 2.4)

### What NOT To Build

- **Score normalization** — Story 2.4 handles BM25→0.0-1.0 normalization. Return raw BM25 scores.
- **Fusion/hybrid search** — Story 2.5. This story is syntactic-only.
- **Explain mode** — Story 2.6. No per-axis breakdown yet.
- **Pagination tokens** — Story 2.6. Use simple offset/limit for now.
- **Semantic or graph search** — Stories 2.2, 2.3. Completely separate.
- **CorpusStatisticsActor** — Story 2.4. Not needed for raw BM25 results.
- **No new interfaces** — Decision D9: concrete `SyntacticSearchService` class, no `ISyntacticSearchService` until a second implementation exists.
- **Tenant authorization** — Epic 5 handles authentication/authorization enforcement. MVP relies on trusted callers. `TenantIdGuard.Validate()` checks format only, not permission.

### Project Structure Notes

- New `Server/Search/` folder — first file in this namespace
- Contracts go in existing `Contracts/V1/` alongside other records
- Tests follow existing patterns: `Server.Tests/Search/SyntacticSearchServiceTests.cs`, `Contracts.Tests/V1/SearchQuerySerializationTests.cs`
- Integration tests: `IntegrationTests/Search/SyntacticSearchIntegrationTests.cs`

### Previous Story Intelligence

**From Story 1.5 (Three-Backend Indexing):**
- `[FromKeyedServices("redis")] IConnectionMultiplexer redis` pattern for Redis injection
- `TenantIdGuard.Validate()` for tenant ID validation before any Redis operation
- Index creation is idempotent (catches "Index already exists" exception)
- `NRedisStack.Search` namespace for all FT.* operations

**From Story 1.6 (Ingestion Workflow):**
- REST endpoints use minimal API pattern in `Program.cs` (no controllers)
- Validation returns `ErrorResponse` record for 400 responses
- `Results.Ok()`, `Results.BadRequest()`, `Results.NotFound()` for responses

**From Story 1.7 (Provider Configuration):**
- Singleton registration for services holding `IConnectionMultiplexer`
- `EmbeddingClient` changed from typed HttpClient to singleton with factory — similar pattern for search service

### Git Intelligence

Recent commits show:
- `2253f09` Merge PR #5: Three-backend indexing
- `b1db3e9` Unit tests for activities and workflows
- `ed267d7` feat: implement three-backend indexing

The indexing infrastructure is stable and merged. Search can build on top of the existing index schema.

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 2, Story 2.1]
- [Source: _bmad-output/planning-artifacts/architecture.md — Search Architecture, §Server/Search/, §RediSearchQueryExecutor]
- [Source: _bmad-output/planning-artifacts/prd.md — FR1-FR2, NFR1, NFR24]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs — Index schema]
- [Source: src/Hexalith.Memories.Server/Program.cs — DI and endpoint patterns]
- [Source: NRedisStack docs — FT.SEARCH query API, dialect 2]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
