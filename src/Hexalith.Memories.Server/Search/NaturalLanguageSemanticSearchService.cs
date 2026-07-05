// <copyright file="NaturalLanguageSemanticSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;

using StackExchange.Redis;

using RedisSearchResult = NRedisStack.Search.SearchResult;

/// <summary>Story 9.2 Task 4.9 — library-only KNN search against the natural-language (LLM-authored)
/// semantic index <c>{tenant}:memories:vec:nl</c>. Structural clone of <see cref="SemanticSearchService"/>
/// with one constant change: it queries the NL index and the NL hash prefix. Hits carry the
/// <c>naturalLanguageDescription</c>, <c>descriptionConfidence</c>, and
/// <c>descriptionConfidenceSource</c> fields for operator inspection.
///
/// Exposes both the original vector-only API and the shared search-contract adapter used by
/// <c>axis=nl</c> standalone and hybrid search.</summary>
public sealed partial class NaturalLanguageSemanticSearchService
{
    private static readonly string[] _requiredEnrichmentFields = ["content", "sourceUri", "sourceType"];

    private readonly IConnectionMultiplexer _redis;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<NaturalLanguageSemanticSearchService> _logger;

    /// <summary>Initializes a new instance of the <see cref="NaturalLanguageSemanticSearchService"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer (keyed "redis").</param>
    /// <param name="embeddingClient">The embedding client for generating query vectors.</param>
    /// <param name="logger">The logger instance.</param>
    public NaturalLanguageSemanticSearchService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        EmbeddingClient embeddingClient,
        ILogger<NaturalLanguageSemanticSearchService> logger)
    {
        _redis = redis;
        _embeddingClient = embeddingClient;
        _logger = logger;
    }

    /// <summary>Executes natural-language semantic search using the tenant's configured embedding provider.</summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="embeddingConfig">The tenant's embedding configuration.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Ranked search results adapted to the shared search contract.</returns>
    public async Task<Contracts.V1.SearchResult> SearchAsync(
        SearchQuery query,
        TenantEmbeddingConfig embeddingConfig,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(embeddingConfig);
        TenantIdGuard.Validate(query.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        int maxResults = SearchPaginationOptions.NormalizeCandidateSize(query.MaxResults);
        int offset = SearchPaginationOptions.NormalizeOffset(query.Offset);
        int candidateCount = SemanticSearchService.CalculateKnnCandidateCount(
            offset,
            maxResults,
            SemanticSearchService.RequiresServiceSidePostFilter(query));

        float[] queryVector = await _embeddingClient.GenerateAsync(
            query.Query,
            query.TenantId,
            embeddingConfig,
            cancellationToken).ConfigureAwait(false);

        if (queryVector.Length != embeddingConfig.Dimensions)
        {
            throw new SemanticSearchDimensionMismatchException(queryVector.Length, embeddingConfig.Dimensions);
        }

        (IReadOnlyList<NaturalLanguageSemanticSearchHit> hits, bool hasNlIndex) = await SearchCoreAsync(
            query.TenantId,
            queryVector,
            candidateCount,
            cancellationToken).ConfigureAwait(false);

        if (hits.Count == 0)
        {
            return new Contracts.V1.SearchResult
            {
                Results = [],
                TotalCount = 0,
                HasIndexedMemoryUnits = hasNlIndex,
                Query = query.Query,
            };
        }

        List<ScoredResult> candidates = await EnrichResultsAsync(
            _redis.GetDatabase(),
            query.TenantId,
            hits,
            query.SourceTypeFilter,
            query.MetadataQuery).ConfigureAwait(false);

        return new Contracts.V1.SearchResult
        {
            Results = [.. candidates.Skip(offset).Take(maxResults)],
            TotalCount = candidates.Count,
            HasIndexedMemoryUnits = true,
            Query = query.Query,
        };
    }

    /// <summary>Executes a KNN search against the tenant's natural-language semantic index using a
    /// precomputed query vector. Unlike <see cref="SemanticSearchService"/>, this service does NOT embed
    /// the query text itself — callers supply a vector produced by the same embedding provider + model
    /// that populated the NL index (typically via <c>EmbeddingClient.GenerateAsync</c>).</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="queryVector">The precomputed query vector. Must match the tenant's configured embedding dimensions.</param>
    /// <param name="topK">The number of nearest neighbors to return (clamped to [1, 100]).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Ranked hits carrying the NL-specific description + confidence metadata.</returns>
    public async Task<IReadOnlyList<NaturalLanguageSemanticSearchHit>> SearchAsync(
        string tenantId,
        ReadOnlyMemory<float> queryVector,
        int topK,
        CancellationToken cancellationToken)
        => (await SearchCoreAsync(tenantId, queryVector, topK, cancellationToken).ConfigureAwait(false)).Hits;

    private async Task<(IReadOnlyList<NaturalLanguageSemanticSearchHit> Hits, bool HasIndex)> SearchCoreAsync(
        string tenantId,
        ReadOnlyMemory<float> queryVector,
        int topK,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (queryVector.IsEmpty)
        {
            throw new ArgumentException("Query vector must not be empty.", nameof(queryVector));
        }

        int clamped = Math.Clamp(topK, 1, 100);
        byte[] queryVectorBytes = MemoryMarshal.AsBytes(queryVector.Span).ToArray();

        string indexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId);
        string fallbackIndexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId);
        string queryString = $"*=>[KNN {clamped} @embedding $query_vec AS __vector_score]";

        var redisQuery = new Query(queryString)
            .AddParam("query_vec", queryVectorBytes)
            .SetSortBy("__vector_score", true)
            .Limit(0, clamped)
            .Dialect(2);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        cancellationToken.ThrowIfCancellationRequested();
        RedisSearchResult result;
        try
        {
            result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (RediSearchErrorClassifier.IsMissingIndexError(ex))
        {
            if (!string.Equals(indexName, fallbackIndexName, StringComparison.Ordinal))
            {
                indexName = fallbackIndexName;
                try
                {
                    result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
                    goto SearchSucceeded;
                }
                catch (RedisServerException fallbackEx) when (RediSearchErrorClassifier.IsMissingIndexError(fallbackEx))
                {
                }
            }

            LogMissingNlIndex(_logger, indexName, tenantId);
            return ([], false);
        }

SearchSucceeded:

        if (result.TotalResults == 0)
        {
            return ([], true);
        }

        var hits = new List<NaturalLanguageSemanticSearchHit>(result.Documents.Count);
        foreach (Document doc in result.Documents)
        {
            string memoryUnitId = (string)doc["memoryUnitId"]!;
            double distance = double.Parse((string)doc["__vector_score"]!, CultureInfo.InvariantCulture);
            double similarity = Math.Clamp(1.0 - distance, 0.0, 1.0);
            string? description = TryRead(doc, "naturalLanguageDescription");
            string? confidenceRaw = TryRead(doc, "descriptionConfidence");
            float? confidence = TryParseConfidence(confidenceRaw);
            string? confidenceSource = TryRead(doc, "descriptionConfidenceSource");

            hits.Add(new NaturalLanguageSemanticSearchHit(
                memoryUnitId,
                similarity,
                description ?? string.Empty,
                confidence,
                confidenceSource ?? "unknown"));
        }

        LogSearchComplete(_logger, hits.Count, tenantId);
        return (hits, true);
    }

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

    private async Task<List<ScoredResult>> EnrichResultsAsync(
        IDatabase db,
        string tenantId,
        IReadOnlyList<NaturalLanguageSemanticSearchHit> hits,
        string? sourceTypeFilter,
        string? metadataQuery)
    {
        IBatch batch = db.CreateBatch();
        Task<RedisValue[]>[] tasks = hits.Select(h =>
            batch.HashGetAsync(
                IndexSchemaDefinitions.BuildSyntacticKey(tenantId, h.MemoryUnitId),
                [new RedisValue("content"), new RedisValue("sourceUri"), new RedisValue("sourceType"), new RedisValue("caseId"), new RedisValue("metadataText")])).ToArray();
        batch.Execute();
        RedisValue[][] hashResults = await Task.WhenAll(tasks).ConfigureAwait(false);

        var results = new List<ScoredResult>(hits.Count);
        for (int i = 0; i < hits.Count; i++)
        {
            NaturalLanguageSemanticSearchHit hit = hits[i];
            RedisValue[] fields = hashResults[i];

            if (!HasRequiredEnrichmentFields(fields))
            {
                LogEnrichmentSkipped(_logger, hit.MemoryUnitId, "syntactic hash missing required fields");
                continue;
            }

            string content = (string)fields[0]!;
            string sourceUri = (string)fields[1]!;
            string sourceTypeValue = (string)fields[2]!;
            string? caseIdValue = fields.Length > 3 && fields[3].HasValue ? (string)fields[3]! : null;
            string? metadataText = fields.Length > 4 && fields[4].HasValue ? (string)fields[4]! : null;

            if (!string.IsNullOrWhiteSpace(sourceTypeFilter)
                && !string.Equals(sourceTypeValue, sourceTypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(metadataQuery)
                && (string.IsNullOrEmpty(metadataText) || !metadataText.Contains(metadataQuery, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (TryBuildScoredResult(
                hit,
                content,
                sourceUri,
                sourceTypeValue,
                caseIdValue,
                out ScoredResult? scored))
            {
                results.Add(scored);
            }
            else
            {
                LogEnrichmentSkipped(_logger, hit.MemoryUnitId, $"invalid sourceType '{sourceTypeValue}'");
            }
        }

        return [.. results
            .OrderByDescending(static r => r.Score)
            .ThenBy(static r => r.MemoryUnitId, StringComparer.Ordinal)];
    }

    internal static bool TryBuildScoredResult(
        NaturalLanguageSemanticSearchHit hit,
        string content,
        string sourceUri,
        string sourceTypeValue,
        string? caseIdValue,
        [NotNullWhen(true)] out ScoredResult? result)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentNullException.ThrowIfNull(sourceTypeValue);

        if (!Enum.TryParse(sourceTypeValue, ignoreCase: true, out SourceType sourceType))
        {
            result = null;
            return false;
        }

        result = new ScoredResult
        {
            MemoryUnitId = hit.MemoryUnitId,
            Score = hit.Similarity,
            ContentSnippet = SearchSnippetBuilder.FromStoredContent(content),
            SourceUri = sourceUri,
            SourceType = sourceType,
            Axis = "nl",
            CaseId = string.IsNullOrWhiteSpace(caseIdValue) ? null : caseIdValue,
        };
        return true;
    }

    private static string? TryRead(Document doc, string fieldName)
    {
        foreach (KeyValuePair<string, RedisValue> pair in doc.GetProperties())
        {
            if (string.Equals(pair.Key, fieldName, StringComparison.Ordinal))
            {
                return pair.Value.IsNullOrEmpty ? null : (string)pair.Value!;
            }
        }

        return null;
    }

    private static float? TryParseConfidence(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Natural-language semantic search for tenant {TenantId} returned {HitCount} hits")]
    private static partial void LogSearchComplete(ILogger logger, int hitCount, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Natural-language semantic index {IndexName} not found for tenant {TenantId} — returning empty results")]
    private static partial void LogMissingNlIndex(ILogger logger, string indexName, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Natural-language enrichment skipped for memory unit {MemoryUnitId}: {Reason}")]
    private static partial void LogEnrichmentSkipped(ILogger logger, string memoryUnitId, string reason);
}
