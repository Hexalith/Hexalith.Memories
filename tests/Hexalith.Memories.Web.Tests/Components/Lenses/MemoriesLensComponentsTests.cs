// <copyright file="MemoriesLensComponentsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.AgentPacket;
using Hexalith.Memories.Web.Components.Lenses.Benchmark;
using Hexalith.Memories.Web.Components.Lenses.CaseActivity;
using Hexalith.Memories.Web.Components.Lenses.Ingestion;
using Hexalith.Memories.Web.Components.Lenses.OperatorHealth;
using Hexalith.Memories.Web.Components.Recovery;
using Hexalith.Memories.Web.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

public sealed class MemoriesLensComponentsTests : FrontComposerTestBase
{
    public MemoriesLensComponentsTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void CaseActivityTrail_RendersScopeReturnPathRowsAndOrderingNote()
    {
        IRenderedComponent<MemoriesCaseActivityTrail> component = Render<MemoriesCaseActivityTrail>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.Happy())
            .Add(p => p.ReturnRoute, "memories/evidence?packet=memory-a"));

        component.Find("[data-testid='mem-lens-shell']").GetAttribute("data-lens")
            .ShouldBe("CaseActivityTrail");
        component.Find("[data-testid='mem-lens-tenant']").TextContent.ShouldContain("tenant-a");
        component.Find("[data-testid='mem-lens-return-route']").TextContent.ShouldContain("memories/evidence");
        component.Find("[data-testid='mem-activity-trail']").GetAttribute("data-timestamps-available")
            .ShouldBe("true");
        component.FindAll("[data-testid='mem-activity-row']").ShouldNotBeEmpty();
    }

    [Fact]
    public void IngestionLifecycleTracker_CriticalStateUsesAssertiveLiveRegionAndEmitsSafeIntent()
    {
        RecoveryActionInvocation? captured = null;
        IRenderedComponent<MemoriesIngestionLifecycleTracker> component = Render<MemoriesIngestionLifecycleTracker>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.Unauthorized())
            .Add(p => p.OnRecoveryAction, (RecoveryActionInvocation i) => captured = i));

        IElement tracker = component.Find("[data-testid='mem-ingestion-tracker']");
        tracker.GetAttribute("role").ShouldBe("alert");
        tracker.GetAttribute("aria-live").ShouldBe("assertive");

        IElement button = component.Find("[data-testid='mem-ingestion-recovery-button']");
        button.Click();

        captured.ShouldNotBeNull();
        captured!.Kind.ShouldBe(EvidencePacketRecoveryKind.CheckAuthorization);
        captured.TenantId.ShouldBe("tenant-a");
        captured.CaseId.ShouldBe("case-a");
    }

    [Fact]
    public void OperatorHealthMatrix_RendersTrustBlockingChecksAndDisablesUnsanctionedActions()
    {
        IRenderedComponent<MemoriesOperatorHealthMatrix> component = Render<MemoriesOperatorHealthMatrix>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.Unauthorized()));

        IElement matrix = component.Find("[data-testid='mem-health-matrix']");
        matrix.GetAttribute("data-has-trust-blocking").ShouldBe("true");
        matrix.GetAttribute("role").ShouldBe("alert");
        component.FindAll("[data-testid='mem-health-check']")
            .ShouldContain(e => e.GetAttribute("data-trust-blocking") == "true");
        component.Markup.ShouldNotContain("memory-secret");
    }

    [Fact]
    public void BenchmarkResultComparator_RendersTextEquivalentForAxisBarsAndBenchmarkMetadata()
    {
        IRenderedComponent<MemoriesBenchmarkResultComparator> component = Render<MemoriesBenchmarkResultComparator>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.Happy()));

        component.Find("[data-testid='mem-benchmark-comparator']").GetAttribute("data-result-state")
            .ShouldBe(nameof(BenchmarkResultState.Passed));
        component.Find("[data-testid='mem-benchmark-ndcg']").GetAttribute("data-availability")
            .ShouldBe(nameof(LensFieldAvailability.Available));
        component.Find("[data-testid='mem-benchmark-ndcg']").TextContent.ShouldContain("hybrid 0.875");
        component.FindAll("[data-testid='mem-benchmark-axis']").ShouldNotBeEmpty();
        component.Find("[data-testid='mem-benchmark-axis-score']").TextContent.ShouldContain("0.91");
    }

    [Fact]
    public void AgentPacketInspector_CopyControlUsesSameSanitizedPayloadAsJsonView()
    {
        string? copied = null;
        IRenderedComponent<MemoriesAgentPacketInspector> component = Render<MemoriesAgentPacketInspector>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.TenantCaseSensitive())
            .Add(p => p.OnCopy, (string text) => copied = text));

        component.Find("[data-testid='mem-packet-copy']").Click();

        copied.ShouldNotBeNull();
        copied.ShouldBe(component.Find("[data-testid='mem-packet-json']").TextContent);
        copied.ShouldNotContain("Bearer ");
        copied.ShouldNotContain("C:\\Users\\Jerome");
        copied.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void CaseActivityTrail_CrossTenantPacket_RendersForeignTenantAndCaseScope()
    {
        IRenderedComponent<MemoriesCaseActivityTrail> component = Render<MemoriesCaseActivityTrail>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.CrossTenant())
            .Add(p => p.ReturnRoute, "memories/evidence?packet=memory-a"));

        // Switching the packet to a different tenant repartitions the shared shell scope; no tenant-a residue.
        component.Find("[data-testid='mem-lens-tenant']").TextContent.ShouldContain("tenant-b");
        component.Find("[data-testid='mem-lens-tenant']").TextContent.ShouldNotContain("tenant-a");
        component.Find("[data-testid='mem-lens-case']").TextContent.ShouldContain("case-b");
    }

    [Fact]
    public void CaseActivityTrail_ReturnAction_IsReachableAndEmitsSanitizedReturnRoute()
    {
        string? returned = null;
        IRenderedComponent<MemoriesCaseActivityTrail> component = Render<MemoriesCaseActivityTrail>(parameters => parameters
            .Add(p => p.Packet, LensPacketFixtures.Happy())
            .Add(p => p.ReturnRoute, "memories/evidence?packet=memory-a")
            .Add(p => p.OnReturn, (string route) => returned = route));

        component.Find("[data-testid='mem-lens-return-action']").Click();

        returned.ShouldBe("memories/evidence?packet=memory-a");
    }

    [Fact]
    public void Localization_EveryLensKeyResolves()
    {
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();

        foreach (string key in AllLensResourceKeys())
        {
            LocalizedString value = localizer[key];
            value.ResourceNotFound.ShouldBeFalse($"Missing localization resource for key '{key}'.");
            value.Value.ShouldNotBeNullOrWhiteSpace();
        }
    }

    private static IEnumerable<string> AllLensResourceKeys()
    {
        yield return LensResourceKeys.ShellLabel;
        yield return LensResourceKeys.TenantLabel;
        yield return LensResourceKeys.CaseLabel;
        yield return LensResourceKeys.TenantScope;
        yield return LensResourceKeys.ActiveLensLabel;
        yield return LensResourceKeys.RoleLabel;
        yield return LensResourceKeys.ConfidenceLabel;
        yield return LensResourceKeys.FreshnessLabel;
        yield return LensResourceKeys.StateLabel;
        yield return LensResourceKeys.CapabilityLabel;
        yield return LensResourceKeys.ContractVersionLabel;
        yield return LensResourceKeys.ReturnLabel;
        yield return LensResourceKeys.ReturnAction;

        foreach (LensKind lens in Enum.GetValues<LensKind>())
        {
            yield return LensResourceKeys.LensTitle(lens);
            yield return LensResourceKeys.LensDescription(lens);
        }

        foreach (LensRole role in Enum.GetValues<LensRole>())
        {
            yield return LensResourceKeys.Role(role);
        }

        foreach (LensFieldAvailability availability in Enum.GetValues<LensFieldAvailability>())
        {
            yield return LensResourceKeys.Availability(availability);
            yield return CaseActivityResourceKeys.LinkStatus(availability);
        }

        yield return CaseActivityResourceKeys.RegionLabel;
        yield return CaseActivityResourceKeys.Empty;
        yield return CaseActivityResourceKeys.OrderingBasis;
        yield return CaseActivityResourceKeys.StatusLabel;
        yield return CaseActivityResourceKeys.LinkLabel;
        foreach (CaseActivityKind kind in Enum.GetValues<CaseActivityKind>())
        {
            yield return CaseActivityResourceKeys.Kind(kind);
        }

        yield return IngestionLifecycleResourceKeys.RegionLabel;
        yield return IngestionLifecycleResourceKeys.Empty;
        yield return IngestionLifecycleResourceKeys.StageNote;
        yield return IngestionLifecycleResourceKeys.UnitLabel;
        yield return IngestionLifecycleResourceKeys.StageLabel;
        yield return IngestionLifecycleResourceKeys.OutcomeLabel;
        yield return IngestionLifecycleResourceKeys.FailureLabel;
        yield return IngestionLifecycleResourceKeys.RecoveryLabel;
        yield return IngestionLifecycleResourceKeys.RecoveryDisabledReason;
        yield return IngestionLifecycleResourceKeys.RecoveryNone;
        yield return IngestionLifecycleResourceKeys.FailureNone;
        foreach (IngestionOutcome outcome in Enum.GetValues<IngestionOutcome>())
        {
            yield return IngestionLifecycleResourceKeys.Outcome(outcome);
        }

        yield return OperatorHealthResourceKeys.RegionLabel;
        yield return OperatorHealthResourceKeys.LastCheckedNote;
        yield return OperatorHealthResourceKeys.CheckLabel;
        yield return OperatorHealthResourceKeys.StatusLabel;
        yield return OperatorHealthResourceKeys.CapabilityLabel;
        yield return OperatorHealthResourceKeys.EvidenceLabel;
        yield return OperatorHealthResourceKeys.NextActionLabel;
        yield return OperatorHealthResourceKeys.NextActionNone;
        yield return OperatorHealthResourceKeys.NextActionDisabled;
        yield return OperatorHealthResourceKeys.TrustBlockingLabel;
        foreach (OperatorCheckKind kind in Enum.GetValues<OperatorCheckKind>())
        {
            yield return OperatorHealthResourceKeys.Check(kind);
        }

        foreach (OperatorCheckStatus status in Enum.GetValues<OperatorCheckStatus>())
        {
            yield return OperatorHealthResourceKeys.Status(status);
        }

        yield return BenchmarkResourceKeys.RegionLabel;
        yield return BenchmarkResourceKeys.NdcgLabel;
        yield return BenchmarkResourceKeys.ThresholdLabel;
        yield return BenchmarkResourceKeys.PerQueryLabel;
        yield return BenchmarkResourceKeys.EvidenceLinkLabel;
        yield return BenchmarkResourceKeys.AxisEvidenceLabel;
        yield return BenchmarkResourceKeys.ProxyNote;
        yield return BenchmarkResourceKeys.ResultStateLabel;
        yield return BenchmarkResourceKeys.AxisLabel;
        yield return BenchmarkResourceKeys.ScoreLabel;
        yield return BenchmarkResourceKeys.NormalizationLabel;
        yield return BenchmarkResourceKeys.UnavailableAxesLabel;
        yield return BenchmarkResourceKeys.Empty;
        foreach (BenchmarkResultState state in Enum.GetValues<BenchmarkResultState>())
        {
            yield return BenchmarkResourceKeys.ResultState(state);
        }

        yield return AgentPacketResourceKeys.RegionLabel;
        yield return AgentPacketResourceKeys.RequestSummaryLabel;
        yield return AgentPacketResourceKeys.QueryLabel;
        yield return AgentPacketResourceKeys.CountsLabel;
        yield return AgentPacketResourceKeys.SchemaLabel;
        yield return AgentPacketResourceKeys.JsonLabel;
        yield return AgentPacketResourceKeys.TokenBudgetLabel;
        yield return AgentPacketResourceKeys.OmittedFieldsLabel;
        yield return AgentPacketResourceKeys.OmittedFieldsNone;
        yield return AgentPacketResourceKeys.ExpansionHandlesLabel;
        yield return AgentPacketResourceKeys.ExpansionHandlesNone;
        yield return AgentPacketResourceKeys.ErrorLabel;
        yield return AgentPacketResourceKeys.DiagnosticLabel;
        yield return AgentPacketResourceKeys.CopyLabel;
        yield return AgentPacketResourceKeys.TokenBudgetCompressed;
        yield return AgentPacketResourceKeys.TokenBudgetWithin;
        foreach (PacketSchemaFieldKind kind in Enum.GetValues<PacketSchemaFieldKind>())
        {
            yield return AgentPacketResourceKeys.Field(kind);
        }
    }
}
