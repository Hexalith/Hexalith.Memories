// <copyright file="BenchmarkFixture.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks.Fixtures;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Hexalith.Memories.Benchmarks.Data;
using Hexalith.Memories.Benchmarks.Infrastructure;
using Hexalith.Memories.Benchmarks.Models;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.Logging.Abstractions;

using StackExchange.Redis;

/// <summary>
/// Benchmark fixture providing Redis Stack + FalkorDB containers, seeded corpus data, and corpus statistics.
/// Implements IAsyncLifetime for xUnit async setup/teardown.
/// </summary>
public sealed class BenchmarkFixture : IAsyncLifetime
{
    private const string FalkorDbImage = "falkordb/falkordb:latest@sha256:4b7c79901ad409a39655f049b772adbc499b92ee2e01db80c3502572444df84d";
    private const string RedisStackImage = "redis/redis-stack:latest@sha256:880df9c228597cb0d15b585f39a4327d6ee2d8b0d0f155e3f75dba9a761d4ec3";
    private const string TenantId = "benchmark-tenant";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private bool _disposed;
    private IContainer? _falkorDbContainer;
    private IContainer? _redisStackContainer;

    /// <summary>Gets the Redis connection (for syntactic + semantic search).</summary>
    public IConnectionMultiplexer Redis { get; private set; } = null!;

    /// <summary>Gets the FalkorDB connection (for graph traversal).</summary>
    public IConnectionMultiplexer FalkorDb { get; private set; } = null!;

    /// <summary>Gets the loaded benchmark corpus.</summary>
    public BenchmarkCorpus Corpus { get; private set; } = null!;

    /// <summary>Gets the loaded ground truth queries.</summary>
    public IReadOnlyList<BenchmarkQuery> GroundTruth { get; private set; } = null!;

    /// <summary>Gets the pre-computed corpus statistics for BM25 normalization.</summary>
    public CorpusStatistics CorpusStats { get; private set; } = null!;

    /// <summary>Gets the benchmark tenant ID.</summary>
    public string BenchmarkTenantId => TenantId;

    private BenchmarkSuiteResult? _cachedBenchmarkResult;

    /// <summary>Gets or sets the cached benchmark suite result for this shared test collection.</summary>
    public BenchmarkSuiteResult? CachedBenchmarkResult
    {
        get => Volatile.Read(ref _cachedBenchmarkResult);
        set => Volatile.Write(ref _cachedBenchmarkResult, value);
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        try
        {
            // Start both containers in parallel
            _falkorDbContainer = new ContainerBuilder(FalkorDbImage)
                .WithPortBinding(0, 6379)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilInternalTcpPortIsAvailable(6379)
                        .UntilCommandIsCompleted("redis-cli", "PING"))
                .Build();

            _redisStackContainer = new ContainerBuilder(RedisStackImage)
                .WithPortBinding(0, 6379)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilInternalTcpPortIsAvailable(6379)
                        .UntilCommandIsCompleted("redis-cli", "PING"))
                .Build();

            await Task.WhenAll(
                _falkorDbContainer.StartAsync(),
                _redisStackContainer.StartAsync()).WaitAsync(StartupTimeout).ConfigureAwait(false);

            FalkorDb = await ConnectionMultiplexer.ConnectAsync(
                $"localhost:{_falkorDbContainer.GetMappedPublicPort(6379)}").ConfigureAwait(false);
            Redis = await ConnectionMultiplexer.ConnectAsync(
                $"localhost:{_redisStackContainer.GetMappedPublicPort(6379)}").ConfigureAwait(false);

            // Load and validate corpus + ground truth
            Corpus = BenchmarkCorpusLoader.LoadCorpus();
            GroundTruth = BenchmarkCorpusLoader.LoadGroundTruth();
            BenchmarkCorpusLoader.CrossValidate(Corpus, GroundTruth);

            // Seed all data into backends
            await BenchmarkSeeder.SeedAsync(Corpus, Redis, FalkorDb, TenantId).ConfigureAwait(false);

            // Compute corpus statistics from the seeded RediSearch index using the production parser.
            CorpusStats = await LoadCorpusStatisticsAsync().ConfigureAwait(false);

            // Smoke test: verify syntactic search works
            await VerifySyntacticSearchSmokeTestAsync().ConfigureAwait(false);
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Redis is not null)
        {
            await Redis.CloseAsync().ConfigureAwait(false);
            Redis.Dispose();
        }

        if (FalkorDb is not null)
        {
            await FalkorDb.CloseAsync().ConfigureAwait(false);
            FalkorDb.Dispose();
        }

        if (_falkorDbContainer is not null)
        {
            await _falkorDbContainer.StopAsync().ConfigureAwait(false);
            await _falkorDbContainer.DisposeAsync().ConfigureAwait(false);
        }

        if (_redisStackContainer is not null)
        {
            await _redisStackContainer.StopAsync().ConfigureAwait(false);
            await _redisStackContainer.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads corpus statistics from the seeded RediSearch index using the same FT.INFO parser as production.
    /// </summary>
    private async Task<CorpusStatistics> LoadCorpusStatisticsAsync()
    {
        IDatabase db = Redis.GetDatabase();
        RedisResult raw = await db.ExecuteAsync("FT.INFO", $"{TenantId}:memories:idx").ConfigureAwait(false);
        return CorpusStatisticsActor.ParseFtInfoResult(raw, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Smoke test: search for a term that appears in exactly one document to verify index is functional.
    /// Catches schema mismatches that would silently produce all-zero NDCG scores.
    /// </summary>
    private async Task VerifySyntacticSearchSmokeTestAsync()
    {
        SyntacticSearchService service = new(Redis, NullLogger<SyntacticSearchService>.Instance);
        SearchResult result = await service.SearchAsync(new SearchQuery
        {
            TenantId = TenantId,
            Query = "invoice",
            MaxResults = 5,
        }).ConfigureAwait(false);

        if (result.TotalCount != 1 || result.Results.Count != 1)
        {
            throw new InvalidOperationException(
                $"Smoke test failed: expected exactly one result for 'invoice' but found {result.TotalCount}. " +
                "The benchmark search path may have schema mismatches, broken query construction, or failed seeding.");
        }

        if (!string.Equals(result.Results[0].MemoryUnitId, "mu-005", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Smoke test failed: expected 'mu-005' for the unique term 'invoice' but got '{result.Results[0].MemoryUnitId}'.");
        }

        if (!result.Results[0].ContentSnippet.Contains("invoice", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Smoke test failed: the expected invoice document snippet was not returned for 'invoice'.");
        }
    }
}

/// <summary>xUnit collection definition for benchmark tests sharing the BenchmarkFixture.</summary>
[CollectionDefinition("Benchmark")]
public class BenchmarkCollection : ICollectionFixture<BenchmarkFixture>;
