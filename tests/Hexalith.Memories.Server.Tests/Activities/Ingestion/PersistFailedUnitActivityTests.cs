// <copyright file="PersistFailedUnitActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using System.Linq;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class PersistFailedUnitActivityTests
{
    [Fact]
    public async Task RunAsync_PersistsHashAndZAdd_ViaLuaWithExpectedKeys()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        PersistFailedUnitActivity activity = new(redis, NullLogger<PersistFailedUnitActivity>.Instance);
        DateTimeOffset failedAt = new(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);

        bool result = await activity.RunAsync(
            Substitute.For<WorkflowActivityContext>(),
            new FailedUnitInput(
                TenantId: "tenant-1",
                CaseId: "case-1",
                MemoryUnitId: "mu-99",
                SourceUri: "https://example.com/x",
                SourceType: SourceType.Url,
                IngestedBy: "user@example.com",
                ContentType: null,
                Stage: "embedding",
                ErrorCode: "PROVIDER_500",
                ErrorMessage: "Provider returned 500",
                RetryCount: 5,
                LastRetryAt: failedAt,
                FailedAt: failedAt));

        result.ShouldBeTrue();

        var call = db.ReceivedCalls().Single(x => x.GetMethodInfo().Name == nameof(IDatabase.ScriptEvaluateAsync));
        object?[] args = call.GetArguments();
        ((string)args[0]!).ShouldContain("HSET");
        ((string)args[0]!).ShouldContain("ZADD");

        RedisKey[] keys = (RedisKey[])args[1]!;
        keys[0].ToString().ShouldBe("tenant-1:failed-unit:mu-99");
        keys[1].ToString().ShouldBe("tenant-1:case:case-1:failed-units");

        RedisValue[] argv = (RedisValue[])args[2]!;
        argv[^1].ToString().ShouldBe("mu-99");
    }

    [Fact]
    public async Task RunAsync_RedisFailure_PropagatesException()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Returns(Task.FromException<RedisResult>(new InvalidOperationException("redis hiccup")));
        PersistFailedUnitActivity activity = new(redis, NullLogger<PersistFailedUnitActivity>.Instance);
        DateTimeOffset failedAt = new(2026, 4, 15, 12, 0, 0, TimeSpan.Zero);

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                Substitute.For<WorkflowActivityContext>(),
                new FailedUnitInput(
                    "t", "c", "m", "u", SourceType.File, "by", "text/plain",
                    "indexing", "OOM", "out of memory", 1, null, failedAt)));
    }

    [Fact]
    public void BuildHashKey_FormatsAsTenantFailedUnitId()
        => PersistFailedUnitActivity.BuildHashKey("tA", "mB").ShouldBe("tA:failed-unit:mB");

    [Fact]
    public void BuildSortedSetKey_FormatsAsTenantCaseFailedUnits()
        => PersistFailedUnitActivity.BuildSortedSetKey("tA", "cB").ShouldBe("tA:case:cB:failed-units");

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase().Returns(db);
        redis.GetDatabase(Arg.Any<int>(), Arg.Is<object?>(v => v == null)).Returns(db);
        return (db, redis);
    }
}
