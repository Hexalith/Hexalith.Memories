// <copyright file="CompactGridColumnPlanner.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Grid;

/// <summary>
/// Pure, deterministic planner that decides which data-grid columns stay visible and which collapse when a
/// grid renders in a compact layout.
/// </summary>
/// <remarks>
/// Story 17.3 (AC6) — trust-critical columns are always placed in the visible set and never collapsed, even
/// when their count exceeds the requested cap. Non-trust-critical columns fill the remaining visible slots
/// in declaration order; the rest become collapsible (reachable through an expand affordance, never
/// horizontal-scroll-only).
/// </remarks>
public static class CompactGridColumnPlanner
{
    /// <summary>Plans the visible and collapsible column sets.</summary>
    /// <param name="columns">The declared columns, in display order.</param>
    /// <param name="maxVisible">The target maximum number of visible columns in compact mode.</param>
    /// <param name="compact">Whether the grid is rendering in a compact layout.</param>
    /// <returns>The column plan.</returns>
    public static GridColumnPlan Plan(IReadOnlyList<MemoriesGridColumn> columns, int maxVisible, bool compact)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentOutOfRangeException.ThrowIfNegative(maxVisible);

        // A non-compact grid shows every column; nothing collapses.
        if (!compact)
        {
            return new GridColumnPlan(columns, []);
        }

        List<MemoriesGridColumn> visible = [];
        List<MemoriesGridColumn> collapsible = [];

        // Trust-critical columns are always visible — trust beats the cap.
        int trustCriticalCount = columns.Count(static c => c.IsTrustCritical);
        int remaining = Math.Max(0, maxVisible - trustCriticalCount);

        foreach (MemoriesGridColumn column in columns)
        {
            if (column.IsTrustCritical)
            {
                visible.Add(column);
            }
            else if (remaining > 0)
            {
                visible.Add(column);
                remaining--;
            }
            else
            {
                collapsible.Add(column);
            }
        }

        return new GridColumnPlan(visible, collapsible);
    }
}
