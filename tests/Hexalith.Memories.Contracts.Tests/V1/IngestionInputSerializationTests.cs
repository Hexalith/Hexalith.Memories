namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class IngestionInputSerializationTests
{
    [Fact]
    public void RoundTrip_AllFieldsPopulated_ShouldProduceIdenticalJson()
    {
        IngestionInput original = CreateFullInput();

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_NullableCausationIdNull_ShouldSerializeCorrectly()
    {
        IngestionInput original = CreateFullInput() with { CausationId = null };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CausationId.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_NullableCorrelationIdNull_ShouldSerializeCorrectly()
    {
        IngestionInput original = CreateFullInput() with { CorrelationId = null };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_ByteArray_ShouldPreserveValues()
    {
        byte[] content = Encoding.UTF8.GetBytes("Hello, world!");
        IngestionInput original = CreateFullInput() with { ContentBytes = content };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ContentBytes.ShouldBe(content);
    }

    [Fact]
    public void RoundTrip_MetadataDictionary_ShouldPreserve()
    {
        IngestionInput original = CreateFullInput();
        original.Metadata["author"] = new MetadataField("John", MetadataOrigin.Human, 1.0f);

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
        deserialized.Metadata.ShouldContainKey("author");
        deserialized.Metadata["author"].Value.ShouldBe("John");
        deserialized.Metadata.ContainsKey("AUTHOR").ShouldBeFalse();
    }

    [Fact]
    public void SourceType_ShouldSerializeAsCamelCaseString()
    {
        IngestionInput original = CreateFullInput() with { SourceType = SourceType.Event };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"sourceType\":");
        json.ShouldNotContain("\"sourceType\":2");
    }

    private static IngestionInput CreateFullInput() => new()
    {
        TenantId = "tenant-001",
        CaseId = "case-001",
        SourceUri = "file:///document.pdf",
        ContentBytes = Encoding.UTF8.GetBytes("Sample content"),
        ContentType = "application/pdf",
        SourceType = SourceType.File,
        IngestedBy = "user@example.com",
        CausationId = "mu-cause-001",
        CorrelationId = "mu-corr-001",
    };
}
