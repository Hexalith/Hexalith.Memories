// <copyright file="AccessTelemetryPhysicalReclamationEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Contracts;

/// <summary>Verified C3 adapter-authority evidence for PostgreSQL allocator reclamation.</summary>
public sealed record AccessTelemetryPhysicalReclamationEvidence
{
    /// <summary>Gets the configured evidence identifier.</summary>
    public required string EvidenceId { get; init; }

    /// <summary>Gets the exact approved component-profile hash.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets the verified immutable C3 artifact hash.</summary>
    public required string ArtifactSha256 { get; init; }

    /// <summary>Gets when physical reclamation was observed.</summary>
    public required long ObservedAtUnixMilliseconds { get; init; }

}
