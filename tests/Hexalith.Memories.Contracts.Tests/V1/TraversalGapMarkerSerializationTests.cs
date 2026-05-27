namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TraversalGapMarkerSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new TraversalGapMarker(
            "mu-missing",
            2,
            [
                new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "mu-001", "incoming"),
                new TraversalEdgeInfo(EdgeType.CorrelatedWith, 0.5f, EdgeOrigin.Inferred, "mu-003", "outgoing"),
            ]);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalGapMarker? deserialized = JsonSerializer.Deserialize<TraversalGapMarker>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MissingNodeId.ShouldBe("mu-missing");
        deserialized.HopDistance.ShouldBe(2);
        deserialized.Edges.Count.ShouldBe(2);
        deserialized.Edges[0].EdgeType.ShouldBe(EdgeType.CausedBy);
        deserialized.Edges[1].ConnectedNodeId.ShouldBe("mu-003");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TraversalGapMarker("mu-missing", 1, []);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"missingNodeId\":");
        json.ShouldContain("\"hopDistance\":");
        json.ShouldContain("\"edges\":");

        json.ShouldNotContain("\"MissingNodeId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"HopDistance\":", Shouldly.Case.Sensitive);
    }
}
