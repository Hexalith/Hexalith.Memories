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
/// Story 7.5 — composes the <c>GET /api/v1/tenants/{tenantId}/telemetry/summary</c> response (AC #6).
/// Reader over <see cref="TelemetrySnapshotCache"/> (the same cached substrate that feeds the observable
/// gauges) and <see cref="RollingCounterStore"/> (search + ingest counter deltas).
/// ADR-7.5-003: operator-facing poke, NOT a metrics backend.
/// </summary>
public sealed class TelemetrySummaryService
{
    private readonly TelemetrySnapshotCache _snapshotCache;
    private readonly RollingCounterStore _counterStore;

    public TelemetrySummaryService(
        TelemetrySnapshotCache snapshotCache,
        RollingCounterStore counterStore)
    {
        _snapshotCache = snapshotCache;
        _counterStore = counterStore;
    }

    public Task<TelemetrySummary> GetSummaryAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();

        TelemetrySnapshotCache.TenantSnapshot snapshot = _snapshotCache.GetTenantSnapshot(tenantId);

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
            QueueDepth = snapshot.QueueDepth,
        };

        return Task.FromResult(new TelemetrySummary
        {
            TenantId = tenantId,
            AsOf = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            IndexSizes = new TelemetryIndexSizes
            {
                Syntactic = snapshot.IndexSizes.SyntacticKeyCount,
                Semantic = snapshot.IndexSizes.SemanticKeyCount,
                Graph = snapshot.IndexSizes.GraphNodeCount,
            },
            IndexHealth = new TelemetryIndexHealth
            {
                Syntactic = snapshot.IndexStatus.Syntactic,
                Semantic = snapshot.IndexStatus.Semantic,
                Graph = snapshot.IndexStatus.Graph,
            },
            SearchMetrics = searchMetrics,
            IngestionMetrics = ingestionMetrics,
        });
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
