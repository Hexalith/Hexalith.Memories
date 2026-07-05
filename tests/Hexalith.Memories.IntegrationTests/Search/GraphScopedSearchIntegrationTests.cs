namespace Hexalith.Memories.IntegrationTests.Search;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests verifying GraphScopedSearch against real FalkorDB + Redis Stack.
/// Validates traversal, depth limiting, tenant isolation, enrichment, and latency.
/// </summary>
[Collection("GraphSearch")]
[Trait("Category", "Integration")]
public class GraphScopedSearchIntegrationTests
{
    private readonly CompositeSearchFixture _fixture;
    private readonly GraphQueryBuilder _builder = new();

    public GraphScopedSearchIntegrationTests(CompositeSearchFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SearchAsync_ChainTraversal_ShouldReturnNodesWithinDepth()
    {
        // Arrange — seed A→B→C chain (CAUSED_BY edges)
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B", "mu-C");
        await SeedSyntacticHashAsync(tenantId, "mu-A", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-B", "Beta content");
        await SeedSyntacticHashAsync(tenantId, "mu-C", "Charlie content");

        GraphScopedSearch service = CreateService();

        // Act — traverse from A, depth 2
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-A", depth: 2);

        // Assert — A, B, and C should all be returned
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(3);
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-A");
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-B");
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-C");
    }

    [Fact]
    public async Task SearchAsync_DepthLimiting_ShouldNotReturnNodesBeyondDepth()
    {
        // Arrange — seed A→B→C chain
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B", "mu-C");
        await SeedSyntacticHashAsync(tenantId, "mu-A", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-B", "Beta content");
        await SeedSyntacticHashAsync(tenantId, "mu-C", "Charlie content");

        GraphScopedSearch service = CreateService();

        // Act — traverse from A, depth 1
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-A", depth: 1);

        // Assert — only A and B, not C
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-A");
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-B");
        result.Results.Select(r => r.MemoryUnitId).ShouldNotContain("mu-C");
    }

    [Fact]
    public async Task SearchAsync_IsolatedNode_ShouldReturnOnlyStartingNode()
    {
        // Arrange — seed isolated node (no edges)
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        (string muQuery, IDictionary<string, object> muParams) = _builder.BuildMergeMemoryUnitNode(
            "mu-isolated", "case-iso", "isolated content", "hash-iso",
            "file:///isolated.txt", SourceType.File, "provider", 3, "test@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.QueryAsync(tenantId, muQuery, muParams);
        await SeedSyntacticHashAsync(tenantId, "mu-isolated", "isolated content");

        GraphScopedSearch service = CreateService();

        // Act — traverse depth 2
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-isolated", depth: 2);

        // Assert — only the starting node
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-isolated");
    }

    [Fact]
    public async Task SearchAsync_TenantIsolation_ShouldNotReturnCrossTenantResults()
    {
        // Arrange — seed graph in tenant A only
        string tenantA = $"tenant-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-b-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantA, caseId, "mu-A", "mu-B");
        await SeedSyntacticHashAsync(tenantA, "mu-A", "Alpha content");
        await SeedSyntacticHashAsync(tenantA, "mu-B", "Beta content");

        GraphScopedSearch service = CreateService();

        // Act — traverse in tenant B
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantB, Query = string.Empty, MaxResults = 10 },
            "mu-A", depth: 2);

        // Assert — empty (zero cross-leak)
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_StartingNodeNotFound_ShouldReturnEmptyResults()
    {
        // Arrange — seed some data but query different node
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B");

        GraphScopedSearch service = CreateService();

        // Act — traverse from non-existent node
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-nonexistent", depth: 2);

        // Assert — empty, not exception
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_BidirectionalTraversal_ShouldDiscoverNodeInBothDirections()
    {
        // Arrange — seed A→B edge, traverse from B
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B");
        await SeedSyntacticHashAsync(tenantId, "mu-A", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-B", "Beta content");

        GraphScopedSearch service = CreateService();

        // Act — traverse from B (reverse direction)
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-B", depth: 1);

        // Assert — A should be discovered via bidirectional traversal
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-A");
    }

    [Fact]
    public async Task SearchAsync_MultiPathDistinct_ShouldReturnNodeOnlyOnce()
    {
        // Arrange — seed A→B, A→C, B→D, C→D (diamond pattern, D reachable via two paths)
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-A", caseId);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-B", caseId);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-C", caseId);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-D", caseId);
        await CreateCausedByEdgeAsync(falkor, tenantId, "mu-A", "mu-B");
        await CreateCausedByEdgeAsync(falkor, tenantId, "mu-A", "mu-C");
        await CreateCausedByEdgeAsync(falkor, tenantId, "mu-B", "mu-D");
        await CreateCausedByEdgeAsync(falkor, tenantId, "mu-C", "mu-D");

        await SeedSyntacticHashAsync(tenantId, "mu-A", "Alpha");
        await SeedSyntacticHashAsync(tenantId, "mu-B", "Beta");
        await SeedSyntacticHashAsync(tenantId, "mu-C", "Charlie");
        await SeedSyntacticHashAsync(tenantId, "mu-D", "Delta");

        GraphScopedSearch service = CreateService();

        // Act — traverse from A, depth 2
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-A", depth: 2);

        // Assert — D appears exactly once (DISTINCT in Cypher)
        result.Results.Count(r => r.MemoryUnitId == "mu-D").ShouldBe(1);
        result.Results.Count.ShouldBe(4); // A, B, C, D
    }

    [Fact]
    public async Task SearchAsync_DefaultTraversal_ShouldNotDiscoverSiblingsViaContainsCaseHub()
    {
        // Arrange — case hub contains siblings, while the start node also has one semantic neighbor.
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        // Create case node
        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, caseQuery, caseParams);

        // Create 3 MUs and connect to case via CONTAINS
        foreach (string muId in new[] { "mu-sibling-1", "mu-sibling-2", "mu-sibling-3" })
        {
            await CreateMemoryUnitNodeAsync(falkor, tenantId, muId, caseId);
            (string edgeQuery, IDictionary<string, object> edgeParams) = _builder.BuildMergeEdge(
                caseId, muId, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
            await falkor.QueryAsync(tenantId, edgeQuery, edgeParams);
            await SeedSyntacticHashAsync(tenantId, muId, $"Content for {muId}");
        }

        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-semantic", caseId);
        await CreateCausedByEdgeAsync(falkor, tenantId, "mu-sibling-1", "mu-semantic");
        await SeedSyntacticHashAsync(tenantId, "mu-semantic", "Semantic neighbor");

        GraphScopedSearch service = CreateService();

        // Act — default graph-scoped traversal uses semantic edges, not structural CONTAINS edges.
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-sibling-1", depth: 2);

        // Assert — semantic neighbor is reachable, structural siblings are not.
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-sibling-1");
        result.Results.Select(r => r.MemoryUnitId).ShouldContain("mu-semantic");
        result.Results.Select(r => r.MemoryUnitId).ShouldNotContain("mu-sibling-2");
        result.Results.Select(r => r.MemoryUnitId).ShouldNotContain("mu-sibling-3");
    }

    [Fact]
    public async Task SearchAsync_CaseScopedTraversal_ShouldNotReachTargetThroughCrossCaseBridge()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        const string caseA = "case-a";
        const string caseB = "case-b";
        const string startNodeId = "mu-a-start";
        const string bridgeNodeId = "mu-b-bridge";
        const string targetNodeId = "mu-a-target";
        const string directNodeId = "mu-a-direct";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateMemoryUnitNodeAsync(falkor, tenantId, startNodeId, caseA);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, bridgeNodeId, caseB);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, targetNodeId, caseA);
        await CreateMemoryUnitNodeAsync(falkor, tenantId, directNodeId, caseA);
        await CreateCausedByEdgeAsync(falkor, tenantId, startNodeId, bridgeNodeId);
        await CreateCausedByEdgeAsync(falkor, tenantId, bridgeNodeId, targetNodeId);
        await CreateCausedByEdgeAsync(falkor, tenantId, startNodeId, directNodeId);

        await SeedSyntacticHashAsync(tenantId, startNodeId, "Case A start content", caseId: caseA);
        await SeedSyntacticHashAsync(tenantId, bridgeNodeId, "Case B bridge content", caseId: caseB);
        await SeedSyntacticHashAsync(tenantId, targetNodeId, "Case A target content", caseId: caseA);
        await SeedSyntacticHashAsync(tenantId, directNodeId, "Case A direct content", caseId: caseA);

        GraphScopedSearch service = CreateService();
        SearchQuery query = new() { TenantId = tenantId, CaseId = caseA, Query = "case scoped", MaxResults = 10 };

        SearchResult pureGraphResult = await service.SearchAsync(query, startNodeId, depth: 2);

        pureGraphResult.Results.Select(r => r.MemoryUnitId).ShouldContain(startNodeId);
        pureGraphResult.Results.Select(r => r.MemoryUnitId).ShouldContain(directNodeId);
        pureGraphResult.Results.Select(r => r.MemoryUnitId).ShouldNotContain(bridgeNodeId);
        pureGraphResult.Results.Select(r => r.MemoryUnitId).ShouldNotContain(targetNodeId);

        List<IReadOnlyCollection<RedisKey>> observedKeySets = [];
        Task<SearchResult> InnerSearch(SearchQuery q, IReadOnlyCollection<RedisKey> graphScopeKeys)
        {
            observedKeySets.Add(graphScopeKeys);
            return Task.FromResult(new SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = true,
                Query = q.Query,
            });
        }

