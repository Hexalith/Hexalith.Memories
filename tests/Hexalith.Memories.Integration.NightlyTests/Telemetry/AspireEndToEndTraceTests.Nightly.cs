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

/// <summary>Contains the nightly integration cases for AspireEndToEndTraceTests.</summary>
public sealed partial class AspireEndToEndTraceTests
{
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
        string query = $"traceredisprobe{Guid.NewGuid():N}";
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
}
