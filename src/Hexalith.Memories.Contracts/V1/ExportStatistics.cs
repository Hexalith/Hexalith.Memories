// <copyright file="ExportStatistics.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Aggregate counts emitted as the final top-level field of an export envelope (Story 8.3).
/// Values are tallied during streaming and reflect what was actually serialized.
/// </summary>
/// <param name="MemoryUnitCount">Number of memory units written to <c>memoryUnits[]</c>.</param>
/// <param name="EdgeCount">Number of unique graph edges written to <c>edges[]</c>.</param>
/// <param name="CaseCount">Number of cases covered: <c>1</c> for case-scope, <c>N</c> for tenant-scope.</param>
public sealed record ExportStatistics(
    int MemoryUnitCount,
    int EdgeCount,
    int CaseCount);
