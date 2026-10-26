// <copyright file="FailedNaturalLanguageEmbeddingRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Net;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>Gets the registered retry database used for durable scheduling authority.</summary>
    internal IDatabase Database => _redis.GetDatabase();
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
        await TrimLiveQueueAsync(db, record.TenantId, _liveMaxEntries, _deadMaxEntries, cancellationToken).ConfigureAwait(false);
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
        var selected = await SelectAsync(db, LiveKey(tenantId), LivePayloadKey(tenantId), clamped, cancellationToken).ConfigureAwait(false);
        RedisValue[] memoryUnitIds = selected.Select(entry => entry.Member).ToArray();
        RedisValue[] payloads = selected.Select(entry => entry.Payload).ToArray();
        List<FailedNaturalLanguageEmbeddingRecord> records = new(memoryUnitIds.Length);

        for (int i = 0; i < memoryUnitIds.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double? score = selected[i].Score;
            FailedNaturalLanguageEmbeddingRecord? parsed = payloads[i].HasValue
                ? TryDeserialize(payloads[i].ToString())
                : TryDeserialize(memoryUnitIds[i].ToString());
            if (parsed is not null && payloads[i].HasValue && !HasQualifiedShape(payloads[i].ToString()))
                throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
            if (parsed is not null && (parsed.TenantId != tenantId || (payloads[i].HasValue && parsed.MemoryUnitId != memoryUnitIds[i])))
                throw new InvalidOperationException("nl-retry-payload-scope");
            if (parsed is not null && parsed.TenantId == tenantId
                && !await db.KeyExistsAsync(SourcePublicationVisibility.Tombstone(tenantId, parsed.MemoryUnitId)).ConfigureAwait(false)
                && !await db.KeyExistsAsync(TenantIngestionOwnership.Fence(tenantId)).ConfigureAwait(false))
            {
                if (!payloads[i].HasValue && (!HasQualifiedShape(memoryUnitIds[i].ToString()) || !await PromoteLegacyAsync(db, parsed, memoryUnitIds[i], score).ConfigureAwait(false)))
                {
                    RedisValue modern = await db.HashGetAsync(LivePayloadKey(tenantId), parsed.MemoryUnitId).WaitAsync(cancellationToken).ConfigureAwait(false);
                    var current = modern.HasValue ? TryDeserialize(modern.ToString()) : null;
                    if (current is null)
                    {
                        if (!HasQualifiedShape(memoryUnitIds[i].ToString())) throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
                        continue;
                    }
                    if (current.TenantId != tenantId || current.MemoryUnitId != parsed.MemoryUnitId) throw new InvalidOperationException("nl-retry-payload-scope");
                    parsed = current;
                }
                if (!records.Any(record => record.MemoryUnitId == parsed.MemoryUnitId)) records.Add(parsed);
                continue;
            }

            if (parsed is not null && !HasQualifiedShape(payloads[i].HasValue ? payloads[i].ToString() : memoryUnitIds[i].ToString()))
                throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
            await RemoveObservedAsync(db, tenantId, LiveKey(tenantId), LivePayloadKey(tenantId), memoryUnitIds[i], payloads[i], score, cancellationToken).ConfigureAwait(false);
        }
        await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);

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
        if (currentPayload.HasValue && TryDeserialize(currentPayload.ToString()) != record)
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "complete");
            return;
        }

        if (currentPayload.HasValue)
        {
            if (!HasQualifiedShape(currentPayload.ToString())) throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
            expectedPayload = currentPayload.ToString();
        }
        List<RedisValue> aliases = [];
        if (await db.SortedSetLengthAsync(LiveKey(record.TenantId)).WaitAsync(cancellationToken).ConfigureAwait(false) > 100_000)
            throw new InvalidOperationException("nl-retry-legacy-owner-reconciliation-required");
        await foreach (SortedSetEntry entry in db.SortedSetScanAsync(LiveKey(record.TenantId), pageSize: 100).WithCancellation(cancellationToken))
        {
            FailedNaturalLanguageEmbeddingRecord? alias = TryDeserialize(entry.Element.ToString());
            if (alias is not null && HasQualifiedShape(entry.Element.ToString()) && SameLineage(record, alias))
            {
                RedisValue payload = await db.HashGetAsync(payloadKey, entry.Element).WaitAsync(cancellationToken).ConfigureAwait(false);
                if (payload.HasValue) throw new InvalidOperationException("nl-retry-payload-scope");
                aliases.AddRange([entry.Element, (RedisValue)entry.Score, payload.HasValue ? "1" : "0", payload.HasValue ? payload : RedisValue.EmptyString]);
            }
        }
        if (!await MutateAsync(db, record, "complete", expectedPayload, aliases: [.. aliases], expectedPayload: expectedPayload).ConfigureAwait(false))
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
        RedisValue current = await db.HashGetAsync(LivePayloadKey(record.TenantId), record.MemoryUnitId).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (current.HasValue && (TryDeserialize(current.ToString()) != record || !HasQualifiedShape(current.ToString()))) return false;
        if (!await MutateAsync(db, record, dead ? "dead" : "requeue", SerializeRecord(next), expectedPayload: current.HasValue ? current.ToString() : null).ConfigureAwait(false))
        {
            LogStaleRecordIgnored(_logger, record.TenantId, record.MemoryUnitId, "attempt-increment");
            return false;
        }
        if (dead)
        {
            LogDeadLettered(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
            await RemoveTenantBacklogIfEmptyAsync(db, record.TenantId).ConfigureAwait(false);
            await TrimQueueAsync(db, record.TenantId, DeadKey(record.TenantId), DeadPayloadKey(record.TenantId), _deadMaxEntries, cancellationToken).ConfigureAwait(false);
            return true;
        }
        LogAttemptIncremented(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
        await TrimLiveQueueAsync(db, record.TenantId, _liveMaxEntries, _deadMaxEntries, cancellationToken).ConfigureAwait(false);
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
                await RemoveTenantBacklogIfEmptyAsync(db, value).ConfigureAwait(false);
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

    /// <summary>Rejects unknown, duplicate and omitted admission properties before historical lineage authorizes mutation.</summary>
    internal static bool HasQualifiedShape(string payload)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            HashSet<string> expected = new(StringComparer.OrdinalIgnoreCase)
            {
                "tenantId", "memoryUnitId", "truncatedRawJsonPayload", "eventType", "aggregateType", "caseId",
                "embeddingProvider", "embeddingModel", "embeddingDimensions", "queuedAtTicks", "attempts",
            };
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
                if (!expected.Remove(property.Name)) return false;
            return expected.Count == 0;
        }
        catch (JsonException) { return false; }
    }

    /// <summary>Captures rank selection, exact payload bytes and scores in one Redis observation.</summary>
    internal static async Task<(RedisValue Member, RedisValue Payload, double Score)[]> SelectAsync(
        IDatabase db, RedisKey queue, RedisKey payloads, int count, CancellationToken token)
    {
        const string select = """
            -- nl-retry-observed-selection-v1
            local members=redis.call('ZRANGE',KEYS[1],0,tonumber(ARGV[1])-1,'WITHSCORES')
            local observed={}
            for i=1,#members,2 do
                observed[#observed+1]=members[i]
                observed[#observed+1]=members[i+1]
                observed[#observed+1]=redis.call('HGET',KEYS[2],members[i]) or false
            end
            return observed
            """;
        RedisResult raw = await db.ScriptEvaluateAsync(select, [queue, payloads], [(RedisValue)Math.Clamp(count, 1, 100_000)]).WaitAsync(token).ConfigureAwait(false);
        RedisResult[] values = (RedisResult[])raw!;
        var result = new (RedisValue Member, RedisValue Payload, double Score)[values.Length / 3];
        for (int i = 0; i < result.Length; i++)
            result[i] = (values[3 * i].ToString(), values[3 * i + 2].IsNull ? RedisValue.Null : (RedisValue)values[3 * i + 2].ToString(),
                double.Parse(values[3 * i + 1].ToString(), CultureInfo.InvariantCulture));
        return result;
    }

    internal static int ClampQueueMaxEntries(int value) => Math.Clamp(value, 1, 100_000);

    /// <summary>Matches all immutable admission fields, allowing only attempt counters to differ.</summary>
    internal static bool SameLineage(FailedNaturalLanguageEmbeddingRecord left, FailedNaturalLanguageEmbeddingRecord right)
        => left with { Attempts = 0 } == right with { Attempts = 0 };

    /// <summary>Computes the content-free version of the complete retry record.</summary>
    internal static string PayloadVersion(FailedNaturalLanguageEmbeddingRecord record)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(SerializeRecord(record))));

    /// <summary>Gets the exact-unit completed-lineage marker key.</summary>
    internal static string LineageKey(string tenant, string unit) => "nl-retry-completed-lineage:" + tenant + ":" + unit;

    /// <summary>Computes immutable admission lineage independently of retry attempts.</summary>
    internal static string Lineage(FailedNaturalLanguageEmbeddingRecord record)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(SerializeRecord(record with { Attempts = 0 }))));

    /// <summary>Deletes only the selected member, payload bytes and score observed by the caller.</summary>
    internal static async Task<bool> RemoveObservedAsync(IDatabase db, string tenant, RedisKey queue, RedisKey payloads,
        RedisValue member, RedisValue payload, double? score, CancellationToken token)
    {
        const string prune = """
            -- nl-retry-observed-prune-v1
            local score=redis.call('ZSCORE',KEYS[1],ARGV[1])
            if (ARGV[2]=='' and score) or (ARGV[2]~='' and (not score or tonumber(score)~=tonumber(ARGV[2]))) then return 0 end
            local payload=redis.call('HGET',KEYS[2],ARGV[1])
            if (ARGV[3]=='1' and payload~=ARGV[4]) or (ARGV[3]=='0' and payload) then return 0 end
            if ARGV[5]~='' then
                if redis.call('SCARD',KEYS[3])>=65536 and redis.call('SISMEMBER',KEYS[3],ARGV[5])==0 then return redis.error_reply('nl-retry-history-capacity') end
                redis.call('SADD',KEYS[3],ARGV[5])
            end
            if ARGV[5]=='' then redis.call('SET',KEYS[4],'1') end
            redis.call('ZREM',KEYS[1],ARGV[1]); redis.call('HDEL',KEYS[2],ARGV[1]); return 1
            """;
        string obligation = TryDeserialize(payload.HasValue ? payload.ToString() : member.ToString())?.MemoryUnitId ?? string.Empty;
        RedisResult result = await db.ScriptEvaluateAsync(prune, [queue, payloads, (RedisKey)RetryWorkflowHistory.Units(tenant), (RedisKey)RetryWorkflowHistory.Unresolved(tenant)],
            [member, score.HasValue ? score.Value.ToString("R", CultureInfo.InvariantCulture) : "", payload.HasValue ? "1" : "0",
                payload.HasValue ? payload : RedisValue.EmptyString, (RedisValue)obligation]).WaitAsync(token).ConfigureAwait(false);
        return (long)result == 1;
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

    private static async Task TrimLiveQueueAsync(IDatabase db, string tenantId, int liveMaxEntries, int deadMaxEntries, CancellationToken token = default)
    {
        long length = await db.SortedSetLengthAsync(LiveKey(tenantId)).ConfigureAwait(false);
        long excess = length - liveMaxEntries;
        if (excess <= 0)
        {
            return;
        }

        var selected = await SelectAsync(db, LiveKey(tenantId), LivePayloadKey(tenantId), (int)Math.Min(excess, 100_000), token).ConfigureAwait(false);
        RedisValue[] oldestMembers = selected.Select(entry => entry.Member).ToArray();
        RedisValue[] payloads = selected.Select(entry => entry.Payload).ToArray();
        for (int i = 0; i < oldestMembers.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            double? score = selected[i].Score;
            FailedNaturalLanguageEmbeddingRecord? record = payloads[i].HasValue
                ? TryDeserialize(payloads[i].ToString())
                : TryDeserialize(oldestMembers[i].ToString());
            if (record is null)
            {
                await RemoveObservedAsync(db, tenantId, LiveKey(tenantId), LivePayloadKey(tenantId), oldestMembers[i], payloads[i], score, token).ConfigureAwait(false);
                continue;
            }

            if (!HasQualifiedShape(payloads[i].HasValue ? payloads[i].ToString() : oldestMembers[i].ToString()))
                throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
            if (record.TenantId != tenantId || (payloads[i].HasValue && record.MemoryUnitId != oldestMembers[i]))
            {
                throw new InvalidOperationException("nl-retry-payload-scope");
            }
            _ = await MutateAsync(db, record, "overflow", payloads[i].HasValue ? payloads[i].ToString() : SerializeRecord(record),
                oldestMembers[i], score, payloads[i]).ConfigureAwait(false);
        }

        await RemoveTenantBacklogIfEmptyAsync(db, tenantId).ConfigureAwait(false);
        await TrimQueueAsync(db, tenantId, DeadKey(tenantId), DeadPayloadKey(tenantId), deadMaxEntries, token).ConfigureAwait(false);
    }

    private static async Task TrimQueueAsync(IDatabase db, string tenant, RedisKey queueKey, RedisKey payloadKey, int maxEntries, CancellationToken token)
    {
        long excess = await db.SortedSetLengthAsync(queueKey).WaitAsync(token).ConfigureAwait(false) - maxEntries;
        if (excess <= 0) return;
        var selected = await SelectAsync(db, queueKey, payloadKey, (int)Math.Min(excess, 100_000), token).ConfigureAwait(false);
        RedisValue[] members = selected.Select(entry => entry.Member).ToArray();
        RedisValue[] payloads = selected.Select(entry => entry.Payload).ToArray();
        for (int i = 0; i < members.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            FailedNaturalLanguageEmbeddingRecord? record = TryDeserialize(payloads[i].HasValue ? payloads[i].ToString() : members[i].ToString());
            if (record is not null && !HasQualifiedShape(payloads[i].HasValue ? payloads[i].ToString() : members[i].ToString()))
                throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
            if (record is not null && (record.TenantId != tenant || (payloads[i].HasValue && record.MemoryUnitId != members[i])))
                throw new InvalidOperationException("nl-retry-payload-scope");
            double? score = selected[i].Score;
            await RemoveObservedAsync(db, tenant, queueKey, payloadKey, members[i], payloads[i], score, token).ConfigureAwait(false);
        }
    }

    private static async Task<bool> PromoteLegacyAsync(IDatabase db, FailedNaturalLanguageEmbeddingRecord record, RedisValue member, double? score)
    {
        return await MutateAsync(db, record, "promote", SerializeRecord(record), member, score).ConfigureAwait(false);
    }

    // One awaited Redis command owns preflight and mutation. EXEC itself does not roll back
    // successful sibling commands after WRONGTYPE; no source removal is queued separately.
    private static async Task<bool> MutateAsync(IDatabase db, FailedNaturalLanguageEmbeddingRecord record,
        string mode, string nextPayload, RedisValue legacyMember = default, double? observedScore = null, RedisValue observedPayload = default, RedisValue[]? aliases = null, string? expectedPayload = null)
    {
        IngestionIdentityGuard.Scope(record.TenantId, record.MemoryUnitId);
        IngestionIdentityGuard.Handle(record.MemoryUnitId);
        const string mutate = """
            -- nl-retry-atomic-move-v3
            for i,expected in ipairs({'zset','hash','zset','hash','set'}) do
                local kind=redis.call('TYPE',KEYS[i]).ok
                if kind~='none' and kind~=expected then return redis.error_reply('WRONGTYPE nl-retry-state') end
            end
            for i,expected in pairs({[8]='string',[9]='set'}) do
                local kind=redis.call('TYPE',KEYS[i]).ok
                if kind~='none' and kind~=expected then return redis.error_reply('WRONGTYPE nl-retry-state') end
            end
            if redis.call('EXISTS',KEYS[6],KEYS[7])~=0 then return 0 end
            local unit,tenant,mode=ARGV[1],ARGV[2],ARGV[3]
            local current=redis.call('HGET',KEYS[2],unit)
            local legacy=ARGV[7]
            if mode=='overflow' or mode=='promote' then
                local score=redis.call('ZSCORE',KEYS[1],legacy)
                local selected=redis.call('HGET',KEYS[2],legacy)
                if not score or ARGV[8]=='' or tonumber(score)~=tonumber(ARGV[8]) then return 0 end
                if (ARGV[9]=='1' and selected~=ARGV[10]) or (ARGV[9]=='0' and selected) then return 0 end
            end
            if mode=='overflow' and legacy~=unit and (current or redis.call('ZSCORE',KEYS[1],unit)) then return 0 end
            if mode=='promote' then
                if not redis.call('ZSCORE',KEYS[1],legacy) then return 0 end
                if not current and redis.call('ZSCORE',KEYS[1],unit) then return redis.error_reply('nl-retry-legacy-lineage-reconciliation-required') end
                if current then
                    local ok,parsed=pcall(cjson.decode,current)
                    if not ok or parsed.tenantId~=tenant or parsed.memoryUnitId~=unit then return redis.error_reply('nl-retry-payload-scope') end
                    -- Completion alone may retire aliases after independent lineage verification.
                    return 0
                end
                if redis.call('EXISTS',KEYS[8])==1 then return redis.error_reply('nl-retry-legacy-lineage-reconciliation-required') end
            elseif mode~='enqueue' then
                if current and current~=ARGV[4] then return 0 end
                if not current and not redis.call('ZSCORE',KEYS[1],ARGV[4]) and not redis.call('ZSCORE',KEYS[1],legacy) then return 0 end
            end
            if mode=='dead' or mode=='overflow' then
                local target=redis.call('HGET',KEYS[4],unit)
                if target and target~=ARGV[4] and target~=ARGV[5] then return 0 end
            end
            if redis.call('SCARD',KEYS[9])>=65536 and redis.call('SISMEMBER',KEYS[9],unit)==0 then return redis.error_reply('nl-retry-history-capacity') end
            redis.call('SADD',KEYS[9],unit)
            if mode=='complete' then
                redis.call('SET',KEYS[8],ARGV[11])
                redis.call('ZREM',KEYS[1],unit); redis.call('HDEL',KEYS[2],unit)
                for i=12,#ARGV,4 do
                    local score=redis.call('ZSCORE',KEYS[1],ARGV[i])
                    local payload=redis.call('HGET',KEYS[2],ARGV[i])
                    if score and tonumber(score)==tonumber(ARGV[i+1]) and ((ARGV[i+2]=='0' and not payload) or (ARGV[i+2]=='1' and payload==ARGV[i+3])) then
                        redis.call('ZREM',KEYS[1],ARGV[i]); redis.call('HDEL',KEYS[2],ARGV[i])
                    end
                end
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
                (RedisKey)TenantIngestionOwnership.Fence(record.TenantId), (RedisKey)LineageKey(record.TenantId, record.MemoryUnitId),
                (RedisKey)RetryWorkflowHistory.Units(record.TenantId)],
            [(RedisValue)record.MemoryUnitId, (RedisValue)record.TenantId, (RedisValue)mode,
                (RedisValue)(expectedPayload ?? (mode == "overflow" ? nextPayload : SerializeRecord(record))), (RedisValue)nextPayload, (RedisValue)record.QueuedAtTicks,
                legacyMember.HasValue ? legacyMember : RedisValue.EmptyString,
                observedScore.HasValue ? observedScore.Value.ToString("R", CultureInfo.InvariantCulture) : "",
                observedPayload.HasValue ? "1" : "0", observedPayload.HasValue ? observedPayload : RedisValue.EmptyString,
                (RedisValue)Lineage(record), .. aliases ?? []]).ConfigureAwait(false);
        return (long)result == 1;
    }

    /// <summary>Fences and physically removes exact-unit modern and legacy live/dead payloads.</summary>
    internal static async Task PurgeUnitAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        await FenceUnitAsync(db, tenant, unit, token).ConfigureAwait(false);
        foreach ((string queue, string payloads) in new[] { (LiveKey(tenant), LivePayloadKey(tenant)), (DeadKey(tenant), DeadPayloadKey(tenant)) })
        {
            if (await db.SortedSetLengthAsync(queue).WaitAsync(token).ConfigureAwait(false) > 100_000)
            {
                throw new InvalidOperationException("nl-retry-legacy-owner-reconciliation-required");
            }
            RedisValue modern = await db.HashGetAsync(payloads, unit).WaitAsync(token).ConfigureAwait(false);
            if (modern.HasValue)
            {
                var scoped = TryDeserialize(modern.ToString());
                if (!HasQualifiedShape(modern.ToString())) throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
                if (scoped is null || scoped.TenantId != tenant || scoped.MemoryUnitId != unit)
                    throw new InvalidOperationException("nl-retry-payload-scope");
            }
            double? modernScore = await db.SortedSetScoreAsync(queue, unit).WaitAsync(token).ConfigureAwait(false);
            if ((modern.HasValue || modernScore.HasValue) && !await RemoveObservedAsync(db, tenant, queue, payloads, unit, modern, modernScore, token).ConfigureAwait(false))
                throw new InvalidOperationException("nl-retry-payload-changed-during-erasure");
            await foreach (SortedSetEntry entry in db.SortedSetScanAsync(queue, pageSize: 100).WithCancellation(token))
            {
                FailedNaturalLanguageEmbeddingRecord? legacy = TryDeserialize(entry.Element.ToString());
                if (legacy is not null && legacy.TenantId == tenant && legacy.MemoryUnitId == unit)
                {
                    if (!HasQualifiedShape(entry.Element.ToString())) throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
                    RedisValue payload = await db.HashGetAsync(payloads, entry.Element).WaitAsync(token).ConfigureAwait(false);
                    if (payload.HasValue) throw new InvalidOperationException("nl-retry-payload-scope");
                    if (!await RemoveObservedAsync(db, tenant, queue, payloads, entry.Element, payload, entry.Score, token).ConfigureAwait(false))
                        throw new InvalidOperationException("nl-retry-payload-changed-during-erasure");
                }
            }
        }
        await RemoveTenantBacklogIfEmptyAsync(db, tenant).ConfigureAwait(false);
    }

    /// <summary>Installs permanent deletion evidence and retains bounded history authority without queue rediscovery.</summary>
    internal static async Task FenceUnitAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        await db.StringSetAsync(SourcePublicationVisibility.Tombstone(tenant, unit), "1").WaitAsync(token).ConfigureAwait(false);
        if (await db.KeyExistsAsync(RetryWorkflowHistory.Lease(tenant, unit)).WaitAsync(token).ConfigureAwait(false))
            throw new InvalidOperationException("nl-retry-scheduling-unresolved");
        await RetryWorkflowHistory.RetainUnitAsync(db, tenant, unit, token).ConfigureAwait(false);
    }

    /// <summary>Reads current retry payload only while the exact unit remains admitted.</summary>
    internal static async Task<FailedNaturalLanguageEmbeddingRecord?> ReadCurrentAsync(IDatabase db, string tenant, string unit)
        => (await ReadCurrentPayloadAsync(db, tenant, unit).ConfigureAwait(false)).Record;

    /// <summary>Reads the admitted record together with its exact persisted bytes.</summary>
    internal static async Task<(FailedNaturalLanguageEmbeddingRecord? Record, string? Payload)> ReadCurrentPayloadAsync(IDatabase db, string tenant, string unit)
    {
        const string read = "if redis.call('EXISTS',KEYS[1],KEYS[2])~=0 or redis.call('EXISTS',KEYS[4])~=1 then return false end return redis.call('HGET',KEYS[3],ARGV[1])";
        RedisResult raw = await db.ScriptEvaluateAsync(read,
            [(RedisKey)SourcePublicationVisibility.Tombstone(tenant, unit), (RedisKey)TenantIngestionOwnership.Fence(tenant), (RedisKey)LivePayloadKey(tenant), (RedisKey)Hexalith.Memories.Server.Infrastructure.IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit)], [(RedisValue)unit]).ConfigureAwait(false);
        string? payload = raw.IsNull ? null : raw.ToString();
        FailedNaturalLanguageEmbeddingRecord? record = payload is null ? null : TryDeserialize(payload);
        if (record is not null && (record.TenantId != tenant || record.MemoryUnitId != unit))
            throw new InvalidOperationException("nl-retry-payload-scope");
        if (record is not null && !HasQualifiedShape(payload!))
            throw new InvalidOperationException("nl-retry-legacy-lineage-reconciliation-required");
        return (record, payload);
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
