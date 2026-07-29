// <copyright file="DaprAccessTelemetryStateStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Tests.Lifecycle;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Microsoft.Extensions.Time.Testing;

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
        var store = CreateStore(state);
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
        transaction.ShouldAllBe(static operation => operation.Options!.Concurrency == ConcurrencyMode.FirstWrite);
        transaction.ShouldAllBe(static operation => operation.Options!.Consistency == ConsistencyMode.Strong);
        state.Get<AccessTelemetryExpiryBucket>(transaction[1].Key).Entries.ShouldHaveSingleItem().ShouldBe(entry);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([entry.ExpiryMinute]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_TraversesExplicitBucketsWithoutQueryApi()
    {
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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
    public async Task WriteRecordAndIndexAsync_IdempotentRecordWithMissingIndex_RepairsBucketAndCatalog()
    {
        var state = new TransactionalDaprState();
        DaprAccessTelemetryStateStore store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        state.Seed(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId), record);

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Idempotent);
        IReadOnlyList<StateTransactionRequest> repair = state.Transactions.ShouldHaveSingleItem();
        repair.Select(static operation => operation.Key).ShouldBe(
        [
            AccessTelemetryStateStoreTestRecords.BucketKey(entry),
            "expiry-catalog",
        ]);
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entry))
            .Entries.ShouldHaveSingleItem().ShouldBe(entry);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([entry.ExpiryMinute]);
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_SameRecordIdWithDifferentEnvelope_ReturnsConflictAndCommitsNothing()
    {
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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
    public async Task GetDueEntriesAsync_ConcurrentDueMinuteWrite_InvalidatesEmptyMinutePruneAndKeepsEntryDiscoverable()
    {
        var state = new TransactionalDaprState();
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        state.Seed("expiry-catalog", new AccessTelemetryExpiryCatalog([entry.ExpiryMinute]));
        DaprAccessTelemetryStateStore store = CreateStore(state, Expiry.AddMinutes(2));
        AccessTelemetryStoreWriteStatus? writeStatus = null;
        state.BeforeTransaction = () =>
        {
            state.BeforeTransaction = null;
            writeStatus = store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        };

        _ = await Should.ThrowAsync<DaprException>(() => store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None));

        writeStatus.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);
        due.ShouldHaveSingleItem().ShouldBe(entry);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldContain(entry.ExpiryMinute);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_IndexEntryRefersToSupersededEnvelope_ReturnsStaleIndexAndProtectsLiveRecord()
    {
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        // The superseded entry shares (ExpiryMinute, Shard) with the live record's own entry, so a
        // prune that matched on RecordId alone would delete the live entry too.
        AccessTelemetryExpiryEntry staleEntry = entry with { EnvelopeHash = new string('b', 64) };
        AccessTelemetryStateStoreTestRecords.BucketKey(staleEntry)
            .ShouldBe(AccessTelemetryStateStoreTestRecords.BucketKey(entry));
        state.Seed(
            AccessTelemetryStateStoreTestRecords.BucketKey(entry),
            new AccessTelemetryExpiryBucket(entry.ExpiryMinute, entry.Shard, [entry, staleEntry]));

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(staleEntry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.StaleIndex);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeTrue();
        state.Get<AccessTelemetryRecord>(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId))
            .EnvelopeHash.ShouldBe(record.EnvelopeHash);

        // The live record's own index entry must survive, and it must stay purgeable. Buckets are
        // the only source GetDueEntriesAsync reads, so losing the entry orphans the record forever.
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entry))
            .Entries.ShouldHaveSingleItem().ShouldBe(entry);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None);
        due.ShouldHaveSingleItem().ShouldBe(entry);
    }

    [Theory]
    [InlineData("record-id")]
    [InlineData("expiry-minute")]
    [InlineData("shard")]
    [InlineData("envelope-hash")]
    [InlineData("expires-at")]
    public async Task WriteRecordAndIndexAsync_ExpiryEntryIdentityDoesNotMatchRecord_Throws(string mismatch)
    {
        var state = new TransactionalDaprState();
        DaprAccessTelemetryStateStore store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        AccessTelemetryExpiryEntry mismatched = mismatch switch
        {
            "record-id" => entry with { RecordId = "01K0A000000000000000000002" },
            "expiry-minute" => entry with { ExpiryMinute = entry.ExpiryMinute + 1 },
            "shard" => entry with { Shard = (entry.Shard + 1) % 64 },
            "envelope-hash" => entry with { EnvelopeHash = new string('b', 64) },
            "expires-at" => entry with { ExpiresAtUtc = AccessTelemetryStateStoreTestRecords.Format(Expiry.AddMinutes(1)) },
            _ => throw new InvalidOperationException($"Unknown mismatch '{mismatch}'."),
        };

        _ = await Should.ThrowAsync<ArgumentException>(() => store.WriteRecordAndIndexAsync(
            record,
            mismatched,
            3600,
            CancellationToken.None));

        state.Transactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_TtlReapedRecordWithLingeringBucketEntry_ReturnsAlreadyAbsentAndPrunesEntry()
    {
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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
    public async Task DeleteAndVerifyAsync_EntryCarryingAnotherTenantMarker_IsDeniedAndLeavesTheRecordPurgeable()
    {
        // Envelope-binding denial at the state-store boundary (Task 1, PG-ONPREM-1 qualification).
        // The sealed envelope hash covers TenantMarker, so an expiry entry minted against a
        // different tenant's marker can never authorise deletion of this tenant's record.
        //
        // Scope, stated precisely: this is NOT physical cross-tenant isolation evidence. The record
        // key (`records/{shard}/{recordId}`) carries no tenant dimension - this is a single global
        // infrastructure store owned by one fixed actor - so both records below resolve to the same
        // state key by construction. What is proven here is that the marker is inside the sealed
        // envelope and therefore participates in the authorisation decision. Gate C1.11 (physical
        // cross-tenant denial against the running profile) is not discharged by this test and says so.
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
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

        // The denial must not cost tenant A its index entry: the foreign entry lands in the same
        // (ExpiryMinute, Shard) bucket, so a RecordId-only prune would silently make the record
        // unpurgeable while still reporting a successful denial.
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entryA))
            .Entries.ShouldHaveSingleItem().ShouldBe(entryA);
        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            entryA.ExpiryMinute,
            10,
            CancellationToken.None);
        due.ShouldHaveSingleItem().ShouldBe(entryA);
    }

    [Fact]
    public async Task GetDueEntriesAsync_EntriesNotYetDue_AreExcluded()
    {
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
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
        var store = CreateStore(state);
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

    [Fact]
    public async Task GetDueEntriesAsync_EmptyMinutePrefix_ScansAndPrunesAtMostFourMinutesPerTurn()
    {
        var state = new TransactionalDaprState();
        DaprAccessTelemetryStateStore store = CreateStore(state);
        long[] minutes = Enumerable.Range(0, 5).Select(offset => 1000L + offset).ToArray();
        state.Seed("expiry-catalog", new AccessTelemetryExpiryCatalog(minutes));

        IReadOnlyList<AccessTelemetryExpiryEntry> due = await store.GetDueEntriesAsync(
            minutes[^1],
            1,
            CancellationToken.None);

        due.ShouldBeEmpty();
        state.ReadKeys.Count(static key => key.StartsWith("expiry-bucket/", StringComparison.Ordinal)).ShouldBe(4 * 64);
        state.Get<AccessTelemetryExpiryCatalog>("expiry-catalog").ActiveMinutes.ShouldBe([minutes[^1]]);
    }

    [Fact]
    public async Task GetDueEntriesAsync_BucketIdentityDoesNotMatchStateKey_Throws()
    {
        var state = new TransactionalDaprState();
        DaprAccessTelemetryStateStore store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        state.Seed("expiry-catalog", new AccessTelemetryExpiryCatalog([entry.ExpiryMinute]));
        state.Seed(
            AccessTelemetryStateStoreTestRecords.BucketKey(entry),
            new AccessTelemetryExpiryBucket(entry.ExpiryMinute + 1, entry.Shard, [entry]));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => store.GetDueEntriesAsync(
            entry.ExpiryMinute,
            10,
            CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_BackendKeepsTheBucketEntryAfterAcknowledgedRewrite_ReturnsVerificationFailed()
    {
        // The index side of the durability contract: the backend acknowledges the bucket rewrite
        // that drops this entry, but the strong re-read still observes it.
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
        // Both identifiers hash to shard 1 and share the expiry minute, so both entries live in one
        // bucket and removing either is an Upsert of the surviving bucket, not a Delete of the key.
        AccessTelemetryRecord kept = CreateRecord("01K0A000000000000000000033");
        AccessTelemetryRecord alsoDue = CreateRecord("01K0A000000000000000000069", Expiry.AddMilliseconds(500));
        AccessTelemetryExpiryEntry keptEntry = CreateEntry(kept);
        AccessTelemetryExpiryEntry alsoDueEntry = CreateEntry(alsoDue);
        await store.WriteRecordAndIndexAsync(kept, keptEntry, 3600, CancellationToken.None);
        await store.WriteRecordAndIndexAsync(alsoDue, alsoDueEntry, 3600, CancellationToken.None);
        AccessTelemetryStateStoreTestRecords.BucketKey(alsoDueEntry)
            .ShouldBe(AccessTelemetryStateStoreTestRecords.BucketKey(keptEntry));

        _ = state.UnappliedUpsertKeys.Add(AccessTelemetryStateStoreTestRecords.BucketKey(keptEntry));

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(keptEntry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.VerificationFailed);
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(kept.RecordId)).ShouldBeFalse();
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(keptEntry))
            .Entries.ShouldContain(keptEntry);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_StaleEntryRewriteIsAcknowledgedButNotApplied_ThrowsRatherThanReportingSuccess()
    {
        // The stale-index prune runs outside the main transaction, so it verifies its own removal
        // and must refuse to report StaleIndex when the backend did not actually apply it.
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        // A superseded generation of the same record still occupies the bucket, so pruning it is a
        // bucket Upsert that leaves the live entry behind rather than a Delete of the bucket key.
        AccessTelemetryRecord superseded = CreateRecord("01K0A000000000000000000001", durationMs: 99);
        AccessTelemetryExpiryEntry supersededEntry = CreateEntry(superseded);
        state.Seed(
            AccessTelemetryStateStoreTestRecords.BucketKey(entry),
            new AccessTelemetryExpiryBucket(entry.ExpiryMinute, entry.Shard, [entry, supersededEntry]));
        _ = state.UnappliedUpsertKeys.Add(AccessTelemetryStateStoreTestRecords.BucketKey(entry));

        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            store.DeleteAndVerifyAsync(supersededEntry, CancellationToken.None));

        // The live record and its own index entry are untouched by the failed prune.
        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeTrue();
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entry))
            .Entries.ShouldContain(entry);
    }

    [Fact]
    public async Task WriteRecordAndIndexAsync_CarriesPartitionAndTtlMetadataTheComponentRequires()
    {
        // The fake rejects a transaction whose operations drop `partitionKey`, or whose record
        // upsert drops `ttlInSeconds`, so this asserts the metadata contract rather than restating
        // the captured request.
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);

        AccessTelemetryStoreWriteStatus result = await store.WriteRecordAndIndexAsync(
            record,
            entry,
            3600,
            CancellationToken.None);

        result.ShouldBe(AccessTelemetryStoreWriteStatus.Inserted);
        IReadOnlyList<StateTransactionRequest> committed = state.Transactions.ShouldHaveSingleItem();
        committed.ShouldAllBe(static operation => operation.Metadata!["partitionKey"] == "access-telemetry");
        committed
            .Where(static operation => operation.Key.StartsWith("records/", StringComparison.Ordinal))
            .ShouldAllBe(static operation => operation.Metadata!.ContainsKey("ttlInSeconds"));
        committed
            .Where(static operation => !operation.Key.StartsWith("records/", StringComparison.Ordinal))
            .ShouldAllBe(static operation => !operation.Metadata!.ContainsKey("ttlInSeconds"));
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_CarriesPartitionMetadataAndNeverTtlOnAnyOperation()
    {
        // The write path has its own metadata guard above; the delete path had none. The fake's
        // RequireMetadata throws for any operation with wrong metadata, so the existing green suite
        // proves this implicitly — but implicitly only, and a reader auditing the component contract
        // for the delete transaction had no assertion to point at. Records expire natively, so a
        // delete must never carry ttlInSeconds while still carrying the co-location partition key.
        var state = new TransactionalDaprState();
        var store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);

        AccessTelemetryDeleteStatus result = await store.DeleteAndVerifyAsync(entry, CancellationToken.None);

        result.ShouldBe(AccessTelemetryDeleteStatus.Deleted);
        IReadOnlyList<StateTransactionRequest> deleteTransaction = state.Transactions.Skip(1).ShouldHaveSingleItem();
        deleteTransaction.Count.ShouldBe(2);
        deleteTransaction.ShouldAllBe(static operation => operation.Metadata!["partitionKey"] == "access-telemetry");
        deleteTransaction.ShouldAllBe(static operation => !operation.Metadata!.ContainsKey("ttlInSeconds"));
        deleteTransaction.ShouldAllBe(static operation => operation.OperationType == StateOperationType.Delete);
    }

    [Fact]
    public async Task DeleteAndVerifyAsync_SecondOperationFails_CommitsNoPartialDelete()
    {
        var state = new TransactionalDaprState();
        DaprAccessTelemetryStateStore store = CreateStore(state);
        AccessTelemetryRecord record = CreateRecord("01K0A000000000000000000001");
        AccessTelemetryExpiryEntry entry = CreateEntry(record);
        _ = await store.WriteRecordAndIndexAsync(record, entry, 3600, CancellationToken.None);
        state.FailAtOperationIndex = 1;

        _ = await Should.ThrowAsync<DaprException>(() => store.DeleteAndVerifyAsync(entry, CancellationToken.None));

        state.Contains(AccessTelemetryStateStoreTestRecords.RecordKey(record.RecordId)).ShouldBeTrue();
        state.Get<AccessTelemetryExpiryBucket>(AccessTelemetryStateStoreTestRecords.BucketKey(entry))
            .Entries.ShouldHaveSingleItem().ShouldBe(entry);
    }

    private static AccessTelemetryExpiryEntry CreateEntry(AccessTelemetryRecord record)
        => AccessTelemetryStateStoreTestRecords.CreateEntry(record);

    private static AccessTelemetryRecord CreateRecord(
        string recordId,
        DateTimeOffset? expiry = null,
        int durationMs = 42)
        => AccessTelemetryStateStoreTestRecords.CreateRecord(recordId, expiry, durationMs);

    private static DaprAccessTelemetryStateStore CreateStore(
        TransactionalDaprState state,
        DateTimeOffset? now = null)
        => new(state.Client, new FakeTimeProvider(now ?? Expiry.AddHours(-1)));
}
