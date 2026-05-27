namespace Hexalith.Memories.Server.Tests.Tenants;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

public class TenantStatusGuardTests
{
    [Fact]
    public async Task ValidateTenantActiveAsync_TenantNotFound_ReturnsTenantNotFound()
    {
        TenantRegistryService registry = CreateRegistryReturning(null);
        TenantStatusGuard guard = new(registry);

        ErrorResponse? result = await guard.ValidateTenantActiveAsync("nonexistent", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task ValidateTenantActiveAsync_ActiveTenant_ReturnsNull()
    {
        TenantRegistryService registry = CreateRegistryReturning(
            new TenantInfo("active-tenant", "Active", TenantStatus.Active, DateTimeOffset.UtcNow));
        TenantStatusGuard guard = new(registry);

        ErrorResponse? result = await guard.ValidateTenantActiveAsync("active-tenant", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ValidateTenantActiveAsync_DeletingTenant_ReturnsTenantDeleting()
    {
        TenantRegistryService registry = CreateRegistryReturning(
            new TenantInfo("deleting-tenant", "Deleting", TenantStatus.Deleting, DateTimeOffset.UtcNow));
        TenantStatusGuard guard = new(registry);

        ErrorResponse? result = await guard.ValidateTenantActiveAsync("deleting-tenant", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Code.ShouldBe("TENANT_DELETING");
    }

    [Fact]
    public async Task ValidateTenantActiveAsync_ProvisioningTenant_ReturnsTenantProvisioning()
    {
        TenantRegistryService registry = CreateRegistryReturning(
            new TenantInfo("provisioning-tenant", "Provisioning", TenantStatus.Provisioning, DateTimeOffset.UtcNow));
        TenantStatusGuard guard = new(registry);

        ErrorResponse? result = await guard.ValidateTenantActiveAsync("provisioning-tenant", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Code.ShouldBe("TENANT_PROVISIONING");
    }

    [Fact]
    public async Task ValidateTenantActiveAsync_FailedTenant_ReturnsTenantFailed()
    {
        TenantRegistryService registry = CreateRegistryReturning(
            new TenantInfo("failed-tenant", "Failed", TenantStatus.Failed, DateTimeOffset.UtcNow));
        TenantStatusGuard guard = new(registry);

        ErrorResponse? result = await guard.ValidateTenantActiveAsync("failed-tenant", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Code.ShouldBe("TENANT_FAILED");
    }

    private static TenantRegistryService CreateRegistryReturning(TenantInfo? tenant)
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ILogger<TenantRegistryService> logger = Substitute.For<ILogger<TenantRegistryService>>();

        string tenantId = tenant?.Id ?? "nonexistent";
        TenantRegistryEntry? entry = tenant is not null
            ? new TenantRegistryEntry(tenant, null)
            : null;

        daprClient.GetStateAsync<TenantRegistryEntry?>(
                "statestore",
                $"tenant-registry-{tenantId}",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(entry);

        return new TenantRegistryService(daprClient, logger);
    }
}
