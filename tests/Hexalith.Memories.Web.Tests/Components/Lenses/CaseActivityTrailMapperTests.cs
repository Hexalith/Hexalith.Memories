// <copyright file="CaseActivityTrailMapperTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Lenses;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Lenses;
using Hexalith.Memories.Web.Components.Lenses.CaseActivity;

using Shouldly;

public sealed class CaseActivityTrailMapperTests
{
    [Fact]
    public void Map_NullPacket_Throws()
        => Should.Throw<ArgumentNullException>(() => CaseActivityTrailMapper.Map(null!, LensRole.Developer));

    [Fact]
    public void Map_Happy_ProducesSourceLinkedStatusLabelledRows()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.Happy(), LensRole.Developer);

        view.IsEmpty.ShouldBeFalse();
        view.TimestampsAvailable.ShouldBeTrue();
        view.OrderingBasisKey.ShouldBe(CaseActivityResourceKeys.OrderingBasis);
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.SourceCitation && r.LinkAvailability == LensFieldAvailability.Available);
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.TrustState);

        // Every row carries a non-empty localized status key (status never relies on color/position alone).
        view.Rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.StatusLabelKey));

        view.Rows.ShouldContain(r => r.TimestampAvailability == LensFieldAvailability.Available);
        view.Rows.ShouldContain(r => r.SafeTimestamp.StartsWith("2026-07-05T", StringComparison.Ordinal));

        // Order is deterministic and contiguous.
        view.Rows.Select(r => r.Order).ShouldBe(Enumerable.Range(0, view.Rows.Count));
    }

    [Fact]
    public void Map_MissingSource_RendersExplicitUnavailableLinkState()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.MissingSource(), LensRole.Developer);

        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.SourceCitation);
        view.Rows.First(r => r.Kind == CaseActivityKind.SourceCitation).LinkAvailability
            .ShouldBe(LensFieldAvailability.Unavailable);
    }

    [Fact]
    public void Map_SensitiveSource_RedactsLinkAndSummary()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.Sensitive(), LensRole.Developer);

        CaseActivityRow source = view.Rows.First(r => r.Kind == CaseActivityKind.SourceCitation);
        source.LinkAvailability.ShouldBe(LensFieldAvailability.Redacted);
        source.SafeLink.ShouldNotContain("C:\\");
        source.SafeSummary.ShouldNotContain("Bearer ");
        source.SafeSummary.ShouldNotContain("redis://");
    }

    [Fact]
    public void Map_Unauthorized_SuppressesSourceAndGraphActivity()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.Unauthorized(), LensRole.Operator);

        view.IsEmpty.ShouldBeTrue();
        view.Rows.ShouldNotContain(r => r.Kind == CaseActivityKind.SourceCitation);
        view.Rows.ShouldNotContain(r => r.Kind == CaseActivityKind.Relationship);

        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.TrustState);
        view.Rows.First(r => r.Kind == CaseActivityKind.TrustState).LinkAvailability
            .ShouldBe(LensFieldAvailability.Unauthorized);
    }

    [Fact]
    public void Map_SchemaMismatch_DoesNotThrowAndStaysSafe()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.SchemaMismatch(), LensRole.Developer);

        // The out-of-range state falls back to a safe unknown trust state, never an empty success.
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.TrustState);
    }

    [Fact]
    public void Map_EmptyPacket_IsEmptyButStillCarriesTrustStateContinuity()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.Empty(), LensRole.Developer);

        // No source/annotation/relationship/gap activity exists, but the trail is never silently blank:
        // the current trust state remains as continuity context with a localized status label.
        view.IsEmpty.ShouldBeTrue();
        view.Rows.ShouldNotContain(r => r.Kind == CaseActivityKind.SourceCitation);
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.TrustState);
        view.Rows.ShouldAllBe(r => !string.IsNullOrWhiteSpace(r.StatusLabelKey));
    }

    [Fact]
    public void Map_DegradedPacket_KeepsSourceActivityWithAStatusLabelledTrustState()
    {
        CaseActivityTrailViewModel view = CaseActivityTrailMapper.Map(LensPacketFixtures.Degraded(), LensRole.Developer);

        view.IsEmpty.ShouldBeFalse();
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.SourceCitation);
        view.Rows.ShouldContain(r => r.Kind == CaseActivityKind.TrustState);
        view.Rows.Select(r => r.Order).ShouldBe(Enumerable.Range(0, view.Rows.Count));
    }
}
