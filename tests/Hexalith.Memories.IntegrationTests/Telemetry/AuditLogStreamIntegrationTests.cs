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
using System.Net.Http.Headers;
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

using Xunit;

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
public sealed partial class AuditLogStreamIntegrationTests
{
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

    private Task<string> EnsureProvisionedTenantAsync(CancellationToken cancellationToken = default)
        => _fixture.ProvisionActiveTenantAsync(
            tenantId: $"tenant-telemetry-audit-{Guid.NewGuid():N}",
            cancellationToken: cancellationToken);

    private async Task<string> CreateCaseAsync(string tenantId)
    {
        using var http = new HttpClient
        {
            BaseAddress = _fixture.MemoriesClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            AspireIngestionPipelineFixture.MintServerBearer(tenantId));
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
