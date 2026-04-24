// <copyright file="MemoriesMeter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

/// <summary>
/// Provides a single static <see cref="Meter"/> with the Story 7.5 custom metrics (NFR29).
/// All instruments are registered once at static-construction time; callers emit values via the
/// public properties. Tag-key policy is enforced by <see cref="MetricTagKeyPolicy"/> — a compile-time
/// dictionary read by <c>MemoriesMetricsTests.AllRegisteredMetricsHaveExpectedTagKeys</c>.
/// </summary>
public static class MemoriesMeter
{
    private static readonly object ObservableInstrumentGate = new();
    private static ObservableGauge<long>? _indexSizeGauge;
    private static ObservableGauge<int>? _pipelineQueueDepthGauge;
    private static ObservableGauge<long>? _naturalLanguageEmbeddingQueueDepthGauge;
    private static ObservableGauge<long>? _naturalLanguageEmbeddingQueueBytesGauge;

    /// <summary>The meter name registered with OpenTelemetry.</summary>
    public const string Name = "Hexalith.Memories";

    /// <summary>Instrument name: total documents ingested successfully.</summary>
    public const string IngestionDocumentsName = "memories.ingestion.documents";

    /// <summary>Instrument name: total documents that failed ingestion scheduling.</summary>
    public const string IngestionFailuresName = "memories.ingestion.failures";

    /// <summary>Instrument name: total search requests per resolved axis.</summary>
    public const string SearchRequestsName = "memories.search.requests";

    /// <summary>Instrument name: search request latency histogram.</summary>
    public const string SearchDurationName = "memories.search.duration";

    /// <summary>Instrument name: per-tenant per-axis index size gauge.</summary>
    public const string IndexSizeName = "memories.index.size";

    /// <summary>Instrument name: per-tenant ingestion queue depth gauge.</summary>
    public const string PipelineQueueDepthName = "memories.pipeline.queue_depth";

    /// <summary>Story 9.2 — instrument name: per-call NL description latency histogram.</summary>
    public const string NaturalLanguageDescriptionDurationName = "memories_natural_language_description_duration_ms";

    /// <summary>Story 9.2 — instrument name: per-tenant NL retry queue depth gauge.</summary>
    public const string NaturalLanguageEmbeddingQueueDepthName = "memories_natural_language_embedding_queue_depth";

    /// <summary>Story 9.2 (Spike 0.1 payload-by-value fallback) — instrument name: per-tenant NL retry
    /// queue byte-size gauge. Present only when the retry record carries the truncated raw JSON payload
    /// (bounded by <c>NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes</c>). Operators size
    /// Redis memory by combining depth × average-payload.</summary>
    public const string NaturalLanguageEmbeddingQueueBytesName = "memories_natural_language_embedding_queue_bytes";

    /// <summary>Story 9.2 / Risk #16 — instrument name: DAPR Conversation response-cache hit/miss
    /// counter. Non-zero values observed across multiple tenants are the canonical cross-tenant
    /// cache-leak signature (the cache is shared at the sidecar level regardless of tenant id).
    /// Tags: <c>tenant_id</c>, <c>cache_status</c> (hit | miss).</summary>
    public const string ConversationCacheHitName = "memories_conversation_cache_hit_total";

    /// <summary>Story 9.2 / Risk #6 — instrument name: per-call embedding API counter partitioned by
    /// tenant + content kind. Operators observe the raw-payload / NL-description 2:1 split under
    /// dual-embedding and size the per-tenant rate-limit ceiling accordingly.</summary>
    public const string EmbeddingApiCallsName = "memories.embedding.api_calls";

    /// <summary>Synthetic tenant id used when a request is rejected before tenant resolution.</summary>
    public const string RejectedTenantTag = "__rejected__";

    /// <summary>Gets the singleton <see cref="Meter"/> instance.</summary>
    public static Meter Instance { get; } = new(Name);

    /// <summary>Counter: total documents ingested successfully. Tag: <c>tenant_id</c>.</summary>
    public static Counter<long> IngestionDocuments { get; } =
        Instance.CreateCounter<long>(IngestionDocumentsName, unit: "{documents}", description: "Total documents ingested successfully per tenant.");

    /// <summary>Counter: total ingestion failures. Tags: <c>tenant_id</c>, <c>error_code</c>.</summary>
    public static Counter<long> IngestionFailures { get; } =
        Instance.CreateCounter<long>(IngestionFailuresName, unit: "{documents}", description: "Total ingestion scheduling failures per tenant with error code.");

    /// <summary>Counter: total search requests. Tags: <c>tenant_id</c>, <c>axis</c>.</summary>
    public static Counter<long> SearchRequests { get; } =
        Instance.CreateCounter<long>(SearchRequestsName, unit: "{requests}", description: "Total search requests per tenant and resolved axis.");

    /// <summary>Histogram: search request latency in milliseconds. Tags: <c>tenant_id</c>, <c>axis</c>.</summary>
    public static Histogram<double> SearchDuration { get; } =
        Instance.CreateHistogram<double>(SearchDurationName, unit: "ms", description: "Search request latency per tenant and resolved axis.");

