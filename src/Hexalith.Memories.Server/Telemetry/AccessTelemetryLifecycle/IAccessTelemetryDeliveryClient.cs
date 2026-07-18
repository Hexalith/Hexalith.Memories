// <copyright file="IAccessTelemetryDeliveryClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry.AccessTelemetryLifecycle;

using Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Write-only Dapr-addressed lifecycle delivery boundary.</summary>
internal interface IAccessTelemetryDeliveryClient
{
    /// <summary>Sends one bounded record batch.</summary>
    Task<AccessTelemetryWriteBatchResponse> SendAsync(
        IReadOnlyList<AccessTelemetryRecord> records,
        CancellationToken cancellationToken);
}
