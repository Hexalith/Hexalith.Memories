namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CaseActivityEventSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new CaseActivityEvent(
            "1712345678901-0",
            DateTimeOffset.Parse("2024-04-05T19:41:18.901+00:00"),
            CaseActivityEventType.MemoryUnitIngested,
            "user-123",
            "Memory unit abc indexed",
            "mu-abc");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseActivityEvent? deserialized = JsonSerializer.Deserialize<CaseActivityEvent>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe("1712345678901-0");
        deserialized.EventType.ShouldBe(CaseActivityEventType.MemoryUnitIngested);
        deserialized.Actor.ShouldBe("user-123");
        deserialized.Description.ShouldBe("Memory unit abc indexed");
        deserialized.MemoryUnitId.ShouldBe("mu-abc");
    }

    [Fact]
    public void NullMemoryUnitId_ShouldBeOmitted()
    {
        var original = new CaseActivityEvent(
            "1712345678901-0",
            DateTimeOffset.UtcNow,
            CaseActivityEventType.SearchExecuted,
            "system",
            "Search executed",
            null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("\"memoryUnitId\":");
    }

    [Fact]
    public void EventType_ShouldSerializeAsCamelCase()
    {
        var original = new CaseActivityEvent(
            "1-0",
            DateTimeOffset.UtcNow,
            CaseActivityEventType.CaseCreated,
            "system",
            "Created",
            null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"caseCreated\"");
    }

    [Fact]
    public void ListRoundTrip_ShouldWorkForAot()
    {
        List<CaseActivityEvent> original =
        [
            new("1-0", DateTimeOffset.UtcNow, CaseActivityEventType.CaseCreated, "system", "Created", null),
            new("2-0", DateTimeOffset.UtcNow, CaseActivityEventType.MemoryUnitIngested, "user", "Ingested", "mu-1"),
        ];

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        List<CaseActivityEvent>? deserialized = JsonSerializer.Deserialize<List<CaseActivityEvent>>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Count.ShouldBe(2);
        deserialized[0].EventType.ShouldBe(CaseActivityEventType.CaseCreated);
        deserialized[1].MemoryUnitId.ShouldBe("mu-1");
    }
}
