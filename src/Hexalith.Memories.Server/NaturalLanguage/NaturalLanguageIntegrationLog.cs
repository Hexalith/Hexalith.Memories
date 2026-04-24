// <copyright file="NaturalLanguageIntegrationLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using Microsoft.Extensions.Logging;

/// <summary>Story 9.2 — structured log events for the dual-embedding pipeline. EventId bank
/// <c>9150-9199</c> is pinned for this story; DO NOT reuse IDs elsewhere. Ranges:
/// <list type="bullet">
///   <item><description>9150-9159 — Happy-path / behavioral (NL generation, queue, retry, stub resolution)</description></item>
///   <item><description>9160-9169 — Configuration / environment (outages, echo dev, rate-limit sizing)</description></item>
///   <item><description>9170-9179 — Operational (queue backlog, deploy gate, cache, startup)</description></item>
///   <item><description>9180+ — Terminal failures (dead-letter)</description></item>
/// </list></summary>
internal static partial class NaturalLanguageIntegrationLog
{
    [LoggerMessage(
        EventId = 9150,
        Level = LogLevel.Debug,
        Message = "NL description generated for tenant {TenantId}, memoryUnit {MemoryUnitId} via {LlmProvider}/{LlmModel} in {DurationMs}ms (confidenceSource={ConfidenceSource}).")]
    public static partial void NaturalLanguageDescriptionGenerated(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string llmProvider,
        string llmModel,
        long durationMs,
        string confidenceSource);

    [LoggerMessage(
        EventId = 9151,
        Level = LogLevel.Information,
        Message = "NL description skipped (LLM unavailable) for tenant {TenantId}, memoryUnit {MemoryUnitId}: {Reason}.")]
    public static partial void NaturalLanguageDescriptionSkippedLlmUnavailable(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string reason);

    [LoggerMessage(
        EventId = 9152,
        Level = LogLevel.Information,
        Message = "NL embedding queued for retry: tenant {TenantId}, memoryUnit {MemoryUnitId}, queuedAt={QueuedAtTicks}.")]
    public static partial void NaturalLanguageEmbeddingQueuedForRetry(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        long queuedAtTicks);

    [LoggerMessage(
        EventId = 9161,
        Level = LogLevel.Critical,
        Message = "DAPR Conversation component '{ComponentName}' resolves to the echo component in Production and has been rejected.")]
    public static partial void EchoComponentRejectedInProduction(
        ILogger logger,
        string componentName);

    [LoggerMessage(
        EventId = 9164,
        Level = LogLevel.Critical,
        Message = "DAPR Conversation component '{ComponentName}' enables response caching with TTL '{CacheTtl}' without explicit cross-tenant acknowledgment.")]
    public static partial void ResponseCacheRejectedWithoutAcknowledgment(
        ILogger logger,
        string componentName,
        string cacheTtl);

    [LoggerMessage(
        EventId = 9153,
        Level = LogLevel.Information,
        Message = "NL embedding retry succeeded for tenant {TenantId}, memoryUnit {MemoryUnitId} after {Attempts} attempt(s).")]
    public static partial void NaturalLanguageEmbeddingRetrySucceeded(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        int attempts);

    [LoggerMessage(
        EventId = 9154,
        Level = LogLevel.Information,
        Message = "Stub node resolved for tenant {TenantId}, memoryUnit {MemoryUnitId} (causing event {CausingEventId}); stubCreatedAt={StubCreatedAt}, resolvedAt={ResolvedAt}.")]
    public static partial void StubNodeResolved(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string causingEventId,
        string stubCreatedAt,
        string resolvedAt);

    [LoggerMessage(
        EventId = 9155,
        Level = LogLevel.Debug,
        Message = "CorrelationId self-edge skipped for memoryUnit {MemoryUnitId} (event IS the correlation root).")]
    public static partial void CorrelationIdSelfEdgeSkipped(
        ILogger logger,
        string memoryUnitId);

    [LoggerMessage(
        EventId = 9162,
        Level = LogLevel.Warning,
        Message = "DAPR Conversation resolved to the echo component ({LlmProvider}); NL embeddings will be degenerate (equal to raw payload). Development only — production rejects this via the options validator.")]
    public static partial void ConversationApiIsEchoComponent(
        ILogger logger,
        string llmProvider);
}
