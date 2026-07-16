// <copyright file="FusionEngine.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Pure static fusion function that merges scored results from multiple search axes
/// into a single ranked list using weighted reciprocal rank fusion.
/// No I/O, no injected services, no state — all dependencies passed as parameters.
/// </summary>
internal static class FusionEngine
{
    private const int RrfRankConstant = 60;
    private const int SyntacticAxis = 0;
    private const int SemanticAxis = 1;
    private const int GraphAxis = 2;
    private const int NlAxis = 3;

    /// <summary>
    /// Fuses scored results from up to three legacy search axes into a deduplicated, ranked list.
    /// </summary>
    /// <param name="syntacticResults">BM25 search results (raw scores), or null if axis was not queried.</param>
    /// <param name="semanticResults">Vector search results (cosine similarity), or null if axis was not queried.</param>
    /// <param name="graphResults">Graph traversal results (already normalized proximity), or null if axis was not queried.</param>
    /// <param name="weights">The relative weights for each axis.</param>
    /// <param name="documentCount">Ignored for RRF; retained for internal call-site compatibility.</param>
    /// <param name="averageDocumentLength">Ignored for RRF; retained for internal call-site compatibility.</param>
    /// <returns>A sorted list of fused results, descending by composite score, ties broken by MemoryUnitId.</returns>
    internal static IReadOnlyList<FusedScoredResult> Fuse(
        IReadOnlyList<ScoredResult>? syntacticResults,
        IReadOnlyList<ScoredResult>? semanticResults,
        IReadOnlyList<ScoredResult>? graphResults,
        FusionWeights weights,
        int documentCount,
        double averageDocumentLength)
        => Fuse(syntacticResults, semanticResults, graphResults, null, weights, documentCount, averageDocumentLength);

