// <copyright file="SyntacticSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using StackExchange.Redis;

using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Executes syntactic (BM25) searches against RediSearch indexes.</summary>
public sealed partial class SyntacticSearchService
{
    private const int MaxSnippetLength = 200;

    private static readonly string[] _requiredFields = ["content", "sourceUri", "sourceType"];

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<SyntacticSearchService> _logger;

    /// <summary>Initializes a new instance of the <see cref="SyntacticSearchService"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="logger">The logger instance.</param>
    public SyntacticSearchService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<SyntacticSearchService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>Executes a syntactic search using BM25 ranking against the tenant's RediSearch index.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <returns>Ranked search results with BM25 scores.</returns>
    public async Task<Contracts.V1.SearchResult> SearchAsync(SearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        TenantIdGuard.Validate(query.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        int maxResults = Math.Clamp(query.MaxResults, 1, 100);
        int offset = Math.Max(query.Offset, 0);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        string escapedTerms = EscapeRedisQuery(query.Query);
        string queryString = BuildQueryString(escapedTerms, query.CaseId);

        var redisQuery = new Query(queryString)
            .SetWithScores(true)
            .Limit(offset, maxResults)
            .Dialect(2)
            .ReturnFields("content", "sourceUri", "sourceType", "metadataJson", "ingestedBy", "ingestedAt");

        string indexName = $"{query.TenantId}:memories:idx";
        RedisSearchResult result;

        try
        {
            result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown Index name") || ex.Message.Contains("No such index"))
        {
            LogMissingIndex(_logger, indexName, query.TenantId);
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = false,
                Query = query.Query,
            };
        }

        bool hasIndexedMemoryUnits = result.TotalResults > 0;
        if (!hasIndexedMemoryUnits)
        {
            hasIndexedMemoryUnits = await HasIndexedMemoryUnitsAsync(db, indexName).ConfigureAwait(false);
        }

        List<ScoredResult> results = [];
        foreach (Document doc in result.Documents)
        {
            if (!HasRequiredFields(doc))
            {
                LogStaleEntry(_logger, doc.Id, query.TenantId);
                continue;
            }

            results.Add(MapDocumentToScoredResult(doc, query.TenantId));
        }

        return new Contracts.V1.SearchResult
        {
            Results = results,
            TotalCount = result.TotalResults,
            HasIndexedMemoryUnits = hasIndexedMemoryUnits,
            Query = query.Query,
        };
    }

    /// <summary>Maps a RediSearch document to a scored result record.</summary>
    /// <param name="doc">The RediSearch document.</param>
    /// <param name="tenantId">The tenant identifier for ID prefix stripping.</param>
    /// <returns>A scored result with extracted fields.</returns>
    internal static ScoredResult MapDocumentToScoredResult(Document doc, string tenantId)
    {
        string prefix = $"{tenantId}:mu:";
        string memoryUnitId = doc.Id.StartsWith(prefix, StringComparison.Ordinal)
            ? doc.Id[prefix.Length..]
            : doc.Id;

        string content = (string)doc["content"]!;
        string sourceUri = (string)doc["sourceUri"]!;
        string sourceTypeValue = (string)doc["sourceType"]!;

        _ = Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType);

        return new ScoredResult
        {
            MemoryUnitId = memoryUnitId,
            Score = doc.Score,
            ContentSnippet = TruncateContent(content),
            SourceUri = sourceUri,
            SourceType = sourceType,
            Axis = "syntactic",
        };
    }

    /// <summary>Builds the FT.SEARCH query string, optionally scoped to a case.</summary>
    /// <param name="searchTerms">The escaped search terms.</param>
    /// <param name="caseId">An optional case identifier for TAG filtering.</param>
    /// <returns>The query string for FT.SEARCH.</returns>
    internal static string BuildQueryString(string searchTerms, string? caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return searchTerms;
        }

        string escapedCaseId = EscapeRedisQuery(caseId);
        return $"@caseId:{{{escapedCaseId}}} {searchTerms}";
    }

    /// <summary>Escapes RediSearch special characters in user input to prevent query injection.</summary>
    /// <param name="input">The raw user input.</param>
    /// <returns>The escaped input safe for RediSearch queries.</returns>
    internal static string EscapeRedisQuery(string input)
        => EscapeRegex().Replace(input, @"\$0");

    private static async Task<bool> HasIndexedMemoryUnitsAsync(IDatabase db, string indexName)
    {
        RedisSearchResult countResult = await db.FT()
            .SearchAsync(indexName, new Query("*").Limit(0, 0).Dialect(2))
            .ConfigureAwait(false);

        return countResult.TotalResults > 0;
    }

    private static bool HasRequiredFields(Document doc)
    {
        foreach (string field in _requiredFields)
        {
            RedisValue value = doc[field];
            if (value.IsNullOrEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private static string TruncateContent(string content)
    {
        if (content.Length <= MaxSnippetLength)
        {
            return content;
        }

        int lastSpace = content.LastIndexOf(' ', MaxSnippetLength);
        int cutoff = lastSpace > 0 ? lastSpace : MaxSnippetLength;
        return content[..cutoff] + "...";
    }

    [GeneratedRegex(@"[-@!{}()\[\]^~*?:\\""'|]")]
    private static partial Regex EscapeRegex();

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index {IndexName} not found for tenant {TenantId} — returning empty results")]
    private static partial void LogMissingIndex(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping stale index entry {DocumentId} for tenant {TenantId} — missing required fields")]
    private static partial void LogStaleEntry(ILogger logger, string documentId, string tenantId);
}
