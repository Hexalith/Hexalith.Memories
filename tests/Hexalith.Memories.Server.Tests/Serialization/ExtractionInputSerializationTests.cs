namespace Hexalith.Memories.Server.Tests.Serialization;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class ExtractionInputSerializationTests
{
    [Fact]
    public void RoundTrip_AllFields_ShouldProduceIdenticalJson()
    {
        ExtractionInput original = CreateTestInput();
        string json1 = JsonSerializer.Serialize(original, MemoriesPersistenceJsonContext.Options);
        ExtractionInput? deserialized = JsonSerializer.Deserialize<ExtractionInput>(json1, MemoriesPersistenceJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesPersistenceJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void ByteArray_ShouldSerializeAsBase64()
    {
        // Non-trivial binary content (not just ASCII)
        byte[] binaryContent = [0x00, 0xFF, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        ExtractionInput input = new(
            "file:///binary.bin",
            binaryContent,
            "application/octet-stream",
            SourceType.File);

        string json = JsonSerializer.Serialize(input, MemoriesPersistenceJsonContext.Options);

        // byte[] serializes as Base64
        string expectedBase64 = Convert.ToBase64String(binaryContent);
        json.ShouldContain(expectedBase64);

        ExtractionInput? deserialized = JsonSerializer.Deserialize<ExtractionInput>(json, MemoriesPersistenceJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.ContentBytes.ShouldBe(binaryContent);
    }

    [Fact]
    public void SourceType_ShouldSerializeAsString()
    {
        ExtractionInput input = CreateTestInput();

        string json = JsonSerializer.Serialize(input, MemoriesPersistenceJsonContext.Options);

        json.ShouldContain("\"sourceType\":\"file\"");
    }

    [Fact]
    public void RoundTrip_WithTenantId_PreservesTenantIdField()
    {
        ExtractionInput input = new(
            "file:///doc.pdf",
            [0x01, 0x02],
            "application/pdf",
            SourceType.File,
            "tenant-42");

        string json = JsonSerializer.Serialize(input, MemoriesPersistenceJsonContext.Options);
        json.ShouldContain("\"tenantId\":\"tenant-42\"");

        ExtractionInput? deserialized = JsonSerializer.Deserialize<ExtractionInput>(json, MemoriesPersistenceJsonContext.Options);
        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("tenant-42");
    }

    [Fact]
    public void Deserialize_LegacyPayloadWithoutTenantId_DefaultsToEmptyString()
    {
        // Story 6.2 Breaking Changes: legacy history predating the TenantId field must still deserialize.
        string legacyJson = "{\"sourceUri\":\"file:///old.txt\",\"contentBytes\":\"AQI=\",\"contentType\":\"text/plain\",\"sourceType\":\"file\"}";

        ExtractionInput? deserialized = JsonSerializer.Deserialize<ExtractionInput>(legacyJson, MemoriesPersistenceJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe(string.Empty);
    }

    private static ExtractionInput CreateTestInput()
    {
        return new ExtractionInput(
            "file:///documents/test.pdf",
            [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34],
            "application/pdf",
            SourceType.File);
    }
}
