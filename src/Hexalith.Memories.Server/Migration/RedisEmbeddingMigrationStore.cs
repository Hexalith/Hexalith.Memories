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
            if (key.ToString().StartsWith(nlPrefix, StringComparison.Ordinal))
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

        long nl = 0;
        long nlStale = 0;
        await foreach (RedisKey key in ScanKeysAsync(nlPrefix, ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            nl++;
            SemanticMigrationState? state = await ReadSemanticStateAsync(db, key, ct).ConfigureAwait(false);
            if (!IsTargetState(state, targetConfig))
            {
                nlStale++;
            }
        }

        return new EmbeddingMigrationTenantCounts(syntactic, raw, nl, rawStale, nlStale);
    }

    /// <inheritdoc/>
    public Task<EmbeddingMigrationIndexInfo> GetIndexInfoAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();
        IDatabase db = redis.GetDatabase();
        int? rawDimensions = TryGetIndexDimensions(db, IndexSchemaDefinitions.GetSemanticIndexName(tenantId));
        int? nlDimensions = TryGetIndexDimensions(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId));
        return Task.FromResult(new EmbeddingMigrationIndexInfo(rawDimensions, nlDimensions));
    }

    /// <inheritdoc/>
    public Task DropAndRecreateSemanticIndexesAsync(string tenantId, int dimensions, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ct.ThrowIfCancellationRequested();
        IDatabase db = redis.GetDatabase();
        string rawIndex = IndexSchemaDefinitions.GetSemanticIndexName(tenantId);
        string nlIndex = IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId);

        DropIndexIfExists(db, rawIndex);
        DropIndexIfExists(db, nlIndex);

        var ft = db.FT();
        bool rawCreated = false;
        try
        {
            ft.Create(
                rawIndex,
                IndexSchemaDefinitions.CreateSemanticParams(tenantId),
                IndexSchemaDefinitions.CreateSemanticSchema(dimensions));
            rawCreated = true;

            ct.ThrowIfCancellationRequested();

            ft.Create(
                nlIndex,
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticParams(tenantId),
                IndexSchemaDefinitions.CreateNaturalLanguageSemanticSchema(dimensions));
        }
        catch
        {
            if (rawCreated)
            {
                DropIndexIfExists(db, rawIndex);
            }

            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StartMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, bool resume, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);

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

        if (resume)
        {
            bool exists = await db.KeyExistsAsync(key).WaitAsync(ct).ConfigureAwait(false);
            if (!exists)
            {
                throw new InvalidOperationException(
                    $"--resume specified but no prior migration marker exists for tenant '{tenantId}' targeting '{targetConfig.Provider}/{targetConfig.Model}'.");
            }
        }

        HashEntry[] entries =
        [
            new("tenantId", tenantId),
            new("targetProvider", targetConfig.Provider),
            new("targetModel", targetConfig.Model),
            new("targetDimensions", targetConfig.Dimensions),
            new("status", resume ? MigrationMarkerStatus.Resumed : MigrationMarkerStatus.Started),
            new("startedAt", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)),
        ];

        // F3: the per-target key and the active-marker key must be written atomically. A non-atomic two-key write
        // can leave the active marker missing while the per-target marker is `started`, silently disabling the
        // runtime guard.
        await WriteMarkerHashesAtomicallyAsync(
            db,
            key,
            EmbeddingMigrationMarkerReader.GetActiveMarkerKey(tenantId),
            entries,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EmbeddingMigrationMarker?> GetActiveMigrationMarkerAsync(string tenantId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        IDatabase db = redis.GetDatabase();
        return await EmbeddingMigrationMarkerReader.ReadActiveMarkerAsync(db, tenantId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteMigrationMarkerAsync(string tenantId, TenantEmbeddingConfig targetConfig, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(targetConfig);
        IDatabase db = redis.GetDatabase();
        string key = GetMarkerKey(tenantId, targetConfig);
        HashEntry[] entries =
        [
            new("status", MigrationMarkerStatus.Completed),
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
            string? keyText = key.ToString();
            if (string.IsNullOrEmpty(keyText) || keyText.Length <= prefix.Length)
            {
                continue;
            }

            string memoryUnitId = keyText[prefix.Length..];
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
        RedisKey key = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId) + memoryUnitId;
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
        RedisKey key = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId) + memoryUnitId;
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

        RedisKey key = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId) + write.MemoryUnitId;
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

        RedisKey key = IndexSchemaDefinitions.GetNaturalLanguageSemanticKeyPrefix(tenantId) + write.MemoryUnitId;
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
        bool rawExists = IndexExists(db, IndexSchemaDefinitions.GetSemanticIndexName(tenantId) + ":previous");
        bool nlExists = IndexExists(db, IndexSchemaDefinitions.GetNaturalLanguageSemanticIndexName(tenantId) + ":previous");
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
