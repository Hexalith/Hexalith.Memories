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

using Dapr.Client;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Story 9.3 — Dapr-state-store implementation of <see cref="IObservedEventTypeStore"/>
/// (spec-infrastructure-dependency-abstraction — F6, Decision D30, ADR-IDA-001). Migrated off direct
/// Redis. The per-tenant rolling-window observation data is modelled as two Dapr state keys per tenant:
/// an aggregates-index list (cardinality-capped) and a per-aggregate <c>eventType → (count, lastSeenAt)</c>
/// dictionary. Both are mutated with ETag optimistic concurrency (bounded retry) and expire via
/// <c>ttlInSeconds</c> metadata; window filtering and ordering that Redis did with <c>ZRANGEBYSCORE</c>
/// are now performed in-memory on read.</summary>
/// <remarks>Substrate note: the previous StackExchange.Redis implementation used one atomic Lua script
/// (cardinality-capped <c>SADD</c> + <c>ZADD</c> + <c>HINCRBY</c>) and a sorted-set range query. The Dapr
/// state building block has no cross-key atomicity or range-by-score primitive, so the cardinality cap is
/// now enforced with an ETag compare-and-set (a small race window may admit a few entries over the cap
/// under extreme concurrency — the cap is defence-in-depth, not exact) and observation writes are two
/// non-atomic state writes. Write posture stays <b>fail-open</b>; reads MAY throw. See ADR-IDA-001.</remarks>
internal sealed class DaprObservedEventTypeStore : IObservedEventTypeStore
{
    /// <summary>TTL applied on every write — 2x the widest supported observation window (48h = 2×24h).</summary>
    internal static readonly TimeSpan KeyTtl = TimeSpan.FromHours(48);

    /// <summary>Cap on the per-tenant aggregates-index cardinality (Delta #10 — prevents a malicious
    /// publisher from inflating the discovery index with an unbounded stream of distinct aggregateTypes).</summary>
    internal const long AggregatesIndexCardinalityCap = 1024;

    /// <summary>Bounded retry budget for ETag optimistic-concurrency conflicts.</summary>
    private const int MaxConcurrencyRetries = 8;

    /// <summary>Synthetic tenant id mirrored from <c>MemoriesMeter.RejectedTenantTag</c> — guarded here
    /// as defense-in-depth (Risk #9). MUST stay in sync with the Telemetry package constant.</summary>
    private const string RejectedTenantTag = "__rejected__";

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
            // Observations are recorded even for an aggregate type that the cardinality cap keeps out of
            // the discovery index (the cap bounds the index only), matching the prior atomic-script behavior.
            _ = await UpdateObservationsAsync(tenantId, aggregateType, eventType, observedAt, cancellationToken)
                .ConfigureAwait(false);

            (bool rejectedAtCap, long preCardinality) =
                await UpdateAggregatesIndexAsync(tenantId, aggregateType, cancellationToken).ConfigureAwait(false);

            if (rejectedAtCap)
            {
                EventStoreIntegrationLog.ObservationAggregatesSetCardinalityWarning(_logger, tenantId, preCardinality);
            }

            EventStoreIntegrationLog.ObservedEventTypeRecorded(_logger, tenantId, aggregateType, eventType);
        }
        catch (Dapr.DaprException ex)
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

        Dictionary<string, ObservationCounter>? observations = await _daprClient
            .GetStateAsync<Dictionary<string, ObservationCounter>?>(
                _stateStoreName, GetObservationsKey(tenantId, aggregateType), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (observations is null || observations.Count == 0)
        {
            return Array.Empty<ObservedEventType>();
        }

        long minUnixMs = (_timeProvider.GetUtcNow() - window).ToUnixTimeMilliseconds();

        return observations
            .Where(kvp => kvp.Value.LastSeenAtUnixMs >= minUnixMs)
            .OrderByDescending(kvp => kvp.Value.LastSeenAtUnixMs)
            .Select(kvp => new ObservedEventType(
                aggregateType,
                kvp.Key,
                kvp.Value.Count,
                DateTimeOffset.FromUnixTimeMilliseconds(kvp.Value.LastSeenAtUnixMs)))
            .ToArray();
    }

    public async Task<IReadOnlyList<ObservedEventType>> GetAllObservedTypesAsync(
        string tenantId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        List<string>? aggregateTypes = await _daprClient
            .GetStateAsync<List<string>?>(_stateStoreName, GetAggregatesIndexKey(tenantId), cancellationToken: cancellationToken)
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

    private static string GetAggregatesIndexKey(string tenantId) =>
        $"{tenantId}:eventstore:observed-aggregates";

    private static string GetObservationsKey(string tenantId, string aggregateType) =>
        $"{tenantId}:eventstore:observed:{aggregateType}";

    private static IReadOnlyDictionary<string, string> TtlMetadata()
        => new Dictionary<string, string>
        {
            ["ttlInSeconds"] = ((long)KeyTtl.TotalSeconds).ToString(CultureInfo.InvariantCulture),
        };

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

            // Re-reading fresh state each attempt means a retry never double-counts: the count is derived
            // once from the latest observed value. Late/out-of-order writes keep the max lastSeenAt.
            observations[eventType] = observations.TryGetValue(eventType, out ObservationCounter? existing)
                ? new ObservationCounter(existing.Count + 1, Math.Max(existing.LastSeenAtUnixMs, observedMs))
                : new ObservationCounter(1, observedMs);

            if (await _daprClient
                .TrySaveStateAsync(_stateStoreName, key, observations, etag, metadata: TtlMetadata(), cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<(bool RejectedAtCap, long PreCardinality)> UpdateAggregatesIndexAsync(
        string tenantId,
        string aggregateType,
        CancellationToken cancellationToken)
    {
        string key = GetAggregatesIndexKey(tenantId);

        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            (List<string>? index, string etag) = await _daprClient
                .GetStateAndETagAsync<List<string>?>(_stateStoreName, key, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            index ??= [];

            long preCardinality = index.Count;
            bool alreadyRegistered = index.Contains(aggregateType);

            if (!alreadyRegistered && preCardinality >= AggregatesIndexCardinalityCap)
            {
                // Cap reached and this is a new aggregate type — reject it from the discovery index.
                return (true, preCardinality);
            }

            if (!alreadyRegistered)
            {
                index.Add(aggregateType);
            }

            // Re-save even when already registered so the index TTL is refreshed for as long as
            // observations keep arriving (mirrors the prior EXPIRE-on-every-write behavior).
            if (await _daprClient
                .TrySaveStateAsync(_stateStoreName, key, index, etag, metadata: TtlMetadata(), cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                return (false, preCardinality);
            }
        }

        return (false, AggregatesIndexCardinalityCap);
    }

    /// <summary>Serialized per-event-type observation counter (occurrence count + last-seen timestamp).</summary>
    internal sealed record ObservationCounter(long Count, long LastSeenAtUnixMs);
}
