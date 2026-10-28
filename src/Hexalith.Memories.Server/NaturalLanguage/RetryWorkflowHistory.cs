namespace Hexalith.Memories.Server.NaturalLanguage;

using Dapr.Workflow;
using Hexalith.Memories.Server.Ingestion;
using StackExchange.Redis;

/// <summary>Durable content-free retry history discovery and unresolved scheduling authority.</summary>
internal static class RetryWorkflowHistory
{
    /// <summary>Identifies independently reconciled historical workflow coverage.</summary>
    internal const string ReconciledCoverage = "owner-reconciled-v1";

    /// <summary>Gets the bounded tenant unit inventory key.</summary>
    internal static string Units(string tenant) => "nl-retry-history-units:" + tenant;
    /// <summary>Gets the tenant scheduling authority hash key.</summary>
    internal static string Scheduling(string tenant) => "nl-retry-history-scheduling:" + tenant;
    /// <summary>Gets the exact-unit scheduler lease key.</summary>
    internal static string Lease(string tenant, string unit) => "nl-retry-scheduler-lease:" + tenant + ":" + unit;
    /// <summary>Gets the tenant historical coverage attestation key.</summary>
    internal static string Coverage(string tenant) => "nl-retry-history-coverage:" + tenant;
    /// <summary>Gets the content-free unresolved-history marker key.</summary>
    internal static string Unresolved(string tenant) => "nl-retry-history-unresolved:" + tenant;

