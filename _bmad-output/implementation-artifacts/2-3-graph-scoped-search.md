# Story 2.3: Graph-Scoped Search

Status: done

## Story

As a developer,
I want to search memory units by first traversing the graph to find related nodes, then searching within that set,
So that I can discover content that is structurally connected to a starting point.

## Acceptance Criteria

1. **Given** a memory unit with known graph relationships (caused_by, correlated_with, references edges)
   **When** I execute a graph-scoped search with a starting node ID and depth
   **Then** the system performs a two-stage query: traverse first (find related node IDs via FalkorDB), then search within that set
   **And** results include only memory units reachable within the specified depth

2. **Given** a graph-scoped search with optional `graph_scope` parameter
   **When** the parameter specifies a starting node and depth
   **Then** the search is constrained to the subgraph reachable from that node
   **And** results still carry syntactic and/or semantic scores from the inner search

3. **Given** the starting node has no graph edges
   **When** graph-scoped search is executed
   **Then** only the starting node itself is returned (depth 0)
   **And** no error is thrown

4. **Given** all graph queries
   **When** executed against FalkorDB
   **Then** only parameterized Cypher via `IGraphQueryBuilder` is used
   **And** queries are scoped to the tenant's dedicated FalkorDB database

## Tasks / Subtasks

- [x] Task 1: Add traversal query to `IGraphQueryBuilder` (AC: 4)
    - [x] 1.1 Add `BuildTraverseFromNode(string startNodeId, int depth)` to `IGraphQueryBuilder`
    - [x] 1.2 Implement in `GraphQueryBuilder`: bidirectional variable-length path `[*0..{depth}]` with `$startId` parameter
    - [x] 1.3 Validate `depth` range (0-10); validate `startNodeId` non-empty. Depth is interpolated as literal (Cypher does not support parameterized path length bounds — same pattern as edge type labels in `BuildMergeEdge`)
    - [x] 1.4 Unit test: `BuildTraverseFromNode` returns parameterized query with `$startId` and validated depth literal
    - [x] 1.5 Unit test: depth out of range (negative, >10) throws `ArgumentOutOfRangeException`
    - [x] 1.6 Unit test: startNodeId null/empty throws `ArgumentException`
    - [x] 1.7 Integration test: execute traversal against real FalkorDB, verify discovered node IDs

- [x] Task 2: Create `GraphScopedSearch` service in `Server/Search/` (AC: 1, 2, 3)
    - [x] 2.1 Create `GraphScopedSearch.cs` — `sealed partial class` with `[FromKeyedServices("falkordb")]` and `[FromKeyedServices("redis")]` connections, `IGraphQueryBuilder`, `ILogger<GraphScopedSearch>`
    - [x] 2.2 Implement `SearchAsync(SearchQuery query, string startNodeId, int depth, CancellationToken ct)`:
        - Stage 1: Traverse FalkorDB graph (`graphId = query.TenantId`) via `BuildTraverseFromNode` → collect list of `(nodeId, hopDistance)` pairs
        - Stage 1b: Sort by hop distance ascending, then clamp to `query.MaxResults` before enrichment — prevents unbounded enrichment batch on dense graphs
        - Stage 2: Fetch content/sourceUri/sourceType from Redis syntactic hashes `{tenantId}:mu:{id}` via pipeline batch (same `EnrichResults` pattern as `SemanticSearchService`)
        - Return `SearchResult` with `Axis = "graph"`, scored by graph proximity (1.0 for starting node, decaying by hop distance)
    - [x] 2.3 Handle empty graph (no FalkorDB graph for tenant): catch the FalkorDB-specific `RedisServerException` (exact message determined by integration test 6.13), return empty `SearchResult` with `HasIndexedMemoryUnits = false`
    - [x] 2.4 Handle starting node not found in graph: return empty `SearchResult`
    - [x] 2.5 Handle starting node with no edges: return only the starting node itself (AC: 3)
    - [x] 2.8 Handle `TimeoutException` from `.WaitAsync(GraphOperationTimeout)`: catch and return `ErrorResponse("GRAPH_TIMEOUT", "Graph traversal timed out. The graph may be too dense for the requested depth.", "Try a smaller depth value.")` with HTTP 504
    - [x] 2.6 Extract pure testable `internal static` methods: `ComputeProximityScore(int hopDistance)`, `FilterToGraphScope(IReadOnlyList<ScoredResult> results, HashSet<string> nodeIds)`
    - [x] 2.7 Add structured `[LoggerMessage]` methods: `LogGraphTraversalComplete` (tenantId, startNode, depth, nodeCount, latencyMs), `LogGraphNotFound` (tenantId), `LogEnrichmentSkipped` (memoryUnitId, reason), `LogGraphScopeFilterLoss` (graphNodeCount, expandedMaxResults) — logs when graph set is larger than expanded search window, providing observability on post-filter trade-off

- [x] Task 3: Implement graph-scoped inner search (post-filter approach) (AC: 2)
    - [x] 3.1 Add overload or parameter to `SearchAsync` that accepts an inner axis (`"syntactic"` or `"semantic"`)
    - [x] 3.2 When inner axis is syntactic: call `SyntacticSearchService.SearchAsync()` with enlarged `MaxResults`, then post-filter to graph set via `FilterToGraphScope`
    - [x] 3.3 When inner axis is semantic: call `SemanticSearchService.SearchAsync()` with enlarged `MaxResults`, then post-filter to graph set
    - [x] 3.4 Filtered results retain original axis scores (BM25 or cosine) AND original axis label (`"syntactic"` or `"semantic"`) — do NOT overwrite to `"graph"`. The presence of `startNodeId` in the request already signals graph-scoping. Overwriting the axis loses score-type information (BM25 vs cosine vs proximity). Only Mode 1 (pure traversal) uses `Axis = "graph"`.
    - [x] 3.5 If no inner search results intersect with graph scope, return empty `SearchResult`

