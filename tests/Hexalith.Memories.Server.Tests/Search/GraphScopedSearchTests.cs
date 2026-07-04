namespace Hexalith.Memories.Server.Tests.Search;

using System.Reflection;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Graph;
using Hexalith.Memories.Server.Search;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using StackExchange.Redis;

public class GraphScopedSearchTests
{
    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(1, 0.5)]
    [InlineData(2, 1.0 / 3.0)]
    [InlineData(3, 0.25)]
    [InlineData(10, 1.0 / 11.0)]
    public void ComputeProximityScore_ShouldReturnInverseHopDistance(int hopDistance, double expected)
    {
        double result = GraphScopedSearch.ComputeProximityScore(hopDistance);

        result.ShouldBe(expected, tolerance: 0.001);
    }

    [Fact]
    public void FilterToGraphScope_AllResultsInGraphSet_ShouldReturnAll()
    {
        List<ScoredResult> results =
        [
            CreateScoredResult("mu-1", 0.9),
            CreateScoredResult("mu-2", 0.8),
            CreateScoredResult("mu-3", 0.7),
        ];
        HashSet<string> graphSet = ["mu-1", "mu-2", "mu-3"];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.Count.ShouldBe(3);
        filtered[0].MemoryUnitId.ShouldBe("mu-1");
        filtered[1].MemoryUnitId.ShouldBe("mu-2");
        filtered[2].MemoryUnitId.ShouldBe("mu-3");
    }

    [Fact]
    public void FilterToGraphScope_NoResultsInGraphSet_ShouldReturnEmpty()
    {
        List<ScoredResult> results =
        [
            CreateScoredResult("mu-1", 0.9),
            CreateScoredResult("mu-2", 0.8),
        ];
        HashSet<string> graphSet = ["mu-99", "mu-100"];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.ShouldBeEmpty();
    }

    [Fact]
    public void FilterToGraphScope_PartialOverlap_ShouldReturnOnlyMatching()
    {
        List<ScoredResult> results =
        [
            CreateScoredResult("mu-1", 0.9),
            CreateScoredResult("mu-2", 0.8),
            CreateScoredResult("mu-3", 0.7),
        ];
        HashSet<string> graphSet = ["mu-1", "mu-3"];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.Count.ShouldBe(2);
        filtered[0].MemoryUnitId.ShouldBe("mu-1");
        filtered[1].MemoryUnitId.ShouldBe("mu-3");
    }

    [Fact]
    public void FilterToGraphScope_ShouldPreserveOrderingAndScores()
    {
        List<ScoredResult> results =
        [
            CreateScoredResult("mu-3", 0.9),
            CreateScoredResult("mu-1", 0.7),
            CreateScoredResult("mu-2", 0.5),
        ];
        HashSet<string> graphSet = ["mu-1", "mu-2", "mu-3"];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.Count.ShouldBe(3);
        filtered[0].MemoryUnitId.ShouldBe("mu-3");
        filtered[0].Score.ShouldBe(0.9);
        filtered[1].MemoryUnitId.ShouldBe("mu-1");
        filtered[1].Score.ShouldBe(0.7);
        filtered[2].MemoryUnitId.ShouldBe("mu-2");
        filtered[2].Score.ShouldBe(0.5);
    }

    [Fact]
    public void FilterToGraphScope_EmptyResults_ShouldReturnEmpty()
    {
        List<ScoredResult> results = [];
        HashSet<string> graphSet = ["mu-1", "mu-2"];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.ShouldBeEmpty();
    }

    [Fact]
    public void FilterToGraphScope_EmptyGraphSet_ShouldReturnEmpty()
    {
        List<ScoredResult> results =
        [
            CreateScoredResult("mu-1", 0.9),
        ];
        HashSet<string> graphSet = [];

        List<ScoredResult> filtered = GraphScopedSearch.FilterToGraphScope(results, graphSet);

        filtered.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchWithinGraphScope_WithAdversarialFilters_ShouldPassQueryToInnerSearch()
    {
        GraphScopedSearch service = new(
            Substitute.For<IConnectionMultiplexer>(),
            Substitute.For<IConnectionMultiplexer>(),
            Substitute.For<IGraphQueryBuilder>(),
            NullLogger<GraphScopedSearch>.Instance);
        SearchQuery query = new()
        {
            TenantId = "tenant-1",
            Query = "@content:{secret} | * -",
            CaseId = "case} @sourceType:{event}",
            SourceTypeFilter = "file=>[KNN 100 @embedding $query_vec]",
            MetadataQuery = "metadata) @caseId:{other}",
            CloudEventSubject = "subject} @content:{secret}",
            AttributeFilters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["role@field"] = "admin|*",
            },
            Offset = 0,
            MaxResults = 5,
        };
        List<SearchQuery> observedQueries = [];

        SearchResult result = await InvokeSearchWithinGraphScopeAsync(
            service,
            query,
            ["mu-1"],
            innerQuery =>
            {
                observedQueries.Add(innerQuery);
                return Task.FromResult(new SearchResult
                {
                    Results = [CreateScoredResult("mu-1", 0.9)],
                    TotalCount = 1,
                    HasIndexedMemoryUnits = true,
                    Query = innerQuery.Query,
                });
            });

        result.Results.Count.ShouldBe(1);
        observedQueries.Count.ShouldBe(1);
        observedQueries[0].Query.ShouldBe(query.Query);
        observedQueries[0].CaseId.ShouldBe(query.CaseId);
        observedQueries[0].SourceTypeFilter.ShouldBe(query.SourceTypeFilter);
        observedQueries[0].MetadataQuery.ShouldBe(query.MetadataQuery);
        observedQueries[0].CloudEventSubject.ShouldBe(query.CloudEventSubject);
        observedQueries[0].AttributeFilters.ShouldBe(query.AttributeFilters);
    }

    private static Task<SearchResult> InvokeSearchWithinGraphScopeAsync(
        GraphScopedSearch service,
        SearchQuery query,
        HashSet<string> graphSet,
        Func<SearchQuery, Task<SearchResult>> innerSearch)
    {
        MethodInfo? method = typeof(GraphScopedSearch).GetMethod(
            "SearchWithinGraphScopeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();

        object? value = method.Invoke(service, [query, graphSet, innerSearch, CancellationToken.None]);
        value.ShouldBeOfType<Task<SearchResult>>();
        return (Task<SearchResult>)value;
    }

    private static ScoredResult CreateScoredResult(string id, double score) => new()
    {
        MemoryUnitId = id,
        Score = score,
        ContentSnippet = "test content",
        SourceUri = "file:///test.txt",
        SourceType = SourceType.File,
        Axis = "syntactic",
    };
}
