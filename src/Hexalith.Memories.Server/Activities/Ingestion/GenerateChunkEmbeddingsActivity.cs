// <copyright file="GenerateChunkEmbeddingsActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

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
public sealed class GenerateChunkEmbeddingsActivity : WorkflowTraceLinkedActivity<EmbeddingInput, ChunkEmbeddingBatchResult>
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<GenerateChunkEmbeddingsActivity> _logger;
    private readonly ContentChunkingOptions _options;
    private readonly IWorkflowPayloadStore? _payloadStore;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ITenantEmbeddingConfigProvider _tenantEmbeddingConfigProvider;

    /// <summary>Initializes a new instance of the <see cref="GenerateChunkEmbeddingsActivity"/> class.</summary>
    public GenerateChunkEmbeddingsActivity(
        EmbeddingClient embeddingClient,
        IActorProxyFactory actorProxyFactory,
        IOptions<ContentChunkingOptions> options,
        ILogger<GenerateChunkEmbeddingsActivity> logger,
        [FromKeyedServices("redis")] IConnectionMultiplexer? redis = null,
        IWorkflowPayloadStore? payloadStore = null,
        ITenantEmbeddingConfigProvider? tenantEmbeddingConfigProvider = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentNullException.ThrowIfNull(actorProxyFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _embeddingClient = embeddingClient;
        _actorProxyFactory = actorProxyFactory;
        _options = options.Value;
        _logger = logger;
        _payloadStore = payloadStore;
        _redis = redis;
        _tenantEmbeddingConfigProvider = tenantEmbeddingConfigProvider
            ?? new TenantEmbeddingConfigProvider(
                actorProxyFactory,
                Options.Create(new TenantEmbeddingConfigCacheOptions()),
                TimeProvider.System);
    }

    /// <inheritdoc/>
    protected override async Task<ChunkEmbeddingBatchResult> RunActivityAsync(
        WorkflowActivityContext context,
        EmbeddingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        if (string.IsNullOrWhiteSpace(input.ContentText) && input.ContentReference is null)
        {
            throw new ArgumentException("ContentText or ContentReference is required.", nameof(input));
        }

        string contentText = input.ContentText;
        string memoryUnitId = input.ContentReference?.MemoryUnitId ?? "embedding";
        if (input.ContentReference is not null)
        {
            byte[] contentBytes = await RequirePayloadStore()
                .ReadAsync(
                    input.ContentReference,
                    input.TenantId,
                    input.ContentReference.MemoryUnitId,
                    WorkflowPayloadKind.ExtractedText,
                    CancellationToken.None)
                .ConfigureAwait(false);
            contentText = System.Text.Encoding.UTF8.GetString(contentBytes);
            memoryUnitId = input.ContentReference.MemoryUnitId;
        }

        ContentChunker chunker = new(_options);
        IReadOnlyList<ContentChunk> chunks = chunker.Split(contentText);

        TenantEmbeddingConfig config = await _tenantEmbeddingConfigProvider
            .GetAsync(input.TenantId, CancellationToken.None)
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

        bool providerCallInProgress = false;
        try
        {
            List<float[]> vectors = new(chunks.Count);
            for (int start = 0; start < chunks.Count; start += _options.MaxChunksPerBatch)
            {
                ContentChunk[] batchChunks = [.. chunks.Skip(start).Take(_options.MaxChunksPerBatch)];
                bool allowed = await rateLimiter.TryConsumeWithCeilingAsync(config.RateLimitPerMinute).ConfigureAwait(false);
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
                WorkflowPayloadReference? textReference = null;
                WorkflowPayloadReference? vectorReference = null;
                string chunkText = chunk.Text;
                float[] vector = vectors[i];
                if (_payloadStore is not null && input.ContentKind == EmbeddingContentKind.Payload)
                {
                    textReference = await _payloadStore
                        .SaveAsync(
                            input.TenantId,
                            memoryUnitId,
                            WorkflowPayloadKind.ChunkText,
                            System.Text.Encoding.UTF8.GetBytes(chunk.Text),
                            idSuffix: chunk.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(false);
                    byte[] vectorBytes = System.Runtime.InteropServices.MemoryMarshal.AsBytes(vectors[i].AsSpan()).ToArray();
                    vectorReference = await _payloadStore
                        .SaveAsync(
                            input.TenantId,
                            memoryUnitId,
                            WorkflowPayloadKind.ChunkVector,
                            vectorBytes,
                            idSuffix: chunk.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            cancellationToken: CancellationToken.None)
                        .ConfigureAwait(false);
                    chunkText = string.Empty;
                    vector = [];
                }

                results.Add(new ChunkEmbeddingResult
                {
                    Sequence = chunk.Sequence,
                    Text = chunkText,
                    TextReference = textReference,
                    StartOffset = chunk.StartOffset,
                    EndOffset = chunk.EndOffset,
                    EstimatedTokens = chunk.EstimatedTokens,
                    Vector = vector,
                    VectorReference = vectorReference,
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
            int retryAfter = EmbeddingRateLimitRetryAfter.NormalizeSeconds(ex.RetryAfterSeconds);
            RateLimitingLog.LogProviderRateLimitReceived(_logger, input.TenantId, retryAfter);
            await rateLimiter.ReportRateLimitedAsync(retryAfter).ConfigureAwait(false);
            throw new EmbeddingRateLimitException(input.TenantId, retryAfter);
        }
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "embedding-input");
}
