// <copyright file="Ac2RedisSkipReviewBy.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using System;

/// <summary>
/// Story 8.4 Task 7 — self-expiring skip infrastructure for AC #2 (Redis OTEL instrumentation).
/// AC #2 was framed as "informational only" until <c>StackExchange.Redis.Extensions.OpenTelemetry</c>
/// instrumentation registers on the Server's tracer pipeline; without a review trigger the skip
/// could silently persist for months.
/// <para>
/// This constant is the date AFTER WHICH the skip is no longer acceptable without an explicit
/// renewal. The unit test <c>Ac2SkipReviewBy_IsNotInThePast_AtStoryMergeTime</c> in this assembly
/// asserts the date sits at least 60 days in the future at merge time — so a contributor cannot
/// merge an extension that is already-expired (Task 7.4 regression guard).
/// </para>
/// <para>
/// Tracking issue (file at story merge time per Task 7.3, then paste the URL into both this
/// constant's XML doc + the <c>AssertWithinReviewWindow</c> diagnostic): see Story 8.4 Change Log
/// Rev 0.6 dev notes — placeholder URL is referenced when filing.
/// </para>
/// </summary>
internal static class Ac2RedisSkipReviewBy
{
    /// <summary>The date after which the AC #2 Redis-instrumentation skip MUST be re-evaluated.</summary>
    public static readonly DateOnly ReviewByDate = new(2026, 7, 1);

    /// <summary>Tracking issue URL for the Redis OTEL instrumentation work that flips this skip
    /// into a hard assertion. Updated when the issue is filed (Task 7.3).</summary>
    public const string TrackingIssueUrl = "https://github.com/Hexalith/Hexalith.Memories/issues/TBD-Story-8.4-Redis-OTEL-instrumentation";

    /// <summary>Throws an <see cref="InvalidOperationException"/> with a triage diagnostic when
    /// the current UTC date is on or after <see cref="ReviewByDate"/>. Caller is the AC #2 test;
    /// the diagnostic instructs the future contributor to either register the instrumentation
    /// (flipping the skip into a hard assertion) or to extend <see cref="ReviewByDate"/> with a
    /// linked tracking issue reference.</summary>
    /// <param name="now">The current UTC date (test injects for determinism).</param>
    public static void AssertWithinReviewWindow(DateOnly now)
    {
        if (now >= ReviewByDate)
        {
            throw new InvalidOperationException(
                $"AC #2 Redis-instrumentation skip review-by date ({ReviewByDate:yyyy-MM-dd}) has elapsed (now: {now:yyyy-MM-dd}). " +
                $"Either register `StackExchange.Redis.Extensions.OpenTelemetry` instrumentation on the Memories Server's tracer " +
                $"and convert this assertion into a hard check, OR extend `Ac2RedisSkipReviewBy.ReviewByDate` in source with a " +
                $"linked tracking issue reference. Tracking issue: {TrackingIssueUrl}");
        }
    }
}
