namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

public class FusionEngineTests
{
    private static readonly FusionWeights DefaultWeights = new();

    // --- Helper methods ---

    private static ScoredResult MakeResult(string id, double score, string axis, string snippet = "snippet", string uri = "file:///test", SourceType sourceType = SourceType.File)
        => new()
        {
            MemoryUnitId = id,
            Score = score,
            ContentSnippet = snippet,
            SourceUri = uri,
            SourceType = sourceType,
            Axis = axis,
        };

    // 7.2: All three axes with known scores -> expected composite scores
    [Fact]
    public void Fuse_AllThreeAxes_ShouldProduceExpectedCompositeScores()
    {
        // BM25 raw=5.0, docCount=1000, avgDocLen=200 -> k=~19.934 -> normalized=5/(5+19.934)=~0.2006
        // Cosine=0.85 -> normalized=0.85
        // Graph=0.5 (already normalized)
        // composite = (0.4*0.2006 + 0.4*0.85 + 0.2*0.5) / (0.4+0.4+0.2) = (0.08024 + 0.34 + 0.1) / 1.0 = 0.52024
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.5, "graph") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, graph, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        FusedScoredResult r = results[0];
        r.MemoryUnitId.ShouldBe("mu-1");
        r.CompositeScore.ShouldBe(0.52, tolerance: 0.02);
        r.SyntacticScore.ShouldNotBeNull();
        r.SemanticScore.ShouldNotBeNull();
        r.GraphScore.ShouldNotBeNull();
    }

    // 7.3: Syntactic + semantic only (graph null) -> composite uses only two-axis weights
    [Fact]
    public void Fuse_TwoAxes_ShouldUseOnlyTwoAxisWeights()
    {
        // BM25 raw=5.0 -> normalized~0.2006
        // Cosine=0.85 -> normalized=0.85
        // composite = (0.4*0.2006 + 0.4*0.85) / (0.4+0.4) = (0.08024+0.34)/0.8 = 0.5253
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        FusedScoredResult r = results[0];
        r.CompositeScore.ShouldBe(0.525, tolerance: 0.02);
        r.GraphScore.ShouldBeNull();
    }

    // 7.4: Single axis only -> composite equals normalized single-axis score
    [Fact]
    public void Fuse_SingleAxis_ShouldEqualNormalizedScore()
    {
        // Cosine=0.85 -> normalized=0.85
        // composite = (0.4*0.85) / 0.4 = 0.85
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].CompositeScore.ShouldBe(0.85, tolerance: 0.001);
        results[0].SyntacticScore.ShouldBeNull();
        results[0].GraphScore.ShouldBeNull();
    }

    [Fact]
    public void Fuse_EmptyQueriedAxis_ShouldKeepAxisWeightActive()
    {
        var syntactic = new List<ScoredResult>();
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldBeNull();
        results[0].SemanticScore.ShouldBe(0.85);
        results[0].CompositeScore.ShouldBe(0.425, tolerance: 0.001);
    }

    // 7.5: Same memory unit appearing in multiple axes -> merged with per-axis scores populated
    [Fact]
    public void Fuse_SameUnitMultipleAxes_ShouldMergeWithPerAxisScores()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.9, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldNotBeNull();
        results[0].SemanticScore.ShouldNotBeNull();
        results[0].SemanticScore!.Value.ShouldBe(0.9, tolerance: 0.001);
    }

    // 7.6: Memory unit appearing in only one active axis -> other active axis scores stay null, but composite keeps the axis weight active
    [Fact]
    public void Fuse_UnitInOneAxisOnly_ShouldKeepNullForOtherActiveAxes()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-2", 0.9, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(2);
        FusedScoredResult mu1 = results.First(r => r.MemoryUnitId == "mu-1");
        FusedScoredResult mu2 = results.First(r => r.MemoryUnitId == "mu-2");

        // Public result contract preserves null for axes that did not return this document.
        mu1.SemanticScore.ShouldBeNull();
        mu2.SyntacticScore.ShouldBeNull();

        // Inactive axis (graph was null) stays null ("not queried")
        mu1.GraphScore.ShouldBeNull();
        mu2.GraphScore.ShouldBeNull();

        // Composite scoring still penalizes the missing active axis internally.
        mu1.CompositeScore.ShouldBe(0.100, tolerance: 0.02);
        mu2.CompositeScore.ShouldBe(0.45, tolerance: 0.001);
    }

    // 7.7: Determinism — same inputs produce identical output ordering (NFR25)
    [Fact]
    public void Fuse_SameInputs_ShouldProduceIdenticalOutput()
    {
        var syntactic = new List<ScoredResult>
        {
            MakeResult("mu-1", 5.0, "syntactic"),
            MakeResult("mu-2", 3.0, "syntactic"),
        };
        var semantic = new List<ScoredResult>
        {
            MakeResult("mu-1", 0.85, "semantic"),
            MakeResult("mu-3", 0.92, "semantic"),
        };

        IReadOnlyList<FusedScoredResult> first = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        for (int i = 0; i < 100; i++)
        {
            IReadOnlyList<FusedScoredResult> subsequent = FusionEngine.Fuse(
                syntactic, semantic, null, DefaultWeights, 1000, 200.0);

            subsequent.Count.ShouldBe(first.Count);
            for (int j = 0; j < first.Count; j++)
            {
                subsequent[j].MemoryUnitId.ShouldBe(first[j].MemoryUnitId);
                subsequent[j].CompositeScore.ShouldBe(first[j].CompositeScore);
            }
        }
    }

    // 7.8: Tie-breaking — two units with same composite score ordered by MemoryUnitId
    [Fact]
    public void Fuse_TiedScores_ShouldBreakTieByMemoryUnitId()
    {
        // Two units with identical cosine scores
        var semantic = new List<ScoredResult>
        {
            MakeResult("mu-b", 0.8, "semantic"),
            MakeResult("mu-a", 0.8, "semantic"),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(2);
        results[0].MemoryUnitId.ShouldBe("mu-a");
        results[1].MemoryUnitId.ShouldBe("mu-b");
    }

    // 7.9: Empty inputs -> empty result list
    [Fact]
    public void Fuse_AllNullInputs_ShouldReturnEmptyList()
    {
        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, null, null, DefaultWeights, 1000, 200.0);

        results.ShouldBeEmpty();
    }

    [Fact]
    public void Fuse_AllEmptyLists_ShouldReturnEmptyList()
    {
        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            [], [], [], DefaultWeights, 1000, 200.0);

        results.ShouldBeEmpty();
    }

    // 7.10: BM25 normalization applied correctly
    [Fact]
    public void Fuse_Bm25Normalization_ShouldProduceExpectedNormalizedValue()
    {
        // BM25 raw=10.0, docCount=1000, avgDocLen=200 -> k=~19.934 -> normalized=10/(10+19.934)=~0.334
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 10.0, "syntactic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, null, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldNotBeNull();
        results[0].SyntacticScore!.Value.ShouldBe(0.334, tolerance: 0.01);
    }

    // 7.11: Cosine passthrough
    [Fact]
    public void Fuse_CosineScore_ShouldPassThroughUnchanged()
    {
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, DefaultWeights, 1000, 200.0);

        results[0].SemanticScore.ShouldBe(0.85);
    }

    // 7.12: Graph scores passed through (already normalized)
    [Fact]
    public void Fuse_GraphScore_ShouldPassThrough()
    {
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.5, "graph") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, null, graph, DefaultWeights, 1000, 200.0);

        results[0].GraphScore.ShouldBe(0.5);
    }

    // 7.13: Content snippet taken from syntactic result when available
    [Fact]
    public void Fuse_ContentSnippet_ShouldPreferSyntacticSource()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic", snippet: "syntactic-snippet") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic", snippet: "semantic-snippet") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results[0].ContentSnippet.ShouldBe("syntactic-snippet");
    }

    // 7.14: Content snippet falls back to semantic then graph
    [Fact]
    public void Fuse_ContentSnippet_ShouldFallbackToSemanticThenGraph()
    {
        // Only semantic and graph — should pick semantic
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic", snippet: "semantic-snippet") };
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.5, "graph", snippet: "graph-snippet") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, graph, DefaultWeights, 1000, 200.0);

        results[0].ContentSnippet.ShouldBe("semantic-snippet");

        // Only graph — should pick graph
        var graphOnly = new List<ScoredResult> { MakeResult("mu-2", 0.5, "graph", snippet: "graph-only") };

        IReadOnlyList<FusedScoredResult> graphResults = FusionEngine.Fuse(
            null, null, graphOnly, DefaultWeights, 1000, 200.0);

        graphResults[0].ContentSnippet.ShouldBe("graph-only");
    }

    // 7.15: All-zero weights for active axes -> returns empty list
    [Fact]
    public void Fuse_AllZeroWeightsForActiveAxes_ShouldReturnEmptyList()
    {
        var weights = new FusionWeights { SyntacticWeight = 0.0, SemanticWeight = 0.0, GraphWeight = 0.5 };
        // Only semantic results, but semantic weight is 0 -> NaN composite -> filtered out
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, weights, 1000, 200.0);

        results.ShouldBeEmpty();
    }

    // 7.16: Composite score always in [0.0, 1.0] with random inputs
    [Fact]
    public void Fuse_RandomInputs_CompositeScoreShouldBeInRange()
    {
        Random rng = new(42); // deterministic seed

        for (int i = 0; i < 10; i++)
        {
            var syntactic = Enumerable.Range(0, rng.Next(1, 5))
                .Select(j => MakeResult($"mu-{i}-s{j}", rng.NextDouble() * 20.0, "syntactic"))
                .ToList();
            var semantic = Enumerable.Range(0, rng.Next(1, 5))
                .Select(j => MakeResult($"mu-{i}-v{j}", rng.NextDouble(), "semantic"))
                .ToList();

            IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
                syntactic, semantic, null, DefaultWeights, rng.Next(1, 10000), rng.NextDouble() * 500.0 + 1.0);

            foreach (FusedScoredResult r in results)
            {
                r.CompositeScore.ShouldBeInRange(0.0, 1.0);
            }
        }
    }

    // 7.17: BM25 raw score = NaN -> normalized to 0.0
    [Fact]
    public void Fuse_Bm25NaN_ShouldNormalizeToZero()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", double.NaN, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        // NaN BM25 -> normalized to 0.0 by ScoreNormalizer, so syntactic score = 0.0
        results[0].SyntacticScore.ShouldBe(0.0);
        // Composite should still be valid (semantic contributes)
        results[0].CompositeScore.ShouldBeInRange(0.0, 1.0);
    }

    // 7.18: BM25 raw score = PositiveInfinity -> normalized to 0.0
    [Fact]
    public void Fuse_Bm25Infinity_ShouldNormalizeToZero()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", double.PositiveInfinity, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldBe(0.0);
        results[0].CompositeScore.ShouldBeInRange(0.0, 1.0);
    }
}
