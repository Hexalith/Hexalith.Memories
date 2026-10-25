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
    public async Task MigrateAsync_DelegatesExactUnitCopyAndRetirementToOneFencedAtomicScript()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(1L));
        IServer server = CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]);
        await RedisNaturalLanguageNamespaceMigrator.MigrateAsync(db, server, "tenant-a", CancellationToken.None);
        await db.Received(1).ScriptEvaluateAsync(Arg.Is<string>(script => script.Contains("nl-namespace-migration-fenced-v2", StringComparison.Ordinal)),
            Arg.Is<RedisKey[]>(keys => keys[0] == IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")
                && keys[1] == IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey("tenant-a", "mu-1")),
            Arg.Is<RedisValue[]>(values => values[0] == "mu-1" && values[1] == "tenant-a"), Arg.Any<CommandFlags>());
        await db.DidNotReceiveWithAnyArgs().HashSetAsync(default, default(HashEntry[])!, default);
        await db.DidNotReceiveWithAnyArgs().KeyDeleteAsync(default(RedisKey), default);
    }

    [Fact]
    public async Task MigrateAsync_CancellationNeverStartsTheAtomicMutation()
    {
        IDatabase db = Substitute.For<IDatabase>();
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => RedisNaturalLanguageNamespaceMigrator.MigrateAsync(db,
            CreateServer([IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey("tenant-a", "mu-1")]), "tenant-a", canceled.Token));
        await db.DidNotReceiveWithAnyArgs().ScriptEvaluateAsync(default(string)!, default, default, default);
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
