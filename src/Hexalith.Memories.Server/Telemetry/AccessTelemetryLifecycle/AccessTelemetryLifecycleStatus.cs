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
        => Volatile.Write(ref _current, new AccessTelemetryLifecycleStatusSnapshot(health, reason));
}
