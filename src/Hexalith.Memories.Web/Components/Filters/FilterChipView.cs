// <copyright file="FilterChipView.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// A sanitized, inspectable projection of a single active filter for the filter summary.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — every chip carries its axis, sanitized value, trust effect, severity, and
/// availability so the user can see what each filter is and how it changes the meaning of the result set.
/// All labels are localization keys; the value is pre-sanitized.
/// </remarks>
/// <param name="Axis">The filter axis.</param>
/// <param name="AxisLabelKey">Localization key for the axis label.</param>
/// <param name="ValueText">The sanitized filter value text.</param>
/// <param name="Effect">The trust effect the filter has.</param>
/// <param name="EffectLabelKey">Localization key for the effect label.</param>
/// <param name="Severity">The display severity tier for the effect.</param>
/// <param name="Availability">Whether the filter is usable or an unavailable contract-boundary state.</param>
/// <param name="UnavailableReasonKey">Localization key for the unavailable explanation, or null when available.</param>
public sealed record FilterChipView(
    MemoriesFilterAxis Axis,
    string AxisLabelKey,
    string ValueText,
    MemoriesFilterEffect Effect,
    string EffectLabelKey,
    InteractionSeverity Severity,
    FilterChipAvailability Availability,
    string? UnavailableReasonKey);
