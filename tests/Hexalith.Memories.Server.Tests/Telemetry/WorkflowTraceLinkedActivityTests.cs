// <copyright file="WorkflowTraceLinkedActivityTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Diagnostics;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities;
using Hexalith.Memories.Telemetry;

using NSubstitute;

using Shouldly;

[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class WorkflowTraceLinkedActivityTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _stopped = [];

    public WorkflowTraceLinkedActivityTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MemoriesActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _stopped.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public async Task RunAsync_WithValidTraceContext_EmitsLinkedSpan()
    {
        var input = new TestActivityInput(
            "tenant-1",
            "case-1",
            "mu-1",
            new WorkflowTraceContext
            {
                TraceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                TraceState = "vendor=story24",
            });
        var activity = new TestLinkedActivity();

        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        result.ShouldBeTrue();
        Activity emitted = _stopped.ShouldHaveSingleItem();
        emitted.OperationName.ShouldBe(MemoriesActivitySource.WorkflowActivity);
        emitted.GetTagItem(MemoriesActivitySource.TagOperation).ShouldBe(nameof(TestLinkedActivity));
        emitted.GetTagItem(MemoriesActivitySource.TagTenantId).ShouldBe("tenant-1");
        emitted.GetTagItem(MemoriesActivitySource.TagCaseId).ShouldBe("case-1");
        emitted.GetTagItem(MemoriesActivitySource.TagMemoryUnitId).ShouldBe("mu-1");
        emitted.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe("ok");
        ActivityLink link = emitted.Links.ShouldHaveSingleItem();
        link.Context.TraceId.ToString().ShouldBe("4bf92f3577b34da6a3ce929d0e0e4736");
        link.Context.SpanId.ToString().ShouldBe("00f067aa0ba902b7");
    }

    [Fact]
    public async Task RunAsync_WithInvalidTraceContext_StillRunsWithoutLinks()
    {
        var input = new TestActivityInput(
            "tenant-1",
            "case-1",
            "mu-1",
            new WorkflowTraceContext { TraceParent = "not-a-traceparent" });
        var activity = new TestLinkedActivity();

        bool result = await activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input);

        result.ShouldBeTrue();
        Activity emitted = _stopped.ShouldHaveSingleItem();
        emitted.Links.ShouldBeEmpty();
        emitted.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe("ok");
    }

    [Fact]
    public async Task RunAsync_WhenActivityThrows_EmitsErrorSpanAndRethrows()
    {
        var input = new TestActivityInput(
            "tenant-1",
            "case-1",
            "mu-1",
            new WorkflowTraceContext
            {
                TraceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-1111111111111111-01",
            });
        var activity = new TestLinkedActivity(shouldThrow: true);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            () => activity.RunAsync(Substitute.For<WorkflowActivityContext>(), input));

        exception.Message.ShouldBe("workflow activity failed");
        Activity emitted = _stopped.ShouldHaveSingleItem();
        emitted.Links.ShouldHaveSingleItem();
        emitted.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe("error");
        emitted.GetTagItem(MemoriesActivitySource.TagErrorCode).ShouldBe(nameof(InvalidOperationException));
        emitted.Status.ShouldBe(ActivityStatusCode.Error);
    }

    public void Dispose() => _listener.Dispose();

    private sealed record TestActivityInput(
        string TenantId,
        string CaseId,
        string MemoryUnitId,
        WorkflowTraceContext? TraceContext) : IWorkflowTraceContextCarrier;

    private sealed class TestLinkedActivity : WorkflowTraceLinkedActivity<TestActivityInput, bool>
    {
        private readonly bool _shouldThrow;

        public TestLinkedActivity(bool shouldThrow = false) => _shouldThrow = shouldThrow;

        protected override Task<bool> RunActivityAsync(WorkflowActivityContext context, TestActivityInput input)
        {
            if (_shouldThrow)
            {
                throw new InvalidOperationException("workflow activity failed");
            }

            return Task.FromResult(true);
        }
    }
}
