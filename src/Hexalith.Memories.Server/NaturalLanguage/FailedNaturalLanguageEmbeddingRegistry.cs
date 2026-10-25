// <copyright file="FailedNaturalLanguageEmbeddingRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Serialization;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>Redis-backed natural-language embedding retry queue.</summary>
public sealed partial class FailedNaturalLanguageEmbeddingRegistry : IFailedNaturalLanguageEmbeddingRegistry
{
    internal const string LiveKeyPrefix = "nl-embedding-retry:";
    internal const string DeadKeyPrefix = "nl-embedding-retry-dead:";
    internal const string LivePayloadKeyPrefix = "nl-embedding-retry-payload:";
    internal const string DeadPayloadKeyPrefix = "nl-embedding-retry-dead-payload:";
    internal const string TenantBacklogKey = "nl-embedding-retry-tenants";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FailedNaturalLanguageEmbeddingRegistry> _logger;
    private readonly int _liveMaxEntries;
    private readonly int _deadMaxEntries;

    public FailedNaturalLanguageEmbeddingRegistry(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<FailedNaturalLanguageEmbeddingRegistry> logger,
        IOptions<NaturalLanguageDescriptionOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
        NaturalLanguageDescriptionOptions value = (options ?? Options.Create(new NaturalLanguageDescriptionOptions())).Value;
        _liveMaxEntries = ClampQueueMaxEntries(value.LiveRetryQueueMaxEntries);
        _deadMaxEntries = ClampQueueMaxEntries(value.DeadRetryQueueMaxEntries);
    }

    /// <inheritdoc/>
    public async Task EnqueueAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        if (!await MutateAsync(db, record, "enqueue", SerializeRecord(record)).ConfigureAwait(false)) return;
        await TrimLiveQueueAsync(db, record.TenantId, _liveMaxEntries, _deadMaxEntries).ConfigureAwait(false);
        LogEnqueued(_logger, record.TenantId, record.MemoryUnitId, record.Attempts);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>> DequeueBatchAsync(
        string tenantId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();

        int clamped = Math.Clamp(batchSize, 1, 100);
        IDatabase db = _redis.GetDatabase();
        RedisValue[] memoryUnitIds = await db
            .SortedSetRangeByRankAsync(LiveKey(tenantId), 0, clamped - 1, Order.Ascending)
            .ConfigureAwait(false);

        if (memoryUnitIds.Length == 0)
        {
            return [];
        }

        RedisValue[] payloads = await db.HashGetAsync(LivePayloadKey(tenantId), memoryUnitIds).ConfigureAwait(false);
        List<FailedNaturalLanguageEmbeddingRecord> records = new(memoryUnitIds.Length);
        List<RedisValue> corruptMembers = [];
        for (int i = 0; i < memoryUnitIds.Length; i++)
        {
            FailedNaturalLanguageEmbeddingRecord? parsed = payloads[i].HasValue
                ? TryDeserialize(payloads[i].ToString())
                : TryDeserialize(memoryUnitIds[i].ToString());
            if (parsed is not null && parsed.TenantId == tenantId
                && !await db.KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenantId, parsed.MemoryUnitId)).ConfigureAwait(false)
                && !await db.KeyExistsAsync(TenantIngestionOwnership.Fence(tenantId)).ConfigureAwait(false))
            {
                if (!payloads[i].HasValue && !await PromoteLegacyAsync(db, parsed, memoryUnitIds[i]).ConfigureAwait(false))
                    continue;
                records.Add(parsed);
                continue;
            }

