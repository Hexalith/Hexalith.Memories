// <copyright file="RedisAggregateCaseMappingStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using Hexalith.Memories.EventStore;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public sealed class RedisAggregateCaseMappingStoreTests
{
    [Fact]
    public async Task DeleteCaseMappingsAsync_ShouldDeleteAllFieldsPointingAtCase()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashScanAsync(
                "tenant-1:eventstore:aggregate-case-map",
                default,
                1000,
                0,
                0,
                Arg.Any<CommandFlags>())
            .Returns(GetHashEntries(
                new HashEntry("events:Claims", "case-delete"),
                new HashEntry("events:Orders", "case-keep"),
                new HashEntry("events:Invoices", "case-delete")));
        db.HashDeleteAsync(
                "tenant-1:eventstore:aggregate-case-map",
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Returns(callInfo => (long)((RedisValue[])callInfo[1]).Length);

        RedisAggregateCaseMappingStore store = new(CreateRedis(db));

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(2);
        await db.Received(1).HashDeleteAsync(
            "tenant-1:eventstore:aggregate-case-map",
            Arg.Is<RedisValue[]>(fields => fields.Select(static f => f.ToString()).SequenceEqual(new[]
            {
                "events:Claims",
                "events:Invoices",
            })),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DeleteCaseMappingsAsync_WhenMapMissing_ShouldReturnZeroWithoutDelete()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.HashScanAsync(
                "tenant-1:eventstore:aggregate-case-map",
                default,
                1000,
                0,
                0,
                Arg.Any<CommandFlags>())
            .Returns(GetHashEntries());

        RedisAggregateCaseMappingStore store = new(CreateRedis(db));

        long deleted = await store.DeleteCaseMappingsAsync("tenant-1", "case-delete", CancellationToken.None);

        deleted.ShouldBe(0);
        await db.DidNotReceive().HashDeleteAsync(
            Arg.Any<RedisKey>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }

    [Theory]
    [InlineData("", "case-1")]
    [InlineData(" ", "case-1")]
    [InlineData("tenant-1", "")]
    [InlineData("tenant-1", " ")]
    public async Task DeleteCaseMappingsAsync_WithInvalidInput_ShouldThrowBeforeRedisCall(string tenantId, string caseId)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        RedisAggregateCaseMappingStore store = new(redis);

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.DeleteCaseMappingsAsync(tenantId, caseId, CancellationToken.None));

        _ = redis.DidNotReceive().GetDatabase(Arg.Any<int>(), Arg.Any<object?>());
    }

    private static IConnectionMultiplexer CreateRedis(IDatabase db)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);
        return redis;
    }

    private static async IAsyncEnumerable<HashEntry> GetHashEntries(params HashEntry[] entries)
    {
        foreach (HashEntry entry in entries)
        {
            yield return entry;
            await Task.Yield();
        }
    }
}
