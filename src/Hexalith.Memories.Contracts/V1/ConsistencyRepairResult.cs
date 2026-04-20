// <copyright file="ConsistencyRepairResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Aggregate result returned by <c>ConsistencyRepairWorkflow</c>.
/// </summary>
/// <param name="TenantId">The tenant repaired.</param>
/// <param name="TotalDiscrepancies">Number of discrepancies observed at workflow start.</param>
/// <param name="RepairedCount">Number of discrepancies that converged (<see cref="RepairActionRecord.Succeeded"/> true and not <c>Unrepairable</c>).</param>
/// <param name="UnrepairableCount">Number of units flagged <c>Unrepairable</c>.</param>
/// <param name="Actions">Per-unit action records; truncated to at most 10,000 entries on very large tenants.</param>
/// <param name="PassesExecuted">Number of repair passes actually executed (1-3).</param>
/// <param name="StartedAt">Repair start timestamp (UTC).</param>
/// <param name="CompletedAt">Repair completion timestamp (UTC).</param>
/// <param name="Duration">Total wall-clock duration.</param>
public sealed record ConsistencyRepairResult(
    string TenantId,
    int TotalDiscrepancies,
    int RepairedCount,
    int UnrepairableCount,
    IReadOnlyList<RepairActionRecord> Actions,
    int PassesExecuted,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration);
