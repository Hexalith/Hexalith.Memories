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
    public async Task TickAsync_BoundsEachTenantBatchAndDrainsSmallTenantsDespiteLargeBacklog()
    {
        string[] tenantIds = Enumerable.Range(0, 100).Select(index => $"tenant-{index}").ToArray();
        var backlog = tenantIds.ToDictionary(tenant => tenant, tenant => new Queue<FailedNaturalLanguageEmbeddingRecord>(
            Enumerable.Range(0, tenant == "tenant-0" ? 1000 : 10)
                .Select(index => Record($"unit-{index}") with { TenantId = tenant })), StringComparer.Ordinal);
        var registry = Substitute.For<IFailedNaturalLanguageEmbeddingRegistry>();
        registry.ListTenantsWithBacklogAsync(Arg.Any<CancellationToken>())
            .Returns(_ => EnumerateTenantsAsync(tenantIds));
        registry.GetBacklogCountAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => backlog[call.Arg<string>()].Count);
        registry.DequeueBatchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => backlog[call.Arg<string>()].Take(call.Arg<int>()).ToArray());

        var scheduled = new List<(string Tenant, string Unit)>();
        var client = Substitute.For<IDaprWorkflowClient>();
        client.ScheduleNewWorkflowAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<NaturalLanguageEmbeddingRetryInput>())
            .Returns(call =>
            {
                NaturalLanguageEmbeddingRetryInput input = call.Arg<NaturalLanguageEmbeddingRetryInput>();
                string id = call.ArgAt<string>(1);
                id.ShouldBe(NaturalLanguageEmbeddingRetryHostedService.GetRetryWorkflowInstanceId(input.TenantId, input.MemoryUnitId));
                backlog[input.TenantId].Dequeue().MemoryUnitId.ShouldBe(input.MemoryUnitId);
                scheduled.Add((input.TenantId, input.MemoryUnitId));
                return id;
            });

        var options = new NaturalLanguageDescriptionOptions { RetryWorkerEnabled = true, BatchSize = 5 };
        var monitor = Substitute.For<IOptionsMonitor<NaturalLanguageDescriptionOptions>>();
        monitor.CurrentValue.Returns(options);
        var worker = new NaturalLanguageEmbeddingRetryHostedService(
            registry, client, monitor, NullLogger<NaturalLanguageEmbeddingRetryHostedService>.Instance);

        await worker.TickAsync(options, TestContext.Current.CancellationToken);
        scheduled.GroupBy(item => item.Tenant).ToDictionary(group => group.Key, group => group.Count())
            .ShouldBe(tenantIds.ToDictionary(tenant => tenant, _ => 5));
        await worker.TickAsync(options, TestContext.Current.CancellationToken);
        await worker.TickAsync(options, TestContext.Current.CancellationToken);

        scheduled.Count.ShouldBe((99 * 10) + 15);
        scheduled.Distinct().Count().ShouldBe(scheduled.Count);
        backlog["tenant-0"].Count.ShouldBe(985);
        backlog.Where(item => item.Key != "tenant-0").ShouldAllBe(item => item.Value.Count == 0);
    }

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

    private static IAsyncEnumerable<string> OneTenant()
        => EnumerateTenantsAsync(["tenant-a"]);

    private static async IAsyncEnumerable<string> EnumerateTenantsAsync(IEnumerable<string> tenants)
    {
        foreach (string tenant in tenants)
        {
            yield return tenant;
        }

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
