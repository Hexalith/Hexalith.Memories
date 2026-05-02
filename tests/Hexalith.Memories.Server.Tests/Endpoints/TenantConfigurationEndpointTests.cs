// <copyright file="TenantConfigurationEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.IO;
using System.Net;
using System.Text.Json;

using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 5.5 AC3 tests. Endpoint behavior is wired through minimal-API delegates in
/// <c>Program.cs</c>; these tests verify the composable pieces (input validation, shape of
/// <see cref="TenantStatusGuard.ToHttpResult(ErrorResponse)"/>, contract serialization) so the
/// delegate stays thin and the behavior is protected.
/// </summary>
public class TenantConfigurationEndpointTests
{
    // ToHttpResult mutation-guard (Task 5.2) — parameterized over every TENANT_* code.
    // Protects 5-4's fix: any change that routes a non-not-found code to 404, or vice versa, fails.
    [Theory]
    [InlineData("TENANT_NOT_FOUND", StatusCodes.Status404NotFound)]
    [InlineData("TENANT_DELETING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_PROVISIONING", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_FAILED", StatusCodes.Status409Conflict)]
    [InlineData("TENANT_UNAVAILABLE", StatusCodes.Status409Conflict)]
    public void ToHttpResult_RoutesStatusCodesCorrectly(string code, int expectedStatus)
    {
        ErrorResponse error = new(code, $"{code} message", "suggestion");

        IResult result = TenantStatusGuard.ToHttpResult(error);

        result.ShouldNotBeNull();
        int actualStatus = result switch
        {
            Microsoft.AspNetCore.Http.HttpResults.NotFound<ErrorResponse> => StatusCodes.Status404NotFound,
            Microsoft.AspNetCore.Http.HttpResults.Conflict<ErrorResponse> => StatusCodes.Status409Conflict,
            _ => -1,
        };
        actualStatus.ShouldBe(expectedStatus);
    }

    [Fact]
    public void TenantUpdateInput_SerializesAndDeserializesDisplayNameRoundTrip()
    {
        TenantUpdateInput input = new("New Display Name");

        string json = JsonSerializer.Serialize(input, MemoriesJsonContext.Options);
        TenantUpdateInput? deserialized = JsonSerializer.Deserialize<TenantUpdateInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.DisplayName.ShouldBe("New Display Name");
        json.ShouldContain("\"displayName\"");
    }

    [Fact]
    public void TenantSummary_SerializesWithAllRequiredFields()
    {
        TenantSummary summary = new()
        {
            Id = "acme",
            DisplayName = "Acme Corp",
            Status = TenantStatus.Active,
            CreatedAt = new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
            MemoryUnitCount = 42L,
            IndexSizes = new TenantIndexSizes(100, 100, 50),
            IndexStatus = new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready),
            ReindexRequired = false,
            LastActivityAt = new DateTimeOffset(2026, 4, 13, 12, 0, 0, TimeSpan.Zero),
        };

        string json = JsonSerializer.Serialize(summary, MemoriesJsonContext.Options);
        TenantSummary? deserialized = JsonSerializer.Deserialize<TenantSummary>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe("acme");
        deserialized.MemoryUnitCount.ShouldBe(42L);
        deserialized.IndexSizes.RediSearchKeyCount.ShouldBe(100L);
        deserialized.IndexStatus.FalkorDb.ShouldBe(IndexHealth.Ready);
        deserialized.ReindexRequired.ShouldBeFalse();
        deserialized.LastActivityAt.ShouldNotBeNull();
    }

    [Fact]
    public void TenantSummary_NullableCountsSerializeAsNull()
    {
        TenantSummary summary = new()
        {
            Id = "acme",
            DisplayName = "Acme",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            MemoryUnitCount = null,
            IndexSizes = new TenantIndexSizes(null, null, null),
            IndexStatus = new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            ReindexRequired = false,
            LastActivityAt = null,
        };

        string json = JsonSerializer.Serialize(summary, MemoriesJsonContext.Options);
        json.ShouldContain("\"memoryUnitCount\":null");
        json.ShouldContain("\"lastActivityAt\":null");
        json.ShouldContain("\"rediSearchKeyCount\":null");
    }

    [Fact]
    public void IndexHealth_SerializesAsCamelCaseString()
    {
        string json = JsonSerializer.Serialize(new TenantIndexStatus(
            IndexHealth.Ready,
            IndexHealth.Missing,
            IndexHealth.Unknown), MemoriesJsonContext.Options);

        json.ShouldContain("\"rediSearch\":\"ready\"");
        json.ShouldContain("\"redisVector\":\"missing\"");
        json.ShouldContain("\"falkorDb\":\"unknown\"");
    }

    [Fact]
    public void TenantConfigurationView_EmbedsFullEmbeddingConfig_NotProjected()
    {
        TenantEmbeddingConfig config = EmbeddingProviderDefaults.Ollama();
        TenantConfigurationView view = new()
        {
            Id = "acme",
            DisplayName = "Acme",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            EmbeddingConfig = config,
            IndexStatus = new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready),
        };

        string json = JsonSerializer.Serialize(view, MemoriesJsonContext.Options);
        // apiSecretKeyName is non-sensitive and should appear (Amendment C).
        json.ShouldContain("\"apiSecretKeyName\":\"memories-embedding-client-secret\"");
        json.ShouldContain("\"provider\":\"ollama\"");
        json.ShouldContain("\"model\":\"qwen3-embedding:4b\"");
        json.ShouldContain("\"dimensions\":2560");
        json.ShouldContain("\"rateLimitPerMinute\":6000");
        json.ShouldContain("\"reindexRequired\":false");
        json.ShouldContain("\"baseUrl\":\"https://llm.tache.ai\"");
        json.ShouldContain("\"authMode\":\"oidc-client-credentials\"");
        json.ShouldContain("\"oidcTokenEndpoint\":\"https://auth.tache.ai/realms/tache/protocol/openid-connect/token\"");
        json.ShouldContain("\"oidcClientId\":\"memories-embedding\"");
        json.ShouldContain("\"oidcScope\":\"openid\"");
        // Assert exact JSON key shape rather than bare substrings so future field renames
        // (e.g. an unrelated `oidcClientSecretName` metadata reference) do not falsely fail.
        // apiSecretKeyName is a DAPR secret-name reference; raw client secrets must not appear.
        json.ShouldNotContain("\"client_secret\":");
        json.ShouldNotContain("\"clientSecret\":");
        json.ShouldNotContain("resolved-secret-value");
        json.ShouldNotContain("super-secret-client-secret");
    }

