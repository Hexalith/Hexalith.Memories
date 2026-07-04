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
    public async Task RunAsync_KeyStillOwnedByLoser_DeletesThroughConditionalTransaction()
    {
        (IDatabase db, ITransaction transaction, IConnectionMultiplexer redis) = CreateRedis(committed: true, deleted: true);
        ReleaseDedupKeyIfOwnedActivity activity = new(redis);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:source", "mu-loser"));

        result.ShouldBeTrue();
        db.Received(1).CreateTransaction(null);
        transaction.Received(1).AddCondition(Arg.Any<Condition>());
        await transaction.Received(1).KeyDeleteAsync((RedisKey)"dedup:tenant-1:case-1:source", CommandFlags.None);
        await transaction.Received(1).ExecuteAsync(CommandFlags.None);
    }

    [Fact]
    public async Task RunAsync_KeyNoLongerOwnedByLoser_DoesNotReportDelete()
    {
        (_, ITransaction transaction, IConnectionMultiplexer redis) = CreateRedis(committed: false, deleted: true);
        ReleaseDedupKeyIfOwnedActivity activity = new(redis);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new DedupKeyInput("dedup:tenant-1:case-1:source", "mu-loser"));

        result.ShouldBeFalse();
        transaction.Received(1).AddCondition(Arg.Any<Condition>());
        await transaction.Received(1).ExecuteAsync(CommandFlags.None);
    }

    private static (IDatabase Db, ITransaction Transaction, IConnectionMultiplexer Redis) CreateRedis(
        bool committed,
        bool deleted)
    {
        ITransaction transaction = Substitute.For<ITransaction>();
        transaction.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(deleted));
        transaction.ExecuteAsync(Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(committed));

        IDatabase db = Substitute.For<IDatabase>();
        db.CreateTransaction(null).Returns(transaction);
        db.CreateTransaction(Arg.Is<object?>(value => value == null)).Returns(transaction);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(value => value == null)).Returns(db);

        return (db, transaction, redis);
    }
}
