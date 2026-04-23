namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Globalization;
using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit in RediSearch for full-text search.</summary>
public sealed class IndexSyntacticActivity : WorkflowActivity<IndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSyntacticActivity> _logger;

    public IndexSyntacticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSyntacticActivity> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task<IndexResult> RunAsync(
        WorkflowActivityContext context,
        IndexInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        TenantIdGuard.Validate(input.TenantId);

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();
        string sourceType = ToCamelCase(input.SourceType);
        string metadataText = FlattenMetadata(input.Metadata);
        string metadataJson = JsonSerializer.Serialize(input.Metadata, MemoriesJsonContext.Options);
        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");
        string ingestedAt = input.IngestedAt.ToString("o");

        string indexName = IndexSchemaDefinitions.GetSyntacticIndexName(input.TenantId);
        string hashKey = $"{input.TenantId}:mu:{input.MemoryUnitId}";

        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateSyntacticParams(input.TenantId),
                IndexSchemaDefinitions.CreateSyntacticSchema());
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureSyntacticIndexReady(db, indexName, input.TenantId);
            _logger.LogWarning("RediSearch index {IndexName} already exists for tenant {TenantId}", indexName, input.TenantId);
        }

        List<HashEntry> hashEntries =
        [
            new HashEntry("id", input.MemoryUnitId),
            // Story 5.4 AC2: tenantId persisted on the MU hash to enable tertiary
            // mismatch detection in CaseService (primary defense is the key prefix).
            new HashEntry("tenantId", input.TenantId),
            new HashEntry("content", input.Content),
            new HashEntry("sourceUri", input.SourceUri),
            new HashEntry("sourceUriText", input.SourceUri),
            new HashEntry("sourceType", sourceType),
            new HashEntry("sourceTypeText", sourceType),
            new HashEntry("metadataText", metadataText),
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

    private static string ToCamelCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }

    private void EnsureSyntacticIndexReady(IDatabase db, string indexName, string tenantId)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        List<string> problems = [];

        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        string expectedPrefix = IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expectedFields = new(IndexSchemaDefinitions.GetSyntacticFieldIdentifiers(), StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expectedFields)
            && IndexSchemaDefinitions.TryUpgradeMissingTagField(db, indexName, actualFields, expectedFields, "cloudeventSubject"))
        {
            actualFields.Add("cloudeventSubject");
            _logger.LogInformation(
                "Added missing cloudeventSubject field to RediSearch index {IndexName} for tenant {TenantId}",
                indexName,
                tenantId);
        }

        if (!actualFields.SetEquals(expectedFields))
        {
            problems.Add($"expected fields [{string.Join(", ", expectedFields.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing RediSearch index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
        }
    }
}
