namespace Hexalith.Memories.Contracts.Tests.V1;

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

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IndexResult? deserialized = JsonSerializer.Deserialize<IndexResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        var original = new IndexResult("syntactic", "mu-test-001", "tenant-test");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IndexResult? deserialized = JsonSerializer.Deserialize<IndexResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Backend.ShouldBe("syntactic");
        deserialized.MemoryUnitId.ShouldBe("mu-test-001");
        deserialized.TenantId.ShouldBe("tenant-test");
    }
}
