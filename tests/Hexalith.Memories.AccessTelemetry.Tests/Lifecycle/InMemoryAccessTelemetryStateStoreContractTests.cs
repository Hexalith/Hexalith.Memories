// <copyright file="InMemoryAccessTelemetryStateStoreContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Shouldly;

/// <summary>
/// Story 27.3 contract guard for <see cref="InMemoryAccessTelemetryStateStore"/>. The portable
/// lifecycle checkpoints run against this adapter but qualify the Dapr adapter, so every behaviour
/// a checkpoint can observe must agree with <see cref="DaprAccessTelemetryStateStore"/>. Each test
/// below pins one behaviour that previously diverged.
/// </summary>
public sealed class InMemoryAccessTelemetryStateStoreContractTests
{
    private static readonly DateTimeOffset Expiry = AccessTelemetryStateStoreTestRecords.Expiry;

    [Fact]
    public async Task WriteRecordAndIndexAsync_EntrySlotAlreadyOccupied_ReturnsConflictAndCommitsNothing()
    {
        // Previously an ArgumentException from Dictionary.Add, thrown after _records had already
        // been mutated: a non-atomic write where the Dapr adapter returns Conflict and commits
        // nothing. The rejected write must leave record count, index count and catalog untouched.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        (await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None))
            .ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);

        // Native component TTL reaps the record and leaves the index entry orphaned, so the
        // record-level idempotency check no longer short-circuits the write.
        store.ExpireByTtl(record.RecordId);

