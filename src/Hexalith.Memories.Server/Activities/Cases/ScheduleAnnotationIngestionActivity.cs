// <copyright file="ScheduleAnnotationIngestionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

/// <summary>Schedules the ingestion workflow for an annotation memory unit.</summary>
internal sealed class ScheduleAnnotationIngestionActivity(
    DaprWorkflowClient workflowClient,
    IWorkflowPayloadStore payloadStore,
    IngestionWorkflowConfigurationCapture workflowConfigurationCapture)
    : WorkflowActivity<AnnotationProjectionInput, string>
{
    /// <inheritdoc/>
    public override async Task<string> RunAsync(WorkflowActivityContext context, AnnotationProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var ingestionInput = new IngestionInput
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
            WorkflowConfiguration = input.WorkflowConfiguration,
        };

        if (ingestionInput.WorkflowConfiguration is null)
        {
            ingestionInput = workflowConfigurationCapture.Apply(ingestionInput);
        }

        ingestionInput = await IngestionPayloadClaimCheck
            .PrepareAsync(payloadStore, input.AnnotationMemoryUnitId, ingestionInput)
            .ConfigureAwait(false);

        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            instanceId: input.AnnotationMemoryUnitId,
            input: ingestionInput).ConfigureAwait(false);
        return input.AnnotationMemoryUnitId;
    }
}
