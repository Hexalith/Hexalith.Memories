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

    // Story 18.4 — optional explicit idempotency token (AC1/AC2).
    [Fact]
    public void IdempotencyToken_WhenPopulated_ShouldSerializeAsCamelCaseAndRoundTrip()
    {
        IngestionInput original = CreateFullInput() with { IdempotencyToken = "idem-token-abc" };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        json.ShouldContain("\"idempotencyToken\":\"idem-token-abc\"");
        deserialized.ShouldNotBeNull();
        deserialized.IdempotencyToken.ShouldBe("idem-token-abc");
    }

    [Fact]
    public void IdempotencyToken_WhenNull_ShouldRoundTripAsNull()
    {
        IngestionInput original = CreateFullInput();

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.IdempotencyToken.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_PayloadWithoutIdempotencyToken_ShouldSucceedWithNullToken()
    {
        // Back-compat: a pre-18.4 payload that never carried the field must still deserialize.
        const string legacyJson = """
            {
              "tenantId": "tenant-001",
              "caseId": "case-001",
              "sourceUri": "file:///document.pdf",
              "contentType": "application/pdf",
              "sourceType": "file",
              "ingestedBy": "user@example.com"
            }
            """;

        IngestionInput? deserialized = JsonSerializer.Deserialize<IngestionInput>(legacyJson, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.IdempotencyToken.ShouldBeNull();
        deserialized.SourceUri.ShouldBe("file:///document.pdf");
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
