// <copyright file="Epic17AccessibilitySweepTests.cs" company="ITANEO">
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
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Forms;
using Hexalith.Memories.Web.Tests.Components.Evidence;
using Hexalith.Memories.Web.Tests.Components.Forms;
using Hexalith.Memories.Web.Tests.Components.Lenses;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

/// <summary>
/// Story 17.5 Tasks 2, 3, 4 — cross-surface automated accessibility gates that run at component-specimen
/// level: zero-node anchor guards, focusable interactive controls, no hover-only behaviour, valid
/// live-region pairings, and non-color comprehension of trust states. Browser-only checks (axe, contrast,
/// forced-colors, reduced-motion, screen reader) are deferred and tracked in <see cref="Epic17ValidationInventory"/>.
/// </summary>
public sealed class Epic17AccessibilitySweepTests : Epic17ValidationTestBase
{
    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_RootAnchor_MatchesAtLeastOneNode(string surface)
    {
        (string anchor, EvidencePacket packet) = AnchorFor(surface);

        string markup = RenderSurface(surface, packet);

        // Fail closed against an empty or wrong-root render: the required anchor must match a real node.
        QueryAll(markup, $"[data-testid='{anchor}']").Count.ShouldBeGreaterThanOrEqualTo(
            1,
            $"Surface '{surface}' rendered without its required selector anchor '{anchor}'.");
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_DoesNotDependOnHoverOnlyInteraction(string surface)
    {
        foreach (EvidencePacket packet in RepresentativeStates())
        {
            string markup = RenderSurface(surface, packet);

            // No raw hover handlers: the Epic 17 boundary forbids hand-rolled HTML/JS, so any literal
            // onmouse* attribute would be a regression toward hover-only behaviour.
            markup.ShouldNotContain("onmouseover", Shouldly.Case.Insensitive);
            markup.ShouldNotContain("onmouseenter", Shouldly.Case.Insensitive);
            markup.ShouldNotContain("onmouseleave", Shouldly.Case.Insensitive);
            markup.ShouldNotContain("onmouseout", Shouldly.Case.Insensitive);

            // Positive guarantee: trust-critical behaviour is reachable without hover. A negative substring
            // check alone is vacuous because Blazor never serialises @onmouse* bindings into bUnit markup, so
            // also assert every interactive affordance is keyboard-focusable (no negative tabindex) and not
            // hidden from assistive technology.
            foreach (IElement control in QueryAll(markup, "fluent-button, fluent-anchor, [role='button'], button, a[href]"))
            {
                control.GetAttribute("aria-hidden").ShouldNotBe(
                    "true",
                    $"Surface '{surface}' hides an interactive control from assistive technology.");

                string? tabIndex = control.GetAttribute("tabindex");
                (tabIndex is null || int.Parse(tabIndex, CultureInfo.InvariantCulture) >= 0)
                    .ShouldBeTrue($"Surface '{surface}' has a pointer/hover-only control with a negative tabindex.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_InteractiveControls_AreFocusableAndNotHidden(string surface)
    {
        foreach (EvidencePacket packet in RepresentativeStates())
        {
            string markup = RenderSurface(surface, packet);

            foreach (IElement button in QueryAll(markup, "fluent-button"))
            {
                button.GetAttribute("aria-hidden").ShouldBeNull();

                string? tabIndex = button.GetAttribute("tabindex");
                (tabIndex is null || int.Parse(tabIndex, CultureInfo.InvariantCulture) >= 0)
                    .ShouldBeTrue($"Surface '{surface}' has a button with a negative tabindex.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(PacketSurfaceNames))]
    public void Surface_LiveRegions_NeverPairStatusWithAssertiveOrAlertWithPolite(string surface)
    {
        foreach (EvidencePacket packet in RepresentativeStates())
        {
            string markup = RenderSurface(surface, packet);

            // Non-blocking updates announce politely; only blocking/safety-critical states announce assertively.
            QueryAll(markup, "[role='status']")
                .ShouldAllBe(e => e.GetAttribute("aria-live") != "assertive");
            QueryAll(markup, "[role='alert']")
                .ShouldAllBe(e => e.GetAttribute("aria-live") != "polite");
        }
    }

    [Fact]
    public void TrustStrip_StatesAreConveyedAsAccessibleText_NotColorAlone()
    {
        string markup = RenderSurface("TrustStrip", LensPacketFixtures.Compressed());

        // Confidence, freshness, evidence health, and token budget all carry text-bearing accessible names.
        markup.ShouldContain("aria-label=\"Confidence:");
        markup.ShouldContain("aria-label=\"Freshness:");
        markup.ShouldContain("aria-label=\"Evidence health:");
        markup.ShouldContain("aria-label=\"Token budget:");
    }

    [Fact]
    public void RecoveryPanel_SeverityAndCapabilityAreConveyedAsText_NotColorAlone()
    {
        string markup = RenderSurface("RecoveryActionPanel", LensPacketFixtures.Compressed());

        markup.ShouldContain("aria-label=\"Severity:");
        markup.ShouldContain("aria-label=\"Affected capability:");
    }

    [Fact]
    public void BenchmarkComparator_AxisBarsExposeTextScore_NotColorOrPositionAlone()
    {
        string markup = RenderSurface("BenchmarkResultComparator", LensPacketFixtures.Happy());

        QueryAll(markup, "[data-testid='mem-benchmark-axis']").ShouldNotBeEmpty();
        QueryAll(markup, "[data-testid='mem-benchmark-axis-score']")
            .ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.TextContent));
    }

    [Fact]
    public void OperatorHealthMatrix_CheckStatusIsTextLabelled_NotColorAlone()
    {
        string markup = RenderSurface("OperatorHealthMatrix", LensPacketFixtures.Degraded());

        IReadOnlyList<IElement> checks = QueryAll(markup, "[data-testid='mem-health-check']");
        checks.ShouldNotBeEmpty();

        // Every check carries a data-status token AND a human-readable status name (text equivalent).
        checks.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.GetAttribute("data-status")));
        QueryAll(markup, "[data-testid='mem-health-check-name']")
            .ShouldAllBe(n => !string.IsNullOrWhiteSpace(n.TextContent));
    }

    [Fact]
    public void IngestionTracker_OutcomeIsTextLabelled_NotColorAlone()
    {
        string markup = RenderSurface("IngestionLifecycleTracker", LensPacketFixtures.Degraded());

        IReadOnlyList<IElement> units = QueryAll(markup, "[data-testid='mem-ingestion-unit']");
        units.ShouldNotBeEmpty();
        units.ShouldAllBe(u => !string.IsNullOrWhiteSpace(u.GetAttribute("data-outcome")));
    }

    [Fact]
    public void CommandSurface_DisabledControls_ExposeTextReason_NotColorAlone()
    {
        // A stale active scope disables unsafe commands; the reason must be readable text, not color only.
        IRenderedComponent<Hexalith.Memories.Web.Components.Interaction.MemoriesCommandSurface> component =
            Render<Hexalith.Memories.Web.Components.Interaction.MemoriesCommandSurface>(p => p
                .Add(c => c.Packet, EvidencePacketFixtures.CompletePacket())
                .Add(c => c.Snapshot, Hexalith.Memories.Web.Tests.Components.Interaction.InteractionContextTests.Snapshot())
                .Add(c => c.ActiveTenantId, "tenant-b")
                .Add(c => c.ActiveCaseId, "case-a"));

        component.FindAll("[data-testid='mem-command-disabled-reason']")
            .ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.TextContent));
    }

    [Theory]
    [MemberData(nameof(HeadingOutlineStates))]
    public void Cockpit_ComposedAccordionHeaders_UseOneLevelTwoOutlineWithStableNames(string fixtureName)
    {
        // Story 25.7: the Fluent accordion owns the sibling section headings. HeadingLevel=2 gives every
        // item one consistent native outline level when the web component hydrates; Header is the current
        // Fluent V5 member and must always carry a readable localized name.
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(c => c.Packet, CockpitHeadingFixture(fixtureName)));

        IRenderedComponent<FluentAccordion> accordion = component.FindComponent<FluentAccordion>();
        accordion.Instance.HeadingLevel.ShouldBe(2);
        IReadOnlyList<IRenderedComponent<FluentAccordionItem>> items = component.FindComponents<FluentAccordionItem>();
        items.ShouldNotBeEmpty($"Cockpit '{fixtureName}' rendered no accordion headers.");
        items.ShouldAllBe(static item => !string.IsNullOrWhiteSpace(item.Instance.Header));
        items[0].Instance.Header.ShouldBe("Evidence");
        items[0].Instance.Expanded.ShouldBeTrue();
        items[1].Instance.Header.ShouldBe("Recovery and feedback");
        items[1].Instance.Expanded.ShouldBeTrue();
    }

    [Fact]
    public void InteractionForm_Controls_ExposeAccessibleNames_NotColorOrPlacementAlone()
    {
        // AC2 (form labels): the form region is named, every field row carries a text label, and the submit
        // control has a readable accessible name rather than relying on colour or position.
        string markup = Render<MemoriesInteractionForm>(parameters => parameters
            .Add(c => c.Request, FormFixtures.Request())).Markup;

        IElement form = QueryAll(markup, "[data-testid='mem-interaction-form']").Single();
        form.GetAttribute("role").ShouldBe("form");
        form.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();

        IReadOnlyList<IElement> labels = QueryAll(markup, "[data-testid='mem-form-field-label']");
        labels.ShouldNotBeEmpty();
        labels.ShouldAllBe(static l => !string.IsNullOrWhiteSpace(l.TextContent));

        QueryAll(markup, "[data-testid='mem-form-submit']").Single()
            .TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InteractionForm_EveryAriaDescribedbyReference_ResolvesToARenderedId()
    {
        // AC2 (ARIA validity): a blank required tenant forces a field-associated validation message, and
        // every aria-describedby idref must resolve to exactly one rendered element (no dangling reference).
        string markup = Render<MemoriesInteractionForm>(parameters => parameters
            .Add(c => c.Request, FormFixtures.Request(
                requestedTenant: " ",
                fields: [FormFixtures.Tenant(" "), FormFixtures.Case()]))).Markup;

        IReadOnlyList<IElement> described = QueryAll(markup, "[aria-describedby]");
        described.ShouldNotBeEmpty("the field-association path was never exercised.");

        foreach (IElement element in described)
        {
            foreach (string idref in (element.GetAttribute("aria-describedby") ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                QueryAll(markup, $"#{idref}").Count.ShouldBe(
                    1,
                    $"aria-describedby points at a missing or duplicated id '{idref}'.");
            }
        }
    }

    public static IEnumerable<object[]> HeadingOutlineStates()
    {
        yield return ["Happy"];
        yield return ["Degraded"];
        yield return ["Stale"];
        yield return ["Compressed"];
        yield return ["Unauthorized"];
    }

    private static EvidencePacket CockpitHeadingFixture(string name)
        => name switch
        {
            "Happy" => LensPacketFixtures.Happy(),
            "Degraded" => LensPacketFixtures.Degraded(),
            "Stale" => LensPacketFixtures.Stale(),
            "Compressed" => LensPacketFixtures.Compressed(),
            "Unauthorized" => LensPacketFixtures.Unauthorized(),
            _ => throw new InvalidOperationException($"Unknown fixture '{name}'."),
        };

    private static (string Anchor, EvidencePacket Packet) AnchorFor(string surface)
        => surface switch
        {
            "EvidenceCockpit" => ("mem-evidence-cockpit", LensPacketFixtures.Happy()),
            "TrustStrip" => ("mem-trust-strip", LensPacketFixtures.Happy()),
            "RecoveryActionPanel" => ("mem-evidence-recovery", LensPacketFixtures.Degraded()),
            "EvidenceGrid" => ("mem-evidence-grid", LensPacketFixtures.Happy()),
            "CommandSurface" => ("mem-command-surface", LensPacketFixtures.Happy()),
            "ContextNavigation" => ("mem-context-navigation", LensPacketFixtures.Happy()),
            "CaseActivityTrail" => ("mem-activity-trail", LensPacketFixtures.Happy()),
            "IngestionLifecycleTracker" => ("mem-ingestion-tracker", LensPacketFixtures.Degraded()),
            "OperatorHealthMatrix" => ("mem-health-matrix", LensPacketFixtures.Degraded()),
            "BenchmarkResultComparator" => ("mem-benchmark-comparator", LensPacketFixtures.Happy()),
            "AgentPacketInspector" => ("mem-packet-inspector", LensPacketFixtures.Happy()),
            _ => throw new System.InvalidOperationException($"Unknown surface '{surface}'."),
        };

    private static IEnumerable<EvidencePacket> RepresentativeStates()
    {
        yield return LensPacketFixtures.Happy();
        yield return LensPacketFixtures.Degraded();
        yield return LensPacketFixtures.Unauthorized();
        yield return LensPacketFixtures.Compressed();
        yield return LensPacketFixtures.Stale();
        yield return LensPacketFixtures.Empty();
    }
}
