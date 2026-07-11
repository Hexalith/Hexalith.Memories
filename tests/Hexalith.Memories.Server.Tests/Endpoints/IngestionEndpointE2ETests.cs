// <copyright file="IngestionEndpointE2ETests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Endpoints;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Authentication;
using Hexalith.Memories.Server.Tests.EventStoreIntegration;
using Hexalith.Memories.TestHelpers.Factories;
using Hexalith.Memories.Telemetry;

using NSubstitute;

using Shouldly;

/// <summary>Story 23.8 API-level coverage for file ingestion scheduling through the deterministic scheduler seam.</summary>
public sealed class IngestionEndpointE2ETests : IDisposable
{
    private const string StoreName = "statestore";
    private const string TenantId = "tenant-a";
    private const string CaseId = "case-1";

    private readonly EventStoreWebAppFactory _factory = new();
    private readonly IIngestionWorkflowScheduler _scheduler = Substitute.For<IIngestionWorkflowScheduler>();

    public IngestionEndpointE2ETests()
    {
        _factory.IngestionWorkflowSchedulerOverride = _scheduler;
        _factory.ConfigurationOverrides["EventStoreIntegration:Routing:PreflightDedupEnabled"] = "false";
    }

    [Fact]
    public async Task PostIngest_WithValidFilePayload_SchedulesThroughIngestionWorkflowScheduler()
    {
        StubTenantActive();
        IngestionInput request = IngestionInputFactory.Create(
            tenantId: TenantId,
            caseId: CaseId,
            sourceUri: "file:///evidence/story-23-8.txt",
            contentBytes: Encoding.UTF8.GetBytes("workflow config determinism evidence"),
            ingestedBy: "qa@example.com",
            causationId: "cause-23-8",
            correlationId: "corr-23-8");
        _scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(0));
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/ingest",
            request,
            MemoriesJsonContext.Options,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string instanceId = await ReadInstanceIdAsync(response);
        instanceId.ShouldNotBeNullOrWhiteSpace();
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldBe($"/api/v1/ingest/{instanceId}");

        await _scheduler.Received(1).ScheduleAsync(
            instanceId,
            Arg.Is<IngestionInput>(input =>
                input.TenantId == TenantId &&
                input.CaseId == CaseId &&
                input.SourceType == SourceType.File &&
                input.SourceUri == request.SourceUri &&
                input.IngestedBy == request.IngestedBy &&
                input.CausationId == request.CausationId &&
                input.CorrelationId == request.CorrelationId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostIngest_WithTraceparentHeader_SchedulingBoundaryCanCaptureSerializedTraceContext()
    {
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        const string traceState = "vendor=ingest24";
        WorkflowTraceContext? capturedTraceContext = null;
        StubTenantActive();
        IngestionInput request = IngestionInputFactory.Create(
            tenantId: TenantId,
            caseId: CaseId,
            sourceUri: "file:///evidence/story-24-1.txt",
            contentBytes: Encoding.UTF8.GetBytes("workflow trace context evidence"),
            ingestedBy: "qa@example.com");
        _scheduler
            .ScheduleAsync(Arg.Any<string>(), Arg.Any<IngestionInput>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedTraceContext = new WorkflowTraceContextCapture().Capture();
                return callInfo.ArgAt<string>(0);
            });
        using ActivityListener listener = CreateServerTraceListener();
        using HttpClient client = CreateAuthorizedClient();
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, "/api/v1/ingest")
        {
            Content = JsonContent.Create(request, options: MemoriesJsonContext.Options),
        };
        httpRequest.Headers.Add("traceparent", $"00-{traceId}-0000000000000001-01");
        httpRequest.Headers.TryAddWithoutValidation("tracestate", traceState);

        using HttpResponseMessage response = await client.SendAsync(
            httpRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        capturedTraceContext.ShouldNotBeNull();
        capturedTraceContext.TraceParent.ShouldStartWith($"00-{traceId}-");
        capturedTraceContext.TraceState.ShouldBe(traceState);
        await _scheduler.Received(1).ScheduleAsync(
            Arg.Any<string>(),
            Arg.Is<IngestionInput>(input => input.TraceContext == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostIngest_WithInvalidFilePayload_ReturnsBadRequestWithoutScheduling()
    {
        IngestionInput request = IngestionInputFactory.Create(
            tenantId: TenantId,
            caseId: CaseId,
            sourceUri: "file:///evidence/missing-content.txt",
            contentBytes: [],
            ingestedBy: "qa@example.com");
        using HttpClient client = CreateAuthorizedClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/ingest",
            request,
            MemoriesJsonContext.Options,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse error = await ReadJsonAsync<ErrorResponse>(response);
        error.Code.ShouldBe("INVALID_INPUT");
        await _scheduler.DidNotReceive().ScheduleAsync(
            Arg.Any<string>(),
            Arg.Any<IngestionInput>(),
            Arg.Any<CancellationToken>());
    }

    public void Dispose() => _factory.Dispose();

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
            .GetStateAsync<StoredTenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key == $"tenant-registry-{TenantId}"),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);
    }

    private static ActivityListener CreateServerTraceListener()
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name == MemoriesActivitySource.SourceName
                || source.Name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static async Task<string> ReadInstanceIdAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.TryGetProperty("instanceId", out JsonElement instanceId).ShouldBeTrue();
        return instanceId.GetString()!;
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        where T : class
    {
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        T? value = JsonSerializer.Deserialize<T>(body, MemoriesJsonContext.Options);
        value.ShouldNotBeNull();
        return value;
    }
}
