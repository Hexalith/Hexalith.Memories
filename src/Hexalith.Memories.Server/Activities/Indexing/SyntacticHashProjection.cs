// <copyright file="SyntacticHashProjection.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;
using Hexalith.Memories.Server.Serialization;

using StackExchange.Redis;

/// <summary>
/// Single source of truth for the RediSearch syntactic memory-unit hash (<c>{tenantId}:mu:{memoryUnitId}</c>).
/// Shared by <see cref="IndexSyntacticActivity"/> (ingest) and the Story 26.2 restore path so both write the
/// identical field set and both round-trip through <c>CaseService.ParseMemoryUnitFromHash</c>. Keeping the
/// projection in one place is what makes export → import fidelity byte-exact (AC2/AC7).
/// </summary>
internal static class SyntacticHashProjection
{
    /// <summary>Builds the ordered syntactic hash entries for a memory unit.</summary>
    /// <param name="memoryUnitId">The memory unit identifier (hash field <c>id</c>).</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="content">The already-resolved extracted content text.</param>
    /// <param name="sourceUri">The source URI.</param>
    /// <param name="sourceType">The source type (persisted camelCase in <c>sourceType</c>/<c>sourceTypeText</c>).</param>
    /// <param name="metadata">The metadata dictionary (flattened into search text/tags and stored JSON).</param>
    /// <param name="contentHash">The content hash.</param>
    /// <param name="caseId">The owning case identifier.</param>
    /// <param name="embeddingProvider">The embedding provider identifier (may be null for legacy units).</param>
    /// <param name="embeddingModel">The embedding model identifier (may be null for legacy units).</param>
    /// <param name="embeddingDimensions">The source embedding vector dimensions.</param>
    /// <param name="ingestedBy">The principal that ingested the unit.</param>
    /// <param name="ingestedAt">The ingestion timestamp (persisted as ISO-8601 round-trip in <c>ingestedAt</c>).</param>
    /// <param name="lastUpdated">The last-updated timestamp (persisted as ISO-8601 round-trip in <c>lastUpdated</c>).</param>
    /// <returns>The syntactic hash entries, including the conditional <c>cloudeventSubject</c> field.</returns>
    internal static List<HashEntry> BuildEntries(
        string memoryUnitId,
        string tenantId,
        string content,
        string sourceUri,
        SourceType sourceType,
        IReadOnlyDictionary<string, MetadataField> metadata,
        string contentHash,
        string caseId,
        string? embeddingProvider,
        string? embeddingModel,
        int embeddingDimensions,
        string ingestedBy,
        DateTimeOffset ingestedAt,
        DateTimeOffset lastUpdated)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        string sourceTypeText = ToCamelCase(sourceType);
        string metadataText = FlattenMetadata(metadata);
        string attributeTags = FlattenMetadataTags(metadata);
        string metadataJson = JsonSerializer.Serialize(
            PersistenceModelMapper.ToStored(metadata),
            MemoriesPersistenceJsonContext.Options);
        string? cloudEventSubject = TryGetMetadataValue(metadata, "cloudevent.subject");

        List<HashEntry> hashEntries =
        [
            new HashEntry("id", memoryUnitId),

            // Story 5.4 AC2: tenantId persisted on the MU hash to enable tertiary
            // mismatch detection in CaseService (primary defense is the key prefix).
            new HashEntry("tenantId", tenantId),
            new HashEntry("content", content),
            new HashEntry("sourceUri", sourceUri),
            new HashEntry("sourceUriText", sourceUri),
            new HashEntry("sourceType", sourceTypeText),
            new HashEntry("sourceTypeText", sourceTypeText),
            new HashEntry("metadataText", metadataText),
            new HashEntry("attributeTags", attributeTags),
            new HashEntry("metadataJson", metadataJson),
            new HashEntry("contentHash", contentHash),
            new HashEntry("caseId", caseId),
            new HashEntry("embeddingProvider", embeddingProvider ?? string.Empty),

            // Story 5.5 FR70: persist the embedding model so future audits can attribute
            // vectors to the (provider, model) pair that generated them.
            new HashEntry("embeddingModel", embeddingModel ?? string.Empty),
            new HashEntry("embeddingDimensions", embeddingDimensions),
            new HashEntry("ingestedBy", ingestedBy),
            new HashEntry("ingestedAt", ingestedAt.ToString("o")),
            new HashEntry("lastUpdated", lastUpdated.ToString("o")),
        ];

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
        }

        return hashEntries;
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

    private static string? TryGetMetadataValue(IReadOnlyDictionary<string, MetadataField> metadata, string key)
        => metadata.TryGetValue(key, out MetadataField? field) && !string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : null;

    private static string ToCamelCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
