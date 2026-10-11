// <copyright file="ClockSigningReadinessHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Proves the clock can produce a signed attestation from its configured UTC sources.</summary>
internal sealed class ClockSigningReadinessHealthCheck(IServiceProvider services) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ClockAttestationService clock = services.GetRequiredService<ClockAttestationService>();
            SignedClockAttestation attestation = await clock.AttestAsync(
                new ClockAttestationRequest
                {
                    DeploymentId = "readiness",
                    AppId = "memories-access-telemetry-clock",
                    ComponentProfileHash = "readiness",
                    Nonce = Guid.NewGuid().ToString("N"),
                    RequestingProcessEpoch = "readiness",
                    RequestingServiceInstanceId = "readiness",
                },
                cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(attestation.Signature)
                ? HealthCheckResult.Unhealthy("Clock signing material is unavailable.")
                : HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Clock attestation is unavailable.", exception);
        }
    }
}
