// <copyright file="AuditLogStreamIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit.Abstractions;

/// <summary>
/// Story 8.4 Task 3 — Tier-3 audit log stream integration tests. Closes Story 7.5 Task 11.4.
/// <para>
/// FR67 authoritative gate: every instrumented operation (search / ingest / traverse / case-access)
/// MUST surface a structured <see cref="AccessTelemetryEvent"/> on the Server container's stdout
/// stream. The reader (<see cref="AuditEventStreamReader"/>) parses Aspire-captured stdout JSON
/// lines and asserts the AC #4 schema (Story 7.5).
/// </para>
/// <para>
/// Health-probe regression guard mirrors the Tier-2 pattern at
/// <c>TelemetryHealthExclusionTests</c> — health endpoints (<c>/health</c>, <c>/alive</c>,
/// <c>/ready</c>) MUST emit zero <see cref="AccessTelemetryEvent"/> entries (AC #5 from Story 7.5,
/// re-asserted here against the deployed stack to catch any regression in the
/// <c>ShouldTraceHttpRequest</c> filter at the cross-process boundary).
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class AuditLogStreamIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private const string OperationSearch = "search";
    private const string OperationIngest = "ingest";
    private const string OperationTraverse = "traverse";
    private const string OperationCaseAccess = "case-access";
    private const string OutcomeOk = "ok";
    private const string OutcomeError = "error";

    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AuditLogStreamIntegrationTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task SearchOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        string tenantId = await EnsureProvisionedTenantAsync();
        string query = $"absent-syntactic-{Guid.NewGuid():N}";

        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: OperationSearch,
            expectedTenantId: tenantId,
            expectedOutcome: OutcomeOk,
            expectedErrorCode: null,
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync(
                    $"/api/search?tenantId={tenantId}&query={query}&axis=syntactic");
                r.StatusCode.ShouldBe(HttpStatusCode.OK);

                SearchResult? result = await r.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
                result.ShouldNotBeNull();
                result.Results.ShouldBeEmpty();
            });
    }

    [Fact]
    public async Task IngestOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        string tenantId = await EnsureProvisionedTenantAsync();
        string caseId = await CreateCaseAsync(tenantId);

        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: OperationIngest,
            expectedTenantId: tenantId,
            expectedOutcome: OutcomeOk,
            expectedErrorCode: null,
            invokeAsync: async client =>
            {
                IngestionInput input = new()
                {
                    TenantId = tenantId,
                    CaseId = caseId,
                    SourceUri = $"test://telemetry/{Guid.NewGuid():N}",
                    ContentBytes = System.Text.Encoding.UTF8.GetBytes($"ingest-probe-{Guid.NewGuid():N}"),
                    ContentType = "text/plain",
                    SourceType = SourceType.File,
                    IngestedBy = "tests-8-4",
                };

                using HttpResponseMessage r = await client.PostAsJsonAsync(
                    "/api/ingest",
                    input,
                    options: MemoriesJsonContext.Options);
                r.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            });
    }

    [Fact]
    public async Task TraverseOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        string tenantId = await EnsureProvisionedTenantAsync();

        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: OperationTraverse,
            expectedTenantId: tenantId,
            expectedOutcome: OutcomeError,
            expectedErrorCode: "MISSING_START_NODE",
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync($"/api/tenants/{tenantId}/traverse?depth=1");
                r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            });
    }

    [Fact]
    public async Task CaseAccessOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        string tenantId = await EnsureProvisionedTenantAsync();

        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: OperationCaseAccess,
            expectedTenantId: tenantId,
            expectedOutcome: OutcomeError,
            expectedErrorCode: "MEMORY_UNIT_NOT_FOUND",
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync(
                    $"/api/tenants/{tenantId}/cases/case-missing/memory-units/memory-unit-missing");
                r.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            });
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task SearchOperation_RetrySequence_EmitsDistinctAuditEventsPerStatus()
    {
        string tenantId = await EnsureProvisionedTenantAsync();
        string startNodeId = $"retry-probe-{Guid.NewGuid():N}";
        int logStartIndex = _fixture.LogEntryCount;

        await _fixture.StopFalkorDbContainerAsync();

        using Activity retryRoot = new Activity("telemetry-retry-sequence-root")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();

        using (Activity firstAttempt = new Activity("telemetry-retry-attempt-1").Start())
        {
            using HttpResponseMessage first = await _fixture.MemoriesClient.GetAsync(
                $"/api/search?tenantId={tenantId}&axis=graph&startNodeId={startNodeId}&depth=1");
            first.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        await _fixture.RestartTopologyAsync();
        await WaitForTenantActiveAsync(tenantId);

        string traceId = retryRoot.TraceId.ToString();

        using Activity secondAttempt = new Activity("telemetry-retry-attempt-2").Start();
        using HttpResponseMessage second = await _fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={tenantId}&axis=graph&startNodeId={startNodeId}&depth=1");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? retryResult = await second.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        retryResult.ShouldNotBeNull();

        TimeSpan timeout = TimeSpan.FromMinutes(4);
        using CancellationTokenSource cts = new(timeout);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 2,
            timeout,
            cts.Token,
            matchPredicate: captured =>
                string.Equals(captured.AuditEvent.OperationType, OperationSearch, StringComparison.Ordinal)
                && string.Equals(captured.AuditEvent.TraceId, traceId, StringComparison.Ordinal));

        IReadOnlyList<CapturedAuditEvent> matching =
            [.. events.Where(captured =>
                string.Equals(captured.AuditEvent.OperationType, OperationSearch, StringComparison.Ordinal)
                && string.Equals(captured.AuditEvent.TraceId, traceId, StringComparison.Ordinal))];

        matching.Count.ShouldBeGreaterThanOrEqualTo(2);
        matching.Count(captured => string.Equals(captured.AuditEvent.Outcome, OutcomeError, StringComparison.Ordinal))
            .ShouldBeGreaterThanOrEqualTo(1);
        matching.Count(captured => string.Equals(captured.AuditEvent.Outcome, OutcomeOk, StringComparison.Ordinal))
            .ShouldBeGreaterThanOrEqualTo(1);

        HashSet<(string, string, string, string)> tuples =
            [.. matching.Select(captured =>
            {
                AccessTelemetryEvent audit = captured.AuditEvent;
                return (
                    audit.TraceId ?? "<null>",
                    audit.SpanId ?? "<null>",
                    audit.OperationType,
                    FormatStatusProxy(audit));
            })];

        tuples.Count.ShouldBe(
            matching.Count,
            "Retry sequence must not be rejected as a duplicate emission when statuses differ across attempts.");
    }

    [Fact]
    public async Task HealthProbes_EmitZeroAuditEvents()
    {
        // Story 7.5 AC #5 + Story 8.4 AC #3 regression guard at the deployed-stack level. Health
        // endpoints (/health, /alive, /ready) are NOT in the four enumerated operation types, so
        // no EndpointTelemetryScope runs and no audit event is emitted on the deployed stack.
        int logStartIndex = _fixture.LogEntryCount;

        for (int i = 0; i < 5; i++)
        {
            using HttpResponseMessage health = await _fixture.MemoriesClient.GetAsync("/health");
            health.IsSuccessStatusCode.ShouldBeTrue();
            using HttpResponseMessage alive = await _fixture.MemoriesClient.GetAsync("/alive");
            alive.IsSuccessStatusCode.ShouldBeTrue();
            using HttpResponseMessage ready = await _fixture.MemoriesClient.GetAsync("/ready");
            ready.IsSuccessStatusCode.ShouldBeTrue();
        }

        // Use a short polling window for the negative assertion — we don't want to burn the full
        // 10s default for a test that asserts emptiness. 3s is enough for any spurious audit event
        // to surface; if zero arrive in 3s we trust zero is the steady state.
        TimeSpan negativeWindow = TimeSpan.FromSeconds(3);
        using CancellationTokenSource cts = new(negativeWindow);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 1,
            negativeWindow,
            cts.Token);

        events.Count.ShouldBe(
            0,
            "Health probes (/health, /alive, /ready) MUST emit zero AccessTelemetryEvent entries " +
            "(AC #5 regression guard from Story 7.5). Captured events: " +
            string.Join(", ", events.Select(e => $"{e.EventId}/{e.AuditEvent.OperationType}")));
    }

    [Fact]
    public async Task SchemaVersion_IsOneForAllEmittedEvents()
    {
        // Future-proofing: aggregate across one of each operation type and assert every captured event
        // carries schemaVersion == 1. A breaking field change that bumped the version would fail loudly.
        string tenantId = await EnsureProvisionedTenantAsync();
        int logStartIndex = _fixture.LogEntryCount;

        // Mix a successful search with a valid-tenant traverse validation error so both Information and
        // Warning audit events are covered on the deployed stack.
        using HttpResponseMessage searchResp = await _fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={tenantId}&query=schema-probe-{Guid.NewGuid():N}&axis=syntactic");
        searchResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using HttpResponseMessage traverseResp = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/traverse?depth=1");
        traverseResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource cts = new(timeout);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 2,
            timeout,
            cts.Token,
            matchPredicate: captured =>
                string.Equals(captured.AuditEvent.TenantId, tenantId, StringComparison.Ordinal)
                && (string.Equals(captured.AuditEvent.OperationType, OperationSearch, StringComparison.Ordinal)
                    || string.Equals(captured.AuditEvent.OperationType, OperationTraverse, StringComparison.Ordinal)));

        try
        {
            events.Count.ShouldBeGreaterThanOrEqualTo(
                2,
                "Expected at least 2 audit events (one search + one traverse). " +
                "If fewer were captured, AC #3 stdout-emission gate is broken at the deployed stack.");

            foreach (CapturedAuditEvent captured in events.Where(captured => string.Equals(captured.AuditEvent.TenantId, tenantId, StringComparison.Ordinal)))
            {
                captured.AuditEvent.SchemaVersion.ShouldBe(
                    AccessTelemetryEvent.CurrentSchemaVersion,
                    $"Audit event for operationType={captured.AuditEvent.OperationType} carries " +
                    $"schemaVersion={captured.AuditEvent.SchemaVersion}; expected " +
                    $"{AccessTelemetryEvent.CurrentSchemaVersion}. A breaking schema change must bump the version.");
            }
        }
        catch
        {
            DumpDiagnostics(logStartIndex);
            throw;
        }
    }

    private async Task AssertOperationEmitsExpectedAuditEventAsync(
        string operationType,
        string expectedTenantId,
        string expectedOutcome,
        string? expectedErrorCode,
        Func<HttpClient, Task> invokeAsync)
    {
        int logStartIndex = _fixture.LogEntryCount;

        await invokeAsync(_fixture.MemoriesClient);

        TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource cts = new(timeout);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 1,
            timeout,
            cts.Token,
            matchPredicate: captured =>
                string.Equals(captured.AuditEvent.OperationType, operationType, StringComparison.Ordinal)
                && string.Equals(captured.AuditEvent.TenantId, expectedTenantId, StringComparison.Ordinal));

        try
        {
            // Count-first guard (Risk R1): empty .All(...) is vacuously true.
            events.Count.ShouldBeGreaterThanOrEqualTo(
                1,
                $"Expected at least one Server-side audit event for operationType='{operationType}' within " +
                $"{timeout.TotalSeconds}s; got 0. Override via {AuditEventStreamReader.TimeoutEnvVar} if the runner is slow.");

            IReadOnlyList<CapturedAuditEvent> matching =
                [.. events.Where(e =>
                    string.Equals(e.AuditEvent.OperationType, operationType, StringComparison.Ordinal)
                    && string.Equals(e.AuditEvent.TenantId, expectedTenantId, StringComparison.Ordinal))];
            matching.Count.ShouldBeGreaterThanOrEqualTo(
                1,
                $"Expected at least one event with operationType='{operationType}' and tenantId='{expectedTenantId}'. " +
                $"Got operationTypes: {string.Join(",", events.Select(e => e.AuditEvent.OperationType))}.");

            // De-duplication tuple (TraceId, SpanId, OperationType, HttpStatus) — the HttpStatus axis tolerates
            // legitimate retry chains (5xx → 2xx) by treating distinct statuses as distinct emissions; same-attempt
            // duplications still fail. We approximate HttpStatus via Outcome+ErrorCode since the audit event
            // doesn't directly carry httpStatus.
            HashSet<(string, string, string, string)> tuples = [];
            foreach (CapturedAuditEvent c in matching)
            {
                AccessTelemetryEvent ev = c.AuditEvent;
                string statusProxy = FormatStatusProxy(ev);
                tuples.Add((ev.TraceId ?? "<null>", ev.SpanId ?? "<null>", ev.OperationType, statusProxy));
            }

            tuples.Count.ShouldBe(
                matching.Count,
                $"Duplicate audit events detected for operationType='{operationType}' on the same trace+span+status. " +
                "AC #3 de-duplication guard violated.");

            // AC #4 schema (Story 7.5): every matching event has the required fields populated.
            foreach (CapturedAuditEvent c in matching)
            {
                AccessTelemetryEvent ev = c.AuditEvent;
                ev.SchemaVersion.ShouldBe(AccessTelemetryEvent.CurrentSchemaVersion);
                ev.EventId.ShouldBeInRange(AuditEventStreamReader.MinEventId, AuditEventStreamReader.MaxEventId);
                ev.TenantId.ShouldBe(expectedTenantId);
                ev.OperationType.ShouldBe(operationType);
                ev.TraceId.ShouldNotBeNullOrWhiteSpace();
                ev.SpanId.ShouldNotBeNullOrWhiteSpace();
                ev.DurationMs.ShouldBeGreaterThanOrEqualTo(0);
                ev.Timestamp.ShouldNotBeNullOrWhiteSpace();
                ev.QueryParams.ShouldNotBeNull();
                ev.Outcome.ShouldBe(expectedOutcome);
                ev.ErrorCode.ShouldBe(expectedErrorCode);
            }
        }
        catch
        {
            DumpDiagnostics(logStartIndex);
            throw;
        }
    }

    private async Task<string> EnsureProvisionedTenantAsync()
    {
        string tenantId = $"tenant-telemetry-audit-{Guid.NewGuid():N}";
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");
        return tenantId;
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/tenants",
            new TenantProvisioningInput(tenantId, displayName),
            MemoriesJsonContext.Options);
        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForTenantActiveAsync(tenantId);
    }

    private async Task WaitForTenantActiveAsync(string tenantId)
    {

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActivationTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient.GetAsync($"/api/tenants/{tenantId}");
            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options);
                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not reach Active state within {ActivationTimeout}.");
    }

    private async Task<string> CreateCaseAsync(string tenantId)
    {
        using var http = new HttpClient
        {
            BaseAddress = _fixture.MemoriesClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions
        {
            Endpoint = _fixture.MemoriesClient.BaseAddress,
        });
        var client = new MemoriesClient(http, options, NullLogger<MemoriesClient>.Instance);

#pragma warning disable HXL001
        Hexalith.Memories.Contracts.V1.Case created = await client.CreateCaseAsync(tenantId, $"case-{Guid.NewGuid():N}", null, CancellationToken.None);
#pragma warning restore HXL001
        return created.Id;
    }

    private static string FormatStatusProxy(AccessTelemetryEvent auditEvent)
        => $"{auditEvent.Outcome}/{auditEvent.ErrorCode ?? "<none>"}";

    private void DumpDiagnostics(int logStartIndex)
    {
        _output.WriteLine($"--- Story 8.4 AuditLogStreamIntegrationTests diagnostics ---");
        _output.WriteLine($"--- Last server stdout lines (max 50) ---");
        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in AuditEventStreamReader.TailRawLogs(_fixture, logStartIndex, maxLines: 50))
        {
            _output.WriteLine($"[{entry.Level}] {entry.Category}: {entry.Message}");
        }
    }
}
