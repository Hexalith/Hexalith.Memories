// <copyright file="ServerEndpointAuthorizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Text;

using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

        using HttpResponseMessage response = await client.GetAsync("/api/handlers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer realm=\"hexalith-memories-server\"");
    }

    [Fact]
    public async Task ApiEndpoint_WithValidBearer_ReturnsRepresentativeApiResponse()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ServerTestBearerToken.Create());

        using HttpResponseMessage response = await client.GetAsync("/api/handlers", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Memories-API-Experimental", out IEnumerable<string>? values).ShouldBeTrue();
        values.ShouldContain("HXL002");
    }

    [Fact]
    public async Task ApiEndpoint_WithInvalidBearer_ReturnsInvalidTokenChallenge()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using HttpResponseMessage response = await client.GetAsync("/api/handlers", TestContext.Current.CancellationToken);

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
    public void ApiRoutes_DoNotCarryAnonymousMetadata()
    {
        List<RouteEndpoint> apiRoutes = GetRouteEndpoints()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        apiRoutes.Count.ShouldBeGreaterThan(0);
        apiRoutes
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ShouldBeEmpty();
    }

    [Fact]
    public void AnonymousRoutes_AreLimitedToNamedInfrastructureAndDaprActorRuntime()
    {
        string[] namedAnonymousRoutes =
        [
            "/health",
            "/alive",
            "/ready",
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

    public void Dispose() => _factory.Dispose();
}
