// <copyright file="RepairUnitInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Contracts.V1;

/// <summary>
/// Input for <c>RepairUnitActivity</c>. Carries the stale recommendation (from the
/// verification pass) so the activity can log intent; the activity always re-verifies
/// the unit before acting (Risk #1 — double-check via
/// <c>ConsistencyInspectionService</c>).
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="MemoryUnitId">The memory unit to repair.</param>
/// <param name="Recommendation">The stale recommendation produced by the verification pass.</param>
/// <param name="IncludeUnrepairable">
/// When <c>false</c>, units with an <c>Unrepairable</c> recommendation return a
/// <see cref="RepairActionRecord"/> with <c>Applied=Unrepairable</c> and
/// <c>Succeeded=false</c> but the activity does not attempt any writes.
/// </param>
public sealed record RepairUnitInput(
    string TenantId,
    string MemoryUnitId,
    ConsistencyRepairRecommendation Recommendation,
    bool IncludeUnrepairable = false);
