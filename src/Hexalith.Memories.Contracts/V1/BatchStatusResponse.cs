// <copyright file="BatchStatusResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Aggregated status response for GET /api/v1/ingest/batches/{batchId}.</summary>
/// <param name="BatchId">The batch identifier.</param>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="CaseId">The owning case.</param>
/// <param name="Discovered">Total files the enumerator saw when the batch was scheduled.</param>
/// <param name="Enqueued">Workflow instances actually scheduled.</param>
/// <param name="Skipped">Count of files skipped at discovery (for summary only).</param>
/// <param name="Counts">Aggregate counts by current stage/status.</param>
/// <param name="Instances">Per-instance detail.</param>
public sealed record BatchStatusResponse(
    string BatchId,
    string TenantId,
    string CaseId,
    int Discovered,
    int Enqueued,
    int Skipped,
    BatchStatusCounts Counts,
    IReadOnlyList<BatchInstanceStatus> Instances);
