// <copyright file="IngestionWorkflowStatus.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Safe public status projection for a single ingestion workflow.</summary>
/// <param name="InstanceId">The workflow instance id.</param>
/// <param name="TenantId">The tenant that owns the workflow input.</param>
/// <param name="CaseId">The case that owns the workflow input.</param>
/// <param name="RuntimeStatus">The Dapr workflow runtime status name.</param>
/// <param name="CreatedAt">The workflow creation timestamp.</param>
/// <param name="LastUpdatedAt">The latest workflow state transition timestamp.</param>
/// <param name="MemoryUnitId">The indexed memory unit id when a completed output can be projected safely.</param>
/// <param name="MemoryUnitStatus">The completed memory unit status when output can be projected safely.</param>
/// <param name="FailureSummary">A sanitized failure summary when the workflow or output projection failed.</param>
public sealed record IngestionWorkflowStatus(
    string InstanceId,
    string TenantId,
    string CaseId,
    string RuntimeStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt,
    string? MemoryUnitId,
    MemoryUnitStatus? MemoryUnitStatus,
    string? FailureSummary);
