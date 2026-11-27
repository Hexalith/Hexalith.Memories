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
/// (true HSET-NX analog). A per-tenant index enumerates active and retired mappings for
/// count, case deletion, and tenant purge. The creation lock is a per-aggregate FirstWrite + TTL key.</summary>
/// <remarks>Substrate note: the previous whole-document ETag-CAS map contended across unrelated aggregate
/// types. Per-key FirstWrite removes that cross-type contention. The atomic-reserve
/// <see cref="RedisPreflightDedupStore"/> is deliberately NOT migrated (see ADR-IDA-001).</remarks>
internal sealed class DaprAggregateCaseMappingStore : IAggregateCaseMappingStore
{
    /// <summary>Bounded retry budget for ETag optimistic-concurrency conflicts on the index key.</summary>
    private const int MaxConcurrencyRetries = 8;
    private const int MappingReadBatchSize = 64;

    private const string LockedValue = "locked";
    private const string DeletedMapping = "";

    private static readonly StateOptions FirstWriteOptions = new() { Concurrency = ConcurrencyMode.FirstWrite };
    private static readonly TimeSpan IndexRecoveryTimeout = TimeSpan.FromSeconds(5);

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

        string? caseId = await _daprClient
            .GetStateAsync<string?>(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrEmpty(caseId) ? null : caseId;
    }

    public async Task<long> GetAggregateCountAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        List<string>? index = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (index is null)
        {
            return 0;
        }

        long count = 0;
        foreach (string[] batch in index.Chunk(MappingReadBatchSize))
        {
            string[] keys = [.. batch.Select(aggregateType => GetMapEntryKey(tenantId, aggregateType))];
            IReadOnlyList<BulkStateItem<string>> entries = await _daprClient
                .GetBulkStateAsync<string>(_stateStoreName, keys, parallelism: 16, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            HashSet<string> resolvedKeys = new(StringComparer.Ordinal);
            foreach (BulkStateItem<string> entry in entries)
            {
                if (entry.Value is not null)
                {
                    _ = resolvedKeys.Add(entry.Key);
                    if (!string.IsNullOrEmpty(entry.Value))
                    {
                        count++;
                    }
                }
            }

            // The SDK omits per-item errors, so re-read unresolved entries before counting.
            // A direct read distinguishes an absent legacy row from a state-store failure.
            foreach (string key in keys)
            {
                if (!resolvedKeys.Contains(key))
                {
                    string? caseId = await _daprClient
                        .GetStateAsync<string?>(_stateStoreName, key, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(caseId))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
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
        catch (Exception failure)
        {
            // Cancellation or a transport failure can hide a committed index write.
            // Inspect it using an independent budget before removing the mapping.
            using var recovery = new CancellationTokenSource(IndexRecoveryTimeout);
            try
            {
                List<string>? index = await _daprClient
                    .GetStateAsync<List<string>?>(_stateStoreName, GetIndexKey(tenantId), cancellationToken: recovery.Token)
                    .ConfigureAwait(false);
                if (index?.Contains(aggregateType, StringComparer.Ordinal) != true)
                {
                    await _daprClient
                        .DeleteStateAsync(_stateStoreName, mapKey, cancellationToken: recovery.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception recoveryFailure)
            {
                failure.Data["CompensationFailure"] = recoveryFailure;
            }

            throw;
        }

        return true;
    }

    public async Task<long> DeleteCaseMappingsAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        cancellationToken.ThrowIfCancellationRequested();

        List<string>? index = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        long removedCount = 0;
        foreach (string aggregateType in index ?? [])
        {
            if (await RetireCaseMappingAsync(tenantId, aggregateType, caseId, cancellationToken).ConfigureAwait(false))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    public async Task DeleteAllTenantDataAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        string indexKey = GetIndexKey(tenantId);

        // Keep the index available until each map and lock delete is acknowledged.
        // A retry can then repeat the deletes for the same types after a partial failure.
        for (int attempt = 0; attempt <= MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (index is null && string.IsNullOrEmpty(etag))
            {
                return;
            }

            if (attempt == MaxConcurrencyRetries)
            {
                break;
            }

            if (string.IsNullOrEmpty(etag))
            {
                throw new InvalidOperationException("Cannot conditionally delete an aggregate-to-case mapping index without an ETag.");
            }

            foreach (string aggregateType in index ?? [])
            {
                await _daprClient
                    .DeleteStateAsync(_stateStoreName, GetMapEntryKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await _daprClient
                    .DeleteStateAsync(_stateStoreName, GetLockKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            bool cleared = await _daprClient
                .TryDeleteStateAsync(_stateStoreName, indexKey, etag, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!cleared)
            {
                await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
            }

            // Re-read after success to catch an index recreated before the purge returns.
        }

        throw new InvalidOperationException(
            $"Failed to purge aggregate→case mappings for tenant '{tenantId}' after {MaxConcurrencyRetries} concurrency retries.");
    }

    private async Task<bool> RetireCaseMappingAsync(
        string tenantId, string aggregateType, string caseId, CancellationToken cancellationToken)
    {
        string mapKey = GetMapEntryKey(tenantId, aggregateType);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string? mappedCaseId, string etag) = await _daprClient
                .GetStateAndETagAsync<string?>(_stateStoreName, mapKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(mappedCaseId, caseId, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrEmpty(etag))
            {
                throw new InvalidOperationException("Cannot conditionally retire an aggregate-to-case mapping without an ETag.");
            }

            // Preserve the key's ETag lineage. Deleting and recreating the key would
            // let an earlier delete act on a replacement with the recycled ETag.
            if (await _daprClient
                .TrySaveStateAsync(_stateStoreName, mapKey, DeletedMapping, etag, FirstWriteOptions, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                return true;
            }

            await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Failed to delete aggregate→case mapping for tenant '{tenantId}' and case '{caseId}' after {MaxConcurrencyRetries} concurrency retries.");
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

            List<string> nextIndex = [.. index, aggregateType];
            StateOptions? concurrency = string.IsNullOrEmpty(etag) ? FirstWriteOptions : null;
            try
            {
                if (await _daprClient
                    .TrySaveStateAsync(_stateStoreName, indexKey, nextIndex, etag, concurrency, cancellationToken: cancellationToken)
                    .ConfigureAwait(false))
                {
                    return;
                }
            }
            catch (DaprException) when (concurrency is not null)
            {
                // Redis can throw on a FirstWrite conflict. Retry only when an index now exists.
                List<string>? current = await _daprClient
                    .GetStateAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (current is null)
                {
                    throw;
                }

                if (current.Contains(aggregateType, StringComparer.Ordinal))
                {
                    return;
                }
            }

            await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Failed to index aggregate type '{aggregateType}' for tenant '{tenantId}' after {MaxConcurrencyRetries} concurrency retries.");
    }
}
