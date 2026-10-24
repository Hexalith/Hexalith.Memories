namespace Hexalith.Memories.Server.Ingestion;

using System.Security.Cryptography;
using System.Text.Json;
using StackExchange.Redis;

/// <summary>Retains bounded claim-check identities before save so failed compensation cannot orphan producer payload bytes.</summary>
internal sealed class WorkflowPayloadLedger(IConnectionMultiplexer redis)
{
    private const int Capacity = 4096;
    private const string Retain = "if redis.call('EXISTS',KEYS[2])==1 then return -1 end if redis.call('SCARD',KEYS[1])>=4096 and redis.call('SISMEMBER',KEYS[1],ARGV[1])==0 then return 0 end if redis.call('HLEN',KEYS[3])>=4096 then return 0 end redis.call('SADD',KEYS[1],ARGV[1]); redis.call('HSET',KEYS[3],ARGV[2],ARGV[1]); redis.call('SADD',KEYS[4],ARGV[3]); return 1";

    /// <summary>Persists the exact content-free artifact reference before the Dapr write.</summary>
    internal async Task<string> BeginAsync(WorkflowPayloadReference reference, CancellationToken token)
    {
        string json = JsonSerializer.Serialize(reference);
        string writeId = Guid.NewGuid().ToString("N");
        if (json.Length > 16384) { throw new InvalidOperationException("workflow-payload-ledger-reference-bound"); }
        long result = (long)await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), reference.TenantId, Retain,
            [(RedisKey)Key(reference.TenantId, reference.MemoryUnitId), (RedisKey)FenceKey(reference.TenantId, reference.MemoryUnitId), (RedisKey)PendingKey(reference.TenantId, reference.MemoryUnitId), (RedisKey)TenantIngestionOwnership.PayloadUnits(reference.TenantId)],
            [(RedisValue)json, (RedisValue)writeId, (RedisValue)reference.MemoryUnitId]).WaitAsync(token).ConfigureAwait(false);
        if (result != 1) { throw new InvalidOperationException(result == -1 ? "workflow-payload-unit-erased" : "workflow-payload-ledger-capacity"); }
        return writeId;
    }

    /// <summary>Settles only a writer whose Dapr save returned an affirmative acknowledgement; ambiguous completions remain retained.</summary>
    internal async Task CompleteWriteAsync(WorkflowPayloadReference reference, string writeId, CancellationToken token)
    {
        const string complete = "if redis.call('HGET',KEYS[2],ARGV[1])~=ARGV[2] then return 0 end redis.call('SADD',KEYS[1],ARGV[2]); redis.call('HDEL',KEYS[2],ARGV[1]); return 1";
        if ((long)await redis.GetDatabase().ScriptEvaluateAsync(complete,
            [(RedisKey)Key(reference.TenantId, reference.MemoryUnitId), (RedisKey)PendingKey(reference.TenantId, reference.MemoryUnitId)],
            [(RedisValue)writeId, (RedisValue)JsonSerializer.Serialize(reference)]).WaitAsync(token).ConfigureAwait(false) != 1)
        { throw new InvalidOperationException("workflow-payload-write-provenance-mismatch"); }
    }

    /// <summary>Subtracts only an exact reference after independent physical absence.</summary>
    internal async Task RemoveAsync(WorkflowPayloadReference reference, CancellationToken token)
    {
        const string remove = "if redis.call('HLEN',KEYS[2])~=0 then return 0 end return redis.call('SREM',KEYS[1],ARGV[1])";
        await redis.GetDatabase().ScriptEvaluateAsync(remove,
            [(RedisKey)Key(reference.TenantId, reference.MemoryUnitId), (RedisKey)PendingKey(reference.TenantId, reference.MemoryUnitId)],
            [(RedisValue)JsonSerializer.Serialize(reference)]).WaitAsync(token).ConfigureAwait(false);
    }

    /// <summary>Fences late writers and physically reconciles every retained claim-check reference.</summary>
    internal async Task PurgeAsync(string tenant, string unit, IWorkflowPayloadStore payloads, CancellationToken token)
    {
        IDatabase db = redis.GetDatabase();
        await db.StringSetAsync(FenceKey(tenant, unit), "1").WaitAsync(token).ConfigureAwait(false);
        if (await db.HashLengthAsync(PendingKey(tenant, unit)).WaitAsync(token).ConfigureAwait(false) != 0)
        { throw new InvalidOperationException("workflow-payload-write-completion-unresolved"); }
        if (await db.SetLengthAsync(Key(tenant, unit)).WaitAsync(token).ConfigureAwait(false) > Capacity)
        { throw new InvalidOperationException("workflow-payload-ledger-capacity"); }
        RedisValue[] values = await db.SetMembersAsync(Key(tenant, unit)).WaitAsync(token).ConfigureAwait(false);
        foreach (RedisValue value in values)
        {
            if (value.ToString().Length > 16384) { throw new InvalidOperationException("workflow-payload-ledger-reference-bound"); }
            WorkflowPayloadReference reference = JsonSerializer.Deserialize<WorkflowPayloadReference>(value.ToString())
                ?? throw new InvalidOperationException("workflow-payload-ledger-reference-invalid");
            if (reference.TenantId != tenant || reference.MemoryUnitId != unit)
            { throw new InvalidOperationException("workflow-payload-ledger-reference-scope"); }
            await payloads.DeleteAsync(reference, token).ConfigureAwait(false);
            try
            {
                _ = await payloads.ReadAsync(reference, tenant, unit, reference.ContentKind, token).ConfigureAwait(false);
                throw new InvalidOperationException("workflow-payload-ledger-bytes-retained");
            }
            catch (WorkflowPayloadException error) when (error.ErrorCode == "PAYLOAD_NOT_FOUND") { }
            await RemoveAsync(reference, token).ConfigureAwait(false);
        }
    }

    /// <summary>Checks the permanent writer fence after an asynchronous save completes.</summary>
    internal async Task<bool> IsFencedAsync(string tenant, string unit, CancellationToken token)
    {
        RedisResult fenced = await redis.GetDatabase().ScriptEvaluateAsync("return redis.call('EXISTS',KEYS[1],KEYS[2])",
            [(RedisKey)FenceKey(tenant, unit), (RedisKey)TenantIngestionOwnership.Fence(tenant)]).WaitAsync(token).ConfigureAwait(false);
        return (long)fenced != 0;
    }

    internal static string Key(string tenant, string unit) => "workflow-payload-ledger:" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { tenant, unit })));
    internal static string PendingKey(string tenant, string unit) => Key(tenant, unit) + ":writing";
    private static string FenceKey(string tenant, string unit) => Key(tenant, unit) + ":erased";
}
