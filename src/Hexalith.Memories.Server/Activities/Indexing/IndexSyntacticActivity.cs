namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Text.Json;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

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
        string ingestedAt = input.IngestedAt.ToString("o");

        string indexName = $"{input.TenantId}:memories:idx";
        string hashKey = $"{input.TenantId}:mu:{input.MemoryUnitId}";

        try
        {
            ft.Create(
                indexName,
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix($"{input.TenantId}:mu:"),
                new Schema()
                    .AddTextField("content", 1.0)
                    .AddTextField("sourceUriText", 0.25)
                    .AddTextField("sourceTypeText", 0.25)
                    .AddTextField("metadataText", 0.25)
                    .AddTagField("sourceUri")
                    .AddTagField("sourceType")
                    .AddTagField("contentHash")
                    .AddTagField("caseId")
                    .AddTagField("embeddingProvider"));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            _logger.LogWarning("RediSearch index {IndexName} already exists for tenant {TenantId}", indexName, input.TenantId);
        }

        await db.HashSetAsync(
            hashKey,
            [
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
                new HashEntry("ingestedBy", input.IngestedBy),
                new HashEntry("ingestedAt", ingestedAt),
                new HashEntry("lastUpdated", ingestedAt),
            ]).ConfigureAwait(false);

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

    private static string ToCamelCase<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
