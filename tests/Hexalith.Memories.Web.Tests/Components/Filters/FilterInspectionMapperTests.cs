// <copyright file="FilterInspectionMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Filters;

using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class FilterInspectionMapperTests
{
    [Fact]
    public void Map_AllStoryAxes_ProducesInspectableChipsAndTraceability()
    {
        IReadOnlyList<MemoriesFilter> filters =
        [
            new(MemoriesFilterAxis.RetrievalAxis, "semantic", MemoriesFilterEffect.ExcludesAxis, true),
            new(MemoriesFilterAxis.SourceType, "file", MemoriesFilterEffect.NarrowsScope, true),
            new(MemoriesFilterAxis.Freshness, "stale", MemoriesFilterEffect.HidesStaleOrConflicting, true),
            new(MemoriesFilterAxis.Confidence, "weak", MemoriesFilterEffect.AffectsConfidence, true),
            new(MemoriesFilterAxis.TimeRange, "last-7-days", MemoriesFilterEffect.NarrowsScope, true),
            new(MemoriesFilterAxis.Metadata, "owner:ops", MemoriesFilterEffect.NarrowsScope, true),
            new(MemoriesFilterAxis.GraphDepth, "2", MemoriesFilterEffect.ChangesGraphDepth, true),
            new(MemoriesFilterAxis.EvidenceState, "degraded", MemoriesFilterEffect.BroadensScope, true),
        ];

        FilterInspectionViewModel view = FilterInspectionMapper.Map(filters);

        view.ActiveCount.ShouldBe(8);
        view.HasAxisExclusion.ShouldBeTrue();
        view.HasConfidenceImpact.ShouldBeTrue();
        view.HasScopeBroadeningChange.ShouldBeTrue();
        view.Chips.Select(static c => c.Axis).ShouldBe(Enum.GetValues<MemoriesFilterAxis>());
        view.ContractSources.ShouldContain("EvidencePacket.Evidence.EvidenceStrength");
    }

    [Fact]
    public void Map_UnknownToken_RendersUnavailableBoundaryChip()
    {
        FilterInspectionViewModel view = FilterInspectionMapper.Map(
        [
            new(MemoriesFilterAxis.EvidenceState, "future-state", MemoriesFilterEffect.None, false),
        ]);

        view.HasUnavailableFilter.ShouldBeTrue();
        view.Chips[0].Availability.ShouldBe(FilterChipAvailability.Unavailable);
        view.Chips[0].UnavailableReasonKey.ShouldBe(FilterResourceKeys.UnavailableReason);
    }

    [Fact]
    public void ResolveEmptyReason_UnauthorizedScope_DoesNotRevealFilteredEvidence()
    {
        FilteredEmptyReason reason = FilterInspectionMapper.ResolveEmptyReason(
            EvidencePacketFixtures.UnauthorizedPacket(),
            filtersActive: true);

        reason.ShouldBe(FilteredEmptyReason.InaccessibleScope);
    }

    [Fact]
    public void ResolveEmptyReason_DistinguishesFilteredOutFromNoMatch()
    {
        FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.EmptyPacket(), filtersActive: true)
            .ShouldBe(FilteredEmptyReason.FilteredOut);
        FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.EmptyPacket(), filtersActive: false)
            .ShouldBe(FilteredEmptyReason.NoMatch);
    }
}
