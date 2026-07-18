// <copyright file="AccessTelemetryRuntimeGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Restart-scoped terminal runtime gate updated only by the capability probe runner.</summary>
internal sealed class AccessTelemetryRuntimeGate : IAccessTelemetryRuntimeGate
{
    private readonly TimeProvider _timeProvider;
    private AccessTelemetryCapabilityGateResult _current = new(
        false,
        true,
        AccessTelemetryHealthState.Unhealthy,
        AccessTelemetryReason.CapabilityUnproven);

    /// <summary>Initializes a fail-closed gate using the supplied clock.</summary>
    public AccessTelemetryRuntimeGate(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public AccessTelemetryCapabilityGateResult Current
    {
        get
        {
            AccessTelemetryCapabilityGateResult current = Volatile.Read(ref _current);
            return current.AllowsWrites && current.ValidUntilUtc <= _timeProvider.GetUtcNow()
                ? new AccessTelemetryCapabilityGateResult(
                    false,
                    true,
                    AccessTelemetryHealthState.Unhealthy,
                    AccessTelemetryReason.CapabilityUnproven)
                : current;
        }
    }

    /// <summary>Publishes one immutable exact-profile decision.</summary>
    public void Publish(AccessTelemetryCapabilityGateResult decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        Volatile.Write(ref _current, decision);
    }
}
