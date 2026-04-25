// <copyright file="SearchResponseMetadataApplier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Applies Story 10.2 token-budget truncation and degraded-state metadata to single-axis
/// <see cref="SearchResult"/> and aggregated <see cref="HybridSearchResult"/> responses
/// before they are written to the wire.
/// </summary>
internal static class SearchResponseMetadataApplier
{
    /// <summary>Applies token-budget truncation + degraded-state metadata to a single-axis search result.</summary>
    /// <param name="result">The pre-truncation single-axis search result.</param>
    /// <param name="axisName">The axis that produced the result (e.g. <c>"syntactic"</c>, <c>"semantic"</c>, <c>"graph"</c>).</param>
    /// <param name="budget">The optional token budget. Null means no truncation.</param>
    /// <param name="degraded">Whether the response should advertise a degraded backend state.</param>
    /// <param name="unavailableAxes">The unavailable axis names to surface; null or empty omits the field.</param>
    /// <returns>The result with token-budget + degraded metadata populated.</returns>
    public static SearchResult ApplySearch(
        SearchResult result,
        string axisName,
        int? budget,
        bool degraded = false,
        IReadOnlyList<string>? unavailableAxes = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(axisName);

        var truncation = TokenBudgetTruncator.TruncateByRank(
            result.Results,
            budget,
            static scored => TokenBudgetTruncator.EstimateTokensForSnippet(scored.ContentSnippet));

        IReadOnlyList<string>? unavailable = unavailableAxes is { Count: > 0 } ? unavailableAxes : null;

        return result with
        {
            Results = truncation.Kept,
            OmittedCount = truncation.Omitted,
            EstimatedTokensTotal = truncation.EstimatedTokensTotal,
            OmittedReason = CombineOmittedReasons(truncation.OmittedReason, degraded),
            Degraded = degraded,
            UnavailableAxes = unavailable,
            AxesUsed = [axisName],
        };
    }

    /// <summary>Applies token-budget truncation + axes-used metadata to a hybrid search result.</summary>
    /// <param name="result">The pre-truncation hybrid result, already carrying any <see cref="HybridSearchResult.Degraded"/> / <see cref="HybridSearchResult.UnavailableAxes"/> signals from the fusion pipeline.</param>
    /// <param name="budget">The optional token budget. Null means no truncation.</param>
    /// <param name="enabledAxes">The axes the caller requested.</param>
    /// <param name="embeddingConfig">The tenant's embedding configuration (null when semantic was disabled or unreachable).</param>
    /// <param name="graphStart">The graph-scoped start node id (null when graph is disabled or unreachable).</param>
    /// <returns>The result with token-budget + axes-used metadata populated.</returns>
    public static HybridSearchResult ApplyHybrid(
        HybridSearchResult result,
        int? budget,
        IReadOnlySet<string> enabledAxes,
        TenantEmbeddingConfig? embeddingConfig,
        string? graphStart)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(enabledAxes);

        var truncation = TokenBudgetTruncator.TruncateByRank(
            result.Results,
            budget,
            static scored => TokenBudgetTruncator.EstimateTokensForSnippet(scored.ContentSnippet));

        HashSet<string> unavailableAxes = new(result.UnavailableAxes, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> axesUsed = DetermineAxesUsed(
            truncation.Kept,
            enabledAxes,
            unavailableAxes,
            embeddingConfig,
            graphStart);

        return result with
        {
            Results = truncation.Kept,
            OmittedCount = truncation.Omitted,
            EstimatedTokensTotal = truncation.EstimatedTokensTotal,
            OmittedReason = CombineOmittedReasons(truncation.OmittedReason, result.Degraded),
            AxesUsed = axesUsed,
        };
    }

    private static OmittedReason CombineOmittedReasons(OmittedReason truncationReason, bool degraded)
        => (truncationReason, degraded) switch
        {
            (OmittedReason.TokenBudget, true) => OmittedReason.Combined,
            (OmittedReason.None, true) => OmittedReason.BackendDegraded,
            _ => truncationReason,
        };

    private static IReadOnlyList<string> DetermineAxesUsed(
        IReadOnlyList<FusedScoredResult> results,
        IReadOnlySet<string> enabledAxes,
        HashSet<string> unavailableAxes,
        TenantEmbeddingConfig? embeddingConfig,
        string? graphStart)
    {
        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);
        foreach (FusedScoredResult result in results)
        {
            if (result.GraphScore is not null)
            {
                _ = used.Add("graph");
            }

            if (result.SemanticScore is not null)
            {
                _ = used.Add("semantic");
            }

            if (result.SyntacticScore is not null)
            {
                _ = used.Add("syntactic");
            }
        }

        return used
            .Where(axis => enabledAxes.Contains(axis))
            .Where(axis => !unavailableAxes.Contains(axis))
            .Where(axis => !string.Equals(axis, "semantic", StringComparison.OrdinalIgnoreCase) || embeddingConfig is not null)
            .Where(axis => !string.Equals(axis, "graph", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(graphStart))
            .OrderBy(static axis => axis, StringComparer.Ordinal)
            .ToArray();
    }
}
