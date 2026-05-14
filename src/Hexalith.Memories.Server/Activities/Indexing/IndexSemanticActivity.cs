// <copyright file="IndexSemanticActivity.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using System.Runtime.InteropServices;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Migration;

using Microsoft.Extensions.Logging;

using NRedisStack.RedisStackCommands;

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
        EmbeddingMigrationMarker? marker = await EmbeddingMigrationMarkerReader
            .ReadActiveMarkerAsync(db, input.TenantId, CancellationToken.None)
            .ConfigureAwait(false);
        EmbeddingMigrationMarkerReader.EnsureWriteMatchesMarker(
            marker,
            input.EmbeddingProvider,
            input.EmbeddingModel,
            input.EmbeddingDimensions);

        var ft = db.FT();
        string? cloudEventSubject = TryGetMetadataValue(input.Metadata, "cloudevent.subject");

        string indexName = IndexSchemaDefinitions.GetSemanticIndexName(input.TenantId);
        string hashKey = $"{input.TenantId}:vec:{input.MemoryUnitId}";

        try
        {
            ft.Create(
                indexName,
                IndexSchemaDefinitions.CreateSemanticParams(input.TenantId),
                IndexSchemaDefinitions.CreateSemanticSchema(input.EmbeddingDimensions));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            EnsureSemanticIndexReady(db, indexName, input.TenantId, input.EmbeddingDimensions);
            _logger.LogWarning("Redis Vector index {IndexName} already exists for tenant {TenantId}", indexName, input.TenantId);
        }

        List<HashEntry> hashEntries =
        [
            new HashEntry("embedding", vectorBytes),
            new HashEntry("memoryUnitId", input.MemoryUnitId),
            new HashEntry("caseId", input.CaseId),
            new HashEntry("embeddingProvider", input.EmbeddingProvider),
            new HashEntry("embeddingModel", input.EmbeddingModel),
            new HashEntry("embeddingDimensions", input.EmbeddingDimensions),
        ];

        if (!string.IsNullOrWhiteSpace(cloudEventSubject))
        {
            hashEntries.Add(new HashEntry("cloudeventSubject", cloudEventSubject));
        }

        await db.HashSetAsync(hashKey, [.. hashEntries]).ConfigureAwait(false);

        _logger.LogInformation(
            "Indexed memory unit {MemoryUnitId} in Redis Vector for tenant {TenantId}",
            input.MemoryUnitId,
            input.TenantId);

        return new IndexResult("semantic", input.MemoryUnitId, input.TenantId);
    }

    private void EnsureSemanticIndexReady(IDatabase db, string indexName, string tenantId, int expectedDimensions)
    {
        RedisResult info = db.Execute("FT.INFO", indexName);
        List<string> problems = [];

        IReadOnlyList<string> prefixes = IndexSchemaDefinitions.GetIndexPrefixes(info);
        string expectedPrefix = IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(IndexSchemaDefinitions.GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expectedFields = new(IndexSchemaDefinitions.GetSemanticFieldIdentifiers(), StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expectedFields)
            && IndexSchemaDefinitions.TryUpgradeMissingTagField(db, indexName, actualFields, expectedFields, "cloudeventSubject"))
        {
            actualFields.Add("cloudeventSubject");
            _logger.LogInformation(
                "Added missing cloudeventSubject field to Redis Vector index {IndexName} for tenant {TenantId}",
                indexName,
                tenantId);
        }

        if (!actualFields.SetEquals(expectedFields))
        {
            problems.Add($"expected fields [{string.Join(", ", expectedFields.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        int? actualDimensions = TryFindVectorDimensions(info, "embedding");
        if (actualDimensions is null)
        {
            problems.Add("embedding vector dimensions are missing from FT.INFO");
        }
        else if (actualDimensions.Value != expectedDimensions)
        {
            problems.Add($"expected {expectedDimensions} dimensions but found {actualDimensions.Value}");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Existing Redis Vector index '{indexName}' does not match the expected tenant schema: {string.Join("; ", problems)}.");
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

    private static string? TryGetMetadataValue(IReadOnlyDictionary<string, MetadataField> metadata, string key)
        => metadata.TryGetValue(key, out MetadataField? field) && !string.IsNullOrWhiteSpace(field.Value)
            ? field.Value
            : null;
}
