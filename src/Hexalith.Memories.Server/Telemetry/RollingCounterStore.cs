// <copyright file="RollingCounterStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Telemetry;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Story 7.5 — singleton that subscribes to <see cref="MemoriesMeter"/> counters via
/// <see cref="MeterListener"/> and maintains per-<c>(tenantId, axis)</c> rolling 5-minute windows.
/// Ring-buffer shape PINNED at 5 slots × 1-minute wall-clock granularity (Rev 0.3 — Tree of Thoughts).
/// </summary>
public sealed class RollingCounterStore : IHostedService, IDisposable
{
    private const int SlotCount = 5;
    private const int SlotDurationSeconds = 60;
    private const string SearchErrorsMetricName = "memories.search.errors";

    private readonly MeterListener _listener = new();
    private readonly ConcurrentDictionary<CounterKey, CounterRing> _rings = new();
    private readonly TimeProvider _time;

    public RollingCounterStore(TimeProvider? timeProvider = null)
    {
        _time = timeProvider ?? TimeProvider.System;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name != MemoriesMeter.Name)
            {
                return;
            }

            if (instrument.Name is MemoriesMeter.SearchRequestsName
                or MemoriesMeter.IngestionDocumentsName
                or MemoriesMeter.IngestionFailuresName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>(OnMeasurement);
        _listener.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _listener.Dispose();

    /// <summary>Returns the sum over the last 5 minutes of the measurements tagged with the given key.</summary>
    public long GetLast5MinutesCount(string metricName, string tenantId, string? axis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return GetLast5MinutesCountCore(new(metricName, tenantId, axis));
    }

    /// <summary>Returns the last-5-minute error count for the given tenant + resolved search axis.</summary>
    public long GetLast5MinutesSearchErrorCount(string tenantId, string axis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(axis);
        return GetLast5MinutesCountCore(new(SearchErrorsMetricName, tenantId, axis));
    }

    /// <summary>Records a single search-error occurrence for the given tenant + resolved axis.</summary>
    public void RecordSearchError(string tenantId, string axis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(axis);
        AddMeasurement(SearchErrorsMetricName, tenantId, axis, 1);
    }

    private void OnMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        string? tenantId = null;
        string? axis = null;
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (tag.Key == "tenant_id")
            {
                tenantId = tag.Value as string;
            }
            else if (tag.Key == "axis")
            {
                axis = tag.Value as string;
            }
        }

        if (tenantId is null)
        {
            return;
        }

        AddMeasurement(instrument.Name, tenantId, axis, measurement);
    }

    private void AddMeasurement(string metricName, string tenantId, string? axis, long measurement)
    {
        if (measurement == 0)
        {
            return;
        }

        CounterRing ring = _rings.GetOrAdd(new CounterKey(metricName, tenantId, axis), _ => new CounterRing());
        ring.Add(GetCurrentMinute(), measurement);
    }

    private long GetLast5MinutesCountCore(CounterKey key)
        => _rings.TryGetValue(key, out CounterRing? ring)
            ? ring.Sum(GetCurrentMinute())
            : 0;

    private long GetCurrentMinute() => _time.GetUtcNow().ToUnixTimeSeconds() / SlotDurationSeconds;

    private static int GetSlotIndex(long minute)
        => (int)(((minute % SlotCount) + SlotCount) % SlotCount);

    private sealed class CounterRing
    {
        private readonly object _gate = new();
        private readonly long[] _minutes = new long[SlotCount];
        private readonly long[] _values = new long[SlotCount];

        public void Add(long minute, long measurement)
        {
            lock (_gate)
            {
                int slotIndex = GetSlotIndex(minute);
                if (_minutes[slotIndex] != minute)
                {
                    _minutes[slotIndex] = minute;
                    _values[slotIndex] = 0;
                }

                _values[slotIndex] += measurement;
            }
        }

        public long Sum(long currentMinute)
        {
            long earliestMinute = currentMinute - (SlotCount - 1);

            lock (_gate)
            {
                long sum = 0;
                for (int i = 0; i < SlotCount; i++)
                {
                    long stampedMinute = _minutes[i];
                    if (stampedMinute < earliestMinute || stampedMinute > currentMinute)
                    {
                        _minutes[i] = 0;
                        _values[i] = 0;
                        continue;
                    }

                    sum += _values[i];
                }

                return sum;
            }
        }
    }

    private readonly record struct CounterKey(string MetricName, string TenantId, string? Axis);
}
