// <copyright file="DaprAccessTelemetryHeartbeatClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Net.Http.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Native HTTP client over Dapr service invocation to the fixed heartbeat route.</summary>
internal sealed class DaprAccessTelemetryHeartbeatClient(
    HttpClient httpClient,
    IAccessTelemetryClockEvidenceProvider clockEvidence) : IAccessTelemetryHeartbeatClient
{
    /// <inheritdoc/>
    public async Task<WriterHeartbeatResponse> SendAsync(WriterHeartbeat heartbeat, CancellationToken cancellationToken)
    {
        SignedClockAttestation attestation = await clockEvidence.GetAsync(cancellationToken).ConfigureAwait(false);
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "v1/access-telemetry/heartbeat",
            new WriterHeartbeatRequest { Heartbeat = heartbeat, ClockAttestation = attestation },
            cancellationToken).ConfigureAwait(false);
        WriterHeartbeatResponse bounded = await response.Content.ReadFromJsonAsync<WriterHeartbeatResponse>(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The lifecycle service returned an empty heartbeat response.");
        if (!response.IsSuccessStatusCode && bounded.Reason is not (
            AccessTelemetryReason.ConfigurationInvalid or
            AccessTelemetryReason.RecordIdConflict or
            AccessTelemetryReason.SchemaMismatch or
            AccessTelemetryReason.ClockUntrusted or
            AccessTelemetryReason.CapabilityUnproven))
        {
            response.EnsureSuccessStatusCode();
        }

        return bounded;
    }
}
