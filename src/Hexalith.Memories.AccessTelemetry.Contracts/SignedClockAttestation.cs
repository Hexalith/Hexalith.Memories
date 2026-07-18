// <copyright file="SignedClockAttestation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Independently signed, context-bound majority UTC interval.</summary>
public sealed record SignedClockAttestation
{
    /// <summary>Gets the deployment identity.</summary>
    public required string DeploymentId { get; init; }

    /// <summary>Gets the calling app identity.</summary>
    public required string AppId { get; init; }

    /// <summary>Gets the unique clock service-instance identity.</summary>
    public required string ServiceInstanceId { get; init; }

    /// <summary>Gets the process epoch.</summary>
    public required string ProcessEpoch { get; init; }

    /// <summary>Gets the component-profile hash.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets the requesting process epoch bound into the signature.</summary>
    public required string RequestingProcessEpoch { get; init; }

    /// <summary>Gets the requesting service-instance identity bound into the signature.</summary>
    public required string RequestingServiceInstanceId { get; init; }

    /// <summary>Gets the request nonce.</summary>
    public required string Nonce { get; init; }

    /// <summary>Gets the majority interval lower bound.</summary>
    public required long NotBeforeUnixMilliseconds { get; init; }

    /// <summary>Gets the majority interval upper bound.</summary>
    public required long NotAfterUnixMilliseconds { get; init; }

    /// <summary>Gets the evidence issue time.</summary>
    public required long IssuedAtUnixMilliseconds { get; init; }

    /// <summary>Gets the evidence expiry time.</summary>
    public required long ExpiresAtUnixMilliseconds { get; init; }

    /// <summary>Gets the signer/key epoch.</summary>
    public required string SignerKeyEpoch { get; init; }

    /// <summary>Gets the base64 signature.</summary>
    public required string Signature { get; init; }
}
