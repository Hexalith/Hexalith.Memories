// <copyright file="QueueNaturalLanguageEmbeddingRetryInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Story 9.2 Task 5.4 — payload for <c>QueueNaturalLanguageEmbeddingRetryActivity</c>. Carries
/// identifiers only (payload-by-reference — pre-mortem Failure δ mitigation). The retry workflow re-reads
/// the raw event bytes from the memory unit's existing hash; queue entries stay ~100 bytes regardless of
/// event size.</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="RawJsonPayload">Bounded payload-by-value fallback (Spike 0.1 UNCLEAR branch) — truncated
/// to <c>NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes</c>. Empty string when the retry-service
/// can re-read from a durable store. Present only when the ingestion-time payload cannot be recovered.</param>
/// <param name="EventType">The CloudEvents <c>type</c> attribute (e.g., <c>"CounterIncrementedV1"</c>).</param>
/// <param name="AggregateType">Optional aggregate-type metadata value.</param>
/// <param name="CaseId">The case identifier — needed by <c>IndexNaturalLanguageSemanticActivity</c>.</param>
/// <param name="EmbeddingProvider">The embedding provider identifier (carried forward from ingestion).</param>
/// <param name="EmbeddingModel">The embedding model identifier.</param>
/// <param name="EmbeddingDimensions">The embedding dimensions.</param>
/// <param name="QueuedAtTicks">Workflow-deterministic timestamp (<c>WorkflowContext.CurrentUtcDateTime.Ticks</c>)
/// used as the Sorted-Set score for FIFO ordering of the retry queue. Positional with default <c>0</c> so
/// historical activity-input JSON (pre-fix) deserializes to the legacy shape; the activity falls back to
/// <c>DateTime.UtcNow.Ticks</c> when the value is <c>0</c>.</param>
public sealed record QueueNaturalLanguageEmbeddingRetryInput(
    string TenantId,
    string MemoryUnitId,
    string RawJsonPayload,
    string EventType,
    string? AggregateType,
    string CaseId,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingDimensions,
    long QueuedAtTicks = 0L);
