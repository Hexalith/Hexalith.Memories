// <copyright file="MemoriesCommandSurfaceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Interaction;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InspectSource_EmitsSelectedMemoryUnitWithoutRankOrUri(bool selectByUri)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        EvidencePacketSource first = packet.Sources[0];
        EvidencePacketSource selected = first with
        {
            Rank = 97,
            MemoryUnitId = "chosen-unit",
            SourceUri = "https://user:credential@example.test/chosen",
        };
        packet = packet with { Sources = [first, selected] };
        InteractionContextSnapshot snapshot = InteractionContextTests.Snapshot() with
        {
            TargetId = selectByUri ? selected.SourceUri : selected.MemoryUnitId,
        };
        MemoriesCommandInvocation? invocation = null;
        IRenderedComponent<MemoriesCommandSurface> component = Render<MemoriesCommandSurface>(parameters => parameters
            .Add(p => p.Packet, packet)
            .Add(p => p.Snapshot, snapshot)
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a")
            .Add(p => p.OnCommand, (MemoriesCommandInvocation value) => invocation = value));

        component.Find("[data-command-kind='InspectSource']").Click();

        invocation.ShouldBe(new MemoriesCommandInvocation(
            MemoriesCommandKind.InspectSource, "tenant-a", "case-a", "chosen-unit", snapshot.ReturnRoute));
        component.Markup.ShouldNotContain("credential");
        component.Markup.ShouldNotContain("chosen-unit");
        component.Find("[data-command-kind='InspectSource']").TextContent.Trim().ShouldBe("Inspect source");
        component.FindAll("[data-source-rank]").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("tenant-b", "case-a", "case-a", "memory-a")]
    [InlineData("tenant-a", "case-b", "case-a", "memory-a")]
    [InlineData("tenant-a", "case-a", "case-b", "memory-a")]
    [InlineData("tenant-a", "case-a", "case-a", "missing-unit")]
    public void InspectSource_InvalidScopeOrStaleSelection_HasNoActionableTarget(
        string activeTenant,
        string activeCase,
        string sourceCase,
        string selectedId)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with { Sources = [packet.Sources[0] with { CaseId = sourceCase }] };
        MemoriesCommandInvocation? invocation = null;
        IRenderedComponent<MemoriesCommandSurface> component = Render<MemoriesCommandSurface>(parameters => parameters
            .Add(p => p.Packet, packet)
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot() with { TargetId = selectedId })
            .Add(p => p.ActiveTenantId, activeTenant)
            .Add(p => p.ActiveCaseId, activeCase)
            .Add(p => p.OnCommand, (MemoriesCommandInvocation value) => invocation = value));

        component.Find("[data-command-kind='InspectSource']").HasAttribute("disabled").ShouldBeTrue();
        component.Find("[data-command-kind='InspectSource']").Click();
        invocation.ShouldBeNull();
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
