namespace Hexalith.Memories.Server.Tests.Search;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

public class HybridSearchServiceTests
{
    private static readonly FusionWeights DefaultWeights = new();
    private static readonly CorpusStatistics DefaultStats = new(1000, 200.0, DateTimeOffset.UtcNow);

    private static ScoredResult MakeResult(string id, double score, string axis)
        => new()
        {
            MemoryUnitId = id,
            Score = score,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            Axis = axis,
        };

    private static SearchResult MakeSearchResult(params ScoredResult[] results)
        => new()
        {
            Results = results,
            TotalCount = results.Length,
            HasIndexedMemoryUnits = true,
            Query = "test query",
        };

    private static SearchResult MakeSearchResult(IReadOnlyList<ScoredResult> results, long totalCount, bool hasIndexedMemoryUnits)
        => new()
        {
            Results = results,
            TotalCount = totalCount,
            HasIndexedMemoryUnits = hasIndexedMemoryUnits,
            Query = "test query",
        };

    private static (HybridSearchService Service, Func<SearchQuery, Task<SearchResult>> Syntactic, Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> Semantic, Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> Graph, IActorProxyFactory ActorFactory) CreateService()
    {
        Func<SearchQuery, Task<SearchResult>> syntactic = Substitute.For<Func<SearchQuery, Task<SearchResult>>>();
        Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> semantic = Substitute.For<Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>>>();
        Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> graph = Substitute.For<Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>>>();

        IActorProxyFactory actorFactory = Substitute.For<IActorProxyFactory>();
        ICorpusStatisticsActor statsActor = Substitute.For<ICorpusStatisticsActor>();
        statsActor.GetStatisticsAsync().Returns(DefaultStats);
        actorFactory.CreateActorProxy<ICorpusStatisticsActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(statsActor);

        var service = new HybridSearchService(
            syntactic, semantic, graph, actorFactory,
            NullLogger<HybridSearchService>.Instance);

        return (service, syntactic, semantic, graph, actorFactory);
    }

    private static SearchQuery MakeQuery(string tenantId = "tenant-1", int maxResults = 10, int offset = 0)
        => new() { TenantId = tenantId, Query = "test query", MaxResults = maxResults, Offset = offset };

    private static TenantEmbeddingConfig MakeEmbeddingConfig()
        => new() { Provider = "google", Model = "text-embedding-004", Dimensions = 768, RateLimitPerMinute = 60, ApiSecretKeyName = "test-key" };

