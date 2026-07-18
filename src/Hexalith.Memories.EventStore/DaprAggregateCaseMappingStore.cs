// <copyright file="DaprAggregateCaseMappingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Dapr.Client;

using Microsoft.Extensions.Options;

/// <summary>Dapr-state-store implementation of <see cref="IAggregateCaseMappingStore"/>
/// (spec-infrastructure-dependency-abstraction — F6, Decision D30, ADR-IDA-001). Migrated off direct
/// Redis: the authoritative aggregate-type → case-id map is a single per-tenant state key holding a
/// serialized dictionary, mutated with ETag optimistic concurrency (bounded retry) so first-writer-wins
/// and delete-by-case semantics are preserved; the short-lived creation lock is a per-aggregate state key
/// written with FirstWrite (set-if-not-exists) concurrency + a TTL.</summary>
/// <remarks>Substrate note: the previous StackExchange.Redis implementation used a Redis hash
/// (<c>HSET/HGET/HLEN</c> + <c>HSET NX</c>) and a <c>SET NX</c> lock. Those Redis-native atomics are
/// re-expressed here via Dapr state ETag CAS; under high contention the CAS retries rather than blocking.
/// The atomic-reserve <see cref="RedisPreflightDedupStore"/> is deliberately NOT migrated (see ADR-IDA-001).</remarks>
internal sealed class DaprAggregateCaseMappingStore : IAggregateCaseMappingStore
{
    /// <summary>Bounded retry budget for ETag optimistic-concurrency conflicts on the map key.</summary>
    private const int MaxConcurrencyRetries = 8;

    private const string LockedValue = "locked";

    private readonly DaprClient _daprClient;
    private readonly string _stateStoreName;

    public DaprAggregateCaseMappingStore(DaprClient daprClient, IOptions<EventStoreStateStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(options);
        _daprClient = daprClient;
        _stateStoreName = options.Value.StateStoreName;
    }

    public async Task<string?> GetCaseIdAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, string> map = await GetMapAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return map.TryGetValue(aggregateType, out string? caseId) ? caseId : null;
    }

    public async Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<string, string> map = await GetMapAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return map.Count;
    }

    public async Task<bool> TryAcquireCreationLockAsync(string tenantId, string aggregateType, TimeSpan leaseTtl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        string lockKey = GetLockKey(tenantId, aggregateType);
        (string? existing, string etag) = await _daprClient
            .GetStateAndETagAsync<string?>(_stateStoreName, lockKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(existing))
        {
            return false;
        }

        // Set-if-not-exists: FirstWrite concurrency + the (empty when absent) ETag reserves the lease
        // atomically; a concurrent winner makes this save fail and we report the lock as not acquired.
        long ttlSeconds = Math.Max(1, (long)Math.Ceiling(leaseTtl.TotalSeconds));
        return await _daprClient.TrySaveStateAsync(
            _stateStoreName,
            lockKey,
            LockedValue,
            etag,
            new StateOptions { Concurrency = ConcurrencyMode.FirstWrite },
            BuildTtlMetadata(ttlSeconds),
            cancellationToken).ConfigureAwait(false);
    }

    public Task ReleaseCreationLockAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        cancellationToken.ThrowIfCancellationRequested();

        return _daprClient.DeleteStateAsync(_stateStoreName, GetLockKey(tenantId, aggregateType), cancellationToken: cancellationToken);
    }

    public async Task<bool> TryStoreCaseIdAsync(string tenantId, string aggregateType, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        string mapKey = GetMapKey(tenantId);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (Dictionary<string, string>? map, string etag) = await _daprClient
                .GetStateAndETagAsync<Dictionary<string, string>?>(_stateStoreName, mapKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            map ??= new(StringComparer.Ordinal);

            // First-writer-wins: an existing mapping is never overwritten (mirrors HSET NX).
            if (map.ContainsKey(aggregateType))
            {
                return false;
            }

            map[aggregateType] = caseId;
            bool saved = await _daprClient
                .TrySaveStateAsync(_stateStoreName, mapKey, map, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (saved)
            {
                return true;
            }
        }

        // Exhausted the retry budget under sustained contention; a concurrent writer has almost certainly
        // stored a value for this aggregate type, so report not-stored rather than risk a lost overwrite.
        return false;
    }

    public async Task<long> DeleteCaseMappingsAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        string mapKey = GetMapKey(tenantId);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (Dictionary<string, string>? map, string etag) = await _daprClient
                .GetStateAndETagAsync<Dictionary<string, string>?>(_stateStoreName, mapKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (map is null || map.Count == 0)
            {
                return 0;
            }

            string[] toRemove = map
                .Where(kvp => string.Equals(kvp.Value, caseId, StringComparison.Ordinal))
                .Select(kvp => kvp.Key)
                .ToArray();

            if (toRemove.Length == 0)
            {
                return 0;
            }

            foreach (string aggregateType in toRemove)
            {
                _ = map.Remove(aggregateType);
            }

            bool saved = await _daprClient
                .TrySaveStateAsync(_stateStoreName, mapKey, map, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (saved)
            {
                return toRemove.Length;
            }
        }

        return 0;
    }

    private static IReadOnlyDictionary<string, string> BuildTtlMetadata(long ttlSeconds)
        => new Dictionary<string, string>
        {
            ["ttlInSeconds"] = ttlSeconds.ToString(CultureInfo.InvariantCulture),
        };

    private static string GetMapKey(string tenantId) => $"{tenantId}:eventstore:aggregate-case-map";

    private static string GetLockKey(string tenantId, string aggregateType)
        => $"{tenantId}:eventstore:aggregate-case-lock:{aggregateType}";

    private async Task<Dictionary<string, string>> GetMapAsync(string tenantId, CancellationToken cancellationToken)
    {
        Dictionary<string, string>? map = await _daprClient
            .GetStateAsync<Dictionary<string, string>?>(_stateStoreName, GetMapKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return map ?? new(StringComparer.Ordinal);
    }
}
