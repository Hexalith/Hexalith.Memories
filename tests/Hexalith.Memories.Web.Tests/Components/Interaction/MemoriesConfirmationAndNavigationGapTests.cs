// <copyright file="MemoriesConfirmationAndNavigationGapTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// QA gap coverage for confirmation accept/cancel transitions, tenant-wide confirmation copy, confirmation
/// mapper guards, navigation context sanitization, and the stale-navigation disabled-reason surface.
/// </summary>
public sealed class MemoriesConfirmationAndNavigationGapTests : FrontComposerTestBase
{
    public MemoriesConfirmationAndNavigationGapTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void ConfirmationPromptMapper_NullCommand_Throws()
        => Should.Throw<ArgumentNullException>(
            () => ConfirmationPromptMapper.Map(null!, InteractionContextTests.Snapshot()));

    [Fact]
    public void ConfirmationPromptMapper_NullSnapshot_Throws()
        => Should.Throw<ArgumentNullException>(
            () => ConfirmationPromptMapper.Map(ExportCommand(), null!));

    [Fact]
    public void ConfirmationPromptMapper_TenantWideCase_NamesTenantWide()
    {
        ConfirmationPrompt prompt = ConfirmationPromptMapper.Map(
            ExportCommand(),
            InteractionContextTests.Snapshot() with { CaseId = null });

        prompt.BodyLines.ShouldContain(line => line.Contains("tenant-wide", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Confirmation_ConfirmClick_InvokesOnConfirm()
    {
        bool confirmed = false;
        bool cancelled = false;
        IRenderedComponent<MemoriesActionConfirmation> component = Render<MemoriesActionConfirmation>(parameters => parameters
            .Add(p => p.Command, ExportCommand())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.OnConfirm, () => confirmed = true)
            .Add(p => p.OnCancel, () => cancelled = true));

        await component.InvokeAsync(() => component.Find("[data-testid='fc-destructive-confirm']").Click());

        confirmed.ShouldBeTrue();
        cancelled.ShouldBeFalse();
    }

    [Fact]
    public async Task Confirmation_CancelClick_InvokesOnCancel()
    {
        bool confirmed = false;
        bool cancelled = false;
        IRenderedComponent<MemoriesActionConfirmation> component = Render<MemoriesActionConfirmation>(parameters => parameters
            .Add(p => p.Command, ExportCommand())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.OnConfirm, () => confirmed = true)
            .Add(p => p.OnCancel, () => cancelled = true));

        await component.InvokeAsync(() => component.Find("[data-testid='fc-destructive-cancel']").Click());

        cancelled.ShouldBeTrue();
        confirmed.ShouldBeFalse();
    }

    [Fact]
    public void Navigation_SensitiveSnapshot_IsRedacted()
    {
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot() with
            {
                TenantId = "tenant Bearer leaked-token",
                CaseId = "C:\\Users\\Jerome\\case.txt",
                Query = "redis://localhost:6379",
                ReturnRoute = "/home/jerome/return-secret",
            })
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a"));

        string markup = component.Markup;
        markup.ShouldContain("[REDACTED]");
        markup.ShouldNotContain("Bearer ");
        markup.ShouldNotContain("C:\\");
        markup.ShouldNotContain("redis://");
        markup.ShouldNotContain("/home/jerome");
    }

    [Fact]
    public void Navigation_SensitiveReturnRoute_IsRedactedInDataAttribute()
    {
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot() with
            {
                ReturnRoute = "C:\\Users\\Jerome\\return.txt",
            })
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a"));

        // The diagnostic data-return-route attribute is a snapshot/DOM surface and must share the same
        // redaction path as the visible return-path label.
        string returnRoute = component.Find("[data-testid='mem-context-navigation']").GetAttribute("data-return-route")!;
        returnRoute.ShouldContain("[REDACTED]");
        returnRoute.ShouldNotContain("C:\\");
        component.Markup.ShouldNotContain("C:\\");
    }

    [Fact]
    public void Navigation_StaleContext_RendersDisabledReason()
    {
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-b")
            .Add(p => p.ActiveCaseId, "case-a"));

        component.Find("[data-testid='mem-context-navigation']").GetAttribute("data-valid").ShouldBe("false");
        component.Find("[data-testid='mem-navigation-disabled-reason']").TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    private static MemoriesCommandView ExportCommand()
        => MemoriesCommandSurfaceMapper.Map(
                EvidencePacketFixtures.CompletePacket(),
                InteractionContextTests.Snapshot(),
                "tenant-a",
                "case-a")
            .Single(c => c.Kind == MemoriesCommandKind.ExportPacket);
}
