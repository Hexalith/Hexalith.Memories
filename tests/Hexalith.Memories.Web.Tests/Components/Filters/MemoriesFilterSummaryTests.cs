// <copyright file="MemoriesFilterSummaryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Filters;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class MemoriesFilterSummaryTests : FrontComposerTestBase
{
    public MemoriesFilterSummaryTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Summary_RendersTrustEffectChipsAndAggregateFlags()
    {
        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters,
            [
                new(MemoriesFilterAxis.RetrievalAxis, "semantic", MemoriesFilterEffect.ExcludesAxis, true),
                new(MemoriesFilterAxis.Confidence, "weak", MemoriesFilterEffect.AffectsConfidence, true),
            ])
            .Add(p => p.FilteredCount, 1)
            .Add(p => p.TotalCount, 2));

        IElement root = component.Find("[data-testid='mem-filter-summary']");
        root.GetAttribute("data-excludes-axis").ShouldBe("true");
        root.GetAttribute("data-affects-confidence").ShouldBe("true");
        component.FindAll("[data-testid='mem-filter-chip']").Count.ShouldBe(2);
    }

    [Fact]
    public void Summary_UnknownFilter_RendersUnavailableExplanation()
    {
        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [new MemoriesFilter(MemoriesFilterAxis.EvidenceState, "future", MemoriesFilterEffect.None, false)])
            .Add(p => p.FilteredCount, 0)
            .Add(p => p.TotalCount, 1)
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket()));

        component.Find("[data-testid='mem-filter-summary']").GetAttribute("data-has-unavailable").ShouldBe("true");
        component.Find("[data-testid='mem-filter-chip-unavailable']").TextContent.ShouldContain("not recognized");
    }

    [Fact]
    public void Summary_EmptyUnauthorizedPacket_RendersInaccessibleScopeReason()
    {
        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [new MemoriesFilter(MemoriesFilterAxis.SourceType, "file", MemoriesFilterEffect.NarrowsScope, true)])
            .Add(p => p.FilteredCount, 0)
            .Add(p => p.TotalCount, 4)
            .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket()));

        component.Find("[data-testid='mem-filter-empty']")
            .GetAttribute("data-empty-reason")
            .ShouldBe(nameof(FilteredEmptyReason.InaccessibleScope));
    }

    [Fact]
    public void Summary_RemoveAndReset_EmitIntentsOnly()
    {
        MemoriesFilter? removed = null;
        bool reset = false;
        MemoriesFilter filter = new(MemoriesFilterAxis.SourceType, "file", MemoriesFilterEffect.NarrowsScope, true);

        IRenderedComponent<MemoriesFilterSummary> component = Render<MemoriesFilterSummary>(parameters => parameters
            .Add(p => p.Filters, [filter])
            .Add(p => p.FilteredCount, 1)
            .Add(p => p.TotalCount, 1)
            .Add(p => p.OnRemoveFilter, (MemoriesFilter f) => removed = f)
            .Add(p => p.OnReset, () => reset = true));

        component.Find("[data-testid='mem-filter-chip-remove']").Click();
        component.Find("[data-testid='mem-filter-reset']").Click();

        removed.ShouldBe(filter);
        reset.ShouldBeTrue();
    }
}
