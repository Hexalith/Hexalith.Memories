// <copyright file="FailedNaturalLanguageEmbeddingRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.NaturalLanguage;

using System.Runtime.CompilerServices;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Story 9.2 Task 8.2 — Redis-backed implementation over the sorted-set
/// <c>nl-embedding-retry:{tenantId}</c> (live queue) and <c>nl-embedding-retry-dead:{tenantId}</c>
/// (dead-letter). Serialized payload is the JSON of <see cref="FailedNaturalLanguageEmbeddingRecord"/>
/// — score is <see cref="FailedNaturalLanguageEmbeddingRecord.QueuedAtTicks"/> so dequeuing by rank
/// (ascending) is natural FIFO.</summary>
public sealed partial class FailedNaturalLanguageEmbeddingRegistry : IFailedNaturalLanguageEmbeddingRegistry
{
    internal const string LiveKeyPrefix = "nl-embedding-retry:";
    internal const string DeadKeyPrefix = "nl-embedding-retry-dead:";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FailedNaturalLanguageEmbeddingRegistry> _logger;

    public FailedNaturalLanguageEmbeddingRegistry(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<FailedNaturalLanguageEmbeddingRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task EnqueueAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        string json = SerializeRecord(record);
        IDatabase db = _redis.GetDatabase();
        await db.SortedSetAddAsync(LiveKey(record.TenantId), json, record.QueuedAtTicks).ConfigureAwait(false);
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
        SortedSetEntry[] entries = await db
            .SortedSetRangeByRankWithScoresAsync(LiveKey(tenantId), 0, clamped - 1, Order.Ascending)
            .ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return [];
        }

        List<FailedNaturalLanguageEmbeddingRecord> records = new(entries.Length);
        foreach (SortedSetEntry entry in entries)
        {
            FailedNaturalLanguageEmbeddingRecord? parsed = TryDeserialize(entry.Element.ToString());
            if (parsed is not null)
            {
                records.Add(parsed);
            }
        }

        return records;
    }

    /// <inheritdoc/>
    public async Task CompleteAsync(FailedNaturalLanguageEmbeddingRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        string json = SerializeRecord(record);
        IDatabase db = _redis.GetDatabase();
        bool removed = await db.SortedSetRemoveAsync(LiveKey(record.TenantId), json).ConfigureAwait(false);
        if (removed)
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

        string existingJson = SerializeRecord(record);
        FailedNaturalLanguageEmbeddingRecord next = record with { Attempts = record.Attempts + 1 };
        string nextJson = SerializeRecord(next);

        IDatabase db = _redis.GetDatabase();
        ITransaction tx = db.CreateTransaction();
        _ = tx.SortedSetRemoveAsync(LiveKey(record.TenantId), existingJson);

        if (next.Attempts >= maxAttempts)
        {
            _ = tx.SortedSetAddAsync(DeadKey(record.TenantId), nextJson, next.QueuedAtTicks);
            await tx.ExecuteAsync().ConfigureAwait(false);
            LogDeadLettered(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
            return true;
        }

        _ = tx.SortedSetAddAsync(LiveKey(record.TenantId), nextJson, next.QueuedAtTicks);
        await tx.ExecuteAsync().ConfigureAwait(false);
        LogAttemptIncremented(_logger, record.TenantId, record.MemoryUnitId, next.Attempts);
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
            if (result.IsNull)
            {
                return 0;
            }

            return (long)result;
        }
        catch (RedisServerException ex)
        {
            LogMemoryUsageUnavailable(_logger, ex, tenantId);
            return 0;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ListTenantsWithBacklogAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IServer? server = GetFirstConnectedServer();
        if (server is null)
        {
            yield break;
        }

        await foreach (RedisKey key in server.KeysAsync(pattern: LiveKeyPrefix + "*").WithCancellation(cancellationToken))
        {
            string keyStr = key.ToString();
            if (keyStr.StartsWith(LiveKeyPrefix, StringComparison.Ordinal))
            {
                yield return keyStr[LiveKeyPrefix.Length..];
            }
        }
    }

    internal static string LiveKey(string tenantId) => LiveKeyPrefix + tenantId;

    internal static string DeadKey(string tenantId) => DeadKeyPrefix + tenantId;

    internal static string SerializeRecord(FailedNaturalLanguageEmbeddingRecord record)
        => JsonSerializer.Serialize(record, MemoriesJsonContext.Options);

    internal static FailedNaturalLanguageEmbeddingRecord? TryDeserialize(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<FailedNaturalLanguageEmbeddingRecord>(payload, MemoriesJsonContext.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private IServer? GetFirstConnectedServer()
    {
        foreach (System.Net.EndPoint endpoint in _redis.GetEndPoints())
        {
            IServer server = _redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        return null;
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
}
