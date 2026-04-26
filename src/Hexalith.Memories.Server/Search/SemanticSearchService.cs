// <copyright file="SemanticSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using StackExchange.Redis;

using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Executes semantic (KNN vector) searches against Redis Vector indexes.</summary>
public sealed partial class SemanticSearchService
{
    private const int MaxSnippetLength = 200;
    private static readonly string[] _requiredEnrichmentFields = ["content", "sourceUri", "sourceType"];

    private readonly IConnectionMultiplexer _redis;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<SemanticSearchService> _logger;

    /// <summary>Initializes a new instance of the <see cref="SemanticSearchService"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="embeddingClient">The embedding client for generating query vectors.</param>
    /// <param name="logger">The logger instance.</param>
    public SemanticSearchService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        EmbeddingClient embeddingClient,
        ILogger<SemanticSearchService> logger)
    {
        _redis = redis;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    /// <summary>Executes a semantic search using KNN vector similarity against the tenant's Redis Vector index.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="embeddingConfig">The tenant's embedding configuration.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Ranked search results with cosine similarity scores.</returns>
    public async Task<Contracts.V1.SearchResult> SearchAsync(
        SearchQuery query,
        TenantEmbeddingConfig embeddingConfig,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(embeddingConfig);
        TenantIdGuard.Validate(query.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        int maxResults = Math.Clamp(query.MaxResults, 1, 100);

        // Step 1: Embed the query text
        long embeddingStart = Environment.TickCount64;
        float[] queryVector = await _embeddingClient.GenerateAsync(
            query.Query, query.TenantId, embeddingConfig, cancellationToken).ConfigureAwait(false);
        long embeddingElapsed = Environment.TickCount64 - embeddingStart;
        LogEmbeddingGenerated(_logger, query.TenantId, queryVector.Length, embeddingElapsed);

        // Step 2: Validate dimensions match config
        if (queryVector.Length != embeddingConfig.Dimensions)
        {
            throw new SemanticSearchDimensionMismatchException(queryVector.Length, embeddingConfig.Dimensions);
        }

        // Step 3: Convert vector to bytes and build KNN query
        byte[] queryVectorBytes = MemoryMarshal.AsBytes(queryVector.AsSpan()).ToArray();
        string queryString = BuildKnnQueryString(maxResults, query.CaseId, query.SourceTypeFilter, query.CloudEventSubject);

        var redisQuery = new Query(queryString)
            .AddParam("query_vec", queryVectorBytes)
            .Dialect(2);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();
        string indexName = $"{query.TenantId}:memories:vec";

        // Step 4: Execute KNN search
        long searchStart = Environment.TickCount64;
        RedisSearchResult result;
        try
        {
            result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown Index name") || ex.Message.Contains("No such index"))
        {
            LogMissingVectorIndex(_logger, indexName, query.TenantId);
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = false,
                Query = query.Query,
            };
        }
        catch (RedisServerException ex) when (!ex.Message.Contains("Unknown Index name") && !ex.Message.Contains("No such index"))
        {
            LogDimensionMismatch(_logger, indexName, queryVector.Length, embeddingConfig.Dimensions, ex.Message);
            throw new SemanticSearchDimensionMismatchException(queryVector.Length, embeddingConfig.Dimensions, ex);
        }

        if (result.TotalResults == 0)
        {
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = true,
                Query = query.Query,
            };
        }

        // Step 5: Extract memoryUnitIds and scores from KNN results
        var knnResults = new List<(string MemoryUnitId, double Similarity)>(result.Documents.Count);
        foreach (Document doc in result.Documents)
        {
            string memoryUnitId = (string)doc["memoryUnitId"]!;
            double distance = double.Parse((string)doc["__vector_score"]!, CultureInfo.InvariantCulture);
            double similarity = ConvertDistanceToSimilarity(distance);
            knnResults.Add((memoryUnitId, similarity));
        }

        // Step 6: Enrich from syntactic hashes via pipeline batch (metadataQuery post-filtered here)
        List<ScoredResult> results = await EnrichResultsAsync(
            db, query.TenantId, knnResults, query.MetadataQuery).ConfigureAwait(false);

        long searchElapsed = Environment.TickCount64 - searchStart;
        LogSemanticSearchComplete(_logger, results.Count, searchElapsed);

        return new Contracts.V1.SearchResult
        {
            Results = results,
            TotalCount = results.Count,
            HasIndexedMemoryUnits = true,
            Query = query.Query,
        };
    }

    /// <summary>Converts Redis COSINE distance to similarity score, clamped to [0.0, 1.0].</summary>
    /// <param name="distance">The COSINE distance from Redis (0.0 = identical, 2.0 = opposite).</param>
    /// <returns>Cosine similarity in [0.0, 1.0].</returns>
    internal static double ConvertDistanceToSimilarity(double distance)
        => Math.Clamp(1.0 - distance, 0.0, 1.0);

    /// <summary>Builds a KNN query string with optional case and source type pre-filters.</summary>
    /// <param name="maxResults">The number of nearest neighbors to return.</param>
    /// <param name="caseId">An optional case identifier for TAG filtering.</param>
    /// <param name="sourceTypeFilter">An optional source type for TAG filtering.</param>
    /// <returns>The KNN query string for FT.SEARCH.</returns>
    internal static string BuildKnnQueryString(
        int maxResults,
        string? caseId,
        string? sourceTypeFilter = null,
        string? cloudEventSubject = null)
    {
        List<string> filterParts = [];

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            filterParts.Add($"@caseId:{{{EscapeTagValue(caseId)}}}");
        }

        if (!string.IsNullOrWhiteSpace(sourceTypeFilter))
        {
            filterParts.Add($"@sourceType:{{{EscapeTagValue(sourceTypeFilter)}}}");
        }

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            filterParts.Add($"@cloudeventSubject:{{{EscapeTagValue(cloudEventSubject)}}}");
        }

        string preFilter = filterParts.Count > 0 ? string.Join(" ", filterParts) : "*";
        return $"{preFilter}=>[KNN {maxResults} @embedding $query_vec AS __vector_score]";
    }

    /// <summary>Escapes RediSearch TAG field special characters in a value.</summary>
    /// <param name="input">The raw tag value.</param>
    /// <returns>The escaped value safe for RediSearch TAG filters.</returns>
    internal static string EscapeTagValue(string input)
        => EscapeRegex().Replace(input, @"\$0");

    /// <summary>Checks whether the enrichment hash returned all required fields.</summary>
    /// <param name="fields">The Redis hash values returned for content, sourceUri, and sourceType.</param>
    /// <returns><see langword="true"/> when all required fields are present and non-empty; otherwise <see langword="false"/>.</returns>
    internal static bool HasRequiredEnrichmentFields(IReadOnlyList<RedisValue> fields)
    {
        if (fields.Count < _requiredEnrichmentFields.Length)
        {
            return false;
        }

        for (int i = 0; i < _requiredEnrichmentFields.Length; i++)
        {
            if (fields[i].IsNullOrEmpty)
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

    private async Task<List<ScoredResult>> EnrichResultsAsync(
        IDatabase db,
        string tenantId,
        List<(string MemoryUnitId, double Similarity)> knnResults,
        string? metadataQuery = null)
    {
        // Pipeline batch: fetch content/sourceUri/sourceType/caseId from syntactic hashes
        IBatch batch = db.CreateBatch();
        Task<RedisValue[]>[] tasks = knnResults.Select(r =>
            batch.HashGetAsync(
                $"{tenantId}:mu:{r.MemoryUnitId}",
                [new RedisValue("content"), new RedisValue("sourceUri"), new RedisValue("sourceType"), new RedisValue("caseId"), new RedisValue("metadataText")])).ToArray();
        batch.Execute();
        RedisValue[][] hashResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        var results = new List<ScoredResult>(knnResults.Count);
        for (int i = 0; i < knnResults.Count; i++)
        {
            (string memoryUnitId, double similarity) = knnResults[i];
            RedisValue[] fields = hashResults[i];

            // Skip if syntactic hash is missing or any required enrichment field is empty
            if (!HasRequiredEnrichmentFields(fields))
            {
                LogEnrichmentSkipped(_logger, memoryUnitId, "syntactic hash missing required fields");
                continue;
            }

            string content = (string)fields[0]!;
            string sourceUri = (string)fields[1]!;
            string sourceTypeValue = (string)fields[2]!;
            string? caseIdValue = fields.Length > 3 && fields[3].HasValue ? (string)fields[3]! : null;
            string? metadataText = fields.Length > 4 && fields[4].HasValue ? (string)fields[4]! : null;

            // Post-filter: metadataQuery cannot be a KNN pre-filter (TEXT fields unsupported)
            if (!string.IsNullOrWhiteSpace(metadataQuery)
                && (string.IsNullOrEmpty(metadataText) || !metadataText.Contains(metadataQuery, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (!Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType))
            {
                LogEnrichmentSkipped(_logger, memoryUnitId, $"invalid sourceType '{sourceTypeValue}'");
                continue;
            }

            results.Add(new ScoredResult
            {
                MemoryUnitId = memoryUnitId,
                Score = similarity,
                ContentSnippet = TruncateContent(content),
                SourceUri = sourceUri,
                SourceType = sourceType,
                Axis = "semantic",
                CaseId = string.IsNullOrWhiteSpace(caseIdValue) ? null : caseIdValue,
            });
        }

        return [.. results
            .OrderByDescending(static r => r.Score)
            .ThenBy(static r => r.MemoryUnitId, StringComparer.Ordinal)];
    }

    [GeneratedRegex(@"[-@!{}()\[\]^~*?:\\""'|,]")]
    private static partial Regex EscapeRegex();

    [LoggerMessage(Level = LogLevel.Information, Message = "Semantic search complete: {ResultCount} results in {LatencyMs}ms")]
    private static partial void LogSemanticSearchComplete(ILogger logger, int resultCount, long latencyMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Embedding generated for tenant {TenantId}: {Dimensions} dimensions in {ElapsedMs}ms")]
    private static partial void LogEmbeddingGenerated(ILogger logger, string tenantId, int dimensions, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Enrichment skipped for memory unit {MemoryUnitId}: {Reason}")]
    private static partial void LogEnrichmentSkipped(ILogger logger, string memoryUnitId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis Vector index {IndexName} not found for tenant {TenantId} — returning empty results")]
    private static partial void LogMissingVectorIndex(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Dimension mismatch on index {IndexName}: query has {QueryDimensions} dims, config expects {ConfigDimensions} dims. Redis error: {ErrorMessage}")]
    private static partial void LogDimensionMismatch(ILogger logger, string indexName, int queryDimensions, int configDimensions, string errorMessage);
}
