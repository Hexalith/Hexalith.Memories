// <copyright file="NaturalLanguageSemanticSearchService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

using System.Globalization;
using System.Runtime.InteropServices;

using Hexalith.Memories.Server.Infrastructure;

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
/// NOT wired into <see cref="HybridSearchService"/> — AC #7 requires staged rollout. Consumers opt in by
/// injecting this service directly. A follow-up search-side story adds an <c>axis=nl</c> hybrid query
/// path.</summary>
public sealed partial class NaturalLanguageSemanticSearchService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<NaturalLanguageSemanticSearchService> _logger;

    /// <summary>Initializes a new instance of the <see cref="NaturalLanguageSemanticSearchService"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer (keyed "redis").</param>
    /// <param name="logger">The logger instance.</param>
    public NaturalLanguageSemanticSearchService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<NaturalLanguageSemanticSearchService> logger)
    {
        _redis = redis;
        _logger = logger;
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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (queryVector.IsEmpty)
        {
            throw new ArgumentException("Query vector must not be empty.", nameof(queryVector));
        }

        int clamped = Math.Clamp(topK, 1, 100);
        byte[] queryVectorBytes = MemoryMarshal.AsBytes(queryVector.Span).ToArray();

        string indexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId);
        string queryString = $"*=>[KNN {clamped} @embedding $query_vec AS __vector_score]";

        var redisQuery = new Query(queryString)
            .AddParam("query_vec", queryVectorBytes)
            .Dialect(2);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        cancellationToken.ThrowIfCancellationRequested();
        RedisSearchResult result;
        try
        {
            result = await ft.SearchAsync(indexName, redisQuery).ConfigureAwait(false);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown Index name") || ex.Message.Contains("No such index"))
        {
            LogMissingNlIndex(_logger, indexName, tenantId);
            return [];
        }

        if (result.TotalResults == 0)
        {
            return [];
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
        return hits;
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
}

/// <summary>Story 9.2 Task 4.9 — a single hit from the natural-language semantic search.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="Similarity">Cosine similarity in [0.0, 1.0] (1.0 = identical).</param>
/// <param name="NaturalLanguageDescription">The LLM-authored description (may be empty if the hash is corrupt).</param>
/// <param name="DescriptionConfidence">Nullable confidence signal matching the ingestion-time confidence.</param>
/// <param name="ConfidenceSource">The confidence source discriminator (<c>logprobs</c>, <c>constant</c>, <c>unknown</c>).</param>
public sealed record NaturalLanguageSemanticSearchHit(
    string MemoryUnitId,
    double Similarity,
    string NaturalLanguageDescription,
    float? DescriptionConfidence,
    string ConfidenceSource);
