namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantProvisioningInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantProvisioningInput("tenant-1", "Acme Corp") { VectorDimensions = 768 };
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantProvisioningInput? deserialized = JsonSerializer.Deserialize<TenantProvisioningInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantProvisioningInput("tenant-1", "Test");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"displayName\":");
        json.ShouldContain("\"vectorDimensions\":");
    }

    [Fact]
    public void DefaultDimensions_ShouldBe768()
    {
        var input = new TenantProvisioningInput("test", "Test");
        input.VectorDimensions.ShouldBe(768);
    }

    [Fact]
    public void Deserialized_ShouldMatchOriginalValues()
    {
        var original = new TenantProvisioningInput("acme", "ACME Corp") { VectorDimensions = 1536 };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantProvisioningInput? deserialized = JsonSerializer.Deserialize<TenantProvisioningInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("acme");
        deserialized.DisplayName.ShouldBe("ACME Corp");
        deserialized.VectorDimensions.ShouldBe(1536);
    }
}
