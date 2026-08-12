// <copyright file="ServerEndpointAuthorizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;
using Hexalith.Memories.ServiceDefaults.Health;
using Hexalith.Memories.Telemetry;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

/// <summary>Endpoint-level guards for the Memories Server fallback authorization policy.</summary>
[Trait("Category", "Unit")]
public sealed class ServerEndpointAuthorizationTests : IDisposable
{
    private readonly EventStoreWebAppFactory _factory = new();

    [Fact]
    public async Task ApiEndpoint_WithoutBearer_ReturnsBearerChallenge()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/handlers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer realm=\"hexalith-memories-server\"");
    }

    [Fact]
    public async Task ApiEndpoint_WithValidBearer_ReturnsRepresentativeApiResponse()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServerTestBearerToken.Create());

        using HttpResponseMessage response = await client.GetAsync("/api/v1/handlers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Memories-API-Experimental", out IEnumerable<string>? values).ShouldBeTrue();
        values.ShouldContain("HXL002");
    }

    [Fact]
    public async Task TenantPathEndpoint_WithMatchingTenant_ReachesEndpointBusinessLogic()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/tenants/tenant-a",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_NOT_FOUND");
        error.Code.ShouldNotBe("TENANT_FORBIDDEN");
    }

    [Theory]
    [InlineData("/api/v1/tenants/tenant-b/handlers/mismatches")]
    [InlineData("/api/v1/tenants/tenant-b/telemetry/summary")]
    [InlineData("/api/v1/tenants/tenant-b/cases/case-1/memory-units/memory-1")]
    [InlineData("/api/v1/tenants/tenant-b/traverse?startNodeId=memory-1")]
    public async Task TenantPathEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeTenantState(string path)
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.ActorProxyFactory.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task TenantPathEndpoint_WithMalformedTenant_ReturnsTenantForbiddenBeforeTenantState()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/tenants/bad~tenant/handlers/mismatches",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.ActorProxyFactory.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("syntactic")]
    [InlineData("semantic")]
    [InlineData("nl")]
    [InlineData("graph")]
    [InlineData("hybrid")]
    public async Task SearchEndpoint_WithMismatchedTenant_ReturnsTenantForbiddenBeforeSearchDependencies(string axis)
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));
        string startNode = axis == "graph" ? "&startNodeId=memory-1" : string.Empty;

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/search?tenantId=tenant-b&query=fraud&axis={axis}{startNode}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.ActorProxyFactory.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchEndpoint_WithMalformedTenant_ReturnsTenantForbiddenBeforeSearchDependencies()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/search?tenantId=bad~tenant&query=fraud&axis=syntactic",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.ActorProxyFactory.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("/api/v1/ingest", "document")]
    [InlineData("/api/v1/ingest/url", "url")]
    [InlineData("/api/v1/ingest/directory", "directory")]
    public async Task TenantScopedIngestSchedulingEndpoint_WithMismatchedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies(
        string path,
        string requestKind)
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpContent content = CreateMismatchedTenantSchedulingContent(requestKind);
        using HttpResponseMessage response = await client.PostAsync(
            path,
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.PreflightDedup.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("/api/v1/ingest", "document")]
    [InlineData("/api/v1/ingest/url", "url")]
    [InlineData("/api/v1/ingest/directory", "directory")]
    public async Task TenantScopedIngestSchedulingEndpoint_WithMalformedBodyTenant_ReturnsTenantForbiddenBeforeSchedulingDependencies(
        string path,
        string requestKind)
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: ["tenant-a"]));

        using HttpContent content = CreateSchedulingContent(requestKind, "bad~tenant");
        using HttpResponseMessage response = await client.PostAsync(
            path,
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ErrorResponse error = await ReadErrorResponseAsync(response);
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.PreflightDedup.ReceivedCalls().ShouldBeEmpty();
        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ApiEndpoint_WithInvalidBearer_ReturnsInvalidTokenChallenge()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using HttpResponseMessage response = await client.GetAsync("/api/v1/handlers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ToString()
            .ShouldBe("Bearer realm=\"hexalith-memories-server\", error=\"invalid_token\", error_description=\"The token is invalid\"");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    [InlineData("/dapr/subscribe")]
    public async Task InfrastructureEndpoint_WithoutBearer_DoesNotReturnUnauthorized(string path)
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EventIngest_WithoutBearer_DoesNotReturnUnauthorized()
    {
        using HttpClient client = CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync("/events/ingest", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ApiRoutes_OnlyDaprHealthCarriesAnonymousMetadata()
    {
        List<RouteEndpoint> apiRoutes = GetRouteEndpoints()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        apiRoutes.Count.ShouldBeGreaterThan(0);
        apiRoutes
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ShouldBe([HealthEndpointPaths.DaprApiHealth]);
    }

    [Fact]
    public void AnonymousRoutes_AreLimitedToNamedInfrastructureAndDaprActorRuntime()
    {
        string[] namedAnonymousRoutes =
        [
            "/health",
            "/alive",
            "/ready",
            HealthEndpointPaths.DaprApiHealth,
            "dapr/subscribe",
            "events/ingest",
        ];
        List<string> anonymousRoutes = GetRouteEndpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        anonymousRoutes.ShouldContain("/health");
        anonymousRoutes.ShouldContain("/alive");
        anonymousRoutes.ShouldContain("/ready");
        anonymousRoutes.ShouldContain(HealthEndpointPaths.DaprApiHealth);
        anonymousRoutes.ShouldContain("dapr/subscribe");
        anonymousRoutes.ShouldContain("events/ingest");
        anonymousRoutes.Where(IsDaprActorRuntimeRoute).ShouldNotBeEmpty();

        anonymousRoutes
            .Where(route => !namedAnonymousRoutes.Contains(route, StringComparer.OrdinalIgnoreCase))
            .Where(route => !IsDaprActorRuntimeRoute(route))
            .ShouldBeEmpty();
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private List<RouteEndpoint> GetRouteEndpoints()
    {
        EndpointDataSource dataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        return dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static bool IsDaprActorRuntimeRoute(string route) =>
        route.StartsWith("actors/", StringComparison.OrdinalIgnoreCase)
        || route.StartsWith("/actors/", StringComparison.OrdinalIgnoreCase)
        || string.Equals(route, "dapr/config", StringComparison.OrdinalIgnoreCase)
        || string.Equals(route, "/healthz", StringComparison.OrdinalIgnoreCase)
        || route.Contains("{actorType}", StringComparison.OrdinalIgnoreCase);

    private static async Task<ErrorResponse> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        return error;
    }

    private static HttpContent CreateMismatchedTenantSchedulingContent(string requestKind)
        => CreateSchedulingContent(requestKind, "tenant-b");

    private static HttpContent CreateSchedulingContent(string requestKind, string tenantId)
        => requestKind switch
        {
            "document" => JsonContent.Create(
                new IngestionInput
                {
                    TenantId = tenantId,
                    CaseId = "case-1",
                    SourceUri = "test://source",
                    ContentType = "text/plain",
                    SourceType = SourceType.Event,
                    IngestedBy = "spoofed-user",
                },
                options: MemoriesJsonContext.Options),
            "url" => JsonContent.Create(
                new UrlIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = "case-1",
                    Url = "https://example.com/evidence.txt",
                    IngestedBy = "spoofed-user",
                },
                options: MemoriesJsonContext.Options),
            "directory" => JsonContent.Create(
                new DirectoryIngestionRequest
                {
                    TenantId = tenantId,
                    CaseId = "case-1",
                    DirectoryPath = "/tmp/hexalith-memories-fixture",
                    IngestedBy = "spoofed-user",
                },
                options: MemoriesJsonContext.Options),
            _ => throw new ArgumentOutOfRangeException(nameof(requestKind), requestKind, "Unknown scheduling request kind."),
        };

    public void Dispose() => _factory.Dispose();
}
