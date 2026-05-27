namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using System.Collections.Generic;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteTenantDataKeysActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldDeleteCaseAndDedupKeysUsingExpectedPrefixes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>())
            .Returns(callInfo => (long)((RedisKey[])callInfo[0]).Length);

        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(0, "tenant-1:case:*", 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
            .Returns(GetKeys("tenant-1:case:case-1", "tenant-1:case:case-1:members"));
        server.KeysAsync(0, "dedup:tenant-1:*", 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
            .Returns(GetKeys("dedup:tenant-1:case-1:hash"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns(new[] { server });

        ILogger<DeleteTenantDataKeysActivity> logger = Substitute.For<ILogger<DeleteTenantDataKeysActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        DeleteTenantDataKeysActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, new TenantDeletionInput("tenant-1"));

        result.ShouldBeTrue();
        await db.Received(2).KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey[]>(keys => keys.Select(static k => k.ToString()).SequenceEqual(new[]
            {
                "tenant-1:case:case-1",
                "tenant-1:case:case-1:members",
            })),
            Arg.Any<CommandFlags>());
        await db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey[]>(keys => keys.Select(static k => k.ToString()).SequenceEqual(new[]
            {
                "dedup:tenant-1:case-1:hash",
            })),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_WhenNoConnectedServer_ShouldThrowInvalidOperationException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);

        IServer disconnectedServer = Substitute.For<IServer>();
        disconnectedServer.IsConnected.Returns(false);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns(new[] { disconnectedServer });

        ILogger<DeleteTenantDataKeysActivity> logger = Substitute.For<ILogger<DeleteTenantDataKeysActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        DeleteTenantDataKeysActivity activity = new(redis, logger);

        await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(context, new TenantDeletionInput("tenant-1")));
    }

    private static async IAsyncEnumerable<RedisKey> GetKeys(params string[] keys)
    {
        foreach (string key in keys)
        {
            yield return (RedisKey)key;
            await Task.Yield();
        }
    }
}
