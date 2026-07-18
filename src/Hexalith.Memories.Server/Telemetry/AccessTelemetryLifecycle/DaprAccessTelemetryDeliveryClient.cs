// <copyright file="DaprAccessTelemetryDeliveryClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Diagnostics;
using System.Net.Http.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Native HTTP client over Dapr service invocation to the fixed lifecycle app ID.</summary>
internal sealed class DaprAccessTelemetryDeliveryClient(
    HttpClient httpClient,
    IAccessTelemetryClockEvidenceProvider clockEvidence,
    AccessTelemetryOptions options) : IAccessTelemetryDeliveryClient
{
    /// <inheritdoc/>
    public async Task<AccessTelemetryWriteBatchResponse> SendAsync(
        IReadOnlyList<AccessTelemetryRecord> records,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);
        SignedClockAttestation attestation = await clockEvidence.GetAsync(cancellationToken).ConfigureAwait(false);
        var request = new AccessTelemetryWriteBatchRequest
        {
            ConfigurationEpoch = options.ConfigurationEpoch,
            ComponentProfileHash = options.ComponentProfileHash,
            ClockAttestation = attestation,
            Records = records,
        };
        long started = Stopwatch.GetTimestamp();
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                "v1/access-telemetry/write",
                request,
                cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AccessTelemetryWriteBatchResponse>(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The lifecycle service returned an empty bounded response.");
        }
        finally
        {
            ServerAccessTelemetryLifecycleMetrics.RecordDaprLatency(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
