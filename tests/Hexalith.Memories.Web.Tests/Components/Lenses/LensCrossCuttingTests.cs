// <copyright file="LensCrossCuttingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using System.Globalization;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.AgentPacket;
using Hexalith.Memories.Web.Components.Lenses.Benchmark;
using Hexalith.Memories.Web.Components.Lenses.CaseActivity;
using Hexalith.Memories.Web.Components.Lenses.Ingestion;
using Hexalith.Memories.Web.Components.Lenses.OperatorHealth;

using Shouldly;

/// <summary>
/// Story 17.4 — cross-cutting guardrail coverage that spans every lens: cross-lens consistency of the shared
/// shell, tenant isolation, role-density invariance, fail-closed authorization, unknown-scope handling,
/// contract-version stability, and stale-context command revalidation. These are the Task 6 guardrails that
/// are not specific to a single lens mapper.
/// </summary>
public sealed class LensCrossCuttingTests
{
    private static EvidencePacket Unauthorized(EvidencePacketIsolationStatus status, EvidencePacket source)
        => source with
        {
            Scope = new EvidencePacketScope(
                TenantId: source.Scope.TenantId,
                CaseId: source.Scope.CaseId,
                IsolationStatus: status,
                PermissionsContext: source.Scope.PermissionsContext),
        };

    // ----- Cross-lens consistency of the shared shell (same packet → identical trust context) -----

