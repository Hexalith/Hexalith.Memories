// <copyright file="RestoreStatusResponse.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Projected status of a restore workflow (Story 26.2), returned by
/// <c>GET /api/v1/tenants/{tenantId}/restore/{instanceId}</c>.
/// </summary>
/// <param name="InstanceId">The restore workflow instance identifier.</param>
/// <param name="TenantId">The target tenant of the restore.</param>
/// <param name="Status">The runtime/custom status of the restore (for example <c>restoring-data-plane</c>, <c>reindexing</c>, <c>completed</c>, <c>failed</c>).</param>
/// <param name="CreatedAt">When the restore workflow was created; <see langword="null"/> when unknown.</param>
/// <param name="LastUpdatedAt">When the restore workflow last changed state; <see langword="null"/> when unknown.</param>
/// <param name="RestoredMemoryUnits">Count of restored memory units once the workflow completes; <see langword="null"/> while running.</param>
/// <param name="RestoredCases">Count of restored cases once the workflow completes; <see langword="null"/> while running.</param>
/// <param name="RestoredEdges">Count of restored graph edges once the workflow completes; <see langword="null"/> while running.</param>
public sealed record RestoreStatusResponse(
    string InstanceId,
    string TenantId,
    string Status,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastUpdatedAt,
    int? RestoredMemoryUnits,
    int? RestoredCases,
    int? RestoredEdges);
