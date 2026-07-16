// <copyright file="EvidencePacketViewMapping.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Evidence;

using Hexalith.Memories.Contracts.V1;

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
        new("scope.tenant", "EvidencePacket.Scope.TenantId", EvidenceResourceKeys.UnknownTenant),
        new("scope.case", "EvidencePacket.Scope.CaseId", EvidenceResourceKeys.TenantScope),
        new("scope.isolation", "EvidencePacket.Scope.IsolationStatus", EvidenceResourceKeys.Isolation(EvidencePacketIsolationStatus.Unknown)),
        new("trust.confidence", "EvidencePacket.Evidence.EvidenceStrength", EvidenceResourceKeys.Strength(EvidencePacketEvidenceStrength.None)),
        new("trust.freshness", "EvidencePacket.Metadata.Freshness / EvidencePacket.Sources[].Freshness", EvidenceResourceKeys.FreshnessUnavailable),
        new("trust.sourceCount", "EvidencePacket.Sources.Count", EvidenceResourceKeys.SourceCountUnavailable),
        new("trust.evidenceHealth", "EvidencePacket.State", EvidenceResourceKeys.State(EvidencePacketState.Empty)),
        new("trust.tokenBudget", "EvidencePacket.OmittedDetails.Reason", EvidenceResourceKeys.TokenBudgetWithin),
        new("result.query", "EvidencePacket.Result.Query", EvidenceResourceKeys.NoQuery),
        new("result.summary", "EvidencePacket.Result.Summary", EvidenceResourceKeys.SummaryUnavailable),
        new("sources.originIdentifier", "EvidencePacket.Sources[].SourceUri", EvidenceResourceKeys.SourceUnavailable),
        new("sources.type", "EvidencePacket.Sources[].SourceType", EvidenceResourceKeys.UnknownSourceType),
        new("sources.snippet", "EvidencePacket.Sources[].Snippet", EvidenceResourceKeys.SnippetUnavailable),
        new("sources.memoryUnit", "EvidencePacket.Sources[].MemoryUnitId", EvidenceResourceKeys.MemoryUnitUnavailable),
        new("sources.rank", "EvidencePacket.Sources[].Rank", EvidenceResourceKeys.Unavailable),
        new("sources.score", "EvidencePacket.Sources[].Score", EvidenceResourceKeys.ScoreUnavailable),
        new("sources.timestamp", "EvidencePacket.Sources[].Timestamp", EvidenceResourceKeys.TimestampUnavailable),
        new("sources.freshness", "EvidencePacket.Sources[].Freshness", EvidenceResourceKeys.FreshnessUnavailable),
        new("axes.axis", "EvidencePacket.Evidence.AxisEvidence[].Axis", EvidenceResourceKeys.AxisUnavailable),
        new("axes.normalizedScore", "EvidencePacket.Evidence.AxisEvidence[].Score", EvidenceResourceKeys.ScoreUnavailable),
        new("axes.rankingReason", "EvidencePacket.Evidence.AxisEvidence[].Description", EvidenceResourceKeys.RankingReasonUnavailable),
        new("axes.normalizationMethod", "EvidencePacket.Evidence.AxisEvidence[].NormalizationMethod", EvidenceResourceKeys.NormalizationUnavailable),
        new("axes.unavailableAxes", "EvidencePacket.Evidence.UnavailableAxes", EvidenceResourceKeys.NoUnavailableAxes),
        new("axes.caveat", "EvidencePacket.Evidence.Caveat", EvidenceResourceKeys.NoCaveat),
        new("graph.path", "EvidencePacket.Graph.RelatedPath", EvidenceResourceKeys.NoTraversalPath),
        new("graph.edgeTypes", "EvidencePacket.Graph.EdgeTypes", EvidenceResourceKeys.EdgeTypeUnavailable),
        new("graph.gapMarkers", "EvidencePacket.Graph.GapMarkers", EvidenceResourceKeys.NoGapMarkers),
        new("recovery.label", "EvidencePacket.Recovery[].Label", EvidenceResourceKeys.Unavailable),
        new("recovery.guidance", "EvidencePacket.Recovery[].Guidance", EvidenceResourceKeys.Unavailable),
    ];
}
