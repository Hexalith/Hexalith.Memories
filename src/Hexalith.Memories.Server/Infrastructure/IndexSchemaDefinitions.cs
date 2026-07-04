// <copyright file="IndexSchemaDefinitions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Infrastructure;

using System.Globalization;

using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

using StackExchange.Redis;

/// <summary>Single source of truth for RediSearch and Redis Vector index schemas.
/// Both provisioning activities and ingestion activities reference this class to prevent schema drift.</summary>
internal static class IndexSchemaDefinitions
{
    private static readonly string[] SemanticFieldIdentifiers = ["embedding", "memoryUnitId", "caseId", "cloudeventSubject"];
    private static readonly string[] NaturalLanguageSemanticFieldIdentifiers = ["embedding", "memoryUnitId", "caseId", "naturalLanguageDescription"];
    private static readonly string[] SyntacticFieldIdentifiers =
    [
        "content",
        "sourceUriText",
        "sourceTypeText",
        "metadataText",
        "sourceUri",
        "sourceType",
        "contentHash",
        "caseId",
        "cloudeventSubject",
        "attributeTags",
        "embeddingProvider",
    ];

    /// <summary>Gets the index name suffix for RediSearch syntactic indexes.</summary>
    public const string SyntacticIndexSuffix = ":memories:idx";

    /// <summary>Gets the index name suffix for Redis Vector semantic indexes.</summary>
    public const string SemanticIndexSuffix = ":memories:vec";

    /// <summary>Gets the key prefix suffix for syntactic hash entries.</summary>
    public const string SyntacticKeyPrefixSuffix = ":mu:";

    /// <summary>Gets the key prefix suffix for semantic hash entries.</summary>
    public const string SemanticKeyPrefixSuffix = ":vec:";

    /// <summary>Story 9.2: index name suffix for the natural-language (LLM-authored) semantic index.</summary>
    public const string NaturalLanguageSemanticIndexSuffix = ":memories:vec:nl";

    /// <summary>Story 21.3: legacy nested key prefix suffix for natural-language semantic hash entries.</summary>
    public const string LegacyNaturalLanguageSemanticKeyPrefixSuffix = ":vec:nl:";

    /// <summary>Story 9.2: key prefix suffix for natural-language semantic hash entries.</summary>
    public const string NaturalLanguageSemanticKeyPrefixSuffix = ":vecnl:";

    /// <summary>Gets the RediSearch syntactic index name for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The full index name.</returns>
    public static string GetSyntacticIndexName(string tenantId)
        => tenantId + SyntacticIndexSuffix;

    /// <summary>Gets the Redis Vector semantic index name for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The full index name.</returns>
    public static string GetSemanticIndexName(string tenantId)
        => tenantId + SemanticIndexSuffix;

    /// <summary>Gets the key prefix for syntactic hash entries.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The key prefix.</returns>
    public static string GetSyntacticKeyPrefix(string tenantId)
        => tenantId + SyntacticKeyPrefixSuffix;

    /// <summary>Gets the key prefix for semantic hash entries.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The key prefix.</returns>
    public static string GetSemanticKeyPrefix(string tenantId)
        => tenantId + SemanticKeyPrefixSuffix;

    /// <summary>Story 9.2: Gets the Redis Vector natural-language semantic index name for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The full index name.</returns>
    public static string GetNaturalLanguageSemanticIndexName(string tenantId)
        => tenantId + NaturalLanguageSemanticIndexSuffix;

    /// <summary>Story 9.2: Gets the key prefix for natural-language semantic hash entries.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The key prefix.</returns>
    public static string GetNaturalLanguageSemanticKeyPrefix(string tenantId)
        => tenantId + NaturalLanguageSemanticKeyPrefixSuffix;

    /// <summary>Story 21.3: Gets the legacy nested key prefix for natural-language semantic hash migration.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The legacy key prefix.</returns>
    public static string GetLegacyNaturalLanguageSemanticKeyPrefix(string tenantId)
        => tenantId + LegacyNaturalLanguageSemanticKeyPrefixSuffix;

