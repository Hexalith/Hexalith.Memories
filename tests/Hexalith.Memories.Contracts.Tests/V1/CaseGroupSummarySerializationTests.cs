namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CaseGroupSummarySerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new CaseGroupSummary("case-001", "Investigation Alpha", 5);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseGroupSummary? deserialized = JsonSerializer.Deserialize<CaseGroupSummary>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new CaseGroupSummary("case-001", "Test Case", 3);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"caseId\":");
        json.ShouldContain("\"caseName\":");
        json.ShouldContain("\"resultCount\":");

        json.ShouldNotContain("\"CaseId\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"CaseName\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"ResultCount\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void List_ShouldRoundTrip()
    {
        var list = new List<CaseGroupSummary>
        {
            new("case-001", "Alpha", 5),
            new("case-002", "Beta", 3),
        };

        string json = JsonSerializer.Serialize(list, MemoriesJsonContext.Options);
        List<CaseGroupSummary>? deserialized = JsonSerializer.Deserialize<List<CaseGroupSummary>>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldBe(2);
        deserialized[0].ShouldBe(list[0]);
        deserialized[1].ShouldBe(list[1]);
    }

    [Fact]
    public void ZeroResultCount_ShouldSerializeCorrectly()
    {
        var original = new CaseGroupSummary("case-empty", "Empty Case", 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseGroupSummary? deserialized = JsonSerializer.Deserialize<CaseGroupSummary>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ResultCount.ShouldBe(0);
    }
}
