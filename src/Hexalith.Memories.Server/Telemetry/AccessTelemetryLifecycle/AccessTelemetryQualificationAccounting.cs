// <copyright file="AccessTelemetryQualificationAccounting.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Collections.Concurrent;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.Contracts.V1;

/// <summary>Maintains aggregate accounting for the qualification-only fixed workload.</summary>
internal sealed class AccessTelemetryQualificationAccounting
{
    private const int CorrelationLength = 46;
    private readonly ConcurrentDictionary<string, Counters> _correlated = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _recordCorrelations = new(StringComparer.Ordinal);
    private long _attempted;
    private long _conflicted;
    private long _dropped;
    private long _enqueued;
    private long _persisted;
    private long _rejected;

    /// <summary>Gets one atomic-enough monotonic counter snapshot.</summary>
    public AccessTelemetryQualificationAccountingSnapshot Current
        => new(
            Volatile.Read(ref _attempted),
            Volatile.Read(ref _enqueued),
            Volatile.Read(ref _persisted),
            Volatile.Read(ref _rejected),
            Volatile.Read(ref _dropped),
            Volatile.Read(ref _conflicted));

    /// <summary>Gets the counters belonging only to one fixed-workload segment.</summary>
    /// <param name="correlation">The privacy-safe segment correlation.</param>
    /// <returns>The segment's monotonic counter snapshot.</returns>
    public AccessTelemetryQualificationAccountingSnapshot ForCorrelation(string correlation)
        => _correlated.TryGetValue(correlation, out Counters? counters)
            ? counters.Current
            : new(0, 0, 0, 0, 0, 0);

    /// <summary>Records one attempted typed lifecycle event.</summary>
    public void RecordAttempted() => Interlocked.Increment(ref _attempted);

    /// <summary>Records an attempt and attributes a qualification record when present.</summary>
    /// <param name="source">The typed source event.</param>
    /// <returns>The bounded correlation, or <see langword="null"/> for ordinary traffic.</returns>
    public string? RecordAttempted(AccessTelemetryEvent source)
    {
        RecordAttempted();
        string? correlation = TryGetCorrelation(source);
        if (correlation is not null)
        {
            _correlated.GetOrAdd(correlation, static _ => new()).RecordAttempted();
        }

        return correlation;
    }

    /// <summary>Records one successfully enqueued lifecycle record.</summary>
    public void RecordEnqueued() => Interlocked.Increment(ref _enqueued);

    /// <summary>Records and tracks one correlated queued record.</summary>
    public void RecordEnqueued(AccessTelemetryRecord record, string? correlation)
    {
        RecordEnqueued();
        if (correlation is not null)
        {
            _correlated.GetOrAdd(correlation, static _ => new()).RecordEnqueued();
            _recordCorrelations[record.RecordId] = correlation;
        }
    }

    /// <summary>Records acknowledged persistence.</summary>
    /// <param name="count">The acknowledged record count.</param>
    public void RecordPersisted(long count) => AddNonNegative(ref _persisted, count);

    /// <summary>Records persistence for an exact response prefix.</summary>
    public void RecordPersisted(IReadOnlyList<AccessTelemetryRecord> records, int offset, int count)
    {
        RecordPersisted(count);
        RecordDisposition(records, offset, count, static counters => counters.RecordPersisted());
    }

    /// <summary>Records lifecycle rejection.</summary>
    /// <param name="count">The rejected record count.</param>
    /// <param name="conflicted">Whether the rejection was a record-ID conflict.</param>
    public void RecordRejected(long count, bool conflicted = false)
    {
        AddNonNegative(ref _rejected, count);
        if (conflicted)
        {
            AddNonNegative(ref _conflicted, count);
        }
    }

    /// <summary>Records rejection for an exact response range.</summary>
    public void RecordRejected(
        IReadOnlyList<AccessTelemetryRecord> records,
        int offset,
        int count,
        bool conflicted = false)
    {
        RecordRejected(count, conflicted);
        RecordDisposition(
            records,
            offset,
            count,
            counters => counters.RecordRejected(conflicted));
    }

    /// <summary>Records bounded loss before persistence.</summary>
    /// <param name="count">The dropped record count.</param>
    public void RecordDropped(long count = 1) => AddNonNegative(ref _dropped, count);

    /// <summary>Records a pre-queue correlated drop.</summary>
    public void RecordDropped(string? correlation)
    {
        RecordDropped();
        if (correlation is not null)
        {
            _correlated.GetOrAdd(correlation, static _ => new()).RecordDropped();
        }
    }

    /// <summary>Records records removed from the queue before delivery.</summary>
    public void RecordDropped(IReadOnlyList<AccessTelemetryRecord> records)
    {
        RecordDropped(records.Count);
        RecordDisposition(records, 0, records.Count, static counters => counters.RecordDropped());
    }

    /// <summary>Records a pre-queue correlated rejection.</summary>
    public void RecordRejected(string? correlation)
    {
        RecordRejected(1);
        if (correlation is not null)
        {
            _correlated.GetOrAdd(correlation, static _ => new()).RecordRejected(conflicted: false);
        }
    }

    private static string? TryGetCorrelation(AccessTelemetryEvent source)
    {
        if (!string.Equals(source.TenantId, "qualification-tenant", StringComparison.Ordinal) ||
            !source.QueryParams.TryGetValue("workflowInstanceIdPrefix", out object? value) ||
            value is not string marker || marker.Length < CorrelationLength + 4 ||
            !marker.StartsWith("qualification-", StringComparison.Ordinal) || marker[CorrelationLength] != '-')
        {
            return null;
        }

        ReadOnlySpan<char> hash = marker.AsSpan("qualification-".Length, 32);
        ReadOnlySpan<char> ordinal = marker.AsSpan(CorrelationLength + 1);
        return hash.IndexOfAnyExcept("0123456789abcdef") < 0 &&
            ordinal.Length == 3 && ordinal.IndexOfAnyExcept("0123456789") < 0
            ? marker[..CorrelationLength]
            : null;
    }

    private void RecordDisposition(
        IReadOnlyList<AccessTelemetryRecord> records,
        int offset,
        int count,
        Action<Counters> disposition)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(disposition);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset > records.Count - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (int index = offset; index < offset + count; index++)
        {
            if (_recordCorrelations.TryRemove(records[index].RecordId, out string? correlation))
            {
                disposition(_correlated.GetOrAdd(correlation, static _ => new()));
            }
        }
    }

    private static void AddNonNegative(ref long target, long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _ = Interlocked.Add(ref target, count);
    }

    private sealed class Counters
    {
        private long _attempted;
        private long _conflicted;
        private long _dropped;
        private long _enqueued;
        private long _persisted;
        private long _rejected;

        public AccessTelemetryQualificationAccountingSnapshot Current => new(
            Volatile.Read(ref _attempted),
            Volatile.Read(ref _enqueued),
            Volatile.Read(ref _persisted),
            Volatile.Read(ref _rejected),
            Volatile.Read(ref _dropped),
            Volatile.Read(ref _conflicted));

        public void RecordAttempted() => Interlocked.Increment(ref _attempted);

        public void RecordEnqueued() => Interlocked.Increment(ref _enqueued);

        public void RecordPersisted() => Interlocked.Increment(ref _persisted);

        public void RecordRejected(bool conflicted)
        {
            Interlocked.Increment(ref _rejected);
            if (conflicted)
            {
                Interlocked.Increment(ref _conflicted);
            }
        }

        public void RecordDropped() => Interlocked.Increment(ref _dropped);
    }
}
