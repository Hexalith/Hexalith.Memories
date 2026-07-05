// <copyright file="IndexSemanticChunksActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes raw payload chunk embeddings in Redis Vector Search.</summary>
public sealed class IndexSemanticChunksActivity : WorkflowActivity<SemanticChunkIndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSemanticChunksActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="IndexSemanticChunksActivity"/> class.</summary>
    public IndexSemanticChunksActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSemanticChunksActivity> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<IndexResult> RunAsync(
        WorkflowActivityContext context,
        SemanticChunkIndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.EmbeddingProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.EmbeddingModel);
        if (input.EmbeddingDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "EmbeddingDimensions must be positive.");
        }

        if (input.Chunks.Count == 0)
        {
            throw new ArgumentException("At least one semantic chunk is required.", nameof(input));
        }

        IDatabase db = _redis.GetDatabase();
        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
            .ReadActiveMarkerAsync(db, input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);
        EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
            marker,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions);

        var ft = db.FT();
        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");

        string indexName = IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId);
        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateSemanticParams(input.TenantId),
                IndexSchemaDefinitions.CreateSemanticSchema(input.EmbeddingDimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            _logger.LogWarning("Redis Vector index {IndexName} already exists for tenant {TenantId}", indexName, input.TenantId);
        }

        foreach (ChunkEmbeddingResult chunk in input.Chunks.OrderBy(static c => c.Sequence))
        {
            ValidateChunk(input, chunk);
            byte[] vectorBytes = MemoryMarshal.AsBytes(chunk.Vector.AsSpan()).ToArray();
            string hashKey = IndexSchemaDefinitions.BuildSemanticChunkKey(input.TenantId, input.MemoryUnitId, chunk.Sequence);

            List<HashEntry> hashEntries =
            [
                new HashEntry("embedding", vectorBytes),
                new HashEntry("memoryUnitId", input.MemoryUnitId),
                new HashEntry("caseId", input.CaseId),
                new HashEntry("embeddingProvider", input.EmbeddingProvider),
                new HashEntry("embeddingModel", input.EmbeddingModel),
                new HashEntry("embeddingDimensions", input.EmbeddingDimensions),
                new HashEntry("chunkSequence", chunk.Sequence),
                new HashEntry("chunkStartOffset", chunk.StartOffset),
                new HashEntry("chunkEndOffset", chunk.EndOffset),
                new HashEntry("chunkText", chunk.Text),
            ];

            if (!string.IsNullOrWhiteSpace(cloudEventSubject))
            {
                hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
            }

            await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Indexed {ChunkCount} semantic chunks for memory unit {MemoryUnitId} in tenant {TenantId}",
            input.Chunks.Count,
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic", input.MemoryUnitId, input.TenantId);
    }

    private static void ValidateChunk(SemanticChunkIndexInput input, ChunkEmbeddingResult chunk)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chunk.Text);
        ArgumentNullException.ThrowIfNull(chunk.Vector);
        if (chunk.Sequence < 0 || chunk.StartOffset < 0 || chunk.EndOffset <= chunk.StartOffset)
        {
            throw new ArgumentException("Chunk sequence and source offsets must be valid.", nameof(input));
        }

        if (chunk.Vector.Length != input.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Chunk {chunk.Sequence} vector dimension {chunk.Vector.Length} does not match expected {input.EmbeddingDimensions}.");
        }
    }

    private static string? TryGetMetadataValue(IReadOnlyDictionary<string, MetadataField> metadata, string key)
        => metadata.TryGetValue(key, out MetadataField? field) && !string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : null;
}
