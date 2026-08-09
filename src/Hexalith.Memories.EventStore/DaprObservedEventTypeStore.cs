// <copyright file="DaprObservedEventTypeStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dapr;
using Dapr.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.3 — Dapr-state-store implementation of <see cref="IObservedEventTypeStore"/>
/// (spec-infrastructure-dependency-abstraction — F6, Decision D30, ADR-IDA-001; review D1 redesign).
/// Per-aggregate observation dictionaries plus FirstWrite membership markers remove cross-type contention
/// on discovery-index rewrites; an uncapped written-aggregates index preserves deletion coverage for
/// cap-rejected types. Window filtering is in-memory; write posture stays <b>fail-open</b> except for
/// written-index CAS exhaustion (D3 purge enumerability — fail-loud after observation compensate).</summary>
internal sealed class DaprObservedEventTypeStore : IObservedEventTypeStore
{
    /// <summary>TTL applied on every write — 2x the widest supported observation window (48h = 2×24h).</summary>
    internal static readonly TimeSpan KeyTtl = TimeSpan.FromHours(48);

    /// <summary>Cap on the per-tenant discovery-index cardinality (Delta #10).</summary>
    internal const long AggregatesIndexCardinalityCap = 1024;

    /// <summary>Bounded retry budget for ETag optimistic-concurrency conflicts.</summary>
    private const int MaxConcurrencyRetries = 8;

    private const string MembershipValue = "1";

    /// <summary>Synthetic tenant id mirrored from <c>MemoriesMeter.RejectedTenantTag</c>.</summary>
    private const string RejectedTenantTag = "__rejected__";

    private static readonly StateOptions FirstWriteOptions = new() { Concurrency = ConcurrencyMode.FirstWrite };

    private readonly DaprClient _daprClient;
    private readonly string _stateStoreName;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DaprObservedEventTypeStore> _logger;

