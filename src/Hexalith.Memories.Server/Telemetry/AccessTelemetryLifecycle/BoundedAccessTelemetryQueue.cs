// <copyright file="BoundedAccessTelemetryQueue.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Collections.Concurrent;
using System.Globalization;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Nonblocking, process-local queue bounded by both record count and canonical bytes.</summary>
internal sealed class BoundedAccessTelemetryQueue
{
    private const long RecordReservation = 1L << 32;
    private readonly ConcurrentQueue<AccessTelemetryQueuedRecord> _incoming = new();
    private readonly Queue<AccessTelemetryQueuedRecord> _pending = [];
    private readonly ConcurrentDictionary<string, int> _markerCounts = new(StringComparer.Ordinal);
    private readonly object _consumerGate = new();
    private readonly int _recordLimit;
    private readonly int _byteLimit;
    // One CAS reserves both bounds: upper 32 bits are records, lower 32 bits are bytes.
    private long _reservedCapacity;
    private AccessTelemetryQueuedRecord? _pendingHead;

    /// <summary>Initializes a queue with exact inclusive limits.</summary>
    public BoundedAccessTelemetryQueue(int recordLimit, int byteLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(recordLimit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLimit, 1);
        _recordLimit = recordLimit;
        _byteLimit = byteLimit;
    }

    /// <summary>Gets the reserved record count, including pending delivery and publication.</summary>
    public int Count => (int)(Volatile.Read(ref _reservedCapacity) >> 32);

    /// <summary>Gets the reserved canonical byte count, including pending delivery and publication.</summary>
    public int ByteCount => (int)(Volatile.Read(ref _reservedCapacity) & uint.MaxValue);

    /// <summary>Gets the FIFO head emission time without acquiring the consumer gate.</summary>
    public DateTimeOffset? OldestEmittedAtUtc
    {
        get
        {
            AccessTelemetryQueuedRecord? oldest = Volatile.Read(ref _pendingHead);
            if (oldest is null)
            {
                _incoming.TryPeek(out oldest);
                // A consumer may have moved that incoming head since the first read.
                oldest = Volatile.Read(ref _pendingHead) ?? oldest;
            }

            return oldest is null ? null : ParseEmittedAt(oldest.Record);
        }
    }

    /// <summary>Gets the non-negative age of the oldest queued record without exposing its identity.</summary>
    public double GetOldestAgeSeconds(DateTimeOffset observedAt)
        => OldestEmittedAtUtc is DateTimeOffset emittedAt
            ? Math.Max(0, (observedAt - emittedAt).TotalSeconds)
            : 0;

    /// <summary>Attempts admission without waiting for consumer work, backend I/O, or capacity.</summary>
    /// <param name="record">The sanitized record.</param>
    /// <param name="reason">The bounded admission result.</param>
    /// <param name="onAdmitted">Optional admission accounting, completed before the record becomes deliverable.</param>
    public bool TryEnqueue(AccessTelemetryRecord record, out AccessTelemetryReason reason, Action? onAdmitted = null)
    {
        ArgumentNullException.ThrowIfNull(record);
        int bytes = AccessTelemetryCanonicalizer.CanonicalizeRecord(record).Length;
        var queued = new AccessTelemetryQueuedRecord(record, bytes);
        // Register ownership before reserving capacity so heartbeat drain checks cannot
        // overlook an accepted record whose admission accounting has not yet returned.
        _markerCounts.AddOrUpdate(record.MarkerKeyId, 1, static (_, count) => count + 1);
        long capacity = Volatile.Read(ref _reservedCapacity);
        while (true)
        {
            if ((capacity >> 32) >= _recordLimit || bytes > _byteLimit - (capacity & uint.MaxValue))
            {
                ReleaseMarker(record.MarkerKeyId);
                reason = AccessTelemetryReason.QueueFull;
                return false;
            }

            long updated = capacity + RecordReservation + bytes;
            long observed = Interlocked.CompareExchange(ref _reservedCapacity, updated, capacity);
            if (observed == capacity)
            {
                break;
            }

            capacity = observed;
        }

        try
        {
            onAdmitted?.Invoke();
            _incoming.Enqueue(queued);
        }
        catch
        {
            Release(queued);
            throw;
        }

        reason = AccessTelemetryReason.None;
        return true;
    }

