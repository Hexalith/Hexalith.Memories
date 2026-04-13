// <copyright file="CaseService.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Cases;

using System.IO;
using System.Text.Json;

using BaUlid = ByteAether.Ulid.Ulid;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

/// <summary>Manages case lifecycle: create, list, and get operations backed by Redis and FalkorDB.</summary>
internal sealed class CaseService
{
    private const int MaxMembersPerCase = 1000;

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
            string keyStr = key.ToString();
            if (keyStr.EndsWith(":activity", StringComparison.Ordinal) ||
                keyStr.EndsWith(":members", StringComparison.Ordinal))
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
        Task<int> memberCountTask = GetMemberCountAsync(tenantId, caseId, cancellationToken);
        await Task.WhenAll(lastActivityTask, failedCountTask, memberCountTask).ConfigureAwait(false);

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
            FailedCount: failedCountTask.Result,
            MemberCount: memberCountTask.Result);
    }

    /// <summary>Adds a member to a case using atomic HSETNX for idempotency.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="input">The member details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tuple of the member and whether it was newly created.</returns>
    public async Task<(CaseMember Member, bool Created)> AddMemberAsync(
        string tenantId, string caseId, AddCaseMemberInput input, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        // Enforce member count limit before attempting add
        long currentCount = await db.HashLengthAsync(membersKey).ConfigureAwait(false);
        if (currentCount >= MaxMembersPerCase)
        {
            RedisValue existingAtLimit = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
            if (existingAtLimit.HasValue)
            {
                return (DeserializeStoredMemberOrThrow(existingAtLimit, tenantId, caseId, input.MemberId), false);
            }

            throw new InvalidOperationException($"Case '{caseId}' has reached the maximum of {MaxMembersPerCase} members.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var member = new CaseMember(input.MemberId, input.MemberType, now);
        string json = JsonSerializer.Serialize(member, MemoriesJsonContext.Options);

        // Atomic idempotent add via HSETNX -- no TOCTOU race
        bool created = await db.HashSetAsync(membersKey, input.MemberId, json, When.NotExists).ConfigureAwait(false);

        if (created)
        {
            // Activity event ONLY for new members -- await to match CreateCaseAsync pattern
            _ = await _activityService.RecordEventAsync(
                tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
                $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);

            return (member, true);
        }

        // HSETNX returned false -- member already existed. Read the stored version.
        // Edge case: member could have been deleted between HSETNX and HashGet (rare race).
        RedisValue existing = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
        if (!existing.HasValue)
        {
            // Member was deleted between HSETNX check and read. Retry the add.
            bool retriedCreated = await db.HashSetAsync(membersKey, input.MemberId, json, When.NotExists).ConfigureAwait(false);
            if (retriedCreated)
            {
                _ = await _activityService.RecordEventAsync(
                    tenantId, caseId, CaseActivityEventType.MemberAdded, "system",
                    $"Member '{input.MemberId}' ({input.MemberType}) added", null, cancellationToken).ConfigureAwait(false);
                return (member, true);
            }

            existing = await db.HashGetAsync(membersKey, input.MemberId).ConfigureAwait(false);
            if (!existing.HasValue)
            {
                throw new InvalidDataException(
                    $"Stored member '{input.MemberId}' for case '{caseId}' in tenant '{tenantId}' was unavailable during idempotency recovery.");
            }
        }

        CaseMember existingMember = DeserializeStoredMemberOrThrow(existing, tenantId, caseId, input.MemberId);
        return (existingMember, false);
    }

    /// <summary>Removes a member from a case.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="memberId">The member identifier to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the member was removed; <see langword="false"/> if not found.</returns>
    public async Task<bool> RemoveMemberAsync(
        string tenantId, string caseId, string memberId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        bool removed = await db.HashDeleteAsync(membersKey, memberId).ConfigureAwait(false);
        if (removed)
        {
            _ = await _activityService.RecordEventAsync(
                tenantId, caseId, CaseActivityEventType.MemberRemoved, "system",
                $"Member '{memberId}' removed", null, cancellationToken).ConfigureAwait(false);
        }

        return removed;
    }

    /// <summary>Lists all members of a case ordered by when they were added.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The list of case members ordered by <see cref="CaseMember.AddedAt"/>.</returns>
    public async Task<List<CaseMember>> ListMembersAsync(
        string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";

        HashEntry[] entries = await db.HashGetAllAsync(membersKey).ConfigureAwait(false);
        List<CaseMember> members = new(entries.Length);
        foreach (HashEntry entry in entries)
        {
            if (TryDeserializeStoredMember(entry.Value, tenantId, caseId, entry.Name.ToString(), out CaseMember? parsed) && parsed is not null)
            {
                members.Add(parsed);
            }
        }

        return members.OrderBy(m => m.AddedAt).ToList();
    }

    /// <summary>Batch-resolves case names from Redis hashes for a set of case IDs.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseIds">The case IDs to resolve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping each case ID to its name (falls back to case ID if name is missing).</returns>
    public async Task<Dictionary<string, string>> ResolveNamesAsync(
        string tenantId, IEnumerable<string> caseIds, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        List<string> uniqueIds = caseIds.Distinct().ToList();
        if (uniqueIds.Count == 0)
        {
            return [];
        }

        IBatch batch = db.CreateBatch();
        Task<RedisValue>[] tasks = uniqueIds.Select(id =>
            batch.HashGetAsync($"{tenantId}:case:{id}", "name")).ToArray();
        batch.Execute();
        RedisValue[] names = await Task.WhenAll(tasks).ConfigureAwait(false);

        Dictionary<string, string> result = new(uniqueIds.Count);
        for (int i = 0; i < uniqueIds.Count; i++)
        {
            result[uniqueIds[i]] = names[i].HasValue ? (string)names[i]! : uniqueIds[i];
        }

        return result;
    }

    /// <summary>Gets the number of members in a case via HashLengthAsync.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="caseId">The case identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The member count.</returns>
    public async Task<int> GetMemberCountAsync(
        string tenantId, string caseId, CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";
        long count = await db.HashLengthAsync(membersKey).ConfigureAwait(false);
        return (int)count;
    }

    private CaseMember DeserializeStoredMemberOrThrow(
        RedisValue value,
        string tenantId,
        string caseId,
        string memberId)
    {
        if (TryDeserializeStoredMember(value, tenantId, caseId, memberId, out CaseMember? member) && member is not null)
        {
            return member;
        }

        throw new InvalidDataException(
            $"Stored member '{memberId}' for case '{caseId}' in tenant '{tenantId}' contains invalid JSON.");
    }

    private bool TryDeserializeStoredMember(
        RedisValue value,
        string tenantId,
        string caseId,
        string memberId,
        out CaseMember? member)
    {
        string payload = value.ToString();

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryGetRequiredJsonString(root, "memberId", out string? storedMemberId) ||
                !TryGetRequiredJsonString(root, "memberType", out _) ||
                !TryGetRequiredJsonString(root, "addedAt", out _))
            {
                LogCorruptMemberRecord(tenantId, caseId, memberId, "Required properties are missing.");
                member = null;
                return false;
            }

            member = JsonSerializer.Deserialize<CaseMember>(payload, MemoriesJsonContext.Options);
            if (member is null ||
                !string.Equals(member.MemberId, storedMemberId, StringComparison.Ordinal) ||
                !string.Equals(member.MemberId, memberId, StringComparison.Ordinal) ||
                member.AddedAt == default)
            {
                LogCorruptMemberRecord(tenantId, caseId, memberId, "Stored JSON does not match the hash entry.");
                member = null;
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            LogCorruptMemberRecord(tenantId, caseId, memberId, "Stored JSON is invalid.", ex);
            member = null;
            return false;
        }
    }

    private void LogCorruptMemberRecord(
        string tenantId,
        string caseId,
        string memberId,
        string reason,
        Exception? exception = null)
    {
        if (exception is null)
        {
            _logger.LogWarning(
                "Skipping corrupt member record {MemberId} for case {CaseId} in tenant {TenantId}: {Reason}",
                memberId,
                caseId,
                tenantId,
                reason);
            return;
        }

        _logger.LogWarning(
            exception,
            "Skipping corrupt member record {MemberId} for case {CaseId} in tenant {TenantId}: {Reason}",
            memberId,
            caseId,
            tenantId,
            reason);
    }

    private static bool TryGetRequiredJsonString(JsonElement root, string propertyName, out string? value)
    {
        if (root.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
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
