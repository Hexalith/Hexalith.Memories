// <copyright file="ClockAttestationVerifier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Security.Cryptography;

/// <summary>Fail-closed signature, context, freshness, majority, delta, and replay verifier.</summary>
public static class ClockAttestationVerifier
{
    /// <summary>Verifies trusted-clock evidence against the exact caller context.</summary>
    public static ClockAttestationValidationResult Verify(
        SignedClockAttestation attestation,
        ClockAttestationValidationContext expected,
        ReadOnlySpan<byte> subjectPublicKeyInfo,
        DateTimeOffset now,
        BoundedNonceReplayCache replayCache)
    {
        ArgumentNullException.ThrowIfNull(attestation);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(replayCache);
        try
        {
            if (!string.Equals(attestation.DeploymentId, expected.DeploymentId, StringComparison.Ordinal) ||
                !string.Equals(attestation.AppId, expected.AppId, StringComparison.Ordinal) ||
                !string.Equals(attestation.ComponentProfileHash, expected.ComponentProfileHash, StringComparison.Ordinal) ||
                !string.Equals(attestation.Nonce, expected.Nonce, StringComparison.Ordinal) ||
                !string.Equals(attestation.RequestingProcessEpoch, expected.RequestingProcessEpoch, StringComparison.Ordinal) ||
                !string.Equals(attestation.RequestingServiceInstanceId, expected.RequestingServiceInstanceId, StringComparison.Ordinal) ||
                expected.ClockServiceInstanceId is not null && !string.Equals(attestation.ServiceInstanceId, expected.ClockServiceInstanceId, StringComparison.Ordinal) ||
                expected.ClockProcessEpoch is not null && !string.Equals(attestation.ProcessEpoch, expected.ClockProcessEpoch, StringComparison.Ordinal) ||
                expected.SignerKeyEpoch is not null && !string.Equals(attestation.SignerKeyEpoch, expected.SignerKeyEpoch, StringComparison.Ordinal) ||
                !IsUlid(attestation.ServiceInstanceId) || !IsUlid(attestation.ProcessEpoch) ||
                !IsUlid(attestation.RequestingProcessEpoch) || !IsUlid(attestation.RequestingServiceInstanceId) ||
                string.IsNullOrWhiteSpace(attestation.SignerKeyEpoch) || attestation.SignerKeyEpoch.Length > 32)
            {
                return Invalid();
            }

            long nowMilliseconds = now.ToUnixTimeMilliseconds();
            if (attestation.NotBeforeUnixMilliseconds > attestation.NotAfterUnixMilliseconds ||
                attestation.NotAfterUnixMilliseconds - attestation.NotBeforeUnixMilliseconds > 250 ||
                attestation.ExpiresAtUnixMilliseconds <= nowMilliseconds ||
                attestation.IssuedAtUnixMilliseconds > nowMilliseconds + 1000 ||
                nowMilliseconds < attestation.NotBeforeUnixMilliseconds - 1000 ||
                nowMilliseconds > attestation.NotAfterUnixMilliseconds + 1000)
            {
                return Invalid();
            }

            using ECDsa verifier = ECDsa.Create();
            verifier.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out int bytesRead);
            if (bytesRead != subjectPublicKeyInfo.Length || !verifier.VerifyData(
                ClockAttestationCanonicalizer.Canonicalize(attestation),
                Convert.FromBase64String(attestation.Signature),
                HashAlgorithmName.SHA256))
            {
                return Invalid();
            }

            return replayCache.TryAdd(attestation.Nonce)
                ? new ClockAttestationValidationResult(
                    true,
                    AccessTelemetryReason.None,
                    attestation.NotBeforeUnixMilliseconds + ((attestation.NotAfterUnixMilliseconds - attestation.NotBeforeUnixMilliseconds) / 2))
                : Invalid();
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or ArgumentException)
        {
            return Invalid();
        }
    }

    private static ClockAttestationValidationResult Invalid()
        => new(false, AccessTelemetryReason.ClockUntrusted);

    private static bool IsUlid(string value)
        => value.Length == 26 && value.All(static character =>
            character is >= '0' and <= '9' or >= 'A' and <= 'H' or >= 'J' and <= 'N' or >= 'P' and <= 'T' or >= 'V' and <= 'Z');
}
