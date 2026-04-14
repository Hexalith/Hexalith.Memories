namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantDeletionResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantDeletionResult("tenant-1", TenantStatus.Active, "Tenant deleted successfully.")
        {
            DeletedBackends = ["RediSearch", "RedisVector", "FalkorDB"],
        };
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantDeletionResult? deserialized = JsonSerializer.Deserialize<TenantDeletionResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void DeletedBackends_NullOmitted()
    {
        var original = new TenantDeletionResult("tenant-1", TenantStatus.Active, "Done.");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldNotContain("deletedBackends");
    }

    [Fact]
    public void DeletedBackends_IncludedWhenPopulated()
    {
        var original = new TenantDeletionResult("tenant-1", TenantStatus.Active, "Done.")
        {
            DeletedBackends = ["RediSearch"],
        };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"deletedBackends\":");
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantDeletionResult("tenant-1", TenantStatus.Active, "Done.");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"status\":");
        json.ShouldContain("\"message\":");
    }
}
