// <copyright file="EvidenceCockpitTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;

using Shouldly;

public sealed class EvidenceCockpitTests : FrontComposerTestBase
{
    public EvidenceCockpitTests()
    {
        Host.ValidateVersionAlignment();
    }

    [Fact]
    public void MemoriesEvidenceCockpit_CompletePacket_ShouldRenderScopeBeforeResultContent()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket()));

        AssertScopeBeforeResult(component);
        component.Find("[data-testid='mem-evidence-scope']").TextContent.ShouldContain("tenant-a");
        component.Find("[data-testid='mem-evidence-scope']").TextContent.ShouldContain("case-a");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_LoadingState_ShouldKeepScopeBeforeStatusAndShowUnavailableTrustValues()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.IsLoading, true));

        AssertScopeBeforeResult(component);
        component.Find("[data-testid='mem-evidence-loading']").TextContent.ShouldContain("Loading evidence");
        component.Find("[data-testid='mem-trust-strip']").GetAttribute("data-strip-mode").ShouldBe("Loading");
        component.Markup.ShouldContain("Confidence: Loading");
        component.Markup.ShouldContain("Evidence health: Loading");
        component.Markup.ShouldContain("Token budget: Unavailable");
        // No subordinate evidence detail should render during loading.
        component.FindAll("[data-testid='mem-source-stack']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-axis-breakdown']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-graph-summary']").ShouldBeEmpty();
    }

    [Fact]
    public void MemoriesEvidenceCockpit_ErrorState_ShouldKeepScopeSuppressDetailsAndSanitizeMessage()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.ErrorMessage, "Failed at C:\\Users\\Jerome\\secret.txt with Bearer abc.def.ghi"));

        AssertScopeBeforeResult(component);
        IElement error = component.Find("[data-testid='mem-evidence-error']");
        error.TextContent.ShouldContain("Evidence unavailable");
        error.TextContent.ShouldNotContain("Bearer ");
        error.TextContent.ShouldNotContain("C:\\");
        // Suppress evidence children on error to avoid duplicated stub messages.
        component.FindAll("[data-testid='mem-source-stack']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-axis-breakdown']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-graph-summary']").ShouldBeEmpty();
    }

    [Fact]
    public void MemoriesTrustStrip_CompressedPacket_ShouldRenderVisibleLabelsAndAccessibleNames()
    {
        IRenderedComponent<MemoriesTrustStrip> component = Render<MemoriesTrustStrip>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket())
            .Add(p => p.Mode, MemoriesTrustStrip.TrustStripMode.Packet));

        component.Markup.ShouldContain("aria-label=\"Confidence: Strong\"");
        component.Markup.ShouldContain($"aria-label=\"Freshness: {EvidenceDisplay.FreshnessLabel(EvidencePacketFixtures.CompressedPacket())}\"");
        component.Markup.ShouldContain("aria-label=\"Evidence health: Pending expansion\"");
        component.Markup.ShouldContain("aria-label=\"Token budget: compressed\"");
        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("1 source");
    }

    [Fact]
    public void MemoriesTrustStrip_AcronymEnumValues_ShouldPreserveAcronyms()
    {
        EvidenceDisplay.Label(SampleAcronymEnum.MCP).ShouldBe("MCP");
        EvidenceDisplay.Label(SampleAcronymEnum.MCPHandler).ShouldBe("MCP handler");
        EvidenceDisplay.Label(SampleAcronymEnum.PendingExpansion).ShouldBe("Pending expansion");
        EvidenceDisplay.Label(SampleAcronymEnum.Strong).ShouldBe("Strong");
    }

    [Fact]
    public void EvidencePacketViewMapping_RenderedFields_ShouldHaveNamedContractSourcesOrExplicitNoSource()
    {
        IReadOnlyList<EvidencePacketFieldMapping> mappings = EvidencePacketViewMapping.RenderedFields;

        mappings.ShouldAllBe(static x => !string.IsNullOrWhiteSpace(x.DisplayField));
        mappings.ShouldAllBe(static x => !string.IsNullOrWhiteSpace(x.ContractSource));
        mappings.ShouldAllBe(static x => !string.IsNullOrWhiteSpace(x.UnavailableFallback));

        EvidencePacketFieldMapping freshness = mappings.Single(static x => x.DisplayField == "trust.freshness");
        freshness.ContractSource.ShouldContain("EvidencePacket.Metadata.Freshness");
        freshness.UnavailableFallback.ShouldBe(EvidenceDisplay.FreshnessUnavailable);

        // Every UI display field shown to operators must be tracked.
        string[] requiredFields =
        [
            "scope.tenant", "scope.case", "scope.isolation",
            "trust.confidence", "trust.freshness", "trust.sourceCount", "trust.evidenceHealth", "trust.tokenBudget",
            "result.query", "result.summary",
            "sources.originIdentifier", "sources.type", "sources.snippet", "sources.memoryUnit", "sources.rank", "sources.score",
            "sources.timestamp", "sources.freshness",
            "axes.axis", "axes.normalizedScore", "axes.rankingReason", "axes.normalizationMethod", "axes.unavailableAxes", "axes.caveat",
            "graph.path", "graph.edgeTypes", "graph.gapMarkers",
            "recovery.label", "recovery.guidance",
        ];
        foreach (string field in requiredFields)
        {
            mappings.ShouldContain(m => m.DisplayField == field, $"Mapping table missing entry for '{field}'.");
        }
    }

    [Fact]
    public void MemoriesEvidenceCockpit_UnauthorizedPacket_ShouldScrubSourceAxisAndGraphContent()
    {
        // Fixture intentionally keeps sources/graph populated so a missing scope guard would leak content.
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket()));

        component.Find("[data-testid='mem-evidence-restrictive-state']").GetAttribute("data-restrictive-kind").ShouldBe("unauthorized");
        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-evidence-restrictive-state\"", StringComparison.Ordinal).ShouldBeLessThan(
            markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal));

        // Children must be suppressed entirely — neither the data nor the empty-state placeholders.
        component.FindAll("[data-testid='mem-source-stack']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-axis-breakdown']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-graph-summary']").ShouldBeEmpty();

        // Hard negative: source URIs, snippets, graph nodes, axis names must not appear anywhere.
        markup.ShouldNotContain("memory-secret");
        markup.ShouldNotContain("memory-secret-a");
        markup.ShouldNotContain("memory-secret-b");
        markup.ShouldNotContain("secret-supports");
        markup.ShouldNotContain("secret-gap");
        markup.ShouldNotContain("secret-axis-evidence");
        markup.ShouldNotContain("https://docs.example/restricted");

        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("sources unavailable");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_UnknownIsolation_ShouldTreatScopeRestrictively()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.UnknownScopePacket()));

        component.Find("[data-testid='mem-evidence-restrictive-state']").GetAttribute("data-restrictive-kind").ShouldBe("unauthorized");
        component.FindAll("[data-testid='mem-source-stack']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-axis-breakdown']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-graph-summary']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(nameof(EvidencePacketFixtures.DegradedPacket), "missing-source")]
    [InlineData(nameof(EvidencePacketFixtures.PartialPacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.WeakPacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.StalePacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.EmptyPacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.RedactedPacket), "redacted")]
    [InlineData(nameof(EvidencePacketFixtures.CompressedPacket), "compressed")]
    public void MemoriesEvidenceCockpit_RestrictiveStatePrecedence_ShouldSelectExpectedKind(string fixtureName, string expectedKind)
    {
        EvidencePacket packet = fixtureName switch
        {
            nameof(EvidencePacketFixtures.DegradedPacket) => EvidencePacketFixtures.DegradedPacket(),
            nameof(EvidencePacketFixtures.PartialPacket) => EvidencePacketFixtures.PartialPacket(),
            nameof(EvidencePacketFixtures.WeakPacket) => EvidencePacketFixtures.WeakPacket(),
            nameof(EvidencePacketFixtures.StalePacket) => EvidencePacketFixtures.StalePacket(),
            nameof(EvidencePacketFixtures.EmptyPacket) => EvidencePacketFixtures.EmptyPacket(),
            nameof(EvidencePacketFixtures.RedactedPacket) => EvidencePacketFixtures.RedactedPacket(),
            nameof(EvidencePacketFixtures.CompressedPacket) => EvidencePacketFixtures.CompressedPacket(),
            _ => throw new InvalidOperationException($"Unsupported fixture '{fixtureName}'."),
        };

        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, packet));

        component.Find("[data-testid='mem-evidence-restrictive-state']").GetAttribute("data-restrictive-kind").ShouldBe(expectedKind);
    }

    [Fact]
    public void MemoriesEvidenceCockpit_MultiSourcePacket_ShouldPreservePacketOrderingAndOrderBasis()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.MultiSourcePacket()));

        component.FindAll("[data-testid='mem-source-item']").Select(x => x.GetAttribute("data-source-rank")).ShouldBe(["1", "2"]);
        component.FindAll("[data-testid='mem-axis-item']").Select(x => x.GetAttribute("data-axis")).ShouldBe(["semantic", "syntactic"]);
        component.FindAll("[data-testid='mem-graph-node']").Select(x => x.TextContent.Trim()).ShouldBe(["memory-a", "memory-b"]);
        component.Find("[data-testid='mem-source-order-basis']").TextContent.ShouldContain("packet order");
        component.Find("[data-testid='mem-axis-order-basis']").TextContent.ShouldContain("packet order");
        component.Find("[data-testid='mem-graph-order-basis']").TextContent.ShouldContain("packet order");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_SensitivePacket_ShouldNotRenderRawPathsTokensOrRestrictedDiagnostics()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.SensitivePacket()));

        string markup = component.Markup;
        markup.ShouldNotContain("Bearer ");
        markup.ShouldNotContain("C:\\Users\\Jerome");
        markup.ShouldNotContain("/home/jerome");
        markup.ShouldNotContain("redis://localhost");
        markup.ShouldContain(EvidenceDisplay.RedactedMarker);
        // Non-sensitive context surrounding the redaction must survive: axis description and graph nodes
        // never matched the regex and stay in the rendered markup.
        markup.ShouldContain("semantic vector match");
        markup.ShouldContain("memory-a");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_TenantCaseSensitivePacket_ShouldSanitizeScopeIdentifiers()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.TenantCaseSensitivePacket()));

        IElement scope = component.Find("[data-testid='mem-evidence-scope']");
        scope.TextContent.ShouldNotContain("Bearer ");
        scope.TextContent.ShouldNotContain("C:\\Users\\Jerome");
        scope.TextContent.ShouldContain(EvidenceDisplay.RedactedMarker);
    }

    [Fact]
    public void MemoriesEvidenceCockpit_RecoveryActions_ShouldRenderTenantCaseAndTargetContext()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket()));

        IElement recovery = component.Find("[data-testid='mem-recovery-item']");
        recovery.GetAttribute("data-recovery-kind").ShouldBe("IncreaseTokenBudget");
        recovery.TextContent.ShouldContain("increaseTokenBudget");
        recovery.TextContent.ShouldContain("Re-run with a larger tokenBudget.");
        recovery.TextContent.ShouldContain("tenant-a");
        recovery.TextContent.ShouldContain("case-a");
        recovery.TextContent.ShouldContain("rankedResults");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_SafeTextScoreLabel_ShouldHandleNonFiniteAndExtremeScores()
    {
        EvidenceDisplay.ScoreLabel(null).ShouldBe("score unavailable");
        EvidenceDisplay.ScoreLabel(double.NaN).ShouldBe("score unavailable");
        EvidenceDisplay.ScoreLabel(double.PositiveInfinity).ShouldBe("score unavailable");
        EvidenceDisplay.ScoreLabel(double.NegativeInfinity).ShouldBe("score unavailable");
        EvidenceDisplay.ScoreLabel(0.123d).ShouldBe("0.123");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_SafeText_ShouldPreserveNonSensitiveSurroundings()
    {
        const string Input = "Loaded model OK with Bearer abc.def.ghi for tenant.";
        string result = EvidenceDisplay.SafeText(Input);

        result.ShouldStartWith("Loaded model OK with ");
        result.ShouldContain(EvidenceDisplay.RedactedMarker);
        result.ShouldEndWith(" for tenant.");
    }

    [Fact]
    public void MemoriesSourceCitationStack_KeyboardReachability_ShouldNotDependOnHover()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.MultiSourcePacket()));

        // Every interactive child must be focusable via Tab (no negative tabindex, no aria-hidden ancestor).
        foreach (IElement item in component.FindAll("[data-testid='mem-source-item']"))
        {
            string? tabIndex = item.GetAttribute("tabindex");
            (tabIndex is null || int.Parse(tabIndex, System.Globalization.CultureInfo.InvariantCulture) >= 0).ShouldBeTrue();
            item.GetAttribute("aria-hidden").ShouldBeNull();
        }

        component.Find("[data-testid='mem-source-stack']").GetAttribute("aria-hidden").ShouldBeNull();
        component.Find("[data-testid='mem-axis-breakdown']").GetAttribute("aria-hidden").ShouldBeNull();
        component.Find("[data-testid='mem-graph-summary']").GetAttribute("aria-hidden").ShouldBeNull();
    }

    [Fact]
    public void MemoriesScopeHeader_ShouldExposeStableIsolationSelector()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket()));

        component.Find("[data-testid='mem-scope-isolation']").ShouldNotBeNull();
        component.Find("[data-testid='mem-scope-tenant']").TextContent.ShouldContain("tenant-a");
        component.Find("[data-testid='mem-scope-case']").TextContent.ShouldContain("case-a");
    }

    private static void AssertScopeBeforeResult<TComponent>(IRenderedComponent<TComponent> component)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        string markup = component.Markup;
        int scope = markup.IndexOf("data-testid=\"mem-evidence-scope\"", StringComparison.Ordinal);
        int result = markup.IndexOf("data-testid=\"mem-evidence-result\"", StringComparison.Ordinal);
        scope.ShouldBeGreaterThanOrEqualTo(0);
        result.ShouldBeGreaterThanOrEqualTo(0);
        scope.ShouldBeLessThan(result);
    }

    private enum SampleAcronymEnum
    {
        MCP,
        MCPHandler,
        PendingExpansion,
        Strong,
    }
}
