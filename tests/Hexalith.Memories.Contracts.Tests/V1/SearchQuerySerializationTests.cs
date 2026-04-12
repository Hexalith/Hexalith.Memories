namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class SearchQuerySerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new SearchQuery
        {
            TenantId = "tenant-001",
            Query = "claim denied",
            CaseId = "case-42",
            MaxResults = 25,
            Offset = 10,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchQuery? deserialized = JsonSerializer.Deserialize<SearchQuery>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new SearchQuery
        {
            TenantId = "tenant-001",
            Query = "test",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"query\":");
        json.ShouldContain("\"maxResults\":");
        json.ShouldContain("\"offset\":");

        json.ShouldNotContain("\"TenantId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"Query\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void DefaultValues_ShouldSerializeCorrectly()
    {
        var original = new SearchQuery
        {
            TenantId = "tenant-001",
            Query = "test",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        SearchQuery? deserialized = JsonSerializer.Deserialize<SearchQuery>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MaxResults.ShouldBe(10);
        deserialized.Offset.ShouldBe(0);
        deserialized.CaseId.ShouldBeNull();
    }
}