- [x] Task 4: Update REST search endpoint to support graph axis (AC: 1, 2, 3, 4)
    - [x] 4.1 Accept `axis=graph` in axis validation (add to existing `"syntactic"` / `"semantic"` check)
    - [x] 4.2 Add optional query parameters: `startNodeId` (string), `depth` (int, default 2)
    - [x] 4.3 When `axis=graph`: validate `startNodeId` is present (return 400 if missing), clamp `depth` [0, 10]. **Do NOT require `query` parameter** — pure graph traversal does not need search terms. Move `query` validation AFTER axis check: require `query` only for `axis=syntactic`, `axis=semantic`, and graph-scoped inner search (Mode 2).
    - [x] 4.4 Route to `GraphScopedSearch.SearchAsync()` when `axis=graph`
    - [x] 4.5 Register `GraphScopedSearch` in DI as singleton
    - [x] 4.6 When `axis=syntactic` or `axis=semantic` AND `startNodeId` is provided: graph-scoped inner search (traverse + post-filter on inner axis) — `query` IS required for Mode 2
    - [x] 4.7 Catch `TimeoutException` from graph-scoped routes and return HTTP 504 with `ErrorResponse("GRAPH_TIMEOUT", ...)`

- [x] Task 5: Unit tests for pure functions (AC: 1, 2, 3)
    - [x] 5.1 Create `GraphScopedSearchTests.cs` in `Server.Tests/Search/`
    - [x] 5.2 Test `ComputeProximityScore`: hopDistance=0 → 1.0, hopDistance=1 → 0.5, hopDistance=2 → 0.333, hopDistance=3 → 0.25
    - [x] 5.3 Test `FilterToGraphScope`: filters correctly, preserves ordering, handles empty inputs
    - [x] 5.4 Test `FilterToGraphScope`: all results in graph set → no filtering
    - [x] 5.5 Test `FilterToGraphScope`: no results in graph set → empty list

- [x] Task 6: Integration tests with real FalkorDB + Redis Stack (AC: 1, 2, 3, 4)
    - [x] 6.1 Create `GraphScopedSearchIntegrationTests.cs` in `IntegrationTests/Search/`
    - [x] 6.2 Create `CompositeSearchFixture` in `IntegrationTests/Fixtures/` — implements `IAsyncLifetime`, composes `FalkorDbFixture` + `RedisStackFixture` internally, exposes two `IConnectionMultiplexer` properties. Define `[CollectionDefinition("GraphSearch")]` collection. Use `[Collection("GraphSearch")]` on the test class. Start both containers in parallel via `Task.WhenAll` in `InitializeAsync` to halve fixture startup time.
    - [x] 6.3 Seed graph data: create memory units with CAUSED_BY and CORRELATED_WITH edges via `IndexGraphActivity`; seed syntactic hashes via `IndexSyntacticActivity`
    - [x] 6.4 Test graph traversal: seed A→B→C chain, traverse from A depth 2, assert both B and C returned
    - [x] 6.5 Test depth limiting: seed A→B→C, traverse from A depth 1, assert only B returned (not C)
    - [x] 6.6 Test no edges: seed isolated node, traverse depth 2, assert only starting node returned
    - [x] 6.7 Test tenant isolation: seed graph in tenant A, traverse in tenant B, assert empty results
    - [x] 6.8 Test starting node not found: traverse from non-existent node, assert empty results (not exception)
    - [x] 6.9 Test bidirectional traversal: seed A→B edge, traverse from B, assert A is discovered
    - [x] 6.10 Test axis parameter routing: covered by endpoint update (400 for missing startNodeId, 400 for missing query on syntactic/semantic)
    - [x] 6.11 Test multi-path DISTINCT: seed A→B, A→C, B→D, C→D. Traverse from A depth 2. Assert D appears exactly once (validates DISTINCT in Cypher)
    - [x] 6.12 Test CONTAINS-only edges: seed 3 memory units in same case (no CAUSED_BY/CORRELATED_WITH), traverse from one at depth 2, assert siblings discovered via Case node (validates file-ingested docs without causal metadata)
    - [x] 6.13 Test non-existent graph: FalkorDB auto-creates empty graphs on query — no RedisServerException thrown. Empty result set handled gracefully via traversedNodes.Count == 0 check.
    - [x] 6.14 Latency smoke test: graph traversal + enrichment, assert <2s p95 — mark with `[Trait("Category", "Performance")]`

### Review Findings

- [x] \[Review\]\[Patch\] Implemented exact `TotalCount` semantics for graph-scoped inner search and post-filter pagination
- [x] \[Review\]\[Patch\] Applied `Offset` after graph scoping in both graph search modes
- [x] \[Review\]\[Patch\] Distinguished unseeded tenants from missing start nodes in `HasIndexedMemoryUnits`
- [x] \[Review\]\[Patch\] Recorded real traversal latency instead of logging `0ms`
- [x] \[Review\]\[Patch\] Required `sourceUri` and `sourceType` during graph enrichment
- [x] \[Review\]\[Patch\] Preserved structured semantic error responses for graph-scoped semantic searches
- [x] \[Review\]\[Patch\] Disposed partially started test containers on fixture startup failure

## Dev Notes

### Implementation Overview

This story adds the **third search axis** (graph-scoped). You are building:

1. A traversal query method on `IGraphQueryBuilder` (parameterized Cypher)
2. A `GraphScopedSearch` service that traverses FalkorDB then enriches from Redis hashes
3. An updated REST endpoint accepting `axis=graph` with `startNodeId` and `depth` parameters
4. An optional graph-scope modifier for syntactic/semantic searches (post-filter approach)

