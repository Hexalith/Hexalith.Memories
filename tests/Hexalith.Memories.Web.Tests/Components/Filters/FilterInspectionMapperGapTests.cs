// <copyright file="FilterInspectionMapperGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Filters;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="FilterInspectionMapper"/>: the empty-reason branches the existing suite
/// does not exercise (not-ingested, degraded backend, stale memory, insufficient evidence, unknown scope),
/// per-effect chip severity, filter-value sanitization, and defensive null guards.
/// </summary>
public sealed class FilterInspectionMapperGapTests
{
    [Fact]
    public void Map_NullFilters_Throws()
        => Should.Throw<ArgumentNullException>(() => FilterInspectionMapper.Map(null!));

    [Fact]
    public void MapChip_NullFilter_Throws()
        => Should.Throw<ArgumentNullException>(() => FilterInspectionMapper.MapChip(null!));

    [Fact]
    public void ResolveEmptyReason_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => FilterInspectionMapper.ResolveEmptyReason(null!, filtersActive: true));

    [Fact]
    public void Map_NoFilters_HasNoActiveChipsOrAggregateFlags()
    {
        FilterInspectionViewModel view = FilterInspectionMapper.Map([]);

        view.ActiveCount.ShouldBe(0);
        view.Chips.ShouldBeEmpty();
        view.HasUnavailableFilter.ShouldBeFalse();
        view.HasAxisExclusion.ShouldBeFalse();
        view.HasConfidenceImpact.ShouldBeFalse();
        view.HasScopeBroadeningChange.ShouldBeFalse();
        view.ContractSources.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveEmptyReason_NotIngested_WhenNoIndexedMemoryUnits()
    {
        EvidencePacket packet = EvidencePacketFixtures.EmptyPacket();
        packet = packet with { Result = packet.Result with { HasIndexedMemoryUnits = false } };

        FilterInspectionMapper.ResolveEmptyReason(packet, filtersActive: true)
            .ShouldBe(FilteredEmptyReason.NotIngested);
    }

    [Fact]
    public void ResolveEmptyReason_DegradedBackend_WhenEvidenceDegraded()
        => FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.DegradedPacket(), filtersActive: true)
            .ShouldBe(FilteredEmptyReason.DegradedBackend);

    [Fact]
    public void ResolveEmptyReason_StaleMemory_WhenPacketStale()
        => FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.StalePacket(), filtersActive: true)
            .ShouldBe(FilteredEmptyReason.StaleMemory);

    [Fact]
    public void ResolveEmptyReason_InsufficientEvidence_WhenPacketPartial()
        => FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.PartialPacket(), filtersActive: true)
            .ShouldBe(FilteredEmptyReason.InsufficientEvidence);

    [Fact]
    public void ResolveEmptyReason_UnknownIsolation_IsInaccessibleScope()
        => FilterInspectionMapper.ResolveEmptyReason(EvidencePacketFixtures.UnknownScopePacket(), filtersActive: true)
            .ShouldBe(FilteredEmptyReason.InaccessibleScope);

    [Theory]
    [InlineData(MemoriesFilterEffect.BroadensScope, InteractionSeverity.Warning)]
    [InlineData(MemoriesFilterEffect.ExcludesAxis, InteractionSeverity.Warning)]
    [InlineData(MemoriesFilterEffect.HidesStaleOrConflicting, InteractionSeverity.Caution)]
    [InlineData(MemoriesFilterEffect.AffectsConfidence, InteractionSeverity.Caution)]
    [InlineData(MemoriesFilterEffect.NarrowsScope, InteractionSeverity.Info)]
    [InlineData(MemoriesFilterEffect.ChangesGraphDepth, InteractionSeverity.Info)]
    [InlineData(MemoriesFilterEffect.None, InteractionSeverity.None)]
    public void MapChip_KnownEffect_MapsTrustSeverity(MemoriesFilterEffect effect, InteractionSeverity expected)
    {
        FilterChipView chip = FilterInspectionMapper.MapChip(
            new MemoriesFilter(MemoriesFilterAxis.SourceType, "file", effect, IsContractKnown: true));

        chip.Availability.ShouldBe(FilterChipAvailability.Available);
        chip.Severity.ShouldBe(expected);
    }

    [Fact]
    public void MapChip_SensitiveValueToken_IsRedacted()
    {
        FilterChipView chip = FilterInspectionMapper.MapChip(
            new MemoriesFilter(MemoriesFilterAxis.Metadata, "Bearer leaked-token", MemoriesFilterEffect.NarrowsScope, IsContractKnown: true));

        chip.ValueText.ShouldNotContain("Bearer ");
        chip.ValueText.ShouldNotContain("leaked-token");
        chip.ValueText.ShouldContain("[REDACTED]");
    }
}
