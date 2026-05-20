// <copyright file="EvidenceCockpitTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;

using Shouldly;

public sealed class EvidenceCockpitTests : FrontComposerTestBase
{
    [Fact]
    public void MemoriesEvidenceCockpit_CompletePacket_ShouldRenderScopeBeforeResultContent()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket()));

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-evidence-scope\"", StringComparison.Ordinal).ShouldBeLessThan(
            markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal));
        component.Find("[data-testid='mem-evidence-scope']").TextContent.ShouldContain("tenant-a");
        component.Find("[data-testid='mem-evidence-scope']").TextContent.ShouldContain("case-a");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_LoadingState_ShouldKeepScopeBeforeStatusContent()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.IsLoading, true));

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-evidence-scope\"", StringComparison.Ordinal).ShouldBeLessThan(
            markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal));
        component.Find("[data-testid='mem-evidence-result']").TextContent.ShouldContain("Loading evidence");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_ErrorState_ShouldKeepScopeAndSanitizeMessage()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.ErrorMessage, "Failed at C:\\Users\\Jerome\\secret.txt with Bearer abc.def.ghi"));

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-evidence-scope\"", StringComparison.Ordinal).ShouldBeLessThan(
            markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal));
        component.Markup.ShouldContain("Evidence unavailable");
        component.Markup.ShouldNotContain("Bearer ");
        component.Markup.ShouldNotContain("C:\\");
    }

    [Fact]
    public void MemoriesTrustStrip_States_ShouldRenderVisibleLabelsAndAccessibleNames()
    {
        IRenderedComponent<MemoriesTrustStrip> component = Render<MemoriesTrustStrip>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket()));

        component.Markup.ShouldContain("aria-label=\"Confidence: strong\"");
        component.Markup.ShouldContain("aria-label=\"Freshness: unknown\"");
        component.Markup.ShouldContain("aria-label=\"Evidence health: pending expansion\"");
        component.Markup.ShouldContain("aria-label=\"Token budget: compressed\"");
        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("1 source");
    }

    [Fact]
    public void EvidencePacketViewMapping_RenderedFields_ShouldHaveNamedContractSources()
    {
        IReadOnlyList<EvidencePacketFieldMapping> mappings = EvidencePacketViewMapping.RenderedFields;

        mappings.ShouldContain(static x => x.DisplayField == "trust.confidence" && x.ContractSource == "EvidencePacket.Evidence.EvidenceStrength");
        mappings.ShouldContain(static x => x.DisplayField == "trust.evidenceHealth" && x.ContractSource == "EvidencePacket.State");
        mappings.ShouldContain(static x => x.DisplayField == "scope.tenant" && x.ContractSource == "EvidencePacket.Scope.TenantId");
        mappings.ShouldContain(static x => x.DisplayField == "sources.originIdentifier" && x.ContractSource == "EvidencePacket.Sources[].SourceUri");
        mappings.ShouldContain(static x => x.DisplayField == "axes.rankingReason" && x.ContractSource == "EvidencePacket.Evidence.AxisEvidence[].Description");
        mappings.ShouldContain(static x => x.DisplayField == "graph.gapMarkers" && x.ContractSource == "EvidencePacket.Graph.GapMarkers");
        mappings.ShouldAllBe(static x => !string.IsNullOrWhiteSpace(x.UnavailableFallback));
    }

    [Fact]
    public void MemoriesEvidenceCockpit_RestrictedPacket_ShouldDisplayRestrictiveStateBeforeEvidence()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket()));

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-evidence-restrictive-state\"", StringComparison.Ordinal).ShouldBeLessThan(
            markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal));
        component.Markup.ShouldNotContain("memory-secret");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_Details_ShouldPreservePacketOrderingAndUnavailableOrderBasis()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.MultiSourcePacket()));

        component.FindAll("[data-testid='mem-source-item']").Select(x => x.GetAttribute("data-source-rank")).ShouldBe(["1", "2"]);
        component.FindAll("[data-testid='mem-axis-item']").Select(x => x.GetAttribute("data-axis")).ShouldBe(["semantic", "syntactic"]);
        component.Find("[data-testid='mem-source-order-basis']").TextContent.ShouldContain("packet order");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_SensitivePacket_ShouldNotRenderRawPathsTokensOrRestrictedDiagnostics()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.SensitivePacket()));

        component.Markup.ShouldNotContain("Bearer ");
        component.Markup.ShouldNotContain("C:\\");
        component.Markup.ShouldNotContain("/home/");
        component.Markup.ShouldNotContain("redis://");
        component.Markup.ShouldContain("redacted source");
    }
}
