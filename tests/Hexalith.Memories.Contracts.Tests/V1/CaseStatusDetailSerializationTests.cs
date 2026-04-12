namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CaseStatusDetailSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new CaseStatusDetail(
            "case-001",
            "tenant-1",
            "Test Case",
            "A description",
            CaseStatus.Active,
            DateTimeOffset.Parse("2026-04-01T10:00:00+00:00"),
            DateTimeOffset.Parse("2026-04-01T11:00:00+00:00"),
            MemoryUnitCount: 5,
            LastActivityAt: DateTimeOffset.Parse("2026-04-01T11:30:00+00:00"),
            IndexedCount: 5,
            FailedCount: 2);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseStatusDetail? deserialized = JsonSerializer.Deserialize<CaseStatusDetail>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe("case-001");
        deserialized.TenantId.ShouldBe("tenant-1");
        deserialized.Name.ShouldBe("Test Case");
        deserialized.Description.ShouldBe("A description");
        deserialized.Status.ShouldBe(CaseStatus.Active);
        deserialized.MemoryUnitCount.ShouldBe(5);
        deserialized.LastActivityAt.ShouldBe(DateTimeOffset.Parse("2026-04-01T11:30:00+00:00"));
        deserialized.IndexedCount.ShouldBe(5);
        deserialized.FailedCount.ShouldBe(2);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new CaseStatusDetail(
            "id", "tid", "name", null, CaseStatus.Active,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            0, null, 0, 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"memoryUnitCount\":");
        json.ShouldContain("\"indexedCount\":");
        json.ShouldContain("\"failedCount\":");
        json.ShouldNotContain("\"MemoryUnitCount\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"IndexedCount\":", Shouldly.Case.Sensitive);
        json.ShouldNotContain("\"FailedCount\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void NullDescription_ShouldBeOmitted()
    {
        var original = new CaseStatusDetail(
            "id", "tid", "name", null, CaseStatus.Active,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            0, null, 0, 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("\"description\":");
    }

    [Fact]
    public void NullLastActivityAt_ShouldSerializeAsNull()
    {
        var original = new CaseStatusDetail(
            "id", "tid", "name", null, CaseStatus.Active,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            0, LastActivityAt: null, 0, 0);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseStatusDetail? deserialized = JsonSerializer.Deserialize<CaseStatusDetail>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.LastActivityAt.ShouldBeNull();
    }

    [Fact]
    public void HealthIndicators_ShouldSerializeCorrectly()
    {
        var original = new CaseStatusDetail(
            "id", "tid", "name", null, CaseStatus.Active,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            MemoryUnitCount: 10,
            LastActivityAt: DateTimeOffset.UtcNow,
            IndexedCount: 10,
            FailedCount: 3);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseStatusDetail? deserialized = JsonSerializer.Deserialize<CaseStatusDetail>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.IndexedCount.ShouldBe(10);
        deserialized.FailedCount.ShouldBe(3);
    }
}