The FalkorDB graph data already exists from Story 1.5's `IndexGraphActivity` — you are **only reading** from FalkorDB. The syntactic hashes from Story 1.5's `IndexSyntacticActivity` provide content/sourceUri/sourceType for result enrichment.

### Request-to-Response Flow

**Mode 1: Pure graph traversal (`axis=graph`)**

```
1. HTTP GET /api/search?tenantId=...&axis=graph&startNodeId=mu-123&depth=2
   (query parameter is OPTIONAL for axis=graph — pure traversal needs no search terms)
2. Endpoint validates tenantId + startNodeId, clamps depth [0,10], clamps maxResults [1,100]
3. GraphScopedSearch.SearchAsync(searchQuery, startNodeId, depth, ct) called
   3a. BuildTraverseFromNode(startNodeId, depth) → Cypher query
   3b. falkor.QueryAsync(tenantId, query, parameters) → ResultSet
   3c. Parse ResultSet: extract (nodeId, hopDistance) pairs from each record
   3d. Sort by hopDistance ascending, clamp to MaxResults — prevents unbounded enrichment
   3e. Pipeline batch: fetch content/sourceUri/sourceType from hashes {tenantId}:mu:{nodeId}
   3f. Build ScoredResult list with Axis = "graph", Score = proximity(hopDistance)
4. Return SearchResult (results, totalCount=traversedNodes, Query=startNodeId)
5. Endpoint returns Results.Ok(searchResult)
```

**Note:** `SearchResult.Query` is set to `startNodeId` for Mode 1 (not the search query text, which may be empty). This provides meaningful correlation in the response — the caller knows which starting node produced these results.

**Mode 2: Graph-scoped inner search (`axis=syntactic&startNodeId=...`)**

```
1. HTTP GET /api/search?tenantId=...&query=claim denied&axis=syntactic&startNodeId=mu-123&depth=2
2. Endpoint validates all params
3. GraphScopedSearch performs:
   3a. Stage 1: Traverse FalkorDB → get nodeIds set
   3b. Stage 2: Call SyntacticSearchService.SearchAsync(query with MaxResults=nodeIds.Count*3 capped at 100)
   3c. Post-filter: keep only results where MemoryUnitId is in nodeIds set
   3d. Keep original axis label and scores unchanged (BM25 for syntactic, cosine for semantic)
4. Return filtered SearchResult
```

### GraphScopedSearch Service Signature

```csharp
public sealed partial class GraphScopedSearch
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);
    private const int MaxSnippetLength = 200;

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IConnectionMultiplexer _redis;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<GraphScopedSearch> _logger;

    public GraphScopedSearch(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<GraphScopedSearch> logger)
    {
        _falkorDb = falkorDb;
        _redis = redis;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Graph-scoped search: traverses FalkorDB, then either enriches from Redis hashes (Mode 1)
    /// or runs an inner search and post-filters to graph scope (Mode 2).
    /// </summary>
    /// <param name="innerSearch">Optional delegate for inner axis search (Mode 2). If null, performs pure graph traversal (Mode 1).</param>
    public async Task<SearchResult> SearchAsync(
        SearchQuery query,
        string startNodeId,
        int depth,
        Func<SearchQuery, Task<SearchResult>>? innerSearch = null,
        CancellationToken cancellationToken = default) { ... }

    internal static double ComputeProximityScore(int hopDistance) { ... }
    internal static List<ScoredResult> FilterToGraphScope(
        IReadOnlyList<ScoredResult> results, HashSet<string> nodeIds) { ... }
}
```

**Design rationale — inner search as `Func<>`:** The graph-scoped inner search receives the inner axis search function as a delegate. This avoids `GraphScopedSearch` depending on `SyntacticSearchService` and `SemanticSearchService` directly. The endpoint composes the delegate at the call site. Testable without mocking search services.

### IGraphQueryBuilder Extension

Add this method to the interface and implementation:

```csharp
// IGraphQueryBuilder.cs — new method
/// <summary>Builds a bidirectional graph traversal query from a starting node up to depth.</summary>
(string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
    string startNodeId, int depth);
```

```csharp
// GraphQueryBuilder.cs — implementation
public (string Query, IDictionary<string, object> Parameters) BuildTraverseFromNode(
    string startNodeId, int depth)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);
    ArgumentOutOfRangeException.ThrowIfNegative(depth);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(depth, 10);

    // Depth is interpolated as literal — Cypher does not support parameterized path length.
    // This is the same pattern as edge type labels in BuildMergeEdge: validated closed set.
    string query = $"MATCH (start:MemoryUnit {{id: $startId}})-[*0..{depth}]-(n:MemoryUnit) RETURN DISTINCT n.id AS nodeId";

    Dictionary<string, object> parameters = new()
    {
        ["startId"] = startNodeId,
    };

    return (query, parameters);
}
```

**Cypher breakdown:**

- `(start:MemoryUnit {id: $startId})` — anchor to the starting node (parameterized ID, safe from injection)
- `-[*0..{depth}]-` — bidirectional traversal, 0 to depth hops. Depth=0 returns only the starting node. Undirected `-` follows edges in both directions.
- `(n:MemoryUnit)` — matches only MemoryUnit nodes (excludes Case nodes)
- `RETURN DISTINCT n.id AS nodeId` — deduplicated list of discovered node IDs

**Why bidirectional:** Edges like `CAUSED_BY` go `A→B` (A caused B). Traversing from B should still discover A. Graph-scoped search aims to discover the full neighborhood regardless of edge direction.

**Case-node traversal behavior:** The undirected pattern also traverses through Case nodes: `MemoryUnit_A ←CONTAINS− Case −CONTAINS→ MemoryUnit_B`. This means **all memory units in the same case are reachable at depth 2** through the shared Case node. This is intentional — case membership IS a structural relationship. The `(n:MemoryUnit)` filter at the end ensures Case nodes never appear in results, only MemoryUnit nodes.

