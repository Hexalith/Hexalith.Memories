// <copyright file="ScheduleAnnotationIngestionActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Cases;

using Hexalith.Memories.Server.Activities;

using System.Text;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

/// <summary>Schedules the ingestion workflow for an annotation memory unit.</summary>
internal sealed class ScheduleAnnotationIngestionActivity(
    DaprWorkflowClient workflowClient,
    IWorkflowPayloadStore payloadStore,
    IngestionWorkflowConfigurationCapture workflowConfigurationCapture,
    WorkflowTraceContextCapture workflowTraceContextCapture,
    SourceInventoryStore? inventory = null)
    : WorkflowTraceLinkedActivity<AnnotationProjectionInput, string>
{
    /// <inheritdoc/>
    protected override async Task<string> RunActivityAsync(WorkflowActivityContext context, AnnotationProjectionInput input)
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
            TraceContext = input.TraceContext ?? workflowTraceContextCapture.Capture(),
        };

        if (ingestionInput.WorkflowConfiguration is null)
        {
            ingestionInput = workflowConfigurationCapture.Apply(ingestionInput);
        }

        if (inventory is not null)
        {
            await inventory.AdmitAsync(input.AnnotationMemoryUnitId, ingestionInput, CancellationToken.None).ConfigureAwait(false);
            ingestionInput = ingestionInput with { SourceInventoryTracked = true };
        }
        try
        {
            ingestionInput = await IngestionPayloadClaimCheck
                .PrepareAsync(payloadStore, input.AnnotationMemoryUnitId, ingestionInput)
                .ConfigureAwait(false);

        }
        catch
        {
            if (inventory is not null)
            {
                await inventory.SetSchedulingAsync(ingestionInput.TenantId, ingestionInput.SourceUri, input.AnnotationMemoryUnitId,
                    "Unscheduled", CancellationToken.None).ConfigureAwait(false);
            }
            throw;
        }
        if (inventory is not null)
        {
            await inventory.SetSchedulingAsync(ingestionInput.TenantId, ingestionInput.SourceUri, input.AnnotationMemoryUnitId,
                "Scheduling", CancellationToken.None).ConfigureAwait(false);
        }
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            instanceId: input.AnnotationMemoryUnitId,
            input: ingestionInput).ConfigureAwait(false);
        return input.AnnotationMemoryUnitId;
    }
}
