// <copyright file="ReIngestionCoordinator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>Coordinates claim-and-schedule re-ingestion flows, including failed-unit restoration when scheduling fails.</summary>
internal sealed class ReIngestionCoordinator
{
    private readonly IFailedUnitsRegistry _registry;
    private readonly IWorkflowPayloadStore _payloadStore;
    private readonly IIngestionWorkflowScheduler _scheduler;
    private readonly ILogger<ReIngestionCoordinator> _logger;

    public ReIngestionCoordinator(
        IFailedUnitsRegistry registry,
        IWorkflowPayloadStore payloadStore,
        IIngestionWorkflowScheduler scheduler,
        ILogger<ReIngestionCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(payloadStore);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _payloadStore = payloadStore;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task<ReIngestionAttemptResult> TryScheduleAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        CancellationToken cancellationToken)
    {
        FailedUnitRecord? record = await _registry
            .GetAsync(tenantId, memoryUnitId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return ReIngestionAttemptResult.NotFound(memoryUnitId);
        }

        if (!string.Equals(record.CaseId, caseId, StringComparison.Ordinal))
        {
            return ReIngestionAttemptResult.CaseMismatch(memoryUnitId);
        }

        ReIngestionAttemptResult? unsupported = await ValidateSourcePayloadAsync(
            record,
            cancellationToken).ConfigureAwait(false);
        if (unsupported is not null)
        {
            return unsupported;
        }

        bool claimed = await _registry
            .RemoveAsync(tenantId, caseId, memoryUnitId, record.SourceUri, cancellationToken)
            .ConfigureAwait(false);
        if (!claimed)
        {
            return ReIngestionAttemptResult.Conflict(memoryUnitId);
        }

        try
        {
            string workflowInstanceId = await _scheduler
                .ScheduleAsync(memoryUnitId, BuildIngestionInput(record), cancellationToken)
                .ConfigureAwait(false);
            RetryFailureLog.LogReIngestionScheduled(_logger, tenantId, caseId, memoryUnitId, workflowInstanceId);
            return ReIngestionAttemptResult.Scheduled(memoryUnitId, workflowInstanceId);
        }
        catch (Exception schedulingException)
        {
            await RestoreClaimAsync(record, schedulingException).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<BulkReIngestionResponse> TryScheduleManyAsync(
        string tenantId,
        string caseId,
        IReadOnlyList<string> memoryUnitIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(memoryUnitIds);

        int scheduled = 0;
        int notFound = 0;
        int conflicted = 0;
        int unsupported = 0;
        int errored = 0;
        List<ReIngestedUnitInfo> outcomes = new(memoryUnitIds.Count);

        foreach (string memoryUnitId in memoryUnitIds)
        {
            try
            {
                ReIngestionAttemptResult attempt = await TryScheduleAsync(
                    tenantId,
                    caseId,
                    memoryUnitId,
                    cancellationToken).ConfigureAwait(false);

                switch (attempt.Outcome)
                {
                    case ReIngestionAttemptOutcome.Scheduled:
                        outcomes.Add(new ReIngestedUnitInfo(memoryUnitId, attempt.WorkflowInstanceId, "scheduled", null));
                        scheduled++;
                        break;

                    case ReIngestionAttemptOutcome.NotFound:
                        outcomes.Add(new ReIngestedUnitInfo(memoryUnitId, null, "not-found", null));
                        notFound++;
                        RetryFailureLog.LogBulkReIngestionUnitSkipped(_logger, tenantId, memoryUnitId, "not-found");
                        break;

                    case ReIngestionAttemptOutcome.CaseMismatch:
                        outcomes.Add(new ReIngestedUnitInfo(memoryUnitId, null, "not-found", "case mismatch"));
                        notFound++;
                        RetryFailureLog.LogBulkReIngestionUnitSkipped(_logger, tenantId, memoryUnitId, "not-found");
                        break;

                    case ReIngestionAttemptOutcome.Conflict:
                        outcomes.Add(new ReIngestedUnitInfo(memoryUnitId, null, "conflict", null));
                        conflicted++;
                        RetryFailureLog.LogBulkReIngestionUnitSkipped(_logger, tenantId, memoryUnitId, "conflict");
                        break;

                    case ReIngestionAttemptOutcome.UnsupportedSourcePayload:
                        outcomes.Add(new ReIngestedUnitInfo(
                            memoryUnitId,
                            null,
                            "unsupported-source-payload",
                            attempt.Message)
                        {
                            ErrorCode = attempt.ErrorCode,
                        });
                        unsupported++;
                        RetryFailureLog.LogBulkReIngestionUnitSkipped(_logger, tenantId, memoryUnitId, "unsupported-source-payload");
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported re-ingestion attempt outcome '{attempt.Outcome}'.");
                }
            }
            catch (Exception ex)
            {
                outcomes.Add(new ReIngestedUnitInfo(memoryUnitId, null, "error", ex.Message));
                errored++;
                RetryFailureLog.LogBulkReIngestionUnitSkipped(_logger, tenantId, memoryUnitId, "error");
            }
        }

        return new BulkReIngestionResponse(scheduled, notFound, conflicted, errored, outcomes)
        {
            Unsupported = unsupported,
        };
    }

    private static IngestionInput BuildIngestionInput(FailedUnitRecord record) => new()
    {
        TenantId = record.TenantId,
        CaseId = record.CaseId,
        SourceUri = record.SourceUri,
        SourceType = record.SourceType,
        IngestedBy = record.IngestedBy,
        ContentType = record.SourceType == SourceType.Url
            ? string.IsNullOrWhiteSpace(record.ContentType)
                ? "application/octet-stream"
                : record.ContentType
            : string.IsNullOrWhiteSpace(record.ContentType)
                ? string.Empty
                : record.ContentType,
        ContentBytes = null,
        PayloadReference = record.SourceType == SourceType.Url ? null : record.SourcePayloadReference,
        Metadata = record.Metadata is null
            ? []
            : new Dictionary<string, MetadataField>(record.Metadata, StringComparer.Ordinal),
        CausationId = record.CausationId,
        CorrelationId = record.CorrelationId,
    };

    private async Task<ReIngestionAttemptResult?> ValidateSourcePayloadAsync(
        FailedUnitRecord record,
        CancellationToken cancellationToken)
    {
        if (record.SourceType == SourceType.Url)
        {
            return null;
        }

        WorkflowPayloadReference? reference = record.SourcePayloadReference;
        if (reference is null)
        {
            return ReIngestionAttemptResult.UnsupportedSourcePayload(record.MemoryUnitId);
        }

        string? expectedMemoryUnitId = ResolveSourcePayloadScope(record, reference);
        if (expectedMemoryUnitId is null)
        {
            return ReIngestionAttemptResult.UnsupportedSourcePayload(record.MemoryUnitId);
        }

        try
        {
            _ = await _payloadStore.ReadAsync(
                reference,
                record.TenantId,
                expectedMemoryUnitId,
                WorkflowPayloadKind.SourceBytes,
                cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex) when (ex is WorkflowPayloadException or ArgumentException)
        {
            return ReIngestionAttemptResult.UnsupportedSourcePayload(record.MemoryUnitId);
        }
    }

    private static string? ResolveSourcePayloadScope(FailedUnitRecord record, WorkflowPayloadReference reference)
    {
        if (!string.Equals(reference.TenantId, record.TenantId, StringComparison.Ordinal)
            || reference.ContentKind != WorkflowPayloadKind.SourceBytes)
        {
            return null;
        }

        if (string.Equals(reference.MemoryUnitId, record.MemoryUnitId, StringComparison.Ordinal))
        {
            return record.MemoryUnitId;
        }

        if (record.SourceType == SourceType.Event
            && string.Equals(
                reference.MemoryUnitId,
                DedupKeyBuilder.BuildKey(record.TenantId, record.CaseId, record.SourceUri),
                StringComparison.Ordinal))
        {
            return reference.MemoryUnitId;
        }

        return null;
    }

    private async Task RestoreClaimAsync(FailedUnitRecord record, Exception schedulingException)
    {
        try
        {
            await _registry.RestoreAsync(record, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception restoreException)
        {
            _logger.LogError(
                restoreException,
                "Failed to restore failed-unit claim for {MemoryUnitId} after re-ingestion scheduling failed.",
                record.MemoryUnitId);

            throw new InvalidOperationException(
                $"Re-ingestion scheduling failed for '{record.MemoryUnitId}' and the failed-unit claim could not be restored.",
                new AggregateException(schedulingException, restoreException));
        }
    }
}

internal enum ReIngestionAttemptOutcome
{
    Scheduled,
    NotFound,
    CaseMismatch,
    Conflict,
    UnsupportedSourcePayload,
}

internal sealed record ReIngestionAttemptResult(
    ReIngestionAttemptOutcome Outcome,
    string MemoryUnitId,
    string? WorkflowInstanceId,
    string? ErrorCode = null,
    string? Message = null,
    string? Suggestion = null)
{
    public static ReIngestionAttemptResult Scheduled(string memoryUnitId, string workflowInstanceId)
        => new(ReIngestionAttemptOutcome.Scheduled, memoryUnitId, workflowInstanceId);

    public static ReIngestionAttemptResult NotFound(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.NotFound, memoryUnitId, null);

    public static ReIngestionAttemptResult CaseMismatch(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.CaseMismatch, memoryUnitId, null);

    public static ReIngestionAttemptResult Conflict(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.Conflict, memoryUnitId, null);

    public static ReIngestionAttemptResult UnsupportedSourcePayload(string memoryUnitId)
        => new(
            ReIngestionAttemptOutcome.UnsupportedSourcePayload,
            memoryUnitId,
            null,
            "NON_URL_REINGESTION_UNAVAILABLE",
            "Cannot re-ingest this non-URL failed unit because the original source content is unavailable.",
            "Re-ingest from the original file or event source if available, or ingest the content again.");
}
