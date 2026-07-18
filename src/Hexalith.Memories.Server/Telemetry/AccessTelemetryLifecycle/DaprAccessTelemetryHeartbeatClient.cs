// <copyright file="DaprAccessTelemetryHeartbeatClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using System.Net.Http.Json;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Native HTTP client over Dapr service invocation to the fixed heartbeat route.</summary>
internal sealed class DaprAccessTelemetryHeartbeatClient(HttpClient httpClient) : IAccessTelemetryHeartbeatClient
{
    /// <inheritdoc/>
    public async Task SendAsync(WriterHeartbeat heartbeat, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "v1/access-telemetry/heartbeat",
            heartbeat,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
