// <copyright file="ServerEndpointRateLimitTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using NSubstitute;

using Shouldly;

/// <summary>Focused TestServer coverage for inbound ASP.NET Core rate limiting.</summary>
[Trait("Category", "Unit")]
public sealed class ServerEndpointRateLimitTests : IDisposable
{
    private readonly TelemetryWebAppFactory _factory = new();
    private readonly WebApplicationFactory<Program> _limitedFactory;

    public ServerEndpointRateLimitTests()
    {
        _limitedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("InboundRateLimiting:PermitLimit", "1");
            builder.UseSetting("InboundRateLimiting:WindowSeconds", "60");
            builder.UseSetting("InboundRateLimiting:QueueLimit", "0");
        });
    }

    [Fact]
    public async Task TenantScopedApi_WhenLimitExceeded_ReturnsSanitized429BeforeDependencies()
    {
        using HttpClient client = CreateClient(tenants: ["acme"]);

        using HttpResponseMessage first = await client.GetAsync(
            "/api/search?tenantId=acme&query=&axis=hybrid",
            TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        _factory.RedisDatabase.ClearReceivedCalls();
        _factory.FalkorDbDatabase.ClearReceivedCalls();
        _factory.DaprClient.ClearReceivedCalls();
        _factory.ActorProxyFactory.ClearReceivedCalls();

        using HttpResponseMessage rejected = await client.GetAsync(
            "/api/search?tenantId=acme&query=&axis=hybrid",
            TestContext.Current.CancellationToken);

        rejected.StatusCode.ShouldBe((HttpStatusCode)429);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        ErrorResponse error = await ReadErrorResponseAsync(rejected);
        error.Code.ShouldBe("RATE_LIMIT_EXCEEDED");

        string body = await rejected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("Bearer");
        body.ShouldNotContain("eyJ", Shouldly.Case.Insensitive);
        body.ShouldNotContain("Authorization", Shouldly.Case.Insensitive);

        _factory.RedisDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.FalkorDbDatabase.ReceivedCalls().ShouldBeEmpty();
        _factory.DaprClient.ReceivedCalls().ShouldBeEmpty();
        _factory.ActorProxyFactory.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task TenantScopedApi_PartitionsByAuthorizedTenant()
    {
        using HttpClient client = CreateClient(tenants: ["tenant-a", "tenant-b"]);

        using HttpResponseMessage tenantAFirst = await client.GetAsync(
            "/api/search?tenantId=tenant-a&query=&axis=hybrid",
            TestContext.Current.CancellationToken);
        tenantAFirst.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using HttpResponseMessage tenantBFirst = await client.GetAsync(
            "/api/search?tenantId=tenant-b&query=&axis=hybrid",
            TestContext.Current.CancellationToken);
        tenantBFirst.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using HttpResponseMessage tenantASecond = await client.GetAsync(
            "/api/search?tenantId=tenant-a&query=&axis=hybrid",
            TestContext.Current.CancellationToken);
        tenantASecond.StatusCode.ShouldBe((HttpStatusCode)429);
    }

    [Fact]
    public async Task BodyBoundIngestEndpoint_WhenLimitExceeded_Returns429AfterTenantAuthorizationFilter()
    {
        using HttpClient client = CreateClient(tenants: ["acme"]);
        IngestionInput input = new()
        {
            TenantId = "acme",
            CaseId = "case-1",
            SourceUri = "file:///empty.txt",
            ContentBytes = [],
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "operator-1",
        };

        using HttpResponseMessage first = await client.PostAsync(
            "/api/ingest",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using HttpResponseMessage rejected = await client.PostAsync(
            "/api/ingest",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe((HttpStatusCode)429);

        ErrorResponse error = await ReadErrorResponseAsync(rejected);
        error.Code.ShouldBe("RATE_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task BodyBoundIngestEndpoint_SharesTenantQuotaWithRouteAndQueryApiTraffic()
    {
        using HttpClient client = CreateClient(tenants: ["acme"]);
        IngestionInput input = new()
        {
            TenantId = "acme",
            CaseId = "case-1",
            SourceUri = "file:///empty.txt",
            ContentBytes = [],
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "operator-1",
        };

        using HttpResponseMessage first = await client.GetAsync(
            "/api/search?tenantId=acme&query=&axis=hybrid",
            TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using HttpResponseMessage rejected = await client.PostAsync(
            "/api/ingest",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe((HttpStatusCode)429);

        ErrorResponse error = await ReadErrorResponseAsync(rejected);
        error.Code.ShouldBe("RATE_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task InfrastructureEndpoint_WhenApiLimitExceeded_DoesNotReturn429()
    {
        using HttpClient client = _limitedFactory.CreateClient();

        using HttpResponseMessage first = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        using HttpResponseMessage second = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        first.StatusCode.ShouldNotBe((HttpStatusCode)429);
        second.StatusCode.ShouldNotBe((HttpStatusCode)429);
    }

    [Fact]
    public async Task TenantCreateEndpoint_RateLimitPartitionsByAuthenticatedPrincipalNotBodyTenant()
    {
        using HttpClient firstPrincipal = CreateClient("operator-a", tenants: ["tenant-a"]);
        using HttpClient secondPrincipal = CreateClient("operator-b", tenants: ["tenant-a"]);
        TenantProvisioningInput input = new("tenant-a", string.Empty);

        using HttpResponseMessage first = await firstPrincipal.PostAsync(
            "/api/tenants",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using HttpResponseMessage otherPrincipalFirst = await secondPrincipal.PostAsync(
            "/api/tenants",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        otherPrincipalFirst.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using HttpResponseMessage firstPrincipalSecond = await firstPrincipal.PostAsync(
            "/api/tenants",
            JsonContent.Create(input, options: MemoriesJsonContext.Options),
            TestContext.Current.CancellationToken);
        firstPrincipalSecond.StatusCode.ShouldBe((HttpStatusCode)429);
    }

    public void Dispose()
    {
        _limitedFactory.Dispose();
        _factory.Dispose();
    }

    private HttpClient CreateClient(string[] tenants)
        => CreateClient("operator-1", tenants);

    private HttpClient CreateClient(string subject, string[] tenants)
    {
        HttpClient client = _limitedFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(subject: subject, tenants: tenants));
        return client;
    }

    private static async Task<ErrorResponse> ReadErrorResponseAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        error.ShouldNotBeNull(body);
        return error;
    }
}
