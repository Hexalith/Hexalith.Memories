namespace Hexalith.Memories.Server.Ingestion;

using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

/// <summary>Exact bounded tenant ownership and a permanent content-free fence for technical ingestion artifacts.</summary>
internal static class TenantIngestionOwnership
{
    internal const int Capacity = 65536;

    internal static string Key(string tenant) => "ingestion-tenant-keys:" + Digest(tenant);
    internal static string Fence(string tenant) => "ingestion-tenant-erased:" + Digest(tenant);
    internal static string PayloadUnits(string tenant) => "ingestion-tenant-payload-units:" + Digest(tenant);
    internal static string Operations(string tenant) => "ingestion-tenant-writes:" + Digest(tenant);

    /// <summary>Retains exact key ownership and checks the deletion fence in the same atomic operation as each write.</summary>
    internal static async Task<RedisResult> WriteAsync(IDatabase db, string tenant, string script, RedisKey[] keys,
        RedisValue[] arguments, CancellationToken token = default, string? precondition = null)
    {
        IngestionIdentityGuard.Handle(tenant, 128);
        int registry = keys.Length + 1, fence = keys.Length + 2;
        string retain = $$"""
            if redis.call('EXISTS',KEYS[{{fence}}])==1 then return redis.error_reply('tenant-ingestion-erased') end
            local missing=0
            for i=1,{{keys.Length}} do
                if string.sub(KEYS[i],1,9)~='__unused-' and redis.call('SISMEMBER',KEYS[{{registry}}],KEYS[i])==0 then missing=missing+1 end
            end
            if redis.call('SCARD',KEYS[{{registry}}])+missing>{{Capacity}} then return redis.error_reply('tenant-ingestion-ownership-capacity') end
            for i=1,{{keys.Length}} do
                if string.sub(KEYS[i],1,9)~='__unused-' then redis.call('SADD',KEYS[{{registry}}],KEYS[i]) end
            end
            """;
        try
        {
            return await db.ScriptEvaluateAsync((precondition is null ? string.Empty : precondition + "\n") + retain + "\n" + script,
                [.. keys, (RedisKey)Key(tenant), (RedisKey)Fence(tenant)], arguments).WaitAsync(token).ConfigureAwait(false);
        }
        catch (RedisServerException error) when (error.Message.Contains("tenant-ingestion-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(error.Message, error);
        }
    }

    /// <summary>Retains non-expiring write authority before an asynchronous graph materialization.</summary>
    internal static async Task<string> BeginOperationAsync(IDatabase db, string tenant, CancellationToken token)
    {
        string writeId = Guid.NewGuid().ToString("N");
        await WriteAsync(db, tenant, "if redis.call('SCARD',KEYS[1])>=4096 then return redis.error_reply('tenant-ingestion-operation-capacity') end return redis.call('SADD',KEYS[1],ARGV[1])",
            [(RedisKey)Operations(tenant)], [(RedisValue)writeId], token).ConfigureAwait(false);
        return writeId;
    }

    /// <summary>Settles only an independently acknowledged graph write, including after deletion admission closes.</summary>
    internal static Task<bool> CompleteOperationAsync(IDatabase db, string tenant, string writeId, CancellationToken token)
        => db.SetRemoveAsync(Operations(tenant), writeId).WaitAsync(token);

    /// <summary>Runs one graph command with durable authority, settling only acknowledgement or definite command non-execution.</summary>
    internal static async Task GraphWriteAsync(IDatabase db, string tenant, Func<Task> write, CancellationToken token)
    {
        string writeId = await BeginOperationAsync(db, tenant, token).ConfigureAwait(false);
        try { await write().WaitAsync(token).ConfigureAwait(false); }
        catch (RedisServerException error) when (IsDefiniteGraphRejection(error))
        {
            await CompleteOperationAsync(db, tenant, writeId, token).ConfigureAwait(false);
            throw;
        }
        await CompleteOperationAsync(db, tenant, writeId, token).ConfigureAwait(false);
    }

    /// <summary>Recognizes the server's explicit command ACL denial, which proves that command could not execute.</summary>
    internal static bool IsDefiniteGraphRejection(RedisServerException error)
        => error.Message.StartsWith("NOPERM ", StringComparison.Ordinal)
            && error.Message.Contains("graph.query", StringComparison.OrdinalIgnoreCase);

    /// <summary>Fences all future writes, reconciles payload bytes, then removes every owned key; unresolved writers block success.</summary>
    internal static async Task PurgeAsync(IConnectionMultiplexer redis, string tenant, IWorkflowPayloadStore? payloads, CancellationToken token)
    {
        IDatabase db = redis.GetDatabase();
        await db.StringSetAsync(Fence(tenant), "1").WaitAsync(token).ConfigureAwait(false);
        if (await db.SetLengthAsync(Operations(tenant)).WaitAsync(token).ConfigureAwait(false) != 0)
        { throw new InvalidOperationException("tenant-ingestion-write-completion-unresolved"); }
        if (await db.SetLengthAsync(PayloadUnits(tenant)).WaitAsync(token).ConfigureAwait(false) > Capacity)
        { throw new InvalidOperationException("tenant-ingestion-ownership-capacity"); }
        RedisValue[] units = await db.SetMembersAsync(PayloadUnits(tenant)).WaitAsync(token).ConfigureAwait(false);
        if (units.Length != 0 && payloads is null) { throw new InvalidOperationException("tenant-ingestion-payload-store-required"); }
        foreach (RedisValue unit in units)
        {
            IngestionIdentityGuard.Handle(unit.ToString());
            await new WorkflowPayloadLedger(redis).PurgeAsync(tenant, unit.ToString(), payloads!, token).ConfigureAwait(false);
        }
        if (await db.SetLengthAsync(Key(tenant)).WaitAsync(token).ConfigureAwait(false) > Capacity)
        { throw new InvalidOperationException("tenant-ingestion-ownership-capacity"); }
        RedisValue[] owned = await db.SetMembersAsync(Key(tenant)).WaitAsync(token).ConfigureAwait(false);
        foreach (RedisValue[] batch in owned.Chunk(256))
        {
            token.ThrowIfCancellationRequested();
            await db.KeyDeleteAsync(batch.Select(key => (RedisKey)key.ToString()).ToArray()).WaitAsync(token).ConfigureAwait(false);
        }
        await db.KeyDeleteAsync(Key(tenant)).WaitAsync(token).ConfigureAwait(false);
    }

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
