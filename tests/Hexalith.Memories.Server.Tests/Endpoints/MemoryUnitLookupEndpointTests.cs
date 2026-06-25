// <copyright file="MemoryUnitLookupEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Endpoints;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 18.5 — endpoint behaviour for <c>GET …/memory-units/by-source-uri</c>. Most branches are exercised
/// against the extracted <see cref="MemoryUnitLookupEndpoint.HandleAsync"/> handler (no host) following the
/// <see cref="TenantConfigurationEndpointTests"/> precedent; the literal-route-precedence case (AC1) runs
/// through the real router via the keyed-redis-faking app factory.
/// </summary>
public sealed class MemoryUnitLookupEndpointTests
{
    private const string Tenant = "acme";
    private const string Case = "case-1";
    private const string SourceUri = "file:///doc.pdf";

    [Fact]
    public async Task HandleAsync_KeyHoldsId_Returns200WithMemoryUnitId()
    {
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri)).Returns((RedisValue)"mu-123");

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status200OK);
        MemoryUnitIdLookupResponse? payload = JsonSerializer.Deserialize<MemoryUnitIdLookupResponse>(body, MemoriesJsonContext.Options);
        payload.ShouldNotBeNull();
        payload.MemoryUnitId.ShouldBe("mu-123");
    }

    [Fact]
    public async Task HandleAsync_KeyMissing_Returns404StructuredNotFound()
    {
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status404NotFound);
        DecodeError(body).Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_TransientReservationMarker_Returns404()
    {
        // AC3 — the in-flight "reserved" marker is treated as not-found, never returned as an id.
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri))
            .Returns((RedisValue)Hexalith.Memories.EventStore.PreflightDedupReservation.ReservedValue);

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status404NotFound);
        DecodeError(body).Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_InvalidTenant_Returns400()
    {
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), "bad tenant!", Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status400BadRequest);
        DecodeError(body).Code.ShouldBe("INVALID_TENANT_ID");
        await db.DidNotReceive().StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankSourceUri_Returns400(string? sourceUri)
    {
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, sourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status400BadRequest);
        DecodeError(body).Code.ShouldBe("INVALID_SOURCE_URI");
        await db.DidNotReceive().StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task HandleAsync_RedisDown_Returns503BackendError_NotFalse404()
    {
        // AC6 — a backend outage must surface as a structured 503, never a false 404 that could trigger a
        // duplicate re-ingest downstream.
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        DecodeError(body).Code.ShouldBe("LOOKUP_BACKEND_UNAVAILABLE");
    }

    [Fact]
    public async Task HandleAsync_RedisServerError_Returns503BackendError_NotFalse404()
    {
        // AC6 — the catch is on the RedisException base class, so a server-side Redis failure (not just a
        // connection drop) maps to the structured 503 too, never a false 404.
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new RedisServerException("LOADING Redis is loading the dataset in memory"));

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        DecodeError(body).Code.ShouldBe("LOOKUP_BACKEND_UNAVAILABLE");
    }

    [Fact]
    public async Task HandleAsync_CrossTenant_Returns404()
    {
        // AC5 — the dedup key embeds the tenant; a lookup under a different tenant reads a distinct (absent)
        // key and resolves to not-found.
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri)).Returns((RedisValue)"mu-acme");

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), "other-tenant", Case, SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status404NotFound);
        DecodeError(body).Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task HandleAsync_DifferentCase_Returns404()
    {
        // AC5 — the dedup key embeds the case too; a different case misses.
        (IDatabase db, SourceUriMemoryUnitLookup lookup) = CreateLookup();
        db.StringGetAsync(DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri)).Returns((RedisValue)"mu-case1");

        IResult result = await MemoryUnitLookupEndpoint.HandleAsync(
            lookup, NullLogger<AccessTelemetryCategory>.Instance, new DefaultHttpContext(), Tenant, "other-case", SourceUri, CancellationToken.None);

        (int status, string body) = await ExecuteAsync(result);
        status.ShouldBe(StatusCodes.Status404NotFound);
        DecodeError(body).Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task Route_LiteralBySourceUri_BeatsMemoryUnitIdTemplate_Returns200()
    {
        // AC1 — through the REAL router: GET …/memory-units/by-source-uri must hit the lookup endpoint (literal
        // segment), NOT the sibling …/memory-units/{memoryUnitId} get-by-id handler. The factory fakes the keyed
        // redis, so a 200 with the lookup body proves the literal route won.
        using var factory = new TelemetryWebAppFactory();
        factory.RedisDatabase.StringGetAsync(DedupKeyBuilder.BuildKey(Tenant, Case, SourceUri))
            .Returns((RedisValue)"mu-literal-wins");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"api/tenants/{Tenant}/cases/{Case}/memory-units/by-source-uri?sourceUri={Uri.EscapeDataString(SourceUri)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MemoryUnitIdLookupResponse? payload = await response.Content
            .ReadFromJsonAsync<MemoryUnitIdLookupResponse>(MemoriesJsonContext.Options);
        payload.ShouldNotBeNull();
        payload.MemoryUnitId.ShouldBe("mu-literal-wins");
    }

    private static (IDatabase Db, SourceUriMemoryUnitLookup Lookup) CreateLookup()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, new SourceUriMemoryUnitLookup(redis));
    }

    private static ErrorResponse DecodeError(string body)
    {
        ErrorResponse? error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        return error;
    }

    private static async Task<(int StatusCode, string Body)> ExecuteAsync(IResult result)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext context = new() { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string body = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }
}
