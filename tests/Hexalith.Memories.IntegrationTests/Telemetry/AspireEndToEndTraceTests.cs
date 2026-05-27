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
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Memories.Cli;
using Hexalith.Memories.Cli.Commands;
using Hexalith.Memories.Cli.Execution;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.IntegrationTests.Telemetry.Infrastructure;
using Hexalith.Memories.Server.Activities.Indexing;
using Hexalith.Memories.Telemetry;
using Hexalith.Memories.TestHelpers.Factories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using Xunit;

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
/// Story 8.5: AC #2 was flipped from a self-expiring soft-skip to a hard assertion on the
/// Redis-span capture path. The CliSearch end-to-end test now asserts at least one captured
/// Redis-source activity shares the CLI-root TraceId and has a parent chain reaching the CLI root
/// span. The retired <c>Ac2RedisSkipReviewBy</c>/<c>Ac2SkipReviewByTests</c> helpers are deleted;
/// their tracking link (GitHub issue #9) is closed by this story.
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
    private const string SearchOperationType = "search";

    /// <summary>
    /// Story 8.5 — upstream StackExchange.Redis OTEL ActivitySource name
    /// (<c>StackExchangeRedisConnectionInstrumentation.ActivitySourceName</c>, assembly-derived).
    /// Pinned as a constant so a future upstream rename surfaces here as a compile break rather
    /// than a silently dropped assertion.
    /// </summary>
    private const string RedisOtelSourceName = "OpenTelemetry.Instrumentation.StackExchangeRedis";

    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AspireEndToEndTraceTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task CliSearch_EndToEnd_SingleTraceIdAcrossAllHops()
    {
        // AC #1 (NFR28 HTTP-hop gate): invoke the real CLI `memories search query ...` path in-process via
        // the DI-rooted System.CommandLine tree, then prove the combined CLI + server span chain:
        //   memories.cli.invoke → System.Net.Http → AspNetCore server span → memories.search.
        //
        // The CLI spans are captured in-process by CliTracingHarness. The server-side AspNetCore +
        // memories.search spans are emitted as test-only activity breadcrumbs to the server's stderr under
        // HEXALITH_MEMORIES_TELEMETRY_INMEMORY=1 and parsed from the Aspire log stream.
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-telemetry-trace-{Guid.NewGuid():N}");
        string query = $"trace-redis-probe-{Guid.NewGuid():N}";
        await SeedSyntacticDocumentAsync(tenantId, query);

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

            Activity cliRootSpan = SelectSingle(
                capturedSpans.Where(a => string.Equals(a.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal)).ToList(),
                $"CLI root span ({MemoriesActivitySource.CliInvoke})");

            Activity httpClientSpan = SelectSingle(
                capturedSpans.Where(a => a.Source.Name == "System.Net.Http").ToList(),
                "outbound HttpClient span (System.Net.Http source)");

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

            CapturedServerActivity aspNetCoreSpan = SelectSingle(
                matchingServerActivities.Where(IsAspNetCoreServerSpan).ToList(),
                $"AspNetCore server span for trace {expectedTraceId}");

            CapturedServerActivity searchSpan = SelectSingle(
                matchingServerActivities.Where(IsSearchActivity).ToList(),
                $"{MemoriesActivitySource.SearchRequest} activity for trace {expectedTraceId}");

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

            // Story 8.5 AC #2 hard assertion (flipped from Story 8.4's soft-skip): at least one
            // Redis-source activity is captured in the end-to-end trace, shares the CLI-root
            // TraceId, and has a parent chain reaching the CLI root. The Redis DrainThread runs
            // on its own 100ms cadence (ADR-8.5-001 (e)) which ForceFlushAsync does NOT
            // accelerate, so use the TelemetryAsserts.WaitForActivityAsync bounded-polling helper
            // for the presence check.
            await AssertRedisSpanInTraceAsync(
                logStartIndex,
                expectedTraceId,
                cliRootNode,
                nodesBySpanId,
                capturedSpans);
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
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-telemetry-cross-ref-{Guid.NewGuid():N}");
        string query = $"cross-ref-probe-{Guid.NewGuid():N}";

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
            Activity cliRootSpan = SelectSingle(
                harness.Collector.Snapshot().Where(a => string.Equals(a.OperationName, MemoriesActivitySource.CliInvoke, StringComparison.Ordinal)).ToList(),
                $"CLI root span ({MemoriesActivitySource.CliInvoke})");
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

            CapturedServerActivity searchActivity = SelectSingle(
                serverActivities.Where(activity =>
                    string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal)
                    && IsSearchActivity(activity)).ToList(),
                $"server-side {MemoriesActivitySource.SearchRequest} activity for trace {expectedTraceId}");

            IReadOnlyList<CapturedAuditEvent> events = await AuditEventStreamReader.ReadAsync(
                _fixture,
                logStartIndex,
                minimumEvents: 1,
                timeout,
                cts.Token,
                matchPredicate: captured =>
                    string.Equals(captured.AuditEvent.OperationType, SearchOperationType, StringComparison.Ordinal)
                    && string.Equals(captured.AuditEvent.TraceId, expectedTraceId, StringComparison.Ordinal));

            CapturedAuditEvent serverAudit = SelectSingle(
                events.Where(captured =>
                    string.Equals(captured.AuditEvent.OperationType, SearchOperationType, StringComparison.Ordinal)
                    && string.Equals(captured.AuditEvent.TraceId, expectedTraceId, StringComparison.Ordinal)).ToList(),
                $"search audit event for trace {expectedTraceId}");

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

    private async Task SeedSyntacticDocumentAsync(string tenantId, string queryToken)
    {
        IndexInput input = IndexInputFactory.Create(
            tenantId: tenantId,
            memoryUnitId: $"mu-trace-{Guid.NewGuid():N}",
            caseId: $"case-trace-{Guid.NewGuid():N}",
            content: $"Redis span trace probe document containing {queryToken}.",
            sourceUri: $"file:///{Guid.NewGuid():N}.txt",
            embeddingVector: IndexInputFactory.CreateRealisticVector(768),
            embeddingDimensions: 768);

        var activity = new IndexSyntacticActivity(
            _fixture.RedisConnection,
            NullLogger<IndexSyntacticActivity>.Instance);
        Dapr.Workflow.WorkflowActivityContext context = Substitute.For<Dapr.Workflow.WorkflowActivityContext>();
        _ = await activity.RunAsync(context, input).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the single element of <paramref name="candidates"/>, throwing a Shouldly-shaped diagnostic when
    /// the set is empty or contains more than one match. Replaces <c>SingleOrDefault(...) ?? throw</c> patterns
    /// so the failure message names the expected element and includes the actual count — where
    /// <c>SingleOrDefault</c> throws a raw <see cref="InvalidOperationException"/> on multiple matches and loses
    /// the Shouldly diagnostic surface.
    /// </summary>
    private static T SelectSingle<T>(IReadOnlyList<T> candidates, string expectationLabel)
    {
        if (candidates.Count == 0)
        {
            throw new ShouldAssertException(
                $"Expected exactly one {expectationLabel}, but captured none. " +
                "See span / audit diagnostics dump for the full list of observed entries.");
        }

        if (candidates.Count > 1)
        {
            throw new ShouldAssertException(
                $"Expected exactly one {expectationLabel}, but captured {candidates.Count} matching entries. " +
                "Instrumentation drift may have introduced duplicates — see diagnostics dump.");
        }

        return candidates[0];
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
        //
        // Precondition: the expected CLI root span MUST be present in the captured node set. Without this check,
        // a missed ForceFlushAsync on the root span would turn the HttpClient span into the apparent orphan and
        // produce a misleading diagnostic ("HttpClient has no parent"). Verifying presence up-front means the
        // failure clearly attributes the problem to capture wiring rather than to trace topology.
        if (!nodesBySpanId.ContainsKey(expectedRootSpan.SpanId))
        {
            throw new ShouldAssertException(
                $"CLI root span ({expectedRootSpan.OperationName}, span={expectedRootSpan.SpanId}) is missing from " +
                "the captured node set. This typically indicates a missed ForceFlushAsync on the tracer provider " +
                "or a CLI-side capture-wiring regression — fix the flush, not the traversal.");
        }

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

    /// <summary>
    /// Story 8.5 AC #2 hard assertion. Polls the server breadcrumb stream for at least one
    /// activity from the StackExchange.Redis OTEL source whose TraceId matches the CLI root
    /// TraceId, then asserts the Redis span's parent chain reaches the CLI root.
    /// </summary>
    private async Task AssertRedisSpanInTraceAsync(
        int logStartIndex,
        string expectedTraceId,
        TraceNode cliRootNode,
        IReadOnlyDictionary<string, TraceNode> baseNodesBySpanId,
        IReadOnlyList<Activity> capturedCliSpans)
    {
        TimeSpan redisTimeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource redisCts = new(redisTimeout);

        // Redis instrumentation runs its own DrainThread at 100ms cadence. Use the bounded
        // poll helper instead of the ForceFlush + settle-delay smell documented in Task 3.1.1.
        IReadOnlyList<CapturedServerActivity> redisCandidates = await ServerActivityStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 1,
            redisTimeout,
            redisCts.Token,
            matchPredicate: activity =>
                string.Equals(activity.SourceName, RedisOtelSourceName, StringComparison.Ordinal)
                && string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal));

        IReadOnlyList<CapturedServerActivity> matchingRedisActivities =
            [.. redisCandidates.Where(activity =>
                string.Equals(activity.SourceName, RedisOtelSourceName, StringComparison.Ordinal)
                && string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal))];

        // Count-first guard (Story 8.4 Risk R1 convention): assert Count >= 1 before predicate
        // checks. Empty collections vacuously satisfy All(...).
        matchingRedisActivities.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            $"Expected at least one Redis-source activity ({RedisOtelSourceName}) for trace {expectedTraceId}. " +
            "Story 8.5 AC #2 requires the Redis instrumentation to emit spans inside the distributed trace. " +
            "If zero were captured, check: (a) both keyed IConnectionMultiplexer registrations are wired; " +
            "(b) the IntegrationActivityProcessor breadcrumb filter accepts the Redis source under a " +
            "Memories/AspNetCore parent; (c) the search request actually touched a Redis-backed search path.");

        // AC #2 parent-chain reachability: pick the first Redis span with a non-null parent chain
        // that's consistent with the CLI root. Extend the node map with all relevant nodes
        // (CLI subset + server breadcrumbs, including the Redis candidates we just captured) so
        // the traversal can hop via server breadcrumbs.
        Dictionary<string, TraceNode> extendedNodes = new(baseNodesBySpanId, StringComparer.Ordinal);
        foreach (CapturedServerActivity redis in matchingRedisActivities)
        {
            TraceNode node = ToTraceNode(redis);
            extendedNodes[node.SpanId] = node;
        }

        // Add every server breadcrumb we've seen for this trace so the parent-chain walk can
        // traverse through server-side activities beyond just AspNetCore + memories.search.
        TimeSpan traceExpansionTimeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource traceExpansionCts = new(traceExpansionTimeout);
        IReadOnlyList<CapturedServerActivity> allTraceActivities = await ServerActivityStreamReader.ReadAsync(
            _fixture,
            logStartIndex,
            minimumEvents: 1,
            traceExpansionTimeout,
            traceExpansionCts.Token,
            matchPredicate: activity =>
                string.Equals(activity.TraceId, expectedTraceId, StringComparison.Ordinal));

        foreach (CapturedServerActivity a in allTraceActivities
            .Where(a => string.Equals(a.TraceId, expectedTraceId, StringComparison.Ordinal)))
        {
            TraceNode node = ToTraceNode(a);
            extendedNodes[node.SpanId] = node;
        }

        // Finally, include all CLI-captured spans that share the trace so the walk does not
        // stall when a Redis span's ancestor path threads through an unexpected intermediary.
        foreach (Activity a in capturedCliSpans
            .Where(a => string.Equals(a.TraceId.ToString(), expectedTraceId, StringComparison.Ordinal)))
        {
            TraceNode node = ToTraceNode(a);
            extendedNodes[node.SpanId] = node;
        }

        TraceNode redisNode = ToTraceNode(matchingRedisActivities[0]);
        AssertParentChainReachesCliRoot(redisNode, cliRootNode, extendedNodes);
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
        => string.IsNullOrWhiteSpace(value) || value == InMemoryTelemetryEnvironment.EmptySpanIdHex
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
