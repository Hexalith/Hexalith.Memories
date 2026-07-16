// <copyright file="EmbeddingRateLimiterActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors.Runtime;

using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;

/// <summary>DAPR Actor that enforces per-tenant embedding rate limits. Thin host delegating to <see cref="RateLimiterLogic"/>.</summary>
internal sealed class EmbeddingRateLimiterActor : Actor, IEmbeddingRateLimiterActor
{
    private const string StateName = "rateState";

    private readonly RateLimiterLogic _logic;
    private readonly ILogger<EmbeddingRateLimiterActor> _logger;

    /// <summary>Initializes a new instance of the <see cref="EmbeddingRateLimiterActor"/> class.</summary>
    /// <param name="host">The actor host provided by the DAPR runtime.</param>
    /// <param name="logger">The logger.</param>
    public EmbeddingRateLimiterActor(ActorHost host, ILogger<EmbeddingRateLimiterActor> logger)
        : base(host)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logic = new RateLimiterLogic(TimeProvider.System);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RateLimitState> GetStateAsync()
    {
        return await GetStateOrDefaultAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ResetAsync()
    {
        RateLimitState state = await GetStateOrDefaultAsync().ConfigureAwait(false);
        RateLimitState newState = _logic.Reset(state);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetCeilingAsync(int ceiling)
    {
        RateLimitState state = await GetStateOrDefaultAsync().ConfigureAwait(false);
        RateLimitState newState = RateLimiterLogic.SetCeiling(state, ceiling);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TryConsumeAsync()
    {
        RateLimitState state = await GetStateOrDefaultAsync().ConfigureAwait(false);
        (bool allowed, RateLimitState newState) = _logic.TryConsume(state);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
        return allowed;
    }

    /// <inheritdoc/>
    public async Task<bool> TryConsumeWithCeilingAsync(int ceiling)
    {
        RateLimitState state = await GetStateOrDefaultAsync().ConfigureAwait(false);
        (bool allowed, RateLimitState newState) = _logic.TryConsume(state, ceiling);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
        return allowed;
    }

    /// <inheritdoc/>
    public async Task ReportRateLimitedAsync(int retryAfterSeconds)
    {
        RateLimitState state = await GetStateOrDefaultAsync().ConfigureAwait(false);
        RateLimitState newState = _logic.ReportRateLimited(state, retryAfterSeconds);
        await StateManager.SetStateAsync(StateName, newState).ConfigureAwait(false);
        RateLimitingLog.LogRateLimitActorUpdated(_logger, Id.GetId(), newState.Remaining, newState.WindowStart);
    }

    private async Task<RateLimitState> GetStateOrDefaultAsync()
    {
        ConditionalValue<RateLimitState> result = await StateManager
            .TryGetStateAsync<RateLimitState>(StateName)
            .ConfigureAwait(false);

        if (result.HasValue)
        {
            return result.Value;
        }

        return _logic.CreateDefaultState();
    }
}
