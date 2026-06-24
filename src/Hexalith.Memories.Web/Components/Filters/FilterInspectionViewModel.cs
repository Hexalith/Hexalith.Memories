// <copyright file="FilterInspectionViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// The inspectable projection of the full active filter set produced by <see cref="FilterInspectionMapper"/>.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — surfaces the per-filter chips plus aggregate flags so the summary can indicate when
/// the active filter set broadens scope, excludes an axis, or affects confidence interpretation.
/// </remarks>
/// <param name="Chips">The per-filter inspectable chips, in supplied order.</param>
/// <param name="ActiveCount">The number of active filters.</param>
/// <param name="HasScopeBroadeningChange">Whether any active filter broadens scope.</param>
/// <param name="HasAxisExclusion">Whether any active filter excludes a retrieval axis.</param>
/// <param name="HasConfidenceImpact">Whether any active filter affects confidence interpretation.</param>
/// <param name="HasUnavailableFilter">Whether any active filter is an unavailable contract-boundary state.</param>
/// <param name="ContractSources">The distinct named contract fields the active axes map to, for traceability.</param>
public sealed record FilterInspectionViewModel(
    IReadOnlyList<FilterChipView> Chips,
    int ActiveCount,
    bool HasScopeBroadeningChange,
    bool HasAxisExclusion,
    bool HasConfidenceImpact,
    bool HasUnavailableFilter,
    IReadOnlyList<string> ContractSources);
