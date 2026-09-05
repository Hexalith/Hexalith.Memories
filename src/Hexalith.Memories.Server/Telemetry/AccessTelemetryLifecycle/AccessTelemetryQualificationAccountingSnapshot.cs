// <copyright file="AccessTelemetryQualificationAccountingSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

/// <summary>Represents process-local fixed-workload accounting without record identity.</summary>
/// <param name="Attempted">The number of typed access events observed by the lifecycle provider.</param>
/// <param name="Enqueued">The number of records accepted by the bounded queue.</param>
/// <param name="Persisted">The number of records acknowledged by the lifecycle service.</param>
/// <param name="Rejected">The number of records rejected before or during lifecycle delivery.</param>
/// <param name="Dropped">The number of records dropped at a bounded queue or retry limit.</param>
/// <param name="Conflicted">The number of records rejected because an existing record ID differed.</param>
internal sealed record AccessTelemetryQualificationAccountingSnapshot(
    long Attempted,
    long Enqueued,
    long Persisted,
    long Rejected,
    long Dropped,
    long Conflicted)
{
    /// <summary>Subtracts an earlier monotonic snapshot from this snapshot.</summary>
    /// <param name="earlier">The earlier process-local snapshot.</param>
    /// <returns>The non-negative counter deltas.</returns>
    public AccessTelemetryQualificationAccountingSnapshot Since(
        AccessTelemetryQualificationAccountingSnapshot earlier)
    {
        ArgumentNullException.ThrowIfNull(earlier);
        return new(
            Math.Max(0, Attempted - earlier.Attempted),
            Math.Max(0, Enqueued - earlier.Enqueued),
            Math.Max(0, Persisted - earlier.Persisted),
            Math.Max(0, Rejected - earlier.Rejected),
            Math.Max(0, Dropped - earlier.Dropped),
            Math.Max(0, Conflicted - earlier.Conflicted));
    }
}
