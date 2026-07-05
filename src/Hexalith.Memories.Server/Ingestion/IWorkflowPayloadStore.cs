// <copyright file="IWorkflowPayloadStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Stores and resolves claim-checked ingestion workflow payloads.</summary>
public interface IWorkflowPayloadStore
{
    /// <summary>Saves payload bytes and returns a tenant-scoped reference.</summary>
    Task<WorkflowPayloadReference> SaveAsync(
        string tenantId,
        string memoryUnitId,
        WorkflowPayloadKind kind,
        ReadOnlyMemory<byte> payload,
        string? idSuffix = null,
        CancellationToken cancellationToken = default);

    /// <summary>Reads and verifies payload bytes for the expected tenant, memory unit, and kind.</summary>
    Task<byte[]> ReadAsync(
        WorkflowPayloadReference reference,
        string tenantId,
        string memoryUnitId,
        WorkflowPayloadKind expectedKind,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the referenced payload if it exists.</summary>
    Task DeleteAsync(WorkflowPayloadReference reference, CancellationToken cancellationToken = default);
}
