namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class MemoryUnitSerializationTests
{
    [Fact]
    public void RoundTrip_AllRequiredFields_ShouldProduceIdenticalJson()
    {
        MemoryUnit original = CreateFullMemoryUnit();
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void NullableFields_WhenNull_ShouldSerializeAsNull()
    {
        MemoryUnit original = CreateFullMemoryUnit() with
        {
            EmbeddingProvider = null,
            EmbeddingDimensions = null,
            Classification = null,
            FailureDetails = null,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"classification\":null");
        json.ShouldContain("\"failureDetails\":null");
        json.ShouldContain("\"embeddingProvider\":null");
        json.ShouldContain("\"embeddingDimensions\":null");

        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json);
    }

    [Fact]
    public void NullableFields_WhenPopulated_ShouldRoundTrip()
    {
        MemoryUnit original = CreateFullMemoryUnit() with
        {
            EmbeddingProvider = "google:text-embedding-004",
            EmbeddingDimensions = 768,
            Classification = "confidential",
            FailureDetails = new FailureDetails("embedding", "TIMEOUT", 3),
        };

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void EmptyMetadata_ShouldSerializeAsEmptyObject()
    {
        MemoryUnit original = CreateFullMemoryUnit() with { Metadata = [] };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        json.ShouldContain("\"metadata\":{}");

        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);
        json2.ShouldBe(json);
    }

    [Fact]
    public void PopulatedMetadata_ShouldRoundTrip()
    {
        MemoryUnit original = CreateFullMemoryUnit() with
        {
            Metadata = new Dictionary<string, MetadataField>
            {
                ["tag1"] = new("payment-related", MetadataOrigin.Human, 0.5f),
                ["tag2"] = new("fraud-indicator", MetadataOrigin.Ai, 0.8f),
            },
        };

        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void MetadataNullInJson_ShouldDeserializeAsEmptyDictionary()
    {
        string json = """
            {
                "id": "01HZ0001",
                "tenantId": "t1",
                "caseId": "c1",
                "content": "test",
                "contentHash": "abc123",
                "sourceUri": "file:///test.txt",
                "sourceType": "file",
                "ingestedBy": "user1",
                "ingestedAt": "2026-03-28T10:00:00+02:00",
                "lastUpdated": "2026-03-28T10:00:00+02:00",
                "status": "indexed",
                "metadata": null
            }
            """;

        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Metadata.ShouldNotBeNull();
        deserialized.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void MetadataOmittedInJson_ShouldDeserializeAsPersistentEmptyDictionary()
    {
        string json = """
            {
                "id": "01HZ0001",
                "tenantId": "t1",
                "caseId": "c1",
                "content": "test",
                "contentHash": "abc123",
                "sourceUri": "file:///test.txt",
                "sourceType": "file",
                "ingestedBy": "user1",
                "ingestedAt": "2026-03-28T10:00:00+02:00",
                "lastUpdated": "2026-03-28T10:00:00+02:00",
                "status": "indexed"
            }
            """;

        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.Metadata.ShouldNotBeNull();
        deserialized.Metadata.ShouldBeEmpty();

        deserialized.Metadata["category"] = new MetadataField("legal", MetadataOrigin.Human, 1.0f);
        deserialized.Metadata.Count.ShouldBe(1);
        deserialized.Metadata.Keys.ShouldContain("category");
    }

    [Fact]
    public void DateTimeOffset_ShouldPreserveOffset()
    {
        var offset = new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.FromHours(2));
        MemoryUnit original = CreateFullMemoryUnit() with
        {
            IngestedAt = offset,
            LastUpdated = offset,
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        json.ShouldContain("+02:00");

        MemoryUnit? deserialized = JsonSerializer.Deserialize<MemoryUnit>(json, MemoriesJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.IngestedAt.Offset.ShouldBe(TimeSpan.FromHours(2));
        deserialized.LastUpdated.Offset.ShouldBe(TimeSpan.FromHours(2));
    }

    private static MemoryUnit CreateFullMemoryUnit()
    {
        return new MemoryUnit
        {
            Id = "01HZ0001",
            TenantId = "tenant-1",
            CaseId = "case-1",
            Content = "Test content for serialization",
            ContentHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            SourceUri = "file:///documents/test.txt",
            SourceType = SourceType.File,
            IngestedBy = "user@example.com",
            IngestedAt = new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.FromHours(2)),
            LastUpdated = new DateTimeOffset(2026, 3, 28, 11, 0, 0, TimeSpan.FromHours(2)),
            Status = MemoryUnitStatus.Indexed,
            Metadata = new Dictionary<string, MetadataField>
            {
                ["category"] = new("legal", MetadataOrigin.Human, 1.0f),
            },
            EmbeddingProvider = "google:text-embedding-004",
            EmbeddingDimensions = 768,
            Classification = null,
            FailureDetails = null,
        };
    }
}
