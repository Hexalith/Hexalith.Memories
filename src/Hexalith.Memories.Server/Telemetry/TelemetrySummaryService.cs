// <copyright file="TelemetrySummaryService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Telemetry;

/// <summary>
/// Story 7.5 — composes the <c>GET /api/tenants/{tenantId}/telemetry/summary</c> response (AC #6).
/// Reader over <see cref="TenantMetricsService"/> (index sizes + health),
/// <see cref="PerTenantConcurrencyGate"/> (queue depth), and <see cref="RollingCounterStore"/>
/// (search + ingest counter deltas). ADR-7.5-003: operator-facing poke, NOT a metrics backend.
/// </summary>
public sealed class TelemetrySummaryService
{
    private readonly TenantMetricsService _metrics;
    private readonly PerTenantConcurrencyGate _gate;
    private readonly RollingCounterStore _counterStore;

    public TelemetrySummaryService(
        TenantMetricsService metrics,
        PerTenantConcurrencyGate gate,
        RollingCounterStore counterStore)
    {
        _metrics = metrics;
        _gate = gate;
        _counterStore = counterStore;
    }

    public async Task<TelemetrySummary> GetSummaryAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        (TenantIndexSizes sizes, TenantIndexStatus health) = await _metrics
            .GetIndexSizesAsync(tenantId, ct)
            .ConfigureAwait(false);

        int queueDepth = _gate.GetCurrentDepth(tenantId);

        TelemetrySearchMetrics searchMetrics = new()
        {
            Syntactic = BuildAxisCounters(tenantId, "syntactic", "graph-scoped-syntactic"),
            Semantic = BuildAxisCounters(tenantId, "semantic", "graph-scoped-semantic"),
            Graph = BuildAxisCounters(tenantId, "graph"),
            Hybrid = BuildAxisCounters(tenantId, "hybrid"),
        };

        TelemetryIngestionMetrics ingestionMetrics = new()
        {
            DocumentsLast5m = _counterStore.GetLast5MinutesCount(MemoriesMeter.IngestionDocumentsName, tenantId, axis: null),
            FailuresLast5m = _counterStore.GetLast5MinutesCount(MemoriesMeter.IngestionFailuresName, tenantId, axis: null),
            QueueDepth = queueDepth,
        };

        return new TelemetrySummary
        {
            TenantId = tenantId,
            AsOf = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            IndexSizes = new TelemetryIndexSizes
            {
                Syntactic = sizes.RediSearchKeyCount,
                Semantic = sizes.RedisVectorKeyCount,
                Graph = sizes.FalkorDbNodeCount,
            },
            IndexHealth = new TelemetryIndexHealth
            {
                Syntactic = health.RediSearch,
                Semantic = health.RedisVector,
                Graph = health.FalkorDb,
            },
            SearchMetrics = searchMetrics,
            IngestionMetrics = ingestionMetrics,
        };
    }

    private TelemetryAxisCounters BuildAxisCounters(string tenantId, params string[] axes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(axes);

        long requests = 0;
        long errors = 0;
        foreach (string axis in axes)
        {
            if (string.IsNullOrWhiteSpace(axis))
            {
                continue;
            }

            requests += _counterStore.GetLast5MinutesCount(MemoriesMeter.SearchRequestsName, tenantId, axis);
            errors += _counterStore.GetLast5MinutesSearchErrorCount(tenantId, axis);
        }

        return new()
        {
            RequestsLast5m = requests,
            ErrorsLast5m = errors,
        };
    }
}
