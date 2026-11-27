// <copyright file="DaprCaseProjectionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Dapr.Workflow;

using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

/// <summary>Dapr Workflow implementation of <see cref="ICaseProjectionWorkflowScheduler"/>.</summary>
internal sealed class DaprCaseProjectionWorkflowScheduler(
    IDaprWorkflowClient workflowClient,
    IIngestionWorkflowInFlightRegistry inFlightRegistry,
    TimeProvider timeProvider) : ICaseProjectionWorkflowScheduler
{
    /// <inheritdoc/>
    public async Task<string> ScheduleAsync(
        string workflowName,
        string instanceId,
        object input,
        CancellationToken cancellationToken)
    {
        if (workflowName == nameof(AnnotationProjectionWorkflow))
        {
            if (input is not AnnotationProjectionInput annotation
                || instanceId != AnnotationProjectionWorkflow.GetInstanceId(annotation.AnnotationMemoryUnitId))
            {
                throw new ArgumentException("Annotation projection requires its canonical parent and child instance identifiers.", nameof(instanceId));
            }

            // Track before the sidecar call. The parent can launch its child immediately, and a
            // failed acknowledgement does not prove the sidecar rejected the schedule.
            await inFlightRegistry.TrackAsync(new(annotation.TenantId, instanceId, timeProvider.GetUtcNow()), cancellationToken)
                .ConfigureAwait(false);
        }

        return await workflowClient.ScheduleNewWorkflowAsync(workflowName, instanceId, input, null, cancellationToken)
            .ConfigureAwait(false);
    }
}
