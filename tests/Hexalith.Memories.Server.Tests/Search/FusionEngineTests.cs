namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

public class FusionEngineTests
{
    private static readonly FusionWeights DefaultWeights = new();

    // --- Helper methods ---

    private static ScoredResult MakeResult(
        string id,
        double score,
        string axis,
        string snippet = "snippet",
        string uri = "file:///test",
        SourceType sourceType = SourceType.File,
        string? caseId = null,
        string? caseName = null,
        int annotationsCount = 0)
        => new()
        {
            MemoryUnitId = id,
            Score = score,
            ContentSnippet = snippet,
            SourceUri = uri,
            SourceType = sourceType,
            Axis = axis,
            CaseId = caseId,
            CaseName = caseName,
            AnnotationsCount = annotationsCount,
        };

    // 7.2: All three axes with the same top-ranked memory unit -> maximum RRF score
    [Fact]
    public void Fuse_AllThreeAxes_ShouldProduceExpectedCompositeScores()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.5, "graph") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, graph, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        FusedScoredResult r = results[0];
        r.MemoryUnitId.ShouldBe("mu-1");
        r.CompositeScore.ShouldBe(1.0);
        r.SyntacticScore.ShouldBe(1.0);
        r.SemanticScore.ShouldBe(1.0);
        r.GraphScore.ShouldBe(1.0);
    }

    // 7.3: Syntactic + semantic only (graph null) -> composite uses only two-axis weights
    [Fact]
    public void Fuse_TwoAxes_ShouldUseOnlyTwoAxisWeights()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 5.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        FusedScoredResult r = results[0];
        r.CompositeScore.ShouldBe(1.0);
        r.GraphScore.ShouldBeNull();
    }

    // 7.4: Single axis only -> composite equals normalized top-rank contribution
    [Fact]
    public void Fuse_SingleAxis_ShouldEqualNormalizedScore()
    {
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].CompositeScore.ShouldBe(1.0);
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
        results[0].SemanticScore.ShouldBe(1.0);
        results[0].CompositeScore.ShouldBe(1.0);
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
        results[0].SemanticScore!.Value.ShouldBe(1.0);
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

        // Composite scoring is based on active axis rank contribution, not raw score magnitude.
        mu1.CompositeScore.ShouldBe(0.3 / 0.65, tolerance: 1e-12);
        mu2.CompositeScore.ShouldBe(0.35 / 0.65, tolerance: 1e-12);
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

    // 7.10: Syntactic contribution is rank-derived rather than raw BM25 magnitude
    [Fact]
    public void Fuse_SyntacticScore_ShouldUseRankContribution()
    {
        var syntactic = new List<ScoredResult>
        {
            MakeResult("mu-1", 10.0, "syntactic"),
            MakeResult("mu-2", 9.0, "syntactic"),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, null, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(2);
        results[0].SyntacticScore.ShouldNotBeNull();
        results[0].SyntacticScore!.Value.ShouldBe(1.0);
        results[1].SyntacticScore!.Value.ShouldBeLessThan(1.0);
    }

    [Fact]
    public void Fuse_TenthRank_ShouldUseCalibratedTopTenContribution()
    {
        List<ScoredResult> syntactic = Enumerable.Range(1, 10)
            .Select(rank => MakeResult($"mu-{rank}", 11 - rank, "syntactic"))
            .ToList();

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, null, null, DefaultWeights, 1000, 200.0);

        FusedScoredResult tenth = results.Single(result => result.MemoryUnitId == "mu-10");
        tenth.SyntacticScore.ShouldNotBeNull();
        tenth.SyntacticScore!.Value.ShouldBe(11.0 / 20.0, tolerance: 1e-12);
        tenth.CompositeScore.ShouldBe(11.0 / 20.0, tolerance: 1e-12);
    }

    // 7.11: Semantic contribution is rank-derived rather than raw cosine passthrough
    [Fact]
    public void Fuse_SemanticScore_ShouldUseRankContribution()
    {
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.85, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, semantic, null, DefaultWeights, 1000, 200.0);

        results[0].SemanticScore.ShouldBe(1.0);
    }

    // 7.12: Graph contribution is rank-derived rather than raw proximity passthrough
    [Fact]
    public void Fuse_GraphScore_ShouldUseRankContribution()
    {
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.5, "graph") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, null, graph, DefaultWeights, 1000, 200.0);

        results[0].GraphScore.ShouldBe(1.0);
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

    // 7.17: Raw non-finite syntactic score does not leak into public fused scores
    [Fact]
    public void Fuse_Bm25NaN_ShouldUseFiniteRankContribution()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", double.NaN, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldBe(1.0);
        results[0].CompositeScore.ShouldBeInRange(0.0, 1.0);
    }

    // 7.18: Infinite syntactic score does not leak into public fused scores
    [Fact]
    public void Fuse_Bm25Infinity_ShouldUseFiniteRankContribution()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", double.PositiveInfinity, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.8, "semantic") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].SyntacticScore.ShouldBe(1.0);
        results[0].CompositeScore.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void Fuse_SyntacticOnlyResult_ShouldPreserveCaseAttribution()
    {
        var syntactic = new List<ScoredResult>
        {
            MakeResult("mu-1", 10.0, "syntactic", caseId: "case-1", caseName: "Case One", annotationsCount: 3),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, null, null, DefaultWeights, 1000, 200.0);

        results[0].CaseId.ShouldBe("case-1");
        results[0].CaseName.ShouldBe("Case One");
        results[0].AnnotationsCount.ShouldBe(3);
    }

    [Theory]
    [InlineData("semantic")]
    [InlineData("graph")]
    public void Fuse_SingleNonSyntacticAxis_ShouldPreserveCaseAttribution(string axis)
    {
        var axisResults = new List<ScoredResult>
        {
            MakeResult("mu-1", 0.9, axis, caseId: "case-1", caseName: "Case One", annotationsCount: 2),
        };

        IReadOnlyList<FusedScoredResult> results = axis == "semantic"
            ? FusionEngine.Fuse(null, axisResults, null, DefaultWeights, 1000, 200.0)
            : FusionEngine.Fuse(null, null, axisResults, DefaultWeights, 1000, 200.0);

        results[0].CaseId.ShouldBe("case-1");
        results[0].CaseName.ShouldBe("Case One");
        results[0].AnnotationsCount.ShouldBe(2);
    }

    [Fact]
    public void Fuse_MixedAxes_ShouldFillMissingAttributionFromLaterAxes()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 10.0, "syntactic") };
        var semantic = new List<ScoredResult>
        {
            MakeResult("mu-1", 0.9, "semantic", caseId: "case-1", caseName: "Case One", annotationsCount: 4),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results[0].CaseId.ShouldBe("case-1");
        results[0].CaseName.ShouldBe("Case One");
        results[0].AnnotationsCount.ShouldBe(4);
    }

    [Fact]
    public void Fuse_ConflictingCaseIds_ShouldKeepFirstNonEmptyCaseId()
    {
        var syntactic = new List<ScoredResult>
        {
            MakeResult("mu-1", 10.0, "syntactic", caseId: "case-first", caseName: "First"),
        };
        var semantic = new List<ScoredResult>
        {
            MakeResult("mu-1", 0.9, "semantic", caseId: "case-second", caseName: "Second", annotationsCount: 5),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, null, DefaultWeights, 1000, 200.0);

        results[0].CaseId.ShouldBe("case-first");
        results[0].CaseName.ShouldBe("First");
        results[0].AnnotationsCount.ShouldBe(5);
    }

    [Fact]
    public void Fuse_SkewedRawScores_ShouldPreferBetterCrossAxisRanks()
    {
        var syntactic = new List<ScoredResult>
        {
            MakeResult("mu-bm25", 10_000.0, "syntactic"),
            MakeResult("mu-consensus", 1.0, "syntactic"),
        };
        var semantic = new List<ScoredResult>
        {
            MakeResult("mu-consensus", 0.95, "semantic"),
            MakeResult("mu-bm25", 0.10, "semantic"),
        };
        var graph = new List<ScoredResult>
        {
            MakeResult("mu-consensus", 0.9, "graph"),
        };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, graph, DefaultWeights, 1000, 200.0);

        results[0].MemoryUnitId.ShouldBe("mu-consensus");
        results[0].CompositeScore.ShouldBeGreaterThan(results[1].CompositeScore);
    }

    [Fact]
    public void Fuse_NaturalLanguageAxis_ShouldPopulateNlScore()
    {
        var nl = new List<ScoredResult> { MakeResult("mu-1", 0.91, "nl") };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            null, null, null, nl, DefaultWeights, 1000, 200.0);

        results.Count.ShouldBe(1);
        results[0].MemoryUnitId.ShouldBe("mu-1");
        results[0].NlScore.ShouldBe(1.0);
        results[0].CompositeScore.ShouldBe(1.0);
    }

    [Fact]
    public void Fuse_ZeroNlWeight_ShouldPreserveThreeAxisRanking()
    {
        var syntactic = new List<ScoredResult> { MakeResult("mu-1", 10.0, "syntactic") };
        var semantic = new List<ScoredResult> { MakeResult("mu-1", 0.9, "semantic") };
        var graph = new List<ScoredResult> { MakeResult("mu-1", 0.8, "graph") };
        var nl = new List<ScoredResult> { MakeResult("mu-2", 0.99, "nl") };
        FusionWeights weights = DefaultWeights with { NlWeight = 0.0 };

        IReadOnlyList<FusedScoredResult> results = FusionEngine.Fuse(
            syntactic, semantic, graph, nl, weights, 1000, 200.0);

        results[0].MemoryUnitId.ShouldBe("mu-1");
        results.ShouldNotContain(r => r.MemoryUnitId == "mu-2");
    }
}
