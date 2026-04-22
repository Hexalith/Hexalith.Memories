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
/// <para>
/// The horizon assertion now reads the current date via <see cref="TimeProvider.System"/> and adds a
/// one-day slack to the 60-day horizon so a test authored close to midnight UTC cannot flap when CI
/// runs the next hour — the review-by date always has at least 60 days of runway from "today",
/// rounded up.
/// </para>
/// </summary>
[Trait("Category", "Unit")]
public sealed class Ac2SkipReviewByTests
{
    /// <summary>Horizon in days the AC #2 review-by date must remain beyond the authoring moment.</summary>
    public const int HorizonDays = 60;

    /// <summary>
    /// Additional slack (in days) beyond <see cref="HorizonDays"/> applied when comparing against
    /// "today" via the injected clock. Absorbs CI-runner clock skew and the UTC midnight boundary
    /// between two otherwise-identical test runs (e.g. authored at 23:59 UTC on day N, executed at
    /// 00:01 UTC on day N+1).
    /// </summary>
    public const int ClockSkewSlackDays = 1;

    [Fact]
    public void Ac2SkipReviewBy_IsNotInThePast_AtStoryMergeTime()
        => Ac2SkipReviewBy_IsNotInThePast_Impl(TimeProvider.System);

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
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.TrackingReference);
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.ReviewByDate.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void AssertWithinReviewWindow_Throws_AfterReviewByDate()
    {
        DateOnly later = Ac2RedisSkipReviewBy.ReviewByDate.AddDays(30);
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => Ac2RedisSkipReviewBy.AssertWithinReviewWindow(later));
        ex.Message.ShouldContain("Either register");
        ex.Message.ShouldContain(Ac2RedisSkipReviewBy.TrackingReference);
    }

    /// <summary>
    /// Internal entry point that accepts an injected <see cref="TimeProvider"/> so a future test can
    /// exercise the horizon calculation against a deterministic clock (e.g. to validate the midnight
    /// boundary behavior explicitly). Keeping the public-facing fact parameterless preserves xUnit
    /// default-discovery semantics while leaving a test seam available.
    /// </summary>
    /// <param name="clock">The clock abstraction — use <see cref="TimeProvider.System"/> in production tests.</param>
    internal static void Ac2SkipReviewBy_IsNotInThePast_Impl(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        // Story 8.4 Task 7.4 regression guard: at any merge time, the AC #2 review-by date MUST
        // be at least HorizonDays in the future. If a contributor tries to extend by merging an
        // already-elapsed (or near-elapsed) date, this test fails LOUDLY before the merge lands —
        // the review-by mechanism degenerates into silent tech debt otherwise.
        //
        // The horizon is computed from the UTC date portion of "now" to avoid UTC-midnight flake:
        // the day boundary is the only meaningful unit when comparing DateOnly values.
        DateOnly today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime.Date);
        DateOnly horizon = today.AddDays(HorizonDays - ClockSkewSlackDays);

        Ac2RedisSkipReviewBy.ReviewByDate.DayNumber.ShouldBeGreaterThanOrEqualTo(
            horizon.DayNumber,
            $"Ac2RedisSkipReviewBy.ReviewByDate ({Ac2RedisSkipReviewBy.ReviewByDate:yyyy-MM-dd}) must be at least " +
            $"{HorizonDays} days in the future (horizon: {horizon:yyyy-MM-dd}, computed from today: {today:yyyy-MM-dd} " +
            $"with {ClockSkewSlackDays}-day clock-skew slack). Either register Redis OTEL instrumentation " +
            $"on the Memories Server's tracer (and convert AC #2 into a hard assertion) or extend " +
            $"Ac2RedisSkipReviewBy.ReviewByDate with a linked tracking issue. Tracking: {Ac2RedisSkipReviewBy.TrackingReference}");
    }
}
