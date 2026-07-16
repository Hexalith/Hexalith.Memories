// <copyright file="BoundedCache.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Caching;

using System.Collections.Concurrent;

/// <summary>
/// Eviction helper for the short-lived per-process read caches (Story 24.2 review P4).
/// The tenant read caches are <see cref="ConcurrentDictionary{TKey,TValue}"/> instances with lazy
/// per-key expiry only; without a bound, distinct-key probing (e.g. negative tenant-status entries
/// seeded by <c>/api/v1/search</c> with attacker-controlled but valid-format tenant ids) grows the
/// dictionary without limit. This helper caps that growth before each insert.
/// </summary>
internal static class BoundedCache
{
    /// <summary>
    /// Prunes <paramref name="cache"/> back under <paramref name="maxEntries"/> when it has reached the cap.
    /// Removes already-expired entries first (the common case for short TTLs); if still at the cap with
    /// live entries, evicts those closest to expiry. A no-op while the cache is below the cap.
    /// </summary>
    /// <typeparam name="TValue">The cache value tuple type.</typeparam>
    /// <param name="cache">The cache to prune.</param>
    /// <param name="maxEntries">The maximum number of retained entries.</param>
    /// <param name="now">The current timestamp used to detect expired entries.</param>
    /// <param name="expiresAtSelector">Projects an entry value to its expiry timestamp.</param>
    public static void PruneIfNeeded<TValue>(
        ConcurrentDictionary<string, TValue> cache,
        int maxEntries,
        DateTimeOffset now,
        Func<TValue, DateTimeOffset> expiresAtSelector)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(expiresAtSelector);

        if (cache.Count < maxEntries)
        {
            return;
        }

        // First pass: drop entries whose TTL already elapsed.
        foreach (KeyValuePair<string, TValue> pair in cache)
        {
            if (expiresAtSelector(pair.Value) <= now)
            {
                cache.TryRemove(pair.Key, out _);
            }
        }

        if (cache.Count < maxEntries)
        {
            return;
        }

        // Second pass: still at the cap with live entries. Evict those closest to expiry so a single
        // caller can make room, bounding worst-case memory under sustained distinct-key load.
        int toRemove = (cache.Count - maxEntries) + 1;
        foreach (string key in cache
            .OrderBy(pair => expiresAtSelector(pair.Value))
            .Take(toRemove)
            .Select(pair => pair.Key)
            .ToArray())
        {
            cache.TryRemove(key, out _);
        }
    }
}
