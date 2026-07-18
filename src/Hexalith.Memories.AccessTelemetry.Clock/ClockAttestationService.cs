// <copyright file="ClockAttestationService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Clock;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Produces signed majority UTC intervals from at least three independent authorities.</summary>
internal sealed class ClockAttestationService
{
    private static readonly TimeSpan EvidenceLifetime = TimeSpan.FromSeconds(30);
    private readonly IReadOnlyList<IAuthenticatedUtcSource> _sources;
    private readonly IClockAttestationSigner _signer;
    private readonly TimeProvider _timeProvider;
    private readonly string _serviceInstanceId;
    private readonly string _processEpoch;

    /// <summary>Initializes one clock-service process and its unique epochs.</summary>
    public ClockAttestationService(
        IEnumerable<IAuthenticatedUtcSource> sources,
        IClockAttestationSigner signer,
        TimeProvider timeProvider,
        MonotonicRecordIdGenerator ids)
    {
        _sources = sources?.ToArray() ?? throw new ArgumentNullException(nameof(sources));
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ArgumentNullException.ThrowIfNull(ids);
        _serviceInstanceId = ids.NewId();
        _processEpoch = ids.NewId();
    }

    /// <summary>Produces one signed context-bound attestation.</summary>
    public async Task<SignedClockAttestation> AttestAsync(
        ClockAttestationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        AuthenticatedUtcSample[] samples = (await Task.WhenAll(
            _sources.Select(source => source.GetUtcSampleAsync(cancellationToken))).ConfigureAwait(false))
            .Where(static sample => sample.Authenticated && sample.NotBefore <= sample.NotAfter)
            .GroupBy(static sample => sample.SourceId, StringComparer.Ordinal)
            .Select(static group => group.Single())
            .ToArray();
        if (samples.Length < 3)
        {
            throw new ClockAttestationException(AccessTelemetryReason.ClockUntrusted);
        }

        long[] lower = samples.Select(static sample => sample.NotBefore.ToUnixTimeMilliseconds()).Order().ToArray();
        long[] upper = samples.Select(static sample => sample.NotAfter.ToUnixTimeMilliseconds()).Order().ToArray();
        long majorityLower = lower[lower.Length / 2];
        long majorityUpper = upper[upper.Length / 2];
        if (majorityLower > majorityUpper || majorityUpper - majorityLower > 250)
        {
            throw new ClockAttestationException(AccessTelemetryReason.ClockUntrusted);
        }

        long issued = _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        SignedClockAttestation unsigned = new()
        {
            DeploymentId = request.DeploymentId,
            AppId = request.AppId,
            ServiceInstanceId = _serviceInstanceId,
            ProcessEpoch = _processEpoch,
            ComponentProfileHash = request.ComponentProfileHash,
            RequestingProcessEpoch = request.RequestingProcessEpoch,
            RequestingServiceInstanceId = request.RequestingServiceInstanceId,
            Nonce = request.Nonce,
            NotBeforeUnixMilliseconds = majorityLower,
            NotAfterUnixMilliseconds = majorityUpper,
            IssuedAtUnixMilliseconds = issued,
            ExpiresAtUnixMilliseconds = issued + (long)EvidenceLifetime.TotalMilliseconds,
            SignerKeyEpoch = _signer.KeyEpoch,
            Signature = string.Empty,
        };
        return unsigned with
        {
            Signature = Convert.ToBase64String(_signer.Sign(ClockAttestationCanonicalizer.Canonicalize(unsigned))),
        };
    }
}
