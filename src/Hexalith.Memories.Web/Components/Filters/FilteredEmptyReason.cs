// <copyright file="FilteredEmptyReason.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// The reason a filtered view rendered no results, distinguished only where the Evidence Packet allows.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — empty filtered states must distinguish no match from filtered-out evidence,
/// inaccessible scope, missing source, stale memory, degraded backend, or insufficient evidence, instead
/// of always reading as a successful empty result. The reason is derived side-channel safely: an
/// inaccessible scope never reveals whether matching evidence exists beyond the boundary.
/// </remarks>
public enum FilteredEmptyReason
{
    /// <summary>The query ran in scope and genuinely matched nothing.</summary>
    NoMatch = 0,

    /// <summary>Evidence exists but the active filters excluded it.</summary>
    FilteredOut,

    /// <summary>The requested scope is unauthorized or untrusted.</summary>
    InaccessibleScope,

    /// <summary>Matching knowledge is not ingested yet.</summary>
    NotIngested,

    /// <summary>The available memory is stale.</summary>
    StaleMemory,

    /// <summary>A retrieval backend or axis was degraded.</summary>
    DegradedBackend,

    /// <summary>There is not enough evidence to present a result.</summary>
    InsufficientEvidence,
}
