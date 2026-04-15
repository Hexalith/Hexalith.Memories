// <copyright file="SaveDedupKeyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Linq;

using Dapr.Workflow;

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
    public async Task RunAsync_ShouldReturnTrue()
    {
        (IDatabase _, IConnectionMultiplexer redis) = CreateRedis();
        SaveDedupKeyActivity activity = new(redis);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:abc123", "mu-001"));

        result.ShouldBeTrue();
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
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(value => value == null)).Returns(db);
        return (db, redis);
    }
}