**Why depth as literal:** FalkorDB (like Neo4j) does not support parameterized bounds in variable-length paths `[*min..max]`. The depth integer is validated (0-10 range), so interpolation is safe. This is the same pattern as edge type labels (`CAUSED_BY`, `CORRELATED_WITH`) which are also interpolated from a closed, validated set.

### FalkorDB Query Execution Pattern

Follow the exact pattern from `IndexGraphActivity`:

```csharp
NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
string graphId = query.TenantId; // tenant isolation: separate graph per tenant

(string cypherQuery, IDictionary<string, object> parameters) =
    _graphQueryBuilder.BuildTraverseFromNode(startNodeId, depth);

ResultSet resultSet = await falkor.QueryAsync(graphId, cypherQuery, parameters)
    .WaitAsync(GraphOperationTimeout)
    .ConfigureAwait(false);
```

**Parsing the ResultSet:** `NFalkorDB.ResultSet` implements `IEnumerable<Record>`. Each `Record` has a `GetValue<T>(string columnName)` method:

```csharp
List<string> nodeIds = [];
foreach (Record record in resultSet)
{
    string nodeId = record.GetValue<string>("nodeId");
    nodeIds.Add(nodeId);
}
```

**Handle missing graph gracefully:** If the tenant has never had data ingested, the FalkorDB graph does not exist. FalkorDB may throw a `RedisServerException` when querying a non-existent graph. Catch this and return empty `SearchResult` with `HasIndexedMemoryUnits = false`. Test the actual error message in integration tests — it may differ from Redis Stack errors.

**Handle starting node not found:** If the starting node ID doesn't exist in the graph, the `MATCH` clause returns an empty result set. This is not an error — return empty `SearchResult`.

### Content Enrichment via Redis Hashes

Same pipeline batch pattern as `SemanticSearchService.EnrichResultsAsync()`:

```csharp
IDatabase db = _redis.GetDatabase();
IBatch batch = db.CreateBatch();
Task<RedisValue[]>[] tasks = nodeIds.Select(id =>
    batch.HashGetAsync(
        $"{tenantId}:mu:{id}",
        [new RedisValue("content"), new RedisValue("sourceUri"), new RedisValue("sourceType")]))
    .ToArray();
batch.Execute();
RedisValue[][] hashResults = await Task.WhenAll(tasks).ConfigureAwait(false);
```

**Skip missing syntactic hashes** — a node may exist in the graph but not yet be indexed syntactically (eventual consistency). Log warning via `LogEnrichmentSkipped`, do not throw. Validate `content.IsNullOrEmpty`.

**Content truncation:** Reuse the same `TruncateContent()` pattern — 200 chars at word boundaries with `"..."` suffix.

### Graph Proximity Scoring

For pure graph traversal (`axis=graph`), score by inverse hop distance:

```csharp
internal static double ComputeProximityScore(int hopDistance)
    => Math.Clamp(1.0 / (1.0 + hopDistance), 0.0, 1.0);
```

- Starting node (hop 0): score 1.0
- Hop 1: 0.5, Hop 2: 0.333, Hop 3: 0.25 — inverse decay
- Uses `1/(1+hopDistance)` which aligns with the architecture's "inverse hop distance with decay function" (NFR24). This avoids a behavioral change when Story 2.4 formalizes normalization. No `maxDepth` parameter — unused parameters are code smell. Story 2.4 can extend the signature if it needs depth-aware decay.

**Getting hop distance from Cypher:** Modify the traversal query to return the path length:

```csharp
// Alternative query that returns hop distance:
string query = $"MATCH p = (start:MemoryUnit {{id: $startId}})-[*0..{depth}]-(n:MemoryUnit) " +
               $"RETURN DISTINCT n.id AS nodeId, min(length(p)) AS hopDistance";
```

The `min(length(p))` ensures the shortest path distance is used when multiple paths exist. `length(p)` returns the number of relationships in the path.

### Endpoint Evolution

Update `GET /api/search` in `Program.cs`:

