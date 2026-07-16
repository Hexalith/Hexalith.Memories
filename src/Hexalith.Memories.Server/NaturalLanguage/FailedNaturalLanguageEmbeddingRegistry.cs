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

        string json = SerializeRecord(record);
        IDatabase db = _redis.GetDatabase();
        await db.SetAddAsync(TenantBacklogKey, record.TenantId).ConfigureAwait(false);
        await db.HashSetAsync(LivePayloadKey(record.TenantId), record.MemoryUnitId, json).ConfigureAwait(false);
        await db.SortedSetAddAsync(LiveKey(record.TenantId), record.MemoryUnitId, record.QueuedAtTicks).ConfigureAwait(false);
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
            if (parsed is not null)
            {
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

        ITransaction tx = db.CreateTransaction();
        if (currentPayload.HasValue)
        {
            tx.AddCondition(Condition.HashEqual(payloadKey, record.MemoryUnitId, expectedPayload));
        }

        Task<long> removeTask = tx.SortedSetRemoveAsync(LiveKey(record.TenantId), LiveMembers(record));
        _ = tx.HashDeleteAsync(payloadKey, record.MemoryUnitId);
        bool committed = await tx.ExecuteAsync().ConfigureAwait(false);
        if (!committed)
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "complete");
            return;
        }

        long removed = await removeTask.ConfigureAwait(false);
        await RemoveTenantBacklogIfEmptyAsync(db, record.TenantId).ConfigureAwait(false);
        if (removed > 0)
        {
            LogCompleted(_logger, record.TenantId, record.MemoryUnitId, record.Attempts);
        }
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
        string nextJson = SerializeRecord(next);

        IDatabase db = _redis.GetDatabase();
        RedisKey livePayloadKey = LivePayloadKey(record.TenantId);
        string expectedPayload = SerializeRecord(record);
        RedisValue currentPayload = await db.HashGetAsync(livePayloadKey, record.MemoryUnitId).ConfigureAwait(false);
        if (currentPayload.HasValue && currentPayload != expectedPayload)
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "attempt-increment");
            return false;
        }

        ITransaction tx = db.CreateTransaction();
        if (currentPayload.HasValue)
        {
            tx.AddCondition(Condition.HashEqual(livePayloadKey, record.MemoryUnitId, expectedPayload));
        }

        _ = tx.SortedSetRemoveAsync(LiveKey(record.TenantId), record.MemoryUnitId);
        _ = tx.SortedSetRemoveAsync(LiveKey(record.TenantId), SerializeRecord(record));
        _ = tx.HashDeleteAsync(livePayloadKey, record.MemoryUnitId);

        if (next.Attempts >= maxAttempts)
        {
            _ = tx.HashSetAsync(DeadPayloadKey(record.TenantId), record.MemoryUnitId, nextJson);
            _ = tx.SortedSetAddAsync(DeadKey(record.TenantId), record.MemoryUnitId, next.QueuedAtTicks);
            bool deadCommitted = await tx.ExecuteAsync().ConfigureAwait(false);
            if (!deadCommitted)
            {
                LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "dead-letter");
                return false;
            }

            LogDeadLettered(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
            await RemoveTenantBacklogIfEmptyAsync(db, record.TenantId).ConfigureAwait(false);
            await TrimQueueAsync(db, DeadKey(record.TenantId), DeadPayloadKey(record.TenantId), _deadMaxEntries).ConfigureAwait(false);
            return true;
        }

        _ = tx.HashSetAsync(LivePayloadKey(record.TenantId), record.MemoryUnitId, nextJson);
        _ = tx.SortedSetAddAsync(LiveKey(record.TenantId), record.MemoryUnitId, next.QueuedAtTicks);
        bool liveCommitted = await tx.ExecuteAsync().ConfigureAwait(false);
        if (!liveCommitted)
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "attempt-increment");
            return false;
        }

        LogAttemptIncremented(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
        await db.SetAddAsync(TenantBacklogKey, record.TenantId).ConfigureAwait(false);
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

        _ = await db.SortedSetRemoveAsync(LiveKey(tenantId), members).ConfigureAwait(false);
        _ = await db.HashDeleteAsync(LivePayloadKey(tenantId), members).ConfigureAwait(false);
        await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);
    }

    private static async Task RemoveTenantBacklogIfEmptyAsync(IDatabase db, string tenantId)
    {
        long remaining = await db.SortedSetLengthAsync(LiveKey(tenantId)).ConfigureAwait(false);
        if (remaining == 0)
        {
            _ = await db.SetRemoveAsync(TenantBacklogKey, tenantId).ConfigureAwait(false);
        }
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

            string payload = payloads[i].HasValue ? payloads[i].ToString() : SerializeRecord(record);
            _ = await db.HashSetAsync(DeadPayloadKey(tenantId), record.MemoryUnitId, payload).ConfigureAwait(false);
            _ = await db.SortedSetAddAsync(DeadKey(tenantId), record.MemoryUnitId, record.QueuedAtTicks).ConfigureAwait(false);
            removableMembers.Add(oldestMembers[i]);
        }

        if (removableMembers.Count > 0)
        {
            _ = await db.SortedSetRemoveAsync(LiveKey(tenantId), [.. removableMembers]).ConfigureAwait(false);
            _ = await db.HashDeleteAsync(LivePayloadKey(tenantId), [.. removableMembers]).ConfigureAwait(false);
            await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);
            await TrimQueueAsync(db, DeadKey(tenantId), DeadPayloadKey(tenantId), deadMaxEntries).ConfigureAwait(false);
        }
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

        _ = await db.SortedSetRemoveAsync(queueKey, oldestMemoryUnitIds).ConfigureAwait(false);
        _ = await db.HashDeleteAsync(payloadKey, oldestMemoryUnitIds).ConfigureAwait(false);
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
