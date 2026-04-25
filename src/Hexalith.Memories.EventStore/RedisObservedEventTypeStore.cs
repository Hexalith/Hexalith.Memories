// <copyright file="RedisObservedEventTypeStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Story 9.3 — Redis-backed <see cref="IObservedEventTypeStore"/> using a 3-key-per-aggregate
/// pattern pipelined through <see cref="IDatabaseAsync.CreateBatch"/> (see ADR-9.3-001 for the rationale
/// behind not using Redis Streams).</summary>
internal sealed class RedisObservedEventTypeStore : IObservedEventTypeStore
{
    /// <summary>TTL applied on every write — 2x the widest supported observation window (48h = 2×24h).</summary>
    internal static readonly TimeSpan KeyTtl = TimeSpan.FromHours(48);

    /// <summary>Cap on the per-tenant aggregates-index SET cardinality (Delta #10 — prevents a malicious
    /// publisher from inflating the discovery index with an unbounded stream of distinct aggregateTypes).</summary>
    internal const long AggregatesIndexCardinalityCap = 1024;

    /// <summary>Synthetic tenant id mirrored from <c>MemoriesMeter.RejectedTenantTag</c> — guarded here
    /// as defense-in-depth (Risk #9). MUST stay in sync with the Telemetry package constant.</summary>
    private const string RejectedTenantTag = "__rejected__";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisObservedEventTypeStore> _logger;

    public RedisObservedEventTypeStore(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<RedisObservedEventTypeStore> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    public async Task RecordObservationAsync(
        string tenantId,
        string aggregateType,
        string eventType,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        if (string.Equals(tenantId, RejectedTenantTag, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Tenant id '{RejectedTenantTag}' is reserved and cannot be used as an observation-store key prefix.",
                nameof(tenantId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisKey aggregatesIndexKey = GetAggregatesIndexKey(tenantId);
            RedisKey sortedSetKey = GetSortedSetKey(tenantId, aggregateType);
            RedisKey counterHashKey = GetCounterHashKey(tenantId, aggregateType);

            // Delta #10 — bounded aggregates-index set. Check cardinality before SADD; if cap is reached,
            // skip the SADD for this observation (ZADD + HINCRBY continue) and emit 9142 warning.
            long currentCardinality = await db.SetLengthAsync(aggregatesIndexKey).ConfigureAwait(false);
            bool willSkipSadd = currentCardinality >= AggregatesIndexCardinalityCap
                && !await db.SetContainsAsync(aggregatesIndexKey, aggregateType).ConfigureAwait(false);

            if (willSkipSadd)
            {
                EventStoreIntegrationLog.ObservationAggregatesSetCardinalityWarning(_logger, tenantId, currentCardinality);
            }

            IBatch batch = db.CreateBatch();
            List<Task> batchTasks = new(6);

            if (!willSkipSadd)
            {
                batchTasks.Add(batch.SetAddAsync(aggregatesIndexKey, aggregateType));
                batchTasks.Add(batch.KeyExpireAsync(aggregatesIndexKey, KeyTtl));
            }

            batchTasks.Add(batch.SortedSetAddAsync(
                sortedSetKey, eventType, observedAt.ToUnixTimeMilliseconds(), When.Always));
            batchTasks.Add(batch.HashIncrementAsync(counterHashKey, eventType, 1));
            batchTasks.Add(batch.KeyExpireAsync(sortedSetKey, KeyTtl));
            batchTasks.Add(batch.KeyExpireAsync(counterHashKey, KeyTtl));

            batch.Execute();
            await Task.WhenAll(batchTasks).ConfigureAwait(false);

            EventStoreIntegrationLog.ObservedEventTypeRecorded(_logger, tenantId, aggregateType, eventType);
        }
        catch (RedisException ex)
        {
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);

            // Fail-open — Risk #1 hot-path safety.
        }
        catch (TimeoutException ex)
        {
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);
        }
    }

    public async Task<IReadOnlyList<ObservedEventType>> GetObservedTypesAsync(
        string tenantId,
        string aggregateType,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisKey sortedSetKey = GetSortedSetKey(tenantId, aggregateType);
        RedisKey counterHashKey = GetCounterHashKey(tenantId, aggregateType);

        double minScore = (DateTimeOffset.UtcNow - window).ToUnixTimeMilliseconds();

        // ZRANGEBYSCORE + SORTED SET values with scores (event types observed within the window).
        SortedSetEntry[] entries = await db.SortedSetRangeByScoreWithScoresAsync(
            sortedSetKey,
            start: minScore,
            stop: double.PositiveInfinity,
            exclude: Exclude.None,
            order: Order.Descending).ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return Array.Empty<ObservedEventType>();
        }

        RedisValue[] eventTypeFields = entries
            .Select(e => (RedisValue)e.Element.ToString())
            .ToArray();

        RedisValue[] counts = await db.HashGetAsync(counterHashKey, eventTypeFields).ConfigureAwait(false);

        List<ObservedEventType> result = new(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            string eventType = entries[i].Element.ToString();
            long count = counts[i].IsNullOrEmpty ? 0 : (long)counts[i];
            DateTimeOffset lastSeenAt = DateTimeOffset.FromUnixTimeMilliseconds((long)entries[i].Score);
            result.Add(new ObservedEventType(aggregateType, eventType, count, lastSeenAt));
        }

        return result;
    }

    public async Task<IReadOnlyList<ObservedEventType>> GetAllObservedTypesAsync(
        string tenantId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] aggregateTypes = await db
            .SetMembersAsync(GetAggregatesIndexKey(tenantId))
            .ConfigureAwait(false);

        if (aggregateTypes.Length == 0)
        {
            return Array.Empty<ObservedEventType>();
        }

        List<ObservedEventType> aggregated = new();
        foreach (RedisValue aggregateType in aggregateTypes)
        {
            IReadOnlyList<ObservedEventType> perAggregate = await GetObservedTypesAsync(
                tenantId, aggregateType.ToString(), window, cancellationToken).ConfigureAwait(false);
            aggregated.AddRange(perAggregate);
        }

        return aggregated;
    }

    private static string GetAggregatesIndexKey(string tenantId) =>
        $"{tenantId}:eventstore:observed-aggregates";

    private static string GetSortedSetKey(string tenantId, string aggregateType) =>
        $"{tenantId}:eventstore:observed:{aggregateType}";

    private static string GetCounterHashKey(string tenantId, string aggregateType) =>
        $"{tenantId}:eventstore:observed-count:{aggregateType}";
}
