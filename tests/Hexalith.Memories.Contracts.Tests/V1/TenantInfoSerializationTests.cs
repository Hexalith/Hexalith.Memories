namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantInfoSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantInfo("tenant-1", "Acme Corp", TenantStatus.Active, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantInfo? deserialized = JsonSerializer.Deserialize<TenantInfo>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantInfo("tenant-1", "Test Tenant", TenantStatus.Provisioning, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"id\":");
        json.ShouldContain("\"displayName\":");
        json.ShouldContain("\"status\":");
        json.ShouldContain("\"createdAt\":");
        json.ShouldNotContain("\"Id\":", Shouldly.Case.Sensitive);
    }

    [Fact]
    public void NullableFields_ShouldBeOmittedWhenNull()
    {
        var original = new TenantInfo("tenant-1", "Test", TenantStatus.Active, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("embeddingProvider");
        json.ShouldNotContain("embeddingModel");
    }

    [Fact]
    public void NullableFields_ShouldBePresentWhenPopulated()
    {
        var original = new TenantInfo("tenant-1", "Test", TenantStatus.Active, DateTimeOffset.UtcNow)
        {
            EmbeddingProvider = "google",
            EmbeddingModel = "text-embedding-004",
        };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"embeddingProvider\":\"google\"");
        json.ShouldContain("\"embeddingModel\":\"text-embedding-004\"");
    }

    [Fact]
    public void Status_ShouldSerializeAsCamelCaseString()
    {
        var original = new TenantInfo("tenant-1", "Test", TenantStatus.CompensationFailed, DateTimeOffset.UtcNow);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"status\":\"compensationFailed\"");
    }
}
