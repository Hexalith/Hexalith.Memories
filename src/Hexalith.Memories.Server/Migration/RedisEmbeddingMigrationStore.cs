// <copyright file="RedisEmbeddingMigrationStore.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Tenants;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>Redis, DAPR actor, and tenant registry implementation of <see cref="IEmbeddingMigrationStore"/>.</summary>
/// <param name="redis">The Redis connection multiplexer.</param>
/// <param name="tenantRegistry">The committed tenant registry surface.</param>
/// <param name="actorProxyFactory">The DAPR actor proxy factory.</param>
public sealed partial class RedisEmbeddingMigrationStore(
    IConnectionMultiplexer redis,
    TenantRegistryService tenantRegistry,
    IActorProxyFactory actorProxyFactory) : IEmbeddingMigrationStore
{
    private const int ScanPageSize = 1000;
    private static readonly TimeSpan FailureListRetention = TimeSpan.FromDays(30);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListTenantIdsAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tenantRegistry);
        IReadOnlyList<TenantInfo> tenants = await tenantRegistry.ListTenantsAsync(ct).ConfigureAwait(false);
        return tenants.Select(t => t.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Order(StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc/>
    public async Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ITenantConfigurationActor actor = CreateTenantConfigActor(tenantId);
        return await actor.GetEmbeddingConfigAsync().WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SetEmbeddingConfigAsync(string tenantId, TenantEmbeddingConfig config, bool forceReindex, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);
        ITenantConfigurationActor actor = CreateTenantConfigActor(tenantId);
        await actor.SetEmbeddingConfigAsync(config, forceReindex).WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingMigrationTenantCounts> GetCountsAsync(
        string tenantId,
        TenantEmbeddingConfig targetConfig,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        IDatabase db = redis.GetDatabase();
        string syntacticPrefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId);
        string rawPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId);
        string nlPrefix = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId);
        string legacyNlPrefix = IndexSchemaDefinitions.GetLegacyNaturalLanguageSemanticKeyPrefix(tenantId);

        long syntactic = 0;
        await foreach (RedisKey _ in ScanKeysAsync(syntacticPrefix, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            syntactic++;
        }

        long raw = 0;
        long rawStale = 0;
        await foreach (RedisKey key in ScanKeysAsync(rawPrefix, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            if (!IndexSchemaDefinitions.TryParseSemanticMemoryUnitId(tenantId, key, out _))
            {
                continue;
            }

            raw++;
            SemanticMigrationState? state = await ReadSemanticStateAsync(db, key, ct).ConfigureAwait(false);
            if (!IsTargetState(state, targetConfig))
            {
                rawStale++;
            }
        }

        Dictionary<string, RedisKey> nlKeysByUnitId = new(StringComparer.Ordinal);
        await foreach (RedisKey key in ScanKeysAsync(legacyNlPrefix, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            AddScannedKey(
                nlKeysByUnitId,
                key,
                scannedKey => IndexSchemaDefinitions.TryParseLegacyNaturalLanguageSemanticMemoryUnitId(tenantId, scannedKey, out string id) ? id : null);
        }

        await foreach (RedisKey key in ScanKeysAsync(nlPrefix, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            AddScannedKey(
                nlKeysByUnitId,
                key,
                scannedKey => IndexSchemaDefinitions.TryParseNaturalLanguageSemanticMemoryUnitId(tenantId, scannedKey, out string id) ? id : null);
        }

        long nl = nlKeysByUnitId.Count;
        long nlStale = 0;
        foreach (RedisKey key in nlKeysByUnitId.Values)
        {
            ct.ThrowIfCancellationRequested();
            SemanticMigrationState? state = await ReadSemanticStateAsync(db, key, ct).ConfigureAwait(false);
            if (!IsTargetState(state, targetConfig))
            {
                nlStale++;
            }
        }

        return new EmbeddingMigrationTenantCounts(syntactic, raw, nl, rawStale, nlStale);
    }

    private static void AddScannedKey(Dictionary<string, RedisKey> keysByUnitId, RedisKey key, Func<RedisKey, string?> parseMemoryUnitId)
    {
        string? memoryUnitId = parseMemoryUnitId(key);
        if (string.IsNullOrEmpty(memoryUnitId))
        {
            return;
        }

        keysByUnitId[memoryUnitId] = key;
    }

    /// <inheritdoc/>
    public Task<EmbeddingMigrationIndexInfo> GetIndexInfoAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();
        IDatabase db = redis.GetDatabase();
        int? rawDimensions = TryGetIndexDimensions(db, IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId))
            ?? TryGetIndexDimensions(db, IndexSchemaDefinitions.GetSemanticIndexName(tenantId));
        int? nlDimensions = TryGetIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId))
            ?? TryGetIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId));
        return Task.FromResult(new EmbeddingMigrationIndexInfo(rawDimensions, nlDimensions));
    }

    /// <inheritdoc/>
    public async Task PrepareStagingSemanticIndexesAsync(string tenantId, TenantEmbeddingConfig targetConfig, string version, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ct.ThrowIfCancellationRequested();
        IDatabase db = redis.GetDatabase();
        string rawIndex = IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, version);
        string nlIndex = IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, version);

        await RedisNaturalLanguageNamespaceMigrator
            .MigrateAsync(db, GetAnyServer(), tenantId, ct)
            .ConfigureAwait(false);

        var ft = db.FT();
        bool rawCreated = false;
        try
        {
            ft.Create(
                rawIndex,
                IndexSchemaDefinitions.CreateSemanticStagingParams(tenantId, version),
                IndexSchemaDefinitions.CreateSemanticSchema(targetConfig.Dimensions));
            rawCreated = true;

            ct.ThrowIfCancellationRequested();

            ft.Create(
                nlIndex,
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticStagingParams(tenantId, version),
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(targetConfig.Dimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
        {
            VerifyStagingIndexes(db, tenantId, targetConfig, version);
        }
        catch
        {
            if (rawCreated)
            {
                DropIndexIfExists(db, rawIndex);
            }

            throw;
        }

    }

    /// <inheritdoc/>
    public async Task<EmbeddingMigrationLease> StartMigrationMarkerAsync(
        string tenantId,
        TenantEmbeddingConfig currentConfig,
        TenantEmbeddingConfig targetConfig,
        string ownerId,
        TimeSpan lockTtl,
        bool resume,
        bool recoverStaleLock,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(currentConfig);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        // F21: refuse to persist a marker with non-positive dimensions; durable storage of a zero/negative
        // dimensions value would block every subsequent semantic write for the tenant.
        if (targetConfig.Dimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetConfig),
                targetConfig.Dimensions,
                "Target embedding dimensions must be positive.");
        }

        IDatabase db = redis.GetDatabase();
        string key = GetMarkerKey(tenantId, targetConfig);
        string activeMarkerKey = EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId);
        string lockKey = GetLockKey(tenantId);

        if (resume)
        {
            Dictionary<string, string> existing = await ReadHashAsync(db, key, ct).ConfigureAwait(false);
            if (existing.Count == 0)
            {
                throw new InvalidOperationException(
                    $"--resume specified but no prior migration marker exists for tenant '{tenantId}' targeting '{targetConfig.Provider}/{targetConfig.Model}'.");
            }

            EnsureTargetMatches(tenantId, existing, targetConfig);
            string existingVersion = RequireMarkerField(existing, "migrationVersion", tenantId);
            await AcquireOrRenewLockAsync(db, lockKey, ownerId, lockTtl, recoverStaleLock, ct).ConfigureAwait(false);
            await WriteMarkerHashesAtomicallyAsync(
                db,
                key,
                activeMarkerKey,
                BuildResumeMarkerEntries(ownerId, existingVersion, lockTtl),
                ct).ConfigureAwait(false);
            return new EmbeddingMigrationLease(ownerId, existingVersion);
        }

        await AcquireOrRenewLockAsync(db, lockKey, ownerId, lockTtl, recoverStaleLock, ct).ConfigureAwait(false);
        string version = ownerId;
        HashEntry[] entries = BuildMarkerEntries(tenantId, currentConfig, targetConfig, ownerId, version, MigrationMarkerStatus.Started);

        // F3: the per-target key and the active-marker key must be written atomically. A non-atomic two-key write
        // can leave the active marker missing while the per-target marker is `started`, silently disabling the
        // runtime guard.
        await WriteMarkerHashesAtomicallyAsync(
            db,
            key,
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            entries,
            ct).ConfigureAwait(false);
        return new EmbeddingMigrationLease(ownerId, version);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingMigrationMarker?> GetActiveMigrationMarkerAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        IDatabase db = redis.GetDatabase();
        return await EmbeddingMigrationMarkerReader.ReadActiveMarkerAsync(db, tenantId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task HeartbeatMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, EmbeddingMigrationLease lease, TimeSpan lockTtl, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(lease);
        IDatabase db = redis.GetDatabase();
        await EnsureOwnedLockAsync(db, GetLockKey(tenantId), lease.OwnerId, lockTtl, ct).ConfigureAwait(false);
        string heartbeat = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        HashEntry[] entries =
        [
            new("ownerId", lease.OwnerId),
            new("migrationVersion", lease.Version),
            new("lastHeartbeatAt", heartbeat),
            new("updatedAt", heartbeat),
            new("expiresAt", DateTimeOffset.UtcNow.Add(lockTtl).ToString("O", CultureInfo.InvariantCulture)),
        ];
        await WriteMarkerHashesAtomicallyAsync(
            db,
            GetMarkerKey(tenantId, targetConfig),
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            entries,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task VerifyStagingSemanticIndexesAsync(string tenantId, TenantEmbeddingConfig targetConfig, string version, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ct.ThrowIfCancellationRequested();
        VerifyStagingIndexes(redis.GetDatabase(), tenantId, targetConfig, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task CutoverStagingSemanticIndexesAsync(
        string tenantId,
        TenantEmbeddingConfig previousConfig,
        TenantEmbeddingConfig targetConfig,
        EmbeddingMigrationLease lease,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(previousConfig);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(lease);
        IDatabase db = redis.GetDatabase();
        await EnsureOwnedLockAsync(db, GetLockKey(tenantId), lease.OwnerId, TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        VerifyStagingIndexes(db, tenantId, targetConfig, lease.Version);

        string rawActiveAlias = IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId);
        string nlActiveAlias = IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId);
        EnsureActiveAlias(db, rawActiveAlias, IndexSchemaDefinitions.GetSemanticIndexName(tenantId));
        EnsureActiveAlias(db, nlActiveAlias, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId));

        ITransaction transaction = db.CreateTransaction();
        _ = transaction.ExecuteAsync("FT.ALIASUPDATE", rawActiveAlias, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, lease.Version));
        _ = transaction.ExecuteAsync("FT.ALIASUPDATE", nlActiveAlias, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, lease.Version));
        _ = transaction.HashSetAsync(
            GetMarkerKey(tenantId, targetConfig),
            [
                new HashEntry("status", MigrationMarkerStatus.Cutover),
                new HashEntry("previousProvider", previousConfig.Provider),
                new HashEntry("previousModel", previousConfig.Model),
                new HashEntry("previousDimensions", previousConfig.Dimensions),
                new HashEntry("cutoverAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ]);
        _ = transaction.HashSetAsync(
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            [
                new HashEntry("status", MigrationMarkerStatus.Cutover),
                new HashEntry("previousProvider", previousConfig.Provider),
                new HashEntry("previousModel", previousConfig.Model),
                new HashEntry("previousDimensions", previousConfig.Dimensions),
                new HashEntry("cutoverAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ]);
        bool committed = await transaction.ExecuteAsync().WaitAsync(ct).ConfigureAwait(false);
        if (!committed)
        {
            throw new InvalidOperationException("Failed to atomically update blue/green embedding migration aliases.");
        }

        await SetEmbeddingConfigAsync(tenantId, targetConfig, forceReindex: false, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, EmbeddingMigrationLease lease, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(lease);
        IDatabase db = redis.GetDatabase();
        await EnsureOwnedLockAsync(db, GetLockKey(tenantId), lease.OwnerId, TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        string key = GetMarkerKey(tenantId, targetConfig);
        HashEntry[] entries =
        [
            new("status", MigrationMarkerStatus.Completed),
            new("ownerId", lease.OwnerId),
            new("migrationVersion", lease.Version),
            new("completedAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
        ];

        // F3: atomic two-key write so completion cannot leave the active marker in an inconsistent state
        // relative to the per-target marker.
        await WriteMarkerHashesAtomicallyAsync(
            db,
            key,
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            entries,
            ct).ConfigureAwait(false);
        await db.KeyDeleteAsync(GetLockKey(tenantId)).WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RollbackMigrationAsync(string tenantId, TenantEmbeddingConfig targetConfig, EmbeddingMigrationLease lease, CancellationToken ct)
        => await RollbackMigrationAsync(tenantId, targetConfig, lease, releaseLock: true, ct).ConfigureAwait(false);

    private async Task RollbackMigrationAsync(
        string tenantId,
        TenantEmbeddingConfig targetConfig,
        EmbeddingMigrationLease lease,
        bool releaseLock,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(lease);
        IDatabase db = redis.GetDatabase();
        await EnsureOwnedLockAsync(db, GetLockKey(tenantId), lease.OwnerId, TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        Dictionary<string, string> marker = await ReadHashAsync(db, GetMarkerKey(tenantId, targetConfig), ct).ConfigureAwait(false);
        TenantEmbeddingConfig previous = ReadPreviousConfig(tenantId, marker);
        string rawPrevious = RequireMarkerField(marker, "previousRawTarget", tenantId);
        string nlPrevious = RequireMarkerField(marker, "previousNaturalLanguageTarget", tenantId);

        ITransaction transaction = db.CreateTransaction();
        _ = transaction.ExecuteAsync("FT.ALIASUPDATE", IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId), rawPrevious);
        _ = transaction.ExecuteAsync("FT.ALIASUPDATE", IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId), nlPrevious);
        _ = transaction.HashSetAsync(
            GetMarkerKey(tenantId, targetConfig),
            [
                new HashEntry("status", MigrationMarkerStatus.RolledBack),
                new HashEntry("rolledBackAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ]);
        _ = transaction.HashSetAsync(
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            [
                new HashEntry("status", MigrationMarkerStatus.RolledBack),
                new HashEntry("rolledBackAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ]);
        bool committed = await transaction.ExecuteAsync().WaitAsync(ct).ConfigureAwait(false);
        if (!committed)
        {
            throw new InvalidOperationException("Failed to atomically roll back blue/green embedding migration aliases.");
        }

        await SetEmbeddingConfigAsync(tenantId, previous, forceReindex: false, ct).ConfigureAwait(false);
        if (releaseLock)
        {
            await db.KeyDeleteAsync(GetLockKey(tenantId)).WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task AbortMigrationAsync(string tenantId, TenantEmbeddingConfig targetConfig, EmbeddingMigrationLease lease, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(lease);
        IDatabase db = redis.GetDatabase();
        await EnsureOwnedLockAsync(db, GetLockKey(tenantId), lease.OwnerId, TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        Dictionary<string, string> marker = await ReadHashAsync(db, GetMarkerKey(tenantId, targetConfig), ct).ConfigureAwait(false);
        if (marker.TryGetValue("status", out string? status)
            && string.Equals(status, MigrationMarkerStatus.Cutover, StringComparison.OrdinalIgnoreCase))
        {
            await RollbackMigrationAsync(tenantId, targetConfig, lease, releaseLock: false, ct).ConfigureAwait(false);
        }

        await DropStagingIndexesAsync(db, tenantId, lease.Version, ct).ConfigureAwait(false);
        await WriteMarkerHashesAtomicallyAsync(
            db,
            GetMarkerKey(tenantId, targetConfig),
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            [
                new HashEntry("status", MigrationMarkerStatus.Aborted),
                new HashEntry("abortedAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
            ],
            ct).ConfigureAwait(false);
        await db.KeyDeleteAsync(GetLockKey(tenantId)).WaitAsync(ct).ConfigureAwait(false);
    }

    private static async Task WriteMarkerHashesAtomicallyAsync(
        IDatabase db,
        RedisKey perTargetKey,
        RedisKey activeMarkerKey,
        HashEntry[] entries,
        CancellationToken ct)
    {
        ITransaction tran = db.CreateTransaction();
        _ = tran.HashSetAsync(perTargetKey, entries);
        _ = tran.HashSetAsync(activeMarkerKey, entries);
        bool committed = await tran.ExecuteAsync().WaitAsync(ct).ConfigureAwait(false);
        if (!committed)
        {
            throw new InvalidOperationException(
                "Failed to atomically write embedding migration marker hashes; transaction was discarded by Redis.");
        }
    }

    /// <inheritdoc/>
    public async Task RecordFailureAsync(EmbeddingMigrationUnitFailure failure, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(failure);
        IDatabase db = redis.GetDatabase();
        string listKey = $"{failure.TenantId}:embedding-migration:failures";
        string json = JsonSerializer.Serialize(failure, MigrationJsonOptions);
        await db.ListRightPushAsync(listKey, json).WaitAsync(ct).ConfigureAwait(false);
        await db.KeyExpireAsync(listKey, FailureListRetention).WaitAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<SyntacticMigrationUnit> EnumerateSyntacticUnitsAsync(
        string tenantId,
        int pageSize,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        IDatabase db = redis.GetDatabase();
        string prefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId);
        await foreach (RedisKey key in ScanKeysAsync(prefix, ct, pageSize).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            if (!IndexSchemaDefinitions.TryParseSyntacticMemoryUnitId(tenantId, key, out string memoryUnitId))
            {
                continue;
            }

            RedisValue[] values = await db.HashGetAsync(
                    key,
                    ["content", "caseId", "cloudeventSubject"])
                .WaitAsync(ct)
                .ConfigureAwait(false);

            yield return new SyntacticMigrationUnit(
                memoryUnitId,
                NullableString(values[0]),
                NullableString(values[1]),
                NullableString(values[2]));
        }
    }

    /// <inheritdoc/>
    public async Task<SemanticMigrationState?> GetRawSemanticStateAsync(string tenantId, string memoryUnitId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        IDatabase db = redis.GetDatabase();
        RedisKey key = await GetSemanticReadKeyAsync(db, tenantId, memoryUnitId, ct).ConfigureAwait(false);
        if (!await db.KeyExistsAsync(key).WaitAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        return await ReadSemanticStateAsync(db, key, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<NaturalLanguageMigrationUnit?> GetNaturalLanguageSemanticUnitAsync(
        string tenantId,
        string memoryUnitId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryUnitId);
        IDatabase db = redis.GetDatabase();
        RedisKey key = await GetNaturalLanguageSemanticReadKeyAsync(db, tenantId, memoryUnitId, ct).ConfigureAwait(false);
        if (!await db.KeyExistsAsync(key).WaitAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        RedisValue[] values = await db.HashGetAsync(
                key,
                [
                    "caseId",
                    "naturalLanguageDescription",
                    "descriptionOrigin",
                    "descriptionConfidence",
                    "descriptionConfidenceSource",
                    "embeddingProvider",
                    "embeddingModel",
                    "embeddingDimensions",
                ])
            .WaitAsync(ct)
            .ConfigureAwait(false);

        SemanticMigrationState state = new(NullableString(values[5]), NullableString(values[6]), TryParseInt(values[7]));
        return new NaturalLanguageMigrationUnit(
            memoryUnitId,
            NullableString(values[0]),
            NullableString(values[1]),
            NullableString(values[2]),
            NullableString(values[3]),
            NullableString(values[4]),
            state);
    }

    /// <inheritdoc/>
    public async Task WriteRawSemanticAsync(
        string tenantId,
        TenantEmbeddingConfig targetConfig,
        RawSemanticMigrationWrite write,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(write);
        IDatabase db = redis.GetDatabase();
        byte[] vectorBytes = ConvertVectorToLittleEndianBytes(write.Embedding);
        List<HashEntry> entries =
        [
            new("embedding", vectorBytes),
            new("memoryUnitId", write.MemoryUnitId),
            new("caseId", write.CaseId),
            new("embeddingProvider", targetConfig.Provider),
            new("embeddingModel", targetConfig.Model),
            new("embeddingDimensions", targetConfig.Dimensions),
        ];
        if (!string.IsNullOrWhiteSpace(write.CloudEventSubject))
        {
            entries.Add(new HashEntry("cloudeventSubject", write.CloudEventSubject));
        }

        string version = await GetActiveMigrationVersionAsync(db, tenantId, ct).ConfigureAwait(false);
        RedisKey key = IndexSchemaDefinitions.BuildSemanticStagingKey(tenantId, version, write.MemoryUnitId);
        ITransaction transaction = db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        _ = transaction.HashSetAsync(key, [.. entries]);
        if (!await transaction.ExecuteAsync().WaitAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Failed to atomically replace raw semantic hash for memory unit '{write.MemoryUnitId}'.");
        }
    }

    /// <inheritdoc/>
    public async Task WriteNaturalLanguageSemanticAsync(
        string tenantId,
        TenantEmbeddingConfig targetConfig,
        NaturalLanguageSemanticMigrationWrite write,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        ArgumentNullException.ThrowIfNull(write);
        IDatabase db = redis.GetDatabase();
        byte[] vectorBytes = ConvertVectorToLittleEndianBytes(write.Embedding);
        List<HashEntry> entries =
        [
            new("embedding", vectorBytes),
            new("memoryUnitId", write.MemoryUnitId),
            new("caseId", write.CaseId),
            new("naturalLanguageDescription", write.NaturalLanguageDescription),
            new("embeddingProvider", targetConfig.Provider),
            new("embeddingModel", targetConfig.Model),
            new("embeddingDimensions", targetConfig.Dimensions),
        ];
        if (!string.IsNullOrWhiteSpace(write.DescriptionOrigin))
        {
            entries.Add(new HashEntry("descriptionOrigin", write.DescriptionOrigin));
        }

        if (!string.IsNullOrWhiteSpace(write.DescriptionConfidence))
        {
            entries.Add(new HashEntry("descriptionConfidence", write.DescriptionConfidence));
        }

        if (!string.IsNullOrWhiteSpace(write.DescriptionConfidenceSource))
        {
            entries.Add(new HashEntry("descriptionConfidenceSource", write.DescriptionConfidenceSource));
        }

        string version = await GetActiveMigrationVersionAsync(db, tenantId, ct).ConfigureAwait(false);
        RedisKey key = IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(tenantId, version, write.MemoryUnitId);
        ITransaction transaction = db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(key);
        _ = transaction.HashSetAsync(key, [.. entries]);
        if (!await transaction.ExecuteAsync().WaitAsync(ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Failed to atomically replace NL semantic hash for memory unit '{write.MemoryUnitId}'.");
        }
    }

    /// <inheritdoc/>
    public Task<bool> HasRetainedPreviousVersionIndexesAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();
        IDatabase db = redis.GetDatabase();
        bool rawExists = IndexExists(db, IndexSchemaDefinitions.GetSemanticPreviousAliasName(tenantId, "current"));
        bool nlExists = IndexExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticPreviousAliasName(tenantId, "current"));
        return Task.FromResult(rawExists && nlExists);
    }

    private static byte[] ConvertVectorToLittleEndianBytes(float[] vector)
    {
        if (vector.Length == 0)
        {
            return [];
        }

        byte[] bytes = new byte[vector.Length * sizeof(float)];
        for (int i = 0; i < vector.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), vector[i]);
        }

        return bytes;
    }

    private static HashEntry[] BuildMarkerEntries(
        string tenantId,
        TenantEmbeddingConfig currentConfig,
        TenantEmbeddingConfig targetConfig,
        string ownerId,
        string version,
        string status)
    {
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return
        [
            new("tenantId", tenantId),
            new("targetProvider", targetConfig.Provider),
            new("targetModel", targetConfig.Model),
            new("targetDimensions", targetConfig.Dimensions),
            new("previousProvider", currentConfig.Provider),
            new("previousModel", currentConfig.Model),
            new("previousDimensions", currentConfig.Dimensions),
            new("previousRateLimitPerMinute", currentConfig.RateLimitPerMinute),
            new("previousApiSecretKeyName", currentConfig.ApiSecretKeyName),
            new("previousReindexRequired", currentConfig.ReindexRequired.ToString(CultureInfo.InvariantCulture)),
            new("previousBaseUrl", currentConfig.BaseUrl ?? string.Empty),
            new("previousAuthMode", currentConfig.AuthMode),
            new("previousOidcTokenEndpoint", currentConfig.OidcTokenEndpoint ?? string.Empty),
            new("previousOidcClientId", currentConfig.OidcClientId ?? string.Empty),
            new("previousOidcScope", currentConfig.OidcScope ?? string.Empty),
            new("ownerId", ownerId),
            new("migrationVersion", version),
            new("status", status),
            new("createdAt", now),
            new("updatedAt", now),
            new("lastHeartbeatAt", now),
            new("activeRawTarget", IndexSchemaDefinitions.GetSemanticActiveAliasName(tenantId)),
            new("activeNaturalLanguageTarget", IndexSchemaDefinitions.GetNaturalLanguageSemanticActiveAliasName(tenantId)),
            new("stagingRawTarget", IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, version)),
            new("stagingNaturalLanguageTarget", IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, version)),
            new("previousRawTarget", IndexSchemaDefinitions.GetSemanticIndexName(tenantId)),
            new("previousNaturalLanguageTarget", IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId)),
        ];
    }

    private static HashEntry[] BuildResumeMarkerEntries(string ownerId, string version, TimeSpan lockTtl)
    {
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return
        [
            new("ownerId", ownerId),
            new("migrationVersion", version),
            new("status", MigrationMarkerStatus.Resumed),
            new("updatedAt", now),
            new("lastHeartbeatAt", now),
            new("expiresAt", DateTimeOffset.UtcNow.Add(lockTtl).ToString("O", CultureInfo.InvariantCulture)),
        ];
    }

    private static async Task AcquireOrRenewLockAsync(
        IDatabase db,
        RedisKey lockKey,
        string ownerId,
        TimeSpan lockTtl,
        bool recoverStaleLock,
        CancellationToken ct)
    {
        RedisValue currentOwner = await db.StringGetAsync(lockKey).WaitAsync(ct).ConfigureAwait(false);
        if (!currentOwner.IsNullOrEmpty && string.Equals(currentOwner.ToString(), ownerId, StringComparison.Ordinal))
        {
            await db.KeyExpireAsync(lockKey, lockTtl).WaitAsync(ct).ConfigureAwait(false);
            return;
        }

        if (!currentOwner.IsNullOrEmpty && !recoverStaleLock)
        {
            throw new InvalidOperationException("Embedding migration lock is already owned by another active run.");
        }

        bool acquired = await db.StringSetAsync(lockKey, ownerId, lockTtl, When.NotExists).WaitAsync(ct).ConfigureAwait(false);
        if (!acquired)
        {
            RedisValue ownerAfterAttempt = await db.StringGetAsync(lockKey).WaitAsync(ct).ConfigureAwait(false);
            if (string.Equals(ownerAfterAttempt.ToString(), ownerId, StringComparison.Ordinal))
            {
                await db.KeyExpireAsync(lockKey, lockTtl).WaitAsync(ct).ConfigureAwait(false);
                return;
            }

            throw new InvalidOperationException("Embedding migration lock could not be acquired; another owner holds the tenant lock.");
        }
    }

    private static async Task EnsureOwnedLockAsync(
        IDatabase db,
        RedisKey lockKey,
        string ownerId,
        TimeSpan lockTtl,
        CancellationToken ct)
    {
        RedisValue currentOwner = await db.StringGetAsync(lockKey).WaitAsync(ct).ConfigureAwait(false);
        if (currentOwner.IsNullOrEmpty || !string.Equals(currentOwner.ToString(), ownerId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Embedding migration lock heartbeat failed because the run no longer owns the tenant lock.");
        }

        await db.KeyExpireAsync(lockKey, lockTtl).WaitAsync(ct).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, string>> ReadHashAsync(IDatabase db, RedisKey key, CancellationToken ct)
    {
        HashEntry[] entries = await db.HashGetAllAsync(key, CommandFlags.DemandMaster).WaitAsync(ct).ConfigureAwait(false);
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private static string RequireMarkerField(IReadOnlyDictionary<string, string> marker, string fieldName, string tenantId)
    {
        if (!marker.TryGetValue(fieldName, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new EmbeddingMigrationMarkerCorruptException(tenantId, $"missing {fieldName} field");
        }

        return value;
    }

    private static void EnsureTargetMatches(string tenantId, IReadOnlyDictionary<string, string> marker, TenantEmbeddingConfig targetConfig)
    {
        string provider = RequireMarkerField(marker, "targetProvider", tenantId);
        string model = RequireMarkerField(marker, "targetModel", tenantId);
        string dimensionsText = RequireMarkerField(marker, "targetDimensions", tenantId);
        if (!int.TryParse(dimensionsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dimensions)
            || !string.Equals(provider, targetConfig.Provider, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(model, targetConfig.Model, StringComparison.OrdinalIgnoreCase)
            || dimensions != targetConfig.Dimensions)
        {
            throw new InvalidOperationException("Resume target does not match the active embedding migration marker.");
        }
    }

    private static TenantEmbeddingConfig ReadPreviousConfig(string tenantId, IReadOnlyDictionary<string, string> marker)
    {
        string provider = RequireMarkerField(marker, "previousProvider", tenantId);
        string model = RequireMarkerField(marker, "previousModel", tenantId);
        string dimensionsText = RequireMarkerField(marker, "previousDimensions", tenantId);
        string rateLimitText = RequireMarkerField(marker, "previousRateLimitPerMinute", tenantId);
        string secretName = RequireMarkerField(marker, "previousApiSecretKeyName", tenantId);
        if (!int.TryParse(dimensionsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dimensions))
        {
            throw new EmbeddingMigrationMarkerCorruptException(tenantId, "previousDimensions field is unparseable");
        }

        if (!int.TryParse(rateLimitText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rateLimit))
        {
            throw new EmbeddingMigrationMarkerCorruptException(tenantId, "previousRateLimitPerMinute field is unparseable");
        }

        bool reindexRequired = marker.TryGetValue("previousReindexRequired", out string? reindexText)
            && bool.TryParse(reindexText, out bool parsedReindex)
            && parsedReindex;
        string? baseUrl = OptionalMarkerField(marker, "previousBaseUrl");
        string authMode = OptionalMarkerField(marker, "previousAuthMode") ?? "api-key";
        return new TenantEmbeddingConfig(
            provider,
            model,
            dimensions,
            rateLimit,
            secretName,
            reindexRequired,
            baseUrl,
            authMode,
            OptionalMarkerField(marker, "previousOidcTokenEndpoint"),
            OptionalMarkerField(marker, "previousOidcClientId"),
            OptionalMarkerField(marker, "previousOidcScope"));
    }

    private static string? OptionalMarkerField(IReadOnlyDictionary<string, string> marker, string fieldName)
        => marker.TryGetValue(fieldName, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private async Task<string> GetActiveMigrationVersionAsync(IDatabase db, string tenantId, CancellationToken ct)
    {
        Dictionary<string, string> marker = await ReadHashAsync(db, EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId), ct).ConfigureAwait(false);
        return RequireMarkerField(marker, "migrationVersion", tenantId);
    }

    private async Task<RedisKey> GetSemanticReadKeyAsync(IDatabase db, string tenantId, string memoryUnitId, CancellationToken ct)
    {
        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader.ReadActiveMarkerAsync(db, tenantId, ct).ConfigureAwait(false);
        if (marker is null)
        {
            return IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId);
        }

        string version = await GetActiveMigrationVersionAsync(db, tenantId, ct).ConfigureAwait(false);
        RedisKey staging = IndexSchemaDefinitions.BuildSemanticStagingKey(tenantId, version, memoryUnitId);
        return await db.KeyExistsAsync(staging).WaitAsync(ct).ConfigureAwait(false)
            ? staging
            : IndexSchemaDefinitions.BuildSemanticKey(tenantId, memoryUnitId);
    }

    private async Task<RedisKey> GetNaturalLanguageSemanticReadKeyAsync(IDatabase db, string tenantId, string memoryUnitId, CancellationToken ct)
    {
        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader.ReadActiveMarkerAsync(db, tenantId, ct).ConfigureAwait(false);
        if (marker is null)
        {
            return IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
        }

        string version = await GetActiveMigrationVersionAsync(db, tenantId, ct).ConfigureAwait(false);
        RedisKey staging = IndexSchemaDefinitions.BuildNaturalLanguageSemanticStagingKey(tenantId, version, memoryUnitId);
        return await db.KeyExistsAsync(staging).WaitAsync(ct).ConfigureAwait(false)
            ? staging
            : IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, memoryUnitId);
    }

    private static void VerifyStagingIndexes(IDatabase db, string tenantId, TenantEmbeddingConfig targetConfig, string version)
    {
        RedisResult rawInfo = db.Execute("FT.INFO", IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, version));
        RedisResult nlInfo = db.Execute("FT.INFO", IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, version));
        IReadOnlyList<string> rawProblems = IndexSchemaDefinitions.DescribeVectorSchemaProblems(
            rawInfo,
            IndexSchemaDefinitions.GetSemanticStagingKeyPrefix(tenantId, version),
            IndexSchemaDefinitions.GetSemanticFieldIdentifiers(),
            targetConfig.Dimensions);
        IReadOnlyList<string> nlProblems = IndexSchemaDefinitions.DescribeVectorSchemaProblems(
            nlInfo,
            IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingKeyPrefix(tenantId, version),
            IndexSchemaDefinitions.GetNaturalLanguageSemanticFieldIdentifiers(),
            targetConfig.Dimensions);
        if (rawProblems.Count > 0 || nlProblems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Staging semantic indexes failed verification: raw=[{string.Join("; ", rawProblems)}], nl=[{string.Join("; ", nlProblems)}].");
        }
    }

    private static void EnsureActiveAlias(IDatabase db, string aliasName, string physicalIndexName)
    {
        try
        {
            _ = db.Execute("FT.ALIASADD", aliasName, physicalIndexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("alias already exists", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private async Task DropStagingIndexesAsync(IDatabase db, string tenantId, string version, CancellationToken ct)
    {
        DropIndexIfExists(db, IndexSchemaDefinitions.GetSemanticStagingIndexName(tenantId, version));
        ct.ThrowIfCancellationRequested();
        DropIndexIfExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingIndexName(tenantId, version));
        await DeleteKeysByPrefixAsync(db, IndexSchemaDefinitions.GetSemanticStagingKeyPrefix(tenantId, version), ct).ConfigureAwait(false);
        await DeleteKeysByPrefixAsync(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticStagingKeyPrefix(tenantId, version), ct).ConfigureAwait(false);
    }

    private async Task DeleteKeysByPrefixAsync(IDatabase db, string prefix, CancellationToken ct)
    {
        List<RedisKey> keys = [];
        await foreach (RedisKey key in ScanKeysAsync(prefix, ct).ConfigureAwait(false))
        {
            keys.Add(key);
        }

        if (keys.Count > 0)
        {
            await db.KeyDeleteAsync([.. keys]).WaitAsync(ct).ConfigureAwait(false);
        }
    }

    private ITenantConfigurationActor CreateTenantConfigActor(string tenantId)
        => actorProxyFactory.CreateActorProxy<ITenantConfigurationActor>(
            new ActorId(tenantId),
            nameof(TenantConfigurationActor));

    private async IAsyncEnumerable<RedisKey> ScanKeysAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken ct,
        int pageSize = ScanPageSize)
    {
        IServer server = GetAnyServer();
        await foreach (RedisKey key in server.KeysAsync(pattern: prefix + "*", pageSize: pageSize).WithCancellation(ct))
        {
            yield return key;
        }
    }

    private IServer GetAnyServer()
    {
        foreach (EndPoint endpoint in redis.GetEndPoints())
        {
            IServer server = redis.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        throw new InvalidOperationException("No connected Redis server is available for embedding vector migration.");
    }

    private static async Task<SemanticMigrationState?> ReadSemanticStateAsync(IDatabase db, RedisKey key, CancellationToken ct)
    {
        RedisValue[] values = await db.HashGetAsync(key, ["embeddingProvider", "embeddingModel", "embeddingDimensions"])
            .WaitAsync(ct)
            .ConfigureAwait(false);
        return new SemanticMigrationState(NullableString(values[0]), NullableString(values[1]), TryParseInt(values[2]));
    }

    private static bool IsTargetState(SemanticMigrationState? state, TenantEmbeddingConfig targetConfig)
        => state is not null
            && !string.IsNullOrEmpty(state.Provider)
            && !string.IsNullOrEmpty(state.Model)
            && state.Dimensions is not null
            && string.Equals(state.Provider, targetConfig.Provider, StringComparison.OrdinalIgnoreCase)
            && string.Equals(state.Model, targetConfig.Model, StringComparison.OrdinalIgnoreCase)
            && state.Dimensions == targetConfig.Dimensions;

    private static int? TryGetIndexDimensions(IDatabase db, string indexName)
    {
        try
        {
            RedisResult info = db.Execute("FT.INFO", indexName);
            return IndexSchemaDefinitions.TryGetVectorDimensions(info, "embedding", out int dimensions) ? dimensions : null;
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }

    private static bool IndexExists(IDatabase db, string indexName)
    {
        try
        {
            _ = db.Execute("FT.INFO", indexName);
            return true;
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    private static void DropIndexIfExists(IDatabase db, string indexName)
    {
        try
        {
            db.Execute("FT.DROPINDEX", indexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static int? TryParseInt(RedisValue value)
    {
        if (value.IsNull || value.IsNullOrEmpty)
        {
            return null;
        }

        return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : null;
    }

    private static string? NullableString(RedisValue value)
        => value.IsNull ? null : value.ToString();

    private static string GetMarkerKey(string tenantId, TenantEmbeddingConfig targetConfig)
    {
        string sanitizedProvider = SanitizeMarkerSegment(targetConfig.Provider).ToLowerInvariant();
        string sanitizedModel = SanitizeMarkerSegment(targetConfig.Model).ToLowerInvariant();
        return $"{tenantId}:embedding-migration:{sanitizedProvider}:{sanitizedModel}";
    }

    private static string GetLockKey(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"{tenantId}:embedding-migration:lock";
    }

    private static string SanitizeMarkerSegment(string value)
        => string.IsNullOrEmpty(value) ? "_" : MarkerSanitizationRegex().Replace(value, "_");

    [GeneratedRegex("[^A-Za-z0-9._-]")]
    private static partial Regex MarkerSanitizationRegex();

    private static readonly JsonSerializerOptions MigrationJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };
}
