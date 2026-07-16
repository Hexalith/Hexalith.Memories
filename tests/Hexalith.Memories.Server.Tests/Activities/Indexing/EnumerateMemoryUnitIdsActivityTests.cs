// <copyright file="EnumerateMemoryUnitIdsActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Indexing;

using System.Net;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 8.2 — AC #1 (three-backend enumeration + union), Risk #3 (orphan in graph/vector
/// missed by syntactic-only enumeration), Risk #6 (SCAN-vs-KEYS regression guard).
/// </summary>
public class EnumerateMemoryUnitIdsActivityTests
{
    private const string TestTenantId = "tenant-1";

    [Fact]
    public async Task RunAsync_AllThreeBackendsUnion_ReturnsDeduplicatedIds()
    {
        EnumerateMemoryUnitIdsActivity activity = CreateActivity(
            syntacticIds: ["a", "b"],
            semanticIds: ["b", "c"],
            graphIds: ["c", "d"]);

        EnumerateMemoryUnitIdsResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId));

        result.MemoryUnitIds.ShouldBe(["a", "b", "c", "d"]);
        result.TotalUnionCount.ShouldBe(4);
        result.Truncated.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_OrphanInVectorOnly_IsReturnedInUnion()
    {
        EnumerateMemoryUnitIdsActivity activity = CreateActivity(
            syntacticIds: [],
            semanticIds: ["orphan-vector"],
            graphIds: []);

        EnumerateMemoryUnitIdsResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId));

        result.MemoryUnitIds.ShouldContain("orphan-vector");
        result.TotalUnionCount.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_NaturalLanguageSemanticKeyUnderDisjointPrefix_DoesNotCreatePhantomId()
    {
        RedisKey[] redisKeys =
        [
            IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, "mu-1"),
            IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(TestTenantId, "mu-1"),
        ];
        EnumerateMemoryUnitIdsActivity activity = CreateActivityFromRedisKeys(redisKeys, graphIds: []);

        EnumerateMemoryUnitIdsResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId));

        result.MemoryUnitIds.ShouldBe(["mu-1"]);
        result.TotalUnionCount.ShouldBe(1);
        result.MemoryUnitIds.ShouldNotContain("nl:mu-1");
    }

    [Fact]
    public async Task RunAsync_OrphanInGraphOnly_IsReturnedInUnion()
    {
        EnumerateMemoryUnitIdsActivity activity = CreateActivity(
            syntacticIds: [],
            semanticIds: [],
            graphIds: ["graph-only-1"]);

        EnumerateMemoryUnitIdsResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId));

        result.MemoryUnitIds.ShouldContain("graph-only-1");
        result.TotalUnionCount.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_UsesCursorScan_NotKeysCommand()
    {
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);

        // Return a FRESH IAsyncEnumerable per invocation — NSubstitute caches the single
        // returned instance, and a compiler-generated async iterator cannot be iterated
        // concurrently by the two parallel ScanAsync calls (syntactic + semantic).
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Any<RedisValue>(),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(_ => EmptyAsyncEnumerable());

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);

        IConnectionMultiplexer falkorMux = VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []);
        IGraphQueryBuilder builder = CreateBuilder();

        EnumerateMemoryUnitIdsActivity activity = new(
            redis, falkorMux, builder, Substitute.For<ILogger<EnumerateMemoryUnitIdsActivity>>());

        _ = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId));

        // Risk #6 guard: KeysAsync (cursor SCAN) must be invoked; a paranoid future refactor
        // substituting the blocking KEYS command would fail this assertion.
        _ = server.Received().KeysAsync(
            Arg.Any<int>(),
            Arg.Any<RedisValue>(),
            Arg.Any<int>(),
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_SixtyThousandUnits_TruncatesAndFlags()
    {
        List<string> manyIds = Enumerable.Range(0, 60_000).Select(i => $"unit-{i:D6}").ToList();

        EnumerateMemoryUnitIdsActivity activity = CreateActivity(
            syntacticIds: manyIds,
            semanticIds: [],
            graphIds: []);

        EnumerateMemoryUnitIdsResult result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId, MaxUnits: 50_000));

        result.Truncated.ShouldBeTrue();
        result.TotalUnionCount.ShouldBe(60_000);
        result.MemoryUnitIds.Count.ShouldBe(50_000);
        result.MemoryUnitIds[0].ShouldBe("unit-000000");
        result.MemoryUnitIds[^1].ShouldBe("unit-049999");
    }

    [Fact]
    public async Task RunAsync_RedisScanOperationCanceled_BubblesUp()
    {
        // DAPR activities do not receive a caller cancellation token today (`RunAsync` uses
        // `CancellationToken.None`). This guard only proves that an OperationCanceledException
        // coming out of the SCAN async enumerable bubbles up unchanged instead of being mapped
        // to the RedisException failure path.
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSyntacticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(OperationCanceledAsyncEnumerable());
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSemanticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(EmptyAsyncEnumerable());

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);

        EnumerateMemoryUnitIdsActivity activity = new(
            redis,
            VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []),
            CreateBuilder(),
            Substitute.For<ILogger<EnumerateMemoryUnitIdsActivity>>());

        await Should.ThrowAsync<OperationCanceledException>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId)));
    }

    [Fact]
    public async Task RunAsync_RedisScanFailure_Throws()
    {
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSyntacticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(ThrowingAsyncEnumerable());
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSemanticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(EmptyAsyncEnumerable());

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);

        EnumerateMemoryUnitIdsActivity activity = new(
            redis,
            VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds: []),
            CreateBuilder(),
            Substitute.For<ILogger<EnumerateMemoryUnitIdsActivity>>());

        await Should.ThrowAsync<InvalidOperationException>(() => activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new EnumerateMemoryUnitIdsInput(TestTenantId)));
    }

    private static EnumerateMemoryUnitIdsActivity CreateActivity(
        IReadOnlyList<string> syntacticIds,
        IReadOnlyList<string> semanticIds,
        IReadOnlyList<string> graphIds)
    {
        IConnectionMultiplexer redis = CreateRedisMultiplexer(syntacticIds, semanticIds);
        IConnectionMultiplexer falkorMux = VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds);
        IGraphQueryBuilder builder = CreateBuilder();

        return new EnumerateMemoryUnitIdsActivity(
            redis,
            falkorMux,
            builder,
            Substitute.For<ILogger<EnumerateMemoryUnitIdsActivity>>());
    }

    private static IConnectionMultiplexer CreateRedisMultiplexer(
        IReadOnlyList<string> syntacticIds,
        IReadOnlyList<string> semanticIds)
    {
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);

        RedisKey[] syntacticKeys = syntacticIds.Select(id => (RedisKey)IndexSchemaDefinitions.BuildSyntacticKey(TestTenantId, id)).ToArray();
        RedisKey[] semanticKeys = semanticIds.Select(id => (RedisKey)IndexSchemaDefinitions.BuildSemanticKey(TestTenantId, id)).ToArray();

        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSyntacticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(ToAsyncEnumerable(syntacticKeys));

        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Is<RedisValue>(v => v.ToString() == IndexSchemaDefinitions.GetSemanticKeyPrefix(TestTenantId) + "*"),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(ToAsyncEnumerable(semanticKeys));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);

        return redis;
    }

    private static EnumerateMemoryUnitIdsActivity CreateActivityFromRedisKeys(
        RedisKey[] keys,
        IReadOnlyList<string> graphIds)
    {
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(
                Arg.Any<int>(),
                Arg.Any<RedisValue>(),
                Arg.Any<int>(),
                Arg.Any<long>(),
                Arg.Any<int>(),
                Arg.Any<CommandFlags>())
            .Returns(callInfo =>
            {
                string pattern = callInfo.ArgAt<RedisValue>(1).ToString()!;
                string prefix = pattern.EndsWith('*') ? pattern[..^1] : pattern;
                RedisKey[] matched = keys
                    .Where(key => key.ToString().StartsWith(prefix, StringComparison.Ordinal))
                    .ToArray();
                return ToAsyncEnumerable(matched);
            });

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object>()).Returns(server);

        return new EnumerateMemoryUnitIdsActivity(
            redis,
            VerifyConsistencyActivityTestsFactory.CreateFalkorMultiplexer(graphIds),
            CreateBuilder(),
            Substitute.For<ILogger<EnumerateMemoryUnitIdsActivity>>());
    }

    private static IGraphQueryBuilder CreateBuilder()
    {
        IGraphQueryBuilder builder = Substitute.For<IGraphQueryBuilder>();
        builder.BuildEnumerateMemoryUnitIds()
            .Returns(("MATCH (m:MemoryUnit) RETURN m.id", new Dictionary<string, object>()));
        return builder;
    }

    private static async IAsyncEnumerable<RedisKey> ToAsyncEnumerable(RedisKey[] keys)
    {
        foreach (RedisKey key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }

    private static async IAsyncEnumerable<RedisKey> EmptyAsyncEnumerable()
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<RedisKey> ThrowingAsyncEnumerable()
    {
        await Task.Yield();
        if (DateTime.UtcNow.Ticks >= 0)
        {
            throw new RedisException("simulated scan failure");
        }

        yield break;
    }

    private static async IAsyncEnumerable<RedisKey> OperationCanceledAsyncEnumerable()
    {
        await Task.Yield();
        if (DateTime.UtcNow.Ticks >= 0)
        {
            throw new OperationCanceledException("simulated cancellation");
        }

        yield break;
    }
}
