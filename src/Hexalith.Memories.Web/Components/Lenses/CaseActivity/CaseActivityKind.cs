// <copyright file="CaseActivityKind.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.CaseActivity;

/// <summary>
/// Kind of case-activity row, derived only from fields the canonical Evidence Packet exposes.
/// </summary>
/// <remarks>
/// Story 17.4 — the Case Activity Trail consumes the packet's source citations, annotation counts, graph
/// relationships, trust state, and recovery actions. The canonical contract exposes no activity-type
/// taxonomy or timestamps, so unknown or future activity types are not invented; rows trace to a named
/// contract field and ordering is deterministic by rank with an explicit "timestamps unavailable" note.
/// </remarks>
public enum CaseActivityKind
{
    /// <summary>A ranked source citation (EvidencePacket.Sources[]).</summary>
    SourceCitation = 0,

    /// <summary>An annotation count on a memory unit (EvidencePacket.Sources[].AnnotationsCount).</summary>
    Annotation,

    /// <summary>A graph relationship between memory units (EvidencePacket.Graph.RelatedPath / EdgeTypes).</summary>
    Relationship,

    /// <summary>A graph gap marker (EvidencePacket.Graph.GapMarkers).</summary>
    GraphGap,

    /// <summary>The current packet trust state (EvidencePacket.State via the recovery grammar).</summary>
    TrustState,

    /// <summary>A safe recovery action attached to the packet (EvidencePacket.Recovery[]).</summary>
    Recovery,
}
