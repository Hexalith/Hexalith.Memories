// <copyright file="BenchmarkResultComparatorViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

using Hexalith.Memories.Web.Components.Lenses;

/// <summary>
/// Typed, pure projection of an Evidence Packet into the Benchmark Result Comparator lens (AC4).
/// </summary>
/// <remarks>
/// Story 17.4 — produced by <see cref="BenchmarkResultComparatorMapper.Map"/>. NDCG@10, threshold,
/// per-query breakdowns, and reproducible-evidence references render only when the canonical packet
/// exposes benchmark metadata. The axis rows remain retrieval-evidence context, not benchmark scores.
/// </remarks>
/// <param name="ResultState">The explicit, fail-closed benchmark result state.</param>
/// <param name="ResultStateKey">Localization key for the result state label.</param>
/// <param name="AxisRows">Sanitized retrieval-axis evidence rows shown as the proxy.</param>
/// <param name="UnavailableAxes">Sanitized names of degraded or unavailable axes.</param>
/// <param name="NdcgAvailability">Availability of NDCG@10 scores (currently unavailable).</param>
/// <param name="SafeNdcg">Sanitized NDCG@10 summary, or the unavailable fallback.</param>
/// <param name="ThresholdAvailability">Availability of the 80% threshold status (currently unavailable).</param>
/// <param name="SafeThreshold">Sanitized threshold status, or the unavailable fallback.</param>
/// <param name="PerQueryAvailability">Availability of the per-query breakdown (currently unavailable).</param>
/// <param name="SafePerQuery">Sanitized per-query summary, or the unavailable fallback.</param>
/// <param name="EvidenceLinkAvailability">Availability of the reproducible-evidence link (currently unavailable).</param>
/// <param name="SafeEvidenceLink">Sanitized reproducible evidence link, or the unavailable fallback.</param>
/// <param name="ProxyNoteKey">Localization key clarifying the axis rows are a proxy, not a benchmark.</param>
/// <param name="IsEmpty">Whether no axis evidence is available to show as a proxy.</param>
/// <param name="EmptyReasonKey">Localization key shown when no proxy evidence is available.</param>
public sealed record BenchmarkResultComparatorViewModel(
    BenchmarkResultState ResultState,
    string ResultStateKey,
    IReadOnlyList<BenchmarkAxisRow> AxisRows,
    IReadOnlyList<string> UnavailableAxes,
    LensFieldAvailability NdcgAvailability,
    string SafeNdcg,
    LensFieldAvailability ThresholdAvailability,
    string SafeThreshold,
    LensFieldAvailability PerQueryAvailability,
    string SafePerQuery,
    LensFieldAvailability EvidenceLinkAvailability,
    string SafeEvidenceLink,
    string ProxyNoteKey,
    bool IsEmpty,
    string EmptyReasonKey);