    public DaprObservedEventTypeStore(
        DaprClient daprClient,
        IOptions<EventStoreStateStoreOptions> options,
        TimeProvider timeProvider,
        ILogger<DaprObservedEventTypeStore> logger)
    {
        ArgumentNullException.ThrowIfNull(daprClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _daprClient = daprClient;
        _stateStoreName = options.Value.StateStoreName;
        _timeProvider = timeProvider;
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
            bool observationsSaved = await UpdateObservationsAsync(tenantId, aggregateType, eventType, observedAt, cancellationToken)
                .ConfigureAwait(false);
            if (!observationsSaved)
            {
                EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, "CasExhausted");
                return;
            }

            // Uncapped written index always records the aggregate so tenant deletion can find
            // cap-rejected observation keys the discovery index does not list (review D3).
            // Fail-loud on CAS exhaustion after compensating the observation key (review patch #4).
            await EnsureWrittenIndexedAsync(tenantId, aggregateType, cancellationToken).ConfigureAwait(false);

            (bool rejectedAtCap, long preCardinality, bool discoveryRecorded) =
                await UpdateDiscoveryIndexAsync(tenantId, aggregateType, cancellationToken).ConfigureAwait(false);

            if (!discoveryRecorded)
            {
                // Discovery CAS exhausted — do not claim the observation was fully recorded (patch #5).
                return;
            }

            if (rejectedAtCap)
            {
                EventStoreIntegrationLog.ObservationAggregatesSetCardinalityWarning(_logger, tenantId, preCardinality);
            }

            EventStoreIntegrationLog.ObservedEventTypeRecorded(_logger, tenantId, aggregateType, eventType);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Sidecar-shutdown race: treat as fail-open on the ingestion hot path (review P2).
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, nameof(OperationCanceledException));
        }
        catch (Dapr.DaprException ex)
        {
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);
        }
        catch (TimeoutException ex)
        {
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);
        }
        catch (System.Text.Json.JsonException ex)
        {
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // Corrupt/out-of-range unix-ms conversion must stay on the fail-open surface (review patch #17).
            EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, ex.GetType().Name);
        }
        catch (InvalidOperationException ex) when (ex.Data["WrittenIndexCasExhausted"] is not true)
        {
            // Schema-drift / unexpected serialized shape from GetStateAndETagAsync (not written-index fail-loud).
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

        Dictionary<string, ObservationCounter>? observations = await _daprClient
            .GetStateAsync<Dictionary<string, ObservationCounter>?>(
                _stateStoreName, GetObservationsKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (observations is null || observations.Count == 0)
        {
            return Array.Empty<ObservedEventType>();
        }

        long minUnixMs = (_timeProvider.GetUtcNow() - window).ToUnixTimeMilliseconds();

        List<ObservedEventType> results = [];
        foreach (KeyValuePair<string, ObservationCounter> kvp in observations
            .Where(kvp => kvp.Value.LastSeenAtUnixMs >= minUnixMs)
            .OrderByDescending(kvp => kvp.Value.LastSeenAtUnixMs))
        {
            try
            {
                results.Add(new ObservedEventType(
                    aggregateType,
                    kvp.Key,
                    kvp.Value.Count,
                    DateTimeOffset.FromUnixTimeMilliseconds(kvp.Value.LastSeenAtUnixMs)));
            }
            catch (ArgumentOutOfRangeException)
            {
                // Skip corrupt LastSeenAtUnixMs rather than failing the read path.
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<ObservedEventType>> GetAllObservedTypesAsync(
        string tenantId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        List<string>? aggregateTypes = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetDiscoveryIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (aggregateTypes is null || aggregateTypes.Count == 0)
        {
            return Array.Empty<ObservedEventType>();
        }

        List<ObservedEventType> aggregated = [];
        foreach (string aggregateType in aggregateTypes)
        {
            IReadOnlyList<ObservedEventType> perAggregate = await GetObservedTypesAsync(
                tenantId, aggregateType, window, cancellationToken).ConfigureAwait(false);
            aggregated.AddRange(perAggregate);
        }

        return aggregated;
    }

    public async Task DeleteAllTenantDataAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string> aggregateTypes = new(StringComparer.Ordinal);

        List<string>? written = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetWrittenIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (written is not null)
        {
            foreach (string aggregateType in written)
            {
                _ = aggregateTypes.Add(aggregateType);
            }
        }

        List<string>? discovery = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetDiscoveryIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (discovery is not null)
        {
            foreach (string aggregateType in discovery)
            {
                _ = aggregateTypes.Add(aggregateType);
            }
        }

        foreach (string aggregateType in aggregateTypes)
        {
            await _daprClient
                .DeleteStateAsync(_stateStoreName, GetObservationsKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _daprClient
                .DeleteStateAsync(_stateStoreName, GetMembershipKey(tenantId, aggregateType), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        await _daprClient.DeleteStateAsync(_stateStoreName, GetDiscoveryIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await _daprClient.DeleteStateAsync(_stateStoreName, GetWrittenIndexKey(tenantId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static string GetDiscoveryIndexKey(string tenantId) =>
        $"{tenantId}:eventstore:observed-aggregates";

    private static string GetWrittenIndexKey(string tenantId) =>
        $"{tenantId}:eventstore:observed-written-aggregates";

    private static string GetMembershipKey(string tenantId, string aggregateType) =>
        $"{tenantId}:eventstore:observed-agg-member:{aggregateType}";

    private static string GetObservationsKey(string tenantId, string aggregateType) =>
        $"{tenantId}:eventstore:observed:{aggregateType}";

    private static IReadOnlyDictionary<string, string> TtlMetadata()
        => new Dictionary<string, string>
        {
            ["ttlInSeconds"] = ((long)KeyTtl.TotalSeconds).ToString(CultureInfo.InvariantCulture),
        };

    private static async Task DelayBackoffAsync(int attempt, CancellationToken cancellationToken)
    {
        int delayMs = Random.Shared.Next(1, 1 << Math.Min(attempt + 1, 4));
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> UpdateObservationsAsync(
        string tenantId,
        string aggregateType,
        string eventType,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        string key = GetObservationsKey(tenantId, aggregateType);
        long observedMs = observedAt.ToUnixTimeMilliseconds();

        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (Dictionary<string, ObservationCounter>? observations, string etag) = await _daprClient
                .GetStateAndETagAsync<Dictionary<string, ObservationCounter>?>(_stateStoreName, key, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            observations ??= new(StringComparer.Ordinal);

            observations[eventType] = observations.TryGetValue(eventType, out ObservationCounter? existing)
                ? new ObservationCounter(existing.Count + 1, Math.Max(existing.LastSeenAtUnixMs, observedMs))
                : new ObservationCounter(1, observedMs);

            StateOptions? concurrency = string.IsNullOrEmpty(etag) ? FirstWriteOptions : null;
            try
            {
                if (await _daprClient
                    .TrySaveStateAsync(_stateStoreName, key, observations, etag, concurrency, TtlMetadata(), cancellationToken)
                    .ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (DaprException) when (concurrency is not null)
            {
                // FirstWrite conflict surfaced as an exception by the Redis state component.
            }

            await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task EnsureWrittenIndexedAsync(string tenantId, string aggregateType, CancellationToken cancellationToken)
    {
        string indexKey = GetWrittenIndexKey(tenantId);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            index ??= [];

            if (index.Contains(aggregateType, StringComparer.Ordinal))
            {
                // Refresh TTL so the written index cannot expire while observation keys remain (patch #3).
                _ = await _daprClient
                    .TrySaveStateAsync(_stateStoreName, indexKey, index, etag, metadata: TtlMetadata(), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            index.Add(aggregateType);
            StateOptions? concurrency = string.IsNullOrEmpty(etag) ? FirstWriteOptions : null;
            if (await _daprClient
                .TrySaveStateAsync(_stateStoreName, indexKey, index, etag, concurrency, TtlMetadata(), cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
        }

        // Compensate observation (+ membership) so D3 purge cannot miss an unindexed key, then fail loud.
        EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, "WrittenIndexCasExhausted");
        await _daprClient
            .DeleteStateAsync(_stateStoreName, GetObservationsKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await _daprClient
            .DeleteStateAsync(_stateStoreName, GetMembershipKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var ex = new InvalidOperationException(
            $"Failed to record written-aggregates index for tenant '{tenantId}' aggregate '{aggregateType}' after {MaxConcurrencyRetries} concurrency retries.");
        ex.Data["WrittenIndexCasExhausted"] = true;
        throw ex;
    }

    private async Task<(bool RejectedAtCap, long PreCardinality, bool Recorded)> UpdateDiscoveryIndexAsync(
        string tenantId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        string membershipKey = GetMembershipKey(tenantId, aggregateType);
        (string? existingMember, string memberEtag) = await _daprClient
            .GetStateAndETagAsync<string?>(_stateStoreName, membershipKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(existingMember))
        {
            // Already discovered — refresh membership + index TTLs so they cannot expire while
            // observation keys remain (review patch #3).
            _ = await _daprClient.TrySaveStateAsync(
                _stateStoreName,
                membershipKey,
                MembershipValue,
                memberEtag,
                metadata: TtlMetadata(),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await RefreshIndexTtlAsync(GetDiscoveryIndexKey(tenantId), cancellationToken).ConfigureAwait(false);
            await RefreshIndexTtlAsync(GetWrittenIndexKey(tenantId), cancellationToken).ConfigureAwait(false);
            return (false, AggregatesIndexCardinalityCap, true);
        }

        string indexKey = GetDiscoveryIndexKey(tenantId);
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            index ??= [];

            long preCardinality = index.Count;
            bool alreadyRegistered = index.Contains(aggregateType, StringComparer.Ordinal);

            if (!alreadyRegistered && preCardinality >= AggregatesIndexCardinalityCap)
            {
                // Cap-rejected: observation + written index remain; discovery intentionally omits.
                return (true, preCardinality, true);
            }

            if (!alreadyRegistered)
            {
                index.Add(aggregateType);
            }

            StateOptions? concurrency = string.IsNullOrEmpty(etag) ? FirstWriteOptions : null;
            if (!await _daprClient
                .TrySaveStateAsync(_stateStoreName, indexKey, index, etag, concurrency, TtlMetadata(), cancellationToken)
                .ConfigureAwait(false))
            {
                await DelayBackoffAsync(attempt, cancellationToken).ConfigureAwait(false);
                continue;
            }

            // FirstWrite membership marker so concurrent discoverers of unrelated types do not contend
            // on re-checking this aggregate.
            _ = await _daprClient.TrySaveStateAsync(
                _stateStoreName,
                membershipKey,
                MembershipValue,
                memberEtag,
                FirstWriteOptions,
                TtlMetadata(),
                cancellationToken).ConfigureAwait(false);

            return (false, preCardinality, true);
        }

        EventStoreIntegrationLog.ObservedEventTypeStoreWriteFailed(_logger, tenantId, "DiscoveryIndexCasExhausted");
        return (false, AggregatesIndexCardinalityCap, false);
    }

    private async Task RefreshIndexTtlAsync(string indexKey, CancellationToken cancellationToken)
    {
        (List<string>? index, string etag) = await _daprClient
            .GetStateAndETagAsync<List<string>?>(_stateStoreName, indexKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (index is null)
        {
            return;
        }

        _ = await _daprClient
            .TrySaveStateAsync(_stateStoreName, indexKey, index, etag, metadata: TtlMetadata(), cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Serialized per-event-type observation counter (occurrence count + last-seen timestamp).</summary>
    internal sealed record ObservationCounter(long Count, long LastSeenAtUnixMs);
}
