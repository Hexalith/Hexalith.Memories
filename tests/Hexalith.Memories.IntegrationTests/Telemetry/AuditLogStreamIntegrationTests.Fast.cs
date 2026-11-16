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

/// <summary>Contains the fast integration cases for AuditLogStreamIntegrationTests.</summary>
public sealed partial class AuditLogStreamIntegrationTests
{
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
                    $"/api/v1/search?tenantId={tenantId}&query={query}&axis=syntactic");
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
                    "/api/v1/ingest",
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
                using HttpResponseMessage r = await client.GetAsync($"/api/v1/tenants/{tenantId}/traverse?depth=1");
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
                    $"/api/v1/tenants/{tenantId}/cases/case-missing/memory-units/memory-unit-missing");
                r.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            });
    }

    [Fact]
    public async Task HealthProbes_EmitZeroAuditEvents()
    {
        // Story 7.5 AC #5 + Story 8.4 AC #3 regression guard at the deployed-stack level. Health
        // endpoints (/health, /alive, /ready) are NOT in the four enumerated operation types, so
        // no EndpointTelemetryScope runs and no audit event is emitted on the deployed stack.
        //
        // The test has two phases to defend against both false-positive ("a prior test's late-
        // arriving audit event flipped the zero-events assertion") AND false-negative ("the entire
        // audit pipeline is broken but the test passes vacuously because zero events is the always-
        // true answer") failure modes:
        //
        //   1. Sentinel phase — provision a tenant and run ONE search to prove the audit pipeline is
        //      alive. Wait for its audit event to land. This both (a) proves the pipeline works and
        //      (b) drains any in-flight events from prior tests in the shared Aspire collection.
        //   2. Probe phase — record a post-sentinel log start index, run the health probes, wait the
        //      full negative window, and scan once for any audit events emitted after the start index.
        string sentinelTenantId = await EnsureProvisionedTenantAsync();
        int sentinelLogStart = _fixture.LogEntryCount;
        string sentinelQuery = $"health-probe-sentinel-{Guid.NewGuid():N}";
        using (HttpResponseMessage sentinelResp = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={sentinelTenantId}&query={sentinelQuery}&axis=syntactic"))
        {
            sentinelResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        TimeSpan sentinelTimeout = AuditEventStreamReader.ResolveTimeout();
        using CancellationTokenSource sentinelCts = new(sentinelTimeout);
        IReadOnlyList<CapturedAuditEvent> sentinelEvents = await AuditEventStreamReader.ReadAsync(
            _fixture,
            sentinelLogStart,
            minimumEvents: 1,
            sentinelTimeout,
            sentinelCts.Token,
            matchPredicate: captured =>
                string.Equals(captured.AuditEvent.TenantId, sentinelTenantId, StringComparison.Ordinal)
                && string.Equals(captured.AuditEvent.OperationType, OperationSearch, StringComparison.Ordinal));

        sentinelEvents.Count(captured =>
            string.Equals(captured.AuditEvent.TenantId, sentinelTenantId, StringComparison.Ordinal)
            && string.Equals(captured.AuditEvent.OperationType, OperationSearch, StringComparison.Ordinal))
            .ShouldBeGreaterThanOrEqualTo(
                1,
                "Sentinel search did not emit an audit event — the audit pipeline is broken at the " +
                "deployed stack level, so the zero-events assertion that follows would pass vacuously. " +
                "Fix the audit emission path before re-running HealthProbes_EmitZeroAuditEvents.");

        // Short drain window to let any straggling audit events from prior tests in the shared
        // collection land before we capture the probe-phase start index.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        int probeLogStart = _fixture.LogEntryCount;

        for (int i = 0; i < 5; i++)
        {
            using HttpResponseMessage health = await _fixture.MemoriesClient.GetAsync("/health");
            health.IsSuccessStatusCode.ShouldBeTrue();
            using HttpResponseMessage alive = await _fixture.MemoriesClient.GetAsync("/alive");
            alive.IsSuccessStatusCode.ShouldBeTrue();
            using HttpResponseMessage ready = await _fixture.MemoriesClient.GetAsync("/ready");
            ready.IsSuccessStatusCode.ShouldBeTrue();
        }

        // Wait the full negative window before scanning — a polling Read with minimumEvents: 1 would
        // return early on the first spurious event from any concurrent activity, defeating the
        // negative-space assertion. We want "no events emitted during or shortly after the probe
        // phase", which requires a full-window wait + one-shot scan.
        TimeSpan negativeWindow = TimeSpan.FromSeconds(3);
        await Task.Delay(negativeWindow);

        IReadOnlyList<CapturedAuditEvent> probeEvents = AuditEventStreamReader.Scan(_fixture, probeLogStart);
        probeEvents.Count.ShouldBe(
            0,
            "Health probes (/health, /alive, /ready) MUST emit zero AccessTelemetryEvent entries " +
            "(AC #5 regression guard from Story 7.5). Captured events: " +
            string.Join(", ", probeEvents.Select(e => $"{e.EventId}/{e.AuditEvent.OperationType}/{e.AuditEvent.TenantId}")));
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
            $"/api/v1/search?tenantId={tenantId}&query=schema-probe-{Guid.NewGuid():N}&axis=syntactic");
        searchResp.StatusCode.ShouldBe(HttpStatusCode.OK);

        using HttpResponseMessage traverseResp = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/traverse?depth=1");
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
}
