// <copyright file="RedisNaturalLanguageNamespaceMigrator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;

using StackExchange.Redis;

/// <summary>Migrates legacy nested natural-language semantic hashes to the disjoint Story 21.3 prefix.</summary>
internal static class RedisNaturalLanguageNamespaceMigrator
{
    private const int ScanPageSize = 1000;

    /// <summary>Copies verified legacy NL hashes to the disjoint prefix and deletes only after target verification.</summary>
    /// <param name="db">The Redis database.</param>
    /// <param name="server">The Redis server used for cursor scanning.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when migration has converged for the tenant.</returns>
    public static async Task MigrateAsync(IDatabase db, IServer server, string tenantId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string legacyPrefix = IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(tenantId);

        await foreach (RedisKey legacyKey in server.KeysAsync(pattern: legacyPrefix + "*", pageSize: ScanPageSize).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (!IndexSchemaDefinitions.TryParseLegacyNaturalLanguageSemanticMemoryUnitId(tenantId, legacyKey, out string memoryUnitId))
            {
                continue;
            }

            await MigrateOneAsync(db, tenantId, memoryUnitId, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Copies and retires exactly one legacy hash under the same atomic erasure and source fence.</summary>
    internal static async Task MigrateOneAsync(IDatabase db, string tenant, string unit, CancellationToken ct)
    {
        RedisKey sourceKey = IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit);
        RedisValue source = await db.HashGetAsync(sourceKey, "sourceUri").WaitAsync(ct).ConfigureAwait(false);
        const string ownership = """
            -- nl-namespace-migration-fenced-v3
            if redis.call('EXISTS',KEYS[3],KEYS[7])~=0 or redis.call('EXISTS',KEYS[1])==0 then return 0 end
            if redis.call('HGET',KEYS[4],'id')~=ARGV[1]
                or redis.call('HGET',KEYS[4],'tenantId')~=ARGV[2]
                or ARGV[3]=='' or redis.call('HGET',KEYS[4],'sourceUri')~=ARGV[3]
                or redis.call('HGET',KEYS[4],'caseId')~=redis.call('HGET',KEYS[1],'caseId') then return -3 end
            local function valid(key)
                if redis.call('HGET',key,'memoryUnitId')~=ARGV[1] then return false end
                local tenant=redis.call('HGET',key,'tenantId')
                if tenant and tenant~=ARGV[2] then return false end
                for _,field in ipairs({'embedding','caseId','naturalLanguageDescription'}) do
                    local value=redis.call('HGET',key,field)
                    if not value or value=='' then return false end
                end
                return redis.call('HGET',key,'caseId')==redis.call('HGET',KEYS[4],'caseId')
            end
            if not valid(KEYS[1]) then return -4 end
            if redis.call('EXISTS',KEYS[2])~=0 and not valid(KEYS[2]) then return -1 end
            local inventory=redis.call('GET',KEYS[5])
            if inventory and cjson.decode(inventory).admissionClosed then return 0 end
            if redis.call('EXISTS',KEYS[2])==0 and redis.call('SCARD',KEYS[6])>=4096
                and redis.call('SISMEMBER',KEYS[6],KEYS[2])==0 then return -2 end
            """;
        const string migrate = """
            if redis.call('EXISTS',KEYS[2])==0 then
                redis.call('HSET',KEYS[2],unpack(redis.call('HGETALL',KEYS[1])))
                redis.call('SADD',KEYS[6],KEYS[2])
            end
            redis.call('DEL',KEYS[1])
            return 1
            """;
        RedisResult result = await TenantIngestionOwnership.WriteAsync(db, tenant, migrate,
            [(RedisKey)IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit),
                (RedisKey)IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit),
                (RedisKey)SourcePublicationVisibility.Tombstone(tenant, unit), sourceKey,
                (RedisKey)(source.HasValue ? SourceInventoryStore.BuildKey(tenant, source.ToString()) : "__unused-source-inventory"),
                (RedisKey)SourcePublicationVisibility.Keys(tenant, unit), (RedisKey)TenantIngestionOwnership.Fence(tenant)],
            [(RedisValue)unit, (RedisValue)tenant, source.HasValue ? source : RedisValue.EmptyString], ct, precondition: ownership).ConfigureAwait(false);
        if ((long)result == -4)
            throw new InvalidOperationException("nl-namespace-legacy-owner-reconciliation-required");
        if ((long)result == -3)
            throw new InvalidOperationException("nl-namespace-live-owner-reconciliation-required");
        if ((long)result == -2)
            throw new InvalidOperationException("nl-namespace-inventory-capacity-owner-reconciliation-required");
        if ((long)result == -1)
            throw new InvalidOperationException("nl-namespace-canonical-owner-reconciliation-required");
    }
}
