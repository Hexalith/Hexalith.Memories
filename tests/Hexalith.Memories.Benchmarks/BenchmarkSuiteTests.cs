// <copyright file="BenchmarkSuiteTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Benchmarks;

using Hexalith.Memories.Benchmarks.Fixtures;
using Hexalith.Memories.Benchmarks.Infrastructure;
using Hexalith.Memories.Benchmarks.Models;
using Hexalith.Memories.Benchmarks.Reporting;
using Hexalith.Memories.Benchmarks.Scoring;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.Logging.Abstractions;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

using Xunit;

/// <summary>
/// Integration tests that run the benchmark suite against real Redis Stack + FalkorDB backends
/// to validate the three-axis thesis.
/// </summary>
[Collection("Benchmark")]
[Trait("Category", "Benchmark")]
[Trait("Category", "Integration")]
public class BenchmarkSuiteTests
{
    private const string Caveat =
        "Results use synthetic pre-computed vectors, not real embeddings. " +
        "This validates fusion algorithm correctness, not production search quality. " +
        "Real-world validation with actual embeddings is planned for Phase 1.5.";
    private static readonly TimeSpan BenchmarkTimeout = TimeSpan.FromMinutes(5);

    private readonly BenchmarkFixture _fixture;
    private readonly TenantEmbeddingConfig _embeddingConfig;
    private readonly ITestOutputHelper _output;
    private readonly SemanticSearchService _semanticService;

    public BenchmarkSuiteTests(BenchmarkFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _embeddingConfig = EmbeddingProviderDefaults.Google();
        _output = output;
        _semanticService = new SemanticSearchService(
            _fixture.Redis,
            new BenchmarkEmbeddingClient(),
            NullLogger<SemanticSearchService>.Instance);
    }

    [Fact]
    public async Task RunBenchmarkSuite_ProducesValidResults()
    {
        // Arrange & Act
        BenchmarkSuiteResult result = await GetOrRunBenchmarkAsync().WaitAsync(BenchmarkTimeout);

        // Log human-readable report
        string report = BenchmarkReporter.FormatConsoleReport(result);
        _output.WriteLine(report);

        // Assert infrastructure correctness (should always pass)
        result.QueryResults.Count.ShouldBe(_fixture.GroundTruth.Count);

        foreach (BenchmarkQueryResult qr in result.QueryResults)
        {
            qr.HybridNdcg10.ShouldBeInRange(0.0, 1.0, $"Hybrid NDCG@10 out of range for {qr.QueryId}");
            qr.SyntacticNdcg10.ShouldBeInRange(0.0, 1.0, $"Syntactic NDCG@10 out of range for {qr.QueryId}");
            qr.SemanticNdcg10.ShouldBeInRange(0.0, 1.0, $"Semantic NDCG@10 out of range for {qr.QueryId}");
            qr.GraphNdcg10.ShouldBeInRange(0.0, 1.0, $"Graph NDCG@10 out of range for {qr.QueryId}");
            qr.HybridPrecisionAt3.ShouldBeInRange(0.0, 1.0, $"Hybrid P@3 out of range for {qr.QueryId}");
            qr.BestSingleAxisPrecisionAt3.ShouldBeInRange(0.0, 1.0, $"Best single P@3 out of range for {qr.QueryId}");
        }

        System.IO.File.Exists(GetBenchmarkResultsOutputPath()).ShouldBeTrue("benchmark-results.json should be written");
        result.TotalQueries.ShouldBe(_fixture.GroundTruth.Count);
        result.Caveat.ShouldBe(Caveat);
    }

    [Fact]
    [Trait("Category", "ThesisValidation")]
    public async Task ThesisValidation_HybridOutperforms80Percent()
    {
        BenchmarkSuiteResult result = await GetOrRunBenchmarkAsync().WaitAsync(BenchmarkTimeout);

        // Log results for diagnostic visibility
        string report = BenchmarkReporter.FormatConsoleReport(result);
        _output.WriteLine(report);

        System.IO.File.Exists(GetBenchmarkResultsOutputPath()).ShouldBeTrue("benchmark-results.json should be written");

        result.ThesisValidated.ShouldBeTrue($"Hybrid win rate: {result.HybridWinRate:P0} ({result.HybridWins}/{result.TotalQueries})");
    }

