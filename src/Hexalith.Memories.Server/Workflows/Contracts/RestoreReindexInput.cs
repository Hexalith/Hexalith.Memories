// <copyright file="RestoreReindexInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>
/// Input to <c>RestoreReindexUnitActivity</c>. Deliberately carries only ids — the activity re-reads the
/// unit's content from the syntactic hash written by the data-plane activity, so the (potentially large)
/// content never flows through the workflow orchestrator.
/// </summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="MemoryUnitId">The memory unit to re-index (re-embed and write semantic vectors).</param>
public sealed record RestoreReindexInput(
    string TenantId,
    string MemoryUnitId);
