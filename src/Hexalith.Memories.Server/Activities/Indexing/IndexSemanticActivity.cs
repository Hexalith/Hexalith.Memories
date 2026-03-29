namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

using StackExchange.Redis;

/// <summary>DAPR Workflow activity that indexes a memory unit embedding in Redis Vector Search.</summary>
public sealed class IndexSemanticActivity : WorkflowActivity<IndexInput, IndexResult>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IndexSemanticActivity> _logger;

    public IndexSemanticActivity(
        [FromKeyedServices("redis")] IConnectionMultiplexer redis,
        ILogger<IndexSemanticActivity> logger)
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
        ArgumentNullException.ThrowIfNull(input.EmbeddingVector);
        if (input.EmbeddingVector.Length == 0)
        {
            throw new ArgumentException("EmbeddingVector must not be empty.", nameof(input));
        }

        byte[] vectorBytes = MemoryMarshal.AsBytes(input.EmbeddingVector.AsSpan()).ToArray();
        if (vectorBytes.Length != input.EmbeddingDimensions * sizeof(float))
        {
            throw new InvalidOperationException(
                $"Vector byte length {vectorBytes.Length} does not match expected {input.EmbeddingDimensions * sizeof(float)} bytes for {input.EmbeddingDimensions} dimensions");
        }

        IDatabase db = _redis.GetDatabase();
        var ft = db.FT();

        string indexName = $"{input.TenantId}:memories:vec";
        string hashKey = $"{input.TenantId}:vec:{input.MemoryUnitId}";

        try
        {
            ft.Create(
                indexName,
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix($"{input.TenantId}:vec:"),
                new Schema()
                    .AddVectorField(
                        "embedding",
                        Schema.VectorField.VectorAlgo.HNSW,
                        new Dictionary<string, object>()
                        {
                            ["TYPE"] = "FLOAT32",
                            ["DIM"] = input.EmbeddingDimensions.ToString(),
                            ["DISTANCE_METRIC"] = "COSINE",
                        })
                    .AddTagField("memoryUnitId")
                    .AddTagField("caseId"));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            await EnsureVectorDimensionsMatchAsync(db, indexName, input.EmbeddingDimensions).ConfigureAwait(false);
            _logger.LogWarning("Redis Vector index {IndexName} already exists for tenant {TenantId}", indexName, input.TenantId);
        }

        await db.HashSetAsync(
            hashKey,
            [
                new HashEntry("embedding", vectorBytes),
                new HashEntry("memoryUnitId", input.MemoryUnitId),
                new HashEntry("caseId", input.CaseId),
            ]).ConfigureAwait(false);

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in Redis Vector for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic", input.MemoryUnitId, input.TenantId);
    }

    private static async Task EnsureVectorDimensionsMatchAsync(IDatabase db, string indexName, int expectedDimensions)
    {
        RedisResult info = await db.ExecuteAsync("FT.INFO", indexName).ConfigureAwait(false);
        int? actualDimensions = TryFindVectorDimensions(info, "embedding");

        if (actualDimensions is null)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not expose dimensions for the embedding field.");
        }

        if (actualDimensions.Value != expectedDimensions)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' uses {actualDimensions.Value} dimensions but the current embedding requires {expectedDimensions}.");
        }
    }

    private static int? TryFindVectorDimensions(RedisResult result, string attributeName)
    {
        if (TryReadVectorFieldDimensions(result, attributeName, out int dimensions))
        {
            return dimensions;
        }

        RedisResult[]? items = TryGetArray(result);
        if (items is null)
        {
            return null;
        }

        foreach (RedisResult item in items)
        {
            int? nestedDimensions = TryFindVectorDimensions(item, attributeName);
            if (nestedDimensions is not null)
            {
                return nestedDimensions;
            }
        }

        return null;
    }

    private static bool TryReadVectorFieldDimensions(RedisResult result, string attributeName, out int dimensions)
    {
        dimensions = 0;

        RedisResult[]? items = TryGetArray(result);
        if (items is null || items.Length < 2 || items.Length % 2 != 0)
        {
            return false;
        }

        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < items.Length; i += 2)
        {
            string? key = TryGetString(items[i]);
            string? value = TryGetString(items[i + 1]);

            if (!string.IsNullOrWhiteSpace(key) && value is not null)
            {
                values[key] = value;
            }
        }

        bool nameMatches =
            (values.TryGetValue("identifier", out string? identifier)
                && string.Equals(identifier, attributeName, StringComparison.OrdinalIgnoreCase))
            || (values.TryGetValue("attribute", out string? attribute)
                && string.Equals(attribute, attributeName, StringComparison.OrdinalIgnoreCase));

        if (!nameMatches
            || !values.TryGetValue("type", out string? type)
            || !string.Equals(type, "VECTOR", StringComparison.OrdinalIgnoreCase)
            || !values.TryGetValue("dim", out string? dimensionText)
            || !int.TryParse(dimensionText, out dimensions))
        {
            return false;
        }

        return true;
    }

    private static RedisResult[]? TryGetArray(RedisResult result)
    {
        try
        {
            RedisResult[]? items = (RedisResult[]?)result;
            return items;
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    private static string? TryGetString(RedisResult result)
        => result.ToString();
}
