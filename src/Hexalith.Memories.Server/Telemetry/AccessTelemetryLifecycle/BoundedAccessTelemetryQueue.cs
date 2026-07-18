// <copyright file="BoundedAccessTelemetryQueue.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Nonblocking, process-local queue bounded by both record count and canonical bytes.</summary>
internal sealed class BoundedAccessTelemetryQueue
{
    private readonly Queue<AccessTelemetryQueuedRecord> _records = [];
    private readonly Lock _gate = new();
    private readonly int _recordLimit;
    private readonly int _byteLimit;
    private int _byteCount;

    /// <summary>Initializes a queue with exact inclusive limits.</summary>
    public BoundedAccessTelemetryQueue(int recordLimit, int byteLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(recordLimit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLimit, 1);
        _recordLimit = recordLimit;
        _byteLimit = byteLimit;
    }

    /// <summary>Gets the queued record count.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _records.Count;
            }
        }
    }

    /// <summary>Gets the queued canonical byte count.</summary>
    public int ByteCount
    {
        get
        {
            lock (_gate)
            {
                return _byteCount;
            }
        }
    }

    /// <summary>Attempts to enqueue without waiting and drops the new record at either bound.</summary>
    public bool TryEnqueue(AccessTelemetryRecord record, out AccessTelemetryReason reason)
    {
        ArgumentNullException.ThrowIfNull(record);
        byte[] canonical = AccessTelemetryCanonicalizer.CanonicalizeRecord(record);
        lock (_gate)
        {
            if (_records.Count >= _recordLimit || canonical.Length > _byteLimit - _byteCount)
            {
                reason = AccessTelemetryReason.QueueFull;
                return false;
            }

            _records.Enqueue(new AccessTelemetryQueuedRecord(record, canonical.Length));
            _byteCount += canonical.Length;
            reason = AccessTelemetryReason.None;
            return true;
        }
    }

    /// <summary>Peeks a bounded FIFO batch without removing records before acknowledgement.</summary>
    public IReadOnlyList<AccessTelemetryRecord> PeekBatch(int recordLimit, int byteLimit)
    {
        lock (_gate)
        {
            List<AccessTelemetryRecord> batch = [];
            int bytes = 0;
            foreach (AccessTelemetryQueuedRecord queued in _records)
            {
                if (batch.Count >= recordLimit || queued.CanonicalBytes > byteLimit - bytes)
                {
                    break;
                }

                batch.Add(queued.Record);
                bytes += queued.CanonicalBytes;
            }

            return batch;
        }
    }

    /// <summary>Removes exactly the acknowledged FIFO prefix.</summary>
    public void Acknowledge(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        lock (_gate)
        {
            if (count > _records.Count)
            {
                throw new InvalidOperationException("Cannot acknowledge more lifecycle records than are queued.");
            }

            for (int index = 0; index < count; index++)
            {
                _byteCount -= _records.Dequeue().CanonicalBytes;
            }
        }
    }
}
