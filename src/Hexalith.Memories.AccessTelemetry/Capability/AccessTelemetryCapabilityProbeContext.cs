// <copyright file="AccessTelemetryCapabilityProbeContext.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Immutable deployment evidence bound to one exact behavioral-probe run.</summary>
internal sealed record AccessTelemetryCapabilityProbeContext
{
    /// <summary>Gets the exact lowercase component-profile SHA-256.</summary>
    public required string ComponentProfileHash { get; init; }

    /// <summary>Gets whether the selected component version is pinned exactly.</summary>
    public required bool ExactVersionPinned { get; init; }

    /// <summary>Gets whether application code remained behind Dapr APIs.</summary>
    public required bool DaprOnlyBoundary { get; init; }

    /// <summary>Gets whether this is a Production evaluation.</summary>
    public required bool Production { get; init; }

    /// <summary>Gets whether an alpha component is explicitly allowed.</summary>
    public required bool AllowAlpha { get; init; }

    /// <summary>Gets whether the exact selected component is alpha.</summary>
    public required bool IsAlpha { get; init; }

    /// <summary>Gets the physical-capacity evidence identity.</summary>
    public required string CapacityEvidenceId { get; init; }

    /// <summary>Gets the physical-reclamation evidence-hook identity.</summary>
    public required string PhysicalReclamationEvidenceId { get; init; }

    /// <summary>Gets the exact-profile proof expiry.</summary>
    public required DateTimeOffset ValidUntilUtc { get; init; }
}
