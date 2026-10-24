namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Server.Infrastructure;
using StackExchange.Redis;

/// <summary>Atomic staged Redis writes, permanent unit tombstones and native search visibility.</summary>
internal static class SourcePublicationVisibility
{
    internal const string Field = "publicationVisible";
    internal const int KeyCapacity = 4096;
    private const string Stage = """
        -- source-publication-write-v1
        if redis.call('EXISTS',KEYS[3]) == 1 then return 0 end
        if ARGV[1] == '0' and ARGV[3] == '1' then
            local inventory = redis.call('GET',KEYS[4])
            if inventory and cjson.decode(inventory).admissionClosed then return 0 end
        end
        local managed = ARGV[1] == '1' or redis.call('EXISTS',KEYS[7]) == 1
        if managed then
            if redis.call('SCARD',KEYS[2]) >= 4096 and redis.call('SISMEMBER',KEYS[2],KEYS[1]) == 0 then return 0 end
            local initial = '0'
            if ARGV[1] == '0' then initial = '1' end
            redis.call('HSETNX',KEYS[1],'publicationVisible',initial)
            redis.call('SADD',KEYS[2],KEYS[1])
        end
        redis.call('HSET',KEYS[1],unpack(ARGV,4))
        if ARGV[3] == '1' and redis.call('EXISTS',KEYS[5]) == 1 then
            local visible = '0'
            if redis.call('GET',KEYS[6]) == ARGV[2] then visible = '1' end
            redis.call('HSET',KEYS[1],'publicationVisible',visible)
        end
        if ARGV[1] == '1' then redis.call('SET',KEYS[7],'modern') end
        return 1
        """;

    internal static string Keys(string tenant, string unit) => "publication-keys:" + tenant + ":" + unit;
    internal static string KeyProvenance(string tenant, string unit) => "publication-key-provenance:" + tenant + ":" + unit;
    internal static string Tombstone(string tenant, string unit) => "publication-tombstone:" + tenant + ":" + unit;

    internal static async Task WriteAsync(IDatabase db, string tenant, string unit, string key, HashEntry[] entries, bool tracked,
        string? caseId = null, string? sourceUri = null)
    {
        IngestionIdentityGuard.Handle(unit);
        if (!tracked && (string.IsNullOrWhiteSpace(sourceUri) || string.IsNullOrWhiteSpace(caseId)))
        {
            RedisValue[] scope = await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenant, unit), ["caseId", "sourceUri"]).ConfigureAwait(false);
            if (scope is { Length: 2 }) { caseId = scope[0]; sourceUri = scope[1]; }
        }
        bool scoped = !string.IsNullOrWhiteSpace(caseId) && !string.IsNullOrWhiteSpace(sourceUri);
        if (scoped) { IngestionIdentityGuard.Scope(tenant, sourceUri!); IngestionIdentityGuard.Handle(caseId!); }
        RedisValue[] arguments = [(RedisValue)(tracked ? "1" : "0"), (RedisValue)unit, (RedisValue)(scoped ? "1" : "0"),
            .. entries.Where(entry => entry.Name != Field).SelectMany(entry => new[] { entry.Name, entry.Value })];
        RedisResult result = await TenantIngestionOwnership.WriteAsync(db, tenant, Stage,
            [(RedisKey)key, (RedisKey)Keys(tenant, unit), (RedisKey)Tombstone(tenant, unit),
                (RedisKey)(scoped ? SourceInventoryStore.BuildKey(tenant, sourceUri!) : "__unused-source-inventory"),
                (RedisKey)(scoped ? RefreshPublicationStore.ManagedSourceKey(tenant, sourceUri!) : "__unused-managed-source"),
                (RedisKey)(scoped ? Hexalith.Memories.Server.Activities.Ingestion.DedupKeyBuilder.BuildKey(tenant, caseId!, sourceUri!) : "__unused-source-pointer"), (RedisKey)KeyProvenance(tenant, unit)], arguments).ConfigureAwait(false);
        if ((long)result != 1) { throw new InvalidOperationException("source-publication-write-fenced-or-full"); }
    }

    /// <summary>Adds the optional capability field only to existing indexes, preserving the legacy base schema and rows.</summary>
    internal static async Task EnsureSchemaAsync(IDatabase db, IEnumerable<string> names)
    {
        foreach (string name in names.Distinct(StringComparer.Ordinal))
        {
            RedisResult info;
            try { info = await db.ExecuteAsync("FT.INFO", name).ConfigureAwait(false); }
            catch (RedisServerException ex) when (IsMissingIndex(ex)) { continue; }
            if (!IndexSchemaDefinitions.GetAttributeIdentifiers(info).Contains(Field))
            {
                try { await db.ExecuteAsync("FT.ALTER", name, "SCHEMA", "ADD", Field, "NUMERIC").ConfigureAwait(false); }
                catch (RedisServerException ex) when (ex.Message.Contains("Duplicate field", StringComparison.OrdinalIgnoreCase)) { }
                info = await db.ExecuteAsync("FT.INFO", name).ConfigureAwait(false);
            }
            if (!IndexSchemaDefinitions.IsNumericAttribute(info, Field)) { throw new InvalidOperationException("source-publication-index-field-incompatible"); }
        }
    }

    internal static async Task<bool> ManagedAsync(IDatabase db, string tenant)
        => (await db.StringGetAsync("refresh-managed-tenant:" + tenant).ConfigureAwait(false)).HasValue;

    internal static async Task<string> FilterQueryAsync(IDatabase db, string tenant, string query, params string[] indexes)
    {
        if (!await ManagedAsync(db, tenant).ConfigureAwait(false)) { return query; }
        await EnsureSchemaAsync(db, indexes).ConfigureAwait(false);
        int knn = query.IndexOf("=>", StringComparison.Ordinal);
        string predicate = (knn < 0 ? query : query[..knn]).Trim();
        string grouped = predicate == "*" ? string.Empty : "(" + predicate + ") ";
        return knn < 0 ? grouped + "-@" + Field + ":[0 0]"
            : "(" + grouped + "-@" + Field + ":[0 0])" + query[knn..];
    }

    private static bool IsMissingIndex(RedisServerException error)
        => error.Message.Contains("Unknown Index", StringComparison.OrdinalIgnoreCase)
            || error.Message.Contains("no such index", StringComparison.OrdinalIgnoreCase);
}
