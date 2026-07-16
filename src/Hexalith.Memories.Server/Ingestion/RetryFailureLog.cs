// <copyright file="RetryFailureLog.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Structured log events for Story 6.3 retry, failure visibility, and re-ingestion. Event IDs 6301–6310
/// are pinned for dashboard/alert wiring — do NOT reuse these IDs elsewhere.
/// </summary>
internal static partial class RetryFailureLog
{
    [LoggerMessage(
        EventId = 6301,
        Level = LogLevel.Debug,
        Message = "Retry attempt started for {ActivityName} on {MemoryUnitId} (attempt {Attempt}).")]
    internal static partial void LogRetryAttemptStarted(
        ILogger logger,
        string activityName,
        string memoryUnitId,
        int attempt);

    [LoggerMessage(
        EventId = 6302,
        Level = LogLevel.Warning,
        Message = "Retry exhausted for {ActivityName} on {MemoryUnitId}; final error code {FinalErrorCode}.")]
    internal static partial void LogRetryExhausted(
        ILogger logger,
        string activityName,
        string memoryUnitId,
        string finalErrorCode);

    [LoggerMessage(
        EventId = 6303,
        Level = LogLevel.Information,
        Message = "Failed unit persisted for tenant {TenantId}, unit {MemoryUnitId} at stage {Stage} (errorCode {ErrorCode}).")]
    internal static partial void LogFailedUnitPersisted(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string stage,
        string errorCode);

    [LoggerMessage(
        EventId = 6304,
        Level = LogLevel.Information,
        Message = "Re-ingestion scheduled for tenant {TenantId}, case {CaseId}, unit {MemoryUnitId} as workflow {NewWorkflowInstanceId}.")]
    internal static partial void LogReIngestionScheduled(
        ILogger logger,
        string tenantId,
        string caseId,
        string memoryUnitId,
        string newWorkflowInstanceId);

    [LoggerMessage(
        EventId = 6305,
        Level = LogLevel.Warning,
        Message = "Bulk re-ingestion skipped tenant {TenantId} unit {MemoryUnitId} ({Reason}).")]
    internal static partial void LogBulkReIngestionUnitSkipped(
        ILogger logger,
        string tenantId,
        string memoryUnitId,
        string reason);

    [LoggerMessage(
        EventId = 6306,
        Level = LogLevel.Debug,
        Message = "Failed-units list queried for tenant {TenantId}, case {CaseId} (limit {Limit}, offset {Offset}, returned {ReturnedCount} of {TotalCount}).")]
    internal static partial void LogFailedUnitsListQueried(
        ILogger logger,
        string tenantId,
        string caseId,
        int limit,
        int offset,
        int returnedCount,
        int totalCount);

    [LoggerMessage(
        EventId = 6307,
        Level = LogLevel.Debug,
        Message = "Counter actor transition applied for tenant {TenantId}, case {CaseId}: {PreviousStage} → {NextStage} ({TransitionId}).")]
    internal static partial void LogCounterActorTransitionApplied(
        ILogger logger,
        string tenantId,
        string caseId,
        string previousStage,
        string nextStage,
        string transitionId);

    [LoggerMessage(
        EventId = 6308,
        Level = LogLevel.Debug,
        Message = "Counter actor transition idempotent for tenant {TenantId}, case {CaseId} ({TransitionId}).")]
    internal static partial void LogCounterActorTransitionIdempotent(
        ILogger logger,
        string tenantId,
        string caseId,
        string transitionId);

    [LoggerMessage(
        EventId = 6309,
        Level = LogLevel.Error,
        Message = "Failed-unit persistence failed for {MemoryUnitId}: {Reason}.")]
    internal static partial void LogFailedUnitPersistenceFailed(
        ILogger logger,
        string memoryUnitId,
        string reason);

    [LoggerMessage(
        EventId = 6310,
        Level = LogLevel.Warning,
        Message = "Counter actor transition failed for tenant {TenantId}, case {CaseId} ({PreviousStage} → {NextStage}): {Reason}.")]
    internal static partial void LogCounterTransitionFailed(
        ILogger logger,
        string tenantId,
        string caseId,
        string previousStage,
        string nextStage,
        string reason);
}