    // 8.3: All three axes enabled -> all three delegate functions called
    [Fact]
    public async Task SearchAsync_AllAxesEnabled_ShouldCallAllDelegates()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));
        graph(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.5, "graph")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), "start-node", 2, DefaultWeights, axes, CancellationToken.None);

        await syntactic.Received(1)(Arg.Any<SearchQuery>());
        await semantic.Received(1)(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>());
        await graph.Received(1)(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        result.Results.ShouldNotBeEmpty();
    }

    // 8.4: Only syntactic + semantic -> graph NOT called
    [Fact]
    public async Task SearchAsync_TwoAxesEnabled_ShouldNotCallExcludedAxis()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        await syntactic.Received(1)(Arg.Any<SearchQuery>());
        await semantic.Received(1)(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>());
        await graph.DidNotReceive()(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        result.Degraded.ShouldBeFalse();
    }

    // 8.5: Syntactic throws -> degraded=true, unavailableAxes=["syntactic"]
    [Fact]
    public async Task SearchAsync_SyntacticThrows_ShouldReturnDegraded()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).ThrowsAsync(new InvalidOperationException("Redis down"));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("syntactic");
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-1");
    }

    // 8.6: Semantic enabled but embeddingConfig null -> intentionally excluded, NOT degraded
    [Fact]
    public async Task SearchAsync_SemanticWithNullConfig_ShouldNotBeDegraded()
    {
        var (service, syntactic, semantic, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), embeddingConfig: null, null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        await semantic.DidNotReceive()(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>());
    }

    // 8.7: Graph enabled but graphStartNodeId null -> intentionally excluded, NOT degraded
    [Fact]
    public async Task SearchAsync_GraphWithNullStartNode_ShouldNotBeDegraded()
    {
        var (service, syntactic, _, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), null, graphStartNodeId: null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        await graph.DidNotReceive()(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // 8.8: Corpus stats actor called with correct tenantId
    [Fact]
    public async Task SearchAsync_ShouldFetchCorpusStatsForCorrectTenant()
    {
        var (service, syntactic, _, _, actorFactory) = CreateService();
        HashSet<string> axes = ["syntactic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));

        await service.SearchAsync(
            MakeQuery("my-tenant"), null, null, 2, DefaultWeights, axes, CancellationToken.None);

        actorFactory.Received(1).CreateActorProxy<ICorpusStatisticsActor>(
            Arg.Is<ActorId>(id => id.ToString() == "my-tenant"),
            Arg.Is(nameof(CorpusStatisticsActor)));
    }

    // 8.8b: Corpus stats actor throws -> service still returns results with BM25 normalization using 0/0 defaults
    [Fact]
    public async Task SearchAsync_CorpusStatsFailure_ShouldStillReturnResults()
    {
        Func<SearchQuery, Task<SearchResult>> syntactic = Substitute.For<Func<SearchQuery, Task<SearchResult>>>();
        Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>> semantic = Substitute.For<Func<SearchQuery, TenantEmbeddingConfig, CancellationToken, Task<SearchResult>>>();
        Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>> graph = Substitute.For<Func<SearchQuery, string, int, CancellationToken, Task<SearchResult>>>();

        IActorProxyFactory actorFactory = Substitute.For<IActorProxyFactory>();
        ICorpusStatisticsActor statsActor = Substitute.For<ICorpusStatisticsActor>();
        statsActor.GetStatisticsAsync().ThrowsAsync(new InvalidOperationException("Actor unavailable"));
        actorFactory.CreateActorProxy<ICorpusStatisticsActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(statsActor);

        var service = new HybridSearchService(
            syntactic, semantic, graph, actorFactory,
            NullLogger<HybridSearchService>.Instance);

        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 10.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Results.ShouldNotBeEmpty();
        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("syntactic");
        result.Results[0].SyntacticScore.ShouldBeNull();
        // Semantic should still contribute normally
        result.Results[0].SemanticScore.ShouldNotBeNull();
        result.Results[0].SemanticScore!.Value.ShouldBe(0.85, tolerance: 0.001);
        result.Results[0].CompositeScore.ShouldBe(0.85, tolerance: 0.001);
    }

    [Fact]
    public async Task SearchAsync_UnindexedAxisShouldNotPenalizeOtherAxes()
    {
        var (service, syntactic, semantic, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult([], 0, false));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.Count.ShouldBe(1);
        result.Results[0].SyntacticScore.ShouldBeNull();
        result.Results[0].SemanticScore.ShouldNotBeNull();
        result.Results[0].SemanticScore!.Value.ShouldBe(0.85, tolerance: 0.001);
        result.Results[0].CompositeScore.ShouldBe(0.85, tolerance: 0.001);
    }

    [Fact]
    public async Task SearchAsync_StaleOnlyAxisShouldBeExcludedFromFusion()
    {
        var (service, syntactic, semantic, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult([], 1, true));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("semantic");
        result.Results.Count.ShouldBe(1);
        result.Results[0].MemoryUnitId.ShouldBe("mu-1");
        result.Results[0].SyntacticScore.ShouldNotBeNull();
        result.Results[0].SemanticScore.ShouldBeNull();
        result.Results[0].CompositeScore.ShouldBe(result.Results[0].SyntacticScore!.Value, tolerance: 0.001);
    }

    [Fact]
    public async Task SearchAsync_StaleLeadingPageShouldBackfillLaterValidHits()
    {
        var (service, syntactic, semantic, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(
                Arg.Is<SearchQuery>(q => q.Offset == 0 && q.MaxResults == 2),
                Arg.Any<TenantEmbeddingConfig>(),
                Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult([], 4, true));
        semantic(
                Arg.Is<SearchQuery>(q => q.Offset == 2 && q.MaxResults == 2),
                Arg.Any<TenantEmbeddingConfig>(),
                Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult([MakeResult("mu-2", 0.80, "semantic")], 4, true));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(maxResults: 2), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.Count.ShouldBe(2);
        result.Results.ShouldContain(r => r.MemoryUnitId == "mu-2");
        await semantic.Received(1)(
            Arg.Is<SearchQuery>(q => q.Offset == 0 && q.MaxResults == 2),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
        await semantic.Received(1)(
            Arg.Is<SearchQuery>(q => q.Offset == 2 && q.MaxResults == 2),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());
    }

    // 8.9: Pagination — offset=5, maxResults=3 correctly slices fused results
    [Fact]
    public async Task SearchAsync_Pagination_ShouldSliceFusedResults()
    {
        var (service, _, semantic, _, _) = CreateService();
        HashSet<string> axes = ["semantic"];

        // Return 10 results
        ScoredResult[] results = Enumerable.Range(0, 10)
            .Select(i => MakeResult($"mu-{i:D2}", 0.9 - (i * 0.05), "semantic"))
            .ToArray();
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(results));

        HybridSearchResult hybridResult = await service.SearchAsync(
            MakeQuery(maxResults: 3, offset: 5), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        await semantic.Received(1)(
            Arg.Is<SearchQuery>(q => q.Offset == 0 && q.MaxResults == 8),
            Arg.Any<TenantEmbeddingConfig>(),
            Arg.Any<CancellationToken>());

        hybridResult.TotalCount.ShouldBe(8);
        hybridResult.Results.Count.ShouldBe(3);
        // Results should be the 6th, 7th, 8th items after sorting (offset=5, take 3)
        hybridResult.Results[0].MemoryUnitId.ShouldBe("mu-05");
        hybridResult.Results[1].MemoryUnitId.ShouldBe("mu-06");
        hybridResult.Results[2].MemoryUnitId.ShouldBe("mu-07");
    }

    [Fact]
    public async Task SearchAsync_PreUnavailableSemanticAxis_ShouldRemainDegraded()
    {
        var (service, syntactic, semantic, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(),
            embeddingConfig: null,
            graphStartNodeId: null,
            2,
            DefaultWeights,
            axes,
            preUnavailableAxes: ["semantic"],
            CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("semantic");
        result.AllEnabledAxesUnavailable.ShouldBe(false);
        await semantic.DidNotReceive()(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_PreUnavailableOnlyAxis_ShouldNotPromoteTotalFailure()
    {
        var (service, _, semantic, _, _) = CreateService();
        HashSet<string> axes = ["semantic"];

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(),
            embeddingConfig: null,
            graphStartNodeId: null,
            2,
            DefaultWeights,
            axes,
            preUnavailableAxes: ["semantic"],
            CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("semantic");
        result.AllEnabledAxesUnavailable.ShouldBeNull();
        result.Results.ShouldBeEmpty();
        await semantic.DidNotReceive()(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_CanceledAxis_ShouldPropagateCancellation()
    {
        var (service, _, semantic, _, _) = CreateService();
        HashSet<string> axes = ["semantic"];
        using CancellationTokenSource cts = new();
        cts.Cancel();

        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<SearchResult>(cts.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => service.SearchAsync(
            MakeQuery(),
            MakeEmbeddingConfig(),
            graphStartNodeId: null,
            2,
            DefaultWeights,
            axes,
            cts.Token));
    }

    // ==================================================================================
    // Story 5.6 — AllEnabledAxesUnavailable signal (AC1, AC2, AC3)
    // ==================================================================================

    [Fact]
    public async Task SearchAsync_SemanticFails_OthersSucceed_ShouldReportNotAllUnavailable()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StackExchange.Redis.RedisConnectionException(
                StackExchange.Redis.ConnectionFailureType.SocketFailure, "Semantic down"));
        graph(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.5, "graph")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), "start-node", 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("semantic");
        result.AllEnabledAxesUnavailable.ShouldBe(false);
        result.Results.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_GraphFails_OthersSucceed_ShouldReportNotAllUnavailable()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));
        graph(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StackExchange.Redis.RedisConnectionException(
                StackExchange.Redis.ConnectionFailureType.SocketFailure, "Graph down"));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), "start-node", 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("graph");
        result.AllEnabledAxesUnavailable.ShouldBe(false);
        result.Results.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_AllAxesFail_ShouldReportAllEnabledAxesUnavailable()
    {
        var (service, syntactic, semantic, graph, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).ThrowsAsync(new StackExchange.Redis.RedisConnectionException(
            StackExchange.Redis.ConnectionFailureType.SocketFailure, "Syntactic down"));
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StackExchange.Redis.RedisConnectionException(
                StackExchange.Redis.ConnectionFailureType.SocketFailure, "Semantic down"));
        graph(Arg.Any<SearchQuery>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StackExchange.Redis.RedisConnectionException(
                StackExchange.Redis.ConnectionFailureType.SocketFailure, "Graph down"));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), "start-node", 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.AllEnabledAxesUnavailable.ShouldBe(true);
        result.UnavailableAxes.ShouldContain("syntactic");
        result.UnavailableAxes.ShouldContain("semantic");
        result.UnavailableAxes.ShouldContain("graph");
    }

    [Fact]
    public async Task SearchAsync_SkippedAxesNotAttempted_ShouldNotBeCountedAsFailed()
    {
        // AC3.4 edge: enabled axes skipped for missing inputs are NOT attempted → do not count toward total failure.
        var (service, syntactic, _, _, _) = CreateService();
        HashSet<string> axes = ["syntactic", "semantic", "graph"];

        syntactic(Arg.Any<SearchQuery>()).Returns(MakeSearchResult(MakeResult("mu-1", 5.0, "syntactic")));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(),
            embeddingConfig: null, // semantic skipped
            graphStartNodeId: null, // graph skipped
            2,
            DefaultWeights,
            axes,
            CancellationToken.None);

        result.Degraded.ShouldBeFalse();
        result.AllEnabledAxesUnavailable.ShouldBe(false);
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_AllAxesSkipped_ShouldReportAllEnabledAxesUnavailableNull()
    {
        // AC3.4 edge: if no axis is attempted (all skipped), AllEnabledAxesUnavailable is null.
        var (service, _, _, _, _) = CreateService();
        HashSet<string> axes = ["semantic", "graph"];

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(),
            embeddingConfig: null, // semantic skipped
            graphStartNodeId: null, // graph skipped
            2,
            DefaultWeights,
            axes,
            CancellationToken.None);

        result.AllEnabledAxesUnavailable.ShouldBeNull();
        result.Degraded.ShouldBeFalse();
        result.UnavailableAxes.ShouldBeEmpty();
        result.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TransientThenSuccess_SecondCallShouldNotBeDegraded()
    {
        // AC4: auto-recovery — once the mock stops throwing, the next call is a clean success.
        var (service, _, semantic, _, _) = CreateService();
        HashSet<string> axes = ["semantic"];

        int callCount = 0;
        semantic(Arg.Any<SearchQuery>(), Arg.Any<TenantEmbeddingConfig>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1
                    ? throw new StackExchange.Redis.RedisConnectionException(
                        StackExchange.Redis.ConnectionFailureType.SocketFailure, "transient")
                    : Task.FromResult(MakeSearchResult(MakeResult("mu-1", 0.85, "semantic")));
            });

        HybridSearchResult first = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);
        HybridSearchResult second = await service.SearchAsync(
            MakeQuery(), MakeEmbeddingConfig(), null, 2, DefaultWeights, axes, CancellationToken.None);

        first.Degraded.ShouldBeTrue();
        first.UnavailableAxes.ShouldContain("semantic");
        first.AllEnabledAxesUnavailable.ShouldBe(true);

        second.Degraded.ShouldBeFalse();
        second.UnavailableAxes.ShouldBeEmpty();
        second.AllEnabledAxesUnavailable.ShouldBe(false);
        second.Results.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("LOADING Redis is loading the dataset in memory")]
    [InlineData("BUSY Redis is busy running a script. You can only call SCRIPT KILL or SHUTDOWN NOSAVE.")]
    [InlineData("OOM command not allowed when used memory > 'maxmemory'.")]
    public async Task SearchAsync_TransientRedisServerException_ShouldMarkAxisUnavailable(string redisMessage)
    {
        // Task 5.4: LOADING / BUSY / OOM must be classified as backend-unavailable, not missing-data.
        var (service, syntactic, _, _, _) = CreateService();
        HashSet<string> axes = ["syntactic"];

        syntactic(Arg.Any<SearchQuery>()).ThrowsAsync(new StackExchange.Redis.RedisServerException(redisMessage));

        HybridSearchResult result = await service.SearchAsync(
            MakeQuery(), null, null, 2, DefaultWeights, axes, CancellationToken.None);

        result.Degraded.ShouldBeTrue();
        result.UnavailableAxes.ShouldContain("syntactic");
        result.AllEnabledAxesUnavailable.ShouldBe(true);
    }
}
