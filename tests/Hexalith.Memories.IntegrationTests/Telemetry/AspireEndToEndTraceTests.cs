// <copyright file="AspireEndToEndTraceTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Telemetry;

using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Microsoft.Extensions.DependencyInjection;

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
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private const string SearchOperationType = "search";

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
        // AC #1 (NFR28 HTTP-hop gate): invoke the real CLI `memories search query ...` path in-process via
        // the DI-rooted System.CommandLine tree, then prove the combined CLI + server span chain:
        //   memories.cli.invoke → System.Net.Http → AspNetCore server span → memories.search.
        //
        // The CLI spans are captured in-process by CliTracingHarness. The server-side AspNetCore +
        // memories.search spans are emitted as test-only activity breadcrumbs to the server's stderr under
        // HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1 and parsed from the Aspire log stream.
        string tenantId = $"tenant-telemetry-trace-{Guid.NewGuid():N}";
        string query = $"trace-probe-{Guid.NewGuid():N}";
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        await using CliTracingHarness harness = CliTracingHarness.Create();
        int logStartIndex = _fixture.LogEntryCount;
        string httpSpanId = string.Empty;
        string cliStdout = string.Empty;
        string cliStderr = string.Empty;

        CliInvocationResult invocation = await InvokeCliSearchAsync(tenantId, query);
        cliStdout = invocation.Stdout;
        cliStderr = invocation.Stderr;
        invocation.ExitCode.ShouldBe(
            CliExitCodes.Success,
            $"CLI search command failed. stdout:{Environment.NewLine}{cliStdout}{Environment.NewLine}stderr:{Environment.NewLine}{cliStderr}");
        cliStderr.ShouldBeEmpty($"CLI stderr must stay empty on the happy path. stdout:{Environment.NewLine}{cliStdout}");
        cliStdout.ShouldNotBeNullOrWhiteSpace();

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
            string expectedTraceId = cliRootSpan.TraceId.ToString();

            TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
            using CancellationTokenSource cts = new(timeout);
            IReadOnlyList<CapturedServerActivity> serverActivities = await ServerActivityStreamReader.ReadAsync(
                _fixture,
                logStartIndex,
                minimumEvents: 2,
                timeout,
                cts.Token,
                matchPredicate: activity =>
                    string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal)
                    && (IsAspNetCoreServerSpan(activity) || IsSearchActivity(activity)));

            IReadOnlyList<CapturedServerActivity> matchingServerActivities =
                [.. serverActivities.Where(activity =>
                    string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal)
                    && (IsAspNetCoreServerSpan(activity) || IsSearchActivity(activity)))];

            matchingServerActivities.Count.ShouldBeGreaterThanOrEqualTo(
                2,
                $"Expected at least one AspNetCore server span and one memories.search span for trace {expectedTraceId}. " +
                "If fewer were captured, the server-side activity breadcrumb path is broken.");

            CapturedServerActivity aspNetCoreSpan = matchingServerActivities.SingleOrDefault(IsAspNetCoreServerSpan)
                ?? throw new ShouldAssertException(
                    $"Expected one AspNetCore server span for trace {expectedTraceId}; got " +
                    $"{matchingServerActivities.Count(a => IsAspNetCoreServerSpan(a))}.");

            CapturedServerActivity searchSpan = matchingServerActivities.SingleOrDefault(IsSearchActivity)
                ?? throw new ShouldAssertException(
                    $"Expected one {MemoriesActivitySource.SearchRequest} activity for trace {expectedTraceId}; got " +
                    $"{matchingServerActivities.Count(a => IsSearchActivity(a))}.");

            List<TraceNode> relevantNodes =
            [
                ToTraceNode(cliRootSpan),
                ToTraceNode(httpClientSpan),
                ToTraceNode(aspNetCoreSpan),
                ToTraceNode(searchSpan),
            ];

            relevantNodes.Count.ShouldBeGreaterThanOrEqualTo(
                4,
                "Expected the four required chain nodes (CLI root, HttpClient, AspNetCore, memories.search). " +
                "A missing node means the end-to-end span-chain proof is incomplete.");

            // AC #1 invariant 1: shared TraceId across the in-process spans.
            cliRootSpan.TraceId.ShouldBe(httpClientSpan.TraceId);
            cliRootSpan.TraceId.ToString().ShouldBe(expectedTraceId);
            relevantNodes.ShouldAllBe(node => node.TraceId == expectedTraceId);

            // AC #1 invariant 2: ancestor-descendant reachability across the full chain, not just the
            // in-process CLI subset. Parent ids are reconstructed from the CLI activities and the
            // server-side activity breadcrumbs.
            IReadOnlyDictionary<string, TraceNode> nodesBySpanId = relevantNodes.ToDictionary(node => node.SpanId, StringComparer.Ordinal);
            TraceNode cliRootNode = relevantNodes.Single(node => node.OperationName == MemoriesActivitySource.CliInvoke);
            AssertParentChainReachesCliRoot(relevantNodes.Single(node => node.SourceName == "System.Net.Http"), cliRootNode, nodesBySpanId);
            AssertParentChainReachesCliRoot(relevantNodes.Single(node => IsAspNetCoreServerSpan(node)), cliRootNode, nodesBySpanId);
            AssertParentChainReachesCliRoot(relevantNodes.Single(node => node.OperationName == MemoriesActivitySource.SearchRequest), cliRootNode, nodesBySpanId);
        }
        catch
        {
            DumpDiagnostics(harness, logStartIndex, httpSpanId, cliStdout, cliStderr);
            throw;
        }
    }

    [Fact]
    public async Task CliSearch_AuditEvent_TraceIdMatchesSpan()
    {
        // AC #4: cross-reference the actual captured server-side memories.search span against the audit
        // event emitted for that same request.
        string tenantId = $"tenant-telemetry-cross-ref-{Guid.NewGuid():N}";
        string query = $"cross-ref-probe-{Guid.NewGuid():N}";
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        await using CliTracingHarness harness = CliTracingHarness.Create();
        int logStartIndex = _fixture.LogEntryCount;
        string cliStdout = string.Empty;
        string cliStderr = string.Empty;

        CliInvocationResult invocation = await InvokeCliSearchAsync(tenantId, query);
        cliStdout = invocation.Stdout;
        cliStderr = invocation.Stderr;
        invocation.ExitCode.ShouldBe(
            CliExitCodes.Success,
            $"CLI search command failed. stdout:{Environment.NewLine}{cliStdout}{Environment.NewLine}stderr:{Environment.NewLine}{cliStderr}");
        cliStderr.ShouldBeEmpty();

        await harness.ForceFlushAsync(TimeSpan.FromSeconds(2));

        try
        {
            Activity cliRootSpan = harness.Collector.Snapshot().SingleOrDefault(a =>
                string.Equals(a.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal))
                ?? throw new ShouldAssertException("Expected a CLI root span but captured none.");
            string expectedTraceId = cliRootSpan.TraceId.ToString();

            TimeSpan timeout = AuditEventStreamReader.ResolveTimeout();
            using CancellationTokenSource cts = new(timeout);
            IReadOnlyList<CapturedServerActivity> serverActivities = await ServerActivityStreamReader.ReadAsync(
                _fixture,
                logStartIndex,
                minimumEvents: 1,
                timeout,
                cts.Token,
                matchPredicate: activity =>
                    string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal)
                    && IsSearchActivity(activity));

            CapturedServerActivity searchActivity = serverActivities.SingleOrDefault(activity =>
                    string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal)
                    && IsSearchActivity(activity))
                ?? throw new ShouldAssertException(
                    $"Expected one captured server-side {MemoriesActivitySource.SearchRequest} activity for trace {expectedTraceId}.");

            IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
                _fixture,
                logStartIndex,
                minimumEvents: 1,
                timeout,
                cts.Token,
                matchPredicate: captured =>
                    string.Equals(captured.AuditEvent.OperationType, SearchOperationType, StringComparison.Ordinal)
                    && string.Equals(captured.AuditEvent.TraceId, expectedTraceId, StringComparison.Ordinal));

            CapturedAuditEvent serverAudit = events.SingleOrDefault(captured =>
                    string.Equals(captured.AuditEvent.OperationType, SearchOperationType, StringComparison.Ordinal)
                    && string.Equals(captured.AuditEvent.TraceId, expectedTraceId, StringComparison.Ordinal))
                ?? throw new ShouldAssertException(
                    $"Expected one search audit event for trace {expectedTraceId}; got {events.Count} candidate events.");

            serverAudit.AuditEvent.TraceId.ShouldBe(searchActivity.TraceId);
            serverAudit.AuditEvent.SpanId.ShouldBe(searchActivity.SpanId);
            serverAudit.AuditEvent.SchemaVersion.ShouldBe(AccessTelemetryEvent.CurrentSchemaVersion);
            serverAudit.AuditEvent.OperationType.ShouldBe(SearchOperationType);
        }
        catch
        {
            DumpDiagnostics(harness, logStartIndex, spanId: string.Empty, cliStdout, cliStderr);
            throw;
        }
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/tenants",
            new TenantProvisioningInput(tenantId, displayName),
            MemoriesJsonContext.Options);
        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

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

        false.ShouldBeTrue($"Tenant '{tenantId}' did not reach Active within {ActivationTimeout}.");
    }

    private async Task<CliInvocationResult> InvokeCliSearchAsync(string tenantId, string query)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        IServiceCollection services = CliServices.BuildCollection();
        services.AddSingleton(new CliConsole { Out = stdout, Error = stderr });
        using ServiceProvider provider = services.BuildServiceProvider();

        CliGlobalOptions options = provider.GetRequiredService<CliGlobalOptions>();
        RootCommand root = RootCommandFactory.Build(provider, options);
        ParseResult parse = root.Parse(
            new[]
            {
                "--format", "json",
                "--endpoint", _fixture.MemoriesClient.BaseAddress!.ToString(),
                "search",
                "query",
                "--tenant", tenantId,
                "--query", query,
                "--axis", "syntactic",
            });

        RootCommandFactory.ApplyGlobalOptions(provider, parse, options);
        int exitCode = await parse.InvokeAsync();
        return new CliInvocationResult(exitCode, stdout.ToString(), stderr.ToString());
    }

    private static void AssertParentChainReachesCliRoot(
        TraceNode start,
        TraceNode expectedRootSpan,
        IReadOnlyDictionary<string, TraceNode> nodesBySpanId)
    {
        // AC #1 traversal rules (Story Rev 0.5):
        //   (i)   cycle detection — visited set fails any chain that revisits a span id
        //   (ii)  max-depth ceiling = 16 — chains longer than 16 hops fail with a diagnostic
        //   (iii) root-termination — chain MUST terminate at the CLI root span (null-parent + name == memories.cli.invoke)
        const int MaxDepth = 16;
        HashSet<string> visited = [];
        TraceNode? current = start;
        int depth = 0;
        while (current is not null)
        {
            string spanId = current.SpanId;
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
                    $"({start.SpanId}); no legitimate in-process trace exceeds this. AC #1 invariant violated.");
            }

            if (string.Equals(current.SpanId, expectedRootSpan.SpanId, StringComparison.Ordinal))
            {
                return; // reached CLI root via reachability — pass.
            }

            if (current.ParentSpanId is null)
            {
                if (string.Equals(current.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal))
                {
                    return;
                }

                throw new ShouldAssertException(
                    $"Orphan span detected: {current.OperationName} has no parent and is not the CLI root span. " +
                    "AC #1 root-termination rule violated.");
            }

            if (!nodesBySpanId.TryGetValue(current.ParentSpanId, out TraceNode? parent))
            {
                throw new ShouldAssertException(
                    $"Parent span '{current.ParentSpanId}' for operation {current.OperationName} was not captured. " +
                    "AC #1 ancestor chain is incomplete.");
            }

            current = parent;
        }
    }

    private void DumpDiagnostics(CliTracingHarness harness, int logStartIndex, string spanId, string cliStdout, string cliStderr)
    {
        _output.WriteLine($"--- Story 8.4 AspireEndToEndTraceTests diagnostics ---");
        _output.WriteLine($"httpClientSpanId={spanId}");
        _output.WriteLine($"cliStdout={cliStdout}");
        _output.WriteLine($"cliStderr={cliStderr}");
        _output.WriteLine(harness.Collector.FormatSpanTree());
        _output.WriteLine($"--- Last server stdout lines (max 50) ---");
        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in AuditEventStreamReader.TailRawLogs(_fixture, logStartIndex, maxLines: 50))
        {
            _output.WriteLine($"[{entry.Level}] {entry.Category}: {entry.Message}");
        }
    }

    private static bool IsAspNetCoreServerSpan(CapturedServerActivity activity)
        => string.Equals(activity.Kind, ActivityKind.Server.ToString(), StringComparison.Ordinal)
            && activity.SourceName.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase);

    private static bool IsAspNetCoreServerSpan(TraceNode node)
        => string.Equals(node.Kind, ActivityKind.Server.ToString(), StringComparison.Ordinal)
            && node.SourceName.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase);

    private static bool IsSearchActivity(CapturedServerActivity activity)
        => string.Equals(activity.SourceName, MemoriesActivitySource.SourceName, StringComparison.Ordinal)
            && string.Equals(activity.OperationName, MemoriesActivitySource.SearchRequest, StringComparison.Ordinal);

    private static TraceNode ToTraceNode(Activity activity)
        => new(
            activity.OperationName,
            activity.Source.Name,
            activity.TraceId.ToString(),
            activity.SpanId.ToString(),
            NormalizeSpanId(activity.ParentSpanId.ToString()),
            activity.Kind.ToString());

    private static TraceNode ToTraceNode(CapturedServerActivity activity)
        => new(
            activity.OperationName,
            activity.SourceName,
            activity.TraceId,
            activity.SpanId,
            NormalizeSpanId(activity.ParentSpanId),
            activity.Kind);

    private static string? NormalizeSpanId(string? value)
        => string.IsNullOrWhiteSpace(value) || value == "0000000000000000"
            ? null
            : value;

    private sealed record CliInvocationResult(int ExitCode, string Stdout, string Stderr);

    private sealed record TraceNode(
        string OperationName,
        string SourceName,
        string TraceId,
        string SpanId,
        string? ParentSpanId,
        string Kind);
}
