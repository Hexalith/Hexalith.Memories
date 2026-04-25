// <copyright file="AuditLogStreamTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Telemetry;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;
using Hexalith.Memories.Telemetry;

using Shouldly;

/// <summary>
/// Story 7.5 Task 11.2 — Tier-2 audit log stream coverage. Drives each of the four instrumented operation
/// surfaces (search / ingest / traverse / case-access) through a validation-fail branch that fires the
/// <see cref="EndpointTelemetryScope"/> dispose-time audit emission WITHOUT requiring Redis / FalkorDB /
/// DAPR downstream. Asserts exactly one <see cref="AccessTelemetryEvent"/> per operation with the AC #4
/// schema shape; asserts the audit event's <c>operationType</c>, <c>outcome</c>, and <c>errorCode</c>
/// reflect the endpoint's taxonomy.
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class AuditLogStreamTests : IDisposable
{
    private static readonly ActivitySource TestRootSource = new("Hexalith.Memories.Server.Tests.AuditRoot");
    private static readonly ActivityListener KeepAliveListener = CreateKeepAliveListener();

    private readonly TelemetryWebAppFactory _factory = new();

    private static HttpRequestMessage BuildRequest(HttpMethod method, string requestUri, string traceId)
    {
        HttpRequestMessage request = new(method, requestUri);
        if (!string.IsNullOrEmpty(traceId))
        {
            // Inject a W3C traceparent header carrying the test-root trace id so the ASP.NET Core
            // pipeline attaches its inbound span (and every child — including memories.search) to
            // the same trace. Without this, the TestServer starts a fresh trace on every request
            // and captured audit events cannot be filtered by test identity.
            request.Headers.Add("traceparent", $"00-{traceId}-0000000000000001-01");
        }

        return request;
    }

    [Fact]
    public async Task SearchEndpoint_InvalidInputPath_EmitsExactlyOneAuditEventWithSchema()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("search-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        HttpResponseMessage response = await client.SendAsync(BuildRequest(HttpMethod.Get, "/api/search?tenantId=&query=foo", traceId));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        AccessTelemetryEvent auditEvent = GetSingleAuditEvent(traceId);

        auditEvent.SchemaVersion.ShouldBe(1);
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationSearch);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
        auditEvent.TenantId.ShouldBe(MemoriesMeter.RejectedTenantTag);
        auditEvent.User.ShouldBe(AccessTelemetryLog.UserAnonymous);
        auditEvent.TraceId.ShouldNotBeNullOrWhiteSpace();
        auditEvent.SpanId.ShouldNotBeNullOrWhiteSpace();
        auditEvent.DurationMs.ShouldBeGreaterThanOrEqualTo(0);
        auditEvent.Timestamp.ShouldNotBeNullOrWhiteSpace();
        auditEvent.QueryParams.ShouldNotBeNull();
        auditEvent.QueryParams!.Count.ShouldBeGreaterThan(0);

        // AC #4 anti-pattern guard: queryParams never contains the authorization header or raw token.
        // `tokenBudget` is allowed because it is a response-size hint, not credential material.
        foreach (KeyValuePair<string, object?> kv in auditEvent.QueryParams!)
        {
            IsCredentialQueryKey(kv.Key)
                .ShouldBeFalse(customMessage: $"queryParams must not expose credential-shaped key '{kv.Key}'.");
        }
    }

    [Theory]
    [InlineData("tokenBudget", false)]
    [InlineData("continuationToken", false)]
    [InlineData("authorization", true)]
    [InlineData("token", true)]
    [InlineData("access_token", true)]
    [InlineData("refresh_token", true)]
    [InlineData("authToken", true)]
    [InlineData("bearerToken", true)]
    [InlineData("api_key", true)]
    [InlineData("clientSecret", true)]
    public void CredentialQueryKeyGuard_AllowsBudgetHintsButRejectsCredentialNames(string key, bool expected)
        => IsCredentialQueryKey(key).ShouldBe(expected);

    [Fact]
    public async Task IngestEndpoint_InvalidInputPath_EmitsExactlyOneAuditEventWithSchema()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("ingest-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        // Empty TenantId trips IngestionInputValidator which re-maps the ArgumentException into
        // ErrorResponse(code="INVALID_INPUT") at the endpoint boundary. The empty-tenant path also
        // switches the scope tenant tag to the synthetic __rejected__ bucket (Rev 0.3 finding 1b).
        IngestionInput input = new()
        {
            TenantId = string.Empty,
            CaseId = "case-1",
            SourceUri = "test://x",
            ContentType = "text/plain",
            SourceType = SourceType.Event,
            IngestedBy = "tests",
        };

        HttpRequestMessage request = BuildRequest(HttpMethod.Post, "/api/ingest", traceId);
        request.Content = System.Net.Http.Json.JsonContent.Create(input, options: MemoriesJsonContext.Options);
        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        AccessTelemetryEvent auditEvent = GetSingleAuditEvent(traceId);

        auditEvent.SchemaVersion.ShouldBe(1);
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationIngest);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_INPUT");
        auditEvent.TenantId.ShouldBe(MemoriesMeter.RejectedTenantTag);
        auditEvent.User.ShouldBe("tests");
        auditEvent.CaseId.ShouldBe("case-1");
        auditEvent.TraceId.ShouldNotBeNullOrWhiteSpace();
        auditEvent.ResultCount.ShouldBeNull();

        // AC #4: ingest queryParams stays bounded — NEVER contains the content body.
        auditEvent.QueryParams.ShouldNotBeNull();
        auditEvent.QueryParams!.Keys.ShouldNotContain("content");
        auditEvent.QueryParams!.Keys.ShouldNotContain("contentBody");
        auditEvent.QueryParams!.Keys.ShouldContain("sourceType");
        auditEvent.QueryParams!.Keys.ShouldContain("contentType");
        auditEvent.QueryParams!.Keys.ShouldContain("bytes");
        auditEvent.QueryParams!.Keys.ShouldNotContain("sourceUriPresent");
        auditEvent.QueryParams!.Keys.ShouldNotContain("contentBytes");
        auditEvent.QueryParams!.Keys.ShouldNotContain("metadataCount");
        auditEvent.QueryParams!["sourceType"].ShouldBe(SourceType.Event.ToString());
        auditEvent.QueryParams!["contentType"].ShouldBe("text/plain");
        auditEvent.QueryParams!["bytes"].ShouldBe(0);
    }

    [Fact]
    public async Task SearchEndpoint_XUserIdHeader_EmitsHeaderValueAsAuditUser()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("search-user-header-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        HttpRequestMessage request = BuildRequest(HttpMethod.Get, "/api/search?tenantId=&query=foo", traceId);
        request.Headers.Add("x-user-id", "reader-42");

        HttpResponseMessage response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        AccessTelemetryEvent auditEvent = GetSingleAuditEvent(traceId);
        auditEvent.User.ShouldBe("reader-42");
    }

    [Fact]
    public async Task TraverseEndpoint_InvalidTenantIdPath_EmitsExactlyOneAuditEventWithSchema()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("traverse-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        // Bad tenant id (contains invalid chars) — ValidateTenantId returns INVALID_TENANT_ID → scope.MarkValidationError
        // recognizes the code as a rejected-tenant code and switches tenant tag to __rejected__.
        HttpResponseMessage response = await client.SendAsync(BuildRequest(HttpMethod.Get, "/api/tenants/bad~id/traverse?startNodeId=s1", traceId));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        AccessTelemetryEvent auditEvent = GetSingleAuditEvent(traceId);

        auditEvent.SchemaVersion.ShouldBe(1);
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTraverse);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_TENANT_ID");
        auditEvent.TenantId.ShouldBe(MemoriesMeter.RejectedTenantTag);
        auditEvent.TraceId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CaseAccessEndpoint_InvalidTenantIdPath_EmitsExactlyOneAuditEventWithSchema()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("case-access-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        HttpResponseMessage response = await client.SendAsync(BuildRequest(HttpMethod.Get, "/api/tenants/bad~id/cases/c1/memory-units/m1", traceId));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        AccessTelemetryEvent auditEvent = GetSingleAuditEvent(traceId);

        auditEvent.SchemaVersion.ShouldBe(1);
        auditEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationCaseAccess);
        auditEvent.Outcome.ShouldBe(AccessTelemetryLog.OutcomeError);
        auditEvent.ErrorCode.ShouldBe("INVALID_TENANT_ID");
        auditEvent.TenantId.ShouldBe(MemoriesMeter.RejectedTenantTag);
        auditEvent.CaseId.ShouldBe("c1");
        auditEvent.TraceId.ShouldNotBeNullOrWhiteSpace();
        auditEvent.QueryParams.ShouldContainKey("memoryUnitId");
    }

    [Fact]
    public async Task HealthEndpoint_EmitsNoAuditEvent()
    {
        using HttpClient client = _factory.CreateClient();
        using Activity? root = TestRootSource.StartActivity("health-test");
        string traceId = root?.TraceId.ToString() ?? string.Empty;

        // AC #5 regression guard: health probes are not in the four enumerated operation types,
        // so no EndpointTelemetryScope runs and no audit event is emitted.
        _ = await client.SendAsync(BuildRequest(HttpMethod.Get, "/health", traceId));

        _factory.AuditLogs.AccessTelemetryCaptures
            .Where(c => c.AuditEvent is not null && string.Equals(c.AuditEvent.TraceId, traceId, StringComparison.Ordinal))
            .Count()
            .ShouldBe(0, "Health-probe paths must not emit audit events (Anti-pattern #5).");
    }

    [Fact]
    public async Task SearchThenTraverse_ProducesOneAuditPerOperation_InEmissionOrder()
    {
        using HttpClient client = _factory.CreateClient();

        string searchTraceId;
        string traverseTraceId;

        using (Activity? root1 = TestRootSource.StartActivity("search-leg"))
        {
            searchTraceId = root1?.TraceId.ToString() ?? string.Empty;
            _ = await client.SendAsync(BuildRequest(HttpMethod.Get, "/api/search?tenantId=&query=one", searchTraceId));
        }

        using (Activity? root2 = TestRootSource.StartActivity("traverse-leg"))
        {
            traverseTraceId = root2?.TraceId.ToString() ?? string.Empty;
            _ = await client.SendAsync(BuildRequest(HttpMethod.Get, "/api/tenants/bad~/traverse?startNodeId=s1", traverseTraceId));
        }

        AccessTelemetryEvent searchEvent = GetSingleAuditEvent(searchTraceId);
        AccessTelemetryEvent traverseEvent = GetSingleAuditEvent(traverseTraceId);

        searchEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationSearch);
        traverseEvent.OperationType.ShouldBe(AccessTelemetryLog.OperationTraverse);

        // Each operation has its own trace id (distinct per-request test roots). Ensures we do not
        // collapse separate requests onto a single audit event.
        searchEvent.TraceId.ShouldNotBe(traverseEvent.TraceId);
    }

    public void Dispose() => _factory.Dispose();

    private AccessTelemetryEvent GetSingleAuditEvent(string traceId)
    {
        IReadOnlyList<AuditLogCapture> captures = _factory.AuditLogs.AccessTelemetryCaptures;
        List<AuditLogCapture> withEvent = [.. captures.Where(c =>
            c.AuditEvent is not null &&
            string.Equals(c.AuditEvent.TraceId, traceId, StringComparison.Ordinal))];
        withEvent.Count.ShouldBe(1, customMessage: $"Expected exactly one audit event with traceId {traceId}; got {withEvent.Count}.");
        return withEvent[0].AuditEvent!;
    }

    private static bool IsCredentialQueryKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string normalized = NormalizeQueryKey(key);
        if (normalized is "token budget" or "continuation token")
        {
            return false;
        }

        if (normalized is "authorization" or "bearer" or "token" or "access token" or "id token" or "refresh token" or "api key" or "client secret" or "secret")
        {
            return true;
        }

        return normalized.EndsWith(" token", StringComparison.Ordinal) ||
            normalized.EndsWith(" secret", StringComparison.Ordinal);
    }

    private static string NormalizeQueryKey(string key)
    {
        List<char> chars = [];
        char previous = '\0';
        foreach (char current in key)
        {
            if (char.IsUpper(current) && chars.Count > 0 && previous != ' ')
            {
                chars.Add(' ');
            }
            else if (current is '_' or '-' or '.')
            {
                chars.Add(' ');
                previous = ' ';
                continue;
            }

            chars.Add(char.ToLowerInvariant(current));
            previous = chars[^1];
        }

        return string.Join(
            ' ',
            new string(chars.ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static ActivityListener CreateKeepAliveListener()
    {
        ActivityListener listener = new()
        {
            ShouldListenTo = source => source == TestRootSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
