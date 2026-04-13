namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class SearchResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new SearchResult
        {
            Results =
            [
                new ScoredResult
                {
                    MemoryUnitId = "mu-001",
                    Score = 12.5,
                    ContentSnippet = "The claim was denied...",
                    SourceUri = "file:///claim.pdf",
                    SourceType = SourceType.File,
                    Axis = "syntactic",
                },
                new ScoredResult
                {
                    MemoryUnitId = "mu-002",
                    Score = 8.3,
                    ContentSnippet = "Denied claims must be reviewed...",
                    SourceUri = "file:///policy.pdf",
                    SourceType = SourceType.File,
                    Axis = "syntactic",
                },
            ],
            TotalCount = 42,
            HasIndexedMemoryUnits = true,
            Query = "claim denied",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchResult? deserialized = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TotalCount.ShouldBe(42);
        deserialized.HasIndexedMemoryUnits.ShouldBeTrue();
        deserialized.Query.ShouldBe("claim denied");
        deserialized.Results.Count.ShouldBe(2);
        deserialized.Results[0].MemoryUnitId.ShouldBe("mu-001");
        deserialized.Results[1].Score.ShouldBe(8.3);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = false,
            Query = "test",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"results\":");
        json.ShouldContain("\"totalCount\":");
        json.ShouldContain("\"hasIndexedMemoryUnits\":");
        json.ShouldContain("\"query\":");

        json.ShouldNotContain("\"Results\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"TotalCount\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"HasIndexedMemoryUnits\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void EmptyResults_ShouldSerializeCorrectly()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = false,
            Query = "no results query",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchResult? deserialized = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Results.ShouldBeEmpty();
        deserialized.TotalCount.ShouldBe(0);
        deserialized.HasIndexedMemoryUnits.ShouldBeFalse();
    }

    [Fact]
    public void CaseGroups_WhenPopulated_ShouldRoundTrip()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = true,
            Query = "test",
            CaseGroups = [new CaseGroupSummary("case-1", "Alpha", 5)],
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchResult? deserialized = JsonSerializer.Deserialize<SearchResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CaseGroups.ShouldNotBeNull();
        deserialized.CaseGroups.Count.ShouldBe(1);
        deserialized.CaseGroups[0].CaseId.ShouldBe("case-1");
    }

    [Fact]
    public void CaseGroups_WhenNull_ShouldBeOmittedFromJson()
    {
        var original = new SearchResult
        {
            Results = [],
            TotalCount = 0,
            HasIndexedMemoryUnits = false,
            Query = "test",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("caseGroups");
    }
}
