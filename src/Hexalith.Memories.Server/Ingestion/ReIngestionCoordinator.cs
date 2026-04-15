// <copyright file="ReIngestionCoordinator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

/// <summary>Coordinates claim-and-schedule re-ingestion flows, including failed-unit restoration when scheduling fails.</summary>
internal sealed class ReIngestionCoordinator
{
    private readonly IFailedUnitsRegistry _registry;
    private readonly IIngestionWorkflowScheduler _scheduler;
    private readonly ILogger<ReIngestionCoordinator> _logger;

    public ReIngestionCoordinator(
        IFailedUnitsRegistry registry,
        IIngestionWorkflowScheduler scheduler,
        ILogger<ReIngestionCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
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
                .ScheduleAsync(memoryUnitId, BuildIngestionInput(record))
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

        return new BulkReIngestionResponse(scheduled, notFound, conflicted, errored, outcomes);
    }

    private static IngestionInput BuildIngestionInput(FailedUnitRecord record) => new()
    {
        TenantId = record.TenantId,
        CaseId = record.CaseId,
        SourceUri = record.SourceUri,
        SourceType = record.SourceType,
        IngestedBy = record.IngestedBy,
        ContentType = record.SourceType == SourceType.Url
            ? string.Empty
            : record.ContentType ?? string.Empty,
        ContentBytes = null,
        Metadata = [],
    };

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
}

internal sealed record ReIngestionAttemptResult(
    ReIngestionAttemptOutcome Outcome,
    string MemoryUnitId,
    string? WorkflowInstanceId)
{
    public static ReIngestionAttemptResult Scheduled(string memoryUnitId, string workflowInstanceId)
        => new(ReIngestionAttemptOutcome.Scheduled, memoryUnitId, workflowInstanceId);

    public static ReIngestionAttemptResult NotFound(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.NotFound, memoryUnitId, null);

    public static ReIngestionAttemptResult CaseMismatch(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.CaseMismatch, memoryUnitId, null);

    public static ReIngestionAttemptResult Conflict(string memoryUnitId)
        => new(ReIngestionAttemptOutcome.Conflict, memoryUnitId, null);
}