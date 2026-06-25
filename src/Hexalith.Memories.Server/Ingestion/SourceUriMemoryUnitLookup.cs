// <copyright file="SourceUriMemoryUnitLookup.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

/// <summary>
/// Story 18.5 — exact <c>sourceUri → MemoryUnitId</c> resolution seam over the permanent dedup record. Reads
/// the authoritative index written by <see cref="SaveDedupKeyActivity"/> (the <c>dedup:</c> key whose value is
/// the canonical <c>MemoryUnitId</c>) by exact key; it is NOT a search. Mirrors the keyed-<c>redis</c> read
/// shape of <see cref="CheckIdempotencyActivity"/>, including the transient-reservation exclusion (AC3), so the
/// lookup never returns the in-flight <see cref="PreflightDedupReservation.ReservedValue"/> marker as an id.
/// <para>
/// Backend errors are NOT swallowed: a Redis I/O failure propagates so the endpoint maps it to a structured
/// backend error rather than a false not-found (AC6) — the opposite calculus to the ingest path's fail-open
/// posture (ADR 9.1-B), because for an identity-resolving read a false not-found risks a duplicate re-ingest.
/// </para>
/// </summary>
internal sealed class SourceUriMemoryUnitLookup
{
    private readonly IConnectionMultiplexer _redis;

    /// <summary>Initializes a new instance of the <see cref="SourceUriMemoryUnitLookup"/> class.</summary>
    /// <param name="redis">The keyed <c>redis</c> connection multiplexer holding the permanent dedup record.</param>
    public SourceUriMemoryUnitLookup([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
    }

    /// <summary>
    /// Resolves the canonical <c>MemoryUnitId</c> for a known <paramref name="sourceUri"/> within a tenant and
    /// case by exact key (AC1/AC2). Returns <see langword="null"/> only for a genuine miss or when the key holds
    /// the transient in-flight reservation marker (AC3) — never for a backend I/O error, which is allowed to
    /// propagate (AC6).
    /// </summary>
    /// <param name="tenantId">The tenant identifier (embedded in the dedup key — structural tenant isolation).</param>
    /// <param name="caseId">The case identifier (embedded in the dedup key — structural case isolation).</param>
    /// <param name="sourceUri">The exact source URI to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The canonical <c>MemoryUnitId</c>, or <see langword="null"/> when no committed unit exists.</returns>
    /// <exception cref="RedisException">Propagated on a backend read failure so the caller maps it to a backend error (AC6).</exception>
    public async Task<string?> ResolveMemoryUnitIdAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUri);

        // Reuse the key builder — do NOT re-implement the hash (AC2). The value IS the MemoryUnitId.
        string dedupKey = DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri);

        IDatabase db = _redis.GetDatabase();
        RedisValue existing = await db.StringGetAsync(dedupKey).ConfigureAwait(false);

        // The permanent dedup key can transiently hold the EventStore preflight reservation marker while an
        // event-driven ingest is in flight; treat it as not-found exactly as CheckIdempotencyActivity does (AC3).
        if (PreflightDedupReservation.IsTransientReservation(existing.ToString()))
        {
            return null;
        }

        return existing.HasValue ? existing.ToString() : null;
    }
}
