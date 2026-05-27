// <copyright file="TelemetrySummaryEndpointTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Tests.Telemetry;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Dapr.Client;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Tenants;
using Hexalith.Memories.Server.Tests.Telemetry.Infrastructure;

using NSubstitute;

using Shouldly;

/// <summary>
/// Story 7.5 Task 9.1 — Tier-2 HTTP-contract coverage for
/// <c>GET /api/tenants/{tenantId}/telemetry/summary</c> (AC #6). Exercises the endpoint through
/// <see cref="TelemetryWebAppFactory"/> — fake <see cref="DaprClient"/> governs the
/// <see cref="TenantStatusGuard"/> 404 vs happy-path branch, while the fake Redis/FalkorDB
/// multiplexers produce unavailable-but-non-throwing reads so <see cref="TelemetrySummaryService"/>
/// can compose a real response from <see cref="RollingCounterStore"/> + a zero-queue-depth
/// <see cref="Ingestion.PerTenantConcurrencyGate"/>. The Tier-3 Aspire integration test (Task 11.3)
/// covers downstream index-size accuracy; this test gates the endpoint wiring, 404 semantics, and
/// JSON shape pinning AC #6.
/// </summary>
[Collection(Infrastructure.TelemetryTestCollection.Name)]
public sealed class TelemetrySummaryEndpointTests : IDisposable
{
    private const string StoreName = "statestore";

    private readonly TelemetryWebAppFactory _factory = new();

    [Fact]
    public async Task GetTelemetrySummary_UnknownTenant_Returns404()
    {
        // Fake DaprClient returns null for any state read by default (NSubstitute behavior for reference types) →
        // TenantRegistryService.GetTenantAsync → null → TenantStatusGuard returns TENANT_NOT_FOUND → HTTP 404.
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants/unknown-tenant/telemetry/summary", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task GetTelemetrySummary_InvalidTenantIdFormat_Returns400()
    {
        using HttpClient client = _factory.CreateClient();

        // The ValidateTenantId guard rejects whitespace / underscore before the registry lookup.
        HttpResponseMessage response = await client.GetAsync("/api/tenants/invalid_tenant_id/telemetry/summary", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTelemetrySummary_KnownActiveTenant_Returns200WithPinnedShape()
    {
        const string TenantId = "acme-telemetry";
        TenantRegistryEntry entry = new(
            new TenantInfo(TenantId, "Acme Telemetry", TenantStatus.Active, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<TenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(k => k.Contains(TenantId, StringComparison.Ordinal)),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"/api/tenants/{TenantId}/telemetry/summary", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        TelemetrySummary? summary = await response.Content.ReadFromJsonAsync<TelemetrySummary>(cancellationToken: CancellationToken.None);
        summary.ShouldNotBeNull();

        // AC #6 pinned shape — tenant echo + required sub-records.
        summary.TenantId.ShouldBe(TenantId);
        summary.AsOf.ShouldNotBeNullOrWhiteSpace();
        DateTimeOffset.Parse(summary.AsOf, System.Globalization.CultureInfo.InvariantCulture).ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));

        // Rolling counter store has no measurements → zero requests/errors across all axes (not null).
        summary.SearchMetrics.Syntactic.RequestsLast5m.ShouldBe(0);
        summary.SearchMetrics.Syntactic.ErrorsLast5m.ShouldBe(0);
        summary.SearchMetrics.Semantic.RequestsLast5m.ShouldBe(0);
        summary.SearchMetrics.Semantic.ErrorsLast5m.ShouldBe(0);
        summary.SearchMetrics.Graph.RequestsLast5m.ShouldBe(0);
        summary.SearchMetrics.Graph.ErrorsLast5m.ShouldBe(0);
        summary.SearchMetrics.Hybrid.RequestsLast5m.ShouldBe(0);
        summary.SearchMetrics.Hybrid.ErrorsLast5m.ShouldBe(0);

        // Ingestion metrics: rolling store empty; PerTenantConcurrencyGate has no slot for this tenant → depth 0.
        summary.IngestionMetrics.DocumentsLast5m.ShouldBe(0);
        summary.IngestionMetrics.FailuresLast5m.ShouldBe(0);
        summary.IngestionMetrics.QueueDepth.ShouldBe(0);

        // Fake Redis/FalkorDB multiplexers report no endpoints — index sizes land as nulls (unavailable, not zero);
        // the sub-record itself is present (AC #6 requires the field, nullable values are allowed).
        summary.IndexSizes.ShouldNotBeNull();
        summary.IndexHealth.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTelemetrySummary_DeletingTenant_Returns409()
    {
        const string TenantId = "tenant-deleting";
        TenantRegistryEntry entry = new(
            new TenantInfo(TenantId, "Deleting", TenantStatus.Deleting, DateTimeOffset.UtcNow),
            WorkflowInstanceId: null);

        _factory.DaprClient
            .GetStateAsync<TenantRegistryEntry?>(
                StoreName,
                Arg.Is<string>(k => k.Contains(TenantId, StringComparison.Ordinal)),
                Arg.Any<ConsistencyMode?>(),
                Arg.Any<System.Collections.Generic.IReadOnlyDictionary<string, string>?>(),
                Arg.Any<CancellationToken>())
            .Returns(entry);

        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"/api/tenants/{TenantId}/telemetry/summary", CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_DELETING");
    }

    public void Dispose() => _factory.Dispose();
}
