// <copyright file="TenantExportServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Export;

using System.Net;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Export;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class TenantExportServiceTests
{
    [Fact]
    public async Task CaptureSnapshotAsync_WhenRedisServerUnavailableForTenantExport_ThrowsRedisConnectionException()
    {
        TenantRegistryEntry entry = CreateEntry();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetEndPoints(Arg.Any<bool>()).Returns([]);

        TenantExportService service = CreateService(entry, redis: redis);

        await Should.ThrowAsync<RedisConnectionException>(() => service.CaptureSnapshotAsync("acme", caseId: null, CancellationToken.None));
    }

    [Fact]
    public async Task CaptureSnapshotAsync_WhenTenantConfigurationActorFails_PropagatesDaprException()
    {
        TenantRegistryEntry entry = CreateEntry();
        IConnectionMultiplexer redis = CreateConnectedRedis();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(Task.FromException<TenantEmbeddingConfig>(new Dapr.DaprException("down")));
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(actor);

        TenantExportService service = CreateService(entry, redis: redis, actorProxyFactory: actorProxyFactory);

        await Should.ThrowAsync<Dapr.DaprException>(() => service.CaptureSnapshotAsync("acme", caseId: null, CancellationToken.None));
    }

    [Fact]
    public async Task CaptureSnapshotAsync_TenantExport_UsesRegistryLastUpdatedInTenantConfig()
    {
        DateTimeOffset createdAt = new(2026, 4, 20, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastUpdated = new(2026, 4, 20, 9, 30, 0, TimeSpan.Zero);
        TenantRegistryEntry entry = CreateEntry(createdAt, lastUpdated);
        IConnectionMultiplexer redis = CreateConnectedRedis();
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        TenantEmbeddingConfig embeddingConfig = EmbeddingProviderDefaults.Google();
        actor.GetEmbeddingConfigAsync().Returns(embeddingConfig);
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(actor);

        TenantExportService service = CreateService(entry, redis: redis, actorProxyFactory: actorProxyFactory);

        ExportSnapshot snapshot = await service.CaptureSnapshotAsync("acme", caseId: null, CancellationToken.None);

        snapshot.TenantConfig.ShouldNotBeNull();
        snapshot.TenantConfig.CreatedAt.ShouldBe(createdAt);
        snapshot.TenantConfig.LastUpdated.ShouldBe(lastUpdated);
        snapshot.TenantConfig.Configuration.EmbeddingConfig.Provider.ShouldBe(embeddingConfig.Provider);
        snapshot.TenantConfig.Configuration.EmbeddingConfig.Model.ShouldBe(embeddingConfig.Model);
    }

    private static TenantExportService CreateService(
        TenantRegistryEntry entry,
        IConnectionMultiplexer? redis = null,
        IActorProxyFactory? actorProxyFactory = null)
    {
        redis ??= CreateConnectedRedis();

        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);

        TenantRegistryService tenantRegistry = new(daprClient, Substitute.For<ILogger<TenantRegistryService>>());
        TenantMetricsService tenantMetrics = new(redis, CreateFalkorDown(), Substitute.For<ILogger<TenantMetricsService>>());

        return new TenantExportService(
            redis,
            CreateFalkorDown(),
            Substitute.For<IGraphQueryBuilder>(),
            null!,
            tenantRegistry,
            tenantMetrics,
            actorProxyFactory ?? Substitute.For<IActorProxyFactory>(),
            Substitute.For<ILogger<TenantExportService>>());
    }

    private static TenantRegistryEntry CreateEntry(
        DateTimeOffset? createdAt = null,
        DateTimeOffset? lastUpdated = null)
    {
        DateTimeOffset effectiveCreatedAt = createdAt ?? new DateTimeOffset(2026, 4, 20, 8, 0, 0, TimeSpan.Zero);
        return new TenantRegistryEntry(
            new TenantInfo("acme", "Acme", TenantStatus.Active, effectiveCreatedAt),
            WorkflowInstanceId: null,
            LastUpdated: lastUpdated ?? effectiveCreatedAt);
    }

    private static IConnectionMultiplexer CreateConnectedRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashGetAsync("acme:metadata", "lastActivityAt", Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        db.ExecuteAsync("FT.INFO", Arg.Any<object[]>())
            .Returns(Task.FromException<RedisResult>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis down")));

        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(pattern: IndexSchemaDefinitions.GetSyntacticKeyPrefix("acme") + "*", pageSize: 1000).Returns(EmptyKeys());

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([new DnsEndPoint("localhost", 6379)]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);
        return redis;
    }

    private static IConnectionMultiplexer CreateFalkorDown()
    {
        IConnectionMultiplexer falkor = Substitute.For<IConnectionMultiplexer>();
        IDatabase falkorDb = Substitute.For<IDatabase>();
        falkor.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(falkorDb);
        falkorDb.Execute(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "FalkorDB down"));
        return falkor;
    }

    private static async IAsyncEnumerable<RedisKey> EmptyKeys()
    {
        await Task.Yield();
        yield break;
    }
}