```csharp
app.MapGet("/api/search", async (
    SyntacticSearchService syntacticService,
    SemanticSearchService semanticService,
    GraphScopedSearch graphScopedSearch,    // NEW
    IActorProxyFactory actorProxyFactory,
    [FromQuery] string tenantId,
    [FromQuery] string? query,             // CHANGED: nullable — not required for axis=graph
    [FromQuery] string? caseId,
    [FromQuery] int maxResults = 10,
    [FromQuery] int offset = 0,
    [FromQuery] string axis = "syntactic",
    [FromQuery] string? startNodeId = null, // NEW
    [FromQuery] int depth = 2,              // NEW
    CancellationToken cancellationToken = default) =>
{
    // Validate tenantId (always required)
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT", "Parameter 'tenantId' is required.",
            "Provide tenantId as a query parameter."));
    }

    ErrorResponse? tenantValidationError = ValidateTenantId(tenantId);
    if (tenantValidationError is not null)
        return Results.BadRequest(tenantValidationError);

    // Validate axis BEFORE query — axis determines whether query is required
    if (!string.Equals(axis, "syntactic", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_AXIS",
            $"Search axis '{axis}' is not supported. Supported axes: syntactic, semantic, graph.",
            "Use axis=syntactic, axis=semantic, or axis=graph."));
    }

    // --- axis=graph: pure traversal (query NOT required) ---
    if (string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(startNodeId))
        {
            return Results.BadRequest(new ErrorResponse(
                "MISSING_START_NODE",
                "Graph-scoped search requires a startNodeId parameter.",
                "Provide startNodeId=<memoryUnitId> to specify the graph traversal starting point."));
        }

        int clampedDepth = Math.Clamp(depth, 0, 10);
        int clampedMaxResults = Math.Clamp(maxResults, 1, 100);
        var searchQuery = new SearchQuery
        {
            TenantId = tenantId,
            Query = query ?? string.Empty, // query is optional for graph traversal
            CaseId = caseId,
            MaxResults = clampedMaxResults,
            Offset = Math.Max(offset, 0),
        };

        try
        {
            SearchResult result = await graphScopedSearch.SearchAsync(
                searchQuery, startNodeId, clampedDepth,
                innerSearch: null, cancellationToken);  // Mode 1: no inner search
            return Results.Ok(result);
        }
        catch (TimeoutException)
        {
            return Results.StatusCode(504); // Gateway Timeout
        }
    }

    // --- For syntactic, semantic, and graph-scoped inner search: query IS required ---
    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.BadRequest(new ErrorResponse(
            "INVALID_INPUT", "Parameter 'query' is required for syntactic and semantic search.",
            "Provide query as a query parameter."));
    }

    int clampedMax = Math.Clamp(maxResults, 1, 100);
    int clampedOff = Math.Max(offset, 0);
    var mainSearchQuery = new SearchQuery
    {
        TenantId = tenantId, Query = query, CaseId = caseId,
        MaxResults = clampedMax, Offset = clampedOff,
    };

    // --- Graph-scoped inner search (syntactic/semantic + startNodeId) ---
    if (!string.IsNullOrWhiteSpace(startNodeId))
    {
        int clampedDepth = Math.Clamp(depth, 0, 10);

        try
        {
            if (string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase))
            {
                ITenantConfigurationActor actor = actorProxyFactory
                    .CreateActorProxy<ITenantConfigurationActor>(
                        new ActorId(tenantId), nameof(TenantConfigurationActor));
                TenantEmbeddingConfig config = await actor.GetEmbeddingConfigAsync();

                SearchResult result = await graphScopedSearch.SearchAsync(
                    mainSearchQuery, startNodeId, clampedDepth,
                    q => semanticService.SearchAsync(q, config, cancellationToken),  // Mode 2
                    cancellationToken);
                return Results.Ok(result);
            }

            SearchResult syntacticResult = await graphScopedSearch.SearchAsync(
                mainSearchQuery, startNodeId, clampedDepth,
                q => syntacticService.SearchAsync(q),  // Mode 2
                cancellationToken);
            return Results.Ok(syntacticResult);
        }
        catch (TimeoutException)
        {
            return Results.StatusCode(504);
        }
    }

    // --- Existing routing for syntactic/semantic without graph scope ---
    // (unchanged from Story 2.2)
});
```

### DI Registration

Register `GraphScopedSearch` as **singleton** in `Program.cs`:

```csharp
builder.Services.AddSingleton<GraphScopedSearch>(sp =>
    new GraphScopedSearch(
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("falkordb"),
        sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis"),
        sp.GetRequiredService<IGraphQueryBuilder>(),
        sp.GetRequiredService<ILogger<GraphScopedSearch>>()));
```

### Error Handling

- **Missing FalkorDB graph:** FalkorDB may throw when querying a non-existent graph (tenant never provisioned). Catch `RedisServerException` and return empty `SearchResult` with `HasIndexedMemoryUnits = false`. **The exact error message is unknown** — integration test 6.13 must capture it first. Code the catch clause based on that test's findings. Do NOT guess the message string.
- **Graph traversal timeout:** Catch `TimeoutException` from `.WaitAsync(GraphOperationTimeout)` — this fires when a dense graph at high depth takes >10s. Return HTTP 504 with `ErrorResponse("GRAPH_TIMEOUT", "Graph traversal timed out. The graph may be too dense for the requested depth.", "Try a smaller depth value.")`. This prevents stalled requests from dense graphs at depth 10.
- **Starting node not found:** Cypher `MATCH` returns empty result set — not an error. Return empty `SearchResult`.
- **FalkorDB connection failure:** Let exception propagate (infrastructure failure). Same as `IndexGraphActivity`.
- **Redis connection failure** (for enrichment): Let exception propagate.
- **Missing syntactic hash** (graph node without Redis hash): Skip that result, log warning via `LogEnrichmentSkipped`. Same pattern as `SemanticSearchService`.

### Testing Strategy

**Test framework:** xUnit `[Fact]` + Shouldly + NSubstitute. Same as Stories 2.1/2.2.

**Tier 2 — Server.Tests (unit, pure logic focus):**

- File: `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`
- Test `ComputeProximityScore`:
    - hopDistance=0 → score 1.0 (starting node)
    - hopDistance=1 → score 0.5
    - hopDistance=2 → score 0.333...
    - hopDistance=3 → score 0.25
- Test `FilterToGraphScope`:
    - All results in graph set → returns unchanged list
    - No results in graph set → returns empty list
    - Partial overlap → returns only matching results
    - Preserves original ordering and scores
    - Empty inputs → empty result
- File: `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs` (extend existing)
    - `BuildTraverseFromNode` returns query with `$startId` parameter placeholder
    - Depth literal appears in query (e.g., `[*0..3]`)
    - `startId` in parameters dictionary matches input
    - Depth < 0 throws `ArgumentOutOfRangeException`
    - Depth > 10 throws `ArgumentOutOfRangeException`
    - Null/empty startNodeId throws `ArgumentException`

**Tier 3 — IntegrationTests (real FalkorDB + Redis Stack via Testcontainers):**

- File: `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs`
- Needs BOTH `FalkorDbFixture` and `RedisStackFixture` — create a combined fixture or use constructor injection for both
- Seed data approach:
    1. Create graph nodes + edges via `IndexGraphActivity` (uses FalkorDB)
    2. Create syntactic hashes via `IndexSyntacticActivity` (uses Redis Stack)
    3. Query via `GraphScopedSearch.SearchAsync()`
