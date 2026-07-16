// <copyright file="RateLimiterLogic.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

/// <summary>Testable rate limiting business logic extracted from the DAPR actor.</summary>
public sealed class RateLimiterLogic
{
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="RateLimiterLogic"/> class.</summary>
    /// <param name="timeProvider">The time provider for deterministic time control.</param>
    public RateLimiterLogic(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Creates the default initial state with 1500 requests per minute.</summary>
    /// <returns>A new <see cref="RateLimitState"/> with default values.</returns>
    public RateLimitState CreateDefaultState()
        => new(1500, _timeProvider.GetUtcNow().UtcDateTime, 1500);

    /// <summary>Attempts to consume one rate limit token.</summary>
    /// <param name="currentState">The current rate limit state.</param>
    /// <returns>A tuple of (allowed, newState) indicating whether the request is allowed and the updated state.</returns>
    public (bool Allowed, RateLimitState NewState) TryConsume(RateLimitState currentState)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        RateLimitState state = currentState;

        if (now - state.WindowStart >= WindowDuration)
        {
            state = state with { Remaining = state.CeilingPerMinute, WindowStart = now };
        }

        if (state.Remaining <= 0)
        {
            return (false, state);
        }

        state = state with { Remaining = state.Remaining - 1 };
        return (true, state);
    }

    /// <summary>Applies a current ceiling and attempts to consume one rate limit token.</summary>
    /// <param name="currentState">The current rate limit state.</param>
    /// <param name="ceiling">The current configured ceiling per minute.</param>
    /// <returns>A tuple of (allowed, newState) indicating whether the request is allowed and the updated state.</returns>
    public (bool Allowed, RateLimitState NewState) TryConsume(RateLimitState currentState, int ceiling)
    {
        RateLimitState state = SetCeiling(currentState, ceiling);
        return TryConsume(state);
    }

    /// <summary>Resets the rate limit state to full budget.</summary>
    /// <param name="currentState">The current rate limit state.</param>
    /// <returns>The reset state with full budget and updated window start.</returns>
    public RateLimitState Reset(RateLimitState currentState)
        => currentState with
        {
            Remaining = currentState.CeilingPerMinute,
            WindowStart = _timeProvider.GetUtcNow().UtcDateTime,
        };

    /// <summary>Updates the ceiling on the rate limit state.</summary>
    /// <param name="currentState">The current rate limit state.</param>
    /// <param name="ceiling">The new ceiling per minute.</param>
    /// <returns>The updated state with the new ceiling.</returns>
    public static RateLimitState SetCeiling(RateLimitState currentState, int ceiling)
    {
        if (ceiling <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ceiling), ceiling, "Ceiling must be greater than zero.");
        }

        return currentState with
        {
            CeilingPerMinute = ceiling,
            Remaining = Math.Min(currentState.Remaining, ceiling),
        };
    }

    /// <summary>Records a provider 429 by zero-flooring <see cref="RateLimitState.Remaining"/> and moving
    /// <see cref="RateLimitState.WindowStart"/> so the next refill opens at the provider Retry-After instant.</summary>
    /// <remarks>
    /// <para><see cref="TryConsume"/> refills when <c>now - WindowStart &gt;= 1 min</c>, so this method stores
    /// <c>WindowStart = retryOpen - 1 min</c>. That closes local admission until the provider Retry-After instant
    /// without adding a second full rate-limit window. The <c>retryAfterSeconds</c> parameter is clamped to the
    /// inclusive range [1, 3600] to defend against misbehaving providers.</para>
    /// </remarks>
    /// <param name="currentState">The current rate limit state.</param>
    /// <param name="retryAfterSeconds">Seconds the caller suggests waiting before the next provider call.</param>
    /// <returns>The updated state with budget zeroed and window start advanced.</returns>
    public RateLimitState ReportRateLimited(RateLimitState currentState, int retryAfterSeconds)
    {
        int clamped = Math.Clamp(retryAfterSeconds, 1, 3600);
        DateTime retryOpen = _timeProvider.GetUtcNow().UtcDateTime + TimeSpan.FromSeconds(clamped);
        return currentState with { Remaining = 0, WindowStart = retryOpen - WindowDuration };
    }
}
