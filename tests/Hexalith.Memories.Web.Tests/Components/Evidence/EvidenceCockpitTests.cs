// <copyright file="EvidenceCockpitTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Evidence;

using System.Globalization;
using System.Reflection;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Evidence;
using Hexalith.Memories.Web.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

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

    [Theory]
    [InlineData(EvidencePacketPermissionsContext.Tenant, EvidencePacketIsolationStatus.Authorized, null, "Tenant-scoped request")]
    [InlineData(EvidencePacketPermissionsContext.TenantCase, EvidencePacketIsolationStatus.Authorized, "case-a", "Case-scoped request")]
    [InlineData(EvidencePacketPermissionsContext.McpAuthorization, EvidencePacketIsolationStatus.Unauthorized, "case-a", "MCP authorization denied")]
    [InlineData(EvidencePacketPermissionsContext.McpError, EvidencePacketIsolationStatus.Unknown, "case-a", "MCP scope unavailable")]
    [InlineData(EvidencePacketPermissionsContext.TenantCase, EvidencePacketIsolationStatus.Unknown, "case-a", "Unavailable")]
    [InlineData("raw-permission-value", EvidencePacketIsolationStatus.Authorized, "case-a", "Unavailable")]
    [InlineData(null, EvidencePacketIsolationStatus.Authorized, "case-a", "Unavailable")]
    [InlineData(EvidencePacketPermissionsContext.Tenant, EvidencePacketIsolationStatus.Authorized, "case-a", "Unavailable")]
    [InlineData(EvidencePacketPermissionsContext.TenantCase, EvidencePacketIsolationStatus.Authorized, null, "Unavailable")]
    public void MemoriesScopeHeader_PermissionsContext_ShouldRenderOnlyKnownLabels(
        string? context,
        EvidencePacketIsolationStatus isolationStatus,
        string? caseId,
        string expected)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with { Scope = packet.Scope with { PermissionsContext = context!, IsolationStatus = isolationStatus, CaseId = caseId } };

        Render<MemoriesScopeHeader>(parameters => parameters.Add(p => p.Packet, packet))
            .Find("[data-testid='mem-scope-permissions']").TextContent.ShouldBe(expected);
    }

    [Fact]
    public void MemoriesScopeHeader_AuthorizationOmission_ShouldHideCasePermissionContext()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with { OmittedDetails = packet.OmittedDetails with { Reason = EvidencePacketOmissionReason.Authorization } };

        Render<MemoriesScopeHeader>(parameters => parameters.Add(p => p.Packet, packet))
            .Find("[data-testid='mem-scope-permissions']").TextContent.ShouldBe("Unavailable");
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "Evidence refresh failed")]
    public void MemoriesEvidenceCockpit_Refresh_ShouldHidePriorPermissionContext(bool loading, string? error)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();

        IRenderedComponent<MemoriesEvidenceCockpit> component =
            Render<MemoriesEvidenceCockpit>(parameters => parameters.Add(p => p.Packet, packet));
        component.Find("[data-testid='mem-scope-permissions']").TextContent.ShouldBe("Case-scoped request");

        component.Render(parameters => parameters.Add(p => p.IsLoading, loading).Add(p => p.ErrorMessage, error));

        component.Find("[data-testid='mem-scope-permissions']").TextContent.ShouldBe("Unavailable");
        component.Find("[data-testid='mem-scope-tenant']").TextContent.ShouldBe("tenant-a");
    }

    [Fact]
    public void MemoriesEvidenceCockpit_RealPacket_ShouldRenderOneMultiExpandAccordionWithPrimaryExpanded()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket()));

        IRenderedComponent<FluentAccordion> accordion = component.FindComponent<FluentAccordion>();
        accordion.Instance.ExpandMode.ShouldBe(AccordionExpandMode.Multi);
        accordion.Instance.HeadingLevel.ShouldBe(2);

        IReadOnlyList<IRenderedComponent<FluentAccordionItem>> items = component.FindComponents<FluentAccordionItem>();
        items.Select(static item => item.Instance.Header).ShouldBe(
            ["Evidence", "Recovery and feedback", "Sources", "Retrieval axes", "Graph context"]);
        items[0].Instance.Expanded.ShouldBeTrue();
        items[1].Instance.Expanded.ShouldBeTrue();
        items.Skip(2).ShouldAllBe(static item => !item.Instance.Expanded);
    }

    [Fact]
    public void MemoriesEvidenceCockpit_IdleState_ShouldNotAnnounceAnError()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>();

        component.Find("[data-testid='mem-evidence-unavailable']").TextContent.ShouldContain("Unavailable");
        component.FindAll("[data-testid='mem-evidence-error']").ShouldBeEmpty();
        component.FindAll("[role='alert']").ShouldBeEmpty();
    }

    [Fact]
    public async Task MemoriesEvidenceCockpit_UserCollapsedPrimary_ShouldStayCollapsedAfterParentRerender()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, packet));
        IRenderedComponent<FluentAccordionItem> primary = component.FindComponents<FluentAccordionItem>()[0];

        await component.InvokeAsync(() => primary.Instance.SetExpandedAsync(false));
        component.Render(parameters => parameters.Add(p => p.Packet, packet));

        component.FindComponents<FluentAccordionItem>()[0].Instance.Expanded.ShouldBeFalse();
    }

    [Fact]
    public async Task MemoriesEvidenceCockpit_UserCollapsedRecovery_ShouldStayCollapsedAfterParentRerender()
    {
        // Symmetric to the primary-item persistence guard: the recovery accordion item is also two-way
        // bound (@bind-Expanded="_isRecoveryExpanded"), so a user collapse must survive a parent rerender.
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, packet));
        IRenderedComponent<FluentAccordionItem> recovery = component.FindComponents<FluentAccordionItem>()[1];

        await component.InvokeAsync(() => recovery.Instance.SetExpandedAsync(false));
        component.Render(parameters => parameters.Add(p => p.Packet, packet));

        component.FindComponents<FluentAccordionItem>()[1].Instance.Expanded.ShouldBeFalse();
    }

    [Fact]
    public void MemoriesEvidenceCockpit_UnavailableState_ShouldLocalizeUnknownTenantScope()
    {
        // The idle/loading/error states feed the canonical unavailable packet whose blank tenant renders
        // through the localized "unknown tenant" fallback. This pins the sentinel + localization contract so
        // the two independent "unknown" literals (the cockpit sentinel and the scope-header special case) can
        // no longer drift and leak an untranslated tenant into the most common empty state.
        Render<MemoriesEvidenceCockpit>()
            .Find("[data-testid='mem-scope-tenant']").TextContent.ShouldContain("unknown tenant");

        WithCulture("fr-FR", () =>
        {
            IElement tenant = Render<MemoriesEvidenceCockpit>().Find("[data-testid='mem-scope-tenant']");
            tenant.TextContent.ShouldContain("locataire inconnu");
            tenant.TextContent.ShouldNotContain("unknown");
        });
    }

    [Fact]
    public void EvidenceDisplay_FreshnessLabel_ShouldLocalizePositiveAgeWithoutTimestamp()
    {
        // The positive-age branch (no LastCheckedAt) is the only freshness path with no direct output
        // assertion; pin its localized {state}/{age} substitution in English and French.
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();
        var freshness = new EvidencePacketFreshness(EvidencePacketFreshnessState.Current, AgeSeconds: 120);

        EvidenceDisplay.FreshnessLabel(freshness, localizer).ShouldBe("Current; age 120 s");
        WithCulture("fr-FR", () =>
        {
            string localized = EvidenceDisplay.FreshnessLabel(freshness, localizer);
            localized.ShouldContain("Actuelle");
            localized.ShouldContain("âge 120 s");
        });
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
        component.FindComponents<FluentAccordion>().ShouldBeEmpty();
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
        component.FindComponents<FluentAccordion>().ShouldBeEmpty();
    }

    [Fact]
    public void MemoriesEvidenceCockpit_UnavailableCache_ShouldNotAliasDelimitedTenantAndCaseValues()
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant|case")
            .Add(p => p.CaseId, "one"));

        component.Render(parameters => parameters
            .Add(p => p.TenantId, "tenant")
            .Add(p => p.CaseId, "case|one"));

        EvidencePacket packet = component.FindComponent<MemoriesScopeHeader>().Instance.Packet;
        packet.Scope.TenantId.ShouldBe("tenant");
        packet.Scope.CaseId.ShouldBe("case|one");
    }

    [Fact]
    public void MemoriesTrustStrip_CompressedPacket_ShouldRenderVisibleLabelsAndAccessibleNames()
    {
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();
        IRenderedComponent<MemoriesTrustStrip> component = Render<MemoriesTrustStrip>(parameters => parameters
            .Add(p => p.Packet, EvidencePacketFixtures.CompressedPacket())
            .Add(p => p.Mode, MemoriesTrustStrip.TrustStripMode.Packet));

        component.Markup.ShouldContain("aria-label=\"Confidence: Strong\"");
        component.Markup.ShouldContain($"aria-label=\"Freshness: {EvidenceDisplay.FreshnessLabel(EvidencePacketFixtures.CompressedPacket(), localizer)}\"");
        component.Markup.ShouldContain("aria-label=\"Evidence health: Pending expansion\"");
        component.Markup.ShouldContain("aria-label=\"Token budget: compressed\"");
        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("1 source");
    }

    [Theory]
    [InlineData(false, null, EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.None, "Complete")]
    [InlineData(true, false, EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.None, "Degraded")]
    [InlineData(false, true, EvidencePacketIsolationStatus.Authorized, EvidencePacketState.PendingExpansion, EvidencePacketOmissionReason.TokenBudget, "Degraded")]
    [InlineData(true, true, EvidencePacketIsolationStatus.Unauthorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.None, "Unauthorized")]
    [InlineData(true, true, EvidencePacketIsolationStatus.Unknown, EvidencePacketState.Complete, EvidencePacketOmissionReason.None, "Unavailable")]
    [InlineData(true, true, EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Unauthorized, EvidencePacketOmissionReason.None, "Unauthorized")]
    [InlineData(true, true, EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.Authorization, "Unauthorized")]
    public void MemoriesTrustStrip_ShouldApplyScopeAndDegradationPrecedence(
        bool degraded,
        bool? allAxesUnavailable,
        EvidencePacketIsolationStatus isolation,
        EvidencePacketState state,
        EvidencePacketOmissionReason omissionReason,
        string expected)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with
        {
            Evidence = packet.Evidence with { Degraded = degraded, AllEnabledAxesUnavailable = allAxesUnavailable },
            Scope = packet.Scope with { IsolationStatus = isolation },
            State = state,
            OmittedDetails = packet.OmittedDetails with { Reason = omissionReason },
        };

        IRenderedComponent<MemoriesTrustStrip> component =
            Render<MemoriesTrustStrip>(parameters => parameters.Add(p => p.Packet, packet));
        IElement badge = component.Find($"[aria-label='Evidence health: {expected}']");
        string expectedSlot = expected switch
        {
            "Degraded" => "Warning",
            "Unauthorized" => "Danger",
            "Complete" => "Success",
            _ => "Neutral",
        };
        badge.GetAttribute("data-fc-badge-slot").ShouldBe(expectedSlot);
    }

    [Fact]
    public void MemoriesEvidenceCockpit_AuthorizationOmission_ShouldSuppressEvidenceDetails()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with { OmittedDetails = packet.OmittedDetails with { Reason = EvidencePacketOmissionReason.Authorization } };

        IRenderedComponent<MemoriesEvidenceCockpit> component =
            Render<MemoriesEvidenceCockpit>(parameters => parameters.Add(p => p.Packet, packet));
        component.Find("[data-testid='mem-evidence-restrictive-state']")
            .GetAttribute("data-restrictive-kind").ShouldBe("unauthorized");
        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("sources unavailable");
        component.FindAll("[data-testid='mem-source-stack'], [data-testid='mem-axis-breakdown'], [data-testid='mem-graph-summary']")
            .ShouldBeEmpty();
    }

    [Fact]
    public void MemoriesTrustStrip_AcronymEnumValues_ShouldPreserveAcronyms()
    {
        EvidenceDisplay.Label(SampleAcronymEnum.MCP).ShouldBe("MCP");
        EvidenceDisplay.Label(SampleAcronymEnum.MCPHandler).ShouldBe("MCP handler");
        EvidenceDisplay.Label(SampleAcronymEnum.PendingExpansion).ShouldBe("Pending expansion");
        EvidenceDisplay.Label(SampleAcronymEnum.Strong).ShouldBe("Strong");
    }

    [Theory]
    [InlineData("case-a", "Review dossier", 3, "case-a", "Review dossier", "3")]
    [InlineData("case-a", "Bearer secret-token", 0, "case-a", "Unavailable", "0")]
    [InlineData("case-a", "postgres://synthetic:secret-token@localhost/database", 0, "case-a", "Unavailable", "0")]
    [InlineData("foreign-case", "Restricted case", 8, null, null, null)]
    [InlineData(null, "Unbound case", 0, "Unavailable", "Unavailable", "0")]
    [InlineData("case-a", null, -1, "case-a", "Unavailable", "Unavailable")]
    public void MemoriesSourceCitationStack_ShouldRenderAvailableInScopeMetadata(
        string? caseId,
        string? caseName,
        int annotationCount,
        string? expectedCaseId,
        string? expectedCaseName,
        string? expectedAnnotations)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        EvidencePacketSource source = packet.Sources[0] with { CaseId = caseId, CaseName = caseName, AnnotationsCount = annotationCount };

        IRenderedComponent<MemoriesSourceCitationStack> component =
            Render<MemoriesSourceCitationStack>(parameters => parameters.Add(p => p.Packet, packet with { Sources = [source] }));

        component.FindAll("[data-testid='mem-source-case-id']").Select(static x => x.TextContent)
            .ShouldBe(expectedCaseId is null ? [] : [expectedCaseId]);
        component.FindAll("[data-testid='mem-source-case-name']").Select(static x => x.TextContent)
            .ShouldBe(expectedCaseName is null ? [] : [expectedCaseName]);
        component.FindAll("[data-testid='mem-source-annotations']").Select(static x => x.TextContent.Trim())
            .ShouldBe(expectedAnnotations is null ? [] : [expectedAnnotations]);
        component.Markup.ShouldNotContain("secret-token");
    }

    [Theory]
    [InlineData(EvidencePacketIsolationStatus.Unknown, EvidencePacketState.Complete)]
    [InlineData(EvidencePacketIsolationStatus.Unauthorized, EvidencePacketState.Complete)]
    [InlineData(EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Unauthorized)]
    public void MemoriesSourceCitationStack_ShouldSuppressCitationMetadataForRestrictivePackets(
        EvidencePacketIsolationStatus isolation,
        EvidencePacketState state)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with { Scope = packet.Scope with { IsolationStatus = isolation }, State = state };

        IRenderedComponent<MemoriesSourceCitationStack> component =
            Render<MemoriesSourceCitationStack>(parameters => parameters.Add(p => p.Packet, packet));

        component.FindAll("[data-testid='mem-source-item']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(7_000_000_000L, 1, true, EvidencePacketState.Partial, "Returned: 1. Reported matches: 7000000000.", "Tenant has indexed memories")]
    [InlineData(0L, 0, false, EvidencePacketState.Empty, "Returned: 0. Reported matches: 0.", "Tenant has no indexed memories")]
    [InlineData(0L, 0, null, EvidencePacketState.Empty, "Returned: 0. Reported matches: 0.", "Tenant index status unavailable")]
    [InlineData(0L, 0, null, EvidencePacketState.Degraded, "Returned: 0. Reported matches: 0.", "Tenant index status unavailable")]
    [InlineData(5L, 1, true, EvidencePacketState.Degraded, "Returned: 1. Reported matches: 5.", "Tenant has indexed memories")]
    [InlineData(-1L, 0, null, EvidencePacketState.Empty, "Result counts unavailable", "Tenant index status unavailable")]
    public void MemoriesEvidenceCockpit_ShouldDistinguishReportedCountsAndIndexStatus(
        long total,
        int returned,
        bool? indexed,
        EvidencePacketState state,
        string countsText,
        string indexText)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with
        {
            State = state,
            Result = packet.Result with { TotalCount = total, ReturnedCount = returned, HasIndexedMemoryUnits = indexed },
            Evidence = packet.Evidence with
            {
                Degraded = state == EvidencePacketState.Degraded,
                AllEnabledAxesUnavailable = state == EvidencePacketState.Degraded ? false : null,
            },
        };

        IRenderedComponent<MemoriesEvidenceCockpit> component =
            Render<MemoriesEvidenceCockpit>(parameters => parameters.Add(p => p.Packet, packet));
        component.Find("[data-testid='mem-evidence-result-counts']").TextContent.ShouldBe(countsText);
        component.Find("[data-testid='mem-evidence-index-status']").TextContent.ShouldBe(indexText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MemoriesEvidenceCockpit_UnavailableRetrieval_ShouldNotPresentDefaultCounts(bool allAxesUnavailable)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = allAxesUnavailable
            ? packet with { Evidence = packet.Evidence with { AllEnabledAxesUnavailable = true } }
            : EvidencePacketMapper.FromError(new ErrorResponse("DAPR_UNAVAILABLE", "Backend unavailable", "Retry"), packet.Scope);

        Render<MemoriesEvidenceCockpit>(parameters => parameters.Add(p => p.Packet, packet))
            .Find("[data-testid='mem-evidence-result-counts']").TextContent.ShouldBe("Result counts unavailable");
    }

    [Theory]
    [InlineData(EvidencePacketIsolationStatus.Unknown, EvidencePacketState.Complete, EvidencePacketOmissionReason.None)]
    [InlineData(EvidencePacketIsolationStatus.Unauthorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.None)]
    [InlineData(EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Unauthorized, EvidencePacketOmissionReason.None)]
    [InlineData(EvidencePacketIsolationStatus.Authorized, EvidencePacketState.Complete, EvidencePacketOmissionReason.Authorization)]
    public void MemoriesEvidenceCockpit_RestrictedPacket_ShouldSuppressResultMetadata(
        EvidencePacketIsolationStatus isolation,
        EvidencePacketState state,
        EvidencePacketOmissionReason reason)
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        packet = packet with
        {
            Scope = packet.Scope with { IsolationStatus = isolation },
            State = state,
            OmittedDetails = packet.OmittedDetails with { Reason = reason },
        };

        IRenderedComponent<MemoriesEvidenceCockpit> component =
            Render<MemoriesEvidenceCockpit>(parameters => parameters.Add(p => p.Packet, packet));
        component.FindAll("[data-testid='mem-evidence-result-counts']").ShouldBeEmpty();
        component.FindAll("[data-testid='mem-evidence-index-status']").ShouldBeEmpty();
    }

    [Fact]
    public void MemoriesSourceCitationStack_AuthorizationOmission_ShouldSuppressCitationMetadata()
    {
        EvidencePacket packet = EvidencePacketFixtures.CompletePacket();
        EvidencePacketSource source = packet.Sources[0] with { CaseId = "case-a", CaseName = "Restricted case", AnnotationsCount = 9 };
        packet = packet with
        {
            Sources = [source],
            OmittedDetails = packet.OmittedDetails with { Reason = EvidencePacketOmissionReason.Authorization },
        };

        IRenderedComponent<MemoriesSourceCitationStack> component =
            Render<MemoriesSourceCitationStack>(parameters => parameters.Add(p => p.Packet, packet));
        component.FindAll("[data-testid='mem-source-case-id'], [data-testid='mem-source-case-name'], [data-testid='mem-source-annotations']")
            .ShouldBeEmpty();
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
        freshness.UnavailableFallback.ShouldBe(EvidenceResourceKeys.FreshnessUnavailable);

        // Every UI display field shown to operators must be tracked.
        string[] requiredFields =
        [
            "scope.tenant", "scope.case", "scope.isolation", "scope.permissionsContext",
            "trust.confidence", "trust.freshness", "trust.sourceCount", "trust.evidenceHealth", "trust.tokenBudget",
            "result.query", "result.summary", "result.totalCount", "result.returnedCount", "result.hasIndexedMemoryUnits",
            "sources.originIdentifier", "sources.type", "sources.snippet", "sources.memoryUnit", "sources.rank", "sources.score",
            "sources.timestamp", "sources.freshness", "sources.caseId", "sources.caseName", "sources.annotations",
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
    public void MemoriesTrustStrip_UnauthorizedStateWithAuthorizedScope_ShouldHideSourceCount()
    {
        EvidencePacket packet = EvidencePacketFixtures.UnauthorizedPacket() with
        {
            Scope = EvidencePacketFixtures.UnauthorizedPacket().Scope with
            {
                IsolationStatus = EvidencePacketIsolationStatus.Authorized,
            },
        };
        IRenderedComponent<MemoriesTrustStrip> component = Render<MemoriesTrustStrip>(parameters => parameters
            .Add(p => p.Packet, packet));

        component.Find("[data-testid='mem-trust-source-count']").TextContent.ShouldContain("sources unavailable");
        component.Markup.ShouldNotContain("1 source");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MemoriesEvidenceCockpit_UnavailableInput_ShouldUseCanonicalMapperShape(bool error)
    {
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.TenantId, "tenant-a")
            .Add(p => p.CaseId, "case-a")
            .Add(p => p.IsLoading, !error)
            .Add(p => p.ErrorMessage, error ? "safe failure" : null));

        EvidencePacket packet = component.FindComponent<MemoriesScopeHeader>().Instance.Packet;
        packet.ShouldBe(EvidencePacketMapper.Unavailable("tenant-a", "case-a", isError: error));
        packet.State.ShouldBe(EvidencePacketState.Empty);
        packet.Evidence.Degraded.ShouldBe(error);
        packet.OmittedDetails.Reason.ShouldBe(EvidencePacketOmissionReason.None);
    }

    [Fact]
    public void Localization_EveryEvidenceKeyResolvesInEnglishAndFrench()
    {
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();

        foreach (string cultureName in new[] { "en", "fr" })
        {
            WithCulture(cultureName, () =>
            {
                foreach (string key in AllEvidenceKeys())
                {
                    LocalizedString value = localizer[key];
                    value.ResourceNotFound.ShouldBeFalse($"Missing {cultureName} localization resource for key '{key}'.");
                    value.Value.ShouldNotBeNullOrWhiteSpace();
                    value.Value.ShouldNotBe(key);
                }
            });
        }
    }

    [Fact]
    public void MemoriesEvidenceCockpit_FrenchCulture_ShouldLocalizeAllOwnedStatesWithoutKeyLeakage()
    {
        WithCulture("fr-FR", () =>
        {
            string complete = Render<MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.Packet, EvidencePacketFixtures.CompletePacket())).Markup;
            complete.ShouldContain("Cockpit de preuves");
            complete.ShouldContain("Locataire");
            complete.ShouldContain("Demande limitée au dossier");
            complete.ShouldContain("Le locataire possède des mémoires indexées");
            complete.ShouldContain("Confiance");
            complete.ShouldContain("Preuves");
            complete.ShouldContain("Axes de recherche");
            complete.ShouldContain("Contexte de graphe");
            complete.ShouldContain("Fichier");
            complete.ShouldContain("Fraîcheur");
            complete.ShouldContain("Horodatage");
            complete.ShouldContain("Unité de mémoire");

            string loading = Render<MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.TenantId, "tenant-a")
                .Add(p => p.CaseId, "case-a")
                .Add(p => p.IsLoading, true)).Markup;
            loading.ShouldContain("Chargement des preuves");
            loading.ShouldNotContain("Loading evidence");

            string error = Render<MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.TenantId, "tenant-a")
                .Add(p => p.ErrorMessage, "Failed with Bearer secret-token")).Markup;
            error.ShouldContain("Preuves indisponibles");
            error.ShouldNotContain("Bearer ");

            string degraded = Render<MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.Packet, EvidencePacketFixtures.DegradedPacket())).Markup;
            degraded.ShouldContain("La récupération des preuves est dégradée pour cette portée.");

            string unauthorized = Render<MemoriesEvidenceCockpit>(parameters => parameters
                .Add(p => p.Packet, EvidencePacketFixtures.UnauthorizedPacket())).Markup;
            unauthorized.ShouldContain("Autorisation requise pour cette portée locataire et dossier.");

            foreach (string key in AllEvidenceKeys())
            {
                complete.ShouldNotContain(key);
                loading.ShouldNotContain(key);
                error.ShouldNotContain(key);
                degraded.ShouldNotContain(key);
                unauthorized.ShouldNotContain(key);
            }
        });
    }

    [Fact]
    public void EvidenceDisplay_FrenchCulture_ShouldLocalizeEnumsScoresFreshnessAndTimestamps()
    {
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();

        WithCulture("fr-FR", () =>
        {
            var timestamp = new DateTimeOffset(2026, 7, 5, 7, 0, 0, TimeSpan.Zero);
            var freshness = new EvidencePacketFreshness(
                EvidencePacketFreshnessState.Current,
                LastCheckedAt: timestamp);

            EvidenceDisplay.Label(EvidencePacketState.PendingExpansion, localizer)
                .ShouldBe("Extension en attente");
            EvidenceDisplay.Label((EvidencePacketState)999, localizer).ShouldBe("Indisponible");
            EvidenceDisplay.ScoreLabel(0.123d, localizer).ShouldBe("0,123");
            EvidenceDisplay.ScoreLabel(null, localizer).ShouldBe("score indisponible");
            EvidenceDisplay.ScoreLabel(double.NaN, localizer).ShouldBe("score indisponible");
            EvidenceDisplay.ScoreLabel(double.PositiveInfinity, localizer).ShouldBe("score indisponible");
            EvidenceDisplay.ScoreLabel(double.NegativeInfinity, localizer).ShouldBe("score indisponible");
            EvidenceDisplay.TimestampLabel(timestamp, localizer).ShouldContain("2026-07-05T07:00:00.0000000+00:00");
            EvidenceDisplay.FreshnessLabel(freshness, localizer).ShouldContain("Actuelle");
            EvidenceDisplay.FreshnessLabel(freshness, localizer).ShouldContain("2026-07-05T07:00:00.0000000+00:00");
            EvidenceDisplay.FreshnessLabel(
                new EvidencePacketFreshness(EvidencePacketFreshnessState.Current, AgeSeconds: -1),
                localizer).ShouldBe("Indisponible");
        });
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
        EvidencePacket packet = EvidencePacketFixtures.MultiSourcePacket();
        packet = packet with { Sources = [packet.Sources[1] with { Rank = 7 }, packet.Sources[0]] };
        IRenderedComponent<MemoriesEvidenceCockpit> component = Render<MemoriesEvidenceCockpit>(parameters => parameters
            .Add(p => p.Packet, packet));

        IEnumerable<IElement> citations = component.FindAll("[data-testid='mem-source-item']");
        citations.Select(static item => item.QuerySelector("strong")?.TextContent)
            .ShouldBe(["https://docs.example/source-b", "https://docs.example/source-a"]);
        citations.Select(static item => item.GetAttribute("data-source-rank")).ShouldBe(["7", "1"]);
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

    private static IEnumerable<string> AllEvidenceKeys()
    {
        IEnumerable<string> constants = typeof(EvidenceResourceKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
            .Select(static field => (string)field.GetRawConstantValue()!);

        return constants
            .Concat(Enum.GetValues<EvidencePacketState>().Select(EvidenceResourceKeys.State))
            .Concat(Enum.GetValues<EvidencePacketEvidenceStrength>().Select(EvidenceResourceKeys.Strength))
            .Concat(Enum.GetValues<EvidencePacketIsolationStatus>().Select(EvidenceResourceKeys.Isolation))
            .Concat(Enum.GetValues<EvidencePacketFreshnessState>().Select(EvidenceResourceKeys.Freshness))
            .Concat(Enum.GetValues<SourceType>().Select(EvidenceResourceKeys.SourceType))
            .Distinct(StringComparer.Ordinal);
    }

    private static void WithCulture(string cultureName, Action action)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private enum SampleAcronymEnum
    {
        MCP,
        MCPHandler,
        PendingExpansion,
        Strong,
    }
}
