// <copyright file="EmbeddingVectorMigrationRedisIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Migration;

using System.Buffers.Binary;
using System.Net;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using NRedisStack.RedisStackCommands;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>Redis Stack coverage for blue/green embedding vector migration end-states.</summary>
[Collection("RedisStack")]
[Trait("Category", "Integration")]
public sealed class EmbeddingVectorMigrationRedisIntegrationTests
{
    private readonly RedisStackFixture _redis;

    public EmbeddingVectorMigrationRedisIntegrationTests(RedisStackFixture redis) => _redis = redis;

    [Fact]
    public async Task LiveMigration_768ToOllama_CutsOverAliasesRewritesStagingHashesCompletesMarkerAndKeepsTenantBIsolated()
    {
        string tenantA = $"tenant-mig-a-{Guid.NewGuid():N}";
        string tenantB = $"tenant-mig-b-{Guid.NewGuid():N}";
        const string ownerId = "owner-live-ollama";
        const string memoryUnitId = "mu-live";
        TenantEmbeddingConfig previous = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        IDatabase db = _redis.Connection.GetDatabase();
        await CleanupTenantAsync(tenantA, ownerId);
        await CleanupTenantAsync(tenantB, ownerId);

        try
        {
            CreateActiveVectorIndexes(db, tenantA, 768);
            CreateActiveVectorIndexes(db, tenantB, 768);
            await SeedMigrationUnitAsync(db, tenantA, memoryUnitId, 768);
            await SeedMigrationUnitAsync(db, tenantB, "mu-b", 768);

            ITenantConfigurationActor actor = CreateTenantConfigurationActor(previous);
            RedisEmbeddingMigrationStore store = CreateStore(actor);
            IEmbeddingMigrationVectorGenerator generator = Substitute.For<IEmbeddingMigrationVectorGenerator>();
            generator.GenerateAsync(Arg.Any<string>(), tenantA, Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(CreateVector(target.Dimensions, 0.25f)));
            EmbeddingVectorMigrationService service = new(store, generator);

            EmbeddingMigrationResult result = await service.RunAsync(
                new EmbeddingMigrationOptions
                {
                    Mode = EmbeddingMigrationMode.Live,
                    TenantId = tenantA,
                    Yes = true,
                    OwnerId = ownerId,
                    TargetProvider = target.Provider,
                    TargetModel = target.Model,
                    TargetDimensions = target.Dimensions,
                    BatchSize = 1,
                },
                CancellationToken.None);

            result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.Success);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantA), target.Dimensions);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantA), target.Dimensions);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticIndexName(tenantA), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantA), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantB), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantB), 768);
            AssertIndexMissing(db, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantB, ownerId));
            AssertIndexMissing(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantB, ownerId));

            Dictionary<string, string> raw = await ReadHashAsync(db, IndexSchemaDefinitions.BuildSemanticStagingKey(tenantA, ownerId, memoryUnitId));
            raw["embeddingProvider"].ShouldBe(target.Provider);
            raw["embeddingModel"].ShouldBe(target.Model);
            raw["embeddingDimensions"].ShouldBe(target.Dimensions.ToString(System.Globalization.CultureInfo.InvariantCulture));
            raw["memoryUnitId"].ShouldBe(memoryUnitId);
            raw["caseId"].ShouldBe("case-live");

            Dictionary<string, string> nl = await ReadHashAsync(db, IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(tenantA, ownerId, memoryUnitId));
            nl["embeddingProvider"].ShouldBe(target.Provider);
            nl["embeddingModel"].ShouldBe(target.Model);
            nl["embeddingDimensions"].ShouldBe(target.Dimensions.ToString(System.Globalization.CultureInfo.InvariantCulture));
            nl["memoryUnitId"].ShouldBe(memoryUnitId);
            nl["caseId"].ShouldBe("case-live");
            nl["naturalLanguageDescription"].ShouldBe("Customer reported a delayed shipment.");
            nl["descriptionOrigin"].ShouldBe("ai");
            nl["descriptionConfidence"].ShouldBe("0.97");
            nl["descriptionConfidenceSource"].ShouldBe("model");

            Dictionary<string, string> marker = await ReadHashAsync(db, ActiveMarkerKey(tenantA));
            marker["status"].ShouldBe(MigrationMarkerStatus.Completed);
            marker["ownerId"].ShouldBe(ownerId);
            marker["targetDimensions"].ShouldBe(target.Dimensions.ToString(System.Globalization.CultureInfo.InvariantCulture));
            marker["previousRawTarget"].ShouldBe(IndexSchemaDefinitions.GetSemanticIndexName(tenantA));
            marker["previousNaturalLanguageTarget"].ShouldBe(IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantA));
            (await db.KeyExistsAsync(LockKey(tenantA))).ShouldBeFalse();
            (await db.KeyExistsAsync(LockKey(tenantB))).ShouldBeFalse();
            (await db.KeyExistsAsync(ActiveMarkerKey(tenantB))).ShouldBeFalse();
            (await AnyKeyWithPrefixAsync(IndexSchemaDefinitions.GetSemanticStagingKeyPrefix(tenantB, ownerId))).ShouldBeFalse();
            (await AnyKeyWithPrefixAsync(IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingKeyPrefix(tenantB, ownerId))).ShouldBeFalse();
            await actor.Received(1).SetEmbeddingConfigAsync(Arg.Is<TenantEmbeddingConfig>(c => c!.Dimensions == target.Dimensions), forceReindex: false);
        }
        finally
        {
            await CleanupTenantAsync(tenantA, ownerId);
            await CleanupTenantAsync(tenantB, ownerId);
        }
    }

    [Fact]
    public async Task RollbackUnavailable_MissingPreviousTargets_FailsClosedWithoutAliasOrConfigMutation()
    {
        string tenantId = $"tenant-mig-rb-missing-{Guid.NewGuid():N}";
        const string ownerId = "owner-rollback-missing";
        TenantEmbeddingConfig previous = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        IDatabase db = _redis.Connection.GetDatabase();
        await CleanupTenantAsync(tenantId, ownerId);

        try
        {
            CreateActiveVectorIndexes(db, tenantId, 768);
            await db.HashSetAsync(
                MarkerKey(tenantId, target),
                [
                    new HashEntry("tenantId", tenantId),
                    new HashEntry("targetProvider", target.Provider),
                    new HashEntry("targetModel", target.Model),
                    new HashEntry("targetDimensions", target.Dimensions),
                    new HashEntry("migrationVersion", ownerId),
                    new HashEntry("status", MigrationMarkerStatus.Cutover),
                ]);
            await db.HashSetAsync(
                ActiveMarkerKey(tenantId),
                [
                    new HashEntry("tenantId", tenantId),
                    new HashEntry("targetProvider", target.Provider),
                    new HashEntry("targetModel", target.Model),
                    new HashEntry("targetDimensions", target.Dimensions),
                    new HashEntry("migrationVersion", ownerId),
                    new HashEntry("status", MigrationMarkerStatus.Cutover),
                ]);
            ITenantConfigurationActor actor = CreateTenantConfigurationActor(previous);
            EmbeddingVectorMigrationService service = new(CreateStore(actor), Substitute.For<IEmbeddingMigrationVectorGenerator>());

            EmbeddingMigrationResult result = await service.RunAsync(
                new EmbeddingMigrationOptions
                {
                    Mode = EmbeddingMigrationMode.Rollback,
                    TenantId = tenantId,
                    Yes = true,
                    OwnerId = ownerId,
                    TargetProvider = target.Provider,
                    TargetModel = target.Model,
                    TargetDimensions = target.Dimensions,
                },
                CancellationToken.None);

            result.ExitCode.ShouldBe(EmbeddingMigrationExitCodes.DomainError);
            result.Message.ShouldContain("Rollback failed closed");
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), 768);
            await actor.DidNotReceive().SetEmbeddingConfigAsync(Arg.Any<TenantEmbeddingConfig>(), Arg.Any<bool>());
        }
        finally
        {
            await CleanupTenantAsync(tenantId, ownerId);
        }
    }

    [Fact]
    public async Task AbortPreCutover_CleansStagingStateReleasesLockAndLeavesActiveAliases()
    {
        string tenantId = $"tenant-mig-abort-pre-{Guid.NewGuid():N}";
        const string ownerId = "owner-abort-pre";
        const string memoryUnitId = "mu-abort-pre";
        TenantEmbeddingConfig previous = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        IDatabase db = _redis.Connection.GetDatabase();
        await CleanupTenantAsync(tenantId, ownerId);

        try
        {
            CreateActiveVectorIndexes(db, tenantId, 768);
            await SeedMigrationUnitAsync(db, tenantId, memoryUnitId, 768);
            ITenantConfigurationActor actor = CreateTenantConfigurationActor(previous);
            RedisEmbeddingMigrationStore store = CreateStore(actor);
            EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
                tenantId,
                previous,
                target,
                ownerId,
                TimeSpan.FromMinutes(5),
                resume: false,
                recoverStaleLock: false,
                CancellationToken.None);
            await store.PrepareStagingSemanticIndexesAsync(tenantId, target, lease.Version, CancellationToken.None);
            await store.WriteRawSemanticAsync(
                tenantId,
                target,
                new RawSemanticMigrationWrite(memoryUnitId, "case-live", "subject-live", CreateVector(target.Dimensions, 0.5f)),
                CancellationToken.None);
            await store.WriteNaturalLanguageSemanticAsync(
                tenantId,
                target,
                new NaturalLanguageSemanticMigrationWrite(memoryUnitId, "case-live", "Description", "ai", "0.9", "model", CreateVector(target.Dimensions, 0.6f)),
                CancellationToken.None);
            await store.AbortMigrationAsync(tenantId, target, lease, CancellationToken.None);

            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), 768);
            AssertIndexMissing(db, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, ownerId));
            AssertIndexMissing(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, ownerId));
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticStagingKey(tenantId, ownerId, memoryUnitId))).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(tenantId, ownerId, memoryUnitId))).ShouldBeFalse();
            (await db.KeyExistsAsync(LockKey(tenantId))).ShouldBeFalse();
            Dictionary<string, string> marker = await ReadHashAsync(db, ActiveMarkerKey(tenantId));
            marker["status"].ShouldBe(MigrationMarkerStatus.Aborted);
            await actor.DidNotReceive().SetEmbeddingConfigAsync(Arg.Any<TenantEmbeddingConfig>(), Arg.Any<bool>());
        }
        finally
        {
            await CleanupTenantAsync(tenantId, ownerId);
        }
    }

    [Fact]
    public async Task AbortPostCutover_RestoresPreviousAliasesAndConfigBeforeCleanup()
    {
        string tenantId = $"tenant-mig-abort-post-{Guid.NewGuid():N}";
        const string ownerId = "owner-abort-post";
        const string memoryUnitId = "mu-abort-post";
        TenantEmbeddingConfig previous = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        IDatabase db = _redis.Connection.GetDatabase();
        await CleanupTenantAsync(tenantId, ownerId);

        try
        {
            CreateActiveVectorIndexes(db, tenantId, 768);
            await SeedMigrationUnitAsync(db, tenantId, memoryUnitId, 768);
            ITenantConfigurationActor actor = CreateTenantConfigurationActor(previous);
            RedisEmbeddingMigrationStore store = CreateStore(actor);
            EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
                tenantId,
                previous,
                target,
                ownerId,
                TimeSpan.FromMinutes(5),
                resume: false,
                recoverStaleLock: false,
                CancellationToken.None);
            await store.PrepareStagingSemanticIndexesAsync(tenantId, target, lease.Version, CancellationToken.None);
            await store.WriteRawSemanticAsync(
                tenantId,
                target,
                new RawSemanticMigrationWrite(memoryUnitId, "case-live", "subject-live", CreateVector(target.Dimensions, 0.5f)),
                CancellationToken.None);
            await store.WriteNaturalLanguageSemanticAsync(
                tenantId,
                target,
                new NaturalLanguageSemanticMigrationWrite(memoryUnitId, "case-live", "Description", "ai", "0.9", "model", CreateVector(target.Dimensions, 0.6f)),
                CancellationToken.None);
            await store.CutoverStagingSemanticIndexesAsync(tenantId, previous, target, lease, CancellationToken.None);

            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), target.Dimensions);
            await store.AbortMigrationAsync(tenantId, target, lease, CancellationToken.None);

            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), 768);
            AssertIndexMissing(db, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, ownerId));
            AssertIndexMissing(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, ownerId));
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildSemanticStagingKey(tenantId, ownerId, memoryUnitId))).ShouldBeFalse();
            (await db.KeyExistsAsync(IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(tenantId, ownerId, memoryUnitId))).ShouldBeFalse();
            (await db.KeyExistsAsync(LockKey(tenantId))).ShouldBeFalse();
            Dictionary<string, string> marker = await ReadHashAsync(db, ActiveMarkerKey(tenantId));
            marker["status"].ShouldBe(MigrationMarkerStatus.Aborted);
            await actor.Received(1).SetEmbeddingConfigAsync(Arg.Is<TenantEmbeddingConfig>(c => c!.Dimensions == target.Dimensions), forceReindex: false);
            await actor.Received(1).SetEmbeddingConfigAsync(Arg.Is<TenantEmbeddingConfig>(c => c!.Dimensions == 768), forceReindex: false);
        }
        finally
        {
            await CleanupTenantAsync(tenantId, ownerId);
        }
    }

    [Fact]
    public async Task RollbackPostCutover_RestoresPreviousAliasesAndConfigLeavesRolledBackMarkerAndReleasesLock()
    {
        string tenantId = $"tenant-mig-rollback-post-{Guid.NewGuid():N}";
        const string ownerId = "owner-rollback-post";
        const string memoryUnitId = "mu-rollback-post";
        TenantEmbeddingConfig previous = EmbeddingProviderDefaults.Google();
        TenantEmbeddingConfig target = EmbeddingProviderDefaults.Ollama();
        IDatabase db = _redis.Connection.GetDatabase();
        await CleanupTenantAsync(tenantId, ownerId);

        try
        {
            CreateActiveVectorIndexes(db, tenantId, 768);
            await SeedMigrationUnitAsync(db, tenantId, memoryUnitId, 768);
            ITenantConfigurationActor actor = CreateTenantConfigurationActor(previous);
            RedisEmbeddingMigrationStore store = CreateStore(actor);
            EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
                tenantId,
                previous,
                target,
                ownerId,
                TimeSpan.FromMinutes(5),
                resume: false,
                recoverStaleLock: false,
                CancellationToken.None);
            await store.PrepareStagingSemanticIndexesAsync(tenantId, target, lease.Version, CancellationToken.None);
            await store.WriteRawSemanticAsync(
                tenantId,
                target,
                new RawSemanticMigrationWrite(memoryUnitId, "case-live", "subject-live", CreateVector(target.Dimensions, 0.5f)),
                CancellationToken.None);
            await store.WriteNaturalLanguageSemanticAsync(
                tenantId,
                target,
                new NaturalLanguageSemanticMigrationWrite(memoryUnitId, "case-live", "Description", "ai", "0.9", "model", CreateVector(target.Dimensions, 0.6f)),
                CancellationToken.None);
            await store.CutoverStagingSemanticIndexesAsync(tenantId, previous, target, lease, CancellationToken.None);

            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), target.Dimensions);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), target.Dimensions);
            await store.RollbackMigrationAsync(tenantId, target, lease, CancellationToken.None);

            AssertIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), 768);
            AssertIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), 768);
            (await db.KeyExistsAsync(LockKey(tenantId))).ShouldBeFalse();
            Dictionary<string, string> marker = await ReadHashAsync(db, ActiveMarkerKey(tenantId));
            marker["status"].ShouldBe(MigrationMarkerStatus.RolledBack);
            await actor.Received(1).SetEmbeddingConfigAsync(Arg.Is<TenantEmbeddingConfig>(c => c!.Dimensions == target.Dimensions), forceReindex: false);
            await actor.Received(1).SetEmbeddingConfigAsync(Arg.Is<TenantEmbeddingConfig>(c => c!.Dimensions == 768), forceReindex: false);
        }
        finally
        {
            await CleanupTenantAsync(tenantId, ownerId);
        }
    }

    private RedisEmbeddingMigrationStore CreateStore(ITenantConfigurationActor actor)
    {
        IActorProxyFactory actorProxyFactory = Substitute.For<IActorProxyFactory>();
        actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(actor);
        return new RedisEmbeddingMigrationStore(_redis.Connection, null!, actorProxyFactory);
    }

    private static ITenantConfigurationActor CreateTenantConfigurationActor(TenantEmbeddingConfig current)
    {
        ITenantConfigurationActor actor = Substitute.For<ITenantConfigurationActor>();
        actor.GetEmbeddingConfigAsync().Returns(Task.FromResult(current));
        actor.SetEmbeddingConfigAsync(Arg.Any<TenantEmbeddingConfig>(), Arg.Any<bool>()).Returns(Task.CompletedTask);
        return actor;
    }

    private static void CreateActiveVectorIndexes(IDatabase db, string tenantId, int dimensions)
    {
        db.FT().Create(
            IndexSchemaDefinitions.GetSemanticIndexName(tenantId),
            IndexSchemaDefinitions.CreateSemanticParams(tenantId),
            IndexSchemaDefinitions.CreateSemanticSchema(dimensions));
        db.FT().Create(
            IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenantId),
            IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(dimensions));
        db.Execute("FT.ALIASADD", IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), IndexSchemaDefinitions.GetSemanticIndexName(tenantId));
        db.Execute("FT.ALIASADD", IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId));
    }

    private static async Task SeedMigrationUnitAsync(IDatabase db, string tenantId, string memoryUnitId, int dimensions)
    {
        byte[] vector = ConvertVector(CreateVector(dimensions, 0.1f));
        await db.HashSetAsync(
            IndexSchemaDefinitions.BuildSyntacticKey(tenantId, memoryUnitId),
            [
                new HashEntry("content", "Raw customer support note"),
                new HashEntry("caseId", "case-live"),
                new HashEntry("cloudeventSubject", "subject-live"),
            ]);
        await db.HashSetAsync(
            IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId),
            [
                new HashEntry("embedding", vector),
                new HashEntry("memoryUnitId", memoryUnitId),
                new HashEntry("caseId", "case-live"),
                new HashEntry("embeddingProvider", "google"),
                new HashEntry("embeddingModel", "models/text-embedding-004"),
                new HashEntry("embeddingDimensions", dimensions),
            ]);
        await db.HashSetAsync(
            IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId),
            [
                new HashEntry("embedding", vector),
                new HashEntry("memoryUnitId", memoryUnitId),
                new HashEntry("caseId", "case-live"),
                new HashEntry("naturalLanguageDescription", "Customer reported a delayed shipment."),
                new HashEntry("descriptionOrigin", "ai"),
                new HashEntry("descriptionConfidence", "0.97"),
                new HashEntry("descriptionConfidenceSource", "model"),
                new HashEntry("embeddingProvider", "google"),
                new HashEntry("embeddingModel", "models/text-embedding-004"),
                new HashEntry("embeddingDimensions", dimensions),
            ]);
    }

    private static void AssertIndexDimensions(IDatabase db, string indexName, int expectedDimensions)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        IndexSchemaDefinitions.TryGetVectorDimensions(info, "embedding", out int dimensions).ShouldBeTrue();
        dimensions.ShouldBe(expectedDimensions);
    }

    private static void AssertIndexMissing(IDatabase db, string indexName)
    {
        Should.Throw<RedisServerException>(() => db.Execute("FT.INFO", indexName))
            .Message.ShouldContain("Unknown index");
    }

    private static async Task<Dictionary<string, string>> ReadHashAsync(IDatabase db, RedisKey key)
    {
        HashEntry[] entries = await db.HashGetAllAsync(key);
        entries.Length.ShouldBeGreaterThan(0, $"Expected Redis hash '{key}' to exist.");
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AnyKeyWithPrefixAsync(string prefix)
    {
        foreach (EndPoint endpoint in _redis.Connection.GetEndPoints())
        {
            IServer server = _redis.Connection.GetServer(endpoint);
            await foreach (RedisKey _ in server.KeysAsync(pattern: prefix + "*"))
            {
                return true;
            }
        }

        return false;
    }

    private async Task CleanupTenantAsync(string tenantId, string version)
    {
        IDatabase db = _redis.Connection.GetDatabase();
        DropAliasIfExists(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId));
        DropAliasIfExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId));
        DropIndexIfExists(db, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, version));
        DropIndexIfExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, version));
        DropIndexIfExists(db, IndexSchemaDefinitions.GetSemanticIndexName(tenantId));
        DropIndexIfExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId));

        List<RedisKey> keys = [];
        foreach (EndPoint endpoint in _redis.Connection.GetEndPoints())
        {
            IServer server = _redis.Connection.GetServer(endpoint);
            await foreach (RedisKey key in server.KeysAsync(pattern: tenantId + ":*"))
            {
                keys.Add(key);
            }
        }

        if (keys.Count > 0)
        {
            await db.KeyDeleteAsync([.. keys]);
        }
    }

    private static void DropAliasIfExists(IDatabase db, string aliasName)
    {
        try
        {
            db.Execute("FT.ALIASDEL", aliasName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown alias", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static void DropIndexIfExists(IDatabase db, string indexName)
    {
        try
        {
            db.Execute("FT.DROPINDEX", indexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static float[] CreateVector(int dimensions, float seed)
        => Enumerable.Range(0, dimensions).Select(i => seed + (i * 0.0001f)).ToArray();

    private static byte[] ConvertVector(float[] vector)
    {
        byte[] bytes = new byte[vector.Length * sizeof(float)];
        for (int i = 0; i < vector.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector[i]);
        }

        return bytes;
    }

    private static string MarkerKey(string tenantId, TenantEmbeddingConfig config)
        => $"{tenantId}:embedding-migration:{SanitizeMarkerSegment(config.Provider).ToLowerInvariant()}:{SanitizeMarkerSegment(config.Model).ToLowerInvariant()}";

    private static string ActiveMarkerKey(string tenantId) => $"{tenantId}:embedding-migration:active";

    private static string LockKey(string tenantId) => $"{tenantId}:embedding-migration:lock";

    private static string SanitizeMarkerSegment(string value)
        => new(value.Select(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '_').ToArray());
}
