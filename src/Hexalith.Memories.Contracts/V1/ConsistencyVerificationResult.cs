// <copyright file="ConsistencyVerificationResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Aggregate result returned by <c>ConsistencyVerificationWorkflow</c>.
/// The <see cref="Discrepancies"/> list and the <see cref="Notes"/> list each have their own
/// 10,000-entry payload cap (decision S6-D1, re-review 2026-04-25 — informational notes never
/// evict actionable discrepancies). <see cref="TotalDiscrepancyCount"/> stays un-truncated, and
/// likewise <see cref="TotalNoteCount"/>; comparing each total against the corresponding list
/// length tells the operator which list (if any) was truncated. <see cref="TruncatedAt"/> is
/// non-null iff EITHER list was truncated. Operators needing the full actionable list can either
/// re-run repair (which processes all discrepancies regardless of result truncation) or consume
/// structured log EventId 8201.
/// </summary>
/// <param name="TenantId">The tenant audited.</param>
/// <param name="TotalUnits">Total unique memory unit IDs discovered (union of three backends).</param>
/// <param name="ConsistentCount">
/// Units whose three-axis repair recommendation is <c>NoOp</c>. This includes note-only
/// observations routed to <see cref="Notes"/>, because their primary syntactic/semantic/graph
/// state is already converged.
/// </param>
/// <param name="InconsistentCount">Units reported as discrepancies (any non-<c>NoOp</c> recommendation).</param>
/// <param name="Discrepancies">Discrepancy list, truncated to at most 10,000 entries.</param>
/// <param name="TotalDiscrepancyCount">Un-truncated discrepancy count.</param>
/// <param name="TruncatedAt">Timestamp when truncation occurred; <c>null</c> if not truncated.</param>
/// <param name="EnumerationTruncated">
/// <c>true</c> when the 50,000-unit soft cap was exceeded (Task 1.2a). Operators should
/// process the tenant in sharded passes rather than relying on a single verification run.
/// </param>
/// <param name="StartedAt">Verification start timestamp (UTC).</param>
/// <param name="CompletedAt">Verification completion timestamp (UTC).</param>
/// <param name="Duration">Total wall-clock duration.</param>
public sealed record ConsistencyVerificationResult(
    string TenantId,
    int TotalUnits,
    int ConsistentCount,
    int InconsistentCount,
    IReadOnlyList<ConsistencyDiscrepancy> Discrepancies,
    int TotalDiscrepancyCount,
    DateTimeOffset? TruncatedAt,
    bool EnumerationTruncated,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration)
{
    /// <summary>
    /// In-payload count of note-only observations — equals <c>Notes.Count</c> by construction
    /// (mirrors the relationship between <see cref="Discrepancies"/> and
    /// <see cref="TotalDiscrepancyCount"/>). When the notes list was truncated by the 10,000-entry
    /// cap, <see cref="TotalNoteCount"/> stays un-truncated and exceeds <c>NoteCount</c>.
    /// </summary>
    public int NoteCount { get; init; }

    /// <summary>
    /// Un-truncated total count of note-only observations. Compare against <see cref="NoteCount"/>
    /// (or equivalently <c>Notes.Count</c>) to detect truncation of the notes payload.
    /// </summary>
    public int TotalNoteCount { get; init; }

    /// <summary>Story 9.2 review D7 (committed-branch review 2026-04-24) — structural split
    /// between units requiring repair (<see cref="Discrepancies"/>) and informational observations
    /// (this list). Previously, NL-only gaps (<c>Recommendation = NoOp</c> +
    /// <c>ConsistencyNoteKind = NaturalLanguageEmbeddingMissing</c>) were mixed into the
    /// <see cref="Discrepancies"/> list, forcing consumers to double-filter on both fields.
    /// Now: units where the three-axis recommendation is <c>NoOp</c> but a
    /// <see cref="ConsistencyNoteKind"/> note is present are routed here instead. Consumers
    /// filtering <c>Discrepancies.Where(d =&gt; d.Recommendation != NoOp)</c> no longer miss real NL
    /// gaps, because the NL gap is either (a) here in <see cref="Notes"/> when it is the ONLY
    /// issue, or (b) in <see cref="Discrepancies"/> riding alongside a real repair recommendation
    /// when other axes are also affected. This list has its own independent 10,000-entry payload
    /// cap (decision S6-D1, re-review 2026-04-25); compare <see cref="NoteCount"/> against
    /// <see cref="TotalNoteCount"/> to detect truncation. S6-P4 (re-review 2026-04-25): the
    /// init accessor coalesces null assignments to the empty list so a malformed payload (e.g.,
    /// <c>"notes": null</c>) cannot NRE downstream consumers.</summary>
    public IReadOnlyList<ConsistencyDiscrepancy> Notes
    {
        get => field ?? [];
        init => field = value ?? [];
    }
}
