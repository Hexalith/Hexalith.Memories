namespace Hexalith.Memories.IntegrationTests.Graph;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests for gap detection in causal chain traversal (FR49).
/// Verifies that stub nodes (created by BuildMergeStubNode) are correctly
/// identified as gap markers in the traversal response.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class GapDetectionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly GraphQueryBuilder _builder = new();

    public GapDetectionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Traverse_SingleGap_StubNodeDetectedAsGapMarker()
    {
        // Arrange: A(full) ← B(stub) → C(full)
        string tenantId = $"tenant-gap-single-{Guid.NewGuid():N}";
        string caseId = "case-gap-single";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateStubNodeAsync(falkor, tenantId, "MU-B");
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-C", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-C", EdgeType.CausedBy);

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();

        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-A");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-C");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("MU-B");

        result.GapMarkers.Count.ShouldBe(1);
        result.GapMarkers[0].MissingNodeId.ShouldBe("MU-B");
        result.TotalNodeCount.ShouldBe(2);
    }

    [Fact]
    public async Task Traverse_MultipleGaps_AllFlaggedIndividually()
    {
        // Arrange: A(full) ← B(stub) → C(stub) → D(full)
        string tenantId = $"tenant-gap-multi-{Guid.NewGuid():N}";
        string caseId = "case-gap-multi";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateStubNodeAsync(falkor, tenantId, "MU-B");
        await CreateStubNodeAsync(falkor, tenantId, "MU-C");
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-D", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-C", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-C", "MU-D", EdgeType.CausedBy);

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();

        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-A");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-D");
        result.GapMarkers.Select(g => g.MissingNodeId).ShouldContain("MU-B");
        result.GapMarkers.Select(g => g.MissingNodeId).ShouldContain("MU-C");
    }

    [Fact]
    public async Task Traverse_WithCaseId_PreservesGapMarkersAndGapEdges()
    {
        // Arrange: A(full) ← B(stub) → C(stub) → D(full)
        string tenantId = $"tenant-gap-case-scope-{Guid.NewGuid():N}";
        string caseId = "case-gap-case-scope";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateStubNodeAsync(falkor, tenantId, "MU-B");
        await CreateStubNodeAsync(falkor, tenantId, "MU-C");
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-D", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-C", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-C", "MU-D", EdgeType.CausedBy);

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=5&caseId={Uri.EscapeDataString(caseId)}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();

        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-A");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-D");
        result.GapMarkers.Select(g => g.MissingNodeId).ShouldContain("MU-B");
        result.GapMarkers.Select(g => g.MissingNodeId).ShouldContain("MU-C");

        TraversalGapMarker gapB = result.GapMarkers.Single(g => g.MissingNodeId == "MU-B");
        gapB.Edges.Select(e => e.ConnectedNodeId).ShouldContain("MU-A");
        gapB.Edges.Select(e => e.ConnectedNodeId).ShouldContain("MU-C");
    }

    [Fact]
    public async Task Traverse_RetroactiveGapResolution_StubBecomesFullNode()
    {
        // Arrange: A(full) ← B(stub)
        string tenantId = $"tenant-gap-retro-{Guid.NewGuid():N}";
        string caseId = "case-gap-retro";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateStubNodeAsync(falkor, tenantId, "MU-B");
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);

        // Act 1: Traverse before ingestion — B is a gap
        using HttpResponseMessage response1 = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3");
        TraversalResult? result1 = await response1.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result1.ShouldNotBeNull();
        result1.GapMarkers.Count.ShouldBe(1);
        result1.GapMarkers[0].MissingNodeId.ShouldBe("MU-B");

        // Now ingest B fully — MERGE fills stub properties
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);

        // Act 2: Traverse after ingestion — B should be a full node
        using HttpResponseMessage response2 = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3");
        TraversalResult? result2 = await response2.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result2.ShouldNotBeNull();
        result2.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-A");
        result2.Nodes.Select(n => n.MemoryUnitId).ShouldContain("MU-B");
        result2.GapMarkers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Traverse_ContentMissingInGraphButPresentInRedis_DoesNotCreateGapMarker()
    {
        // Arrange: MU-B is fully indexed, but its graph content was removed after indexing.
        string tenantId = $"tenant-gap-fallback-{Guid.NewGuid():N}";
        string caseId = "case-gap-fallback";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);
        await SeedMemoryUnitHashAsync(redisDb, tenantId, "MU-B", "Redis fallback content for MU-B");
        await falkor.QueryAsync(
            tenantId,
            "MATCH (m:MemoryUnit {id: $id}) REMOVE m.content",
            new Dictionary<string, object> { ["id"] = "MU-B" });

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();

        result.GapMarkers.ShouldBeEmpty();
        TraversalNode recoveredNode = result.Nodes.Single(n => n.MemoryUnitId == "MU-B");
        recoveredNode.ContentSnippet.ShouldContain("Redis fallback content for MU-B");
    }

    [Fact]
    public async Task Traverse_NoGaps_FullNodesNotFlaggedAsGaps()
    {
        // Arrange: A(full) ← B(full)
        string tenantId = $"tenant-gap-none-{Guid.NewGuid():N}";
        string caseId = "case-gap-none";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Nodes.Count.ShouldBe(2);
        result.GapMarkers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Traverse_GapMarkerHasEdges()
    {
        // Arrange: A(full) ← B(stub) → C(full), B has edges to both A and C
        string tenantId = $"tenant-gap-edges-{Guid.NewGuid():N}";
        string caseId = "case-gap-edges";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateStubNodeAsync(falkor, tenantId, "MU-B");
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-C", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-A", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "MU-B", "MU-C", EdgeType.CorrelatedWith, 0.5f, EdgeOrigin.Inferred);

        // Act
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=3&edgeTypes=causedBy,correlatedWith");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();

        result.GapMarkers.Count.ShouldBe(1);
        TraversalGapMarker gap = result.GapMarkers[0];
        gap.MissingNodeId.ShouldBe("MU-B");
        gap.Edges.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    private async Task CreateCaseAsync(FalkorDB falkor, string tenantId, string caseId)
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, query, parameters);
    }

    private async Task CreateMemoryUnitAsync(FalkorDB falkor, string tenantId, string memoryUnitId, string caseId)
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeMemoryUnitNode(
            memoryUnitId,
            caseId,
            $"content for {memoryUnitId}",
            $"hash-{memoryUnitId}",
            $"file:///{memoryUnitId}.txt",
            SourceType.File,
            "provider",
            3,
            "integration@example.com",
            DateTimeOffset.UtcNow,
            "{}");

        await falkor.QueryAsync(tenantId, query, parameters);
    }

    private async Task CreateStubNodeAsync(FalkorDB falkor, string tenantId, string memoryUnitId)
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeStubNode(memoryUnitId, DateTimeOffset.UtcNow);
        await falkor.QueryAsync(tenantId, query, parameters);
    }

    private static Task SeedMemoryUnitHashAsync(IDatabase db, string tenantId, string memoryUnitId, string content)
        => db.HashSetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            [
                new HashEntry("content", content),
                new HashEntry("sourceUri", $"file:///{memoryUnitId}.txt"),
                new HashEntry("sourceType", "file"),
                new HashEntry("ingestedAt", DateTimeOffset.UtcNow.ToString("o")),
            ]);

    private async Task CreateEdgeAsync(
        FalkorDB falkor,
        string tenantId,
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float? confidence = null,
        EdgeOrigin origin = EdgeOrigin.Explicit)
    {
        float edgeConfidence = confidence ?? edgeType switch
        {
            EdgeType.CausedBy => EdgeTypeDefaults.CausedBy,
            EdgeType.CorrelatedWith => EdgeTypeDefaults.CorrelatedWith,
            EdgeType.References => EdgeTypeDefaults.References,
            EdgeType.Contains => EdgeTypeDefaults.Contains,
            EdgeType.Annotates => EdgeTypeDefaults.Annotates,
            _ => 1.0f,
        };

        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            sourceNodeId,
            targetNodeId,
            edgeType,
            edgeConfidence,
            origin);

        await falkor.QueryAsync(tenantId, query, parameters);
    }
}
