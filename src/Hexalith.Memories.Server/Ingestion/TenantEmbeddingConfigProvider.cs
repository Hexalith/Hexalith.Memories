// <copyright file="TenantEmbeddingConfigProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Collections.Concurrent;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Caching;

using Microsoft.Extensions.Options;

/// <summary>Per-process tenant embedding configuration cache backed by the tenant configuration actor.</summary>
public sealed class TenantEmbeddingConfigProvider : ITenantEmbeddingConfigProvider
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly ConcurrentDictionary<string, (TenantEmbeddingConfig Config, DateTimeOffset ExpiresAt)> _cache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (FusionWeights Weights, DateTimeOffset ExpiresAt)> _fusionWeightsCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _generations = new(StringComparer.Ordinal);
    private readonly IOptions<TenantEmbeddingConfigCacheOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="TenantEmbeddingConfigProvider"/> class.</summary>
    /// <param name="actorProxyFactory">The actor proxy factory.</param>
    /// <param name="options">The cache options.</param>
    /// <param name="timeProvider">The time provider.</param>
    public TenantEmbeddingConfigProvider(
        IActorProxyFactory actorProxyFactory,
        IOptions<TenantEmbeddingConfigCacheOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(actorProxyFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _actorProxyFactory = actorProxyFactory;
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task<TenantEmbeddingConfig> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(tenantId, out (TenantEmbeddingConfig Config, DateTimeOffset ExpiresAt) entry) &&
            entry.ExpiresAt > now)
        {
            return entry.Config;
        }

        // Capture the invalidation generation before the actor read so a write that invalidates while
        // we are fetching is not silently re-cached as a stale value (Story 24.2 review P2).
        long generation = GetGeneration(tenantId);

        ITenantConfigurationActor tenantConfigActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(tenantId),
                nameof(TenantConfigurationActor));

        TenantEmbeddingConfig config = await tenantConfigActor
            .GetEmbeddingConfigAsync()
            .ConfigureAwait(false);

        if (GetGeneration(tenantId) == generation)
        {
            DateTimeOffset storedAt = _timeProvider.GetUtcNow();
            BoundedCache.PruneIfNeeded(_cache, _options.Value.GetMaxCacheEntries(), storedAt, static e => e.ExpiresAt);
            _cache[tenantId] = (config, storedAt + GetCacheTtl());
        }

        return config;
    }

    /// <inheritdoc/>
    public async Task<FusionWeights> GetFusionWeightsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (_fusionWeightsCache.TryGetValue(tenantId, out (FusionWeights Weights, DateTimeOffset ExpiresAt) entry) &&
            entry.ExpiresAt > now)
        {
            return entry.Weights;
        }

        long generation = GetGeneration(tenantId);

        ITenantConfigurationActor tenantConfigActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(tenantId),
                nameof(TenantConfigurationActor));

        FusionWeights weights = await tenantConfigActor
            .GetFusionWeightsAsync()
            .ConfigureAwait(false);

        if (GetGeneration(tenantId) == generation)
        {
            DateTimeOffset storedAt = _timeProvider.GetUtcNow();
            BoundedCache.PruneIfNeeded(_fusionWeightsCache, _options.Value.GetMaxCacheEntries(), storedAt, static e => e.ExpiresAt);
            _fusionWeightsCache[tenantId] = (weights, storedAt + GetCacheTtl());
        }

        return weights;
    }

    /// <inheritdoc/>
    public void Invalidate(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        // Bump the generation so an in-flight read that already fetched the pre-write config/weights
        // cannot re-cache the stale value on top of this invalidation (Story 24.2 review P2).
        _generations.AddOrUpdate(tenantId, 1L, static (_, current) => current + 1L);
        _cache.TryRemove(tenantId, out _);
        _fusionWeightsCache.TryRemove(tenantId, out _);
    }

    private long GetGeneration(string tenantId)
        => _generations.TryGetValue(tenantId, out long generation) ? generation : 0L;

    private TimeSpan GetCacheTtl()
        => TimeSpan.FromSeconds(Math.Clamp(_options.Value.CacheTtlSeconds, 1, 300));
}
