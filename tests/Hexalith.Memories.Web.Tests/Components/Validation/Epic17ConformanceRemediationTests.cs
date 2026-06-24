// <copyright file="Epic17ConformanceRemediationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Validation;

using System;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Tests.Components.Evidence;

using Shouldly;

/// <summary>
/// Story 17.6 — rendered-DOM gates for the Task 2/3 remediation <em>behaviour</em> (AC2, AC3).
/// <para>
/// <see cref="Epic17ConformanceTests"/> scans source and proves no raw control element or legacy token
/// <em>remains</em>; these specimen-level tests prove the replacement Fluent UI V5 primitives actually
/// <em>render</em> with the design-system semantics the hand-authored markup/CSS used to carry. The central
/// remediation — the restrictive precedence banner moving from a hand-authored <c>&lt;section role="alert"
/// class="mem-evidence-restrictive"&gt;</c> to a <c>FluentMessageBar</c> whose intent maps the precedence
/// ladder (unauthorized = Error, every other restrictive state = Warning) — had no behavioural coverage
/// before this story, so a regression that dropped the Fluent intent (or reintroduced the deleted status
/// CSS class) would have passed the previous suite.
/// </para>
/// </summary>
public sealed class Epic17ConformanceRemediationTests : Epic17ValidationTestBase
{
    private const string RestrictiveState = "[data-testid='mem-evidence-restrictive-state']";

    [Fact]
    public void RestrictiveBanner_Unauthorized_RendersFluentMessageBarWithErrorIntent()
    {
        string markup = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(c => c.Packet, EvidencePacketFixtures.UnauthorizedPacket())).Markup;

        IElement banner = QueryAll(markup, RestrictiveState).ShouldHaveSingleItem();
        banner.GetAttribute("data-restrictive-kind").ShouldBe("unauthorized");

        // The status banner is a Fluent message primitive, not hand-authored markup, and its intent carries
        // the most-restrictive (Error/red) precedence rather than a one-off CSS status colour.
        IElement messageBar = QueryAll(markup, $"{RestrictiveState} fluent-message-bar").ShouldHaveSingleItem();
        messageBar.GetAttribute("intent").ShouldBe("error");
        messageBar.TextContent.ShouldNotBeNullOrWhiteSpace("the restrictive banner rendered no message text.");
    }

    [Theory]
    [InlineData(nameof(EvidencePacketFixtures.DegradedPacket), "missing-source")]
    [InlineData(nameof(EvidencePacketFixtures.RedactedPacket), "redacted")]
    [InlineData(nameof(EvidencePacketFixtures.CompressedPacket), "compressed")]
    [InlineData(nameof(EvidencePacketFixtures.PartialPacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.WeakPacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.StalePacket), "degraded")]
    [InlineData(nameof(EvidencePacketFixtures.EmptyPacket), "degraded")]
    public void RestrictiveBanner_RestrictiveButAuthorizedState_RendersFluentMessageBarWithWarningIntent(
        string fixtureName,
        string expectedKind)
    {
        EvidencePacket packet = ResolveFixture(fixtureName);

        string markup = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(c => c.Packet, packet)).Markup;

        IElement banner = QueryAll(markup, RestrictiveState).ShouldHaveSingleItem();
        banner.GetAttribute("data-restrictive-kind").ShouldBe(expectedKind);

        // Every restrictive-but-authorized state maps to the amber Warning intent — not the Error reserved for
        // unauthorized, and not a hand-authored amber/red CSS fill.
        IElement messageBar = QueryAll(markup, $"{RestrictiveState} fluent-message-bar").ShouldHaveSingleItem();
        messageBar.GetAttribute("intent").ShouldBe("warning");
        messageBar.TextContent.ShouldNotBeNullOrWhiteSpace("the restrictive banner rendered no message text.");
    }

    [Fact]
    public void RestrictiveBanner_SupportedPacket_RendersNoBanner()
    {
        // A fully supported packet is below the restrictive precedence floor, so no banner renders at all.
        string markup = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(c => c.Packet, EvidencePacketFixtures.CompletePacket())).Markup;

        QueryAll(markup, RestrictiveState).ShouldBeEmpty();
    }

    [Fact]
    public void ScopeHeader_RendersScopeCaptionsViaFluentTypographyPrimitives()
    {
        // Task 2 remediation: the removed .mem-evidence-label hand-authored weight ramp is now Fluent
        // typography (FluentLabel → <fluent-label>), while the trust-contract data-testid anchors the
        // Epic17 suite relies on survive the swap.
        string markup = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(c => c.Packet, EvidencePacketFixtures.CompletePacket())).Markup;

        IElement scope = QueryAll(markup, "[data-testid='mem-evidence-scope']").ShouldHaveSingleItem();
        QueryAll(markup, "[data-testid='mem-evidence-scope'] fluent-label")
            .Count.ShouldBeGreaterThanOrEqualTo(2, "scope captions are no longer Fluent typography primitives.");

        scope.TextContent.ShouldContain("Tenant");
        scope.TextContent.ShouldContain("Case");
        QueryAll(markup, "[data-testid='mem-scope-tenant']").ShouldHaveSingleItem()
            .TextContent.ShouldContain("tenant-a");
        QueryAll(markup, "[data-testid='mem-scope-case']").ShouldHaveSingleItem()
            .TextContent.ShouldContain("case-a");
    }

    private static EvidencePacket ResolveFixture(string fixtureName) => fixtureName switch
    {
        nameof(EvidencePacketFixtures.DegradedPacket) => EvidencePacketFixtures.DegradedPacket(),
        nameof(EvidencePacketFixtures.RedactedPacket) => EvidencePacketFixtures.RedactedPacket(),
        nameof(EvidencePacketFixtures.CompressedPacket) => EvidencePacketFixtures.CompressedPacket(),
        nameof(EvidencePacketFixtures.PartialPacket) => EvidencePacketFixtures.PartialPacket(),
        nameof(EvidencePacketFixtures.WeakPacket) => EvidencePacketFixtures.WeakPacket(),
        nameof(EvidencePacketFixtures.StalePacket) => EvidencePacketFixtures.StalePacket(),
        nameof(EvidencePacketFixtures.EmptyPacket) => EvidencePacketFixtures.EmptyPacket(),
        _ => throw new InvalidOperationException($"Unsupported fixture '{fixtureName}'."),
    };
}
