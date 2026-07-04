// <copyright file="RedisAggregateCaseMappingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

/// <summary>Redis-backed implementation of <see cref="IAggregateCaseMappingStore"/>.
/// Stores the authoritative aggregate-type → case-id map in a per-tenant hash and coordinates concurrent
/// first-time creation across scaled instances with a short-lived distributed lock key.</summary>
internal sealed class RedisAggregateCaseMappingStore : IAggregateCaseMappingStore
{
    private const int DeleteBatchSize = 1000;

    private readonly IConnectionMultiplexer _redis;

    public RedisAggregateCaseMappingStore([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        ArgumentNullException.ThrowIfNull(redis);
        _redis = redis;
    }

    public async Task<string?> GetCaseIdAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue value = await db.HashGetAsync(GetMapKey(tenantId), aggregateType).ConfigureAwait(false);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        return await db.HashLengthAsync(GetMapKey(tenantId)).ConfigureAwait(false);
    }

    public async Task<bool> TryAcquireCreationLockAsync(string tenantId, string aggregateType, TimeSpan leaseTtl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        return await db.StringSetAsync(GetLockKey(tenantId, aggregateType), "locked", leaseTtl, when: When.NotExists)
            .ConfigureAwait(false);
    }

    public async Task ReleaseCreationLockAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        _ = await db.KeyDeleteAsync(GetLockKey(tenantId, aggregateType)).ConfigureAwait(false);
    }

    public async Task<bool> TryStoreCaseIdAsync(string tenantId, string aggregateType, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        return await db.HashSetAsync(GetMapKey(tenantId), aggregateType, caseId, When.NotExists).ConfigureAwait(false);
    }

    public async Task<long> DeleteCaseMappingsAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisKey mapKey = GetMapKey(tenantId);
        List<RedisValue> fields = new(DeleteBatchSize);
        long deleted = 0;

        await foreach (HashEntry entry in db.HashScanAsync(mapKey, pageSize: DeleteBatchSize).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!entry.Value.IsNullOrEmpty && string.Equals(entry.Value.ToString(), caseId, StringComparison.Ordinal))
            {
                fields.Add(entry.Name);
            }

            if (fields.Count >= DeleteBatchSize)
            {
                deleted += await db.HashDeleteAsync(mapKey, [.. fields]).ConfigureAwait(false);
                fields.Clear();
            }
        }

        if (fields.Count > 0)
        {
            deleted += await db.HashDeleteAsync(mapKey, [.. fields]).ConfigureAwait(false);
        }

        return deleted;
    }

    private static string GetMapKey(string tenantId) => $"{tenantId}:eventstore:aggregate-case-map";

    private static string GetLockKey(string tenantId, string aggregateType)
        => $"{tenantId}:eventstore:aggregate-case-lock:{aggregateType}";
}
