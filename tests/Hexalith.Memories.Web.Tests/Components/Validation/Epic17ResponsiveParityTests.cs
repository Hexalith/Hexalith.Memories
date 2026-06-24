// <copyright file="Epic17ResponsiveParityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Grid;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Tests.Components.Evidence;
using Hexalith.Memories.Web.Tests.Components.Lenses;
using Hexalith.Memories.Web.Tests.Components.Recovery;

using Shouldly;

/// <summary>
/// Story 17.5 Task 1 (AC1) — responsive validation at component-specimen level proves information parity:
/// trust-critical fields stay reachable as layouts compact, overflow uses disclosure rather than
/// horizontal-scroll-only access, and changing role/density never drops canonical contract-backed fields.
/// True pixel-width 360/768/1024/1440 layout and reflow are browser dimensions deferred in
/// <see cref="Epic17ValidationInventory"/>.
/// </summary>
public sealed class Epic17ResponsiveParityTests : Epic17ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GridPlanner_AcrossEveryMaxVisibleWidth_NeverCollapsesTrustCriticalColumns(bool compact)
    {
        IReadOnlyList<MemoriesGridColumn> columns =
        [
            new("rank", "rank", false),
            new("tenant", "tenant", true),
            new("case", "case", true),
            new("confidence", "confidence", true),
            new("freshness", "freshness", true),
            new("evidenceHealth", "evidence health", true),
            new("source", "source", false),
            new("annotations", "annotations", false),
        ];

        int trustCritical = columns.Count(static c => c.IsTrustCritical);

        // Sweep the responsive width budget from "phone narrow" (1 column) to "wide desktop" (all columns).
        for (int maxVisible = 1; maxVisible <= columns.Count; maxVisible++)
        {
            GridColumnPlan plan = CompactGridColumnPlanner.Plan(columns, maxVisible, compact);

            plan.Visible.Count(static c => c.IsTrustCritical)
                .ShouldBe(trustCritical, $"compact={compact}, maxVisible={maxVisible}: a trust-critical column was hidden.");
            plan.Collapsible.ShouldAllBe(static c => !c.IsTrustCritical);
        }
    }

    [Fact]
    public void Grid_CompactLayout_KeepsTrustCriticalReachableAndExposesOverflowAsDisclosure()
    {
        IRenderedComponent<MemoriesEvidenceGrid> component = Render<MemoriesEvidenceGrid>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.MultiSourcePacket())
            .Add(p => p.Compact, true)
            .Add(p => p.MaxVisibleColumns, 3));

        // Trust-critical cells are never collapsed away under compaction.
        component.FindAll("[data-testid='mem-grid-cell']")
            .Where(static c => c.GetAttribute("data-trust-critical") == "true")
            .ShouldAllBe(static c => c.GetAttribute("data-collapsed") == "false");

        // Overflow is reached through a disclosure affordance, not horizontal-scroll-only access.
        component.FindAll("[data-testid='mem-grid-more']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Cockpit_CanonicalTrustFields_RemainPresentInDefaultLayout()
    {
        IRenderedComponent<Hexalith.Memories.Web.Components.Evidence.MemoriesEvidenceCockpit> component =
            Render<Hexalith.Memories.Web.Components.Evidence.MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket()));

        // Scope, confidence, freshness, source count, and evidence health are all reachable.
        component.Find("[data-testid='mem-scope-tenant']").TextContent.ShouldContain("tenant-a");
        component.Find("[data-testid='mem-scope-case']").TextContent.ShouldContain("case-a");
        component.Find("[data-testid='mem-scope-isolation']").ShouldNotBeNull();
        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldNotBeNullOrWhiteSpace();

        string markup = component.Markup;
        markup.ShouldContain("aria-label=\"Confidence:");
        markup.ShouldContain("aria-label=\"Freshness:");
        markup.ShouldContain("aria-label=\"Evidence health:");
    }

    [Theory]
    [MemberData(nameof(LensFixtureNames))]
    public void LensShell_ChangingRoleDensity_PreservesCanonicalTrustFields(string fixtureName)
    {
        EvidencePacket packet = FixtureByName(fixtureName);
        const string ReturnRoute = "memories/evidence?packet=memory-a";

        // The developer profile is the most expanded; every other role must keep the same canonical fields.
        LensShellViewModel baseline = LensShellMapper.Map(packet, LensKind.CaseActivityTrail, LensRole.Developer, ReturnRoute);

        foreach (LensRole role in Enum.GetValues<LensRole>())
        {
            LensShellViewModel shell = LensShellMapper.Map(packet, LensKind.CaseActivityTrail, role, ReturnRoute);

            shell.TenantId.ShouldBe(baseline.TenantId, $"role {role} dropped tenant scope.");
            shell.CaseId.ShouldBe(baseline.CaseId, $"role {role} dropped case scope.");
            shell.StateKind.ShouldBe(baseline.StateKind, $"role {role} changed evidence state.");
            shell.ContractVersion.ShouldBe(baseline.ContractVersion, $"role {role} changed contract version.");
            shell.ReturnRoute.ShouldBe(baseline.ReturnRoute, $"role {role} changed the return path.");
            shell.Restrictive.ShouldBe(baseline.Restrictive, $"role {role} changed restrictive disposition.");
        }
    }

    [Fact]
    public void LensRoleDensity_EveryRole_ResolvesAValidDetailProfile()
    {
        foreach (LensRole role in Enum.GetValues<LensRole>())
        {
            LensRoleDensityProfile profile = LensRoleDensity.For(role);
            Enum.IsDefined(profile.DetailLevel).ShouldBeTrue($"role {role} produced an undefined detail level.");
        }
    }

    [Theory]
    [MemberData(nameof(RecoveryBearingFixtureNames))]
    public void Cockpit_SafestRecoveryAction_RemainsReachableAcrossRecoveryStates(string fixtureName)
    {
        // AC1 (Task 1): alongside scope/confidence/freshness/source-count/evidence-health, the safest
        // recovery action must remain visible or keyboard/touch reachable — never hidden or hover-gated.
        IRenderedComponent<Hexalith.Memories.Web.Components.Evidence.MemoriesEvidenceCockpit> component =
            Render<Hexalith.Memories.Web.Components.Evidence.MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.Packet, RecoveryFixtureByName(fixtureName)));

        IElement recovery = component.Find("[data-testid='mem-evidence-recovery']");
        recovery.GetAttribute("aria-hidden").ShouldBeNull();

        IReadOnlyList<IElement> actions = component.FindAll("[data-testid='mem-recovery-action-button']");
        IReadOnlyList<IElement> noAction = component.FindAll("[data-testid='mem-recovery-no-action']");

        (actions.Count > 0 || noAction.Count > 0)
            .ShouldBeTrue($"Cockpit '{fixtureName}' exposed no reachable recovery affordance.");

        foreach (IElement action in actions)
        {
            action.GetAttribute("aria-hidden").ShouldBeNull();
            string? tabIndex = action.GetAttribute("tabindex");
            (tabIndex is null || int.Parse(tabIndex, CultureInfo.InvariantCulture) >= 0)
                .ShouldBeTrue($"Cockpit '{fixtureName}' has a recovery action with a negative tabindex.");
        }
    }

    public static IEnumerable<object[]> LensFixtureNames()
    {
        yield return ["Happy"];
        yield return ["Degraded"];
        yield return ["Stale"];
        yield return ["Compressed"];
        yield return ["Unauthorized"];
    }

    public static IEnumerable<object[]> RecoveryBearingFixtureNames()
    {
        yield return ["DegradedBackendNoSources"];
        yield return ["NotIngestedYet"];
        yield return ["StaleMemory"];
        yield return ["MultiActionNoMatch"];
    }

    private static EvidencePacket RecoveryFixtureByName(string name)
        => name switch
        {
            "DegradedBackendNoSources" => RecoveryPacketFixtures.DegradedBackendNoSources(),
            "NotIngestedYet" => RecoveryPacketFixtures.NotIngestedYet(),
            "StaleMemory" => RecoveryPacketFixtures.StaleMemory(),
            "MultiActionNoMatch" => RecoveryPacketFixtures.MultiActionNoMatch(),
            _ => throw new InvalidOperationException($"Unknown recovery fixture '{name}'."),
        };

    private static EvidencePacket FixtureByName(string name)
        => name switch
        {
            "Happy" => LensPacketFixtures.Happy(),
            "Degraded" => LensPacketFixtures.Degraded(),
            "Stale" => LensPacketFixtures.Stale(),
            "Compressed" => LensPacketFixtures.Compressed(),
            "Unauthorized" => LensPacketFixtures.Unauthorized(),
            _ => throw new InvalidOperationException($"Unknown fixture '{name}'."),
        };
}
