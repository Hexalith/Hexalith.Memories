// <copyright file="AspireEndToEndTraceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Shouldly;

using Xunit.Abstractions;

/// <summary>
/// Story 8.4 Task 2 — Tier-3 Aspire end-to-end trace propagation tests. Closes Story 7.5 Task 11.3.
/// <para>
/// Hybrid capture model (Story 8.4 Change Log Rev 0.6): the Memories Server runs as a separate
/// process under Aspire orchestration, so the in-test-process tracer cannot directly capture
/// Server-side activities. CLI-side spans (CLI root + HttpClient) are captured via
/// <see cref="CliTracingHarness"/> in this process; Server-side activity evidence is proxied via
/// the audit-event JSON line (which carries Activity.Current TraceId/SpanId from the Server's
/// process). Both invariants — AC #1 (single TraceId end-to-end via W3C traceparent) and AC #4
/// (audit ↔ activity cross-reference) — are still proven; the audit log line is the Server-side
/// evidence.
/// </para>
/// <para>
/// AC #2 (optional Redis OTEL instrumentation) is informational-only in cross-process mode (the
/// in-test-process tracer cannot reflect on the Server's TracerProvider). The accompanying
/// <c>Ac2SkipReviewByTests</c> unit class enforces the self-expiring review-by date so the
/// informational status cannot silently persist forever (Task 7).
/// </para>
/// <para>
/// Lane: <c>[Trait("Category", "Integration")]</c>. Excluded from per-PR runs via
/// <c>--filter Category!=Integration</c>; runs on the Docker-provisioned merge-queue lane (ADR-8.4-001).
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class AspireEndToEndTraceTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AspireEndToEndTraceTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task CliSearch_EndToEnd_SingleTraceIdAcrossAllHops()
    {
        // AC #1 (NFR28 HTTP-hop gate): the W3C `traceparent` header must propagate from the CLI's root
        // activity → outbound HttpClient span → Server's AspNetCore inbound span → memories.search activity.
        // Cross-process model: CLI-side spans captured in-test-process; Server-side spans proxied via the
        // audit log's TraceId/SpanId fields (set from Activity.Current at audit-emission time on the Server).
        // We use the empty-tenantId validation-fail path because (a) it does not require pre-provisioning a
        // tenant in the registry, (b) EndpointTelemetryScope wraps the validation branch so memories.search
        // activity + Warning-level error audit event (7511 LogSearchAccessError) still emit with the
        // propagated TraceId, and (c) the fixture's _logProvider is configured at LogLevel.Warning minimum,
        // so the error-bank audit event is captured while the Information-bank success event would be filtered.
        await using CliTracingHarness harness = CliTracingHarness.Create();
        int logStartIndex = _fixture.LogEntryCount;
        string expectedTraceId;
        string httpSpanId;

        using (Activity? cliRoot = harness.StartCliRootActivity("search"))
        {
            cliRoot.ShouldNotBeNull("CLI tracing harness failed to start a root activity.");
            expectedTraceId = cliRoot.TraceId.ToString();

            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                "/api/search?tenantId=&query=story-8-4-trace-probe&axis=syntactic");
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            httpSpanId = string.Empty; // resolved below from collector
        }

        await harness.ForceFlushAsync(TimeSpan.FromSeconds(2));

        // Count-first guard (Risk R1, Dev Notes "count-first, content-second" convention): empty
        // collections satisfy LINQ .All(...) vacuously, so we MUST check Count BEFORE any predicate
        // that could pass on empty.
        IReadOnlyList<Activity> capturedSpans = harness.Collector.Snapshot();
        try
        {
            capturedSpans.Count.ShouldBeGreaterThanOrEqualTo(
                2,
                "Expected at least one CLI root span and one HttpClient span; got fewer. Capture wiring broken?");

            Activity cliRootSpan = capturedSpans.SingleOrDefault(a =>
                string.Equals(a.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal))
                ?? throw new ShouldAssertException(
                    $"Expected a single CLI root span ({MemoriesActivitySource.CliInvoke}); see span dump for what was captured.");

            Activity httpClientSpan = capturedSpans.SingleOrDefault(a => a.Source.Name == "System.Net.Http")
                ?? throw new ShouldAssertException(
                    "Expected exactly one outbound HttpClient span (System.Net.Http source). " +
                    "If HttpClient instrumentation drift adds a wrapper span, this lookup may need to broaden.");

            httpSpanId = httpClientSpan.SpanId.ToString();

            // AC #1 invariant 1: shared TraceId across the in-process spans.
            cliRootSpan.TraceId.ShouldBe(httpClientSpan.TraceId);
            cliRootSpan.TraceId.ToString().ShouldBe(expectedTraceId);

            // AC #1 invariant 2: ancestor-descendant reachability via Activity.Parent traversal.
            // Cycle detection (visited-set) + max-depth ceiling = 16 + root-termination at CLI root.
            AssertParentChainReachesCliRoot(httpClientSpan, expectedRootSpan: cliRootSpan);

            // AC #1 invariant 3: Server-side spans reached by trace-id propagation. Proxied via the
            // audit log: if the Server received the W3C traceparent, the audit event will carry the
            // same TraceId. This is the cross-process bridge per ADR-8.4-003 + Change Log Rev 0.6.
            CapturedAuditEvent serverAudit = await ReadServerSearchAuditEventAsync(logStartIndex, expectedTraceId);
            serverAudit.AuditEvent.TraceId.ShouldBe(
                expectedTraceId,
                "Server's audit-event TraceId must equal the CLI root TraceId — proves W3C traceparent " +
                "propagated across the HTTP boundary (NFR28 HTTP-hop gate).");
            serverAudit.AuditEvent.SpanId.ShouldNotBeNullOrWhiteSpace(
                "Server's audit-event SpanId must be populated (recorded from Activity.Current on the Server side).");
            serverAudit.AuditEvent.SpanId!.Length.ShouldBe(
                16,
                "Server's audit-event SpanId must be a well-formed W3C 16-hex-char span id.");
        }
        catch
        {
            DumpDiagnostics(harness, logStartIndex, expectedTraceId, httpSpanId);
            throw;
        }
    }

    [Fact]
    public async Task CliSearch_AuditEvent_TraceIdMatchesSpan()
    {
        // AC #4: cross-reference between activity and audit event. Ships using the CLI-side captured
        // spans + the Server-side audit log; the Server's audit event carries TraceId/SpanId from
        // Activity.Current on the Server, so AC #4's "auditEvent.TraceId == searchActivity.TraceId" is
        // proven by audit event TraceId == CLI root TraceId (W3C traceparent transit).
        await using CliTracingHarness harness = CliTracingHarness.Create();
        int logStartIndex = _fixture.LogEntryCount;
        string expectedTraceId;

        using (Activity? cliRoot = harness.StartCliRootActivity("search"))
        {
            cliRoot.ShouldNotBeNull();
            expectedTraceId = cliRoot.TraceId.ToString();
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                "/api/search?tenantId=&query=story-8-4-cross-ref-probe&axis=syntactic");
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        await harness.ForceFlushAsync(TimeSpan.FromSeconds(2));

        try
        {
            CapturedAuditEvent serverAudit = await ReadServerSearchAuditEventAsync(logStartIndex, expectedTraceId);
            serverAudit.AuditEvent.TraceId.ShouldBe(expectedTraceId);
            serverAudit.AuditEvent.SpanId.ShouldNotBeNullOrWhiteSpace();
            serverAudit.AuditEvent.SpanId!.Length.ShouldBe(16);
            serverAudit.AuditEvent.SchemaVersion.ShouldBe(AccessTelemetryEvent.CurrentSchemaVersion);
            serverAudit.AuditEvent.OperationType.ShouldBe("search");
        }
        catch
        {
            DumpDiagnostics(harness, logStartIndex, expectedTraceId, spanId: string.Empty);
            throw;
        }
    }

    private async Task<CapturedAuditEvent> ReadServerSearchAuditEventAsync(int logStartIndex, string expectedTraceId)
    {
        TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource cts = new(timeout);
        IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 1,
            timeout,
            cts.Token);

        IReadOnlyList<CapturedAuditEvent> matching =
            [.. events.Where(e =>
                string.Equals(e.AuditEvent.OperationType, "search", StringComparison.Ordinal)
                && string.Equals(e.AuditEvent.TraceId, expectedTraceId, StringComparison.Ordinal))];

        matching.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            $"Expected at least one Server-side 'search' audit event with TraceId={expectedTraceId} " +
            $"within {timeout.TotalSeconds}s; got {matching.Count} matching of {events.Count} candidate audit events. " +
            $"Override the timeout via {AuditEventStreamReader.TimeoutEnvVar}=<seconds> if the merge-queue runner is slow.");

        return matching[0];
    }

    private static void AssertParentChainReachesCliRoot(Activity start, Activity expectedRootSpan)
    {
        // AC #1 traversal rules (Story Rev 0.5):
        //   (i)   cycle detection — visited set fails any chain that revisits a span id
        //   (ii)  max-depth ceiling = 16 — chains longer than 16 hops fail with a diagnostic
        //   (iii) root-termination — chain MUST terminate at the CLI root span (null-parent + name == memories.cli.invoke)
        const int MaxDepth = 16;
        HashSet<string> visited = [];
        Activity? current = start;
        int depth = 0;
        while (current is not null)
        {
            string spanId = current.SpanId.ToString();
            if (!visited.Add(spanId))
            {
                throw new ShouldAssertException(
                    $"Parent-chain cycle detected at span {spanId} (operation={current.OperationName}). " +
                    "AC #1 invariant violated.");
            }

            if (++depth > MaxDepth)
            {
                throw new ShouldAssertException(
                    $"Parent-chain depth exceeds {MaxDepth} hops starting at {start.OperationName} " +
                    "({start.SpanId}); no legitimate in-process trace exceeds this. AC #1 invariant violated.");
            }

            if (ReferenceEquals(current, expectedRootSpan))
            {
                return; // reached CLI root via reachability — pass.
            }

            Activity? parent = current.Parent;
            if (parent is null)
            {
                if (string.Equals(current.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal))
                {
                    return;
                }

                throw new ShouldAssertException(
                    $"Orphan span detected: {current.OperationName} has no parent and is not the CLI root span. " +
                    "AC #1 root-termination rule violated.");
            }

            current = parent;
        }
    }

    private void DumpDiagnostics(CliTracingHarness harness, int logStartIndex, string expectedTraceId, string spanId)
    {
        _output.WriteLine($"--- Story 8.4 AspireEndToEndTraceTests diagnostics ---");
        _output.WriteLine($"expectedTraceId={expectedTraceId}");
        _output.WriteLine($"httpClientSpanId={spanId}");
        _output.WriteLine(harness.Collector.FormatSpanTree());
        _output.WriteLine($"--- Last server stdout lines (max 50) ---");
        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in AuditEventStreamReader.TailRawLogs(_fixture, logStartIndex, maxLines: 50))
        {
            _output.WriteLine($"[{entry.Level}] {entry.Category}: {entry.Message}");
        }
    }
}
