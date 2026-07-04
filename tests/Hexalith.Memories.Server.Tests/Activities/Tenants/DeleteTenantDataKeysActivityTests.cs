namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using System.Collections.Generic;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class DeleteTenantDataKeysActivityTests
{
    [Fact]
    public async Task RunAsync_ShouldDeleteAllTenantDataKeysUsingExpectedPrefixes()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.Database.Returns(0);
        db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>())
            .Returns(callInfo => (long)((RedisKey[])callInfo[0]).Length);

        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        IReadOnlyDictionary<string, string> patterns = BuildExpectedPatternKeys("tenant-1");
        foreach (KeyValuePair<string, string> pattern in patterns)
        {
            server.KeysAsync(0, pattern.Key, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
                .Returns(GetKeys(pattern.Value));
        }

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        redis.GetServers().Returns(new[] { server });

        ILogger<DeleteTenantDataKeysActivity> logger = Substitute.For<ILogger<DeleteTenantDataKeysActivity>>();
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();
        DeleteTenantDataKeysActivity activity = new(redis, logger);

        bool result = await activity.RunAsync(context, new TenantDeletionInput("tenant-1"));

        result.ShouldBeTrue();
        await db.Received(patterns.Count).KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>());
        foreach (KeyValuePair<string, string> pattern in patterns)
        {
            _ = server.Received(1).KeysAsync(0, pattern.Key, 1000, Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>());
            await db.Received(1).KeyDeleteAsync(
                Arg.Is<RedisKey[]>(keys => keys.Select(static k => k.ToString()).SequenceEqual(new[] { pattern.Value })),
                Arg.Any<CommandFlags>());
        }
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

    private static IReadOnlyDictionary<string, string> BuildExpectedPatternKeys(string tenantId)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{tenantId}:case:*"] = $"{tenantId}:case:case-1",
            [$"dedup:{tenantId}:*"] = $"dedup:{tenantId}:case-1:hash",
            [$"{tenantId}:eventstore:*"] = $"{tenantId}:eventstore:aggregate-case-map",
            [$"{tenantId}:embedding-migration:*"] = $"{tenantId}:embedding-migration:active",
            [IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildSemanticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, "mu-1"),
            [IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(tenantId) + "*"] = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, "mu-1"),
        };
}
