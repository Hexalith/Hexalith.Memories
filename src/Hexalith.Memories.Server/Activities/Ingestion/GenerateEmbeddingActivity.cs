// <copyright file="GenerateEmbeddingActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using System.Collections.Concurrent;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Migration;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that generates embeddings via configurable provider API with per-tenant rate limiting.</summary>
public sealed class GenerateEmbeddingActivity : WorkflowActivity<EmbeddingInput, EmbeddingResult>
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> RetryTrackingKeys = new(StringComparer.Ordinal);
    private static readonly TimeSpan RetryTrackingTtl = TimeSpan.FromHours(1);

    private const int DefaultRetryAfterSecondsOn429 = 30;
    private const int JitterMaxExclusiveMilliseconds = 500;

    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IJitterSource _jitterSource;
    private readonly ILogger<GenerateEmbeddingActivity> _logger;
    private readonly IConnectionMultiplexer? _redis;

    /// <summary>Initializes a new instance of the <see cref="GenerateEmbeddingActivity"/> class.</summary>
    /// <param name="embeddingClient">The embedding client for provider API calls.</param>
    /// <param name="actorProxyFactory">The factory for creating DAPR actor proxies.</param>
    /// <param name="jitterSource">Jitter source providing the pre-call retry delay (Story 6.2).</param>
    /// <param name="logger">Logger for structured rate-limit events (6201-6203).</param>
    /// <param name="redis">The tenant-scoped migration marker store.</param>
    public GenerateEmbeddingActivity(
        EmbeddingClient embeddingClient,
        IActorProxyFactory actorProxyFactory,
        IJitterSource jitterSource,
        ILogger<GenerateEmbeddingActivity> logger,
        [FromKeyedServices("redis")] IConnectionMultiplexer? redis = null)
    {
        ArgumentNullException.ThrowIfNull(embeddingClient);
        ArgumentNullException.ThrowIfNull(actorProxyFactory);
        ArgumentNullException.ThrowIfNull(jitterSource);
        ArgumentNullException.ThrowIfNull(logger);
        _embeddingClient = embeddingClient;
        _actorProxyFactory = actorProxyFactory;
        _jitterSource = jitterSource;
        _logger = logger;
        _redis = redis;
    }

    /// <inheritdoc/>
    public override async Task<EmbeddingResult> RunAsync(
        WorkflowActivityContext context,
        EmbeddingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ContentText);

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

        bool allowed = await rateLimiter.TryConsumeAsync().ConfigureAwait(false);
        if (!allowed)
        {
            RateLimitingLog.LogRateLimitExceededLocally(_logger, input.TenantId);
            throw new EmbeddingRateLimitException(input.TenantId);
        }

        // Dapr.Workflow 1.17.6 exposes InstanceId/TaskExecutionKey but no retry-attempt counter on
        // WorkflowActivityContext. This workflow invokes GenerateEmbeddingActivity at most once per
        // workflow instance, so repeated executions for the same instance are retry attempts.
        if (ShouldApplyRetryJitter(context))
        {
            int jitterMs = _jitterSource.NextMilliseconds(JitterMaxExclusiveMilliseconds);
            if (jitterMs > 0)
            {
                await Task.Delay(jitterMs, CancellationToken.None).ConfigureAwait(false);
            }
        }

        // Story 9.2 Task 3.3 / Risk #6: partition embedding API volume by content kind so operators can
        // see the raw-payload / NL-description 2:1 split under dual-embedding.
        string contentKindTag = input.ContentKind == EmbeddingContentKind.NaturalLanguageDescription
            ? "naturalLanguageDescription"
            : "payload";
        MemoriesMeter.EmbeddingApiCalls.Add(
            1,
            new KeyValuePair<string, object?>("tenant_id", input.TenantId),
            new KeyValuePair<string, object?>("content_kind", contentKindTag));

        try
        {
            float[] vector = await _embeddingClient
                .GenerateAsync(input.ContentText, input.TenantId, config, CancellationToken.None)
                .ConfigureAwait(false);

            ClearRetryTracking(context);

            return new EmbeddingResult(vector, $"{config.Provider}:{config.Model}", config.Dimensions)
            {
                Model = config.Model,
            };
        }
        catch (EmbeddingRateLimitException ex)
        {
            int retryAfter = ex.RetryAfterSeconds > 0 ? ex.RetryAfterSeconds : DefaultRetryAfterSecondsOn429;
            RateLimitingLog.LogProviderRateLimitReceived(_logger, input.TenantId, retryAfter);
            await rateLimiter.ReportRateLimitedAsync(retryAfter).ConfigureAwait(false);
            throw;
        }
    }

    private static void ClearRetryTracking(WorkflowActivityContext context)
    {
        string? trackingKey = GetRetryTrackingKey(context);
        if (!string.IsNullOrWhiteSpace(trackingKey))
        {
            RetryTrackingKeys.TryRemove(trackingKey, out _);
        }
    }

    private static string? GetRetryTrackingKey(WorkflowActivityContext context)
        => !string.IsNullOrWhiteSpace(context.InstanceId)
            ? context.InstanceId
            : !string.IsNullOrWhiteSpace(context.TaskExecutionKey)
                ? context.TaskExecutionKey
                : null;

    private static bool ShouldApplyRetryJitter(WorkflowActivityContext context)
    {
        CleanupExpiredRetryTrackingEntries();

        string? trackingKey = GetRetryTrackingKey(context);
        if (string.IsNullOrWhiteSpace(trackingKey))
        {
            return false;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool isRetryAttempt = !RetryTrackingKeys.TryAdd(trackingKey, now);
        RetryTrackingKeys[trackingKey] = now;
        return isRetryAttempt;
    }

    private static void CleanupExpiredRetryTrackingEntries()
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - RetryTrackingTtl;
        foreach ((string trackingKey, DateTimeOffset seenAt) in RetryTrackingKeys)
        {
            if (seenAt < cutoff)
            {
                RetryTrackingKeys.TryRemove(trackingKey, out _);
            }
        }
    }
}
