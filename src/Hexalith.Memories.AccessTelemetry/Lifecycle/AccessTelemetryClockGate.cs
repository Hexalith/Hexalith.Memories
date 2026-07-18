// <copyright file="AccessTelemetryClockGate.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Signature and exact deployment/app/profile gate for lifecycle mutations.</summary>
internal sealed class AccessTelemetryClockGate : IAccessTelemetryClockGate
{
    private readonly string _deploymentId;
    private readonly string _componentProfileHash;
    private readonly byte[] _publicKey;
    private readonly TimeProvider _timeProvider;
    private readonly BoundedNonceReplayCache _replayCache = new(8192);

    /// <summary>Initializes the lifecycle mutation clock gate.</summary>
    public AccessTelemetryClockGate(
        string deploymentId,
        string componentProfileHash,
        byte[] publicKey,
        TimeProvider timeProvider)
    {
        _deploymentId = deploymentId;
        _componentProfileHash = componentProfileHash;
        _publicKey = publicKey;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public ClockAttestationValidationResult Validate(SignedClockAttestation attestation)
    {
        if (!string.Equals(attestation.AppId, "memories", StringComparison.Ordinal))
        {
            return new ClockAttestationValidationResult(false, AccessTelemetryReason.ClockUntrusted);
        }

        return ClockAttestationVerifier.Verify(
            attestation,
            new ClockAttestationValidationContext(
                _deploymentId,
                "memories",
                _componentProfileHash,
                attestation.Nonce,
                attestation.RequestingProcessEpoch,
                attestation.RequestingServiceInstanceId),
            _publicKey,
            _timeProvider.GetUtcNow(),
            _replayCache);
    }
}