    /// <summary>
    /// Fuses scored results from up to four search axes into a deduplicated, ranked list.
    /// </summary>
    /// <param name="syntacticResults">BM25 search results (raw scores), or null if axis was not queried.</param>
    /// <param name="semanticResults">Vector search results (cosine similarity), or null if axis was not queried.</param>
    /// <param name="graphResults">Graph traversal results (already normalized proximity), or null if axis was not queried.</param>
    /// <param name="nlResults">Natural-language semantic results, or null if axis was not queried.</param>
    /// <param name="weights">The relative weights for each axis.</param>
    /// <param name="documentCount">Ignored for RRF; retained for internal call-site compatibility.</param>
    /// <param name="averageDocumentLength">Ignored for RRF; retained for internal call-site compatibility.</param>
    /// <returns>A sorted list of fused results, descending by composite score, ties broken by MemoryUnitId.</returns>
    internal static IReadOnlyList<FusedScoredResult> Fuse(
        IReadOnlyList<ScoredResult>? syntacticResults,
        IReadOnlyList<ScoredResult>? semanticResults,
        IReadOnlyList<ScoredResult>? graphResults,
        IReadOnlyList<ScoredResult>? nlResults,
        FusionWeights weights,
        int documentCount,
        double averageDocumentLength)
    {
        bool hasSyntactic = syntacticResults is { Count: > 0 };
        bool hasSemantic = semanticResults is { Count: > 0 };
        bool hasGraph = graphResults is { Count: > 0 };
        bool hasNl = nlResults is { Count: > 0 } && weights.NlWeight > 0.0;

        if (!hasSyntactic && !hasSemantic && !hasGraph && !hasNl)
        {
            return [];
        }

        // Build accumulator keyed by MemoryUnitId
        Dictionary<string, FusionAccumulator> accumulators = new(StringComparer.Ordinal);

        if (hasSyntactic)
        {
            AccumulateAxis(accumulators, syntacticResults!, SyntacticAxis);
        }

        if (hasSemantic)
        {
            AccumulateAxis(accumulators, semanticResults!, SemanticAxis);
        }

        if (hasGraph)
        {
            AccumulateAxis(accumulators, graphResults!, GraphAxis);
        }

        if (hasNl)
        {
            AccumulateAxis(accumulators, nlResults!, NlAxis);
        }

        // Compute composite scores and build result list
        List<FusedScoredResult> fused = new(accumulators.Count);
        foreach (KeyValuePair<string, FusionAccumulator> kvp in accumulators)
        {
            FusionAccumulator acc = kvp.Value;
            double compositeScore = ComputeCompositeScore(acc, weights, hasSyntactic, hasSemantic, hasGraph, hasNl);

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
                NlScore = acc.NlScore,
                CaseId = acc.CaseId,
                CaseName = acc.CaseName,
                AnnotationsCount = acc.AnnotationsCount,
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

    private static void AccumulateAxis(
        Dictionary<string, FusionAccumulator> accumulators,
        IReadOnlyList<ScoredResult> results,
        int axis)
    {
        int currentRank = 0;
        for (int i = 0; i < results.Count; i++)
        {
            ScoredResult result = results[i];
            if (i == 0 || !ScoresTie(results[i - 1].Score, result.Score))
            {
                currentRank = i + 1;
            }

            double contribution = ComputeRankContribution(currentRank);
            ref FusionAccumulator acc = ref GetOrAddAccumulator(accumulators, result);

            switch (axis)
            {
                case SyntacticAxis when acc.SyntacticScore is null:
                    acc.SyntacticScore = contribution;
                    break;
                case SemanticAxis when acc.SemanticScore is null:
                    acc.SemanticScore = contribution;
                    break;
                case GraphAxis when acc.GraphScore is null:
                    acc.GraphScore = contribution;
                    break;
                case NlAxis when acc.NlScore is null:
                    acc.NlScore = contribution;
                    break;
            }

            MergeAttribution(acc, result);
        }
    }

    private static double ComputeCompositeScore(
        FusionAccumulator acc,
        FusionWeights weights,
        bool hasSyntactic,
        bool hasSemantic,
        bool hasGraph,
        bool hasNl)
    {
        double weightedSum = 0.0;
        double maximumWeightedSum = 0.0;
        double topRankContribution = ComputeRawRrfContribution(1);

        if (hasSyntactic)
        {
            weightedSum += weights.SyntacticWeight * ConvertContributionToRawRrf(acc.SyntacticScore);
            maximumWeightedSum += weights.SyntacticWeight * topRankContribution;
        }

        if (hasSemantic)
        {
            weightedSum += weights.SemanticWeight * ConvertContributionToRawRrf(acc.SemanticScore);
            maximumWeightedSum += weights.SemanticWeight * topRankContribution;
        }

        if (hasGraph)
        {
            weightedSum += weights.GraphWeight * ConvertContributionToRawRrf(acc.GraphScore);
            maximumWeightedSum += weights.GraphWeight * topRankContribution;
        }

        if (hasNl)
        {
            weightedSum += weights.NlWeight * ConvertContributionToRawRrf(acc.NlScore);
            maximumWeightedSum += weights.NlWeight * topRankContribution;
        }

        if (maximumWeightedSum == 0.0)
        {
            return double.NaN;
        }

        return Math.Clamp(weightedSum / maximumWeightedSum, 0.0, 1.0);
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

    private static double ComputeRankContribution(int rank)
    {
        double topRankContribution = ComputeRawRrfContribution(1);
        return Math.Clamp(ComputeRawRrfContribution(rank) / topRankContribution, 0.0, 1.0);
    }

    private static double ComputeRawRrfContribution(int rank)
        => 1.0 / (RrfRankConstant + Math.Max(rank, 1));

    private static double ConvertContributionToRawRrf(double? normalizedContribution)
        => normalizedContribution is { } contribution
            ? contribution * ComputeRawRrfContribution(1)
            : 0.0;

    private static bool ScoresTie(double left, double right)
        => left.Equals(right) || (double.IsNaN(left) && double.IsNaN(right));

    private static void MergeAttribution(FusionAccumulator accumulator, ScoredResult result)
    {
        string? resultCaseId = string.IsNullOrWhiteSpace(result.CaseId) ? null : result.CaseId;
        string? resultCaseName = string.IsNullOrWhiteSpace(result.CaseName) ? null : result.CaseName;

        if (accumulator.CaseId is null && resultCaseId is not null)
        {
            accumulator.CaseId = resultCaseId;
            accumulator.CaseName = resultCaseName;
        }
        else if (string.Equals(accumulator.CaseId, resultCaseId, StringComparison.Ordinal) && accumulator.CaseName is null)
        {
            accumulator.CaseName = resultCaseName;
        }

        accumulator.AnnotationsCount = Math.Max(accumulator.AnnotationsCount, result.AnnotationsCount);
    }
}
