// <copyright file="TelemetryMetricsRecorderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Telemetry;

using Shouldly;

/// <summary>
/// Story 7.5 Task 9.2 sibling — MeterListener-based coverage for
/// <see cref="TelemetryMetricsRecorder"/>. Asserts each recorder method emits on the pinned instrument
/// with the pinned tag-key set. This is the runtime counterpart to
/// <see cref="MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys"/> which only covers the
/// compile-time manifest.
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class TelemetryMetricsRecorderTests
{
    [Fact]
    public void RecordSearch_EmitsRequestCounterAndDurationHistogram_WithPinnedTags()
    {
        List<CapturedLongMeasurement> counterHits = [];
        List<CapturedDoubleMeasurement> histogramHits = [];

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name != MemoriesMeter.Name)
                {
                    return;
                }

                if (instrument.Name == MemoriesMeter.SearchRequestsName
                    || instrument.Name == MemoriesMeter.SearchDurationName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name == MemoriesMeter.SearchRequestsName)
            {
                counterHits.Add(new CapturedLongMeasurement(value, ToArray(tags)));
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            if (instrument.Name == MemoriesMeter.SearchDurationName)
            {
                histogramHits.Add(new CapturedDoubleMeasurement(value, ToArray(tags)));
            }
        });
        listener.Start();

        TelemetryMetricsRecorder.RecordSearch("acme", "hybrid", elapsedMs: 42.5);

        counterHits.ShouldHaveSingleItem();
        counterHits[0].Value.ShouldBe(1);
        AssertTagsEqual(counterHits[0].Tags, ("tenant_id", "acme"), ("axis", "hybrid"));

        histogramHits.ShouldHaveSingleItem();
        histogramHits[0].Value.ShouldBe(42.5);
        AssertTagsEqual(histogramHits[0].Tags, ("tenant_id", "acme"), ("axis", "hybrid"));
    }

    [Fact]
    public void RecordIngestSuccess_EmitsDocumentsCounter_WithTenantTagOnly()
    {
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.IngestionDocumentsName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordIngestSuccess("acme");

        hits.ShouldHaveSingleItem();
        hits[0].Value.ShouldBe(1);
        AssertTagsEqual(hits[0].Tags, ("tenant_id", "acme"));
    }

    [Fact]
    public void RecordIngestFailure_EmitsFailuresCounter_WithTenantAndErrorCodeTags()
    {
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.IngestionFailuresName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordIngestFailure("acme", "INVALID_SOURCE_TYPE");

        hits.ShouldHaveSingleItem();
        hits[0].Value.ShouldBe(1);
        AssertTagsEqual(hits[0].Tags, ("tenant_id", "acme"), ("error_code", "INVALID_SOURCE_TYPE"));
    }

    [Fact]
    public void RecordIngestSuccess_WithMultipleDocuments_EmitsProvidedCount()
    {
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.IngestionDocumentsName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordIngestSuccess("acme", documentCount: 7);

        hits.ShouldHaveSingleItem();
        hits[0].Value.ShouldBe(7);
        AssertTagsEqual(hits[0].Tags, ("tenant_id", "acme"));
    }

    [Fact]
    public void RecordIngestFailure_RejectedTenantTag_FlowsThroughRecorder()
    {
        // Cardinality injection mitigation (Rev 0.3 finding 1b): caller is expected to pass
        // MemoriesMeter.RejectedTenantTag when a tenant-guard rejection happens before the call.
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.IngestionFailuresName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordIngestFailure(MemoriesMeter.RejectedTenantTag, "TENANT_NOT_FOUND");

        hits[0].Tags.First(t => t.Key == "tenant_id").Value.ShouldBe("__rejected__");
    }

    [Fact]
    public void RecordRateLimitRejection_EmitsCounter_WithPinnedTags()
    {
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.RateLimitRejectionsName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordRateLimitRejection("acme", "RATE_LIMIT_EXCEEDED");

        hits.ShouldHaveSingleItem();
        hits[0].Value.ShouldBe(1);
        AssertTagsEqual(hits[0].Tags, ("tenant_id", "acme"), ("error_code", "RATE_LIMIT_EXCEEDED"));
    }

    [Fact]
    public void RecordRateLimitRejection_EmptyTenant_UsesRejectedTenantTag()
    {
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.RateLimitRejectionsName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordRateLimitRejection(string.Empty, "RATE_LIMIT_EXCEEDED");

        hits.ShouldHaveSingleItem();
        AssertTagsEqual(hits[0].Tags, ("tenant_id", MemoriesMeter.RejectedTenantTag), ("error_code", "RATE_LIMIT_EXCEEDED"));
    }

    [Fact]
    public void RecordSearch_DoesNotInjectCaseIdOrUserTags()
    {
        // Risk #1 cardinality mitigation — regression guard asserting the recorder is not a drift surface.
        List<CapturedLongMeasurement> hits = [];

        using MeterListener listener = BuildLongListener(MemoriesMeter.SearchRequestsName, hits);
        listener.Start();

        TelemetryMetricsRecorder.RecordSearch("acme", "syntactic", elapsedMs: 1);

        hits.ShouldHaveSingleItem();
        HashSet<string> keys = [.. hits[0].Tags.Select(t => t.Key)];
        keys.ShouldNotContain("case_id");
        keys.ShouldNotContain("user");
        keys.ShouldNotContain("memory_unit_id");
    }

    private static MeterListener BuildLongListener(string instrumentName, List<CapturedLongMeasurement> hits)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == MemoriesMeter.Name && instrument.Name == instrumentName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            hits.Add(new CapturedLongMeasurement(value, ToArray(tags))));
        return listener;
    }

    private static KeyValuePair<string, object?>[] ToArray(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var array = new KeyValuePair<string, object?>[tags.Length];
        for (int i = 0; i < tags.Length; i++)
        {
            array[i] = tags[i];
        }

        return array;
    }

    private static void AssertTagsEqual(
        KeyValuePair<string, object?>[] tags,
        params (string Key, string Value)[] expected)
    {
        tags.Length.ShouldBe(expected.Length);
        Dictionary<string, object?> actual = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> kvp in tags)
        {
            actual[kvp.Key] = kvp.Value;
        }

        foreach ((string key, string value) in expected)
        {
            actual.ShouldContainKey(key);
            actual[key].ShouldBe(value);
        }
    }

    private sealed record CapturedLongMeasurement(long Value, KeyValuePair<string, object?>[] Tags);

    private sealed record CapturedDoubleMeasurement(double Value, KeyValuePair<string, object?>[] Tags);
}
