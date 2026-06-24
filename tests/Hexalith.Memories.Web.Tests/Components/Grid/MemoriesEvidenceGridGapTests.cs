// <copyright file="MemoriesEvidenceGridGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Grid;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Filters;
using Hexalith.Memories.Web.Components.Grid;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for the evidence grid: planner guard/non-compact paths, multi-row rendering, source
/// sanitization, and the non-restrictive empty and unknown-scope empty states.
/// </summary>
public sealed class MemoriesEvidenceGridGapTests : FrontComposerTestBase
{
    public MemoriesEvidenceGridGapTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Planner_NonCompact_KeepsAllColumnsVisibleAndNoneCollapsible()
    {
        IReadOnlyList<MemoriesGridColumn> columns =
        [
            new("rank", "rank", false),
            new("case", "case", true),
            new("source", "source", false),
        ];

        GridColumnPlan plan = CompactGridColumnPlanner.Plan(columns, maxVisible: 1, compact: false);

        plan.Visible.Count.ShouldBe(3);
        plan.Collapsible.ShouldBeEmpty();
    }

    [Fact]
    public void Planner_NegativeMaxVisible_Throws()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => CompactGridColumnPlanner.Plan([new MemoriesGridColumn("rank", "rank", false)], maxVisible: -1, compact: true));

    [Fact]
    public void Planner_NullColumns_Throws()
        => Should.Throw<ArgumentNullException>(() => CompactGridColumnPlanner.Plan(null!, maxVisible: 3, compact: true));

    [Fact]
    public void Grid_MultiSourcePacket_RendersEveryRowAndRowAction()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.MultiSourcePacket()));

        component.Find("[data-testid='mem-evidence-grid']").GetAttribute("data-row-count").ShouldBe("2");
        component.FindAll("[data-testid='mem-grid-row-action']").Count.ShouldBe(2);
    }

    [Fact]
    public void Grid_SensitiveSourceUri_IsRedacted()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.SensitivePacket()));

        component.Markup.ShouldContain("[REDACTED]");
        component.Markup.ShouldNotContain("C:\\");
        component.Markup.ShouldNotContain("Bearer ");
    }

    [Fact]
    public void Grid_NonRestrictiveEmptyPacket_RendersNoMatchReason()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.EmptyPacket()));

        component.Find("[data-testid='mem-evidence-grid']").GetAttribute("data-row-count").ShouldBe("0");
        component.Find("[data-testid='mem-grid-empty']")
            .GetAttribute("data-empty-reason")
            .ShouldBe(nameof(FilteredEmptyReason.NoMatch));
    }

    [Fact]
    public void Grid_UnknownScopePacket_RendersNoRowsAsInaccessibleScope()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnknownScopePacket()));

        component.Find("[data-testid='mem-evidence-grid']").GetAttribute("data-row-count").ShouldBe("0");
        component.Find("[data-testid='mem-grid-empty']")
            .GetAttribute("data-empty-reason")
            .ShouldBe(nameof(FilteredEmptyReason.InaccessibleScope));
    }
}
