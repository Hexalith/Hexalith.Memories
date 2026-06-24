// <copyright file="Epic17InventoryTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;
using System.Linq;

using Shouldly;

/// <summary>
/// Story 17.5 Task 0 / Task 6 — fail-closed checks over the validation inventory and tooling-gap registry.
/// A surface cannot be counted as covered unless every required column is filled, and every browser /
/// assistive-technology gap must carry an owner, severity, waiver state, and release disposition.
/// </summary>
public sealed class Epic17InventoryTests
{
    [Fact]
    public void Inventory_EveryAcceptanceCriterion1Surface_IsNamedWithAValidationLevel()
    {
        // The trust surfaces explicitly named in AC1 (plus the lens/interaction surfaces validated here).
        string[] requiredSurfaces =
        [
            "Evidence Cockpit",
            "Trust Strip",
            "Scope Header",
            "Source Citation Stack",
            "Retrieval Axis Breakdown",
            "Graph Path Summary",
            "Recovery Action Panel",
            "Case Activity Trail",
            "Ingestion Lifecycle Tracker",
            "Operator Health Matrix",
            "Benchmark Result Comparator",
            "Agent Packet Inspector",
        ];

        foreach (string surface in requiredSurfaces)
        {
            Epic17ValidationInventory.Surfaces
                .ShouldContain(s => s.Surface == surface, $"AC1 surface '{surface}' missing from the inventory.");
        }
    }

    [Fact]
    public void Inventory_EveryRow_FillsEveryRequiredColumn()
    {
        foreach (Epic17ValidationInventory.SurfaceRow row in Epic17ValidationInventory.Surfaces)
        {
            row.Surface.ShouldNotBeNullOrWhiteSpace();
            row.UpstreamStory.ShouldNotBeNullOrWhiteSpace();
            row.ImplementationSource.ShouldNotBeNullOrWhiteSpace();
            row.RunnableSpecimen.ShouldNotBeNullOrWhiteSpace();
            row.FixtureFamily.ShouldNotBeNullOrWhiteSpace();
            row.SelectorAnchor.ShouldNotBeNullOrWhiteSpace();
            row.ValidationLevel.ShouldBe(Epic17ValidationInventory.ComponentSpecimen);

            // Fail closed: a component-specimen claim must always name how the browser dimensions are dispositioned.
            row.BrowserDisposition.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Inventory_DoesNotClaimAnyProductRouteOrFullBrowserValidation()
    {
        // There is no runnable Memories web host; nothing may be claimed as product-route validation.
        Epic17ValidationInventory.Surfaces
            .ShouldAllBe(s => s.ValidationLevel != "product-route");
    }

    [Fact]
    public void ToolingGaps_AreNonEmptyAndEachCarriesOwnerSeverityWaiverAndReleaseDisposition()
    {
        Epic17ValidationInventory.Gaps.ShouldNotBeEmpty();

        foreach (Epic17ValidationInventory.ToolingGap gap in Epic17ValidationInventory.Gaps)
        {
            gap.Check.ShouldNotBeNullOrWhiteSpace();
            gap.Reason.ShouldNotBeNullOrWhiteSpace();
            gap.Severity.ShouldBeOneOf("High", "Medium", "Low");
            gap.Owner.ShouldNotBeNullOrWhiteSpace();
            gap.WaiverState.ShouldNotBeNullOrWhiteSpace();
            gap.ReleaseDisposition.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ToolingGaps_RecordTheKnownBrowserAndAssistiveTechnologyDimensions()
    {
        string allChecks = string.Join(" | ", Epic17ValidationInventory.Gaps.Select(static g => g.Check))
            .ToUpperInvariant();

        // Each dimension the ACs require but bUnit cannot run must be explicitly tracked, not omitted.
        allChecks.ShouldContain("PLAYWRIGHT");
        allChecks.ShouldContain("AXE");
        allChecks.ShouldContain("CONTRAST");
        allChecks.ShouldContain("FORCED-COLORS");
        allChecks.ShouldContain("REDUCED-MOTION");
        allChecks.ShouldContain("ZOOM");
        allChecks.ShouldContain("SCREEN-READER");
    }

    [Fact]
    public void AcceptanceCriteriaToTestMap_CoversEverySixAcceptanceCriteria()
    {
        for (int i = 1; i <= 6; i++)
        {
            string key = $"AC{i}";
            Epic17ValidationInventory.AcceptanceCriteriaToTests.Keys.ShouldContain(key);
            Epic17ValidationInventory.AcceptanceCriteriaToTests[key].Length.ShouldBeGreaterThan(0);
        }
    }
}
