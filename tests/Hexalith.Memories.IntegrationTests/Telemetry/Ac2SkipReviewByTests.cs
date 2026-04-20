// <copyright file="Ac2SkipReviewByTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System;

using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using Shouldly;

/// <summary>
/// Story 8.4 Task 7 — unit-tier tests for the self-expiring AC #2 skip infrastructure.
/// <para>
/// AC #2 (Redis OTEL instrumentation, optional downstream span attribution) is informational-only
/// in the cross-process Aspire model — the test process cannot reflect on the Server's
/// <c>TracerProvider</c> registered sources from another process. To prevent the informational
/// status from silently persisting forever (Risk 9), <see cref="Ac2RedisSkipReviewBy.ReviewByDate"/>
/// pins a date past which the skip is no longer acceptable. This test class enforces that contract.
/// </para>
/// <para>
/// Marked <c>[Trait("Category", "Unit")]</c> so it runs on the per-PR lane (not Tier-3) — the
/// review-by date check is fast and deterministic, and a stale date should fail BEFORE the slow
/// merge-queue lane wastes minutes.
/// </para>
/// </summary>
[Trait("Category", "Unit")]
public sealed class Ac2SkipReviewByTests
{
    [Fact]
    public void Ac2SkipReviewBy_IsNotInThePast_AtStoryMergeTime()
    {
        // Story 8.4 Task 7.4 regression guard: at any merge time, the AC #2 review-by date MUST
        // be at least 60 days in the future. If a contributor tries to extend by merging an
        // already-elapsed (or near-elapsed) date, this test fails LOUDLY before the merge lands —
        // the review-by mechanism degenerates into silent tech debt otherwise.
        DateOnly horizon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));
        Ac2RedisSkipReviewBy.ReviewByDate.ShouldBeGreaterThan(
            horizon,
            $"Ac2RedisSkipReviewBy.ReviewByDate ({Ac2RedisSkipReviewBy.ReviewByDate:yyyy-MM-dd}) must be at least " +
            $"60 days in the future (horizon: {horizon:yyyy-MM-dd}). Either register Redis OTEL instrumentation " +
            $"on the Memories Server's tracer (and convert AC #2 into a hard assertion) or extend " +
            $"Ac2RedisSkipReviewBy.ReviewByDate with a linked tracking issue. Tracking: {Ac2RedisSkipReviewBy.TrackingIssueUrl}");
    }

    [Fact]
    public void AssertWithinReviewWindow_DoesNotThrow_BeforeReviewByDate()
    {
        // A "now" one day before the review-by date is still within the skip window.
        DateOnly justBefore = Ac2RedisSkipReviewBy.ReviewByDate.AddDays(-1);
        Should.NotThrow(() => Ac2RedisSkipReviewBy.AssertWithinReviewWindow(justBefore));
    }

    [Fact]
    public void AssertWithinReviewWindow_Throws_OnReviewByDate()
    {
        // The review-by date itself is exclusive: AC2 must already have been re-evaluated by then.
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => Ac2RedisSkipReviewBy.AssertWithinReviewWindow(Ac2RedisSkipReviewBy.ReviewByDate));
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.TrackingIssueUrl);
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.ReviewByDate.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void AssertWithinReviewWindow_Throws_AfterReviewByDate()
    {
        DateOnly later = Ac2RedisSkipReviewBy.ReviewByDate.AddDays(30);
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => Ac2RedisSkipReviewBy.AssertWithinReviewWindow(later));
        ex.Message.ShouldContain("Either register");
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.TrackingIssueUrl);
    }
}
