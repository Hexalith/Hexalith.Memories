// <copyright file="DaprEventIngestionWorkflowScheduler.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.EventStore;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

/// <summary>Package-owned default workflow scheduler that uses <see cref="DaprWorkflowClient"/> directly.
/// The workflow name is a stable contract string so the package can schedule the server-hosted
/// <c>IngestionWorkflow</c> without taking a compile-time reference on the Server assembly.</summary>
internal sealed class DaprEventIngestionWorkflowScheduler : IEventIngestionWorkflowScheduler
{
    internal const string DefaultWorkflowName = "IngestionWorkflow";

    private readonly DaprWorkflowClient _workflowClient;

    public DaprEventIngestionWorkflowScheduler(DaprWorkflowClient workflowClient)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        _workflowClient = workflowClient;
    }

    public async Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        return await _workflowClient
            .ScheduleNewWorkflowAsync(DefaultWorkflowName, instanceId, input)
            .ConfigureAwait(false);
    }
}
