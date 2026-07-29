// <copyright file="DaprAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Globalization;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr state adapter with strong reads and explicit transactional expiry buckets.</summary>
internal sealed class DaprAccessTelemetryStateStore(DaprClient daprClient, TimeProvider timeProvider) : IAccessTelemetryStateStore
{
    private const int CatalogWriteGuardMinutes = 1;
    private const string ExpiryCatalogKey = "expiry-catalog";
    private const int MaxMinutesPerDueScan = 4;
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
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttlInSeconds, 0);
        byte[] canonicalRecord = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        ValidateEntryMatchesRecord(record, expiryEntry);
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

            await EnsureIndexPresentAsync(expiryEntry, cancellationToken).ConfigureAwait(false);
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

        (AccessTelemetryExpiryCatalog? catalog, string catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);

        AccessTelemetryExpiryBucket updatedBucket = new(
            expiryEntry.ExpiryMinute,
            expiryEntry.Shard,
            (bucket?.Entries ?? [])
                .Append(expiryEntry)
                .OrderBy(static entry => entry.ExpiresAtUtc, StringComparer.Ordinal)
                .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
                .ToArray());
        bool addCatalogMinute = catalog is null || !catalog.ActiveMinutes.Contains(expiryEntry.ExpiryMinute);
        AccessTelemetryExpiryCatalog updatedCatalog = addCatalogMinute
            ? new AccessTelemetryExpiryCatalog(
                (catalog?.ActiveMinutes ?? [])
                    .Append(expiryEntry.ExpiryMinute)
                    .Distinct()
                    .Order()
                    .ToArray())
            : catalog!;
        bool guardAgainstDueMinutePruning = expiryEntry.ExpiryMinute <= AccessTelemetryExpiryIndex.GetExpiryMinute(
            timeProvider.GetUtcNow().AddMinutes(CatalogWriteGuardMinutes));

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
        if (addCatalogMinute || guardAgainstDueMinutePruning)
        {
            operations.Add(new StateTransactionRequest(
                ExpiryCatalogKey,
                JsonSerializer.SerializeToUtf8Bytes(updatedCatalog),
                StateOperationType.Upsert,
                string.IsNullOrEmpty(catalogEtag) ? null : catalogEtag,
                metadata: PartitionMetadata,
                options: StrongFirstWrite));
        }

        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return AccessTelemetryStoreWriteStatus.Inserted;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AccessTelemetryExpiryEntry>> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return [];
        }

        (AccessTelemetryExpiryCatalog? catalog, string catalogEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryCatalog>(
            StoreName,
            ExpiryCatalogKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (catalog is null)
        {
            return [];
        }

        var due = new List<AccessTelemetryExpiryEntry>(limit);
        var emptyMinutes = new List<long>();
        foreach (long minute in catalog.ActiveMinutes
            .Where(minute => minute <= dueMinute)
            .Order()
            .Take(MaxMinutesPerDueScan))
        {
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

            due.AddRange(minuteEntries
                .OrderBy(static entry => entry.ExpiresAtUtc, StringComparer.Ordinal)
                .ThenBy(static entry => entry.Shard)
                .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
                .Take(limit - due.Count));
            if (due.Count >= limit)
            {
                break;
            }
        }

        if (emptyMinutes.Count > 0)
        {
            await RemoveEmptyMinutesAsync(catalog, catalogEtag, emptyMinutes, cancellationToken).ConfigureAwait(false);
        }

        return due;
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
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
    {
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

        var operations = new List<StateTransactionRequest>();
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

        // A missing bucket is repaired only while ETag-touching the catalog. This couples the
        // repair to any empty-minute prune that may have observed the bucket before it appeared.
        if (!catalogContainsMinute || !bucketContainsEntry)
        {
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
        }

        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
            remaining.Length == 0
                ? null
                : JsonSerializer.SerializeToUtf8Bytes(new AccessTelemetryExpiryCatalog(remaining)),
            remaining.Length == 0 ? StateOperationType.Delete : StateOperationType.Upsert,
            string.IsNullOrEmpty(catalogEtag) ? null : catalogEtag,
            PartitionMetadata,
            StrongFirstWrite);
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            [request],
            PartitionMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
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
