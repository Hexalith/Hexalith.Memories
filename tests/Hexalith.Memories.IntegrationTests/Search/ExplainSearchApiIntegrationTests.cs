// <copyright file="ExplainSearchApiIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>HTTP integration tests covering explain-mode search responses inside the Aspire topology.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class ExplainSearchApiIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly EmbeddingClient _embeddingClient;
    private readonly TenantEmbeddingConfig _embeddingConfig;
    private readonly GraphQueryBuilder _graphQueryBuilder = new();

    public ExplainSearchApiIntegrationTests(AspireIngestionPipelineFixture fixture)
    {
        _fixture = fixture;
        _embeddingClient = new EmbeddingClient(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<DaprClient>(),
            CreateFakeEmbeddingConfiguration(),
            CreateDevelopmentHostEnvironment());
        _embeddingConfig = EmbeddingProviderDefaults.Google();
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnDefaultSyntactic_ShouldIncludeSyntacticExplanation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string content = "default explain axis content";
        await SeedIndexedDocumentAsync(tenantId, "mu-default-explain", content);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(content)}&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("\"explanation\":");

        SearchResult? result = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].Axis.ShouldBe("syntactic");
        result.Explanation.ShouldNotBeNull();
        AssertSingleAxisExplanation(result.Explanation!, "syntactic");
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnSemanticAxis_ShouldIncludeSemanticExplanation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string content = "semantic explain axis content";
        await SeedIndexedDocumentAsync(tenantId, "mu-semantic-explain", content);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(content)}&axis=semantic&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        SearchResult? result = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Results.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Results[0].Axis.ShouldBe("semantic");
        result.Explanation.ShouldNotBeNull();
        AssertSingleAxisExplanation(result.Explanation!, "semantic");
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnPureGraphAxis_ShouldIncludeGraphExplanation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-graph-explain-a", "mu-graph-explain-b");
        await SeedSyntacticHashAsync(tenantId, "mu-graph-explain-a", "Graph explain alpha content");
        await SeedSyntacticHashAsync(tenantId, "mu-graph-explain-b", "Graph explain beta content");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&axis=graph&startNodeId=mu-graph-explain-a&depth=1&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        SearchResult? result = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-graph-explain-a");
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-graph-explain-b");
        result.Explanation.ShouldNotBeNull();
        AssertSingleAxisExplanation(result.Explanation!, "graph");
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnGraphScopedSyntactic_ShouldIncludeSyntacticExplanation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        string content = "graph scoped syntactic explain content";
        await SeedGraphChainAsync(tenantId, caseId, "mu-syntactic-scope-a", "mu-syntactic-scope-b");
        await SeedIndexedDocumentAsync(tenantId, "mu-syntactic-scope-a", content, caseId);
        await SeedIndexedDocumentAsync(tenantId, "mu-syntactic-scope-b", content, caseId);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(content)}&axis=syntactic&startNodeId=mu-syntactic-scope-a&depth=1&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        SearchResult? result = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-syntactic-scope-a");
        result.Explanation.ShouldNotBeNull();
        AssertSingleAxisExplanation(result.Explanation!, "syntactic");
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnGraphScopedSemantic_ShouldIncludeSemanticExplanation()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        string content = "graph scoped semantic explain content";
        await SeedGraphChainAsync(tenantId, caseId, "mu-semantic-scope-a", "mu-semantic-scope-b");
        await SeedIndexedDocumentAsync(tenantId, "mu-semantic-scope-a", content, caseId);
        await SeedIndexedDocumentAsync(tenantId, "mu-semantic-scope-b", content, caseId);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(content)}&axis=semantic&startNodeId=mu-semantic-scope-a&depth=1&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        SearchResult? result = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-semantic-scope-a");
        result.Explanation.ShouldNotBeNull();
        AssertSingleAxisExplanation(result.Explanation!, "semantic");
    }

    [Fact]
    public async Task GetSearch_WithExplainEnabledOnHybridGraphAxis_ShouldIncludeGraphExplanationAndWeights()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-hybrid-explain-a", "mu-hybrid-explain-b");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-explain-a", "Hybrid graph explain alpha");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-explain-b", "Hybrid graph explain beta");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=graph-traversal&axis=hybrid&axes=graph&graphStartNodeId=mu-hybrid-explain-a&depth=1&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        HybridSearchResult? result = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-explain-a");
        result.Results.Select(item => item.MemoryUnitId).ShouldContain("mu-hybrid-explain-b");
        result.Explanation.ShouldNotBeNull();
        result.Explanation!.AxisDetails.Count.ShouldBe(1);
        result.Explanation.AxisDetails.ShouldContainKey("graph");
        result.Explanation.WeightsUsed.ShouldNotBeNull();
        result.Explanation.WeightsUsed!.GraphWeight.ShouldBe(0.2);
    }

    [Fact]
    public async Task GetSearch_WithHybridExplainDisabled_ShouldOmitExplanationFromJson()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string caseId = $"case-{Guid.NewGuid():N}";
        await SeedGraphChainAsync(tenantId, caseId, "mu-hybrid-no-explain-a", "mu-hybrid-no-explain-b");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-no-explain-a", "Hybrid graph hidden explain alpha");
        await SeedSyntacticHashAsync(tenantId, "mu-hybrid-no-explain-b", "Hybrid graph hidden explain beta");

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=graph-traversal&axis=hybrid&axes=graph&graphStartNodeId=mu-hybrid-no-explain-a&depth=1&explain=false");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.ShouldNotContain("\"explanation\"");

        HybridSearchResult? result = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Explanation.ShouldBeNull();
    }

    [Fact]
    public async Task GetSearch_WithHybridGraphRequestedWithoutStartNode_ShouldExplainOnlyExecutedSemanticAxis()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();
        string content = "hybrid explain fallback content";
        await SeedIndexedDocumentAsync(tenantId, "mu-hybrid-fallback", content);

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query={Uri.EscapeDataString(content)}&axis=hybrid&axes=semantic,graph&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        HybridSearchResult? result = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Explanation.ShouldNotBeNull();
        result.Explanation!.AxisDetails.Count.ShouldBe(1);
        result.Explanation.AxisDetails.ShouldContainKey("semantic");
        result.Explanation.AxisDetails.ShouldNotContainKey("graph");
    }

    private static void AssertSingleAxisExplanation(SearchExplanation explanation, string axisName)
    {
        explanation.AxisDetails.Count.ShouldBe(1);
        explanation.AxisDetails.ShouldContainKey(axisName);
        explanation.WeightsUsed.ShouldBeNull();
    }

    private async Task SeedIndexedDocumentAsync(string tenantId, string memoryUnitId, string content, string caseId = "default-case")
    {
        float[] vector = await _embeddingClient.GenerateAsync(
            content,
            tenantId,
            _embeddingConfig,
            CancellationToken.None);

        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: memoryUnitId,
            content: content,
            caseId: caseId,
            embeddingVector: vector,
            embeddingDimensions: _embeddingConfig.Dimensions);

        var context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();

        IndexSyntacticActivity syntacticActivity = new(
            _fixture.RedisConnection,
            NullLogger<IndexSyntacticActivity>.Instance);
        await syntacticActivity.RunAsync(context, input);

        IndexSemanticActivity semanticActivity = new(
            _fixture.RedisConnection,
            NullLogger<IndexSemanticActivity>.Instance);
        await semanticActivity.RunAsync(context, input);
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

    private static IConfiguration CreateFakeEmbeddingConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "true",
            })
            .Build();

    private static IHostEnvironment CreateDevelopmentHostEnvironment()
    {
        IHostEnvironment hostEnv = Substitute.For<IHostEnvironment>();
        hostEnv.EnvironmentName.Returns("Development");
        return hostEnv;
    }
}
