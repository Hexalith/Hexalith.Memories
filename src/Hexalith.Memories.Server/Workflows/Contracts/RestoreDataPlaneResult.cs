// <copyright file="RestoreDataPlaneResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Result of <c>RestoreDataPlaneActivity</c>.</summary>
/// <param name="MemoryUnitIds">The ids of the memory units whose data plane was restored (drives the re-index fan-out).</param>
/// <param name="RestoredCaseCount">The number of cases restored.</param>
/// <param name="RestoredEdgeCount">The number of graph edges restored (excludes rebuilt CONTAINS edges).</param>
public sealed record RestoreDataPlaneResult(
    IReadOnlyList<string> MemoryUnitIds,
    int RestoredCaseCount,
    int RestoredEdgeCount);
