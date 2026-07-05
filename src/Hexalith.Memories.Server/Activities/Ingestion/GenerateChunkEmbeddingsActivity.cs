// <copyright file="GenerateChunkEmbeddingsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that chunks raw payload content and embeds it through the provider batch API.</summary>
public sealed class GenerateChunkEmbeddingsActivity : WorkflowActivity<EmbeddingInput, ChunkEmbeddingBatchResult>
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<GenerateChunkEmbeddingsActivity> _logger;
    private readonly ContentChunkingOptions _options;
    private readonly IConnectionMultiplexer? _redis;

    /// <summary>Initializes a new instance of the <see cref="GenerateChunkEmbeddingsActivity"/> class.</summary>
    public GenerateChunkEmbeddingsActivity(
        EmbeddingClient embeddingClient,
        IActorProxyFactory actorProxyFactory,
        IOptions<ContentChunkingOptions> options,
        ILogger<GenerateChunkEmbeddingsActivity> logger,
        [FromKeyedServices("redis")] IConnectionMultiplexer? redis = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentNullException.ThrowIfNull(actorProxyFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _embeddingClient = embeddingClient;
        _actorProxyFactory = actorProxyFactory;
        _options = options.Value;
        _logger = logger;
        _redis = redis;
    }

    /// <inheritdoc/>
    public override async Task<ChunkEmbeddingBatchResult> RunAsync(
        WorkflowActivityContext context,
        EmbeddingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ContentText);

        ContentChunker chunker = new(_options);
        IReadOnlyList<ContentChunk> chunks = chunker.Split(input.ContentText);

        ITenantConfigurationActor tenantConfigActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(input.TenantId),
                nameof(TenantConfigurationActor));

        TenantEmbeddingConfig config = await tenantConfigActor
            .GetEmbeddingConfigAsync()
            .ConfigureAwait(false);

        if (_redis is not null)
        {
            EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
                .ReadActiveMarkerAsync(_redis, input.TenantId, CancellationToken.None)
                .ConfigureAwait(false);
            EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
                marker,
                config.Provider,
                config.Model,
                config.Dimensions);
        }

        await _embeddingClient
            .PrimeApiKeyAsync(input.TenantId, config, CancellationToken.None)
            .ConfigureAwait(false);

        IEmbeddingRateLimiterActor rateLimiter = _actorProxyFactory
            .CreateActorProxy<IEmbeddingRateLimiterActor>(
                new ActorId(input.TenantId),
                nameof(EmbeddingRateLimiterActor));

        await rateLimiter.SetCeilingAsync(config.RateLimitPerMinute).ConfigureAwait(false);

        bool providerCallInProgress = false;
        try
        {
            List<float[]> vectors = new(chunks.Count);
            for (int start = 0; start < chunks.Count; start += _options.MaxChunksPerBatch)
            {
                ContentChunk[] batchChunks = [.. chunks.Skip(start).Take(_options.MaxChunksPerBatch)];
                bool allowed = await rateLimiter.TryConsumeAsync().ConfigureAwait(false);
                if (!allowed)
                {
                    RateLimitingLog.LogRateLimitExceededLocally(_logger, input.TenantId);
                    throw new EmbeddingRateLimitException(input.TenantId);
                }

                MemoriesMeter.EmbeddingApiCalls.Add(
                    1,
                    new KeyValuePair<string, object?>("tenant_id", input.TenantId),
                    new KeyValuePair<string, object?>("content_kind", "payload"));

                providerCallInProgress = true;
                IReadOnlyList<float[]> batchVectors = await _embeddingClient
                    .GenerateBatchAsync(batchChunks.Select(static c => c.Text).ToArray(), input.TenantId, config, CancellationToken.None)
                    .ConfigureAwait(false);
                providerCallInProgress = false;

                if (batchVectors.Count != batchChunks.Length)
                {
                    throw new InvalidOperationException(
                        $"Batch embedding returned {batchVectors.Count} vectors for {batchChunks.Length} chunks.");
                }

                vectors.AddRange(batchVectors);
            }

            if (vectors.Count != chunks.Count)
            {
                throw new InvalidOperationException(
                    $"Batch embedding returned {vectors.Count} vectors for {chunks.Count} chunks.");
            }

            List<ChunkEmbeddingResult> results = new(chunks.Count);
            for (int i = 0; i < chunks.Count; i++)
            {
                if (vectors[i].Length != config.Dimensions)
                {
                    throw new InvalidOperationException(
                        $"Chunk {i} vector dimension {vectors[i].Length} does not match configured dimension {config.Dimensions}.");
                }

                ContentChunk chunk = chunks[i];
                results.Add(new ChunkEmbeddingResult
                {
                    Sequence = chunk.Sequence,
                    Text = chunk.Text,
                    StartOffset = chunk.StartOffset,
                    EndOffset = chunk.EndOffset,
                    EstimatedTokens = chunk.EstimatedTokens,
                    Vector = vectors[i],
                });
            }

            return new ChunkEmbeddingBatchResult
            {
                Chunks = results,
                Provider = $"{config.Provider}:{config.Model}",
                Model = config.Model,
                Dimensions = config.Dimensions,
            };
        }
        catch (EmbeddingRateLimitException ex) when (providerCallInProgress)
        {
            int retryAfter = ex.RetryAfterSeconds > 0 ? ex.RetryAfterSeconds : 30;
            RateLimitingLog.LogProviderRateLimitReceived(_logger, input.TenantId, retryAfter);
            await rateLimiter.ReportRateLimitedAsync(retryAfter).ConfigureAwait(false);
            throw;
        }
    }
}