        AccessTelemetryStoreWriteStatus retry = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        retry.ShouldBe(AccessTelemetryStoreWriteStatus.Conflict);
        store.RecordCount.ShouldBe(0);
        store.IndexCount.ShouldBe(1);
        store.TransactionOperationCounts.ShouldBe([3]);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_BackendKeepsRecordAfterAcknowledgedDelete_ReturnsVerificationFailed()
    {
        // VerificationFailed was unreachable, so the durability defect the enum exists for was
        // never exercised through the adapter the portable checkpoints actually run against.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        (await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None))
            .ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        store.SuppressRecordDeletion(record.RecordId);

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.VerificationFailed);
        store.ContainsRecord(record.RecordId).ShouldBeTrue();
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_NonPositiveTtl_IsRejectedBeforeAnyMutation()
    {
        // ttlInSeconds was accepted and silently discarded. The Dapr adapter forwards it to the
        // component as expiry authority, so a zero or negative value is a contract violation.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);

        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            store.WriteRecordAndIndexAsync(record, entry, 0, CancellationToken.None));
        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            store.WriteRecordAndIndexAsync(record, entry, -1, CancellationToken.None));

        store.RecordCount.ShouldBe(0);
        store.IndexCount.ShouldBe(0);
        store.TransactionOperationCounts.ShouldBeEmpty();

        (await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None))
            .ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        store.LastTtlInSeconds.ShouldBe(3600);
    }

    [Fact]
    public async Task GetDueEntriesAsync_AfterAMinuteIsFullyPurged_PrunesThatMinuteOnTheNextScan()
    {
        // The due scan had no catalog and no empty-minute side effect, so repeat scans diverged
        // from the Dapr adapter, whose scan prunes drained minutes from the expiry catalog.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord first = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            Expiry);
        AccessTelemetryRecord second = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMinutes(1));
        AccessTelemetryExpiryEntry firstEntry = AccessTelemetryStateStoreTestRecords.CreateEntry(first);
        AccessTelemetryExpiryEntry secondEntry = AccessTelemetryStateStoreTestRecords.CreateEntry(second);
        _ = await store.WriteRecordAndIndexAsync(first, firstEntry, 3600, CancellationToken.None);
        _ = await store.WriteRecordAndIndexAsync(second, secondEntry, 3600, CancellationToken.None);
        store.ActiveMinuteCount.ShouldBe(2);

        long dueThrough = AccessTelemetryExpiryIndex.GetExpiryMinute(Expiry.AddMinutes(5));
        (await store.DeleteAndVerifyAsync(firstEntry, CancellationToken.None))
            .ShouldBe(AccessTelemetryDeleteStatus.Deleted);

        // The drained minute is still active until a scan observes it empty, then it is gone.
        store.ActiveMinuteCount.ShouldBe(2);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            dueThrough,
            10,
            CancellationToken.None);
        due.ShouldHaveSingleItem().ShouldBe(secondEntry);
        store.ActiveMinuteCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetDueEntriesAsync_LimitReachedInAnEarlierMinute_LeavesLaterMinutesUnvisited()
    {
        // Truncation must stop the traversal, exactly as the Dapr adapter does, so an untraversed
        // minute is never mistaken for a drained one and pruned.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord first = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            Expiry);
        AccessTelemetryRecord second = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000002",
            Expiry.AddMinutes(1));
        AccessTelemetryExpiryEntry firstEntry = AccessTelemetryStateStoreTestRecords.CreateEntry(first);
        _ = await store.WriteRecordAndIndexAsync(first, firstEntry, 3600, CancellationToken.None);
        _ = await store.WriteRecordAndIndexAsync(
            second,
            AccessTelemetryStateStoreTestRecords.CreateEntry(second),
            3600,
            CancellationToken.None);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            AccessTelemetryExpiryIndex.GetExpiryMinute(Expiry.AddMinutes(5)),
            1,
            CancellationToken.None);

        due.ShouldHaveSingleItem().ShouldBe(firstEntry);
        store.ActiveMinuteCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetDueEntriesAsync_NonPositiveLimit_ReturnsEmptyWithoutPruningTheCatalog()
    {
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        (await store.DeleteAndVerifyAsync(entry, CancellationToken.None))
            .ShouldBe(AccessTelemetryDeleteStatus.Deleted);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            0,
            CancellationToken.None);

        due.ShouldBeEmpty();
        store.ActiveMinuteCount.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_SupersededEntrySharingTheLiveSlot_LeavesTheLiveEntryPurgeable()
    {
        // Mirrors the Dapr adapter's index-orphaning guard: pruning must match the full entry
        // identity, never the record identifier alone.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        AccessTelemetryExpiryEntry staleEntry = entry with { EnvelopeHash = new string('b', 64) };

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(staleEntry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.StaleIndex);
        store.ContainsRecord(record.RecordId).ShouldBeTrue();
        store.IndexCount.ShouldBe(1);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);
        due.ShouldHaveSingleItem().ShouldBe(entry);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_StalePruneIsAcknowledgedButNotApplied_ThrowsRatherThanReportingStaleIndex()
    {
        // Parity with DaprAccessTelemetryStateStore.RemoveBucketEntryAsync, which strongly re-reads
        // the bucket after pruning a stale entry and throws when the entry survived. Without this
        // branch the portable checkpoints cannot observe an index-side durability regression:
        // deleting the Dapr adapter's re-read would leave every in-memory checkpoint green.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        AccessTelemetryExpiryEntry staleEntry = entry with { EnvelopeHash = new string('b', 64) };
        store.Seed(staleEntry);
        store.SuppressEntryRemoval(staleEntry);

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => store.DeleteAndVerifyAsync(staleEntry, CancellationToken.None));

        thrown.Message.ShouldContain("strongly verified absent");

        // The failed prune must not have cost the live record its own index entry.
        store.ContainsRecord(record.RecordId).ShouldBeTrue();
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);
        due.ShouldContain(entry);
    }

    [Fact]
    public async Task ExpireByTtl_RecordIdThatIsNotStored_ThrowsInsteadOfSilentlyNoOpping()
    {
        // Fail-fast parity with the sibling TransactionalDaprState.ExpireByTtl helper, which throws
        // KeyNotFoundException for the identical mistake. A silent no-op here lets a fixture that
        // mistypes the identifier still "arrange" TTL reaping and then assert against a record that
        // was never reaped, so the test passes for the wrong reason.
        var store = new InMemoryAccessTelemetryStateStore();
        AccessTelemetryRecord record = AccessTelemetryStateStoreTestRecords.CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = AccessTelemetryStateStoreTestRecords.CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        KeyNotFoundException thrown = Should.Throw<KeyNotFoundException>(
            () => store.ExpireByTtl("01K0A000000000000000000009"));

        thrown.Message.ShouldContain("01K0A000000000000000000009");
        thrown.Message.ShouldContain(record.RecordId);
        store.ContainsRecord(record.RecordId).ShouldBeTrue();
    }
}
