// <copyright file="SyntacticSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using StackExchange.Redis;

using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Executes syntactic (BM25) searches against RediSearch indexes.</summary>
public sealed partial class SyntacticSearchService
{
    private const int MaxSnippetLength = 200;

    private static readonly HashSet<string> s_naturalLanguageLeadingTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "describe",
        "explain",
        "find",
        "how",
        "list",
        "show",
        "summarize",
        "tell",
        "what",
        "when",
        "where",
        "which",
        "who",
        "why",
    };

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
    public Task<Contracts.V1.SearchResult> SearchAsync(SearchQuery query)
        => SearchAsync(query, graphScopeKeys: null);

    /// <summary>Executes a syntactic search with an optional graph-scope key pre-filter.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="graphScopeKeys">Optional tenant-scoped Redis hash keys to apply with RediSearch <c>INKEYS</c>.</param>
    /// <returns>Ranked search results with BM25 scores.</returns>
    internal async Task<Contracts.V1.SearchResult> SearchAsync(SearchQuery query, IReadOnlyCollection<RedisKey>? graphScopeKeys)
    {
        ArgumentNullException.ThrowIfNull(query);
        TenantIdGuard.Validate(query.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        int maxResults = SearchPaginationOptions.NormalizeCandidateSize(query.MaxResults);
        int offset = SearchPaginationOptions.NormalizeOffset(query.Offset);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        string searchTerms = BuildSearchTermsQuery(query.Query);
        string queryString = BuildQueryString(
            searchTerms,
            query.CaseId,
            query.SourceTypeFilter,
            query.MetadataQuery,
            query.CloudEventSubject,
            query.AttributeFilters);

        var redisQuery = new Query(queryString)
            .SetWithScores(true)
            .Limit(offset, maxResults)
            .Dialect(2)
            .ReturnFields("content", "sourceUri", "sourceType", "caseId", "metadataJson", "ingestedBy", "ingestedAt");

        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(query.TenantId);
        RedisSearchResult result;

        if (graphScopeKeys is not null)
        {
            RedisKey[] scopedKeys = ValidateGraphScopeKeys(query.TenantId, graphScopeKeys);
            if (scopedKeys.Length == 0)
            {
                return new Contracts.V1.SearchResult
                {
                    Results = [],
                    TotalCount = 0,
                    HasIndexedMemoryUnits = await HasIndexedMemoryUnitsAsync(db, indexName).ConfigureAwait(false),
                    Query = query.Query,
                };
            }

            return await SearchWithGraphScopeKeysAsync(
                db,
                indexName,
                query,
                queryString,
                scopedKeys,
                offset,
                maxResults).ConfigureAwait(false);
        }

        try
        {
            result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (RediSearchErrorClassifier.IsMissingIndexError(ex))
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
        catch (RedisServerException ex) when (RediSearchErrorClassifier.IsQuerySyntaxError(ex))
        {
            LogQuerySyntaxRejected(_logger, query.TenantId, "syntactic");
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = true,
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
        string memoryUnitId = IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(tenantId, (RedisKey)doc.Id, out string parsedMemoryUnitId)
            ? parsedMemoryUnitId
            : doc.Id;

        string content = (string)doc["content"]!;
        string sourceUri = (string)doc["sourceUri"]!;
        string sourceTypeValue = (string)doc["sourceType"]!;
        RedisValue caseIdField = doc["caseId"];
        string? caseIdValue = caseIdField.IsNullOrEmpty ? null : caseIdField.ToString();

        _ = Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType);

        return new ScoredResult
        {
            MemoryUnitId = memoryUnitId,
            Score = doc.Score,
            ContentSnippet = TruncateContent(content),
            SourceUri = sourceUri,
            SourceType = sourceType,
            Axis = "syntactic",
            CaseId = string.IsNullOrWhiteSpace(caseIdValue) ? null : caseIdValue,
        };
    }

    /// <summary>Builds the FT.SEARCH query string with optional case, source type, and metadata filters.</summary>
    /// <param name="searchTerms">The escaped search terms.</param>
    /// <param name="caseId">An optional case identifier for TAG filtering.</param>
    /// <param name="sourceTypeFilter">An optional source type for TAG filtering.</param>
    /// <param name="metadataQuery">An optional metadata text query for TEXT filtering.</param>
    /// <returns>The query string for FT.SEARCH.</returns>
    internal static string BuildQueryString(
        string searchTerms,
        string? caseId,
        string? sourceTypeFilter = null,
        string? metadataQuery = null,
        string? cloudEventSubject = null,
        IReadOnlyDictionary<string, string>? attributeFilters = null)
    {
        List<string> parts = [];

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            parts.Add($"@caseId:{{{RediSearchQueryEscaper.EscapeTag(caseId)}}}");
        }

        if (!string.IsNullOrWhiteSpace(sourceTypeFilter))
        {
            parts.Add($"@sourceType:{{{RediSearchQueryEscaper.EscapeTag(sourceTypeFilter)}}}");
        }

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            parts.Add($"@cloudeventSubject:{{{RediSearchQueryEscaper.EscapeTag(cloudEventSubject)}}}");
        }

        if (attributeFilters is { Count: > 0 })
        {
            foreach ((string key, string value) in attributeFilters.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                parts.Add($"@attributeTags:{{{RediSearchQueryEscaper.EscapeTagComposite(key, value)}}}");
            }
        }

        if (!string.IsNullOrWhiteSpace(metadataQuery))
        {
            parts.Add($"@metadataText:({RediSearchQueryEscaper.EscapeText(metadataQuery)})");
        }

        parts.Add(searchTerms);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Builds a RediSearch-safe token query for natural-language input.
    /// Multiple terms are combined with OR semantics so sentence-like user queries do not require every token
    /// to appear in the same document to contribute syntactic signal.
    /// </summary>
    /// <param name="input">The raw user query text.</param>
    /// <returns>An escaped RediSearch query over the input tokens.</returns>
    internal static string BuildSearchTermsQuery(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        string[] rawTerms = input
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        if (!LooksLikeNaturalLanguageQuery(input, rawTerms))
        {
            return RediSearchQueryEscaper.EscapeText(input);
        }

        string[] terms = rawTerms
            .Select(RediSearchQueryEscaper.EscapeText)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return terms.Length switch
        {
            0 => RediSearchQueryEscaper.EscapeText(input),
            1 => terms[0],
            _ => $"({string.Join(" | ", terms)})",
        };
    }

    private async Task<Contracts.V1.SearchResult> SearchWithGraphScopeKeysAsync(
        IDatabase db,
        string indexName,
        SearchQuery query,
        string queryString,
        IReadOnlyList<RedisKey> graphScopeKeys,
        int offset,
        int maxResults)
    {
        List<object> args =
        [
            indexName,
            queryString,
            "INKEYS",
            graphScopeKeys.Count,
        ];

        foreach (RedisKey key in graphScopeKeys)
        {
            args.Add(key);
        }

        args.Add("WITHSCORES");
        args.Add("RETURN");
        args.Add(7);
        args.Add("content");
        args.Add("sourceUri");
        args.Add("sourceType");
        args.Add("caseId");
        args.Add("metadataJson");
        args.Add("ingestedBy");
        args.Add("ingestedAt");
        args.Add("LIMIT");
        args.Add(offset);
        args.Add(maxResults);
        args.Add("DIALECT");
        args.Add(2);

        RedisResult rawResult;
        try
        {
            rawResult = await db.ExecuteAsync("FT.SEARCH", args, CommandFlags.None).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (RediSearchErrorClassifier.IsMissingIndexError(ex))
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
        catch (RedisServerException ex) when (RediSearchErrorClassifier.IsQuerySyntaxError(ex))
        {
            LogQuerySyntaxRejected(_logger, query.TenantId, "syntactic");
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = true,
                Query = query.Query,
            };
        }

        (long totalResults, List<ScoredResult> results) = ParseRawSearchResult(rawResult, query.TenantId);
        bool hasIndexedMemoryUnits = totalResults > 0
            || await HasIndexedMemoryUnitsAsync(db, indexName).ConfigureAwait(false);

        return new Contracts.V1.SearchResult
        {
            Results = results,
            TotalCount = totalResults,
            HasIndexedMemoryUnits = hasIndexedMemoryUnits,
            Query = query.Query,
        };
    }

    internal static string BuildAttributeTag(string key, string value)
        => $"{key.Trim()}={value.Trim()}";

    internal static RedisKey[] ValidateGraphScopeKeys(string tenantId, IReadOnlyCollection<RedisKey> graphScopeKeys)
    {
        ArgumentNullException.ThrowIfNull(graphScopeKeys);

        RedisKey[] keys = [.. graphScopeKeys.Distinct()];
        foreach (RedisKey key in keys)
        {
            if (!IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(tenantId, key, out _))
            {
                throw new ArgumentException(
                    "Graph scope contains a key that is not a tenant-scoped syntactic memory-unit key.",
                    nameof(graphScopeKeys));
            }
        }

        return keys;
    }

    private static bool LooksLikeNaturalLanguageQuery(string input, IReadOnlyList<string> terms)
        => input.Contains('?')
        || (terms.Count >= 5 && s_naturalLanguageLeadingTerms.Contains(terms[0]));

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

    private static bool HasRequiredFields(IReadOnlyDictionary<string, RedisValue> fields)
    {
        foreach (string field in _requiredFields)
        {
            if (!fields.TryGetValue(field, out RedisValue value) || value.IsNullOrEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private static ScoredResult MapRawFieldsToScoredResult(
        RedisKey documentId,
        double score,
        IReadOnlyDictionary<string, RedisValue> fields,
        string tenantId)
    {
        string memoryUnitId = IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(tenantId, documentId, out string parsedMemoryUnitId)
            ? parsedMemoryUnitId
            : documentId.ToString();

        string content = fields["content"].ToString();
        string sourceUri = fields["sourceUri"].ToString();
        string sourceTypeValue = fields["sourceType"].ToString();
        fields.TryGetValue("caseId", out RedisValue caseIdField);

        _ = Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType);

        return new ScoredResult
        {
            MemoryUnitId = memoryUnitId,
            Score = score,
            ContentSnippet = TruncateContent(content),
            SourceUri = sourceUri,
            SourceType = sourceType,
            Axis = "syntactic",
            CaseId = caseIdField.IsNullOrEmpty ? null : caseIdField.ToString(),
        };
    }

    private static (long TotalResults, List<ScoredResult> Results) ParseRawSearchResult(RedisResult rawResult, string tenantId)
    {
        RedisResult[] values = (RedisResult[]?)rawResult ?? [];
        if (values.Length == 0)
        {
            return (0, []);
        }

        long totalResults = Convert.ToInt64(values[0].ToString(), CultureInfo.InvariantCulture);
        List<ScoredResult> results = [];

        for (int i = 1; i < values.Length;)
        {
            RedisKey documentId = values[i++].ToString();
            if (i >= values.Length)
            {
                break;
            }

            double score = double.Parse(values[i++].ToString(), CultureInfo.InvariantCulture);
            if (i >= values.Length)
            {
                break;
            }

            Dictionary<string, RedisValue> fields = ParseFieldMap(values[i++]);
            if (!HasRequiredFields(fields))
            {
                continue;
            }

            results.Add(MapRawFieldsToScoredResult(documentId, score, fields, tenantId));
        }

        return (totalResults, results);
    }

    private static Dictionary<string, RedisValue> ParseFieldMap(RedisResult fieldResult)
    {
        RedisResult[] rawFields = (RedisResult[]?)fieldResult ?? [];
        Dictionary<string, RedisValue> fields = new(StringComparer.Ordinal);
        for (int i = 0; i < rawFields.Length - 1; i += 2)
        {
            string? fieldName = rawFields[i].ToString();
            if (string.IsNullOrEmpty(fieldName))
            {
                continue;
            }

            fields[fieldName] = rawFields[i + 1].ToString();
        }

        return fields;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index {IndexName} not found for tenant {TenantId} — returning empty results")]
    private static partial void LogMissingIndex(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch rejected sanitized {Axis} query syntax for tenant {TenantId} — returning empty results")]
    private static partial void LogQuerySyntaxRejected(ILogger logger, string tenantId, string axis);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping stale index entry {DocumentId} for tenant {TenantId} — missing required fields")]
    private static partial void LogStaleEntry(ILogger logger, string documentId, string tenantId);
}
