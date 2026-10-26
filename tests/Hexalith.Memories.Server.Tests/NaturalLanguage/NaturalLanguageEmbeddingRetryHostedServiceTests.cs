// <copyright file="NaturalLanguageEmbeddingRetryHostedServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.NaturalLanguage;

using Dapr.Workflow;

using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows.Contracts;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class NaturalLanguageEmbeddingRetryHostedServiceTests
{
    [Fact]
    public async Task TickAsync_DisabledDuringBacklogRead_StopsBeforeDequeuing()
    {
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        var options = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        bool enabled = true;
        options.CurrentValue.Returns(_ => new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = enabled });
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OneTenant());
        registry.GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            enabled = false;
            return Task.FromResult(2L);
        });
        var worker = new NaturalLanguageEmbeddingRetryHostedService(
            registry,
            Substitute.For<IDaprWorkflowClient>(),
            options,
            NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);

        await worker.TickAsync(new NaturalLanguageDescriptionOptions(), TestContext.Current.CancellationToken);

        _ = registry.Received(1).GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>());
        _ = registry.DidNotReceive().DequeueBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TickAsync_DisabledDuringBatchRead_LeavesReturnedRecordsUnscheduled()
    {
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        var workflowClient = Substitute.For<IDaprWorkflowClient>();
        var options = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        bool enabled = true;
        options.CurrentValue.Returns(_ => new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = enabled });
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OneTenant());
        registry.GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(2L);
        FailedNaturalLanguageEmbeddingRecord[] records = [Record("first"), Record("second")];
        registry.DequeueBatchAsync("tenant-a", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            enabled = false;
            return Task.FromResult<IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>>(records);
        });
        var worker = new NaturalLanguageEmbeddingRetryHostedService(
            registry,
            workflowClient,
            options,
            NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);

        registry.ClearReceivedCalls();
        workflowClient.ClearReceivedCalls();
        await worker.TickAsync(new NaturalLanguageDescriptionOptions(), TestContext.Current.CancellationToken);

        enabled.ShouldBeFalse();
        _ = await registry.Received(1).DequeueBatchAsync("tenant-a", Arg.Any<int>(), Arg.Any<CancellationToken>());
        workflowClient.ReceivedCalls().ShouldBeEmpty();
        await registry.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default);
        await registry.DidNotReceiveWithAnyArgs().IncrementAttemptsAsync(default!, default, default);
    }

    [Fact]
    public async Task TickAsync_DisabledAfterFirstScheduleStarts_DoesNotScheduleOrFinalizeSecondRecord()
    {
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        var workflowClient = Substitute.For<IDaprWorkflowClient>();
        var options = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        bool enabled = true;
        options.CurrentValue.Returns(_ => new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = enabled });
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OneTenant());
        registry.GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(2L);
        FailedNaturalLanguageEmbeddingRecord[] records = [Record("first"), Record("second")];
        registry.DequeueBatchAsync("tenant-a", Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>>(records));
        var firstScheduleStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstSchedule = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        workflowClient.ScheduleNewWorkflowAsync(
                Arg.Any<string>(),
                NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("tenant-a", "first"),
                Arg.Any<NaturalLanguageEmbeddingRetryInput>())
            .Returns(_ =>
            {
                enabled = false;
                firstScheduleStarted.TrySetResult();
                return releaseFirstSchedule.Task;
            });
        var worker = new NaturalLanguageEmbeddingRetryHostedService(
            registry,
            workflowClient,
            options,
            NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);

        Task tick = worker.TickAsync(new NaturalLanguageDescriptionOptions(), TestContext.Current.CancellationToken);
        await firstScheduleStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        enabled.ShouldBeFalse();
        releaseFirstSchedule.SetResult(NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("tenant-a", "first"));
        await tick;

        _ = await workflowClient.Received(1).ScheduleNewWorkflowAsync(
            Arg.Any<string>(),
            NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("tenant-a", "first"),
            Arg.Any<NaturalLanguageEmbeddingRetryInput>());
        _ = await workflowClient.DidNotReceive().ScheduleNewWorkflowAsync(
            Arg.Any<string>(),
            NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("tenant-a", "second"),
            Arg.Any<NaturalLanguageEmbeddingRetryInput>());
        await registry.DidNotReceive().CompleteAsync(records[1], Arg.Any<CancellationToken>());
        _ = await registry.DidNotReceive().IncrementAttemptsAsync(
            records[1], Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies tenant Isolation Does Not Swallow Caller Cancellation.
    /// </summary>
    [Fact]
    public async Task TenantIsolationDoesNotSwallowCallerCancellation()
    {
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>()).Returns(_ => OneTenant());
        registry.GetBacklogCountAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(1L);
        registry.DequeueBatchAsync("tenant-a", Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancelled.Cancel();
            return Task.FromCanceled<IReadOnlyList<FailedNaturalLanguageEmbeddingRecord>>(cancelled.Token);
        });
        var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true };
        var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>(); monitor.CurrentValue.Returns(options);
        var worker = new NaturalLanguageEmbeddingRetryHostedService(registry, Substitute.For<IDaprWorkflowClient>(), monitor,
            NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);
        await Should.ThrowAsync<OperationCanceledException>(() => worker.TickAsync(options, cancelled.Token));
    }

    private static FailedNaturalLanguageEmbeddingRecord Record(string memoryUnitId)
        => new("tenant-a", memoryUnitId, "{}", "event", null, "case-a", "provider", "model", 3, 1, 0);

    private static async IAsyncEnumerable<string> OneTenant()
    {
        yield return "tenant-a";
        await Task.CompletedTask;
    }

    [Fact]
    public void GetRetryWorkflowInstanceId_PreservesScopeBoundariesAndUsesDisjointBoundedNamespace()
    {
        string first = NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("a-b", "c");
        first.ShouldNotBe(NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("a", "b-c"));
        first.ShouldBe(NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId("a-b", "c"));
        first.Length.ShouldBe(75);
        first.ShouldStartWith("retry-nlv2-");
        first.ShouldNotBe(NaturalLanguageEmbeddingRetryHostedService.GetLegacyRetryWorkflowInstanceId("a-b", "c"));
    }

    [Theory]
    [InlineData(WorkflowRuntimeStatus.Completed, true)]
    [InlineData(WorkflowRuntimeStatus.Failed, true)]
    [InlineData(WorkflowRuntimeStatus.Canceled, true)]
    [InlineData(WorkflowRuntimeStatus.Terminated, true)]
    [InlineData(WorkflowRuntimeStatus.Running, false)]
    [InlineData(WorkflowRuntimeStatus.Pending, false)]
    public void IsTerminalStatus_MatchesExpectedTerminalStates(WorkflowRuntimeStatus status, bool expected)
    {
        NaturalLanguageEmbeddingRetryHostedService.IsTerminalStatus(status).ShouldBe(expected);
    }
}
