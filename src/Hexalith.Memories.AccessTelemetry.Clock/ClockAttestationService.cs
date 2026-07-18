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
    private static readonly TimeSpan SourceTimeout = TimeSpan.FromSeconds(2);
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
        if (_sources.Count is < 3 or > 9 || _sources.Select(static source => source.SourceId).Distinct(StringComparer.Ordinal).Count() != _sources.Count)
        {
            throw new ClockAttestationException(AccessTelemetryReason.ClockUntrusted);
        }
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
            _sources.Select(source => TryGetSampleAsync(source, cancellationToken))).ConfigureAwait(false))
            .Where(static sample => sample is not null && sample.Authenticated && sample.NotBefore <= sample.NotAfter)
            .Select(static sample => sample!)
            .ToArray();
        int majority = Math.Max(3, (_sources.Count / 2) + 1);
        if (samples.Length < majority)
        {
            throw new ClockAttestationException(AccessTelemetryReason.ClockUntrusted);
        }

        (long majorityLower, long majorityUpper)? interval = FindMajorityInterval(samples, majority);
        if (interval is null)
        {
            throw new ClockAttestationException(AccessTelemetryReason.ClockUntrusted);
        }

        long majorityLower = interval.Value.majorityLower;
        long majorityUpper = interval.Value.majorityUpper;
        long issued = majorityLower + ((majorityUpper - majorityLower) / 2);
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

    private static (long majorityLower, long majorityUpper)? FindMajorityInterval(
        IReadOnlyList<AuthenticatedUtcSample> samples,
        int required)
    {
        (long Lower, long Upper)? best = null;
        int[] selected = new int[required];
        Search(0, 0);
        return best is null ? null : (best.Value.Lower, best.Value.Upper);

        void Search(int start, int depth)
        {
            if (depth == required)
            {
                long lower = selected.Max(index => samples[index].NotBefore.ToUnixTimeMilliseconds());
                long upper = selected.Min(index => samples[index].NotAfter.ToUnixTimeMilliseconds());
                if (lower <= upper && upper - lower <= 250 &&
                    (best is null || upper - lower < best.Value.Upper - best.Value.Lower))
                {
                    best = (lower, upper);
                }

                return;
            }

            for (int index = start; index <= samples.Count - (required - depth); index++)
            {
                selected[depth] = index;
                Search(index + 1, depth + 1);
            }
        }
    }

    private static async Task<AuthenticatedUtcSample?> TryGetSampleAsync(
        IAuthenticatedUtcSource source,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SourceTimeout);
        try
        {
            AuthenticatedUtcSample sample = await source.GetUtcSampleAsync(timeout.Token).ConfigureAwait(false);
            return string.Equals(sample.SourceId, source.SourceId, StringComparison.Ordinal) ? sample : null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            return null;
        }
    }
}
