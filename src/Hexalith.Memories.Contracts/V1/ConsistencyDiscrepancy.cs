// <copyright file="ConsistencyDiscrepancy.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// A single memory unit discrepancy surfaced by <c>ConsistencyVerificationWorkflow</c>.
/// The <see cref="Recommendation"/> is derived from the three presence booleans via
/// <c>RepairPlanCalculator</c> and tells the operator (or the repair workflow) what
/// corrective action would converge the unit.
/// </summary>
/// <param name="MemoryUnitId">The memory unit identifier (ULID).</param>
/// <param name="SyntacticPresent">Whether the unit is present in the RediSearch index.</param>
/// <param name="SemanticPresent">Whether the unit is present in Redis Vector.</param>
/// <param name="GraphPresent">Whether the unit is present in FalkorDB.</param>
/// <param name="Recommendation">Repair plan for this unit.</param>
public sealed record ConsistencyDiscrepancy(
    string MemoryUnitId,
    bool SyntacticPresent,
    bool SemanticPresent,
    bool GraphPresent,
    ConsistencyRepairRecommendation Recommendation)
{
    public bool NaturalLanguageSemanticPresent { get; init; }

    public NaturalLanguageEmbeddingStatus NaturalLanguageEmbeddingStatus { get; init; }
        = NaturalLanguageEmbeddingStatus.NotApplicable;

    public string? ConsistencyNote { get; init; }

    /// <summary>Story 9.2 Review D7 — typed identifier for the <see cref="ConsistencyNote"/>.
    /// Consumers pattern-match on this enum rather than parsing the free-form note string.</summary>
    public ConsistencyNoteKind ConsistencyNoteKind { get; init; }
}
