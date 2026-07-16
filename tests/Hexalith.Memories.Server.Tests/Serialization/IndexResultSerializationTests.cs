namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class IndexResultSerializationTests
{
    [Theory]
    [InlineData("syntactic")]
    [InlineData("semantic")]
    [InlineData("graph")]
    public void RoundTrip_EachBackend_ShouldProduceIdenticalJson(string backend)
    {
        var original = new IndexResult(backend, "mu-001", "tenant-001");

        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexResult? deserialized = JsonSerializer.Deserialize<IndexResult>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        var original = new IndexResult("syntactic", "mu-test-001", "tenant-test");

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexResult? deserialized = JsonSerializer.Deserialize<IndexResult>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Backend.ShouldBe("syntactic");
        deserialized.MemoryUnitId.ShouldBe("mu-test-001");
        deserialized.TenantId.ShouldBe("tenant-test");
    }
}
