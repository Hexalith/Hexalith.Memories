// <copyright file="IndexSyntacticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Globalization;
using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in RediSearch for full-text search.</summary>
public sealed class IndexSyntacticActivity : WorkflowActivity<IndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSyntacticActivity> _logger;
    private readonly IWorkflowPayloadStore? _payloadStore;
    private readonly ITenantIndexReadinessVerifier _readinessVerifier;

    public IndexSyntacticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSyntacticActivity> logger,
        IWorkflowPayloadStore? payloadStore = null,
        ITenantIndexReadinessVerifier? readinessVerifier = null)
    {
        _redis = redis;
        _logger = logger;
        _payloadStore = payloadStore;
        _readinessVerifier = readinessVerifier
            ?? new TenantIndexReadinessVerifier(NullLogger<TenantIndexReadinessVerifier>.Instance);
    }

    /// <inheritdoc/>
    public override async Task<IndexResult> RunAsync(
        WorkflowActivityContext context,
        IndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);
        string content = await ResolveContentAsync(input).ConfigureAwait(false);

        IDatabase db = _redis.GetDatabase();
        string sourceType = ToCamelCase(input.SourceType);
        string metadataText = FlattenMetadata(input.Metadata);
        string attributeTags = FlattenMetadataTags(input.Metadata);
        string metadataJson = JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options);
        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");
        string ingestedAt = input.IngestedAt.ToString("o");

        string hashKey = IndexSchemaDefinitions.BuildSyntacticKey(input.TenantId, input.MemoryUnitId);

        // Story 23.7 (A34): TenantProvisioningWorkflow owns index creation. Ingestion only verifies the tenant's
        // syntactic index exists and matches the expected schema, memoized once per tenant/index family/process —
        // no per-document FT.CREATE, no "index already exists" warning, no blocking Thread.Sleep retry.
        await _readinessVerifier
            .EnsureReadyAsync(db, input.TenantId, TenantIndexFamily.Syntactic, null, CancellationToken.None)
            .ConfigureAwait(false);

        List<HashEntry> hashEntries =
        [
            new HashEntry("id", input.MemoryUnitId),
            // Story 5.4 AC2: tenantId persisted on the MU hash to enable tertiary
            // mismatch detection in CaseService (primary defense is the key prefix).
            new HashEntry("tenantId", input.TenantId),
            new HashEntry("content", content),
            new HashEntry("sourceUri", input.SourceUri),
            new HashEntry("sourceUriText", input.SourceUri),
            new HashEntry("sourceType", sourceType),
            new HashEntry("sourceTypeText", sourceType),
            new HashEntry("metadataText", metadataText),
            new HashEntry("attributeTags", attributeTags),
            new HashEntry("metadataJson", metadataJson),
            new HashEntry("contentHash", input.ContentHash),
            new HashEntry("caseId", input.CaseId),
            new HashEntry("embeddingProvider", input.EmbeddingProvider),
            // Story 5.5 FR70: persist the embedding model so future audits can attribute
            // vectors to the (provider, model) pair that generated them.
            new HashEntry("embeddingModel", input.EmbeddingModel),
            new HashEntry("ingestedBy", input.IngestedBy),
            new HashEntry("ingestedAt", ingestedAt),
            new HashEntry("lastUpdated", ingestedAt),
        ];

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
        }

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        // Story 5.5 AC1 / Amendment A + L + T: stamp last-activity AFTER the hash write succeeds
        // (ordering L: never advertise activity that never happened). Fire-and-forget because a
        // stale timestamp is acceptable; a failed ingest is not. Deploy-doc TODO: the
        // {tenantId}:metadata hash field requires a noeviction (or volatile-*) maxmemory-policy
        // so it is not silently lost under memory pressure (Amendment T).
        try
        {
            _ = db.HashSetAsync(
                $"{input.TenantId}:metadata",
                "lastActivityAt",
                input.IngestedAt.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture),
                flags: CommandFlags.FireAndForget);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to stamp lastActivityAt for tenant {TenantId}; ingest continues",
                input.TenantId);
        }

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in RediSearch for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("syntactic", input.MemoryUnitId, input.TenantId);
    }

    private static string FlattenMetadata(IReadOnlyDictionary<string, MetadataField> metadata)
    {
        if (metadata.Count == 0)
        {
            return string.Empty;
        }

        List<string> parts = [];
        foreach ((string key, MetadataField field) in metadata)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                parts.Add(key);
            }

            if (!string.IsNullOrWhiteSpace(field.Value))
            {
                parts.Add(field.Value);
            }

            parts.Add(ToCamelCase(field.Origin));
        }

        return string.Join(' ', parts);
    }

    private static string? TryGetMetadataValue(IReadOnlyDictionary<string, MetadataField> metadata, string key)
        => metadata.TryGetValue(key, out MetadataField? field) && !string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : null;

    private async Task<string> ResolveContentAsync(IndexInput input)
    {
        if (input.ContentReference is null)
        {
            return input.Content;
        }

        byte[] contentBytes = await RequirePayloadStore()
            .ReadAsync(
                input.ContentReference,
                input.TenantId,
                input.MemoryUnitId,
                WorkflowPayloadKind.ExtractedText,
                CancellationToken.None)
            .ConfigureAwait(false);
        return System.Text.Encoding.UTF8.GetString(contentBytes);
    }

    private IWorkflowPayloadStore RequirePayloadStore()
        => _payloadStore ?? throw new WorkflowPayloadException("PAYLOAD_STORE_UNAVAILABLE", "index-content");

    private static string FlattenMetadataTags(IReadOnlyDictionary<string, MetadataField> metadata)
    {
        if (metadata.Count == 0)
        {
            return string.Empty;
        }

        List<string> tags = [];
        foreach ((string key, MetadataField field) in metadata.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(field.Value))
            {
                tags.Add(SyntacticSearchService.BuildAttributeTag(key, field.Value));
            }
        }

        return string.Join(',', tags);
    }

    private static string ToCamelCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