    [Theory]
    [InlineData(nameof(LensPacketFixtures.Happy))]
    [InlineData(nameof(LensPacketFixtures.Degraded))]
    [InlineData(nameof(LensPacketFixtures.Unauthorized))]
    [InlineData(nameof(LensPacketFixtures.Compressed))]
    [InlineData(nameof(LensPacketFixtures.Stale))]
    public void Shell_SamePacket_RendersIdenticalTrustContextAcrossEveryLens(string fixtureName)
    {
        EvidencePacket packet = ResolveFixture(fixtureName);

        LensShellViewModel[] shells = Enum.GetValues<LensKind>()
            .Select(lens => LensShellMapper.Map(packet, lens, LensRole.Operator, "memories/evidence?packet=memory-a"))
            .ToArray();

        // The same packet state, severity, affected capability, confidence, freshness, contract version, and
        // scope must read identically across all five lenses — that is the shared lens shell rule.
        shells.Select(s => s.StateKind).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.Severity).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.AffectedCapabilityKey).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.StateTitleKey).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.ConfidenceLabel).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.FreshnessLabel).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.Restrictive).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.TenantId).Distinct().Count().ShouldBe(1, fixtureName);
        shells.Select(s => s.CaseId).Distinct().Count().ShouldBe(1, fixtureName);

        // Only the lens identity and its title key differ between lenses.
        shells.Select(s => s.Lens).Distinct().Count().ShouldBe(shells.Length, fixtureName);
        shells.Select(s => s.LensTitleKey).Distinct().Count().ShouldBe(shells.Length, fixtureName);
    }

    [Fact]
    public void Shell_RestrictiveScope_SuppressesConfidenceConsistentlyAcrossEveryLens()
    {
        foreach (LensKind lens in Enum.GetValues<LensKind>())
        {
            LensShellViewModel shell = LensShellMapper.Map(
                LensPacketFixtures.Unauthorized(),
                lens,
                LensRole.Operator,
                "memories/evidence");

            shell.Restrictive.ShouldBeTrue(lens.ToString());
            shell.ConfidenceLabel.ShouldBe(LensResourceKeys.ConfidenceUnavailableText, lens.ToString());
        }
    }

    [Fact]
    public void Shell_AnyPacket_AlwaysReportsTheSupportedContractVersionAcrossEveryLens()
    {
        foreach (EvidencePacket packet in new[] { LensPacketFixtures.Happy(), LensPacketFixtures.SchemaMismatch(), LensPacketFixtures.CrossTenant() })
        {
            foreach (LensKind lens in Enum.GetValues<LensKind>())
            {
                LensShellViewModel shell = LensShellMapper.Map(packet, lens, LensRole.Developer, "memories/evidence");
                shell.ContractVersion.ShouldBe(InteractionContextSnapshot.SupportedContractVersion, lens.ToString());
            }
        }
    }

    // ----- Tenant isolation: changing the packet's tenant repartitions every derived field -----

    [Fact]
    public void Shell_CrossTenantPacket_CarriesForeignTenantScopeAcrossEveryLensWithoutResidue()
    {
        foreach (LensKind lens in Enum.GetValues<LensKind>())
        {
            LensShellViewModel shell = LensShellMapper.Map(
                LensPacketFixtures.CrossTenant(),
                lens,
                LensRole.Developer,
                "memories/evidence?packet=memory-a");

            shell.TenantId.ShouldBe("tenant-b", lens.ToString());
            shell.CaseId.ShouldBe("case-b", lens.ToString());
        }
    }

    [Fact]
    public void AgentPacket_TenantChange_RepartitionsCopyPayloadWithNoOriginatingTenantResidue()
    {
        AgentPacketInspectorViewModel tenantA = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.Happy(),
            LensRole.AgentIntegrator);
        AgentPacketInspectorViewModel tenantB = AgentPacketInspectorMapper.Map(
            LensPacketFixtures.CrossTenant(),
            LensRole.AgentIntegrator);

        tenantA.SafeCopyText.ShouldContain("tenant-a");
        tenantB.SafeCopyText.ShouldContain("tenant-b");

        // Switching the packet's tenant must reset the copy payload / command target — no tenant-a residue.
        tenantB.SafeCopyText.ShouldNotContain("tenant-a");
        tenantB.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.ScopeTenant).SafeValue.ShouldBe("tenant-b");
        tenantB.SchemaFields.Single(f => f.Kind == PacketSchemaFieldKind.ScopeCase).SafeValue.ShouldBe("case-b");
    }

    // ----- Role-density invariance: changing the role never changes packet semantics, only presentation -----

    [Fact]
    public void Ingestion_RoleChange_PreservesUnitSemantics()
    {
        string[] developer = IngestionSemantics(LensRole.Developer);
        string[] operatorView = IngestionSemantics(LensRole.Operator);
        string[] teamLead = IngestionSemantics(LensRole.TeamLead);

        operatorView.ShouldBe(developer);
        teamLead.ShouldBe(developer);
    }

    [Fact]
    public void OperatorHealth_RoleChange_PreservesCheckSemantics()
    {
        string[] developer = HealthSemantics(LensRole.Developer);
        string[] operatorView = HealthSemantics(LensRole.Operator);
        string[] teamLead = HealthSemantics(LensRole.TeamLead);

        operatorView.ShouldBe(developer);
        teamLead.ShouldBe(developer);
    }

    [Fact]
    public void Benchmark_RoleChange_ProducesIdenticalProjection()
    {
        // The benchmark comparator ignores the role entirely; the projection content must be identical.
        // (Record equality uses reference equality for the list members, so compare structurally.)
        BenchmarkResultComparatorViewModel developer = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.Degraded(), LensRole.Developer);
        BenchmarkResultComparatorViewModel teamLead = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.Degraded(), LensRole.TeamLead);

        developer.ResultState.ShouldBe(teamLead.ResultState);
        developer.NdcgAvailability.ShouldBe(teamLead.NdcgAvailability);
        developer.ThresholdAvailability.ShouldBe(teamLead.ThresholdAvailability);
        developer.IsEmpty.ShouldBe(teamLead.IsEmpty);
        developer.AxisRows.ShouldBe(teamLead.AxisRows);
        developer.UnavailableAxes.ShouldBe(teamLead.UnavailableAxes);
    }

    [Fact]
    public void AgentPacket_RoleChange_ProducesIdenticalProjection()
    {
        // The agent packet inspector ignores the role entirely; the projection content must be identical.
        AgentPacketInspectorViewModel developer = AgentPacketInspectorMapper.Map(LensPacketFixtures.Compressed(), LensRole.Developer);
        AgentPacketInspectorViewModel agent = AgentPacketInspectorMapper.Map(LensPacketFixtures.Compressed(), LensRole.AgentIntegrator);

        developer.SafeCopyText.ShouldBe(agent.SafeCopyText);
        developer.HasError.ShouldBe(agent.HasError);
        developer.Restrictive.ShouldBe(agent.Restrictive);
        developer.CountsAvailability.ShouldBe(agent.CountsAvailability);
        developer.TokenBudgetStateKey.ShouldBe(agent.TokenBudgetStateKey);
        developer.SchemaFields.ShouldBe(agent.SchemaFields);
        developer.OmittedFieldNames.ShouldBe(agent.OmittedFieldNames);
        developer.Expansions.ShouldBe(agent.Expansions);
    }

    // ----- Fail closed: no role broadens authorization on a restrictive packet -----

    [Theory]
    [InlineData(LensRole.Developer)]
    [InlineData(LensRole.Operator)]
    [InlineData(LensRole.TeamLead)]
    [InlineData(LensRole.AgentIntegrator)]
    public void EveryLens_UnauthorizedPacket_StaysRestrictiveForEveryRole(LensRole role)
    {
        EvidencePacket packet = LensPacketFixtures.Unauthorized();

        LensShellMapper.Map(packet, LensKind.CaseActivityTrail, role, null).Restrictive.ShouldBeTrue();

        CaseActivityTrailViewModel activity = CaseActivityTrailMapper.Map(packet, role);
        activity.IsEmpty.ShouldBeTrue();
        activity.Rows.ShouldNotContain(r => r.Kind == CaseActivityKind.SourceCitation);

        IngestionLifecycleViewModel ingestion = IngestionLifecycleMapper.Map(packet, role);
        ingestion.Units.ShouldAllBe(u => u.Outcome == IngestionOutcome.Unauthorized);

        OperatorHealthMatrixMapper.Map(packet, role).HasTrustBlocking.ShouldBeTrue();

        BenchmarkResultComparatorViewModel benchmark = BenchmarkResultComparatorMapper.Map(packet, role);
        benchmark.IsEmpty.ShouldBeTrue();
        benchmark.ResultState.ShouldBe(BenchmarkResultState.Unavailable);

        AgentPacketInspectorMapper.Map(packet, role).Restrictive.ShouldBeTrue();
    }

    // ----- Unknown isolation status is treated as restrictively as unauthorized, across every lens -----

    [Fact]
    public void EveryLens_UnknownIsolationScope_FailsClosedLikeUnauthorized()
    {
        EvidencePacket packet = LensPacketFixtures.UnknownScope();

        LensShellMapper.Map(packet, LensKind.AgentPacketInspector, LensRole.Operator, null).Restrictive.ShouldBeTrue();
        CaseActivityTrailMapper.Map(packet, LensRole.Operator).IsEmpty.ShouldBeTrue();
        IngestionLifecycleMapper.Map(packet, LensRole.Operator).Units.ShouldAllBe(u => u.Outcome == IngestionOutcome.Unauthorized);
        OperatorHealthMatrixMapper.Map(packet, LensRole.Operator).HasTrustBlocking.ShouldBeTrue();
        BenchmarkResultComparatorMapper.Map(packet, LensRole.Operator).NdcgAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        AgentPacketInspectorMapper.Map(packet, LensRole.Operator).Restrictive.ShouldBeTrue();
    }

    // ----- Stale / changed context revalidation: a previously-actionable command disables when the
    //       authorization context becomes restrictive, before any activation. -----

    [Fact]
    public void AgentPacket_CompressedPacketBecomesRestrictive_DisablesExpansionCommand()
    {
        AgentPacketInspectorViewModel safe = AgentPacketInspectorMapper.Map(LensPacketFixtures.Compressed(), LensRole.AgentIntegrator);
        safe.Expansions.ShouldNotBeEmpty();

        EvidencePacket restrictive = Unauthorized(EvidencePacketIsolationStatus.Unauthorized, LensPacketFixtures.Compressed());
        AgentPacketInspectorViewModel revalidated = AgentPacketInspectorMapper.Map(restrictive, LensRole.AgentIntegrator);

        revalidated.Restrictive.ShouldBeTrue();
        revalidated.Expansions.ShouldBeEmpty();
        revalidated.OmittedFieldNames.ShouldBeEmpty();
    }

    [Fact]
    public void Ingestion_DegradedPacketBecomesRestrictive_DisablesBackendRecoveryCommand()
    {
        IngestionLifecycleViewModel safe = IngestionLifecycleMapper.Map(LensPacketFixtures.Degraded(), LensRole.Operator);
        safe.Units.ShouldContain(u => u.RecoveryAvailable);

        EvidencePacket restrictive = Unauthorized(EvidencePacketIsolationStatus.Unauthorized, LensPacketFixtures.Degraded());
        IngestionUnitRow row = IngestionLifecycleMapper.Map(restrictive, LensRole.Operator).Units.ShouldHaveSingleItem();

        row.Outcome.ShouldBe(IngestionOutcome.Unauthorized);
        row.RecoveryAvailable.ShouldBeFalse();
    }

    private static string[] IngestionSemantics(LensRole role)
        => IngestionLifecycleMapper.Map(LensPacketFixtures.Degraded(), role).Units
            .Select(static u => string.Create(
                CultureInfo.InvariantCulture,
                $"{u.UnitId}|{u.Outcome}|{u.StageAvailability}|{u.SafeFailureSummary}|{u.AffectedCapabilityKey}|{u.RecoveryKind}|{u.RecoveryAvailable}|{u.Severity}"))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

    private static string[] HealthSemantics(LensRole role)
        => OperatorHealthMatrixMapper.Map(LensPacketFixtures.Degraded(), role).Checks
            .Select(static c => string.Create(
                CultureInfo.InvariantCulture,
                $"{c.Kind}|{c.Status}|{c.AffectedCapabilityKey}|{c.SafeEvidence}|{c.NextActionKind}|{c.NextActionAvailable}|{c.TrustBlocking}|{c.Severity}"))
            .OrderBy(static x => x, StringComparer.Ordinal)
            .ToArray();

    private static EvidencePacket ResolveFixture(string name) => name switch
    {
        nameof(LensPacketFixtures.Happy) => LensPacketFixtures.Happy(),
        nameof(LensPacketFixtures.Degraded) => LensPacketFixtures.Degraded(),
        nameof(LensPacketFixtures.Unauthorized) => LensPacketFixtures.Unauthorized(),
        nameof(LensPacketFixtures.Compressed) => LensPacketFixtures.Compressed(),
        nameof(LensPacketFixtures.Stale) => LensPacketFixtures.Stale(),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown fixture."),
    };
}
