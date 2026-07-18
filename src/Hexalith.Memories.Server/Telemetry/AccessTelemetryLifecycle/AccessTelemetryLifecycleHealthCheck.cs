// <copyright file="AccessTelemetryLifecycleHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Reports lifecycle health separately from business readiness.</summary>
internal sealed class AccessTelemetryLifecycleHealthCheck(
    AccessTelemetryLifecycleStatus status,
    AccessTelemetryOptions options,
    TimeProvider timeProvider) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        AccessTelemetryLifecycleStatusSnapshot current = status.Current;
        AccessTelemetryHealthState effectiveHealth = current.Health;
        if (options.Enabled && effectiveHealth == AccessTelemetryHealthState.Healthy &&
            (current.LastAcceptedOrRejectedUtc is null || timeProvider.GetUtcNow() - current.LastAcceptedOrRejectedUtc >= TimeSpan.FromMinutes(15)))
        {
            effectiveHealth = AccessTelemetryHealthState.NoData;
        }

        IReadOnlyDictionary<string, object> details = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["cause"] = Cause(current.Reason),
            ["impact"] = "Lifecycle writes may be unavailable; business readiness is unaffected.",
            ["owner"] = "Hexalith Platform Operations",
            ["nextAction"] = NextAction(current.Reason),
        };
        HealthCheckResult result = effectiveHealth switch
        {
            AccessTelemetryHealthState.Unhealthy => HealthCheckResult.Unhealthy("Access telemetry lifecycle is fail-closed.", data: details),
            AccessTelemetryHealthState.Degraded => HealthCheckResult.Degraded("Access telemetry lifecycle validation is pending.", data: details),
            AccessTelemetryHealthState.NoData => HealthCheckResult.Healthy("Access telemetry lifecycle has no data in the bounded window.", data: details),
            _ => HealthCheckResult.Healthy("Access telemetry lifecycle gate is healthy.", data: details),
        };
        return Task.FromResult(result);
    }

    private static string Cause(AccessTelemetryReason reason)
        => reason switch
        {
            AccessTelemetryReason.RemoteValidationPending => "Remote lifecycle validation has not completed.",
            AccessTelemetryReason.ClockUntrusted => "Trusted clock evidence is unavailable.",
            AccessTelemetryReason.CapabilityUnproven => "The exact component profile is unproven or stale.",
            AccessTelemetryReason.None => "No lifecycle gate failure is present.",
            _ => "Lifecycle configuration is invalid.",
        };

    private static string NextAction(AccessTelemetryReason reason)
        => reason == AccessTelemetryReason.RemoteValidationPending
            ? "Restore the Dapr lifecycle dependencies and observe automatic revalidation."
            : "Validate lifecycle configuration and evidence, then restart the lifecycle path.";
}
