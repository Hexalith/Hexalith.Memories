// <copyright file="RedisEmbeddingMigrationStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Migration;

using System.Net;

using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class RedisEmbeddingMigrationStoreTests
{
    [Fact]
    public async Task DropAndRecreateSemanticIndexesAsync_LegacyNaturalLanguageHashExists_MigratesBeforeRebuildingIndexes()
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

        await store.DropAndRecreateSemanticIndexesAsync("tenant-a", 1536, CancellationToken.None);

        operations.Take(2).ShouldBe(["migrate:write-target", "migrate:delete-legacy"]);
        operations.IndexOf("migrate:delete-legacy").ShouldBeLessThan(operations.IndexOf("drop:" + IndexSchemaDefinitions.GetSemanticIndexName("tenant-a")));
        operations.IndexOf("migrate:delete-legacy").ShouldBeLessThan(operations.IndexOf("drop:" + IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName("tenant-a")));
        ftCreateCalls.Count.ShouldBe(2);
        ftCreateCalls.ShouldContain(args => ContainsArgument(args, IndexSchemaDefinitions.GetSemanticKeyPrefix("tenant-a")));
        ftCreateCalls.ShouldContain(args => ContainsArgument(args, IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix("tenant-a")));
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

    private static bool ContainsArgument(IEnumerable<object?> args, string expected)
        => args.Any(arg => string.Equals(arg?.ToString(), expected, StringComparison.Ordinal));

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
