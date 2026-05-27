namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

#pragma warning disable SA1402 // File may only contain a single type -- test grouping

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

    [Fact]
    public void CaseId_WhenPopulated_ShouldRoundTrip()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 5.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            CaseId = "case-abc",
            CaseName = "Investigation Alpha",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ScoredResult? deserialized = JsonSerializer.Deserialize<ScoredResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CaseId.ShouldBe("case-abc");
        deserialized.CaseName.ShouldBe("Investigation Alpha");
    }

    [Fact]
    public void CaseId_WhenNull_ShouldBeOmittedFromJson()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 5.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("caseId");
        json.ShouldNotContain("caseName");
    }

    [Fact]
    public void AnnotationsCount_WhenZero_ShouldBeOmittedFromJson()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 5.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            AnnotationsCount = 0,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("annotationsCount");
    }

    [Fact]
    public void AnnotationsCount_WhenNonZero_ShouldBeIncludedInJson()
    {
        var original = new ScoredResult
        {
            MemoryUnitId = "mu-001",
            Score = 5.0,
            ContentSnippet = "snippet",
            SourceUri = "file:///test",
            SourceType = SourceType.File,
            AnnotationsCount = 3,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        ScoredResult? deserialized = JsonSerializer.Deserialize<ScoredResult>(json, MemoriesJsonContext.Options);

        json.ShouldContain("\"annotationsCount\":3");
        deserialized.ShouldNotBeNull();
        deserialized.AnnotationsCount.ShouldBe(3);
    }
}
