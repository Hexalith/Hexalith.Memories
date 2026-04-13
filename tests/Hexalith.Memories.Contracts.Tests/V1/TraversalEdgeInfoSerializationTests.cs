namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TraversalEdgeInfoSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.CausedBy,
            1.0f,
            EdgeOrigin.Explicit,
            "mu-002",
            "outgoing");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalEdgeInfo? deserialized = JsonSerializer.Deserialize<TraversalEdgeInfo>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.CorrelatedWith,
            0.8f,
            EdgeOrigin.Inferred,
            "mu-003",
            "incoming");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"edgeType\":");
        json.ShouldContain("\"confidence\":");
        json.ShouldContain("\"origin\":");
        json.ShouldContain("\"connectedNodeId\":");
        json.ShouldContain("\"direction\":");

        json.ShouldNotContain("\"EdgeType\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"ConnectedNodeId\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void EdgeType_ShouldSerializeAsCamelCaseString()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.CausedBy,
            1.0f,
            EdgeOrigin.Explicit,
            "mu-002",
            "outgoing");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"causedBy\"");
    }

    [Fact]
    public void EdgeOrigin_ShouldSerializeAsCamelCaseString()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.References,
            0.9f,
            EdgeOrigin.Inferred,
            "mu-004",
            "incoming");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"inferred\"");
    }

    [Fact]
    public void VerifiedByAndPreviousConfidence_WhenSet_ShouldSerialize()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.CausedBy,
            1.0f,
            EdgeOrigin.Explicit,
            "mu-002",
            "outgoing")
        {
            VerifiedBy = "user@test.com",
            PreviousConfidence = 0.5f,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalEdgeInfo? deserialized = JsonSerializer.Deserialize<TraversalEdgeInfo>(json, MemoriesJsonContext.Options);

        json.ShouldContain("\"verifiedBy\":");
        json.ShouldContain("\"previousConfidence\":");
        deserialized.ShouldNotBeNull();
        deserialized.VerifiedBy.ShouldBe("user@test.com");
        deserialized.PreviousConfidence.ShouldBe(0.5f);
    }

    [Fact]
    public void VerifiedByAndPreviousConfidence_WhenNull_ShouldBeOmittedFromJson()
    {
        var original = new TraversalEdgeInfo(
            EdgeType.CausedBy,
            1.0f,
            EdgeOrigin.Explicit,
            "mu-002",
            "outgoing");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("verifiedBy");
        json.ShouldNotContain("previousConfidence");
    }
}
