// <copyright file="IndexNaturalLanguageSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Globalization;
using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>Story 9.2 Task 4.3 — DAPR Workflow activity that indexes the natural-language (LLM-authored)
/// embedding for a memory unit in Redis Vector Search under the sibling <c>:memories:vec:nl</c> index.
/// Structural clone of <see cref="IndexSemanticActivity"/> with two line-level differences: index name
/// and hash-key prefix. Keeping the two activities separate preserves per-activity telemetry, retry, and
/// cleanup granularity.</summary>
public sealed class IndexNaturalLanguageSemanticActivity : WorkflowActivity<NaturalLanguageIndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexNaturalLanguageSemanticActivity> _logger;

    public IndexNaturalLanguageSemanticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexNaturalLanguageSemanticActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<IndexResult> RunAsync(
        WorkflowActivityContext context,
        NaturalLanguageIndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        ArgumentNullException.ThrowIfNull(input.EmbeddingVector);
        if (input.EmbeddingVector.Length == 0)
        {
            throw new ArgumentException("EmbeddingVector must not be empty.", nameof(input));
        }

        // Review P5: the NL description is the business-meaning signal — indexing an empty or null
        // value writes a degenerate hash whose `naturalLanguageDescription` field is absent/empty,
        // and a subsequent KNN search may return false-positive matches for zero-vector-adjacent
        // neighbours. Fail fast so the retry workflow / compensation path can react.
        ArgumentException.ThrowIfNullOrWhiteSpace(input.NaturalLanguageDescription);

        byte[] vectorBytes = MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray();
        if (vectorBytes.Length != input.EmbeddingDimensions * sizeof(float))
        {
            throw new InvalidOperationException(
                $"Vector byte length {vectorBytes.Length} does not match expected {input.EmbeddingDimensions * sizeof(float)} bytes for {input.EmbeddingDimensions} dimensions");
        }

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        string indexName = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(input.TenantId);
        string hashKey = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(input.TenantId) + input.MemoryUnitId;

        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(input.TenantId),
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(input.EmbeddingDimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureNaturalLanguageSchemaMatches(db, indexName, input.TenantId, input.EmbeddingDimensions);
            _logger.LogWarning(
                "Natural-language Redis Vector index {IndexName} already exists for tenant {TenantId}",
                indexName,
                input.TenantId);
        }

        string confidenceValue = input.DescriptionConfidence?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty;
        string confidenceSourceValue = input.ConfidenceSource.ToString().ToLowerInvariant();

        List<HashEntry> hashEntries =
        [
            new HashEntry("embedding", vectorBytes),
            new HashEntry("memoryUnitId", input.MemoryUnitId),
            new HashEntry("caseId", input.CaseId),
            new HashEntry("naturalLanguageDescription", input.NaturalLanguageDescription),
            new HashEntry("descriptionOrigin", "ai"),
            new HashEntry("descriptionConfidence", confidenceValue),
            new HashEntry("descriptionConfidenceSource", confidenceSourceValue),
            new HashEntry("embeddingProvider", input.EmbeddingProvider),
            new HashEntry("embeddingModel", input.EmbeddingModel),
            new HashEntry("embeddingDimensions", input.EmbeddingDimensions),
        ];

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in NL semantic index for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic-nl", input.MemoryUnitId, input.TenantId);
    }

    private static void EnsureNaturalLanguageSchemaMatches(IDatabase db, string indexName, string tenantId, int expectedDimensions)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        IReadOnlyList<string> problems = IndexSchemaDefinitions.DescribeVectorSchemaProblems(
            info,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
            expectedDimensions);

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
        }
    }
}
