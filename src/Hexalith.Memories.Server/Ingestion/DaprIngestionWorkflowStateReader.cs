// <copyright file="DaprIngestionWorkflowStateReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

using Microsoft.Extensions.Logging;

/// <summary>Dapr-backed ingestion workflow state reader.</summary>
internal sealed partial class DaprIngestionWorkflowStateReader(
    IDaprWorkflowClient workflowClient,
    IIngestionWorkflowInFlightRegistry inFlightRegistry,
    ILogger<DaprIngestionWorkflowStateReader> logger) : IIngestionWorkflowStateReader
{
    /// <inheritdoc />
    public async Task<WorkflowState?> GetWorkflowStateAsync(
        string instanceId,
        bool includeInputsAndOutputs,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        WorkflowState? state = await workflowClient
            .GetWorkflowStateAsync(instanceId, includeInputsAndOutputs, cancellationToken)
            .ConfigureAwait(false);

        if (state is null || !state.Exists || IsTerminalStatus(state.RuntimeStatus))
        {
            await TryRemoveTrackedInstanceAsync(instanceId, cancellationToken).ConfigureAwait(false);
        }

        return state;
    }

    internal static bool IsTerminalStatus(WorkflowRuntimeStatus status)
        => status == WorkflowRuntimeStatus.Completed
            || status == WorkflowRuntimeStatus.Failed
            || status == WorkflowRuntimeStatus.Canceled
            || status == WorkflowRuntimeStatus.Terminated;

    private async Task TryRemoveTrackedInstanceAsync(string instanceId, CancellationToken cancellationToken)
    {
        try
        {
            await inFlightRegistry.RemoveAsync(instanceId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogInFlightPruneFailed(logger, ex, instanceId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to prune tracked ingestion workflow {InstanceId} after a terminal or missing status read.")]
    private static partial void LogInFlightPruneFailed(ILogger logger, Exception exception, string instanceId);
}
