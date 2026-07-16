// <copyright file="MemoriesMetricsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Memories.Telemetry;

using Shouldly;

/// <summary>Story 7.5 Task 8.2 — asserts Meter name, instrument names, and tag-key policy.</summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class MemoriesMetricsTests
{
    [Fact]
    public void MeterName_IsPinned() => MemoriesMeter.Name.ShouldBe("Hexalith.Memories");

    [Fact]
    public void InstrumentNames_ArePinned()
    {
        ExpectedMetricTags.Keys.ShouldBe(new[]
        {
            "memories.ingestion.documents",
            "memories.ingestion.failures",
            "memories.search.requests",
            "memories.search.duration",
            "memories.rate.limit.rejections",
            "memories.index.size",
            "memories.pipeline.queue.depth",
            "memories.natural.language.description.duration",
            "memories.natural.language.embedding.queue.depth",
            "memories.natural.language.embedding.queue.bytes",
            "memories.embedding.api.calls",
            "memories.conversation.cache.hits",
            "memories.handlers.registered",
            "memories.handlers.mismatches",
            "memories.handlers.observations.dropped",
        });
    }

    [Fact]
    public void Instance_MatchesMeterName()
    {
        MemoriesMeter.Instance.ShouldNotBeNull();
        MemoriesMeter.Instance.Name.ShouldBe(MemoriesMeter.Name);
    }

    [Fact]
    public void AllRegisteredMetricsHaveExpectedTagKeys()
    {
        // Risk #1 cardinality mitigation — case_id and user are NEVER in the metric tag keys.
        MemoriesMeter.MetricTagKeyPolicy.Count.ShouldBe(ExpectedMetricTags.Count);
        foreach ((string name, string[] tagKeys) in ExpectedMetricTags)
        {
            MemoriesMeter.MetricTagKeyPolicy.ShouldContainKey(name);
            MemoriesMeter.MetricTagKeyPolicy[name].ShouldBe(tagKeys);
        }
    }

    [Fact]
    public void MetricTagKeyPolicyKeys_AreDotSeparatedMemoriesFamily()
    {
        foreach (string metricName in MemoriesMeter.MetricTagKeyPolicy.Keys)
        {
            metricName.ShouldStartWith("memories.");
            metricName.ShouldNotContain("_");
            metricName.Split('.', StringSplitOptions.RemoveEmptyEntries).Length.ShouldBeGreaterThan(2);
        }
    }

    [Fact]
    public void MetricTagKeyPolicy_DoesNotIncludeCaseIdOrUser()
    {
        foreach (var entry in MemoriesMeter.MetricTagKeyPolicy)
        {
            entry.Value.ShouldNotContain("case_id");
            entry.Value.ShouldNotContain("user");
            entry.Value.ShouldNotContain("memory_unit_id");
        }
    }

    [Fact]
    public void RejectedTenantTag_IsPinned() => MemoriesMeter.RejectedTenantTag.ShouldBe("__rejected__");

    private static IReadOnlyDictionary<string, string[]> ExpectedMetricTags { get; } =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [MemoriesMeter.IngestionDocumentsName] = ["tenant_id"],
            [MemoriesMeter.IngestionFailuresName] = ["tenant_id", "error_code"],
            [MemoriesMeter.SearchRequestsName] = ["tenant_id", "axis"],
            [MemoriesMeter.SearchDurationName] = ["tenant_id", "axis"],
            [MemoriesMeter.RateLimitRejectionsName] = ["tenant_id", "error_code"],
            [MemoriesMeter.IndexSizeName] = ["tenant_id", "axis"],
            [MemoriesMeter.PipelineQueueDepthName] = ["tenant_id"],
            [MemoriesMeter.NaturalLanguageDescriptionDurationName] = ["tenant_id"],
            [MemoriesMeter.NaturalLanguageEmbeddingQueueDepthName] = ["tenant_id"],
            [MemoriesMeter.NaturalLanguageEmbeddingQueueBytesName] = ["tenant_id"],
            [MemoriesMeter.EmbeddingApiCallsName] = ["tenant_id", "content_kind"],
            [MemoriesMeter.ConversationCacheHitName] = ["tenant_id", "cache_status"],
            [MemoriesMeter.HandlersRegisteredName] = ["tenant_id"],
            [MemoriesMeter.HandlerMismatchesName] = ["tenant_id", "severity"],
            [MemoriesMeter.ObservationsDroppedName] = ["reason"],
        };
}
