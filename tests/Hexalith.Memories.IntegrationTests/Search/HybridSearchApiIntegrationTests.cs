namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class HybridSearchApiIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly GraphQueryBuilder _graphQueryBuilder = new();

    public HybridSearchApiIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetSearch_WithGraphStartNodeIdAlias_ShouldReturnHybridGraphResultsAsync()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-hybrid-a", "mu-hybrid-b");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-a", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-b", "Beta content");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=graph-traversal&axis=hybrid&axes=graph&graphStartNodeId=mu-hybrid-a&depth=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-a");
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-b");
        result.Results.All(item => item.GraphScore.HasValue).ShouldBeTrue();
    }

    [Fact]
    public async Task GetSearch_HybridSyntacticOffsetBeyondOneHundredWithinWindow_ShouldReturnFusedPageAsync()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        for (int i = 0; i < 130; i++)
        {
            await SeedIndexedDocumentAsync(
                tenantId,
                $"mu-hybrid-page-{i:D3}",
                $"pagination common window document {i:D3}");
        }

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=pagination%20common%20window%20document&axis=hybrid&axes=syntactic&maxResults=5&offset=120");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(125);
        result.Results.ShouldAllBe(static item => item.SyntacticScore.HasValue);
    }

    [Fact]
    public async Task GetSearch_HybridSyntacticOffsetBeyondCandidateWindow_ShouldReturnPaginationLimitExceededAsync()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=pagination&axis=hybrid&axes=syntactic&maxResults=1&offset=1000");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("PAGINATION_LIMIT_EXCEEDED");
        error.Message.ShouldContain("hybrid");
        error.Suggestion.ShouldContain("offset + maxResults");
    }

    private async Task SeedGraphChainAsync(string tenantId, string caseId, params string[] nodeIds)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        (string caseQuery, IDictionary<string, object> caseParams) = _graphQueryBuilder.BuildMergeCaseNode(caseId);
        await falkor.SelectGraph(tenantId).QueryAsync(caseQuery, caseParams);

        for (int i = 0; i < nodeIds.Length; i++)
        {
            await CreateMemoryUnitNodeAsync(falkor, tenantId, nodeIds[i], caseId);

            (string containsQuery, IDictionary<string, object> containsParams) = _graphQueryBuilder.BuildMergeEdge(
                caseId,
                nodeIds[i],
                EdgeType.Contains,
                EdgeTypeDefaults.Contains,
                EdgeOrigin.Explicit);
            await falkor.SelectGraph(tenantId).QueryAsync(containsQuery, containsParams);

            if (i > 0)
            {
                (string edgeQuery, IDictionary<string, object> edgeParams) = _graphQueryBuilder.BuildMergeEdge(
                    nodeIds[i - 1],
                    nodeIds[i],
                    EdgeType.CausedBy,
                    EdgeTypeDefaults.CausedBy,
                    EdgeOrigin.Explicit);
                await falkor.SelectGraph(tenantId).QueryAsync(edgeQuery, edgeParams);
            }
        }
    }

    private async Task CreateMemoryUnitNodeAsync(FalkorDB falkor, string tenantId, string memoryUnitId, string caseId)
    {
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeMemoryUnitNode(
            memoryUnitId,
            caseId,
            $"Content for {memoryUnitId}",
            $"hash-{memoryUnitId}",
            $"file:///{memoryUnitId}.txt",
            SourceType.File,
            "provider",
            3,
            "test@example.com",
            DateTimeOffset.UtcNow,
            "{}");
        await falkor.SelectGraph(tenantId).QueryAsync(query, parameters);
    }

    private async Task SeedIndexedDocumentAsync(string tenantId, string memoryUnitId, string content)
    {
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: "case-hybrid-pagination");

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        IndexSyntacticActivity activity = new(
            _fixture.RedisConnection,
            NullLogger<IndexSyntacticActivity>.Instance);

        await activity.RunAsync(context, input);
    }

    private async Task SeedSyntacticHashAsync(string tenantId, string memoryUnitId, string content)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        string key = $"{tenantId}:mu:{memoryUnitId}";
        HashEntry[] entries =
        [
            new("content", content),
            new("sourceUri", $"file:///{memoryUnitId}.txt"),
            new("sourceType", SourceType.File.ToString().ToLowerInvariant()),
        ];
        await db.HashSetAsync(key, entries);
    }
}