    [Fact]
    public async Task GetTenantConfigurationAsync_WhenTenantDisappearsAfterExistsCheck_ReturnsNotFound()
    {
        TenantRegistryEntry existing = CreateEntry("acme", "Acme", TenantStatus.Active);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(existing, (TenantRegistryEntry?)null);

        TenantRegistryService registry = new(daprClient, CreateLogger<TenantRegistryService>());
        TenantStatusGuard guard = new(registry);
        TenantMetricsService metrics = CreateMetricsService();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();

        IResult result = await TenantEndpointHandlers.GetTenantConfigurationAsync(
            registry,
            guard,
            metrics,
            actorProxyFactory,
            "acme",
            CancellationToken.None);

        (int statusCode, ErrorResponse? error) = await ExecuteErrorResultAsync(result);

        statusCode.ShouldBe(StatusCodes.Status404NotFound);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
        actorProxyFactory.DidNotReceiveWithAnyArgs().CreateActorProxy<ITenantConfigurationActor>(default!, default!);
    }

    [Fact]
    public async Task GetTenantConfigurationAsync_WhenActorThrowsDaprException_Returns503()
    {
        TenantRegistryEntry existing = CreateEntry("acme", "Acme", TenantStatus.Active);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(existing);

        TenantRegistryService registry = new(daprClient, CreateLogger<TenantRegistryService>());
        TenantStatusGuard guard = new(registry);
        TenantMetricsService metrics = CreateMetricsService();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(Task.FromException<TenantEmbeddingConfig>(new Dapr.DaprException("down")));
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<Dapr.Actors.ActorId>(), Arg.Any<string>())
            .Returns(actor);

