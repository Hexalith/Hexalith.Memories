// <copyright file="BenchmarkQueryResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Per-query benchmark result containing NDCG@10 scores for each axis and hybrid fusion.
/// </summary>
public sealed record BenchmarkQueryResult
{
    /// <summary>Gets the unique query identifier (e.g., "BQ-01").</summary>
    public required string QueryId { get; init; }

    /// <summary>Gets the human-readable description of what the query tests.</summary>
    public required string QueryDescription { get; init; }

    /// <summary>Gets the NDCG@10 score for hybrid search.</summary>
    public required double HybridNdcg10 { get; init; }

    /// <summary>Gets the NDCG@10 score for syntactic-only search.</summary>
    public required double SyntacticNdcg10 { get; init; }

    /// <summary>Gets the NDCG@10 score for semantic-only search.</summary>
    public required double SemanticNdcg10 { get; init; }

    /// <summary>Gets the NDCG@10 score for graph-only search (0.0 if graph axis was skipped).</summary>
    public required double GraphNdcg10 { get; init; }

    /// <summary>Gets a value indicating whether the graph axis was executed for this query.</summary>
    public required bool GraphAxisActive { get; init; }

    /// <summary>Gets the fraction of top-3 hybrid results that are in ground truth.</summary>
    public required double HybridPrecisionAt3 { get; init; }

    /// <summary>Gets the highest Precision@3 among active single-axis searches.</summary>
    public required double BestSingleAxisPrecisionAt3 { get; init; }

    /// <summary>
    /// Gets a value indicating whether hybrid outperforms the best active single-axis score.
    /// Skipped axes are excluded from comparison.
    /// </summary>
    public required bool HybridOutperforms { get; init; }
}
