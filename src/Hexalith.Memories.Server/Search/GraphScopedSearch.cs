// <copyright file="GraphScopedSearch.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Diagnostics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NFalkorDB;

using StackExchange.Redis;

/// <summary>
/// Graph-scoped search: traverses FalkorDB to find structurally connected memory units,
/// then either enriches from Redis hashes (Mode 1: pure graph traversal) or runs an inner
/// search and post-filters to the graph scope (Mode 2: graph-scoped syntactic/semantic).
/// </summary>
public sealed partial class GraphScopedSearch
{
    private static readonly TimeSpan GraphOperationTimeout = TimeSpan.FromSeconds(10);
    private static readonly long GraphOperationTimeoutMilliseconds =
        GraphQueryExecutionOptions.ToServerTimeoutMilliseconds(GraphOperationTimeout);

    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IConnectionMultiplexer _redis;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly ILogger<GraphScopedSearch> _logger;

    public GraphScopedSearch(
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        IGraphQueryBuilder graphQueryBuilder,
        ILogger<GraphScopedSearch> logger)
    {
        _falkorDb = falkorDb;
        _redis = redis;
        _graphQueryBuilder = graphQueryBuilder;
        _logger = logger;
    }

    /// <summary>
    /// Graph-scoped search: traverses FalkorDB, then either enriches from Redis hashes (Mode 1)
    /// or runs an inner search and post-filters to graph scope (Mode 2).
    /// </summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="startNodeId">The memory unit ID to start graph traversal from.</param>
    /// <param name="depth">Maximum traversal depth (0-10).</param>
    /// <param name="innerSearch">Optional delegate for inner axis search (Mode 2). If null, performs pure graph traversal (Mode 1).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Search results constrained to the graph scope.</returns>
    public async Task<SearchResult> SearchAsync(
        SearchQuery query,
        string startNodeId,
        int depth,
        Func<SearchQuery, Task<SearchResult>>? innerSearch = null,
        CancellationToken cancellationToken = default,
        Func<SearchQuery, IReadOnlyCollection<RedisKey>, Task<SearchResult>>? scopedInnerSearch = null,
        Func<string, string, string>? graphScopeKeyBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(startNodeId);

        SearchQuery normalizedQuery = query with
        {
            MaxResults = SearchPaginationOptions.NormalizePageSize(query.MaxResults),
            Offset = SearchPaginationOptions.NormalizeOffset(query.Offset),
        };
        int candidateWindow = SearchPaginationOptions.CalculateCandidateWindow(
            "graph-scoped",
            normalizedQuery.Offset,
            normalizedQuery.MaxResults);

        // Stage 1: Traverse FalkorDB graph
        FalkorDB falkor = new(_falkorDb.GetDatabase());
        string graphId = normalizedQuery.TenantId;

        (string cypherQuery, IDictionary<string, object> parameters) =
            _graphQueryBuilder.BuildTraverseFromNode(startNodeId, depth, normalizedQuery.CaseId);

        List<(string NodeId, int HopDistance)> traversedNodes;
        long traversalElapsedMs;
        try
        {
            long traversalStart = Stopwatch.GetTimestamp();
            ResultSet resultSet = await falkor.QueryAsync(graphId, cypherQuery, parameters, timeout: GraphOperationTimeoutMilliseconds)
                .WaitAsync(GraphOperationTimeout, cancellationToken)
                .ConfigureAwait(false);
            traversalElapsedMs = (long)Stopwatch.GetElapsedTime(traversalStart).TotalMilliseconds;

            traversedNodes = [];
            foreach (Record record in resultSet)
            {
                string nodeId = record.GetValue<string>("nodeId");
                long hopDistance = record.GetValue<long>("hopDistance");
                traversedNodes.Add((nodeId, (int)hopDistance));
            }
        }
        catch (RedisServerException ex) when (IsGraphNotFoundError(ex))
        {
            LogGraphNotFound(_logger, normalizedQuery.TenantId);
            return new SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = false,
                Query = startNodeId,
            };
        }

        LogGraphTraversalComplete(
            _logger,
            normalizedQuery.TenantId,
            startNodeId,
            depth,
            traversedNodes.Count,
            traversalElapsedMs);

