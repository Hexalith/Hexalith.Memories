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
}
