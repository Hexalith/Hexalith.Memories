// <copyright file="RedisObservedEventTypeStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 9.3 — unit tests for <see cref="RedisObservedEventTypeStore"/>. Focus on input
/// validation, the __rejected__ guard, and fail-open posture. Redis-integration coverage (real batch
/// round-trip, ZRANGEBYSCORE window semantics) is in the Tier-2 integration suite.</summary>
public sealed class RedisObservedEventTypeStoreTests
{
    [Fact]
    public async Task RecordObservationAsync_WithRejectedTenantTag_ShouldThrowArgumentException()
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.RecordObservationAsync(
                tenantId: "__rejected__",
                aggregateType: "Claims",
                eventType: "ClaimSubmittedV2",
                observedAt: DateTimeOffset.UtcNow,
                cancellationToken: CancellationToken.None));

        ex.Message.ShouldContain("__rejected__");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task RecordObservationAsync_WithEmptyTenantId_ShouldThrowArgumentException(string tenantId)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        _ = await Should.ThrowAsync<ArgumentException>(async () =>
            await store.RecordObservationAsync(
                tenantId: tenantId,
                aggregateType: "Claims",
                eventType: "ClaimSubmittedV2",
                observedAt: DateTimeOffset.UtcNow,
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_OnRedisException_ShouldFailOpen_WithoutSurfacingException()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SetLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        // Fail-open contract — the ingestion hot path never sees an exception here.
        await Should.NotThrowAsync(async () =>
            await store.RecordObservationAsync(
                tenantId: "acme",
                aggregateType: "Claims",
                eventType: "ClaimSubmittedV2",
                observedAt: DateTimeOffset.UtcNow,
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task RecordObservationAsync_OnTimeoutException_ShouldFailOpen()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SetLengthAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new TimeoutException("redis timeout"));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        await Should.NotThrowAsync(async () =>
            await store.RecordObservationAsync(
                tenantId: "acme",
                aggregateType: "Claims",
                eventType: "ClaimSubmittedV2",
                observedAt: DateTimeOffset.UtcNow,
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public void AggregatesIndexCardinalityCap_ShouldBePinnedAt1024()
    {
        // Guard test — Delta #10 cap is load-bearing for Risk #1 (write-amplification mitigation).
        RedisObservedEventTypeStore.AggregatesIndexCardinalityCap.ShouldBe(1024L);
    }

    [Fact]
    public void KeyTtl_ShouldBeTwiceWindow()
    {
        // TTL is 2x the 24h window — headroom for queries at the tail of the window.
        RedisObservedEventTypeStore.KeyTtl.ShouldBe(TimeSpan.FromHours(48));
    }

    [Fact]
    public async Task GetObservedTypesAsync_WithEmptySortedSet_ShouldReturnEmptyList()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SortedSetRangeByScoreWithScoresAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<Order>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<SortedSetEntry>()));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        System.Collections.Generic.IReadOnlyList<ObservedEventType> result =
            await store.GetObservedTypesAsync(
                tenantId: "acme",
                aggregateType: "Claims",
                window: TimeSpan.FromHours(24),
                cancellationToken: CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllObservedTypesAsync_WithEmptyAggregatesSet_ShouldReturnEmptyList()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SetMembersAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(Task.FromResult(Array.Empty<RedisValue>()));

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisObservedEventTypeStore store = new(redis, NullLogger<RedisObservedEventTypeStore>.Instance);

        System.Collections.Generic.IReadOnlyList<ObservedEventType> result =
            await store.GetAllObservedTypesAsync(
                tenantId: "acme",
                window: TimeSpan.FromHours(24),
                cancellationToken: CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
