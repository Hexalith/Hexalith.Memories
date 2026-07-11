namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ExtractionResultSerializationTests
{
    [Fact]
    public void RoundTrip_AllFields_ShouldProduceIdenticalJson()
    {
        ExtractionResult original = CreateTestResult();
        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        ExtractionResult? deserialized = JsonSerializer.Deserialize<ExtractionResult>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void DateTimeOffset_ShouldPreserveOffset()
    {
        var offset = new DateTimeOffset(2026, 3, 28, 14, 30, 0, TimeSpan.FromHours(2));
        ExtractionResult result = new(
            "extracted text",
            "abc123hash",
            offset);

        string json = JsonSerializer.Serialize(result, MemoriesPersistenceJsonContext.Options);
        json.ShouldContain("+02:00");

        ExtractionResult? deserialized = JsonSerializer.Deserialize<ExtractionResult>(json, MemoriesPersistenceJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.ExtractedAt.Offset.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void UtcOffset_ShouldRoundTrip()
    {
        ExtractionResult result = new(
            "content",
            "hash",
            DateTimeOffset.UtcNow);

        string json = JsonSerializer.Serialize(result, MemoriesPersistenceJsonContext.Options);
        ExtractionResult? deserialized = JsonSerializer.Deserialize<ExtractionResult>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ExtractedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    private static ExtractionResult CreateTestResult()
    {
        return new ExtractionResult(
            "This is extracted text content from a PDF document.",
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.FromHours(2)));
    }
}
