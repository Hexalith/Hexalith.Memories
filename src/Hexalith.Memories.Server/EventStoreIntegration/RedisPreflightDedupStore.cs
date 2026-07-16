// <copyright file="RedisPreflightDedupStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Hexalith.Memories.EventStore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Server-side adapter implementing <see cref="IPreflightDedupStore"/> on top of the shared
/// Redis connection. Uses <c>StringSet(..., When.NotExists)</c> as the atomic reservation primitive and
/// fails OPEN on Redis outage so the workflow-level permanent dedup key remains the authoritative
/// safety net (ADR 9.1-B).</summary>
internal sealed partial class RedisPreflightDedupStore : IPreflightDedupStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPreflightDedupStore> _logger;

    public RedisPreflightDedupStore(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<RedisPreflightDedupStore> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    public async Task<PreflightReservationResult> TryReserveAsync(
        string dedupKey,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dedupKey);

        try
        {
            IDatabase db = _redis.GetDatabase();
            bool acquired = await db
                .StringSetAsync(dedupKey, PreflightDedupReservation.ReservedValue, ttl, when: When.NotExists)
                .ConfigureAwait(false);
            if (acquired)
            {
                return PreflightReservationResult.Reserved;
            }

            RedisValue existing = await db.StringGetAsync(dedupKey).ConfigureAwait(false);
            if (PreflightDedupReservation.IsTransientReservation(existing.ToString()))
            {
                LogTransientReservationFallback(_logger, dedupKey);
                return PreflightReservationResult.FailOpen;
            }

            return PreflightReservationResult.Duplicate;
        }
        catch (RedisException ex)
        {
            LogFailOpen(_logger, dedupKey, ex.GetType().Name);
            return PreflightReservationResult.FailOpen;
        }
        catch (TimeoutException ex)
        {
            LogFailOpen(_logger, dedupKey, ex.GetType().Name);
            return PreflightReservationResult.FailOpen;
        }
    }

    public async Task ReleaseAsync(string dedupKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dedupKey);

        try
        {
            IDatabase db = _redis.GetDatabase();
            _ = await db.KeyDeleteAsync(dedupKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            LogReleaseFailed(_logger, dedupKey, ex.GetType().Name);
        }
        catch (TimeoutException ex)
        {
            LogReleaseFailed(_logger, dedupKey, ex.GetType().Name);
        }
    }

    [LoggerMessage(
        EventId = 9123,
        Level = LogLevel.Warning,
        Message = "EventStore preflight dedup failed-open for key {DedupKey} ({ExceptionType}); workflow-level dedup is authoritative.")]
    private static partial void LogFailOpen(ILogger logger, string dedupKey, string exceptionType);

    [LoggerMessage(
        EventId = 9124,
        Level = LogLevel.Warning,
        Message = "EventStore preflight dedup release failed for key {DedupKey} ({ExceptionType}); reservation will expire naturally.")]
    private static partial void LogReleaseFailed(ILogger logger, string dedupKey, string exceptionType);

    [LoggerMessage(
        EventId = 9125,
        Level = LogLevel.Warning,
        Message = "EventStore preflight dedup encountered a transient reservation for key {DedupKey}; falling back to workflow-level dedup.")]
    private static partial void LogTransientReservationFallback(ILogger logger, string dedupKey);
}