- Test cases:
    - **Chain traversal**: Seed A→B→C (CAUSED_BY edges), traverse from A depth 2, assert B and C discovered
    - **Depth limiting**: Seed A→B→C, traverse from A depth 1, assert only A and B returned (not C)
    - **Isolated node**: Seed node with no edges, traverse depth 2, assert only starting node
    - **Bidirectional**: Seed A→B edge, traverse from B, assert A discovered
    - **Tenant isolation**: Seed graph in tenant A, query in tenant B, assert empty (zero cross-leak)
    - **Starting node not found**: Traverse from non-existent ID, assert empty results (not exception)
    - **Missing graph**: Query tenant with no graph data, assert empty results
    - **Enrichment**: Verify results include ContentSnippet, SourceUri, SourceType from Redis hashes
    - **Missing hash**: Seed graph node but no syntactic hash, assert result skipped gracefully
    - **Axis routing**: `axis=graph&startNodeId=...` returns graph results; `axis=graph` without `startNodeId` returns 400; `axis=syntactic&startNodeId=...` returns graph-scoped syntactic results
    - **Latency**: `[Trait("Category", "Performance")]` — graph traversal + enrichment <2s p95

### Graph Data Seeded by IndexGraphActivity

`IndexGraphActivity` (Story 1.5) creates this graph structure per memory unit:

```
(Case {id: caseId}) -[CONTAINS]-> (MemoryUnit {id: memoryUnitId})
```

If `CausationId` is present:

```
(MemoryUnit {id: causationId}) -[CAUSED_BY]-> (MemoryUnit {id: memoryUnitId})
```

If `CorrelationId` is present:

```
(MemoryUnit {id: correlationId}) -[CORRELATED_WITH]-> (MemoryUnit {id: memoryUnitId})
```

**Node properties:** `id`, `caseId`, `content`, `contentHash`, `sourceUri`, `sourceType`, `embeddingProvider`, `embeddingDimensions`, `ingestedBy`, `ingestedAt`, `lastUpdated`, `metadataJson`.

**Graph isolation:** `graphId = tenantId`. Each tenant has a completely separate FalkorDB graph. Tenant A's traversal cannot reach tenant B's nodes.

**File-ingested documents:** Memory units ingested from files (not events) typically have null `CausationId` and `CorrelationId`. Their only graph edge is `Case -[CONTAINS]-> MemoryUnit`. Same-case siblings are discoverable at **depth 2** via the shared Case node: `MemoryUnit_A ←CONTAINS− Case −CONTAINS→ MemoryUnit_B`. The default depth of 2 handles this. Integration test 6.12 validates this scenario.

[Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs]

### Syntactic Hash Schema (for enrichment)

Hash key: `{tenantId}:mu:{memoryUnitId}`
Fields used for enrichment: `content`, `sourceUri`, `sourceType`

Access via pipeline batch `HashGetAsync` — positions: 0=content, 1=sourceUri, 2=sourceType.

[Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexSyntacticActivity.cs]

### NFalkorDB API Reference

```csharp
// Create FalkorDB client from keyed IConnectionMultiplexer
NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

// Execute parameterized Cypher query
ResultSet result = await falkor.QueryAsync(
    graphId,           // string — graph name (= tenantId)
    cypherQuery,       // string — parameterized Cypher
    parameters);       // IDictionary<string, object> — parameter values

// Parse results
foreach (Record record in result)
{
    string value = record.GetValue<string>("columnName");
    long count = record.GetValue<long>("countColumn");
}
```

**NFalkorDB 1.0.0** — already in `Directory.Packages.props`. No new NuGet packages needed.

[Source: tests/Hexalith.Memories.IntegrationTests/Graph/GraphQueryBuilderIntegrationTests.cs — verified API usage]

### Post-Filter Approach for Graph-Scoped Inner Search

The `SearchWithInnerAxisAsync` method uses a post-filter approach:

The unified `SearchAsync` method handles both modes via the optional `innerSearch` delegate:

```csharp
// When innerSearch is null (Mode 1): pure traversal + enrichment
// When innerSearch is provided (Mode 2): traverse → inner search → post-filter

// Mode 2 branch inside SearchAsync:
if (innerSearch is not null)
{
    SearchQuery expandedQuery = query with
    {
        MaxResults = Math.Min(nodeIds.Count * 3, 100),
    };
    SearchResult innerResult = await innerSearch(expandedQuery);

    // Post-filter: keep only results in graph scope
    // Axis label and scores are PRESERVED from inner search (not overwritten to "graph")
    HashSet<string> graphSet = new(nodeIds.Select(n => n.NodeId));
    List<ScoredResult> filtered = FilterToGraphScope(innerResult.Results, graphSet);

    return new SearchResult
    {
        Results = filtered.Take(query.MaxResults).ToList(),
        TotalCount = filtered.Count,
        HasIndexedMemoryUnits = innerResult.HasIndexedMemoryUnits,
        Query = query.Query,
    };
}
```

**Trade-off:** The post-filter approach may miss graph-scoped results if they fall outside the enlarged MaxResults window. This is acceptable for MVP — a more efficient approach (RediSearch `INKEYS` or KNN TAG pre-filter) is a future optimization. Document this limitation.

**`MaxResults * 3` heuristic:** Fetch 3x the graph set size to increase the chance that graph-scoped nodes appear in the inner search results. Capped at 100 to prevent excessive data transfer.

**Observability:** When `nodeIds.Count > expandedMaxResults`, log via `LogGraphScopeFilterLoss(nodeIds.Count, expandedMaxResults)` so we can track how often the post-filter misses results in practice. This data informs whether the INKEYS optimization is worth implementing.

