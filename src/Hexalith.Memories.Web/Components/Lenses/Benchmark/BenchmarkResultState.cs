// <copyright file="BenchmarkResultState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

/// <summary>
/// Explicit benchmark result state.
/// </summary>
/// <remarks>
/// Story 17.4 — benchmark result states are emitted only from canonical Evidence Packet benchmark metadata
/// or fail-closed availability states. The web layer never computes NDCG@10 from retrieval evidence.
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

    /// <summary>The benchmark passed the configured thesis threshold.</summary>
    Passed,

    /// <summary>Benchmark comparison would be inconclusive (requires canonical fixtures).</summary>
    Inconclusive,

    /// <summary>A regression versus baseline (requires canonical fixtures).</summary>
    Regression,

    /// <summary>The evidence is not reproducible (requires canonical fixtures).</summary>
    Unreproducible,
}
