// <copyright file="RecoveryActionPanelTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Recovery;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Recovery;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

public sealed class RecoveryActionPanelTests : FrontComposerTestBase
{
    public RecoveryActionPanelTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Panel_CompressedPacket_RendersStateGrammarWithVisibleTextAndAccessibleNames()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());

        IElement panel = component.Find("[data-testid='mem-evidence-recovery']");
        panel.GetAttribute("data-recovery-state").ShouldBe(nameof(RecoveryStateKind.Compressed));
        panel.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();

        component.Find("[data-testid='mem-recovery-title']").TextContent.ShouldBe("Compressed evidence");
        component.Find("[data-testid='mem-recovery-explanation']").TextContent.ShouldContain("omitted");
        component.Find("[data-testid='mem-recovery-clue']").TextContent.ShouldContain("Diagnostic clue");

        // Severity and affected capability render as text-bearing badges with accessible names, not color alone.
        component.Markup.ShouldContain("aria-label=\"Severity: Caution\"");
        component.Markup.ShouldContain("aria-label=\"Affected capability: Detail completeness\"");
    }

    [Fact]
    public void Panel_NonCriticalState_AnnouncesPolitely()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());

        IElement panel = component.Find("[data-testid='mem-evidence-recovery']");
        panel.GetAttribute("role").ShouldBe("status");
        panel.GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Panel_UnauthorizedState_AnnouncesAssertivelyWhenStandalone()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Unauthorized());

        IElement panel = component.Find("[data-testid='mem-evidence-recovery']");
        panel.GetAttribute("data-recovery-state").ShouldBe(nameof(RecoveryStateKind.Unauthorized));
        panel.GetAttribute("role").ShouldBe("alert");
        panel.GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Fact]
    public void Panel_UnauthorizedState_DoesNotLeakRestrictedContent()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Unauthorized());

        string markup = component.Markup;
        markup.ShouldNotContain("memory-secret");
        markup.ShouldNotContain("secret-supports");
        markup.ShouldNotContain("secret-gap");
        markup.ShouldNotContain("secret-axis-evidence");
        markup.ShouldNotContain("https://docs.example/restricted");

        // Only the safe authorization action is surfaced.
        component.Find("[data-testid='mem-recovery-primary']")
            .QuerySelector("[data-testid='mem-recovery-item']")!
            .GetAttribute("data-recovery-kind")
            .ShouldBe(nameof(EvidencePacketRecoveryKind.CheckAuthorization));
    }

    [Fact]
    public void Panel_RestrictiveScope_RendersScopeExpandingActionDisabledWithReason()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.UnauthorizedWithExpandingActions());

        IElement broaden = component.FindAll("[data-testid='mem-recovery-item']")
            .Single(e => e.GetAttribute("data-recovery-kind") == nameof(EvidencePacketRecoveryKind.BroadenScope));

        broaden.GetAttribute("data-availability").ShouldBe(nameof(RecoveryActionAvailability.Unavailable));
        broaden.QuerySelector("[data-testid='mem-recovery-disabled-reason']")!.TextContent
            .ShouldContain("Authorization required");

        // The disabled action is rendered, not hidden.
        broaden.QuerySelector("fluent-button")!.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Panel_MultipleActions_RendersSafestPrimaryBeforeSecondary()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.MultiActionNoMatch());

        IElement primary = component.Find("[data-testid='mem-recovery-primary'] [data-testid='mem-recovery-item']");
        primary.GetAttribute("data-recovery-kind").ShouldBe(nameof(EvidencePacketRecoveryKind.FetchMemoryUnit));

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-recovery-primary\"", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("data-testid=\"mem-recovery-secondary\"", StringComparison.Ordinal));

        component.Find("[data-testid='mem-recovery-secondary']")
            .QuerySelectorAll("[data-testid='mem-recovery-item']").Length.ShouldBe(2);
    }

    [Fact]
    public void Panel_RecoveryItem_RendersKindLabelGuidanceTenantCaseAndTarget()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());

        IElement item = component.Find("[data-testid='mem-recovery-item']");
        item.GetAttribute("data-recovery-kind").ShouldBe(nameof(EvidencePacketRecoveryKind.IncreaseTokenBudget));
        item.TextContent.ShouldContain("increaseTokenBudget");
        item.TextContent.ShouldContain("Re-run with a larger tokenBudget.");
        item.TextContent.ShouldContain("tenant-a");
        item.TextContent.ShouldContain("case-a");
        item.TextContent.ShouldContain("rankedResults");
    }

    [Fact]
    public void Panel_PrimaryActionClick_EmitsSanitizedIntent()
    {
        RecoveryActionInvocation? captured = null;
        IRenderedComponent<MemoriesRecoveryActionPanel> component = Render<MemoriesRecoveryActionPanel>(parameters => parameters
            .Add(p => p.Packet, RecoveryPacketFixtures.Compressed())
            .Add(p => p.OnRecoveryAction, (RecoveryActionInvocation i) => captured = i));

        component.Find("[data-testid='mem-recovery-primary'] [data-testid='mem-recovery-action-button']").Click();

        captured.ShouldNotBeNull();
        captured!.Kind.ShouldBe(EvidencePacketRecoveryKind.IncreaseTokenBudget);
        captured.Target.ShouldBe("rankedResults");
        captured.TenantId.ShouldBe("tenant-a");
        captured.CaseId.ShouldBe("case-a");
    }

    [Fact]
    public void Panel_DisabledActionClick_DoesNotEmitIntent()
    {
        RecoveryActionInvocation? captured = null;
        IRenderedComponent<MemoriesRecoveryActionPanel> component = Render<MemoriesRecoveryActionPanel>(parameters => parameters
            .Add(p => p.Packet, RecoveryPacketFixtures.UnauthorizedWithExpandingActions())
            .Add(p => p.OnRecoveryAction, (RecoveryActionInvocation i) => captured = i));

        IElement broaden = component.FindAll("[data-testid='mem-recovery-item']")
            .Single(e => e.GetAttribute("data-recovery-kind") == nameof(EvidencePacketRecoveryKind.BroadenScope));

        broaden.QuerySelector("[data-testid='mem-recovery-action-button']")!.Click();

        captured.ShouldBeNull();
    }

    [Fact]
    public void Panel_ConflictingEvidence_DoesNotRenderConfidentAnswer()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.ConflictingViaDegraded());

        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("data-recovery-state")
            .ShouldBe(nameof(RecoveryStateKind.Conflicting));
    }

    [Fact]
    public void Panel_CompressedEvidence_IsAnnouncedAsOmittedNotAbsent()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());

        component.Find("[data-testid='mem-recovery-clue']").TextContent.ShouldContain("omission=tokenBudget");
        component.Find("[data-testid='mem-recovery-explanation']").TextContent.ShouldContain("expanded");
    }

    [Fact]
    public void Panel_CompressedPacket_RendersOmittedDetailNamesAndExpansionGuidance()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Compressed());

        component.Find("[data-testid='mem-recovery-omitted-names']").TextContent.ShouldContain("rankedResults");

        IElement expansion = component.Find("[data-testid='mem-recovery-expansion']");
        expansion.GetAttribute("data-expansion-kind").ShouldBe(nameof(EvidencePacketRecoveryKind.IncreaseTokenBudget));
        expansion.TextContent.ShouldContain("rankedResults");
    }

    [Fact]
    public void Panel_Unauthorized_DoesNotRenderOmittedDetailHints()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Unauthorized());

        component.FindAll("[data-testid='mem-recovery-omitted']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_RiskMarkers_DecoratePrimaryStateWhenPresent()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.WeakAndCompressed());

        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("data-recovery-state")
            .ShouldBe(nameof(RecoveryStateKind.Weak));
        component.Find("[data-testid='mem-recovery-risk-markers']").TextContent.ShouldContain("Compressed");
    }

    [Fact]
    public void Panel_SensitiveRecoveryAction_RedactsSecretsPathsAndTokens()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.SensitiveRecoveryAction());

        string markup = component.Markup;
        markup.ShouldNotContain("Bearer ");
        markup.ShouldNotContain("C:\\Users\\Jerome");
        markup.ShouldNotContain("/home/jerome");
        markup.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void Panel_SensitiveScope_RedactsTenantAndCaseIdentifiers()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.SensitiveScopeRecovery());

        IElement item = component.Find("[data-testid='mem-recovery-item']");
        item.TextContent.ShouldNotContain("Bearer ");
        item.TextContent.ShouldNotContain("C:\\Users\\Jerome");
        item.TextContent.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void Panel_SupportedPacket_RendersNothing()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component = RenderPanel(RecoveryPacketFixtures.Supported());

        component.FindAll("[data-testid='mem-evidence-recovery']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_ActionControls_AreKeyboardReachable()
    {
        IRenderedComponent<MemoriesRecoveryActionPanel> component =
            RenderPanel(RecoveryPacketFixtures.MultiActionNoMatch());

        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("aria-hidden").ShouldBeNull();

        foreach (IElement button in component.FindAll("[data-testid='mem-recovery-action-button']"))
        {
            string? tabIndex = button.GetAttribute("tabindex");
            (tabIndex is null || int.Parse(tabIndex, System.Globalization.CultureInfo.InvariantCulture) >= 0).ShouldBeTrue();
            button.GetAttribute("aria-hidden").ShouldBeNull();
        }
    }

    [Fact]
    public void Localization_EveryRecoveryKeyResolves()
    {
        IStringLocalizer<Hexalith.Memories.Web.Resources.MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<Hexalith.Memories.Web.Resources.MemoriesWebResources>>();

        foreach (string key in AllResourceKeys())
        {
            LocalizedString value = localizer[key];
            value.ResourceNotFound.ShouldBeFalse($"Missing localization resource for key '{key}'.");
            value.Value.ShouldNotBeNullOrWhiteSpace();
        }
    }

    private static IEnumerable<string> AllResourceKeys()
    {
        yield return RecoveryResourceKeys.PanelLabel;
        yield return RecoveryResourceKeys.DiagnosticClueLabel;
        yield return RecoveryResourceKeys.SeverityLabel;
        yield return RecoveryResourceKeys.CapabilityLabel;
        yield return RecoveryResourceKeys.PrimaryActionLabel;
        yield return RecoveryResourceKeys.SecondaryActionsLabel;
        yield return RecoveryResourceKeys.RiskMarkersLabel;
        yield return RecoveryResourceKeys.NoAction;
        yield return RecoveryResourceKeys.DisabledAuthRequired;
        yield return RecoveryResourceKeys.TenantLabel;
        yield return RecoveryResourceKeys.CaseLabel;
        yield return RecoveryResourceKeys.TargetLabel;
        yield return RecoveryResourceKeys.TenantScope;
        yield return RecoveryResourceKeys.OmittedDetailsLabel;
        yield return RecoveryResourceKeys.ExpansionLabel;

        foreach (RecoveryStateKind kind in Enum.GetValues<RecoveryStateKind>())
        {
            yield return RecoveryResourceKeys.Title(kind);
            yield return RecoveryResourceKeys.Explanation(kind);
        }

        foreach (RecoverySeverity severity in Enum.GetValues<RecoverySeverity>())
        {
            yield return RecoveryResourceKeys.Severity(severity);
        }

        foreach (RecoveryCapability capability in Enum.GetValues<RecoveryCapability>())
        {
            yield return RecoveryResourceKeys.Capability(capability);
        }

        foreach (EvidencePacketRecoveryKind kind in Enum.GetValues<EvidencePacketRecoveryKind>())
        {
            yield return RecoveryResourceKeys.Action(kind);
        }

        yield return RecoveryResourceKeys.RiskMarker("Compressed");
        yield return RecoveryResourceKeys.RiskMarker("Stale");
        yield return RecoveryResourceKeys.RiskMarker("Degraded");
    }

    private IRenderedComponent<MemoriesRecoveryActionPanel> RenderPanel(EvidencePacket packet)
        => Render<MemoriesRecoveryActionPanel>(parameters => parameters.Add(p => p.Packet, packet));
}
