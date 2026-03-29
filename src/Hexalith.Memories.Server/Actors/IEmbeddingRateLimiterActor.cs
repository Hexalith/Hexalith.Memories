// <copyright file="IEmbeddingRateLimiterActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors;

/// <summary>DAPR Actor interface for per-tenant embedding rate limiting.</summary>
public interface IEmbeddingRateLimiterActor : IActor
{
    /// <summary>Attempts to consume one rate limit token.</summary>
    /// <returns>True if the request is within budget; false if the rate limit is exhausted.</returns>
    Task<bool> TryConsumeAsync();

    /// <summary>Resets the rate limit window to full budget.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResetAsync();

    /// <summary>Gets the current rate limit state.</summary>
    /// <returns>The current <see cref="RateLimitState"/>.</returns>
    Task<RateLimitState> GetStateAsync();

    /// <summary>Updates the rate limit ceiling (requests per minute).</summary>
    /// <param name="ceiling">The new ceiling value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetCeilingAsync(int ceiling);
}
