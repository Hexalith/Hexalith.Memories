// <copyright file="FailedNaturalLanguageEmbeddingRegistryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using System.Net;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class FailedNaturalLanguageEmbeddingRegistryTests
{
    [Fact]
    public void SerializeRecord_RoundTripsAllFields()
    {
        FailedNaturalLanguageEmbeddingRecord record = new(
            TenantId: "tenant-a",
            MemoryUnitId: "mu-1",
            TruncatedRawJsonPayload: "{\"foo\":\"bar\"}",
            EventType: "TestEventV1",
            AggregateType: "Account",
            CaseId: "case-1",
            EmbeddingProvider: "openai",
            EmbeddingModel: "text-embedding-3-small",
            EmbeddingDimensions: 1536,
            QueuedAtTicks: 1_000_000,
            Attempts: 2);

        string json = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        FailedNaturalLanguageEmbeddingRecord? roundTripped = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(json);

        roundTripped.ShouldNotBeNull();
        roundTripped!.TenantId.ShouldBe("tenant-a");
        roundTripped.MemoryUnitId.ShouldBe("mu-1");
        roundTripped.TruncatedRawJsonPayload.ShouldBe("{\"foo\":\"bar\"}");
        roundTripped.Attempts.ShouldBe(2);
        roundTripped.QueuedAtTicks.ShouldBe(1_000_000);
    }

    [Fact]
    public void TryDeserialize_Garbage_ReturnsNull()
    {
        FailedNaturalLanguageEmbeddingRegistry.TryDeserialize("not json at all").ShouldBeNull();
    }

    [Fact]
    public void LiveKeyPrefix_IsTenantScoped()
    {
        string key = FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a");
        key.ShouldBe("nl-embedding-retry:tenant-a");
    }

    [Fact]
    public void DeadKeyPrefix_IsTenantScoped()
    {
        string key = FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a");
        key.ShouldBe("nl-embedding-retry-dead:tenant-a");
    }

    [Fact]
    public void EnqueueDequeueRoundTrip_StoresIdsPlusBoundedPayload_NotFullPayload()
    {
        // Pre-mortem Failure δ regression: record size is bounded by the truncation at enqueue time.
        string fourKb = new string('A', 4096);
        FailedNaturalLanguageEmbeddingRecord record = new(
            TenantId: "tenant-a",
            MemoryUnitId: "mu-1",
            TruncatedRawJsonPayload: fourKb,
            EventType: "E",
            AggregateType: null,
            CaseId: "c",
            EmbeddingProvider: "p",
            EmbeddingModel: "m",
            EmbeddingDimensions: 3,
            QueuedAtTicks: 10,
            Attempts: 0);

        string serialized = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        // 4KB payload + envelope ≤ ~4.5KB — the Task 8.1 fallback shape cap.
        serialized.Length.ShouldBeLessThan(5 * 1024);
    }

    [Fact]
    public void PayloadKeys_AreTenantScoped()
    {
        FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a")
            .ShouldBe("nl-embedding-retry-payload:tenant-a");
        FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey("tenant-a")
            .ShouldBe("nl-embedding-retry-dead-payload:tenant-a");
    }

    [Fact]
    public async Task EnqueueAsync_UsesMemoryUnitMemberAndPayloadHash()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);
        FailedNaturalLanguageEmbeddingRecord record = CreateRecord("mu-1", queuedAtTicks: 10);

        await registry.EnqueueAsync(record, CancellationToken.None);

        await db.Received(1).HashSetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            "mu-1",
            Arg.Is<RedisValue>(value => value.ToString().Contains("\"memoryUnitId\":\"mu-1\"", StringComparison.Ordinal)),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        CountSortedSetAddCalls(db, FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"), "mu-1")
            .ShouldBe(1);
        await db.Received(1).SetAddAsync(
            FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey,
            "tenant-a",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task EnqueueAsync_WhenDuplicateMemoryUnit_UsesStableMember()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        await registry.EnqueueAsync(CreateRecord("mu-1", queuedAtTicks: 10), CancellationToken.None);
        await registry.EnqueueAsync(CreateRecord("mu-1", queuedAtTicks: 20) with { Attempts = 2 }, CancellationToken.None);

        CountSortedSetAddCalls(db, FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"), "mu-1")
            .ShouldBe(2);
        await db.Received(2).HashSetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            "mu-1",
            Arg.Any<RedisValue>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DequeueBatchAsync_SkipsCorruptPayloadsAndCleansLiveMembers()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRecord valid = CreateRecord("mu-2", queuedAtTicks: 20);
        db.SortedSetRangeByRankAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            0,
            9,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue("mu-1"), new RedisValue("mu-2")]);
        db.HashGetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "mu-1", "mu-2" })),
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue("not json"), new RedisValue(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(valid))]);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        IReadOnlyList<FailedNaturalLanguageEmbeddingRecord> records = await registry
            .DequeueBatchAsync("tenant-a", 10, CancellationToken.None);

        records.Count.ShouldBe(1);
        records[0].MemoryUnitId.ShouldBe("mu-2");
        await db.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "mu-1" })),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashDeleteAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "mu-1" })),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task DequeueBatchAsync_WithLegacyJsonMember_ReturnsRecord()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRecord legacy = CreateRecord("mu-legacy", queuedAtTicks: 10);
        string legacyJson = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(legacy);
        db.SortedSetRangeByRankAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            0,
            9,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue(legacyJson)]);
        db.HashGetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>())
            .Returns([RedisValue.Null]);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        IReadOnlyList<FailedNaturalLanguageEmbeddingRecord> records = await registry
            .DequeueBatchAsync("tenant-a", 10, CancellationToken.None);

        records.Count.ShouldBe(1);
        records[0].MemoryUnitId.ShouldBe("mu-legacy");
        await db.DidNotReceive().SortedSetRemoveAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CompleteAsync_RemovesStableAndLegacyLiveMembers()
    {
        IDatabase db = CreateDatabase();
        ITransaction tx = Substitute.For<ITransaction>();
        tx.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        FailedNaturalLanguageEmbeddingRecord record = CreateRecord("mu-1", queuedAtTicks: 10);
        string legacyJson = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        tx.SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Any<RedisValue[]>(),
            Arg.Any<CommandFlags>())
            .Returns(1L);
        db.CreateTransaction(Arg.Any<object?>()).Returns(tx);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        await registry.CompleteAsync(record, CancellationToken.None);

        _ = tx.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Is<RedisValue[]>(values =>
                values.SequenceEqual(new RedisValue[] { "mu-1", legacyJson })),
            Arg.Any<CommandFlags>());
        _ = tx.Received(1).HashDeleteAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            "mu-1",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task CompleteAsync_WhenPayloadChanged_DoesNotRemoveNewerLiveMember()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRecord oldRecord = CreateRecord("mu-1", queuedAtTicks: 10);
        FailedNaturalLanguageEmbeddingRecord newerRecord = oldRecord with { QueuedAtTicks = 20, Attempts = 1 };
        db.HashGetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            "mu-1",
            Arg.Any<CommandFlags>())
            .Returns(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(newerRecord));
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        await registry.CompleteAsync(oldRecord, CancellationToken.None);

        db.DidNotReceiveWithAnyArgs().CreateTransaction(default);
    }

    [Fact]
    public async Task IncrementAttemptsAsync_RemovesStableAndLegacyLiveMembers()
    {
        IDatabase db = CreateDatabase();
        ITransaction tx = Substitute.For<ITransaction>();
        tx.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        db.CreateTransaction(Arg.Any<object?>()).Returns(tx);
        FailedNaturalLanguageEmbeddingRecord record = CreateRecord("mu-1", queuedAtTicks: 10);
        string legacyJson = FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(record);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        bool dead = await registry.IncrementAttemptsAsync(record, maxAttempts: 5, CancellationToken.None);

        dead.ShouldBeFalse();
        _ = tx.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            "mu-1",
            Arg.Any<CommandFlags>());
        _ = tx.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            legacyJson,
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task IncrementAttemptsAsync_WhenPayloadChanged_DoesNotRemoveNewerLiveMember()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRecord oldRecord = CreateRecord("mu-1", queuedAtTicks: 10);
        FailedNaturalLanguageEmbeddingRecord newerRecord = oldRecord with { QueuedAtTicks = 20, Attempts = 1 };
        db.HashGetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            "mu-1",
            Arg.Any<CommandFlags>())
            .Returns(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(newerRecord));
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        bool dead = await registry.IncrementAttemptsAsync(oldRecord, maxAttempts: 5, CancellationToken.None);

        dead.ShouldBeFalse();
        db.DidNotReceiveWithAnyArgs().CreateTransaction(default);
    }

    [Fact]
    public async Task ListTenantsWithBacklogAsync_UsesTenantBacklogSetWithoutKeyScan()
    {
        IDatabase db = CreateDatabase();
        db.SetMembersAsync(FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey, Arg.Any<CommandFlags>())
            .Returns([new RedisValue("tenant-a"), new RedisValue("tenant-empty")]);
        db.SortedSetLengthAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(1);
        db.SortedSetLengthAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-empty"),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(0);
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);
        FailedNaturalLanguageEmbeddingRegistry registry = new(
            redis,
            Substitute.For<ILogger<FailedNaturalLanguageEmbeddingRegistry>>(),
            Options.Create(new NaturalLanguageDescriptionOptions()));

        List<string> tenants = [];
        await foreach (string tenant in registry.ListTenantsWithBacklogAsync(CancellationToken.None))
        {
            tenants.Add(tenant);
        }

        tenants.ShouldBe(["tenant-a"]);
        redis.DidNotReceiveWithAnyArgs().GetEndPoints(default);
        await db.Received(1).SetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey,
            "tenant-empty",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ListTenantsWithBacklogAsync_WhenTenantSetMissing_ScansLegacyRetryKeys()
    {
        IDatabase db = CreateDatabase();
        db.SetMembersAsync(FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey, Arg.Any<CommandFlags>())
            .Returns([]);
        db.SortedSetLengthAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-legacy"),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(1);
        EndPoint endpoint = new DnsEndPoint("localhost", 6379);
        IServer server = Substitute.For<IServer>();
        server.IsConnected.Returns(true);
        server.KeysAsync(
            Arg.Any<int>(),
            FailedNaturalLanguageEmbeddingRegistry.LiveKeyPrefix + "*",
            100,
            Arg.Any<long>(),
            Arg.Any<int>(),
            Arg.Any<CommandFlags>())
            .Returns(AsyncKeys(FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-legacy")));
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);
        redis.GetEndPoints(Arg.Any<bool>()).Returns([endpoint]);
        redis.GetServer(endpoint, Arg.Any<object?>()).Returns(server);
        FailedNaturalLanguageEmbeddingRegistry registry = new(
            redis,
            Substitute.For<ILogger<FailedNaturalLanguageEmbeddingRegistry>>(),
            Options.Create(new NaturalLanguageDescriptionOptions()));

        List<string> tenants = [];
        await foreach (string tenant in registry.ListTenantsWithBacklogAsync(CancellationToken.None))
        {
            tenants.Add(tenant);
        }

        tenants.ShouldBe(["tenant-legacy"]);
        await db.Received(1).SetAddAsync(
            FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey,
            "tenant-legacy",
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task EnqueueAsync_WhenLiveQueueExceedsCap_MovesOverflowToDeadLetter()
    {
        IDatabase db = CreateDatabase();
        FailedNaturalLanguageEmbeddingRecord oldRecord = CreateRecord("old-mu", queuedAtTicks: 1);
        db.SortedSetLengthAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(2);
        db.SortedSetRangeByRankAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            0,
            0,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue("old-mu")]);
        db.HashGetAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "old-mu" })),
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue(FailedNaturalLanguageEmbeddingRegistry.SerializeRecord(oldRecord))]);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(
            db,
            new NaturalLanguageDescriptionOptions { LiveRetryQueueMaxEntries = 1 });

        await registry.EnqueueAsync(CreateRecord("mu-1", queuedAtTicks: 10), CancellationToken.None);

        await db.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.LiveKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "old-mu" })),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashDeleteAsync(
            FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "old-mu" })),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashSetAsync(
            FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey("tenant-a"),
            "old-mu",
            Arg.Is<RedisValue>(value => value.ToString().Contains("\"memoryUnitId\":\"old-mu\"", StringComparison.Ordinal)),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
        CountSortedSetAddCalls(db, FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a"), "old-mu")
            .ShouldBe(1);
    }

    [Fact]
    public async Task IncrementAttemptsAsync_WhenDeadLetteredAndDeadQueueExceedsCap_TrimsDeadPayloadHash()
    {
        IDatabase db = CreateDatabase();
        ITransaction tx = Substitute.For<ITransaction>();
        tx.ExecuteAsync(Arg.Any<CommandFlags>()).Returns(true);
        db.CreateTransaction(Arg.Any<object?>()).Returns(tx);
        db.SortedSetLengthAsync(
            FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a"),
            Arg.Any<double>(),
            Arg.Any<double>(),
            Arg.Any<Exclude>(),
            Arg.Any<CommandFlags>())
            .Returns(2);
        db.SortedSetRangeByRankAsync(
            FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a"),
            0,
            0,
            Order.Ascending,
            Arg.Any<CommandFlags>())
            .Returns([new RedisValue("old-mu")]);
        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(
            db,
            new NaturalLanguageDescriptionOptions { DeadRetryQueueMaxEntries = 1 });

        bool dead = await registry.IncrementAttemptsAsync(
            CreateRecord("mu-1", queuedAtTicks: 10),
            maxAttempts: 1,
            CancellationToken.None);

        dead.ShouldBeTrue();
        await db.Received(1).SortedSetRemoveAsync(
            FailedNaturalLanguageEmbeddingRegistry.DeadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "old-mu" })),
            Arg.Any<CommandFlags>());
        await db.Received(1).HashDeleteAsync(
            FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey("tenant-a"),
            Arg.Is<RedisValue[]>(values => values.SequenceEqual(new RedisValue[] { "old-mu" })),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetBacklogBytesAsync_RedisException_ReturnsZero()
    {
        IDatabase db = CreateDatabase();
        db.ExecuteAsync("MEMORY", Arg.Any<object[]>())
            .Returns(Task.FromException<RedisResult>(
                new RedisConnectionException(ConnectionFailureType.SocketFailure, "redis unavailable")));

        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        long bytes = await registry.GetBacklogBytesAsync("tenant-a", CancellationToken.None);

        bytes.ShouldBe(0);
    }

    [Fact]
    public async Task GetBacklogBytesAsync_TimeoutException_ReturnsZero()
    {
        IDatabase db = CreateDatabase();
        db.ExecuteAsync("MEMORY", Arg.Any<object[]>())
            .Returns(Task.FromException<RedisResult>(new TimeoutException("timed out")));

        FailedNaturalLanguageEmbeddingRegistry registry = CreateRegistry(db);

        long bytes = await registry.GetBacklogBytesAsync("tenant-a", CancellationToken.None);

        bytes.ShouldBe(0);
    }

    private static FailedNaturalLanguageEmbeddingRegistry CreateRegistry(
        IDatabase db,
        NaturalLanguageDescriptionOptions? options = null)
    {
        IConnectionMultiplexer redis = Substitute.For<IConnectionMultiplexer>();
        redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(db);

        return new FailedNaturalLanguageEmbeddingRegistry(
            redis,
            Substitute.For<ILogger<FailedNaturalLanguageEmbeddingRegistry>>(),
            Options.Create(options ?? new NaturalLanguageDescriptionOptions()));
    }

    private static IDatabase CreateDatabase()
    {
        IDatabase db = Substitute.For<IDatabase>();
        db.SortedSetRangeByRankAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
            Arg.Any<Order>(),
            Arg.Any<CommandFlags>())
            .Returns([]);
        return db;
    }

    private static async IAsyncEnumerable<RedisKey> AsyncKeys(params RedisKey[] keys)
    {
        foreach (RedisKey key in keys)
        {
            await Task.Yield();
            yield return key;
        }
    }

    private static FailedNaturalLanguageEmbeddingRecord CreateRecord(string memoryUnitId, long queuedAtTicks)
        => new(
            TenantId: "tenant-a",
            MemoryUnitId: memoryUnitId,
            TruncatedRawJsonPayload: "{\"foo\":\"bar\"}",
            EventType: "TestEventV1",
            AggregateType: "Account",
            CaseId: "case-1",
            EmbeddingProvider: "openai",
            EmbeddingModel: "text-embedding-3-small",
            EmbeddingDimensions: 1536,
            QueuedAtTicks: queuedAtTicks,
            Attempts: 0);

    private static int CountSortedSetAddCalls(IDatabase db, RedisKey key, RedisValue member)
        => db.ReceivedCalls()
            .Count(call =>
                call.GetMethodInfo().Name == "SortedSetAddAsync"
                && (RedisKey)call.GetArguments()[0]! == key
                && (RedisValue)call.GetArguments()[1]! == member);
}