    [Fact]
    [Trait("Category", "Reproducibility")]
    public async Task ReproducibilityTest_SameDatasetProducesIdenticalScores()
    {
        // Run benchmark twice against same seeded data
        BenchmarkSuiteResult run1 = await RunFullBenchmarkAsync().WaitAsync(BenchmarkTimeout);
        BenchmarkSuiteResult run2 = await RunFullBenchmarkAsync().WaitAsync(BenchmarkTimeout);

        // Assert identical results between runs (NFR26)
        run1.QueryResults.Count.ShouldBe(run2.QueryResults.Count);

        for (int i = 0; i < run1.QueryResults.Count; i++)
        {
            BenchmarkQueryResult r1 = run1.QueryResults[i];
            BenchmarkQueryResult r2 = run2.QueryResults[i];

            r1.QueryId.ShouldBe(r2.QueryId, $"Query order mismatch at index {i}");
            r1.HybridNdcg10.ShouldBe(r2.HybridNdcg10, $"HybridNdcg10 differs for {r1.QueryId}");
            r1.SyntacticNdcg10.ShouldBe(r2.SyntacticNdcg10, $"SyntacticNdcg10 differs for {r1.QueryId}");
            r1.SemanticNdcg10.ShouldBe(r2.SemanticNdcg10, $"SemanticNdcg10 differs for {r1.QueryId}");
            r1.GraphNdcg10.ShouldBe(r2.GraphNdcg10, $"GraphNdcg10 differs for {r1.QueryId}");
            r1.HybridOutperforms.ShouldBe(r2.HybridOutperforms, $"HybridOutperforms differs for {r1.QueryId}");
        }

        run1.ThesisValidated.ShouldBe(run2.ThesisValidated, "ThesisValidated should be identical between runs");
    }

