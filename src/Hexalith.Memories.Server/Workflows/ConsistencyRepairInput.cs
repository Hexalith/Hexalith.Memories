// <copyright file="ConsistencyRepairInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows;

/// <summary>Input for <c>ConsistencyRepairWorkflow</c>.</summary>
/// <param name="TenantId">The tenant to repair.</param>
/// <param name="BatchSize">Per-batch fan-out size (must be in [10, 5000]).</param>
/// <param name="IncludeUnrepairable">When <c>true</c>, record entries even for Unrepairable discrepancies.</param>
public sealed record ConsistencyRepairInput(string TenantId, int BatchSize = 500, bool IncludeUnrepairable = false);
