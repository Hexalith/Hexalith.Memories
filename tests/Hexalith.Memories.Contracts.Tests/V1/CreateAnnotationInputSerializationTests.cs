namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class CreateAnnotationInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "This is a correction", "user@example.com", "correction");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CreateAnnotationInput? deserialized = JsonSerializer.Deserialize<CreateAnnotationInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", "user@example.com");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"caseId\":");
        json.ShouldContain("\"targetMemoryUnitId\":");
        json.ShouldContain("\"content\":");
        json.ShouldContain("\"ingestedBy\":");
        json.ShouldNotContain("\"TenantId\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void NullAnnotationType_ShouldSerializeAsNull()
    {
        var original = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "content", "user@example.com");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CreateAnnotationInput? deserialized = JsonSerializer.Deserialize<CreateAnnotationInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.AnnotationType.ShouldBeNull();
    }

    [Fact]
    public void WithAnnotationType_ShouldRoundTrip()
    {
        var original = new CreateAnnotationInput("tenant-1", "case-001", "mu-001", "corrected info", "user@example.com", "correction");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        CreateAnnotationInput? deserialized = JsonSerializer.Deserialize<CreateAnnotationInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.AnnotationType.ShouldBe("correction");
        deserialized.TargetMemoryUnitId.ShouldBe("mu-001");
    }
}