        if (traversedNodes.Count == 0)
        {
            return new SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = await HasIndexedMemoryUnitsAsync(falkor, graphId, cancellationToken).ConfigureAwait(false),
                Query = innerSearch is not null || scopedInnerSearch is not null ? normalizedQuery.Query : startNodeId,
            };
        }

        // Mode 2: Graph-scoped inner search.
        if (innerSearch is not null)
        {
            HashSet<string> graphSet = new(traversedNodes.Select(n => n.NodeId));
            return await SearchWithinGraphScopeAsync(
                normalizedQuery,
                graphSet,
                innerSearch,
                candidateWindow,
                cancellationToken).ConfigureAwait(false);
        }

        if (scopedInnerSearch is not null)
        {
            ArgumentNullException.ThrowIfNull(graphScopeKeyBuilder);

            RedisKey[] graphScopeKeys = [.. traversedNodes
                .Select(n => (RedisKey)graphScopeKeyBuilder(normalizedQuery.TenantId, n.NodeId))
                .Distinct()];

            return await SearchWithinGraphScopeAsync(
                normalizedQuery,
                graphScopeKeys,
                scopedInnerSearch,
                candidateWindow,
                cancellationToken).ConfigureAwait(false);
        }

        // Mode 1: Pure graph traversal — sort by hop distance, enrich/filter, then paginate.
        List<(string NodeId, int HopDistance)> sorted = traversedNodes
            .OrderBy(n => n.HopDistance)
            .ThenBy(n => n.NodeId, StringComparer.Ordinal)
            .ToList();

        List<ScoredResult> enrichableResults = await EnrichResultsAsync(
            _redis.GetDatabase(), normalizedQuery.TenantId, sorted, normalizedQuery.SourceTypeFilter, normalizedQuery.MetadataQuery).ConfigureAwait(false);

        return new SearchResult
        {
            Results = [.. enrichableResults
                .Skip(normalizedQuery.Offset)
                .Take(normalizedQuery.MaxResults)],
            TotalCount = enrichableResults.Count,
            HasIndexedMemoryUnits = true,
            Query = startNodeId,
        };
    }

    internal static double ComputeProximityScore(int hopDistance)
        => ScoreNormalizer.NormalizeGraphProximity(hopDistance);

    internal static List<ScoredResult> FilterToGraphScope(
        IReadOnlyList<ScoredResult> results, HashSet<string> nodeIds)
    {
        List<ScoredResult> filtered = [];
        foreach (ScoredResult result in results)
        {
            if (nodeIds.Contains(result.MemoryUnitId))
            {
                filtered.Add(result);
            }
        }

        return filtered;
    }

    private async Task<bool> HasIndexedMemoryUnitsAsync(
        FalkorDB falkor,
        string graphId,
        CancellationToken cancellationToken)
    {
        (string countQuery, IDictionary<string, object> countParameters) = _graphQueryBuilder.BuildCountMemoryUnits();
        ResultSet resultSet = await falkor.QueryAsync(graphId, countQuery, countParameters, timeout: GraphOperationTimeoutMilliseconds)
            .WaitAsync(GraphOperationTimeout, cancellationToken)
            .ConfigureAwait(false);

        foreach (Record record in resultSet)
        {
            return record.GetValue<long>("count") > 0;
        }

        return false;
    }

    private async Task<SearchResult> SearchWithinGraphScopeAsync(
        SearchQuery query,
        HashSet<string> graphSet,
        Func<SearchQuery, Task<SearchResult>> innerSearch,
        int candidateWindow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(graphSet);
        ArgumentNullException.ThrowIfNull(innerSearch);

        cancellationToken.ThrowIfCancellationRequested();

        SearchResult innerResult = await innerSearch(query with
        {
            Offset = 0,
            MaxResults = candidateWindow,
        }).ConfigureAwait(false);

        List<ScoredResult> filteredResults = FilterToGraphScope(innerResult.Results, graphSet);

        return new SearchResult
        {
            Results = filteredResults
                .Skip(query.Offset)
                .Take(query.MaxResults)
                .ToList(),
            TotalCount = filteredResults.Count,
            HasIndexedMemoryUnits = innerResult.HasIndexedMemoryUnits,
            Query = query.Query,
        };
    }

    private static async Task<SearchResult> SearchWithinGraphScopeAsync(
        SearchQuery query,
        IReadOnlyCollection<RedisKey> graphScopeKeys,
        Func<SearchQuery, IReadOnlyCollection<RedisKey>, Task<SearchResult>> scopedInnerSearch,
        int candidateWindow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(graphScopeKeys);
        ArgumentNullException.ThrowIfNull(scopedInnerSearch);

        cancellationToken.ThrowIfCancellationRequested();

        SearchResult innerResult = await scopedInnerSearch(query with
        {
            Offset = 0,
            MaxResults = candidateWindow,
        }, graphScopeKeys).ConfigureAwait(false);

        return new SearchResult
        {
            Results = innerResult.Results
                .Skip(query.Offset)
                .Take(query.MaxResults)
                .ToList(),
            TotalCount = innerResult.TotalCount,
            HasIndexedMemoryUnits = innerResult.HasIndexedMemoryUnits,
            Query = query.Query,
        };
    }

    private static bool IsGraphNotFoundError(RedisServerException ex)
        => ex.Message.Contains("Graph not found", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("ERR Invalid graph operation", StringComparison.OrdinalIgnoreCase);

    private static bool HasRequiredEnrichmentFields(IReadOnlyList<RedisValue> fields)
        => fields.Count >= 3
        && !fields[0].IsNullOrEmpty
        && !fields[1].IsNullOrEmpty
        && !fields[2].IsNullOrEmpty;

    private async Task<List<ScoredResult>> EnrichResultsAsync(
        IDatabase db,
        string tenantId,
        List<(string NodeId, int HopDistance)> nodes,
        string? sourceTypeFilter = null,
        string? metadataQuery = null)
    {
        IBatch batch = db.CreateBatch();
        Task<RedisValue[]>[] tasks = nodes.Select(n =>
            batch.HashGetAsync(
                IndexSchemaDefinitions.BuildSyntacticKey(tenantId, n.NodeId),
                [new RedisValue("content"), new RedisValue("sourceUri"), new RedisValue("sourceType"), new RedisValue("caseId"), new RedisValue("metadataText")])).ToArray();
        batch.Execute();
        RedisValue[][] hashResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        var results = new List<ScoredResult>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            (string nodeId, int hopDistance) = nodes[i];
            RedisValue[] fields = hashResults[i];

            if (!HasRequiredEnrichmentFields(fields))
            {
                LogEnrichmentSkipped(_logger, nodeId, "syntactic hash missing required fields");
                continue;
            }

            string content = (string)fields[0]!;
            string sourceUri = (string)fields[1]!;
            string sourceTypeValue = (string)fields[2]!;
            string? caseIdValue = fields.Length > 3 && fields[3].HasValue ? (string)fields[3]! : null;
            string? metadataText = fields.Length > 4 && fields[4].HasValue ? (string)fields[4]! : null;

            if (!Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType))
            {
                LogEnrichmentSkipped(_logger, nodeId, $"invalid sourceType '{sourceTypeValue}'");
                continue;
            }

            // Post-filter: sourceTypeFilter on already-enriched SourceType field
            if (!string.IsNullOrWhiteSpace(sourceTypeFilter)
                && !string.Equals(sourceTypeValue, sourceTypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Post-filter: metadataQuery on already-enriched metadataText field
            if (!string.IsNullOrWhiteSpace(metadataQuery)
                && (string.IsNullOrEmpty(metadataText) || !metadataText.Contains(metadataQuery, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            results.Add(new ScoredResult
            {
                MemoryUnitId = nodeId,
                Score = ComputeProximityScore(hopDistance),
                ContentSnippet = SearchSnippetBuilder.FromStoredContent(content),
                SourceUri = sourceUri,
                SourceType = sourceType,
                Axis = "graph",
                CaseId = string.IsNullOrWhiteSpace(caseIdValue) ? null : caseIdValue,
            });
        }

        return results;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Graph traversal complete: tenant={TenantId}, startNode={StartNode}, depth={Depth}, nodeCount={NodeCount}, latencyMs={LatencyMs}")]
    private static partial void LogGraphTraversalComplete(ILogger logger, string tenantId, string startNode, int depth, int nodeCount, long latencyMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FalkorDB graph not found for tenant {TenantId} — returning empty results")]
    private static partial void LogGraphNotFound(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Enrichment skipped for memory unit {MemoryUnitId}: {Reason}")]
    private static partial void LogEnrichmentSkipped(ILogger logger, string memoryUnitId, string reason);
}
