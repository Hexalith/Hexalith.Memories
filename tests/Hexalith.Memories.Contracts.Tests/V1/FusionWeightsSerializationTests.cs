namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class FusionWeightsSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new FusionWeights
        {
            SyntacticWeight = 0.5,
            SemanticWeight = 0.3,
            GraphWeight = 0.2,
            NlWeight = 0.1,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        FusionWeights? deserialized = JsonSerializer.Deserialize<FusionWeights>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void DefaultConstructor_ShouldProduceExpectedDefaults()
    {
        var weights = new FusionWeights();

        weights.SyntacticWeight.ShouldBe(0.3);
        weights.SemanticWeight.ShouldBe(0.35);
        weights.GraphWeight.ShouldBe(0.35);
        weights.NlWeight.ShouldBe(0.2);
    }

    [Fact]
    public void DefaultSerialization_ShouldExposeCalibratedLiveDefaults()
    {
        var weights = new FusionWeights();

        string json = JsonSerializer.Serialize(weights, MemoriesJsonContext.Options);
        FusionWeights? deserialized = JsonSerializer.Deserialize<FusionWeights>(json, MemoriesJsonContext.Options);

        json.ShouldContain("\"syntacticWeight\":0.3");
        json.ShouldContain("\"semanticWeight\":0.35");
        json.ShouldContain("\"graphWeight\":0.35");
        json.ShouldContain("\"nlWeight\":0.2");
        deserialized.ShouldBe(weights);
    }

    [Fact]
    public void MissingJsonProperties_ShouldUseCalibratedLiveDefaults()
    {
        FusionWeights? empty = JsonSerializer.Deserialize<FusionWeights>("{}", MemoriesJsonContext.Options);
        FusionWeights? partial = JsonSerializer.Deserialize<FusionWeights>(
            """
            {"syntacticWeight":0.6}
            """,
            MemoriesJsonContext.Options);

        empty.ShouldBe(new FusionWeights());
        partial.ShouldBe(new FusionWeights { SyntacticWeight = 0.6 });
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new FusionWeights();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"syntacticWeight\":");
        json.ShouldContain("\"semanticWeight\":");
        json.ShouldContain("\"graphWeight\":");
        json.ShouldContain("\"nlWeight\":");

        json.ShouldNotContain("\"SyntacticWeight\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void Validate_AllPositive_ShouldNotThrow()
    {
        var weights = new FusionWeights();

        Should.NotThrow(() => weights.Validate());
    }

    [Fact]
    public void Validate_NegativeWeight_ShouldThrow()
    {
        var weights = new FusionWeights { SyntacticWeight = -0.1 };

        Should.Throw<ArgumentOutOfRangeException>(() => weights.Validate());
    }

    [Fact]
    public void Validate_AllZero_ShouldThrow()
    {
        var weights = new FusionWeights { SyntacticWeight = 0.0, SemanticWeight = 0.0, GraphWeight = 0.0, NlWeight = 0.0 };

        Should.Throw<ArgumentException>(() => weights.Validate());
    }
}
