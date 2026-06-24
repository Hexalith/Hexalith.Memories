// <copyright file="IngestionLifecycleMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.Ingestion;
using Hexalith.Memories.Web.Components.Recovery;

using Shouldly;

public sealed class IngestionLifecycleMapperTests
{
    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => IngestionLifecycleMapper.Map(null!, LensRole.Operator));

    [Fact]
    public void Map_HappyPacket_RendersIndexedUnitsWithUnavailableStageBoundary()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.Happy(), LensRole.Operator);

        view.IsEmpty.ShouldBeFalse();
        view.StageTaxonomyAvailable.ShouldBeFalse();
        view.StageNoteKey.ShouldBe(IngestionLifecycleResourceKeys.StageNote);
        view.Units.ShouldAllBe(u => u.StageAvailability == LensFieldAvailability.Unavailable);
        view.Units.ShouldContain(u => u.Outcome == IngestionOutcome.Indexed);
        view.Units.ShouldAllBe(u => string.IsNullOrEmpty(u.SafeFailureSummary));
    }

    [Fact]
    public void Map_NotIngestedPacket_RendersTenantScopeRecoveryState()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.NotIngested(), LensRole.Operator);

        IngestionUnitRow unit = view.Units.ShouldHaveSingleItem();
        unit.UnitId.ShouldBe("tenant scope");
        unit.Outcome.ShouldBe(IngestionOutcome.NotIngestedYet);
        unit.SafeFailureSummary.ShouldNotBeNullOrWhiteSpace();
        unit.Severity.ShouldBe(RecoverySeverity.Warning);
    }

    [Fact]
    public void Map_DegradedPacket_DistinguishesBackendDegradationWithoutSecrets()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.Degraded(), LensRole.Operator);

        view.Units.ShouldContain(u => u.Outcome == IngestionOutcome.Degraded);
        view.Units.ShouldAllBe(u => !u.SafeFailureSummary.Contains("redis://", StringComparison.OrdinalIgnoreCase));
        view.Units.ShouldAllBe(u => !u.SafeFailureSummary.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_BackendUnavailable_RendersBackendUnavailableOutcome()
    {
        EvidencePacket packet = LensPacketFixtures.Happy();
        packet = packet with
        {
            OmittedDetails = packet.OmittedDetails with { Reason = EvidencePacketOmissionReason.BackendUnavailable },
        };

        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(packet, LensRole.Operator);

        view.Units.ShouldContain(u => u.Outcome == IngestionOutcome.BackendUnavailable);
        view.HighestSeverity.ShouldBe(RecoverySeverity.Warning);
    }

    [Fact]
    public void Map_UnauthorizedPacket_SuppressesUnitDetailAndKeepsAuthorizationBoundary()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.Unauthorized(), LensRole.Operator);

        IngestionUnitRow unit = view.Units.ShouldHaveSingleItem();
        unit.UnitId.ShouldBe("unit unavailable");
        unit.StageAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        unit.Outcome.ShouldBe(IngestionOutcome.Unauthorized);
        unit.Severity.ShouldBe(RecoverySeverity.Critical);
        unit.SafeFailureSummary.ShouldNotContain("memory-secret");
    }

    [Fact]
    public void Map_EmptyPacket_LeavesTrackerEmptyWithoutInventingUnits()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.Empty(), LensRole.Operator);

        // A search that simply matched nothing is not an ingestion signal: the tracker stays empty rather
        // than fabricating a unit, and reports no severity.
        view.IsEmpty.ShouldBeTrue();
        view.Units.ShouldBeEmpty();
        view.HighestSeverity.ShouldBe(RecoverySeverity.None);
        view.StageTaxonomyAvailable.ShouldBeFalse();
    }

    [Fact]
    public void Map_SchemaMismatchPacket_FailsClosedToASafeRowWithoutThrowingOrLeaking()
    {
        IngestionLifecycleViewModel view = IngestionLifecycleMapper.Map(LensPacketFixtures.SchemaMismatch(), LensRole.Operator);

        // The out-of-range state must not crash the projection or produce empty success; the stage stays an
        // explicit unavailable boundary and no raw payload leaks into the failure summary.
        view.Units.ShouldNotBeEmpty();
        view.Units.ShouldAllBe(u => u.StageAvailability == LensFieldAvailability.Unavailable);
        view.Units.ShouldAllBe(u => !u.SafeFailureSummary.Contains("Bearer ", StringComparison.OrdinalIgnoreCase));
    }
}
