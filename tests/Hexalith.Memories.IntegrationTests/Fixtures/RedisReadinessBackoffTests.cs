// <copyright file="RedisReadinessBackoffTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Fixtures;

using Hexalith.Memories.AppHost;

using Shouldly;

/// <summary>Executable tests for the AppHost Redis readiness backoff policy.</summary>
[Trait("Category", "Integration")]
public sealed class RedisReadinessBackoffTests
{
    private static readonly TimeSpan _ample = TimeSpan.FromMinutes(2);

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 4000)]
    [InlineData(4, 4000)]
    [InlineData(int.MaxValue, 4000)]
    public void GetDelay_DoublesUpToTheCap(int failedAttempts, int expectedBackoffMilliseconds)
    {
        RedisReadinessBackoff.GetDelay(failedAttempts, 0, _ample)
            .ShouldBe(TimeSpan.FromMilliseconds(expectedBackoffMilliseconds / 2.0));
        RedisReadinessBackoff.GetDelay(failedAttempts, 0.5, _ample)
            .ShouldBe(TimeSpan.FromMilliseconds(expectedBackoffMilliseconds * 0.75));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.999999)]
    public void GetDelay_JitterStaysBetweenHalfAndAllOfTheBackoff(double jitterSample)
    {
        TimeSpan delay = RedisReadinessBackoff.GetDelay(2, jitterSample, _ample);

        delay.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(1000));
        delay.ShouldBeLessThan(TimeSpan.FromMilliseconds(2000));
    }

    [Fact]
    public void GetDelay_NeverOutlastsTheDeadline()
    {
        RedisReadinessBackoff.GetDelay(10, 0.9, TimeSpan.FromMilliseconds(120))
            .ShouldBe(TimeSpan.FromMilliseconds(120));
        RedisReadinessBackoff.GetDelay(10, 0.9, TimeSpan.Zero).ShouldBe(TimeSpan.Zero);
        RedisReadinessBackoff.GetDelay(10, 0.9, TimeSpan.FromSeconds(-1)).ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void GetDelay_NeverProbesFasterThanTheFormerFixedCadence()
    {
        for (int attempt = 1; attempt <= 20; attempt++)
        {
            // Jitter sample 0 gives the shortest delay for each attempt.
            RedisReadinessBackoff.GetDelay(attempt, 0, _ample).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(500));
        }
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(-1, 0.5)]
    [InlineData(1, -0.1)]
    [InlineData(1, 1.0)]
    [InlineData(1, double.NaN)]
    public void GetDelay_InvalidArguments_Throw(int failedAttempts, double jitterSample)
        => Should.Throw<ArgumentOutOfRangeException>(
            () => RedisReadinessBackoff.GetDelay(failedAttempts, jitterSample, _ample));
}
