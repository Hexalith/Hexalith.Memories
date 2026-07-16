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

    // Story 18.4 — explicit idempotency token: precedence (token first) with sourceUri natural-key fallback.
    [Fact]
    public async Task RunAsync_WithToken_TokenRecordExists_ReturnsDuplicateFromTokenKey()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string tokenKey = DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz");
        db.StringGetAsync(Arg.Is<RedisKey>(k => k == tokenKey), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"mu-from-token");
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1", "idem-xyz"));

        result.IsDuplicate.ShouldBeTrue();
        result.ExistingMemoryUnitId.ShouldBe("mu-from-token");
    }

    [Fact]
    public async Task RunAsync_WithToken_TokenRecordMissing_FallsBackToSourceUriKey()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        string sourceKey = DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        // Token key is unconfigured → RedisValue.Null (miss); sourceUri natural key holds the existing unit.
        db.StringGetAsync(Arg.Is<RedisKey>(k => k == sourceKey), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"mu-from-source");
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        IdempotencyResult result = await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1", "idem-xyz"));

        result.IsDuplicate.ShouldBeTrue();
        result.ExistingMemoryUnitId.ShouldBe("mu-from-source");
    }

    [Fact]
    public async Task RunAsync_WithToken_ChecksTokenKeyBeforeSourceUriKey()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1", "idem-xyz"));

        string tokenKey = DedupKeyBuilder.BuildTokenKey("tenant-1", "case-1", "idem-xyz");
        string sourceKey = DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        await db.Received(1).StringGetAsync(tokenKey, Arg.Any<CommandFlags>());
        await db.Received(1).StringGetAsync(sourceKey, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RunAsync_NoToken_OnlyChecksSourceUriKey()
    {
        (IDatabase db, IConnectionMultiplexer redis) = CreateRedis();
        db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(RedisValue.Null);
        CheckIdempotencyActivity activity = new(redis);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        await activity.RunAsync(
            context,
            new IdempotencyInput("file:///doc.pdf", "tenant-1", "case-1"));

        string sourceKey = DedupKeyBuilder.BuildKey("tenant-1", "case-1", "file:///doc.pdf");
        await db.Received(1).StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        await db.Received(1).StringGetAsync(sourceKey, Arg.Any<CommandFlags>());
    }

    private static (IDatabase Db, IConnectionMultiplexer Redis) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        return (db, redis);
    }
}
