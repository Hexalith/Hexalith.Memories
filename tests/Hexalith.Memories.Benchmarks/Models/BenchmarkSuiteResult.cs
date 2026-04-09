// <copyright file="BenchmarkSuiteResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Models;

/// <summary>
/// Overall benchmark suite result containing per-query results and thesis validation outcome.
/// </summary>
public sealed record BenchmarkSuiteResult
{
    /// <summary>Gets the per-query benchmark results.</summary>
    public required IReadOnlyList<BenchmarkQueryResult> QueryResults { get; init; }

    /// <summary>Gets the total number of benchmark queries.</summary>
    public required int TotalQueries { get; init; }

    /// <summary>Gets the count of queries where hybrid outperforms all active single-axis searches.</summary>
    public required int HybridWins { get; init; }

    /// <summary>Gets the hybrid win rate (HybridWins / TotalQueries).</summary>
    public required double HybridWinRate { get; init; }

    /// <summary>Gets a value indicating whether the thesis is validated (HybridWinRate >= 0.80).</summary>
    public required bool ThesisValidated { get; init; }

    /// <summary>Gets the timestamp when the suite was executed.</summary>
    public required DateTimeOffset RunTimestamp { get; init; }

    /// <summary>Gets the standard caveat message about synthetic vectors.</summary>
    public required string Caveat { get; init; }
}
