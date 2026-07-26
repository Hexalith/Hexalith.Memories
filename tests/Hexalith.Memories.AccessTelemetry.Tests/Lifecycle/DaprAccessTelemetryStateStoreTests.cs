// <copyright file="DaprAccessTelemetryStateStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 27.3 C1 coverage for the Dapr state adapter qualified against PG-ONPREM-1: the happy
/// transactional path plus every failure status the purge loop and the ADR atomicity contract
/// depend on.
/// </summary>
public sealed class DaprAccessTelemetryStateStoreTests
{
    private static readonly DateTimeOffset Expiry = AccessTelemetryStateStoreTestRecords.Expiry;

    [Fact]
    public async Task WriteRecordAndIndexAsync_CommitsRecordBucketAndCatalogInOneTransaction()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        IReadOnlyList<StateTransactionRequest> transaction = state.Transactions.ShouldHaveSingleItem();
        transaction.Count.ShouldBe(3);
        transaction.Select(static operation => operation.Key).ShouldBe(
        [
            $"records/{entry.Shard:D2}/{record.RecordId}",
            $"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}",
            "expiry-catalog",
        ]);
        transaction[0].Metadata!["ttlInSeconds"].ShouldBe("3600");
        transaction.ShouldAllBe(static operation => operation.Metadata!["partitionKey"] == "access-telemetry");
        state.Get<AccessTelemetryExpiryBucket>(transaction[1].Key).Entries.ShouldHaveSingleItem().ShouldBe(entry);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([entry.ExpiryMinute]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_TraversesExplicitBucketsWithoutQueryApi()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord later = CreateRecord("01K0A000000000000000000002", Expiry.AddSeconds(20));
        AccessTelemetryRecord earlier = CreateRecord("01K0A000000000000000000001", Expiry.AddSeconds(5));
        await store.WriteRecordAndIndexAsync(later, CreateEntry(later), 3600, CancellationToken.None);
        await store.WriteRecordAndIndexAsync(earlier, CreateEntry(earlier), 3600, CancellationToken.None);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            AccessTelemetryExpiryIndex.GetExpiryMinute(Expiry.AddMinutes(1)),
            10,
            CancellationToken.None);

