// <copyright file="MemoriesCommandSurfaceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

public sealed class MemoriesCommandSurfaceTests : FrontComposerTestBase
{
    public MemoriesCommandSurfaceTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Map_CompletePacket_ExposesRequiredCommands()
    {
        IReadOnlyList<MemoriesCommandView> commands = MemoriesCommandSurfaceMapper.Map(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot(),
            "tenant-a",
            "case-a");

        commands.Select(static c => c.Kind).ShouldBe(Enum.GetValues<MemoriesCommandKind>());
        commands.Single(static c => c.Kind == MemoriesCommandKind.ExportPacket).RequiresConfirmation.ShouldBeTrue();
        commands.Single(static c => c.Kind == MemoriesCommandKind.RetryIngestion).RequiresConfirmation.ShouldBeTrue();
        commands.ShouldAllBe(static c => c.IsAvailable || c.Kind == MemoriesCommandKind.OpenGraph);
    }

    [Fact]
    public void Map_UnauthorizedPacket_AllowsVerifyTenantButDisablesUnsafeActions()
    {
        IReadOnlyList<MemoriesCommandView> commands = MemoriesCommandSurfaceMapper.Map(
            EvidencePacketFixtures.UnauthorizedPacket(),
            InteractionContextTests.Snapshot() with { TargetKind = InteractionTargetKind.Packet, TargetId = null },
            "tenant-a",
            "case-a");

        commands.Single(static c => c.Kind == MemoriesCommandKind.VerifyTenant).IsAvailable.ShouldBeTrue();
        commands.Single(static c => c.Kind == MemoriesCommandKind.ExportPacket).IsAvailable.ShouldBeFalse();
        commands.Single(static c => c.Kind == MemoriesCommandKind.InspectSource).IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Surface_AvailableCommand_EmitsScopedInvocation()
    {
        MemoriesCommandInvocation? invocation = null;
        IRenderedComponent<MemoriesCommandSurface> component = Render<MemoriesCommandSurface>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnCommand, (MemoriesCommandInvocation i) => invocation = i));

        component.FindAll("[data-testid='mem-command-action']")
            .Single(e => e.GetAttribute("data-command-kind") == nameof(MemoriesCommandKind.InspectSource))
            .Click();

        invocation.ShouldNotBeNull();
        invocation!.TenantId.ShouldBe("tenant-a");
        invocation.Target.ShouldBe("memory-a");
        invocation.ReturnRoute.ShouldBe("memories/evidence?packet=memory-a");
    }

    [Fact]
    public void Surface_DisabledCommand_DoesNotEmit()
    {
        MemoriesCommandInvocation? invocation = null;
        IRenderedComponent<MemoriesCommandSurface> component = Render<MemoriesCommandSurface>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot())
            .Add(p => p.ActiveTenantId, "tenant-b")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnCommand, (MemoriesCommandInvocation i) => invocation = i));

        component.FindAll("[data-testid='mem-command-action']")
            .Single(e => e.GetAttribute("data-command-kind") == nameof(MemoriesCommandKind.Search))
            .Click();

        invocation.ShouldBeNull();
        component.FindAll("[data-testid='mem-command-disabled-reason']").ShouldNotBeEmpty();
    }
}
