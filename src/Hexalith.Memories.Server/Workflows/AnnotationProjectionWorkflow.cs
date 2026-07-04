// <copyright file="AnnotationProjectionWorkflow.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Projects annotation intent into graph, ingestion workflow scheduling, and activity stream.</summary>
internal sealed class AnnotationProjectionWorkflow : Workflow<AnnotationProjectionInput, string>
{
    /// <inheritdoc/>
    public override async Task<string> RunAsync(WorkflowContext context, AnnotationProjectionInput input)
    {
        WorkflowTaskOptions retryOptions = CreateRetryOptions();
        WorkflowTaskOptions compensationOptions = CreateCompensationOptions();
        bool graphProjected = false;

        try
        {
            await context.CallActivityAsync<bool>(nameof(ProjectAnnotationGraphActivity), input, retryOptions);
            graphProjected = true;

            await context.CallChildWorkflowAsync<IngestionResult>(
                nameof(IngestionWorkflow),
                CreateIngestionInput(input),
                new ChildWorkflowTaskOptions(
                    input.AnnotationMemoryUnitId,
                    RetryPolicy: null,
                    TargetAppId: null,
                    HistoryPropagationScope.None));

            await context.CallActivityAsync<bool>(
                nameof(RecordCaseActivityActivity),
                new CaseActivityInput(
                    input.TenantId,
                    input.CaseId,
                    CaseActivityEventType.AnnotationCreated,
                    "system",
                    $"Annotation created on memory unit '{input.TargetMemoryUnitId}'",
                    input.AnnotationMemoryUnitId),
                retryOptions);

            return input.AnnotationMemoryUnitId;
        }
        catch (WorkflowTaskFailedException)
        {
            if (graphProjected)
            {
                await context.CallActivityAsync<bool>(
                    nameof(CleanupGraphActivity),
                    new CleanupInput(input.AnnotationMemoryUnitId, input.TenantId),
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

    private static IngestionInput CreateIngestionInput(AnnotationProjectionInput input)
        => new()
        {
            TenantId = input.TenantId,
            CaseId = input.CaseId,
            SourceUri = input.SourceUri,
            ContentBytes = Encoding.UTF8.GetBytes(input.Content),
            ContentType = "text/plain",
            SourceType = SourceType.Annotation,
            IngestedBy = input.IngestedBy,
            Metadata = input.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal),
            CausationId = input.TargetMemoryUnitId,
        };
}
