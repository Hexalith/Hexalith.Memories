// <copyright file="IngestionStatusEndpointAuthorizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Authentication;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

using Dapr.Client;
using Dapr.Common.Serialization;
using Dapr.Workflow;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;

using NSubstitute;
using Shouldly;

public sealed class IngestionStatusEndpointAuthorizationTests : IDisposable
{
    private static readonly JsonDaprSerializer Serializer = new(MemoriesJsonContext.Options);
    private static readonly Type WorkflowMetadataType = Type.GetType(
        "Dapr.Workflow.Client.WorkflowMetadata, Dapr.Workflow",
        throwOnError: true)!;
    private readonly EventStoreWebAppFactory _factory = new();

    [Fact]
    public async Task SingleWorkflowStatus_WithMismatchedTenant_ReturnsTenantForbiddenWithoutRawWorkflowState()
    {
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("wf-tenant-b", true, Arg.Any<CancellationToken>())
            .Returns(CreateWorkflowState("wf-tenant-b", "tenant-b", WorkflowRuntimeStatus.Running));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/wf-tenant-b",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options)!;
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        body.ShouldNotContain("WorkflowState", Shouldly.Case.Insensitive);
        body.ShouldNotContain("SerializedInput", Shouldly.Case.Insensitive);
        body.ShouldNotContain("ContentBytes", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task SingleWorkflowStatus_WithMatchingTenant_ReturnsProjectedStatus()
    {
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("wf-tenant-a", true, Arg.Any<CancellationToken>())
            .Returns(CreateWorkflowState("wf-tenant-a", "tenant-a", WorkflowRuntimeStatus.Completed, "mu-1"));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/wf-tenant-a",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        IngestionWorkflowStatus status = JsonSerializer.Deserialize<IngestionWorkflowStatus>(
            body,
            MemoriesJsonContext.Options)!;
        status.InstanceId.ShouldBe("wf-tenant-a");
        status.TenantId.ShouldBe("tenant-a");
        status.CaseId.ShouldBe("case-1");
        status.MemoryUnitId.ShouldBe("mu-1");
        status.MemoryUnitStatus.ShouldBe(MemoryUnitStatus.Indexed);
        body.ShouldNotContain("WorkflowState", Shouldly.Case.Insensitive);
        body.ShouldNotContain("SerializedInput", Shouldly.Case.Insensitive);
        body.ShouldNotContain("ContentBytes", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task SingleWorkflowStatus_WithMissingWorkflowState_ReturnsNotFound()
    {
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("missing", true, Arg.Any<CancellationToken>())
            .Returns((WorkflowState?)null);
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/missing",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SingleWorkflowStatus_WhenWorkflowStateCannotBeRead_ReturnsStructuredNotFound()
    {
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("unreadable", true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<WorkflowState?>(new InvalidOperationException("Dapr state is unavailable.")));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/unreadable",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            MemoriesJsonContext.Options,
            TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Missing not-found body.");
        error.Code.ShouldBe("INGESTION_STATUS_NOT_FOUND");
    }

    [Fact]
    public async Task SingleWorkflowStatus_WithUnreadableWorkflowInput_ReturnsTenantForbiddenWithoutRawWorkflowState()
    {
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("wf-bad-input", true, Arg.Any<CancellationToken>())
            .Returns(CreateWorkflowState(
                "wf-bad-input",
                "tenant-b",
                WorkflowRuntimeStatus.Running,
                inputJson: "{\"tenantId\":"));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/wf-bad-input",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options)!;
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        body.ShouldNotContain("WorkflowState", Shouldly.Case.Insensitive);
        body.ShouldNotContain("SerializedInput", Shouldly.Case.Insensitive);
        body.ShouldNotContain("tenant-b", Shouldly.Case.Insensitive);
    }

    [Fact]
    public async Task BatchStatus_WithMismatchedTenant_DeniesBeforeWorkflowFanOut()
    {
        _factory.DaprClient.GetStateAsync<DirectoryBatchState>(
            DirectoryIngestionService.StateStoreName,
            DirectoryIngestionService.BatchStateKeyPrefix + "batch-tenant-b",
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateBatchState("batch-tenant-b", "tenant-b"));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/batches/batch-tenant-b",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain(@"D:\\docs\\a.txt", Shouldly.Case.Insensitive);
        ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options)!;
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        await _factory.IngestionWorkflowStateReader.DidNotReceiveWithAnyArgs()
            .GetWorkflowStateAsync(default!, default, default);
    }

    [Fact]
    public async Task BatchStatus_WithMissingBatchState_ReturnsNotFoundBeforeWorkflowFanOut()
    {
        _factory.DaprClient.GetStateAsync<DirectoryBatchState>(
            DirectoryIngestionService.StateStoreName,
            DirectoryIngestionService.BatchStateKeyPrefix + "missing-batch",
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns((DirectoryBatchState)null!);
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/batches/missing-batch",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse error = await response.Content.ReadFromJsonAsync<ErrorResponse>(
            MemoriesJsonContext.Options,
            TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Missing not-found body.");
        error.Code.ShouldBe("BATCH_NOT_FOUND");
        await _factory.IngestionWorkflowStateReader.DidNotReceiveWithAnyArgs()
            .GetWorkflowStateAsync(default!, default, default);
    }

    [Fact]
    public async Task BatchStatus_WithMalformedStoredTenant_DeniesBeforeWorkflowFanOut()
    {
        _factory.DaprClient.GetStateAsync<DirectoryBatchState>(
            DirectoryIngestionService.StateStoreName,
            DirectoryIngestionService.BatchStateKeyPrefix + "batch-malformed-tenant",
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateBatchState("batch-malformed-tenant", "tenant with spaces"));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/batches/batch-malformed-tenant",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ErrorResponse error = JsonSerializer.Deserialize<ErrorResponse>(body, MemoriesJsonContext.Options)!;
        error.Code.ShouldBe("TENANT_FORBIDDEN");
        body.ShouldNotContain("tenant with spaces", Shouldly.Case.Insensitive);
        body.ShouldNotContain(@"D:\\docs\\a.txt", Shouldly.Case.Insensitive);
        await _factory.IngestionWorkflowStateReader.DidNotReceiveWithAnyArgs()
            .GetWorkflowStateAsync(default!, default, default);
    }

    [Fact]
    public async Task BatchStatus_WithMatchingTenant_PreservesBatchStatusResponse()
    {
        _factory.DaprClient.GetStateAsync<DirectoryBatchState>(
            DirectoryIngestionService.StateStoreName,
            DirectoryIngestionService.BatchStateKeyPrefix + "batch-tenant-a",
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateBatchState("batch-tenant-a", "tenant-a"));
        _factory.IngestionWorkflowStateReader
            .GetWorkflowStateAsync("wf-1", true, Arg.Any<CancellationToken>())
            .Returns(CreateWorkflowState("wf-1", "tenant-a", WorkflowRuntimeStatus.Completed, "mu-1"));
        using HttpClient client = CreateAuthorizedClient("tenant-a");

        using HttpResponseMessage response = await client.GetAsync(
            "/api/v1/ingest/batches/batch-tenant-a",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        BatchStatusResponse status = await response.Content.ReadFromJsonAsync<BatchStatusResponse>(
            MemoriesJsonContext.Options,
            TestContext.Current.CancellationToken) ?? throw new InvalidOperationException("Missing batch body.");
        status.BatchId.ShouldBe("batch-tenant-a");
        status.TenantId.ShouldBe("tenant-a");
        status.CaseId.ShouldBe("case-1");
        status.Instances.Count.ShouldBe(1);
        status.Instances[0].MemoryUnitId.ShouldBe("mu-1");
        status.Counts.Indexed.ShouldBe(1);
    }

    private HttpClient CreateAuthorizedClient(string tenantId)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ServerTestBearerToken.Create(tenants: [tenantId]));
        return client;
    }

    private static DirectoryBatchState CreateBatchState(string batchId, string tenantId)
        => new(
            batchId,
            tenantId,
            "case-1",
            Discovered: 1,
            InstanceIds: ["wf-1"],
            Files: [new BatchFileRef("wf-1", @"D:\\docs\\a.txt")],
            Skipped: [],
            CreatedAt: DateTimeOffset.Parse("2026-07-04T10:00:00+00:00"));

    private static WorkflowState CreateWorkflowState(
        string instanceId,
        string tenantId,
        WorkflowRuntimeStatus status,
        string? memoryUnitId = null,
        string? inputJson = null)
    {
        DateTime created = new(2026, 7, 4, 10, 0, 0, DateTimeKind.Utc);
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = "case-1",
            SourceUri = "test://source",
            ContentType = "text/plain",
            SourceType = SourceType.Event,
            IngestedBy = "operator-1",
        };
        IngestionResult? result = memoryUnitId is null
            ? null
            : new IngestionResult(
                memoryUnitId,
                MemoryUnitStatus.Indexed,
                DateTimeOffset.Parse("2026-07-04T10:05:00+00:00"),
                WasDuplicate: false,
                ConsistencyNote: null);
        object metadata = Activator.CreateInstance(
            WorkflowMetadataType,
            instanceId,
            "IngestionWorkflow",
            status,
            created,
            created.AddMinutes(5),
            Serializer)!;
        WorkflowMetadataType.GetProperty("SerializedInput")!.SetValue(metadata, inputJson ?? Serializer.Serialize(input));
        WorkflowMetadataType.GetProperty("SerializedOutput")!
            .SetValue(metadata, result is null ? null : Serializer.Serialize(result));

        return (WorkflowState)Activator.CreateInstance(
            typeof(WorkflowState),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [metadata],
            culture: null)!;
    }

    public void Dispose() => _factory.Dispose();
}