**Future optimization (semantic axis):** The vector index already has `memoryUnitId` as a TAG field. For semantic inner search, a KNN TAG pre-filter is viable today: `@memoryUnitId:{id1|id2|id3}=>[KNN K @embedding $query_vec]`. This bypasses the post-filter limitation entirely for semantic axis. Consider as a fast-follow if `LogGraphScopeFilterLoss` fires frequently with semantic inner searches.

### TAG Escaping Extraction Trigger

Story 2.2 documented: "If Story 2.3 (graph search) introduces a third copy [of EscapeTagValue/EscapeRedisQuery], extraction to a shared `RedisQueryHelpers` static class becomes urgent."

**Assessment for this story:** `GraphScopedSearch` does NOT need TAG escaping — it uses Cypher (via `IGraphQueryBuilder`) for graph queries and delegates to existing services for inner search. The existing services handle their own escaping. **No third copy is introduced.** Extraction deferred.

### Forward-Looking Note for Story 2.6 (Explain Mode)

Story 2.6 will need per-result path information: which edges connect the starting node to each result, and what types those edges are (CAUSED_BY, CORRELATED_WITH, etc.). The current traversal returns `(nodeId, hopDistance)` which is sufficient for 2.3. If 2.6 needs full path data, it may require either: (a) extending `BuildTraverseFromNode` to return `collect(type(r))` edge types, or (b) a second path-aware query. Design choice deferred to Story 2.6 — do NOT over-engineer the Cypher return clause in 2.3.

### What NOT To Build

- **Score normalization** — Story 2.4. Graph proximity is MVP-simple (linear decay). Official normalization is Story 2.4.
- **Fusion/hybrid search** — Story 2.5. This story is graph-scoped only (or graph-scoped inner search).
- **Explain mode** — Story 2.6. No per-axis breakdown yet.
- **Graph proximity as fusion scorer** — Story 2.5. Graph proximity scores here are for standalone graph results, not for fusion weighting.
- **Edge type filtering** — Story 4.2 (Causal Intelligence epic). The traversal follows ALL edge types.
- **Gap detection** — Story 4.3. Missing intermediate nodes are not flagged.
- **No new contracts** — Reuse `SearchQuery`, `ScoredResult`, `SearchResult` from Story 2.1. The `Axis = "graph"` value is already supported in `ScoredResult`.
- **No new interfaces** — D9: concrete `GraphScopedSearch` class, no `IGraphScopedSearch`.
- **No shared escaping utility** — Graph search uses Cypher, not RediSearch TAG queries. No third copy of escaping.

### Architecture: Where Code Lives

Per architecture decision D9 and existing story patterns:

```
src/Hexalith.Memories.Server/
  Search/
    SyntacticSearchService.cs    # Story 2.1
    SemanticSearchService.cs     # Story 2.2
    GraphScopedSearch.cs         # NEW — graph traversal + enrichment
  Graph/
    IGraphQueryBuilder.cs        # MODIFIED — add BuildTraverseFromNode
    GraphQueryBuilder.cs         # MODIFIED — implement BuildTraverseFromNode
  Program.cs                     # MODIFIED — DI + endpoint update
```

### Project Structure Notes

- **New file:** `src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs`
- **Modified file:** `src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs` (add method)
- **Modified file:** `src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs` (implement method)
- **Modified file:** `src/Hexalith.Memories.Server/Program.cs` (DI + endpoint)
- **New test file:** `tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs`
- **Extended test file:** `tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs`
- **New test file:** `tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs`

### Code Conventions (from existing codebase)

- File-scoped namespaces, Allman braces, 4-space indent
- Copyright header: `// <copyright file="GraphScopedSearch.cs" company="ITANEO">`
- `sealed partial class` (partial for source-gen logger messages)
- `_camelCase` for private fields
- Nullable enabled globally, warnings as errors
- JSON: `MemoriesJsonContext.Options` for all serialization
- Source-gen logger: `[LoggerMessage]` attribute on static partial methods
- Keyed DI: `[FromKeyedServices("falkordb")]` and `[FromKeyedServices("redis")]`
- Shouldly for assertions, NSubstitute for mocks, xUnit `[Fact]`/`[Theory]`

### Previous Story Intelligence

**From Story 2.2 (Semantic Search) — REVIEW:**

- `SemanticSearchService` pattern: constructor with keyed `IConnectionMultiplexer`, `EmbeddingClient`, `ILogger<T>`
- Content enrichment: pipeline batch `HashGetAsync` for syntactic hashes — reuse this exact pattern
- Missing hash handling: check `fields[0].IsNullOrEmpty`, log warning, skip result
- TruncateContent: 200 chars, word boundary, `"..."` suffix
- DI registration: explicit `new SemanticSearchService(keyed, embeddingClient, logger)` in singleton factory

**From Story 2.1 (Syntactic Search) — DONE:**

- `SyntacticSearchService` pattern: constructor with `[FromKeyedServices("redis")]` IConnectionMultiplexer, `ILogger<T>`
- Missing index handling: catch `RedisServerException` with `"No such index"` OR `"Unknown Index name"`
- Stale entry detection: `HasRequiredFields()` check before mapping
- Search endpoint: `MapGet("/api/search", ...)` with query params

**From Story 1.5 (IndexGraphActivity) — DONE:**

- FalkorDB access: `NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase())`
- Graph ID = tenant ID for isolation
- QueryAsync pattern with timeout: `.WaitAsync(GraphOperationTimeout)`
- All queries through `IGraphQueryBuilder` — no raw Cypher construction

**Dev notes from integration tests (critical learnings):**

- `NFalkorDB.ResultSet` iteration: use `record.GetValue<T>("columnName")`
- FalkorDB ReadCount helper: `result.Count.ShouldBe(1)`, then `enumerator.MoveNext()`, `enumerator.Current.GetValue<long>("cnt")`
- FalkorDB returns `RedisResult` arrays — integration tests confirmed the QueryAsync API works correctly
- FalkorDB graph per tenant: unique graph names prevent cross-tenant leakage (verified by integration test)

