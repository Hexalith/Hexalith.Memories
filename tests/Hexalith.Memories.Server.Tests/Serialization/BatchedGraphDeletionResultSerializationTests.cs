namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class BatchedGraphDeletionResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new BatchedGraphDeletionResult(1500, 500, false);
        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        BatchedGraphDeletionResult? deserialized = JsonSerializer.Deserialize<BatchedGraphDeletionResult>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void IsComplete_True_ShouldSerialize()
    {
        var original = new BatchedGraphDeletionResult(0, 50, true);
        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        BatchedGraphDeletionResult? deserialized = JsonSerializer.Deserialize<BatchedGraphDeletionResult>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.IsComplete.ShouldBeTrue();
        deserialized.RemainingNodes.ShouldBe(0);
        deserialized.DeletedInBatch.ShouldBe(50);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new BatchedGraphDeletionResult(100, 500, false);
        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);

        json.ShouldContain("\"remainingNodes\":");
        json.ShouldContain("\"deletedInBatch\":");
        json.ShouldContain("\"isComplete\":");
    }
}
