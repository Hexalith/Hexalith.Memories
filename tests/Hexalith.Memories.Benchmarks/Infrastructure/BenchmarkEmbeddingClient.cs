// <copyright file="BenchmarkEmbeddingClient.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Infrastructure;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using NSubstitute;

/// <summary>
/// Deterministic benchmark embedding client that maps fixed benchmark queries into the same
/// synthetic cluster space used by the benchmark corpus generator.
/// </summary>
internal sealed class BenchmarkEmbeddingClient : EmbeddingClient
{
    private const int ClusterCount = 7;
    private const int Seed = 42;

    private static readonly float[][] s_clusterBaseVectors = CreateClusterBaseVectors();

    /// <summary>Initializes a new instance of the <see cref="BenchmarkEmbeddingClient"/> class.</summary>
    internal BenchmarkEmbeddingClient()
        : base(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<DaprClient>(),
            CreateConfiguration(),
            CreateHostEnvironment())
    {
    }

    /// <inheritdoc/>
    public override Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);
        EmbeddingProviderDefaults.Validate(config);

        int[] clusters = ResolveQueryClusters(NormalizeQuery(text));
        return Task.FromResult(CreateCentroidVector(config.Dimensions, clusters));
    }

    private static float[] CreateCentroidVector(int dimensions, IReadOnlyList<int> clusters)
    {
        float[] vector = new float[dimensions];

        foreach (int cluster in clusters)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(cluster);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(cluster, ClusterCount);

            float[] baseVector = s_clusterBaseVectors[cluster];
            int limit = Math.Min(dimensions, baseVector.Length);
            for (int i = 0; i < limit; i++)
            {
                vector[i] += baseVector[i];
            }
        }

        Normalize(vector);
        return vector;
    }

    private static float[][] CreateClusterBaseVectors()
    {
        Random rng = new(Seed);
        float[][] baseVectors = new float[ClusterCount][];

        for (int cluster = 0; cluster < ClusterCount; cluster++)
        {
            float[] vector = new float[768];
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] = (float)(rng.NextDouble() * 2.0d) - 1.0f;
            }

            Normalize(vector);
            baseVectors[cluster] = vector;
        }

        return baseVectors;
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Memories:Testing:UseFakeEmbedding"] = "true",
            })
            .Build();

    private static IHostEnvironment CreateHostEnvironment()
    {
        IHostEnvironment hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns("Development");
        return hostEnvironment;
    }

    private static void Normalize(float[] vector)
    {
        double norm = 0.0d;
        for (int i = 0; i < vector.Length; i++)
        {
            norm += vector[i] * (double)vector[i];
        }

        norm = Math.Sqrt(norm);
        if (norm <= 0.0d)
        {
            return;
        }

        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / norm);
        }
    }

    private static string NormalizeQuery(string text)
        => string.Join(' ', text.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();

    private static int[] ResolveQueryClusters(string normalizedQuery)
        => normalizedQuery switch
        {
            "what caused the payment processing outage in march?" or
            "march payment processing outage root cause" => [0, 0, 1, 3, 3, 6],

            "find all discussions related to the api redesign decision" or
            "api redesign decision discussions" => [2, 2, 2, 5, 5],

            "what were the consequences of the database migration?" or
            "database migration consequences after the march incident" => [0, 2, 3, 3, 3, 3],

            "how did the march deployment affect transaction success rates?" or
            "march deployment impact on transaction success rates" => [0, 0, 0, 1, 1, 1, 3],

            "what remediation actions were taken after the incident?" or
            "incident remediation actions and follow-up" => [1, 2, 5, 6, 6, 6],

            "what monitoring detected the connection pool exhaustion?" or
            "connection pool exhaustion monitoring alerts" => [0, 3, 4, 4, 4],

            "what code review issues led to the production incident?" or
            "code review issues before the production incident" => [0, 1, 3, 3, 5, 5],

            "show the investigation findings about database timeout root cause" or
            "database timeout root cause investigation findings" => [0, 0, 3, 3, 3, 3],

            _ => throw new InvalidOperationException(
                $"No deterministic benchmark embedding mapping exists for query '{normalizedQuery}'. Update {nameof(BenchmarkEmbeddingClient)} when adding new benchmark queries."),
        };
}
