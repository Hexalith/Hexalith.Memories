// <copyright file="MemoriesFilterSummaryGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Filters;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="MemoriesFilterSummary"/>: the no-filters render path, redaction parity for
/// a sensitive chip value, and that distinct empty-state reasons are surfaced to the rendered region.
/// </summary>
public sealed class MemoriesFilterSummaryGapTests : FrontComposerTestBase
{
    public MemoriesFilterSummaryGapTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Summary_NoFilters_RendersNoneLabelAndNoChips()
    {
        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [])
            .Add(p => p.FilteredCount, 4)
            .Add(p => p.TotalCount, 4));

        component.Find("[data-testid='mem-filter-summary']").GetAttribute("data-active-count").ShouldBe("0");
        component.FindAll("[data-testid='mem-filter-none']").ShouldNotBeEmpty();
        component.FindAll("[data-testid='mem-filter-chip']").ShouldBeEmpty();
    }

    [Fact]
    public void Summary_SensitiveFilterValue_IsRedactedInChip()
    {
        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [new MemoriesFilter(MemoriesFilterAxis.Metadata, "Bearer leaked-token", MemoriesFilterEffect.NarrowsScope, true)])
            .Add(p => p.FilteredCount, 1)
            .Add(p => p.TotalCount, 1));

        component.Find("[data-testid='mem-filter-chip-value']").TextContent.ShouldContain("[REDACTED]");
        component.Markup.ShouldNotContain("Bearer ");
        component.Markup.ShouldNotContain("leaked-token");
    }

    [Theory]
    [InlineData(nameof(FilteredEmptyReason.DegradedBackend))]
    [InlineData(nameof(FilteredEmptyReason.StaleMemory))]
    public void Summary_DistinctDegradedStates_SurfaceTheirEmptyReason(string expectedReason)
    {
        EvidencePacket packet = expectedReason == nameof(FilteredEmptyReason.DegradedBackend)
            ? EvidencePacketFixtures.DegradedPacket()
            : EvidencePacketFixtures.StalePacket();

        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [new MemoriesFilter(MemoriesFilterAxis.SourceType, "file", MemoriesFilterEffect.NarrowsScope, true)])
            .Add(p => p.FilteredCount, 0)
            .Add(p => p.TotalCount, 4)
            .Add(p => p.Packet, packet));

        component.Find("[data-testid='mem-filter-empty']").GetAttribute("data-empty-reason").ShouldBe(expectedReason);
    }
}
