// <copyright file="ClockAttestationCanonicalizer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

/// <summary>Deterministic signed payload for clock attestations.</summary>
public static class ClockAttestationCanonicalizer
{
    /// <summary>Canonicalizes every signed field, excluding only the signature.</summary>
    public static byte[] Canonicalize(SignedClockAttestation attestation)
    {
        ArgumentNullException.ThrowIfNull(attestation);
        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            writer.WriteStartObject();
            writer.WriteString("appId", attestation.AppId);
            writer.WriteString("componentProfileHash", attestation.ComponentProfileHash);
            writer.WriteString("deploymentId", attestation.DeploymentId);
            writer.WriteNumber("expiresAtUnixMilliseconds", attestation.ExpiresAtUnixMilliseconds);
            writer.WriteNumber("issuedAtUnixMilliseconds", attestation.IssuedAtUnixMilliseconds);
            writer.WriteNumber("notAfterUnixMilliseconds", attestation.NotAfterUnixMilliseconds);
            writer.WriteNumber("notBeforeUnixMilliseconds", attestation.NotBeforeUnixMilliseconds);
            writer.WriteString("nonce", attestation.Nonce);
            writer.WriteString("processEpoch", attestation.ProcessEpoch);
            writer.WriteString("requestingProcessEpoch", attestation.RequestingProcessEpoch);
            writer.WriteString("requestingServiceInstanceId", attestation.RequestingServiceInstanceId);
            writer.WriteString("serviceInstanceId", attestation.ServiceInstanceId);
            writer.WriteString("signerKeyEpoch", attestation.SignerKeyEpoch);
            writer.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }
}
