// <copyright file="ReleaseDedupKeyIfOwnedActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Ingestion;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public sealed class ReleaseDedupKeyIfOwnedActivityTests
{
    [Fact]
    public async Task RunAsync_KeyStillOwnedByLoser_DeletesAtomicallyWithOwnershipCondition()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis(deleted: true);
        ReleaseDedupKeyIfOwnedActivity activity = new(redis);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:source", "mu-loser"));

        result.ShouldBeTrue();
        await db.Received(1).StringDeleteAsync(
            (RedisKey)"dedup:tenant-1:case-1:source",
            Arg.Is<ValueCondition>(condition => condition.Equals(ValueCondition.Equal("mu-loser"))),
            CommandFlags.None);
    }

    [Fact]
    public async Task RunAsync_KeyNoLongerOwnedByLoser_DoesNotReportDelete()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis(deleted: false);
        ReleaseDedupKeyIfOwnedActivity activity = new(redis);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:source", "mu-loser"));

        result.ShouldBeFalse();
        await db.Received(1).StringDeleteAsync(
            (RedisKey)"dedup:tenant-1:case-1:source",
            Arg.Is<ValueCondition>(condition => condition.Equals(ValueCondition.Equal("mu-loser"))),
            CommandFlags.None);
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis(bool deleted)
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.StringDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<ValueCondition>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(deleted));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(value => value == null)).Returns(db);

        return (db, redis);
    }
}
