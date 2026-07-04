// <copyright file="RedisEmbeddingMigrationStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Migration;

using System.Net;

using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class RedisEmbeddingMigrationStoreTests
{
    [Fact]
    public async Task PrepareStagingSemanticIndexesAsync_LegacyNaturalLanguageHashExists_MigratesBeforeCreatingStagingIndexes()
    {
        List<string> operations = [];
        List<object[]> ftCreateCalls = [];
        IDatabase db = Substitute.For<IDatabase>();
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        IConnectionMultiplexer redis = CreateRedis(db, server);
        HashEntry[] legacyEntries = CreateNaturalLanguageHashEntries();

        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns([], legacyEntries);
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns(legacyEntries);
        db.HashSetAsync((RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                operations.Add("migrate:write-target");
                return Task.FromResult(true);
            });
        db.KeyDeleteAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                operations.Add("migrate:delete-legacy");
                return Task.FromResult(true);
            });
        db.Execute(Arg.Is<string>(command => command == "FT.DROPINDEX"), Arg.Do<object[]>(args => operations.Add($"drop:{args[0]}")))
            .Returns(RedisResult.Create(new RedisValue("OK")));
        db.Execute(
                Arg.Is<string>(command => command == "FT.CREATE"),
                Arg.Do<object[]>(args =>
                {
                    operations.Add($"create:{args[0]}");
                    ftCreateCalls.Add(args);
                }))
            .Returns(RedisResult.Create(new RedisValue("OK")));
        db.Execute(
                Arg.Is<string>(command => command == "FT.CREATE"),
                Arg.Do<ICollection<object>>(args =>
                {
                    operations.Add($"create:{args.First()}");
                    ftCreateCalls.Add([.. args]);
                }),
                Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        TenantEmbeddingConfig targetConfig = new(
            "openai",
            "text-embedding-3-small",
            1536,
            60,
            "embedding-secret");

        await store.PrepareStagingSemanticIndexesAsync("tenant-a", targetConfig, "version-1", CancellationToken.None);

        operations.Take(2).ShouldBe(["migrate:write-target", "migrate:delete-legacy"]);
        operations.ShouldNotContain("drop:" + IndexSchemaDefinitions.GetSemanticIndexName("tenant-a"));
        operations.ShouldNotContain("drop:" + IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a"));
        ftCreateCalls.Count.ShouldBe(2);
        ftCreateCalls.ShouldContain(args => ContainsArgument(args, IndexSchemaDefinitions.GetSemanticStagingKeyPrefix("tenant-a", "version-1")));
        ftCreateCalls.ShouldContain(args => ContainsArgument(args, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingKeyPrefix("tenant-a", "version-1")));
        ftCreateCalls.ShouldNotContain(args => ContainsArgument(args, IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix("tenant-a")));
    }

    [Fact]
    public async Task GetCountsAsync_LegacyNaturalLanguageHashExists_CountsItAsNaturalLanguageNotRaw()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        IConnectionMultiplexer redis = CreateRedis(db, server);
        db.HashGetAsync(
                (RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Returns([new RedisValue("openai"), new RedisValue("text-embedding-3-small"), new RedisValue("1536")]);
        TenantEmbeddingConfig targetConfig = new(
            "openai",
            "text-embedding-3-small",
            1536,
            60,
            "embedding-secret");
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        EmbeddingMigrationTenantCounts counts = await store.GetCountsAsync(
            "tenant-a",
            targetConfig,
            CancellationToken.None);

        counts.RawSemanticUnitCount.ShouldBe(0);
        counts.NaturalLanguageSemanticUnitCount.ShouldBe(1);
        counts.RawStaleMetadataCount.ShouldBe(0);
        counts.NaturalLanguageStaleMetadataCount.ShouldBe(0);
    }

    [Fact]
    public async Task StartMigrationMarkerAsync_UsesSetNxOwnerLockWithTtl()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(Task.FromResult(true));
        db.CreateTransaction(Arg.Any<object>()).Returns(transaction);
        transaction.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<HashEntry[]>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
            "tenant-a",
            EmbeddingProviderDefaults.Google(),
            EmbeddingProviderDefaults.Ollama(),
            "owner-1",
            TimeSpan.FromMinutes(5),
            resume: false,
            recoverStaleLock: false,
            CancellationToken.None);

        lease.ShouldBe(new EmbeddingMigrationLease("owner-1", "owner-1"));
        await db.Received(1).StringSetAsync(
            (RedisKey)"tenant-a:embedding-migration:lock",
            (RedisValue)"owner-1",
            TimeSpan.FromMinutes(5),
            When.NotExists);
    }

    [Fact]
    public async Task StartMigrationMarkerAsync_OtherOwnerLockExists_FailsClosed()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns((RedisValue)"owner-2");
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            store.StartMigrationMarkerAsync(
                "tenant-a",
                EmbeddingProviderDefaults.Google(),
                EmbeddingProviderDefaults.Ollama(),
                "owner-1",
                TimeSpan.FromMinutes(5),
                resume: false,
                recoverStaleLock: false,
                CancellationToken.None));

        ex.Message.ShouldContain("another active run");
        await db.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>());
    }

    [Fact]
    public async Task StartMigrationMarkerAsync_ResumePreservesPreviousRollbackFields()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        List<HashEntry[]> markerWrites = [];
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(
            [
                new("tenantId", "tenant-a"),
                new("targetProvider", "ollama"),
                new("targetModel", "qwen3-embedding:4b"),
                new("targetDimensions", "2560"),
                new("migrationVersion", "original-run"),
                new("previousProvider", "google"),
                new("previousModel", "models/text-embedding-004"),
                new("previousDimensions", "768"),
                new("previousRawTarget", IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")),
                new("previousNaturalLanguageTarget", IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")),
            ]);
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(Task.FromResult(true));
        db.CreateTransaction(Arg.Any<object>()).Returns(transaction);
        transaction.HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Do<HashEntry[]>(entries => markerWrites.Add(entries)),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        EmbeddingMigrationLease lease = await store.StartMigrationMarkerAsync(
            "tenant-a",
            EmbeddingProviderDefaults.Ollama(),
            EmbeddingProviderDefaults.Ollama(),
            "resume-owner",
            TimeSpan.FromMinutes(5),
            resume: true,
            recoverStaleLock: false,
            CancellationToken.None);

        lease.ShouldBe(new EmbeddingMigrationLease("resume-owner", "original-run"));
        markerWrites.ShouldAllBe(entries => HasEntry(entries, "ownerId", "resume-owner"));
        markerWrites.ShouldAllBe(entries => HasEntry(entries, "migrationVersion", "original-run"));
        markerWrites.ShouldAllBe(entries => !HasField(entries, "previousProvider"));
        markerWrites.ShouldAllBe(entries => !HasField(entries, "previousRawTarget"));
    }

    [Fact]
    public async Task StartMigrationMarkerAsync_WritesConsistentPerTargetAndActiveMarkerFields()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        List<RedisKey> markerKeys = [];
        List<HashEntry[]> markerWrites = [];
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(Task.FromResult(true));
        db.CreateTransaction(Arg.Any<object>()).Returns(transaction);
        transaction.HashSetAsync(
                Arg.Do<RedisKey>(key => markerKeys.Add(key)),
                Arg.Do<HashEntry[]>(entries => markerWrites.Add(entries)),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        await store.StartMigrationMarkerAsync(
            "tenant-a",
            EmbeddingProviderDefaults.Google(),
            EmbeddingProviderDefaults.Ollama(),
            "owner-1",
            TimeSpan.FromMinutes(5),
            resume: false,
            recoverStaleLock: false,
            CancellationToken.None);

        markerKeys.Count.ShouldBe(2);
        markerKeys.ShouldContain((RedisKey)"tenant-a:embedding-migration:ollama:qwen3-embedding_4b");
        markerKeys.ShouldContain((RedisKey)"tenant-a:embedding-migration:active");
        markerWrites.Count.ShouldBe(2);
        markerWrites[0].ShouldBe(markerWrites[1]);
        HashEntry[] entries = markerWrites[0];
        HasEntry(entries, "tenantId", "tenant-a").ShouldBeTrue();
        HasEntry(entries, "targetProvider", "ollama").ShouldBeTrue();
        HasEntry(entries, "targetDimensions", "2560").ShouldBeTrue();
        HasEntry(entries, "previousProvider", "google").ShouldBeTrue();
        HasEntry(entries, "status", MigrationMarkerStatus.Started).ShouldBeTrue();
        HasEntry(entries, "ownerId", "owner-1").ShouldBeTrue();
        HasEntry(entries, "activeRawTarget", IndexSchemaDefinitions.GetSemanticActiveAliasName("tenant-a")).ShouldBeTrue();
        HasEntry(entries, "stagingRawTarget", IndexSchemaDefinitions.GetSemanticStagingIndexName("tenant-a", "owner-1")).ShouldBeTrue();
        HasEntry(entries, "previousNaturalLanguageTarget", IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")).ShouldBeTrue();
    }

    [Fact]
    public async Task CompleteMigrationMarkerAsync_OwnerMismatchRefusesAndKeepsLock()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns((RedisValue)"owner-2");
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            store.CompleteMigrationMarkerAsync(
                "tenant-a",
                EmbeddingProviderDefaults.Ollama(),
                new EmbeddingMigrationLease("owner-1", "version-1"),
                CancellationToken.None));

        ex.Message.ShouldContain("no longer owns");
        await db.DidNotReceive().KeyDeleteAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CompleteMigrationMarkerAsync_OwnedLockWritesCompletedMarkerAndDeletesLock()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([]));
        List<HashEntry[]> markerWrites = [];
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns((RedisValue)"owner-1");
        db.KeyExpireAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<TimeSpan?>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        db.CreateTransaction(Arg.Any<object>()).Returns(transaction);
        transaction.HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Do<HashEntry[]>(entries => markerWrites.Add(entries)),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        await store.CompleteMigrationMarkerAsync(
            "tenant-a",
            EmbeddingProviderDefaults.Ollama(),
            new EmbeddingMigrationLease("owner-1", "version-1"),
            CancellationToken.None);

        markerWrites.Count.ShouldBe(2);
        markerWrites.ShouldAllBe(entries => HasEntry(entries, "status", MigrationMarkerStatus.Completed));
        markerWrites.ShouldAllBe(entries => HasEntry(entries, "ownerId", "owner-1"));
        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task AbortMigrationAsync_StartedMarkerDropsStagingWritesAbortedAndDeletesLock()
    {
        IDatabase db = Substitute.For<IDatabase>();
        ITransaction transaction = Substitute.For<ITransaction>();
        RedisKey rawStagingKey = IndexSchemaDefinitions.BuildSemanticStagingKey("tenant-a", "version-1", "mu-1");
        RedisKey naturalLanguageStagingKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey("tenant-a", "version-1", "mu-1");
        IConnectionMultiplexer redis = CreateRedis(db, CreateServer([rawStagingKey, naturalLanguageStagingKey]));
        List<HashEntry[]> markerWrites = [];
        db.StringGetAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>())
            .Returns((RedisValue)"owner-1");
        db.KeyExpireAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<TimeSpan?>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns([new HashEntry("status", MigrationMarkerStatus.Started)]);
        db.Execute(Arg.Is<string>(command => command == "FT.DROPINDEX"), Arg.Any<object[]>())
            .Returns(RedisResult.Create(new RedisValue("OK")));
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(1L));
        db.CreateTransaction(Arg.Any<object>()).Returns(transaction);
        transaction.HashSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Do<HashEntry[]>(entries => markerWrites.Add(entries)),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(true));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        RedisEmbeddingMigrationStore store = new(redis, null!, Substitute.For<IActorProxyFactory>());

        await store.AbortMigrationAsync(
            "tenant-a",
            EmbeddingProviderDefaults.Ollama(),
            new EmbeddingMigrationLease("owner-1", "version-1"),
            CancellationToken.None);

        db.Received(1).Execute("FT.DROPINDEX", IndexSchemaDefinitions.GetSemanticStagingIndexName("tenant-a", "version-1"));
        db.Received(1).Execute("FT.DROPINDEX", IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName("tenant-a", "version-1"));
        await db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey[]>(keys => keys.Contains(rawStagingKey)),
            Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey[]>(keys => keys.Contains(naturalLanguageStagingKey)),
            Arg.Any<CommandFlags>());
        markerWrites.Count.ShouldBe(2);
        markerWrites.ShouldAllBe(entries => HasEntry(entries, "status", MigrationMarkerStatus.Aborted));
        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-a:embedding-migration:lock", Arg.Any<CommandFlags>());
    }

    private static bool ContainsArgument(IEnumerable<object?> args, string expected)
        => args.Any(arg => string.Equals(arg?.ToString(), expected, StringComparison.Ordinal));

    private static bool HasField(IEnumerable<HashEntry> entries, string fieldName)
        => entries.Any(entry => string.Equals(entry.Name.ToString(), fieldName, StringComparison.OrdinalIgnoreCase));

    private static bool HasEntry(IEnumerable<HashEntry> entries, string fieldName, string value)
        => entries.Any(entry => string.Equals(entry.Name.ToString(), fieldName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(entry.Value.ToString(), value, StringComparison.Ordinal));

    private static IConnectionMultiplexer CreateRedis(IDatabase db, IServer server)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);
        return redis;
    }

    private static IServer CreateServer(IReadOnlyList<RedisKey> keys)
    {
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Any<RedisValue>(),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                string pattern = callInfo.ArgAt<RedisValue>(1).ToString()!;
                string prefix = pattern.EndsWith('*') ? pattern[..^1] : pattern;
                RedisKey[] matched = keys
                    .Where(key => key.ToString().StartsWith(prefix, StringComparison.Ordinal))
                    .ToArray();
                return ToAsyncEnumerable(matched);
            });
        return server;
    }

    private static HashEntry[] CreateNaturalLanguageHashEntries() =>
    [
        new("embedding", new byte[] { 1, 2, 3, 4 }),
        new("memoryUnitId", "mu-1"),
        new("caseId", "case-1"),
        new("naturalLanguageDescription", "A customer opened a claim."),
        new("descriptionOrigin", "ai"),
        new("descriptionConfidence", "0.9"),
        new("descriptionConfidenceSource", "model"),
        new("embeddingProvider", "openai"),
        new("embeddingModel", "text-embedding-3-small"),
        new("embeddingDimensions", "1536"),
    ];

    private static async IAsyncEnumerable<RedisKey> ToAsyncEnumerable(RedisKey[] keys)
    {
        foreach (RedisKey key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }
}
