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
    private readonly IngestionWorkflowConfigurationCapture _workflowConfigurationCapture;

    public DaprIngestionWorkflowScheduler(
        DaprWorkflowClient workflowClient,
        IWorkflowPayloadStore payloadStore,
        IngestionWorkflowConfigurationCapture workflowConfigurationCapture)
    {
        ArgumentNullException.ThrowIfNull(workflowClient);
        ArgumentNullException.ThrowIfNull(payloadStore);
        ArgumentNullException.ThrowIfNull(workflowConfigurationCapture);
        _workflowClient = workflowClient;
        _payloadStore = payloadStore;
        _workflowConfigurationCapture = workflowConfigurationCapture;
    }

    public async Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(input);

        IngestionInput slimInput = await PrepareInputAsync(
                _payloadStore,
                instanceId,
                input,
                _workflowConfigurationCapture,
                cancellationToken)
            .ConfigureAwait(false);

        return await _workflowClient
            .ScheduleNewWorkflowAsync(nameof(IngestionWorkflow), instanceId, slimInput, null, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static Task<IngestionInput> PrepareInputAsync(
        IWorkflowPayloadStore payloadStore,
        string instanceId,
        IngestionInput input,
        IngestionWorkflowConfigurationCapture workflowConfigurationCapture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflowConfigurationCapture);
        IngestionInput configuredInput = workflowConfigurationCapture.Apply(input);
        return IngestionPayloadClaimCheck.PrepareAsync(payloadStore, instanceId, configuredInput, cancellationToken);
    }
}
