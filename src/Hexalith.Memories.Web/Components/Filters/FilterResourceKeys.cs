// <copyright file="FilterResourceKeys.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

/// <summary>
/// Stable localization key conventions for the inspectable filter summary.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — axis labels, effect labels, unavailable-boundary explanations, and empty-state
/// reasons all resolve through keys defined here so filter-inspection text uses the same localization path
/// as the surrounding FrontComposer UI.
/// </remarks>
public static class FilterResourceKeys
{
    /// <summary>Accessible label for the filter summary region.</summary>
    public const string SummaryLabel = "Filter_Summary_Label";

    /// <summary>Accessible label for the active filter chip strip.</summary>
    public const string ChipsLabel = "Filter_Chips_Label";

    /// <summary>Shown when no filters are active.</summary>
    public const string NoFilters = "Filter_NoFilters";

    /// <summary>Label for the reset-all-filters control.</summary>
    public const string ResetLabel = "Filter_Reset_Label";

    /// <summary>Label for the per-chip remove control.</summary>
    public const string RemoveLabel = "Filter_Remove_Label";

    /// <summary>Explanation shown on an unavailable contract-boundary chip.</summary>
    public const string UnavailableReason = "Filter_Unavailable_Reason";

    /// <summary>Accessible label for the empty filtered-state region.</summary>
    public const string EmptyStateLabel = "Filter_EmptyState_Label";

    /// <summary>Label preceding the count of trust-affecting filters.</summary>
    public const string TrustNoticeLabel = "Filter_TrustNotice_Label";

    /// <summary>Builds the axis label key.</summary>
    /// <param name="axis">The filter axis.</param>
    /// <returns>The localization key.</returns>
    public static string Axis(MemoriesFilterAxis axis) => $"Filter_Axis_{axis}";

    /// <summary>Builds the effect label key.</summary>
    /// <param name="effect">The trust effect.</param>
    /// <returns>The localization key.</returns>
    public static string Effect(MemoriesFilterEffect effect) => $"Filter_Effect_{effect}";

    /// <summary>Builds the empty-reason label key.</summary>
    /// <param name="reason">The empty-state reason.</param>
    /// <returns>The localization key.</returns>
    public static string EmptyReason(FilteredEmptyReason reason) => $"Filter_Empty_{reason}";
}
