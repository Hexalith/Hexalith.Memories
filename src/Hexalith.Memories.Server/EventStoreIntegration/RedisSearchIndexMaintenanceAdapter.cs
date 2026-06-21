// <copyright file="RedisSearchIndexMaintenanceAdapter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.EventStoreIntegration;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>Server-side adapter implementing <see cref="ISearchIndexMaintenance"/> over RediSearch.
///
/// <para>Writes one curated document per source aggregate into the tenant's existing syntactic index
/// (<c>{tenantId}:memories:idx</c>) at the deterministic key <c>{tenantId}:mu:{aggregateId}</c>, so a
/// re-published entry overwrites the prior one (upsert) and the existing <c>SyntacticSearchService</c>
/// returns the curated entry unchanged — its stored <c>sourceUri</c> is the CloudEvent id the caller parses
/// back to a source aggregate id. The document is written with the same field set the generic ingestion
/// path uses (Story 5.4/5.5 schema) so no index schema change is required.</para>
///
/// <para>Attributes are persisted as a flattened, searchable <c>metadataText</c> field plus a verbatim
/// <c>metadataJson</c> blob (so exact values survive for a later exact-match upgrade). Exact-match attribute
/// TAG filtering is a separate follow-up (handoff §3.6); until it lands, callers filter on the hydrated
/// authoritative status instead.</para>
/// </summary>
internal sealed partial class RedisSearchIndexMaintenanceAdapter : ISearchIndexMaintenance
{
    private const string CuratedSourceType = "event";
    private const string CuratedIngestedBy = "events";
    private const string CuratedEmbeddingProvider = "none";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSearchIndexMaintenanceAdapter> _logger;

    public RedisSearchIndexMaintenanceAdapter(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<RedisSearchIndexMaintenanceAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ApplyEntryChangedAsync(
        string indexTenantId,
        string sourceUri,
        SearchIndexEntryChanged entry,
        string? caseId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        TenantIdGuard.Validate(indexTenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.AggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUri);

        IDatabase db = _redis.GetDatabase();
        EnsureSyntacticIndexExists(db, indexTenantId);

        string hashKey = IndexSchemaDefinitions.GetSyntacticKeyPrefix(indexTenantId) + entry.AggregateId;
        string now = DateTimeOffset.UtcNow.ToString("o");
        string metadataText = FlattenAttributes(entry.Attributes);
        string metadataJson = JsonSerializer.Serialize(
            entry.Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal),
            MemoriesJsonContext.Options);
        string contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entry.Text)));

        HashEntry[] hashEntries =
        [
            new HashEntry("id", entry.AggregateId),
            new HashEntry("tenantId", indexTenantId),
            new HashEntry("content", entry.Text),
            new HashEntry("sourceUri", sourceUri),
            new HashEntry("sourceUriText", sourceUri),
            new HashEntry("sourceType", CuratedSourceType),
            new HashEntry("sourceTypeText", CuratedSourceType),
            new HashEntry("metadataText", metadataText),
            new HashEntry("metadataJson", metadataJson),
            new HashEntry("contentHash", contentHash),
            new HashEntry("caseId", caseId ?? string.Empty),
            new HashEntry("embeddingProvider", CuratedEmbeddingProvider),
            new HashEntry("cloudeventSubject", entry.AggregateId),
            new HashEntry("ingestedBy", CuratedIngestedBy),
            new HashEntry("ingestedAt", now),
            new HashEntry("lastUpdated", now),
        ];

        // HashSet overwrites the existing key in place with the full field set (every write sets the same
        // fields), so a revised entry replaces the prior text + attributes with no orphaned fields — upsert.
        await db.HashSetAsync(hashKey, hashEntries).ConfigureAwait(false);

        LogEntryUpserted(_logger, indexTenantId, entry.AggregateId, sourceUri);
    }

    /// <inheritdoc/>
    public async Task ApplyEntryRemovedAsync(
        string indexTenantId,
        SearchIndexEntryRemoved entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        TenantIdGuard.Validate(indexTenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.AggregateId);

        IDatabase db = _redis.GetDatabase();
        string hashKey = IndexSchemaDefinitions.GetSyntacticKeyPrefix(indexTenantId) + entry.AggregateId;

        // Deleting the hash drops it from the RediSearch index automatically. Idempotent: deleting a
        // non-existent key is a no-op.
        bool removed = await db.KeyDeleteAsync(hashKey).ConfigureAwait(false);
        LogEntryRemoved(_logger, indexTenantId, entry.AggregateId, removed);
    }

    private static string FlattenAttributes(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = [];
        foreach ((string key, string value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                parts.Add(key);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        return string.Join(' ', parts);
    }

    private void EnsureSyntacticIndexExists(IDatabase db, string tenantId)
    {
        // Safety net: the index is normally created by TenantProvisioningWorkflow before the tenant is Active
        // (and routing only reaches here for an Active tenant). Create-if-missing keeps a curated write from
        // failing on a rare race where the tenant exists but the index is not yet present.
        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(tenantId);
        try
        {
            db.FT().Create(
                indexName,
                IndexSchemaDefinitions.CreateSyntacticParams(tenantId),
                IndexSchemaDefinitions.CreateSyntacticSchema());
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Expected steady-state path: the index already exists.
        }
    }

    [LoggerMessage(
        EventId = 9190,
        Level = LogLevel.Information,
        Message = "Curated search index upserted entry for tenant {TenantId}, aggregate {AggregateId} (sourceUri={SourceUri}).")]
    private static partial void LogEntryUpserted(ILogger logger, string tenantId, string aggregateId, string sourceUri);

    [LoggerMessage(
        EventId = 9191,
        Level = LogLevel.Information,
        Message = "Curated search index removed entry for tenant {TenantId}, aggregate {AggregateId} (existed={Existed}).")]
    private static partial void LogEntryRemoved(ILogger logger, string tenantId, string aggregateId, bool existed);
}
