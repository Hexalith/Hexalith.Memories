// <copyright file="BenchmarkSeeder.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Infrastructure;

using System.Runtime.InteropServices;

using Hexalith.Memories.Benchmarks.Models;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;

using NFalkorDB;

using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;

using StackExchange.Redis;

/// <summary>
/// Seeds benchmark corpus data into Redis (syntactic + semantic indexes) and FalkorDB (graph).
/// Idempotent — calling twice produces the same indexed state via MERGE operations and index-exists handling.
/// </summary>
internal sealed class BenchmarkSeeder
{
    private const int VectorDimensions = 768;
    private static readonly DateTimeOffset SeedTimestamp = new(2026, 3, 15, 8, 0, 0, TimeSpan.Zero);

    internal static DateTimeOffset BenchmarkSeedTimestamp => SeedTimestamp;

    /// <summary>Seeds all benchmark data into the specified backends.</summary>
    internal static async Task SeedAsync(
        BenchmarkCorpus corpus,
        IConnectionMultiplexer redis,
        IConnectionMultiplexer falkorDb,
        string tenantId)
    {
        string caseId = GetSharedCaseId(corpus);

        await SeedRedisSyntacticAsync(corpus, redis, tenantId).ConfigureAwait(false);
        await SeedRedisSemanticAsync(corpus, redis, tenantId).ConfigureAwait(false);
        await SeedFalkorDbGraphAsync(corpus, falkorDb, tenantId, caseId).ConfigureAwait(false);
        await VerifySeededCountsAsync(corpus, redis, falkorDb, tenantId).ConfigureAwait(false);
    }

