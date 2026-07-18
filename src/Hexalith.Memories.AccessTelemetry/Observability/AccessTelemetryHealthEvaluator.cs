// <copyright file="AccessTelemetryHealthEvaluator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Observability;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Applies Unhealthy &gt; Degraded &gt; NoData/Healthy precedence.</summary>
internal static class AccessTelemetryHealthEvaluator
{
    /// <summary>Evaluates lifecycle health without affecting business readiness.</summary>
    public static AccessTelemetryHealthSnapshot Evaluate(
        bool enabled,
        bool allGatesHealthy,
        bool remoteAvailable,
        DateTimeOffset? lastAcceptedOrRejected,
        DateTimeOffset now)
    {
        if (!enabled || !allGatesHealthy)
        {
            return CreateFailure(AccessTelemetryReason.ConfigurationInvalid);
        }

        if (!remoteAvailable)
        {
            return CreateFailure(AccessTelemetryReason.RemoteValidationPending) with
            {
                State = AccessTelemetryHealthState.Degraded,
            };
        }

        bool noData = lastAcceptedOrRejected is null || now - lastAcceptedOrRejected >= TimeSpan.FromMinutes(15);
        return new AccessTelemetryHealthSnapshot
        {
            State = noData ? AccessTelemetryHealthState.NoData : AccessTelemetryHealthState.Healthy,
            Reason = AccessTelemetryReason.None,
            Cause = noData ? "No accepted or rejected lifecycle record in the bounded window." : "All lifecycle gates are healthy.",
            Impact = "Business readiness remains available.",
            Owner = "Hexalith Platform Operations",
            NextAction = noData ? "Confirm expected access traffic." : "No action required.",
        };
    }

    /// <summary>Creates bounded operational detail for a failure/degradation reason.</summary>
    public static AccessTelemetryHealthSnapshot CreateFailure(AccessTelemetryReason reason)
        => new()
        {
            State = AccessTelemetryHealthState.Unhealthy,
            Reason = reason,
            Cause = reason switch
            {
                AccessTelemetryReason.RemoteValidationPending => "Lifecycle service validation is pending.",
                AccessTelemetryReason.ClockUntrusted => "Trusted time evidence is unavailable.",
                AccessTelemetryReason.CapabilityUnproven => "The exact component profile is unproven or stale.",
                _ => "Lifecycle configuration is invalid.",
            },
            Impact = "Lifecycle writes are fail-closed; business readiness remains available.",
            Owner = "Hexalith Platform Operations",
            NextAction = "Validate the bounded configuration and capability evidence, then restart the lifecycle path.",
        };
}
