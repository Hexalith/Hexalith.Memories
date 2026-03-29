// <copyright file="EmbeddingRateLimiterActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors.Runtime;

/// <summary>DAPR Actor that enforces per-tenant embedding rate limits. Thin host delegating to <see cref="RateLimiterLogic"/>.</summary>
internal sealed class EmbeddingRateLimiterActor : Actor, IEmbeddingRateLimiterActor
{
    private const string StateName = "rateState";

    private readonly RateLimiterLogic _logic;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimiterActor"/> class.</summary>
    /// <param name="host">The actor host provided by the DAPR runtime.</param>
    public EmbeddingRateLimiterActor(ActorHost host)
        : base(host)
    {
        _logic = new RateLimiterLogic(TimeProvider.System);
    }

    /// <inheritdoc/>
    public async Task<RateLimitState> GetStateAsync()
    {
        return await GetOrCreateStateAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResetAsync()
    {
        RateLimitState state = await GetOrCreateStateAsync().ConfigureAwait(false);
        RateLimitState newState = _logic.Reset(state);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetCeilingAsync(int ceiling)
    {
        RateLimitState state = await GetOrCreateStateAsync().ConfigureAwait(false);
        RateLimitState newState = RateLimiterLogic.SetCeiling(state, ceiling);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TryConsumeAsync()
    {
        RateLimitState state = await GetOrCreateStateAsync().ConfigureAwait(false);
        (bool allowed, RateLimitState newState) = _logic.TryConsume(state);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
        return allowed;
    }

    private async Task<RateLimitState> GetOrCreateStateAsync()
    {
        ConditionalValue<RateLimitState> result = await StateManager
            .TryGetStateAsync<RateLimitState>(StateName)
            .ConfigureAwait(false);

        if (result.HasValue)
        {
            return result.Value;
        }

        RateLimitState defaultState = _logic.CreateDefaultState();
        await StateManager.SetStateAsync(StateName, defaultState).ConfigureAwait(false);
        return defaultState;
    }
}
