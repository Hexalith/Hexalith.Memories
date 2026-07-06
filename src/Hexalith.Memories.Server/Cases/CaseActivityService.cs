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

            List<NameValueEntry> fields =
            [
                new NameValueEntry("type", JsonNamingPolicy.CamelCase.ConvertName(eventType.ToString())),
                new NameValueEntry("actor", actor),
                new NameValueEntry("description", description),
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
                    SummaryKey(tenantId, caseId),
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
            RedisValue value = await db
                .HashGetAsync(SummaryKey(tenantId, caseId), FailedCountField)
                .ConfigureAwait(false);

            if (value.HasValue && int.TryParse(value.ToString(), out int count))
            {
                return Math.Max(0, count);
            }

            (int failedCount, _) = await BackfillSummaryFromStreamAsync(db, tenantId, caseId).ConfigureAwait(false);
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

            (_, DateTimeOffset? lastActivity) = await BackfillSummaryFromStreamAsync(db, tenantId, caseId).ConfigureAwait(false);
            return lastActivity;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get last activity timestamp for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return null;
        }
    }

    private static async Task<(int FailedCount, DateTimeOffset? LastActivity)> BackfillSummaryFromStreamAsync(
        IDatabase db,
        string tenantId,
        string caseId)
    {
        StreamEntry[] entries = await db
            .StreamRangeAsync(
                ActivityKey(tenantId, caseId),
                minId: null,
                maxId: null,
                count: null,
                messageOrder: Order.Ascending)
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

        RedisKey summaryKey = SummaryKey(tenantId, caseId);
        _ = await db
            .HashSetAsync(summaryKey, FailedCountField, failedCount.ToString(CultureInfo.InvariantCulture))
            .ConfigureAwait(false);
        _ = await db
            .HashSetAsync(
                summaryKey,
                LastActivityUnixMillisecondsField,
                (lastActivity?.ToUnixTimeMilliseconds() ?? -1).ToString(CultureInfo.InvariantCulture))
            .ConfigureAwait(false);

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
            description,
            string.IsNullOrEmpty(memoryUnitId) ? null : memoryUnitId);
    }
}
