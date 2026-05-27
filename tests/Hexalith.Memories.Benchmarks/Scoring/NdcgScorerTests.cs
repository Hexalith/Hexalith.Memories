// <copyright file="NdcgScorerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Scoring;

using Shouldly;

public class NdcgScorerTests
{
    // --- ComputeNdcg tests ---

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_PerfectRanking_Returns1()
    {
        // All relevant docs in perfect order
        string[] ranked = ["a", "b", "c", "d", "e"];
        string[] groundTruth = ["a", "b", "c", "d", "e"];

        double ndcg = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        ndcg.ShouldBe(1.0, tolerance: 0.001);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_CompletelyIrrelevant_Returns0()
    {
        string[] ranked = ["x", "y", "z"];
        string[] groundTruth = ["a", "b", "c"];

        double ndcg = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        ndcg.ShouldBe(0.0);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_PartialMatches_ReturnsBetween0And1()
    {
        // Ranked: [a, x, b, y, c] — 3 relevant out of 5, not in ideal order
        string[] ranked = ["a", "x", "b", "y", "c"];
        string[] groundTruth = ["a", "b", "c"];

        double ndcg = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        // DCG = 1/log2(2) + 0 + 1/log2(4) + 0 + 1/log2(6) = 1.0 + 0 + 0.5 + 0 + 0.3869 = 1.8869
        // IDCG = 1/log2(2) + 1/log2(3) + 1/log2(4) = 1.0 + 0.6309 + 0.5 = 2.1309
        // NDCG = 1.8869 / 2.1309 ≈ 0.8855
        ndcg.ShouldBeGreaterThan(0.0);
        ndcg.ShouldBeLessThan(1.0);
        ndcg.ShouldBe(0.8855, tolerance: 0.01);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_FewerResultsThanK_HandlesGracefully()
    {
        string[] ranked = ["a", "b"]; // Only 2 results but k=10
        string[] groundTruth = ["a", "b", "c", "d", "e"];

        double ndcg = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        // DCG = 1/log2(2) + 1/log2(3) = 1.0 + 0.6309 = 1.6309
        // IDCG with 5 relevant, k=10: 1/log2(2) + 1/log2(3) + 1/log2(4) + 1/log2(5) + 1/log2(6) = 1.0 + 0.6309 + 0.5 + 0.4307 + 0.3869 = 2.9485
        // NDCG = 1.6309 / 2.9485 ≈ 0.5531
        ndcg.ShouldBeGreaterThan(0.0);
        ndcg.ShouldBeLessThan(1.0);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_EmptyGroundTruth_Returns0()
    {
        string[] ranked = ["a", "b", "c"];
        string[] groundTruth = [];

        double ndcg = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        ndcg.ShouldBe(0.0);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputeNdcg_Deterministic_SameInputsSameOutput()
    {
        string[] ranked = ["a", "x", "b", "y", "c"];
        string[] groundTruth = ["a", "b", "c"];

        double first = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);

        for (int i = 0; i < 10; i++)
        {
            double result = NdcgScorer.ComputeNdcg(ranked, groundTruth, k: 10);
            result.ShouldBe(first);
        }
    }

    // --- ComputePrecisionAtK tests ---

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputePrecisionAtK_AllTopKRelevant_Returns1()
    {
        string[] ranked = ["a", "b", "c", "x", "y"];
        string[] groundTruth = ["a", "b", "c"];

        double precision = NdcgScorer.ComputePrecisionAtK(ranked, groundTruth, k: 3);

        precision.ShouldBe(1.0);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputePrecisionAtK_NoTopKRelevant_Returns0()
    {
        string[] ranked = ["x", "y", "z", "a", "b"];
        string[] groundTruth = ["a", "b", "c"];

        double precision = NdcgScorer.ComputePrecisionAtK(ranked, groundTruth, k: 3);

        precision.ShouldBe(0.0);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputePrecisionAtK_TwoOfThreeRelevant_ReturnsTwoThirds()
    {
        string[] ranked = ["a", "x", "b"];
        string[] groundTruth = ["a", "b", "c"];

        double precision = NdcgScorer.ComputePrecisionAtK(ranked, groundTruth, k: 3);

        precision.ShouldBe(2.0 / 3.0, tolerance: 0.001);
    }

    [Fact]
    [Trait("Category", "Benchmark")]
    public void ComputePrecisionAtK_FewerResultsThanK_DividesByActualCount()
    {
        string[] ranked = ["a", "b"]; // Only 2 results, k=3
        string[] groundTruth = ["a", "b", "c"];

        double precision = NdcgScorer.ComputePrecisionAtK(ranked, groundTruth, k: 3);

        // 2 relevant out of 2 actual = 1.0
        precision.ShouldBe(1.0);
    }
}
