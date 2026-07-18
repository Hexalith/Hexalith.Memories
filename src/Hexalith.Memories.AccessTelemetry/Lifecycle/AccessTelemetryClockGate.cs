// <copyright file="AccessTelemetryClockGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Signature and exact deployment/app/profile gate for lifecycle mutations.</summary>
internal sealed class AccessTelemetryClockGate : IAccessTelemetryClockGate
{
    private readonly AccessTelemetryRuntimeOptionsProvider _optionsProvider;
    private readonly TimeProvider _timeProvider;
    private readonly BoundedNonceReplayCache _replayCache = new(8192);

    /// <summary>Initializes the lifecycle mutation clock gate.</summary>
    public AccessTelemetryClockGate(
        AccessTelemetryRuntimeOptionsProvider optionsProvider,
        TimeProvider timeProvider)
    {
        _optionsProvider = optionsProvider;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public ClockAttestationValidationResult Validate(
        SignedClockAttestation attestation,
        string expectedAppId,
        string expectedProcessEpoch,
        string expectedServiceInstanceId)
    {
        if (!string.Equals(attestation.AppId, expectedAppId, StringComparison.Ordinal))
        {
            return new ClockAttestationValidationResult(false, AccessTelemetryReason.ClockUntrusted);
        }

        AccessTelemetryOptions options = _optionsProvider.Current;
        byte[] publicKey;
        try
        {
            publicKey = Convert.FromBase64String(options.AttestationVerificationKey);
        }
        catch (FormatException)
        {
            return new ClockAttestationValidationResult(false, AccessTelemetryReason.ClockUntrusted);
        }

        return ClockAttestationVerifier.Verify(
            attestation,
            new ClockAttestationValidationContext(
                options.DeploymentId,
                expectedAppId,
                options.ComponentProfileHash,
                attestation.Nonce,
                expectedProcessEpoch,
                expectedServiceInstanceId,
                SignerKeyEpoch: options.ClockSignerKeyEpoch),
            publicKey,
            _timeProvider.GetUtcNow(),
            _replayCache);
    }
}
