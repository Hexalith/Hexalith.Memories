namespace Hexalith.Memories.IntegrationTests.Graph;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

/// <summary>
/// HTTP endpoint integration tests for edge type filtering on the traverse endpoint.
/// Tests validation, error responses, and parameter parsing.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TraversalEdgeTypeEndpointIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly GraphQueryBuilder _builder = new();

    public TraversalEdgeTypeEndpointIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Traverse_InvalidEdgeType_Returns400WithErrorCode()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_EDGE_TYPE");
        error.Message.ShouldContain("invalid");
    }

    [Fact]
    public async Task Traverse_MixedValidAndInvalidEdgeTypes_Returns400FailFast()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=causedBy,invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_EDGE_TYPE");
        error.Message.ShouldContain("invalid");
    }

    [Fact]
    public async Task Traverse_UnderscoreFormat_Returns400()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=caused_by");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_EDGE_TYPE");
        error.Suggestion.ShouldContain("camelCase");
    }

    [Fact]
    public async Task Traverse_EmptyEdgeTypes_Returns200WithDefaultSemanticTypes()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedSemanticAndStructuralGraphAsync(tenantId, "case-empty");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-001");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-002");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-003");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-004");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-005");
    }

    [Fact]
    public async Task Traverse_WhitespacePaddedValues_ParsesCorrectly()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedSemanticAndStructuralGraphAsync(tenantId, "case-space");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=causedBy,%20correlatedWith");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-001");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-002");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-003");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-004");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-005");
    }

    [Fact]
    public async Task Traverse_ValidEdgeTypes_Returns200()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedSemanticAndStructuralGraphAsync(tenantId, "case-valid");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2&edgeTypes=causedBy,correlatedWith");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.StartNodeId.ShouldBe("mu-001");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-001");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-002");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-003");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-004");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-005");

        TraversalNode startNode = result.Nodes.Single(n => n.MemoryUnitId == "mu-001");
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.CausedBy);
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.CorrelatedWith);
        startNode.Edges.Select(e => e.EdgeType).ShouldNotContain(EdgeType.References);
        startNode.Edges.Select(e => e.EdgeType).ShouldNotContain(EdgeType.Contains);
    }

    [Fact]
    public async Task Traverse_NoEdgeTypesParam_Returns200WithDefaultBehavior()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        await SeedSemanticAndStructuralGraphAsync(tenantId, "case-default");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?startNodeId=mu-001&depth=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TraversalResult? result = await response.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-001");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-002");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-003");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldContain("mu-004");
        result.Nodes.Select(n => n.MemoryUnitId).ShouldNotContain("mu-005");

        TraversalNode startNode = result.Nodes.Single(n => n.MemoryUnitId == "mu-001");
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.CausedBy);
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.CorrelatedWith);
        startNode.Edges.Select(e => e.EdgeType).ShouldContain(EdgeType.References);
        startNode.Edges.Select(e => e.EdgeType).ShouldNotContain(EdgeType.Contains);
    }

    private async Task SeedSemanticAndStructuralGraphAsync(string tenantId, string caseId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        (string caseQuery, IDictionary<string, object> caseParams) = _builder.BuildMergeCaseNode(caseId);
        await falkor.SelectGraph(tenantId).QueryAsync(caseQuery, caseParams);

        await CreateMemoryUnitAsync(falkor, tenantId, "mu-001", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "mu-002", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "mu-003", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "mu-004", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "mu-005", caseId);

        await CreateEdgeAsync(falkor, tenantId, "mu-001", "mu-002", EdgeType.CausedBy);
        await CreateEdgeAsync(falkor, tenantId, "mu-001", "mu-003", EdgeType.CorrelatedWith);
        await CreateEdgeAsync(falkor, tenantId, "mu-001", "mu-004", EdgeType.References);
        await CreateEdgeAsync(falkor, tenantId, caseId, "mu-001", EdgeType.Contains);
        await CreateEdgeAsync(falkor, tenantId, caseId, "mu-005", EdgeType.Contains);
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

        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters);
    }

    private async Task CreateEdgeAsync(FalkorDB falkor, string tenantId, string sourceNodeId, string targetNodeId, EdgeType edgeType)
    {
        float confidence = edgeType switch
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
            confidence,
            EdgeOrigin.Explicit);

        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters);
    }
}
