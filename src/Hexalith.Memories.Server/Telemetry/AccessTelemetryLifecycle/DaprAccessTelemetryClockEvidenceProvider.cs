// <copyright file="DaprAccessTelemetryClockEvidenceProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics;
using System.Net.Http.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr-addressed clock client that verifies every response before Server acceptance.</summary>
internal sealed class DaprAccessTelemetryClockEvidenceProvider : IAccessTelemetryClockEvidenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly AccessTelemetryOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly string _serviceInstanceId;
    private readonly string _processEpoch;

    /// <summary>Initializes one Server process clock-evidence client.</summary>
    public DaprAccessTelemetryClockEvidenceProvider(
        HttpClient httpClient,
        AccessTelemetryOptions options,
        TimeProvider timeProvider,
        MonotonicRecordIdGenerator ids)
    {
        _httpClient = httpClient;
        _options = options;
        _timeProvider = timeProvider;
        _serviceInstanceId = ids.NewId();
        _processEpoch = ids.NewId();
    }

    /// <inheritdoc/>
    public async Task<SignedClockAttestation> GetAsync(CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();
        try
        {
        var request = new ClockAttestationRequest
        {
            DeploymentId = _options.DeploymentId,
            AppId = "memories",
            ComponentProfileHash = _options.ComponentProfileHash,
            Nonce = new MonotonicRecordIdGenerator().NewId(),
            RequestingProcessEpoch = _processEpoch,
            RequestingServiceInstanceId = _serviceInstanceId,
        };
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "v1/time/attest",
            request,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        SignedClockAttestation attestation = await response.Content.ReadFromJsonAsync<SignedClockAttestation>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Clock service returned an empty attestation.");
        ClockAttestationValidationResult validation = ClockAttestationVerifier.Verify(
            attestation,
            new ClockAttestationValidationContext(
                request.DeploymentId,
                request.AppId,
                request.ComponentProfileHash,
                request.Nonce,
                request.RequestingProcessEpoch,
                request.RequestingServiceInstanceId),
            Convert.FromBase64String(_options.AttestationVerificationKey),
            _timeProvider.GetUtcNow(),
            new BoundedNonceReplayCache(1));
        if (!validation.IsValid)
        {
            throw new AccessTelemetryContractException("clock_untrusted");
        }

        return attestation;
        }
        finally
        {
            ServerAccessTelemetryLifecycleMetrics.RecordAttestationLatency(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
