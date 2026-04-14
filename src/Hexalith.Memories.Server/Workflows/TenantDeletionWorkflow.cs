// <copyright file="TenantDeletionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Tenants;

using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates tenant deletion: validate -> check registry -> set Deleting ->
/// drop RediSearch -> drop RedisVector -> batched FalkorDB delete -> graph finalize ->
/// clean data keys -> remove from registry. Non-reversible; partial failures resume on re-trigger.
/// </summary>
public sealed partial class TenantDeletionWorkflow : Workflow<TenantDeletionInput, TenantDeletionResult>
{
    /// <inheritdoc/>
    public override async Task<TenantDeletionResult> RunAsync(
        WorkflowContext context,
        TenantDeletionInput input)
    {
        var logger = context.CreateReplaySafeLogger<TenantDeletionWorkflow>();
        long deletionStartedTimestamp = Stopwatch.GetTimestamp();
        bool resumedDeletion = false;

        // 1. Validate input
        try
        {
            TenantIdGuard.Validate(input.TenantId);
        }
        catch (ArgumentException ex)
        {
            return new TenantDeletionResult(input.TenantId, TenantStatus.Failed, ex.Message);
        }

        var retryOptions = new WorkflowTaskOptions(
            new WorkflowRetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(2),
                backoffCoefficient: 2.0,
                maxRetryInterval: TimeSpan.FromMinutes(5)));

        // 2. Check tenant registry
        TenantInfo? tenant = await context.CallActivityAsync<TenantInfo?>(
            nameof(GetTenantRegistryActivity),
            input.TenantId,
            retryOptions);

        if (tenant is null)
        {
            // Tenant already fully deleted (e.g., DAPR replay after completed deletion)
            return new TenantDeletionResult(input.TenantId, TenantStatus.Active, "Tenant already deleted.");
        }

        switch (tenant.Status)
        {
            case TenantStatus.Provisioning:
                return new TenantDeletionResult(input.TenantId, TenantStatus.Provisioning,
                    $"Cannot delete tenant '{input.TenantId}' while provisioning is in progress.");

            case TenantStatus.Deleting:
                resumedDeletion = true;
                break;

            default:
                // Active, Failed, CompensationFailed — proceed normally
                // 3. Set status to Deleting
                await context.CallActivityAsync<bool>(
                    nameof(UpdateTenantStatusActivity),
                    new TenantStatusUpdateInput(input.TenantId, TenantStatus.Deleting),
                    retryOptions);
                break;
        }

        if (resumedDeletion)
        {
            LogDeletionResumed(logger, input.TenantId, DateTimeOffset.UtcNow);
        }
        else
        {
            LogDeletionStarted(logger, input.TenantId, DateTimeOffset.UtcNow);
        }

        // 4. Sequential backend cleanup
        List<string> deletedBackends = [];

