// <copyright file="ConsistencyInspectionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Result of a synchronous per-unit inspection via
/// <c>GET /api/tenants/{tenantId}/consistency/inspect/{memoryUnitId}</c>.
/// Returned only when the unit is present in at least one backend; otherwise
/// the endpoint returns 404 with an <see cref="ErrorResponse"/>.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit identifier (ULID).</param>
/// <param name="SyntacticPresent">Whether the unit is present in RediSearch.</param>
/// <param name="SemanticPresent">Whether the unit is present in Redis Vector.</param>
/// <param name="GraphPresent">Whether the unit is present in FalkorDB.</param>
/// <param name="SyntacticDetail">Detail from the <c>{tenantId}:mu:{id}</c> hash; <c>null</c> if absent.</param>
/// <param name="SemanticDetail">Detail from the <c>{tenantId}:vec:{id}</c> hash; <c>null</c> if absent.</param>
/// <param name="GraphDetail">Detail from the FalkorDB node; <c>null</c> if absent.</param>
/// <param name="Recommendation">Repair recommendation (<c>NoOp</c> when fully consistent).</param>
/// <param name="CheckedAt">Timestamp of the probe (UTC).</param>
public sealed record ConsistencyInspectionResult(
    string TenantId,
    string MemoryUnitId,
    bool SyntacticPresent,
    bool SemanticPresent,
    bool GraphPresent,
    ConsistencySyntacticDetail? SyntacticDetail,
    ConsistencySemanticDetail? SemanticDetail,
    ConsistencyGraphDetail? GraphDetail,
    ConsistencyRepairRecommendation Recommendation,
    DateTimeOffset CheckedAt);
