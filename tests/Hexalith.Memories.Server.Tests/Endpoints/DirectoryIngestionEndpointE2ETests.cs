// <copyright file="DirectoryIngestionEndpointE2ETests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 23.6 API-level coverage for directory batch scalability behavior through the real minimal API routes.
/// </summary>
public sealed class DirectoryIngestionEndpointE2ETests : IDisposable
{
    private const string StoreName = "statestore";
    private const string TenantId = "tenant-a";
    private const string CaseId = "case-1";

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly IWorkflowPayloadStore _payloadStore = Substitute.For<IWorkflowPayloadStore>();
    private readonly IIngestionWorkflowScheduler _scheduler = Substitute.For<IIngestionWorkflowScheduler>();
    private readonly List<IngestionInput> _scheduledInputs = [];
    private readonly List<DirectoryBatchState> _savedStates = [];
    private readonly object _gate = new();

    public DirectoryIngestionEndpointE2ETests()
    {
        _factory.WorkflowPayloadStoreOverride = _payloadStore;
        _factory.IngestionWorkflowSchedulerOverride = _scheduler;
    }

    [Fact]
    public async Task PostDirectory_WithMixedBatch_ShouldAcceptCheckpointStateAndExposeQueuedBatchStatus()
    {
        string root = CreateTempDirectory();
        try
        {
            for (int i = 0; i < 6; i++)
            {
                await File.WriteAllTextAsync(Path.Combine(root, $"document-{i:00}.TXT"), $"document {i}", TestContext.Current.CancellationToken);
            }

            string unsupportedPath = Path.Combine(root, "unsupported.xyz");
            await File.WriteAllTextAsync(unsupportedPath, "unsupported", TestContext.Current.CancellationToken);
            ConfigureDirectoryIngestion(root, checkpointSize: 3);
            StubTenantActive();
            StubDaprBatchStateStore();
            StubPayloadStore();
            StubScheduler();
            using HttpClient client = CreateAuthorizedClient();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/ingest/directory",
                CreateRequest(root),
                MemoriesJsonContext.Options,
                TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            DirectoryIngestionOutcome outcome = await ReadJsonAsync<DirectoryIngestionOutcome>(response);
            outcome.TenantId.ShouldBe(TenantId);
            outcome.CaseId.ShouldBe(CaseId);
            outcome.Discovered.ShouldBe(7);
            outcome.Enqueued.ShouldBe(6);
            outcome.InstanceIds.Count.ShouldBe(6);
            outcome.InstanceIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(6);
            outcome.Skipped.ShouldContain(item => item.Path == unsupportedPath && item.Reason == "UNSUPPORTED_EXTENSION");
            response.Headers.Location.ShouldNotBeNull();
            response.Headers.Location!.ToString().ShouldBe($"/api/ingest/batches/{outcome.BatchId}");

            List<IngestionInput> scheduledInputs;
            lock (_gate)
            {
                scheduledInputs = [.. _scheduledInputs];
            }

            scheduledInputs.Count.ShouldBe(6);
            foreach (IngestionInput input in scheduledInputs)
            {
                input.ContentBytes.ShouldBeNull();
                input.PayloadReference.ShouldNotBeNull();
            }

            scheduledInputs.ShouldAllBe(input => input.TenantId == TenantId);
            scheduledInputs.ShouldAllBe(input => input.CaseId == CaseId);
            scheduledInputs.ShouldAllBe(input => input.SourceType == SourceType.File);
            scheduledInputs.ShouldAllBe(input => input.CorrelationId == outcome.BatchId);
            scheduledInputs.ShouldAllBe(input => input.CausationId == "cause-23-6");

            _savedStates.Count.ShouldBe(4);
            _savedStates.Count.ShouldBeLessThan(outcome.Enqueued);
            DirectoryBatchState finalState = _savedStates[^1];
            finalState.Files.Length.ShouldBe(6);
            finalState.Skipped.ShouldContain(item => item.Path == unsupportedPath && item.Reason == "UNSUPPORTED_EXTENSION");
            finalState.Files.Select(file => file.SourceUri).ShouldBe(finalState.Files.Select(file => file.SourceUri).Order(StringComparer.Ordinal));

            using HttpResponseMessage statusResponse = await client.GetAsync(
                $"/api/ingest/batches/{outcome.BatchId}",
                TestContext.Current.CancellationToken);

            statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            BatchStatusResponse status = await ReadJsonAsync<BatchStatusResponse>(statusResponse);
            status.BatchId.ShouldBe(outcome.BatchId);
            status.Enqueued.ShouldBe(6);
            status.Skipped.ShouldBe(1);
            status.Counts.Queued.ShouldBe(6);
            status.Instances.Count.ShouldBe(6);
            status.Instances.ShouldAllBe(instance => instance.Status == "queued");
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task PostDirectory_WhenSchedulingFailsAfterClaimCheck_ShouldReturnStructuredFailureAndDeletePayload()
    {
        string root = CreateTempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "document.txt"), "content", TestContext.Current.CancellationToken);
            WorkflowPayloadReference reference = CreatePayloadReference("mu-failed", 7);
            ConfigureDirectoryIngestion(root, checkpointSize: 10);
            StubTenantActive();
            StubDaprBatchStateStore();
            _payloadStore
                .SaveAsync(
                    TenantId,
                    Arg.Any<string>(),
                    WorkflowPayloadKind.SourceBytes,
                    Arg.Any<ReadOnlyMemory<byte>>(),
                    "source",
                    Arg.Any<CancellationToken>())
                .Returns(reference);
            _scheduler
                .ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<string>(new InvalidOperationException("scheduler unavailable")));
            using HttpClient client = CreateAuthorizedClient();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/ingest/directory",
                CreateRequest(root),
                MemoriesJsonContext.Options,
                TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            ErrorResponse error = await ReadJsonAsync<ErrorResponse>(response);
            error.Code.ShouldBe("BATCH_SCHEDULING_FAILED");
            await _payloadStore.Received(1).DeleteAsync(reference, Arg.Any<CancellationToken>());
            _savedStates[^1].Files.ShouldBeEmpty();
            _savedStates[^1].InstanceIds.ShouldBeEmpty();
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    public void Dispose() => _factory.Dispose();

