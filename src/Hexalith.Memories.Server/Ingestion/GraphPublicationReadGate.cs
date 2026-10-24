namespace Hexalith.Memories.Server.Ingestion;

using System.Text.Json;
using Hexalith.Memories.Server.Activities.Ingestion;
using StackExchange.Redis;

/// <summary>Repairs durable cross-store publication and rejects graph snapshots that overlap a canonical publication.</summary>
internal static class GraphPublicationReadGate
{
    internal static string JournalKey(string tenant) => "refresh-graph-repairs:" + tenant;
    internal static string EpochKey(string tenant) => "refresh-graph-epoch:" + tenant;

    /// <summary>Reconciles persisted repair authority before observing a graph snapshot.</summary>
    internal static async Task<RedisValue> BeginAsync(IConnectionMultiplexer redis, IConnectionMultiplexer graph, string tenant, CancellationToken token)
    {
        if (!await SourcePublicationVisibility.ManagedAsync(redis.GetDatabase(), tenant).ConfigureAwait(false)) { return RedisValue.Null; }
        RedisValue epoch = await redis.GetDatabase().StringGetAsync(EpochKey(tenant)).WaitAsync(token).ConfigureAwait(false);
        await ReconcileAsync(redis, graph, tenant, token).ConfigureAwait(false);
        await EndAsync(redis, tenant, epoch, token).ConfigureAwait(false);
        return epoch;
    }

    /// <summary>Fails closed if a newer pointer or unreconciled graph write overlaps a read.</summary>
    internal static async Task EndAsync(IConnectionMultiplexer redis, string tenant, RedisValue epoch, CancellationToken token)
    {
        if (!epoch.HasValue && !await SourcePublicationVisibility.ManagedAsync(redis.GetDatabase(), tenant).ConfigureAwait(false)) { return; }
        const string check = "local current=redis.call('GET',KEYS[1]); if (current or '')~=ARGV[1] or redis.call('HLEN',KEYS[2])~=0 then return 0 end return 1";
        if ((long)await redis.GetDatabase().ScriptEvaluateAsync(check, [(RedisKey)EpochKey(tenant), (RedisKey)JournalKey(tenant)],
            [(RedisValue)(epoch.HasValue ? epoch.ToString() : string.Empty)]).WaitAsync(token).ConfigureAwait(false) != 1)
        { throw new InvalidOperationException("source-publication-graph-snapshot-changed"); }
    }

    /// <summary>Retries canonical graph publication across workflow retry exhaustion and serialized process recreation.</summary>
    internal static async Task ReconcileAsync(IConnectionMultiplexer redis, IConnectionMultiplexer graph, string tenant, CancellationToken token)
    {
        IDatabase db = redis.GetDatabase();
        if (await db.HashLengthAsync(JournalKey(tenant)).WaitAsync(token).ConfigureAwait(false) > SourceInventoryStore.Capacity)
        { throw new InvalidOperationException("source-publication-graph-repair-capacity"); }
        HashEntry[] pending = await db.HashGetAllAsync(JournalKey(tenant)).WaitAsync(token).ConfigureAwait(false);
        foreach (HashEntry item in pending)
        {
            token.ThrowIfCancellationRequested();
            if (item.Value.ToString().Length > 16384) { throw new InvalidOperationException("source-publication-graph-repair-invalid"); }
            GraphPublicationRepair repair = JsonSerializer.Deserialize<GraphPublicationRepair>(item.Value.ToString())
                ?? throw new InvalidOperationException("source-publication-graph-repair-invalid");
            if (repair.TenantId != tenant || repair.Revision < 0 || string.IsNullOrWhiteSpace(repair.MemoryUnitId)
                || item.Name.ToString() != DedupKeyBuilder.BuildKey(tenant, repair.CaseId, repair.SourceUri))
            { throw new InvalidOperationException("source-publication-graph-repair-scope"); }
            await TenantIngestionOwnership.GraphWriteAsync(db, tenant,
                () => GraphSourcePublication.SetCurrentAsync(graph, tenant, repair.CaseId, repair.SourceUri, repair.MemoryUnitId, repair.Revision), token).ConfigureAwait(false);
            const string settled = "if redis.call('HGET',KEYS[1],ARGV[1])==ARGV[2] then redis.call('HDEL',KEYS[1],ARGV[1]); return 1 end return 0";
            await db.ScriptEvaluateAsync(settled, [(RedisKey)JournalKey(tenant)], [item.Name, item.Value]).WaitAsync(token).ConfigureAwait(false);
        }
    }
}
