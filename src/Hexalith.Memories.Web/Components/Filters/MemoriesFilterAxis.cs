// <copyright file="MemoriesFilterAxis.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// The inspectable filter axes a Story 17.3 search/filter surface can expose.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — active filters for each of these axes must remain inspectable, and the UI must
/// indicate when a filter narrows scope, broadens scope, excludes a retrieval axis, or affects confidence.
/// The axis vocabulary is consume-only; it names the Evidence Packet dimensions a filter constrains and
/// never invents new filter semantics.
/// </remarks>
public enum MemoriesFilterAxis
{
    /// <summary>Retrieval axis (syntactic, semantic, graph).</summary>
    RetrievalAxis = 0,

    /// <summary>Source type.</summary>
    SourceType,

    /// <summary>Source freshness.</summary>
    Freshness,

    /// <summary>Confidence / evidence strength.</summary>
    Confidence,

    /// <summary>Time range.</summary>
    TimeRange,

    /// <summary>Metadata.</summary>
    Metadata,

    /// <summary>Graph traversal depth.</summary>
    GraphDepth,

    /// <summary>Evidence state.</summary>
    EvidenceState,
}
