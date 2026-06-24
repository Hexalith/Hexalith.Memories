// <copyright file="MemoriesGridColumn.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Grid;

/// <summary>
/// A declared data-grid column with a stable key, a localized header, and whether it is trust-critical.
/// </summary>
/// <remarks>
/// Story 17.3 (AC6) — trust-critical columns are never collapsed behind horizontal-scroll-only access by
/// <see cref="CompactGridColumnPlanner"/>.
/// </remarks>
/// <param name="ColumnKey">Stable machine identifier for the column.</param>
/// <param name="HeaderKey">Localization key for the column header.</param>
/// <param name="IsTrustCritical">Whether the column carries trust-critical information.</param>
public sealed record MemoriesGridColumn(string ColumnKey, string HeaderKey, bool IsTrustCritical);
