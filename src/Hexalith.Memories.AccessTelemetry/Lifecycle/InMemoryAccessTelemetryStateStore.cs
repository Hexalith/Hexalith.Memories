// <copyright file="InMemoryAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>
/// Deterministic state adapter for lifecycle tests; not registered by the runtime host. It stands
/// in for <see cref="DaprAccessTelemetryStateStore"/> in the portable lifecycle checkpoints, so it
/// must model the same observable contract: all-or-nothing record/index/catalog writes, the
/// anti-resurrection conflict guard, minute-major purge order with empty-minute pruning, and strong
/// post-delete verification that can fail on both the record side
/// (<see cref="AccessTelemetryDeleteStatus.VerificationFailed"/>, forced by
/// <see cref="SuppressRecordDeletion"/>) and the stale-entry index side (an
/// <see cref="InvalidOperationException"/> mirroring
/// <c>DaprAccessTelemetryStateStore.RemoveBucketEntryAsync</c>, forced by
/// <see cref="SuppressEntryRemoval"/>).
/// </summary>
internal sealed class InMemoryAccessTelemetryStateStore : IAccessTelemetryStateStore
{
    private readonly Dictionary<string, AccessTelemetryRecord> _records = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AccessTelemetryExpiryEntry> _entries = new(StringComparer.Ordinal);
    private readonly HashSet<long> _activeMinutes = [];
    private readonly HashSet<string> _undeletableRecordIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _unremovableEntryKeys = new(StringComparer.Ordinal);
    private readonly List<int> _transactionOperationCounts = [];
    private readonly Lock _gate = new();
    private int _lastTtlInSeconds;

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

    /// <summary>Gets the retained active expiry-minute count, mirroring the Dapr expiry catalog.</summary>
    public int ActiveMinuteCount
    {
        get
        {
            lock (_gate)
            {
                return _activeMinutes.Count;
            }
        }
    }

    /// <summary>
    /// Gets the operation count of every committed atomic write, in completion order. A write that
    /// opens a new expiry minute commits three operations (record, bucket, catalog); a write into
    /// an already-active minute commits two, exactly as the Dapr adapter does.
    /// </summary>
    public IReadOnlyList<int> TransactionOperationCounts
    {
        get
        {
            lock (_gate)
            {
                return _transactionOperationCounts.ToArray();
            }
        }
    }

    /// <summary>Gets the operation count in the last atomic transaction, or zero if none committed.</summary>
    public int LastTransactionOperationCount
    {
        get
        {
            lock (_gate)
            {
                return _transactionOperationCounts.Count == 0
                    ? 0
                    : _transactionOperationCounts[^1];
            }
        }
    }

    /// <summary>Gets the time-to-live carried by the last committed write, or zero if none committed.</summary>
    public int LastTtlInSeconds
    {
        get
        {
            lock (_gate)
            {
                return _lastTtlInSeconds;
            }
        }
    }

    /// <summary>Reports whether a record remains retained.</summary>
    public bool ContainsRecord(string recordId)
    {
        lock (_gate)
        {
            return _records.ContainsKey(recordId);
        }
    }

    /// <summary>Gets one retained record for deterministic lifecycle verification.</summary>
    public AccessTelemetryRecord? GetRecord(string recordId)
    {
        lock (_gate)
        {
            return _records.GetValueOrDefault(recordId);
        }
    }

    /// <summary>
    /// Removes one record the way native component TTL reaping would, leaving its expiry-index
    /// entry and active minute behind. Models the orphaned-entry state the purge loop must resolve.
    /// A record that is not retained is a mistyped fixture, not a no-op reaping, so it throws for
    /// fail-fast parity with the sibling <c>TransactionalDaprState.ExpireByTtl</c> helper.
    /// </summary>
    public void ExpireByTtl(string recordId)
    {
        lock (_gate)
        {
            if (!_records.Remove(recordId))
            {
                throw new KeyNotFoundException(
                    $"{nameof(ExpireByTtl)} was given the record identifier '{recordId}', which this store does not retain. " +
                    $"Retained record identifiers: {string.Join(", ", _records.Keys.Order(StringComparer.Ordinal))}.");
            }
        }
    }

    /// <summary>
    /// Makes the backend acknowledge a record deletion it never applies, so the strong re-read still
    /// observes the record. Models the durability defect
    /// <see cref="AccessTelemetryDeleteStatus.VerificationFailed"/> exists to catch.
    /// </summary>
    public void SuppressRecordDeletion(string recordId)
    {
        lock (_gate)
        {
            _ = _undeletableRecordIds.Add(recordId);
        }
    }

    /// <summary>
    /// Makes the backend acknowledge a stale expiry-entry prune it never applies, so the strong
    /// re-read still observes the entry. Models the index-side durability defect that
    /// <c>DaprAccessTelemetryStateStore.RemoveBucketEntryAsync</c> throws on.
    /// </summary>
    public void SuppressEntryRemoval(AccessTelemetryExpiryEntry entry)
    {
        lock (_gate)
        {
            _ = _unremovableEntryKeys.Add(GetEntryKey(entry));
        }
    }

