// <copyright file="FailedUnitsRegistryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Linq;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class FailedUnitsRegistryTests
{
    [Fact]
    public async Task ListAsync_ClampsLimitToBetweenOneAndFiveHundred()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.SortedSetLengthAsync(Arg.Any<RedisKey>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<Exclude>(), Arg.Any<CommandFlags>()).Returns(0L);
        db.SortedSetRangeByRankAsync(Arg.Any<RedisKey>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<Order>(), Arg.Any<CommandFlags>())
            .Returns(System.Array.Empty<RedisValue>());
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        FailedUnitsPage page = await registry.ListAsync("t", "c", limit: 9999, offset: -100, default);

        page.Limit.ShouldBe(500);
        page.Offset.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_RangeReadsDescendingByRank()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.SortedSetLengthAsync(Arg.Any<RedisKey>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<Exclude>(), Arg.Any<CommandFlags>()).Returns(2L);
        db.SortedSetRangeByRankAsync(Arg.Any<RedisKey>(), 0, 4, Order.Descending, Arg.Any<CommandFlags>())
            .Returns([(RedisValue)"mu-2", (RedisValue)"mu-1"]);
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(System.Array.Empty<HashEntry>());
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        FailedUnitsPage page = await registry.ListAsync("t", "c", 5, 0, default);

        page.TotalCount.ShouldBe(2);
        page.Limit.ShouldBe(5);
        page.Offset.ShouldBe(0);
    }

    [Fact]
    public async Task GetSummaryAsync_NoHash_ReturnsNull()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(System.Array.Empty<HashEntry>());
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        (await registry.GetSummaryAsync("t", "missing", default)).ShouldBeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_PopulatedHash_ReturnsParsedSummary()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        DateTimeOffset failedAt = new(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);
        HashEntry[] entries =
        [
            new(PersistFailedUnitActivity.FieldTenantId, "tA"),
            new(PersistFailedUnitActivity.FieldCaseId, "cB"),
            new(PersistFailedUnitActivity.FieldSourceUri, "https://x"),
            new(PersistFailedUnitActivity.FieldSourceType, nameof(SourceType.Url)),
            new(PersistFailedUnitActivity.FieldIngestedBy, "user"),
            new(PersistFailedUnitActivity.FieldStage, "embedding"),
            new(PersistFailedUnitActivity.FieldErrorCode, "PROVIDER_500"),
            new(PersistFailedUnitActivity.FieldErrorMessage, "Provider returned 500"),
            new(PersistFailedUnitActivity.FieldRetryCount, "5"),
            new(PersistFailedUnitActivity.FieldFailedAt, failedAt.ToString("O")),
        ];
        db.HashGetAllAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(entries);
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        FailedUnitSummary? summary = await registry.GetSummaryAsync("tA", "mu1", default);

        summary.ShouldNotBeNull();
        summary!.MemoryUnitId.ShouldBe("mu1");
        summary.CaseId.ShouldBe("cB");
        summary.Stage.ShouldBe("embedding");
        summary.ErrorCode.ShouldBe("PROVIDER_500");
        summary.ErrorMessage.ShouldBe("Provider returned 500");
        summary.RetryCount.ShouldBe(5);
        summary.FailedAt.ShouldBe(failedAt);
        summary.SourceType.ShouldBe(SourceType.Url);
    }

    [Fact]
    public async Task RemoveAsync_LuaReturnsOne_ReturnsTrue()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create((RedisValue)1L));
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        bool removed = await registry.RemoveAsync("t", "c", "m", "https://x", default);

        removed.ShouldBeTrue();
        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.ScriptEvaluateAsync));
        ((string)call.GetArguments()[0]!).ShouldContain("EXISTS");
        ((string)call.GetArguments()[0]!).ShouldContain("ZREM");
        RedisKey[] keys = (RedisKey[])call.GetArguments()[1]!;
        keys.Length.ShouldBe(3);
        keys[2].ToString().ShouldStartWith("dedup:t:c:");
    }

    [Fact]
    public async Task RemoveAsync_LuaReturnsZero_ReturnsFalse()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create((RedisValue)0L));
        FailedUnitsRegistry registry = new(redis, NullLogger<FailedUnitsRegistry>.Instance);

        bool removed = await registry.RemoveAsync("t", "c", "m", "https://x", default);

        removed.ShouldBeFalse();
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(v => v == null)).Returns(db);
        return (db, redis);
    }
}
