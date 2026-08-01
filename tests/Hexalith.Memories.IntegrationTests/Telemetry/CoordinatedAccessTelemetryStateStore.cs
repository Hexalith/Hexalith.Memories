// <copyright file="CoordinatedAccessTelemetryStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Test-only state-store decorator that records committed writes and coordinates concurrent writers.</summary>
internal sealed class CoordinatedAccessTelemetryStateStore(IAccessTelemetryStateStore inner) : IAccessTelemetryStateStore
{
    private readonly List<AccessTelemetryExpiryEntry> _committedExpiryEntries = [];
    private readonly List<AccessTelemetryRecord> _committedRecords = [];
    private readonly Lock _committedWritesGate = new();
    private AccessTelemetryOperationRendezvous? _concurrentWriteRendezvous;

    /// <summary>Gets snapshots of every record accepted by the inner store.</summary>
    public IReadOnlyList<AccessTelemetryRecord> CommittedRecords
    {
        get
        {
            lock (_committedWritesGate)
            {
                return _committedRecords.ToArray();
            }
        }
    }

    /// <summary>Gets snapshots of every expiry entry accepted by the inner store.</summary>
    public IReadOnlyList<AccessTelemetryExpiryEntry> CommittedExpiryEntries
    {
        get
        {
            lock (_committedWritesGate)
            {
                return _committedExpiryEntries.ToArray();
            }
        }
    }

    /// <summary>Gets whether two independent writers reached the shared write seam together.</summary>
    public bool ConcurrentWriteOverlapObserved =>
        Volatile.Read(ref _concurrentWriteRendezvous)?.OverlapObserved is true;

    /// <summary>Arms a two-writer rendezvous at the shared state-store write seam.</summary>
    public void ArmConcurrentWriteRendezvous()
    {
        var rendezvous = new AccessTelemetryOperationRendezvous(participantCount: 2);
        if (Interlocked.CompareExchange(ref _concurrentWriteRendezvous, rendezvous, null) is not null)
        {
            throw new InvalidOperationException("The concurrent-write rendezvous may be armed only once.");
        }
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
    {
        AccessTelemetryOperationRendezvous? concurrentWrites = Volatile.Read(ref _concurrentWriteRendezvous);
        if (concurrentWrites is not null)
        {
            await concurrentWrites.EnterAsync(cancellationToken).ConfigureAwait(false);
        }

        AccessTelemetryStoreWriteStatus status = await inner.WriteRecordAndIndexAsync(
            record,
            expiryEntry,
            ttlInSeconds,
            cancellationToken).ConfigureAwait(false);
        if (status is AccessTelemetryStoreWriteStatus.Inserted or AccessTelemetryStoreWriteStatus.Idempotent)
        {
            lock (_committedWritesGate)
            {
                _committedRecords.Add(record);
                _committedExpiryEntries.Add(expiryEntry);
            }
        }

        return status;
    }

    /// <inheritdoc/>
    public Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
        => inner.GetDueEntriesAsync(dueMinute, limit, cancellationToken);

    /// <inheritdoc/>
    public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
        => inner.DeleteAndVerifyAsync(entry, cancellationToken);
}
