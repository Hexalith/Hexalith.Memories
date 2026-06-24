// <copyright file="Epic17FocusContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;
using Hexalith.Memories.Web.Tests.Components.Interaction;

using Shouldly;

/// <summary>
/// Story 17.5 Tasks 3 and 4 (AC4, AC5) — one documented focus contract per interactive/overlay surface,
/// plus the component-level evidence that overlays use focusable controls, preserve the return path, and
/// route destructive confirmation through the FrontComposer destructive dialog that owns focus trap and
/// focus return. Live focus movement and screen-reader announcement are browser/AT dimensions deferred in
/// <see cref="Epic17ValidationInventory"/> and paired with a required manual pass.
/// </summary>
public sealed class Epic17FocusContractTests : Epic17ValidationTestBase
{
    /// <summary>A documented focus contract for one interactive or overlay surface.</summary>
    /// <param name="Surface">The surface the contract governs.</param>
    /// <param name="InitialFocus">Where focus lands when the surface opens.</param>
    /// <param name="TabOrder">The trust-workflow tab order.</param>
    /// <param name="EscapeBehavior">Escape/cancel behaviour.</param>
    /// <param name="FocusReturnTarget">Where focus returns when the surface closes.</param>
    /// <param name="Announcement">The live-region announcement contract.</param>
    public sealed record FocusContract(
        string Surface,
        string InitialFocus,
        string TabOrder,
        string EscapeBehavior,
        string FocusReturnTarget,
        string Announcement);

    /// <summary>The focus contracts for every overlay or interactive Epic 17 surface.</summary>
    public static IReadOnlyList<FocusContract> Contracts { get; } =
    [
        new(
            "Action Confirmation",
            "First actionable control inside the destructive dialog",
            "Scope summary → consequence → recovery → cancel → confirm",
            "Escape cancels unless the operation cannot be safely dismissed",
            "The invoking command control",
            "Dialog is announced; destructive intent named in text"),
        new(
            "Context Navigation",
            "The open/inspect control",
            "Context summary → open → return",
            "Escape/back returns to the invoking evidence view",
            "The invoking evidence control; return path preserved even when open is disabled",
            "Stale/invalid context announced as a disabled reason"),
        new(
            "Command Surface",
            "First available command",
            "Available commands in trust-workflow order; disabled commands remain focusable with a reason",
            "Escape leaves the command surface without dispatching",
            "The evidence row/packet that invoked the command",
            "Disabled commands expose a text reason"),
        new(
            "Recovery Action Panel",
            "The safest (primary) recovery action",
            "Primary action → secondary actions",
            "No dismissal: recovery state persists until resolved",
            "Focus stays within the evidence cockpit after acting",
            "polite for non-blocking states, assertive for unauthorized/fatal"),
        new(
            "Source / Axis / Graph Preview",
            "The selected source/axis/graph row",
            "Citation stack rows in packet order; each row keyboard-reachable",
            "Escape returns to the citation stack",
            "The invoking citation row",
            "Restricted previews announce unavailability without leaking content"),
        new(
            "Lens Detail Shell",
            "The lens title region",
            "Scope → trust strip → lens body → return",
            "Return action navigates back to the evidence origin",
            "The invoking evidence/lens entry control",
            "Critical lens states announce assertively"),
    ];

    [Fact]
    public void FocusContracts_CoverEveryOverlayAndInteractiveSurface_WithEveryFieldFilled()
    {
        string[] requiredSurfaces =
        [
            "Action Confirmation",
            "Context Navigation",
            "Command Surface",
            "Recovery Action Panel",
            "Source / Axis / Graph Preview",
            "Lens Detail Shell",
        ];

        foreach (string surface in requiredSurfaces)
        {
            Contracts.ShouldContain(c => c.Surface == surface, $"Missing focus contract for '{surface}'.");
        }

        foreach (FocusContract contract in Contracts)
        {
            contract.InitialFocus.ShouldNotBeNullOrWhiteSpace();
            contract.TabOrder.ShouldNotBeNullOrWhiteSpace();
            contract.EscapeBehavior.ShouldNotBeNullOrWhiteSpace();
            contract.FocusReturnTarget.ShouldNotBeNullOrWhiteSpace();
            contract.Announcement.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ActionConfirmation_RoutesThroughFrontComposerDestructiveDialog_ThatOwnsFocusReturn()
    {
        MemoriesCommandView command = MemoriesCommandSurfaceMapper.Map(
                EvidencePacketFixtures.CompletePacket(),
                InteractionContextTests.Snapshot(),
                "tenant-a",
                "case-a")
            .Single(static c => c.Kind == MemoriesCommandKind.ExportPacket);

        IRenderedComponent<MemoriesActionConfirmation> component = Render<MemoriesActionConfirmation>(parameters => parameters
            .Add(p => p.Command, command)
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot()));

        // The destructive dialog (FrontComposer) owns focus trap + return; the body keeps scope and
        // consequence reachable as text so the confirmation is comprehensible without color or hover.
        IElement dialog = component.Find("[data-testid='fc-destructive-dialog']");
        dialog.GetAttribute("aria-hidden").ShouldBeNull();
        dialog.TextContent.ShouldContain("Tenant: tenant-a");
        dialog.TextContent.ShouldContain("Consequence:");
        dialog.TextContent.ShouldContain("Recovery:");
    }

    [Fact]
    public void ContextNavigation_StaleContext_DisablesOpenButPreservesReturnPath()
    {
        string? returned = null;
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-b")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnReturn, (string route) => returned = route));

        component.Find("[data-testid='mem-context-navigation']").GetAttribute("data-valid").ShouldBe("false");

        // The return path back to the invoking control survives even when the forward action is disabled.
        component.Find("[data-testid='mem-navigation-return-action']").Click();
        returned.ShouldBe("memories/evidence?packet=memory-a");
    }

    [Fact]
    public void OverlayActionControls_AreFocusableAndNotAriaHidden()
    {
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a"));

        IReadOnlyList<IElement> buttons = component.FindAll("fluent-button");
        buttons.ShouldNotBeEmpty();
        foreach (IElement button in buttons)
        {
            button.GetAttribute("aria-hidden").ShouldBeNull();
            string? tabIndex = button.GetAttribute("tabindex");
            (tabIndex is null || int.Parse(tabIndex, CultureInfo.InvariantCulture) >= 0).ShouldBeTrue();
        }
    }
}
