// <copyright file="ReIngestionCoordinatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

public class ReIngestionCoordinatorTests
{
    [Fact]
    public async Task TryScheduleAsync_WhenRecordMissing_ReturnsNotFound()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns((FailedUnitRecord?)null);
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.NotFound);
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenClaimLost_ReturnsConflict()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-1");

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(false);

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Conflict);
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenCaseMismatch_ReturnsCaseMismatchWithoutClaim()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-1");

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "other-case", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.CaseMismatch);
        await registry.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenUrlRecordHasNoSourcePayload_SchedulesWithoutPayloadReference()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-url", SourceType.Url);

        registry.GetAsync("tenant-1", "mu-url", Arg.Any<CancellationToken>()).Returns(record);
        registry.RemoveAsync("tenant-1", "case-1", "mu-url", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-url", Arg.Any<IngestionInput>()).Returns("wf-mu-url");
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-url", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        await payloadStore.DidNotReceive().ReadAsync(Arg.Any<WorkflowPayloadReference>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorkflowPayloadKind>(), Arg.Any<CancellationToken>());
        await scheduler.Received().ScheduleAsync(
            "mu-url",
            Arg.Is<IngestionInput>(input =>
                input.SourceType == SourceType.Url
                && input.PayloadReference == null
                && input.ContentBytes == null));
    }

    [Fact]
    public async Task TryScheduleAsync_WhenFileRecordHasValidSourcePayload_SchedulesWithPayloadReference()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference sourceReference = CreateSourceReference("mu-1");
        FailedUnitRecord record = CreateRecord("mu-1", SourceType.File, "text/markdown", sourceReference);

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(sourceReference, "tenant-1", "mu-1", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns([1, 2, 3]);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-1", Arg.Any<IngestionInput>()).Returns("wf-mu-1");

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        result.WorkflowInstanceId.ShouldBe("wf-mu-1");
        await scheduler.Received().ScheduleAsync(
            "mu-1",
            Arg.Is<IngestionInput>(input =>
                input.TenantId == "tenant-1"
                && input.CaseId == "case-1"
                && input.SourceUri == record.SourceUri
                && input.SourceType == SourceType.File
                && input.ContentType == "text/markdown"
                && input.ContentBytes == null
                && input.PayloadReference == sourceReference
                && input.Metadata.Count == 0));
    }

    [Fact]
    public async Task TryScheduleAsync_WhenEventRecordHasValidSourcePayload_PreservesMetadata()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference sourceReference = CreateSourceReference("mu-event");
        Dictionary<string, MetadataField> metadata = new(StringComparer.Ordinal)
        {
            ["cloudevent.type"] = new("ClaimSubmitted", MetadataOrigin.Human, 1.0f),
            ["event.aggregateType"] = new("Claim", MetadataOrigin.Human, 1.0f),
        };
        FailedUnitRecord record = CreateRecord(
            "mu-event",
            SourceType.Event,
            "application/cloudevents+json",
            sourceReference,
            metadata);

        registry.GetAsync("tenant-1", "mu-event", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(sourceReference, "tenant-1", "mu-event", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns([123]);
        registry.RemoveAsync("tenant-1", "case-1", "mu-event", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-event", Arg.Any<IngestionInput>()).Returns("wf-mu-event");

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-event", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        await scheduler.Received().ScheduleAsync(
            "mu-event",
            Arg.Is<IngestionInput>(input =>
                input.SourceType == SourceType.Event
                && input.SourceUri == record.SourceUri
                && input.PayloadReference == sourceReference
                && input.ContentBytes == null
                && input.Metadata["cloudevent.type"].Value == "ClaimSubmitted"
                && input.Metadata["event.aggregateType"].Value == "Claim"));
    }

    [Fact]
    public async Task TryScheduleAsync_WhenEventPayloadReferenceUsesOriginalDedupScope_SchedulesWithReference()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        string sourceUri = "event-123";
        string dedupScopeId = DedupKeyBuilder.BuildKey("tenant-1", "case-1", sourceUri);
        WorkflowPayloadReference sourceReference = CreateSourceReference(dedupScopeId);
        FailedUnitRecord record = CreateRecord(
            "mu-event",
            SourceType.Event,
            "application/cloudevents+json",
            sourceReference,
            new Dictionary<string, MetadataField>(StringComparer.Ordinal)
            {
                ["cloudevent.type"] = new("ClaimSubmitted", MetadataOrigin.Human, 1.0f),
            }) with
            {
                SourceUri = sourceUri,
            };

        registry.GetAsync("tenant-1", "mu-event", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(sourceReference, "tenant-1", dedupScopeId, WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns([123]);
        registry.RemoveAsync("tenant-1", "case-1", "mu-event", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-event", Arg.Any<IngestionInput>()).Returns("wf-mu-event");

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-event", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Scheduled);
        await payloadStore.Received(1).ReadAsync(
            sourceReference,
            "tenant-1",
            dedupScopeId,
            WorkflowPayloadKind.SourceBytes,
            Arg.Any<CancellationToken>());
        await scheduler.Received().ScheduleAsync(
            "mu-event",
            Arg.Is<IngestionInput>(input =>
                input.SourceType == SourceType.Event
                && input.SourceUri == sourceUri
                && input.PayloadReference == sourceReference
                && input.ContentBytes == null));
    }

    [Fact]
    public async Task TryScheduleAsync_WhenFileRecordHasNoSourcePayload_ReturnsUnsupportedWithoutClaim()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-legacy", SourceType.File, "text/plain");

        registry.GetAsync("tenant-1", "mu-legacy", Arg.Any<CancellationToken>()).Returns(record);
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-legacy", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.UnsupportedSourcePayload);
        result.ErrorCode.ShouldBe("NON_URL_REINGESTION_UNAVAILABLE");
        await registry.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenPayloadKindInvalid_ReturnsUnsupportedWithoutClaim()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference reference = CreateSourceReference("mu-bad") with { ContentKind = WorkflowPayloadKind.ExtractedText };
        FailedUnitRecord record = CreateRecord("mu-bad", SourceType.File, "text/plain", reference);

        registry.GetAsync("tenant-1", "mu-bad", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(reference, "tenant-1", "mu-bad", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new WorkflowPayloadException("PAYLOAD_KIND_MISMATCH", reference.Id)));
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-bad", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.UnsupportedSourcePayload);
        await registry.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenPayloadExpired_ReturnsUnsupportedWithoutClaim()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference reference = CreateSourceReference("mu-expired");
        FailedUnitRecord record = CreateRecord("mu-expired", SourceType.Event, "application/json", reference);

        registry.GetAsync("tenant-1", "mu-expired", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(reference, "tenant-1", "mu-expired", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new WorkflowPayloadException("PAYLOAD_NOT_FOUND", reference.Id)));
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-expired", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.UnsupportedSourcePayload);
        await registry.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenPayloadScopeMismatched_ReturnsUnsupportedWithoutClaim()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference reference = CreateSourceReference("mu-scope") with { MemoryUnitId = "other-memory-unit" };
        FailedUnitRecord record = CreateRecord("mu-scope", SourceType.File, "text/plain", reference);

        registry.GetAsync("tenant-1", "mu-scope", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(reference, "tenant-1", "mu-scope", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<byte[]>(new WorkflowPayloadException("PAYLOAD_MEMORY_UNIT_MISMATCH", reference.Id)));
        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-scope", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.UnsupportedSourcePayload);
        await registry.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenWorkflowSchedulingFails_RestoresClaimAndRethrows()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        WorkflowPayloadReference sourceReference = CreateSourceReference("mu-1");
        FailedUnitRecord record = CreateRecord("mu-1", SourceType.File, "text/plain", sourceReference);

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        payloadStore.ReadAsync(sourceReference, "tenant-1", "mu-1", WorkflowPayloadKind.SourceBytes, Arg.Any<CancellationToken>())
            .Returns([1, 2, 3]);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-1", Arg.Any<IngestionInput>())
            .Returns(Task.FromException<string>(new InvalidOperationException("scheduler down")));

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None));

        ex.Message.ShouldBe("scheduler down");
        await registry.Received().RestoreAsync(record, CancellationToken.None);
    }

    [Fact]
    public async Task TryScheduleManyAsync_WhenThirdUnitRemoveThrows_ReportsErrorOutcomeAndLogs6305()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        CapturingLogger<ReIngestionCoordinator> logger = new();
        string[] ids = ["mu-1", "mu-2", "mu-3", "mu-4", "mu-5"];

        registry.GetAsync("tenant-1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateRecord(callInfo.ArgAt<string>(1)));
        registry.RemoveAsync("tenant-1", "case-1", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                string memoryUnitId = callInfo.ArgAt<string>(2);
                return memoryUnitId == "mu-3"
                    ? throw new InvalidOperationException("redis hiccup")
                    : true;
            });
        scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>())
            .Returns(callInfo => Task.FromResult($"wf-{callInfo.ArgAt<string>(0)}"));

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, logger);

        BulkReIngestionResponse response = await coordinator.TryScheduleManyAsync(
            "tenant-1",
            "case-1",
            ids,
            CancellationToken.None);

        response.Scheduled.ShouldBe(4);
        response.NotFound.ShouldBe(0);
        response.Conflicted.ShouldBe(0);
        response.Errored.ShouldBe(1);
        response.Units[2].MemoryUnitId.ShouldBe("mu-3");
        response.Units[2].Outcome.ShouldBe("error");
        response.Units[2].ErrorMessage.ShouldBe("redis hiccup");
        logger.Entries.Count(entry => entry.EventId.Id == 6305 && entry.Message.Contains("error", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1);
    }

    [Fact]
    public async Task TryScheduleManyAsync_WithUnsupportedRecord_CountsUnsupportedDistinctly()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        CapturingLogger<ReIngestionCoordinator> logger = new();

        registry.GetAsync("tenant-1", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(1) == "mu-unsupported"
                ? CreateRecord("mu-unsupported", SourceType.File, "text/plain")
                : CreateRecord(callInfo.ArgAt<string>(1)));
        registry.RemoveAsync("tenant-1", "case-1", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        scheduler.ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>())
            .Returns(callInfo => Task.FromResult($"wf-{callInfo.ArgAt<string>(0)}"));

        ReIngestionCoordinator coordinator = new(registry, payloadStore, scheduler, logger);

        BulkReIngestionResponse response = await coordinator.TryScheduleManyAsync(
            "tenant-1",
            "case-1",
            ["mu-ok", "mu-unsupported"],
            CancellationToken.None);

        response.Scheduled.ShouldBe(1);
        response.Unsupported.ShouldBe(1);
        response.Errored.ShouldBe(0);
        response.Units[1].Outcome.ShouldBe("unsupported-source-payload");
        response.Units[1].ErrorCode.ShouldBe("NON_URL_REINGESTION_UNAVAILABLE");
    }

    private static FailedUnitRecord CreateRecord(
        string memoryUnitId,
        SourceType sourceType = SourceType.Url,
        string? contentType = null,
        WorkflowPayloadReference? sourcePayloadReference = null,
        IReadOnlyDictionary<string, MetadataField>? metadata = null)
        => new(
            TenantId: "tenant-1",
            CaseId: "case-1",
            MemoryUnitId: memoryUnitId,
            SourceUri: $"https://example.test/{memoryUnitId}",
            SourceType: sourceType,
            IngestedBy: "reviewer@example.com",
            ContentType: contentType,
            Stage: "embedding",
            ErrorCode: "PROVIDER_500",
            ErrorMessage: "provider failed",
            RetryCount: 5,
            LastRetryAt: DateTimeOffset.Parse("2026-04-15T10:00:00+00:00"),
            FailedAt: DateTimeOffset.Parse("2026-04-15T10:05:00+00:00"),
            SourcePayloadReference: sourcePayloadReference,
            Metadata: metadata);

    private static WorkflowPayloadReference CreateSourceReference(string memoryUnitId)
        => new(
            $"{memoryUnitId}:sourcebytes:abc:source",
            "abc",
            3,
            WorkflowPayloadKind.SourceBytes,
            "tenant-1",
            memoryUnitId);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }
}
