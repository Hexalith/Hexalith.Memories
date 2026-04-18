// <copyright file="RollingCounterStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

/// <summary>
/// Story 7.5 Task 9.1 sibling — coverage for <see cref="RollingCounterStore"/>. The store powers the
/// telemetry summary endpoint's <c>requestsLast5m</c> / <c>errorsLast5m</c> / <c>documentsLast5m</c> /
/// <c>failuresLast5m</c> fields (AC #6). Ring-buffer shape: 5 slots × 1-minute wall-clock granularity
/// (Rev 0.3 — Tree of Thoughts). Tests use <see cref="FakeTimeProvider"/> to drive wall-clock rollover
/// deterministically.
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class RollingCounterStoreTests
{
    private const string Tenant = "acme";
    private const string SearchRequestsMetric = MemoriesMeter.SearchRequestsName;
    private const string IngestDocumentsMetric = MemoriesMeter.IngestionDocumentsName;

    [Fact]
    public void GetLast5MinutesCount_EmptyStore_ReturnsZero()
    {
        using var store = new RollingCounterStore();
        store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, axis: "hybrid").ShouldBe(0);
    }

    [Fact]
    public async Task MeterListener_CapturesSearchRequests_AndSumsAcrossMinute()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);

            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "hybrid").ShouldBe(3);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetLast5MinutesCount_AfterOneMinute_StillIncludesPriorSlot()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);

            time.Advance(TimeSpan.FromMinutes(1));
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);

            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "hybrid").ShouldBe(3);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task GetLast5MinutesCount_SlotsOlderThan5Minutes_AreDropped()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1); // minute 0

            time.Advance(TimeSpan.FromMinutes(5));                                  // now at minute 5
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1); // minute 5

            // Window [1, 5] — minute 0 is outside; we should see only the 1 new measurement.
            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "hybrid").ShouldBe(1);

            time.Advance(TimeSpan.FromMinutes(10));                                 // now at minute 15
            // Window [11, 15] — both prior measurements are outside.
            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "hybrid").ShouldBe(0);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task MeterListener_DifferentAxisTags_ProduceDistinctBuckets()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);
            TelemetryMetricsRecorder.RecordSearch(Tenant, "hybrid", elapsedMs: 1);
            TelemetryMetricsRecorder.RecordSearch(Tenant, "syntactic", elapsedMs: 1);

            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "hybrid").ShouldBe(2);
            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "syntactic").ShouldBe(1);
            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, "semantic").ShouldBe(0);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task MeterListener_DifferentTenants_ProduceDistinctBuckets()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            TelemetryMetricsRecorder.RecordIngestSuccess("acme");
            TelemetryMetricsRecorder.RecordIngestSuccess("acme");
            TelemetryMetricsRecorder.RecordIngestSuccess("globex");

            store.GetLast5MinutesCount(IngestDocumentsMetric, "acme", axis: null).ShouldBe(2);
            store.GetLast5MinutesCount(IngestDocumentsMetric, "globex", axis: null).ShouldBe(1);
            store.GetLast5MinutesCount(IngestDocumentsMetric, "initech", axis: null).ShouldBe(0);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public void RecordSearchError_IsScopedByAxis()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        store.RecordSearchError(Tenant, "hybrid");
        store.RecordSearchError(Tenant, "hybrid");
        store.RecordSearchError(Tenant, "syntactic");

        store.GetLast5MinutesSearchErrorCount(Tenant, "hybrid").ShouldBe(2);
        store.GetLast5MinutesSearchErrorCount(Tenant, "syntactic").ShouldBe(1);
        store.GetLast5MinutesSearchErrorCount(Tenant, "semantic").ShouldBe(0);
    }

    [Fact]
    public void GetLast5MinutesCount_NullMetricName_Throws()
    {
        using var store = new RollingCounterStore();
        Should.Throw<ArgumentException>(() => store.GetLast5MinutesCount("", Tenant, axis: null));
        Should.Throw<ArgumentException>(() => store.GetLast5MinutesCount("  ", Tenant, axis: null));
    }

    [Fact]
    public void RecordSearchError_NullTenant_Throws()
    {
        using var store = new RollingCounterStore();
        Should.Throw<ArgumentException>(() => store.RecordSearchError("", "hybrid"));
        Should.Throw<ArgumentException>(() => store.RecordSearchError(Tenant, ""));
    }

    [Fact]
    public async Task MeterListener_IgnoresMeasurementsFromForeignMeters()
    {
        FakeTimeProvider time = new(DateTimeOffset.Parse("2026-04-18T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var store = new RollingCounterStore(time);
        await store.StartAsync(CancellationToken.None);
        try
        {
            using var foreignMeter = new System.Diagnostics.Metrics.Meter("Some.Other.Meter");
            var counter = foreignMeter.CreateCounter<long>(SearchRequestsMetric);
            counter.Add(10, new System.Collections.Generic.KeyValuePair<string, object?>("tenant_id", Tenant));

            // Same instrument name on a different meter MUST NOT feed the Memories ring.
            store.GetLast5MinutesCount(SearchRequestsMetric, Tenant, axis: null).ShouldBe(0);
        }
        finally
        {
            await store.StopAsync(CancellationToken.None);
        }
    }
}
