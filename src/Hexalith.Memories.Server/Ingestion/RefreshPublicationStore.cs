namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.Json;
using Hexalith.Memories.Server.Search;
using StackExchange.Redis;

/// <summary>Revision publication and search visibility share one canonical compare/exchange authority.</summary>
internal sealed class RefreshPublicationStore([FromKeyedServices("redis")] IConnectionMultiplexer redis, SourceInventoryStore inventory,
    [FromKeyedServices("falkordb")] IConnectionMultiplexer falkorDb)
{
    private const string Publish = """
        if redis.call('HEXISTS',KEYS[9],KEYS[1]) == 0 and redis.call('HLEN',KEYS[9]) >= 1024 then return 0 end
        if redis.call('EXISTS',KEYS[7]) == 1 or redis.call('GET',KEYS[4]) ~= ARGV[4] then return 0 end
        local current = redis.call('GET', KEYS[1])
        local token = redis.call('GET', KEYS[2])
        if token then
            if token == ARGV[2] and current == ARGV[2] and redis.call('GET',KEYS[4]) == ARGV[4] then return 1 end
            return 0
        end
        local revision = redis.call('GET',KEYS[3])
        if revision and (not string.match(revision,'^[0-9]+$') or #ARGV[3] < #revision or (#ARGV[3] == #revision and (ARGV[3] < revision or (ARGV[3] == revision and (ARGV[1] == '' or redis.call('EXISTS',KEYS[8]) == 0))))) then return 0 end
        if (ARGV[1] == '' and current) or (ARGV[1] ~= '' and current ~= ARGV[1]) then return 0 end
        local candidateKeys = redis.call('SMEMBERS', KEYS[5])
        local priorKeys = {}
        if ARGV[1] ~= '' then priorKeys = redis.call('SMEMBERS', KEYS[6]) end
        if #candidateKeys == 0 or #candidateKeys > 4096 or #priorKeys > 4096 then return 0 end
        for _, key in ipairs(candidateKeys) do if redis.call('EXISTS',key) == 0 then return 0 end end
        for _, key in ipairs(priorKeys) do if redis.call('EXISTS',key) == 1 then redis.call('HSET',key,'publicationVisible','0') end end
        for _, key in ipairs(candidateKeys) do redis.call('HSET',key,'publicationVisible','1') end
        redis.call('SET', KEYS[1], ARGV[2])
        redis.call('SET', KEYS[2], ARGV[2])
        redis.call('SET', KEYS[3], ARGV[3])
        redis.call('SET', KEYS[4], ARGV[4])
        redis.call('HSET',KEYS[9],KEYS[1],ARGV[5])
        redis.call('INCR',KEYS[10])
        return 1
        """;

    /// <summary>Establishes the publication visibility barrier before a candidate can reach any derived store.</summary>
    internal async Task<string> PrepareAsync(IngestionInput input, CancellationToken cancellationToken)
    {
        IngestionInputValidator.Validate(input, requireInlineRefreshContent: true);
        ArgumentNullException.ThrowIfNull(input.ContentBytes);
        if (input.ReopenSourceVersion is not null)
        {
            await inventory.ReopenAsync(input.TenantId, input.SourceUri, input.ReopenSourceVersion, cancellationToken).ConfigureAwait(false);
        }
        await inventory.EnsureAdmissionOpenAsync(input.TenantId, input.SourceUri, cancellationToken).ConfigureAwait(false);
        string source = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
        string tokenKey = DedupKeyBuilder.BuildTokenKey(input.TenantId, input.CaseId, input.IdempotencyToken!);
        IDatabase db = redis.GetDatabase();
        string digest = SourceRefreshProtocol.CreateIdentityDigest(input.SourceUri, input.ContentType, input.SourceRevisionNumber,
            input.Metadata, input.ContentBytes!);
        RedisValue[] values = await db.StringGetAsync([(RedisKey)source, (RedisKey)tokenKey, (RedisKey)RevisionKey(source), (RedisKey)TokenBindingKey(tokenKey)]).WaitAsync(cancellationToken).ConfigureAwait(false);
        string? current = values[0].HasValue ? values[0].ToString() : null;
        bool duplicateCurrent = values[1].HasValue && values[1] == values[0] && values[3].ToString() == digest;
        bool erasedPredecessor = current is not null && await db.KeyExistsAsync(SourcePublicationVisibility.Tombstone(input.TenantId, current))
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        if (!duplicateCurrent && values[2].HasValue && (!long.TryParse(values[2].ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out long revision)
            || input.SourceRevisionNumber < revision || (input.SourceRevisionNumber == revision && !erasedPredecessor)))
        {
            throw new InvalidOperationException("source-refresh-revision-stale");
        }
        if (values[3].HasValue && values[3].ToString() != digest)
        {
            throw new InvalidOperationException("source-refresh-identity-mismatch");
        }
        if (!duplicateCurrent && current != input.ExpectedMemoryUnitId)
        {
            throw new InvalidOperationException("source-refresh-predecessor-mismatch");
        }
        const string bindRetry = "local prior=redis.call('GET',KEYS[1]); if prior and prior~=ARGV[1] then return 0 end if not prior then redis.call('SET',KEYS[1],ARGV[1]) end return 1";
        if ((long)await TenantIngestionOwnership.WriteAsync(db, input.TenantId, bindRetry, [(RedisKey)TokenBindingKey(tokenKey)], [(RedisValue)digest])
            .WaitAsync(cancellationToken).ConfigureAwait(false) != 1)
        {
            throw new InvalidOperationException("source-refresh-identity-mismatch");
        }
        if (current is not null)
        {
            RedisValue[] scope = await db.HashGetAsync(IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, current),
                [(RedisValue)"caseId", (RedisValue)"sourceUri"]).WaitAsync(cancellationToken).ConfigureAwait(false);
            if (scope.Any(value => value.HasValue) && (scope[0].ToString() != input.CaseId || scope[1].ToString() != input.SourceUri))
            {
                throw new InvalidOperationException("source-refresh-predecessor-mismatch");
            }
            await inventory.RetainCanonicalAsync(input.TenantId, input.SourceUri, input.CaseId, current, cancellationToken).ConfigureAwait(false);
        }
        await SourcePublicationVisibility.EnsureSchemaAsync(db,
            [IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId), IndexSchemaDefinitions.GetSemanticActiveAliasName(input.TenantId),
                IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId), IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(input.TenantId),
                IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(input.TenantId)]).ConfigureAwait(false);
        if (current is not null) { await SemanticKeyDiscovery.RegisterLegacyKeysAsync(db, input.TenantId, current, cancellationToken).ConfigureAwait(false); }
        long currentRevision = values[2].HasValue ? long.Parse(values[2].ToString(), CultureInfo.InvariantCulture) : -1;
        await TenantIngestionOwnership.GraphWriteAsync(db, input.TenantId,
            () => GraphSourcePublication.SetCurrentAsync(falkorDb, input.TenantId, input.CaseId, input.SourceUri, current ?? string.Empty, currentRevision), cancellationToken).ConfigureAwait(false);
        await TenantIngestionOwnership.WriteAsync(db, input.TenantId,
            "redis.call('SET',KEYS[1],'1'); return redis.call('SET',KEYS[2],'1')",
            [(RedisKey)ManagedSourceKey(input.TenantId, input.SourceUri), (RedisKey)TenantManagedKey(input.TenantId)], [], cancellationToken).ConfigureAwait(false);
        return digest;
    }

    /// <summary>Atomically publishes the exact predecessor and retry identity after all content stores have completed.</summary>
    internal async Task<bool> PublishAsync(IngestionInput input, string candidateId, CancellationToken cancellationToken)
    {
        string source = DedupKeyBuilder.BuildKey(input.TenantId, input.CaseId, input.SourceUri);
        string tokenKey = DedupKeyBuilder.BuildTokenKey(input.TenantId, input.CaseId, input.IdempotencyToken!);
        RedisResult result = await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), input.TenantId, Publish,
            [(RedisKey)source, (RedisKey)tokenKey, (RedisKey)RevisionKey(source), (RedisKey)TokenBindingKey(tokenKey),
                (RedisKey)SourcePublicationVisibility.Keys(input.TenantId, candidateId),
                (RedisKey)(input.ExpectedMemoryUnitId is { } predecessor ? SourcePublicationVisibility.Keys(input.TenantId, predecessor) : "__unused-prior-keys"),
                (RedisKey)SourcePublicationVisibility.Tombstone(input.TenantId, candidateId),
                (RedisKey)(input.ExpectedMemoryUnitId is { } prior ? SourcePublicationVisibility.Tombstone(input.TenantId, prior) : "__unused-prior-tombstone"),
                (RedisKey)GraphPublicationReadGate.JournalKey(input.TenantId), (RedisKey)GraphPublicationReadGate.EpochKey(input.TenantId)],
            [(RedisValue)(input.ExpectedMemoryUnitId ?? string.Empty), (RedisValue)candidateId,
                (RedisValue)input.SourceRevisionNumber.ToString(CultureInfo.InvariantCulture), (RedisValue)input.RefreshIdentityDigest!,
                (RedisValue)JsonSerializer.Serialize(new GraphPublicationRepair(input.TenantId, input.CaseId, input.SourceUri, candidateId, input.SourceRevisionNumber))])
            .WaitAsync(cancellationToken).ConfigureAwait(false);
        bool published = (long)result == 1;
        if (published)
        {
            await GraphPublicationReadGate.ReconcileAsync(redis, falkorDb, input.TenantId, cancellationToken).ConfigureAwait(false);
        }
        return published;
    }

    /// <summary>Filters every search axis against the same current pointer, hiding staged and superseded candidates.</summary>
    internal async Task<IReadOnlyList<ScoredResult>> FilterAsync(string tenantId, IReadOnlyList<ScoredResult> results, CancellationToken cancellationToken)
    {
        if (results.Count == 0 || !await HasManagedSourcesAsync(tenantId, cancellationToken).ConfigureAwait(false)) { return results; }
        if (results.Count > 1000) { throw new InvalidOperationException("source-refresh-search-bound"); }
        RedisKey[] keys = [.. results.SelectMany(result => new RedisKey[]
        {
            ManagedSourceKey(tenantId, result.SourceUri),
            DedupKeyBuilder.BuildKey(tenantId, result.CaseId ?? "__unresolved-case", result.SourceUri),
        })];
        RedisValue[] values = await redis.GetDatabase().StringGetAsync(keys).WaitAsync(cancellationToken).ConfigureAwait(false);
        return results.Where((result, index) => !values[index * 2].HasValue || values[index * 2 + 1].ToString() == result.MemoryUnitId).ToArray();
    }

    /// <summary>Filters the fused result type while retaining its original scores and contribution metadata.</summary>
    internal async Task<IReadOnlyList<FusedScoredResult>> FilterAsync(string tenantId, IReadOnlyList<FusedScoredResult> results, CancellationToken cancellationToken)
    {
        IReadOnlyList<ScoredResult> visible = await FilterAsync(tenantId, results.Select(result => new ScoredResult
        {
            MemoryUnitId = result.MemoryUnitId, SourceUri = result.SourceUri, CaseId = result.CaseId,
            SourceType = result.SourceType, Score = 0, ContentSnippet = string.Empty,
        }).ToArray(), cancellationToken).ConfigureAwait(false);
        HashSet<string> ids = visible.Select(result => result.MemoryUnitId).ToHashSet(StringComparer.Ordinal);
        return results.Where(result => ids.Contains(result.MemoryUnitId)).ToArray();
    }

    /// <summary>Determines whether the endpoint needs publication-aware candidate retrieval.</summary>
    internal async Task<bool> HasManagedSourcesAsync(string tenantId, CancellationToken cancellationToken)
        => (await redis.GetDatabase().StringGetAsync(TenantManagedKey(tenantId)).WaitAsync(cancellationToken).ConfigureAwait(false)).HasValue;

    /// <summary>Checks native-filtered pages against the canonical pointer without changing ordinary paging/counts.</summary>
    internal async Task<SearchResult> FilterResultAsync(string tenantId, SearchResult result, int size, int offset, int? budget, CancellationToken cancellationToken)
    {
        IReadOnlyList<ScoredResult> visible = await FilterAsync(tenantId, result.Results, cancellationToken).ConfigureAwait(false);
        if (visible.Count != result.Results.Count) { throw new InvalidOperationException("source-refresh-search-window-incomplete"); }
        return result;
    }

    internal async Task<HybridSearchResult> FilterResultAsync(string tenantId, HybridSearchResult result, int size, int offset, int? budget, CancellationToken cancellationToken)
    {
        IReadOnlyList<FusedScoredResult> visible = await FilterAsync(tenantId, result.Results, cancellationToken).ConfigureAwait(false);
        if (visible.Count != result.Results.Count) { throw new InvalidOperationException("source-refresh-search-window-incomplete"); }
        return result;
    }

    private static string TenantManagedKey(string tenantId) => "refresh-managed-tenant:" + tenantId;

    internal async Task CommitOrdinaryAsync(string tenantId, string caseId, string sourceUri, string candidate, CancellationToken cancellationToken)
    {
        const string commit = "if redis.call('GET',KEYS[1]) ~= ARGV[1] or redis.call('EXISTS',KEYS[3]) == 1 then return 0 end local keys=redis.call('SMEMBERS',KEYS[2]); if #keys > 4096 then return 0 end for _,key in ipairs(keys) do if redis.call('EXISTS',key) == 1 then redis.call('HSET',key,'publicationVisible','1') end end return 1";
        await TenantIngestionOwnership.WriteAsync(redis.GetDatabase(), tenantId, commit,
            [(RedisKey)DedupKeyBuilder.BuildKey(tenantId, caseId, sourceUri),
                (RedisKey)SourcePublicationVisibility.Keys(tenantId, candidate), (RedisKey)SourcePublicationVisibility.Tombstone(tenantId, candidate)],
            [(RedisValue)candidate]).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string RevisionKey(string sourceKey) => "refresh-version:" + sourceKey;
    private static string TokenBindingKey(string tokenKey) => "refresh-token-binding:" + tokenKey;

    internal static string ManagedSourceKey(string tenantId, string sourceUri)
        => "refresh-managed:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(new[] { tenantId, sourceUri }))));
}
