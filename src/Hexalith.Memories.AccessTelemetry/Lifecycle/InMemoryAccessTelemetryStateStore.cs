// <copyright file="InMemoryAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Deterministic state adapter for lifecycle tests; not registered by the runtime host.</summary>
internal sealed class InMemoryAccessTelemetryStateStore : IAccessTelemetryStateStore
{
    private readonly Dictionary<string, AccessTelemetryRecord> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AccessTelemetryExpiryEntry> _entries = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <summary>Gets the retained record count.</summary>
    public int RecordCount
    {
        get
        {
            lock (_gate)
            {
                return _records.Count;
            }
        }
    }

    /// <summary>Gets the retained index-entry count.</summary>
    public int IndexCount
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>Gets the operation count in the last atomic transaction.</summary>
    public int LastTransactionOperationCount { get; private set; }

    /// <summary>Reports whether a record remains retained.</summary>
    public bool ContainsRecord(string recordId)
    {
        lock (_gate)
        {
            return _records.ContainsKey(recordId);
        }
    }

    /// <inheritdoc/>
    public Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_records.TryGetValue(record.RecordId, out AccessTelemetryRecord? existing))
            {
                AccessTelemetryStoreWriteStatus existingStatus =
                    string.Equals(existing.EnvelopeHash, record.EnvelopeHash, StringComparison.Ordinal) &&
                    string.Equals(existing.ExpiresAtUtc, record.ExpiresAtUtc, StringComparison.Ordinal)
                        ? AccessTelemetryStoreWriteStatus.Idempotent
                        : AccessTelemetryStoreWriteStatus.Conflict;
                return Task.FromResult(existingStatus);
            }

            _records.Add(record.RecordId, record);
            _entries.Add(GetEntryKey(expiryEntry), expiryEntry);
            LastTransactionOperationCount = 2;
            return Task.FromResult(AccessTelemetryStoreWriteStatus.Inserted);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccessTelemetryExpiryEntry>> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<AccessTelemetryExpiryEntry> entries = _entries.Values
                .Where(entry => entry.ExpiryMinute <= dueMinute)
                .OrderBy(static entry => entry.ExpiryMinute)
                .ThenBy(static entry => entry.Shard)
                .ThenBy(static entry => entry.RecordId, StringComparer.Ordinal)
                .Take(limit)
                .ToArray();
            return Task.FromResult(entries);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAndVerifyAsync(AccessTelemetryExpiryEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _ = _records.Remove(entry.RecordId);
            bool absent = !_records.ContainsKey(entry.RecordId);
            if (absent)
            {
                _ = _entries.Remove(GetEntryKey(entry));
            }

            return Task.FromResult(absent);
        }
    }

    private static string GetEntryKey(AccessTelemetryExpiryEntry entry)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{entry.ExpiryMinute:D12}/{entry.Shard:D2}/{entry.RecordId}");
}
