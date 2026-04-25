// <copyright file="ConsistencyVerificationWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Consistency;

using Microsoft.Extensions.Logging;

/// <summary>
/// Story 8.2 — orchestrates tenant-wide consistency verification. Enumerates the union of
/// memory unit IDs across the three backends, then fans out per-unit probes in bounded
/// batches and aggregates actionable discrepancies plus informational notes.
/// </summary>
/// <remarks>
/// <para>
/// Read-only workflow — no state mutation on any backend. Idempotent re-entry: re-running
/// with the same input produces the same result (up to timing fields).
/// </para>
/// <para>
/// Risk #2 mitigation: fan-out is bounded per batch via <c>Task.WhenAll</c> on a slice of
/// <c>batchSize</c> <c>VerifyConsistencyActivity</c> calls; batches run sequentially.
/// </para>
/// <para>
/// Risk #7 mitigation: <c>Discrepancies</c> and <c>Notes</c> have INDEPENDENT 10,000-entry payload
/// caps (decision S6-D1, re-review 2026-04-25 — informational notes never evict actionable
/// discrepancies). The total discrepancy/note counters remain un-truncated and
/// <c>TruncatedAt</c> is set when EITHER list was truncated.
/// </para>
/// </remarks>
public sealed partial class ConsistencyVerificationWorkflow
    : Workflow<ConsistencyVerificationInput, ConsistencyVerificationResult>
{
    /// <summary>Maximum number of discrepancy entries that fit in the DAPR workflow state store per invocation.</summary>
    public const int MaxDiscrepancyEntries = 10_000;

    /// <summary>Maximum number of informational note entries (independent cap from discrepancies — decision S6-D1, 2026-04-25).</summary>
    public const int MaxNoteEntries = 10_000;

    /// <summary>Minimum valid batch size.</summary>
    public const int MinBatchSize = 10;

    /// <summary>Maximum valid batch size.</summary>
    public const int MaxBatchSize = 5_000;

    /// <inheritdoc/>
    public override async Task<ConsistencyVerificationResult> RunAsync(
        WorkflowContext context,
        ConsistencyVerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ILogger logger = context.CreateReplaySafeLogger<ConsistencyVerificationWorkflow>();
        DateTimeOffset startedAt = context.CurrentUtcDateTime;

        int batchSize = ClampBatchSize(input.BatchSize);

        WorkflowTaskOptions retryOptions = new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(5)));

        context.SetCustomStatus(new ConsistencyWorkflowProgress("enumerating", 0, 0));

        // 1. Enumerate the union across three backends.
        EnumerateMemoryUnitIdsResult enumeration = await context.CallActivityAsync<EnumerateMemoryUnitIdsResult>(
            nameof(EnumerateMemoryUnitIdsActivity),
            new EnumerateMemoryUnitIdsInput(input.TenantId),
            retryOptions);

        List<ConsistencyDiscrepancy> discrepancies = [];
        List<ConsistencyDiscrepancy> notes = [];
        int consistentCount = 0;
        int totalDiscrepancyCount = 0;
        int totalNoteCount = 0;
        IReadOnlyList<string> allIds = enumeration.MemoryUnitIds;
        int totalBatches = allIds.Count == 0 ? 0 : (int)Math.Ceiling(allIds.Count / (double)batchSize);

        context.SetCustomStatus(new ConsistencyWorkflowProgress("verifying", 0, totalBatches));

        // 2. Fan out per-unit probes in bounded batches.
        for (int offset = 0; offset < allIds.Count; offset += batchSize)
        {
            int end = Math.Min(offset + batchSize, allIds.Count);
            Task<ConsistencyResult>[] batchTasks = new Task<ConsistencyResult>[end - offset];

            for (int i = offset; i < end; i++)
            {
                batchTasks[i - offset] = context.CallActivityAsync<ConsistencyResult>(
                    nameof(VerifyConsistencyActivity),
                    new ConsistencyInput(allIds[i], input.TenantId),
                    retryOptions);
            }

            ConsistencyResult[] batchResults = await Task.WhenAll(batchTasks);

            for (int i = 0; i < batchResults.Length; i++)
            {
                string unitId = allIds[offset + i];
                ConsistencyResult probe = batchResults[i];

                ConsistencyRepairRecommendation recommendation = RepairPlanCalculator.Calculate(
                    probe.SyntacticExists,
                    probe.SemanticExists,
                    probe.GraphExists);

                ConsistencyNoteKind consistencyNoteKind = probe.ConsistencyNoteKind != ConsistencyNoteKind.None
                    ? probe.ConsistencyNoteKind
                    : NaturalLanguageConsistencyState.BuildConsistencyNoteKind(
                        probe.NaturalLanguageEmbeddingStatus,
                        probe.NaturalLanguageSemanticExists);
                bool hasConsistencyNote = consistencyNoteKind != ConsistencyNoteKind.None
                    || !string.IsNullOrWhiteSpace(probe.ConsistencyNote);
                // S6-P11 (re-review 2026-04-25): hoist isNoteOnlyObservation once so early-continue,
                // counter increment, and routing all share a single source of truth.
                bool isNoteOnlyObservation = recommendation == ConsistencyRepairRecommendation.NoOp && hasConsistencyNote;

                if (recommendation == ConsistencyRepairRecommendation.NoOp)
                {
                    consistentCount++;

                    if (!hasConsistencyNote)
                    {
                        continue;
                    }

                    totalNoteCount++;
                }
                else
                {
                    totalDiscrepancyCount++;
                }

                string discrepancyLabel = recommendation != ConsistencyRepairRecommendation.NoOp
                    ? recommendation.ToString()
                    : consistencyNoteKind != ConsistencyNoteKind.None
                        ? consistencyNoteKind.ToString()
                        : "InformationalNote";
                LogDiscrepancyDetected(logger, input.TenantId, unitId, discrepancyLabel);

                ConsistencyDiscrepancy entry = new ConsistencyDiscrepancy(
                    unitId,
                    probe.SyntacticExists,
                    probe.SemanticExists,
                    probe.GraphExists,
                    recommendation)
                {
                    NaturalLanguageSemanticPresent = probe.NaturalLanguageSemanticExists,
                    NaturalLanguageEmbeddingStatus = probe.NaturalLanguageEmbeddingStatus,
                    ConsistencyNote = probe.ConsistencyNote,
                    ConsistencyNoteKind = consistencyNoteKind,
                };

                // Independent caps (decision S6-D1, re-review 2026-04-25): notes never evict
                // actionable discrepancies because each list has its own budget.
                if (isNoteOnlyObservation)
                {
                    if (notes.Count < MaxNoteEntries)
                    {
                        notes.Add(entry);
                    }
                }
                else if (discrepancies.Count < MaxDiscrepancyEntries)
                {
                    discrepancies.Add(entry);
                }
            }

            int processedBatches = (offset / batchSize) + 1;
            context.SetCustomStatus(new ConsistencyWorkflowProgress("verifying", processedBatches, totalBatches));
        }

        int totalUnits = allIds.Count;
        // S6-P2 (re-review 2026-04-25): restore the original invariant
        // ConsistentCount + InconsistentCount = TotalUnits by computing inconsistentCount as the
        // complement of consistentCount. This stays equal to totalDiscrepancyCount by construction
        // (every non-NoOp recommendation increments both consistentCount's complement AND
        // totalDiscrepancyCount), but the explicit form documents the invariant.
        int inconsistentCount = totalUnits - consistentCount;

        // Independent truncation tracking (decision S6-D1) — TruncatedAt is set when EITHER list
        // reached its cap; operators detect which list was truncated by comparing
        // totalDiscrepancyCount against discrepancies.Count and totalNoteCount against notes.Count.
        bool discrepancyTruncated = totalDiscrepancyCount > discrepancies.Count;
        bool noteTruncated = totalNoteCount > notes.Count;
        DateTimeOffset? truncatedAt = null;
        if (discrepancyTruncated)
        {
            truncatedAt = context.CurrentUtcDateTime;
            LogDiscrepancyListTruncated(logger, input.TenantId, totalDiscrepancyCount, MaxDiscrepancyEntries);
        }

        if (noteTruncated)
        {
            truncatedAt ??= context.CurrentUtcDateTime;
            LogNotesListTruncated(logger, input.TenantId, totalNoteCount, MaxNoteEntries);
        }

        DateTimeOffset completedAt = context.CurrentUtcDateTime;
        context.SetCustomStatus(new ConsistencyWorkflowProgress("completed", totalBatches, totalBatches));
        LogVerificationCompleted(logger, input.TenantId, totalUnits, consistentCount, inconsistentCount, totalNoteCount);

        return new ConsistencyVerificationResult(
            TenantId: input.TenantId,
            TotalUnits: totalUnits,
            ConsistentCount: consistentCount,
            InconsistentCount: inconsistentCount,
            Discrepancies: discrepancies,
            TotalDiscrepancyCount: totalDiscrepancyCount,
            TruncatedAt: truncatedAt,
            EnumerationTruncated: enumeration.Truncated,
            StartedAt: startedAt,
            CompletedAt: completedAt,
            Duration: completedAt - startedAt)
        {
            // S6-P1 (re-review 2026-04-25): NoteCount is the in-payload count (mirrors
            // Discrepancies.Count for the discrepancy list); TotalNoteCount is the un-truncated
            // tally. Comparing them detects truncation of the notes payload.
            NoteCount = notes.Count,
            TotalNoteCount = totalNoteCount,
            Notes = notes,
        };
    }

    private static int ClampBatchSize(int requested)
    {
        if (requested <= 0)
        {
            return 500;
        }

        if (requested < MinBatchSize)
        {
            return MinBatchSize;
        }

        if (requested > MaxBatchSize)
        {
            return MaxBatchSize;
        }

        return requested;
    }

    [LoggerMessage(
        EventId = 8201,
        Level = LogLevel.Information,
        Message = "DiscrepancyDetected tenant '{TenantId}' unit '{MemoryUnitId}' recommendation {Recommendation}")]
    private static partial void LogDiscrepancyDetected(
        ILogger logger, string tenantId, string memoryUnitId, string recommendation);

    // S6-P6 (re-review 2026-04-25): restore the original DiscrepancyListTruncated message prefix
    // so operator dashboards keyed on the literal text continue to fire. With independent caps
    // (S6-D1), notes truncation gets its own EventId 8210.
    [LoggerMessage(
        EventId = 8204,
        Level = LogLevel.Warning,
        Message = "DiscrepancyListTruncated tenant '{TenantId}' total {TotalDiscrepancies}, truncated to {Cap}")]
    private static partial void LogDiscrepancyListTruncated(
        ILogger logger, string tenantId, int totalDiscrepancies, int cap);

    [LoggerMessage(
        EventId = 8210,
        Level = LogLevel.Warning,
        Message = "NotesListTruncated tenant '{TenantId}' total {TotalNotes}, truncated to {Cap}")]
    private static partial void LogNotesListTruncated(
        ILogger logger, string tenantId, int totalNotes, int cap);

    [LoggerMessage(
        EventId = 8205,
        Level = LogLevel.Information,
        Message = "VerificationCompleted tenant '{TenantId}' total {TotalUnits} consistent {ConsistentCount} inconsistent {InconsistentCount} notes {NoteCount}")]
    private static partial void LogVerificationCompleted(
        ILogger logger, string tenantId, int totalUnits, int consistentCount, int inconsistentCount, int noteCount);
}
