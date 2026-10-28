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

using NRedisStack.Search;

using StackExchange.Redis;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Executes syntactic (BM25) searches against RediSearch indexes.</summary>
public sealed partial class SyntacticSearchService
{
    /// <summary>Explicit RediSearch BM25-family scorer used by all syntactic search paths.</summary>
    internal const string RedisSearchScorerName = "BM25STD";

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

        string searchTerms = BuildSearchTermsQuery(query.Query);
        string queryString = BuildQueryString(
            searchTerms,
            query.CaseId,
            query.SourceTypeFilter,
            query.MetadataQuery,
            query.CloudEventSubject,
            query.AttributeFilters);

        queryString = await SourcePublicationVisibility.FilterQueryAsync(db, query.TenantId, queryString,
            IndexSchemaDefinitions.GetSyntacticIndexName(query.TenantId)).ConfigureAwait(false);
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(query.TenantId);
        RedisKey[]? scopedKeys = null;

        if (graphScopeKeys is not null)
        {
            scopedKeys = ValidateGraphScopeKeys(query.TenantId, graphScopeKeys);
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

        }

        try
        {
            // Score-only FT.SEARCH heaps have no unique order across separate LIMIT windows.
            // Order raw positions before hydration so stale hashes still consume their positions.
            RedisResult window = await ExecuteCompleteQueryAsync(db, "AGGREGATE",
                BuildOrderedWindowArguments(indexName, queryString, scopedKeys, offset, maxResults)).ConfigureAwait(false);
            (long totalResults, RedisKey[] orderedKeys) = ParseOrderedWindow(window);
            long expectedRows = Math.Min(maxResults, Math.Max(0L, totalResults - offset));
            if (orderedKeys.LongLength != expectedRows)
            {
                // MAXAGGREGATERESULTS can cap the sorter's retained prefix below MAXSEARCHRESULTS.
                // In that deployment a valid deep SEARCH offset must fail rather than look exhausted.
                throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch ordered window; the configured aggregate result limit cannot serve the requested raw offset.");
            }

            List<ScoredResult> results = [];
            if (orderedKeys.Length > 0)
            {
                RedisResult hydrated = await ExecuteCompleteQueryAsync(db, "SEARCH",
                    BuildGraphScopedSearchArguments(indexName, queryString, orderedKeys, 0, maxResults)).ConfigureAwait(false);
                ValidateHydratedKeys(hydrated, orderedKeys);
                (long hydratedCount, List<ScoredResult> hydratedResults) = ParseRawSearchResult(hydrated, query.TenantId);
                if (hydratedCount != orderedKeys.Length)
                {
                    throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch ordered window hydration; retry against an unchanged index.");
                }

                Dictionary<string, ScoredResult> byId = hydratedResults.ToDictionary(result => result.MemoryUnitId, StringComparer.Ordinal);
                foreach (RedisKey key in orderedKeys)
                {
                    string memoryUnitId = IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(query.TenantId, key, out string parsedId)
                        ? parsedId : key.ToString();
                    if (byId.TryGetValue(memoryUnitId, out ScoredResult? result))
                    {
                        results.Add(result);
                    }
                }
            }

            return new Contracts.V1.SearchResult
            {
                Results = results,
                TotalCount = totalResults,
                HasIndexedMemoryUnits = totalResults > 0 || await HasIndexedMemoryUnitsAsync(db, indexName).ConfigureAwait(false),
                Query = query.Query,
            };
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
            ContentSnippet = SearchSnippetBuilder.FromHighlightedContent(content, content),
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

    /// <summary>Builds a BM25-ordered raw window with the unique Redis key breaking score ties.</summary>
    /// <param name="indexName">The tenant-scoped index.</param>
    /// <param name="queryString">The escaped query including visibility and attribute filters.</param>
    /// <param name="graphScopeKeys">Optional validated graph keys applied before selection.</param>
    /// <param name="offset">The raw offset.</param>
    /// <param name="maxResults">The normalized window size.</param>
    /// <returns>The aggregation arguments.</returns>
    internal static List<object> BuildOrderedWindowArguments(
        string indexName,
        string queryString,
        IReadOnlyList<RedisKey>? graphScopeKeys,
        int offset,
        int maxResults)
    {
        List<object> args = [indexName, queryString];
        if (graphScopeKeys is not null)
        {
            args.Add("INKEYS");
            args.Add(graphScopeKeys.Count);
            args.AddRange(graphScopeKeys.Select(static key => (object)key));
        }

        args.AddRange([
            "ADDSCORES", "SCORER", RedisSearchScorerName,
            "LOAD", 1, "__key",
            "SORTBY", 4, "@__score", "DESC", "@__key", "ASC",
            "MAX", (long)offset + maxResults,
            "LIMIT", offset, maxResults,
            "DIALECT", 2,
        ]);
        return args;
    }

    private static async Task<RedisResult> ExecuteCompleteQueryAsync(IDatabase db, string queryType, List<object> arguments)
    {
        // PROFILE reports execution warnings even in RESP2, where ordinary queries silently
        // omit timeout warnings under ON_TIMEOUT RETURN. Inherit the bounded server timeout.
        List<object> profileArguments = [arguments[0], queryType, "LIMITED", "QUERY", .. arguments.Skip(1)];
        RedisResult raw = await db.ExecuteAsync("FT.PROFILE", profileArguments, CommandFlags.None).ConfigureAwait(false);
        RedisResult[] values = (RedisResult[]?)raw ?? [];
        RedisResult result;
        RedisResult profile;
        if (values.Length == 2 && values[0].Resp2Type == ResultType.Array)
        {
            result = values[0];
            profile = values[1];
        }
        else
        {
            Dictionary<string, RedisResult> map = BuildResultMap(values);
            if (!map.TryGetValue("profile", out RedisResult? profileValue))
            {
                throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch execution; completion profile is missing.");
            }

            result = raw!;
            profile = profileValue!;
        }

        if (!ValidateProfileWarnings(profile))
        {
            throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch execution; completion warning is missing.");
        }

        return result;
    }

    private static bool ValidateProfileWarnings(RedisResult profile)
    {
        if (profile.Resp2Type != ResultType.Array)
        {
            return false;
        }

        RedisResult[] entries = (RedisResult[]?)profile ?? [];
        bool foundWarning = false;
        for (int i = 0; i < entries.Length;)
        {
            if (entries[i].Resp2Type == ResultType.Array)
            {
                foundWarning |= ValidateProfileWarnings(entries[i++]);
                continue;
            }

            // Map keys and RESP2 section names occur before their values. Never interpret
            // an iterator's query-term value (for example "warning") as a completion warning.
            if (string.Equals(entries[i].ToString(), "Warning", StringComparison.OrdinalIgnoreCase))
            {
                foundWarning = true;
                // RESP2 has a one-element [Warning] row on success; RESP3 returns Warning: None.
                if (i + 1 < entries.Length && !string.Equals(entries[i + 1].ToString(), "None", StringComparison.OrdinalIgnoreCase))
                {
                    string warning = entries[i + 1].ToString();
                    if (warning.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                        || warning.Contains("timed out", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new RedisTimeoutException(CommandFlags.None, "Incomplete RediSearch execution; query profile reported a timeout.", CommandStatus.Unknown);
                    }

                    throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch execution; query profile reported a warning.");
                }
            }
            else if (i + 1 < entries.Length && entries[i + 1].Resp2Type == ResultType.Array)
            {
                foundWarning |= ValidateProfileWarnings(entries[i + 1]);
            }

            i += 2;
        }

        return foundWarning;
    }

    private static (long TotalResults, RedisKey[] Keys) ParseOrderedWindow(RedisResult rawResult)
    {
        RedisResult[] values = (RedisResult[]?)rawResult ?? [];
        if (values.Length == 0)
        {
            throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None, "Incomplete RediSearch ordered window response.");
        }

        if (long.TryParse(values[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long total))
        {
            return (total, values.Skip(1).Select(value => (RedisKey)ParseFieldMap(value)["__key"].ToString()).ToArray());
        }

        Dictionary<string, RedisResult> map = BuildResultMap(values);
        total = long.Parse(map["total_results"].ToString(), CultureInfo.InvariantCulture);
        RedisResult[] rows = (RedisResult[]?)map["results"] ?? [];
        return (total, rows.Select(row =>
        {
            Dictionary<string, RedisResult> fields = BuildResultMap((RedisResult[]?)row ?? []);
            return (RedisKey)ParseFieldMap(fields["extra_attributes"])["__key"].ToString();
        }).ToArray());
    }

    internal static string BuildAttributeTag(string key, string value)
        => $"{key.Trim()}={value.Trim()}";

    internal static Query BuildRedisQuery(string queryString, int offset, int maxResults)
        => new Query(queryString)
            .SetWithScores(true)
            .SetScorer(RedisSearchScorerName)
            .HighlightFields("content")
            .SummarizeFields(SearchSnippetBuilder.MaxSnippetLength, 1, " ... ", "content")
            .Limit(offset, maxResults)
            .Dialect(2)
            .ReturnFields("content", "sourceUri", "sourceType", "caseId", "metadataJson", "ingestedBy", "ingestedAt");

    internal static List<object> BuildGraphScopedSearchArguments(
        string indexName,
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
        args.Add("SCORER");
        args.Add(RedisSearchScorerName);
        args.Add("HIGHLIGHT");
        args.Add("FIELDS");
        args.Add(1);
        args.Add("content");
        args.Add("SUMMARIZE");
        args.Add("FIELDS");
        args.Add(1);
        args.Add("content");
        args.Add("FRAGS");
        args.Add(1);
        args.Add("LEN");
        args.Add(SearchSnippetBuilder.MaxSnippetLength);
        args.Add("SEPARATOR");
        args.Add(" ... ");
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

        return args;
    }

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
        RedisResult countResult = await ExecuteCompleteQueryAsync(db, "SEARCH", [indexName, "*", "LIMIT", 0, 0, "DIALECT", 2]).ConfigureAwait(false);
        RedisResult[] values = (RedisResult[]?)countResult ?? [];
        return values.Length > 0 && (long.TryParse(values[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long total)
            ? total > 0
            : long.Parse(BuildResultMap(values)["total_results"].ToString(), CultureInfo.InvariantCulture) > 0);
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
            ContentSnippet = SearchSnippetBuilder.FromHighlightedContent(content, content),
            SourceUri = sourceUri,
            SourceType = sourceType,
            Axis = "syntactic",
            CaseId = string.IsNullOrWhiteSpace(caseIdField.ToString()) ? null : caseIdField.ToString(),
        };
    }

    private static void ValidateHydratedKeys(RedisResult rawResult, IReadOnlyCollection<RedisKey> selectedKeys)
    {
        RedisResult[] values = (RedisResult[]?)rawResult ?? [];
        RedisKey[] returnedKeys;
        if (values.Length > 0 && long.TryParse(values[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            // WITHSCORES returns one (id, score, fields) triple for every raw position.
            returnedKeys = (values.Length - 1) % 3 == 0
                ? Enumerable.Range(0, (values.Length - 1) / 3).Select(i => (RedisKey)values[1 + i * 3].ToString()).ToArray()
                : [];
        }
        else
        {
            Dictionary<string, RedisResult> map = BuildResultMap(values);
            RedisResult[] documents = map.TryGetValue("results", out RedisResult? rows) ? (RedisResult[]?)rows ?? [] : [];
            returnedKeys = documents.Select(document =>
            {
                Dictionary<string, RedisResult> fields = BuildResultMap((RedisResult[]?)document ?? []);
                return fields.TryGetValue("id", out RedisResult? id) ? (RedisKey)id.ToString() : (RedisKey)string.Empty;
            }).ToArray();
        }

        // Check raw identities before required-field filtering. Missing fields may consume a
        // position, but a missing, duplicated or substituted selected key cannot be accepted.
        if (returnedKeys.Length != selectedKeys.Count || !selectedKeys.ToHashSet().SetEquals(returnedKeys))
        {
            throw new RedisConnectionException(ConnectionFailureType.ProtocolFailure, CommandFlags.None,
                "Incomplete RediSearch ordered window hydration; returned identities do not match the selected raw positions.");
        }
    }

    private (long TotalResults, List<ScoredResult> Results) ParseRawSearchResult(RedisResult rawResult, string tenantId)
    {
        RedisResult[] values = (RedisResult[]?)rawResult ?? [];
        if (values.Length == 0)
        {
            return (0, []);
        }

        return long.TryParse(values[0].ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long legacyTotal)
            ? ParseLegacyArraySearchResult(values, legacyTotal, tenantId)
            : ParseRespMapSearchResult(values, tenantId);
    }

    private (long TotalResults, List<ScoredResult> Results) ParseLegacyArraySearchResult(
        RedisResult[] values,
        long totalResults,
        string tenantId)
    {
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
                LogStaleEntry(_logger, documentId.ToString(), tenantId);
                continue;
            }

            results.Add(MapRawFieldsToScoredResult(documentId, score, fields, tenantId));
        }

        return (totalResults, results);
    }

    private (long TotalResults, List<ScoredResult> Results) ParseRespMapSearchResult(
        RedisResult[] mapEntries,
        string tenantId)
    {
        Dictionary<string, RedisResult> map = BuildResultMap(mapEntries);

        long totalResults = 0;
        if (map.TryGetValue("total_results", out RedisResult? totalValue)
            && long.TryParse(totalValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedTotal))
        {
            totalResults = parsedTotal;
        }

        List<ScoredResult> results = [];
        if (!map.TryGetValue("results", out RedisResult? resultsValue))
        {
            return (totalResults, results);
        }

        RedisResult[] docs = (RedisResult[]?)resultsValue ?? [];
        foreach (RedisResult docResult in docs)
        {
            RedisResult[]? docEntries = (RedisResult[]?)docResult;
            if (docEntries is null)
            {
                continue;
            }

            Dictionary<string, RedisResult> doc = BuildResultMap(docEntries);
            RedisKey documentId = doc.TryGetValue("id", out RedisResult? idValue) ? idValue.ToString() : (RedisKey)string.Empty;
            double score = doc.TryGetValue("score", out RedisResult? scoreValue)
                && double.TryParse(scoreValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedScore)
                    ? parsedScore
                    : 0.0;
            Dictionary<string, RedisValue> fields = doc.TryGetValue("extra_attributes", out RedisResult? attributes)
                ? ParseFieldMap(attributes)
                : [];

            if (!HasRequiredFields(fields))
            {
                LogStaleEntry(_logger, documentId.ToString(), tenantId);
                continue;
            }

            results.Add(MapRawFieldsToScoredResult(documentId, score, fields, tenantId));
        }

        return (totalResults, results);
    }

    private static Dictionary<string, RedisResult> BuildResultMap(RedisResult[] entries)
    {
        Dictionary<string, RedisResult> map = new(StringComparer.Ordinal);
        for (int i = 0; i + 1 < entries.Length; i += 2)
        {
            string? key = entries[i].ToString();
            if (!string.IsNullOrEmpty(key))
            {
                map[key] = entries[i + 1];
            }
        }

        return map;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch index {IndexName} not found for tenant {TenantId} — returning empty results")]
    private static partial void LogMissingIndex(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RediSearch rejected sanitized {Axis} query syntax for tenant {TenantId} — returning empty results")]
    private static partial void LogQuerySyntaxRejected(ILogger logger, string tenantId, string axis);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping stale index entry {DocumentId} for tenant {TenantId} — missing required fields")]
    private static partial void LogStaleEntry(ILogger logger, string documentId, string tenantId);
}
