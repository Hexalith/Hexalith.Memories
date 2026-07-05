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
/// Story 17.4 — display-only. Benchmark values render from optional Story 2.7 benchmark metadata when
/// supplied by the canonical Evidence Packet; otherwise they fail closed to unavailable boundaries. The web
/// layer never computes NDCG@10 from retrieval axis evidence.
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
                SafeNdcg: "unavailable",
                ThresholdAvailability: LensFieldAvailability.Unauthorized,
                SafeThreshold: "unavailable",
                PerQueryAvailability: LensFieldAvailability.Unauthorized,
                SafePerQuery: "unavailable",
                EvidenceLinkAvailability: LensFieldAvailability.Unauthorized,
                SafeEvidenceLink: "unavailable",
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

        EvidencePacketBenchmarkEvidence? benchmark = packet.Metadata?.Benchmark;
        BenchmarkResultState state = DetermineState(packet, benchmark);

        return new BenchmarkResultComparatorViewModel(
            ResultState: state,
            ResultStateKey: BenchmarkResourceKeys.ResultState(state),
            AxisRows: rows,
            UnavailableAxes: unavailableAxes,
            NdcgAvailability: HasNdcg(benchmark) ? LensFieldAvailability.Available : LensFieldAvailability.Unavailable,
            SafeNdcg: FormatNdcg(benchmark),
            ThresholdAvailability: HasThreshold(benchmark) ? LensFieldAvailability.Available : LensFieldAvailability.Unavailable,
            SafeThreshold: FormatThreshold(benchmark),
            PerQueryAvailability: benchmark?.PerQuery is { Count: > 0 } ? LensFieldAvailability.Available : LensFieldAvailability.Unavailable,
            SafePerQuery: FormatPerQuery(benchmark),
            EvidenceLinkAvailability: string.IsNullOrWhiteSpace(benchmark?.EvidenceUri)
                ? LensFieldAvailability.Unavailable
                : LensFieldAvailability.Available,
            SafeEvidenceLink: EvidenceDisplay.SafeText(benchmark?.EvidenceUri, "evidence link unavailable"),
            ProxyNoteKey: BenchmarkResourceKeys.ProxyNote,
            IsEmpty: rows.Count == 0,
            EmptyReasonKey: BenchmarkResourceKeys.Empty);
    }

    private static BenchmarkResultState DetermineState(EvidencePacket packet, EvidencePacketBenchmarkEvidence? benchmark)
    {
        if (packet.State == EvidencePacketState.Stale)
        {
            return BenchmarkResultState.Stale;
        }

        if (packet.Evidence.UnavailableAxes.Count > 0 || packet.Evidence.Degraded)
        {
            return BenchmarkResultState.DegradedAxis;
        }

        if (benchmark is not null)
        {
            return benchmark.ThresholdPassed switch
            {
                true => BenchmarkResultState.Passed,
                false => BenchmarkResultState.Regression,
                _ => BenchmarkResultState.Inconclusive,
            };
        }

        return BenchmarkResultState.MissingBaseline;
    }

    private static bool HasNdcg(EvidencePacketBenchmarkEvidence? benchmark)
        => benchmark?.HybridNdcg10.HasValue == true
            || benchmark?.SyntacticNdcg10.HasValue == true
            || benchmark?.SemanticNdcg10.HasValue == true
            || benchmark?.GraphNdcg10.HasValue == true;

    private static bool HasThreshold(EvidencePacketBenchmarkEvidence? benchmark)
        => benchmark?.Threshold.HasValue == true || benchmark?.ThresholdPassed.HasValue == true;

    private static string FormatNdcg(EvidencePacketBenchmarkEvidence? benchmark)
    {
        if (!HasNdcg(benchmark))
        {
            return "NDCG@10 unavailable";
        }

        List<string> parts = [];
        AddScore(parts, "hybrid", benchmark!.HybridNdcg10);
        AddScore(parts, "syntactic", benchmark.SyntacticNdcg10);
        AddScore(parts, "semantic", benchmark.SemanticNdcg10);
        AddScore(parts, "graph", benchmark.GraphNdcg10);
        return EvidenceDisplay.SafeText(string.Join("; ", parts), "NDCG@10 unavailable");
    }

    private static string FormatThreshold(EvidencePacketBenchmarkEvidence? benchmark)
    {
        if (!HasThreshold(benchmark))
        {
            return "threshold unavailable";
        }

        string status = benchmark!.ThresholdPassed switch
        {
            true => "passed",
            false => "failed",
            _ => "inconclusive",
        };

        return benchmark.Threshold.HasValue
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{status} at {benchmark.Threshold.Value:0.###}")
            : status;
    }

    private static string FormatPerQuery(EvidencePacketBenchmarkEvidence? benchmark)
    {
        if (benchmark?.PerQuery is not { Count: > 0 } perQuery)
        {
            return "per-query evidence unavailable";
        }

        return EvidenceDisplay.SafeText(
            string.Join(", ", perQuery.Select(static query => $"{query.QueryId}:{Score(query.HybridNdcg10)}")),
            "per-query evidence unavailable");
    }

    private static void AddScore(List<string> parts, string label, double? value)
    {
        if (value.HasValue && double.IsFinite(value.Value))
        {
            parts.Add($"{label} {Score(value)}");
        }
    }

    private static string Score(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "n/a";
}
