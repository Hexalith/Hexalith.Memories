// <copyright file="AccessTelemetryProcessorStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Process-local bounded processor health shared with readiness reporting.</summary>
internal sealed class AccessTelemetryProcessorStatus
{
    private Snapshot _current = new(AccessTelemetryHealthState.Healthy, AccessTelemetryReason.None, null);

    /// <summary>Gets the latest bounded status.</summary>
    public Snapshot Current => Volatile.Read(ref _current);

    /// <summary>Publishes one processor outcome.</summary>
    public void Publish(AccessTelemetryHealthState health, AccessTelemetryReason reason, DateTimeOffset? activityUtc = null)
    {
        Snapshot current = Current;
        Volatile.Write(ref _current, new Snapshot(health, reason, activityUtc ?? current.LastAcceptedOrRejectedUtc));
    }

    /// <summary>Bounded immutable processor status.</summary>
    internal sealed record Snapshot(
        AccessTelemetryHealthState Health,
        AccessTelemetryReason Reason,
        DateTimeOffset? LastAcceptedOrRejectedUtc);
}