            corruptMembers.Add(memoryUnitIds[i]);
        }

        if (corruptMembers.Count > 0)
        {
            await RemoveLiveEntriesAsync(db, tenantId, [.. corruptMembers]).ConfigureAwait(false);
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisKey payloadKey = LivePayloadKey(record.TenantId);
        string expectedPayload = SerializeRecord(record);
        RedisValue currentPayload = await db.HashGetAsync(payloadKey, record.MemoryUnitId).ConfigureAwait(false);
        if (currentPayload.HasValue && currentPayload != expectedPayload)
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "complete");
            return;
        }

        if (!await MutateAsync(db, record, "complete", expectedPayload).ConfigureAwait(false))
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "complete");
            return;
        }
        await RemoveTenantBacklogIfEmptyAsync(db, record.TenantId).ConfigureAwait(false);
        LogCompleted(_logger, record.TenantId, record.MemoryUnitId, record.Attempts);
    }

    /// <inheritdoc/>
    public async Task<bool> IncrementAttemptsAsync(
        FailedNaturalLanguageEmbeddingRecord record,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        FailedNaturalLanguageEmbeddingRecord next = record with { Attempts = record.Attempts + 1 };
        IDatabase db = _redis.GetDatabase();
        bool dead = next.Attempts >= maxAttempts;
        if (!await MutateAsync(db, record, dead ? "dead" : "requeue", SerializeRecord(next)).ConfigureAwait(false))
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "attempt-increment");
            return false;
        }
        if (dead)
        {
            LogDeadLettered(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
            await RemoveTenantBacklogIfEmptyAsync(db, record.TenantId).ConfigureAwait(false);
            await TrimQueueAsync(db, DeadKey(record.TenantId), DeadPayloadKey(record.TenantId), _deadMaxEntries).ConfigureAwait(false);
            return true;
        }
        LogAttemptIncremented(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
        await TrimLiveQueueAsync(db, record.TenantId, _liveMaxEntries, _deadMaxEntries).ConfigureAwait(false);
        return false;
    }

    /// <inheritdoc/>
    public async Task<long> GetBacklogCountAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IDatabase db = _redis.GetDatabase();
        return await db.SortedSetLengthAsync(LiveKey(tenantId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<long> GetBacklogBytesAsync(string tenantId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IDatabase db = _redis.GetDatabase();
        try
        {
            RedisResult result = await db.ExecuteAsync("MEMORY", "USAGE", LiveKey(tenantId)).ConfigureAwait(false);
            long liveBytes = result.IsNull ? 0 : (long)result;
            RedisResult payloadResult = await db.ExecuteAsync("MEMORY", "USAGE", LivePayloadKey(tenantId)).ConfigureAwait(false);
            long payloadBytes = payloadResult.IsNull ? 0 : (long)payloadResult;
            return liveBytes + payloadBytes;
        }
        catch (RedisException ex)
        {
            LogMemoryUsageUnavailable(_logger, ex, tenantId);
            return 0;
        }
        catch (TimeoutException ex)
        {
            LogMemoryUsageUnavailable(_logger, ex, tenantId);
            return 0;
        }
        catch (InvalidCastException ex)
        {
            // Review P13: a future Redis Stack release could change the MEMORY USAGE return shape
            // from an integer to a different RedisResult type. Cast throws InvalidCastException —
            // without this guard the telemetry pipeline propagates the exception and stops emitting
            // the gauge for ALL tenants until the next 30s tick.
            LogMemoryUsageUnavailable(_logger, ex, tenantId);
            return 0;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ListTenantsWithBacklogAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] tenantIds = await db.SetMembersAsync(TenantBacklogKey).ConfigureAwait(false);
        if (tenantIds.Length == 0)
        {
            await foreach (string tenantId in ListLegacyTenantsWithBacklogAsync(db, cancellationToken).ConfigureAwait(false))
            {
                yield return tenantId;
            }

            yield break;
        }

        foreach (RedisValue tenantId in tenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!tenantId.HasValue)
            {
                continue;
            }

            string value = tenantId.ToString();
            if (await db.SortedSetLengthAsync(LiveKey(value)).ConfigureAwait(false) <= 0)
            {
                _ = await db.SetRemoveAsync(TenantBacklogKey, tenantId).ConfigureAwait(false);
                continue;
            }

            yield return value;
        }
    }

    internal static string LiveKey(string tenantId) => LiveKeyPrefix + tenantId;

    internal static string DeadKey(string tenantId) => DeadKeyPrefix + tenantId;

    internal static string LivePayloadKey(string tenantId) => LivePayloadKeyPrefix + tenantId;

    internal static string DeadPayloadKey(string tenantId) => DeadPayloadKeyPrefix + tenantId;

    internal static string SerializeRecord(FailedNaturalLanguageEmbeddingRecord record)
        => JsonSerializer.Serialize(record, MemoriesPersistenceJsonContext.Options);

    internal static FailedNaturalLanguageEmbeddingRecord? TryDeserialize(string payload)
    {
        try
        {
            FailedNaturalLanguageEmbeddingRecord? record = JsonSerializer.Deserialize<FailedNaturalLanguageEmbeddingRecord>(
                payload,
                MemoriesPersistenceJsonContext.Options);

            // Review P12: a corrupted or partially-persisted record (null/empty TenantId or
            // MemoryUnitId) must not flow into the retry pipeline — downstream activities would
            // fail with NullReferenceException or enqueue work against a non-existent tenant.
            if (record is null
                || string.IsNullOrWhiteSpace(record.TenantId)
                || string.IsNullOrWhiteSpace(record.MemoryUnitId))
            {
                return null;
            }

            return record;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static int ClampQueueMaxEntries(int value) => Math.Clamp(value, 1, 100_000);

    private static RedisValue[] LiveMembers(FailedNaturalLanguageEmbeddingRecord record)
        => [record.MemoryUnitId, SerializeRecord(record)];

    private static async Task RemoveLiveEntriesAsync(IDatabase db, string tenantId, RedisValue[] members)
    {
        if (members.Length == 0)
        {
            return;
        }

        await RemoveQueueEntriesAsync(db, LiveKey(tenantId), LivePayloadKey(tenantId), members).ConfigureAwait(false);
        await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);
    }

    private static async Task RemoveTenantBacklogIfEmptyAsync(IDatabase db, string tenantId)
    {
        await db.ScriptEvaluateAsync("if redis.call('ZCARD',KEYS[1])==0 then return redis.call('SREM',KEYS[2],ARGV[1]) end return 0",
            [(RedisKey)LiveKey(tenantId), (RedisKey)TenantBacklogKey], [(RedisValue)tenantId]).ConfigureAwait(false);
    }

    private async IAsyncEnumerable<string> ListLegacyTenantsWithBacklogAsync(
        IDatabase db,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IServer? server = GetFirstConnectedServer();
        if (server is null)
        {
            yield break;
        }

        await foreach (RedisKey key in server
            .KeysAsync(database: db.Database, pattern: LiveKeyPrefix + "*", pageSize: 100)
            .WithCancellation(cancellationToken))
        {
            string value = key.ToString();
            if (value.Length <= LiveKeyPrefix.Length)
            {
                continue;
            }

            string tenantId = value[LiveKeyPrefix.Length..];
            if (await db.SortedSetLengthAsync(LiveKey(tenantId)).ConfigureAwait(false) <= 0)
            {
                continue;
            }

            _ = await db.SetAddAsync(TenantBacklogKey, tenantId).ConfigureAwait(false);
            yield return tenantId;
        }
    }

    private IServer? GetFirstConnectedServer()
    {
        foreach (EndPoint endpoint in _redis.GetEndPoints())
        {
            IServer server = _redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        return null;
    }

    private static async Task TrimLiveQueueAsync(IDatabase db, string tenantId, int liveMaxEntries, int deadMaxEntries)
    {
        long length = await db.SortedSetLengthAsync(LiveKey(tenantId)).ConfigureAwait(false);
        long excess = length - liveMaxEntries;
        if (excess <= 0)
        {
            return;
        }

        RedisValue[] oldestMembers = await db
            .SortedSetRangeByRankAsync(LiveKey(tenantId), 0, excess - 1, Order.Ascending)
            .ConfigureAwait(false);

        if (oldestMembers.Length == 0)
        {
            return;
        }

        RedisValue[] payloads = await db.HashGetAsync(LivePayloadKey(tenantId), oldestMembers).ConfigureAwait(false);
        List<RedisValue> removableMembers = new(oldestMembers.Length);
        for (int i = 0; i < oldestMembers.Length; i++)
        {
            FailedNaturalLanguageEmbeddingRecord? record = payloads[i].HasValue
                ? TryDeserialize(payloads[i].ToString())
                : TryDeserialize(oldestMembers[i].ToString());
            if (record is null)
            {
                removableMembers.Add(oldestMembers[i]);
                continue;
            }

            if (record.TenantId != tenantId)
            {
                throw new InvalidOperationException("nl-retry-payload-scope");
            }
            _ = await MutateAsync(db, record, "overflow", payloads[i].HasValue ? payloads[i].ToString() : SerializeRecord(record),
                oldestMembers[i]).ConfigureAwait(false);
        }

        if (removableMembers.Count > 0)
        {
            await RemoveQueueEntriesAsync(db, LiveKey(tenantId), LivePayloadKey(tenantId), [.. removableMembers]).ConfigureAwait(false);
        }
        await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);
        await TrimQueueAsync(db, DeadKey(tenantId), DeadPayloadKey(tenantId), deadMaxEntries).ConfigureAwait(false);
    }

    private static async Task TrimQueueAsync(IDatabase db, RedisKey queueKey, RedisKey payloadKey, int maxEntries)
    {
        long length = await db.SortedSetLengthAsync(queueKey).ConfigureAwait(false);
        long excess = length - maxEntries;
        if (excess <= 0)
        {
            return;
        }

        RedisValue[] oldestMemoryUnitIds = await db
            .SortedSetRangeByRankAsync(queueKey, 0, excess - 1, Order.Ascending)
            .ConfigureAwait(false);

        if (oldestMemoryUnitIds.Length == 0)
        {
            return;
        }

        await RemoveQueueEntriesAsync(db, queueKey, payloadKey, oldestMemoryUnitIds).ConfigureAwait(false);
    }

    private static async Task RemoveQueueEntriesAsync(IDatabase db, RedisKey queueKey, RedisKey payloadKey, RedisValue[] members)
    {
        const string trim = """
            for i,expected in ipairs({'zset','hash'}) do
                local kind=redis.call('TYPE',KEYS[i]).ok
                if kind~='none' and kind~=expected then return redis.error_reply('WRONGTYPE nl-retry-state') end
            end
            -- Bounded expansion stays in this one atomic, preflighted mutation.
            for start=1,#ARGV,256 do
                local last=math.min(start+255,#ARGV)
                redis.call('ZREM',KEYS[1],unpack(ARGV,start,last))
                redis.call('HDEL',KEYS[2],unpack(ARGV,start,last))
            end
            return 1
            """;
        _ = await db.ScriptEvaluateAsync(trim, [queueKey, payloadKey], members).ConfigureAwait(false);
    }

    private static async Task<bool> PromoteLegacyAsync(IDatabase db, FailedNaturalLanguageEmbeddingRecord record, RedisValue member)
    {
        return await MutateAsync(db, record, "promote", SerializeRecord(record), member).ConfigureAwait(false);
    }

    // One awaited Redis command owns preflight and mutation. EXEC itself does not roll back
    // successful sibling commands after WRONGTYPE; no source removal is queued separately.
    private static async Task<bool> MutateAsync(IDatabase db, FailedNaturalLanguageEmbeddingRecord record,
        string mode, string nextPayload, RedisValue legacyMember = default)
    {
        const string mutate = """
            -- nl-retry-atomic-move-v3
            for i,expected in ipairs({'zset','hash','zset','hash','set'}) do
                local kind=redis.call('TYPE',KEYS[i]).ok
                if kind~='none' and kind~=expected then return redis.error_reply('WRONGTYPE nl-retry-state') end
            end
            if redis.call('EXISTS',KEYS[6],KEYS[7])~=0 then return 0 end
            local unit,tenant,mode=ARGV[1],ARGV[2],ARGV[3]
            local current=redis.call('HGET',KEYS[2],unit)
            local legacy=ARGV[7]
            if mode=='promote' then
                if not redis.call('ZSCORE',KEYS[1],legacy) then return 0 end
                if current then
                    local ok,parsed=pcall(cjson.decode,current)
                    if not ok or parsed.tenantId~=tenant or parsed.memoryUnitId~=unit then return redis.error_reply('nl-retry-payload-scope') end
                    -- Only this exact, scoped alias is redundant; modern content wins unchanged.
                    if not redis.call('ZSCORE',KEYS[1],unit) then redis.call('ZADD',KEYS[1],parsed.queuedAtTicks,unit) end
                    redis.call('ZREM',KEYS[1],legacy); return 0
                end
            elseif mode~='enqueue' then
                if current and current~=ARGV[4] then return 0 end
                if not current and not redis.call('ZSCORE',KEYS[1],ARGV[4]) and not redis.call('ZSCORE',KEYS[1],legacy) then return 0 end
            end
            if mode=='complete' then
                redis.call('ZREM',KEYS[1],unit,ARGV[4]); redis.call('HDEL',KEYS[2],unit)
            elseif mode=='dead' or mode=='overflow' then
                redis.call('HSET',KEYS[4],unit,ARGV[5]); redis.call('ZADD',KEYS[3],ARGV[6],unit)
                redis.call('ZREM',KEYS[1],unit,ARGV[4],legacy); redis.call('HDEL',KEYS[2],unit,legacy)
            else
                redis.call('HSET',KEYS[2],unit,ARGV[5]); redis.call('ZADD',KEYS[1],ARGV[6],unit)
                if mode~='enqueue' then redis.call('ZREM',KEYS[1],ARGV[4],legacy) end
                redis.call('SADD',KEYS[5],tenant)
            end
            return 1
            """;
        RedisResult result = await db.ScriptEvaluateAsync(mutate,
            [(RedisKey)LiveKey(record.TenantId), (RedisKey)LivePayloadKey(record.TenantId),
                (RedisKey)DeadKey(record.TenantId), (RedisKey)DeadPayloadKey(record.TenantId),
                (RedisKey)TenantBacklogKey, (RedisKey)SourcePublicationVisibility.Tombstone(record.TenantId, record.MemoryUnitId),
                (RedisKey)TenantIngestionOwnership.Fence(record.TenantId)],
            [(RedisValue)record.MemoryUnitId, (RedisValue)record.TenantId, (RedisValue)mode,
                (RedisValue)(mode == "overflow" ? nextPayload : SerializeRecord(record)), (RedisValue)nextPayload, (RedisValue)record.QueuedAtTicks,
                legacyMember.HasValue ? legacyMember : RedisValue.EmptyString]).ConfigureAwait(false);
        return (long)result == 1;
    }

    /// <summary>Fences and physically removes exact-unit modern and legacy live/dead payloads.</summary>
    internal static async Task PurgeUnitAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        await db.StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, unit), "1").WaitAsync(token).ConfigureAwait(false);
        foreach ((string queue, string payloads) in new[] { (LiveKey(tenant), LivePayloadKey(tenant)), (DeadKey(tenant), DeadPayloadKey(tenant)) })
        {
            if (await db.SortedSetLengthAsync(queue).WaitAsync(token).ConfigureAwait(false) > 100_000)
            {
                throw new InvalidOperationException("nl-retry-legacy-owner-reconciliation-required");
            }
            await db.HashDeleteAsync(payloads, unit).WaitAsync(token).ConfigureAwait(false);
            await db.SortedSetRemoveAsync(queue, unit).WaitAsync(token).ConfigureAwait(false);
            await foreach (SortedSetEntry entry in db.SortedSetScanAsync(queue, pageSize: 100).WithCancellation(token))
            {
                FailedNaturalLanguageEmbeddingRecord? legacy = TryDeserialize(entry.Element.ToString());
                if (legacy is not null && legacy.TenantId == tenant && legacy.MemoryUnitId == unit)
                {
                    await db.SortedSetRemoveAsync(queue, entry.Element).WaitAsync(token).ConfigureAwait(false);
                }
            }
        }
        await RemoveTenantBacklogIfEmptyAsync(db, tenant).ConfigureAwait(false);
    }

    /// <summary>Reads current retry payload only while the exact unit remains admitted.</summary>
    internal static async Task<FailedNaturalLanguageEmbeddingRecord?> ReadCurrentAsync(IDatabase db, string tenant, string unit)
    {
        const string read = "if redis.call('EXISTS',KEYS[1],KEYS[2])~=0 or redis.call('EXISTS',KEYS[4])~=1 then return false end return redis.call('HGET',KEYS[3],ARGV[1])";
        RedisResult raw = await db.ScriptEvaluateAsync(read,
            [(RedisKey)SourcePublicationVisibility.Tombstone(tenant, unit), (RedisKey)TenantIngestionOwnership.Fence(tenant), (RedisKey)LivePayloadKey(tenant), (RedisKey)Hexalith.Memories.Server.Infrastructure.IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit)], [(RedisValue)unit]).ConfigureAwait(false);
        FailedNaturalLanguageEmbeddingRecord? record = raw.IsNull ? null : TryDeserialize(raw.ToString());
        if (record is not null && (record.TenantId != tenant || record.MemoryUnitId != unit))
        {
            throw new InvalidOperationException("nl-retry-payload-scope");
        }
        return record;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "NL retry queue: enqueued {MemoryUnitId} for tenant {TenantId} (attempts={Attempts}).")]
    private static partial void LogEnqueued(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Information, Message = "NL retry queue: completed {MemoryUnitId} for tenant {TenantId} (attempts={Attempts}).")]
    private static partial void LogCompleted(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NL retry queue: incremented {MemoryUnitId} attempts to {Attempts} for tenant {TenantId}.")]
    private static partial void LogAttemptIncremented(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "NL retry queue: {MemoryUnitId} reached dead-letter after {Attempts} attempts for tenant {TenantId} (event 9180).")]
    private static partial void LogDeadLettered(ILogger logger, string tenantId, string memoryUnitId, int attempts);

    [LoggerMessage(Level = LogLevel.Debug, Message = "NL retry queue: MEMORY USAGE unavailable for tenant {TenantId} — returning 0.")]
    private static partial void LogMemoryUsageUnavailable(ILogger logger, Exception exception, string tenantId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "NL retry queue: ignored stale {Operation} for {MemoryUnitId} on tenant {TenantId}; a newer retry payload exists.")]
    private static partial void LogStaleRecordIgnored(ILogger logger, string tenantId, string memoryUnitId, string operation);
}
