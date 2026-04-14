namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantProvisioningResultSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantProvisioningResult("tenant-1", TenantStatus.Active, "Provisioned successfully.");
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantProvisioningResult? deserialized = JsonSerializer.Deserialize<TenantProvisioningResult>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void CompensatedBackends_ShouldBeOmittedWhenNull()
    {
        var result = new TenantProvisioningResult("tenant-1", TenantStatus.Active, "Success");
        string json = JsonSerializer.Serialize(result, MemoriesJsonContext.Options);

        json.ShouldNotContain("compensatedBackends");
        json.ShouldNotContain("errorCode");
    }

    [Fact]
    public void ErrorCode_ShouldBePresentWhenPopulated()
    {
        var result = new TenantProvisioningResult("tenant-1", TenantStatus.Failed, "Tenant already exists.")
        {
            ErrorCode = "TENANT_ALREADY_EXISTS",
        };

        string json = JsonSerializer.Serialize(result, MemoriesJsonContext.Options);

        json.ShouldContain("\"errorCode\":\"TENANT_ALREADY_EXISTS\"");
    }

    [Fact]
    public void CompensatedBackends_ShouldBePresentWhenPopulated()
    {
        var result = new TenantProvisioningResult("tenant-1", TenantStatus.Failed, "Failed")
        {
            CompensatedBackends = ["RediSearch", "RedisVector"],
        };
        string json = JsonSerializer.Serialize(result, MemoriesJsonContext.Options);

        json.ShouldContain("\"compensatedBackends\":");
        json.ShouldContain("\"RediSearch\"");
        json.ShouldContain("\"RedisVector\"");
    }

    [Fact]
    public void Status_ShouldSerializeAsCamelCaseString()
    {
        var result = new TenantProvisioningResult("tenant-1", TenantStatus.CompensationFailed, "Cleanup failed");
        string json = JsonSerializer.Serialize(result, MemoriesJsonContext.Options);

        json.ShouldContain("\"status\":\"compensationFailed\"");
    }

    [Fact]
    public void RoundTrip_WithCompensatedBackends_ShouldPreserveData()
    {
        var original = new TenantProvisioningResult("tenant-1", TenantStatus.Failed, "Rollback complete")
        {
            CompensatedBackends = ["RediSearch", "FalkorDB"],
        };
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantProvisioningResult? deserialized = JsonSerializer.Deserialize<TenantProvisioningResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.CompensatedBackends.ShouldNotBeNull();
        deserialized.CompensatedBackends.Count.ShouldBe(2);
        deserialized.CompensatedBackends[0].ShouldBe("RediSearch");
        deserialized.CompensatedBackends[1].ShouldBe("FalkorDB");
    }

    [Fact]
    public void RoundTrip_WithErrorCode_ShouldPreserveData()
    {
        var original = new TenantProvisioningResult("tenant-1", TenantStatus.Failed, "Duplicate tenant")
        {
            ErrorCode = "TENANT_ALREADY_EXISTS",
        };

        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantProvisioningResult? deserialized = JsonSerializer.Deserialize<TenantProvisioningResult>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.ErrorCode.ShouldBe("TENANT_ALREADY_EXISTS");
    }
}