    /// <summary>Creates the FTCreateParams for a RediSearch syntactic index.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The FTCreateParams configured for the syntactic index.</returns>
    public static FTCreateParams CreateSyntacticParams(string tenantId)
        => new FTCreateParams()
            .On(IndexDataType.HASH)
            .Prefix(GetSyntacticKeyPrefix(tenantId));

    /// <summary>Creates the schema for a RediSearch syntactic index.</summary>
    /// <returns>The schema with all required fields.</returns>
    public static Schema CreateSyntacticSchema()
        => new Schema()
            .AddTextField("content", 1.0)
            .AddTextField("sourceUriText", 0.25)
            .AddTextField("sourceTypeText", 0.25)
            .AddTextField("metadataText", 0.25)
            .AddTagField("sourceUri")
            .AddTagField("sourceType")
            .AddTagField("contentHash")
            .AddTagField("caseId")
            .AddTagField("cloudeventSubject")
            .AddTagField("attributeTags")
            .AddTagField("embeddingProvider");

    /// <summary>Creates the FTCreateParams for a Redis Vector semantic index.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The FTCreateParams configured for the semantic index.</returns>
    public static FTCreateParams CreateSemanticParams(string tenantId)
        => new FTCreateParams()
            .On(IndexDataType.HASH)
            .Prefix(GetSemanticKeyPrefix(tenantId));

    /// <summary>Creates the schema for a Redis Vector semantic index.</summary>
    /// <param name="dimensions">The number of embedding dimensions.</param>
    /// <returns>The schema with vector and tag fields.</returns>
    public static Schema CreateSemanticSchema(int dimensions)
        => CreateSemanticSchemaCore(dimensions, includeNaturalLanguageText: false, includeCloudEventSubject: true);

    /// <summary>Story 9.2: Creates the FTCreateParams for a natural-language Redis Vector semantic index.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The FTCreateParams configured for the NL semantic index.</returns>
    public static FTCreateParams CreateNaturalLanguageSemanticParams(string tenantId)
        => new FTCreateParams()
            .On(IndexDataType.HASH)
            .Prefix(GetNaturalLanguageSemanticKeyPrefix(tenantId));

    /// <summary>Story 9.2: Creates the schema for the natural-language Redis Vector semantic index.
    /// Mirrors <see cref="CreateSemanticSchema"/> (same HNSW/FLOAT32/COSINE shape at the same dimensions)
    /// plus a TEXT field for the LLM-authored description (business-meaning search surface). Delegates
    /// to the shared core helper so schema drift across both indexes is prevented (Risk #5).</summary>
    /// <param name="dimensions">The number of embedding dimensions.</param>
    /// <returns>The schema with vector, tag, and natural-language text fields.</returns>
    public static Schema CreateNaturalLanguageSemanticSchema(int dimensions)
        => CreateSemanticSchemaCore(dimensions, includeNaturalLanguageText: true, includeCloudEventSubject: false);

    private static Schema CreateSemanticSchemaCore(int dimensions, bool includeNaturalLanguageText, bool includeCloudEventSubject)
    {
        Schema schema = new Schema()
            .AddVectorField(
                "embedding",
                Schema.VectorField.VectorAlgo.HNSW,
                new Dictionary<string, object>()
                {
                    ["TYPE"] = "FLOAT32",
                    ["DIM"] = dimensions.ToString(),
                    ["DISTANCE_METRIC"] = "COSINE",
                })
            .AddTagField("memoryUnitId")
            .AddTagField("caseId");

        if (includeCloudEventSubject)
        {
            schema.AddTagField("cloudeventSubject");
        }

        if (includeNaturalLanguageText)
        {
            schema.AddTextField("naturalLanguageDescription", 1.0);
        }

        return schema;
    }

