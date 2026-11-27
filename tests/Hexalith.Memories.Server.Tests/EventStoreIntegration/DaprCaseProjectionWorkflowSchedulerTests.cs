// <copyright file="DaprCaseProjectionWorkflowSchedulerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.EventStoreIntegration;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Cases;
using Hexalith.Memories.Server.EventStoreIntegration;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

public sealed class DaprCaseProjectionWorkflowSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_TracksAnnotationParentBeforeSubmittingTheWorkflow()
    {
        AnnotationProjectionInput input = new("tenant-a", "case", "annotation-unit", "target", "urn:annotation",
            "content", null, "fixture", new Dictionary<string, MetadataField>());
        string parentId = AnnotationProjectionWorkflow.GetInstanceId(input.AnnotationMemoryUnitId);
        IDaprWorkflowClient client = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        FakeTimeProvider clock = new(DateTimeOffset.UnixEpoch);
        TaskCompletionSource tracked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IngestionWorkflowInFlightEntry entry = new(input.TenantId, parentId, clock.GetUtcNow());
        registry.TrackAsync(entry, CancellationToken.None).Returns(tracked.Task);
        client.ScheduleNewWorkflowAsync(nameof(AnnotationProjectionWorkflow), parentId, input, null, CancellationToken.None)
            .Returns(parentId);
        DaprCaseProjectionWorkflowScheduler scheduler = new(client, registry, clock);

        Task<string> scheduling = scheduler.ScheduleAsync(nameof(AnnotationProjectionWorkflow), parentId, input, CancellationToken.None);
        try
        {
            _ = registry.Received(1).TrackAsync(entry, CancellationToken.None);
            scheduling.IsCompleted.ShouldBeFalse();
            _ = client.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default, default, default, default);
        }
        finally
        {
            tracked.TrySetResult();
        }

        (await scheduling.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(parentId);
        await client.Received(1).ScheduleNewWorkflowAsync(nameof(AnnotationProjectionWorkflow), parentId, input, null, CancellationToken.None);
    }

    [Fact]
    public async Task ScheduleAsync_CaseCreationDoesNotEnterTheIngestionRegistry()
    {
        const string parentId = "case-create-fixture";
        IDaprWorkflowClient client = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprCaseProjectionWorkflowScheduler scheduler = new(client, registry, TimeProvider.System);
        ProjectCaseCreatedInput input = new("tenant-a", "case", "Fixture case", null, DateTimeOffset.UnixEpoch);
        client.ScheduleNewWorkflowAsync(nameof(CaseCreationProjectionWorkflow), parentId, input, null, CancellationToken.None)
            .Returns(parentId);

        (await scheduler.ScheduleAsync(nameof(CaseCreationProjectionWorkflow), parentId, input, CancellationToken.None))
            .ShouldBe(parentId);

        await registry.DidNotReceiveWithAnyArgs().TrackAsync(default!, default);
    }
}
