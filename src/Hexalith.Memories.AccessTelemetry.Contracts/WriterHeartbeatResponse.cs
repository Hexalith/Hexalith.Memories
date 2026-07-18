// <copyright file="WriterHeartbeatResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Bounded marker-generation state returned to one writer heartbeat.</summary>
public sealed record WriterHeartbeatResponse
{
    /// <summary>Gets whether the heartbeat mutation was accepted.</summary>
    public required bool Accepted { get; init; }

    /// <summary>Gets the bounded rejection reason.</summary>
    public required AccessTelemetryReason Reason { get; init; }

    /// <summary>Gets the currently active marker-key generation.</summary>
    public required string ActiveGeneration { get; init; }

    /// <summary>Gets the staged generation, when rotation is in progress.</summary>
    public string? StagedGeneration { get; init; }
}
