// <copyright file="SearchEndpointContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Collections.Generic;
using System.Net;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
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
