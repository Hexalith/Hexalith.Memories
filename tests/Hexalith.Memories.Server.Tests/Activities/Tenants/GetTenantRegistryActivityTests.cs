namespace Hexalith.Memories.Server.Tests.Activities.Tenants;

using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Tenants;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

public class GetTenantRegistryActivityTests
{
    [Fact]
    public async Task RunAsync_TenantExists_ShouldReturnTenantInfo()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ILogger<TenantRegistryService> serviceLogger = Substitute.For<ILogger<TenantRegistryService>>();
        TenantRegistryService registry = new(daprClient, serviceLogger);

        var expected = new TenantInfo("test-tenant", "Test Tenant", TenantStatus.Active, DateTimeOffset.UtcNow);
        var entry = new TenantRegistryEntry(expected, null);
        daprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                "statestore",
                "tenant-registry-test-tenant",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);

        GetTenantRegistryActivity activity = new(registry);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        TenantInfo? result = await activity.RunAsync(context, "test-tenant");

        result.ShouldNotBeNull();
        result.Id.ShouldBe("test-tenant");
        result.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task RunAsync_TenantNotFound_ShouldReturnNull()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ILogger<TenantRegistryService> serviceLogger = Substitute.For<ILogger<TenantRegistryService>>();
        TenantRegistryService registry = new(daprClient, serviceLogger);

        daprClient.GetStateAsync<StoredTenantRegistryEntry?>(
                "statestore",
                "tenant-registry-nonexistent",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((StoredTenantRegistryEntry?)null);

        GetTenantRegistryActivity activity = new(registry);
        WorkflowActivityContext context = Substitute.For<WorkflowActivityContext>();

        TenantInfo? result = await activity.RunAsync(context, "nonexistent");

        result.ShouldBeNull();
    }
}
