// <copyright file="MemoriesConfirmationAndNavigationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class MemoriesConfirmationAndNavigationTests : FrontComposerTestBase
{
    public MemoriesConfirmationAndNavigationTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Confirmation_NamesTenantCaseTargetConsequenceAndRecovery()
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

        string body = component.Find("[data-testid='fc-destructive-dialog']").TextContent;
        body.ShouldContain("Tenant: tenant-a");
        body.ShouldContain("Case: case-a");
        body.ShouldContain("Object: evidence-packet");
        body.ShouldContain("Consequence:");
        body.ShouldContain("Recovery:");
    }

    [Fact]
    public void Confirmation_RedactsSensitiveContext()
    {
        MemoriesCommandView command = new(
            MemoriesCommandKind.ExportPacket,
            InteractionResourceKeys.Command(MemoriesCommandKind.ExportPacket),
            "Bearer token C:\\secret.txt",
            true,
            null,
            true,
            InteractionResourceKeys.CommandConsequence(MemoriesCommandKind.ExportPacket),
            InteractionResourceKeys.CommandRecovery(MemoriesCommandKind.ExportPacket));

        IRenderedComponent<MemoriesActionConfirmation> component = Render<MemoriesActionConfirmation>(parameters => parameters
            .Add(p => p.Command, command)
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot() with
            {
                TenantId = "tenant Bearer leaked",
                CaseId = "C:\\Users\\Jerome\\case.txt",
            }));

        string markup = component.Markup;
        markup.ShouldNotContain("Bearer ");
        markup.ShouldNotContain("C:\\");
        markup.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void Navigation_ValidContext_EmitsOpenAndReturn()
    {
        InteractionContextSnapshot? opened = null;
        string? returned = null;
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnOpen, (InteractionContextSnapshot s) => opened = s)
            .Add(p => p.OnReturn, (string route) => returned = route));

        component.Find("[data-testid='mem-navigation-open']").Click();
        component.Find("[data-testid='mem-navigation-return-action']").Click();

        opened.ShouldBe(InteractionContextTests.Snapshot());
        returned.ShouldBe("memories/evidence?packet=memory-a");
    }

    [Fact]
    public void Navigation_StaleContext_DisablesOpenButPreservesReturnPath()
    {
        InteractionContextSnapshot? opened = null;
        string? returned = null;
        IRenderedComponent<MemoriesContextNavigation> component = Render<MemoriesContextNavigation>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-b")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnOpen, (InteractionContextSnapshot s) => opened = s)
            .Add(p => p.OnReturn, (string route) => returned = route));

        component.Find("[data-testid='mem-context-navigation']").GetAttribute("data-valid").ShouldBe("false");
        component.Find("[data-testid='mem-navigation-open']").Click();
        component.Find("[data-testid='mem-navigation-return-action']").Click();

        opened.ShouldBeNull();
        returned.ShouldBe("memories/evidence?packet=memory-a");
    }
}