    private async Task<BenchmarkSuiteResult> RunFullBenchmarkAsync()
    {
        string tenantId = _fixture.BenchmarkTenantId;
        SyntacticSearchService syntacticService = new(_fixture.Redis, NullLogger<SyntacticSearchService>.Instance);

        List<BenchmarkQueryResult> queryResults = [];

        foreach (BenchmarkQuery bq in _fixture.GroundTruth)
        {
            // Execute all axes in parallel
            Task<IReadOnlyList<ScoredResult>> syntacticTask = ExecuteSyntacticSearchAsync(syntacticService, tenantId, bq.Query);
            Task<IReadOnlyList<ScoredResult>> semanticTask = ExecuteSemanticSearchAsync(tenantId, bq);
            Task<IReadOnlyList<ScoredResult>> graphTask = ExecuteGraphTraversalAsync(tenantId, bq.GraphStartNodeId);

            await Task.WhenAll(syntacticTask, semanticTask, graphTask).ConfigureAwait(false);

            IReadOnlyList<ScoredResult> syntacticResults = syntacticTask.Result;
            IReadOnlyList<ScoredResult> semanticResults = semanticTask.Result;
            IReadOnlyList<ScoredResult> graphResults = graphTask.Result;

            // Compose hybrid via FusionEngine.Fuse() directly
            IReadOnlyList<FusedScoredResult> hybridResults = FusionEngine.Fuse(
                syntacticResults,
                semanticResults,
                graphResults,
                new FusionWeights(),
                _fixture.CorpusStats.DocumentCount,
                _fixture.CorpusStats.AverageDocumentLength);

            // Extract ranked ID lists for scoring
            List<string> hybridRanked = hybridResults.Select(r => r.MemoryUnitId).ToList();
            List<string> syntacticRanked = syntacticResults.Select(r => r.MemoryUnitId).ToList();
            List<string> semanticRanked = semanticResults.Select(r => r.MemoryUnitId).ToList();
            List<string> graphRanked = graphResults.Select(r => r.MemoryUnitId).ToList();

            IReadOnlyList<string> expectedResults = bq.ExpectedResults;

            // Score each axis
            double hybridNdcg = NdcgScorer.ComputeNdcg(hybridRanked, expectedResults);
            double syntacticNdcg = NdcgScorer.ComputeNdcg(syntacticRanked, expectedResults);
            double semanticNdcg = NdcgScorer.ComputeNdcg(semanticRanked, expectedResults);
            bool graphActive = bq.GraphStartNodeId is not null;
            double graphNdcg = graphActive ? NdcgScorer.ComputeNdcg(graphRanked, expectedResults) : 0.0;

            // Precision@3
            double hybridP3 = NdcgScorer.ComputePrecisionAtK(hybridRanked, expectedResults, k: 3);
            double syntacticP3 = NdcgScorer.ComputePrecisionAtK(syntacticRanked, expectedResults, k: 3);
            double semanticP3 = NdcgScorer.ComputePrecisionAtK(semanticRanked, expectedResults, k: 3);
            double graphP3 = graphActive ? NdcgScorer.ComputePrecisionAtK(graphRanked, expectedResults, k: 3) : 0.0;

            // Determine best single axis (only active axes)
            double bestSingleNdcg = Math.Max(syntacticNdcg, semanticNdcg);
            double bestSingleP3 = Math.Max(syntacticP3, semanticP3);
            if (graphActive)
            {
                bestSingleNdcg = Math.Max(bestSingleNdcg, graphNdcg);
                bestSingleP3 = Math.Max(bestSingleP3, graphP3);
            }

            bool hybridOutperforms = hybridNdcg > bestSingleNdcg;

            queryResults.Add(new BenchmarkQueryResult
            {
                QueryId = bq.QueryId,
                QueryDescription = bq.Description,
                HybridNdcg10 = hybridNdcg,
                SyntacticNdcg10 = syntacticNdcg,
                SemanticNdcg10 = semanticNdcg,
                GraphNdcg10 = graphNdcg,
                GraphAxisActive = graphActive,
                HybridPrecisionAt3 = hybridP3,
                BestSingleAxisPrecisionAt3 = bestSingleP3,
                HybridOutperforms = hybridOutperforms,
            });
        }

        int hybridWins = queryResults.Count(qr => qr.HybridOutperforms);
        int totalQueries = queryResults.Count;
        double winRate = totalQueries > 0 ? (double)hybridWins / totalQueries : 0.0;

        return new BenchmarkSuiteResult
        {
            QueryResults = queryResults,
            TotalQueries = totalQueries,
            HybridWins = hybridWins,
            HybridWinRate = winRate,
            ThesisValidated = winRate >= 0.80,
            RunTimestamp = DateTimeOffset.UtcNow,
            Caveat = Caveat,
        };
    }

    private async Task<BenchmarkSuiteResult> GetOrRunBenchmarkAsync()
    {
        BenchmarkSuiteResult result;

        if (_fixture.CachedBenchmarkResult is not null)
        {
            result = _fixture.CachedBenchmarkResult;
        }
        else
        {
            result = await RunFullBenchmarkAsync().ConfigureAwait(false);
            _fixture.CachedBenchmarkResult = result;
        }

        WriteBenchmarkResultsArtifact(result);
        return result;
    }

    private static string GetBenchmarkResultsOutputPath()
        => System.IO.Path.Combine(AppContext.BaseDirectory, "benchmark-results.json");

    private static void WriteBenchmarkResultsArtifact(BenchmarkSuiteResult result)
        => BenchmarkReporter.WriteResults(result, GetBenchmarkResultsOutputPath());

    /// <summary>Executes syntactic (BM25) search via SyntacticSearchService.</summary>
    private static async Task<IReadOnlyList<ScoredResult>> ExecuteSyntacticSearchAsync(
        SyntacticSearchService service,
        string tenantId,
        string queryText)
    {
        SearchQuery query = new()
        {
            TenantId = tenantId,
            Query = queryText,
            MaxResults = 10,
        };

        Contracts.V1.SearchResult result = await service.SearchAsync(query).ConfigureAwait(false);
        return result.Results;
    }

