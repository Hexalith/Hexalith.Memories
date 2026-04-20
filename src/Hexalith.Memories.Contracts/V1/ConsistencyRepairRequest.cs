// <copyright file="ConsistencyRepairRequest.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Request payload for <c>POST /api/tenants/{tenantId}/consistency/repair</c>.
/// </summary>
/// <param name="TenantId">The tenant to repair.</param>
/// <param name="BatchSize">Optional per-batch size; must be in [10, 5000] when provided.</param>
/// <param name="IncludeUnrepairable">
/// When <c>false</c> (default), the repair workflow skips units with the
/// <c>Unrepairable</c> recommendation — those are reported but not attempted. When
/// <c>true</c>, the workflow still records a <c>RepairActionRecord</c> with
/// <c>Succeeded=false</c> for each unrepairable unit (useful for audit trails).
/// </param>
public sealed record ConsistencyRepairRequest(
    string TenantId,
    int? BatchSize = null,
    bool IncludeUnrepairable = false);
