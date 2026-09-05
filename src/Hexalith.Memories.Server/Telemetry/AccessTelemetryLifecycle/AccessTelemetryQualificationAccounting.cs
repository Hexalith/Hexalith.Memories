// <copyright file="AccessTelemetryQualificationAccounting.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Maintains aggregate accounting for the qualification-only fixed workload.</summary>
internal sealed class AccessTelemetryQualificationAccounting
{
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

    /// <summary>Records one attempted typed lifecycle event.</summary>
    public void RecordAttempted() => Interlocked.Increment(ref _attempted);

    /// <summary>Records one successfully enqueued lifecycle record.</summary>
    public void RecordEnqueued() => Interlocked.Increment(ref _enqueued);

    /// <summary>Records acknowledged persistence.</summary>
    /// <param name="count">The acknowledged record count.</param>
    public void RecordPersisted(long count) => AddNonNegative(ref _persisted, count);

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

    /// <summary>Records bounded loss before persistence.</summary>
    /// <param name="count">The dropped record count.</param>
    public void RecordDropped(long count = 1) => AddNonNegative(ref _dropped, count);

    private static void AddNonNegative(ref long target, long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _ = Interlocked.Add(ref target, count);
    }
}
