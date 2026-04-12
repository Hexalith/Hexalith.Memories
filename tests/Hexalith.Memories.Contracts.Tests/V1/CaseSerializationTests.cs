namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

public class CaseSerializationTests
{
    [Fact]
    public void RoundTrip_AllFields_ShouldProduceIdenticalJson()
    {
        CaseRecord original = CreateFullCase();
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseRecord? deserialized = JsonSerializer.Deserialize<CaseRecord>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        CaseRecord original = CreateFullCase();
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"id\":");
        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"name\":");
        json.ShouldContain("\"description\":");
        json.ShouldContain("\"status\":");
        json.ShouldContain("\"createdAt\":");
        json.ShouldContain("\"lastUpdated\":");
        json.ShouldContain("\"memoryUnitCount\":");
    }

    [Fact]
    public void Status_ShouldSerializeAsCamelCaseString()
    {
        CaseRecord active = CreateFullCase() with { Status = CaseStatus.Active };
        CaseRecord closed = CreateFullCase() with { Status = CaseStatus.Closed };

        string activeJson = JsonSerializer.Serialize(active, MemoriesJsonContext.Options);
        string closedJson = JsonSerializer.Serialize(closed, MemoriesJsonContext.Options);

        activeJson.ShouldContain("\"status\":\"active\"");
        closedJson.ShouldContain("\"status\":\"closed\"");
    }

    [Fact]
    public void Description_WhenNull_ShouldBeOmitted()
    {
        CaseRecord original = CreateFullCase() with { Description = null };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("\"description\"");
    }

    [Fact]
    public void ListOfCases_ShouldRoundTrip()
    {
        List<CaseRecord> cases =
        [
            CreateFullCase() with { Status = CaseStatus.Active },
            CreateFullCase() with { Id = "case-002", Status = CaseStatus.Closed, Description = null },
        ];

        string json1 = JsonSerializer.Serialize(cases, MemoriesJsonContext.Options);
        List<CaseRecord>? deserialized = JsonSerializer.Deserialize<List<CaseRecord>>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldBe(2);
        deserialized[0].Status.ShouldBe(CaseStatus.Active);
        deserialized[1].Status.ShouldBe(CaseStatus.Closed);
    }

    private static CaseRecord CreateFullCase()
    {
        return new CaseRecord(
            "case-001",
            "tenant-1",
            "Investigation Alpha",
            "First case for testing",
            CaseStatus.Active,
            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 11, 0, 0, TimeSpan.Zero),
            MemoryUnitCount: 5);
    }
}
