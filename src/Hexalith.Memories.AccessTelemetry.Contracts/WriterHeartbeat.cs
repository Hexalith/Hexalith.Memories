// <copyright file="WriterHeartbeat.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded dynamic-writer membership heartbeat.</summary>
public sealed record WriterHeartbeat
{
    /// <summary>Gets the deployment identity.</summary>
    public required string DeploymentId { get; init; }

    /// <summary>Gets the unique service-instance ULID.</summary>
    public required string ServiceInstanceId { get; init; }

    /// <summary>Gets the process-epoch ULID.</summary>
    public required string ProcessEpoch { get; init; }

    /// <summary>Gets the loaded marker-key generation.</summary>
    public required string MarkerKeyGeneration { get; init; }

    /// <summary>Gets the old-generation queued record count.</summary>
    public required int OldKeyQueueCount { get; init; }

    /// <summary>Gets the lease expiry in Unix milliseconds.</summary>
    public required long LeaseExpiresAtUnixMilliseconds { get; init; }
}
