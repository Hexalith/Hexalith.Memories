// <copyright file="TenantProvisioningWorkflow.cs" company="ITANEO">
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
/// Orchestrates tenant provisioning: validate → register → provision RediSearch → provision Redis Vector
/// → provision FalkorDB → verify → mark active. Uses saga/compensation for rollback on failure.
/// </summary>
public sealed partial class TenantProvisioningWorkflow : Workflow<TenantProvisioningInput, TenantProvisioningResult>
{
    /// <inheritdoc/>
    public override async Task<TenantProvisioningResult> RunAsync(
        WorkflowContext context,
        TenantProvisioningInput input)
    {
        var logger = context.CreateReplaySafeLogger<TenantProvisioningWorkflow>();
        string workflowInstanceId = string.IsNullOrWhiteSpace(context.InstanceId)
            ? context.NewGuid().ToString()
            : context.InstanceId;
        long provisioningStartedTimestamp = Stopwatch.GetTimestamp();

        // Validate input
        try
        {
            TenantIdGuard.Validate(input.TenantId);
        }
        catch (ArgumentException ex)
        {
            return new TenantProvisioningResult(input.TenantId, TenantStatus.Failed, ex.Message)
            {
                ErrorCode = "INVALID_TENANT_ID",
            };
        }

        if (string.IsNullOrWhiteSpace(input.DisplayName))
        {
            return new TenantProvisioningResult(input.TenantId, TenantStatus.Failed, "DisplayName is required.")
            {
                ErrorCode = "INVALID_INPUT",
            };
        }

        var provisionRetryOptions = new WorkflowTaskOptions(
            new WorkflowRetryPolicy(
                maxNumberOfAttempts: 5,
                firstRetryInterval: TimeSpan.FromSeconds(2),
                backoffCoefficient: 2.0,
                maxRetryInterval: TimeSpan.FromMinutes(5)));

        var compensationRetryOptions = new WorkflowTaskOptions(
            new WorkflowRetryPolicy(
                maxNumberOfAttempts: 3,
                firstRetryInterval: TimeSpan.FromSeconds(1),
                maxRetryInterval: TimeSpan.FromSeconds(30)));

        // Initialize tenant registry (atomic check-and-register)
        try
        {
            _ = await context.CallActivityAsync<TenantInfo>(
                nameof(InitializeTenantRegistryActivity),
                new InitializeTenantRegistryInput(input.TenantId, input.DisplayName, workflowInstanceId),
                provisionRetryOptions);
        }
        catch (WorkflowTaskFailedException ex) when (ex.FailureDetails?.ErrorMessage?.Contains("TENANT_ALREADY_EXISTS") == true)
        {
            return new TenantProvisioningResult(input.TenantId, TenantStatus.Failed,
                $"Tenant '{input.TenantId}' already exists.")
            {
                ErrorCode = "TENANT_ALREADY_EXISTS",
            };
        }

        LogProvisioningStarted(logger, input.TenantId, input.DisplayName, DateTimeOffset.UtcNow);

        // Provision backends with saga/compensation
        List<string> completedBackends = [];
        try
        {
            await context.CallActivityAsync<bool>(
                nameof(ProvisionRediSearchActivity), input, provisionRetryOptions);
            completedBackends.Add("syntactic");

            await context.CallActivityAsync<bool>(
                nameof(ProvisionRedisVectorActivity), input, provisionRetryOptions);
            completedBackends.Add("semantic");

            await context.CallActivityAsync<bool>(
                nameof(ProvisionFalkorDbActivity), input, provisionRetryOptions);
            completedBackends.Add("graph");

            // Verify all backends (no retry — verification is deterministic)
            await context.CallActivityAsync<bool>(nameof(VerifyTenantActivity), input);

            // Mark tenant as active
            await context.CallActivityAsync<bool>(
                nameof(UpdateTenantStatusActivity),
                new TenantStatusUpdateInput(input.TenantId, TenantStatus.Active, workflowInstanceId),
                compensationRetryOptions);

            LogProvisioningCompleted(
                logger,
                input.TenantId,
                Stopwatch.GetElapsedTime(provisioningStartedTimestamp).TotalMilliseconds,
                DateTimeOffset.UtcNow);
            return new TenantProvisioningResult(input.TenantId, TenantStatus.Active,
                "Tenant provisioned successfully.");
        }
        catch (WorkflowTaskFailedException ex)
        {
            LogProvisioningFailed(
                logger,
                input.TenantId,
                ex.FailureDetails?.ErrorMessage ?? ex.Message,
                Stopwatch.GetElapsedTime(provisioningStartedTimestamp).TotalMilliseconds,
                DateTimeOffset.UtcNow);

            // Compensate: delete created backends
            try
            {
                long compensationStartedTimestamp = Stopwatch.GetTimestamp();
                LogCompensationStarted(logger, input.TenantId, string.Join(", ", completedBackends), DateTimeOffset.UtcNow);

                if (completedBackends.Contains("syntactic"))
                {
                    await context.CallActivityAsync<bool>(
                        nameof(DeleteRediSearchIndexActivity), input, compensationRetryOptions);
                }

                if (completedBackends.Contains("semantic"))
                {
                    await context.CallActivityAsync<bool>(
                        nameof(DeleteRedisVectorIndexActivity), input, compensationRetryOptions);
                }

                if (completedBackends.Contains("graph"))
                {
                    await context.CallActivityAsync<bool>(
                        nameof(DeleteFalkorDbGraphActivity), input, compensationRetryOptions);
                }

                // Compensation succeeded — mark as Failed (retryable)
                await context.CallActivityAsync<bool>(
                    nameof(UpdateTenantStatusActivity),
                    new TenantStatusUpdateInput(input.TenantId, TenantStatus.Failed, workflowInstanceId),
                    compensationRetryOptions);

                LogCompensationCompleted(
                    logger,
                    input.TenantId,
                    Stopwatch.GetElapsedTime(compensationStartedTimestamp).TotalMilliseconds,
                    DateTimeOffset.UtcNow);

                return new TenantProvisioningResult(input.TenantId, TenantStatus.Failed,
                    $"Provisioning failed: {ex.FailureDetails?.ErrorMessage}. Cleanup completed.")
                {
                    CompensatedAxes = completedBackends,
                };
            }
            catch (WorkflowTaskFailedException compensationEx)
            {
                // Compensation itself failed — orphaned resources exist
                LogCompensationFailed(
                    logger,
                    input.TenantId,
                    string.Join(", ", completedBackends),
                    compensationEx.Message,
                    DateTimeOffset.UtcNow);

                try
                {
                    await context.CallActivityAsync<bool>(
                        nameof(UpdateTenantStatusActivity),
                        new TenantStatusUpdateInput(input.TenantId, TenantStatus.CompensationFailed, workflowInstanceId),
                        compensationRetryOptions);
                }
                catch (WorkflowTaskFailedException)
                {
                    // Even status update failed — log and return
                }

                return new TenantProvisioningResult(input.TenantId, TenantStatus.CompensationFailed,
                    $"Provisioning failed AND cleanup failed. Orphaned resources in: [{string.Join(", ", completedBackends)}]. Manual cleanup required.")
                {
                    CompensatedAxes = [],
                };
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "ProvisioningStarted tenant '{TenantId}' ('{DisplayName}') at {Timestamp:O}")]
    private static partial void LogProvisioningStarted(ILogger logger, string tenantId, string displayName, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Information, Message = "ProvisioningCompleted tenant '{TenantId}' in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogProvisioningCompleted(ILogger logger, string tenantId, double durationMs, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Error, Message = "ProvisioningFailed tenant '{TenantId}' after {DurationMs} ms at {Timestamp:O}: {Error}")]
    private static partial void LogProvisioningFailed(ILogger logger, string tenantId, string error, double durationMs, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CompensationStarted tenant '{TenantId}' for backends [{Backends}] at {Timestamp:O}")]
    private static partial void LogCompensationStarted(ILogger logger, string tenantId, string backends, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Information, Message = "CompensationCompleted tenant '{TenantId}' in {DurationMs} ms at {Timestamp:O}")]
    private static partial void LogCompensationCompleted(ILogger logger, string tenantId, double durationMs, DateTimeOffset timestamp);

    [LoggerMessage(Level = LogLevel.Critical, Message = "CompensationFailed tenant '{TenantId}' for backends [{Backends}] at {Timestamp:O}: {Error}")]
    private static partial void LogCompensationFailed(ILogger logger, string tenantId, string backends, string error, DateTimeOffset timestamp);
}
