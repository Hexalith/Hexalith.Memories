// <copyright file="SaveDedupKeyActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Server.Activities;
using Hexalith.Memories.EventStore;

using Dapr.Workflow;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that persists a dedup key to Redis after successful ingestion.</summary>
public sealed class SaveDedupKeyActivity : WorkflowTraceLinkedActivity<DedupKeyInput, DedupKeySaveResult>
{
    private const string PromotePreflightReservationScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            redis.call('SET', KEYS[1], ARGV[2])
            return 1
        end
        return 0
        """;

    private readonly IConnectionMultiplexer _redis;

    public SaveDedupKeyActivity([FromKeyedServices("redis")] IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc/>
    protected override async Task<DedupKeySaveResult> RunActivityAsync(
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

        if (PreflightDedupReservation.IsTransientReservation(existing.ToString()))
        {
            RedisResult promoted = await db.ScriptEvaluateAsync(
                PromotePreflightReservationScript,
                [(RedisKey)input.DedupKey],
                [(RedisValue)PreflightDedupReservation.ReservedValue, (RedisValue)input.MemoryUnitId]).ConfigureAwait(false);
            if ((long)promoted == 1L)
            {
                return DedupKeySaveResult.Saved(input.MemoryUnitId);
            }

            existing = await db.StringGetAsync(input.DedupKey).ConfigureAwait(false);
            if (!existing.HasValue)
            {
                throw new InvalidOperationException($"Dedup key '{input.DedupKey}' changed while its preflight reservation was being promoted.");
            }
        }

        return DedupKeySaveResult.DuplicateExisting(existing.ToString());
    }
}
