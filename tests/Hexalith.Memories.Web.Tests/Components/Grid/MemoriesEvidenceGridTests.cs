// <copyright file="MemoriesEvidenceGridTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Grid;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Grid;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class MemoriesEvidenceGridTests : FrontComposerTestBase
{
    public MemoriesEvidenceGridTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Planner_CompactMode_NeverCollapsesTrustCriticalColumns()
    {
        IReadOnlyList<MemoriesGridColumn> columns =
        [
            new("rank", "rank", false),
            new("tenant", "tenant", true),
            new("case", "case", true),
            new("confidence", "confidence", true),
            new("source", "source", false),
        ];

        GridColumnPlan plan = CompactGridColumnPlanner.Plan(columns, maxVisible: 2, compact: true);

        plan.Visible.ShouldContain(c => c.ColumnKey == "tenant");
        plan.Visible.ShouldContain(c => c.ColumnKey == "case");
        plan.Visible.ShouldContain(c => c.ColumnKey == "confidence");
        plan.Collapsible.ShouldAllBe(static c => !c.IsTrustCritical);
    }

    [Fact]
    public void Grid_UnauthorizedPacket_DoesNotRenderLeakedRows()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket()));

        component.Find("[data-testid='mem-evidence-grid']").GetAttribute("data-row-count").ShouldBe("0");
        component.Markup.ShouldNotContain("memory-secret");
        component.Find("[data-testid='mem-grid-empty']")
            .GetAttribute("data-empty-reason")
            .ShouldBe(nameof(Hexalith.Memories.Web.Components.Filters.FilteredEmptyReason.InaccessibleScope));
    }

    [Fact]
    public void Grid_CompactMode_CollapsesOnlyReachableNonTrustColumns()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Compact, true)
            .Add(p => p.MaxVisibleColumns, 3));

        component.FindAll("[data-testid='mem-grid-cell']")
            .Where(static c => c.GetAttribute("data-trust-critical") == "true")
            .ShouldAllBe(static c => c.GetAttribute("data-collapsed") == "false");
        component.FindAll("[data-testid='mem-grid-more']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Grid_RowAction_EmitsBoundedSource()
    {
        EvidencePacketSource? selected = null;
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.OnRowAction, (EvidencePacketSource source) => selected = source));

        component.Find("[data-testid='mem-grid-row-action']").Click();

        selected.ShouldNotBeNull();
        selected!.MemoryUnitId.ShouldBe("memory-a");
    }
}
