// <copyright file="RestoreReindexUnitActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Restore;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>
/// Story 26.2 (AC4) — re-derives the semantic vector hashes for one restored memory unit. Embeddings and NL
/// descriptions are <b>not</b> in the export, so this activity re-runs the indexing half of the pipeline: it
/// reads the unit's content from the syntactic hash (already written by <see cref="RestoreDataPlaneActivity"/>),
/// chunks it with the same <see cref="ContentChunker"/> ingestion uses, re-embeds each chunk with the target
/// tenant's configured provider, and writes the chunked <c>{tenantId}:vec:{id}:{sequence}</c> hashes with the
/// identical field contract as <c>IndexSemanticChunksActivity</c>. Under a deterministic embedding provider the
/// re-derived vectors are byte-identical to the originals. Idempotent (HSET overwrite).
/// <para>
/// Natural-language (<c>:vecnl:</c>) vectors are intentionally not re-derived here: they exist only for
/// event-sourced units and require a non-deterministic LLM to regenerate the description (decision D1 option c).
/// They are rebuilt on the next re-index / event replay; see docs/operations/backup-restore.md.
/// </para>
/// </summary>
internal sealed class RestoreReindexUnitActivity : WorkflowActivity<RestoreReindexInput, RestoreReindexResult>, IRestoreReindexUnitProcessor
{
    private readonly IConnectionMultiplexer _redis;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ITenantEmbeddingConfigProvider _tenantEmbeddingConfigProvider;
    private readonly ContentChunkingOptions _chunkingOptions;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;
    private readonly ILogger<RestoreReindexUnitActivity> _logger;

