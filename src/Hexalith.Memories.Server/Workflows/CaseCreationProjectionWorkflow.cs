// <copyright file="CaseCreationProjectionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Projects a case-created EventStore event into Redis, FalkorDB, and activity read models.</summary>
internal sealed class CaseCreationProjectionWorkflow : Workflow<ProjectCaseCreatedInput, bool>
{
    /// <inheritdoc/>
    public override async Task<bool> RunAsync(WorkflowContext context, ProjectCaseCreatedInput input)
    {
        WorkflowTaskOptions retryOptions = CreateRetryOptions();
        WorkflowTaskOptions compensationOptions = CreateCompensationOptions();
        List<string> completed = [];

        try
        {
            await context.CallActivityAsync<bool>(nameof(ProjectCaseHashActivity), input, retryOptions);
            completed.Add(nameof(ProjectCaseHashActivity));

            await context.CallActivityAsync<bool>(nameof(ProjectCaseGraphActivity), input, retryOptions);
            completed.Add(nameof(ProjectCaseGraphActivity));

            await context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                new CaseActivityInput(
                    input.TenantId,
                    input.CaseId,
                    CaseActivityEventType.CaseCreated,
                    "system",
                    $"Case '{input.Name}' created",
                    MemoryUnitId: null),
                retryOptions);

            return true;
        }
        catch (WorkflowTaskFailedException)
        {
            if (completed.Count > 0)
            {
                await context.CallActivityAsync<bool>(
                    nameof(CleanupCaseProjectionActivity),
                    new CaseProjectionCleanupInput(input.TenantId, input.CaseId),
                    compensationOptions);
            }

            throw;
        }
    }

    private static WorkflowTaskOptions CreateRetryOptions()
        => new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));

    private static WorkflowTaskOptions CreateCompensationOptions()
        => new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 3,
            firstRetryInterval: TimeSpan.FromSeconds(1),
            maxRetryInterval: TimeSpan.FromSeconds(30)));
}
