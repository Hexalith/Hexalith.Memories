// <copyright file="AccessTelemetryCapabilityEvidenceOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AccessTelemetry.Capability;

/// <summary>Deployment evidence metadata that complements, but never replaces, behavioral probes.</summary>
internal sealed record AccessTelemetryCapabilityEvidenceOptions
{
    /// <summary>Gets whether the exact component version is pinned.</summary>
    public bool ExactVersionPinned { get; init; }

    /// <summary>Gets the exact-profile evidence expiry.</summary>
    public DateTimeOffset ValidUntilUtc { get; init; }
}
