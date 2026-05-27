// <copyright file="DirectoryIngestionServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using System.Runtime.CompilerServices;

using Dapr.Client;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

public class DirectoryIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_UnreadableCandidate_ShouldPersistNoScheduledFilesAndUlidBatchId()
    {
        string root = CreateTempDirectory();
        string lockedPath = Path.Combine(root, "locked.txt");
        await File.WriteAllTextAsync(lockedPath, "locked", CancellationToken.None);

        await using FileStream lockStream = new(lockedPath, FileMode.Open, FileAccess.Read, FileShare.None);

        DaprWorkflowClient workflowClient = CreateUnusedWorkflowClient();
        DaprClient daprClient = Substitute.For<DaprClient>();
        DirectoryBatchState? savedState = null;

        daprClient
            .SaveStateAsync(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                Arg.Any<DirectoryBatchState>(),
                Arg.Any<StateOptions>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        daprClient
            .When(x => x.SaveStateAsync(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                Arg.Any<DirectoryBatchState>(),
                Arg.Any<StateOptions>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo => savedState = callInfo.ArgAt<DirectoryBatchState>(2));

        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings { AllowedDirectoryRoots = [root] }),
            workflowClient,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance);

        DirectoryIngestionResult result;
        try
        {
            result = await service.IngestAsync(
                new DirectoryIngestionRequest
                {
                    TenantId = "tenant-1",
                    CaseId = "case-1",
                    DirectoryPath = root,
                    IngestedBy = "tester",
                },
                CancellationToken.None);
        }
        finally
        {
            await lockStream.DisposeAsync();
            DeleteTempDirectory(root);
        }

        result.ErrorCode.ShouldBeNull();
        result.Outcome.ShouldNotBeNull();
        result.Outcome.BatchId.Length.ShouldBe(26);
        result.Outcome.BatchId.ShouldNotContain('-');
        result.Outcome.Discovered.ShouldBe(1);
        result.Outcome.Enqueued.ShouldBe(0);
        result.Outcome.InstanceIds.ShouldBeEmpty();
        result.Outcome.Skipped.ShouldContain(item => item.Path == lockedPath && item.Reason == "FILE_UNREADABLE");

        savedState.ShouldNotBeNull();
        savedState!.Discovered.ShouldBe(1);
        savedState.Files.ShouldBeEmpty();
        savedState.Skipped.ShouldContain(item => item.Path == lockedPath && item.Reason == "FILE_UNREADABLE");
    }

    [Fact]
    public void CreateBatchState_ShouldUseScheduledFilesForPersistedMappings()
    {
        DirectoryIngestionRequest request = new()
        {
            TenantId = "tenant-1",
            CaseId = "case-1",
            DirectoryPath = @"D:\\ingest",
            IngestedBy = "tester",
        };
        DateTimeOffset createdAt = new(2026, 4, 15, 10, 30, 0, TimeSpan.Zero);

        DirectoryBatchState state = DirectoryIngestionService.CreateBatchState(
            batchId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            request,
            discovered: 2,
            instanceIds: ["wf-1"],
            scheduledFiles: [new BatchFileRef("wf-1", @"D:\\ingest\\good.txt")],
            skipped: [new SkippedFile(@"D:\\ingest\\locked.txt", "FILE_UNREADABLE")],
            createdAt);

        state.BatchId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        state.Discovered.ShouldBe(2);
        state.InstanceIds.ShouldBe(["wf-1"]);
        state.Files.Length.ShouldBe(1);
        state.Files[0].InstanceId.ShouldBe("wf-1");
        state.Files[0].SourceUri.ShouldBe(@"D:\\ingest\\good.txt");
        state.Skipped.Length.ShouldBe(1);
        state.Skipped[0].Path.ShouldBe(@"D:\\ingest\\locked.txt");
        state.Skipped[0].Reason.ShouldBe("FILE_UNREADABLE");
    }

    [Fact]
    public async Task IngestAsync_WhenSkippedEntriesOverflow_ShouldLogOverflowedItems()
    {
        string root = CreateTempDirectory();
        string firstPath = Path.Combine(root, "a.exe");
        string secondPath = Path.Combine(root, "b.exe");
        string thirdPath = Path.Combine(root, "c.exe");
        await File.WriteAllTextAsync(firstPath, "x", CancellationToken.None);
        await File.WriteAllTextAsync(secondPath, "y", CancellationToken.None);
        await File.WriteAllTextAsync(thirdPath, "z", CancellationToken.None);

        List<(LogLevel Level, EventId EventId, string Message)> logs = [];
        ILogger<DirectoryIngestionService> logger = new ListLogger<DirectoryIngestionService>(logs);
        DaprWorkflowClient workflowClient = CreateUnusedWorkflowClient();
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient
            .SaveStateAsync(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                Arg.Any<DirectoryBatchState>(),
                Arg.Any<StateOptions>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings { AllowedDirectoryRoots = [root], MaxSkippedReportSize = 1 }),
            workflowClient,
            daprClient,
            logger);

        DirectoryIngestionResult result;
        try
        {
            result = await service.IngestAsync(
                new DirectoryIngestionRequest
                {
                    TenantId = "tenant-1",
                    CaseId = "case-1",
                    DirectoryPath = root,
                    IngestedBy = "tester",
                },
                CancellationToken.None);
        }
        finally
        {
            DeleteTempDirectory(root);
        }

        result.ErrorCode.ShouldBeNull();
        result.Outcome.ShouldNotBeNull();
        result.Outcome.Discovered.ShouldBe(3);
        result.Outcome.Enqueued.ShouldBe(0);
        result.Outcome.Skipped.Count.ShouldBe(1);
        result.Outcome.SkippedTruncated.ShouldBeTrue();

        logs.Count(log => log.EventId.Id == 6108).ShouldBe(3);
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "hexalith-directory-ingestion-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static DaprWorkflowClient CreateUnusedWorkflowClient()
        => (DaprWorkflowClient)RuntimeHelpers.GetUninitializedObject(typeof(DaprWorkflowClient));

    private sealed class ListLogger<TCategory>(List<(LogLevel Level, EventId EventId, string Message)> sink) : ILogger<TCategory>
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => sink.Add((logLevel, eventId, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}