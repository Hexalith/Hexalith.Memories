namespace Hexalith.Memories.IntegrationTests.Graph;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using Shouldly;

/// <summary>
/// Integration tests for confidence promotion (FR51).
/// Tests the PATCH /api/tenants/{tenantId}/edges/confidence endpoint
/// and verifies audit trail fields on promoted edges.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class ConfidencePromotionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly GraphQueryBuilder _builder = new();

    public ConfidencePromotionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PromoteInferredEdge_Returns200WithAuditFields()
    {
        // Arrange: A→B edge with confidence=0.5, origin=inferred
        string tenantId = $"tenant-promo-inferred-{Guid.NewGuid():N}";
        string caseId = "case-promo-inferred";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-A", "MU-B", EdgeType.References, 0.5f, EdgeOrigin.Inferred);

        // Act: Promote to 1.0
        var request = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.References, 1.0f, "user@test.com");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConfidencePromotionResult? result = await response.Content.ReadFromJsonAsync<ConfidencePromotionResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.PreviousConfidence.ShouldBe(0.5f);
        result.NewConfidence.ShouldBe(1.0f);
        result.VerifiedBy.ShouldBe("user@test.com");

        // Verify via traversal that edge now shows updated fields
        using HttpResponseMessage traverseResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=1&edgeTypes=references");
        TraversalResult? traverseResult = await traverseResponse.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        traverseResult.ShouldNotBeNull();
        TraversalNode startNode = traverseResult.Nodes.Single(n => n.MemoryUnitId == "MU-A");
        TraversalEdgeInfo promotedEdge = startNode.Edges.Single(e => e.ConnectedNodeId == "MU-B");
        promotedEdge.Confidence.ShouldBe(1.0f);
        promotedEdge.VerifiedBy.ShouldBe("user@test.com");
        promotedEdge.PreviousConfidence.ShouldBe(0.5f);
    }

    [Fact]
    public async Task PromoteExplicitEdge_SucceedsAndPreservesOrigin()
    {
        // Arrange: CausedBy edge with confidence=1.0, origin=explicit
        string tenantId = $"tenant-promo-explicit-{Guid.NewGuid():N}";
        string caseId = "case-promo-explicit";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-A", "MU-B", EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit);

        // Act: Promote to 0.9
        var request = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.CausedBy, 0.9f, "auditor");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConfidencePromotionResult? result = await response.Content.ReadFromJsonAsync<ConfidencePromotionResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.PreviousConfidence.ShouldBe(1.0f);
        result.NewConfidence.ShouldBe(0.9f);

        using HttpResponseMessage traverseResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?startNodeId=MU-A&depth=1&edgeTypes=causedBy");
        TraversalResult? traverseResult = await traverseResponse.Content.ReadFromJsonAsync<TraversalResult>(MemoriesJsonContext.Options);
        traverseResult.ShouldNotBeNull();
        TraversalNode startNode = traverseResult.Nodes.Single(n => n.MemoryUnitId == "MU-A");
        TraversalEdgeInfo promotedEdge = startNode.Edges.Single(e => e.ConnectedNodeId == "MU-B");
        promotedEdge.Origin.ShouldBe(EdgeOrigin.Explicit);
    }

    [Fact]
    public async Task DoublePromotion_PreservesAuditChain()
    {
        // Arrange
        string tenantId = $"tenant-promo-double-{Guid.NewGuid():N}";
        string caseId = "case-promo-double";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-B", caseId);
        await CreateEdgeAsync(falkor, tenantId, "MU-A", "MU-B", EdgeType.References, 0.5f, EdgeOrigin.Inferred);

        // Act 1: First promotion 0.5 → 0.8
        var request1 = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.References, 0.8f, "user1");
        using HttpResponseMessage response1 = await PatchConfidenceAsync(tenantId, request1);
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConfidencePromotionResult? result1 = await response1.Content.ReadFromJsonAsync<ConfidencePromotionResult>(MemoriesJsonContext.Options);
        result1.ShouldNotBeNull();
        result1.PreviousConfidence.ShouldBe(0.5f);

        // Act 2: Second promotion 0.8 → 1.0
        var request2 = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.References, 1.0f, "user2");
        using HttpResponseMessage response2 = await PatchConfidenceAsync(tenantId, request2);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConfidencePromotionResult? result2 = await response2.Content.ReadFromJsonAsync<ConfidencePromotionResult>(MemoriesJsonContext.Options);
        result2.ShouldNotBeNull();
        result2.PreviousConfidence.ShouldBe(0.8f);
        result2.NewConfidence.ShouldBe(1.0f);
        result2.VerifiedBy.ShouldBe("user2");
    }

    [Fact]
    public async Task PromoteNonexistentEdge_Returns404()
    {
        string tenantId = $"tenant-promo-notfound-{Guid.NewGuid():N}";
        string caseId = "case-promo-notfound";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);

        var request = new ConfidencePromotionRequest("MU-A", "MU-NONEXISTENT", EdgeType.CausedBy, 1.0f, "user");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("EDGE_NOT_FOUND");
    }

    [Fact]
    public async Task PromoteWithInvalidConfidence_Returns400()
    {
        string tenantId = $"tenant-promo-invalid-{Guid.NewGuid():N}";
        var request = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.CausedBy, 1.5f, "user");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_CONFIDENCE");
    }

    [Fact]
    public async Task PromoteWithMissingEdgeType_Returns400()
    {
        string tenantId = $"tenant-promo-missing-edgetype-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PatchConfidenceRawAsync(
            tenantId,
            "{\"sourceNodeId\":\"MU-A\",\"targetNodeId\":\"MU-B\",\"newConfidence\":1.0,\"verifiedBy\":\"user\"}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MISSING_EDGE_TYPE");
    }

    [Fact]
    public async Task PromoteWithMissingNewConfidence_Returns400()
    {
        string tenantId = $"tenant-promo-missing-confidence-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PatchConfidenceRawAsync(
            tenantId,
            "{\"sourceNodeId\":\"MU-A\",\"targetNodeId\":\"MU-B\",\"edgeType\":\"causedBy\",\"verifiedBy\":\"user\"}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MISSING_NEW_CONFIDENCE");
    }

    [Fact]
    public async Task PromoteWithEmptyVerifiedBy_Returns400()
    {
        string tenantId = $"tenant-promo-noverifier-{Guid.NewGuid():N}";
        var request = new ConfidencePromotionRequest("MU-A", "MU-B", EdgeType.CausedBy, 1.0f, "");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MISSING_VERIFIED_BY");
    }

    [Fact]
    public async Task PromoteContainsEdge_StructuralEdgePromotable()
    {
        // Arrange: Case→MU CONTAINS edge
        string tenantId = $"tenant-promo-contains-{Guid.NewGuid():N}";
        string caseId = "case-promo-contains";
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        await CreateCaseAsync(falkor, tenantId, caseId);
        await CreateMemoryUnitAsync(falkor, tenantId, "MU-A", caseId);
        await CreateEdgeAsync(falkor, tenantId, caseId, "MU-A", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit);

        // Act
        var request = new ConfidencePromotionRequest(caseId, "MU-A", EdgeType.Contains, 0.7f, "auditor");
        using HttpResponseMessage response = await PatchConfidenceAsync(tenantId, request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ConfidencePromotionResult? result = await response.Content.ReadFromJsonAsync<ConfidencePromotionResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.PreviousConfidence.ShouldBe(1.0f);
        result.NewConfidence.ShouldBe(0.7f);
    }

    private async Task<HttpResponseMessage> PatchConfidenceAsync(string tenantId, ConfidencePromotionRequest request)
    {
        string json = JsonSerializer.Serialize(request, MemoriesJsonContext.Options);
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        return await _fixture.MemoriesClient.PatchAsync(
            $"/api/tenants/{tenantId}/edges/confidence", content);
    }

    private async Task<HttpResponseMessage> PatchConfidenceRawAsync(string tenantId, string requestJson)
    {
        using StringContent content = new(requestJson, Encoding.UTF8, "application/json");
        return await _fixture.MemoriesClient.PatchAsync(
            $"/api/tenants/{tenantId}/edges/confidence", content);
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

    private async Task CreateEdgeAsync(
        FalkorDB falkor,
        string tenantId,
        string sourceNodeId,
        string targetNodeId,
        EdgeType edgeType,
        float confidence,
        EdgeOrigin origin)
    {
        (string query, IDictionary<string, object> parameters) = _builder.BuildMergeEdge(
            sourceNodeId,
            targetNodeId,
            edgeType,
            confidence,
            origin);

        await falkor.QueryAsync(tenantId, query, parameters);
    }
}
