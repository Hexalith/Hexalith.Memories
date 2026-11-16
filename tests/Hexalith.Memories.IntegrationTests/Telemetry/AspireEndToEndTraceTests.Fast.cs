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

/// <summary>Contains the fast integration cases for AspireEndToEndTraceTests.</summary>
public sealed partial class AspireEndToEndTraceTests
{
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
}
