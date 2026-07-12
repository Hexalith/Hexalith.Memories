// <copyright file="SaveDedupKeyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Linq;

using Dapr.Workflow;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Ingestion;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class SaveDedupKeyActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldSaveRedisValueWithCorrectKeyAndValue()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SaveDedupKeyActivity activity = new(redis);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        call.GetArguments()[0].ShouldBe((RedisKey)"dedup:tenant-1:case-1:abc123");
        call.GetArguments()[1].ShouldBe((RedisValue)"mu-001");
    }

    [Fact]
    public async Task RunAsync_ShouldWritePermanentRecordWithNullExpiry()
    {
        // Story 21.7 AC1 — the source-URI dedup record is the MemoryUnitId-stability authority and MUST stay
        // TTL-less and first-writer-wins. A non-null expiry or a When other than NotExists would silently
        // weaken the stability guarantee documented in docs/dev/memory-unit-id-stability.md.
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        SaveDedupKeyActivity activity = new(redis);

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        call.GetArguments()[2].ShouldBeNull("the permanent dedup record must be written with expiry: null (TTL-less).");
        call.GetArguments()[3].ShouldBe(When.NotExists);
    }

    [Fact]
    public async Task RunAsync_FirstWriter_ShouldReturnSaved()
    {
        (IDatabase _, IConnectionMultiplexer redis) = CreateRedis();
        SaveDedupKeyActivity activity = new(redis);

        DedupKeySaveResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        result.Status.ShouldBe(DedupKeySaveStatus.Saved);
        result.MemoryUnitId.ShouldBe("mu-001");
    }

    [Fact]
    public async Task RunAsync_DuplicateExisting_ShouldReturnWinnerIdWithoutOverwrite()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringSetAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(false));
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"mu-winner");
        SaveDedupKeyActivity activity = new(redis);

        DedupKeySaveResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-loser"));

        result.Status.ShouldBe(DedupKeySaveStatus.DuplicateExisting);
        result.MemoryUnitId.ShouldBe("mu-winner");
        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        call.GetArguments()[1].ShouldBe((RedisValue)"mu-loser");
        call.GetArguments()[3].ShouldBe(When.NotExists);
    }

    [Fact]
    public async Task RunAsync_PreflightReservation_ShouldAtomicallyPromoteToPermanentMemoryUnitId()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringSetAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(false));
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)PreflightDedupReservation.ReservedValue);
        db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create((RedisValue)1L));
        SaveDedupKeyActivity activity = new(redis);

        DedupKeySaveResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        result.Status.ShouldBe(DedupKeySaveStatus.Saved);
        result.MemoryUnitId.ShouldBe("mu-001");
        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.ScriptEvaluateAsync));
        ((string)call.GetArguments()[0]!).ShouldContain("redis.call('SET'");
        ((RedisKey[])call.GetArguments()[1]!)[0].ShouldBe((RedisKey)"dedup:tenant-1:case-1:abc123");
        RedisValue[] values = (RedisValue[])call.GetArguments()[2]!;
        values[0].ShouldBe((RedisValue)PreflightDedupReservation.ReservedValue);
        values[1].ShouldBe((RedisValue)"mu-001");
    }

    [Fact]
    public async Task RunAsync_RedisUnavailable_ShouldPropagateException()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        Task<bool> failure = Task.FromException<bool>(new InvalidOperationException("Redis unavailable"));
        db.StringSetAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(failure);
        db.StringSetAsync(default, default, default, default, default, default)
            .ReturnsForAnyArgs(failure);
        SaveDedupKeyActivity activity = new(redis);

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001")));
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.StringSetAsync(default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(true));
        db.StringSetAsync(default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(true));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(value => value == null)).Returns(db);
        return (db, redis);
    }
}