    /// <summary>Gets the expected attribute identifiers for a syntactic index.</summary>
    /// <returns>The expected attribute names.</returns>
    public static IReadOnlyList<string> GetSyntacticFieldIdentifiers()
        => SyntacticFieldIdentifiers;

    /// <summary>Gets the expected attribute identifiers for a semantic index.</summary>
    /// <returns>The expected attribute names.</returns>
    public static IReadOnlyList<string> GetSemanticFieldIdentifiers()
        => SemanticFieldIdentifiers;

    /// <summary>Story 9.2: Gets the expected attribute identifiers for the natural-language semantic index.</summary>
    /// <returns>The expected attribute names.</returns>
    public static IReadOnlyList<string> GetNaturalLanguageSemanticFieldIdentifiers()
        => NaturalLanguageSemanticFieldIdentifiers;

    /// <summary>Describes schema mismatches for a Redis Vector index from a raw <c>FT.INFO</c> response.</summary>
    /// <param name="info">The raw <c>FT.INFO</c> payload.</param>
    /// <param name="expectedPrefix">The expected single key prefix.</param>
    /// <param name="expectedFields">The exact expected indexed fields.</param>
    /// <param name="expectedDimensions">The expected vector dimensions for the <c>embedding</c> field.</param>
    /// <returns>The list of problems; empty when the index matches expectations.</returns>
    public static IReadOnlyList<string> DescribeVectorSchemaProblems(
        RedisResult info,
        string expectedPrefix,
        IReadOnlyCollection<string> expectedFields,
        int expectedDimensions)
    {
        ArgumentNullException.ThrowIfNull(expectedFields);

        List<string> problems = [];

        IReadOnlyList<string> prefixes = GetIndexPrefixes(info);
        if (prefixes.Count != 1 || !string.Equals(prefixes[0], expectedPrefix, StringComparison.Ordinal))
        {
            problems.Add($"expected prefix '{expectedPrefix}' but found [{string.Join(", ", prefixes)}]");
        }

        HashSet<string> actualFields = new(GetAttributeIdentifiers(info), StringComparer.OrdinalIgnoreCase);
        HashSet<string> expected = new(expectedFields, StringComparer.OrdinalIgnoreCase);
        if (!actualFields.SetEquals(expected))
        {
            problems.Add($"expected fields [{string.Join(", ", expected.OrderBy(v => v))}] but found [{string.Join(", ", actualFields.OrderBy(v => v))}]");
        }

        if (!TryGetVectorDimensions(info, "embedding", out int actualDimensions))
        {
            problems.Add("embedding vector dimensions are missing from FT.INFO");
        }
        else if (actualDimensions != expectedDimensions)
        {
            problems.Add($"expected {expectedDimensions} dimensions but found {actualDimensions}");
        }

        return problems;
    }

    /// <summary>Gets the indexed attribute identifiers from a raw <c>FT.INFO</c> response.</summary>
    /// <param name="raw">The raw response.</param>
    /// <returns>A set of indexed field identifiers.</returns>
    public static IReadOnlySet<string> GetAttributeIdentifiers(RedisResult raw)
    {
        HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
        RedisResult[]? attributes = TryGetTopLevelArray(raw, "attributes");
        if (attributes is null)
        {
            return identifiers;
        }

        foreach (RedisResult attribute in attributes)
        {
            Dictionary<string, string> values = ParseKeyValuePairs(attribute);
            if (values.TryGetValue("identifier", out string? identifier) && !string.IsNullOrWhiteSpace(identifier))
            {
                identifiers.Add(identifier);
                continue;
            }

            if (values.TryGetValue("attribute", out string? alias) && !string.IsNullOrWhiteSpace(alias))
            {
                identifiers.Add(alias);
            }
        }

        return identifiers;
    }

