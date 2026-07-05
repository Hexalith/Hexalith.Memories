// <copyright file="SearchEndpointContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Regression tests for the deferred Story 7.5 search/traverse contract gaps.
/// These exercise the real minimal-API delegates through <see cref="TelemetryWebAppFactory"/>
/// while stubbing only the actor and backend seams needed for each failure path.
/// </summary>
[Collection(TelemetryTestCollection.Name)]
public sealed class SearchEndpointContractTests : IDisposable
{
    private const string StoreName = "statestore";

    private readonly TelemetryWebAppFactory _factory = new();

    [Fact]
    public async Task HybridSearch_WithSeparatorOnlyAxes_ReturnsInvalidAxis()
    {
        StubTenantActive("acme-search");

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=hybrid&axes=,,,");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("INVALID_AXIS");
        error.Message.ShouldContain("at least one search axis");
        _factory.ActorProxyFactory.DidNotReceiveWithAnyArgs().CreateActorProxy<ITenantConfigurationActor>(default!, default!);
    }

    [Fact]
    public async Task SemanticSearch_WhenEmbeddingConfigActorUnavailable_ReturnsBackendUnavailable()
    {
        StubTenantActive("acme-search");
        StubSemanticConfigFailure(new Dapr.DaprException("down"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=semantic");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
        GetSingleHeaderValue(response, "Retry-After").ShouldBe("5");
    }

    [Fact]
    public async Task GraphScopedSemanticSearch_WhenEmbeddingConfigActorUnavailable_ReturnsBackendUnavailable()
    {
        StubTenantActive("acme-search");
        StubSemanticConfigFailure(new Dapr.DaprException("down"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=semantic&startNodeId=mu-1");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
        GetSingleHeaderValue(response, "Retry-After").ShouldBe("5");
    }

    [Fact]
    public async Task HybridSemanticOnly_WhenEmbeddingConfigActorUnavailable_ReturnsBackendUnavailable()
    {
        StubTenantActive("acme-search");
        StubSemanticConfigFailure(new Dapr.DaprException("down"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=hybrid&axes=semantic");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
        GetSingleHeaderValue(response, "Retry-After").ShouldBe("5");
    }

    [Fact]
    public async Task NaturalLanguageSearch_WhenEmbeddingConfigActorUnavailable_ReturnsBackendUnavailable()
    {
        StubTenantActive("acme-search");
        StubSemanticConfigFailure(new Dapr.DaprException("down"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=nl");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("BACKEND_UNAVAILABLE");
        GetSingleHeaderValue(response, "Retry-After").ShouldBe("5");
    }

    [Fact]
    public async Task GraphSearch_WhenCandidateWindowExceeded_ReturnsPaginationLimitExceeded()
    {
        StubTenantActive("acme-search");

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&axis=graph&startNodeId=mu-1&offset=901&maxResults=100");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("PAGINATION_LIMIT_EXCEEDED");
        error.Suggestion.ShouldContain("Reduce offset or maxResults");
    }

    [Fact]
    public async Task HybridSearch_WhenCandidateWindowExceeded_ReturnsPaginationLimitExceeded()
    {
        StubTenantActive("acme-search");

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/search?tenantId=acme-search&query=foo&axis=hybrid&axes=syntactic&offset=901&maxResults=100");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("PAGINATION_LIMIT_EXCEEDED");
        error.Suggestion.ShouldContain("Reduce offset or maxResults");
    }

    [Fact]
    public async Task HybridSearch_WithFusedCaseAttribution_ReturnsCaseGroupsAndEnrichedNames()
    {
        StubTenantActive("acme-search");
        StubCaseNames(("case-alpha", "Alpha Case"), ("case-beta", "Beta Case"));

        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<HybridSearchService>();
                services.AddSingleton(CreateHybridSearchServiceWithSyntacticResults(
                    new ScoredResult
                    {
                        MemoryUnitId = "mu-alpha",
                        Score = 42.0,
                        ContentSnippet = "Alpha scoped content",
                        SourceUri = "file:///alpha.txt",
                        SourceType = SourceType.File,
                        Axis = "syntactic",
                        CaseId = "case-alpha",
                        AnnotationsCount = 2,
                    },
                    new ScoredResult
                    {
                        MemoryUnitId = "mu-beta",
                        Score = 21.0,
                        ContentSnippet = "Beta scoped content",
                        SourceUri = "file:///beta.txt",
                        SourceType = SourceType.File,
                        Axis = "syntactic",
                        CaseId = "case-beta",
                        AnnotationsCount = 3,
                    }));
            });
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?tenantId=acme-search&query=calibration&axis=hybrid&axes=syntactic");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.AxesUsed.ShouldBe(["syntactic"]);
        result.Results.Count.ShouldBe(2);

        FusedScoredResult alpha = result.Results.Single(r => r.MemoryUnitId == "mu-alpha");
        alpha.CaseId.ShouldBe("case-alpha");
        alpha.CaseName.ShouldBe("Alpha Case");
        alpha.AnnotationsCount.ShouldBe(2);
        alpha.SyntacticScore.ShouldNotBeNull();

        FusedScoredResult beta = result.Results.Single(r => r.MemoryUnitId == "mu-beta");
        beta.CaseId.ShouldBe("case-beta");
        beta.CaseName.ShouldBe("Beta Case");
        beta.AnnotationsCount.ShouldBe(3);
        beta.SyntacticScore.ShouldNotBeNull();

        result.CaseGroups.ShouldNotBeNull();
        CaseGroupSummary alphaGroup = result.CaseGroups.Single(g => g.CaseId == "case-alpha");
        alphaGroup.CaseName.ShouldBe("Alpha Case");
        alphaGroup.ResultCount.ShouldBe(1);
        CaseGroupSummary betaGroup = result.CaseGroups.Single(g => g.CaseId == "case-beta");
        betaGroup.CaseName.ShouldBe("Beta Case");
        betaGroup.ResultCount.ShouldBe(1);
    }

    [Fact]
    public async Task HybridSearch_Explain_WhenNoQueryWeights_ShouldUseTenantFusionWeights()
    {
        StubTenantActive("acme-search");
        var tenantWeights = new FusionWeights
        {
            SyntacticWeight = 0.7,
            SemanticWeight = 0.2,
            NlWeight = 0.05,
            GraphWeight = 0.05,
        };
        StubFusionWeights(tenantWeights);

        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<HybridSearchService>();
                services.AddSingleton(CreateHybridSearchServiceWithSyntacticResults(MakeSyntacticResult("mu-weight")));
            });
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?tenantId=acme-search&query=weights&axis=hybrid&axes=syntactic&explain=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Explanation.ShouldNotBeNull();
        result.Explanation.WeightsUsed.ShouldBe(tenantWeights);
    }

    [Fact]
    public async Task HybridSearch_Explain_WhenQueryWeightsProvided_ShouldOverrideTenantFusionWeights()
    {
        StubTenantActive("acme-search");
        StubFusionWeights(new FusionWeights
        {
            SyntacticWeight = 0.7,
            SemanticWeight = 0.2,
            NlWeight = 0.05,
            GraphWeight = 0.05,
        });

        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<HybridSearchService>();
                services.AddSingleton(CreateHybridSearchServiceWithSyntacticResults(MakeSyntacticResult("mu-query-weight")));
            });
        });
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?tenantId=acme-search&query=weights&axis=hybrid&axes=syntactic&explain=true&syntacticWeight=1&semanticWeight=0&nlWeight=0&graphWeight=0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        HybridSearchResult? result = await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Explanation.ShouldNotBeNull();
        result.Explanation.WeightsUsed.ShouldBe(new FusionWeights
        {
            SyntacticWeight = 1.0,
            SemanticWeight = 0.0,
            NlWeight = 0.0,
            GraphWeight = 0.0,
        });
    }

