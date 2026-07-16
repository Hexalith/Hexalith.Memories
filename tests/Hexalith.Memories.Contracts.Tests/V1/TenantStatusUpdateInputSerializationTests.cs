namespace Hexalith.Memories.Contracts.Tests.V1;

using System.Text.Json;

using Hexalith.Memories.Contracts.V1;

using Shouldly;

public class TenantStatusUpdateInputSerializationTests
{
    [Fact]
    public void RoundTrip_ShouldProduceIdenticalJson()
    {
        var original = new TenantStatusUpdateInput("tenant-1", TenantStatus.Active);
        string json1 = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantStatusUpdateInput? deserialized = JsonSerializer.Deserialize<TenantStatusUpdateInput>(json1, MemoriesJsonContext.Options);
        string json2 = JsonSerializer.Serialize(deserialized, MemoriesJsonContext.Options);

        json2.ShouldBe(json1);
    }

    [Fact]
    public void PropertyNames_ShouldBeCamelCase()
    {
        var original = new TenantStatusUpdateInput("tenant-1", TenantStatus.Failed, "workflow-1");
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);

        json.ShouldContain("\"tenantId\":");
        json.ShouldContain("\"status\":");
        json.ShouldContain("\"workflowInstanceId\":");
    }

    [Fact]
    public void Deserialized_ShouldMatchOriginalValues()
    {
        var original = new TenantStatusUpdateInput("my-tenant", TenantStatus.CompensationFailed);
        string json = JsonSerializer.Serialize(original, MemoriesJsonContext.Options);
        TenantStatusUpdateInput? deserialized = JsonSerializer.Deserialize<TenantStatusUpdateInput>(json, MemoriesJsonContext.Options);

        deserialized.ShouldNotBeNull();
        deserialized.TenantId.ShouldBe("my-tenant");
        deserialized.Status.ShouldBe(TenantStatus.CompensationFailed);
        deserialized.WorkflowInstanceId.ShouldBeNull();
    }
}
