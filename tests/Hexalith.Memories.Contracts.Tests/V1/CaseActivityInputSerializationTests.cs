namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CaseActivityInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new CaseActivityInput(
            "tenant-1",
            "case-001",
            CaseActivityEventType.MemoryUnitIngested,
            "user-123",
            "Memory unit indexed",
            "mu-abc");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseActivityInput? deserialized = JsonSerializer.Deserialize<CaseActivityInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("tenant-1");
        deserialized.CaseId.ShouldBe("case-001");
        deserialized.EventType.ShouldBe(CaseActivityEventType.MemoryUnitIngested);
        deserialized.Actor.ShouldBe("user-123");
        deserialized.Description.ShouldBe("Memory unit indexed");
        deserialized.MemoryUnitId.ShouldBe("mu-abc");
    }

    [Fact]
    public void NullMemoryUnitId_ShouldRoundTrip()
    {
        var original = new CaseActivityInput(
            "tenant-1",
            "case-001",
            CaseActivityEventType.SearchExecuted,
            "system",
            "Search executed",
            null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CaseActivityInput? deserialized = JsonSerializer.Deserialize<CaseActivityInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.MemoryUnitId.ShouldBeNull();
    }
}