    internal static string GetSharedCaseId(BenchmarkCorpus corpus)
    {
        ArgumentNullException.ThrowIfNull(corpus);

        if (corpus.MemoryUnits.Count == 0)
        {
            throw new InvalidOperationException("Benchmark corpus must contain at least one memory unit before seeding.");
        }

        string caseId = corpus.MemoryUnits[0].CaseId;
        if (string.IsNullOrWhiteSpace(caseId))
        {
            throw new InvalidOperationException("Benchmark corpus memory units must declare a non-empty CaseId.");
        }

        foreach (BenchmarkMemoryUnit memoryUnit in corpus.MemoryUnits)
        {
            if (!string.Equals(memoryUnit.CaseId, caseId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Benchmark seeding expects all memory units to share a single CaseId so containment edges remain deterministic.");
            }
        }

        return caseId;
    }

    private static async Task SeedRedisSyntacticAsync(BenchmarkCorpus corpus, IConnectionMultiplexer redis, string tenantId)
    {
        IDatabase db = redis.GetDatabase();
        var ft = db.FT();
        string indexName = $"{tenantId}:memories:idx";

        // Create RediSearch index
        try
        {
            ft.Create(
                indexName,
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix($"{tenantId}:mu:"),
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
            // Idempotent — index already created
        }

        // Seed each memory unit as a hash
        foreach (BenchmarkMemoryUnit mu in corpus.MemoryUnits)
        {
            string hashKey = $"{tenantId}:mu:{mu.Id}";
            string sourceType = ToCamelCase(mu.SourceType);
            string ingestedAt = SeedTimestamp.ToString("o");

            await db.HashSetAsync(
                hashKey,
                [
                    new HashEntry("content", mu.Content),
                    new HashEntry("sourceUri", mu.SourceUri),
                    new HashEntry("sourceUriText", mu.SourceUri),
                    new HashEntry("sourceType", sourceType),
                    new HashEntry("sourceTypeText", sourceType),
                    new HashEntry("metadataText", string.Empty),
                    new HashEntry("metadataJson", "{}"),
                    new HashEntry("contentHash", $"hash-{mu.Id}"),
                    new HashEntry("caseId", mu.CaseId),
                    new HashEntry("embeddingProvider", "benchmark-synthetic"),
                    new HashEntry("ingestedBy", "benchmark-seeder"),
                    new HashEntry("ingestedAt", ingestedAt),
                    new HashEntry("lastUpdated", ingestedAt),
                ]).ConfigureAwait(false);
        }
    }

    private static async Task SeedRedisSemanticAsync(BenchmarkCorpus corpus, IConnectionMultiplexer redis, string tenantId)
    {
        IDatabase db = redis.GetDatabase();
        var ft = db.FT();
        string indexName = $"{tenantId}:memories:vec";

        // Create vector index
        try
        {
            ft.Create(
                indexName,
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix($"{tenantId}:vec:"),
                new Schema()
                    .AddVectorField(
                        "embedding",
                        Schema.VectorField.VectorAlgo.FLAT,
                        new Dictionary<string, object>()
                        {
                            ["TYPE"] = "FLOAT32",
                            ["DIM"] = VectorDimensions.ToString(),
                            ["DISTANCE_METRIC"] = "COSINE",
                        })
                    .AddTagField("memoryUnitId")
                    .AddTagField("caseId"));
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists"))
        {
            // Idempotent — index already created
        }

        // Seed each memory unit's vector
        foreach (BenchmarkMemoryUnit mu in corpus.MemoryUnits)
        {
            string hashKey = $"{tenantId}:vec:{mu.Id}";
            byte[] vectorBytes = MemoryMarshal.AsBytes(mu.Vector.AsSpan()).ToArray();

            await db.HashSetAsync(
                hashKey,
                [
                    new HashEntry("embedding", vectorBytes),
                    new HashEntry("memoryUnitId", mu.Id),
                    new HashEntry("caseId", mu.CaseId),
                ]).ConfigureAwait(false);
        }
    }

    private static async Task SeedFalkorDbGraphAsync(
        BenchmarkCorpus corpus,
        IConnectionMultiplexer falkorDb,
        string tenantId,
        string caseId)
    {
        FalkorDB falkor = new(falkorDb.GetDatabase());
        GraphQueryBuilder builder = new();

        // Create case node (all memory units share one case)
        (string caseQuery, IDictionary<string, object> caseParams) = builder.BuildMergeCaseNode(caseId);
        await falkor.QueryAsync(tenantId, caseQuery, caseParams).ConfigureAwait(false);

        // Create memory unit nodes
        foreach (BenchmarkMemoryUnit mu in corpus.MemoryUnits)
        {
            (string nodeQuery, IDictionary<string, object> nodeParams) = builder.BuildMergeMemoryUnitNode(
                mu.Id,
                mu.CaseId,
                mu.Content,
                $"hash-{mu.Id}",
                mu.SourceUri,
                mu.SourceType,
                "benchmark-synthetic",
                VectorDimensions,
                "benchmark-seeder",
                SeedTimestamp,
                "{}");
            await falkor.QueryAsync(tenantId, nodeQuery, nodeParams).ConfigureAwait(false);

            // Contains edge: Case → MemoryUnit
            (string containsQuery, IDictionary<string, object> containsParams) = builder.BuildMergeEdge(
                caseId, mu.Id, EdgeType.Contains, EdgeTypeDefaults.Contains, EdgeOrigin.Explicit);
            await falkor.QueryAsync(tenantId, containsQuery, containsParams).ConfigureAwait(false);
        }

        // Create relationship edges
        foreach (BenchmarkEdge edge in corpus.Edges)
        {
            (string edgeQuery, IDictionary<string, object> edgeParams) = builder.BuildMergeEdge(
                edge.SourceId, edge.TargetId, edge.EdgeType, edge.Confidence, edge.Origin);
            await falkor.QueryAsync(tenantId, edgeQuery, edgeParams).ConfigureAwait(false);
        }
    }

    private static async Task VerifySeededCountsAsync(
        BenchmarkCorpus corpus,
        IConnectionMultiplexer redis,
        IConnectionMultiplexer falkorDb,
        string tenantId)
    {
        IDatabase db = redis.GetDatabase();
        int expectedCount = corpus.MemoryUnits.Count;

        // Verify syntactic index document count via FT.SEARCH count query
        var ft = db.FT();
        NRedisStack.Search.SearchResult syntacticResult = await ft.SearchAsync(
            $"{tenantId}:memories:idx",
            new Query("*").Dialect(2).Limit(0, 0)).ConfigureAwait(false);
        if (syntacticResult.TotalResults != expectedCount)
        {
            throw new InvalidOperationException(
                $"RediSearch index has {syntacticResult.TotalResults} documents, expected {expectedCount}. Seeding may have failed silently.");
        }

        // Verify vector index document count via FT.SEARCH count query
        NRedisStack.Search.SearchResult vectorResult = await ft.SearchAsync(
            $"{tenantId}:memories:vec",
            new Query("*").Dialect(2).Limit(0, 0)).ConfigureAwait(false);
        if (vectorResult.TotalResults != expectedCount)
        {
            throw new InvalidOperationException(
                $"Redis Vector index has {vectorResult.TotalResults} documents, expected {expectedCount}. Seeding may have failed silently.");
        }

        // Verify FalkorDB node count
        FalkorDB falkor = new(falkorDb.GetDatabase());
        GraphQueryBuilder builder = new();
        (string countQuery, IDictionary<string, object> countParams) = builder.BuildCountMemoryUnits();
        ResultSet result = await falkor.QueryAsync(tenantId, countQuery, countParams).ConfigureAwait(false);

        long graphCount = 0;
        foreach (Record record in result)
        {
            graphCount = record.GetValue<long>("count");
        }

        if (graphCount != expectedCount)
        {
            throw new InvalidOperationException(
                $"FalkorDB graph has {graphCount} MemoryUnit nodes, expected {expectedCount}. Seeding may have failed silently.");
        }
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
