#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantRegistryServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

public class TenantRegistryServiceTests
{
    [Fact]
    public async Task RegisterTenantAsync_CreatesEntryWithProvisioningStatus()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-acme", Arg.Any<TenantRegistryEntry>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);
        daprClient.GetStateAndETagAsync<List<string>>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(), string.Empty));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-index", Arg.Any<List<string>>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient);

        TenantInfo result = await service.RegisterTenantAsync("acme", "Acme Corp", CancellationToken.None);

        result.Id.ShouldBe("acme");
        result.DisplayName.ShouldBe("Acme Corp");
        result.Status.ShouldBe(TenantStatus.Provisioning);

        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-acme",
            Arg.Is<TenantRegistryEntry>(t =>
                t.Tenant.Id == "acme"
                && t.Tenant.DisplayName == "Acme Corp"
                && t.Tenant.Status == TenantStatus.Provisioning
                && t.WorkflowInstanceId == null),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantAsync_WhenIndexUpdateFails_DeletesTenantEntry()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-acme", Arg.Any<TenantRegistryEntry>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);
        daprClient.GetStateAndETagAsync<List<string>>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(), string.Empty));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-index", Arg.Any<List<string>>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        TenantRegistryService service = CreateService(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(() => service.RegisterTenantAsync("acme", "Acme Corp", CancellationToken.None));

        await daprClient.Received(1).DeleteStateAsync(
            "statestore",
            "tenant-registry-acme",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTenantAsync_ReturnsTenantInfo()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry expected = CreateEntry("tenant-1", "Test", TenantStatus.Active);
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(expected);

        TenantRegistryService service = CreateService(daprClient);

        TenantInfo? result = await service.GetTenantAsync("tenant-1", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe("tenant-1");
        result.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public async Task GetTenantAsync_NotFound_ReturnsNull()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-nonexistent", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (TenantRegistryEntry?)null);

        TenantRegistryService service = CreateService(daprClient);

        TenantInfo? result = await service.GetTenantAsync("nonexistent", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task TenantExistsAsync_ExistingTenant_ReturnsTrue()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry tenant = CreateEntry("existing", "Existing", TenantStatus.Active);
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-existing", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(tenant);

        TenantRegistryService service = CreateService(daprClient);

        bool result = await service.TenantExistsAsync("existing", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task TenantExistsAsync_NonExistent_ReturnsFalse()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-nope", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (TenantRegistryEntry?)null);

        TenantRegistryService service = CreateService(daprClient);

        bool result = await service.TenantExistsAsync("nope", CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_UpdatesStatusAndClearsWorkflowOwnership()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Provisioning, "workflow-123");
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(existing);

        TenantRegistryService service = CreateService(daprClient);

        await service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Active, CancellationToken.None);

        await daprClient.Received(1).SaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Is<TenantRegistryEntry>(t => t.Tenant.Status == TenantStatus.Active && t.WorkflowInstanceId == null),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_ActiveTenant_TransitionsToDeletingAndStoresWorkflowId()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Active);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-tenant-1-abc", false, null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Tenant.Status.ShouldBe(TenantStatus.Deleting);
        result.WorkflowInstanceId.ShouldBe("delete-tenant-1-abc");
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Is<TenantRegistryEntry>(t => t.Tenant.Status == TenantStatus.Deleting && t.WorkflowInstanceId == "delete-tenant-1-abc"),
            "etag-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_DeletingTenantWithoutRetry_ReturnsExistingOwner()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-existing");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));

        TenantRegistryService service = CreateService(daprClient);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-new", false, "delete-existing", CancellationToken.None);

        result.ShouldBe(existing);
        await daprClient.DidNotReceive().TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_ProvisioningTenant_ReturnsExistingEntryWithoutSaving()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Provisioning, "provision-1");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));

        TenantRegistryService service = CreateService(daprClient);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-new", false, existing.WorkflowInstanceId, CancellationToken.None);

        result.ShouldBe(existing);
        await daprClient.DidNotReceive().TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_DeletingTenantWithRetry_ReassignsWorkflowId()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-old");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-new", true, "delete-old", CancellationToken.None);

        result.ShouldNotBeNull();
        result.Tenant.Status.ShouldBe(TenantStatus.Deleting);
        result.WorkflowInstanceId.ShouldBe("delete-new");
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_DeletingTenantWithRetry_WhenOwnerChanged_ReturnsCurrentOwner()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-current");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));

        TenantRegistryService service = CreateService(daprClient);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-new", true, "delete-old", CancellationToken.None);

        result.ShouldBe(existing);
        await daprClient.DidNotReceive().TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTenantsAsync_ReturnsAllRegisteredTenants()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        List<string> index = ["tenant-a", "tenant-b"];
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(index);

        TenantRegistryEntry tenantA = CreateEntry("tenant-a", "A", TenantStatus.Active);
        TenantRegistryEntry tenantB = CreateEntry("tenant-b", "B", TenantStatus.Provisioning, "workflow-456");
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-a", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(tenantA);
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-b", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(tenantB);

        TenantRegistryService service = CreateService(daprClient);

        IReadOnlyList<TenantInfo> results = await service.ListTenantsAsync(CancellationToken.None);

        results.Count.ShouldBe(2);
        results[0].Id.ShouldBe("tenant-a");
        results[1].Id.ShouldBe("tenant-b");
    }

    [Fact]
    public async Task RemoveTenantAsync_DeletesEntryAndUpdatesIndex()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<List<string>>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(["tenant-1", "tenant-2"]), "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-index", Arg.Any<List<string>>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient);

        await service.RemoveTenantAsync("tenant-1", CancellationToken.None);

        await daprClient.Received(1).DeleteStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            cancellationToken: Arg.Any<CancellationToken>());
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-index",
            Arg.Is<List<string>>(list => list.Count == 1 && list[0] == "tenant-2"),
            "etag-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static TenantRegistryEntry CreateEntry(string tenantId, string displayName, TenantStatus status, string? workflowInstanceId = null)
        => new(new TenantInfo(tenantId, displayName, status, DateTimeOffset.UtcNow), workflowInstanceId);

    private static TenantRegistryService CreateService(DaprClient daprClient)
    {
        ILogger<TenantRegistryService> logger = Substitute.For<ILogger<TenantRegistryService>>();
        return new TenantRegistryService(daprClient, logger);
    }
}