    /// <summary>Removes matching records while preserving the FIFO order of all survivors.</summary>
    public int RemoveWhere(
        Func<AccessTelemetryRecord, bool> predicate,
        Action<AccessTelemetryRecord>? removedRecord = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        lock (_consumerGate)
        {
            CollectIncoming();
            int removed = 0;
            int originalCount = _pending.Count;
            for (int index = 0; index < originalCount; index++)
            {
                AccessTelemetryQueuedRecord queued = _pending.Peek();
                bool remove = predicate(queued.Record);
                _pending.Dequeue();
                if (remove)
                {
                    Release(queued);
                    removed++;
                    removedRecord?.Invoke(queued.Record);
                }
                else
                {
                    _pending.Enqueue(queued);
                }
            }

            RefreshPendingHead();
            return removed;
        }
    }

    /// <summary>Conservatively counts marker-key ownership through admission, delivery, and release.</summary>
    public int CountByMarkerKey(string markerKeyId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markerKeyId);
        // Attempts register before capacity admission; cap this conservative observation
        // to the queue limit accepted by the heartbeat protocol.
        return _markerCounts.TryGetValue(markerKeyId, out int count) ? Math.Min(count, _recordLimit) : 0;
    }

    /// <summary>Peeks a bounded FIFO batch; one delivery consumer owns it through acknowledgement.</summary>
    public IReadOnlyList<AccessTelemetryRecord> PeekBatch(int recordLimit, int byteLimit)
    {
        lock (_consumerGate)
        {
            CollectIncoming();
            List<AccessTelemetryRecord> batch = [];
            int bytes = 0;
            foreach (AccessTelemetryQueuedRecord queued in _pending)
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

    /// <summary>Removes exactly the acknowledged FIFO prefix and releases its reservations once.</summary>
    public void Acknowledge(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        lock (_consumerGate)
        {
            CollectIncoming();
            if (count > _pending.Count)
            {
                throw new InvalidOperationException("Cannot acknowledge more lifecycle records than are queued.");
            }

            for (int index = 0; index < count; index++)
            {
                Release(_pending.Dequeue());
            }

            RefreshPendingHead();
        }
    }

    private static DateTimeOffset ParseEmittedAt(AccessTelemetryRecord record)
        => DateTimeOffset.ParseExact(
            record.EmittedAtUtc,
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private void CollectIncoming()
    {
        // Bound each consumer pass even while producers continue publishing.
        int available = _incoming.Count;
        if (_pending.Count == 0 && _incoming.TryPeek(out AccessTelemetryQueuedRecord? head))
        {
            // Publish the head before dequeueing it, preserving metric visibility during transfer.
            Volatile.Write(ref _pendingHead, head);
        }

        for (int index = 0; index < available && _incoming.TryDequeue(out AccessTelemetryQueuedRecord? queued); index++)
        {
            _pending.Enqueue(queued);
        }

        RefreshPendingHead();
    }

    private void RefreshPendingHead()
        => Volatile.Write(ref _pendingHead, _pending.TryPeek(out AccessTelemetryQueuedRecord? head) ? head : null);

    private void Release(AccessTelemetryQueuedRecord queued)
    {
        Interlocked.Add(ref _reservedCapacity, -RecordReservation - queued.CanonicalBytes);
        ReleaseMarker(queued.Record.MarkerKeyId);
    }

    private void ReleaseMarker(string markerKeyId)
    {
        if (_markerCounts.AddOrUpdate(markerKeyId, 0, static (_, count) => count - 1) == 0)
        {
            // Remove only the zero value; a concurrent admission may already own this key again.
            ((ICollection<KeyValuePair<string, int>>)_markerCounts).Remove(new(markerKeyId, 0));
        }
    }
}
