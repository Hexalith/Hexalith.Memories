namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class MetadataFieldSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalObject()
    {
        var original = new MetadataField("payment-related", MetadataOrigin.Human, 0.5f);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MetadataField? deserialized = JsonSerializer.Deserialize<MetadataField>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }

    [Fact]
    public void Origin_ShouldSerializeAsString()
    {
        var field = new MetadataField("test", MetadataOrigin.Ai, 0.5f);
        string json = JsonSerializer.Serialize(field, MemoriesJsonContext.Options);

        json.ShouldContain("\"origin\":");
        json.ShouldNotContain("\"origin\":1");
    }

    [Fact]
    public void ConfidenceBoundary_ZeroAndOne_ShouldRoundTrip()
    {
        var zero = new MetadataField("a", MetadataOrigin.Human, 0.0f);
        var one = new MetadataField("b", MetadataOrigin.Ai, 1.0f);

        MetadataField? zeroRt = JsonSerializer.Deserialize<MetadataField>(
            JsonSerializer.Serialize(zero, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);
        MetadataField? oneRt = JsonSerializer.Deserialize<MetadataField>(
            JsonSerializer.Serialize(one, MemoriesJsonContext.Options),
            MemoriesJsonContext.Options);

        zeroRt.ShouldBe(zero);
        oneRt.ShouldBe(one);
    }

    [Theory]
    [InlineData(MetadataOrigin.Human)]
    [InlineData(MetadataOrigin.Ai)]
    public void AllOriginValues_ShouldRoundTrip(MetadataOrigin origin)
    {
        var original = new MetadataField("test-value", origin, 0.5f);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MetadataField? deserialized = JsonSerializer.Deserialize<MetadataField>(json, MemoriesJsonContext.Options);

        deserialized.ShouldBe(original);
    }
}
