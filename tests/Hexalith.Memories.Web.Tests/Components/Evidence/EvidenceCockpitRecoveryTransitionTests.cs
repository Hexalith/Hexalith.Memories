// <copyright file="EvidenceCockpitRecoveryTransitionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

/// <summary>
/// QA gap coverage for the recovery panel composed inside <see cref="MemoriesEvidenceCockpit"/>: the
/// loading-to-result transition that reveals the panel, and the dual-announcement wiring where an
/// unauthorized packet renders an assertive restrictive banner together with a politely announced
/// recovery panel so the two live regions do not compete.
/// </summary>
public sealed class EvidenceCockpitRecoveryTransitionTests : FrontComposerTestBase
{
    public EvidenceCockpitRecoveryTransitionTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Cockpit_LoadingToResultTransition_RevealsRecoveryPanel()
    {
        // While loading, no real packet exists, so the recovery panel must not render.
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.IsLoading, true));

        component.FindAll("[data-testid='mem-evidence-recovery']").ShouldBeEmpty();

        // Once the packet arrives, the recovery panel appears for the resolved state.
        component.Render(parameters => parameters
            .Add(p => p.IsLoading, false)
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket()));

        component.Find("[data-testid='mem-evidence-recovery']").GetAttribute("data-recovery-state")
            .ShouldBe(nameof(RecoveryStateKind.Compressed));
    }

    [Fact]
    public void Cockpit_UnauthorizedPacket_RecoveryPanelAnnouncesPolitelyBesideAssertiveBanner()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket()));

        // The restrictive banner owns the single assertive alert.
        IElement banner = component.Find("[data-testid='mem-evidence-restrictive-state']");
        banner.GetAttribute("role").ShouldBe("alert");

        // The recovery panel announces politely (role=status) so it does not compete with the banner,
        // even though the unauthorized state would announce assertively when rendered standalone.
        IElement recovery = component.Find("[data-testid='mem-evidence-recovery']");
        recovery.GetAttribute("data-recovery-state").ShouldBe(nameof(RecoveryStateKind.Unauthorized));
        recovery.GetAttribute("role").ShouldBe("status");
        recovery.GetAttribute("aria-live").ShouldBe("polite");

        // The only surfaced action is the safe authorization check, and no restricted content leaks.
        recovery.QuerySelector("[data-testid='mem-recovery-primary'] [data-testid='mem-recovery-item']")!
            .GetAttribute("data-recovery-kind")
            .ShouldBe(nameof(Hexalith.Memories.Contracts.V1.EvidencePacketRecoveryKind.CheckAuthorization));
        recovery.TextContent.ShouldNotContain("memory-secret");
        recovery.TextContent.ShouldNotContain("https://docs.example/restricted");
    }
}
