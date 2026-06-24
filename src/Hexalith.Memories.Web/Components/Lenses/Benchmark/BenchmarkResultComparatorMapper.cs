// <copyright file="BenchmarkResultComparatorMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// Pure, deterministic projection of a canonical Evidence Packet into the Benchmark Result Comparator (AC4).
/// </summary>
/// <remarks>
/// Story 17.4 — display-only. Benchmark NDCG@10 scores, the 80% thesis threshold, per-query breakdowns,
/// corpus/run identifiers, and reproducible-evidence references are not exposed by the canonical contract,
/// so they always render the unavailable boundary and are never computed or inferred in the web layer. The
/// only benchmark-adjacent evidence the contract exposes is per-axis retrieval relevance, surfaced here as
/// an explicitly labelled proxy. The role parameter changes only the shared shell density, not the
/// benchmark rows, which preserves contract-provided ordering.
/// </remarks>
public static class BenchmarkResultComparatorMapper
{
    /// <summary>Maps a packet into the Benchmark Result Comparator view model.</summary>
    /// <param name="packet">The canonical Evidence Packet.</param>
    /// <param name="role">The active role-density profile (affects the shared shell only).</param>
    /// <returns>The typed, sanitized benchmark comparator.</returns>
    public static BenchmarkResultComparatorViewModel Map(EvidencePacket packet, LensRole role)
    {
        ArgumentNullException.ThrowIfNull(packet);
        _ = role;

        bool restrictive = EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized;

        if (restrictive)
        {
            // A restrictive scope suppresses all benchmark and axis detail; nothing is revealed.
            return new BenchmarkResultComparatorViewModel(
                ResultState: BenchmarkResultState.Unavailable,
                ResultStateKey: BenchmarkResourceKeys.ResultState(BenchmarkResultState.Unavailable),
                AxisRows: [],
                UnavailableAxes: [],
                NdcgAvailability: LensFieldAvailability.Unauthorized,
                ThresholdAvailability: LensFieldAvailability.Unauthorized,
                PerQueryAvailability: LensFieldAvailability.Unauthorized,
                EvidenceLinkAvailability: LensFieldAvailability.Unauthorized,
                ProxyNoteKey: BenchmarkResourceKeys.ProxyNote,
                IsEmpty: true,
                EmptyReasonKey: BenchmarkResourceKeys.Empty);
        }

        List<BenchmarkAxisRow> rows = [];
        foreach (EvidencePacketAxisEvidence axis in packet.Evidence.AxisEvidence)
        {
            bool hasScore = axis.Score.HasValue && double.IsFinite(axis.Score.Value);
            int percent = hasScore
                ? Math.Clamp((int)Math.Round(axis.Score!.Value * 100, MidpointRounding.AwayFromZero), 0, 100)
                : 0;
            rows.Add(new BenchmarkAxisRow(
                Axis: EvidenceDisplay.SafeText(axis.Axis, "axis unavailable"),
                SafeScore: EvidenceDisplay.ScoreLabel(axis.Score),
                ScorePercent: percent,
                HasScore: hasScore,
                SafeNormalization: EvidenceDisplay.SafeText(axis.NormalizationMethod, "normalization unavailable"),
                SafeDescription: EvidenceDisplay.SafeText(axis.Description, "ranking reason unavailable")));
        }

        List<string> unavailableAxes = [];
        foreach (string axis in packet.Evidence.UnavailableAxes)
        {
            unavailableAxes.Add(EvidenceDisplay.SafeText(axis, "axis"));
        }

        // Fail closed: with no benchmark baseline in the contract, the result state is MissingBaseline
        // unless degraded axes or staleness give a more specific (still non-NDCG) explanation.
        BenchmarkResultState state = packet.State == EvidencePacketState.Stale
            ? BenchmarkResultState.Stale
            : packet.Evidence.UnavailableAxes.Count > 0 || packet.Evidence.Degraded
                ? BenchmarkResultState.DegradedAxis
                : BenchmarkResultState.MissingBaseline;

        return new BenchmarkResultComparatorViewModel(
            ResultState: state,
            ResultStateKey: BenchmarkResourceKeys.ResultState(state),
            AxisRows: rows,
            UnavailableAxes: unavailableAxes,
            NdcgAvailability: LensFieldAvailability.Unavailable,
            ThresholdAvailability: LensFieldAvailability.Unavailable,
            PerQueryAvailability: LensFieldAvailability.Unavailable,
            EvidenceLinkAvailability: LensFieldAvailability.Unavailable,
            ProxyNoteKey: BenchmarkResourceKeys.ProxyNote,
            IsEmpty: rows.Count == 0,
            EmptyReasonKey: BenchmarkResourceKeys.Empty);
    }
}