    /// <summary>Attempts to read the indexed document count from a raw <c>FT.INFO</c> response.</summary>
    /// <param name="raw">The raw response.</param>
    /// <param name="docCount">The parsed document count.</param>
    /// <returns><see langword="true"/> when the count could be parsed.</returns>
    public static bool TryGetDocumentCount(RedisResult raw, out int docCount)
    {
        RedisResult? result = TryGetTopLevelValue(raw, "num_docs");
        if (result is null)
        {
            docCount = 0;
            return false;
        }

        if (result.Resp2Type == ResultType.Integer)
        {
            long value = (long)result;
            if (value is >= 0 and <= int.MaxValue)
            {
                docCount = (int)value;
                return true;
            }
        }

        if (result.Resp2Type == ResultType.BulkString
            && long.TryParse((string)result!, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedValue)
            && parsedValue is >= 0 and <= int.MaxValue)
        {
            docCount = (int)parsedValue;
            return true;
        }

        docCount = 0;
        return false;
    }

    /// <summary>Attempts to read the configured vector dimensions for the specified attribute from a raw <c>FT.INFO</c> response.</summary>
    /// <param name="raw">The raw response.</param>
    /// <param name="attributeName">The vector attribute identifier.</param>
    /// <param name="dimensions">The parsed vector dimensions.</param>
    /// <returns><see langword="true"/> when dimensions were found.</returns>
    public static bool TryGetVectorDimensions(RedisResult raw, string attributeName, out int dimensions)
    {
        dimensions = 0;

        RedisResult[]? attributes = TryGetTopLevelArray(raw, "attributes");
        if (attributes is null)
        {
            return false;
        }

        foreach (RedisResult attribute in attributes)
        {
            Dictionary<string, string> values = ParseKeyValuePairs(attribute);
            bool nameMatches =
                (values.TryGetValue("identifier", out string? identifier)
                    && string.Equals(identifier, attributeName, StringComparison.OrdinalIgnoreCase))
                || (values.TryGetValue("attribute", out string? alias)
                    && string.Equals(alias, attributeName, StringComparison.OrdinalIgnoreCase));

            if (!nameMatches
                || !values.TryGetValue("type", out string? type)
                || !string.Equals(type, "VECTOR", StringComparison.OrdinalIgnoreCase)
                || !values.TryGetValue("dim", out string? dimensionText)
                || !int.TryParse(dimensionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedDimensions))
            {
                continue;
            }

            dimensions = parsedDimensions;
            return true;
        }

        return false;
    }