    [Fact]
    public async Task HybridSearch_WhenQueryWeightsAreAllZero_ReturnsInvalidFusionWeights()
    {
        StubTenantActive("acme-search");

        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?tenantId=acme-search&query=weights&axis=hybrid&axes=syntactic&syntacticWeight=0&semanticWeight=0&nlWeight=0&graphWeight=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("INVALID_FUSION_WEIGHTS");
        error.Suggestion.ShouldContain("non-negative fusion weights");
        _factory.ActorProxyFactory.DidNotReceiveWithAnyArgs().CreateActorProxy<ITenantConfigurationActor>(default!, default!);
    }

    [Fact]
    public async Task Traverse_WhenGraphQueryTimesOut_ReturnsGraphTimeout()
    {
        StubTenantActive("acme-search");

        _factory.FalkorDbDatabase.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw new TimeoutException("graph query timed out"));
        _factory.FalkorDbDatabase.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(_ => throw new TimeoutException("graph query timed out"));

        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants/acme-search/traverse?startNodeId=mu-1");

        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("GRAPH_TIMEOUT");
        response.Headers.Contains("Retry-After").ShouldBeFalse();
    }

    public void Dispose() => _factory.Dispose();

    private void StubTenantActive(string tenantId)
    {
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, tenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<TenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key.Contains(tenantId, StringComparison.Ordinal)),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private void StubSemanticConfigFailure(Exception exception)
    {
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(Task.FromException<TenantEmbeddingConfig>(exception));
        _factory.ActorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(actor);
    }

    private void StubFusionWeights(FusionWeights weights)
    {
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetFusionWeightsAsync().Returns(weights);
        _factory.ActorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(actor);
    }

    private void StubCaseNames(params (string CaseId, string CaseName)[] cases)
    {
        IBatch batch = Substitute.For<IBatch>();
        _factory.RedisDatabase.CreateBatch(Arg.Any<object>()).Returns(batch);

        foreach ((string caseId, string caseName) in cases)
        {
            batch.HashGetAsync(
                    Arg.Is<RedisKey>(key => key == $"acme-search:case:{caseId}"),
                    Arg.Is<RedisValue>(field => field == "name"),
                    Arg.Any<CommandFlags>())
                .Returns(Task.FromResult((RedisValue)caseName));
        }
    }

    private static HybridSearchService CreateHybridSearchServiceWithSyntacticResults(params ScoredResult[] results)
    {
        Func<SearchQuery, Task<SearchResult>> syntactic = _ => Task.FromResult(new SearchResult
        {
            Results = results,
            TotalCount = results.Length,
            HasIndexedMemoryUnits = true,
            Query = "calibration",
        });

        Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> semantic =
            (_, _, _) => Task.FromException<SearchResult>(new InvalidOperationException("Semantic axis should not run."));
        Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> graph =
            (_, _, _, _) => Task.FromException<SearchResult>(new InvalidOperationException("Graph axis should not run."));

        return new HybridSearchService(syntactic, semantic, graph, NullLogger<HybridSearchService>.Instance);
    }

    private static ScoredResult MakeSyntacticResult(string memoryUnitId) => new()
    {
        MemoryUnitId = memoryUnitId,
        Score = 1.0,
        ContentSnippet = "Weighted content",
        SourceUri = "file:///weighted.txt",
        SourceType = SourceType.File,
        Axis = "syntactic",
    };

    private static async Task<ErrorResponse> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        return error;
    }

    private static string GetSingleHeaderValue(HttpResponseMessage response, string headerName)
    {
        response.Headers.TryGetValues(headerName, out IEnumerable<string>? values).ShouldBeTrue();
        values.ShouldNotBeNull();
        return values.Single();
    }
}
