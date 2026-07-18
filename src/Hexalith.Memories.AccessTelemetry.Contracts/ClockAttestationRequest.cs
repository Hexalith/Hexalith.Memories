// <copyright file="ClockAttestationRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Context-bound request for independent trusted-clock evidence.</summary>
public sealed record ClockAttestationRequest
{
    /// <summary>Gets the deployment identity.</summary>
    public required string DeploymentId { get; init; }

    /// <summary>Gets the requesting Dapr app ID.</summary>
    public required string AppId { get; init; }

    /// <summary>Gets the exact component-profile hash.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets the single-use nonce.</summary>
    public required string Nonce { get; init; }

    /// <summary>Gets the requesting process epoch.</summary>
    public required string RequestingProcessEpoch { get; init; }

    /// <summary>Gets the requesting service-instance ID.</summary>
    public required string RequestingServiceInstanceId { get; init; }
}
