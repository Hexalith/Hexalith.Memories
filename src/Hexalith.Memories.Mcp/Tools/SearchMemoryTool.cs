// <copyright file="SearchMemoryTool.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tools;

using System.ComponentModel;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
    /// Story 10.1 — exposes the text-query search surface as the MCP <c>search_memory</c> tool.
/// </summary>
[McpServerToolType]
internal sealed class SearchMemoryTool
{
    /// <summary>Maximum upper bound for the <c>maxResults</c> parameter (per server validation).</summary>
    internal const int MaxResultsUpperBound = 100;

    /// <summary>Lower bound for the <c>maxResults</c> parameter.</summary>
    internal const int MaxResultsLowerBound = 1;

    private readonly MemoriesClient _client;
    private readonly McpToolExecutor _executor;

    /// <summary>Initializes a new instance of the <see cref="SearchMemoryTool"/> class.</summary>
    /// <param name="client">The Memories REST client (DAPR-routed).</param>
    /// <param name="executor">The shared MCP tool executor.</param>
    public SearchMemoryTool(
        MemoriesClient client,
        McpToolExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executor);
        _client = client;
        _executor = executor;
    }

    /// <summary>The MCP tool method invoked by LLM agents.</summary>
    /// <param name="tenantId">The tenant identifier (required).</param>
    /// <param name="query">The natural-language or keyword query string (required).</param>
    /// <param name="case">Optional case identifier scoping the search to a single case.</param>
    /// <param name="axes">Search axis (default <see cref="SearchAxis.Hybrid"/>).</param>
    /// <param name="maxResults">Maximum number of results to return; clamped to <c>[1, 100]</c>.</param>
    /// <param name="tokenBudget">Optional output token budget. The server truncates ranked results and reports omitted results.</param>
    /// <param name="explain">Whether to include explain metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An MCP tool result carrying a JSON-serialized search result.</returns>
    [McpServerTool(Name = "search_memory")]
    [Description("Searches a tenant's memory corpus across syntactic, semantic, natural-language, or hybrid axes and returns scored memory-unit results. Use traverse_relations for graph traversal.")]
    public async Task<CallToolResult> SearchAsync(
        [Description("The tenant identifier whose memories should be searched.")]
        string tenantId,
        [Description("The natural-language or keyword query string to match against memory units.")]
        string query,
        [Description("Optional case identifier to scope the search to a single case within the tenant.")]
        string? @case = null,
        [Description("Search axis: syntactic (BM25 lexical), semantic (vector similarity), nl (natural-language vector similarity), or hybrid (fused multi-axis). Use traverse_relations for graph traversal.")]
        SearchAxis axes = SearchAxis.Hybrid,
        [Description("Maximum number of results to return; clamped to the inclusive range 1..100.")]
        int maxResults = 10,
        [Description("Maximum output tokens. The server truncates results by relevance rank; the response's omitted_count reports how many results were dropped.")]
        int? tokenBudget = null,
        [Description("Whether to include explain metadata such as per-axis scores and normalization details.")]
        bool explain = false,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "search_memory";

        return await _executor.RunAsync(
            tenantId,
            toolName,
            mapper =>
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    return mapper.MapValidation(
                        "INVALID_INPUT",
                        "tenantId is required.",
                        "Provide a non-empty tenantId.",
                        toolName);
                }

                return string.IsNullOrWhiteSpace(query)
                    ? mapper.MapValidation(
                        "INVALID_INPUT",
                        "query is required.",
                        "Provide a non-empty query string.",
                        toolName)
                    : null;
            },
            async (authorizedTenant, token) =>
            {
                int clampedMax = Math.Clamp(maxResults, MaxResultsLowerBound, MaxResultsUpperBound);
                int? effectiveTokenBudget = tokenBudget is > 0 ? tokenBudget : null;

                if (axes == SearchAxis.Hybrid)
                {
                    var hybridRequest = new HybridSearchRequest(
                        TenantId: authorizedTenant,
                        Query: query,
                        CaseId: @case,
                        MaxResults: clampedMax,
                        Explain: explain,
                        TokenBudget: effectiveTokenBudget);

                    HybridSearchResult hybrid = await _client.HybridSearchAsync(hybridRequest, token)
                        .ConfigureAwait(false);
                    hybrid = hybrid with
                    {
                        EvidencePacket = EvidencePacketMapper.FromHybridSearchResult(
                            hybrid,
                            CreateEvidenceScope(authorizedTenant, @case)),
                    };
                    return McpToolResultSerializer.Success(hybrid);
                }

                var request = new SearchRequest(
                    TenantId: authorizedTenant,
                    Axis: AxisToWire(axes),
                    Query: query,
                    CaseId: @case,
                    MaxResults: clampedMax,
                    Explain: explain,
                    TokenBudget: effectiveTokenBudget);

                SearchResult result = await _client.SearchAsync(request, token).ConfigureAwait(false);
                result = result with
                {
                    EvidencePacket = EvidencePacketMapper.FromSearchResult(
                        result,
                        CreateEvidenceScope(authorizedTenant, @case)),
                };
                return McpToolResultSerializer.Success(result);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static string AxisToWire(SearchAxis axis) => axis switch
    {
        SearchAxis.Syntactic => "syntactic",
        SearchAxis.Semantic => "semantic",
        SearchAxis.Nl => "nl",
        SearchAxis.Hybrid => "hybrid",
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unsupported search axis."),
    };

    private static EvidencePacketScope CreateEvidenceScope(string tenantId, string? caseId)
        => new(tenantId, caseId, EvidencePacketIsolationStatus.Authorized, string.IsNullOrWhiteSpace(caseId) ? "tenant" : "tenant-case");
}
