// <copyright file="NdcgScorer.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Scoring;

/// <summary>
/// Pure static functions for computing information retrieval metrics.
/// No I/O, no state — all data passed as parameters. Deterministic output for identical inputs.
/// </summary>
internal static class NdcgScorer
{
    /// <summary>
    /// Computes NDCG (Normalized Discounted Cumulative Gain) at rank k using binary relevance.
    /// </summary>
    /// <param name="rankedResults">Ordered list of result IDs (best first).</param>
    /// <param name="groundTruth">Ordered list of relevant document IDs (ideal ranking).</param>
    /// <param name="k">Rank cutoff (default 10).</param>
    /// <returns>NDCG@k in [0.0, 1.0]. Returns 0.0 if ground truth is empty or IDCG is 0.</returns>
    internal static double ComputeNdcg(IReadOnlyList<string> rankedResults, IReadOnlyList<string> groundTruth, int k = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (groundTruth.Count == 0)
        {
            return 0.0;
        }

        HashSet<string> relevantSet = new(groundTruth, StringComparer.Ordinal);
        double dcg = ComputeDcg(rankedResults, relevantSet, k);
        double idcg = ComputeIdealDcg(relevantSet.Count, k);

        return idcg == 0.0 ? 0.0 : dcg / idcg;
    }

    /// <summary>
    /// Computes Precision at rank k — the fraction of top-k results that appear in ground truth.
    /// </summary>
    /// <param name="rankedResults">Ordered list of result IDs (best first).</param>
    /// <param name="groundTruth">List of relevant document IDs.</param>
    /// <param name="k">Rank cutoff (default 3).</param>
    /// <returns>Precision@k in [0.0, 1.0]. If rankedResults has fewer than k items, divides by actual count.</returns>
    internal static double ComputePrecisionAtK(IReadOnlyList<string> rankedResults, IReadOnlyList<string> groundTruth, int k = 3)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (rankedResults.Count == 0 || groundTruth.Count == 0)
        {
            return 0.0;
        }

        HashSet<string> relevantSet = new(groundTruth, StringComparer.Ordinal);
        int limit = Math.Min(k, rankedResults.Count);
        int hits = 0;

        for (int i = 0; i < limit; i++)
        {
            if (relevantSet.Contains(rankedResults[i]))
            {
                hits++;
            }
        }

        return (double)hits / limit;
    }

    /// <summary>Computes DCG@k using binary relevance: rel(i) = 1 if in relevant set, else 0.</summary>
    private static double ComputeDcg(IReadOnlyList<string> rankedResults, HashSet<string> relevantSet, int k)
    {
        double dcg = 0.0;
        int limit = Math.Min(k, rankedResults.Count);

        for (int i = 0; i < limit; i++)
        {
            if (relevantSet.Contains(rankedResults[i]))
            {
                // DCG formula: relevance / log2(rank + 2), where rank is 0-based
                // So position 0 → log2(2) = 1.0, position 1 → log2(3) ≈ 1.585, etc.
                dcg += 1.0 / Math.Log2(i + 2);
            }
        }

        return dcg;
    }

    /// <summary>Computes the ideal DCG@k — all relevant docs ranked first, up to k.</summary>
    private static double ComputeIdealDcg(int relevantCount, int k)
    {
        double idcg = 0.0;
        int limit = Math.Min(k, relevantCount);

        for (int i = 0; i < limit; i++)
        {
            idcg += 1.0 / Math.Log2(i + 2);
        }

        return idcg;
    }
}
