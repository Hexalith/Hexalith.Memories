// <copyright file="ReIngestionCoordinatorTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Hexalith.Memories.Contracts.V1;
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
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns((FailedUnitRecord?)null);
        ReIngestionCoordinator coordinator = new(registry, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.NotFound);
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenClaimLost_ReturnsConflict()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-1");

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(false);

        ReIngestionCoordinator coordinator = new(registry, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        ReIngestionAttemptResult result = await coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None);

        result.Outcome.ShouldBe(ReIngestionAttemptOutcome.Conflict);
        await scheduler.DidNotReceive().ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>());
    }

    [Fact]
    public async Task TryScheduleAsync_WhenClaimed_UsesMemoryUnitIdAsWorkflowInstanceId()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-1", SourceType.File, "text/markdown");

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-1", Arg.Any<IngestionInput>()).Returns("wf-mu-1");

        ReIngestionCoordinator coordinator = new(registry, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

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
                && input.Metadata.Count == 0));
    }

    [Fact]
    public async Task TryScheduleAsync_WhenWorkflowSchedulingFails_RestoresClaimAndRethrows()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
        IIngestionWorkflowScheduler scheduler = Substitute.For<IIngestionWorkflowScheduler>();
        FailedUnitRecord record = CreateRecord("mu-1");

        registry.GetAsync("tenant-1", "mu-1", Arg.Any<CancellationToken>()).Returns(record);
        registry.RemoveAsync("tenant-1", "case-1", "mu-1", record.SourceUri, Arg.Any<CancellationToken>()).Returns(true);
        scheduler.ScheduleAsync("mu-1", Arg.Any<IngestionInput>())
            .Returns(Task.FromException<string>(new InvalidOperationException("scheduler down")));

        ReIngestionCoordinator coordinator = new(registry, scheduler, NullLogger<ReIngestionCoordinator>.Instance);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => coordinator.TryScheduleAsync("tenant-1", "case-1", "mu-1", CancellationToken.None));

        ex.Message.ShouldBe("scheduler down");
        await registry.Received().RestoreAsync(record, CancellationToken.None);
    }

    [Fact]
    public async Task TryScheduleManyAsync_WhenThirdUnitRemoveThrows_ReportsErrorOutcomeAndLogs6305()
    {
        IFailedUnitsRegistry registry = Substitute.For<IFailedUnitsRegistry>();
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

        ReIngestionCoordinator coordinator = new(registry, scheduler, logger);

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

    private static FailedUnitRecord CreateRecord(
        string memoryUnitId,
        SourceType sourceType = SourceType.Url,
        string? contentType = null)
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
            FailedAt: DateTimeOffset.Parse("2026-04-15T10:05:00+00:00"));

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