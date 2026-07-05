// <copyright file="DirectoryIngestionServiceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Ingestion;

using Dapr.Client;

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

        IIngestionWorkflowScheduler scheduler = CreateScheduler();
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
            scheduler,
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
        List<IngestionInput> scheduledInputs = [];
        IIngestionWorkflowScheduler scheduler = new DelegateScheduler((instanceId, input, _) =>
        {
            scheduledInputs.Add(input);
            return Task.FromResult(instanceId);
        });
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
            scheduler,
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

    [Fact]
    public async Task IngestAsync_UsesSupportedExtensionsAsAllowlistBeforeClaimCheckAndSchedule()
    {
        string root = CreateTempDirectory();
        string supportedPath = Path.Combine(root, "allowed.TXT");
        string unknownPath = Path.Combine(root, "unknown.xyz");
        string deniedPath = Path.Combine(root, "denied.exe");
        string extensionlessPath = Path.Combine(root, "extensionless");
        await File.WriteAllTextAsync(supportedPath, "allowed", CancellationToken.None);
        await File.WriteAllTextAsync(unknownPath, "unknown", CancellationToken.None);
        await File.WriteAllTextAsync(deniedPath, "denied", CancellationToken.None);
        await File.WriteAllTextAsync(extensionlessPath, "none", CancellationToken.None);

        List<IngestionInput> scheduledInputs = [];
        IIngestionWorkflowScheduler scheduler = new DelegateScheduler((instanceId, input, _) =>
        {
            scheduledInputs.Add(input);
            return Task.FromResult(instanceId);
        });
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .SaveAsync(
                "tenant-1",
                Arg.Any<string>(),
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(CreatePayloadReference(callInfo.ArgAt<string>(1))));
        DaprClient daprClient = CreateSavingDaprClient();
        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings
            {
                AllowedDirectoryRoots = [root],
                SupportedExtensions = ["TXT"],
                UnsupportedExtensions = [".exe"],
            }),
            scheduler,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance,
            payloadStore);

        DirectoryIngestionResult result;
        try
        {
            result = await service.IngestAsync(CreateRequest(root), CancellationToken.None);
        }
        finally
        {
            DeleteTempDirectory(root);
        }

        result.ErrorCode.ShouldBeNull();
        result.Outcome.ShouldNotBeNull();
        result.Outcome.Enqueued.ShouldBe(1);
        result.Outcome.Skipped.Count.ShouldBe(3);
        result.Outcome.Skipped.ShouldContain(item => item.Path == unknownPath && item.Reason == "UNSUPPORTED_EXTENSION");
        result.Outcome.Skipped.ShouldContain(item => item.Path == deniedPath && item.Reason == "UNSUPPORTED_EXTENSION");
        result.Outcome.Skipped.ShouldContain(item => item.Path == extensionlessPath && item.Reason == "UNSUPPORTED_EXTENSION");
        await payloadStore.Received(1).SaveAsync(
            "tenant-1",
            Arg.Any<string>(),
            WorkflowPayloadKind.SourceBytes,
            Arg.Any<ReadOnlyMemory<byte>>(),
            "source",
            Arg.Any<CancellationToken>());
        scheduledInputs.Count.ShouldBe(1);
        scheduledInputs[0].SourceUri.ShouldBe(supportedPath);
        scheduledInputs[0].PayloadReference.ShouldNotBeNull();
    }

    [Fact]
    public async Task IngestAsync_MultiFileBatch_ShouldPersistBoundedCheckpointsAndCompleteFinalState()
    {
        string root = CreateTempDirectory();
        for (int i = 0; i < 12; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(root, $"file-{i:00}.txt"), "content", CancellationToken.None);
        }

        IIngestionWorkflowScheduler scheduler = CreateScheduler();
        List<DirectoryBatchState> savedStates = [];
        DaprClient daprClient = CreateSavingDaprClient(savedStates);
        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings
            {
                AllowedDirectoryRoots = [root],
                DirectorySchedulingParallelism = 4,
                DirectoryBatchCheckpointSize = 5,
                SupportedExtensions = [".txt"],
            }),
            scheduler,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance);

        DirectoryIngestionResult result;
        try
        {
            result = await service.IngestAsync(CreateRequest(root), CancellationToken.None);
        }
        finally
        {
            DeleteTempDirectory(root);
        }

        result.ErrorCode.ShouldBeNull();
        result.Outcome.ShouldNotBeNull();
        result.Outcome.Enqueued.ShouldBe(12);
        savedStates.Count.ShouldBe(4);
        savedStates.Count.ShouldBeLessThan(result.Outcome.Enqueued);
        DirectoryBatchState finalState = savedStates[^1];
        finalState.Files.Length.ShouldBe(12);
        finalState.InstanceIds.Length.ShouldBe(12);
        finalState.Files.Select(file => file.SourceUri).ShouldBe(finalState.Files.Select(file => file.SourceUri).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task IngestAsync_ShouldHonorConfiguredSchedulingParallelism()
    {
        string root = CreateTempDirectory();
        for (int i = 0; i < 6; i++)
        {
            await File.WriteAllTextAsync(Path.Combine(root, $"parallel-{i}.txt"), "content", CancellationToken.None);
        }

        TaskCompletionSource twoInFlight = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSchedulers = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int inFlight = 0;
        int maxInFlight = 0;
        IIngestionWorkflowScheduler scheduler = new DelegateScheduler(async (instanceId, _, cancellationToken) =>
        {
            int current = Interlocked.Increment(ref inFlight);
            UpdateMax(ref maxInFlight, current);
            if (current == 2)
            {
                twoInFlight.TrySetResult();
            }

            await releaseSchedulers.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Decrement(ref inFlight);
            return instanceId;
        });
        DaprClient daprClient = CreateSavingDaprClient();
        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings
            {
                AllowedDirectoryRoots = [root],
                DirectorySchedulingParallelism = 2,
                SupportedExtensions = [".txt"],
            }),
            scheduler,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance);

        Task<DirectoryIngestionResult> ingestTask = service.IngestAsync(CreateRequest(root), CancellationToken.None);
        try
        {
            await twoInFlight.Task.WaitAsync(TimeSpan.FromSeconds(5));
            maxInFlight.ShouldBe(2);
            releaseSchedulers.SetResult();
            DirectoryIngestionResult result = await ingestTask;
            result.ErrorCode.ShouldBeNull();
            result.Outcome.ShouldNotBeNull();
            result.Outcome.Enqueued.ShouldBe(6);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task IngestAsync_WhenSchedulingFailsAfterClaimCheck_ShouldDeleteUnscheduledPayloadAndPersistNoFile()
    {
        string root = CreateTempDirectory();
        string filePath = Path.Combine(root, "document.txt");
        await File.WriteAllTextAsync(filePath, "content", CancellationToken.None);
        WorkflowPayloadReference reference = CreatePayloadReference("created-mu");
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .SaveAsync(
                "tenant-1",
                Arg.Any<string>(),
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(reference);
        IIngestionWorkflowScheduler scheduler = new DelegateScheduler((_, _, _) => throw new InvalidOperationException("scheduler unavailable"));
        List<DirectoryBatchState> savedStates = [];
        DaprClient daprClient = CreateSavingDaprClient(savedStates);
        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings
            {
                AllowedDirectoryRoots = [root],
                SupportedExtensions = [".txt"],
            }),
            scheduler,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance,
            payloadStore);

        DirectoryIngestionResult result;
        try
        {
            result = await service.IngestAsync(CreateRequest(root), CancellationToken.None);
        }
        finally
        {
            DeleteTempDirectory(root);
        }

        result.ErrorCode.ShouldBe("BATCH_SCHEDULING_FAILED");
        result.BatchId.ShouldNotBeNullOrWhiteSpace();
        await payloadStore.Received(1).DeleteAsync(reference, Arg.Any<CancellationToken>());
        DirectoryBatchState finalState = savedStates[^1];
        finalState.Files.ShouldBeEmpty();
        finalState.InstanceIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task IngestAsync_WhenSchedulingIsCanceledAfterClaimCheck_ShouldDeleteUnscheduledPayloadAndNotSucceed()
    {
        string root = CreateTempDirectory();
        string filePath = Path.Combine(root, "document.txt");
        await File.WriteAllTextAsync(filePath, "content", CancellationToken.None);
        WorkflowPayloadReference reference = CreatePayloadReference("created-mu");
        IWorkflowPayloadStore payloadStore = Substitute.For<IWorkflowPayloadStore>();
        payloadStore
            .SaveAsync(
                "tenant-1",
                Arg.Any<string>(),
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(reference);
        using CancellationTokenSource cancellation = new();
        IIngestionWorkflowScheduler scheduler = new DelegateScheduler((_, _, _) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<string>(cancellation.Token);
        });
        DaprClient daprClient = CreateSavingDaprClient();
        DirectoryIngestionService service = new(
            Options.Create(new IngestionSettings
            {
                AllowedDirectoryRoots = [root],
                SupportedExtensions = [".txt"],
            }),
            scheduler,
            daprClient,
            NullLogger<DirectoryIngestionService>.Instance,
            payloadStore);

        try
        {
            await Should.ThrowAsync<OperationCanceledException>(
                () => service.IngestAsync(CreateRequest(root), cancellation.Token));
        }
        finally
        {
            DeleteTempDirectory(root);
        }

        await payloadStore.Received(1).DeleteAsync(reference, Arg.Any<CancellationToken>());
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

    private static DirectoryIngestionRequest CreateRequest(string root) => new()
    {
        TenantId = "tenant-1",
        CaseId = "case-1",
        DirectoryPath = root,
        IngestedBy = "tester",
    };

    private static IIngestionWorkflowScheduler CreateScheduler()
        => new DelegateScheduler((instanceId, _, _) => Task.FromResult(instanceId));

    private static DaprClient CreateSavingDaprClient(List<DirectoryBatchState>? savedStates = null)
    {
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
        if (savedStates is not null)
        {
            daprClient
                .When(x => x.SaveStateAsync(
                    DirectoryIngestionService.StateStoreName,
                    Arg.Any<string>(),
                    Arg.Any<DirectoryBatchState>(),
                    Arg.Any<StateOptions>(),
                    Arg.Any<IReadOnlyDictionary<string, string>>(),
                    Arg.Any<CancellationToken>()))
                .Do(callInfo => savedStates.Add(callInfo.ArgAt<DirectoryBatchState>(2)));
        }

        return daprClient;
    }

    private static WorkflowPayloadReference CreatePayloadReference(string memoryUnitId)
        => new(
            memoryUnitId + ":sourcebytes:payload:source",
            "payload",
            7,
            WorkflowPayloadKind.SourceBytes,
            "tenant-1",
            memoryUnitId);

    private static void UpdateMax(ref int target, int candidate)
    {
        int current;
        do
        {
            current = Volatile.Read(ref target);
            if (candidate <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, candidate, current) != current);
    }

    private sealed class DelegateScheduler(
        Func<string, IngestionInput, CancellationToken, Task<string>> schedule) : IIngestionWorkflowScheduler
    {
        public Task<string> ScheduleAsync(string instanceId, IngestionInput input, CancellationToken cancellationToken = default)
            => schedule(instanceId, input, cancellationToken);
    }

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
