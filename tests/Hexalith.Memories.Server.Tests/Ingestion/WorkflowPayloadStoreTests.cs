// <copyright file="WorkflowPayloadStoreTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class WorkflowPayloadStoreTests
{
    [Fact]
    public async Task SaveAsync_StoresPayloadWithReferenceAndTtlMetadata()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        WorkflowPayloadStoreEntry? captured = null;
        IReadOnlyDictionary<string, string>? metadata = null;
        daprClient
            .SaveStateAsync(
                "statestore",
                Arg.Any<string>(),
                Arg.Do<WorkflowPayloadStoreEntry>(entry => captured = entry),
                metadata: Arg.Do<IReadOnlyDictionary<string, string>>(m => metadata = m),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        DaprWorkflowPayloadStore store = CreateStore(daprClient);

        WorkflowPayloadReference reference = await store.SaveAsync(
            "tenant-a",
            "mu-1",
            WorkflowPayloadKind.SourceBytes,
            new byte[] { 1, 2, 3 },
            idSuffix: "source");

        reference.TenantId.ShouldBe("tenant-a");
        reference.MemoryUnitId.ShouldBe("mu-1");
        reference.ByteLength.ShouldBe(3);
        reference.ContentKind.ShouldBe(WorkflowPayloadKind.SourceBytes);
        captured.ShouldNotBeNull();
        captured.Reference.ShouldBe(reference);
        captured.Payload.ShouldBe([1, 2, 3]);
        metadata.ShouldNotBeNull();
        metadata["ttlInSeconds"].ShouldBe("86400");
    }

    [Fact]
    public async Task ReadAsync_WithStoredPayload_ReturnsBytes()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        DaprWorkflowPayloadStore store = CreateStore(daprClient);
        WorkflowPayloadReference reference = await SaveAndCaptureAsync(daprClient, store);
        WorkflowPayloadStoreEntry entry = new(reference, [1, 2, 3], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        daprClient.GetStateAsync<WorkflowPayloadStoreEntry?>(
                "statestore",
                $"tenant-a:workflow-payload:{reference.Id}",
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((WorkflowPayloadStoreEntry?)entry);

        byte[] bytes = await store.ReadAsync(reference, "tenant-a", "mu-1", WorkflowPayloadKind.SourceBytes);

        bytes.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ReadAsync_MissingPayload_ThrowsStructuredException()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        DaprWorkflowPayloadStore store = CreateStore(daprClient);
        WorkflowPayloadReference reference = await SaveAndCaptureAsync(daprClient, store);
        daprClient.GetStateAsync<WorkflowPayloadStoreEntry?>(
                "statestore",
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((WorkflowPayloadStoreEntry?)null);

        WorkflowPayloadException ex = await Should.ThrowAsync<WorkflowPayloadException>(
            () => store.ReadAsync(reference, "tenant-a", "mu-1", WorkflowPayloadKind.SourceBytes));

        ex.ErrorCode.ShouldBe("PAYLOAD_NOT_FOUND");
    }

    [Fact]
    public async Task ReadAsync_HashMismatch_ThrowsStructuredException()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        DaprWorkflowPayloadStore store = CreateStore(daprClient);
        WorkflowPayloadReference reference = await SaveAndCaptureAsync(daprClient, store);
        WorkflowPayloadStoreEntry entry = new(reference, [9, 9, 9], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        daprClient.GetStateAsync<WorkflowPayloadStoreEntry?>(
                "statestore",
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns((WorkflowPayloadStoreEntry?)entry);

        WorkflowPayloadException ex = await Should.ThrowAsync<WorkflowPayloadException>(
            () => store.ReadAsync(reference, "tenant-a", "mu-1", WorkflowPayloadKind.SourceBytes));

        ex.ErrorCode.ShouldBe("PAYLOAD_HASH_MISMATCH");
    }

    [Fact]
    public async Task ReadAsync_TenantMismatch_ThrowsBeforeReadingStore()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        DaprWorkflowPayloadStore store = CreateStore(daprClient);
        WorkflowPayloadReference reference = await SaveAndCaptureAsync(daprClient, store);

        WorkflowPayloadException ex = await Should.ThrowAsync<WorkflowPayloadException>(
            () => store.ReadAsync(reference, "tenant-b", "mu-1", WorkflowPayloadKind.SourceBytes));

        ex.ErrorCode.ShouldBe("PAYLOAD_TENANT_MISMATCH");
        await daprClient.DidNotReceiveWithAnyArgs()
            .GetStateAsync<WorkflowPayloadStoreEntry?>(default!, default!, cancellationToken: default);
    }

    [Fact]
    public async Task DeleteAsync_DeletesTenantScopedStateKey()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        DaprWorkflowPayloadStore store = CreateStore(daprClient);
        WorkflowPayloadReference reference = await SaveAndCaptureAsync(daprClient, store);

        await store.DeleteAsync(reference);

        await daprClient.Received(1).DeleteStateAsync(
            "statestore",
            $"tenant-a:workflow-payload:{reference.Id}",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static DaprWorkflowPayloadStore CreateStore(DaprClient daprClient)
        => new(
            daprClient,
            Options.Create(new WorkflowPayloadStoreOptions { TtlHours = 24, StateStoreName = "statestore" }),
            TimeProvider.System);

    private static async Task<WorkflowPayloadReference> SaveAndCaptureAsync(
        DaprClient daprClient,
        DaprWorkflowPayloadStore store)
    {
        daprClient
            .SaveStateAsync(
                "statestore",
                Arg.Any<string>(),
                Arg.Any<WorkflowPayloadStoreEntry>(),
                metadata: Arg.Any<IReadOnlyDictionary<string, string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return await store.SaveAsync("tenant-a", "mu-1", WorkflowPayloadKind.SourceBytes, new byte[] { 1, 2, 3 }, "source");
    }
}
