// <copyright file="CaseActivityService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Records and retrieves case activity events using Redis Streams.</summary>
internal sealed class CaseActivityService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CaseActivityService> _logger;

    public CaseActivityService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<CaseActivityService> logger)
    {
        _redis = redis;
        _logger = logger;
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
            string key = $"{tenantId}:case:{caseId}:activity";

            List<NameValueEntry> fields =
            [
                new NameValueEntry("type", eventType.ToString()),
                new NameValueEntry("actor", actor),
                new NameValueEntry("description", description),
            ];

            if (memoryUnitId is not null)
            {
                fields.Add(new NameValueEntry("memoryUnitId", memoryUnitId));
            }

            await db.StreamAddAsync(key, [.. fields]).ConfigureAwait(false);
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
        maxEvents = Math.Clamp(maxEvents, 1, 500);

        IDatabase db = _redis.GetDatabase();
        string key = $"{tenantId}:case:{caseId}:activity";

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

    public async Task<int> GetFailedCountAsync(
        string tenantId,
        string caseId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            string key = $"{tenantId}:case:{caseId}:activity";

            StreamEntry[] entries = await db.StreamRangeAsync(key, null, null).ConfigureAwait(false) ?? [];

            int count = 0;
            foreach (StreamEntry entry in entries)
            {
                string? type = entry.Values.FirstOrDefault(v => v.Name == "type").Value;
                if (string.Equals(type, "IngestionFailed", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
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
            string key = $"{tenantId}:case:{caseId}:activity";

            StreamEntry[] entries = await db.StreamRangeAsync(
                key,
                minId: null,
                maxId: null,
                count: 1,
                messageOrder: Order.Descending).ConfigureAwait(false) ?? [];

            if (entries.Length == 0)
            {
                return null;
            }

            return ParseTimestampFromStreamId(entries[0].Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get last activity timestamp for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return null;
        }
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
