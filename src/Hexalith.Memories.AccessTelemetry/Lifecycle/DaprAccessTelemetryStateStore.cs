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
        ReadOnlyMemory<byte> existingBytes = await daprClient.GetByteStateAsync(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!existingBytes.IsEmpty)
        {
            AccessTelemetryRecord existing = AccessTelemetryCanonicalizer.ParseCanonicalRecord(existingBytes.Span);
            return string.Equals(existing.EnvelopeHash, record.EnvelopeHash, StringComparison.Ordinal) &&
                string.Equals(existing.ExpiresAtUtc, record.ExpiresAtUtc, StringComparison.Ordinal)
                    ? AccessTelemetryStoreWriteStatus.Idempotent
                    : AccessTelemetryStoreWriteStatus.Conflict;
        }

        IReadOnlyDictionary<string, string> ttlMetadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ttlInSeconds"] = ttlInSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        IReadOnlyList<StateTransactionRequest> operations =
        [
            new(
                recordKey,
                AccessTelemetryCanonicalizer.CanonicalizeRecord(record),
                StateOperationType.Upsert,
                metadata: ttlMetadata,
                options: StrongFirstWrite),
            new(
                GetIndexKey(expiryEntry),
                JsonSerializer.SerializeToUtf8Bytes(expiryEntry),
                StateOperationType.Upsert,
                metadata: ttlMetadata,
                options: StrongFirstWrite),
        ];
        await daprClient.ExecuteStateTransactionAsync(
            StoreName,
            operations,
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
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Results
            .Where(static item => item.Data is not null && item.Key.StartsWith("expiry/", StringComparison.Ordinal))
            .Select(static item => item.Data!)
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAndVerifyAsync(AccessTelemetryExpiryEntry entry, CancellationToken cancellationToken)
    {
        string recordKey = GetRecordKey(entry.RecordId);
        await daprClient.DeleteStateAsync(
            StoreName,
            recordKey,
            StrongFirstWrite,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        ReadOnlyMemory<byte> remaining = await daprClient.GetByteStateAsync(
            StoreName,
            recordKey,
            ConsistencyMode.Strong,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!remaining.IsEmpty)
        {
            return false;
        }

        await daprClient.DeleteStateAsync(
            StoreName,
            GetIndexKey(entry),
            StrongFirstWrite,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string GetIndexKey(AccessTelemetryExpiryEntry entry)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"expiry/{entry.ExpiryMinute:D12}/{entry.Shard:D2}/{entry.RecordId}");

    private static string GetRecordKey(string recordId)
        => $"records/{AccessTelemetryExpiryIndex.GetShard(recordId):D2}/{recordId}";
}
