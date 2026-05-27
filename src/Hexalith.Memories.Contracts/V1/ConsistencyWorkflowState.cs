// <copyright file="ConsistencyWorkflowState.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Client-facing projection of a consistency workflow's state. Isolates callers from
/// <see cref="Dapr.Workflow.WorkflowState"/> shape changes across DAPR SDK versions.
/// </summary>
/// <remarks>
/// The Dapr.Workflow SDK's <c>WorkflowState</c> does not expose the raw
/// <c>SerializedCustomStatus</c> / <c>SerializedOutput</c> strings as public members (they
/// live on a private <c>_metadata</c> field). Retrieving the typed result of a completed
/// consistency workflow requires a separate endpoint that calls
/// <c>WorkflowState.ReadOutputAs&lt;ConsistencyVerificationResult&gt;()</c> — deferred to a
/// follow-up story.
/// </remarks>
/// <param name="InstanceId">The workflow instance id.</param>
/// <param name="Status">Runtime status string (e.g. "Running", "Completed", "Failed").</param>
/// <param name="CreatedAt">When the workflow was scheduled.</param>
/// <param name="LastUpdatedAt">Last state transition timestamp.</param>
public sealed record ConsistencyWorkflowState(
    string InstanceId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);
