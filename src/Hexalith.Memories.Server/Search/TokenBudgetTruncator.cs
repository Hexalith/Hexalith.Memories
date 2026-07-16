// <copyright file="TokenBudgetTruncator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using Hexalith.Memories.Contracts.V1;

/// <summary>Applies conservative token-budget truncation to ranked and traversal responses.</summary>
internal static class TokenBudgetTruncator
{
    /// <summary>
    /// Truncates ranked results by retaining the original rank order until the next item would exceed the budget.
    /// Negative estimator values are treated as zero to keep the helper robust against caller bugs.
    /// </summary>
    /// <typeparam name="T">The ranked item type.</typeparam>
    /// <param name="ranked">The ranked items, already sorted descending by relevance.</param>
    /// <param name="tokenBudget">The optional token budget. Null means no truncation.</param>
    /// <param name="tokenEstimator">Returns an estimated token cost for an item.</param>
    /// <returns>The kept items, omission count, pre-truncation estimate, and omission reason.</returns>
    public static (IReadOnlyList<T> Kept, int Omitted, long EstimatedTokensTotal, OmittedReason OmittedReason) TruncateByRank<T>(
        IReadOnlyList<T> ranked,
        int? tokenBudget,
        Func<T, int> tokenEstimator)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(tokenEstimator);

        int[] estimates = ranked.Select(item => Math.Max(0, tokenEstimator(item))).ToArray();
        long estimatedTokensTotal = estimates.Sum(static estimate => (long)estimate);

        if (tokenBudget is null)
        {
            return (ranked, 0, estimatedTokensTotal, OmittedReason.None);
        }

        long budget = Math.Max(0, tokenBudget.Value);
        long runningTotal = 0;
        List<T> kept = [];

        for (int i = 0; i < ranked.Count; i++)
        {
            long nextTotal = runningTotal + estimates[i];
            if (nextTotal > budget)
            {
                break;
            }

            kept.Add(ranked[i]);
            runningTotal = nextTotal;
        }

        int omitted = ranked.Count - kept.Count;
        return (kept, omitted, estimatedTokensTotal, omitted > 0 ? OmittedReason.TokenBudget : OmittedReason.None);
    }

    /// <summary>
    /// Truncates traversal nodes by pruning leaf branches before the primary causal path.
    /// Negative estimator values are treated as zero to keep the helper robust against caller bugs.
    /// </summary>
    /// <param name="nodes">The traversal nodes.</param>
    /// <param name="tokenBudget">The optional token budget. Null means no truncation.</param>
    /// <param name="tokenEstimator">Returns an estimated token cost for a node.</param>
    /// <returns>The kept nodes, omission count, pre-truncation estimate, omission reason, and primary-path signal.</returns>
    public static (IReadOnlyList<TraversalNode> Kept, int Omitted, long EstimatedTokensTotal, OmittedReason OmittedReason, bool PrimaryPathIntact) TruncateTraversal(
        IReadOnlyList<TraversalNode> nodes,
        int? tokenBudget,
        Func<TraversalNode, int> tokenEstimator)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(tokenEstimator);

        int[] estimates = nodes.Select(node => Math.Max(0, tokenEstimator(node))).ToArray();
        long estimatedTokensTotal = estimates.Sum(static estimate => (long)estimate);

        if (tokenBudget is null)
        {
            return (nodes, 0, estimatedTokensTotal, OmittedReason.None, true);
        }

        long budget = Math.Max(0, tokenBudget.Value);
        HashSet<string> primaryPath = FindPrimaryPath(nodes);
        HashSet<string> retainedIds = nodes
            .Select(static node => node.MemoryUnitId)
            .ToHashSet(StringComparer.Ordinal);
        Dictionary<string, int> estimateByNodeId = nodes
            .Select((node, index) => new KeyValuePair<string, int>(node.MemoryUnitId, estimates[index]))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
        long runningTotal = estimatedTokensTotal;
        bool primaryPathIntact = true;

        foreach (TraversalNode candidate in GetPruneOrder(nodes, primaryPath, prunePrimaryPath: false))
        {
            if (runningTotal <= budget)
            {
                break;
            }

            if (retainedIds.Remove(candidate.MemoryUnitId))
            {
                runningTotal -= estimateByNodeId[candidate.MemoryUnitId];
            }
        }

        foreach (TraversalNode candidate in GetPruneOrder(nodes, primaryPath, prunePrimaryPath: true))
        {
            if (runningTotal <= budget)
            {
                break;
            }

            if (retainedIds.Remove(candidate.MemoryUnitId))
            {
                runningTotal -= estimateByNodeId[candidate.MemoryUnitId];
                primaryPathIntact = false;
            }
        }

        List<TraversalNode> kept = nodes
            .Where(node => retainedIds.Contains(node.MemoryUnitId))
            .ToList();
        int omitted = nodes.Count - kept.Count;

        return (kept, omitted, estimatedTokensTotal, omitted > 0 ? OmittedReason.TokenBudget : OmittedReason.None, primaryPathIntact);
    }

    /// <summary>Estimates token count as <c>ceil(chars / 4) + overhead</c>.</summary>
    /// <param name="snippet">The content snippet to estimate.</param>
    /// <param name="overhead">The metadata overhead added per item.</param>
    /// <returns>The conservative token estimate.</returns>
    public static int EstimateTokensForSnippet(string? snippet, int overhead = 24)
        => checked((int)Math.Ceiling((snippet?.Length ?? 0) / 4d) + Math.Max(0, overhead));

    private static IEnumerable<TraversalNode> GetPruneOrder(
        IReadOnlyList<TraversalNode> nodes,
        HashSet<string> primaryPath,
        bool prunePrimaryPath)
        => nodes
            .Where(node => prunePrimaryPath == primaryPath.Contains(node.MemoryUnitId))
            .OrderByDescending(static node => node.HopDistance);

    private static HashSet<string> FindPrimaryPath(IReadOnlyList<TraversalNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        TraversalNode root = nodes.OrderBy(static node => node.HopDistance).First();
        TraversalNode deepest = nodes.OrderByDescending(static node => node.HopDistance).First();

        Dictionary<string, TraversalNode> byId = nodes.ToDictionary(static node => node.MemoryUnitId, StringComparer.Ordinal);
        Dictionary<string, string?> previous = new(StringComparer.Ordinal) { [root.MemoryUnitId] = null };
        Queue<string> queue = new();
        queue.Enqueue(root.MemoryUnitId);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (string.Equals(current, deepest.MemoryUnitId, StringComparison.Ordinal))
            {
                break;
            }

            foreach (string connectedId in byId[current].Edges.Select(static edge => edge.ConnectedNodeId))
            {
                if (!byId.ContainsKey(connectedId) || previous.ContainsKey(connectedId))
                {
                    continue;
                }

                previous[connectedId] = current;
                queue.Enqueue(connectedId);
            }
        }

        HashSet<string> path = new(StringComparer.Ordinal);
        string? cursor = previous.ContainsKey(deepest.MemoryUnitId)
            ? deepest.MemoryUnitId
            : root.MemoryUnitId;

        while (cursor is not null)
        {
            _ = path.Add(cursor);
            cursor = previous[cursor];
        }

        return path;
    }
}
