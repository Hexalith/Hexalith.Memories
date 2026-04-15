// <copyright file="FailedUnitsRegistry.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Stateless service over the Redis-backed failed-units registry (Story 6.3 FR11/FR12).
/// State lives in the hash <c>{tenantId}:failed-unit:{memoryUnitId}</c> + sorted-set
/// <c>{tenantId}:case:{caseId}:failed-units</c>; this class only orchestrates Redis I/O.</summary>
public sealed class FailedUnitsRegistry : IFailedUnitsRegistry
{
    /// <summary>Atomic claim-and-cleanup Lua. KEYS[1]=hash, KEYS[2]=sorted-set, KEYS[3]=dedup key,
    /// ARGV[1]=memoryUnitId. Returns 1 when the hash existed (and was deleted), 0 otherwise.</summary>
    internal const string RemoveScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return 0
        end
        redis.call('DEL', KEYS[1])
        redis.call('ZREM', KEYS[2], ARGV[1])
        redis.call('DEL', KEYS[3])
        return 1
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<FailedUnitsRegistry> _logger;

    public FailedUnitsRegistry(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<FailedUnitsRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <summary>Lists failed units for a case (most-recent first), paged.</summary>
    public async Task<FailedUnitsPage> ListAsync(
        string tenantId, string caseId, int limit, int offset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int boundedLimit = Math.Clamp(limit, 1, 500);
        int boundedOffset = Math.Clamp(offset, 0, 100_000);
        IDatabase db = _redis.GetDatabase();
        string zKey = PersistFailedUnitActivity.BuildSortedSetKey(tenantId, caseId);

        long total = await db.SortedSetLengthAsync(zKey).ConfigureAwait(false);
        RedisValue[] ids = await db.SortedSetRangeByRankAsync(
            zKey, boundedOffset, boundedOffset + boundedLimit - 1, Order.Descending).ConfigureAwait(false);

        List<FailedUnitSummary> units = new(ids.Length);
        foreach (RedisValue id in ids)
        {
            FailedUnitSummary? summary = await ReadSummaryAsync(db, tenantId, id.ToString()).ConfigureAwait(false);
            if (summary is not null)
            {
                units.Add(summary);
            }
        }

        RetryFailureLog.LogFailedUnitsListQueried(
            _logger, tenantId, caseId, boundedLimit, boundedOffset, units.Count, (int)total);
        return new FailedUnitsPage(units, (int)total, boundedLimit, boundedOffset);
    }

    /// <summary>Reads the full failed-unit record (internal — used to rebuild <see cref="IngestionInput"/>).</summary>
    internal async Task<FailedUnitRecord?> GetAsync(string tenantId, string memoryUnitId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IDatabase db = _redis.GetDatabase();
        string hashKey = PersistFailedUnitActivity.BuildHashKey(tenantId, memoryUnitId);
        HashEntry[] entries = await db.HashGetAllAsync(hashKey).ConfigureAwait(false);
        return entries.Length == 0 ? null : ParseRecord(entries, tenantId, memoryUnitId);
    }

    /// <summary>Public projection for the GET /memory-units/{id} fallback path.</summary>
    public async Task<FailedUnitSummary?> GetSummaryAsync(string tenantId, string memoryUnitId, CancellationToken cancellationToken)
    {
        FailedUnitRecord? record = await GetAsync(tenantId, memoryUnitId, cancellationToken).ConfigureAwait(false);
        return record is null
            ? null
            : new FailedUnitSummary(
                record.MemoryUnitId,
                record.CaseId,
                record.SourceUri,
                record.SourceType,
                record.Stage,
                record.ErrorCode,
                record.ErrorMessage,
                record.RetryCount,
                record.LastRetryAt,
                record.FailedAt);
    }

    /// <summary>Atomically deletes the failed-unit hash, the sorted-set entry, and the dedup key.
    /// Returns true when the hash existed (caller claimed exclusive ownership), false otherwise.</summary>
    internal async Task<bool> RemoveAsync(
        string tenantId, string caseId, string memoryUnitId, string sourceUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IDatabase db = _redis.GetDatabase();
        string hashKey = PersistFailedUnitActivity.BuildHashKey(tenantId, memoryUnitId);
        string zKey = PersistFailedUnitActivity.BuildSortedSetKey(tenantId, caseId);
        string dedupKey = DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri);

        RedisResult result = await db.ScriptEvaluateAsync(
            RemoveScript,
            [hashKey, zKey, dedupKey],
            [memoryUnitId]).ConfigureAwait(false);

        return (long)result == 1;
    }

