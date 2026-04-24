// <copyright file="FailedNaturalLanguageEmbeddingRecord.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Story 9.2 Task 8.1 — entry in <c>nl-embedding-retry:{tenantId}</c> Redis Sorted Set.
/// Shape reflects Spike 0.1 UNCLEAR → bounded payload-by-value fallback: <see cref="TruncatedRawJsonPayload"/>
/// is capped at <c>NaturalLanguageDescriptionOptions.QueuedPayloadMaxBytes</c> so the sorted set has
/// <c>bounded count × bounded bytes = bounded Redis memory</c> (pre-mortem Failure δ mitigation).</summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit identifier. Retry workflow uses this as the instance id so
/// DAPR Workflow's instance-level dedup prevents double-scheduling across hosted-service restarts.</param>
/// <param name="TruncatedRawJsonPayload">The raw JSON payload truncated to the options-configured
/// byte cap. Retry workflow truncates the LLM prompt anyway (<c>MaxPayloadChars</c> default 8KB).</param>
/// <param name="EventType">The CloudEvents <c>type</c> attribute (e.g., <c>"CounterIncrementedV1"</c>).</param>
/// <param name="AggregateType">Optional aggregate-type metadata value.</param>
/// <param name="CaseId">The case identifier — needed to write the NL index hash.</param>
/// <param name="EmbeddingProvider">Embedding provider carried forward from the original ingestion.</param>
/// <param name="EmbeddingModel">Embedding model identifier.</param>
/// <param name="EmbeddingDimensions">Embedding dimensions.</param>
/// <param name="QueuedAtTicks">UTC ticks when the record was enqueued. Serves as the Redis Sorted Set
/// score so FIFO dequeue is natural.</param>
/// <param name="Attempts">Number of retry attempts so far. Records with <c>Attempts &gt;= MaxRetryAttempts</c>
/// move to the <c>nl-embedding-retry-dead:{tenantId}</c> set for operator triage.</param>
public sealed record FailedNaturalLanguageEmbeddingRecord(
    string TenantId,
    string MemoryUnitId,
    string TruncatedRawJsonPayload,
    string EventType,
    string? AggregateType,
    string CaseId,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingDimensions,
    long QueuedAtTicks,
    int Attempts);
