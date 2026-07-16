namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteFalkorDbGraphFinalizerActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldCallGraphDelete()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteFalkorDbGraphFinalizerActivity> logger = Substitute.For<ILogger<DeleteFalkorDbGraphFinalizerActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteFalkorDbGraphFinalizerActivity activity = new(falkorDb, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
        await db.Received().ExecuteAsync("GRAPH.DELETE", "test-tenant");
    }

    [Fact]
    public async Task RunAsync_GraphNotFound_ShouldSwallowGracefully()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw new RedisServerException("Graph not found"));

        IConnectionMultiplexer falkorDb = Substitute.For<IConnectionMultiplexer>();
        falkorDb.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteFalkorDbGraphFinalizerActivity> logger = Substitute.For<ILogger<DeleteFalkorDbGraphFinalizerActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteFalkorDbGraphFinalizerActivity activity = new(falkorDb, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }
}
