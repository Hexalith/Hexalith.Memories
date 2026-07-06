// <copyright file="DaprIngestionWorkflowSchedulerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.NaturalLanguage;
using Hexalith.Memories.Server.Workflows;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public sealed class DaprIngestionWorkflowSchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_WhenWorkflowSchedulesSuccessfully_TracksInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            "instance-1",
            Arg.Any<IngestionInput>(),
            null,
            Arg.Any<CancellationToken>())
            .Returns("instance-1");
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowScheduler scheduler = CreateScheduler(workflowClient, registry);
        IngestionInput input = CreateInput();

        string instanceId = await scheduler.ScheduleAsync("instance-1", input, CancellationToken.None);

        instanceId.ShouldBe("instance-1");
        await registry.Received(1).TrackAsync(
            Arg.Is<IngestionWorkflowInFlightEntry>(entry =>
                entry.TenantId == "tenant-a" && entry.InstanceId == "instance-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleAsync_WhenWorkflowScheduleFails_RemovesTrackedInstance()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            "instance-1",
            Arg.Any<IngestionInput>(),
            null,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new InvalidOperationException("scheduler unavailable")));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowScheduler scheduler = CreateScheduler(workflowClient, registry);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => scheduler.ScheduleAsync("instance-1", CreateInput(), CancellationToken.None));

        await registry.Received(1).TrackAsync(
            Arg.Is<IngestionWorkflowInFlightEntry>(entry =>
                entry.TenantId == "tenant-a" && entry.InstanceId == "instance-1"),
            Arg.Any<CancellationToken>());
        await registry.Received(1).RemoveAsync("instance-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleAsync_WhenTrackingFails_DoesNotScheduleWorkflow()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        registry.TrackAsync(Arg.Any<IngestionWorkflowInFlightEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("redis unavailable")));
        DaprIngestionWorkflowScheduler scheduler = CreateScheduler(workflowClient, registry);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => scheduler.ScheduleAsync("instance-1", CreateInput(), CancellationToken.None));

        await workflowClient.DidNotReceiveWithAnyArgs().ScheduleNewWorkflowAsync(default!, default!, default!, default, default);
    }

    private static DaprIngestionWorkflowScheduler CreateScheduler(
        IDaprWorkflowClient workflowClient,
        IIngestionWorkflowInFlightRegistry registry)
        => new(
            workflowClient,
            Substitute.For<IWorkflowPayloadStore>(),
            new IngestionWorkflowConfigurationCapture(
                Options.Create(new IngestionSettings()),
                Options.Create(new NaturalLanguageDescriptionOptions())),
            new WorkflowTraceContextCapture(),
            registry,
            TimeProvider.System,
            NullLogger<DaprIngestionWorkflowScheduler>.Instance);

    private static IngestionInput CreateInput()
        => new()
        {
            TenantId = "tenant-a",
            CaseId = "case-a",
            SourceUri = "https://example.test/source",
            ContentType = "text/plain",
            SourceType = SourceType.Url,
            IngestedBy = "tester",
        };
}