    internal async Task RestoreAsync(FailedUnitRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string hashKey = PersistFailedUnitActivity.BuildHashKey(record.TenantId, record.MemoryUnitId);
        string zKey = PersistFailedUnitActivity.BuildSortedSetKey(record.TenantId, record.CaseId);
        long failedAtMs = record.FailedAt.ToUnixTimeMilliseconds();

        FailureDetails details = new(
            record.Stage,
            record.ErrorCode,
            record.RetryCount,
            record.ErrorMessage,
            record.LastRetryAt);
        string detailsJson = JsonSerializer.Serialize(details, MemoriesJsonContext.Options);

        RedisValue[] argv =
        [
            PersistFailedUnitActivity.FieldTenantId, record.TenantId,
            PersistFailedUnitActivity.FieldCaseId, record.CaseId,
            PersistFailedUnitActivity.FieldSourceUri, record.SourceUri,
            PersistFailedUnitActivity.FieldSourceType, record.SourceType.ToString(),
            PersistFailedUnitActivity.FieldIngestedBy, record.IngestedBy,
            PersistFailedUnitActivity.FieldContentType, record.ContentType ?? string.Empty,
            PersistFailedUnitActivity.FieldStage, record.Stage,
            PersistFailedUnitActivity.FieldErrorCode, record.ErrorCode,
            PersistFailedUnitActivity.FieldErrorMessage, record.ErrorMessage ?? string.Empty,
            PersistFailedUnitActivity.FieldRetryCount, record.RetryCount.ToString(CultureInfo.InvariantCulture),
            PersistFailedUnitActivity.FieldLastRetryAt, record.LastRetryAt?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
            PersistFailedUnitActivity.FieldFailedAt, record.FailedAt.ToString("O", CultureInfo.InvariantCulture),
            PersistFailedUnitActivity.FieldFailureDetailsJson, detailsJson,
            failedAtMs.ToString(CultureInfo.InvariantCulture),
            record.MemoryUnitId,
        ];

        await db.ScriptEvaluateAsync(
            PersistFailedUnitActivity.PersistScript,
            [hashKey, zKey],
            argv).ConfigureAwait(false);
    }

    Task<FailedUnitRecord?> IFailedUnitsRegistry.GetAsync(string tenantId, string memoryUnitId, CancellationToken cancellationToken)
        => GetAsync(tenantId, memoryUnitId, cancellationToken);

    Task<bool> IFailedUnitsRegistry.RemoveAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string sourceUri,
        CancellationToken cancellationToken)
        => RemoveAsync(tenantId, caseId, memoryUnitId, sourceUri, cancellationToken);

    Task IFailedUnitsRegistry.RestoreAsync(FailedUnitRecord record, CancellationToken cancellationToken)
        => RestoreAsync(record, cancellationToken);

    private static async Task<FailedUnitSummary?> ReadSummaryAsync(IDatabase db, string tenantId, string memoryUnitId)
    {
        string hashKey = PersistFailedUnitActivity.BuildHashKey(tenantId, memoryUnitId);
        HashEntry[] entries = await db.HashGetAllAsync(hashKey).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            return null;
        }

        FailedUnitRecord r = ParseRecord(entries, tenantId, memoryUnitId);
        return new FailedUnitSummary(
            r.MemoryUnitId, r.CaseId, r.SourceUri, r.SourceType, r.Stage, r.ErrorCode,
            r.ErrorMessage, r.RetryCount, r.LastRetryAt, r.FailedAt);
    }

    private static FailedUnitRecord ParseRecord(HashEntry[] entries, string tenantId, string memoryUnitId)
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        foreach (HashEntry e in entries)
        {
            fields[e.Name.ToString()] = e.Value.ToString();
        }

        string sourceTypeStr = fields.GetValueOrDefault(PersistFailedUnitActivity.FieldSourceType, nameof(SourceType.File));
        SourceType sourceType = Enum.TryParse(sourceTypeStr, out SourceType st) ? st : SourceType.File;
        string contentType = fields.GetValueOrDefault(PersistFailedUnitActivity.FieldContentType, string.Empty);
        string errorMessage = fields.GetValueOrDefault(PersistFailedUnitActivity.FieldErrorMessage, string.Empty);
        string lastRetryAt = fields.GetValueOrDefault(PersistFailedUnitActivity.FieldLastRetryAt, string.Empty);
        int retryCount = int.TryParse(
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldRetryCount, "0"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int rc) ? rc : 0;
        DateTimeOffset failedAt = DateTimeOffset.TryParse(
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldFailedAt, string.Empty),
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset fa)
            ? fa : DateTimeOffset.MinValue;
        DateTimeOffset? lastRetryAtParsed = string.IsNullOrEmpty(lastRetryAt)
            ? null
            : DateTimeOffset.TryParse(lastRetryAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset lr)
                ? lr : null;

        return new FailedUnitRecord(
            tenantId,
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldCaseId, string.Empty),
            memoryUnitId,
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldSourceUri, string.Empty),
            sourceType,
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldIngestedBy, string.Empty),
            string.IsNullOrEmpty(contentType) ? null : contentType,
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldStage, string.Empty),
            fields.GetValueOrDefault(PersistFailedUnitActivity.FieldErrorCode, string.Empty),
            string.IsNullOrEmpty(errorMessage) ? null : errorMessage,
            retryCount,
            lastRetryAtParsed,
            failedAt);
    }
}
