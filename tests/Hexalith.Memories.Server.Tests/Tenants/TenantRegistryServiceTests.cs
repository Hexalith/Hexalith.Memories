#pragma warning disable CS8620 // Nullability mismatch in NSubstitute Returns() with nullable Dapr client methods.

// <copyright file="TenantRegistryServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Tenants;

using System.Text.Json;

using Dapr;
using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore.Domain.Commands;
using Hexalith.Memories.Server.EventStoreIntegration;
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
        List<string> operationLog = [];
        CapturingCommandStore commandStore = new(operationLog);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(), string.Empty));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                operationLog.Add("transaction:registry");
                return Task.CompletedTask;
            });

        TenantRegistryService service = CreateService(daprClient, commandStore);

        TenantInfo result = await service.RegisterTenantAsync("acme", "Acme Corp", CancellationToken.None);

        result.Id.ShouldBe("acme");
        result.DisplayName.ShouldBe("Acme Corp");
        result.Status.ShouldBe(TenantStatus.Provisioning);
        RegisterTenantCommand command = commandStore.AcceptedCommands.ShouldHaveSingleItem().ShouldBeOfType<RegisterTenantCommand>();
        command.TenantId.ShouldBe("acme");
        command.DisplayName.ShouldBe("Acme Corp");
        operationLog[0].ShouldBe($"accept:{RegisterTenantCommand.CommandType}");
        operationLog[1].ShouldBe("transaction:registry");

        await daprClient.Received(1).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Is<IReadOnlyList<StateTransactionRequest>>(ops =>
                ops.Count == 2
                && ops[0].Key == "tenant-registry-acme"
                && ops[0].OperationType == StateOperationType.Upsert
                && ops[0].ETag == string.Empty
                && Deserialize<TenantRegistryEntry>(ops[0]).Tenant.Status == TenantStatus.Provisioning
                && Deserialize<TenantRegistryEntry>(ops[0]).WorkflowInstanceId == null
                && ops[1].Key == "tenant-registry-index"
                && ops[1].OperationType == StateOperationType.Upsert
                && ops[1].ETag == string.Empty
                && Deserialize<List<string>>(ops[1]).SequenceEqual(new[] { "acme" })),
            null!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantAsync_WhenTransactionFails_DoesNotDeleteTenantEntry()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(), string.Empty));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new DaprException("transaction conflict"));
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (TenantRegistryEntry?)null);
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => []);

        TenantRegistryService service = CreateService(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(() => service.RegisterTenantAsync("acme", "Acme Corp", CancellationToken.None));

        await daprClient.DidNotReceive().DeleteStateAsync(
            "statestore",
            "tenant-registry-acme",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantAsync_WhenTransactionConflictsButEndStateIsConsistent_ReturnsCurrentEntry()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry current = CreateEntry("acme", "Acme Corp", TenantStatus.Provisioning);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(), string.Empty));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new DaprException("transaction conflict"));
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => current);
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ["acme"]);

        TenantRegistryService service = CreateService(daprClient, commandStore);

        TenantInfo result = await service.RegisterTenantAsync("acme", "Acme Corp", CancellationToken.None);

        result.ShouldBe(current.Tenant);
        commandStore.AcceptedCommands.ShouldHaveSingleItem();
        await daprClient.Received(3).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
            null!,
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().DeleteStateAsync(
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
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry existing = CreateEntry(
            "tenant-1",
            "Test",
            TenantStatus.Provisioning,
            "workflow-123",
            new DateTimeOffset(2026, 4, 20, 8, 0, 0, TimeSpan.Zero));
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient, commandStore);

        await service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Active, CancellationToken.None, "workflow-123");

        UpdateTenantLifecycleStatusCommand command = commandStore.AcceptedCommands
            .ShouldHaveSingleItem()
            .ShouldBeOfType<UpdateTenantLifecycleStatusCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.Status.ShouldBe(TenantStatus.Active);
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Is<TenantRegistryEntry>(t =>
                t.Tenant.Status == TenantStatus.Active
                && t.WorkflowInstanceId == null
                && t.LastUpdated > existing.LastUpdated),
            "etag-1",
            cancellationToken: Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().SaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantAsync_WhenEntryAlreadyExists_ReturnsExistingWithoutAppendingDuplicateIndex()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("acme", "Acme Corp", TenantStatus.Active);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-entry"));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<string>(["acme"]), "etag-index"));

        TenantRegistryService service = CreateService(daprClient);

        TenantInfo result = await service.RegisterTenantAsync("acme", "New Name Ignored", CancellationToken.None);

        result.ShouldBe(existing.Tenant);
        await daprClient.DidNotReceive().ExecuteStateTransactionAsync(
            "statestore",
            Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
            null!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTenantAsync_WhenEntryAlreadyExistsButIndexMissing_RepairsIndexInTransaction()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry existing = CreateEntry("acme", "Acme Corp", TenantStatus.Active);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-acme", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-entry"));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<string>(["other"]), "etag-index"));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TenantRegistryService service = CreateService(daprClient, commandStore);

        TenantInfo result = await service.RegisterTenantAsync("acme", "New Name Ignored", CancellationToken.None);

        result.ShouldBe(existing.Tenant);
        commandStore.AcceptedCommands.ShouldBeEmpty();
        await daprClient.Received(1).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Is<IReadOnlyList<StateTransactionRequest>>(ops =>
                ops.Count == 2
                && ops[0].Key == "tenant-registry-acme"
                && ops[0].OperationType == StateOperationType.Upsert
                && ops[0].ETag == "etag-entry"
                && Deserialize<TenantRegistryEntry>(ops[0]).Tenant.Id == "acme"
                && ops[1].Key == "tenant-registry-index"
                && ops[1].OperationType == StateOperationType.Upsert
                && ops[1].ETag == "etag-index"
                && Deserialize<List<string>>(ops[1]).SequenceEqual(new[] { "other", "acme" })),
            null!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_WhenFirstCasConflicts_RetriesWithoutAcceptingCommandAgain()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry first = CreateEntry("tenant-1", "Test", TenantStatus.Provisioning, "workflow-123");
        TenantRegistryEntry second = first with { LastUpdated = DateTimeOffset.UtcNow.AddSeconds(1) };
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((first, "etag-1"), (second, "etag-2"));
        daprClient.TrySaveStateAsync(
                "statestore",
                "tenant-registry-tenant-1",
                Arg.Any<TenantRegistryEntry>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false, true);

        TenantRegistryService service = CreateService(daprClient, commandStore);

        await service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Active, CancellationToken.None, "workflow-123");

        commandStore.AcceptedCommands.ShouldHaveSingleItem();
        await daprClient.Received(2).GetStateAndETagAsync<TenantRegistryEntry?>(
            "statestore",
            "tenant-registry-tenant-1",
            cancellationToken: Arg.Any<CancellationToken>());
        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            "etag-2",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_WhenCasBudgetExhausted_ThrowsPreciseError()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Provisioning, "workflow-123");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), Arg.Any<string>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        TenantRegistryService service = CreateService(daprClient);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Active, CancellationToken.None, "workflow-123"));

        ex.Message.ShouldContain("after 3 attempts");
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_WhenTenantMissing_Throws()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-missing", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));

        TenantRegistryService service = CreateService(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateTenantStatusAsync("missing", TenantStatus.Active, CancellationToken.None, "workflow-123"));
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_ProvisioningTransition_PreservesWorkflowOwnership()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Failed, "workflow-old");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient);

        await service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Provisioning, CancellationToken.None, "workflow-new");

        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Is<TenantRegistryEntry>(entry => entry.WorkflowInstanceId == "workflow-new"),
            "etag-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTenantStatusAsync_StaleRollbackAgainstNewerDeletingOwner_IsBlocked()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry deleting = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-newer");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((deleting, "etag-1"));

        TenantRegistryService service = CreateService(daprClient, commandStore);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateTenantStatusAsync("tenant-1", TenantStatus.Active, CancellationToken.None, "delete-older"));

        ex.Message.ShouldContain("cannot be overwritten");
        commandStore.AcceptedCommands.ShouldBeEmpty();
        await daprClient.DidNotReceive().TrySaveStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            Arg.Any<TenantRegistryEntry>(),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BeginTenantDeletionAsync_ActiveTenant_TransitionsToDeletingAndStoresWorkflowId()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        CapturingCommandStore commandStore = new();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Active);
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-1"));
        daprClient.TrySaveStateAsync("statestore", "tenant-registry-tenant-1", Arg.Any<TenantRegistryEntry>(), "etag-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        TenantRegistryService service = CreateService(daprClient, commandStore);

        TenantRegistryEntry? result = await service.BeginTenantDeletionAsync("tenant-1", "delete-tenant-1-abc", false, null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Tenant.Status.ShouldBe(TenantStatus.Deleting);
        result.WorkflowInstanceId.ShouldBe("delete-tenant-1-abc");
        UpdateTenantLifecycleStatusCommand command = commandStore.AcceptedCommands
            .ShouldHaveSingleItem()
            .ShouldBeOfType<UpdateTenantLifecycleStatusCommand>();
        command.TenantId.ShouldBe("tenant-1");
        command.Status.ShouldBe(TenantStatus.Deleting);
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
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-1");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-entry"));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(["tenant-1", "tenant-2"]), "etag-index"));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TenantRegistryService service = CreateService(daprClient);

        await service.RemoveTenantAsync("tenant-1", CancellationToken.None);

        await daprClient.Received(1).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Is<IReadOnlyList<StateTransactionRequest>>(ops =>
                ops.Count == 2
                && ops[0].Key == "tenant-registry-tenant-1"
                && ops[0].OperationType == StateOperationType.Delete
                && ops[0].ETag == "etag-entry"
                && ops[1].Key == "tenant-registry-index"
                && ops[1].OperationType == StateOperationType.Upsert
                && ops[1].ETag == "etag-index"
                && Deserialize<List<string>>(ops[1]).SequenceEqual(new[] { "tenant-2" })),
            null!,
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().DeleteStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveTenantAsync_WhenEntryMissingButIndexStale_CleansIndexInTransaction()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (new List<string>(["tenant-1", "tenant-2"]), "etag-index"));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        TenantRegistryService service = CreateService(daprClient);

        await service.RemoveTenantAsync("tenant-1", CancellationToken.None);

        await daprClient.Received(1).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Is<IReadOnlyList<StateTransactionRequest>>(ops =>
                ops.Count == 1
                && ops[0].Key == "tenant-registry-index"
                && Deserialize<List<string>>(ops[0]).SequenceEqual(new[] { "tenant-2" })),
            null!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveTenantAsync_WhenTransactionConflictsAndEndStateConsistent_ReturnsSuccess()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-1");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-entry"));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<string>(["tenant-1"]), "etag-index"));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new DaprException("transaction conflict"));
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => (TenantRegistryEntry?)null);
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => []);

        TenantRegistryService service = CreateService(daprClient);

        await service.RemoveTenantAsync("tenant-1", CancellationToken.None);
    }

    [Fact]
    public async Task RemoveTenantAsync_WhenTransactionConflictsAndEndStateIsInconsistent_ThrowsWithoutDirectDelete()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantRegistryEntry existing = CreateEntry("tenant-1", "Test", TenantStatus.Deleting, "delete-1");
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((existing, "etag-entry"));
        daprClient.GetStateAndETagAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns((new List<string>(["tenant-1"]), "etag-index"));
        daprClient.ExecuteStateTransactionAsync(
                "statestore",
                Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
                null!,
                Arg.Any<CancellationToken>())
            .Returns(_ => throw new DaprException("transaction conflict"));
        daprClient.GetStateAsync<TenantRegistryEntry?>("statestore", "tenant-registry-tenant-1", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => existing);
        daprClient.GetStateAsync<List<string>?>("statestore", "tenant-registry-index", cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => []);

        TenantRegistryService service = CreateService(daprClient);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            service.RemoveTenantAsync("tenant-1", CancellationToken.None));

        ex.Message.ShouldContain("Failed to remove tenant 'tenant-1' from registry after 3 attempts");
        await daprClient.Received(3).ExecuteStateTransactionAsync(
            "statestore",
            Arg.Any<IReadOnlyList<StateTransactionRequest>>(),
            null!,
            Arg.Any<CancellationToken>());
        await daprClient.DidNotReceive().DeleteStateAsync(
            "statestore",
            "tenant-registry-tenant-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // Story 5.5 AC3: UpdateTenantDisplayNameAsync — ETag CAS, operational log, not-found path.
    [Fact]
    public async Task UpdateTenantDisplayNameAsync_UpdatesDisplayName_AndEmitsLog()
    {
        TenantRegistryEntry existing = CreateEntry(
            "acme",
            "Old Name",
            TenantStatus.Active,
            lastUpdated: new DateTimeOffset(2026, 4, 20, 8, 0, 0, TimeSpan.Zero));
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)existing, "etag-42"));
        daprClient.TrySaveStateAsync(
                "statestore",
                "tenant-registry-acme",
                Arg.Any<TenantRegistryEntry>(),
                "etag-42",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        List<(LogLevel Level, EventId EventId, string Message)> logs = [];
        ILogger<TenantRegistryService> logger = new ListLogger<TenantRegistryService>(logs);
        TenantRegistryService service = new(daprClient, logger);

        TenantInfo result = await service.UpdateTenantDisplayNameAsync(
            "acme",
            "operator@127.0.0.1",
            "Acme Inc",
            CancellationToken.None);

        result.DisplayName.ShouldBe("Acme Inc");

        await daprClient.Received(1).TrySaveStateAsync(
            "statestore",
            "tenant-registry-acme",
            Arg.Is<TenantRegistryEntry>(e => e.Tenant.DisplayName == "Acme Inc" && e.LastUpdated > existing.LastUpdated),
            "etag-42",
            cancellationToken: Arg.Any<CancellationToken>());

        logs.ShouldContain(l => l.EventId.Id == 5501 && l.Level == LogLevel.Information && l.Message.Contains("field=displayName") && l.Message.Contains("oldValue=Old Name") && l.Message.Contains("newValue=Acme Inc") && l.Message.Contains("actor=operator@127.0.0.1"));
    }

    [Fact]
    public async Task UpdateTenantDisplayNameAsync_WhenTenantMissing_Throws()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-missing",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)null, string.Empty));

        TenantRegistryService service = CreateService(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateTenantDisplayNameAsync("missing", "actor@1.2.3.4", "New", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTenantDisplayNameAsync_WhenSameValue_LogsAndReturns_WithoutSave()
    {
        TenantRegistryEntry existing = CreateEntry("acme", "Acme Corp", TenantStatus.Active);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)existing, "etag-x"));

        List<(LogLevel Level, EventId EventId, string Message)> logs = [];
        ILogger<TenantRegistryService> logger = new ListLogger<TenantRegistryService>(logs);
        TenantRegistryService service = new(daprClient, logger);

        TenantInfo result = await service.UpdateTenantDisplayNameAsync(
            "acme",
            "actor@1.2.3.4",
            "Acme Corp",
            CancellationToken.None);

        result.DisplayName.ShouldBe("Acme Corp");

        await daprClient.DidNotReceive().TrySaveStateAsync(
            "statestore",
            "tenant-registry-acme",
            Arg.Any<TenantRegistryEntry>(),
            Arg.Any<string>(),
            cancellationToken: Arg.Any<CancellationToken>());

        logs.ShouldContain(l => l.EventId.Id == 5501);
    }

    [Fact]
    public async Task UpdateTenantDisplayNameAsync_RetriesOnConcurrentWrite_UntilBudgetExhausted()
    {
        TenantRegistryEntry existing = CreateEntry("acme", "Old", TenantStatus.Active);
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAndETagAsync<TenantRegistryEntry?>(
                "statestore",
                "tenant-registry-acme",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => ((TenantRegistryEntry?)existing, "etag-1"));
        daprClient.TrySaveStateAsync(
                "statestore",
                "tenant-registry-acme",
                Arg.Any<TenantRegistryEntry>(),
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        TenantRegistryService service = CreateService(daprClient);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            service.UpdateTenantDisplayNameAsync("acme", "actor@x", "New", CancellationToken.None));
    }

    private sealed class ListLogger<TCategory>(List<(LogLevel Level, EventId EventId, string Message)> sink) : ILogger<TCategory>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => sink.Add((logLevel, eventId, formatter(state, exception)));
    }

    private static TenantRegistryEntry CreateEntry(
        string tenantId,
        string displayName,
        TenantStatus status,
        string? workflowInstanceId = null,
        DateTimeOffset? lastUpdated = null)
        => new(
            new TenantInfo(tenantId, displayName, status, DateTimeOffset.UtcNow),
            workflowInstanceId,
            lastUpdated ?? DateTimeOffset.UtcNow);

    private static T Deserialize<T>(StateTransactionRequest request)
        => JsonSerializer.Deserialize<T>(request.Value, MemoriesJsonContext.Options)!;

    private sealed class CapturingCommandStore(List<string>? operationLog = null) : IMemoriesCommandStore
    {
        public List<object> AcceptedCommands { get; } = [];

        public Task<string> AcceptAsync<TCommand>(
            string tenantId,
            TCommand command,
            string actorId,
            CancellationToken cancellationToken)
            where TCommand : IMemoriesCommandContract
        {
            operationLog?.Add($"accept:{TCommand.CommandType}");
            AcceptedCommands.Add(command);
            return Task.FromResult($"{TCommand.CommandType}:{command.AggregateId}");
        }
    }

    private static TenantRegistryService CreateService(DaprClient daprClient, IMemoriesCommandStore? commandStore = null)
    {
        ILogger<TenantRegistryService> logger = Substitute.For<ILogger<TenantRegistryService>>();
        return new TenantRegistryService(daprClient, logger, commandStore);
    }
}
