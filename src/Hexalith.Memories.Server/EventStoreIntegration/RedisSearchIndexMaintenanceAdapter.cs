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
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
/// <para>Attributes are persisted as flattened searchable <c>metadataText</c>, exact-match
/// <c>attributeTags</c>, and verbatim <c>metadataJson</c> fields. Search callers can pre-filter on the exact
/// TAG values while still hydrating authoritative domain state before rendering rows.</para>
/// </summary>
internal sealed partial class RedisSearchIndexMaintenanceAdapter : ISearchIndexMaintenance
{
    private const string CuratedSourceType = "event";
    private const string CuratedIngestedBy = "events";
    private const string CuratedEmbeddingProvider = "none";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSearchIndexMaintenanceAdapter> _logger;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    public RedisSearchIndexMaintenanceAdapter(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<RedisSearchIndexMaintenanceAdapter> logger,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        ArgumentNullException.ThrowIfNull(redis);
        ArgumentNullException.ThrowIfNull(logger);
        _redis = redis;
        _logger = logger;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
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

        // Story 23.7 (A34): this curated EventStore/Tenants search-index path shared the same per-write
        // create-if-missing symptom as ingestion. It is now reconciled onto the same memoized readiness policy —
        // TenantProvisioningWorkflow owns creation, and a missing index for an active tenant fails clearly rather
        // than being recreated on every upsert. Routing only reaches here for an Active, provisioned tenant.
        await _readinessVerifier
            .EnsureReadyAsync(db, indexTenantId, TenantIndexFamily.Syntactic, null, cancellationToken)
            .ConfigureAwait(false);

        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(indexTenantId, entry.AggregateId);
        string now = DateTimeOffset.UtcNow.ToString("o");
        string metadataText = FlattenAttributes(entry.Attributes);
        string attributeTags = FlattenAttributeTags(entry.Attributes);
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
            new HashEntry("attributeTags", attributeTags),
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
        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(indexTenantId, entry.AggregateId);

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

    private static string FlattenAttributeTags(IReadOnlyDictionary<string, string>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return string.Empty;
        }

        List<string> tags = [];
        foreach ((string key, string value) in attributes.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                tags.Add(SyntacticSearchService.BuildAttributeTag(key, value));
            }
        }

        return string.Join(',', tags);
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
