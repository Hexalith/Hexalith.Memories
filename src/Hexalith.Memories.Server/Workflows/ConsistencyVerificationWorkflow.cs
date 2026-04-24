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
/// batches and aggregates discrepancies.
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
/// Risk #7 mitigation: <c>Discrepancies</c> is truncated to 10,000 entries;
/// <c>TotalDiscrepancyCount</c> remains un-truncated and <c>TruncatedAt</c> is set.
/// </para>
/// </remarks>
public sealed partial class ConsistencyVerificationWorkflow
    : Workflow<ConsistencyVerificationInput, ConsistencyVerificationResult>
{
    /// <summary>Maximum number of discrepancies that fit in the DAPR workflow state store per invocation.</summary>
    public const int MaxDiscrepancyEntries = 10_000;

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
        int consistentCount = 0;
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

                bool naturalLanguageGap = NaturalLanguageConsistencyState.HasIndexedNaturalLanguageGap(
                    probe.NaturalLanguageEmbeddingStatus,
                    probe.NaturalLanguageSemanticExists);

                if (recommendation == ConsistencyRepairRecommendation.NoOp && !naturalLanguageGap)
                {
                    consistentCount++;
                    continue;
                }

                string discrepancyLabel = naturalLanguageGap
                    ? "MissingNaturalLanguageSemantic"
                    : recommendation.ToString();
                LogDiscrepancyDetected(logger, input.TenantId, unitId, discrepancyLabel);

                if (discrepancies.Count < MaxDiscrepancyEntries)
                {
                    discrepancies.Add(new ConsistencyDiscrepancy(
                        unitId,
                        probe.SyntacticExists,
                        probe.SemanticExists,
                        probe.GraphExists,
                        recommendation)
                    {
                        NaturalLanguageSemanticPresent = probe.NaturalLanguageSemanticExists,
                        NaturalLanguageEmbeddingStatus = probe.NaturalLanguageEmbeddingStatus,
                        ConsistencyNote = probe.ConsistencyNote,
                    });
                }
            }

            int processedBatches = (offset / batchSize) + 1;
            context.SetCustomStatus(new ConsistencyWorkflowProgress("verifying", processedBatches, totalBatches));
        }

        int totalUnits = allIds.Count;
        int inconsistentCount = totalUnits - consistentCount;

        DateTimeOffset? truncatedAt = null;
        if (inconsistentCount > MaxDiscrepancyEntries)
        {
            truncatedAt = context.CurrentUtcDateTime;
            LogTruncation(logger, input.TenantId, inconsistentCount, MaxDiscrepancyEntries);
        }

        DateTimeOffset completedAt = context.CurrentUtcDateTime;
        context.SetCustomStatus(new ConsistencyWorkflowProgress("completed", totalBatches, totalBatches));
        LogVerificationCompleted(logger, input.TenantId, totalUnits, consistentCount, inconsistentCount);

        return new ConsistencyVerificationResult(
            TenantId: input.TenantId,
            TotalUnits: totalUnits,
            ConsistentCount: consistentCount,
            InconsistentCount: inconsistentCount,
            Discrepancies: discrepancies,
            TotalDiscrepancyCount: inconsistentCount,
            TruncatedAt: truncatedAt,
            EnumerationTruncated: enumeration.Truncated,
            StartedAt: startedAt,
            CompletedAt: completedAt,
            Duration: completedAt - startedAt);
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

    [LoggerMessage(
        EventId = 8204,
        Level = LogLevel.Warning,
        Message = "DiscrepancyListTruncated tenant '{TenantId}' total {TotalDiscrepancies}, truncated to {Cap}")]
    private static partial void LogTruncation(
        ILogger logger, string tenantId, int totalDiscrepancies, int cap);

    [LoggerMessage(
        EventId = 8205,
        Level = LogLevel.Information,
        Message = "VerificationCompleted tenant '{TenantId}' total {TotalUnits} consistent {ConsistentCount} inconsistent {InconsistentCount}")]
    private static partial void LogVerificationCompleted(
        ILogger logger, string tenantId, int totalUnits, int consistentCount, int inconsistentCount);
}
