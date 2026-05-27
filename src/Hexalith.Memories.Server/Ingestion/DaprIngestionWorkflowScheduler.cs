// <copyright file="DaprIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Workflows;

/// <summary>DAPR-backed implementation of <see cref="IIngestionWorkflowScheduler"/>.</summary>
internal sealed class DaprIngestionWorkflowScheduler : IIngestionWorkflowScheduler
{
    private readonly DaprWorkflowClient _workflowClient;

    public DaprIngestionWorkflowScheduler(DaprWorkflowClient workflowClient)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        _workflowClient = workflowClient;
    }

    public Task<string> ScheduleAsync(string instanceId, IngestionInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        return _workflowClient.ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, input);
    }
}