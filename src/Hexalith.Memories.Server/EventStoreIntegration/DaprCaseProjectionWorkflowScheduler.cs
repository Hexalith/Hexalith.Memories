// <copyright file="DaprCaseProjectionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using Dapr.Workflow;

/// <summary>Dapr Workflow implementation of <see cref="ICaseProjectionWorkflowScheduler"/>.</summary>
internal sealed class DaprCaseProjectionWorkflowScheduler(DaprWorkflowClient workflowClient) : ICaseProjectionWorkflowScheduler
{
    /// <inheritdoc/>
    public Task<string> ScheduleAsync(
        string workflowName,
        string instanceId,
        object input,
        CancellationToken cancellationToken)
        => workflowClient.ScheduleNewWorkflowAsync(workflowName, instanceId, input, null, cancellationToken);
}
