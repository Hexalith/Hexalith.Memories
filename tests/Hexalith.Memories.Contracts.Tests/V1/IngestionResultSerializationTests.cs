namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class IngestionResultSerializationTests
{
    [Fact]
    public void RoundTrip_AllFieldsPopulated_ShouldProduceIdenticalJson()
    {
        var original = new IngestionResult(
            "mu-001",
            MemoryUnitStatus.Indexed,
            DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
            WasDuplicate: false,
            ConsistencyNote: null);

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionResult? deserialized = JsonSerializer.Deserialize<IngestionResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_WasDuplicateTrue_ShouldPreserve()
    {
        var original = new IngestionResult(
            "mu-existing",
            MemoryUnitStatus.Indexed,
            DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
            WasDuplicate: true,
            ConsistencyNote: null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionResult? deserialized = JsonSerializer.Deserialize<IngestionResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.WasDuplicate.ShouldBeTrue();
    }

    [Fact]
    public void RoundTrip_ConsistencyNotePopulated_ShouldPreserve()
    {
        var original = new IngestionResult(
            "mu-001",
            MemoryUnitStatus.Indexed,
            DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
            WasDuplicate: false,
            ConsistencyNote: "Missing backends: graph");

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionResult? deserialized = JsonSerializer.Deserialize<IngestionResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ConsistencyNote.ShouldBe("Missing backends: graph");
    }

    [Fact]
    public void Status_ShouldSerializeAsCamelCaseString()
    {
        var original = new IngestionResult(
            "mu-001",
            MemoryUnitStatus.Indexed,
            DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
            WasDuplicate: false,
            ConsistencyNote: null);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"status\":");
        json.ShouldNotContain("\"status\":4");
    }
}
