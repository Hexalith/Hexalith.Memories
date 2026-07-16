namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class BatchedGraphDeletionInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new BatchedGraphDeletionInput("tenant-1", 500, 3);
        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        BatchedGraphDeletionInput? deserialized = JsonSerializer.Deserialize<BatchedGraphDeletionInput>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void DefaultValues_ShouldBe500And0()
    {
        var input = new BatchedGraphDeletionInput("test");
        input.BatchSize.ShouldBe(500);
        input.BatchNumber.ShouldBe(0);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new BatchedGraphDeletionInput("tenant-1", 500, 3);
        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"batchSize\":");
        json.ShouldContain("\"batchNumber\":");
    }
}
