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

/// <summary>Contains the nightly integration cases for AuditLogStreamIntegrationTests.</summary>
public sealed partial class AuditLogStreamIntegrationTests
{
    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task SearchOperation_RetrySequence_EmitsDistinctAuditEventsPerStatus()
    {
        string tenantId = await EnsureProvisionedTenantAsync();
        string secondAttemptQuery = $"retry-query-{Guid.NewGuid():N}";
        int logStartIndex = _fixture.LogEntryCount;

        // Register an ActivityListener so the test-owned ActivitySource activates and
        // Activity.Current propagates a real W3C trace id. Without an active listener,
        // new Activity(...).Start() does NOT generate a W3C trace id (Activity.Current
        // stays null or empty) and the traceId-based poll predicate below never matches.
        using ActivitySource retrySource = new($"telemetry-retry-{Guid.NewGuid():N}");
        using ActivityListener listener = new()
        {
            ShouldListenTo = source => ReferenceEquals(source, retrySource),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? retryRoot = retrySource.StartActivity("telemetry-retry-sequence-root", ActivityKind.Client);
        retryRoot.ShouldNotBeNull("ActivityListener registration failed — retry trace id capture will not work.");

        using (Activity? firstAttempt = retrySource.StartActivity("telemetry-retry-attempt-1"))
        {
            using HttpResponseMessage first = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={tenantId}&axis=graph&depth=1");
            first.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        string traceId = retryRoot.TraceId.ToString();

        using Activity? secondAttempt = retrySource.StartActivity("telemetry-retry-attempt-2");
        using HttpResponseMessage second = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&axis=syntactic&query={secondAttemptQuery}");
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
}
