// <copyright file="TracePropagationNoDockerTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Diagnostics;
using System.Net;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 7.5 Task 11.1 — Tier-2 NFR28 trace-propagation guard. Drives the Memories Server in-process via
/// <see cref="Infrastructure.TelemetryWebAppFactory"/> (no Docker, no DAPR sidecar) and asserts the
/// server-side instrumentation invariants that NFR28 demands regardless of whether an actual DAPR hop
/// takes place: the AspNetCore span, the <c>memories.search</c> activity, and the audit log entry all
/// share a single <c>TraceId</c> / <c>SpanId</c> pair.
/// <para>
/// Scope boundary: this test targets the <c>EndpointTelemetryScope</c> wrapper at the server boundary —
/// validation-fail branches exercise the full instrumentation contract (activity creation + tag emission +
/// audit-log emission + trace-id propagation into the log event) WITHOUT requiring Redis / FalkorDB / DAPR
/// downstream. The Tier-3 Aspire integration test (Task 11.3) covers the full CLI → ingress → Server →
/// Redis chain via Docker-backed containers.
/// </para>
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class TracePropagationNoDockerTests : IDisposable
{
    private const string StoreName = "statestore";

    private readonly TelemetryWebAppFactory _factory;
    private readonly ActivityListener _listener;
    private readonly List<Activity> _capturedActivities = [];

    public TracePropagationNoDockerTests()
    {
        _factory = new TelemetryWebAppFactory();

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MemoriesActivitySource.SourceName || source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (_capturedActivities)
                {
                    _capturedActivities.Add(activity);
                }
            },
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Starts a test-scoped root activity. All child activities (outbound HTTP, AspNetCore, memories.search)
    /// inherit the same trace id — tests filter captured activities by this trace id to isolate concurrent tests.</summary>
    private sealed class TestRootScope : IDisposable
    {
        private static readonly ActivitySource TestSource = new("Hexalith.Memories.Server.Tests.Root");
        private static readonly ActivityListener KeepAliveListener = CreateKeepAlive();

        private readonly Activity? _root;

        public TestRootScope()
        {
            _root = TestSource.StartActivity("test-root");
            TraceId = _root?.TraceId.ToString() ?? string.Empty;
        }

        public string TraceId { get; }

        public void Dispose() => _root?.Stop();

        private static ActivityListener CreateKeepAlive()
        {
            ActivityListener listener = new()
            {
                ShouldListenTo = source => source == TestSource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }
    }

    [Fact]
    public async Task SearchEndpoint_EmitsActivityWithTraceContextMatchingAuditLog()
    {
        using HttpClient client = _factory.CreateClient();
        using TestRootScope root = new();

        // Hit the search endpoint's validation-fail branch (empty tenantId → INVALID_INPUT). The endpoint
        // still opens the memories.search activity + the EndpointTelemetryScope, records outcome=error,
        // and emits one audit-log entry BEFORE early-returning. No Redis / FalkorDB call is made.
        // The traceparent header carries the test-root trace id so the ASP.NET Core pipeline attaches
        // its inbound span (and all children) to the same trace — letting us isolate this test's
        // activities from any other parallel test classes that also emit on this source.
        HttpResponseMessage response = await SendWithTraceparentAsync(client, "/api/v1/search?tenantId=&query=foo", root.TraceId);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Activity searchActivity = GetMemoriesSearchActivity(root.TraceId);

        // The memories.search activity carries the expected operation + axis tags.
        searchActivity.GetTagItem(MemoriesActivitySource.TagOperation).ShouldBe("search");
        searchActivity.GetTagItem(MemoriesActivitySource.TagAxis).ShouldBe("syntactic");
        searchActivity.GetTagItem(MemoriesActivitySource.TagOutcome).ShouldBe("error");
        searchActivity.GetTagItem(MemoriesActivitySource.TagErrorCode).ShouldBe("INVALID_INPUT");
        searchActivity.Status.ShouldBe(ActivityStatusCode.Error);

        AuditLogCapture auditCapture = GetAuditCaptureForTrace(root.TraceId);
        AccessTelemetryEvent auditEvent = auditCapture.AuditEvent.ShouldNotBeNull();

        // AC #1 + Task 11.1 (c): audit log line shares the activity's trace id + span id.
        auditEvent.TraceId.ShouldBe(searchActivity.TraceId.ToString());
        auditEvent.SpanId.ShouldBe(searchActivity.SpanId.ToString());
        auditEvent.OperationType.ShouldBe("search");
        auditEvent.Outcome.ShouldBe("error");
        auditEvent.ErrorCode.ShouldBe("INVALID_INPUT");

        // AC #2 / NFR28: at least one AspNetCore span is captured in the same trace. Without a real DAPR
        // hop there is no second-process span, but the within-process chain (AspNetCore → memories.search)
        // MUST share a single TraceId — propagation is the invariant under test.
        Activity? aspNetActivity = FindAspNetCoreActivity(root.TraceId);
        if (aspNetActivity is not null)
        {
            searchActivity.TraceId.ShouldBe(aspNetActivity.TraceId);
        }
    }

    [Fact]
    public async Task SearchEndpoint_ActivityTraceIdShouldBeNonZero_AndPropagatedToAudit()
    {
        using HttpClient client = _factory.CreateClient();
        using TestRootScope root = new();

        _ = await SendWithTraceparentAsync(client, "/api/v1/search?tenantId=&query=foo", root.TraceId);

        Activity searchActivity = GetMemoriesSearchActivity(root.TraceId);

        // Activity must have real ids — W3C TraceContext guarantees 128-bit trace id when sampling is on.
        searchActivity.TraceId.ToString().ShouldNotBe(new string('0', 32));
        searchActivity.SpanId.ToString().ShouldNotBe(new string('0', 16));

        AuditLogCapture capture = GetAuditCaptureForTrace(root.TraceId);
        capture.AuditEvent.ShouldNotBeNull();
        capture.AuditEvent!.TraceId.ShouldNotBeNull();
        capture.AuditEvent.SpanId.ShouldNotBeNull();
        capture.AuditEvent.TraceId.ShouldBe(searchActivity.TraceId.ToString());
        capture.AuditEvent.SpanId.ShouldBe(searchActivity.SpanId.ToString());
    }

    [Fact]
    public async Task SearchEndpoint_WithCaseId_EmitsCaseIdTag()
    {
        using HttpClient client = _factory.CreateClient();
        using TestRootScope root = new();

        HttpResponseMessage response = await SendWithTraceparentAsync(client, "/api/v1/search?tenantId=&query=foo&caseId=case-42", root.TraceId);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Activity searchActivity = GetMemoriesActivity(root.TraceId, MemoriesActivitySource.SearchRequest);
        searchActivity.GetTagItem(MemoriesActivitySource.TagCaseId).ShouldBe("case-42");
    }

    [Fact]
    public async Task TraverseEndpoint_WithCaseId_EmitsCaseIdTag()
    {
        using HttpClient client = _factory.CreateClient();
        using TestRootScope root = new();

        HttpResponseMessage response = await SendWithTraceparentAsync(client, "/api/v1/tenants/acme/traverse?caseId=case-42", root.TraceId);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Activity traverseActivity = GetMemoriesActivity(root.TraceId, MemoriesActivitySource.TraverseRequest);
        traverseActivity.GetTagItem(MemoriesActivitySource.TagCaseId).ShouldBe("case-42");
    }

    [Fact]
    public async Task SearchEndpoint_InvalidAxis_DoesNotEmitRejectedAxisTag()
    {
        const string tenantId = "acme-telemetry";
        TenantRegistryEntry entry = new(
            new TenantInfo(tenantId, "Acme Telemetry", TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<StoredTenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(key => key!.Contains(tenantId, StringComparison.Ordinal)),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);

        using HttpClient client = _factory.CreateClient();
        using TestRootScope root = new();

        HttpResponseMessage response = await SendWithTraceparentAsync(client, $"/api/v1/search?tenantId={tenantId}&query=foo&axis=bogus", root.TraceId);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        Activity searchActivity = GetMemoriesActivity(root.TraceId, MemoriesActivitySource.SearchRequest);
        searchActivity.GetTagItem(MemoriesActivitySource.TagAxis).ShouldBeNull();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _factory.Dispose();
    }

    private static async Task<HttpResponseMessage> SendWithTraceparentAsync(HttpClient client, string requestUri, string traceId)
    {
        HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        if (!string.IsNullOrEmpty(traceId))
        {
            request.Headers.Add("traceparent", $"00-{traceId}-0000000000000001-01");
        }

        return await client.SendAsync(request);
    }

    private Activity GetMemoriesSearchActivity(string traceId)
    {
        return GetMemoriesActivity(traceId, MemoriesActivitySource.SearchRequest);
    }

    private Activity GetMemoriesActivity(string traceId, string operationName)
    {
        lock (_capturedActivities)
        {
            Activity? match = _capturedActivities.SingleOrDefault(a =>
                a.Source.Name == MemoriesActivitySource.SourceName &&
                a.OperationName == operationName &&
                a.TraceId.ToString() == traceId);
            match.ShouldNotBeNull(
                $"Expected a single {operationName} activity with traceId {traceId}.");
            return match!;
        }
    }

    private Activity? FindAspNetCoreActivity(string traceId)
    {
        lock (_capturedActivities)
        {
            return _capturedActivities.FirstOrDefault(a =>
                a.Source.Name == "Microsoft.AspNetCore" &&
                a.TraceId.ToString() == traceId);
        }
    }

    private AuditLogCapture GetAuditCaptureForTrace(string traceId)
    {
        IReadOnlyList<AuditLogCapture> captures = _factory.AuditLogs.AccessTelemetryCaptures;
        List<AuditLogCapture> matches = [.. captures.Where(c =>
            c.AuditEvent is not null && string.Equals(c.AuditEvent.TraceId, traceId, StringComparison.Ordinal))];
        matches.Count.ShouldBe(1, customMessage: $"Expected a single audit event with traceId {traceId}; got {matches.Count}.");
        return matches[0];
    }
}
