// <copyright file="IPreflightDedupStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

/// <summary>Adapter over the Redis preflight dedup reservation. The default Server-side implementation uses
/// <c>StringSet(key, value, expiry, When.NotExists)</c> to atomically reserve a dedup key before scheduling
/// the workflow. A missing/failing Redis instance may fail-open per <see cref="TryReserveAsync"/> semantics.</summary>
public interface IPreflightDedupStore
{
    /// <summary>Attempts to atomically reserve a dedup key.</summary>
    /// <param name="dedupKey">The dedup key, typically <c>dedup:{tenantId}:{caseId}:{sha256(cloudEventId)}</c>.</param>
    /// <param name="ttl">Reservation TTL.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// <see cref="PreflightReservationResult.Reserved"/> on first-time acquisition;
    /// <see cref="PreflightReservationResult.Duplicate"/> when the key already exists;
    /// <see cref="PreflightReservationResult.FailOpen"/> when the store is unavailable and callers should proceed
    /// (the workflow-level permanent dedup key remains authoritative).
    /// </returns>
    Task<PreflightReservationResult> TryReserveAsync(
        string dedupKey,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    /// <summary>Releases a previously-reserved dedup key. Called when workflow scheduling fails after a
    /// successful reservation so the retry path sees a clean slate (AC #9).</summary>
    /// <param name="dedupKey">The dedup key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ReleaseAsync(string dedupKey, CancellationToken cancellationToken);
}

/// <summary>Outcome of a preflight reservation attempt.</summary>
public enum PreflightReservationResult
{
    /// <summary>Key reserved atomically — caller owns it.</summary>
    Reserved,

    /// <summary>Key already exists — this is a duplicate.</summary>
    Duplicate,

    /// <summary>Store unavailable — caller should proceed; workflow-level dedup is the safety net.</summary>
    FailOpen,
}
