namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class GraphEdgeSerializationTests
{
    [Theory]
    [InlineData(EdgeType.CausedBy)]
    [InlineData(EdgeType.CorrelatedWith)]
    [InlineData(EdgeType.References)]
    [InlineData(EdgeType.Contains)]
    [InlineData(EdgeType.Annotates)]
    public void RoundTrip_EachEdgeType_ShouldProduceIdenticalObject(EdgeType edgeType)
    {
        var original = new GraphEdge(
            "01HZ0002",
            "01HZ0001",
            "01HZ0003",
            edgeType,
            0.8f,
            EdgeOrigin.Explicit,
            new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.FromHours(2)));

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        GraphEdge? deserialized = JsonSerializer.Deserialize<GraphEdge>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void ConfidenceBoundary_ZeroAndOne_ShouldRoundTrip()
    {
        var zero = new GraphEdge("01HZ0002", "01HZ0001", "01HZ0003", EdgeType.References, 0.0f, EdgeOrigin.Inferred, DateTimeOffset.UtcNow);
        var one = new GraphEdge("01HZ0004", "01HZ0001", "01HZ0003", EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, DateTimeOffset.UtcNow);

        GraphEdge? zeroRt = JsonSerializer.Deserialize<GraphEdge>(
            JsonSerializer.Serialize(zero, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);
        GraphEdge? oneRt = JsonSerializer.Deserialize<GraphEdge>(
            JsonSerializer.Serialize(one, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);

        zeroRt.ShouldBe(zero);
        oneRt.ShouldBe(one);
    }

    [Fact]
    public void DateTimeOffset_ShouldPreserveOffset()
    {
        var offset = new DateTimeOffset(2026, 3, 28, 14, 30, 0, TimeSpan.FromHours(5));
        var original = new GraphEdge("01HZ0002", "01HZ0001", "01HZ0003", EdgeType.Contains, 1.0f, EdgeOrigin.Explicit, offset);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        json.ShouldContain("+05:00");

        GraphEdge? deserialized = JsonSerializer.Deserialize<GraphEdge>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.CreatedAt.Offset.ShouldBe(TimeSpan.FromHours(5));
    }

    [Fact]
    public void EdgeType_ShouldSerializeAsString()
    {
        var edge = new GraphEdge("01HZ0002", "01HZ0001", "01HZ0003", EdgeType.CausedBy, 1.0f, EdgeOrigin.Explicit, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(edge, MemoriesJsonContext.Options);

        json.ShouldContain("\"edgeType\":");
        json.ShouldNotContain("\"edgeType\":0");
    }

    [Fact]
    public void EdgeOrigin_ShouldSerializeAsString()
    {
        var edge = new GraphEdge("01HZ0002", "01HZ0001", "01HZ0003", EdgeType.CausedBy, 1.0f, EdgeOrigin.Inferred, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(edge, MemoriesJsonContext.Options);

        json.ShouldContain("\"origin\":");
        json.ShouldNotContain("\"origin\":1");
    }
}