    /// <summary>Gets the configured key prefixes from a raw <c>FT.INFO</c> response.</summary>
    /// <param name="raw">The raw response.</param>
    /// <returns>The configured index prefixes.</returns>
    public static IReadOnlyList<string> GetIndexPrefixes(RedisResult raw)
    {
        RedisResult[]? definition = TryGetTopLevelArray(raw, "index_definition");
        if (definition is null)
        {
            return [];
        }

        List<string> prefixes = [];
        for (int i = 0; i < definition.Length - 1; i += 2)
        {
            string? key = definition[i].ToString();
            if (!string.Equals(key, "prefixes", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(key, "prefix", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            RedisResult[]? values = TryGetArray(definition[i + 1]);
            if (values is null)
            {
                string? singlePrefix = definition[i + 1].ToString();
                if (!string.IsNullOrWhiteSpace(singlePrefix))
                {
                    prefixes.Add(singlePrefix);
                }

                continue;
            }

            prefixes.AddRange(values.Select(v => v.ToString()).Where(v => !string.IsNullOrWhiteSpace(v))!);
        }

        return prefixes;
    }

    /// <summary>Attempts a safe in-place index upgrade for a single missing TAG field.
    /// Returns <see langword="true"/> only when the requested field is the sole schema difference and the
    /// alter command was issued successfully.</summary>
    /// <param name="db">The Redis database connection.</param>
    /// <param name="indexName">The index to alter.</param>
    /// <param name="actualFields">The fields currently present on the index.</param>
    /// <param name="expectedFields">The fields the index should expose.</param>
    /// <param name="fieldName">The TAG field to add.</param>
    /// <returns><see langword="true"/> when the in-place upgrade ran.</returns>
    public static bool TryUpgradeMissingTagField(
        IDatabase db,
        string indexName,
        IReadOnlySet<string> actualFields,
        IReadOnlyCollection<string> expectedFields,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        ArgumentNullException.ThrowIfNull(actualFields);
        ArgumentNullException.ThrowIfNull(expectedFields);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

        HashSet<string> expected = new(expectedFields, StringComparer.OrdinalIgnoreCase);
        if (!expected.Contains(fieldName) || actualFields.Contains(fieldName))
        {
            return false;
        }

        if (actualFields.Any(field => !expected.Contains(field)))
        {
            return false;
        }

        List<string> missing = expected.Where(field => !actualFields.Contains(field)).ToList();
        if (missing.Count != 1 || !string.Equals(missing[0], fieldName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        db.Execute("FT.ALTER", indexName, "SCHEMA", "ADD", fieldName, "TAG");
        return true;
    }

    /// <summary>Attempts a safe in-place index upgrade for known missing TAG fields.</summary>
    /// <param name="db">The Redis database connection.</param>
    /// <param name="indexName">The index to alter.</param>
    /// <param name="actualFields">The fields currently present on the index.</param>
    /// <param name="expectedFields">The fields the index should expose.</param>
    /// <param name="fieldNames">The TAG fields that are allowed to be added in place.</param>
    /// <returns>The field names that were added.</returns>
    public static IReadOnlyList<string> TryUpgradeMissingTagFields(
        IDatabase db,
        string indexName,
        IReadOnlySet<string> actualFields,
        IReadOnlyCollection<string> expectedFields,
        IReadOnlyCollection<string> fieldNames)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        ArgumentNullException.ThrowIfNull(actualFields);
        ArgumentNullException.ThrowIfNull(expectedFields);
        ArgumentNullException.ThrowIfNull(fieldNames);

        HashSet<string> expected = new(expectedFields, StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expected.Contains(field)))
        {
            return [];
        }

        HashSet<string> allowed = new(fieldNames, StringComparer.OrdinalIgnoreCase);
        List<string> missing = expected.Where(field => !actualFields.Contains(field)).ToList();
        if (missing.Count == 0 || missing.Any(field => !allowed.Contains(field)))
        {
            return [];
        }

        List<string> added = [];
        foreach (string fieldName in fieldNames)
        {
            if (!missing.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            db.Execute("FT.ALTER", indexName, "SCHEMA", "ADD", fieldName, "TAG");
            added.Add(fieldName);
        }

        return added;
    }

    private static Dictionary<string, string> ParseKeyValuePairs(RedisResult raw)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        RedisResult[]? items = TryGetArray(raw);
        if (items is null || items.Length < 2 || items.Length % 2 != 0)
        {
            return values;
        }

        for (int i = 0; i < items.Length; i += 2)
        {
            string? key = items[i].ToString();
            string? value = items[i + 1].ToString();

            if (!string.IsNullOrWhiteSpace(key) && value is not null)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static RedisResult? TryGetTopLevelValue(RedisResult raw, string key)
    {
        RedisResult[]? items = TryGetArray(raw);
        if (items is null)
        {
            return null;
        }

        for (int i = 0; i < items.Length - 1; i += 2)
        {
            if (string.Equals(items[i].ToString(), key, StringComparison.OrdinalIgnoreCase))
            {
                return items[i + 1];
            }
        }

        return null;
    }

    private static RedisResult[]? TryGetTopLevelArray(RedisResult raw, string key)
        => TryGetTopLevelValue(raw, key) is RedisResult value ? TryGetArray(value) : null;

    private static RedisResult[]? TryGetArray(RedisResult raw)
    {
        try
        {
            return (RedisResult[]?)raw;
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }
}
