// <copyright file="RecoveryActionPanelStateGrammarTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

/// <summary>
/// QA gap coverage for <see cref="MemoriesRecoveryActionPanel"/>: a per-state full grammar render sweep
/// (title, explanation, diagnostic clue, severity, affected capability), and state-transition
/// accessibility (announcement, focus-safe role, and rendered state across packet changes).
/// </summary>
public sealed class RecoveryActionPanelStateGrammarTests : FrontComposerTestBase
{
    public RecoveryActionPanelStateGrammarTests() => Host.ValidateVersionAlignment();

    public static TheoryData<string, RecoveryStateKind> ActionableStates() => new()
    {
        { nameof(RecoveryPacketFixtures.Weak), RecoveryStateKind.Weak },
        { nameof(RecoveryPacketFixtures.StaleMemory), RecoveryStateKind.StaleMemory },
        { nameof(RecoveryPacketFixtures.DegradedBackendWithSources), RecoveryStateKind.DegradedBackend },
        { nameof(RecoveryPacketFixtures.DegradedBackendNoSources), RecoveryStateKind.DegradedBackend },
        { nameof(RecoveryPacketFixtures.ConflictingViaDegraded), RecoveryStateKind.Conflicting },
        { nameof(RecoveryPacketFixtures.ConflictingViaUnavailableAxes), RecoveryStateKind.Conflicting },
        { nameof(RecoveryPacketFixtures.NoMatch), RecoveryStateKind.NoMatch },
        { nameof(RecoveryPacketFixtures.NotIngestedYet), RecoveryStateKind.NotIngestedYet },
        { nameof(RecoveryPacketFixtures.GraphGapNoSources), RecoveryStateKind.GraphGap },
        { nameof(RecoveryPacketFixtures.InsufficientFromPartial), RecoveryStateKind.InsufficientEvidence },
        { nameof(RecoveryPacketFixtures.InsufficientNoSignal), RecoveryStateKind.InsufficientEvidence },
        { nameof(RecoveryPacketFixtures.Compressed), RecoveryStateKind.Compressed },
        { nameof(RecoveryPacketFixtures.Unauthorized), RecoveryStateKind.Unauthorized },
        { nameof(RecoveryPacketFixtures.UnknownState), RecoveryStateKind.Unknown },
    };

    [Theory]
    [MemberData(nameof(ActionableStates))]
    public void Panel_EachState_RendersFullGrammarWithVisibleTextAndAccessibleNames(
        string fixtureName,
        RecoveryStateKind expected)
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(Resolve(fixtureName));

        IElement panel = component.Find("[data-testid='mem-evidence-recovery']");
        panel.GetAttribute("data-recovery-state").ShouldBe(expected.ToString());
        panel.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();

        // What happened / what it affects / how serious / the diagnostic clue — all visible, all non-empty.
        component.Find("[data-testid='mem-recovery-title']").TextContent.ShouldNotBeNullOrWhiteSpace();
        component.Find("[data-testid='mem-recovery-explanation']").TextContent.ShouldNotBeNullOrWhiteSpace();
        component.Find("[data-testid='mem-recovery-clue']").TextContent.ShouldContain("Diagnostic clue");

        // Severity and affected capability are conveyed as text-bearing accessible names, not by color alone.
        component.Markup.ShouldContain("aria-label=\"Severity: ");
        component.Markup.ShouldContain("aria-label=\"Affected capability: ");

