// <copyright file="TraverseResponseMetadataApplier.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Graph;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

/// <summary>
/// Applies Story 10.2 token-budget truncation, gap-marker filtering, and additive metadata fields
/// to a <see cref="TraversalResult"/> before it is written to the wire.
/// </summary>
internal static class TraverseResponseMetadataApplier
{
    /// <summary>Applies token-budget truncation + gap-marker filtering to a traversal result.</summary>
    /// <param name="result">The pre-truncation traversal result returned by <c>GraphTraversalService</c>.</param>
    /// <param name="budget">The optional token budget. Null means no truncation.</param>
    /// <returns>The result with truncated nodes, filtered gap markers, and additive metadata fields populated.</returns>
    /// <remarks>
    /// Gap markers are filtered post-truncation: a marker is retained only when at least one of its
    /// edges still references a retained node id. This prevents dangling gap markers from leaking
    /// into the response when their target leaves were pruned.
    /// </remarks>
    public static TraversalResult ApplyTraversal(TraversalResult result, int? budget)
    {
        ArgumentNullException.ThrowIfNull(result);

        var truncation = TokenBudgetTruncator.TruncateTraversal(
            result.Nodes,
            budget,
            static node => TokenBudgetTruncator.EstimateTokensForSnippet(node.ContentSnippet));

        HashSet<string> retainedNodeIds = truncation.Kept
            .Select(static node => node.MemoryUnitId)
            .ToHashSet(StringComparer.Ordinal);

        return result with
        {
            Nodes = truncation.Kept,
            GapMarkers = result.GapMarkers
                .Where(marker => marker.Edges.Any(edge => retainedNodeIds.Contains(edge.ConnectedNodeId)))
                .ToArray(),
            OmittedCount = truncation.Omitted,
            EstimatedTokensTotal = truncation.EstimatedTokensTotal,
            OmittedReason = truncation.OmittedReason,
            PrimaryPathIntact = truncation.PrimaryPathIntact,
        };
    }
}
