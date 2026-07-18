// <copyright file="DaprLifecycleClockEvidenceProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Lifecycle;

using System.Net.Http;

using Dapr.Client;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr-addressed lifecycle clock client with persistent local replay protection.</summary>
internal sealed class DaprLifecycleClockEvidenceProvider : ILifecycleClockEvidenceProvider
{
    private readonly DaprClient _daprClient;
    private readonly AccessTelemetryRuntimeOptionsProvider _optionsProvider;
    private readonly TimeProvider _timeProvider;
    private readonly BoundedNonceReplayCache _replayCache = new(8192);
    private readonly MonotonicRecordIdGenerator _ids;
    private readonly string _serviceInstanceId;
    private readonly string _processEpoch;

    /// <summary>Initializes one lifecycle-service process clock client.</summary>
    public DaprLifecycleClockEvidenceProvider(
        DaprClient daprClient,
        AccessTelemetryRuntimeOptionsProvider optionsProvider,
        TimeProvider timeProvider,
        MonotonicRecordIdGenerator ids)
    {
        _daprClient = daprClient;
        _optionsProvider = optionsProvider;
        _timeProvider = timeProvider;
        _ids = ids;
        _serviceInstanceId = ids.NewId();
        _processEpoch = ids.NewId();
    }

    /// <inheritdoc/>
    public async Task<LifecycleClockEvidence> GetAsync(CancellationToken cancellationToken)
    {
        AccessTelemetryOptions options = _optionsProvider.Current;
        var request = new ClockAttestationRequest
        {
            DeploymentId = options.DeploymentId,
            AppId = options.LifecycleAppId,
            ComponentProfileHash = options.ComponentProfileHash,
            Nonce = _ids.NewId(),
            RequestingProcessEpoch = _processEpoch,
            RequestingServiceInstanceId = _serviceInstanceId,
        };
#pragma warning disable CS0618 // DaprClient 1.18 has no non-obsolete typed response helper for this internal route.
        SignedClockAttestation attestation = await _daprClient.InvokeMethodAsync<ClockAttestationRequest, SignedClockAttestation>(
            HttpMethod.Post,
            options.ClockAppId,
            "v1/time/attest",
            request,
            cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618
        ClockAttestationValidationResult validation = ClockAttestationVerifier.Verify(
            attestation,
            new ClockAttestationValidationContext(
                request.DeploymentId,
                request.AppId,
                request.ComponentProfileHash,
                request.Nonce,
                request.RequestingProcessEpoch,
                request.RequestingServiceInstanceId,
                SignerKeyEpoch: options.ClockSignerKeyEpoch),
            Convert.FromBase64String(options.AttestationVerificationKey),
            _timeProvider.GetUtcNow(),
            _replayCache);
        if (!validation.IsValid)
        {
            throw new AccessTelemetryContractException("clock_untrusted");
        }

        return new LifecycleClockEvidence(attestation, _processEpoch, _serviceInstanceId);
    }
}