        try
        {
            // Drop RediSearch index (with DD — deletes mu:* hashes)
            await context.CallActivityAsync<bool>(
                nameof(DeleteRediSearchActivity),
                input,
                retryOptions);
            deletedBackends.Add("RediSearch");
            LogBackendDeleted(logger, input.TenantId, "RediSearch");

            // Drop Redis Vector index (with DD — deletes vec:* hashes)
            await context.CallActivityAsync<bool>(
                nameof(DeleteRedisVectorActivity),
                input,
                retryOptions);
            deletedBackends.Add("RedisVector");
            LogBackendDeleted(logger, input.TenantId, "RedisVector");

            // Batched FalkorDB deletion
            int batchNumber = 0;
            int batchSize = 500;

            // Get initial count for maxBatches safety valve
            BatchedGraphDeletionResult firstBatch = await context.CallActivityAsync<BatchedGraphDeletionResult>(
                nameof(DeleteFalkorDbBatchActivity),
                new BatchedGraphDeletionInput(input.TenantId, batchSize, batchNumber++),
                retryOptions);

            LogGraphBatchCompleted(logger, input.TenantId, 0, firstBatch.RemainingNodes);

            if (!firstBatch.IsComplete)
            {
                long initialCount = firstBatch.RemainingNodes + firstBatch.DeletedInBatch;
                int maxBatches = (int)((initialCount / batchSize * 2) + 10);

                while (batchNumber < maxBatches)
                {
                    BatchedGraphDeletionResult batch = await context.CallActivityAsync<BatchedGraphDeletionResult>(
                        nameof(DeleteFalkorDbBatchActivity),
                        new BatchedGraphDeletionInput(input.TenantId, batchSize, batchNumber++),
                        retryOptions);

                    LogGraphBatchCompleted(logger, input.TenantId, batchNumber - 1, batch.RemainingNodes);

                    if (batch.IsComplete)
                    {
                        break;
                    }

                    if (batchNumber >= maxBatches)
                    {
                        // Safety valve — prevent infinite loop
                        string failureMessage =
                            $"Batch loop exceeded maximum iterations ({maxBatches}). {batch.RemainingNodes} nodes remain. Re-trigger deletion to retry.";

                        await context.CallActivityAsync<bool>(
                            nameof(UpdateTenantStatusActivity),
                            new TenantStatusUpdateInput(input.TenantId, TenantStatus.Failed),
                            retryOptions);

                        LogDeletionFailed(
                            logger,
                            input.TenantId,
                            failureMessage,
                            Stopwatch.GetElapsedTime(deletionStartedTimestamp).TotalMilliseconds,
                            DateTimeOffset.UtcNow);

                        return new TenantDeletionResult(input.TenantId, TenantStatus.Failed, failureMessage)
                        {
                            DeletedBackends = deletedBackends,
                        };
                    }
                }
            }

            // Delete the empty graph
            await context.CallActivityAsync<bool>(
                nameof(DeleteFalkorDbGraphFinalizerActivity),
                input,
                retryOptions);
            deletedBackends.Add("FalkorDB");
            LogBackendDeleted(logger, input.TenantId, "FalkorDB");

            // Clean up remaining Redis data keys (case:*, dedup:*)
            await context.CallActivityAsync<bool>(
                nameof(DeleteTenantDataKeysActivity),
                input,
                retryOptions);
            deletedBackends.Add("RedisDataKeys");
            LogBackendDeleted(logger, input.TenantId, "RedisDataKeys");

            // 5. Remove from registry
            await context.CallActivityAsync<bool>(
                nameof(RemoveTenantRegistryActivity),
                input.TenantId,
                retryOptions);

            // 6. Success
            LogDeletionCompleted(
                logger,
                input.TenantId,
                Stopwatch.GetElapsedTime(deletionStartedTimestamp).TotalMilliseconds,
                DateTimeOffset.UtcNow);

            return new TenantDeletionResult(input.TenantId, TenantStatus.Active, "Tenant deleted successfully.")
            {
                DeletedBackends = deletedBackends,
            };
        }
        catch (WorkflowTaskFailedException ex)
        {
            string failureMessage = GetFailureMessage(ex);

            LogDeletionFailed(
                logger,
                input.TenantId,
                failureMessage,
                Stopwatch.GetElapsedTime(deletionStartedTimestamp).TotalMilliseconds,
                DateTimeOffset.UtcNow);

            // Mark as Failed — operator can re-trigger to resume
            try
            {
                await context.CallActivityAsync<bool>(
                    nameof(UpdateTenantStatusActivity),
                    new TenantStatusUpdateInput(input.TenantId, TenantStatus.Failed),
                    retryOptions);
            }
            catch (WorkflowTaskFailedException statusUpdateException)
            {
                string statusUpdateFailureMessage = GetFailureMessage(statusUpdateException);
                return new TenantDeletionResult(
                    input.TenantId,
                    TenantStatus.CompensationFailed,
                    $"Deletion failed: {failureMessage}. Cleanup status update also failed: {statusUpdateFailureMessage}. Cleaned backends: [{string.Join(", ", deletedBackends)}]. Re-trigger DELETE to resume.")
                {
                    DeletedBackends = deletedBackends,
                };
            }

            return new TenantDeletionResult(input.TenantId, TenantStatus.Failed,
                $"Deletion failed: {failureMessage}. Cleaned backends: [{string.Join(", ", deletedBackends)}]. Re-trigger DELETE to resume.")
            {
                DeletedBackends = deletedBackends,
            };
        }
    }

    private static string GetFailureMessage(WorkflowTaskFailedException ex)
        => string.IsNullOrWhiteSpace(ex.FailureDetails?.ErrorMessage)
            ? ex.Message
            : ex.FailureDetails.ErrorMessage;

    [LoggerMessage(Level = LogLevel.Information, Message = "DeletionStarted tenant '{TenantId}' at {Timestamp:O}")]
    private static partial void LogDeletionStarted(ILogger logger, string tenantId, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Information, Message = "DeletionResumed tenant '{TenantId}' (idempotent re-entry) at {Timestamp:O}")]
    private static partial void LogDeletionResumed(ILogger logger, string tenantId, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Information, Message = "BackendDeleted tenant '{TenantId}' backend {BackendName}")]
    private static partial void LogBackendDeleted(ILogger logger, string tenantId, string backendName);

    [LoggerMessage(Level = LogLevel.Information, Message = "GraphBatchCompleted tenant '{TenantId}' batch {BatchNumber}, {RemainingNodes} nodes remaining")]
    private static partial void LogGraphBatchCompleted(ILogger logger, string tenantId, int batchNumber, long remainingNodes);

    [LoggerMessage(Level = LogLevel.Information, Message = "DeletionCompleted tenant '{TenantId}' in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogDeletionCompleted(ILogger logger, string tenantId, double durationMs, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Error, Message = "DeletionFailed tenant '{TenantId}' after {DurationMs} ms at {Timestamp:O}: {Error}")]
    private static partial void LogDeletionFailed(ILogger logger, string tenantId, string error, double durationMs, DateTimeOffset timestamp);
}
