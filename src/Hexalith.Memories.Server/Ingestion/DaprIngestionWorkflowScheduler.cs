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
    private readonly IWorkflowPayloadStore _payloadStore;

    public DaprIngestionWorkflowScheduler(DaprWorkflowClient workflowClient, IWorkflowPayloadStore payloadStore)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(payloadStore);
        _workflowClient = workflowClient;
        _payloadStore = payloadStore;
    }

    public async Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        IngestionInput slimInput = await IngestionPayloadClaimCheck
            .PrepareAsync(_payloadStore, instanceId, input, cancellationToken)
            .ConfigureAwait(false);

        return await _workflowClient
            .ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput, null, cancellationToken)
            .ConfigureAwait(false);
    }
}