        await service.SearchAsync(
            query,
            startNodeId,
            depth: 2,
            innerSearch: null,
            CancellationToken.None,
            scopedInnerSearch: InnerSearch,
            graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSyntacticKey);

        observedKeySets.Count.ShouldBe(1);
        observedKeySets[0].ShouldContain((RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, startNodeId));
        observedKeySets[0].ShouldContain((RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, directNodeId));
        observedKeySets[0].ShouldNotContain((RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, bridgeNodeId));
        observedKeySets[0].ShouldNotContain((RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, targetNodeId));
    }

    [Fact]
    public async Task SearchAsync_NonExistentGraph_ShouldReturnEmptyResults()
    {
        // Arrange — query a tenant that has never had data
        string tenantId = $"tenant-never-{Guid.NewGuid():N}";
        GraphScopedSearch service = CreateService();

        // Act — should not throw
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-ghost", depth: 2);

        // Assert — FalkorDB auto-creates empty graphs, so no error is thrown;
        // the starting node simply isn't found, yielding empty results
        result.Results.ShouldBeEmpty();
        result.HasIndexedMemoryUnits.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchAsync_PureGraphTraversal_ShouldApplyOffsetAfterSortingGraphScope()
    {
        // Arrange — seed A→B→C chain with hashes for enrichment
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B", "mu-C");
        await SeedSyntacticHashAsync(tenantId, "mu-A", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-B", "Beta content");
        await SeedSyntacticHashAsync(tenantId, "mu-C", "Charlie content");

        GraphScopedSearch service = CreateService();

        // Act — skip the starting node, return the next graph-scoped result
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 1, Offset = 1 },
            "mu-A",
            depth: 2);

        // Assert — TotalCount reflects the filtered/enriched result count before offset
        result.TotalCount.ShouldBe(3);
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-B");
    }

    [Fact]
    public async Task SearchAsync_GraphScopedInnerSearch_ShouldApplyOffsetAfterFiltering()
    {
        // Arrange — graph scope contains A, B, C; global ranked results begin with an out-of-graph hit
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-A", "mu-B", "mu-C");

        GraphScopedSearch service = CreateService();

        List<ScoredResult> globalResults =
        [
            CreateScoredResult("mu-out-1", 0.99, "syntactic"),
            CreateScoredResult("mu-A", 0.95, "syntactic"),
            CreateScoredResult("mu-B", 0.90, "syntactic"),
            CreateScoredResult("mu-C", 0.85, "syntactic"),
        ];

        Task<SearchResult> InnerSearch(SearchQuery q) => Task.FromResult(new SearchResult
        {
            Results = globalResults.Skip(q.Offset).Take(q.MaxResults).ToList(),
            TotalCount = globalResults.Count,
            HasIndexedMemoryUnits = true,
            Query = q.Query,
        });

        // Act — skip the first graph-scoped hit (A), then return B
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "graph scoped", MaxResults = 1, Offset = 1 },
            "mu-A",
            depth: 2,
            InnerSearch);

        // Assert
        result.TotalCount.ShouldBe(3);
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-B");
        result.Results[0].Axis.ShouldBe("syntactic");
    }

    [Fact]
    public async Task SearchAsync_GraphScopedInnerSearch_ShouldPassTenantScopedKeysToInnerSearch()
    {
        // Arrange — graph scope contains only the starting node.
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-target", caseId);

        GraphScopedSearch service = CreateService();
        List<SearchQuery> observedQueries = [];
        List<IReadOnlyCollection<RedisKey>> observedKeySets = [];

        Task<SearchResult> InnerSearch(SearchQuery q, IReadOnlyCollection<RedisKey> graphScopeKeys)
        {
            observedQueries.Add(q);
            observedKeySets.Add(graphScopeKeys);
            return Task.FromResult(new SearchResult
            {
                Results = [CreateScoredResult("mu-target", 0.95, "syntactic")],
                TotalCount = 1,
                HasIndexedMemoryUnits = true,
                Query = q.Query,
            });
        }

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "target", MaxResults = 1 },
            "mu-target",
            depth: 0,
            innerSearch: null,
            CancellationToken.None,
            scopedInnerSearch: InnerSearch,
            graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSyntacticKey);

        // Assert
        observedQueries.Count.ShouldBe(1);
        observedQueries[0].Offset.ShouldBe(0);
        observedQueries[0].MaxResults.ShouldBe(1);
        observedKeySets.Count.ShouldBe(1);
        observedKeySets[0].ShouldContain((RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-target"));
        result.TotalCount.ShouldBe(1);
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-target");
        result.Results[0].Axis.ShouldBe("syntactic");
    }

    [Fact]
    public async Task SearchAsync_GraphScopedInnerSearch_PagesShouldBeDisjointAndOrdered()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-page-a", "mu-page-b", "mu-page-c");

        GraphScopedSearch service = CreateService();
        List<ScoredResult> scopedAxisResults =
        [
            CreateScoredResult("mu-page-a", 0.99, "syntactic"),
            CreateScoredResult("mu-page-b", 0.98, "syntactic"),
            CreateScoredResult("mu-page-c", 0.97, "syntactic"),
        ];

        Task<SearchResult> InnerSearch(SearchQuery q, IReadOnlyCollection<RedisKey> _) => Task.FromResult(new SearchResult
        {
            Results = scopedAxisResults.Take(q.MaxResults).ToList(),
            TotalCount = scopedAxisResults.Count,
            HasIndexedMemoryUnits = true,
            Query = q.Query,
        });

        SearchResult page1 = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "page", MaxResults = 2, Offset = 0 },
            "mu-page-a",
            depth: 2,
            innerSearch: null,
            CancellationToken.None,
            scopedInnerSearch: InnerSearch,
            graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSyntacticKey);
        SearchResult page2 = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = "page", MaxResults = 2, Offset = 2 },
            "mu-page-a",
            depth: 2,
            innerSearch: null,
            CancellationToken.None,
            scopedInnerSearch: InnerSearch,
            graphScopeKeyBuilder: IndexSchemaDefinitions.BuildSyntacticKey);

        page1.TotalCount.ShouldBe(3);
        page2.TotalCount.ShouldBe(3);
        page1.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-page-a", "mu-page-b"]);
        page2.Results.Select(r => r.MemoryUnitId).ShouldBe(["mu-page-c"]);
        page1.Results.Select(r => r.MemoryUnitId).Intersect(page2.Results.Select(r => r.MemoryUnitId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Enrichment_ShouldIncludeContentAndSourceFields()
    {
        // Arrange
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-enriched", caseId);
        await SeedSyntacticHashAsync(tenantId, "mu-enriched", "Enriched test content", "file:///enriched.pdf", SourceType.File);

        GraphScopedSearch service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-enriched", depth: 0);

        // Assert
        result.Results.Count.ShouldBe(1);
        ScoredResult item = result.Results[0];
        item.ContentSnippet.ShouldContain("Enriched test content");
        item.SourceUri.ShouldBe("file:///enriched.pdf");
        item.SourceType.ShouldBe(SourceType.File);
        item.Axis.ShouldBe("graph");
        item.Score.ShouldBe(1.0); // hop 0 = starting node
    }

    [Fact]
    public async Task SearchAsync_MissingSyntacticHash_ShouldSkipGracefully()
    {
        // Arrange — graph node exists, but no Redis hash
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        await CreateMemoryUnitNodeAsync(falkor, tenantId, "mu-nohash", caseId);

        GraphScopedSearch service = CreateService();

        // Act
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-nohash", depth: 0);

        // Assert — node was traversed but skipped in enrichment; TotalCount reflects filtered results
        result.TotalCount.ShouldBe(0); // enrichment skipped it, so filtered count is 0
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task SearchAsync_Latency_ShouldBeUnder2Seconds()
    {
        // Arrange — seed a small graph
        string tenantId = $"tenant-perf-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-p1", "mu-p2", "mu-p3");
        await SeedSyntacticHashAsync(tenantId, "mu-p1", "Perf content 1");
        await SeedSyntacticHashAsync(tenantId, "mu-p2", "Perf content 2");
        await SeedSyntacticHashAsync(tenantId, "mu-p3", "Perf content 3");

        GraphScopedSearch service = CreateService();

        // Act
        Stopwatch sw = Stopwatch.StartNew();
        SearchResult result = await service.SearchAsync(
            new SearchQuery { TenantId = tenantId, Query = string.Empty, MaxResults = 10 },
            "mu-p1", depth: 3);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.ShouldBeLessThan(2000);
        result.Results.Count.ShouldBeGreaterThan(0);
    }

    private GraphScopedSearch CreateService()
    {
        return new GraphScopedSearch(
            _fixture.FalkorDbConnection,
            _fixture.RedisConnection,
            _builder,
            NullLogger<GraphScopedSearch>.Instance);
    }

    /// <summary>Seeds a chain of memory units with CAUSED_BY edges: A→B→C...</summary>
    private async Task SeedGraphChainAsync(string tenantId, string caseId, params string[] nodeIds)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        // Create case node
        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, caseQuery, caseParams);

        for (int i = 0; i < nodeIds.Length; i++)
        {
            await CreateMemoryUnitNodeAsync(falkor, tenantId, nodeIds[i], caseId);

            // CONTAINS edge: case → MU
            (string containsQ, IDictionary<string, object> containsP) = _builder.BuildMergeEdge(
                caseId, nodeIds[i], EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
            await falkor.QueryAsync(tenantId, containsQ, containsP);

            // CAUSED_BY edge to previous node
            if (i > 0)
            {
                (string edgeQ, IDictionary<string, object> edgeP) = _builder.BuildMergeEdge(
                    nodeIds[i - 1], nodeIds[i], EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);
                await falkor.QueryAsync(tenantId, edgeQ, edgeP);
            }
        }
    }

    private async Task CreateMemoryUnitNodeAsync(FalkorDB falkor, string tenantId, string muId, string caseId)
    {
        (string q, IDictionary<string, object> p) = _builder.BuildMergeMemoryUnitNode(
            muId, caseId, $"Content for {muId}", $"hash-{muId}",
            $"file:///{muId}.txt", SourceType.File, "provider", 3,
            "test@example.com", DateTimeOffset.UtcNow, "{}");
        await falkor.QueryAsync(tenantId, q, p);
    }

    private async Task CreateCausedByEdgeAsync(FalkorDB falkor, string tenantId, string sourceId, string targetId)
    {
        (string q, IDictionary<string, object> p) = _builder.BuildMergeEdge(
            sourceId, targetId, EdgeType.CausedBy, EdgeTypeDefaults.CausedBy, EdgeOrigin.Explicit);
        await falkor.QueryAsync(tenantId, q, p);
    }

    private async Task SeedSyntacticHashAsync(
        string tenantId,
        string memoryUnitId,
        string content,
        string? sourceUri = null,
        SourceType sourceType = SourceType.File,
        string? caseId = null)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        string key = $"{tenantId}:mu:{memoryUnitId}";
        List<HashEntry> entries =
        [
            new("content", content),
            new("sourceUri", sourceUri ?? $"file:///{memoryUnitId}.txt"),
            new("sourceType", sourceType.ToString().ToLowerInvariant()),
        ];
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            entries.Add(new HashEntry("caseId", caseId));
        }

        await db.HashSetAsync(key, [.. entries]);
    }

    private static ScoredResult CreateScoredResult(string memoryUnitId, double score, string axis) => new()
    {
        MemoryUnitId = memoryUnitId,
        Score = score,
        ContentSnippet = $"Snippet for {memoryUnitId}",
        SourceUri = $"file:///{memoryUnitId}.txt",
        SourceType = SourceType.File,
        Axis = axis,
    };
}
