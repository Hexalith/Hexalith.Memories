// <copyright file="RepairActionRecord.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Outcome of a single <c>RepairUnitActivity</c> invocation. <see cref="BeforeState"/> and
/// <see cref="AfterState"/> capture the presence booleans as short strings (<c>"present"</c>
/// / <c>"absent"</c>) plus any short error code. The dictionaries are intentionally small so
/// that a workflow result with thousands of actions still fits the DAPR state-store budget.
/// </summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="Applied">The recommendation the activity actually dispatched.</param>
/// <param name="Succeeded">Whether the action completed without throwing.</param>
/// <param name="FailureReason">Short explanation when <see cref="Succeeded"/> is <c>false</c>.</param>
/// <param name="BeforeState">Pre-action presence snapshot (e.g. <c>{"syntactic":"present",...}</c>).</param>
/// <param name="AfterState">Post-action presence snapshot.</param>
public sealed record RepairActionRecord(
    string MemoryUnitId,
    ConsistencyRepairRecommendation Applied,
    bool Succeeded,
    string? FailureReason,
    IReadOnlyDictionary<string, string> BeforeState,
    IReadOnlyDictionary<string, string> AfterState);
