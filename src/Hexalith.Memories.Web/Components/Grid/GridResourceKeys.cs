// <copyright file="GridResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Grid;

/// <summary>
/// Stable localization key conventions for the Story 17.3 evidence data grid.
/// </summary>
/// <remarks>
/// Story 17.3 (AC6) — grid headers, the row-action label, the collapsed-column affordance, and the empty
/// state resolve through keys defined here so the grid uses the same localization path as the surrounding
/// FrontComposer UI.
/// </remarks>
public static class GridResourceKeys
{
    /// <summary>Accessible label for the grid region.</summary>
    public const string GridLabel = "Grid_Label";

    /// <summary>Header for the rank column.</summary>
    public const string RankColumn = "Grid_Col_Rank";

    /// <summary>Header for the source column.</summary>
    public const string SourceColumn = "Grid_Col_Source";

    /// <summary>Header for the case column (trust-critical).</summary>
    public const string CaseColumn = "Grid_Col_Case";

    /// <summary>Header for the confidence column (trust-critical).</summary>
    public const string ConfidenceColumn = "Grid_Col_Confidence";

    /// <summary>Header for the annotations column.</summary>
    public const string AnnotationsColumn = "Grid_Col_Annotations";

    /// <summary>Header for the row-actions column.</summary>
    public const string ActionsColumn = "Grid_Col_Actions";

    /// <summary>Label for the per-row inspect action.</summary>
    public const string RowActionLabel = "Grid_RowAction_Label";

    /// <summary>Label preceding the collapsed-column values in compact mode.</summary>
    public const string MoreColumnsLabel = "Grid_MoreColumns_Label";

    /// <summary>Accessible label for the grid empty state.</summary>
    public const string EmptyLabel = "Grid_Empty_Label";
}
