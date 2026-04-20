// <copyright file="AuditLogStreamIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;

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
        // Use the empty-tenantId validation-fail path so EndpointTelemetryScope emits its
        // Warning-level error audit event (7511 LogSearchAccessError). The fixture's TestLogProvider
        // runs at LogLevel.Warning minimum — Information-bank success events (7501) are filtered.
        // Validation-fail vs success doesn't change the schema the test asserts; both go through the
        // same EndpointTelemetryScope wrapper.
        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: "search",
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync(
                    "/api/search?tenantId=&query=stream-probe&axis=syntactic");
                r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            });
    }

    [Fact]
    public async Task IngestOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        // Hit a validation-fail path so we don't have to fully orchestrate Kreuzberg + embeddings.
        // EndpointTelemetryScope still emits one ingest audit event (Tier-2 AuditLogStreamTests proves
        // this at the substrate level; Tier-3 proves it survives the deployed pipeline).
        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: "ingest",
            invokeAsync: async client =>
            {
                IngestionInput input = new()
                {
                    TenantId = string.Empty, // empty → INVALID_INPUT
                    CaseId = "case-stream-probe",
                    SourceUri = "test://stream",
                    ContentType = "text/plain",
                    SourceType = SourceType.Event,
                    IngestedBy = "tests-8-4",
                };

                using HttpResponseMessage r = await client.PostAsJsonAsync(
                    "/api/ingest",
                    input,
                    options: MemoriesJsonContext.Options);
                r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            });
    }

    [Fact]
    public async Task TraverseOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        // Provide a malformed tenant id to exercise INVALID_TENANT_ID — emits one traverse audit event.
        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: "traverse",
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync("/api/tenants/bad~tenant/traverse?startNodeId=s1");
                r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            });
    }

    [Fact]
    public async Task CaseAccessOperation_EmitsOneAuditEvent_WithAC4Schema()
    {
        await AssertOperationEmitsExpectedAuditEventAsync(
            operationType: "case-access",
            invokeAsync: async client =>
            {
                using HttpResponseMessage r = await client.GetAsync("/api/tenants/bad~tenant/cases/c1/memory-units/m1");
                r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            });
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
        int logStartIndex = _fixture.LogEntryCount;

        // Run one of each operation via validation-fail paths so EndpointTelemetryScope emits the
        // Warning-level error audit events (the only ones the fixture's Warning-min log provider sees).
        using HttpResponseMessage searchResp = await _fixture.MemoriesClient.GetAsync(
            "/api/search?tenantId=&query=v-probe&axis=syntactic");
        searchResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using HttpResponseMessage traverseResp = await _fixture.MemoriesClient.GetAsync(
            "/api/tenants/bad~v/traverse?startNodeId=s1");
        traverseResp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource cts = new(timeout);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 2,
            timeout,
            cts.Token);

        try
        {
            events.Count.ShouldBeGreaterThanOrEqualTo(
                2,
                "Expected at least 2 audit events (one search + one traverse). " +
                "If fewer were captured, AC #3 stdout-emission gate is broken at the deployed stack.");

            foreach (CapturedAuditEvent captured in events)
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
            cts.Token);

        try
        {
            // Count-first guard (Risk R1): empty .All(...) is vacuously true.
            events.Count.ShouldBeGreaterThanOrEqualTo(
                1,
                $"Expected at least one Server-side audit event for operationType='{operationType}' within " +
                $"{timeout.TotalSeconds}s; got 0. Override via {AuditEventStreamReader.TimeoutEnvVar} if the runner is slow.");

            IReadOnlyList<CapturedAuditEvent> matching =
                [.. events.Where(e => string.Equals(e.AuditEvent.OperationType, operationType, StringComparison.Ordinal))];
            matching.Count.ShouldBeGreaterThanOrEqualTo(
                1,
                $"Expected at least one event with operationType='{operationType}'. " +
                $"Got operationTypes: {string.Join(",", events.Select(e => e.AuditEvent.OperationType))}.");

            // De-duplication tuple (TraceId, SpanId, OperationType, HttpStatus) — the HttpStatus axis tolerates
            // legitimate retry chains (5xx → 2xx) by treating distinct statuses as distinct emissions; same-attempt
            // duplications still fail. We approximate HttpStatus via Outcome+ErrorCode since the audit event
            // doesn't directly carry httpStatus.
            HashSet<(string, string, string, string)> tuples = [];
            foreach (CapturedAuditEvent c in matching)
            {
                AccessTelemetryEvent ev = c.AuditEvent;
                string statusProxy = $"{ev.Outcome}/{ev.ErrorCode ?? "<none>"}";
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
                ev.TenantId.ShouldNotBeNullOrWhiteSpace();
                ev.OperationType.ShouldBe(operationType);
                ev.TraceId.ShouldNotBeNullOrWhiteSpace();
                ev.SpanId.ShouldNotBeNullOrWhiteSpace();
                ev.DurationMs.ShouldBeGreaterThanOrEqualTo(0);
                ev.Timestamp.ShouldNotBeNullOrWhiteSpace();
                ev.QueryParams.ShouldNotBeNull();
            }
        }
        catch
        {
            DumpDiagnostics(logStartIndex);
            throw;
        }
    }

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