    /// <summary>
    /// Executes semantic search through <see cref="SemanticSearchService"/> using deterministic benchmark query embeddings.
    /// </summary>
    private async Task<IReadOnlyList<ScoredResult>> ExecuteSemanticSearchAsync(
        string tenantId,
        BenchmarkQuery bq)
    {
        SearchResult result = await _semanticService.SearchAsync(
            new SearchQuery
            {
                TenantId = tenantId,
                Query = bq.Query,
                MaxResults = 10,
            },
            _embeddingConfig,
            CancellationToken.None).ConfigureAwait(false);

        return result.Results;
    }

    /// <summary>
    /// Executes graph-only scoring via FalkorDB traversal + hop-distance ranking.
    /// Uses pure graph signal — no syntactic/semantic mixing.
    /// </summary>
    private async Task<IReadOnlyList<ScoredResult>> ExecuteGraphTraversalAsync(
        string tenantId,
        string? graphStartNodeId)
    {
        if (graphStartNodeId is null)
        {
            return [];
        }

        FalkorDB falkor = new(_fixture.FalkorDb.GetDatabase());
        const string cypherQuery = "MATCH p = (start:MemoryUnit {id: $startId})-[:CAUSED_BY|CORRELATED_WITH|REFERENCES|ANNOTATES*0..10]-(n:MemoryUnit) RETURN DISTINCT n.id AS nodeId, min(length(p)) AS hopDistance";
        Dictionary<string, object> parameters = new()
        {
            ["startId"] = graphStartNodeId,
        };

        ResultSet resultSet;
        try
        {
            resultSet = await falkor.SelectGraph(tenantId).QueryAsync(cypherQuery, parameters).ConfigureAwait(false);
        }
        catch (RedisServerException ex)
        {
            throw new InvalidOperationException(
                $"Graph traversal failed for benchmark start node '{graphStartNodeId}'.",
                ex);
        }

        List<(string NodeId, int HopDistance)> traversedNodes = [];
        foreach (NFalkorDB.Record record in resultSet)
        {
            string nodeId = record.GetValue<string>("nodeId");
            long hopDistance = record.GetValue<long>("hopDistance");
            traversedNodes.Add((nodeId, (int)hopDistance));
        }

        // Enrich from Redis and build scored results ranked by hop distance
        IDatabase db = _fixture.Redis.GetDatabase();
        List<ScoredResult> results = [];

        foreach ((string nodeId, int hopDistance) in traversedNodes)
        {
            double score = ScoreNormalizer.NormalizeGraphProximity(hopDistance);

            RedisValue[] fields = await db.HashGetAsync(
                $"{tenantId}:mu:{nodeId}",
                [new RedisValue("content"), new RedisValue("sourceUri"), new RedisValue("sourceType")]).ConfigureAwait(false);

            if (!fields[0].HasValue)
            {
                continue;
            }

            string content = fields[0].ToString();
            string sourceUri = fields[1].HasValue ? fields[1].ToString() : string.Empty;
            string sourceTypeStr = fields[2].HasValue ? fields[2].ToString() : "file";
            SourceType sourceType = Enum.TryParse(sourceTypeStr, true, out SourceType st) ? st : SourceType.File;

            string snippet = content.Length > 200 ? content[..200] : content;

            results.Add(new ScoredResult
            {
                MemoryUnitId = nodeId,
                Score = score,
                ContentSnippet = snippet,
                SourceUri = sourceUri,
                SourceType = sourceType,
                Axis = "graph",
            });
        }

        // Sort by score descending (closest nodes first), then by ID for deterministic tie-breaking.
        results.Sort((a, b) =>
        {
            int scoreComparison = b.Score.CompareTo(a.Score);
            return scoreComparison != 0
                ? scoreComparison
                : StringComparer.Ordinal.Compare(a.MemoryUnitId, b.MemoryUnitId);
        });

        return results;
    }
}
