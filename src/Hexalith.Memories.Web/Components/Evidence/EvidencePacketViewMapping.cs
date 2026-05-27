// <copyright file="EvidencePacketViewMapping.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

/// <summary>Field-by-field mapping from canonical Evidence Packet members to the Memories evidence cockpit.</summary>
public static class EvidencePacketViewMapping
{
    /// <summary>Sentinel for UI fields that have no canonical Evidence Packet source.</summary>
    /// <remarks>
    /// Use this when AC2 requires a visible state but the contract does not yet expose a field
    /// to drive it. The renderer must show the documented unavailable fallback in that case.
    /// </remarks>
    public const string NoContractSource = "(none — no contract source)";

    /// <summary>Gets the fields rendered by the Evidence Cockpit slice.</summary>
    public static IReadOnlyList<EvidencePacketFieldMapping> RenderedFields { get; } =
    [
        new("scope.tenant", "EvidencePacket.Scope.TenantId", "unknown tenant"),
        new("scope.case", "EvidencePacket.Scope.CaseId", "tenant scope"),
        new("scope.isolation", "EvidencePacket.Scope.IsolationStatus", "Unknown"),
        new("trust.confidence", "EvidencePacket.Evidence.EvidenceStrength", "None"),
        new("trust.freshness", NoContractSource, EvidenceDisplay.FreshnessUnavailable),
        new("trust.sourceCount", "EvidencePacket.Sources.Count", "sources unavailable"),
        new("trust.evidenceHealth", "EvidencePacket.State", "Empty"),
        new("trust.tokenBudget", "EvidencePacket.OmittedDetails.Reason", "within budget"),
        new("result.query", "EvidencePacket.Result.Query", "no query"),
        new("result.summary", "EvidencePacket.Result.Summary", "summary unavailable"),
        new("sources.originIdentifier", "EvidencePacket.Sources[].SourceUri", "source unavailable"),
        new("sources.type", "EvidencePacket.Sources[].SourceType", "unknown type"),
        new("sources.snippet", "EvidencePacket.Sources[].Snippet", "snippet unavailable"),
        new("sources.memoryUnit", "EvidencePacket.Sources[].MemoryUnitId", "memory unit unavailable"),
        new("sources.rank", "EvidencePacket.Sources[].Rank", "rank unavailable"),
        new("sources.score", "EvidencePacket.Sources[].Score", "score unavailable"),
        new("axes.axis", "EvidencePacket.Evidence.AxisEvidence[].Axis", "axis unavailable"),
        new("axes.normalizedScore", "EvidencePacket.Evidence.AxisEvidence[].Score", "score unavailable"),
        new("axes.rankingReason", "EvidencePacket.Evidence.AxisEvidence[].Description", "ranking reason unavailable"),
        new("axes.normalizationMethod", "EvidencePacket.Evidence.AxisEvidence[].NormalizationMethod", "normalization unavailable"),
        new("axes.unavailableAxes", "EvidencePacket.Evidence.UnavailableAxes", "no unavailable axes"),
        new("axes.caveat", "EvidencePacket.Evidence.Caveat", "no caveat"),
        new("graph.path", "EvidencePacket.Graph.RelatedPath", "no traversal path"),
        new("graph.edgeTypes", "EvidencePacket.Graph.EdgeTypes", "edge type unavailable"),
        new("graph.gapMarkers", "EvidencePacket.Graph.GapMarkers", "no gap markers"),
        new("recovery.label", "EvidencePacket.Recovery[].Label", "no recovery action"),
        new("recovery.guidance", "EvidencePacket.Recovery[].Guidance", "no guidance"),
    ];
}