    /// <summary>Retains history and in-flight authority before the asynchronous Dapr scheduling boundary.</summary>
    internal static async Task<string?> BeginAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        IngestionIdentityGuard.Scope(tenant, unit);
        IngestionIdentityGuard.Handle(unit);
        string authority = Guid.NewGuid().ToString("N");
        const string begin = """
            for i,expected in pairs({[3]='set',[4]='hash',[5]='string'}) do
                local kind=redis.call('TYPE',KEYS[i]).ok
                if kind~='none' and kind~=expected then return redis.error_reply('WRONGTYPE nl-retry-history') end
            end
            if redis.call('EXISTS',KEYS[1],KEYS[2])~=0 then return 0 end
            if redis.call('SCARD',KEYS[3])>=65536 and redis.call('SISMEMBER',KEYS[3],ARGV[1])==0 then return redis.error_reply('nl-retry-history-capacity') end
            if redis.call('EXISTS',KEYS[5])==1 then return 0 end
            if redis.call('HLEN',KEYS[4])>=65536 then return redis.error_reply('nl-retry-history-capacity') end
            redis.call('SET',KEYS[5],ARGV[2]); redis.call('SADD',KEYS[3],ARGV[1]); redis.call('HSET',KEYS[4],ARGV[2],ARGV[1]); return 1
            """;
        RedisResult result = await db.ScriptEvaluateAsync(begin,
            [(RedisKey)TenantIngestionOwnership.Fence(tenant), (RedisKey)SourcePublicationVisibility.Tombstone(tenant, unit),
                (RedisKey)Units(tenant), (RedisKey)Scheduling(tenant), (RedisKey)Lease(tenant, unit)], [(RedisValue)unit, (RedisValue)authority]).WaitAsync(token).ConfigureAwait(false);
        return (long)result == 1 ? authority : null;
    }

    /// <summary>Settles an acknowledged schedule; ambiguous submissions remain discoverable across restart.</summary>
    internal static async Task AcknowledgeAsync(IDatabase db, string tenant, string unit, string authority, CancellationToken token)
    {
        const string acknowledge = """
            if redis.call('GET',KEYS[2])~=ARGV[1] or redis.call('HGET',KEYS[1],ARGV[1])~=ARGV[2] then return 0 end
            redis.call('HDEL',KEYS[1],ARGV[1]); redis.call('DEL',KEYS[2]); return 1
            """;
        await db.ScriptEvaluateAsync(acknowledge, [(RedisKey)Scheduling(tenant), (RedisKey)Lease(tenant, unit)],
            [(RedisValue)authority, (RedisValue)unit]).WaitAsync(token).ConfigureAwait(false);
    }

    /// <summary>Retains one exact unit with atomic capacity enforcement and existing-member idempotency.</summary>
    internal static async Task RetainUnitAsync(IDatabase db, string tenant, string unit, CancellationToken token)
    {
        IngestionIdentityGuard.Handle(unit);
        const string retain = """
            -- nl-retry-retain-unit-v1
            if redis.call('SCARD',KEYS[1])>=65536 and redis.call('SISMEMBER',KEYS[1],ARGV[1])==0 then return redis.error_reply('nl-retry-history-capacity') end
            return redis.call('SADD',KEYS[1],ARGV[1])
            """;
        await db.ScriptEvaluateAsync(retain, [(RedisKey)Units(tenant)], [(RedisValue)unit]).WaitAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Attests empty retry coverage for a newly provisioned tenant that has no historical inventory.
    /// Existing obligations, inventory, or a different attestation are left unchanged.
    /// </summary>
    /// <param name="db">The Redis database.</param>
    /// <param name="tenant">The tenant identifier.</param>
    /// <param name="token">The cancellation token.</param>
    internal static async Task AttestFreshCoverageAsync(IDatabase db, string tenant, CancellationToken token)
    {
        IngestionIdentityGuard.Scope(tenant, tenant);
        const string attest = """
            -- nl-retry-fresh-coverage-v1
            if redis.call('EXISTS',KEYS[2])==1 or redis.call('SCARD',KEYS[3])~=0 or redis.call('HLEN',KEYS[4])~=0
                or redis.call('ZCARD',KEYS[5])~=0 or redis.call('ZCARD',KEYS[6])~=0 then return 0 end
            local current=redis.call('GET',KEYS[1])
            if current==ARGV[1] then return 1 end
            if current then return 0 end
            redis.call('SET',KEYS[1],ARGV[1]); return 1
            """;
        await db.ScriptEvaluateAsync(attest,
            [(RedisKey)Coverage(tenant), (RedisKey)Unresolved(tenant), (RedisKey)Units(tenant), (RedisKey)Scheduling(tenant),
                (RedisKey)FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), (RedisKey)FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant)],
            [(RedisValue)ReconciledCoverage]).WaitAsync(token).ConfigureAwait(false);
    }

    /// <summary>Reconciles queues after fencing, then purges independently scoped Dapr history.</summary>
    internal static async Task PurgeTenantAsync(IDatabase db, IDaprWorkflowClient? client, string tenant,
        bool legacySchedulersDrained, CancellationToken token)
    {
        IngestionIdentityGuard.Scope(tenant, tenant);
        await db.StringSetAsync(TenantIngestionOwnership.Fence(tenant), "1").WaitAsync(token).ConfigureAwait(false);
        if (await db.HashLengthAsync(Scheduling(tenant)).WaitAsync(token).ConfigureAwait(false) != 0)
            throw new InvalidOperationException("nl-retry-scheduling-unresolved");
        if (await db.StringGetAsync(Coverage(tenant)).WaitAsync(token).ConfigureAwait(false) != ReconciledCoverage
            || await db.KeyExistsAsync(Unresolved(tenant)).WaitAsync(token).ConfigureAwait(false))
            throw new InvalidOperationException("nl-retry-history-owner-reconciliation-required");
        if (client is null) throw new InvalidOperationException("nl-retry-workflow-client-required");
        if (await db.SetLengthAsync(Units(tenant)).WaitAsync(token).ConfigureAwait(false) > TenantIngestionOwnership.Capacity)
            throw new InvalidOperationException("nl-retry-history-capacity");
        HashSet<string> units = new(StringComparer.Ordinal);
        foreach (RedisValue unit in await db.SetMembersAsync(Units(tenant)).WaitAsync(token).ConfigureAwait(false))
            AddUnit(unit.ToString());
        foreach ((string queue, string payloads) in new[]
        {
            (FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant)),
            (FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant)),
        })
        {
            if (await db.SortedSetLengthAsync(queue).WaitAsync(token).ConfigureAwait(false) > 100_000
                || await db.HashLengthAsync(payloads).WaitAsync(token).ConfigureAwait(false) > 100_000)
                throw new InvalidOperationException("nl-retry-history-capacity");
            await foreach (HashEntry entry in db.HashScanAsync(payloads, pageSize: 100).WithCancellation(token))
            {
                var record = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(entry.Value.ToString());
                if (record is null || !FailedNaturalLanguageEmbeddingRegistry.HasQualifiedShape(entry.Value.ToString()) || record.TenantId != tenant || record.MemoryUnitId != entry.Name)
                    throw new InvalidOperationException("nl-retry-payload-scope");
                AddUnit(record.MemoryUnitId);
            }
            await foreach (SortedSetEntry entry in db.SortedSetScanAsync(queue, pageSize: 100).WithCancellation(token))
            {
                RedisValue payload = await db.HashGetAsync(payloads, entry.Element).WaitAsync(token).ConfigureAwait(false);
                var record = FailedNaturalLanguageEmbeddingRegistry.TryDeserialize(payload.HasValue ? payload.ToString() : entry.Element.ToString());
                if (record is null || !FailedNaturalLanguageEmbeddingRegistry.HasQualifiedShape(payload.HasValue ? payload.ToString() : entry.Element.ToString())
                    || record.TenantId != tenant || (payload.HasValue && record.MemoryUnitId != entry.Element))
                    throw new InvalidOperationException("nl-retry-payload-scope");
                AddUnit(record.MemoryUnitId);
            }
        }
        // Preserve the union before the first network call or removal so partial cleanup is restartable.
        foreach (string unit in units)
        {
            token.ThrowIfCancellationRequested();
            await FailedNaturalLanguageEmbeddingRegistry.FenceUnitAsync(db, tenant, unit, token).ConfigureAwait(false);
        }
        foreach (string unit in units)
        {
            await RetryWorkflowErasure.PurgeAsync(client, tenant, unit, token, legacySchedulersDrained).ConfigureAwait(false);
        }
        // The permanent tenant fence closes registered writes. The inventory above was validated once;
        // never rescan both queues once per unit, and retain all history authority until purge succeeds.
        if (await db.HashLengthAsync(Scheduling(tenant)).WaitAsync(token).ConfigureAwait(false) != 0)
            throw new InvalidOperationException("nl-retry-scheduling-unresolved");
        await db.KeyDeleteAsync(new RedisKey[] { FailedNaturalLanguageEmbeddingRegistry.LiveKey(tenant), FailedNaturalLanguageEmbeddingRegistry.LivePayloadKey(tenant),
            FailedNaturalLanguageEmbeddingRegistry.DeadKey(tenant), FailedNaturalLanguageEmbeddingRegistry.DeadPayloadKey(tenant) }).WaitAsync(token).ConfigureAwait(false);
        await db.SetRemoveAsync(FailedNaturalLanguageEmbeddingRegistry.TenantBacklogKey, tenant).WaitAsync(token).ConfigureAwait(false);
        // History units and coverage remain content-free deletion evidence. They never infer historical completeness.

        void AddUnit(string unit)
        {
            IngestionIdentityGuard.Handle(unit);
            units.Add(unit);
            if (units.Count > TenantIngestionOwnership.Capacity) throw new InvalidOperationException("nl-retry-history-capacity");
        }
    }
}
