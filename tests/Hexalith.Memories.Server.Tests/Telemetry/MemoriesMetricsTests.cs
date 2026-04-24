// <copyright file="MemoriesMetricsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

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
        MemoriesMeter.IngestionDocumentsName.ShouldBe("memories.ingestion.documents");
        MemoriesMeter.IngestionFailuresName.ShouldBe("memories.ingestion.failures");
        MemoriesMeter.SearchRequestsName.ShouldBe("memories.search.requests");
        MemoriesMeter.SearchDurationName.ShouldBe("memories.search.duration");
        MemoriesMeter.IndexSizeName.ShouldBe("memories.index.size");
        MemoriesMeter.PipelineQueueDepthName.ShouldBe("memories.pipeline.queue_depth");
        MemoriesMeter.NaturalLanguageDescriptionDurationName.ShouldBe("memories_natural_language_description_duration_ms");
        MemoriesMeter.NaturalLanguageEmbeddingQueueDepthName.ShouldBe("memories_natural_language_embedding_queue_depth");
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
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.IngestionDocumentsName].ShouldBe(new[] { "tenant_id" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.IngestionFailuresName].ShouldBe(new[] { "tenant_id", "error_code" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.SearchRequestsName].ShouldBe(new[] { "tenant_id", "axis" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.SearchDurationName].ShouldBe(new[] { "tenant_id", "axis" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.IndexSizeName].ShouldBe(new[] { "tenant_id", "axis" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.PipelineQueueDepthName].ShouldBe(new[] { "tenant_id" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.NaturalLanguageDescriptionDurationName].ShouldBe(new[] { "tenant_id" });
        MemoriesMeter.MetricTagKeyPolicy[MemoriesMeter.NaturalLanguageEmbeddingQueueDepthName].ShouldBe(new[] { "tenant_id" });
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
}
