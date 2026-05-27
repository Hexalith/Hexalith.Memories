// <copyright file="FailedUnitsPage.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>A page of failed memory units sorted by FailedAt DESC (Story 6.3 FR11).</summary>
/// <remarks>
/// <c>TotalCount</c> represents currently-unresolved failed units (decreases on re-ingestion or delete) —
/// distinct from <see cref="CaseStatusDetail.FailedCount"/> (historical activity-stream count, monotonic).
/// </remarks>
public sealed record FailedUnitsPage(
    IReadOnlyList<FailedUnitSummary> Units,
    int TotalCount,
    int Limit,
    int Offset);
