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
/// <param name="Status">The runtime/custom status of the restore (for example <c>restoring-data-plane</c>, <c>reindexing</c>, <c>Completed</c>, <c>Failed</c>).</param>
/// <param name="CreatedAt">When the restore workflow was created; <see langword="null"/> when unknown.</param>
/// <param name="LastUpdatedAt">When the restore workflow last changed state; <see langword="null"/> when unknown.</param>
/// <param name="RestoredMemoryUnits">Count of restored memory units once the workflow completes; <see langword="null"/> while running.</param>
/// <param name="RestoredCases">Count of restored cases once the workflow completes; <see langword="null"/> while running.</param>
/// <param name="RestoredEdges">Count of restored graph edges once the workflow completes; <see langword="null"/> while running.</param>
/// <param name="SkippedRecords">Count of corrupt records skipped best-effort once the workflow completes; <see langword="null"/> while running. A non-zero value means the export contained invalid records (for example an out-of-range edge confidence or a blank case id).</param>
public sealed record RestoreStatusResponse(
    string InstanceId,
    string TenantId,
    string Status,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastUpdatedAt,
    int? RestoredMemoryUnits,
    int? RestoredCases,
    int? RestoredEdges,
    int? SkippedRecords)
{
    /// <summary>Gets a stable, support-safe failure code for a terminal non-success state.</summary>
    public string? FailureCode { get; init; }

    /// <summary>Gets a sanitized failure summary that never contains raw workflow exception details.</summary>
    public string? FailureMessage { get; init; }

    /// <summary>Gets operator-safe recovery guidance for a terminal non-success state.</summary>
    public string? FailureSuggestion { get; init; }
}
