// <copyright file="WorkflowReplaySafetyHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Hosting;

using System.Reflection;

using Dapr.Common.Serialization;
using Dapr.Workflow;
using Dapr.Workflow.Client;

using Hexalith.Memories.Server.Hosting;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

public class WorkflowReplaySafetyHostedServiceTests
{
    [Fact]
    public void IsActive_TerminalStates_ReturnFalse()
    {
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Completed).ShouldBeFalse();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Failed).ShouldBeFalse();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Canceled).ShouldBeFalse();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Terminated).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_NonTerminalStates_ReturnTrue()
    {
        // Running + Suspended + Pending should all delay startup for the tracked workflow family.
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Running).ShouldBeTrue();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Suspended).ShouldBeTrue();
        WorkflowReplaySafetyHostedService.IsActive(WorkflowRuntimeStatus.Pending).ShouldBeTrue();
    }

    [Fact]
    public void ShouldCountWorkflow_OnlyTracksIngestionWorkflow()
    {
        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "IngestionWorkflow",
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "ConsistencyVerificationWorkflow",
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();

        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: "IngestionWorkflow",
            exists: false,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();
    }

    [Fact]
    public void ShouldBlockForUnreadableWorkflowName_ActiveStatesReturnTrue()
    {
        WorkflowReplaySafetyHostedService.ShouldBlockForUnreadableWorkflowName(
            exists: true,
            status: WorkflowRuntimeStatus.Running).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldBlockForUnreadableWorkflowName(
            exists: true,
            status: WorkflowRuntimeStatus.Pending).ShouldBeTrue();

        WorkflowReplaySafetyHostedService.ShouldBlockForUnreadableWorkflowName(
            exists: true,
            status: WorkflowRuntimeStatus.Completed).ShouldBeFalse();

        WorkflowReplaySafetyHostedService.ShouldBlockForUnreadableWorkflowName(
            exists: true,
            status: WorkflowRuntimeStatus.Canceled).ShouldBeFalse();

        WorkflowReplaySafetyHostedService.ShouldBlockForUnreadableWorkflowName(
            exists: false,
            status: WorkflowRuntimeStatus.Running).ShouldBeFalse();
    }

    // S6-D2 follow-up (code review 2026-04-25): the old fail-open helper was replaced by
    // conservative blocking semantics for unreadable active workflows. Full TryCountInFlightAsync
    // end-to-end coverage remains gated on S6-P9.

    [Fact]
    public void Timeouts_MatchDocumentedEnvelope()
    {
        // Improvement X split: 5s poll, 5min total, 10s per-call (Improvement Z).
        WorkflowReplaySafetyHostedService.PollInterval.ShouldBe(TimeSpan.FromSeconds(5));
        WorkflowReplaySafetyHostedService.TotalTimeout.ShouldBe(TimeSpan.FromMinutes(5));
        WorkflowReplaySafetyHostedService.PerQueryTimeout.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task TryCountInFlightAsync_CountsOnlyRegistryTrackedActiveInstances()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new IngestionWorkflowInFlightEntry("tenant-a", "active-instance", DateTimeOffset.UtcNow),
                new IngestionWorkflowInFlightEntry("tenant-a", "completed-instance", DateTimeOffset.UtcNow),
            ]);
        workflowClient.GetWorkflowStateAsync("active-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("active-instance", WorkflowRuntimeStatus.Running)));
        workflowClient.GetWorkflowStateAsync("completed-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("completed-instance", WorkflowRuntimeStatus.Completed)));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        int? count = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(1);
        await registry.Received(1).RemoveAsync("completed-instance", Arg.Any<CancellationToken>());
        await workflowClient.DidNotReceiveWithAnyArgs().ListInstanceIdsAsync(default, default, default);
    }

    [Fact]
    public async Task TryCountInFlightAsync_WhenTrackedStateMissing_PrunesInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>())
            .Returns([new IngestionWorkflowInFlightEntry("tenant-a", "missing-instance", DateTimeOffset.UtcNow)]);
        workflowClient.GetWorkflowStateAsync("missing-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        int? count = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(0);
        await registry.Received(1).RemoveAsync("missing-instance", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryCountInFlightAsync_WhenRegistryUninitialized_UsesEnumerationFallback()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(false);
        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkflowInstancePage(["active-instance", "other-instance", "completed-instance"], null)));
        workflowClient.GetWorkflowStateAsync("active-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("active-instance", WorkflowRuntimeStatus.Running)));
        workflowClient.GetWorkflowStateAsync("other-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("other-instance", WorkflowRuntimeStatus.Running, "OtherWorkflow")));
        workflowClient.GetWorkflowStateAsync("completed-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateState("completed-instance", WorkflowRuntimeStatus.Completed)));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        int? count = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(1);
        await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);
    }

    [Fact]
    public async Task TryCountInFlightAsync_WhenUninitializedFallbackFindsNoActiveInstances_MarksInitialized()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(false);
        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new WorkflowInstancePage([], null)));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        int? count = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(0);
        await registry.Received(1).MarkInitializedAsync(Arg.Any<CancellationToken>());
    }

    private static WorkflowReplaySafetyHostedService CreateService(
        IDaprWorkflowClient workflowClient,
        IIngestionWorkflowInFlightRegistry registry)
        => new(
            workflowClient,
            registry,
            NullLogger<WorkflowReplaySafetyHostedService>.Instance,
            TimeProvider.System);

    private static WorkflowState CreateState(
        string instanceId,
        WorkflowRuntimeStatus status,
        string workflowName = nameof(IngestionWorkflow))
    {
        Type metadataType = typeof(WorkflowState).Assembly.GetType("Dapr.Workflow.Client.WorkflowMetadata")!;
        object metadata = Activator.CreateInstance(
            metadataType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                instanceId,
                workflowName,
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
