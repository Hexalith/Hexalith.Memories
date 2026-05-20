// <copyright file="EvidencePacketViewMapping.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

/// <summary>Field-by-field mapping from canonical Evidence Packet members to the Memories evidence cockpit.</summary>
public static class EvidencePacketViewMapping
{
    /// <summary>Gets the fields rendered by the first Evidence Cockpit slice.</summary>
    public static IReadOnlyList<EvidencePacketFieldMapping> RenderedFields { get; } =
    [
        new("scope.tenant", "EvidencePacket.Scope.TenantId", "unknown tenant"),
        new("scope.case", "EvidencePacket.Scope.CaseId", "tenant scope"),
        new("scope.isolation", "EvidencePacket.Scope.IsolationStatus", "unknown scope"),
        new("trust.confidence", "EvidencePacket.Evidence.EvidenceStrength", "unknown"),
        new("trust.freshness", "EvidencePacket.Sources[].SourceUri", "unknown"),
        new("trust.sourceCount", "EvidencePacket.Sources.Count", "0 sources"),
        new("trust.evidenceHealth", "EvidencePacket.State", "unavailable"),
        new("trust.tokenBudget", "EvidencePacket.OmittedDetails.Reason", "within budget"),
        new("result.query", "EvidencePacket.Result.Query", "no query"),
        new("result.summary", "EvidencePacket.Result.Summary", "summary unavailable"),
        new("sources.originIdentifier", "EvidencePacket.Sources[].SourceUri", "redacted source"),
        new("sources.type", "EvidencePacket.Sources[].SourceType", "unknown type"),
        new("sources.freshness", "EvidencePacket.Sources[].SourceUri", "unknown"),
        new("sources.score", "EvidencePacket.Sources[].Score", "score unavailable"),
        new("axes.axis", "EvidencePacket.Evidence.AxisEvidence[].Axis", "axis unavailable"),
        new("axes.normalizedScore", "EvidencePacket.Evidence.AxisEvidence[].Score", "score unavailable"),
        new("axes.rankingReason", "EvidencePacket.Evidence.AxisEvidence[].Description", "ranking reason unavailable"),
        new("graph.path", "EvidencePacket.Graph.RelatedPath", "graph path unavailable"),
        new("graph.edgeTypes", "EvidencePacket.Graph.EdgeTypes", "edge type unavailable"),
        new("graph.gapMarkers", "EvidencePacket.Graph.GapMarkers", "no gap markers"),
    ];
}
