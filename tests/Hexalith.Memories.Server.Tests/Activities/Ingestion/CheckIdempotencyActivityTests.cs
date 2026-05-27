// <copyright file="CheckIdempotencyActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Ingestion;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

public class CheckIdempotencyActivityTests
{
    [Fact]
    public async Task RunAsync_NewSource_ShouldReturnIsDuplicateFalse()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        result.IsDuplicate.ShouldBeFalse();
        result.ExistingMemoryUnitId.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_ExistingSource_ShouldReturnIsDuplicateTrue()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"mu-existing-id");
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        result.IsDuplicate.ShouldBeTrue();
        result.ExistingMemoryUnitId.ShouldBe("mu-existing-id");
    }

    [Fact]
    public async Task RunAsync_TransientPreflightReservation_IsNotTreatedAsDuplicate()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)PreflightDedupReservation.ReservedValue);
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        result.IsDuplicate.ShouldBeFalse();
        result.ExistingMemoryUnitId.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_DedupKeyFormat_ShouldUseTenantCaseSourceUriHash()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        string sourceUri = "file:///doc.pdf";
        string expectedKey = DedupKeyBuilder.BuildKey("tenant-1", "case-1", sourceUri);

        await activity.RunAsync(
            context,
            new IdempotencyInput(sourceUri, "tenant-1", "case-1"));

        await db.Received(1).StringGetAsync(expectedKey, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_RedisUnavailable_ShouldPropagateException()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(
                context,
                new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1")));
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, redis);
    }
}
