// <copyright file="CleanupActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class CleanupActivityTests
{
    // --- CleanupSyntacticActivity ---

    [Fact]
    public async Task CleanupSyntactic_ShouldDeleteRedisHashKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        CleanupSyntacticActivity activity = new(redis, Substitute.For<ILogger<CleanupSyntacticActivity>>());

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-1:mu:mu-001", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CleanupSyntactic_KeyDoesNotExist_ShouldNotThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        CleanupSyntacticActivity activity = new(redis, Substitute.For<ILogger<CleanupSyntacticActivity>>());

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        result.ShouldBeFalse(); // Key didn't exist — idempotent
    }

    // --- CleanupSemanticActivity ---

    [Fact]
    public async Task CleanupSemantic_ShouldDeleteRedisVectorHashKey()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        CleanupSemanticActivity activity = new(redis, Substitute.For<ILogger<CleanupSemanticActivity>>());

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-1:vec:mu-001", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CleanupSemantic_DeletesBothHashes_Idempotent()
    {
        // Story 9.2 Task 4.7: compensation must delete BOTH the raw and NL semantic hashes. DEL is a
        // no-op on missing keys so the NL hash absence (SourceType != Event) does not fail compensation.
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        CleanupSemanticActivity activity = new(redis, Substitute.For<ILogger<CleanupSemanticActivity>>());

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-1:vec:mu-001", Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync((RedisKey)"tenant-1:vecnl:mu-001", Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CleanupSemantic_KeyDoesNotExist_ShouldNotThrow()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        CleanupSemanticActivity activity = new(redis, Substitute.For<ILogger<CleanupSemanticActivity>>());

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        result.ShouldBeFalse(); // Idempotent
    }

    // --- CleanupGraphActivity ---

    [Fact]
    public async Task CleanupGraph_ShouldCallBuildDeleteMemoryUnitNode()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildDeleteMemoryUnitNode(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m", new Dictionary<string, object> { ["id"] = "mu-001" }));
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        CleanupGraphActivity activity = new(falkorDb, builder, Substitute.For<ILogger<CleanupGraphActivity>>());

        await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-001", "tenant-1"));

        builder.Received(1).BuildDeleteMemoryUnitNode("mu-001");
    }

    [Fact]
    public async Task CleanupGraph_NodeDoesNotExist_ShouldNotThrow()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildDeleteMemoryUnitNode(Arg.Any<string>())
            .Returns(("MATCH (m:MemoryUnit {id: $id}) DETACH DELETE m", new Dictionary<string, object> { ["id"] = "mu-001" }));
        (IConnectionMultiplexer falkorDb, _) = CreateMockFalkorDb();
        CleanupGraphActivity activity = new(falkorDb, builder, Substitute.For<ILogger<CleanupGraphActivity>>());

        // MATCH+DELETE on non-existent node is a no-op in FalkorDB — should not throw
        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new CleanupInput("mu-nonexistent", "tenant-1"));

        result.ShouldBeTrue();
    }

    private static (IConnectionMultiplexer, IDatabase) CreateMockFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisResult fakeResult = RedisResult.Create(
        [
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(
            [
                RedisResult.Create(new RedisValue("Nodes deleted: 0")),
                RedisResult.Create(new RedisValue("Relationships deleted: 0")),
                RedisResult.Create(new RedisValue("Cached execution: 0")),
                RedisResult.Create(new RedisValue("Query internal execution time: 0.1 milliseconds")),
            ]),
        ]);

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(fakeResult);
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(fakeResult);

        return (falkorDb, db);
    }
}
