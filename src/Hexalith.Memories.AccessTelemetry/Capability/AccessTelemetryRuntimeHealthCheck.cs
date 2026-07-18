// <copyright file="AccessTelemetryRuntimeHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;
using Hexalith.Memories.AccessTelemetry.Lifecycle;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Reports fail-closed lifecycle gate health without identity-bearing details.</summary>
internal sealed class AccessTelemetryRuntimeHealthCheck(
    IAccessTelemetryRuntimeGate runtimeGate,
    AccessTelemetryOptions options,
    AccessTelemetryProcessorStatus processorStatus,
    TimeProvider timeProvider) : IHealthCheck
{
    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Access telemetry lifecycle writes are disabled."));
        }

        AccessTelemetryCapabilityGateResult current = runtimeGate.Current;
        AccessTelemetryProcessorStatus.Snapshot processor = processorStatus.Current;
        AccessTelemetryReason effectiveReason = processor.Health == AccessTelemetryHealthState.Unhealthy
            ? processor.Reason
            : current.Reason;
        IReadOnlyDictionary<string, object> details = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["cause"] = Cause(effectiveReason),
            ["impact"] = "Lifecycle writes are fail-closed.",
            ["owner"] = "Hexalith Platform Operations",
            ["nextAction"] = NextAction(effectiveReason),
        };
        if (!current.AllowsWrites)
        {
            return Task.FromResult(current.Health == AccessTelemetryHealthState.Degraded
                ? HealthCheckResult.Degraded("Access telemetry lifecycle dependencies are pending.", data: details)
                : HealthCheckResult.Unhealthy("Access telemetry lifecycle gates are fail-closed.", data: details));
        }

        if (processor.Health == AccessTelemetryHealthState.Unhealthy)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Access telemetry lifecycle processor is fail-closed.", data: details));
        }

        bool noData = processor.LastAcceptedOrRejectedUtc is null ||
            timeProvider.GetUtcNow() - processor.LastAcceptedOrRejectedUtc >= TimeSpan.FromMinutes(15);
        return Task.FromResult(HealthCheckResult.Healthy(
            noData ? "Access telemetry lifecycle has no data in the bounded window." : "Access telemetry lifecycle gates are healthy.",
            data: details));
    }

    private static string Cause(AccessTelemetryReason reason)
        => reason switch
        {
            AccessTelemetryReason.CapabilityUnproven => "The exact component profile is unproven or stale.",
            AccessTelemetryReason.RecordIdConflict => "A conflicting record identity made lifecycle persistence terminal.",
            AccessTelemetryReason.ClockUntrusted => "Fresh trusted clock evidence is unavailable.",
            AccessTelemetryReason.DependencyUnavailable or AccessTelemetryReason.RemoteValidationPending => "A required lifecycle dependency is unavailable.",
            AccessTelemetryReason.None => "No lifecycle gate failure is present.",
            _ => "Lifecycle configuration is invalid.",
        };

    private static string NextAction(AccessTelemetryReason reason)
        => reason == AccessTelemetryReason.RecordIdConflict
            ? "Investigate the producer conflict and restart with an explicitly validated configuration epoch."
            : "Run the exact-profile capability and configuration checks before enabling lifecycle writes.";
}
