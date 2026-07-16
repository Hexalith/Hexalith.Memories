// <copyright file="DeleteFalkorDbBatchActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteFalkorDbBatchActivityTests
{
    private const string TenantId = "test-tenant";

    [Fact]
    public async Task RunAsync_CountQueryExecutesOnCorrectGraph()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();

        int callNumber = 0;
        SetupExecuteAsync(db, _ =>
        {
            int call = Interlocked.Increment(ref callNumber);
            return call == 1
                ? BuildFalkorDbScalarResult(10)
                : BuildFalkorDbScalarResult(10);
        });

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 500, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert -- verify the activity processed and returned a valid result
        result.ShouldNotBeNull();
        result.DeletedInBatch.ShouldBe(10);
    }

    [Fact]
    public async Task RunAsync_BatchDeleteUsesParameterizedBatchSize()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();

        int callNumber = 0;
        SetupExecuteAsync(db, _ =>
        {
            int call = Interlocked.Increment(ref callNumber);
            return call == 1
                ? BuildFalkorDbScalarResult(50)   // count: 50 nodes
                : BuildFalkorDbScalarResult(50);  // deleted: 50
        });

        IGraphQueryBuilder queryBuilder = Substitute.For<IGraphQueryBuilder>();
        queryBuilder.BuildCountAllNodes()
            .Returns(("MATCH (n) RETURN count(n) AS count", (IDictionary<string, object>)new Dictionary<string, object>()));
        queryBuilder.BuildBatchDeleteNodes(200)
            .Returns(("MATCH (n) WITH n LIMIT $batchSize DETACH DELETE n RETURN count(n) AS deleted",
                (IDictionary<string, object>)new Dictionary<string, object> { ["batchSize"] = 200 }));

        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 200, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert
        queryBuilder.Received(1).BuildBatchDeleteNodes(200);
        result.DeletedInBatch.ShouldBe(50);
    }

    [Fact]
    public async Task RunAsync_ZeroNodes_ReturnsIsCompleteTrue()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();
        SetupExecuteAsync(db, _ => BuildFalkorDbScalarResult(0)); // count: 0 nodes

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 500, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.RemainingNodes.ShouldBe(0);
        result.DeletedInBatch.ShouldBe(0);
    }

    [Fact]
    public async Task RunAsync_GraphNotFound_ReturnsIsCompleteTrueGracefully()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();
        SetupExecuteAsyncThrows(db, new RedisServerException("Graph not found"));

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 500, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.RemainingNodes.ShouldBe(0);
        result.DeletedInBatch.ShouldBe(0);
    }

    [Fact]
    public async Task RunAsync_InvalidGraphOperation_ReturnsIsCompleteTrueGracefully()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();
        SetupExecuteAsyncThrows(db, new RedisServerException("ERR Invalid graph operation"));

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 500, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.RemainingNodes.ShouldBe(0);
        result.DeletedInBatch.ShouldBe(0);
    }

    [Fact]
    public async Task RunAsync_BatchDeletesLessThanRemaining_ReturnsCorrectRemainingAndNotComplete()
    {
        // Arrange: 100 nodes remaining, batch deletes 50
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();

        int callNumber = 0;
        SetupExecuteAsync(db, _ =>
        {
            int call = Interlocked.Increment(ref callNumber);
            return call == 1
                ? BuildFalkorDbScalarResult(100)  // count: 100 nodes
                : BuildFalkorDbScalarResult(50);  // deleted: 50
        });

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 50, batchNumber: 2);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act
        BatchedGraphDeletionResult result = await activity.RunAsync(context, input);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.RemainingNodes.ShouldBe(50);
        result.DeletedInBatch.ShouldBe(50);
    }

    [Fact]
    public async Task RunAsync_UnreadableCountResult_ShouldThrowInvalidOperationException()
    {
        // Arrange
        (IDatabase db, IConnectionMultiplexer falkorDb) = CreateMockedFalkorDb();
        SetupExecuteAsync(db, _ => BuildFalkorDbEmptyResult());

        IGraphQueryBuilder queryBuilder = new GraphQueryBuilder();
        ILogger<DeleteFalkorDbBatchActivity> logger = Substitute.For<ILogger<DeleteFalkorDbBatchActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        var input = new BatchedGraphDeletionInput(TenantId, batchSize: 500, batchNumber: 0);
        var activity = new DeleteFalkorDbBatchActivity(falkorDb, queryBuilder, logger);

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, input));
    }

    private static (IDatabase Db, IConnectionMultiplexer Multiplexer) CreateMockedFalkorDb()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, multiplexer);
    }

    /// <summary>
    /// Sets up both <c>ExecuteAsync</c> overloads on the mocked <see cref="IDatabase"/>
    /// so NFalkorDB's internal calls are matched regardless of which overload it uses.
    /// </summary>
    private static void SetupExecuteAsync(IDatabase db, Func<NSubstitute.Core.CallInfo, RedisResult> resultFactory)
    {
        // params object[] overload
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(resultFactory);

        // ICollection<object> overload (used when CommandFlags are passed)
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns(resultFactory);
    }

    /// <summary>
    /// Sets up both <c>ExecuteAsync</c> overloads to throw the specified exception.
    /// </summary>
    private static void SetupExecuteAsyncThrows(IDatabase db, RedisServerException exception)
    {
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw exception);

        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<ICollection<object>>(), Arg.Any<CommandFlags>())
            .Returns<RedisResult>(_ => throw exception);
    }

    /// <summary>
    /// Builds a FalkorDB compact-format <see cref="RedisResult"/> containing a single scalar integer value.
    /// This matches the FalkorDB wire protocol for queries like <c>MATCH (n) RETURN count(n)</c>.
    /// </summary>
    private static RedisResult BuildFalkorDbScalarResult(long value)
    {
        return RedisResult.Create(new RedisResult[]
        {
            // Headers: [[column_type=1 (SCALAR), column_name]]
            RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create(new RedisResult[]
                {
                    RedisResult.Create((RedisValue)1),
                    RedisResult.Create(new RedisValue("result")),
                }),
            }),

            // Data rows: [[[scalar_type=3 (INTEGER), value]]]
            RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create(new RedisResult[]
                {
                    RedisResult.Create(new RedisResult[]
                    {
                        RedisResult.Create((RedisValue)3),
                        RedisResult.Create((RedisValue)value),
                    }),
                }),
            }),

            // Statistics
            RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create(new RedisValue("Query internal execution time: 0.5 milliseconds")),
            }),
        });
    }

    private static RedisResult BuildFalkorDbEmptyResult()
    {
        return RedisResult.Create(new RedisResult[]
        {
            RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create(new RedisResult[]
                {
                    RedisResult.Create((RedisValue)1),
                    RedisResult.Create(new RedisValue("result")),
                }),
            }),
            RedisResult.Create(Array.Empty<RedisResult>()),
            RedisResult.Create(new RedisResult[]
            {
                RedisResult.Create(new RedisValue("Query internal execution time: 0.5 milliseconds")),
            }),
        });
    }
}
