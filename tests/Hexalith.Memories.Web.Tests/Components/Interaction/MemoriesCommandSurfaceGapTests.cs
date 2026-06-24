// <copyright file="MemoriesCommandSurfaceGapTests.cs" company="ITANEO">
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

/// <summary>
/// QA gap coverage for the command surface: a stale contract-version snapshot must disable every command
/// (including tenant verification), an empty graph target must disable Open Graph without disabling
/// otherwise-valid commands, and the rendered surface must show the disabled reasons.
/// </summary>
public sealed class MemoriesCommandSurfaceGapTests : FrontComposerTestBase
{
    public MemoriesCommandSurfaceGapTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Map_ContractVersionMismatch_DisablesEveryCommandWithVersionReason()
    {
        IReadOnlyList<MemoriesCommandView> commands = MemoriesCommandSurfaceMapper.Map(
            EvidencePacketFixtures.CompletePacket(),
            InteractionContextTests.Snapshot() with { ContractVersion = "v2" },
            "tenant-a",
            "case-a");

        commands.ShouldNotBeEmpty();
        commands.ShouldAllBe(c => !c.IsAvailable);
        commands.ShouldAllBe(c =>
            c.DisabledReasonKey == InteractionResourceKeys.DisabledReason(InteractionContextValidationReason.ContractVersionMismatch));
    }

    [Fact]
    public void Map_EmptyGraph_DisablesOnlyOpenGraph()
    {
        EvidencePacket noGraph = EvidencePacketFixtures.CompletePacket();
        noGraph = noGraph with { Graph = noGraph.Graph with { Available = false, RelatedPath = [], EdgeTypes = [] } };

        IReadOnlyList<MemoriesCommandView> commands = MemoriesCommandSurfaceMapper.Map(
            noGraph,
            InteractionContextTests.Snapshot(),
            "tenant-a",
            "case-a");

        MemoriesCommandView openGraph = commands.Single(c => c.Kind == MemoriesCommandKind.OpenGraph);
        openGraph.IsAvailable.ShouldBeFalse();
        openGraph.DisabledReasonKey.ShouldBe(InteractionResourceKeys.DisabledReason(InteractionContextValidationReason.MissingTarget));

        commands.Single(c => c.Kind == MemoriesCommandKind.InspectSource).IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void Surface_ContractVersionMismatch_RendersAllDisabledWithReasons()
    {
        IRenderedComponent<MemoriesCommandSurface> component = Render<MemoriesCommandSurface>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())
            .Add(p => p.Snapshot, InteractionContextTests.Snapshot() with { ContractVersion = "v2" })
            .Add(p => p.ActiveTenantId, "tenant-a")
            .Add(p => p.ActiveCaseId, "case-a"));

        component.FindAll("[data-testid='mem-command-action']")
            .ShouldAllBe(e => e.HasAttribute("disabled"));
        component.FindAll("[data-testid='mem-command-disabled-reason']").ShouldNotBeEmpty();
    }
}
