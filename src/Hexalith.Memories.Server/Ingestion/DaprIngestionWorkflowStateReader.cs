// <copyright file="DaprIngestionWorkflowStateReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

/// <summary>Dapr-backed ingestion workflow state reader.</summary>
internal sealed class DaprIngestionWorkflowStateReader(DaprWorkflowClient workflowClient) : IIngestionWorkflowStateReader
{
    /// <inheritdoc />
    public async Task<WorkflowState?> GetWorkflowStateAsync(
        string instanceId,
        bool includeInputsAndOutputs,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        return await workflowClient
            .GetWorkflowStateAsync(instanceId, includeInputsAndOutputs, cancellationToken)
            .ConfigureAwait(false);
    }
}
