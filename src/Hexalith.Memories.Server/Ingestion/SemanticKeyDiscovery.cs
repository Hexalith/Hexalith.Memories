namespace Hexalith.Memories.Server.Ingestion;

using System.Globalization;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Search;
using StackExchange.Redis;

/// <summary>Bounded exact-unit semantic discovery with durable completeness provenance.</summary>
internal static class SemanticKeyDiscovery
{
    /// <summary>Resolves and validates all owned semantic keys without scanning the Redis keyspace.</summary>
    internal static async Task<RedisKey[]> FindAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        await RegisterLegacyKeysAsync(db, tenant, unit, token).ConfigureAwait(false);
        RedisValue[] values = await db.SetMembersAsync(SourcePublicationVisibility.Keys(tenant, unit)).WaitAsync(token).ConfigureAwait(false);
        if (values.Length > SourcePublicationVisibility.KeyCapacity)
        {
            throw new InvalidOperationException("source-publication-key-capacity");
        }
        foreach (RedisValue value in values)
        {
            ValidateKey(tenant, unit, value.ToString());
        }
        // Provenance also retains syntactic/NL hashes. Their deletion has separate
        // ordering: keep the syntactic recovery hash until vectors and graph acknowledge.
        string baseKey = IndexSchemaDefinitions.BuildSemanticKey(tenant, unit);
        return values.Select(value => (RedisKey)value.ToString()).Where(key => key == baseKey
                || IndexSchemaDefinitions.TryParseSemanticChunkKey(tenant, key, out string parsed, out _) && parsed == unit)
            .Append((RedisKey)baseKey).Distinct().ToArray();
    }

    internal static async Task RegisterLegacyKeysAsync(IDatabase db, string tenant, string unit, CancellationToken cancellationToken)
    {
        HashSet<RedisKey> keys = [IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), IndexSchemaDefinitions.BuildSemanticKey(tenant, unit),
            IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit), IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit)];
        string trackedKey = SourcePublicationVisibility.Keys(tenant, unit);
        long knownCount = await db.SetLengthAsync(trackedKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (knownCount > SourcePublicationVisibility.KeyCapacity)
        {
            throw new InvalidOperationException("source-publication-key-capacity");
        }
        RedisValue provenance = await db.StringGetAsync(SourcePublicationVisibility.KeyProvenance(tenant, unit)).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (provenance.HasValue && provenance != "modern" && provenance != "legacy-owner-reconciled-v1")
        {
            throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
        }
        RedisValue[] retained = await db.SetMembersAsync(trackedKey).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (retained.Length > SourcePublicationVisibility.KeyCapacity)
        {
            throw new InvalidOperationException("source-publication-key-capacity");
        }
        foreach (RedisValue value in retained)
        {
            ValidateKey(tenant, unit, value.ToString());
        }
        if (provenance.HasValue)
        {
            // Modern writers retain every exact output key before materialization.
            // Refresh must never rediscover those keys through a database scan.
            RedisValue[] known = await db.SetMembersAsync(trackedKey).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (known.Length > SourcePublicationVisibility.KeyCapacity)
            {
                throw new InvalidOperationException("source-publication-key-capacity");
            }
            return;
        }
        try
        {
            // Native TAG results identify obligations, but cannot prove that historical hashes
            // populated the optional TAG. Only an explicitly reconciled owner inventory may
            // grant legacy completeness; unknown historical coverage stays incomplete.
            RedisResult info = await db.ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSemanticIndexName(tenant)).WaitAsync(cancellationToken).ConfigureAwait(false);
            for (int attempt = 0; attempt < 20 && ResultMap((RedisResult[]?)info ?? []).TryGetValue("indexing", out var pending) && pending.ToString() == "1"; attempt++)
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                info = await db.ExecuteAsync("FT.INFO", IndexSchemaDefinitions.GetSemanticIndexName(tenant)).WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            ValidateLegacyCoverage(info ?? throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required"), tenant, unit);
            RedisResult[] policies = (RedisResult[])(await db.ExecuteAsync("FT.CONFIG", "GET", "ON_TIMEOUT").WaitAsync(cancellationToken).ConfigureAwait(false))!;
            // RESP2 returns one nested pair; RESP3 represents the configuration map as a flat pair.
            RedisResult[] policy = policies is { Length: 1 } ? (RedisResult[]?)policies[0] ?? [] : policies ?? [];
            if (policy is not { Length: 2 }
                || !string.Equals(policy[0].ToString(), "ON_TIMEOUT", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(policy[1].ToString(), "FAIL", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
            }
            RedisResult raw = await db.ExecuteAsync("FT.SEARCH", IndexSchemaDefinitions.GetSemanticIndexName(tenant),
                            "@memoryUnitId:{" + RediSearchQueryEscaper.EscapeTag(unit) + "}", "NOCONTENT", "LIMIT", "0",
                            SourcePublicationVisibility.KeyCapacity + 1, "TIMEOUT", "1000", "DIALECT", "2").WaitAsync(cancellationToken).ConfigureAwait(false);
            RedisResult[] rows = (RedisResult[])raw!;
            long total;
            string[] discovered;
            if (rows.Length > 0 && long.TryParse(rows[0].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out total))
            {
                discovered = rows.Skip(1).Select(row => row.ToString()).ToArray();
            }
            else
            {
                Dictionary<string, RedisResult> map = ResultMap(rows);
                if (!map.TryGetValue("total_results", out RedisResult? totalResult)
                    || !long.TryParse(totalResult.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out total)
                    || !map.TryGetValue("results", out RedisResult? resultRows))
                {
                    throw new InvalidOperationException("source-publication-legacy-discovery-invalid");
                }
                discovered = ((RedisResult[]?)resultRows ?? []).Select(row =>
                {
                    Dictionary<string, RedisResult> fields = ResultMap((RedisResult[]?)row ?? []);
                    return fields.TryGetValue("id", out RedisResult? id) ? id.ToString() : string.Empty;
                }).ToArray();
            }
            if (total > SourcePublicationVisibility.KeyCapacity || discovered.Length != total)
            {
                throw new InvalidOperationException("source-publication-key-capacity");
            }
            foreach (string key in discovered)
            {
                if (key != IndexSchemaDefinitions.BuildSemanticKey(tenant, unit)
                    && key != IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit)
                    && (!IndexSchemaDefinitions.TryParseSemanticChunkKey(tenant, (RedisKey)key, out string parsed, out _) || parsed != unit))
                {
                    throw new InvalidOperationException("source-publication-legacy-key-scope");
                }
                keys.Add(key);
            }
            if (keys.Count > SourcePublicationVisibility.KeyCapacity)
            {
                throw new InvalidOperationException("source-publication-key-capacity");
            }
        }
        catch (RedisServerException error)
        {
            throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required", error);
        }
        const string register = """
            -- Complete bounded legacy discovery is sealed only after all exact keys are retained.
            local retained=redis.call('SMEMBERS',KEYS[1])
            local merged={}
            for _,key in ipairs(retained) do merged[key]=true end
            for i=3,#ARGV do merged[ARGV[i]]=true end
            local count=0
            for key,_ in pairs(merged) do
                count=count+1
                if count>4096 then return 0 end
                local kind=redis.call('TYPE',key).ok
                if kind~='none' and kind~='hash' then return 0 end
            end
            for key,_ in pairs(merged) do
                if redis.call('EXISTS',key)==1 and redis.call('HGET',key,'tenantId')==ARGV[1]
                    and redis.call('HGET',key,'memoryUnitId')==ARGV[2] then
                    redis.call('HSETNX',key,'publicationVisible','1')
                    redis.call('SADD',KEYS[1],key)
                end
            end
            -- No completeness authority can be inferred from an optional TAG query.
            return 1
            """;
        RedisResult registered = await TenantIngestionOwnership.WriteAsync(db, tenant, register,
            [(RedisKey)trackedKey, (RedisKey)SourcePublicationVisibility.KeyProvenance(tenant, unit)],
            new RedisValue[] { tenant, unit }.Concat(keys.Select(key => (RedisValue)key.ToString())).ToArray()).WaitAsync(cancellationToken).ConfigureAwait(false);
        _ = registered;
        throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
    }

    private static void ValidateKey(string tenant, string unit, string key)
    {
        bool exact = key == IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit)
            || key == IndexSchemaDefinitions.BuildSemanticKey(tenant, unit)
            || key == IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenant, unit)
            || key == IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenant, unit);
        if (!exact && (!IndexSchemaDefinitions.TryParseSemanticChunkKey(tenant, (RedisKey)key, out string parsed, out _) || parsed != unit))
        {
            throw new InvalidOperationException("source-publication-legacy-key-scope");
        }
    }

    private static void ValidateLegacyCoverage(RedisResult raw, string tenant, string unit)
    {
        Dictionary<string, RedisResult> info = ResultMap((RedisResult[]?)raw ?? []);
        if (!info.TryGetValue("index_definition", out var definitionRaw)
            || !info.TryGetValue("attributes", out var attributesRaw)
            || !info.TryGetValue("indexing", out var indexing) || indexing.ToString() != "0"
            || !info.TryGetValue("hash_indexing_failures", out var failures) || failures.ToString() != "0")
        {
            throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
        }
        Dictionary<string, RedisResult> definition = ResultMap((RedisResult[]?)definitionRaw ?? []);
        if (!definition.TryGetValue("key_type", out var keyType) || keyType.ToString() != "HASH"
            || !definition.TryGetValue("prefixes", out var prefixesRaw)
            || ((RedisResult[]?)prefixesRaw ?? []) is not { Length: 1 } prefixes
            || prefixes[0].ToString() != IndexSchemaDefinitions.GetSemanticKeyPrefix(tenant)
            || definition.TryGetValue("filter", out var filter) && filter.ToString().Length != 0)
        {
            throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
        }
        bool exactUnitTag = ((RedisResult[]?)attributesRaw ?? []).Any(attribute =>
                {
                    var fields = ResultMap((RedisResult[]?)attribute ?? []);
                    return fields.TryGetValue("identifier", out var identifier) && identifier.ToString() == "memoryUnitId"
                        && fields.TryGetValue("attribute", out var alias) && alias.ToString() == "memoryUnitId"
                        && fields.TryGetValue("type", out var type) && type.ToString() == "TAG"
                        && fields.TryGetValue("SEPARATOR", out var separator) && separator.ToString() is { Length: 1 } delimiter
                        && !unit.Contains(delimiter, StringComparison.Ordinal) && unit == unit.Trim();
                });
        if (!exactUnitTag)
        {
            throw new InvalidOperationException("source-publication-legacy-owner-reconciliation-required");
        }
    }

    private static Dictionary<string, RedisResult> ResultMap(RedisResult[] values)
    {
        if (values.Length % 2 != 0)
        {
            throw new InvalidOperationException("source-publication-legacy-discovery-invalid");
        }
        Dictionary<string, RedisResult> map = new(StringComparer.Ordinal);
        for (int index = 0; index < values.Length; index += 2)
        {
            map.Add(values[index].ToString(), values[index + 1]);
        }
        return map;
    }

}