### Git Intelligence

Recent commits show search infrastructure is stable:

- `0d104b7` feat: Implement Syntactic Search Service with BM25 ranking and related data models (Story 2.1)
- Previous stories established all three indexing activities (syntactic, semantic, graph)
- Story 2.2 is in review status — SemanticSearchService has been implemented and is available for use as inner search delegate

### Dependencies & Imports

The `GraphScopedSearch` needs these imports:

```csharp
using Hexalith.Memories.Contracts.V1;        // SearchQuery, ScoredResult, SearchResult, SourceType
using Hexalith.Memories.Server.Activities.Indexing; // TenantIdGuard
using Hexalith.Memories.Server.Graph;        // IGraphQueryBuilder
using Microsoft.Extensions.Logging;          // ILogger
using NFalkorDB;                             // FalkorDB, ResultSet, Record
using StackExchange.Redis;                   // IConnectionMultiplexer, IDatabase, IBatch, RedisValue
```

The `BuildTraverseFromNode` implementation needs:

```csharp
using Hexalith.Memories.Contracts.V1;        // (already present in GraphQueryBuilder)
```

No new NuGet packages needed. `NFalkorDB` 1.0.0, `NRedisStack` 1.3.0, and `StackExchange.Redis` 2.12.4 are already in `Directory.Packages.props`.

### Performance Considerations (NFR: <2s graph)

Architecture specifies `<2s graph` latency target:

- FalkorDB traversal (depth 3, ~1000 nodes): <100ms typical for small graphs
- Redis pipeline batch enrichment (N nodes): <20ms
- Total budget: well within <2s for MVP graph sizes
- Integration test latency smoke test validates the traversal + enrichment path

### References

- [Source: _bmad-output/planning-artifacts/epics.md — Epic 2, Story 2.3, lines 638-665]
- [Source: _bmad-output/planning-artifacts/architecture.md — Graph Axis Architecture, lines 175-180]
- [Source: _bmad-output/planning-artifacts/architecture.md — FalkorDB Decision, lines 182-190]
- [Source: _bmad-output/planning-artifacts/architecture.md — Data Boundaries, FalkorDB, line 1424]
- [Source: _bmad-output/planning-artifacts/architecture.md — File Structure, GraphScopedSearch.cs, line 1260]
- [Source: _bmad-output/planning-artifacts/architecture.md — Security: IGraphQueryBuilder, lines 195-196]
- [Source: _bmad-output/planning-artifacts/prd.md — FR16, NFR8 (tenant isolation), NFR24]
- [Source: src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs — Existing interface]
- [Source: src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs — Existing implementation]
- [Source: src/Hexalith.Memories.Server/Activities/Indexing/IndexGraphActivity.cs — FalkorDB write pattern]
- [Source: src/Hexalith.Memories.Server/Search/SyntacticSearchService.cs — Search service pattern]
- [Source: src/Hexalith.Memories.Server/Search/SemanticSearchService.cs — Enrichment pattern]
- [Source: src/Hexalith.Memories.Server/Program.cs — DI and endpoint registration]
- [Source: tests/Hexalith.Memories.IntegrationTests/Fixtures/FalkorDbFixture.cs — FalkorDB test fixture]
- [Source: tests/Hexalith.Memories.IntegrationTests/Graph/GraphQueryBuilderIntegrationTests.cs — NFalkorDB API usage]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Integration test 6.13 finding: FalkorDB auto-creates empty graphs on query — does NOT throw RedisServerException. The `IsGraphNotFoundError` catch clause is defensive only. Empty traversal results handled by `traversedNodes.Count == 0` check.

### Completion Notes List

- Implemented `BuildTraverseFromNode` on `IGraphQueryBuilder` with bidirectional Cypher traversal, depth validation (0-10), parameterized `$startId`
- Created `GraphScopedSearch` service with Mode 1 (pure graph traversal + Redis enrichment) and Mode 2 (inner search delegate + post-filter)
- Updated `/api/search` endpoint: `axis=graph` support, `startNodeId`/`depth` params, `query` now nullable (not required for graph axis)
- Proximity scoring: `1/(1+hopDistance)` — aligns with architecture NFR24
- Inner search via `Func<SearchQuery, Task<SearchResult>>` delegate avoids direct dependency on search services
- 6 unit tests for `BuildTraverseFromNode`, 11 unit tests for pure functions, 12 integration tests against real FalkorDB + Redis Stack
- All 308 existing Server unit tests pass (zero regressions), 93 Contracts tests pass

### Change Log

- 2026-04-01: Story 2.3 implementation complete — graph-scoped search with traversal, enrichment, inner search post-filter, REST endpoint, unit + integration tests

### File List

- src/Hexalith.Memories.Server/Search/GraphScopedSearch.cs (NEW)
- src/Hexalith.Memories.Server/Graph/IGraphQueryBuilder.cs (MODIFIED — added BuildTraverseFromNode)
- src/Hexalith.Memories.Server/Graph/GraphQueryBuilder.cs (MODIFIED — implemented BuildTraverseFromNode)
- src/Hexalith.Memories.Server/Program.cs (MODIFIED — DI registration + endpoint update)
- tests/Hexalith.Memories.Server.Tests/Search/GraphScopedSearchTests.cs (NEW)
- tests/Hexalith.Memories.Server.Tests/Graph/GraphQueryBuilderTests.cs (MODIFIED — added traversal tests)
- tests/Hexalith.Memories.IntegrationTests/Fixtures/CompositeSearchFixture.cs (NEW)
- tests/Hexalith.Memories.IntegrationTests/Search/GraphScopedSearchIntegrationTests.cs (NEW)
