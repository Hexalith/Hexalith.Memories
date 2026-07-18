// <copyright file="IAccessTelemetryHeartbeatClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Write-only Dapr-addressed heartbeat boundary.</summary>
internal interface IAccessTelemetryHeartbeatClient
{
    /// <summary>Sends one bounded writer lease heartbeat.</summary>
    Task SendAsync(WriterHeartbeat heartbeat, CancellationToken cancellationToken);
}
