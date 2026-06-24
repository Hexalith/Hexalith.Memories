// <copyright file="FilterInspectionMapper.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Components.Filters;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Interaction;

/// <summary>
/// Pure, deterministic projection of active filters into inspectable chips, and side-channel-safe
/// resolution of why a filtered view rendered nothing.
/// </summary>
/// <remarks>
/// Story 17.3 (AC2) — never coerces an unknown filter token into a known label: unrecognized operators or
/// values become unavailable contract-boundary chips with an explanation. Empty-state resolution mirrors
/// the recovery mapper's precedence so an inaccessible scope never reveals whether matching evidence exists.
/// </remarks>
public static class FilterInspectionMapper
{
    /// <summary>Projects the active filter set into an inspectable view model.</summary>
    /// <param name="filters">The active filters, in display order.</param>
    /// <returns>The inspectable filter view model.</returns>
    public static FilterInspectionViewModel Map(IReadOnlyList<MemoriesFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        List<FilterChipView> chips = new(filters.Count);
        foreach (MemoriesFilter filter in filters)
        {
            chips.Add(MapChip(filter));
        }

        bool broaden = chips.Any(static c => c.Availability == FilterChipAvailability.Available && c.Effect == MemoriesFilterEffect.BroadensScope);
        bool exclude = chips.Any(static c => c.Availability == FilterChipAvailability.Available && c.Effect == MemoriesFilterEffect.ExcludesAxis);
        bool confidence = chips.Any(static c => c.Availability == FilterChipAvailability.Available && c.Effect == MemoriesFilterEffect.AffectsConfidence);
        bool unavailable = chips.Any(static c => c.Availability == FilterChipAvailability.Unavailable);

        IReadOnlyList<string> contractSources =
        [
            .. filters
                .SelectMany(static f => FilterAxisTraceability.For(f.Axis).ContractSources)
                .Distinct(StringComparer.Ordinal),
        ];

        return new FilterInspectionViewModel(chips, filters.Count, broaden, exclude, confidence, unavailable, contractSources);
    }

    /// <summary>Projects a single filter into an inspectable chip.</summary>
    /// <param name="filter">The active filter.</param>
    /// <returns>The sanitized chip view.</returns>
    public static FilterChipView MapChip(MemoriesFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        FilterAxisTrace trace = FilterAxisTraceability.For(filter.Axis);
        string value = EvidenceDisplay.SafeText(filter.ValueToken, "unavailable");

        // Unknown or future tokens are never coerced into a known label; they degrade to an unavailable
        // contract-boundary chip carrying an explanation.
        if (!filter.IsContractKnown)
        {
            return new FilterChipView(
                filter.Axis,
                trace.AxisLabelKey,
                value,
                MemoriesFilterEffect.None,
                FilterResourceKeys.Effect(MemoriesFilterEffect.None),
                InteractionSeverity.Warning,
                FilterChipAvailability.Unavailable,
                FilterResourceKeys.UnavailableReason);
        }

        return new FilterChipView(
            filter.Axis,
            trace.AxisLabelKey,
            value,
            filter.Effect,
            FilterResourceKeys.Effect(filter.Effect),
            SeverityForEffect(filter.Effect),
            FilterChipAvailability.Available,
            null);
    }

    /// <summary>Resolves why a filtered view rendered no results, distinguishing only what the contract allows.</summary>
    /// <param name="packet">The canonical Evidence Packet behind the view.</param>
    /// <param name="filtersActive">Whether any filter is currently active.</param>
    /// <returns>The side-channel-safe empty reason.</returns>
    public static FilteredEmptyReason ResolveEmptyReason(EvidencePacket packet, bool filtersActive)
    {
        ArgumentNullException.ThrowIfNull(packet);

        // Inaccessible scope outranks everything and never reveals whether matching evidence exists.
        if (EvidenceDisplay.IsRestrictiveScope(packet.Scope.IsolationStatus)
            || packet.State == EvidencePacketState.Unauthorized
            || packet.OmittedDetails.Reason == EvidencePacketOmissionReason.Authorization)
        {
            return FilteredEmptyReason.InaccessibleScope;
        }

        if (packet.Result.HasIndexedMemoryUnits == false)
        {
            return FilteredEmptyReason.NotIngested;
        }

        if (packet.Evidence.Degraded
            || packet.Evidence.AllEnabledAxesUnavailable == true
            || packet.OmittedDetails.Reason == EvidencePacketOmissionReason.BackendUnavailable)
        {
            return FilteredEmptyReason.DegradedBackend;
        }

        if (packet.State == EvidencePacketState.Stale)
        {
            return FilteredEmptyReason.StaleMemory;
        }

        if (packet.State == EvidencePacketState.Partial)
        {
            return FilteredEmptyReason.InsufficientEvidence;
        }

        return filtersActive ? FilteredEmptyReason.FilteredOut : FilteredEmptyReason.NoMatch;
    }

    private static InteractionSeverity SeverityForEffect(MemoriesFilterEffect effect)
        => effect switch
        {
            MemoriesFilterEffect.BroadensScope => InteractionSeverity.Warning,
            MemoriesFilterEffect.ExcludesAxis => InteractionSeverity.Warning,
            MemoriesFilterEffect.HidesStaleOrConflicting => InteractionSeverity.Caution,
            MemoriesFilterEffect.AffectsConfidence => InteractionSeverity.Caution,
            MemoriesFilterEffect.NarrowsScope => InteractionSeverity.Info,
            MemoriesFilterEffect.ChangesGraphDepth => InteractionSeverity.Info,
            _ => InteractionSeverity.None,
        };
}
