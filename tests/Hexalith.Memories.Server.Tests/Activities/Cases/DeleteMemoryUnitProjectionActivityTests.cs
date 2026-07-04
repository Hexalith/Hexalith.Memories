// <copyright file="DeleteMemoryUnitProjectionActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Cases;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Graph;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 21.2 AC1 failure injection at the memory-unit projection boundaries. The activity must
/// keep the syntactic hash until vector, natural-language vector, and graph deletes succeed so a
/// retried/replayed cleanup can converge, and re-running against already-deleted keys must be a
/// no-op rather than an error.
/// </summary>
public class DeleteMemoryUnitProjectionActivityTests
{
    [Fact]
    public async Task RunAsync_HappyPath_ShouldDeleteAnnotationsBeforeTargetAndSyntacticHashLast()
    {
        // Arrange
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        DeleteMemoryUnitProjectionActivity activity = new(redis, falkorDb, builder);
        MemoryUnitDeletionProjectionInput input = new("tenant-1", "case-001", "mu-001", ["ann-001"]);

        // Act
        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        // Assert
        result.ShouldBeTrue();
        builder.Received(1).BuildDeleteMemoryUnitNode("ann-001");
        builder.Received(1).BuildDeleteMemoryUnitNode("mu-001");
        Received.InOrder(() =>
        {
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:vec:ann-001"), Arg.Any<CommandFlags>());
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:vec:nl:ann-001"), Arg.Any<CommandFlags>());
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:mu:ann-001"), Arg.Any<CommandFlags>());
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:vec:mu-001"), Arg.Any<CommandFlags>());
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:vec:nl:mu-001"), Arg.Any<CommandFlags>());
            redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:mu:mu-001"), Arg.Any<CommandFlags>());
        });
    }

    [Fact]
    public async Task RunAsync_VectorDeleteFails_ShouldKeepSyntacticHashForRetry()
    {
        // Arrange — inject a failure at the semantic vector boundary.
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        DeleteMemoryUnitProjectionActivity activity = new(redis, falkorDb, builder);
        MemoryUnitDeletionProjectionInput input = new("tenant-1", "case-001", "mu-001", []);

        redisDb.KeyDeleteAsync(Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:vec:mu-001"), Arg.Any<CommandFlags>())
            .Returns(Task.FromException<bool>(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Vector delete failed")));

        // Act / Assert — the failure surfaces for workflow retry and the syntactic hash survives,
        // so a replayed cleanup still finds the record and converges instead of diverging silently.
        _ = await Should.ThrowAsync<RedisConnectionException>(
            () => activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input));

        await redisDb.DidNotReceive().KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "tenant-1:mu:mu-001"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_RerunAfterKeysAlreadyDeleted_ShouldConvergeIdempotently()
    {
        // Arrange — every key is already gone (KeyDelete returns false), as after a replayed retry.
        (IConnectionMultiplexer redis, IDatabase redisDb) = CreateMockRedis();
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        IGraphQueryBuilder builder = CreateMockBuilder();
        DeleteMemoryUnitProjectionActivity activity = new(redis, falkorDb, builder);
        MemoryUnitDeletionProjectionInput input = new("tenant-1", "case-001", "mu-001", ["ann-001"]);

        redisDb.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        // Act
        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        // Assert — re-running the cleanup converges without errors or duplicated side effects.
        result.ShouldBeTrue();
    }

    private static IGraphQueryBuilder CreateMockBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildDeleteMemoryUnitNode(Arg.Any<string>())
            .Returns(callInfo => (
                "MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m",
                (IDictionary<string, object>)new Dictionary<string, object> { ["id"] = callInfo.Arg<string>() }));
        return builder;
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (redis, db);
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult fakeGraphResult = CreateEmptyFalkorDbResult();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeGraphResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeGraphResult);

        return (falkorDb, db);
    }

    private static RedisResult CreateEmptyFalkorDbResult() => RedisResult.Create(
    [
        RedisResult.Create(Array.Empty<RedisResult>()),
        RedisResult.Create(Array.Empty<RedisResult>()),
        RedisResult.Create(
        [
            RedisResult.Create(new RedisValue("Nodes created: 0")),
            RedisResult.Create(new RedisValue("Properties set: 0")),
            RedisResult.Create(new RedisValue("Relationships created: 0")),
            RedisResult.Create(new RedisValue("Cached execution: 0")),
            RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
        ]),
    ]);
}
