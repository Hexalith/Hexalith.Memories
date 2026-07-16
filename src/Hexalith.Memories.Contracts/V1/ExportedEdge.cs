// <copyright file="ExportedEdge.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Portable representation of a graph edge for data export (Story 8.3). Ships as a strict superset
/// of <see cref="GraphEdge"/> — it carries the confidence-promotion audit trail
/// (<see cref="VerifiedBy"/>, <see cref="PreviousConfidence"/>) introduced by Story 4.3 so edge history
/// round-trips through re-import.
/// </summary>
/// <param name="Id">
/// FalkorDB edge identifier (scoped to the current graph instance). Stable within a single graph
/// lifetime; NOT stable across graph deletions or recreations. Re-import MUST NOT use this value as
/// edge identity — reconstruct edges from the <c>(SourceId, TargetId, EdgeType, CreatedAt)</c> tuple.
/// </param>
/// <param name="SourceId">Source memory unit identifier.</param>
/// <param name="TargetId">Target memory unit identifier.</param>
/// <param name="EdgeType">Edge type name (camelCase string, e.g. <c>causedBy</c>, <c>supports</c>).</param>
/// <param name="Confidence">Edge confidence in the range [0, 1].</param>
/// <param name="Origin">Origin of the edge (for example, <c>human</c>, <c>inferred</c>).</param>
/// <param name="CreatedAt">When the edge was created.</param>
/// <param name="VerifiedBy">Identity of the operator who promoted the edge confidence (Story 4.3); <see langword="null"/> if never promoted.</param>
/// <param name="PreviousConfidence">Previous confidence value before the most recent promotion; <see langword="null"/> if never promoted.</param>
public sealed record ExportedEdge(
    string Id,
    string SourceId,
    string TargetId,
    string EdgeType,
    float Confidence,
    string Origin,
    DateTimeOffset CreatedAt,
    string? VerifiedBy,
    float? PreviousConfidence);
