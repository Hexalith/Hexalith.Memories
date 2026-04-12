namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class SearchExplanationSerializationTests
{
    [Fact]
    public void RoundTrip_SearchExplanation_AllFieldsPopulated_ShouldProduceIdenticalObject()
    {
        var original = new SearchExplanation
        {
            Caveat = "test caveat",
            AxisDetails = new Dictionary<string, AxisExplanation>
            {
                ["syntactic"] = new AxisExplanation { NormalizationMethod = "bm25_saturation", Description = "BM25 desc" },
                ["semantic"] = new AxisExplanation { NormalizationMethod = "cosine_clamp", Description = "Cosine desc" },
            },
            WeightsUsed = new FusionWeights { SyntacticWeight = 0.5, SemanticWeight = 0.3, GraphWeight = 0.2 },
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchExplanation? deserialized = JsonSerializer.Deserialize<SearchExplanation>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Caveat.ShouldBe(original.Caveat);
        deserialized.AxisDetails.Count.ShouldBe(2);
        deserialized.AxisDetails["syntactic"].NormalizationMethod.ShouldBe("bm25_saturation");
        deserialized.AxisDetails["semantic"].NormalizationMethod.ShouldBe("cosine_clamp");
        deserialized.WeightsUsed.ShouldNotBeNull();
        deserialized.WeightsUsed!.SyntacticWeight.ShouldBe(0.5);
    }

    [Fact]
    public void RoundTrip_AxisExplanation_ShouldProduceIdenticalObject()
    {
        var original = new AxisExplanation
        {
            NormalizationMethod = "bm25_saturation",
            Description = "BM25 saturation normalization",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        AxisExplanation? deserialized = JsonSerializer.Deserialize<AxisExplanation>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_SearchExplanation_WeightsUsedNull_ShouldSerializeCorrectly()
    {
        var original = new SearchExplanation
        {
            Caveat = "test caveat",
            AxisDetails = new Dictionary<string, AxisExplanation>
            {
                ["syntactic"] = new AxisExplanation { NormalizationMethod = "bm25_saturation", Description = "desc" },
            },
            WeightsUsed = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchExplanation? deserialized = JsonSerializer.Deserialize<SearchExplanation>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.WeightsUsed.ShouldBeNull();
        json.ShouldNotContain("\"weightsUsed\"");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new SearchExplanation
        {
            Caveat = "caveat",
            AxisDetails = new Dictionary<string, AxisExplanation>
            {
                ["syntactic"] = new AxisExplanation { NormalizationMethod = "bm25_saturation", Description = "desc" },
            },
            WeightsUsed = new FusionWeights(),
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"caveat\":");
        json.ShouldContain("\"axisDetails\":");
        json.ShouldContain("\"weightsUsed\":");
        json.ShouldContain("\"normalizationMethod\":");
        json.ShouldContain("\"description\":");

        json.ShouldNotContain("\"Caveat\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"AxisDetails\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"NormalizationMethod\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void HybridSearchResult_WithExplanation_ShouldSerializeCorrectly()
    {
        var original = new HybridSearchResult
        {
            Results = [],
            TotalCount = 0,
            Degraded = false,
            UnavailableAxes = [],
            Query = "test",
            Explanation = new SearchExplanation
            {
                Caveat = "caveat",
                AxisDetails = new Dictionary<string, AxisExplanation>
                {
                    ["syntactic"] = new AxisExplanation { NormalizationMethod = "bm25_saturation", Description = "desc" },
                },
                WeightsUsed = new FusionWeights(),
            },
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        HybridSearchResult? deserialized = JsonSerializer.Deserialize<HybridSearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Explanation.ShouldNotBeNull();
        deserialized.Explanation!.Caveat.ShouldBe("caveat");
        deserialized.Explanation.AxisDetails.ShouldContainKey("syntactic");
    }

    [Fact]
    public void SearchResult_WithExplanation_ShouldSerializeCorrectly()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "test",
            Explanation = new SearchExplanation
            {
                Caveat = "caveat",
                AxisDetails = new Dictionary<string, AxisExplanation>
                {
                    ["semantic"] = new AxisExplanation { NormalizationMethod = "cosine_clamp", Description = "desc" },
                },
                WeightsUsed = null,
            },
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchResult? deserialized = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Explanation.ShouldNotBeNull();
        deserialized.Explanation!.AxisDetails.ShouldContainKey("semantic");
    }

    [Fact]
    public void HybridSearchResult_ExplanationNull_ShouldOmitFromJson()
    {
        var original = new HybridSearchResult
        {
            Results = [],
            TotalCount = 0,
            Degraded = false,
            UnavailableAxes = [],
            Query = "test",
            Explanation = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("\"explanation\"");
    }

    [Fact]
    public void SearchResult_ExplanationNull_ShouldOmitFromJson()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "test",
            Explanation = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("\"explanation\"");
    }
}
