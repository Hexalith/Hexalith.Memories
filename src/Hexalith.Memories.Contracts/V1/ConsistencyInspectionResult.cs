// <copyright file="ConsistencyInspectionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Result of a synchronous per-unit inspection via
/// <c>GET /api/v1/tenants/{tenantId}/consistency/inspect/{memoryUnitId}</c>.
/// Returned only when the unit is present on at least one retrieval axis; otherwise
/// the endpoint returns 404 with an <see cref="ErrorResponse"/>.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit identifier (ULID).</param>
/// <param name="SyntacticPresent">Whether the unit is present on the syntactic axis.</param>
/// <param name="SemanticPresent">Whether the unit is present on the semantic axis.</param>
/// <param name="GraphPresent">Whether the unit is present on the graph axis.</param>
/// <param name="SyntacticDetail">Syntactic-axis detail; <c>null</c> if absent.</param>
/// <param name="SemanticDetail">Semantic-axis detail; <c>null</c> if absent.</param>
/// <param name="GraphDetail">Graph-axis detail; <c>null</c> if absent.</param>
/// <param name="Recommendation">Repair recommendation (<c>NoOp</c> when fully consistent).</param>
/// <param name="CheckedAt">Timestamp of the probe (UTC).</param>
public sealed record ConsistencyInspectionResult(
    string TenantId,
    string MemoryUnitId,
    bool SyntacticPresent,
    bool SemanticPresent,
    bool GraphPresent,
    ConsistencySyntacticDetail? SyntacticDetail,
    ConsistencySemanticDetail? SemanticDetail,
    ConsistencyGraphDetail? GraphDetail,
    ConsistencyRepairRecommendation Recommendation,
    DateTimeOffset CheckedAt)
{
    public bool NaturalLanguageSemanticPresent { get; init; }

    public ConsistencySemanticDetail? NaturalLanguageSemanticDetail { get; init; }

    public NaturalLanguageEmbeddingStatus NaturalLanguageEmbeddingStatus { get; init; }
        = NaturalLanguageEmbeddingStatus.NotApplicable;

    public string? ConsistencyNote { get; init; }

    /// <summary>Story 9.2 Review D7 — typed identifier for the <see cref="ConsistencyNote"/>.
    /// Consumers pattern-match on this enum rather than parsing the free-form note string.</summary>
    public ConsistencyNoteKind ConsistencyNoteKind { get; init; }
}
