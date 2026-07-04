// <copyright file="SaveDedupKeyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that persists a dedup key to Redis after successful ingestion.</summary>
public sealed class SaveDedupKeyActivity : WorkflowActivity<DedupKeyInput, DedupKeySaveResult>
{
    private readonly IConnectionMultiplexer _redis;

    public SaveDedupKeyActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    public override async Task<DedupKeySaveResult> RunAsync(
        WorkflowActivityContext context,
        DedupKeyInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DedupKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MemoryUnitId);

        IDatabase db = _redis.GetDatabase();
        bool saved = await db.StringSetAsync(
            input.DedupKey,
            input.MemoryUnitId,
            expiry: null,
            when: When.NotExists,
            flags: CommandFlags.None).ConfigureAwait(false);

        if (saved)
        {
            return DedupKeySaveResult.Saved(input.MemoryUnitId);
        }

        RedisValue existing = await db.StringGetAsync(input.DedupKey).ConfigureAwait(false);
        if (!existing.HasValue)
        {
            throw new InvalidOperationException($"Dedup key '{input.DedupKey}' already existed but no winner value could be read.");
        }

        return DedupKeySaveResult.DuplicateExisting(existing.ToString());
    }
}
