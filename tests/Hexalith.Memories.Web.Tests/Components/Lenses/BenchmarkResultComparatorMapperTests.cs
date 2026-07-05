// <copyright file="BenchmarkResultComparatorMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.Benchmark;

using Shouldly;

public sealed class BenchmarkResultComparatorMapperTests
{
    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => BenchmarkResultComparatorMapper.Map(null!, LensRole.TeamLead));

    [Fact]
    public void Map_HappyPacket_RendersBenchmarkEvidenceFromContractMetadata()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(
            LensPacketFixtures.Happy(),
            LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.Passed);
        view.AxisRows.ShouldNotBeEmpty();
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Available);
        view.SafeNdcg.ShouldContain("hybrid 0.875");
        view.ThresholdAvailability.ShouldBe(LensFieldAvailability.Available);
        view.SafeThreshold.ShouldBe("passed at 0.8");
        view.PerQueryAvailability.ShouldBe(LensFieldAvailability.Available);
        view.SafePerQuery.ShouldContain("q-claim-denied:0.91");
        view.EvidenceLinkAvailability.ShouldBe(LensFieldAvailability.Available);
        view.SafeEvidenceLink.ShouldBe("docs://benchmarks/benchmark-run-2026-07-05");
        view.ProxyNoteKey.ShouldBe(BenchmarkResourceKeys.ProxyNote);
    }

    [Fact]
    public void Map_DegradedPacket_RendersDegradedAxisStateAndUnavailableAxes()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(
            LensPacketFixtures.Degraded(),
            LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.DegradedAxis);
        view.UnavailableAxes.ShouldContain("graph");
    }

    [Fact]
    public void Map_StalePacket_RendersStaleBenchmarkBoundary()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(
            LensPacketFixtures.Stale(),
            LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.Stale);
    }

    [Fact]
    public void Map_UnauthorizedPacket_SuppressesAxisRowsAndMarksBenchmarkFieldsUnauthorized()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(
            LensPacketFixtures.Unauthorized(),
            LensRole.TeamLead);

        view.IsEmpty.ShouldBeTrue();
        view.AxisRows.ShouldBeEmpty();
        view.ResultState.ShouldBe(BenchmarkResultState.Unavailable);
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
        view.ThresholdAvailability.ShouldBe(LensFieldAvailability.Unauthorized);
    }

    [Fact]
    public void Map_NonFiniteAxisScore_RendersTextEquivalentUnavailable()
    {
        EvidencePacket packet = LensPacketFixtures.Happy();
        packet = packet with
        {
            Evidence = packet.Evidence with
            {
                AxisEvidence =
                [
                    new EvidencePacketAxisEvidence("semantic", double.NaN, "cosine", "invalid score"),
                ],
            },
        };

        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(packet, LensRole.TeamLead);

        BenchmarkAxisRow row = view.AxisRows.ShouldHaveSingleItem();
        row.HasScore.ShouldBeFalse();
        row.ScorePercent.ShouldBe(0);
        row.SafeScore.ShouldBe("score unavailable");
    }

    [Fact]
    public void Map_EmptyPacket_IsEmptyButKeepsContractBenchmarkMetadata()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.Empty(), LensRole.TeamLead);

        view.IsEmpty.ShouldBeTrue();
        view.AxisRows.ShouldBeEmpty();
        view.ResultState.ShouldBe(BenchmarkResultState.Passed);
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Available);
    }

    [Fact]
    public void Map_SensitiveAxisEvidence_ScrubsNormalizationAndDescription()
    {
        EvidencePacket packet = LensPacketFixtures.Happy();
        packet = packet with
        {
            Evidence = packet.Evidence with
            {
                AxisEvidence =
                [
                    new EvidencePacketAxisEvidence("semantic", 0.5d, "Bearer abc.def.ghi", "redis://localhost:6379 leaked reason"),
                ],
            },
        };

        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(packet, LensRole.TeamLead);

        BenchmarkAxisRow row = view.AxisRows.ShouldHaveSingleItem();
        row.SafeNormalization.ShouldNotContain("Bearer ");
        row.SafeDescription.ShouldNotContain("redis://");
        row.SafeDescription.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void Map_SchemaMismatchPacket_StillUsesContractBenchmarkMetadata()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.SchemaMismatch(), LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.Passed);
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Available);
        view.ThresholdAvailability.ShouldBe(LensFieldAvailability.Available);
    }

    [Fact]
    public void Map_EveryBoundedFixture_UsesOnlyContractBenchmarkOrRestrictiveAvailability()
    {
        foreach (EvidencePacket packet in LensPacketFixtures.All())
        {
            BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(packet, LensRole.TeamLead);

            view.ResultState.ShouldNotBe(BenchmarkResultState.Unreproducible);

            view.NdcgAvailability.ShouldBeOneOf(
                LensFieldAvailability.Available,
                LensFieldAvailability.Unavailable,
                LensFieldAvailability.Unauthorized);
        }
    }
}