    /// <summary>Initializes a new instance of the <see cref="RestoreReindexUnitActivity"/> class.</summary>
    /// <param name="redis">The Redis connection multiplexer (data plane).</param>
    /// <param name="embeddingClient">The embedding client (resolves the provider from tenant config).</param>
    /// <param name="tenantEmbeddingConfigProvider">The tenant embedding configuration provider.</param>
    /// <param name="chunkingOptions">The content chunking options (must match ingestion for byte-identical vectors).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="readinessVerifier">The tenant index readiness verifier (defaults to a self-constructed instance).</param>
    public RestoreReindexUnitActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        EmbeddingClient embeddingClient,
        ITenantEmbeddingConfigProvider tenantEmbeddingConfigProvider,
        IOptions<ContentChunkingOptions> chunkingOptions,
        ILogger<RestoreReindexUnitActivity> logger,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentNullException.ThrowIfNull(tenantEmbeddingConfigProvider);
        ArgumentNullException.ThrowIfNull(chunkingOptions);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _embeddingClient = embeddingClient;
        _tenantEmbeddingConfigProvider = tenantEmbeddingConfigProvider;
        _chunkingOptions = chunkingOptions.Value;
        _logger = logger;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
    }

    /// <inheritdoc/>
    public override async Task<RestoreReindexResult> RunAsync(WorkflowActivityContext context, RestoreReindexInput input)
        => await ReindexOneAsync(input).ConfigureAwait(false);

    /// <summary>Re-indexes one restored unit; shared by the bounded batch activity.</summary>
    public async Task<RestoreReindexResult> ReindexOneAsync(RestoreReindexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        IDatabase db = _redis.GetDatabase();
        string syntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId);

        RedisValue[] fields = await db.HashGetAsync(
            syntacticKey,
            ["content", "caseId", "embeddingProvider", "embeddingModel", "embeddingDimensions", "cloudeventSubject"]).ConfigureAwait(false);

        string? content = fields[0];
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                $"Re-indexing restored memory unit '{input.MemoryUnitId}' produced zero semantic chunks because its syntactic content is missing or blank.");
        }

        string caseId = fields[1].HasValue ? fields[1].ToString() : string.Empty;
        string embeddingProvider = fields[2].HasValue ? fields[2].ToString() : string.Empty;
        string embeddingModel = fields[3].HasValue ? fields[3].ToString() : string.Empty;
        string embeddingDimensionsText = fields[4].HasValue ? fields[4].ToString() : string.Empty;
        string? cloudEventSubject = fields[5].HasValue ? fields[5].ToString() : null;

        TenantEmbeddingConfig config = await _tenantEmbeddingConfigProvider
            .GetAsync(input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);

        // Story 26.2 review (P3): the target tenant's embedding config must match the export's attribution.
        // Readiness (below) only verifies the target index's self-consistency (dimensions vs the target config),
        // NOT the export vs the target — so if provider/model differ, re-embedding would produce vectors that
        // disagree with the restored (source) provider/model labels and the graph node's dimensions. Enforce the
        // documented restore prerequisite loudly here rather than silently writing inconsistent vectors.
        if (!string.IsNullOrEmpty(embeddingProvider)
            && !MatchesProviderAttribution(embeddingProvider, config.Provider, config.Model))
        {
            throw new InvalidOperationException(
                $"Restore embedding provider mismatch for memory unit {input.MemoryUnitId} in tenant {input.TenantId}: " +
                $"the export was embedded with '{embeddingProvider}' but the target tenant is configured for '{config.Provider}:{config.Model}'. " +
                "Align the target tenant's embedding provider with the export before restoring.");
        }

        if (!string.IsNullOrEmpty(embeddingModel)
            && !string.Equals(embeddingModel, config.Model, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Restore embedding model mismatch for memory unit {input.MemoryUnitId} in tenant {input.TenantId}: " +
                $"the export was embedded with '{embeddingModel}' but the target tenant is configured for '{config.Model}'. " +
                "Align the target tenant's embedding model with the export before restoring.");
        }

        if (!int.TryParse(embeddingDimensionsText, out int sourceDimensions)
            || sourceDimensions != config.Dimensions)
        {
            throw new InvalidOperationException(
                $"Restore embedding dimensions mismatch for memory unit {input.MemoryUnitId} in tenant {input.TenantId}: " +
                $"the export used '{embeddingDimensionsText}' dimensions but the target tenant is configured for '{config.Dimensions}'.");
        }

        // AC5: the tenant's semantic vector index must be provisioned (with matching dimensions) before writing.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Semantic, config.Dimensions, CancellationToken.None)
            .ConfigureAwait(false);

        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
            .ReadActiveMarkerAsync(db, input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);
        EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
            marker,
            config.Provider,
            config.Model,
            config.Dimensions);

        // Chunk identically to ingestion; re-embed each chunk via the target tenant's configured provider.
        IReadOnlyList<ContentChunk> chunks = new ContentChunker(_chunkingOptions).Split(content);
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"Re-indexing restored memory unit '{input.MemoryUnitId}' produced zero semantic chunks.");
        }

        await _embeddingClient.PrimeApiKeyAsync(input.TenantId, config, CancellationToken.None).ConfigureAwait(false);
        IReadOnlyList<float[]> vectors = await _embeddingClient
            .GenerateBatchAsync(chunks.Select(static c => c.Text).ToArray(), input.TenantId, config, CancellationToken.None)
            .ConfigureAwait(false);
        if (vectors.Count != chunks.Count)
        {
            throw new InvalidOperationException(
                $"Embedding provider returned {vectors.Count} vectors for {chunks.Count} restored chunks.");
        }

        // Provider/model are reused from the restored syntactic hash so the chunk hash matches the original
        // byte-for-byte; dimensions come from the (target) tenant config.
        for (int i = 0; i < chunks.Count; i++)
        {
            EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
                marker,
                config.Provider,
                config.Model,
                config.Dimensions);

            ContentChunk chunk = chunks[i];
            float[] vector = vectors[i];
            byte[] vectorBytes = MemoryMarshal.AsBytes(vector.AsSpan()).ToArray();
            if (vectorBytes.Length != config.Dimensions * sizeof(float))
            {
                throw new InvalidOperationException(
                    $"Re-embedded vector byte length {vectorBytes.Length} does not match expected {config.Dimensions * sizeof(float)} bytes for {config.Dimensions} dimensions.");
            }

            string chunkKey = IndexSchemaDefinitions.BuildSemanticChunkKey(input.TenantId, input.MemoryUnitId, chunk.Sequence);
            List<HashEntry> hashEntries =
            [
                new HashEntry("embedding", vectorBytes),
                new HashEntry("tenantId", input.TenantId),
                new HashEntry("memoryUnitId", input.MemoryUnitId),
                new HashEntry("caseId", caseId),
                new HashEntry("embeddingProvider", embeddingProvider),
                new HashEntry("embeddingModel", embeddingModel),
                new HashEntry("embeddingDimensions", config.Dimensions),
                new HashEntry("chunkSequence", chunk.Sequence),
                new HashEntry("chunkStartOffset", chunk.StartOffset),
                new HashEntry("chunkEndOffset", chunk.EndOffset),
                new HashEntry("chunkText", chunk.Text),
            ];

            if (!string.IsNullOrWhiteSpace(cloudEventSubject))
            {
                hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
            }

            await db.HashSetAsync(chunkKey, [.. hashEntries]).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Re-indexed memory unit {MemoryUnitId} for tenant {TenantId}: {ChunkCount} semantic chunk vectors.",
            input.MemoryUnitId,
            input.TenantId,
            chunks.Count);

        return new RestoreReindexResult(input.MemoryUnitId, chunks.Count);
    }

    /// <summary>Matches both provider-only and canonical provider:model export attribution.</summary>
    internal static bool MatchesProviderAttribution(string attribution, string provider, string model)
        => string.Equals(attribution, provider, StringComparison.OrdinalIgnoreCase)
            || string.Equals(attribution, $"{provider}:{model}", StringComparison.OrdinalIgnoreCase);
}
