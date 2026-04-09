// <copyright file="BenchmarkCorpusLoader.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Data;

using System.Reflection;
using System.Text.Json;

using Hexalith.Memories.Benchmarks.Models;

/// <summary>
/// Loads and validates benchmark corpus and ground truth data from embedded JSON resources.
/// </summary>
internal static class BenchmarkCorpusLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Loads and validates the synthetic benchmark corpus from embedded resources.</summary>
    /// <returns>The validated benchmark corpus.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    internal static BenchmarkCorpus LoadCorpus()
    {
        string json = ReadEmbeddedResource("Hexalith.Memories.Benchmarks.Data.synthetic-corpus.json");
        BenchmarkCorpus corpus = JsonSerializer.Deserialize<BenchmarkCorpus>(json, s_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize synthetic-corpus.json.");

        ValidateCorpus(corpus);
        return corpus;
    }

    /// <summary>Loads and validates the ground truth queries from embedded resources.</summary>
    /// <returns>The validated list of benchmark queries.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    internal static IReadOnlyList<BenchmarkQuery> LoadGroundTruth()
    {
        string json = ReadEmbeddedResource("Hexalith.Memories.Benchmarks.Data.ground-truth.json");
        List<BenchmarkQuery> queries = JsonSerializer.Deserialize<List<BenchmarkQuery>>(json, s_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize ground-truth.json.");

        ValidateGroundTruth(queries);
        return queries;
    }

    /// <summary>
    /// Cross-validates ground truth against corpus — all document IDs in expected results must exist in corpus.
    /// </summary>
    /// <param name="corpus">The loaded corpus.</param>
    /// <param name="queries">The loaded ground truth queries.</param>
    /// <exception cref="InvalidOperationException">Thrown when phantom document IDs are found.</exception>
    internal static void CrossValidate(BenchmarkCorpus corpus, IReadOnlyList<BenchmarkQuery> queries)
    {
        HashSet<string> corpusIds = new(corpus.MemoryUnits.Select(mu => mu.Id), StringComparer.Ordinal);

        foreach (BenchmarkQuery query in queries)
        {
            foreach (string docId in query.ExpectedResults)
            {
                if (!corpusIds.Contains(docId))
                {
                    throw new InvalidOperationException(
                        $"Ground truth query '{query.QueryId}' references document '{docId}' which does not exist in the corpus. " +
                        "Phantom document IDs silently penalize NDCG scores.");
                }
            }

            if (query.GraphStartNodeId is not null && !corpusIds.Contains(query.GraphStartNodeId))
            {
                throw new InvalidOperationException(
                    $"Ground truth query '{query.QueryId}' has GraphStartNodeId '{query.GraphStartNodeId}' which does not exist in the corpus.");
            }
        }
    }

    private static void ValidateCorpus(BenchmarkCorpus corpus)
    {
        foreach (BenchmarkMemoryUnit mu in corpus.MemoryUnits)
        {
            if (string.IsNullOrWhiteSpace(mu.Id))
            {
                throw new InvalidOperationException("Corpus contains a memory unit with empty Id.");
            }

            if (string.IsNullOrWhiteSpace(mu.Content))
            {
                throw new InvalidOperationException($"Memory unit '{mu.Id}' has empty Content.");
            }

            if (mu.Vector.Length != 768)
            {
                throw new InvalidOperationException(
                    $"Memory unit '{mu.Id}' has vector of length {mu.Vector.Length}, expected 768.");
            }

            bool allZero = true;
            for (int i = 0; i < mu.Vector.Length; i++)
            {
                if (mu.Vector[i] != 0.0f)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero)
            {
                throw new InvalidOperationException(
                    $"Memory unit '{mu.Id}' has an all-zero vector. Zero vectors have undefined cosine similarity and can cause NaN in distance computations.");
            }
        }
    }

    private static void ValidateGroundTruth(List<BenchmarkQuery> queries)
    {
        foreach (BenchmarkQuery query in queries)
        {
            if (string.IsNullOrWhiteSpace(query.QueryId))
            {
                throw new InvalidOperationException("Ground truth contains a query with empty QueryId.");
            }

            if (string.IsNullOrWhiteSpace(query.Query))
            {
                throw new InvalidOperationException($"Query '{query.QueryId}' has empty Query text.");
            }

            if (query.ExpectedResults.Count < 3)
            {
                throw new InvalidOperationException(
                    $"Query '{query.QueryId}' has {query.ExpectedResults.Count} expected results, minimum is 3 for meaningful NDCG@10 discrimination.");
            }
        }
    }

    private static string ReadEmbeddedResource(string resourceName)
    {
        Assembly assembly = typeof(BenchmarkCorpusLoader).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
