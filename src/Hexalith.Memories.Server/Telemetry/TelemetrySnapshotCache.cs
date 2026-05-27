// <copyright file="TelemetrySnapshotCache.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

/// <summary>
/// Maintains a short-lived cached snapshot for observable-gauge callbacks so the meter reader never
/// blocks on async DAPR/Redis/Falkor lookups.
/// </summary>
public sealed class TelemetrySnapshotCache
{
    private static readonly Snapshot EmptySnapshot = new(
        DateTimeOffset.MinValue,
        [],
        [],
        [],
        [],
        new Dictionary<string, TenantSnapshot>(StringComparer.Ordinal));
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(30);

    private readonly TenantRegistryService _registry;
    private readonly TenantMetricsService _metrics;
    private readonly PerTenantConcurrencyGate _gate;
    private readonly IFailedNaturalLanguageEmbeddingRegistry _naturalLanguageEmbeddingRegistry;
    private readonly ILogger<TelemetrySnapshotCache> _logger;
    private readonly TimeProvider _time;

    private Snapshot _snapshot = EmptySnapshot;
    private int _refreshInProgress;

    public TelemetrySnapshotCache(
        TenantRegistryService registry,
        TenantMetricsService metrics,
        PerTenantConcurrencyGate gate,
        IFailedNaturalLanguageEmbeddingRegistry naturalLanguageEmbeddingRegistry,
        ILogger<TelemetrySnapshotCache> logger)
    {
        _registry = registry;
        _metrics = metrics;
        _gate = gate;
        _naturalLanguageEmbeddingRegistry = naturalLanguageEmbeddingRegistry;
        _logger = logger;
        _time = TimeProvider.System;
    }

    /// <summary>Returns the cached index-size measurements and schedules a background refresh when stale.</summary>
    public IEnumerable<Measurement<long>> GetIndexSizeMeasurements()
    {
        Snapshot snapshot = GetSnapshot();
        return snapshot.IndexSizes;
    }

    /// <summary>Returns the cached queue-depth measurements and schedules a background refresh when stale.</summary>
    public IEnumerable<Measurement<int>> GetQueueDepthMeasurements()
    {
        Snapshot snapshot = GetSnapshot();
        return snapshot.QueueDepths;
    }

    /// <summary>Returns the cached NL retry queue depth measurements and schedules a background refresh when stale.</summary>
    public IEnumerable<Measurement<long>> GetNaturalLanguageEmbeddingQueueDepthMeasurements()
    {
        Snapshot snapshot = GetSnapshot();
        return snapshot.NaturalLanguageEmbeddingQueueDepths;
    }

    /// <summary>Returns the cached NL retry queue byte-size measurements and schedules a background refresh when stale.</summary>
    public IEnumerable<Measurement<long>> GetNaturalLanguageEmbeddingQueueBytesMeasurements()
    {
        Snapshot snapshot = GetSnapshot();
        return snapshot.NaturalLanguageEmbeddingQueueBytes;
    }

    /// <summary>Returns the cached tenant snapshot that also backs the observable gauges.</summary>
    public TenantSnapshot GetTenantSnapshot(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        Snapshot snapshot = GetSnapshot();
        return snapshot.Tenants.TryGetValue(tenantId, out TenantSnapshot? tenantSnapshot)
            ? tenantSnapshot
            : TenantSnapshot.Empty;
    }

    private Snapshot GetSnapshot()
    {
        Snapshot snapshot = Volatile.Read(ref _snapshot);
        if (snapshot.RefreshedAt == DateTimeOffset.MinValue || (_time.GetUtcNow() - snapshot.RefreshedAt) > SnapshotTtl)
        {
            TriggerRefresh();
        }

        return Volatile.Read(ref _snapshot);
    }

    private void TriggerRefresh()
    {
        if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(RefreshAsync);
    }

