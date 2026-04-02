namespace Hexalith.Memories.Server.Tests.Search;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Search;

using Shouldly;

public class ExplainMetadataBuilderTests
{
    [Fact]
    public void BuildForHybrid_AllThreeAxes_ShouldReturnThreeAxisEntries()
    {
        HashSet<string> activeAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic", "graph" };
        var weights = new FusionWeights();

        SearchExplanation result = ExplainMetadataBuilder.BuildForHybrid(activeAxes, weights);

        result.AxisDetails.Count.ShouldBe(3);
        result.AxisDetails.ShouldContainKey("syntactic");
        result.AxisDetails.ShouldContainKey("semantic");
        result.AxisDetails.ShouldContainKey("graph");
        result.AxisDetails["syntactic"].NormalizationMethod.ShouldBe("bm25_saturation");
        result.AxisDetails["semantic"].NormalizationMethod.ShouldBe("cosine_clamp");
        result.AxisDetails["graph"].NormalizationMethod.ShouldBe("inverse_hop_decay");
    }

    [Fact]
    public void BuildForHybrid_TwoAxes_ShouldReturnOnlyRequestedAxes()
    {
        HashSet<string> activeAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic", "semantic" };
        var weights = new FusionWeights();

        SearchExplanation result = ExplainMetadataBuilder.BuildForHybrid(activeAxes, weights);

        result.AxisDetails.Count.ShouldBe(2);
        result.AxisDetails.ShouldContainKey("syntactic");
        result.AxisDetails.ShouldContainKey("semantic");
        result.AxisDetails.ShouldNotContainKey("graph");
    }

    [Fact]
    public void BuildForHybrid_ShouldSetWeightsUsed()
    {
        HashSet<string> activeAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic" };
        var weights = new FusionWeights { SyntacticWeight = 0.6, SemanticWeight = 0.3, GraphWeight = 0.1 };

        SearchExplanation result = ExplainMetadataBuilder.BuildForHybrid(activeAxes, weights);

        result.WeightsUsed.ShouldNotBeNull();
        result.WeightsUsed.ShouldBe(weights);
    }

    [Fact]
    public void BuildForHybrid_ShouldSetCaveatToStandardMessage()
    {
        HashSet<string> activeAxes = new(StringComparer.OrdinalIgnoreCase) { "syntactic" };
        var weights = new FusionWeights();

        SearchExplanation result = ExplainMetadataBuilder.BuildForHybrid(activeAxes, weights);

        result.Caveat.ShouldBe(ExplainMetadataBuilder.Caveat);
    }

    [Fact]
    public void BuildForSingleAxis_Syntactic_ShouldReturnSingleEntry()
    {
        SearchExplanation result = ExplainMetadataBuilder.BuildForSingleAxis("syntactic");

        result.AxisDetails.Count.ShouldBe(1);
        result.AxisDetails.ShouldContainKey("syntactic");
        result.AxisDetails["syntactic"].NormalizationMethod.ShouldBe("bm25_saturation");
        result.WeightsUsed.ShouldBeNull();
        result.Caveat.ShouldBe(ExplainMetadataBuilder.Caveat);
    }

    [Fact]
    public void BuildForSingleAxis_Semantic_ShouldReturnCorrectNormalizationMethod()
    {
        SearchExplanation result = ExplainMetadataBuilder.BuildForSingleAxis("semantic");

        result.AxisDetails.Count.ShouldBe(1);
        result.AxisDetails["semantic"].NormalizationMethod.ShouldBe("cosine_clamp");
    }

    [Fact]
    public void BuildForSingleAxis_Graph_ShouldReturnCorrectNormalizationMethod()
    {
        SearchExplanation result = ExplainMetadataBuilder.BuildForSingleAxis("graph");

        result.AxisDetails.Count.ShouldBe(1);
        result.AxisDetails["graph"].NormalizationMethod.ShouldBe("inverse_hop_decay");
    }

    [Fact]
    public void Caveat_ShouldMatchExactPrdWording()
    {
        ExplainMetadataBuilder.Caveat.ShouldBe(
            "Confidence scores measure query-result relevance, NOT factual accuracy or data completeness");
    }
}
