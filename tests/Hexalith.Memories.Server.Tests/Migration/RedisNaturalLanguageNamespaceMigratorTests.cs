// <copyright file="RedisNaturalLanguageNamespaceMigratorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Migration;

using System.Net;

using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class RedisNaturalLanguageNamespaceMigratorTests
{
    [Fact]
    public async Task MigrateAsync_LegacyHashExists_CopiesAllFieldsThenDeletesLegacyKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        HashEntry[] legacyEntries = CreateNlEntries();
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns([], legacyEntries);
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns(legacyEntries);

        await RedisNaturalLanguageNamespaceMigrator.MigrateAsync(db, server, "tenant-a", CancellationToken.None);

        await db.Received(1).HashSetAsync(
            (RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"),
            Arg.Is<HashEntry[]>(entries =>
                entries.Any(e => e.Name == "embedding")
                && entries.Any(e => e.Name == "embeddingProvider" && e.Value == "openai")
                && entries.Any(e => e.Name == "embeddingDimensions" && e.Value == "1536")),
            Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task MigrateAsync_TargetAlreadyVerified_DoesNotOverwriteTarget()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        HashEntry[] targetEntries = CreateNlEntries();
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns(targetEntries);

        await RedisNaturalLanguageNamespaceMigrator.MigrateAsync(db, server, "tenant-a", CancellationToken.None);

        await db.DidNotReceive().HashSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<HashEntry[]>(),
            Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task MigrateAsync_TargetCannotBeVerified_LeavesLegacyKeyForRetry()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns([]);
        db.HashGetAllAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>())
            .Returns([new HashEntry("memoryUnitId", "mu-1")]);

        await RedisNaturalLanguageNamespaceMigrator.MigrateAsync(db, server, "tenant-a", CancellationToken.None);

        await db.DidNotReceive().KeyDeleteAsync((RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1"), Arg.Any<CommandFlags>());
    }

    private static IServer CreateServer(IReadOnlyList<RedisKey> keys)
    {
        IServer server = Substitute.For<IServer>();
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

    private static HashEntry[] CreateNlEntries() =>
    [
        new("embedding", new byte[] { 1, 2, 3, 4 }),
        new("memoryUnitId", "mu-1"),
        new("caseId", "case-1"),
        new("naturalLanguageDescription", "A customer opened a claim."),
        new("descriptionOrigin", "ai"),
        new("descriptionConfidence", "0.9"),
        new("descriptionConfidenceSource", "logprobs"),
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
