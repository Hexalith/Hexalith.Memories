// <copyright file="PreflightDedupReservation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Shared constants and helpers for the transient preflight dedup reservation marker.
/// This sentinel is intentionally distinct from the workflow's permanent dedup value (memory unit id).
/// When the marker is observed later, callers must fail open and let workflow-level idempotency decide.</summary>
public static class PreflightDedupReservation
{
    /// <summary>The Redis value written while the HTTP endpoint temporarily owns the dedup key.</summary>
    public const string ReservedValue = "reserved";

    /// <summary>Determines whether the persisted value is the transient preflight reservation marker.</summary>
    /// <param name="value">The Redis string value.</param>
    /// <returns><see langword="true"/> when the value is the transient reservation marker.</returns>
    public static bool IsTransientReservation(string? value)
        => string.Equals(value, ReservedValue, StringComparison.Ordinal);
}
