// <copyright file="LensFieldAvailability.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// Availability of a single lens field projected from the canonical Evidence Packet.
/// </summary>
/// <remarks>
/// Story 17.4 — unknown, unexposed, future, or redacted contract fields fail closed to a safe
/// contract-boundary state rather than to empty success or silently hidden data. When an upstream field is
/// unavailable the lens renders <c>unknown</c>, <c>unavailable</c>, <c>redacted</c>, or
/// <c>insufficient evidence</c>; it never infers a value from raw payloads, logs, exception text, local
/// paths, diagnostics, or provider internals.
/// </remarks>
public enum LensFieldAvailability
{
    /// <summary>The contract exposes the field and it is safe to display.</summary>
    Available = 0,

    /// <summary>The contract does not expose the field, or it is degraded; render an unavailable fallback.</summary>
    Unavailable,

    /// <summary>The field exists but was redacted upstream; render a redacted fallback.</summary>
    Redacted,

    /// <summary>The contract cannot safely support the value; render an insufficient-evidence fallback.</summary>
    InsufficientEvidence,

    /// <summary>The scope is restrictive; render an unauthorized fallback without revealing content.</summary>
    Unauthorized,
}
