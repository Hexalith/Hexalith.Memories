// <copyright file="FusionEngine.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Pure static fusion function that merges scored results from multiple search axes
/// into a single ranked list using weighted-average composite scoring.
/// No I/O, no injected services, no state — all dependencies passed as parameters.
/// </summary>
internal static class FusionEngine
{
    /// <summary>
    /// Fuses scored results from up to three search axes into a deduplicated, ranked list.
    /// </summary>
    /// <param name="syntacticResults">BM25 search results (raw scores), or null if axis was not queried.</param>
    /// <param name="semanticResults">Vector search results (cosine similarity), or null if axis was not queried.</param>
    /// <param name="graphResults">Graph traversal results (already normalized proximity), or null if axis was not queried.</param>
    /// <param name="weights">The relative weights for each axis.</param>
    /// <param name="documentCount">Tenant corpus document count for BM25 normalization.</param>
    /// <param name="averageDocumentLength">Average document length in bytes for BM25 normalization.</param>
    /// <returns>A sorted list of fused results, descending by composite score, ties broken by MemoryUnitId.</returns>
    internal static IReadOnlyList<FusedScoredResult> Fuse(
        IReadOnlyList<ScoredResult>? syntacticResults,
        IReadOnlyList<ScoredResult>? semanticResults,
        IReadOnlyList<ScoredResult>? graphResults,
        FusionWeights weights,
        int documentCount,
        double averageDocumentLength)
    {
        bool hasSyntactic = syntacticResults is not null;
        bool hasSemantic = semanticResults is not null;
        bool hasGraph = graphResults is not null;

        if (!hasSyntactic && !hasSemantic && !hasGraph)
        {
            return [];
        }

        // Build accumulator keyed by MemoryUnitId
        Dictionary<string, FusionAccumulator> accumulators = new(StringComparer.Ordinal);

        if (hasSyntactic)
        {
            foreach (ScoredResult result in syntacticResults!)
            {
                double normalized = ScoreNormalizer.NormalizeBm25(result.Score, documentCount, averageDocumentLength);
                ref FusionAccumulator acc = ref GetOrAddAccumulator(accumulators, result);
                acc.SyntacticScore = normalized;
            }
        }

        if (hasSemantic)
        {
            foreach (ScoredResult result in semanticResults!)
            {
                double normalized = ScoreNormalizer.NormalizeCosine(result.Score);
                ref FusionAccumulator acc = ref GetOrAddAccumulator(accumulators, result);
                acc.SemanticScore = normalized;
            }
        }

        if (hasGraph)
        {
            foreach (ScoredResult result in graphResults!)
            {
                // Graph scores are already normalized by GraphScopedSearch — clamp to [0,1] and reject non-finite
                double graphScore = result.Score;
                if (!double.IsFinite(graphScore))
                {
                    continue;
                }

                ref FusionAccumulator acc = ref GetOrAddAccumulator(accumulators, result);
                acc.GraphScore = Math.Clamp(graphScore, 0.0, 1.0);
            }
        }

        // Compute composite scores and build result list
        List<FusedScoredResult> fused = new(accumulators.Count);
        foreach (KeyValuePair<string, FusionAccumulator> kvp in accumulators)
        {
            FusionAccumulator acc = kvp.Value;
            double compositeScore = ComputeCompositeScore(acc, weights, hasSyntactic, hasSemantic, hasGraph);

            // All-zero weights for active axes -> skip (no division by zero)
            if (double.IsNaN(compositeScore))
            {
                continue;
            }

            fused.Add(new FusedScoredResult
            {
                MemoryUnitId = kvp.Key,
                CompositeScore = compositeScore,
                ContentSnippet = acc.ContentSnippet,
                SourceUri = acc.SourceUri,
                SourceType = acc.SourceType,
                SyntacticScore = acc.SyntacticScore,
                SemanticScore = acc.SemanticScore,
                GraphScore = acc.GraphScore,
            });
        }

        // Sort descending by CompositeScore, ties broken by MemoryUnitId (lexicographic ascending) for determinism
        fused.Sort((a, b) =>
        {
            int scoreComparison = b.CompositeScore.CompareTo(a.CompositeScore);
            return scoreComparison != 0
                ? scoreComparison
                : string.Compare(a.MemoryUnitId, b.MemoryUnitId, StringComparison.Ordinal);
        });

        return fused;
    }

    private static double ComputeCompositeScore(
        FusionAccumulator acc,
        FusionWeights weights,
        bool hasSyntactic,
        bool hasSemantic,
        bool hasGraph)
    {
        double weightedSum = 0.0;
        double activeWeightSum = 0.0;

        if (hasSyntactic)
        {
            weightedSum += weights.SyntacticWeight * (acc.SyntacticScore ?? 0.0);
            activeWeightSum += weights.SyntacticWeight;
        }

        if (hasSemantic)
        {
            weightedSum += weights.SemanticWeight * (acc.SemanticScore ?? 0.0);
            activeWeightSum += weights.SemanticWeight;
        }

        if (hasGraph)
        {
            weightedSum += weights.GraphWeight * (acc.GraphScore ?? 0.0);
            activeWeightSum += weights.GraphWeight;
        }

        if (activeWeightSum == 0.0)
        {
            return double.NaN;
        }

        return Math.Clamp(weightedSum / activeWeightSum, 0.0, 1.0);
    }

    private static ref FusionAccumulator GetOrAddAccumulator(
        Dictionary<string, FusionAccumulator> accumulators,
        ScoredResult result)
    {
        ref FusionAccumulator? acc = ref System.Runtime.InteropServices.CollectionsMarshal
            .GetValueRefOrAddDefault(accumulators, result.MemoryUnitId, out bool exists);

        if (!exists)
        {
            acc = new FusionAccumulator
            {
                ContentSnippet = result.ContentSnippet,
                SourceUri = result.SourceUri,
                SourceType = result.SourceType,
            };
        }

        return ref acc!;
    }

    /// <summary>Mutable accumulator used during fusion to collect per-axis scores for a single memory unit.</summary>
    private sealed class FusionAccumulator
    {
        public double? SyntacticScore;
        public double? SemanticScore;
        public double? GraphScore;
        public required string ContentSnippet;
        public required string SourceUri;
        public required SourceType SourceType;
    }
}
