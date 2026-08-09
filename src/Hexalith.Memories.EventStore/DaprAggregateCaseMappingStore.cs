// <copyright file="DaprAggregateCaseMappingStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Dapr;
using Dapr.Client;

using Microsoft.Extensions.Options;

/// <summary>Dapr-state-store implementation of <see cref="IAggregateCaseMappingStore"/>
/// (spec-infrastructure-dependency-abstraction — F6, Decision D30, ADR-IDA-001; review D1 redesign).
/// Each aggregate type is its own state key written with <see cref="ConcurrencyMode.FirstWrite"/>
/// (true HSET-NX analog); a per-tenant index enumerates mapped types for count/delete/purge.
/// The short-lived creation lock remains a per-aggregate FirstWrite + TTL key.</summary>
/// <remarks>Substrate note: the previous whole-document ETag-CAS map contended across unrelated aggregate
/// types. Per-key FirstWrite removes that cross-type contention. The atomic-reserve
/// <see cref="RedisPreflightDedupStore"/> is deliberately NOT migrated (see ADR-IDA-001).</remarks>
internal sealed class DaprAggregateCaseMappingStore : IAggregateCaseMappingStore
{
    /// <summary>Bounded retry budget for ETag optimistic-concurrency conflicts on the index key.</summary>
    private const int MaxConcurrencyRetries = 8;

    private const string LockedValue = "locked";

    private static readonly StateOptions FirstWriteOptions = new() { Concurrency = ConcurrencyMode.FirstWrite };

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

        return await _daprClient
            .GetStateAsync<string?>(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        List<string>? index = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return index?.Count ?? 0;
    }

    public async Task<bool> TryAcquireCreationLockAsync(string tenantId, string aggregateType, TimeSpan leaseTtl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(leaseTtl, TimeSpan.Zero);
        cancellationToken.ThrowIfCancellationRequested();

        string lockKey = GetLockKey(tenantId, aggregateType);
        (string? existing, string etag) = await _daprClient
            .GetStateAndETagAsync<string?>(_stateStoreName, lockKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(existing))
        {
            return false;
        }

        // Preserve prior Redis semantics: positive sub-second leases still round up to 1s TTL metadata.
        long ttlSeconds = Math.Max(1, (long)Math.Ceiling(leaseTtl.TotalSeconds));
        try
        {
            // Redis state.redis may throw DaprException on FirstWrite conflict instead of returning false.
            return await _daprClient.TrySaveStateAsync(
                _stateStoreName,
                lockKey,
                LockedValue,
                etag,
                FirstWriteOptions,
                BuildTtlMetadata(ttlSeconds),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DaprException)
        {
            return false;
        }
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

        string mapKey = GetMapEntryKey(tenantId, aggregateType);
        (string? existing, string etag) = await _daprClient
            .GetStateAndETagAsync<string?>(_stateStoreName, mapKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // First-writer-wins: an existing mapping is never overwritten (mirrors HSET NX).
        if (!string.IsNullOrEmpty(existing))
        {
            return false;
        }

        bool saved;
        try
        {
            // Redis state.redis may throw DaprException on FirstWrite conflict instead of returning false.
            saved = await _daprClient
                .TrySaveStateAsync(_stateStoreName, mapKey, caseId, etag, FirstWriteOptions, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DaprException)
        {
            return false;
        }

        if (!saved)
        {
            // Concurrent FirstWrite winner — report not-stored so the caller re-reads the winner.
            return false;
        }

        try
        {
            await EnsureIndexedAsync(tenantId, aggregateType, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Compensate: an unindexed map entry is invisible to purge/delete-by-case (review patch #2).
            await _daprClient
                .DeleteStateAsync(_stateStoreName, mapKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            throw;
        }

        return true;
    }

    public async Task<long> DeleteCaseMappingsAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        string indexKey = GetIndexKey(tenantId);
        List<string>? pendingRemovals = null;
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (index is null || index.Count == 0)
            {
                return 0;
            }

            // Keep the prior toRemove set across retries so map/index cannot drift (review patch #1).
            if (pendingRemovals is null)
            {
                pendingRemovals = [];
                foreach (string aggregateType in index)
                {
                    string? mappedCaseId = await _daprClient
                        .GetStateAsync<string?>(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (string.Equals(mappedCaseId, caseId, StringComparison.Ordinal))
                    {
                        pendingRemovals.Add(aggregateType);
                    }
                }
            }

            if (pendingRemovals.Count == 0)
            {
                return 0;
            }

            List<string> nextIndex = index
                .Where(aggregateType => !pendingRemovals.Contains(aggregateType, StringComparer.Ordinal))
                .ToList();

            // Defer map-key deletes until the index ETag save succeeds.
            bool saved = await _daprClient
                .TrySaveStateAsync(_stateStoreName, indexKey, nextIndex, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!saved)
            {
                await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            foreach (string aggregateType in pendingRemovals)
            {
                await _daprClient
                    .DeleteStateAsync(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            return pendingRemovals.Count;
        }

        throw new InvalidOperationException(
            $"Failed to delete aggregate→case mappings for tenant '{tenantId}' case '{caseId}' after {MaxConcurrencyRetries} concurrency retries.");
    }

    public async Task DeleteAllTenantDataAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        string indexKey = GetIndexKey(tenantId);

        // CAS-clear the index first, then delete map/lock keys from each snapshot, and re-read until
        // empty so concurrent TryStoreCaseIdAsync cannot leave post-purge leftovers (review patch #18).
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (index is null || index.Count == 0)
            {
                await _daprClient.DeleteStateAsync(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            List<string> snapshot = [.. index];
            bool cleared = await _daprClient
                .TryDeleteStateAsync(_stateStoreName, indexKey, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!cleared)
            {
                await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            foreach (string aggregateType in snapshot)
            {
                await _daprClient
                    .DeleteStateAsync(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await _daprClient
                    .DeleteStateAsync(_stateStoreName, GetLockKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            // Loop again: a concurrent TryStore may have recreated the index after our CAS-delete.
        }

        throw new InvalidOperationException(
            $"Failed to purge aggregate→case mappings for tenant '{tenantId}' after {MaxConcurrencyRetries} concurrency retries.");
    }

    private static IReadOnlyDictionary<string, string> BuildTtlMetadata(long ttlSeconds)
        => new Dictionary<string, string>
        {
            ["ttlInSeconds"] = ttlSeconds.ToString(CultureInfo.InvariantCulture),
        };

    private static string GetMapEntryKey(string tenantId, string aggregateType)
        => $"{tenantId}:eventstore:aggregate-case-map:{aggregateType}";

    private static string GetIndexKey(string tenantId) => $"{tenantId}:eventstore:aggregate-case-map-index";

    private static string GetLockKey(string tenantId, string aggregateType)
        => $"{tenantId}:eventstore:aggregate-case-lock:{aggregateType}";

    private static async Task DelayBackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        int delayMs = Random.Shared.Next(1, 1 << Math.Min(attempt + 1, 4));
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureIndexedAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        string indexKey = GetIndexKey(tenantId);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            index ??= [];

            if (index.Contains(aggregateType, StringComparer.Ordinal))
            {
                return;
            }

            index.Add(aggregateType);
            if (await _daprClient
                .TrySaveStateAsync(_stateStoreName, indexKey, index, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Failed to index aggregate type '{aggregateType}' for tenant '{tenantId}' after {MaxConcurrencyRetries} concurrency retries.");
    }
}
