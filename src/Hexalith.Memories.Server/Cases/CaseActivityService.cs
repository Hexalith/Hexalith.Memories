// <copyright file="CaseActivityService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using System.Globalization;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

/// <summary>Records and retrieves case activity events using Redis Streams.</summary>
internal sealed class CaseActivityService
{
    private const string FailedCountField = "failedCount";
    private const string FailedCountReconciledField = "failedCountReconciled";
    private const string LastActivityUnixMillisecondsField = "lastActivityUnixMilliseconds";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CaseActivityService> _logger;
    private readonly int _streamMaxLength;

    public CaseActivityService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CaseActivityService> logger,
        IOptions<CaseActivityOptions>? options = null)
    {
        _redis = redis;
        _logger = logger;
        _streamMaxLength = CaseActivityOptions.ClampStreamMaxLength(
            (options ?? Options.Create(new CaseActivityOptions())).Value.StreamMaxLength);
    }

    public async Task<bool> RecordEventAsync(
        string tenantId,
        string caseId,
        CaseActivityEventType eventType,
        string actor,
        string description,
        string? memoryUnitId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            string key = ActivityKey(tenantId, caseId);
            RedisKey summaryKey = SummaryKey(tenantId, caseId);
            if (eventType == CaseActivityEventType.IngestionFailed)
            {
                // Establish the legacy baseline before appending the failure we will increment below.
                _ = await GetSummaryAsync(db, key, summaryKey, backfillTimestamp: false, cancellationToken)
                    .ConfigureAwait(false);
            }

            List<NameValueEntry> fields =
            [
                new NameValueEntry("type", JsonNamingPolicy.CamelCase.ConvertName(eventType.ToString())),
                new NameValueEntry("actor", actor),
                new NameValueEntry("description", ProtectIngestionDescription(eventType, memoryUnitId, description)),
            ];

            if (memoryUnitId is not null)
            {
                fields.Add(new NameValueEntry("memoryUnitId", memoryUnitId));
            }

            RedisValue streamId = await db
                .StreamAddAsync(
                    key,
                    [.. fields],
                    messageId: null,
                    maxLength: _streamMaxLength,
                    useApproximateMaxLength: true)
                .ConfigureAwait(false);

            await UpdateSummaryAsync(
                    db,
                    summaryKey,
                    eventType,
                    ParseTimestampFromStreamId(streamId.ToString()))
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record activity event for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return false;
        }
    }

    public async Task<List<CaseActivityEvent>> GetRecentActivityAsync(
        string tenantId,
        string caseId,
        int maxEvents = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            maxEvents = Math.Clamp(maxEvents, 1, 500);

            IDatabase db = _redis.GetDatabase();
            string key = ActivityKey(tenantId, caseId);

            StreamEntry[] entries = await db.StreamRangeAsync(
                key,
                minId: null,
                maxId: null,
                count: maxEvents,
                messageOrder: Order.Descending).ConfigureAwait(false) ?? [];

            List<CaseActivityEvent> events = new(entries.Length);
            foreach (StreamEntry entry in entries)
            {
                CaseActivityEvent? parsed = ParseStreamEntry(entry);
                if (parsed is not null)
                {
                    events.Add(parsed);
                }
            }

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get recent activity for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return [];
        }
    }

    public async Task<int> GetFailedCountAsync(
        string tenantId,
        string caseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            (int failedCount, _) = await GetSummaryAsync(
                db, ActivityKey(tenantId, caseId), SummaryKey(tenantId, caseId), backfillTimestamp: false, cancellationToken)
                .ConfigureAwait(false);
            return failedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get failed count for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return 0;
        }
    }

    public async Task<DateTimeOffset?> GetLastActivityTimestampAsync(
        string tenantId,
        string caseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisValue value = await db
                .HashGetAsync(SummaryKey(tenantId, caseId), LastActivityUnixMillisecondsField)
                .ConfigureAwait(false);

            if (value.HasValue)
            {
                return long.TryParse(value.ToString(), out long unixMilliseconds) && unixMilliseconds >= 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds)
                    : null;
            }

            (_, DateTimeOffset? lastActivity) = await GetSummaryAsync(
                db, ActivityKey(tenantId, caseId), SummaryKey(tenantId, caseId), backfillTimestamp: true, cancellationToken)
                .ConfigureAwait(false);
            return lastActivity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get last activity timestamp for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return null;
        }
    }

    private static async Task<(int FailedCount, DateTimeOffset? LastActivity)> GetSummaryAsync(
        IDatabase db,
        RedisKey activityKey,
        RedisKey summaryKey,
        bool backfillTimestamp,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue[] observed = await db.HashGetAsync(summaryKey, [FailedCountField, FailedCountReconciledField])
                .ConfigureAwait(false);
            string storedCount = observed[0].ToString();
            _ = int.TryParse(storedCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int existingCount);
            bool reconciled = observed[1] == "1" && existingCount >= 0
                && storedCount == existingCount.ToString(CultureInfo.InvariantCulture);
            if (reconciled && !backfillTimestamp)
            {
                return (existingCount, null);
            }

            (int retainedFailures, DateTimeOffset? lastActivity) = await ReadRetainedSummaryAsync(db, activityKey)
                .ConfigureAwait(false);
            int failedCount = existingCount;
            if (!reconciled)
            {
                // An older writer may have undercounted retained failures. Never discard an existing
                // lifetime count that also includes failures trimmed from the stream.
                failedCount = Math.Max(existingCount, retainedFailures);
                ITransaction transaction = db.CreateTransaction();
                _ = transaction.AddCondition(ObservedField(summaryKey, FailedCountField, observed[0]));
                _ = transaction.AddCondition(ObservedField(summaryKey, FailedCountReconciledField, observed[1]));
                _ = transaction.HashSetAsync(summaryKey,
                    [new(FailedCountField, failedCount), new(FailedCountReconciledField, "1")]);
                if (!await transaction.ExecuteAsync().ConfigureAwait(false))
                {
                    continue;
                }
            }

            bool timestampStored = await db.HashSetAsync(
                summaryKey, LastActivityUnixMillisecondsField, lastActivity?.ToUnixTimeMilliseconds() ?? -1, When.NotExists)
                .ConfigureAwait(false);
            if (!timestampStored && backfillTimestamp)
            {
                RedisValue current = await db.HashGetAsync(summaryKey, LastActivityUnixMillisecondsField).ConfigureAwait(false);
                if (long.TryParse(current.ToString(), out long milliseconds))
                {
                    lastActivity = milliseconds >= 0 ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds) : null;
                }
            }

            return (failedCount, lastActivity);
        }
    }

    private static Condition ObservedField(RedisKey key, RedisValue field, RedisValue observed) => observed.IsNull
        ? Condition.HashNotExists(key, field)
        : Condition.HashEqual(key, field, observed);

    private static async Task<(int FailedCount, DateTimeOffset? LastActivity)> ReadRetainedSummaryAsync(
        IDatabase db,
        RedisKey activityKey)
    {
        StreamEntry[] entries = await db
            .StreamRangeAsync(activityKey, minId: null, maxId: null, count: null, messageOrder: Order.Ascending)
            .ConfigureAwait(false) ?? [];

        int failedCount = 0;
        DateTimeOffset? lastActivity = null;
        foreach (StreamEntry entry in entries)
        {
            CaseActivityEvent? parsed = ParseStreamEntry(entry);
            if (parsed is null)
            {
                continue;
            }

            if (parsed.EventType == CaseActivityEventType.IngestionFailed)
            {
                failedCount++;
            }

            if (lastActivity is null || parsed.Timestamp > lastActivity.Value)
            {
                lastActivity = parsed.Timestamp;
            }
        }

        return (failedCount, lastActivity);
    }

    private static DateTimeOffset? ParseTimestampFromStreamId(string streamId)
    {
        int dashIndex = streamId.IndexOf('-');
        if (dashIndex <= 0)
        {
            return null;
        }

        if (long.TryParse(streamId.AsSpan(0, dashIndex), out long millis))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(millis);
        }

        return null;
    }

    private static string ActivityKey(string tenantId, string caseId) => $"{tenantId}:case:{caseId}:activity";

    private static string SummaryKey(string tenantId, string caseId) => $"{ActivityKey(tenantId, caseId)}:summary";

    private static async Task UpdateSummaryAsync(
        IDatabase db,
        RedisKey summaryKey,
        CaseActivityEventType eventType,
        DateTimeOffset? timestamp)
    {
        if (eventType == CaseActivityEventType.IngestionFailed)
        {
            _ = await db
                .HashIncrementAsync(summaryKey, FailedCountField)
                .ConfigureAwait(false);
        }

        if (timestamp is not null)
        {
            _ = await db
                .HashSetAsync(
                    summaryKey,
                    LastActivityUnixMillisecondsField,
                    timestamp.Value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
        }
    }

    // Earlier workflow histories and retained stream entries can contain the original source.
    private static string ProtectIngestionDescription(CaseActivityEventType type, string? unitId, string description)
    {
        if (type == CaseActivityEventType.MemoryUnitIngested
            && description.StartsWith($"Memory unit {unitId} indexed from ", StringComparison.Ordinal))
        {
            return $"Memory unit {unitId} indexed";
        }

        if (type == CaseActivityEventType.IngestionFailed
            && description.StartsWith("Ingestion failed for ", StringComparison.Ordinal))
        {
            return $"Memory unit {unitId} failed";
        }

        return description;
    }

    private static CaseActivityEvent? ParseStreamEntry(StreamEntry entry)
    {
        string? type = entry.Values.FirstOrDefault(v => v.Name == "type").Value;
        string? actor = entry.Values.FirstOrDefault(v => v.Name == "actor").Value;
        string? description = entry.Values.FirstOrDefault(v => v.Name == "description").Value;
        string? memoryUnitId = entry.Values.FirstOrDefault(v => v.Name == "memoryUnitId").Value;

        if (type is null || actor is null || description is null)
        {
            return null;
        }

        if (!Enum.TryParse(type, ignoreCase: true, out CaseActivityEventType eventType))
        {
            return null;
        }

        string streamId = entry.Id.ToString();
        DateTimeOffset timestamp = ParseTimestampFromStreamId(streamId) ?? DateTimeOffset.MinValue;

        return new CaseActivityEvent(
            streamId,
            timestamp,
            eventType,
            actor,
            ProtectIngestionDescription(eventType, memoryUnitId, description),
            string.IsNullOrEmpty(memoryUnitId) ? null : memoryUnitId);
    }
}
