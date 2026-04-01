namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

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
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-hybrid-a", "mu-hybrid-b");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-a", "Alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-b", "Beta content");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={tenantId}&query=graph-traversal&axis=hybrid&axes=graph&graphStartNodeId=mu-hybrid-a&depth=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-a");
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-b");
        result.Results.All(item => item.GraphScore.HasValue).ShouldBeTrue();
    }

    private async Task SeedGraphChainAsync(string tenantId, string caseId, params string[] nodeIds)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        (string caseQuery, IDictionary<string, object> caseParams) = _graphQueryBuilder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, caseQuery, caseParams);

        for (int i = 0; i < nodeIds.Length; i++)
        {
            await CreateMemoryUnitNodeAsync(falkor, tenantId, nodeIds[i], caseId);

            (string containsQuery, IDictionary<string, object> containsParams) = _graphQueryBuilder.BuildMergeEdge(
                caseId,
                nodeIds[i],
                EdgeType.Contains,
                EdgeTypeDefaults.Contains,
                EdgeOrigin.Explicit);
            await falkor.QueryAsync(tenantId, containsQuery, containsParams);

            if (i > 0)
            {
                (string edgeQuery, IDictionary<string, object> edgeParams) = _graphQueryBuilder.BuildMergeEdge(
                    nodeIds[i - 1],
                    nodeIds[i],
                    EdgeType.CausedBy,
                    EdgeTypeDefaults.CausedBy,
                    EdgeOrigin.Explicit);
                await falkor.QueryAsync(tenantId, edgeQuery, edgeParams);
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
        await falkor.QueryAsync(tenantId, query, parameters);
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
