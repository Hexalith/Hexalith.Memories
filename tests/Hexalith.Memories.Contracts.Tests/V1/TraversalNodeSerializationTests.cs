namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TraversalNodeSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new TraversalNode(
            "mu-001",
            "The claim was denied due to insufficient evidence...",
            "file:///docs/claim.pdf",
            SourceType.File,
            DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
            1,
            [
                new TraversalEdgeInfo(EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, "mu-002", "outgoing"),
                new TraversalEdgeInfo(EdgeType.References, 0.9f, EdgeOrigin.Inferred, "mu-003", "incoming"),
            ]);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TraversalNode? deserialized = JsonSerializer.Deserialize<TraversalNode>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemoryUnitId.ShouldBe(original.MemoryUnitId);
        deserialized.ContentSnippet.ShouldBe(original.ContentSnippet);
        deserialized.SourceUri.ShouldBe(original.SourceUri);
        deserialized.SourceType.ShouldBe(original.SourceType);
        deserialized.IngestedAt.ShouldBe(original.IngestedAt);
        deserialized.HopDistance.ShouldBe(original.HopDistance);
        deserialized.Edges.Count.ShouldBe(2);
        deserialized.Edges[0].ShouldBe(original.Edges[0]);
        deserialized.Edges[1].ShouldBe(original.Edges[1]);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TraversalNode(
            "mu-001",
            "snippet",
            "file:///test",
            SourceType.File,
            DateTimeOffset.UtcNow,
            0,
            []);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"memoryUnitId\":");
        json.ShouldContain("\"contentSnippet\":");
        json.ShouldContain("\"sourceUri\":");
        json.ShouldContain("\"sourceType\":");
        json.ShouldContain("\"ingestedAt\":");
        json.ShouldContain("\"hopDistance\":");
        json.ShouldContain("\"edges\":");

        json.ShouldNotContain("\"MemoryUnitId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"ContentSnippet\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void EmptyEdges_ShouldSerializeAsEmptyArray()
    {
        var original = new TraversalNode(
            "mu-001",
            "snippet",
            "file:///test",
            SourceType.File,
            DateTimeOffset.UtcNow,
            0,
            []);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"edges\":[]");
    }

    [Fact]
    public void SourceType_ShouldSerializeAsCamelCaseString()
    {
        var original = new TraversalNode(
            "mu-001",
            "snippet",
            "file:///test",
            SourceType.Event,
            DateTimeOffset.UtcNow,
            0,
            []);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"event\"");
    }
}
