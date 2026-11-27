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
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

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
    public void ShouldCountWorkflow_TracksIngestionAndAnnotationProjection()
    {
        WorkflowReplaySafetyHostedService.ShouldCountWorkflow(
            workflowName: nameof(AnnotationProjectionWorkflow),
            exists: true,
            status: WorkflowRuntimeStatus.Pending).ShouldBeTrue();

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

        (int? count, _, _) = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(1);
        await registry.Received(1).RemoveAsync("completed-instance", Arg.Any<CancellationToken>());
        await workflowClient.DidNotReceiveWithAnyArgs().ListInstanceIdsAsync(default, default, default);
    }

    [Fact]
    public async Task TryCountInFlightAsync_WhenTrackedStateMissing_PrunesInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(true);
        registry.ListAsync(Arg.Any<CancellationToken>())
            .Returns([new IngestionWorkflowInFlightEntry("tenant-a", "missing-instance", DateTimeOffset.UtcNow)]);
        workflowClient.GetWorkflowStateAsync("missing-instance", false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        (int? count, _, _) = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(0);
        await registry.Received(1).RemoveAsync("missing-instance", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TryCountInFlightAsync_WhenRegistryUninitialized_UsesEnumerationFallback(bool hasTrackedCompletedInstance)
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns(hasTrackedCompletedInstance
            ? [new IngestionWorkflowInFlightEntry("tenant-a", "completed-instance", DateTimeOffset.UtcNow)]
            : []);
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

        (int? count, _, _) = await service.TryCountInFlightAsync(CancellationToken.None);

        count.ShouldBe(1);
        await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);
        await registry.Received(hasTrackedCompletedInstance ? 1 : 0)
            .RemoveAsync("completed-instance", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryCountInFlightAsync_RecordsConfirmationOnlyAfterSharedLegacyDrain()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        bool trackedCompleted = false;
        bool drainConfirmed = false;
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns(_ => trackedCompleted
            ? new[] { new IngestionWorkflowInFlightEntry("tenant-a", "tracked-instance", DateTimeOffset.UnixEpoch) }
            : []);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(_ => drainConfirmed);
        registry.MarkInitializedAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            drainConfirmed = true;
            return Task.CompletedTask;
        });
        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstancePage(["legacy-instance"], null));
        workflowClient.GetWorkflowStateAsync("legacy-instance", false, Arg.Any<CancellationToken>())
            .Returns(CreateState("legacy-instance", WorkflowRuntimeStatus.Running),
                CreateState("legacy-instance", WorkflowRuntimeStatus.Running),
                CreateState("legacy-instance", WorkflowRuntimeStatus.Completed));
        workflowClient.GetWorkflowStateAsync("tracked-instance", false, Arg.Any<CancellationToken>())
            .Returns(CreateState("tracked-instance", WorkflowRuntimeStatus.Completed));

        (await CreateService(workflowClient, registry).TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        drainConfirmed.ShouldBeFalse();
        trackedCompleted = true;

        WorkflowReplaySafetyHostedService secondReplica = CreateService(workflowClient, registry);
        (await secondReplica.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        drainConfirmed.ShouldBeFalse();
        (await secondReplica.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
        drainConfirmed.ShouldBeTrue();
        (await CreateService(workflowClient, registry).TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);

        await registry.Received(1).MarkInitializedAsync(Arg.Any<CancellationToken>());
        await workflowClient.Received(3).ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartingAsync_RegistryFailure_LogsCriticalStartupBypassWithCause()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        InvalidOperationException failure = new("Registry unavailable");
        registry.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<IngestionWorkflowInFlightEntry>>(failure));
        using CapturingAuditLoggerProvider logs = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        WorkflowReplaySafetyHostedService service = new(workflowClient, registry, loggerFactory.CreateLogger<WorkflowReplaySafetyHostedService>());

        await service.StartingAsync(CancellationToken.None);

        AuditLogCapture entry = logs.Captures.ShouldHaveSingleItem();
        entry.EventId.ShouldBe(9173);
        entry.Level.ShouldBe(LogLevel.Critical);
        entry.Fields.ToDictionary()["Reason"].ShouldBe("sidecar-query-exception");
        entry.Exception.ShouldBeSameAs(failure);
        await workflowClient.DidNotReceiveWithAnyArgs().ListInstanceIdsAsync(default, default, default);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task StartingAsync_DistinguishesTheFirstQueryCancellation(bool timeoutFirst, bool fireSecondSource)
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>())
            .Returns([new IngestionWorkflowInFlightEntry("tenant-a", "active-instance", DateTimeOffset.UnixEpoch)]);
        using var shutdown = new CancellationTokenSource();
        var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseQuery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        workflowClient.GetWorkflowStateAsync("active-instance", false, Arg.Any<CancellationToken>())
            .Returns(call => CompleteQueryAsync(call.Arg<CancellationToken>()));
        using CapturingAuditLoggerProvider logs = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        FakeTimeProvider timeProvider = new();
        WorkflowReplaySafetyHostedService service = new(
            workflowClient, registry, loggerFactory.CreateLogger<WorkflowReplaySafetyHostedService>(), timeProvider);

        Task starting = service.StartingAsync(shutdown.Token);
        await queryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (timeoutFirst)
        {
            timeProvider.Advance(WorkflowReplaySafetyHostedService.PerQueryTimeout);
            if (fireSecondSource)
            {
                await shutdown.CancelAsync();
            }
        }
        else
        {
            await shutdown.CancelAsync();
            if (fireSecondSource)
            {
                timeProvider.Advance(WorkflowReplaySafetyHostedService.PerQueryTimeout);
            }
        }

        releaseQuery.SetResult();
        await starting.WaitAsync(TimeSpan.FromSeconds(5));

        if (timeoutFirst)
        {
            AuditLogCapture entry = logs.Captures.ShouldHaveSingleItem();
            entry.EventId.ShouldBe(9173);
            entry.Level.ShouldBe(LogLevel.Critical);
            entry.Fields.ToDictionary()["Reason"].ShouldBe("sidecar-query-timeout");
        }
        else
        {
            logs.Captures.ShouldBeEmpty();
        }

        async Task<WorkflowState> CompleteQueryAsync(CancellationToken cancellationToken)
        {
            queryStarted.SetResult();
            await releaseQuery.Task;
            cancellationToken.ThrowIfCancellationRequested();
            return CreateState("active-instance", WorkflowRuntimeStatus.Running);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartingAsync_TotalBudgetEndsQueriesWithinTrackedListOrFallbackPage(bool useEnumeration)
    {
        const int completedQueries = 33;
        string[] ids = Enumerable.Range(0, completedQueries + 3).Select(index => $"instance-{index}").ToArray();
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        FakeTimeProvider timeProvider = new();
        if (useEnumeration)
        {
            registry.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
            registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(false);
            workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
                .Returns(new WorkflowInstancePage(ids, "next-page"));
        }
        else
        {
            registry.ListAsync(Arg.Any<CancellationToken>())
                .Returns(ids.Select(id => new IngestionWorkflowInFlightEntry("tenant-a", id, DateTimeOffset.UnixEpoch)).ToArray());
        }

        var pending = new TaskCompletionSource<WorkflowState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int queryCount = 0;
        CancellationToken pendingQueryToken = default;
        workflowClient.GetWorkflowStateAsync(Arg.Any<string>(), false, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (++queryCount <= completedQueries)
                {
                    timeProvider.Advance(TimeSpan.FromSeconds(9));
                    return Task.FromResult(CreateState(call.Arg<string>(), WorkflowRuntimeStatus.Running));
                }

                pendingQueryToken = call.Arg<CancellationToken>();
                queryStarted.SetResult();
                return pending.Task;
            });
        using CapturingAuditLoggerProvider logs = new();
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        WorkflowReplaySafetyHostedService service = new(
            workflowClient, registry, factory.CreateLogger<WorkflowReplaySafetyHostedService>(), timeProvider);

        Task starting = service.StartingAsync(CancellationToken.None);
        try
        {
            await queryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            starting.IsCompleted.ShouldBeFalse();
            timeProvider.Advance(TimeSpan.FromSeconds(3));
            await starting.WaitAsync(TimeSpan.FromSeconds(5));

            queryCount.ShouldBe(completedQueries + 1);
            pendingQueryToken.IsCancellationRequested.ShouldBeTrue();
            await workflowClient.DidNotReceive().ListInstanceIdsAsync("next-page", Arg.Any<int>(), Arg.Any<CancellationToken>());
            await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);
            AuditLogCapture entry = logs.Captures.ShouldHaveSingleItem();
            entry.EventId.ShouldBe(9172);
            entry.Level.ShouldBe(LogLevel.Critical);
            entry.Fields.ToDictionary()["RemainingCount"].ShouldBeNull();
        }
        finally
        {
            pending.TrySetResult(CreateState(ids[completedQueries], WorkflowRuntimeStatus.Running));
        }
    }

    [Fact]
    public async Task TryCountInFlightAsync_KeepsAnnotationReservationBeforeTheSidecarShowsTheParent()
    {
        const string childId = "annotation-unit";
        string parentId = AnnotationProjectionWorkflow.GetInstanceId(childId);
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([new("tenant-a", parentId, clock.GetUtcNow())]);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(true);
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync(parentId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        WorkflowReplaySafetyHostedService service = new(
            workflowClient, registry, NullLogger<WorkflowReplaySafetyHostedService>.Instance, clock);

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        clock.Advance(WorkflowReplaySafetyHostedService.TotalTimeout);
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
        await registry.DidNotReceive().RemoveAsync(parentId, Arg.Any<CancellationToken>());

        // A parent lost to history cleanup cannot hide a later-visible child, even after the window.
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(childId, WorkflowRuntimeStatus.Running));
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        await registry.DidNotReceive().RemoveAsync(parentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryCountInFlightAsync_DrainsAnnotationChildEvenWhenItsParentHasTerminated()
    {
        const string childId = "annotation-unit";
        string parentId = AnnotationProjectionWorkflow.GetInstanceId(childId);
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([new("tenant-a", parentId, DateTimeOffset.UtcNow)]);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(true);
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync(parentId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(parentId, WorkflowRuntimeStatus.Terminated, nameof(AnnotationProjectionWorkflow)));
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(childId, WorkflowRuntimeStatus.Running), CreateState(childId, WorkflowRuntimeStatus.Completed));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        await registry.DidNotReceive().RemoveAsync(parentId, Arg.Any<CancellationToken>());

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
        await registry.Received(1).RemoveAsync(parentId, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Failed, true, false, false)]
    [InlineData(WorkflowRuntimeStatus.Terminated, true, true, true)]
    [InlineData(WorkflowRuntimeStatus.Completed, false, false, true)]
    public async Task TryCountInFlightAsync_UsesParentHistoryBeforePruningMissingChild(
        WorkflowRuntimeStatus parentStatus,
        bool terminalHistoryAvailable,
        bool childDispatched,
        bool shouldRetain)
    {
        const string childId = "annotation-unit";
        string parentId = AnnotationProjectionWorkflow.GetInstanceId(childId);
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([new("tenant-a", parentId, DateTimeOffset.UtcNow)]);
        registry.IsInitializedAsync(Arg.Any<CancellationToken>()).Returns(true);
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.GetWorkflowStateAsync(parentId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(parentId, parentStatus, nameof(AnnotationProjectionWorkflow)));
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        List<WorkflowHistoryEvent> history = [];
        if (childDispatched)
        {
            history.Add(new WorkflowHistoryEvent(1, WorkflowHistoryEventType.SubOrchestrationInstanceCreated, DateTime.UtcNow));
        }

        if (terminalHistoryAvailable)
        {
            history.Add(new WorkflowHistoryEvent(2, WorkflowHistoryEventType.ExecutionCompleted, DateTime.UtcNow));
        }

        workflowClient.GetInstanceHistoryAsync(parentId, Arg.Any<CancellationToken>()).Returns(history);
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(shouldRetain ? 1 : 0);
        await registry.Received(shouldRetain ? 0 : 1).RemoveAsync(parentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryCountInFlightAsync_InitialDrainIncludesLegacyAnnotationParents()
    {
        const string parentId = "legacy-annotation-parent";
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstancePage([parentId], null));
        workflowClient.GetWorkflowStateAsync(parentId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(parentId, WorkflowRuntimeStatus.Pending, nameof(AnnotationProjectionWorkflow)),
                CreateState(parentId, WorkflowRuntimeStatus.Completed, nameof(AnnotationProjectionWorkflow)));
        WorkflowReplaySafetyHostedService service = CreateService(workflowClient, registry);

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
        await registry.Received(1).MarkInitializedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryCountInFlightAsync_LegacyDrainRemainsUnconfirmedUntilDelayedChildSettles()
    {
        const string childId = "legacy-annotation-unit";
        string parentId = AnnotationProjectionWorkflow.GetInstanceId(childId);
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        registry.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstancePage([parentId], null));
        workflowClient.GetWorkflowStateAsync(parentId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(parentId, WorkflowRuntimeStatus.Terminated, nameof(AnnotationProjectionWorkflow)));
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<WorkflowState>(null!));
        workflowClient.GetInstanceHistoryAsync(parentId, Arg.Any<CancellationToken>()).Returns(
            [new WorkflowHistoryEvent(1, WorkflowHistoryEventType.SubOrchestrationInstanceCreated, DateTime.UtcNow),
             new WorkflowHistoryEvent(2, WorkflowHistoryEventType.ExecutionTerminated, DateTime.UtcNow)]);
        WorkflowReplaySafetyHostedService service = new(
            workflowClient, registry, NullLogger<WorkflowReplaySafetyHostedService>.Instance, clock);

        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        clock.Advance(WorkflowReplaySafetyHostedService.TotalTimeout + TimeSpan.FromSeconds(1));
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
        await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);

        workflowClient.ListInstanceIdsAsync(null, 100, Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstancePage([parentId, childId], null));
        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(childId, WorkflowRuntimeStatus.Running));
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(1);
        await registry.DidNotReceiveWithAnyArgs().MarkInitializedAsync(default);

        workflowClient.GetWorkflowStateAsync(childId, false, Arg.Any<CancellationToken>())
            .Returns(CreateState(childId, WorkflowRuntimeStatus.Completed));
        (await service.TryCountInFlightAsync(CancellationToken.None)).Count.ShouldBe(0);
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
