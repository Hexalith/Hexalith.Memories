// <copyright file="CheckIdempotencyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.EventStore;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that checks whether a source has already been ingested (dedup).</summary>
public sealed class CheckIdempotencyActivity : WorkflowActivity<IdempotencyInput, IdempotencyResult>
{
    private readonly IConnectionMultiplexer _redis;

    public CheckIdempotencyActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    public override async Task<IdempotencyResult> RunAsync(
        WorkflowActivityContext context,
        IdempotencyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.SourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.CaseId);

        string dedupKey = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
        IDatabase db = _redis.GetDatabase();
        RedisValue existing = await db.StringGetAsync(dedupKey).ConfigureAwait(false);

        if (PreflightDedupReservation.IsTransientReservation(existing.ToString()))
        {
            return new IdempotencyResult(false, null);
        }

        return existing.HasValue
            ? new IdempotencyResult(true, existing.ToString())
            : new IdempotencyResult(false, null);
    }
}
