namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ConfidencePromotionRequestSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new ConfidencePromotionRequest(
            "mu-source",
            "mu-target",
            EdgeType.References,
            1.0f,
            "user@test.com");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ConfidencePromotionRequest? deserialized = JsonSerializer.Deserialize<ConfidencePromotionRequest>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.SourceNodeId.ShouldBe("mu-source");
        deserialized.TargetNodeId.ShouldBe("mu-target");
        deserialized.EdgeType.ShouldBe(EdgeType.References);
        deserialized.NewConfidence.ShouldBe(1.0f);
        deserialized.VerifiedBy.ShouldBe("user@test.com");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new ConfidencePromotionRequest("s", "t", EdgeType.CausedBy, 0.5f, "user");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"sourceNodeId\":");
        json.ShouldContain("\"targetNodeId\":");
        json.ShouldContain("\"edgeType\":");
        json.ShouldContain("\"newConfidence\":");
        json.ShouldContain("\"verifiedBy\":");

        json.ShouldNotContain("\"SourceNodeId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"NewConfidence\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void EdgeType_ShouldSerializeAsCamelCaseString()
    {
        var original = new ConfidencePromotionRequest("s", "t", EdgeType.CorrelatedWith, 0.8f, "user");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"correlatedWith\"");
    }
}
