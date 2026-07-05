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

using Microsoft.Extensions.Options;

/// <summary>Per-process tenant embedding configuration cache backed by the tenant configuration actor.</summary>
public sealed class TenantEmbeddingConfigProvider : ITenantEmbeddingConfigProvider
{
    private readonly IActorProxyFactory _actorProxyFactory;
    private readonly ConcurrentDictionary<string, (TenantEmbeddingConfig Config, DateTimeOffset ExpiresAt)> _cache = new(StringComparer.Ordinal);
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

        ITenantConfigurationActor tenantConfigActor = _actorProxyFactory
            .CreateActorProxy<ITenantConfigurationActor>(
                new ActorId(tenantId),
                nameof(TenantConfigurationActor));

        TenantEmbeddingConfig config = await tenantConfigActor
            .GetEmbeddingConfigAsync()
            .ConfigureAwait(false);

        _cache[tenantId] = (config, now + GetCacheTtl());
        return config;
    }

    private TimeSpan GetCacheTtl()
        => TimeSpan.FromSeconds(Math.Clamp(_options.Value.CacheTtlSeconds, 1, 300));
}