        // No state may leak sensitive content into markup.
        string markup = component.Markup;
        markup.ShouldNotContain("Bearer ");
        markup.ShouldNotContain("C:\\Users\\Jerome");
        markup.ShouldNotContain("/home/jerome");
        markup.ShouldNotContain("redis://");
    }

    [Fact]
    public void Panel_Transition_UnauthorizedToAllowed_SwitchesFromAssertiveAlertToPoliteStatus()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Unauthorized());

        IElement before = component.Find("[data-testid='mem-evidence-recovery']");
        before.GetAttribute("data-recovery-state").ShouldBe(nameof(RecoveryStateKind.Unauthorized));
        before.GetAttribute("role").ShouldBe("alert");
        before.GetAttribute("aria-live").ShouldBe("assertive");

        component.Render(parameters => parameters.Add(p => p.Packet, RecoveryPacketFixtures.Weak()));

        IElement after = component.Find("[data-testid='mem-evidence-recovery']");
        after.GetAttribute("data-recovery-state").ShouldBe(nameof(RecoveryStateKind.Weak));
        after.GetAttribute("role").ShouldBe("status");
        after.GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Panel_Transition_CompleteToDegraded_RevealsConflictingState()
    {
        // A confident, complete answer renders nothing; degrading the backend must reveal the conflicting state.
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Supported());
        component.FindAll("[data-testid='mem-evidence-recovery']").ShouldBeEmpty();

        component.Render(parameters =>
            parameters.Add(p => p.Packet, RecoveryPacketFixtures.ConflictingViaDegraded()));

        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("data-recovery-state")
            .ShouldBe(nameof(RecoveryStateKind.Conflicting));
    }

    [Fact]
    public void Panel_Transition_ConflictingToResolved_HidesPanel()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.ConflictingViaDegraded());
        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("data-recovery-state")
            .ShouldBe(nameof(RecoveryStateKind.Conflicting));

        component.Render(parameters => parameters.Add(p => p.Packet, RecoveryPacketFixtures.Supported()));

        component.FindAll("[data-testid='mem-evidence-recovery']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_Transition_CompressedToExpanded_DropsOmittedDetailGrammar()
    {
        // Before expansion the omitted detail group is announced; after the host expands it (a complete
        // packet), the compression grammar is gone instead of silently lingering.
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());
        component.Find("[data-testid='mem-recovery-omitted']").TextContent.ShouldContain("rankedResults");

        component.Render(parameters => parameters.Add(p => p.Packet, RecoveryPacketFixtures.Supported()));

        component.FindAll("[data-testid='mem-evidence-recovery']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-recovery-omitted']").ShouldBeEmpty();
    }

    private static EvidencePacket Resolve(string fixtureName) => fixtureName switch
    {
        nameof(RecoveryPacketFixtures.Weak) => RecoveryPacketFixtures.Weak(),
        nameof(RecoveryPacketFixtures.StaleMemory) => RecoveryPacketFixtures.StaleMemory(),
        nameof(RecoveryPacketFixtures.DegradedBackendWithSources) => RecoveryPacketFixtures.DegradedBackendWithSources(),
        nameof(RecoveryPacketFixtures.DegradedBackendNoSources) => RecoveryPacketFixtures.DegradedBackendNoSources(),
        nameof(RecoveryPacketFixtures.ConflictingViaDegraded) => RecoveryPacketFixtures.ConflictingViaDegraded(),
        nameof(RecoveryPacketFixtures.ConflictingViaUnavailableAxes) => RecoveryPacketFixtures.ConflictingViaUnavailableAxes(),
        nameof(RecoveryPacketFixtures.NoMatch) => RecoveryPacketFixtures.NoMatch(),
        nameof(RecoveryPacketFixtures.NotIngestedYet) => RecoveryPacketFixtures.NotIngestedYet(),
        nameof(RecoveryPacketFixtures.GraphGapNoSources) => RecoveryPacketFixtures.GraphGapNoSources(),
        nameof(RecoveryPacketFixtures.InsufficientFromPartial) => RecoveryPacketFixtures.InsufficientFromPartial(),
        nameof(RecoveryPacketFixtures.InsufficientNoSignal) => RecoveryPacketFixtures.InsufficientNoSignal(),
        nameof(RecoveryPacketFixtures.Compressed) => RecoveryPacketFixtures.Compressed(),
        nameof(RecoveryPacketFixtures.Unauthorized) => RecoveryPacketFixtures.Unauthorized(),
        nameof(RecoveryPacketFixtures.UnknownState) => RecoveryPacketFixtures.UnknownState(),
        _ => throw new InvalidOperationException($"Unsupported fixture '{fixtureName}'."),
    };

    private IRenderedComponent<MemoriesRecoveryActionPanel> RenderPanel(EvidencePacket packet)
        => Render<MemoriesRecoveryActionPanel>(parameters => parameters.Add(p => p.Packet, packet));
}
