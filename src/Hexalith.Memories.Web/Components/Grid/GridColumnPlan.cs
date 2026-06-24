// <copyright file="GridColumnPlan.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Grid;

/// <summary>
/// The result of planning which data-grid columns stay visible and which collapse in a compact layout.
/// </summary>
/// <remarks>
/// Story 17.3 (AC6) — <see cref="Visible"/> always includes every trust-critical column;
/// <see cref="Collapsible"/> only ever contains non-trust-critical columns, which remain reachable through
/// an expand affordance rather than horizontal-scroll-only access.
/// </remarks>
/// <param name="Visible">Columns rendered as primary, in original declaration order.</param>
/// <param name="Collapsible">Columns moved to an expand affordance, in original declaration order.</param>
public sealed record GridColumnPlan(
    IReadOnlyList<MemoriesGridColumn> Visible,
    IReadOnlyList<MemoriesGridColumn> Collapsible);
