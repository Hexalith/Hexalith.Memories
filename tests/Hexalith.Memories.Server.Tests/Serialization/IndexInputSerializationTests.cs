namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class IndexInputSerializationTests
{
    [Fact]
    public void RoundTrip_AllFieldsPopulated_ShouldProduceIdenticalJson()
    {
        IndexInput original = CreateFullInput();

        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexInput? deserialized = JsonSerializer.Deserialize<IndexInput>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void RoundTrip_NullableCausationIdNull_ShouldSerializeCorrectly()
    {
        IndexInput original = CreateFullInput() with { CausationId = null };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexInput? deserialized = JsonSerializer.Deserialize<IndexInput>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CausationId.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_NullableCorrelationIdNull_ShouldSerializeCorrectly()
    {
        IndexInput original = CreateFullInput() with { CorrelationId = null };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexInput? deserialized = JsonSerializer.Deserialize<IndexInput>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CorrelationId.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_FloatArray_ShouldPreserveValues()
    {
        float[] vector = [0.1f, 0.2f, -0.5f, 1.0f, 0.0f];
        IndexInput original = CreateFullInput() with { EmbeddingVector = vector, EmbeddingDimensions = 5 };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexInput? deserialized = JsonSerializer.Deserialize<IndexInput>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.EmbeddingVector.ShouldBe(vector);
    }

    [Fact]
    public void RoundTrip_MetadataDictionary_ShouldPreserve()
    {
        IndexInput original = CreateFullInput();
        original.Metadata["author"] = new MetadataField("John", MetadataOrigin.Human, 1.0f);

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        IndexInput? deserialized = JsonSerializer.Deserialize<IndexInput>(json, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Metadata.Comparer.ShouldBe(StringComparer.Ordinal);
        deserialized.Metadata.ShouldContainKey("author");
        deserialized.Metadata["author"].Value.ShouldBe("John");
        deserialized.Metadata.ContainsKey("AUTHOR").ShouldBeFalse();
    }

    [Fact]
    public void SourceType_ShouldSerializeAsCamelCaseString()
    {
        IndexInput original = CreateFullInput() with { SourceType = SourceType.Event };

        string json = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);

        json.ShouldContain("\"sourceType\":");
        json.ShouldNotContain("\"sourceType\":2"); // Not integer
    }

    private static IndexInput CreateFullInput() => new()
    {
        MemoryUnitId = "mu-001",
        TenantId = "tenant-001",
        CaseId = "case-001",
        Content = "Test content for serialization",
        ContentHash = "sha256hash",
        SourceUri = "file:///document.pdf",
        SourceType = SourceType.File,
        IngestedBy = "user@example.com",
        IngestedAt = DateTimeOffset.Parse("2026-03-29T10:00:00+00:00"),
        EmbeddingVector = [0.1f, 0.2f, 0.3f],
        EmbeddingProvider = "google:text-embedding-004",
        EmbeddingModel = "gemini-embedding-001",
        EmbeddingDimensions = 3,
        CausationId = "mu-cause-001",
        CorrelationId = "mu-corr-001",
    };
}
