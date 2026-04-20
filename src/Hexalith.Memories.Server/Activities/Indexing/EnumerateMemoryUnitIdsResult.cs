// <copyright file="EnumerateMemoryUnitIdsResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

/// <summary>
/// Result of <c>EnumerateMemoryUnitIdsActivity</c>. The activity returns the full
/// de-duplicated union of memory unit IDs across the three backends, up to
/// <c>MaxUnits</c>. The verifying workflow slices this list into batches.
/// </summary>
/// <param name="MemoryUnitIds">Sorted memory unit identifiers (ascending).</param>
/// <param name="TotalUnionCount">
/// The un-capped union size. When this exceeds <c>MaxUnits</c>, the returned list is
/// truncated and <see cref="Truncated"/> is <c>true</c>.
/// </param>
/// <param name="Truncated">
/// <c>true</c> when the returned list was truncated to <c>MaxUnits</c>.
/// </param>
public sealed record EnumerateMemoryUnitIdsResult(
    IReadOnlyList<string> MemoryUnitIds,
    long TotalUnionCount,
    bool Truncated);
