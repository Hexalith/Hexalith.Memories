namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

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
