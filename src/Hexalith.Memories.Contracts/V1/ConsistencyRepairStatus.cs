// <copyright file="ConsistencyRepairStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Client-facing status projection for <c>ConsistencyRepairWorkflow</c>.
/// </summary>
/// <param name="InstanceId">Workflow instance id.</param>
/// <param name="Status">Runtime status string (for example: <c>Running</c>, <c>Completed</c>, <c>Failed</c>).</param>
/// <param name="CreatedAt">Workflow creation timestamp.</param>
/// <param name="LastUpdatedAt">Last workflow state transition timestamp.</param>
/// <param name="Progress">Current progress snapshot, when the workflow has published custom status.</param>
/// <param name="Result">Typed repair result when the workflow is completed successfully.</param>
public sealed record ConsistencyRepairStatus(
    string InstanceId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    ConsistencyWorkflowProgress? Progress,
    ConsistencyRepairResult? Result);