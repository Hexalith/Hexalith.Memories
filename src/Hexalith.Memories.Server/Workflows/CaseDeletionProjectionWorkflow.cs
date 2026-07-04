// <copyright file="CaseDeletionProjectionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Cases;

/// <summary>Deletes case read-model projections after the delete command is accepted.</summary>
internal sealed class CaseDeletionProjectionWorkflow : Workflow<CaseDeletionProjectionInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowContext context, CaseDeletionProjectionInput input)
    {
        WorkflowTaskOptions retryOptions = CreateRetryOptions();
        await context.CallActivityAsync<bool>(
            nameof(MarkCaseDeletingActivity),
            new CaseProjectionCleanupInput(input.TenantId, input.CaseId),
            retryOptions);
        await context.CallActivityAsync<bool>(nameof(DeleteCaseProjectionActivity), input, retryOptions);
        return true;
    }

    private static WorkflowTaskOptions CreateRetryOptions()
        => new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));
}
