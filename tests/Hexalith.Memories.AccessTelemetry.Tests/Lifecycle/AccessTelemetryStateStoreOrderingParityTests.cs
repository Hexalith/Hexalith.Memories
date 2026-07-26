// <copyright file="AccessTelemetryStateStoreOrderingParityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Shouldly;

/// <summary>
/// Story 27.3 parity guard for <see cref="IAccessTelemetryStateStore.GetDueEntriesAsync"/>.
/// Purge order is observable behaviour, so the deterministic in-memory adapter used by the
/// portable lifecycle checkpoints and the Dapr adapter qualified for PG-ONPREM-1 must agree
/// exactly: minute-major, then <c>ExpiresAtUtc</c>, then <c>Shard</c>, then <c>RecordId</c>.
/// </summary>
public sealed class AccessTelemetryStateStoreOrderingParityTests
{
    private static readonly DateTimeOffset Expiry = AccessTelemetryStateStoreTestRecords.Expiry;

    [Fact]
    public async Task GetDueEntriesAsync_WithinOneMinute_BothAdaptersOrderByExpiresAtUtcThenShardThenRecordId()
    {
        // Three records share one expiry minute. Their shards (21, 6, 39) deliberately disagree
        // with their sub-minute expiry order, so a shard-major adapter cannot pass this test.
        AccessTelemetryRecord latest = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            Expiry.AddMilliseconds(300));
        AccessTelemetryRecord middle = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMilliseconds(200));
        AccessTelemetryRecord earliest = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000003",
            Expiry.AddMilliseconds(100));

        (IReadOnlyList<AccessTelemetryExpiryEntry> daprDue, IReadOnlyList<AccessTelemetryExpiryEntry> inMemoryDue) =
            await ReadDueFromBothAdaptersAsync([latest, middle, earliest], Expiry.AddMinutes(1), 10);

        string[] expected = [earliest.RecordId, middle.RecordId, latest.RecordId];
        daprDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.RecordId).ShouldBe(expected);

        // Proves the fixture discriminates: shard-major ordering would have produced 6, 21, 39.
        inMemoryDue.Select(static entry => entry.Shard).ShouldBe([39, 6, 21]);
        daprDue.Select(static entry => entry.Shard).ShouldBe([39, 6, 21]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_AcrossMinutes_BothAdaptersStayMinuteMajor()
    {
        // The later minute holds the numerically smaller shard, so minute-major ordering is the
        // only ordering that can put the earlier minute first.
        AccessTelemetryRecord earlierMinute = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000003",
            Expiry.AddMilliseconds(900));
        AccessTelemetryRecord laterMinute = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMinutes(1));

        (IReadOnlyList<AccessTelemetryExpiryEntry> daprDue, IReadOnlyList<AccessTelemetryExpiryEntry> inMemoryDue) =
            await ReadDueFromBothAdaptersAsync([laterMinute, earlierMinute], Expiry.AddMinutes(5), 10);

        string[] expected = [earlierMinute.RecordId, laterMinute.RecordId];
        daprDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.Shard).ShouldBe([39, 6]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_WithLimitBelowDueCount_BothAdaptersTruncateTheSamePrefix()
    {
        AccessTelemetryRecord latest = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            Expiry.AddMilliseconds(300));
        AccessTelemetryRecord middle = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMilliseconds(200));
        AccessTelemetryRecord earliest = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000003",
            Expiry.AddMilliseconds(100));

        (IReadOnlyList<AccessTelemetryExpiryEntry> daprDue, IReadOnlyList<AccessTelemetryExpiryEntry> inMemoryDue) =
            await ReadDueFromBothAdaptersAsync([latest, middle, earliest], Expiry.AddMinutes(1), 2);

        string[] expected = [earliest.RecordId, middle.RecordId];
        daprDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.RecordId).ShouldBe(expected);
    }

    [Fact]
    public async Task GetDueEntriesAsync_EntriesSharingExpiresAtUtc_BothAdaptersBreakTheTieOnShard()
    {
        // Both records carry the identical canonical expiry instant, so ExpiresAtUtc cannot order
        // them and the Shard tiebreaker is the only thing that can. The shards (21, 6) deliberately
        // disagree with RecordId order, so dropping `.ThenBy(entry => entry.Shard)` falls through to
        // RecordId and reverses both adapters.
        AccessTelemetryRecord higherShard = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            Expiry.AddMilliseconds(100));
        AccessTelemetryRecord lowerShard = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMilliseconds(100));

        (IReadOnlyList<AccessTelemetryExpiryEntry> daprDue, IReadOnlyList<AccessTelemetryExpiryEntry> inMemoryDue) =
            await ReadDueFromBothAdaptersAsync([higherShard, lowerShard], Expiry.AddMinutes(1), 10);

        daprDue.Select(static entry => entry.ExpiresAtUtc).Distinct().ShouldHaveSingleItem();
        string[] expected = [lowerShard.RecordId, higherShard.RecordId];
        daprDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        daprDue.Select(static entry => entry.Shard).ShouldBe([6, 21]);
        inMemoryDue.Select(static entry => entry.Shard).ShouldBe([6, 21]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_EntriesSharingExpiresAtUtcAndShard_BothAdaptersBreakTheTieOnRecordId()
    {
        // Both record identifiers hash to shard 1 and carry the identical expiry instant, so
        // RecordId is the only remaining tiebreaker. They are written in descending identifier
        // order, so dropping `.ThenBy(entry => entry.RecordId)` leaves the in-memory adapter on
        // dictionary insertion order and reverses it.
        AccessTelemetryRecord second = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000069",
            Expiry.AddMilliseconds(100));
        AccessTelemetryRecord first = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000033",
            Expiry.AddMilliseconds(100));

        (IReadOnlyList<AccessTelemetryExpiryEntry> daprDue, IReadOnlyList<AccessTelemetryExpiryEntry> inMemoryDue) =
            await ReadDueFromBothAdaptersAsync([second, first], Expiry.AddMinutes(1), 10);

        // Proves the fixture is a true two-way tie before RecordId is consulted.
        daprDue.Select(static entry => entry.ExpiresAtUtc).Distinct().ShouldHaveSingleItem();
        daprDue.Select(static entry => entry.Shard).ShouldBe([1, 1]);
        string[] expected = [first.RecordId, second.RecordId];
        daprDue.Select(static entry => entry.RecordId).ShouldBe(expected);
        inMemoryDue.Select(static entry => entry.RecordId).ShouldBe(expected);
    }

    private static async Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Dapr, IReadOnlyList<AccessTelemetryExpiryEntry> InMemory)>
        ReadDueFromBothAdaptersAsync(
            IReadOnlyList<AccessTelemetryRecord> records,
            DateTimeOffset dueThrough,
            int limit)
    {
        var state = new TransactionalDaprState();
        var dapr = new DaprAccessTelemetryStateStore(state.Client);
        var inMemory = new InMemoryAccessTelemetryStateStore();

        foreach (AccessTelemetryRecord record in records)
        {
            AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
            (await dapr.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None))
                .ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
            (await inMemory.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None))
                .ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        }

        long dueMinute = AccessTelemetryExpiryIndex.GetExpiryMinute(dueThrough);
        return (
            await dapr.GetDueEntriesAsync(dueMinute, limit, CancellationToken.None),
            await inMemory.GetDueEntriesAsync(dueMinute, limit, CancellationToken.None));
    }
}
