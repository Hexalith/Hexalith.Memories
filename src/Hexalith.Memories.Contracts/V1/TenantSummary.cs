// <copyright file="TenantSummary.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Enriched tenant projection returned by <c>GET /api/tenants</c> (Story 5.5 AC1 / FR41).
/// Field superset of <see cref="TenantInfo"/> (which remains the canonical minimal tenant record
/// used by workflows/actors — <em>do not modify <see cref="TenantInfo"/></em>).
/// <para>
/// Availability of each backend is conveyed by <see cref="IndexHealth.Unknown"/> on the
/// corresponding axis of <see cref="IndexStatus"/>; the matching count in
/// <see cref="IndexSizes"/> is <see langword="null"/> in that case (Amendment P — no separate
/// <c>TenantBackendAvailability</c> record).
/// </para>
/// </summary>
public sealed record TenantSummary
{
    /// <summary>The tenant identifier.</summary>
    public required string Id { get; init; }

    /// <summary>The tenant display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The tenant lifecycle status.</summary>
    public required TenantStatus Status { get; init; }

    /// <summary>When the tenant was registered.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Number of memory units stored for the tenant. <see langword="null"/> when Redis is unavailable
    /// (avoids reporting a misleading zero).
    /// </summary>
    public long? MemoryUnitCount { get; init; }

    /// <summary>Per-backend document / node counts.</summary>
    public required TenantIndexSizes IndexSizes { get; init; }

    /// <summary>Per-backend health; <see cref="IndexHealth.Unknown"/> encodes availability failure.</summary>
    public required TenantIndexStatus IndexStatus { get; init; }

    /// <summary>
    /// Whether the tenant's embedding configuration was marked as needing a reindex (Amendment S:
    /// advisory only in MVP — no automated reindex workflow; operator runbook is delete + re-ingest).
    /// </summary>
    public required bool ReindexRequired { get; init; }

    /// <summary>When the tenant last ingested a memory unit, or <see langword="null"/> for a tenant that has never ingested.</summary>
    public DateTimeOffset? LastActivityAt { get; init; }
}
