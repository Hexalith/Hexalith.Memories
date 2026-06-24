// <copyright file="BenchmarkResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Lenses.Benchmark;

/// <summary>Stable localization key conventions for the Benchmark Result Comparator lens (AC4).</summary>
public static class BenchmarkResourceKeys
{
    /// <summary>Accessible label for the benchmark comparator region.</summary>
    public const string RegionLabel = "Benchmark_Region_Label";

    /// <summary>Label preceding the NDCG@10 result.</summary>
    public const string NdcgLabel = "Benchmark_Ndcg_Label";

    /// <summary>Label preceding the 80% thesis threshold status.</summary>
    public const string ThresholdLabel = "Benchmark_Threshold_Label";

    /// <summary>Label preceding the per-query breakdown.</summary>
    public const string PerQueryLabel = "Benchmark_PerQuery_Label";

    /// <summary>Label preceding the reproducible-evidence link.</summary>
    public const string EvidenceLinkLabel = "Benchmark_EvidenceLink_Label";

    /// <summary>Heading for the retrieval-axis evidence proxy table.</summary>
    public const string AxisEvidenceLabel = "Benchmark_AxisEvidence_Label";

    /// <summary>Note clarifying that the axis rows are a proxy, not a benchmark NDCG result.</summary>
    public const string ProxyNote = "Benchmark_Proxy_Note";

    /// <summary>Column header / accessible prefix for the result state.</summary>
    public const string ResultStateLabel = "Benchmark_ResultState_Label";

    /// <summary>Column header / accessible prefix for an axis name.</summary>
    public const string AxisLabel = "Benchmark_Axis_Label";

    /// <summary>Column header / accessible prefix for an axis score.</summary>
    public const string ScoreLabel = "Benchmark_Score_Label";

    /// <summary>Column header / accessible prefix for the normalization method.</summary>
    public const string NormalizationLabel = "Benchmark_Normalization_Label";

    /// <summary>Label preceding the unavailable axes list.</summary>
    public const string UnavailableAxesLabel = "Benchmark_UnavailableAxes_Label";

    /// <summary>Shown when no proxy axis evidence is available.</summary>
    public const string Empty = "Benchmark_Empty";

    /// <summary>Builds the result-state label key.</summary>
    /// <param name="state">The benchmark result state.</param>
    /// <returns>The localization key.</returns>
    public static string ResultState(BenchmarkResultState state) => $"Benchmark_State_{state}";
}
