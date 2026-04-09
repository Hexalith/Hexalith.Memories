// <copyright file="HybridSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Collections.Concurrent;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates parallel multi-axis search, fuses results via <see cref="FusionEngine"/>,
/// and handles per-axis degradation gracefully.
/// </summary>
internal sealed partial class HybridSearchService(
    Func<SearchQuery, Task<SearchResult>> syntacticSearchFunc,
    Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> semanticSearchFunc,
    Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> graphSearchFunc,
    IActorProxyFactory actorProxyFactory,
    ILogger<HybridSearchService> logger)
{
    private static readonly HashSet<string> ValidAxisNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "syntactic",
        "semantic",
        "graph",
    };

    /// <summary>Executes a hybrid search across enabled axes, fusing results with configurable weights.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="embeddingConfig">Tenant embedding config (required for semantic axis, null to skip).</param>
    /// <param name="graphStartNodeId">Graph traversal start node (required for graph axis, null to skip).</param>
    /// <param name="graphDepth">Graph traversal depth.</param>
    /// <param name="weights">Fusion weights for each axis.</param>
    /// <param name="enabledAxes">Set of axis names to execute (valid: "syntactic", "semantic", "graph").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="HybridSearchResult"/> with fused scores and degradation info.</returns>
    internal async Task<HybridSearchResult> SearchAsync(
        SearchQuery query,
        TenantEmbeddingConfig? embeddingConfig,
        string? graphStartNodeId,
        int graphDepth,
        FusionWeights weights,
        IReadOnlySet<string> enabledAxes,
        CancellationToken cancellationToken)
        => await SearchAsync(
            query,
            embeddingConfig,
            graphStartNodeId,
            graphDepth,
            weights,
            enabledAxes,
            [],
            cancellationToken).ConfigureAwait(false);

    /// <summary>Executes a hybrid search across enabled axes with precomputed unavailable axes.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="embeddingConfig">Tenant embedding config (required for semantic axis, null to skip).</param>
    /// <param name="graphStartNodeId">Graph traversal start node (required for graph axis, null to skip).</param>
    /// <param name="graphDepth">Graph traversal depth.</param>
    /// <param name="weights">Fusion weights for each axis.</param>
    /// <param name="enabledAxes">Set of axis names to execute (valid: "syntactic", "semantic", "graph").</param>
    /// <param name="preUnavailableAxes">Axes known to be unavailable before execution begins.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="HybridSearchResult"/> with fused scores and degradation info.</returns>
    internal async Task<HybridSearchResult> SearchAsync(
        SearchQuery query,
        TenantEmbeddingConfig? embeddingConfig,
        string? graphStartNodeId,
        int graphDepth,
        FusionWeights weights,
        IReadOnlySet<string> enabledAxes,
        IReadOnlyCollection<string> preUnavailableAxes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(enabledAxes);
        ArgumentNullException.ThrowIfNull(preUnavailableAxes);
        weights.Validate();

        ConcurrentDictionary<string, byte> unavailableAxes = CreateUnavailableAxes(preUnavailableAxes);
        SearchQuery axisQuery = CreateAxisQuery(query);

        // Build tasks for enabled axes
        Task<SearchResult?>? syntacticTask = null;
        Task<SearchResult?>? semanticTask = null;
        Task<SearchResult?>? graphTask = null;

        if (enabledAxes.Contains("syntactic"))
        {
            syntacticTask = ExecuteAxisAsync("syntactic", axisQuery, syntacticSearchFunc, unavailableAxes);
        }

        if (enabledAxes.Contains("semantic"))
        {
            if (embeddingConfig is null)
            {
                if (!unavailableAxes.ContainsKey("semantic"))
                {
                    LogSemanticSkipped(logger, query.TenantId, "embeddingConfig is null");
                }
            }
            else
            {
                semanticTask = ExecuteAxisAsync(
                    "semantic",
                    axisQuery,
                    searchQuery => semanticSearchFunc(searchQuery, embeddingConfig, cancellationToken),
                    unavailableAxes);
            }
        }

        if (enabledAxes.Contains("graph"))
        {
            if (string.IsNullOrWhiteSpace(graphStartNodeId))
            {
                if (!unavailableAxes.ContainsKey("graph"))
                {
                    LogGraphSkipped(logger, query.TenantId, "graphStartNodeId is null");
                }
            }
            else
            {
                graphTask = ExecuteAxisAsync(
                    "graph",
                    axisQuery,
                    searchQuery => graphSearchFunc(searchQuery, graphStartNodeId, graphDepth, cancellationToken),
                    unavailableAxes);
            }
        }

        // Execute all enabled axes in parallel
        await Task.WhenAll(
            syntacticTask ?? Task.FromResult<SearchResult?>(null),
            semanticTask ?? Task.FromResult<SearchResult?>(null),
            graphTask ?? Task.FromResult<SearchResult?>(null)).ConfigureAwait(false);

        SearchResult? syntacticResult = syntacticTask is not null ? await syntacticTask.ConfigureAwait(false) : null;
        SearchResult? semanticResult = semanticTask is not null ? await semanticTask.ConfigureAwait(false) : null;
        SearchResult? graphResult = graphTask is not null ? await graphTask.ConfigureAwait(false) : null;

        syntacticResult = NormalizeAxisResult(syntacticResult, "syntactic", query.TenantId, unavailableAxes, logger);
        semanticResult = NormalizeAxisResult(semanticResult, "semantic", query.TenantId, unavailableAxes, logger);
        graphResult = NormalizeAxisResult(graphResult, "graph", query.TenantId, unavailableAxes, logger);

        // Fetch corpus statistics for BM25 normalization
        int documentCount = 0;
        double averageDocumentLength = 0.0;

        if (syntacticResult is { Results.Count: > 0 })
        {
            try
            {
                ICorpusStatisticsActor statsActor = actorProxyFactory
                    .CreateActorProxy<ICorpusStatisticsActor>(
                        new ActorId(query.TenantId),
                        nameof(CorpusStatisticsActor));
                CorpusStatistics stats = await statsActor.GetStatisticsAsync().ConfigureAwait(false);
                documentCount = stats.DocumentCount;
                averageDocumentLength = stats.AverageDocumentLength;
            }
            catch (Exception ex)
            {
                LogCorpusStatsFailure(logger, query.TenantId, ex);
                _ = unavailableAxes.TryAdd("syntactic", 0);
                syntacticResult = null;
            }
        }

        // Fuse results
        IReadOnlyList<FusedScoredResult> fusedResults = FusionEngine.Fuse(
            syntacticResult?.Results,
            semanticResult?.Results,
            graphResult?.Results,
            weights,
            documentCount,
            averageDocumentLength);

        // Apply pagination after fusion
        long totalCount = fusedResults.Count;
        int offset = Math.Max(query.Offset, 0);
        int maxResults = Math.Clamp(query.MaxResults, 1, 100);

        IReadOnlyList<FusedScoredResult> paginatedResults = fusedResults
            .Skip(offset)
            .Take(maxResults)
            .ToList();

        List<string> unavailableAxisList = [.. unavailableAxes.Keys.OrderBy(static axis => axis, StringComparer.Ordinal)];

        return new HybridSearchResult
        {
            Results = paginatedResults,
            TotalCount = totalCount,
            Degraded = unavailableAxisList.Count > 0,
            UnavailableAxes = unavailableAxisList,
            Query = query.Query,
        };
    }

    /// <summary>Validates that all axis names in the set are recognized.</summary>
    /// <param name="axes">The axis names to validate.</param>
    /// <returns>The first invalid axis name, or null if all are valid.</returns>
    internal static string? FindInvalidAxis(IReadOnlySet<string> axes)
    {
        foreach (string axis in axes)
        {
            if (!ValidAxisNames.Contains(axis))
            {
                return axis;
            }
        }

        return null;
    }

    private static SearchQuery CreateAxisQuery(SearchQuery query)
    {
        int requestedOffset = Math.Max(query.Offset, 0);
        int requestedMaxResults = Math.Clamp(query.MaxResults, 1, 100);
        int axisMaxResults = (int)Math.Clamp((long)requestedOffset + requestedMaxResults, 1L, 100L);

        return query with
        {
            Offset = 0,
            MaxResults = axisMaxResults,
        };
    }

    private static ConcurrentDictionary<string, byte> CreateUnavailableAxes(IReadOnlyCollection<string> preUnavailableAxes)
    {
        ConcurrentDictionary<string, byte> unavailableAxes = new(StringComparer.OrdinalIgnoreCase);

        foreach (string axis in preUnavailableAxes)
        {
            if (!string.IsNullOrWhiteSpace(axis))
            {
                _ = unavailableAxes.TryAdd(axis, 0);
            }
        }

        return unavailableAxes;
    }

    private static async Task<SearchResult?> ExecuteAxisAsync(
        string axisName,
        SearchQuery axisQuery,
        Func<SearchQuery, Task<SearchResult>> searchFunc,
        ConcurrentDictionary<string, byte> unavailableAxes)
    {
        try
        {
            int targetWindowSize = Math.Clamp(axisQuery.MaxResults, 1, 100);
            int currentOffset = Math.Max(axisQuery.Offset, 0);
            long totalCount = 0;
            bool hasIndexedMemoryUnits = false;
            SearchResult? firstPage = null;
            List<ScoredResult> collectedResults = [];

            while (collectedResults.Count < targetWindowSize)
            {
                SearchResult page = await searchFunc(axisQuery with
                {
                    Offset = currentOffset,
                    MaxResults = targetWindowSize,
                }).ConfigureAwait(false);

                firstPage ??= page;
                totalCount = page.TotalCount;
                hasIndexedMemoryUnits |= page.HasIndexedMemoryUnits;

                if (!page.HasIndexedMemoryUnits)
                {
                    return page;
                }

                if (page.Results.Count > 0)
                {
                    int remainingSlots = targetWindowSize - collectedResults.Count;
                    collectedResults.AddRange(page.Results.Take(remainingSlots));
                }

                currentOffset += targetWindowSize;

                if (page.TotalCount == 0 || currentOffset >= page.TotalCount)
                {
                    break;
                }
            }

            return firstPage is null
                ? null
                : firstPage with
                {
                    Results = collectedResults,
                    TotalCount = totalCount,
                    HasIndexedMemoryUnits = hasIndexedMemoryUnits,
                };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            _ = unavailableAxes.TryAdd(axisName, 0);
            return null;
        }
    }

    private static SearchResult? NormalizeAxisResult(
        SearchResult? result,
        string axisName,
        string tenantId,
        ConcurrentDictionary<string, byte> unavailableAxes,
        ILogger logger)
    {
        if (result is null)
        {
            return null;
        }

        if (!result.HasIndexedMemoryUnits)
        {
            return null;
        }

        if (result.TotalCount > 0 && result.Results.Count == 0)
        {
            _ = unavailableAxes.TryAdd(axisName, 0);
            LogAxisDroppedFromFusion(logger, axisName, tenantId);
            return null;
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Semantic axis skipped for tenant {TenantId}: {Reason}")]
    private static partial void LogSemanticSkipped(ILogger logger, string tenantId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Graph axis skipped for tenant {TenantId}: {Reason}")]
    private static partial void LogGraphSkipped(ILogger logger, string tenantId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch corpus statistics for tenant {TenantId} — BM25 normalization will use defaults")]
    private static partial void LogCorpusStatsFailure(ILogger logger, string tenantId, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Axis {AxisName} returned only stale or unenrichable hits for tenant {TenantId} — excluding it from fusion")]
    private static partial void LogAxisDroppedFromFusion(ILogger logger, string axisName, string tenantId);
}
