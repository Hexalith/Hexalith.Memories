namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteRedisVectorActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldCallDropIndexWithDD()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRedisVectorActivity> logger = Substitute.For<ILogger<DeleteRedisVectorActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRedisVectorActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
        await db.Received().ExecuteAsync("FT.DROPINDEX", "test-tenant:memories:vec", "DD");
    }

    [Fact]
    public async Task RunAsync_DeletesBothKeyPrefixes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRedisVectorActivity> logger = Substitute.For<ILogger<DeleteRedisVectorActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRedisVectorActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
        // Story 9.2 Task 4.6: both semantic indexes must be dropped so no orphan NL vectors remain.
        await db.Received().ExecuteAsync("FT.DROPINDEX", "test-tenant:memories:vec", "DD");
        await db.Received().ExecuteAsync("FT.DROPINDEX", "test-tenant:memories:vec:nl", "DD");
    }

    [Fact]
    public async Task RunAsync_UnknownIndex_ShouldSwallowGracefully()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw new RedisServerException("Unknown index name"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRedisVectorActivity> logger = Substitute.For<ILogger<DeleteRedisVectorActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRedisVectorActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }
}
