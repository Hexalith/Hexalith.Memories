// <copyright file="RateLimiterLogicTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Actors;

using Hexalith.Memories.Server.Actors;

using Microsoft.Extensions.Time.Testing;

using Shouldly;

public class RateLimiterLogicTests
{
    [Fact]
    public void TryConsume_FirstCallWithinWindow_ReturnsTrueAndDecrementsRemaining()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Act
        (bool allowed, RateLimitState newState) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeTrue();
        newState.Remaining.ShouldBe(1499);
    }

    [Fact]
    public void TryConsume_BudgetExhausted_ReturnsFalse()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Consume all 1500 tokens
        for (int i = 0; i < 1500; i++)
        {
            (_, state) = logic.TryConsume(state);
        }

        // Act — 1501st call
        (bool allowed, RateLimitState _) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeFalse();
    }

    [Fact]
    public void TryConsume_WindowExpiredAt60Seconds_ResetsBudget()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Consume all tokens
        for (int i = 0; i < 1500; i++)
        {
            (_, state) = logic.TryConsume(state);
        }

        // Advance time by exactly 60 seconds
        timeProvider.Advance(TimeSpan.FromSeconds(60));

        // Act
        (bool allowed, RateLimitState newState) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeTrue();
        newState.Remaining.ShouldBe(1499); // reset to 1500, then consumed 1
    }

    [Fact]
    public void TryConsume_WindowNotYetExpiredAt59Seconds_DoesNotResetBudget()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Consume all tokens
        for (int i = 0; i < 1500; i++)
        {
            (_, state) = logic.TryConsume(state);
        }

        // Advance time by 59 seconds (NOT enough to reset)
        timeProvider.Advance(TimeSpan.FromSeconds(59));

        // Act
        (bool allowed, RateLimitState _) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeFalse();
    }

    [Fact]
    public void CreateDefaultState_ReturnsCorrectDefaults()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);

        // Act
        RateLimitState state = logic.CreateDefaultState();

        // Assert
        state.Remaining.ShouldBe(1500);
        state.CeilingPerMinute.ShouldBe(1500);
    }

    [Fact]
    public void TryConsume_CustomCeiling_ResetsToCustomCeilingAfterWindowExpiry()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();
        state = RateLimiterLogic.SetCeiling(state, 500);

        // Consume all 500 tokens
        for (int i = 0; i < 500; i++)
        {
            (_, state) = logic.TryConsume(state);
        }

        // Advance window
        timeProvider.Advance(TimeSpan.FromSeconds(60));

        // Act
        (bool allowed, RateLimitState newState) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeTrue();
        newState.Remaining.ShouldBe(499); // reset to 500, then consumed 1
        newState.CeilingPerMinute.ShouldBe(500);
    }

    [Fact]
    public void SetCeiling_NonPositiveCeiling_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => RateLimiterLogic.SetCeiling(state, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => RateLimiterLogic.SetCeiling(state, -1));
    }

    [Fact]
    public void SetCeiling_LowerThanRemaining_ClampsCurrentWindow()
    {
        // Arrange
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with { Remaining = 1200 };

        // Act
        RateLimitState updatedState = RateLimiterLogic.SetCeiling(state, 500);

        // Assert
        updatedState.CeilingPerMinute.ShouldBe(500);
        updatedState.Remaining.ShouldBe(500);
    }

    [Fact]
    public void TryConsume_WithLowerCeiling_ClampsAndConsumesAtomically()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with { Remaining = 1200, CeilingPerMinute = 1500 };

        (bool allowed, RateLimitState newState) = logic.TryConsume(state, 500);

        allowed.ShouldBeTrue();
        newState.CeilingPerMinute.ShouldBe(500);
        newState.Remaining.ShouldBe(499);
    }

    [Fact]
    public void TryConsume_WithHigherCeiling_PreservesLowerRemainingAndConsumes()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with { Remaining = 50, CeilingPerMinute = 100 };

        (bool allowed, RateLimitState newState) = logic.TryConsume(state, 500);

        allowed.ShouldBeTrue();
        newState.CeilingPerMinute.ShouldBe(500);
        newState.Remaining.ShouldBe(49);
    }

    [Fact]
    public void TryConsume_WithCurrentCeilingAfterWindowExpiry_ResetsToCurrentCeilingThenConsumes()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with
        {
            Remaining = 0,
            CeilingPerMinute = 100,
            WindowStart = timeProvider.GetUtcNow().UtcDateTime,
        };
        timeProvider.Advance(TimeSpan.FromSeconds(60));

        (bool allowed, RateLimitState newState) = logic.TryConsume(state, 250);

        allowed.ShouldBeTrue();
        newState.CeilingPerMinute.ShouldBe(250);
        newState.Remaining.ShouldBe(249);
        newState.WindowStart.ShouldBe(timeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void TryConsume_WithNonPositiveCeiling_ThrowsArgumentOutOfRangeException()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        Should.Throw<ArgumentOutOfRangeException>(() => logic.TryConsume(state, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => logic.TryConsume(state, -1));
    }

    [Fact]
    public void TryConsume_SerializedConcurrentEquivalentWithCeilingOne_AdmitsExactlyOne()
    {
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();
        int admitted = 0;

        for (int i = 0; i < 32; i++)
        {
            (bool allowed, state) = logic.TryConsume(state, 1);
            if (allowed)
            {
                admitted++;
            }
        }

        admitted.ShouldBe(1);
        state.CeilingPerMinute.ShouldBe(1);
        state.Remaining.ShouldBe(0);
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-500, 1)]
    [InlineData(10000, 3600)]
    [InlineData(3600, 3600)]
    [InlineData(1, 1)]
    public void ReportRateLimited_ClampsRetryAfterToRange(int retryAfter, int expectedClamped)
    {
        // Arrange
        DateTimeOffset baseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(baseTime);
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        // Act
        RateLimitState newState = logic.ReportRateLimited(state, retryAfter);

        // Assert
        newState.Remaining.ShouldBe(0, "Remaining should be zero-floored after 429 feedback.");
        newState.WindowStart.ShouldBe(baseTime.UtcDateTime.AddSeconds(expectedClamped).AddMinutes(-1));
        newState.CeilingPerMinute.ShouldBe(state.CeilingPerMinute);
    }

    [Fact]
    public void TryConsume_InsidePausedWindow_ReturnsFalse()
    {
        // Arrange
        DateTimeOffset baseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(baseTime);
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        state = logic.ReportRateLimited(state, 30);

        // Act — advance 29 s (still inside paused window)
        timeProvider.Advance(TimeSpan.FromSeconds(29));
        (bool allowed, RateLimitState newState) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeFalse();
        newState.Remaining.ShouldBe(0);
    }

    [Fact]
    public void TryConsume_AtRetryAfterInstant_RefillsBudget()
    {
        // Arrange
        DateTimeOffset baseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(baseTime);
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState();

        state = logic.ReportRateLimited(state, 30);

        timeProvider.Advance(TimeSpan.FromSeconds(30));

        // Act
        (bool allowed, RateLimitState newState) = logic.TryConsume(state);

        // Assert
        allowed.ShouldBeTrue();
        newState.Remaining.ShouldBe(state.CeilingPerMinute - 1);
    }

    [Fact]
    public void ReportRateLimited_ThenTryConsume_FollowsSerializedOrdering()
    {
        // Ordering test: a TryConsume observed before ReportRateLimited is NOT retroactively throttled;
        // the next TryConsume after the report fails fast; once the refill window elapses, budget returns.
        // Arrange
        DateTimeOffset baseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(baseTime);
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with { Remaining = 100, CeilingPerMinute = 100 };

        // Act — TryConsume at T=0
        (bool allowed0, state) = logic.TryConsume(state);
        allowed0.ShouldBeTrue();
        state.Remaining.ShouldBe(99);

        // ReportRateLimited(30) at T=1s
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        state = logic.ReportRateLimited(state, 30);
        state.Remaining.ShouldBe(0);
        state.WindowStart.ShouldBe(baseTime.UtcDateTime.AddSeconds(31).AddMinutes(-1));

        // TryConsume at T=2s — still paused
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        (bool allowed2, state) = logic.TryConsume(state);
        allowed2.ShouldBeFalse();

        // TryConsume at the provider retry-open instant — window refilled
        timeProvider.Advance(TimeSpan.FromSeconds(29)); // total: baseTime + 31s
        (bool allowed3, state) = logic.TryConsume(state);
        allowed3.ShouldBeTrue();
        state.Remaining.ShouldBe(99);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    public void TryConsume_BeforeRetryAfterInstantClosedAndAtInstantOpen(int retryAfterSeconds)
    {
        DateTimeOffset baseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        FakeTimeProvider timeProvider = new(baseTime);
        RateLimiterLogic logic = new(timeProvider);
        RateLimitState state = logic.CreateDefaultState() with { Remaining = 100, CeilingPerMinute = 100 };

        state = logic.ReportRateLimited(state, retryAfterSeconds);
        timeProvider.Advance(TimeSpan.FromSeconds(retryAfterSeconds - 1));
        (bool allowedBefore, state) = logic.TryConsume(state);
        allowedBefore.ShouldBeFalse();
        state.Remaining.ShouldBe(0);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        (bool allowedAtOpen, RateLimitState openState) = logic.TryConsume(state);
        allowedAtOpen.ShouldBeTrue();
        openState.Remaining.ShouldBe(99);
    }
}
