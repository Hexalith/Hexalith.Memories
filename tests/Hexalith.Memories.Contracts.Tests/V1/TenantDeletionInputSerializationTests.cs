namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantDeletionInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantDeletionInput("tenant-1");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantDeletionInput? deserialized = JsonSerializer.Deserialize<TenantDeletionInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantDeletionInput("tenant-1");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
    }

    [Fact]
    public void Deserialized_ShouldMatchOriginalValues()
    {
        var original = new TenantDeletionInput("acme-corp");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantDeletionInput? deserialized = JsonSerializer.Deserialize<TenantDeletionInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("acme-corp");
    }
}
