// <copyright file="OperatorHealthMatrixMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.OperatorHealth;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

public sealed class OperatorHealthMatrixMapperTests
{
    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => OperatorHealthMatrixMapper.Map(null!, LensRole.Operator));

    [Fact]
    public void Map_HappyPacket_RendersFixedContractChecks()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Happy(), LensRole.Developer);

        view.LastCheckedAvailable.ShouldBeFalse();
        view.LastCheckedNoteKey.ShouldBe(OperatorHealthResourceKeys.LastCheckedNote);
        view.Checks.Select(c => c.Kind).ShouldBe([
            OperatorCheckKind.TenantIsolation,
            OperatorCheckKind.Authorization,
            OperatorCheckKind.RetrievalBackend,
            OperatorCheckKind.AxisAvailability,
            OperatorCheckKind.GraphContext,
            OperatorCheckKind.DetailCompleteness,
        ]);
        view.HasTrustBlocking.ShouldBeFalse();
    }

    [Fact]
    public void Map_UnauthorizedPacket_TreatsScopeAsTrustBlockingAndSuppressesBackendChecks()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Unauthorized(), LensRole.Operator);

        view.HasTrustBlocking.ShouldBeTrue();
        view.Checks.ShouldContain(c =>
            c.Kind == OperatorCheckKind.TenantIsolation &&
            c.Status == OperatorCheckStatus.Blocked &&
            c.TrustBlocking);
        view.Checks.ShouldContain(c =>
            c.Kind == OperatorCheckKind.Authorization &&
            c.Status == OperatorCheckStatus.Blocked &&
            c.TrustBlocking);
        view.Checks.Where(c => c.Kind is OperatorCheckKind.RetrievalBackend or OperatorCheckKind.AxisAvailability)
            .ShouldAllBe(c => c.Status == OperatorCheckStatus.Unknown && c.SafeEvidence == "unavailable");
    }

    [Fact]
    public void Map_DegradedPacket_UsesSharedSeverityAndAffectedCapabilityLabels()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Degraded(), LensRole.Operator);

        OperatorHealthCheckRow retrieval = view.Checks.Single(c => c.Kind == OperatorCheckKind.RetrievalBackend);
        retrieval.Status.ShouldBe(OperatorCheckStatus.Degraded);
        retrieval.Severity.ShouldBe(RecoverySeverity.Warning);
        retrieval.AffectedCapabilityKey.ShouldBe(RecoveryResourceKeys.Capability(RecoveryCapability.Retrieval));

        OperatorHealthCheckRow axes = view.Checks.Single(c => c.Kind == OperatorCheckKind.AxisAvailability);
        axes.Status.ShouldBe(OperatorCheckStatus.Degraded);
        axes.SafeEvidence.ShouldContain("unavailableAxes=1");
    }

    [Fact]
    public void Map_CompressedPacket_OffersOnlyProducerSanctionedTokenBudgetAction()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Compressed(), LensRole.Operator);

        OperatorHealthCheckRow completeness = view.Checks.Single(c => c.Kind == OperatorCheckKind.DetailCompleteness);
        completeness.Status.ShouldBe(OperatorCheckStatus.Caution);
        completeness.NextActionKind.ShouldBe(EvidencePacketRecoveryKind.IncreaseTokenBudget);
        completeness.NextActionAvailable.ShouldBeTrue();
    }

    [Fact]
    public void Map_SensitivePacket_DoesNotLeakDiagnostics()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Sensitive(), LensRole.Operator);

        foreach (OperatorHealthCheckRow check in view.Checks)
        {
            check.SafeEvidence.ShouldNotContain("Bearer ");
            check.SafeEvidence.ShouldNotContain("redis://");
            check.SafeEvidence.ShouldNotContain("C:\\Users\\Jerome");
            check.SafeEvidence.ShouldNotContain("/home/jerome");
        }
    }

    [Fact]
    public void Map_EmptyPacket_RendersAllChecksWithoutTrustBlocking()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Empty(), LensRole.Operator);

        view.Checks.Count.ShouldBe(6);
        view.HasTrustBlocking.ShouldBeFalse();
        view.Checks.ShouldContain(c => c.Kind == OperatorCheckKind.Authorization && c.Status == OperatorCheckStatus.Healthy);
    }

    [Fact]
    public void Map_RedactedPacket_FlagsDetailCompletenessWithoutProducerActionOrLeak()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.Redacted(), LensRole.Operator);

        OperatorHealthCheckRow completeness = view.Checks.Single(c => c.Kind == OperatorCheckKind.DetailCompleteness);
        completeness.Status.ShouldBe(OperatorCheckStatus.Caution);

        // Redaction is not a token-budget compression, so no producer-sanctioned expansion action is offered.
        completeness.NextActionAvailable.ShouldBeFalse();
        view.HasTrustBlocking.ShouldBeFalse();
    }

    [Fact]
    public void Map_SchemaMismatchPacket_StaysSafeWithTheFixedCheckSet()
    {
        OperatorHealthViewModel view = OperatorHealthMatrixMapper.Map(LensPacketFixtures.SchemaMismatch(), LensRole.Operator);

        // The out-of-range state must not crash the matrix; the fixed contract checks still render and the
        // whitelisted evidence clues never leak raw diagnostics.
        view.Checks.Count.ShouldBe(6);
        view.Checks.ShouldAllBe(c => !c.SafeEvidence.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
    }
}
