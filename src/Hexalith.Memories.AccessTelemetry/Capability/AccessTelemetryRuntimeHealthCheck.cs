// <copyright file="AccessTelemetryRuntimeHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Reports fail-closed lifecycle gate health without identity-bearing details.</summary>
internal sealed class AccessTelemetryRuntimeHealthCheck(
    IAccessTelemetryRuntimeGate runtimeGate,
    AccessTelemetryOptions options) : IHealthCheck
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
        IReadOnlyDictionary<string, object> details = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["cause"] = current.Reason == AccessTelemetryReason.CapabilityUnproven
                ? "The exact component profile is unproven or stale."
                : "Lifecycle configuration is invalid.",
            ["impact"] = "Lifecycle writes are fail-closed.",
            ["owner"] = "Hexalith Platform Operations",
            ["nextAction"] = "Run the exact-profile capability probes and restart after valid evidence is available.",
        };
        return Task.FromResult(current.AllowsWrites
            ? HealthCheckResult.Healthy("Access telemetry lifecycle gates are healthy.", data: details)
            : HealthCheckResult.Unhealthy("Access telemetry lifecycle gates are fail-closed.", data: details));
    }
}