        IResult result = await TenantEndpointHandlers.GetTenantConfigurationAsync(
            registry,
            guard,
            metrics,
            actorProxyFactory,
            "acme",
            CancellationToken.None);

        (int statusCode, ErrorResponse? error) = await ExecuteErrorResultAsync(result);

        statusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("DAPR_UNAVAILABLE");
    }

    [Fact]
    public async Task PatchDisplayNameAsync_WhenTenantTurnsDeletingDuringUpdate_ReturnsConflict()
    {
        TenantRegistryEntry active = CreateEntry("acme", "Acme", TenantStatus.Active);
        TenantRegistryEntry deleting = CreateEntry("acme", "Acme", TenantStatus.Deleting, "delete-acme");
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(active, deleting);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((deleting, "etag-1"));

        TenantRegistryService registry = new(daprClient, CreateLogger<TenantRegistryService>());
        TenantStatusGuard guard = new(registry);
        TenantMetricsService metrics = CreateMetricsService();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        IResult result = await TenantEndpointHandlers.PatchDisplayNameAsync(
            registry,
            guard,
            metrics,
            actorProxyFactory,
            httpContext,
            "acme",
            new TenantUpdateInput("Acme Renamed"),
            CancellationToken.None);

        (int statusCode, ErrorResponse? error) = await ExecuteErrorResultAsync(result);

        statusCode.ShouldBe(StatusCodes.Status409Conflict);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_DELETING");
    }

    [Fact]
    public async Task PatchDisplayNameAsync_WhenConcurrentConflictPersists_ReturnsConflict()
    {
        TenantRegistryEntry active = CreateEntry("acme", "Acme", TenantStatus.Active);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(active, active);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((active, "etag-1"));
        daprClient.TrySaveStateAsync(
                "statestore",
                "tenant-registry-acme",
                Arg.Any<TenantRegistryEntry>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        TenantRegistryService registry = new(daprClient, CreateLogger<TenantRegistryService>());
        TenantStatusGuard guard = new(registry);
        TenantMetricsService metrics = CreateMetricsService();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        DefaultHttpContext httpContext = new();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        IResult result = await TenantEndpointHandlers.PatchDisplayNameAsync(
            registry,
            guard,
            metrics,
            actorProxyFactory,
            httpContext,
            "acme",
            new TenantUpdateInput("Acme Renamed"),
            CancellationToken.None);

        (int statusCode, ErrorResponse? error) = await ExecuteErrorResultAsync(result);

        statusCode.ShouldBe(StatusCodes.Status409Conflict);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_UPDATE_CONFLICT");
    }

    private static TenantMetricsService CreateMetricsService()
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetEndPoints(Arg.Any<bool>()).Returns([]);

        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        IDatabase falkorDb = Substitute.For<IDatabase>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDb);

        return new TenantMetricsService(redis, falkor, CreateLogger<TenantMetricsService>());
    }

    private static ILogger<T> CreateLogger<T>()
        => Substitute.For<ILogger<T>>();

    private static TenantRegistryEntry CreateEntry(string tenantId, string displayName, TenantStatus status, string? workflowInstanceId = null)
        => new(new TenantInfo(tenantId, displayName, status, DateTimeOffset.UtcNow), workflowInstanceId);

    private static async Task<(int StatusCode, ErrorResponse? Error)> ExecuteErrorResultAsync(IResult result)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DefaultHttpContext context = new();
        context.RequestServices = serviceProvider;
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body);
        string body = await reader.ReadToEndAsync();
        ErrorResponse? error = string.IsNullOrWhiteSpace(body)
            ? null
            : JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options);
        return (context.Response.StatusCode, error);
    }
}
