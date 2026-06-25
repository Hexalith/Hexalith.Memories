// <copyright file="IngestDedupReservation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Outcome of a REST-ingress preflight dedup reservation attempt (Story 18.4).</summary>
internal enum IngestReservationOutcome
{
    /// <summary>The caller atomically reserved the dedup identity and owns the scheduled workflow.</summary>
    Reserved,

    /// <summary>A concurrent (or recent) same-identity ingest already reserved the key; the existing
    /// workflow instance id is returned so the loser observes the winner's memory unit.</summary>
    DuplicateInFlight,

    /// <summary>Redis is unavailable; the caller should proceed (fail-open, ADR 9.1-B) — the workflow-level
    /// permanent dedup key and <see cref="CheckIdempotencyActivity"/> remain authoritative.</summary>
    FailOpen,
}

/// <summary>Result of a REST-ingress preflight reservation attempt.</summary>
/// <param name="Outcome">The reservation outcome.</param>
/// <param name="ExistingInstanceId">On <see cref="IngestReservationOutcome.DuplicateInFlight"/>, the winning
/// ingest's workflow instance id; otherwise the caller's own instance id (Reserved) or <see langword="null"/>.</param>
internal readonly record struct IngestReservationResult(IngestReservationOutcome Outcome, string? ExistingInstanceId);

/// <summary>
/// Closes the REST <c>/api/ingest</c> race (MEM-4) by atomically reserving the dedup identity before the
/// ingestion workflow is scheduled, reusing the proven Redis <c>SET … NX</c> primitive. The reservation lives
/// on a dedicated <c>ingest-reserve:</c> key namespace whose value is the winning workflow's instance id, so
/// the losing concurrent ingest returns that instance id (and thus observes the same <c>MemoryUnitId</c>)
/// without scheduling a second workflow. Distinct from the workflow's permanent <c>dedup:</c> record, so the
/// <see cref="CheckIdempotencyActivity"/> / <see cref="SaveDedupKeyActivity"/> semantics and the EventStore
/// preflight path are untouched. Fails OPEN on Redis outage (ADR 9.1-B).
/// </summary>
internal sealed partial class IngestDedupReservation
{
    private const string ReservationKeyPrefix = "ingest-reserve:";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IngestDedupReservation> _logger;

    public IngestDedupReservation(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IngestDedupReservation> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <summary>Atomically reserves the dedup identity (token-keyed when a token is supplied, else
    /// sourceUri-keyed) for the supplied workflow instance id.</summary>
    public async Task<IngestReservationResult> TryReserveAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        string? idempotencyToken,
        string instanceId,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        string reservationKey = BuildReservationKey(tenantId, caseId, sourceUri, idempotencyToken);

        try
        {
            IDatabase db = _redis.GetDatabase();
            bool acquired = await db
                .StringSetAsync(reservationKey, instanceId, ttl, when: When.NotExists)
                .ConfigureAwait(false);
            if (acquired)
            {
                return new IngestReservationResult(IngestReservationOutcome.Reserved, instanceId);
            }

            RedisValue existing = await db.StringGetAsync(reservationKey).ConfigureAwait(false);
            if (existing.HasValue)
            {
                return new IngestReservationResult(IngestReservationOutcome.DuplicateInFlight, existing.ToString());
            }

            // The reservation expired between the NX set and the read — proceed (fail-open); the permanent
            // dedup key remains the authoritative safety net.
            return new IngestReservationResult(IngestReservationOutcome.FailOpen, null);
        }
        catch (RedisException ex)
        {
            LogFailOpen(_logger, reservationKey, ex.GetType().Name);
            return new IngestReservationResult(IngestReservationOutcome.FailOpen, null);
        }
        catch (TimeoutException ex)
        {
            LogFailOpen(_logger, reservationKey, ex.GetType().Name);
            return new IngestReservationResult(IngestReservationOutcome.FailOpen, null);
        }
    }

    /// <summary>Releases a held reservation so a retry is not permanently blocked when workflow scheduling
    /// fails after a successful reservation (mirrors the EventStore compensation; the TTL is the backstop).</summary>
    public async Task ReleaseAsync(
        string tenantId,
        string caseId,
        string sourceUri,
        string? idempotencyToken,
        CancellationToken cancellationToken)
    {
        string reservationKey = BuildReservationKey(tenantId, caseId, sourceUri, idempotencyToken);
        try
        {
            IDatabase db = _redis.GetDatabase();
            _ = await db.KeyDeleteAsync(reservationKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            LogReleaseFailed(_logger, reservationKey, ex.GetType().Name);
        }
        catch (TimeoutException ex)
        {
            LogReleaseFailed(_logger, reservationKey, ex.GetType().Name);
        }
    }

    private static string BuildReservationKey(string tenantId, string caseId, string sourceUri, string? idempotencyToken)
        => ReservationKeyPrefix + DedupKeyBuilder.BuildIdentityKey(tenantId, caseId, sourceUri, idempotencyToken);

    [LoggerMessage(
        EventId = 9131,
        Level = LogLevel.Warning,
        Message = "REST ingest preflight reservation failed-open for key {ReservationKey} ({ExceptionType}); workflow-level dedup is authoritative.")]
    private static partial void LogFailOpen(ILogger logger, string reservationKey, string exceptionType);

    [LoggerMessage(
        EventId = 9132,
        Level = LogLevel.Warning,
        Message = "REST ingest preflight reservation release failed for key {ReservationKey} ({ExceptionType}); reservation will expire naturally.")]
    private static partial void LogReleaseFailed(ILogger logger, string reservationKey, string exceptionType);
}
