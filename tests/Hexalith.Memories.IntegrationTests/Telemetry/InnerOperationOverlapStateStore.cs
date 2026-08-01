// <copyright file="InnerOperationOverlapStateStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

/// <summary>Test-only inner store that gates one due read and one write after both operations have entered.</summary>
internal sealed class InnerOperationOverlapStateStore(IAccessTelemetryStateStore inner) : IAccessTelemetryStateStore
{
    private readonly TaskCompletionSource _dueReadEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _writeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _armed;
    private int _dueReadClaimed;
    private int _operationCompleted;
    private int _overlapObserved;
    private int _writeClaimed;

    /// <summary>Gets whether the gated inner due-read operation entered.</summary>
    public bool DueReadEntered => _dueReadEntered.Task.IsCompletedSuccessfully;

    /// <summary>Gets whether the two gated inner operations entered before either completed.</summary>
    public bool OverlapObserved => Volatile.Read(ref _overlapObserved) != 0;

    /// <summary>Gets whether the gated inner write operation entered.</summary>
    public bool WriteEntered => _writeEntered.Task.IsCompletedSuccessfully;

    /// <summary>Arms the one-shot inner due-read/write operation gate after fixture seeding is complete.</summary>
    public void ArmPurgeWriteRendezvous()
    {
        if (Interlocked.Exchange(ref _armed, 1) != 0)
        {
            throw new InvalidOperationException("The inner-operation rendezvous may be armed only once.");
        }
    }

    /// <inheritdoc/>
    public async Task<AccessTelemetryStoreWriteStatus> WriteRecordAndIndexAsync(
        AccessTelemetryRecord record,
        AccessTelemetryExpiryEntry expiryEntry,
        int ttlInSeconds,
        CancellationToken cancellationToken)
    {
        bool participates = Volatile.Read(ref _armed) != 0 &&
            Interlocked.CompareExchange(ref _writeClaimed, 1, 0) == 0;
        if (participates)
        {
            _writeEntered.TrySetResult();
            await _dueReadEntered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            RecordOverlapBeforeCompletion();
        }

        try
        {
            return await inner.WriteRecordAndIndexAsync(
                record,
                expiryEntry,
                ttlInSeconds,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (participates)
            {
                Interlocked.Increment(ref _operationCompleted);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyList<AccessTelemetryExpiryEntry> Entries, bool HasMoreDueEntries)> GetDueEntriesAsync(
        long dueMinute,
        int limit,
        CancellationToken cancellationToken)
    {
        bool participates = Volatile.Read(ref _armed) != 0 &&
            Interlocked.CompareExchange(ref _dueReadClaimed, 1, 0) == 0;
        if (participates)
        {
            _dueReadEntered.TrySetResult();
            await _writeEntered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            RecordOverlapBeforeCompletion();
        }

        try
        {
            return await inner.GetDueEntriesAsync(dueMinute, limit, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (participates)
            {
                Interlocked.Increment(ref _operationCompleted);
            }
        }
    }

    /// <inheritdoc/>
    public Task<AccessTelemetryDeleteStatus> DeleteAndVerifyAsync(
        AccessTelemetryExpiryEntry entry,
        CancellationToken cancellationToken)
        => inner.DeleteAndVerifyAsync(entry, cancellationToken);

    private void RecordOverlapBeforeCompletion()
    {
        if (_dueReadEntered.Task.IsCompletedSuccessfully &&
            _writeEntered.Task.IsCompletedSuccessfully &&
            Volatile.Read(ref _operationCompleted) == 0)
        {
            Volatile.Write(ref _overlapObserved, 1);
        }
    }
}
