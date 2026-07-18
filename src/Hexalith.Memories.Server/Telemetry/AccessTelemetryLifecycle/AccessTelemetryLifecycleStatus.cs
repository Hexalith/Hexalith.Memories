// <copyright file="AccessTelemetryLifecycleStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Process-local lifecycle-only health state that never gates business readiness.</summary>
internal sealed class AccessTelemetryLifecycleStatus
{
    private AccessTelemetryLifecycleStatusSnapshot _current;
    private int _terminal;

    /// <summary>Initializes lifecycle health for the configured enabled state.</summary>
    public AccessTelemetryLifecycleStatus(bool enabled)
    {
        _current = enabled
            ? new AccessTelemetryLifecycleStatusSnapshot(AccessTelemetryHealthState.Degraded, AccessTelemetryReason.RemoteValidationPending)
            : new AccessTelemetryLifecycleStatusSnapshot(AccessTelemetryHealthState.NoData, AccessTelemetryReason.None);
    }

    /// <summary>Gets the immutable current status.</summary>
    public AccessTelemetryLifecycleStatusSnapshot Current => Volatile.Read(ref _current);

    /// <summary>Publishes a bounded status without backend or identity details.</summary>
    public void Publish(AccessTelemetryHealthState health, AccessTelemetryReason reason)
    {
        if (Volatile.Read(ref _terminal) != 0)
        {
            return;
        }

        AccessTelemetryLifecycleStatusSnapshot current = Current;
        Volatile.Write(ref _current, new AccessTelemetryLifecycleStatusSnapshot(health, reason, current.LastAcceptedOrRejectedUtc));
    }

    /// <summary>Publishes a restart-scoped terminal state that cannot be overwritten by later background polls.</summary>
    public void PublishTerminal(AccessTelemetryReason reason)
    {
        Volatile.Write(ref _terminal, 1);
        AccessTelemetryLifecycleStatusSnapshot current = Current;
        Volatile.Write(
            ref _current,
            new AccessTelemetryLifecycleStatusSnapshot(
                AccessTelemetryHealthState.Unhealthy,
                reason,
                current.LastAcceptedOrRejectedUtc));
    }

    /// <summary>Records the most recent accepted or rejected lifecycle record time.</summary>
    public void RecordActivity(DateTimeOffset utcNow)
    {
        AccessTelemetryLifecycleStatusSnapshot current = Current;
        Volatile.Write(ref _current, current with { LastAcceptedOrRejectedUtc = utcNow });
    }
}
