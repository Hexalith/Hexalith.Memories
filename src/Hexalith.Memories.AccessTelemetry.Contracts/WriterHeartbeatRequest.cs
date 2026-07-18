// <copyright file="WriterHeartbeatRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Clock-gated writer heartbeat request.</summary>
public sealed record WriterHeartbeatRequest
{
    /// <summary>Gets the bounded writer heartbeat.</summary>
    public required WriterHeartbeat Heartbeat { get; init; }

    /// <summary>Gets fresh single-use trusted-clock evidence for the mutation.</summary>
    public required SignedClockAttestation ClockAttestation { get; init; }
}
