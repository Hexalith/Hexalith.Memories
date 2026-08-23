namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteRediSearchActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldCallDropIndexWithDD()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(RedisResult.Create(new RedisValue("OK")));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRediSearchActivity> logger = Substitute.For<ILogger<DeleteRediSearchActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRediSearchActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
        await db.Received().ExecuteAsync("FT.DROPINDEX", IndexSchemaDefinitions.GetSyntacticIndexName("test-tenant"), "DD");
    }

    [Fact]
    public async Task RunAsync_UnknownIndex_ShouldSwallowGracefully()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns<RedisResult>(_ => throw Hexalith.Memories.Server.Tests.RedisExceptionFactory.CreateServerException("Unknown index name"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        ILogger<DeleteRediSearchActivity> logger = Substitute.For<ILogger<DeleteRediSearchActivity>>();
        var input = new TenantDeletionInput("test-tenant");
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        DeleteRediSearchActivity activity = new(redis, logger);
        bool result = await activity.RunAsync(context, input);

        result.ShouldBeTrue();
    }
}
