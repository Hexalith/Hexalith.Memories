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
                entry!.TenantId == "tenant-a" && entry.InstanceId == "instance-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleAsync_InlineBytes_SendsCapturedClaimCheckedInputToDapr()
    {
        IDaprWorkflowClient workflowClient = Substitute.For<IDaprWorkflowClient>();
        workflowClient.ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            "instance-claim-check",
            Arg.Any<IngestionInput>(),
            null,
            Arg.Any<CancellationToken>())
            .Returns("instance-claim-check");
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        byte[] contentBytes = [4, 8, 15, 16, 23, 42];
        WorkflowPayloadReference payloadReference = new(
            "instance-claim-check:sourcebytes:hash:source",
            "hash",
            contentBytes.Length,
            WorkflowPayloadKind.SourceBytes,
            "tenant-a",
            "instance-claim-check");
        payloadStore.SaveAsync(
            "tenant-a",
            "instance-claim-check",
            WorkflowPayloadKind.SourceBytes,
            Arg.Any<ReadOnlyMemory<byte>>(),
            "source",
            Arg.Any<CancellationToken>())
            .Returns(payloadReference);
        IngestionWorkflowConfigurationCapture configurationCapture = new(
            Options.Create(new IngestionSettings
            {
                RetryPolicies = new Dictionary<string, ActivityRetryPolicy>(StringComparer.Ordinal)
                {
                    ["ExtractContentActivity"] = new ActivityRetryPolicy
                    {
                        MaxAttempts = 7,
                        FirstRetryIntervalSeconds = 11,
                        BackoffCoefficient = 1.25,
                        MaxRetryIntervalSeconds = 71,
                    },
                },
            }),
            Options.Create(new NaturalLanguageDescriptionOptions { PersistInMetadata = true }));
        IIngestionWorkflowInFlightRegistry registry = Substitute.For<IIngestionWorkflowInFlightRegistry>();
        DaprIngestionWorkflowScheduler scheduler = CreateScheduler(
            workflowClient,
            registry,
            payloadStore,
            configurationCapture);
        WorkflowTraceContext traceContext = new()
        {
            TraceParent = "00-11111111111111111111111111111111-2222222222222222-01",
            TraceState = "vendor=scheduler-proof",
        };
        IngestionInput input = CreateInput() with
        {
            SourceUri = "file:///evidence.txt",
            SourceType = SourceType.File,
            ContentBytes = contentBytes,
            ContentType = "application/x-scheduler-proof",
            IngestedBy = "scheduler-proof@test.local",
            CausationId = "causation-scheduler-proof",
            CorrelationId = "correlation-scheduler-proof",
            TraceContext = traceContext,
            Metadata = new Dictionary<string, MetadataField>(StringComparer.Ordinal)
            {
                ["evidence.kind"] = new("schedule-proof", MetadataOrigin.Human, 1.0f),
            },
        };
        using CancellationTokenSource cancellationSource = new();
        CancellationToken cancellationToken = cancellationSource.Token;

        string result = await scheduler.ScheduleAsync("instance-claim-check", input, cancellationToken);

        result.ShouldBe("instance-claim-check");
        await workflowClient.Received(1).ScheduleNewWorkflowAsync(
            nameof(IngestionWorkflow),
            "instance-claim-check",
            Arg.Is<IngestionInput>(scheduled =>
                scheduled!.TenantId == input.TenantId
                && scheduled.CaseId == input.CaseId
                && scheduled.SourceUri == input.SourceUri
                && scheduled.SourceType == input.SourceType
                && scheduled.ContentType == input.ContentType
                && scheduled.IngestedBy == input.IngestedBy
                && scheduled.CausationId == input.CausationId
                && scheduled.CorrelationId == input.CorrelationId
                && ReferenceEquals(scheduled.TraceContext, traceContext)
                && scheduled.ContentBytes == null
                && scheduled.PayloadReference == payloadReference
                && scheduled.Metadata == input.Metadata
                && scheduled.WorkflowConfiguration != null
                && scheduled.WorkflowConfiguration.NaturalLanguage.PersistInMetadata
                && scheduled.WorkflowConfiguration.Retry.ActivityOverrides["ExtractContentActivity"].MaxAttempts == 7
                && scheduled.WorkflowConfiguration.Retry.ActivityOverrides["ExtractContentActivity"].FirstRetryIntervalSeconds == 11
                && scheduled.WorkflowConfiguration.Retry.ActivityOverrides["ExtractContentActivity"].BackoffCoefficient == 1.25
                && scheduled.WorkflowConfiguration.Retry.ActivityOverrides["ExtractContentActivity"].MaxRetryIntervalSeconds == 71),
            null,
            cancellationToken);
        await payloadStore.Received(1).SaveAsync(
            "tenant-a",
            "instance-claim-check",
            WorkflowPayloadKind.SourceBytes,
            Arg.Is<ReadOnlyMemory<byte>>(payload => payload!.ToArray().SequenceEqual(contentBytes)),
            "source",
            cancellationToken);
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
                entry!.TenantId == "tenant-a" && entry.InstanceId == "instance-1"),
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
        IIngestionWorkflowInFlightRegistry registry,
        IWorkflowPayloadStore? payloadStore = null,
        IngestionWorkflowConfigurationCapture? workflowConfigurationCapture = null)
        => new(
            workflowClient,
            payloadStore ?? Substitute.For<IWorkflowPayloadStore>(),
            workflowConfigurationCapture ?? new IngestionWorkflowConfigurationCapture(
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
