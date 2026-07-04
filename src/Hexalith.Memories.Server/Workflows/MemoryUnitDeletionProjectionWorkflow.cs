// <copyright file="MemoryUnitDeletionProjectionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Deletes memory-unit read-model projections after the delete command is accepted.</summary>
internal sealed class MemoryUnitDeletionProjectionWorkflow : Workflow<MemoryUnitDeletionProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowContext context, MemoryUnitDeletionProjectionInput input)
    {
        WorkflowTaskOptions retryOptions = CreateRetryOptions();

        await context.CallActivityAsync<bool>(nameof(DeleteMemoryUnitProjectionActivity), input, retryOptions);
        await context.CallActivityAsync<bool>(
            nameof(RecordCaseActivityActivity),
            new CaseActivityInput(
                input.TenantId,
                input.CaseId,
                CaseActivityEventType.MemoryUnitDeleted,
                "system",
                $"Memory unit '{input.MemoryUnitId}' deleted (with {input.AnnotationMemoryUnitIds.Count} annotation(s))",
                input.MemoryUnitId),
            retryOptions);

        return true;
    }

    private static WorkflowTaskOptions CreateRetryOptions()
        => new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));
}
