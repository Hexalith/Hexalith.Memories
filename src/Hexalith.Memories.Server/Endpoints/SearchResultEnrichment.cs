// <copyright file="SearchResultEnrichment.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Endpoints;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Cases;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Search;

using StackExchange.Redis;

/// <summary>Enriches search results with case and annotation information.</summary>
internal static class SearchResultEnrichment
{
    /// <summary>Adds tenant-scoped case names and groups to search results.</summary>
    internal static async Task<SearchResult> EnrichResultWithCaseAttributionAsync(
        SearchResult result,
        CaseService caseService,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> caseIds = result.Results
            .Where(r => r.CaseId is not null)
            .Select(r => r.CaseId!)
            .Distinct()
            .ToList();

        if (caseIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken).ConfigureAwait(false);

        List<ScoredResult> enrichedResults = result.Results
            .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
                ? r with { CaseName = name }
                : r)
            .ToList();

        List<CaseGroupSummary> caseGroups = BuildCaseGroups(enrichedResults, caseNames);

        return result with
        {
            Results = enrichedResults,
            CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
        };
    }

    /// <summary>Adds tenant-scoped case names and groups to hybrid results.</summary>
    internal static async Task<HybridSearchResult> EnrichHybridResultWithCaseAttributionAsync(
        HybridSearchResult result,
        CaseService caseService,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> caseIds = result.Results
            .Where(r => r.CaseId is not null)
            .Select(r => r.CaseId!)
            .Distinct()
            .ToList();

        if (caseIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, string> caseNames = await caseService.ResolveNamesAsync(tenantId, caseIds, cancellationToken).ConfigureAwait(false);

        List<FusedScoredResult> enrichedResults = result.Results
            .Select(r => r.CaseId is not null && caseNames.TryGetValue(r.CaseId, out string? name)
                ? r with { CaseName = name }
                : r)
            .ToList();

        List<CaseGroupSummary> caseGroups = enrichedResults
            .Where(r => r.CaseId is not null)
            .GroupBy(r => r.CaseId!)
            .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
            .OrderByDescending(c => c.ResultCount)
            .ToList();

        return result with
        {
            Results = enrichedResults,
            CaseGroups = caseGroups.Count > 0 ? caseGroups : null,
        };
    }

    /// <summary>Adds annotation counts from the tenant graph to search results.</summary>
    internal static async Task<SearchResult> EnrichResultWithAnnotationCountsAsync(
        SearchResult result,
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
        if (muIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
            graphQueryBuilder,
            falkorDb,
            tenantId,
            muIds,
            cancellationToken).ConfigureAwait(false);

        if (counts.Count == 0)
        {
            return result;
        }

        List<ScoredResult> enriched = result.Results
            .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
            .ToList();

        return result with { Results = enriched };
    }

    /// <summary>Adds annotation counts from the tenant graph to hybrid results.</summary>
    internal static async Task<HybridSearchResult> EnrichHybridResultWithAnnotationCountsAsync(
        HybridSearchResult result,
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        CancellationToken cancellationToken)
    {
        if (result.Results.Count == 0)
        {
            return result;
        }

        List<string> muIds = result.Results.Select(r => r.MemoryUnitId).Distinct().ToList();
        if (muIds.Count == 0)
        {
            return result;
        }

        Dictionary<string, int> counts = await LoadAnnotationCountsAsync(
            graphQueryBuilder,
            falkorDb,
            tenantId,
            muIds,
            cancellationToken).ConfigureAwait(false);

        if (counts.Count == 0)
        {
            return result;
        }

        List<FusedScoredResult> enriched = result.Results
            .Select(r => counts.TryGetValue(r.MemoryUnitId, out int count) ? r with { AnnotationsCount = count } : r)
            .ToList();

        return result with { Results = enriched };
    }

    private static async Task<Dictionary<string, int>> LoadAnnotationCountsAsync(
        IGraphQueryBuilder graphQueryBuilder,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        IReadOnlyList<string> memoryUnitIds,
        CancellationToken cancellationToken)
    {
        if (memoryUnitIds.Count == 0)
        {
            return [];
        }

        try
        {
            (string query, IDictionary<string, object> parameters) = graphQueryBuilder.BuildBatchCountAnnotations(memoryUnitIds);
            NFalkorDB.FalkorDB falkor = new(falkorDb.GetDatabase());
            NFalkorDB.ResultSet countResult = await falkor.SelectGraph(tenantId).QueryAsync(query, parameters)
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);

            Dictionary<string, int> counts = [];
            foreach (NFalkorDB.Record record in countResult)
            {
                if (!TryReadAnnotationCount(record, out string? memoryUnitId, out int count) ||
                    string.IsNullOrWhiteSpace(memoryUnitId) ||
                    count <= 0)
                {
                    continue;
                }

                counts[memoryUnitId] = count;
            }

            return counts;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }

    private static bool TryReadAnnotationCount(NFalkorDB.Record record, out string? memoryUnitId, out int count)
    {
        memoryUnitId = null;
        count = 0;

        try
        {
            memoryUnitId = record.GetValue<string>("muId");
            long parsedCount = record.GetValue<long>("count");
            count = checked((int)parsedCount);
            return !string.IsNullOrWhiteSpace(memoryUnitId);
        }
        catch
        {
            return false;
        }
    }

    private static List<CaseGroupSummary> BuildCaseGroups(
        IReadOnlyList<ScoredResult> results, Dictionary<string, string> caseNames)
    {
        return results
            .Where(r => r.CaseId is not null)
            .GroupBy(r => r.CaseId!)
            .Select(g => new CaseGroupSummary(g.Key, caseNames.GetValueOrDefault(g.Key, g.Key), g.Count()))
            .OrderByDescending(c => c.ResultCount)
            .ToList();
    }
}
