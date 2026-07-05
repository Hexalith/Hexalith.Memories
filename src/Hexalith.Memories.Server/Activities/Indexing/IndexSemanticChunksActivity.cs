// <copyright file="IndexSemanticChunksActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Runtime.InteropServices;
using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes raw payload chunk embeddings in Redis Vector Search.</summary>
public sealed class IndexSemanticChunksActivity : WorkflowActivity<SemanticChunkIndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSemanticChunksActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    /// <summary>Initializes a new instance of the <see cref="IndexSemanticChunksActivity"/> class.</summary>
    public IndexSemanticChunksActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSemanticChunksActivity> logger,
        IWorkflowPayloadStore? payloadStore = null,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
        _payloadStore = payloadStore;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
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

        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");

        // Story 23.7 (A34): verify the tenant's raw semantic index once per process instead of issuing FT.CREATE
        // before every chunked write. Creation stays owned by TenantProvisioningWorkflow; the marker check above
        // still runs on every invocation and is never skipped by the readiness cache.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Semantic, input.EmbeddingDimensions, CancellationToken.None)
            .ConfigureAwait(false);

        foreach (ChunkEmbeddingResult chunk in input.Chunks.OrderBy(static c => c.Sequence))
        {
            ResolvedSemanticChunk resolved = await ResolveChunkAsync(input, chunk).ConfigureAwait(false);
            ValidateChunk(input, resolved);
            byte[] vectorBytes = MemoryMarshal.AsBytes(resolved.Vector.AsSpan()).ToArray();
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
                new HashEntry("chunkText", resolved.Text),
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

    private async Task<ResolvedSemanticChunk> ResolveChunkAsync(SemanticChunkIndexInput input, ChunkEmbeddingResult chunk)
    {
        string text = chunk.Text;
        if (chunk.TextReference is not null)
        {
            byte[] textBytes = await RequirePayloadStore()
                .ReadAsync(
                    chunk.TextReference,
                    input.TenantId,
                    input.MemoryUnitId,
                    WorkflowPayloadKind.ChunkText,
                    CancellationToken.None)
                .ConfigureAwait(false);
            text = Encoding.UTF8.GetString(textBytes);
        }

        float[] vector = chunk.Vector;
        if (chunk.VectorReference is not null)
        {
            byte[] vectorBytes = await RequirePayloadStore()
                .ReadAsync(
                    chunk.VectorReference,
                    input.TenantId,
                    input.MemoryUnitId,
                    WorkflowPayloadKind.ChunkVector,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (vectorBytes.Length % sizeof(float) != 0)
            {
                throw new WorkflowPayloadException("PAYLOAD_VECTOR_LENGTH_INVALID", chunk.VectorReference.Id);
            }

            vector = MemoryMarshal.Cast<byte, float>(vectorBytes).ToArray();
        }

        return new ResolvedSemanticChunk(text, vector, chunk.Sequence, chunk.StartOffset, chunk.EndOffset);
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "semantic-chunk");

    private static void ValidateChunk(SemanticChunkIndexInput input, ResolvedSemanticChunk chunk)
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
