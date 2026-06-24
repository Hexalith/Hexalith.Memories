// <copyright file="BenchmarkResultState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

/// <summary>
/// Explicit benchmark result state.
/// </summary>
/// <remarks>
/// Story 17.4 — the canonical Evidence Packet exposes no benchmark NDCG@10 scores, thesis threshold,
/// per-query breakdown, or reproducible-evidence reference, so the comparator never computes or infers a
/// benchmark result in the web layer. It fails closed to <see cref="Unavailable"/> or
/// <see cref="MissingBaseline"/> and surfaces only the retrieval axis evidence the contract does expose, as
/// an explicitly labelled proxy. Regression, inconclusive, and unreproducible states are represented for
/// completeness but require canonical benchmark fixtures from Story 2.7 before they can be emitted.
/// </remarks>
public enum BenchmarkResultState
{
    /// <summary>No benchmark evidence is exposed by the contract; nothing is inferred.</summary>
    Unavailable = 0,

    /// <summary>No benchmark baseline is exposed by the contract to compare against.</summary>
    MissingBaseline,

    /// <summary>One or more retrieval axes were degraded or unavailable in the evidence.</summary>
    DegradedAxis,

    /// <summary>The underlying evidence may be stale.</summary>
    Stale,

    /// <summary>Benchmark comparison would be inconclusive (requires canonical fixtures).</summary>
    Inconclusive,

    /// <summary>A regression versus baseline (requires canonical fixtures).</summary>
    Regression,

    /// <summary>The evidence is not reproducible (requires canonical fixtures).</summary>
    Unreproducible,
}
