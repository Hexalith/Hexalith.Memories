// <copyright file="TelemetrySummary.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Story 7.5 — point-in-time per-tenant telemetry snapshot returned by
/// <c>GET /api/tenants/{tenantId}/telemetry/summary</c> (ADR-7.5-003).
/// Operator-facing poke, NOT a metrics backend; Aspire Dashboard + OTLP collector remain the source of truth.
/// </summary>
public sealed record TelemetrySummary
{
    [JsonPropertyName("tenantId")]
    public required string TenantId { get; init; }

    [JsonPropertyName("asOf")]
    public required string AsOf { get; init; }

    [JsonPropertyName("indexSizes")]
    public required TelemetryIndexSizes IndexSizes { get; init; }

    [JsonPropertyName("indexHealth")]
    public required TelemetryIndexHealth IndexHealth { get; init; }

    [JsonPropertyName("searchMetrics")]
    public required TelemetrySearchMetrics SearchMetrics { get; init; }

    [JsonPropertyName("ingestionMetrics")]
    public required TelemetryIngestionMetrics IngestionMetrics { get; init; }
}

/// <summary>Per-axis current index sizes for the tenant.</summary>
public sealed record TelemetryIndexSizes
{
    [JsonPropertyName("syntactic")]
    public required long? Syntactic { get; init; }

    [JsonPropertyName("semantic")]
    public required long? Semantic { get; init; }

    [JsonPropertyName("graph")]
    public required long? Graph { get; init; }
}

/// <summary>Per-axis current index health for the tenant.</summary>
public sealed record TelemetryIndexHealth
{
    [JsonPropertyName("syntactic")]
    public required IndexHealth Syntactic { get; init; }

    [JsonPropertyName("semantic")]
    public required IndexHealth Semantic { get; init; }

    [JsonPropertyName("graph")]
    public required IndexHealth Graph { get; init; }
}

/// <summary>Per-axis rolling 5-minute search counter deltas.</summary>
public sealed record TelemetrySearchMetrics
{
    [JsonPropertyName("syntactic")]
    public required TelemetryAxisCounters Syntactic { get; init; }

    [JsonPropertyName("semantic")]
    public required TelemetryAxisCounters Semantic { get; init; }

    [JsonPropertyName("graph")]
    public required TelemetryAxisCounters Graph { get; init; }

    [JsonPropertyName("hybrid")]
    public required TelemetryAxisCounters Hybrid { get; init; }
}

/// <summary>Rolling 5-minute counters for a single axis.</summary>
public sealed record TelemetryAxisCounters
{
    [JsonPropertyName("requestsLast5m")]
    public required long RequestsLast5m { get; init; }

    [JsonPropertyName("errorsLast5m")]
    public required long ErrorsLast5m { get; init; }
}

/// <summary>Per-tenant ingestion counters + current queue depth.</summary>
public sealed record TelemetryIngestionMetrics
{
    [JsonPropertyName("documentsLast5m")]
    public required long DocumentsLast5m { get; init; }

    [JsonPropertyName("failuresLast5m")]
    public required long FailuresLast5m { get; init; }

    [JsonPropertyName("queueDepth")]
    public required int QueueDepth { get; init; }
}
