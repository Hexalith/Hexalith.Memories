namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ScoredResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 12.5,
            ContentSnippet = "The claim was denied due to...",
            SourceUri = "file:///docs/claim.pdf",
            SourceType = SourceType.File,
            Axis = "syntactic",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ScoredResult? deserialized = JsonSerializer.Deserialize<ScoredResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 1.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            Axis = "syntactic",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"memoryUnitId\":");
        json.ShouldContain("\"score\":");
        json.ShouldContain("\"contentSnippet\":");
        json.ShouldContain("\"sourceUri\":");
        json.ShouldContain("\"sourceType\":");
        json.ShouldContain("\"axis\":");

        json.ShouldNotContain("\"MemoryUnitId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"Score\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void NullAxis_ShouldSerializeCorrectly()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 5.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.Event,
            Axis = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ScoredResult? deserialized = JsonSerializer.Deserialize<ScoredResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Axis.ShouldBeNull();
    }
}