    /// <summary>
    /// Writes one expiry entry directly, bypassing the write path, so a test can construct index
    /// state no single adapter call produces — a bucket still holding a superseded index generation
    /// alongside the live entry.
    /// </summary>
    public void Seed(AccessTelemetryExpiryEntry entry)
    {
        lock (_gate)
        {
            _entries[GetEntryKey(entry)] = entry;
            _ = _activeMinutes.Add(entry.ExpiryMinute);
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
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ttlInSeconds, 0);
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

            // Anti-resurrection guard, mirroring the Dapr bucket check: an expiry entry already
            // occupying this record's (minute, shard) slot is a conflict, never a silent overwrite.
            // Decided before any mutation so a rejected write leaves no partial state behind.
            if (_entries.Values.Any(candidate =>
                candidate.ExpiryMinute == expiryEntry.ExpiryMinute &&
                candidate.Shard == expiryEntry.Shard &&
                string.Equals(candidate.RecordId, expiryEntry.RecordId, StringComparison.Ordinal)))
            {
                return Task.FromResult(AccessTelemetryStoreWriteStatus.Conflict);
            }

            bool addCatalogMinute = _activeMinutes.Add(expiryEntry.ExpiryMinute);
            _records.Add(record.RecordId, record);
            _entries.Add(GetEntryKey(expiryEntry), expiryEntry);
            _lastTtlInSeconds = ttlInSeconds;
            _transactionOperationCounts.Add(addCatalogMinute ? 3 : 2);
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
        if (limit <= 0)
        {
            return Task.FromResult<IReadOnlyList<AccessTelemetryExpiryEntry>>([]);
        }

        lock (_gate)
        {
            // Purge order is observable behaviour, so this must match DaprAccessTelemetryStateStore
            // exactly: minute-major traversal, then ExpiresAtUtc, Shard, and RecordId within a
            // minute, stopping at the limit without visiting - or pruning - later minutes.
            var due = new List<AccessTelemetryExpiryEntry>(limit);
            var emptyMinutes = new List<long>();
            foreach (long minute in _activeMinutes.Where(minute => minute <= dueMinute).Order())
            {
                AccessTelemetryExpiryEntry[] minuteEntries = _entries.Values
                    .Where(entry => entry.ExpiryMinute == minute)
                    .ToArray();
                if (minuteEntries.Length == 0)
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

            foreach (long minute in emptyMinutes)
            {
                _ = _activeMinutes.Remove(minute);
            }

            return Task.FromResult<IReadOnlyList<AccessTelemetryExpiryEntry>>(due);
        }
    }

    /// <inheritdoc/>
    public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(AccessTelemetryExpiryEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            string entryKey = GetEntryKey(entry);
            if (_records.TryGetValue(entry.RecordId, out AccessTelemetryRecord? record) &&
                (!string.Equals(record.EnvelopeHash, entry.EnvelopeHash, StringComparison.Ordinal) ||
                    !string.Equals(record.ExpiresAtUtc, entry.ExpiresAtUtc, StringComparison.Ordinal)))
            {
                // Prune only this exact stale entry. The live record's own entry can share the same
                // (minute, shard) slot, and removing it would leave the record unpurgeable forever.
                // The Dapr adapter prunes only when the bucket actually holds the entry, so guard
                // on presence before both the removal and its verification.
                bool entryPresent = _entries.ContainsKey(entryKey);
                if (entryPresent && !_unremovableEntryKeys.Contains(entryKey))
                {
                    _ = _entries.Remove(entryKey);
                }

                // Strong post-prune verification, exactly as RemoveBucketEntryAsync performs it.
                if (entryPresent && _entries.ContainsKey(entryKey))
                {
                    throw new InvalidOperationException(
                        "The stale lifecycle expiry bucket entry could not be strongly verified absent.");
                }

                return Task.FromResult(AccessTelemetryDeleteStatus.StaleIndex);
            }

            bool existed = record is not null;
            if (existed && !_undeletableRecordIds.Contains(entry.RecordId))
            {
                _ = _records.Remove(entry.RecordId);
            }

            _ = _entries.Remove(entryKey);

            // Strong post-delete verification, exactly as the Dapr adapter performs it.
            if (_records.ContainsKey(entry.RecordId) || _entries.ContainsKey(entryKey))
            {
                return Task.FromResult(AccessTelemetryDeleteStatus.VerificationFailed);
            }

            return Task.FromResult(existed
                ? AccessTelemetryDeleteStatus.Deleted
                : AccessTelemetryDeleteStatus.AlreadyAbsent);
        }
    }

    private static string GetEntryKey(AccessTelemetryExpiryEntry entry)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{entry.ExpiryMinute:D12}/{entry.Shard:D2}/{entry.RecordId}/{entry.EnvelopeHash}/{entry.ExpiresAtUtc}");
}
