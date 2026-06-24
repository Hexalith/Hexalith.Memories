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
    public void Map_HappyPacket_ShowsAxisProxyButLeavesBenchmarkEvidenceUnavailable()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(
            LensPacketFixtures.Happy(),
            LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.MissingBaseline);
        view.AxisRows.ShouldNotBeEmpty();
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Unavailable);
        view.ThresholdAvailability.ShouldBe(LensFieldAvailability.Unavailable);
        view.PerQueryAvailability.ShouldBe(LensFieldAvailability.Unavailable);
        view.EvidenceLinkAvailability.ShouldBe(LensFieldAvailability.Unavailable);
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
    public void Map_EmptyPacket_IsEmptyWithMissingBaselineAndNoInferredBenchmark()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.Empty(), LensRole.TeamLead);

        view.IsEmpty.ShouldBeTrue();
        view.AxisRows.ShouldBeEmpty();
        view.ResultState.ShouldBe(BenchmarkResultState.MissingBaseline);
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Unavailable);
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
    public void Map_SchemaMismatchPacket_StaysAtMissingBaselineWithUnavailableBenchmarkFields()
    {
        BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(LensPacketFixtures.SchemaMismatch(), LensRole.TeamLead);

        view.ResultState.ShouldBe(BenchmarkResultState.MissingBaseline);
        view.NdcgAvailability.ShouldBe(LensFieldAvailability.Unavailable);
        view.ThresholdAvailability.ShouldBe(LensFieldAvailability.Unavailable);
    }

    [Fact]
    public void Map_EveryBoundedFixture_NeverInfersBenchmarkOnlyStatesOrScores()
    {
        foreach (EvidencePacket packet in LensPacketFixtures.All())
        {
            BenchmarkResultComparatorViewModel view = BenchmarkResultComparatorMapper.Map(packet, LensRole.TeamLead);

            // Regression / Inconclusive / Unreproducible require canonical Story 2.7 benchmark fixtures and
            // must never be inferred from the bounded inventory in the web layer.
            view.ResultState.ShouldNotBe(BenchmarkResultState.Inconclusive);
            view.ResultState.ShouldNotBe(BenchmarkResultState.Regression);
            view.ResultState.ShouldNotBe(BenchmarkResultState.Unreproducible);

            // NDCG@10 is never computed; it is always an explicit unavailable/unauthorized boundary.
            view.NdcgAvailability.ShouldBeOneOf(LensFieldAvailability.Unavailable, LensFieldAvailability.Unauthorized);
        }
    }
}
