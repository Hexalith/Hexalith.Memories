// <copyright file="ReleaseDedupKeyIfOwnedActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that removes a permanent dedup key only when it is still owned by this workflow.</summary>
public sealed class ReleaseDedupKeyIfOwnedActivity : WorkflowTraceLinkedActivity<DedupKeyInput, bool>
{
    private readonly IConnectionMultiplexer _redis;

    public ReleaseDedupKeyIfOwnedActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    protected override async Task<bool> RunActivityAsync(
        WorkflowActivityContext context,
        DedupKeyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DedupKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        IDatabase db = _redis.GetDatabase();
        return await db.StringDeleteAsync(
            input.DedupKey,
            ValueCondition.Equal(input.MemoryUnitId),
            CommandFlags.None).ConfigureAwait(false);
    }
}
