namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ConfidencePromotionResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new ConfidencePromotionResult(
            "mu-source",
            "mu-target",
            EdgeType.CausedBy,
            0.5f,
            1.0f,
            "auditor@test.com");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConfidencePromotionResult? deserialized = JsonSerializer.Deserialize<ConfidencePromotionResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.SourceNodeId.ShouldBe("mu-source");
        deserialized.TargetNodeId.ShouldBe("mu-target");
        deserialized.EdgeType.ShouldBe(EdgeType.CausedBy);
        deserialized.PreviousConfidence.ShouldBe(0.5f);
        deserialized.NewConfidence.ShouldBe(1.0f);
        deserialized.VerifiedBy.ShouldBe("auditor@test.com");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new ConfidencePromotionResult("s", "t", EdgeType.CausedBy, 0.5f, 1.0f, "user");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"previousConfidence\":");
        json.ShouldContain("\"newConfidence\":");
        json.ShouldContain("\"verifiedBy\":");

        json.ShouldNotContain("\"PreviousConfidence\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"NewConfidence\":", Shouldly.Case.Sensitive);
    }
}