    /// <summary>Story 9.2 — histogram: NL description latency in milliseconds. Tag: <c>tenant_id</c>.</summary>
    public static Histogram<double> NaturalLanguageDescriptionDuration { get; } =
        Instance.CreateHistogram<double>(
            NaturalLanguageDescriptionDurationName,
            unit: "ms",
            description: "Natural-language description latency per tenant.");

    /// <summary>Story 9.2 / Risk #6 — counter: total embedding API calls. Tags:
    /// <c>tenant_id</c>, <c>content_kind</c> (payload | naturalLanguageDescription).</summary>
    public static Counter<long> EmbeddingApiCalls { get; } =
        Instance.CreateCounter<long>(
            EmbeddingApiCallsName,
            unit: "{calls}",
            description: "Total embedding API calls per tenant and content kind (dual-embedding observability).");

    /// <summary>Story 9.2 / Risk #16 — counter: DAPR Conversation response-cache hit/miss observations.
    /// Emitted by <c>GenerateNaturalLanguageDescriptionActivity</c> when the DAPR Conversation sidecar
    /// surfaces a cache-status signal. Non-zero hits ACROSS multiple tenants are the canonical
    /// cross-tenant cache-leak signature. Tags: <c>tenant_id</c>, <c>cache_status</c>.</summary>
    public static Counter<long> ConversationCacheHit { get; } =
        Instance.CreateCounter<long>(
            ConversationCacheHitName,
            unit: "{calls}",
            description: "DAPR Conversation response-cache hit/miss counter (Risk #16 cross-tenant leak detector).");

    /// <summary>Gets a value indicating whether the observable gauges have been registered.</summary>
    public static bool ObservableGaugesConfigured =>
        _indexSizeGauge is not null
        && _pipelineQueueDepthGauge is not null
        && _naturalLanguageEmbeddingQueueDepthGauge is not null
        && _naturalLanguageEmbeddingQueueBytesGauge is not null;

    /// <summary>Registers the observable gauges exactly once against the shared meter.</summary>
    public static void EnsureObservableGaugesCreated(
        Func<IEnumerable<Measurement<long>>> indexSizeObserver,
        Func<IEnumerable<Measurement<int>>> pipelineQueueDepthObserver,
        Func<IEnumerable<Measurement<long>>> naturalLanguageEmbeddingQueueDepthObserver,
        Func<IEnumerable<Measurement<long>>> naturalLanguageEmbeddingQueueBytesObserver)
    {
        ArgumentNullException.ThrowIfNull(indexSizeObserver);
        ArgumentNullException.ThrowIfNull(pipelineQueueDepthObserver);
        ArgumentNullException.ThrowIfNull(naturalLanguageEmbeddingQueueDepthObserver);
        ArgumentNullException.ThrowIfNull(naturalLanguageEmbeddingQueueBytesObserver);

        lock (ObservableInstrumentGate)
        {
            _indexSizeGauge ??= Instance.CreateObservableGauge(
                IndexSizeName,
                () => indexSizeObserver(),
                unit: "{documents}",
                description: "Per-tenant per-axis index size.");

            _pipelineQueueDepthGauge ??= Instance.CreateObservableGauge(
                PipelineQueueDepthName,
                () => pipelineQueueDepthObserver(),
                unit: "{items}",
                description: "Per-tenant ingestion queue depth.");

            _naturalLanguageEmbeddingQueueDepthGauge ??= Instance.CreateObservableGauge(
                NaturalLanguageEmbeddingQueueDepthName,
                () => naturalLanguageEmbeddingQueueDepthObserver(),
                unit: "{items}",
                description: "Per-tenant natural-language embedding retry queue depth.");

            _naturalLanguageEmbeddingQueueBytesGauge ??= Instance.CreateObservableGauge(
                NaturalLanguageEmbeddingQueueBytesName,
                () => naturalLanguageEmbeddingQueueBytesObserver(),
                unit: "By",
                description: "Per-tenant natural-language embedding retry queue size in bytes (Spike 0.1 fallback bounded-payload observability).");
        }
    }

    /// <summary>
    /// Pinned tag-key policy per metric instrument. Tests use this manifest to detect drift.
    /// Keys are pinned to bounded-cardinality dimensions (tenant + axis + source_type + error_code) —
    /// case_id and user are NEVER metric tags (Risk #1 cardinality mitigation).
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> MetricTagKeyPolicy { get; } =
        new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal)
        {
            [IngestionDocumentsName] = new[] { "tenant_id" },
            [IngestionFailuresName] = new[] { "tenant_id", "error_code" },
            [SearchRequestsName] = new[] { "tenant_id", "axis" },
            [SearchDurationName] = new[] { "tenant_id", "axis" },
            [IndexSizeName] = new[] { "tenant_id", "axis" },
            [PipelineQueueDepthName] = new[] { "tenant_id" },
            [NaturalLanguageDescriptionDurationName] = new[] { "tenant_id" },
            [NaturalLanguageEmbeddingQueueDepthName] = new[] { "tenant_id" },
            [NaturalLanguageEmbeddingQueueBytesName] = new[] { "tenant_id" },
            [EmbeddingApiCallsName] = new[] { "tenant_id", "content_kind" },
            [ConversationCacheHitName] = new[] { "tenant_id", "cache_status" },
        };
}
