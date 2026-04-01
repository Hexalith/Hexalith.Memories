namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class HybridSearchResultSerializationTests
{
    [Fact]
    public void RoundTrip_HybridSearchResult_ShouldProduceIdenticalObject()
    {
        var original = new HybridSearchResult
        {
            Results =
            [
                new FusedScoredResult
                {
                    MemoryUnitId = "mu-001",
                    CompositeScore = 0.82,
                    ContentSnippet = "The claim was denied due to...",
                    SourceUri = "file:///docs/claim.pdf",
                    SourceType = SourceType.File,
                    SyntacticScore = 0.75,
                    SemanticScore = 0.90,
                    GraphScore = null,
                },
            ],
            TotalCount = 1,
            Degraded = false,
            UnavailableAxes = [],
            Query = "test query",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        HybridSearchResult? deserialized = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TotalCount.ShouldBe(1);
        deserialized.Degraded.ShouldBeFalse();
        deserialized.UnavailableAxes.ShouldBeEmpty();
        deserialized.Query.ShouldBe("test query");
        deserialized.Results.Count.ShouldBe(1);
    }

    [Fact]
    public void RoundTrip_FusedScoredResult_ShouldPreserveAllFields()
    {
        var original = new FusedScoredResult
        {
            MemoryUnitId = "mu-002",
            CompositeScore = 0.65,
            ContentSnippet = "Evidence shows...",
            SourceUri = "file:///docs/evidence.pdf",
            SourceType = SourceType.Url,
            SyntacticScore = 0.3,
            SemanticScore = 0.85,
            GraphScore = 0.5,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FusedScoredResult? deserialized = JsonSerializer.Deserialize<FusedScoredResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_FusedScoredResult_NullScores_ShouldSerializeCorrectly()
    {
        var original = new FusedScoredResult
        {
            MemoryUnitId = "mu-003",
            CompositeScore = 0.9,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            SyntacticScore = null,
            SemanticScore = 0.9,
            GraphScore = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FusedScoredResult? deserialized = JsonSerializer.Deserialize<FusedScoredResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.SyntacticScore.ShouldBeNull();
        deserialized.GraphScore.ShouldBeNull();
        deserialized.SemanticScore.ShouldBe(0.9);
    }

    [Fact]
    public void RoundTrip_DegradedResult_ShouldPreserveUnavailableAxes()
    {
        var original = new HybridSearchResult
        {
            Results = [],
            TotalCount = 0,
            Degraded = true,
            UnavailableAxes = ["syntactic", "graph"],
            Query = "test query",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        HybridSearchResult? deserialized = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Degraded.ShouldBeTrue();
        deserialized.UnavailableAxes.Count.ShouldBe(2);
        deserialized.UnavailableAxes.ShouldContain("syntactic");
        deserialized.UnavailableAxes.ShouldContain("graph");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new HybridSearchResult
        {
            Results = [],
            TotalCount = 0,
            Degraded = false,
            UnavailableAxes = [],
            Query = "test",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"totalCount\":");
        json.ShouldContain("\"degraded\":");
        json.ShouldContain("\"unavailableAxes\":");
        json.ShouldContain("\"query\":");

        json.ShouldNotContain("\"TotalCount\":", Case.Sensitive);
        json.ShouldNotContain("\"Degraded\":", Case.Sensitive);
    }
}
