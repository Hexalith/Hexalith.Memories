// <copyright file="AccessTelemetryPhysicalReclamationEvidenceReceipt.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Receipt returned after authenticated physical-reclamation evidence is accepted.</summary>
public sealed record AccessTelemetryPhysicalReclamationEvidenceReceipt
{
    /// <summary>Gets the bounded acceptance status.</summary>
    public required string Status { get; init; }

    /// <summary>Gets the configured evidence identifier.</summary>
    public required string EvidenceId { get; init; }

    /// <summary>Gets the exact approved component-profile hash.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets the verified immutable C3 artifact hash.</summary>
    public required string ArtifactSha256 { get; init; }

    /// <summary>Gets the C1-reviewed reporter image digest bound to the accepted artifact.</summary>
    public required string ReporterImageDigest { get; init; }

    /// <summary>Gets when physical reclamation was observed.</summary>
    public required long ObservedAtUnixMilliseconds { get; init; }
}