    private async Task RefreshAsync()
    {
        try
        {
            IReadOnlyList<TenantInfo> tenants = await _registry.ListTenantsAsync(CancellationToken.None).ConfigureAwait(false);
            var indexSizeMeasurements = new List<Measurement<long>>(tenants.Count * 3);
            var queueDepthMeasurements = new List<Measurement<int>>(tenants.Count);
            var naturalLanguageQueueDepthMeasurements = new List<Measurement<long>>(tenants.Count);
            var naturalLanguageQueueBytesMeasurements = new List<Measurement<long>>(tenants.Count);
            Dictionary<string, TenantSnapshot> tenantSnapshots = new(StringComparer.Ordinal);

            foreach (TenantInfo tenant in tenants)
            {
                if (string.IsNullOrWhiteSpace(tenant.Id))
                {
                    continue;
                }

                (TenantIndexSizes sizes, TenantIndexStatus health) = await _metrics.GetIndexSizesAsync(tenant.Id, CancellationToken.None).ConfigureAwait(false);
                int queueDepth = _gate.GetCurrentDepth(tenant.Id);
                long naturalLanguageQueueDepth = await _naturalLanguageEmbeddingRegistry
                    .GetBacklogCountAsync(tenant.Id, CancellationToken.None)
                    .ConfigureAwait(false);
                long naturalLanguageQueueBytes = await GetNaturalLanguageQueueBytesSafeAsync(tenant.Id).ConfigureAwait(false);

                AddIndexSizeMeasurement(indexSizeMeasurements, tenant.Id, "syntactic", sizes.RediSearchKeyCount);
                AddIndexSizeMeasurement(indexSizeMeasurements, tenant.Id, "semantic", sizes.RedisVectorKeyCount);
                AddIndexSizeMeasurement(indexSizeMeasurements, tenant.Id, "graph", sizes.FalkorDbNodeCount);

                queueDepthMeasurements.Add(new Measurement<int>(
                    queueDepth,
                    new KeyValuePair<string, object?>("tenant_id", tenant.Id)));

                naturalLanguageQueueDepthMeasurements.Add(new Measurement<long>(
                    naturalLanguageQueueDepth,
                    new KeyValuePair<string, object?>("tenant_id", tenant.Id)));

                naturalLanguageQueueBytesMeasurements.Add(new Measurement<long>(
                    naturalLanguageQueueBytes,
                    new KeyValuePair<string, object?>("tenant_id", tenant.Id)));

                tenantSnapshots[tenant.Id] = new TenantSnapshot(sizes, health, queueDepth, naturalLanguageQueueDepth);
            }

            await foreach (string tenantId in _naturalLanguageEmbeddingRegistry
                .ListTenantsWithBacklogAsync(CancellationToken.None)
                .ConfigureAwait(false))
            {
                if (string.IsNullOrWhiteSpace(tenantId) || tenantSnapshots.ContainsKey(tenantId))
                {
                    continue;
                }

                long naturalLanguageQueueDepth = await _naturalLanguageEmbeddingRegistry
                    .GetBacklogCountAsync(tenantId, CancellationToken.None)
                    .ConfigureAwait(false);
                long naturalLanguageQueueBytes = await GetNaturalLanguageQueueBytesSafeAsync(tenantId).ConfigureAwait(false);

                naturalLanguageQueueDepthMeasurements.Add(new Measurement<long>(
                    naturalLanguageQueueDepth,
                    new KeyValuePair<string, object?>("tenant_id", tenantId)));

                naturalLanguageQueueBytesMeasurements.Add(new Measurement<long>(
                    naturalLanguageQueueBytes,
                    new KeyValuePair<string, object?>("tenant_id", tenantId)));
            }

            Volatile.Write(
                ref _snapshot,
                new Snapshot(
                    _time.GetUtcNow(),
                    indexSizeMeasurements,
                    queueDepthMeasurements,
                    naturalLanguageQueueDepthMeasurements,
                    naturalLanguageQueueBytesMeasurements,
                    tenantSnapshots));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Telemetry snapshot refresh failed; keeping the previous gauge snapshot.");
        }
        finally
        {
            Volatile.Write(ref _refreshInProgress, 0);
        }
    }

    private static void AddIndexSizeMeasurement(
        ICollection<Measurement<long>> target,
        string tenantId,
        string axis,
        long? value)
    {
        if (value is not long current)
        {
            return;
        }

        target.Add(new Measurement<long>(
            current,
            new KeyValuePair<string, object?>("tenant_id", tenantId),
            new KeyValuePair<string, object?>("axis", axis)));
    }

    private async Task<long> GetNaturalLanguageQueueBytesSafeAsync(string tenantId)
    {
        try
        {
            return await _naturalLanguageEmbeddingRegistry
                .GetBacklogBytesAsync(tenantId, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _logger.LogDebug(
                ex,
                "Telemetry snapshot NL queue-bytes probe failed for tenant {TenantId}; defaulting to 0 for this refresh.",
                tenantId);
            return 0;
        }
    }

    private sealed record Snapshot(
        DateTimeOffset RefreshedAt,
        IReadOnlyList<Measurement<long>> IndexSizes,
        IReadOnlyList<Measurement<int>> QueueDepths,
        IReadOnlyList<Measurement<long>> NaturalLanguageEmbeddingQueueDepths,
        IReadOnlyList<Measurement<long>> NaturalLanguageEmbeddingQueueBytes,
        IReadOnlyDictionary<string, TenantSnapshot> Tenants);

    public sealed record TenantSnapshot(
        TenantIndexSizes IndexSizes,
        TenantIndexStatus IndexStatus,
        int QueueDepth,
        long NaturalLanguageEmbeddingQueueDepth)
    {
        public static TenantSnapshot Empty { get; } = new(
            new TenantIndexSizes(null, null, null),
            new TenantIndexStatus(IndexHealth.Unknown, IndexHealth.Unknown, IndexHealth.Unknown),
            QueueDepth: 0,
            NaturalLanguageEmbeddingQueueDepth: 0);
    }
}