        due.Select(static entry => entry.RecordId).ShouldBe([earlier.RecordId, later.RecordId]);
        await state.Client.DidNotReceiveWithAnyArgs().QueryStateAsync<AccessTelemetryExpiryEntry>(default!, default!, default!, default);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_RemovesRecordAndBucketEntryAndPrunesEmptyMinute()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.Deleted);
        due.ShouldBeEmpty();
        state.Contains($"records/{entry.Shard:D2}/{record.RecordId}").ShouldBeFalse();
        state.Contains($"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}").ShouldBeFalse();
        state.Contains("expiry-catalog").ShouldBeFalse();
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_RetryIsIdempotentWithoutDuplicatingBucketEntry()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);

        AccessTelemetryStoreWriteStatus first = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        AccessTelemetryStoreWriteStatus retry = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        first.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        retry.ShouldBe(AccessTelemetryStoreWriteStatus.Idempotent);
        state.Transactions.Count.ShouldBe(1);
        state.Get<AccessTelemetryExpiryBucket>($"expiry-bucket/{entry.ExpiryMinute:D12}/{entry.Shard:D2}")
            .Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_SameRecordIdWithDifferentEnvelope_ReturnsConflictAndCommitsNothing()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord original = CreateRecord("01K0A000000000000000000001");
        await store.WriteRecordAndIndexAsync(original, CreateEntry(original), 3600, CancellationToken.None);

        // Same immutable identifier and expiry, different sealed envelope: never an idempotent retry.
        AccessTelemetryRecord impostor = CreateRecord("01K0A000000000000000000001", durationMs: 99);
        impostor.EnvelopeHash.ShouldNotBe(original.EnvelopeHash);

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            impostor,
            CreateEntry(impostor),
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Conflict);
        state.Transactions.Count.ShouldBe(1);
        state.Get<AccessTelemetryRecord>(AccessTelemetryStateStoreTestRecords.RecordKey(original.RecordId))
            .EnvelopeHash.ShouldBe(original.EnvelopeHash);
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_TtlReapedRecordWithLingeringBucketEntry_ReturnsConflictWithoutResurrection()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        // Native component TTL reaps the record but leaves its expiry-bucket entry behind.
        state.ExpireByTtl(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId));

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Conflict);
        state.Transactions.Count.ShouldBe(1);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeFalse();
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entry))
            .Entries.ShouldHaveSingleItem().RecordId.ShouldBe(record.RecordId);
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_ConcurrentCatalogWriteBetweenReadAndCommit_ThrowsAndCommitsNoPartialState()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord first = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry firstEntry = CreateEntry(first);
        await store.WriteRecordAndIndexAsync(first, firstEntry, 3600, CancellationToken.None);

        AccessTelemetryRecord second = CreateRecord("01K0A000000000000000000002", Expiry.AddMinutes(5));
        AccessTelemetryExpiryEntry secondEntry = CreateEntry(second);

        // A concurrent writer commits to the shared catalog after the adapter read its ETag.
        state.BeforeTransaction = () => state.AdvanceETag("expiry-catalog");

        _ = await Should.ThrowAsync<DaprException>(() => store.WriteRecordAndIndexAsync(
            second,
            secondEntry,
            3600,
            CancellationToken.None));

        // Nothing from the rejected transaction landed: no record, no bucket, no catalog minute.
        state.Transactions.Count.ShouldBe(1);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(second.RecordId)).ShouldBeFalse();
        state.Contains(AccessTelemetryStateStoreTestRecords.BucketKey(secondEntry)).ShouldBeFalse();
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([firstEntry.ExpiryMinute]);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_IndexEntryRefersToSupersededEnvelope_ReturnsStaleIndexAndProtectsLiveRecord()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        AccessTelemetryExpiryEntry staleEntry = entry with { EnvelopeHash = new string('b', 64) };

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(staleEntry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.StaleIndex);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeTrue();
        state.Get<AccessTelemetryRecord>(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId))
            .EnvelopeHash.ShouldBe(record.EnvelopeHash);
        state.Contains(AccessTelemetryStateStoreTestRecords.BucketKey(entry)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_TtlReapedRecordWithLingeringBucketEntry_ReturnsAlreadyAbsentAndPrunesEntry()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        state.ExpireByTtl(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId));

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.AlreadyAbsent);
        state.Contains(AccessTelemetryStateStoreTestRecords.BucketKey(entry)).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_BackendKeepsRecordAfterAcknowledgedDelete_ReturnsVerificationFailed()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        // The backend acknowledges the delete but the strong re-read still observes the record.
        _ = state.UndeletableKeys.Add(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId));

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.VerificationFailed);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_EntryCarryingAnotherTenantMarker_IsDeniedAndLeavesTheRecordIntact()
    {
        // Cross-tenant denial at the state-store boundary (Task 1, PG-ONPREM-1 qualification).
        // The sealed envelope hash covers TenantMarker, so an expiry entry minted against a
        // different tenant's marker can never authorise deletion of this tenant's record.
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord tenantA = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entryA = CreateEntry(tenantA);
        await store.WriteRecordAndIndexAsync(tenantA, entryA, 3600, CancellationToken.None);

        AccessTelemetryRecord tenantB = AccessTelemetryStateStoreTestRecords.CreateRecord(
            "01K0A000000000000000000001",
            tenantMarkerFill: 'b');
        AccessTelemetryExpiryEntry foreignEntry = AccessTelemetryStateStoreTestRecords.CreateEntry(tenantB);
        foreignEntry.EnvelopeHash.ShouldNotBe(entryA.EnvelopeHash);

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(foreignEntry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.StaleIndex);
        result.ShouldNotBe(AccessTelemetryDeleteStatus.Deleted);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(tenantA.RecordId)).ShouldBeTrue();
        state.Get<AccessTelemetryRecord>(AccessTelemetryStateStoreTestRecords.RecordKey(tenantA.RecordId))
            .TenantMarker.ShouldBe(tenantA.TenantMarker);
    }

    [Fact]
    public async Task GetDueEntriesAsync_EntriesNotYetDue_AreExcluded()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute - 1,
            10,
            CancellationToken.None);

        due.ShouldBeEmpty();

        // The not-due minute must survive the traversal: it is not an empty minute to prune.
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([entry.ExpiryMinute]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_LimitReachedInAnEarlierMinute_TruncatesAndStopsTraversal()
    {
        var state = new TransactionalDaprState();
        var store = new DaprAccessTelemetryStateStore(state.Client);
        AccessTelemetryRecord firstMinute = CreateRecord("01K0A000000000000000000001", Expiry);
        AccessTelemetryRecord secondMinute = CreateRecord("01K0A000000000000000000002", Expiry.AddMinutes(1));
        await store.WriteRecordAndIndexAsync(firstMinute, CreateEntry(firstMinute), 3600, CancellationToken.None);
        await store.WriteRecordAndIndexAsync(secondMinute, CreateEntry(secondMinute), 3600, CancellationToken.None);

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            AccessTelemetryExpiryIndex.GetExpiryMinute(Expiry.AddMinutes(5)),
            1,
            CancellationToken.None);

        due.ShouldHaveSingleItem().RecordId.ShouldBe(firstMinute.RecordId);

        // Truncation must not drop the untraversed minute from the catalog.
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.Count.ShouldBe(2);
    }

    private static AccessTelemetryExpiryEntry CreateEntry(AccessTelemetryRecord record)
        => AccessTelemetryStateStoreTestRecords.CreateEntry(record);

    private static AccessTelemetryRecord CreateRecord(
        string recordId,
        DateTimeOffset? expiry = null,
        int durationMs = 42)
        => AccessTelemetryStateStoreTestRecords.CreateRecord(recordId, expiry, durationMs);
}
