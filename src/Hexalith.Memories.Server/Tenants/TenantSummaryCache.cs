// <copyright file="TenantSummaryCache.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tenants;

using System.Collections.Concurrent;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Options;

/// <summary>Short-lived per-process cache for expensive tenant summary read models.</summary>
public sealed class TenantSummaryCache
{
    private readonly ConcurrentDictionary<string, (TenantSummary Summary, DateTimeOffset ExpiresAt)> _cache = new(StringComparer.Ordinal);
    private readonly IOptions<TenantReadCacheOptions> _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="TenantSummaryCache"/> class.</summary>
    /// <param name="options">Tenant read-cache options.</param>
    /// <param name="timeProvider">Time provider.</param>
    public TenantSummaryCache(IOptions<TenantReadCacheOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets a cached summary or creates and stores a fresh summary.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="factory">Factory used on cache miss or expiry.</param>
    /// <returns>The cached or fresh tenant summary.</returns>
    public async Task<TenantSummary> GetOrCreateAsync(string tenantId, Func<Task<TenantSummary>> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(factory);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(tenantId, out (TenantSummary Summary, DateTimeOffset ExpiresAt) entry) &&
            entry.ExpiresAt > now)
        {
            return entry.Summary;
        }

        TenantSummary summary = await factory().ConfigureAwait(false);
        _cache[tenantId] = (summary, now + _options.Value.GetTenantSummaryTtl());
        return summary;
    }

    /// <summary>Invalidates a tenant summary cache entry.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    public void Invalidate(string tenantId)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            _cache.TryRemove(tenantId, out _);
        }
    }
}
