// <copyright file="DaprAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr state adapter with strong reads and record/index transactions.</summary>
internal sealed class DaprAccessTelemetryStateStore(DaprClient daprClient) : IAccessTelemetryStateStore
{
    private const string StoreName = AccessTelemetryOptions.RequiredStateStoreName;
    private static readonly IReadOnlyDictionary<string, string> PartitionMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["partitionKey"] = "access-telemetry",
    };
    private static readonly IReadOnlyDictionary<string, string> QueryMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["contentType"] = "application/json",
        ["partitionKey"] = "access-telemetry",
        ["queryIndexName"] = "expiryMinute",
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
        string recordKey = GetRecordKey(record.RecordId);
        (AccessTelemetryRecord? existing, string recordEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _ = AccessTelemetryCanonicalizer.CanonicalizeRecord(existing);
            return string.Equals(existing.EnvelopeHash, record.EnvelopeHash, StringComparison.Ordinal) &&
                string.Equals(existing.ExpiresAtUtc, record.ExpiresAtUtc, StringComparison.Ordinal)
                    ? AccessTelemetryStoreWriteStatus.Idempotent
                    : AccessTelemetryStoreWriteStatus.Conflict;
        }

        string indexKey = GetIndexKey(expiryEntry);
        (AccessTelemetryExpiryEntry? existingIndex, string indexEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryEntry>(
            StoreName,
            indexKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (existingIndex is not null)
        {
            return AccessTelemetryStoreWriteStatus.Conflict;
        }

        IReadOnlyDictionary<string, string> ttlMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["partitionKey"] = "access-telemetry",
            ["ttlInSeconds"] = ttlInSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        IReadOnlyList<StateTransactionRequest> operations =
        [
            new(
                recordKey,
                AccessTelemetryCanonicalizer.CanonicalizeRecord(record),
                StateOperationType.Upsert,
                string.IsNullOrEmpty(recordEtag) ? null : recordEtag,
                metadata: ttlMetadata,
                options: StrongFirstWrite),
            new(
                indexKey,
                JsonSerializer.SerializeToUtf8Bytes(expiryEntry),
                StateOperationType.Upsert,
                string.IsNullOrEmpty(indexEtag) ? null : indexEtag,
                metadata: PartitionMetadata,
                options: StrongFirstWrite),
        ];
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
        string query = JsonSerializer.Serialize(new
        {
            filter = new { LTE = new { expiryMinute = dueMinute } },
            sort = new[] { new { key = "expiryMinute", order = "ASC" } },
            page = new { limit },
        });
        StateQueryResponse<AccessTelemetryExpiryEntry> response = await daprClient.QueryStateAsync<AccessTelemetryExpiryEntry>(
            StoreName,
            query,
            QueryMetadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Results
            .Where(static item => item.Data is not null && item.Key.StartsWith("expiry/", StringComparison.Ordinal))
            .Select(static item => item.Data!)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(AccessTelemetryExpiryEntry entry, CancellationToken cancellationToken)
    {
        string recordKey = GetRecordKey(entry.RecordId);
        (AccessTelemetryRecord? record, string recordEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        string indexKey = GetIndexKey(entry);
        (_, string indexEtag) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryEntry>(
            StoreName,
            indexKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (record is not null &&
            (!string.Equals(record.EnvelopeHash, entry.EnvelopeHash, StringComparison.Ordinal) ||
                !string.Equals(record.ExpiresAtUtc, entry.ExpiresAtUtc, StringComparison.Ordinal)))
        {
            await DeleteIndexAsync(indexKey, indexEtag, cancellationToken).ConfigureAwait(false);
            return AccessTelemetryDeleteStatus.StaleIndex;
        }

        List<StateTransactionRequest> operations = [];
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

        operations.Add(new StateTransactionRequest(
            indexKey,
            null,
            StateOperationType.Delete,
            string.IsNullOrEmpty(indexEtag) ? null : indexEtag,
            PartitionMetadata,
            StrongFirstWrite));
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryRecord? remaining, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryRecord>(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryExpiryEntry? remainingIndex, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryEntry>(
            StoreName,
            indexKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (remaining is not null || remainingIndex is not null)
        {
            return AccessTelemetryDeleteStatus.VerificationFailed;
        }

        return record is null ? AccessTelemetryDeleteStatus.AlreadyAbsent : AccessTelemetryDeleteStatus.Deleted;
    }

    private async Task DeleteIndexAsync(string indexKey, string indexEtag, CancellationToken cancellationToken)
    {
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            [new StateTransactionRequest(
                indexKey,
                null,
                StateOperationType.Delete,
                string.IsNullOrEmpty(indexEtag) ? null : indexEtag,
                PartitionMetadata,
                StrongFirstWrite)],
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        (AccessTelemetryExpiryEntry? remaining, _) = await daprClient.GetStateAndETagAsync<AccessTelemetryExpiryEntry>(
            StoreName,
            indexKey,
            ConsistencyMode.Strong,
            PartitionMetadata,
            cancellationToken).ConfigureAwait(false);
        if (remaining is not null)
        {
            throw new InvalidOperationException("The stale lifecycle expiry index could not be strongly verified absent.");
        }
    }

    private static string GetIndexKey(AccessTelemetryExpiryEntry entry)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"expiry/{entry.ExpiryMinute:D12}/{entry.Shard:D2}/{entry.RecordId}");

    private static string GetRecordKey(string recordId)
        => $"records/{AccessTelemetryExpiryIndex.GetShard(recordId):D2}/{recordId}";
}
