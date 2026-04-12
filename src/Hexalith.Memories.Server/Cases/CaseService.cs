// <copyright file="CaseService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using BaUlid = ByteAether.Ulid.Ulid;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Manages case lifecycle: create, list, and get operations backed by Redis and FalkorDB.</summary>
internal sealed class CaseService
{
    private static readonly BaUlid.GenerationOptions UlidOptions = new()
    {
        Monotonicity = BaUlid.GenerationOptions.MonotonicityOptions.MonotonicIncrement,
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionMultiplexer _falkorDb;
    private readonly IGraphQueryBuilder _graphQueryBuilder;
    private readonly CaseActivityService _activityService;
    private readonly ILogger<CaseService> _logger;

    public CaseService(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb,
        IGraphQueryBuilder graphQueryBuilder,
        CaseActivityService activityService,
        ILogger<CaseService> logger)
    {
        _redis = redis;
        _falkorDb = falkorDb;
        _graphQueryBuilder = graphQueryBuilder;
        _activityService = activityService;
        _logger = logger;
    }

    public async Task<Case> CreateCaseAsync(CreateCaseInput input, CancellationToken cancellationToken)
    {
        string caseId = BaUlid.New(UlidOptions).ToString();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IDatabase db = _redis.GetDatabase();
        string redisKey = $"{input.TenantId}:case:{caseId}";

        await db.HashSetAsync(
            redisKey,
            [
                new HashEntry("id", caseId),
                new HashEntry("tenantId", input.TenantId),
                new HashEntry("name", input.Name),
                new HashEntry("description", input.Description ?? string.Empty),
                new HashEntry("status", "active"),
                new HashEntry("createdAt", now.ToString("o")),
                new HashEntry("lastUpdated", now.ToString("o")),
            ]).ConfigureAwait(false);

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildMergeCaseNode(
            caseId, input.Name, input.TenantId, now);
        await falkor.QueryAsync(input.TenantId, query, parameters).ConfigureAwait(false);

        _ = await _activityService.RecordEventAsync(
            input.TenantId,
            caseId,
            CaseActivityEventType.CaseCreated,
            "system",
            $"Case '{input.Name}' created",
            memoryUnitId: null,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created case {CaseId} in tenant {TenantId}",
            caseId,
            input.TenantId);

        return new Case(
            caseId,
            input.TenantId,
            input.Name,
            input.Description,
            CaseStatus.Active,
            now,
            now,
            MemoryUnitCount: 0);
    }

    public async Task<List<Case>> ListCasesAsync(string tenantId, int maxResults = 100, CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();
        string pattern = $"{tenantId}:case:*";
        List<Case> candidateCases = [];

        IServer server = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints()[0]);
        foreach (RedisKey key in server.Keys(pattern: pattern, pageSize: maxResults))
        {
            if (key.ToString().EndsWith(":activity", StringComparison.Ordinal))
            {
                continue;
            }

            HashEntry[] entries = await db.HashGetAllAsync(key).ConfigureAwait(false);
            if (entries.Length == 0)
            {
                continue;
            }

            Case? parsed = ParseCaseFromHash(entries, tenantId);
            if (parsed is not null)
            {
                candidateCases.Add(parsed);
            }
        }

        List<Case> orderedCases = candidateCases
            .OrderByDescending(item => item.CreatedAt)
            .Take(maxResults)
            .ToList();

        if (orderedCases.Count == 0)
        {
            return orderedCases;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());
        for (int i = 0; i < orderedCases.Count; i++)
        {
            Case parsed = orderedCases[i];
            orderedCases[i] = parsed with
            {
                MemoryUnitCount = await GetMemoryUnitCountSafe(falkor, tenantId, parsed.Id).ConfigureAwait(false),
            };
        }

        return orderedCases;
    }

    public async Task<Case?> GetCaseAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string redisKey = $"{tenantId}:case:{caseId}";
        HashEntry[] entries = await db.HashGetAllAsync(redisKey).ConfigureAwait(false);

        if (entries.Length == 0)
        {
            return null;
        }

        NFalkorDB.FalkorDB falkor = new(_falkorDb.GetDatabase());

        // Graph ID is tenantId, NOT caseId — each tenant has one FalkorDB database
        int memoryUnitCount = await GetMemoryUnitCountSafe(falkor, tenantId, caseId).ConfigureAwait(false);

        Case? parsed = ParseCaseFromHash(entries, tenantId);
        return parsed is null ? null : parsed with { MemoryUnitCount = memoryUnitCount };
    }

    public async Task<CaseStatusDetail?> GetCaseStatusAsync(string tenantId, string caseId, CancellationToken cancellationToken)
    {
        Case? caseResult = await GetCaseAsync(tenantId, caseId, cancellationToken).ConfigureAwait(false);
        if (caseResult is null)
        {
            return null;
        }

        Task<DateTimeOffset?> lastActivityTask = _activityService.GetLastActivityTimestampAsync(tenantId, caseId, cancellationToken);
        Task<int> failedCountTask = _activityService.GetFailedCountAsync(tenantId, caseId, cancellationToken);
        await Task.WhenAll(lastActivityTask, failedCountTask).ConfigureAwait(false);

        return new CaseStatusDetail(
            caseResult.Id,
            caseResult.TenantId,
            caseResult.Name,
            caseResult.Description,
            caseResult.Status,
            caseResult.CreatedAt,
            caseResult.LastUpdated,
            caseResult.MemoryUnitCount,
            lastActivityTask.Result,
            IndexedCount: caseResult.MemoryUnitCount,
            FailedCount: failedCountTask.Result);
    }

    private async Task<int> GetMemoryUnitCountSafe(NFalkorDB.FalkorDB falkor, string tenantId, string caseId)
    {
        try
        {
            (string query, IDictionary<string, object> parameters) = _graphQueryBuilder.BuildCountCaseMemoryUnits(caseId);
            NFalkorDB.ResultSet result = await falkor.QueryAsync(tenantId, query, parameters).ConfigureAwait(false);
            if (result.Count > 0)
            {
                NFalkorDB.Record record = result.First();
                return Convert.ToInt32(record.Values[0]);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory unit count for case {CaseId} in tenant {TenantId}", caseId, tenantId);
            return 0;
        }
    }

    private static Case? ParseCaseFromHash(HashEntry[] entries, string tenantId)
    {
        Dictionary<string, string> fields = [];
        foreach (HashEntry entry in entries)
        {
            fields[entry.Name!] = entry.Value!;
        }

        if (!fields.TryGetValue("id", out string? id) || string.IsNullOrEmpty(id))
        {
            return null;
        }

        _ = fields.TryGetValue("tenantId", out string? storedTenantId);
        _ = fields.TryGetValue("name", out string? name);
        _ = fields.TryGetValue("description", out string? description);
        _ = fields.TryGetValue("status", out string? statusStr);
        _ = fields.TryGetValue("createdAt", out string? createdAtStr);
        _ = fields.TryGetValue("lastUpdated", out string? lastUpdatedStr);

        CaseStatus status = string.Equals(statusStr, "closed", StringComparison.OrdinalIgnoreCase)
            ? CaseStatus.Closed
            : CaseStatus.Active;

        _ = DateTimeOffset.TryParse(createdAtStr, out DateTimeOffset createdAt);
        _ = DateTimeOffset.TryParse(lastUpdatedStr, out DateTimeOffset lastUpdated);

        return new Case(
            id,
            storedTenantId ?? tenantId,
            name ?? string.Empty,
            string.IsNullOrEmpty(description) ? null : description,
            status,
            createdAt,
            lastUpdated,
            MemoryUnitCount: 0);
    }
}
