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
    [InlineData(1, 250)]
    [InlineData(2, 500)]
    [InlineData(3, 1000)]
    [InlineData(4, 2000)]
    [InlineData(5, 2000)]
    [InlineData(int.MaxValue, 2000)]
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
        TimeSpan delay = RedisReadinessBackoff.GetDelay(3, jitterSample, _ample);

        delay.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(500));
        delay.ShouldBeLessThan(TimeSpan.FromMilliseconds(1000));
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
    public void GetDelay_RepeatedFailuresProbeLessOftenThanTheFixedCadence()
    {
        TimeSpan total = TimeSpan.Zero;
        for (int attempt = 1; attempt <= 9; attempt++)
        {
            total += RedisReadinessBackoff.GetDelay(attempt, 0, _ample);
        }

        // The former fixed 500 ms cadence reached its tenth attempt after 4.5 seconds. The backoff spaces
        // repeated failures further apart while each wait stays bounded by the cap.
        total.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(4500));
        total.ShouldBeLessThanOrEqualTo(TimeSpan.FromSeconds(9));
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