    private static DirectoryIngestionRequest CreateRequest(string root) => new()
    {
        TenantId = TenantId,
        CaseId = CaseId,
        DirectoryPath = root,
        IngestedBy = "tester",
        CausationId = "cause-23-6",
    };

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "hexalith-directory-ingestion-endpoint-tests", Guid.NewGuid().ToString("N"));
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

    private void ConfigureDirectoryIngestion(string root, int checkpointSize)
    {
        _factory.ConfigurationOverrides["Ingestion:AllowedDirectoryRoots:0"] = root;
        _factory.ConfigurationOverrides["Ingestion:DirectorySchedulingParallelism"] = "4";
        _factory.ConfigurationOverrides["Ingestion:DirectoryBatchCheckpointSize"] = checkpointSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private HttpClient CreateAuthorizedClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: [TenantId]));
        return client;
    }

    private void StubTenantActive()
    {
        TenantRegistryEntry entry = new(
            new TenantInfo(TenantId, TenantId, TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<TenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key == $"tenant-registry-{TenantId}"),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private void StubDaprBatchStateStore()
    {
        Dictionary<string, DirectoryBatchState> states = new(StringComparer.Ordinal);
        _factory.DaprClient
            .SaveStateAsync(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                Arg.Any<DirectoryBatchState>(),
                Arg.Any<StateOptions>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _factory.DaprClient
            .When(x => x.SaveStateAsync(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                Arg.Any<DirectoryBatchState>(),
                Arg.Any<StateOptions>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>()))
            .Do(callInfo =>
            {
                string key = callInfo.ArgAt<string>(1);
                DirectoryBatchState state = callInfo.ArgAt<DirectoryBatchState>(2);
                lock (_gate)
                {
                    states[key] = state;
                    _savedStates.Add(state);
                }
            });
        _factory.DaprClient
            .GetStateAsync<DirectoryBatchState>(
                DirectoryIngestionService.StateStoreName,
                Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                string key = callInfo.ArgAt<string>(1);
                lock (_gate)
                {
                    return Task.FromResult(states.GetValueOrDefault(key)!);
                }
            });
    }

    private void StubPayloadStore()
    {
        _payloadStore
            .SaveAsync(
                TenantId,
                Arg.Any<string>(),
                WorkflowPayloadKind.SourceBytes,
                Arg.Any<ReadOnlyMemory<byte>>(),
                "source",
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                string memoryUnitId = callInfo.ArgAt<string>(1);
                ReadOnlyMemory<byte> bytes = callInfo.ArgAt<ReadOnlyMemory<byte>>(3);
                return CreatePayloadReference(memoryUnitId, bytes.Length);
            });
    }

    private void StubScheduler()
    {
        _scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                string instanceId = callInfo.ArgAt<string>(0);
                IngestionInput input = callInfo.ArgAt<IngestionInput>(1);
                lock (_gate)
                {
                    _scheduledInputs.Add(input);
                }

                return instanceId;
            });
    }

    private static WorkflowPayloadReference CreatePayloadReference(string memoryUnitId, long byteLength)
        => new(
            memoryUnitId + ":sourcebytes:payload:source",
            "payload",
            byteLength,
            WorkflowPayloadKind.SourceBytes,
            TenantId,
            memoryUnitId);

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        where T : class
    {
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        T? value = JsonSerializer.Deserialize<T>(body, MemoriesJsonContext.Options);
        value.ShouldNotBeNull();
        return value;
    }
}
