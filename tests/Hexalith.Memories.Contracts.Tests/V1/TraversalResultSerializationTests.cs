namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TraversalResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new TraversalResult(
            "mu-start",
            3,
            [
                new TraversalNode(
                    "mu-start",
                    "Starting node content",
                    "file:///start.pdf",
                    SourceType.File,
                    DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                    0,
                    [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "mu-002", "outgoing")]),
                new TraversalNode(
                    "mu-002",
                    "Connected node content",
                    "file:///connected.pdf",
                    SourceType.Url,
                    DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
                    1,
                    [new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "mu-start", "incoming")]),
            ],
            2);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalResult? deserialized = JsonSerializer.Deserialize<TraversalResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.StartNodeId.ShouldBe("mu-start");
        deserialized.Depth.ShouldBe(3);
        deserialized.TotalNodeCount.ShouldBe(2);
        deserialized.Nodes.Count.ShouldBe(2);
        deserialized.Nodes[0].MemoryUnitId.ShouldBe("mu-start");
        deserialized.Nodes[1].MemoryUnitId.ShouldBe("mu-002");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TraversalResult("mu-start", 2, [], 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"startNodeId\":");
        json.ShouldContain("\"depth\":");
        json.ShouldContain("\"nodes\":");
        json.ShouldContain("\"totalNodeCount\":");

        json.ShouldNotContain("\"StartNodeId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"TotalNodeCount\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void EmptyResult_ShouldSerializeCorrectly()
    {
        var original = new TraversalResult("mu-start", 5, [], 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalResult? deserialized = JsonSerializer.Deserialize<TraversalResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Nodes.ShouldBeEmpty();
        deserialized.TotalNodeCount.ShouldBe(0);
    }
}
