// <copyright file="DaprIngestionWorkflowStateReaderTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Reflection;

using Dapr.Common.Serialization;
using Dapr.Workflow;

using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

public sealed class DaprIngestionWorkflowStateReaderTests
{
    [Fact]
    public async Task GetWorkflowStateAsync_WhenStateMissing_PrunesTrackedInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync("instance-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowStateReader reader = new(
            workflowClient,
            registry,
            NullLogger<DaprIngestionWorkflowStateReader>.Instance);

        _ = await reader.GetWorkflowStateAsync("instance-1", true, CancellationToken.None);

        await registry.Received(1).RemoveAsync("instance-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkflowStateAsync_WhenStateTerminal_PrunesTrackedInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync("instance-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("instance-1", WorkflowRuntimeStatus.Completed)));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowStateReader reader = new(
            workflowClient,
            registry,
            NullLogger<DaprIngestionWorkflowStateReader>.Instance);

        _ = await reader.GetWorkflowStateAsync("instance-1", true, CancellationToken.None);

        await registry.Received(1).RemoveAsync("instance-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkflowStateAsync_WhenStateCanceled_PrunesTrackedInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync("instance-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("instance-1", WorkflowRuntimeStatus.Canceled)));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowStateReader reader = new(
            workflowClient,
            registry,
            NullLogger<DaprIngestionWorkflowStateReader>.Instance);

        _ = await reader.GetWorkflowStateAsync("instance-1", true, CancellationToken.None);

        await registry.Received(1).RemoveAsync("instance-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkflowStateAsync_WhenStateActive_DoesNotPruneTrackedInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync("instance-1", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("instance-1", WorkflowRuntimeStatus.Running)));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowStateReader reader = new(
            workflowClient,
            registry,
            NullLogger<DaprIngestionWorkflowStateReader>.Instance);

        _ = await reader.GetWorkflowStateAsync("instance-1", true, CancellationToken.None);

        await registry.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    private static WorkflowState CreateState(string instanceId, WorkflowRuntimeStatus status)
    {
        Type metadataType = typeof(WorkflowState).Assembly.GetType("Dapr.Workflow.Client.WorkflowMetadata")!;
        object metadata = Activator.CreateInstance(
            metadataType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                instanceId,
                nameof(IngestionWorkflow),
                status,
                DateTime.UtcNow,
                DateTime.UtcNow,
                new JsonDaprSerializer(),
            ],
            culture: null)!;

        return (WorkflowState)Activator.CreateInstance(
            typeof(WorkflowState),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [metadata],
            culture: null)!;
    }
}
