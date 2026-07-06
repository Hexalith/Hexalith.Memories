// <copyright file="IngestionWorkflowInFlightRegistryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Server.Ingestion;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public sealed class IngestionWorkflowInFlightRegistryTests
{
    [Fact]
    public void CreateMember_TryParseMember_RoundTripsTenantAndInstance()
    {
        RedisValue member = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-1");

        bool parsed = RedisIngestionWorkflowInFlightRegistry.TryParseMember(
            member,
            out string? tenantId,
            out string? instanceId);

        parsed.ShouldBeTrue();
        tenantId.ShouldBe("tenant-a");
        instanceId.ShouldBe("instance-1");
    }

    [Fact]
    public async Task TrackAsync_AddsSortedSetEntryAndLookup()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);
        IngestionWorkflowInFlightEntry entry = new("tenant-a", "instance-1", DateTimeOffset.FromUnixTimeMilliseconds(1_000));

        await registry.TrackAsync(entry, CancellationToken.None);

        RedisValue member = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-1");
        CountSortedSetAddCalls(db, RedisIngestionWorkflowInFlightRegistry.RegistryKey, member, 1_000)
            .ShouldBe(1);
        await db.Received(1).HashSetAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            "instance-1",
            member,
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        CountStringSetCalls(db, RedisIngestionWorkflowInFlightRegistry.InitializedKey, "1")
            .ShouldBe(1);
    }

    [Fact]
    public async Task ListAsync_ReturnsParsedEntries()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisValue member = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-1");
        db.SortedSetRangeByRankWithScoresAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            0,
            -1,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([new SortedSetEntry(member, 1_000)]);
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        IReadOnlyList<IngestionWorkflowInFlightEntry> entries = await registry.ListAsync(CancellationToken.None);

        entries.Count.ShouldBe(1);
        entries[0].TenantId.ShouldBe("tenant-a");
        entries[0].InstanceId.ShouldBe("instance-1");
        entries[0].TrackedAt.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1_000));
    }

    [Fact]
    public async Task RemoveAsync_UsesInstanceLookup()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisValue member = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-1");
        db.HashGetAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            "instance-1",
            Arg.Any<CommandFlags>())
            .Returns(member);
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        await registry.RemoveAsync("instance-1", CancellationToken.None);

        await db.Received(1).SortedSetRemoveAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            member,
            Arg.Any<CommandFlags>());
        await db.Received(1).HashDeleteAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            "instance-1",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveAsync_WhenLookupMissing_RemovesMatchingRegistryMember()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisValue matchingMember = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-1");
        RedisValue otherMember = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "instance-2");
        db.HashGetAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            "instance-1",
            Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        db.SortedSetRangeByRankAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            0,
            -1,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([matchingMember, otherMember]);
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        await registry.RemoveAsync("instance-1", CancellationToken.None);

        await db.Received(1).SortedSetRemoveAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new[] { matchingMember })),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashDeleteAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            "instance-1",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task TrackAsync_WhenOverCap_DoesNotTrimBeforeDaprStateObservation()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        db.SortedSetLengthAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(101);
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        await registry.TrackAsync(
            new IngestionWorkflowInFlightEntry("tenant-a", "new-instance", DateTimeOffset.FromUnixTimeMilliseconds(2_000)),
            CancellationToken.None);

        await db.DidNotReceive().SortedSetRemoveAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>());
        await db.DidNotReceive().HashDeleteAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ListAsync_WhenStaleMembersExist_DoesNotPruneBeforeDaprStateObservation()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisValue staleMember = RedisIngestionWorkflowInFlightRegistry.CreateMember("tenant-a", "stale-instance");
        db.SortedSetRangeByScoreAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<Order>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<CommandFlags>())
            .Returns([staleMember]);
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        _ = await registry.ListAsync(CancellationToken.None);

        await db.DidNotReceive().SortedSetRemoveAsync(
            RedisIngestionWorkflowInFlightRegistry.RegistryKey,
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>());
        await db.DidNotReceive().HashDeleteAsync(
            RedisIngestionWorkflowInFlightRegistry.InstanceMemberLookupKey,
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task MarkInitializedAsync_SetsInitializedMarker()
    {
        (IConnectionMultiplexer redis, IDatabase db) = CreateRedis();
        RedisIngestionWorkflowInFlightRegistry registry = CreateRegistry(redis);

        await registry.MarkInitializedAsync(CancellationToken.None);

        CountStringSetCalls(db, RedisIngestionWorkflowInFlightRegistry.InitializedKey, "1")
            .ShouldBe(1);
    }

    private static RedisIngestionWorkflowInFlightRegistry CreateRegistry(IConnectionMultiplexer redis)
        => new(redis);

    private static (IConnectionMultiplexer Redis, IDatabase Database) CreateRedis()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SortedSetRangeByScoreAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<Order>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<CommandFlags>())
            .Returns([]);
        db.SortedSetRangeByRankAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns([]);
        db.SortedSetRangeByRankWithScoresAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns([]);

        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);
        return (redis, db);
    }

    private static int CountSortedSetAddCalls(IDatabase db, RedisKey key, RedisValue member, double score)
        => db.ReceivedCalls()
            .Count(call =>
                call.GetMethodInfo().Name == "SortedSetAddAsync"
                && (RedisKey)call.GetArguments()[0]! == key
                && (RedisValue)call.GetArguments()[1]! == member
                && (double)call.GetArguments()[2]! == score);

    private static int CountStringSetCalls(IDatabase db, RedisKey key, RedisValue value)
        => db.ReceivedCalls()
            .Count(call =>
                call.GetMethodInfo().Name == "StringSetAsync"
                && (RedisKey)call.GetArguments()[0]! == key
                && (RedisValue)call.GetArguments()[1]! == value);
}
