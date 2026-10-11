// <copyright file="ClockSigningReadinessHealthCheck.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using Hexalith.Memories.AccessTelemetry.Contracts;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>Proves the clock can produce a signed attestation from its configured UTC sources.</summary>
internal sealed class ClockSigningReadinessHealthCheck(
    IServiceProvider services,
    IConfiguration configuration,
    TimeProvider timeProvider,
    MonotonicRecordIdGenerator ids) : IHealthCheck
{
    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? encodedKey = configuration["AccessTelemetryLifecycle:AttestationVerificationKey"];
            string? expectedEpoch = configuration["AccessTelemetryLifecycle:ClockSignerKeyEpoch"];
            if (string.IsNullOrWhiteSpace(encodedKey) || string.IsNullOrWhiteSpace(expectedEpoch))
            {
                return HealthCheckResult.Unhealthy("Clock verification key or signer epoch is unavailable.");
            }

            byte[] publicKey = Convert.FromBase64String(encodedKey);
            ClockAttestationService clock = services.GetRequiredService<ClockAttestationService>();
            var request = new ClockAttestationRequest
            {
                DeploymentId = "readiness",
                AppId = "memories-access-telemetry-clock",
                ComponentProfileHash = "readiness",
                Nonce = Guid.NewGuid().ToString("N"),
                RequestingProcessEpoch = ids.NewId(),
                RequestingServiceInstanceId = ids.NewId(),
            };
            SignedClockAttestation attestation = await clock.AttestAsync(
                request,
                cancellationToken).ConfigureAwait(false);
            ClockAttestationValidationResult result = ClockAttestationVerifier.Verify(
                attestation,
                new ClockAttestationValidationContext(
                    request.DeploymentId,
                    request.AppId,
                    request.ComponentProfileHash,
                    request.Nonce,
                    request.RequestingProcessEpoch,
                    request.RequestingServiceInstanceId,
                    SignerKeyEpoch: expectedEpoch),
                publicKey,
                timeProvider.GetUtcNow(),
                new BoundedNonceReplayCache(1));
            return result.IsValid ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Clock attestation verification failed.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Clock attestation is unavailable.", exception);
        }
    }
}
