// <copyright file="IIngestionWorkflowStateReader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Dapr.Workflow;

/// <summary>Reads Dapr ingestion workflow state for status endpoints.</summary>
internal interface IIngestionWorkflowStateReader
{
    /// <summary>Gets workflow state for an ingestion instance.</summary>
    /// <param name="instanceId">The workflow instance id.</param>
    /// <param name="includeInputsAndOutputs">Whether Dapr should include serialized input, output, and custom status.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The workflow state, or <c>null</c> when the state does not exist or cannot be read.</returns>
    Task<WorkflowState?> GetWorkflowStateAsync(
        string instanceId,
        bool includeInputsAndOutputs,
        CancellationToken cancellationToken);
}
