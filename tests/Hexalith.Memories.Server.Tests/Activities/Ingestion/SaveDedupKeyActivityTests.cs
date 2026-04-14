// <copyright file="SaveDedupKeyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

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

        await db.Received(1).StringSetAsync("dedup:tenant-1:case-1:abc123", "mu-001", Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>());
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
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));
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
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, redis);
    }
}
