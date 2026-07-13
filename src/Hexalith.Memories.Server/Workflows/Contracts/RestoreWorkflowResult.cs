// <copyright file="RestoreWorkflowResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Terminal result of the Story 26.2 <c>RestoreWorkflow</c>.</summary>
/// <param name="RestoredMemoryUnits">The number of memory units restored (data plane written + re-indexed).</param>
/// <param name="RestoredCases">The number of cases restored.</param>
/// <param name="RestoredEdges">The number of graph edges restored (excludes rebuilt CONTAINS edges).</param>
/// <param name="SkippedRecords">The number of corrupt records skipped best-effort during restore (zero for a faithful export).</param>
public sealed record RestoreWorkflowResult(
    int RestoredMemoryUnits,
    int RestoredCases,
    int RestoredEdges,
    int SkippedRecords);
