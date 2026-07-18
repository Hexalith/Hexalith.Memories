// <copyright file="AccessTelemetryWriteBatchResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Dapr invocation result with only bounded counters and reasons.</summary>
public sealed record AccessTelemetryWriteBatchResponse
{
    /// <summary>Gets the accepted record count.</summary>
    public required int Accepted { get; init; }

    /// <summary>Gets the rejected record count.</summary>
    public required int Rejected { get; init; }

    /// <summary>Gets the bounded result reason.</summary>
    public required AccessTelemetryReason Reason { get; init; }
}
