// <copyright file="ConsistencyRepairWorkflow.cs" company="ITANEO">
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
/// Story 8.2 — orchestrates repair of the discrepancies found by a fresh verification pass.
/// Enumerates + probes (reusing the verification activities — NOT the verification workflow),
/// then dispatches <c>RepairUnitActivity</c> for each non-<c>NoOp</c> unit.
/// </summary>
/// <remarks>
/// <para>
/// Risk #1 mitigation: each <c>RepairUnitActivity</c> call re-verifies the unit before
/// acting. The workflow does NOT rely on the verification snapshot it produces.
/// </para>
/// <para>
/// Risk #5 mitigation: up to <see cref="MaxRepairPasses"/> passes. If the tenant still has
/// discrepancies after the final pass, remaining units are flagged <c>Unrepairable</c> with
/// "did not converge" reason.
/// </para>
/// </remarks>
public sealed partial class ConsistencyRepairWorkflow
    : Workflow<ConsistencyRepairInput, ConsistencyRepairResult>
{
    /// <summary>Maximum repair passes before remaining discrepancies are marked unrepairable.</summary>
    public const int MaxRepairPasses = 3;

    /// <inheritdoc/>
    public override async Task<ConsistencyRepairResult> RunAsync(
        WorkflowContext context,
        ConsistencyRepairInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ILogger logger = context.CreateReplaySafeLogger<ConsistencyRepairWorkflow>();
        DateTimeOffset startedAt = context.CurrentUtcDateTime;

        int batchSize = ClampBatchSize(input.BatchSize);

        WorkflowTaskOptions retryOptions = new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(5)));

        context.SetCustomStatus(new ConsistencyWorkflowProgress("enumerating", 0, 0));

        List<RepairActionRecord> allActions = [];
        HashSet<string> initialDiscrepancyIds = new(StringComparer.Ordinal);
        HashSet<string> remainingDiscrepancyIds = new(StringComparer.Ordinal);
        Dictionary<string, int> latestActionIndexByUnit = new(StringComparer.Ordinal);
        Dictionary<string, RepairActionRecord> latestActionByUnit = new(StringComparer.Ordinal);
        int passes = 0;

        for (int pass = 0; pass < MaxRepairPasses; pass++)
        {
            passes = pass + 1;
            LogPassStarted(logger, input.TenantId, passes);

            // Enumerate + probe (reusing activities, not the verify workflow).
            EnumerateMemoryUnitIdsResult enumeration = await context.CallActivityAsync<EnumerateMemoryUnitIdsResult>(
                nameof(EnumerateMemoryUnitIdsActivity),
                new EnumerateMemoryUnitIdsInput(input.TenantId),
                retryOptions);

            List<(string UnitId, ConsistencyRepairRecommendation Recommendation)> discrepancies = [];
            int totalVerificationBatches = enumeration.MemoryUnitIds.Count == 0
                ? 0
                : (int)Math.Ceiling(enumeration.MemoryUnitIds.Count / (double)batchSize);

            context.SetCustomStatus(new ConsistencyWorkflowProgress("verifying", 0, totalVerificationBatches));

            for (int offset = 0; offset < enumeration.MemoryUnitIds.Count; offset += batchSize)
            {
                int end = Math.Min(offset + batchSize, enumeration.MemoryUnitIds.Count);
                Task<ConsistencyResult>[] batchTasks = new Task<ConsistencyResult>[end - offset];

                for (int i = offset; i < end; i++)
                {
                    batchTasks[i - offset] = context.CallActivityAsync<ConsistencyResult>(
                        nameof(VerifyConsistencyActivity),
                        new ConsistencyInput(enumeration.MemoryUnitIds[i], input.TenantId),
                        retryOptions);
                }

                ConsistencyResult[] batchResults = await Task.WhenAll(batchTasks);

                for (int i = 0; i < batchResults.Length; i++)
                {
                    string unitId = enumeration.MemoryUnitIds[offset + i];
                    ConsistencyResult probe = batchResults[i];

                    ConsistencyRepairRecommendation recommendation = RepairPlanCalculator.Calculate(
                        probe.SyntacticExists,
                        probe.SemanticExists,
                        probe.GraphExists);

                    if (recommendation != ConsistencyRepairRecommendation.NoOp)
                    {
                        discrepancies.Add((unitId, recommendation));
                    }
                }

                int processedBatches = (offset / batchSize) + 1;
                context.SetCustomStatus(new ConsistencyWorkflowProgress("verifying", processedBatches, totalVerificationBatches));
            }

            remainingDiscrepancyIds = discrepancies
                .Select(d => d.UnitId)
                .ToHashSet(StringComparer.Ordinal);

            if (pass == 0)
            {
                initialDiscrepancyIds = discrepancies
                    .Select(d => d.UnitId)
                    .ToHashSet(StringComparer.Ordinal);
            }

            if (discrepancies.Count == 0)
            {
                // Converged — no need for additional passes.
                break;
            }

            int totalRepairBatches = discrepancies.Count == 0
                ? 0
                : (int)Math.Ceiling(discrepancies.Count / (double)batchSize);
            context.SetCustomStatus(new ConsistencyWorkflowProgress("repairing", 0, totalRepairBatches));

            bool stopAfterCurrentBatch = false;
            int actionCountBeforeDispatch = allActions.Count;

            // Dispatch repair actions for this pass's discrepancies.
            for (int offset = 0; offset < discrepancies.Count; offset += batchSize)
            {
                int end = Math.Min(offset + batchSize, discrepancies.Count);
                Task<RepairActionRecord>[] batchTasks = new Task<RepairActionRecord>[end - offset];

                for (int i = offset; i < end; i++)
                {
                    (string unitId, ConsistencyRepairRecommendation recommendation) = discrepancies[i];
                    batchTasks[i - offset] = context.CallActivityAsync<RepairActionRecord>(
                        nameof(RepairUnitActivity),
                        new RepairUnitInput(input.TenantId, unitId, recommendation, input.IncludeUnrepairable),
                        retryOptions);
                }

                try
                {
                    _ = await Task.WhenAll(batchTasks).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    stopAfterCurrentBatch = true;
                }

                for (int i = 0; i < batchTasks.Length; i++)
                {
                    Task<RepairActionRecord> task = batchTasks[i];
                    (string unitId, _) = discrepancies[offset + i];

                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        RepairActionRecord record = task.Result;
                        allActions.Add(record);
                        latestActionIndexByUnit[unitId] = allActions.Count - 1;
                        latestActionByUnit[unitId] = record;
                        continue;
                    }

                    Exception? exception = task.Exception?.GetBaseException();
                    if (task.IsCanceled || exception is OperationCanceledException)
                    {
                        stopAfterCurrentBatch = true;
                        break;
                    }

                    if (exception is not null)
                    {
                        throw exception;
                    }
                }

                int processedRepairBatches = (offset / batchSize) + 1;
                context.SetCustomStatus(new ConsistencyWorkflowProgress("repairing", processedRepairBatches, totalRepairBatches));

                if (stopAfterCurrentBatch)
                {
                    break;
                }
            }

            // If every remaining discrepancy is Unrepairable after this pass, stop — further
            // passes cannot improve the state.
            if (remainingDiscrepancyIds.Count > 0 && remainingDiscrepancyIds.All(unitId =>
                    latestActionByUnit.TryGetValue(unitId, out RepairActionRecord? record)
                    && record.Applied == ConsistencyRepairRecommendation.Unrepairable))
            {
                break;
            }

            if (remainingDiscrepancyIds.Count > 0
                && discrepancies.Count > 0
                && (allActions.Count - actionCountBeforeDispatch) < discrepancies.Count)
            {
                break;
            }
        }

        // If we exhausted all passes and still have non-NoOp / non-Unrepairable records, mark
        // those units as Unrepairable (Risk #5).
        if (passes >= MaxRepairPasses && remainingDiscrepancyIds.Count > 0)
        {
            foreach (string unitId in remainingDiscrepancyIds)
            {
                if (!latestActionByUnit.TryGetValue(unitId, out RepairActionRecord? record)
                    || record.Applied == ConsistencyRepairRecommendation.Unrepairable)
                {
                    continue;
                }

                string failureReason = string.IsNullOrWhiteSpace(record.FailureReason)
                    ? $"Repair loop did not converge after {MaxRepairPasses} passes."
                    : $"{record.FailureReason} (Repair loop did not converge after {MaxRepairPasses} passes.)";

                RepairActionRecord updated = record with
                {
                    Applied = ConsistencyRepairRecommendation.Unrepairable,
                    Succeeded = false,
                    FailureReason = failureReason,
                };

                latestActionByUnit[unitId] = updated;
                allActions[latestActionIndexByUnit[unitId]] = updated;
            }
        }

        int totalDiscrepancies = initialDiscrepancyIds.Count;
        int repairedCount = totalDiscrepancies - remainingDiscrepancyIds.Count;
        int unrepairableCount = remainingDiscrepancyIds.Count(unitId =>
            latestActionByUnit.TryGetValue(unitId, out RepairActionRecord? record)
            && record.Applied == ConsistencyRepairRecommendation.Unrepairable);

        DateTimeOffset completedAt = context.CurrentUtcDateTime;
        context.SetCustomStatus(new ConsistencyWorkflowProgress("completed", passes, passes));
        LogRepairCompleted(
            logger,
            input.TenantId,
            allActions.Count,
            repairedCount,
            unrepairableCount,
            passes);

        return new ConsistencyRepairResult(
            TenantId: input.TenantId,
            TotalDiscrepancies: totalDiscrepancies,
            RepairedCount: repairedCount,
            UnrepairableCount: unrepairableCount,
            Actions: allActions,
            PassesExecuted: passes,
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

        if (requested < ConsistencyVerificationWorkflow.MinBatchSize)
        {
            return ConsistencyVerificationWorkflow.MinBatchSize;
        }

        if (requested > ConsistencyVerificationWorkflow.MaxBatchSize)
        {
            return ConsistencyVerificationWorkflow.MaxBatchSize;
        }

        return requested;
    }

    [LoggerMessage(
        EventId = 8207,
        Level = LogLevel.Debug,
        Message = "RepairPassStarted tenant '{TenantId}' pass {PassNumber}")]
    private static partial void LogPassStarted(ILogger logger, string tenantId, int passNumber);

    [LoggerMessage(
        EventId = 8206,
        Level = LogLevel.Information,
        Message = "RepairCompleted tenant '{TenantId}' actions {TotalActions} repaired {RepairedCount} unrepairable {UnrepairableCount} passes {PassesExecuted}")]
    private static partial void LogRepairCompleted(
        ILogger logger,
        string tenantId,
        int totalActions,
        int repairedCount,
        int unrepairableCount,
        int passesExecuted);
}
