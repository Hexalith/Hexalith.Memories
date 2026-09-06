// <copyright file="DaprAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Globalization;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Observability;

/// <summary>Dapr state adapter with strong reads and explicit transactional expiry buckets.</summary>
internal sealed class DaprAccessTelemetryStateStore(DaprClient daprClient, TimeProvider timeProvider) : IAccessTelemetryStateStore
{
    private const string ExpiryCatalogKey = "expiry-catalog";
    private const int MaxMinutesPerDueScan = 3;
    private const string StoreName = AccessTelemetryOptions.RequiredStateStoreName;
    private static readonly IReadOnlyDictionary<string, string> PartitionMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["partitionKey"] = "access-telemetry",
    };
    private static readonly StateOptions StrongFirstWrite = new()
    {
        Concurrency = ConcurrencyMode.FirstWrite,
        Consistency = ConsistencyMode.Strong,
    };

    /// <inheritdoc/>
    public async Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttlInSeconds, 0);
        byte[] canonicalRecord = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        ValidateEntryMatchesRecord(record, expiryEntry);
        (AccessTelemetryExpiryCatalog catalog, string catalogEtag) = await GetOrCreateCatalogAsync(cancellationToken)
            .ConfigureAwait(false);
        string recordKey = GetRecordKey(record.RecordId);
        (AccessTelemetryRecord? existing, string recordEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _ = AccessTelemetryCanonicalizer.CanonicalizeRecord(NormalizeStoredRecord(existing));
            if (!string.Equals(existing.EnvelopeHash, record.EnvelopeHash, StringComparison.Ordinal) ||
                !string.Equals(existing.ExpiresAtUtc, record.ExpiresAtUtc, StringComparison.Ordinal))
            {
                return AccessTelemetryStoreWriteStatus.Conflict;
            }

            await EnsureIndexPresentAsync(
                record,
                canonicalRecord,
                recordEtag,
                expiryEntry,
                cancellationToken).ConfigureAwait(false);
            return AccessTelemetryStoreWriteStatus.Idempotent;
        }

        string bucketKey = GetBucketKey(expiryEntry.ExpiryMinute, expiryEntry.Shard);
        (AccessTelemetryExpiryBucket? bucket, string bucketEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            bucketKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        ValidateBucket(bucket, expiryEntry.ExpiryMinute, expiryEntry.Shard);
        if (bucket?.Entries.Any(entry => string.Equals(entry.RecordId, expiryEntry.RecordId, StringComparison.Ordinal)) == true)
        {
            return AccessTelemetryStoreWriteStatus.Conflict;
        }

        AccessTelemetryExpiryBucket updatedBucket = new(
            expiryEntry.ExpiryMinute,
            expiryEntry.Shard,
            (bucket?.Entries ?? [])
                .Append(expiryEntry)
                .OrderBy(static entry => entry.ExpiresAtUtc, StringComparer.Ordinal)
                .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
                .ToArray());
        bool addCatalogMinute = !catalog.ActiveMinutes.Contains(expiryEntry.ExpiryMinute);
        AccessTelemetryExpiryCatalog updatedCatalog = addCatalogMinute
            ? new AccessTelemetryExpiryCatalog(
                catalog.ActiveMinutes
                    .Append(expiryEntry.ExpiryMinute)
                    .Distinct()
                    .Order()
                    .ToArray())
            : catalog;

        IReadOnlyDictionary<string, string> ttlMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["partitionKey"] = "access-telemetry",
            ["ttlInSeconds"] = ttlInSeconds.ToString(CultureInfo.InvariantCulture),
        };
        var operations = new List<StateTransactionRequest>
        {
            new(
                recordKey,
                canonicalRecord,
                StateOperationType.Upsert,
                string.IsNullOrEmpty(recordEtag) ? null : recordEtag,
                metadata: ttlMetadata,
                options: StrongFirstWrite),
            new(
                bucketKey,
                JsonSerializer.SerializeToUtf8Bytes(updatedBucket),
                StateOperationType.Upsert,
                string.IsNullOrEmpty(bucketEtag) ? null : bucketEtag,
                metadata: PartitionMetadata,
                options: StrongFirstWrite),
        };
        operations.Add(new StateTransactionRequest(
            ExpiryCatalogKey,
            JsonSerializer.SerializeToUtf8Bytes(updatedCatalog),
            StateOperationType.Upsert,
            catalogEtag,
            metadata: PartitionMetadata,
            options: StrongFirstWrite));

        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordStateOperations(operations.Count);
        return AccessTelemetryStoreWriteStatus.Inserted;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return ([], false);
        }

        (AccessTelemetryExpiryCatalog? catalog, string catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (catalog is null)
        {
            return ([], false);
        }

        var due = new List<AccessTelemetryExpiryEntry>(limit);
        var emptyMinutes = new List<long>();
        long[] dueMinutes = catalog.ActiveMinutes
            .Where(minute => minute <= dueMinute)
            .Order()
            .ToArray();
        long[] scannedMinutes = dueMinutes.Take(MaxMinutesPerDueScan).ToArray();
        bool hasMoreDueEntries = dueMinutes.Length > scannedMinutes.Length;
        for (int minuteIndex = 0; minuteIndex < scannedMinutes.Length; minuteIndex++)
        {
            long minute = scannedMinutes[minuteIndex];
            var minuteEntries = new List<AccessTelemetryExpiryEntry>();
            for (int shard = 0; shard < 64; shard++)
            {
                (AccessTelemetryExpiryBucket? bucket, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
                    StoreName,
                    GetBucketKey(minute, shard),
                    ConsistencyMode.Strong,
                    PartitionMetadata,
                    cancellationToken).ConfigureAwait(false);
                if (bucket is not null)
                {
                    ValidateBucket(bucket, minute, shard);
                    minuteEntries.AddRange(bucket.Entries);
                }
            }

            if (minuteEntries.Count == 0)
            {
                emptyMinutes.Add(minute);
                continue;
            }

            AccessTelemetryExpiryEntry[] orderedEntries = minuteEntries
                .OrderBy(static entry => entry.ExpiresAtUtc, StringComparer.Ordinal)
                .ThenBy(static entry => entry.Shard)
                .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
                .ToArray();
            int remainingCapacity = limit - due.Count;
            due.AddRange(orderedEntries.Take(remainingCapacity));
            if (due.Count >= limit)
            {
                hasMoreDueEntries = hasMoreDueEntries ||
                    orderedEntries.Length > remainingCapacity ||
                    minuteIndex < dueMinutes.Length - 1;
                break;
            }
        }

        if (emptyMinutes.Count > 0)
        {
            await RemoveEmptyMinutesAsync(catalog, catalogEtag, emptyMinutes, cancellationToken).ConfigureAwait(false);
        }

        return (due, hasMoreDueEntries);
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
    {
        string recordKey = GetRecordKey(entry.RecordId);
        (AccessTelemetryRecord? record, string recordEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        string bucketKey = GetBucketKey(entry.ExpiryMinute, entry.Shard);
        (AccessTelemetryExpiryBucket? bucket, string bucketEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            bucketKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        ValidateBucket(bucket, entry.ExpiryMinute, entry.Shard);
        // Match the complete entry identity, never the record identifier alone. A superseded or
        // foreign entry can share (ExpiryMinute, Shard) with the live record's own entry, and
        // pruning by RecordId would delete the live entry too. GetDueEntriesAsync reads purge
        // candidates only from buckets, so that record would never be rediscovered or purged.
        bool bucketContainsEntry = bucket?.Entries.Any(candidate => candidate == entry) == true;

        if (record is not null &&
            (!string.Equals(record.EnvelopeHash, entry.EnvelopeHash, StringComparison.Ordinal) ||
                !string.Equals(record.ExpiresAtUtc, entry.ExpiresAtUtc, StringComparison.Ordinal)))
        {
            if (bucketContainsEntry)
            {
                await RemoveBucketEntryAsync(bucket!, bucketEtag, entry, cancellationToken).ConfigureAwait(false);
            }

            return AccessTelemetryDeleteStatus.StaleIndex;
        }

        var operations = new List<StateTransactionRequest>();
        if (record is not null)
        {
            operations.Add(new StateTransactionRequest(
                recordKey,
                null,
                StateOperationType.Delete,
                recordEtag,
                PartitionMetadata,
                StrongFirstWrite));
        }

        if (bucketContainsEntry)
        {
            operations.Add(CreateBucketRemoval(bucket!, bucketEtag, entry));
        }

        if (operations.Count > 0)
        {
            await daprClient.ExecuteStateTransactionAsync(
                StoreName,
                operations,
                PartitionMetadata,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            AccessTelemetryLifecycleMetrics.RecordStateOperations(operations.Count);
        }

        (AccessTelemetryRecord? remaining, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryExpiryBucket? remainingBucket, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            bucketKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        ValidateBucket(remainingBucket, entry.ExpiryMinute, entry.Shard);
        if (remaining is not null || remainingBucket?.Entries.Any(candidate => candidate == entry) == true)
        {
            return AccessTelemetryDeleteStatus.VerificationFailed;
        }

        return record is null ? AccessTelemetryDeleteStatus.AlreadyAbsent : AccessTelemetryDeleteStatus.Deleted;
    }

    private static StateTransactionRequest CreateBucketRemoval(
        AccessTelemetryExpiryBucket bucket,
        string bucketEtag,
        AccessTelemetryExpiryEntry entry)
    {
        // Remove only the exact entry. Other entries sharing this record identifier are separate
        // index generations and are resolved on their own purge pass.
        AccessTelemetryExpiryEntry[] remaining = bucket.Entries
            .Where(candidate => candidate != entry)
            .ToArray();
        return new StateTransactionRequest(
            GetBucketKey(bucket.ExpiryMinute, bucket.Shard),
            remaining.Length == 0
                ? null
                : JsonSerializer.SerializeToUtf8Bytes(bucket with { Entries = remaining }),
            remaining.Length == 0 ? StateOperationType.Delete : StateOperationType.Upsert,
            string.IsNullOrEmpty(bucketEtag) ? null : bucketEtag,
            PartitionMetadata,
            StrongFirstWrite);
    }

    private async Task EnsureIndexPresentAsync(
        AccessTelemetryRecord record,
        byte[] canonicalRecord,
        string recordEtag,
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recordEtag))
        {
            throw new InvalidOperationException(
                "An idempotent lifecycle index repair requires the owning record's ETag.");
        }

        string bucketKey = GetBucketKey(entry.ExpiryMinute, entry.Shard);
        (AccessTelemetryExpiryBucket? bucket, string bucketEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            bucketKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        ValidateBucket(bucket, entry.ExpiryMinute, entry.Shard);
        (AccessTelemetryExpiryCatalog? catalog, string catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);

        bool bucketContainsEntry = bucket?.Entries.Any(candidate => candidate == entry) == true;
        bool catalogContainsMinute = catalog?.ActiveMinutes.Contains(entry.ExpiryMinute) == true;
        if (bucketContainsEntry && catalogContainsMinute)
        {
            return;
        }

        DateTimeOffset expiresAt = DateTimeOffset.ParseExact(
            record.ExpiresAtUtc,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        int remainingTtlInSeconds = checked((int)Math.Ceiling((expiresAt - timeProvider.GetUtcNow()).TotalSeconds));
        if (remainingTtlInSeconds <= 0)
        {
            throw new InvalidOperationException("An expired lifecycle record cannot own an index repair.");
        }

        IReadOnlyDictionary<string, string> ttlMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["partitionKey"] = "access-telemetry",
            ["ttlInSeconds"] = remainingTtlInSeconds.ToString(CultureInfo.InvariantCulture),
        };
        var operations = new List<StateTransactionRequest>
        {
            new(
                GetRecordKey(record.RecordId),
                canonicalRecord,
                StateOperationType.Upsert,
                recordEtag,
                ttlMetadata,
                StrongFirstWrite),
        };
        if (!bucketContainsEntry)
        {
            AccessTelemetryExpiryBucket repairedBucket = new(
                entry.ExpiryMinute,
                entry.Shard,
                (bucket?.Entries ?? [])
                    .Append(entry)
                    .Distinct()
                    .OrderBy(static candidate => candidate.ExpiresAtUtc, StringComparer.Ordinal)
                    .ThenBy(static candidate => candidate.RecordId, StringComparer.Ordinal)
                    .ToArray());
            operations.Add(new StateTransactionRequest(
                bucketKey,
                JsonSerializer.SerializeToUtf8Bytes(repairedBucket),
                StateOperationType.Upsert,
                string.IsNullOrEmpty(bucketEtag) ? null : bucketEtag,
                PartitionMetadata,
                StrongFirstWrite));
        }

        // Every repair ETag-touches the permanent catalog. This couples it to concurrent pruning
        // and makes the catalog the shared serialization fence for record/index ownership.
        AccessTelemetryExpiryCatalog repairedCatalog = new(
            (catalog?.ActiveMinutes ?? [])
                .Append(entry.ExpiryMinute)
                .Distinct()
                .Order()
                .ToArray());
        operations.Add(new StateTransactionRequest(
            ExpiryCatalogKey,
            JsonSerializer.SerializeToUtf8Bytes(repairedCatalog),
            StateOperationType.Upsert,
            string.IsNullOrEmpty(catalogEtag) ? null : catalogEtag,
            PartitionMetadata,
            StrongFirstWrite));

        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordStateOperations(operations.Count);

        (AccessTelemetryRecord? verifiedRecord, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            GetRecordKey(record.RecordId),
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryExpiryBucket? verifiedBucket, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            bucketKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryExpiryCatalog? verifiedCatalog, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        ValidateBucket(verifiedBucket, entry.ExpiryMinute, entry.Shard);
        bool recordMatches = verifiedRecord is not null &&
            AccessTelemetryCanonicalizer.CanonicalizeRecord(NormalizeStoredRecord(verifiedRecord)).SequenceEqual(canonicalRecord);
        if (!recordMatches ||
            verifiedBucket?.Entries.Any(candidate => candidate == entry) != true ||
            verifiedCatalog?.ActiveMinutes.Contains(entry.ExpiryMinute) != true)
        {
            throw new InvalidOperationException(
                "The idempotent lifecycle index repair could not be strongly verified.");
        }
    }

    private async Task<(AccessTelemetryExpiryCatalog Catalog, string ETag)> GetOrCreateCatalogAsync(
        CancellationToken cancellationToken)
    {
        (AccessTelemetryExpiryCatalog? catalog, string catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (catalog is null)
        {
            await daprClient.ExecuteStateTransactionAsync(
                StoreName,
                [new StateTransactionRequest(
                    ExpiryCatalogKey,
                    JsonSerializer.SerializeToUtf8Bytes(new AccessTelemetryExpiryCatalog([])),
                    StateOperationType.Upsert,
                    null,
                    PartitionMetadata,
                    StrongFirstWrite)],
                PartitionMetadata,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            AccessTelemetryLifecycleMetrics.RecordStateOperations(1);
            (catalog, catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
                StoreName,
                ExpiryCatalogKey,
                ConsistencyMode.Strong,
                PartitionMetadata,
                cancellationToken).ConfigureAwait(false);
        }

        if (catalog is null || string.IsNullOrEmpty(catalogEtag))
        {
            throw new InvalidOperationException(
                "The lifecycle expiry catalog could not be strongly read with an ETag after initialization.");
        }

        return (catalog, catalogEtag);
    }

    private async Task RemoveBucketEntryAsync(
        AccessTelemetryExpiryBucket bucket,
        string bucketEtag,
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
    {
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            [CreateBucketRemoval(bucket, bucketEtag, entry)],
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordStateOperations(1);
        (AccessTelemetryExpiryBucket? remaining, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryBucket>(
            StoreName,
            GetBucketKey(bucket.ExpiryMinute, bucket.Shard),
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (remaining?.Entries.Any(candidate => candidate == entry) == true)
        {
            throw new InvalidOperationException("The stale lifecycle expiry bucket entry could not be strongly verified absent.");
        }
    }

    private async Task RemoveEmptyMinutesAsync(
        AccessTelemetryExpiryCatalog catalog,
        string catalogEtag,
        IReadOnlyCollection<long> emptyMinutes,
        CancellationToken cancellationToken)
    {
        long[] remaining = catalog.ActiveMinutes.Except(emptyMinutes).Order().ToArray();
        var request = new StateTransactionRequest(
            ExpiryCatalogKey,
            JsonSerializer.SerializeToUtf8Bytes(new AccessTelemetryExpiryCatalog(remaining)),
            StateOperationType.Upsert,
            string.IsNullOrEmpty(catalogEtag) ? null : catalogEtag,
            PartitionMetadata,
            StrongFirstWrite);
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            [request],
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        AccessTelemetryLifecycleMetrics.RecordStateOperations(1);
    }

    private static string GetBucketKey(long expiryMinute, int shard)
        => string.Create(CultureInfo.InvariantCulture, $"expiry-bucket/{expiryMinute:D12}/{shard:D2}");

    private static string GetRecordKey(string recordId)
        => $"records/{AccessTelemetryExpiryIndex.GetShard(recordId):D2}/{recordId}";

    private static void ValidateBucket(AccessTelemetryExpiryBucket? bucket, long expectedMinute, int expectedShard)
    {
        if (bucket is null)
        {
            return;
        }

        if (bucket.ExpiryMinute != expectedMinute || bucket.Shard != expectedShard || bucket.Entries.Any(entry =>
            entry.ExpiryMinute != expectedMinute ||
            entry.Shard != expectedShard ||
            AccessTelemetryExpiryIndex.GetShard(entry.RecordId) != expectedShard ||
            !DateTimeOffset.TryParseExact(
                entry.ExpiresAtUtc,
                "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTimeOffset expiresAt) ||
            AccessTelemetryExpiryIndex.GetExpiryMinute(expiresAt) != expectedMinute))
        {
            throw new InvalidOperationException(
                $"Expiry bucket '{GetBucketKey(expectedMinute, expectedShard)}' contains mismatched identity data.");
        }
    }

    private static void ValidateEntryMatchesRecord(AccessTelemetryRecord record, AccessTelemetryExpiryEntry entry)
    {
        DateTimeOffset expiresAt = DateTimeOffset.ParseExact(
            record.ExpiresAtUtc,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        if (!string.Equals(entry.RecordId, record.RecordId, StringComparison.Ordinal) ||
            !string.Equals(entry.EnvelopeHash, record.EnvelopeHash, StringComparison.Ordinal) ||
            !string.Equals(entry.ExpiresAtUtc, record.ExpiresAtUtc, StringComparison.Ordinal) ||
            entry.Shard != AccessTelemetryExpiryIndex.GetShard(record.RecordId) ||
            entry.ExpiryMinute != AccessTelemetryExpiryIndex.GetExpiryMinute(expiresAt))
        {
            throw new ArgumentException("The expiry entry does not match the canonical record identity.", nameof(entry));
        }
    }

    private static AccessTelemetryRecord NormalizeStoredRecord(AccessTelemetryRecord record)
        => record with
        {
            QueryParams = record.QueryParams.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value is JsonElement element ? ReadStoredScalar(element) : pair.Value,
                StringComparer.Ordinal),
        };

    private static object? ReadStoredScalar(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out int number) => number,
            JsonValueKind.Null => null,
            _ => throw new AccessTelemetryContractException("query_params_value_invalid"),
        };
}
