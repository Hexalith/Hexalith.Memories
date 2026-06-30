// <copyright file="TenantContextEnforcementIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.Logging;

using Shouldly;

/// <summary>Integration tests for Story 5.4 — tenant context enforcement.
/// <para>
/// Tests cover AC1 (registry validation on previously-unprotected endpoints), AC2 (cross-tenant
/// mismatch detection end-to-end, including the planted-corruption scenario), and AC3 (DAPR API
/// token behavior on the Aspire fixture — note: token authentication is only active when
/// <c>DAPR_API_TOKEN_MODE=enabled</c> is set; the standard fixture leaves it disabled to preserve
/// existing test coverage per Story 5.4 task 3.7).
/// </para>
/// <para>
/// These tests follow the 5-1 / 5-2 / 5-3 deferral pattern: <c>Skip</c> marks them as documented and
/// discoverable prerequisites for Gate 2 sign-off. Running them requires the full Aspire AppHost with
/// Redis, FalkorDB, and DAPR. Remove the <c>Skip</c> attribute once the fixture is available in CI.
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantContextEnforcementIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="TenantContextEnforcementIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire pipeline fixture.</param>
    public TenantContextEnforcementIntegrationTests(AspireIngestionPipelineFixture fixture)
        => _fixture = fixture;

    // ---------------------------------------------------------------------------------------------
    // AC1 — Registry validation on previously-unprotected endpoints
    // ---------------------------------------------------------------------------------------------
    [RunnableSkippedFact("Requires Aspire AppHost fixture")]
    public async Task EmbeddingConfig_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/tenants/unknown-tenant-xyz/embedding-config");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with a Provisioning-state tenant")]
    public async Task EmbeddingConfig_ProvisioningTenant_Returns409TenantProvisioning()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/tenants/provisioning-tenant/embedding-config");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_PROVISIONING");
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture")]
    public async Task ProvisionStatus_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/tenants/unknown-tenant-xyz/provision-status/provision-unknown-tenant-xyz-abc");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with a Deleting-state tenant")]
    public async Task DeletionStatus_DeletingTenant_Returns200()
    {
        // AC1 edge case: deletion-status must remain callable for Deleting tenants (that's its purpose).
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/tenants/deleting-tenant/deletion-status/delete-deleting-tenant-abc");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with a Failed-state tenant")]
    public async Task Verify_FailedTenant_Returns200()
    {
        // AC1 edge case: verify endpoint must work on any existing tenant regardless of status
        // (useful for diagnosing Failed tenants).
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsync("/api/tenants/failed-tenant/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture")]
    public async Task Verify_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsync("/api/tenants/unknown-tenant-xyz/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    // ---------------------------------------------------------------------------------------------
    // AC2 — Cross-tenant mismatch detection end-to-end
    // ---------------------------------------------------------------------------------------------
    [RunnableSkippedFact("Requires Aspire AppHost fixture with two provisioned tenants and shared Redis")]
    public async Task MemoryUnit_CorruptedTenantId_Returns404AndLogsCritical()
    {
        // This is the tertiary-defense test from Story 5.4 AC2:
        //   1. Provision tenants A and B.
        //   2. Ingest a memory unit under tenant A (say mu-xyz).
        //   3. Plant corruption by direct Redis write: HSET tenant-a:mu:mu-xyz tenantId tenant-b.
        //   4. GET /api/tenants/tenant-a/cases/{caseId}/memory-units/mu-xyz
        //   5. Assert the endpoint returns 404 (not 200 — no data leakage).
        //   6. Assert a Critical log entry with TENANT_MISMATCH was emitted (captured by test sink).
        int logStart = _fixture.LogEntryCount;

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/tenants/tenant-a/cases/case-1/memory-units/mu-xyz");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _fixture.GetLogEntriesSince(logStart)
            .Any(entry =>
                entry.Level == LogLevel.Critical &&
                entry.Message.Contains("TENANT_MISMATCH", StringComparison.Ordinal))
            .ShouldBeTrue();
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with two provisioned tenants")]
    public async Task Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant()
    {
        // Search scoped to tenant A must never return content from tenant B.
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/search?tenantId=tenant-a&query=tenant-b-specific-phrase");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------
    // AC3 — DAPR API token behavior (manual-verification equivalent)
    // ---------------------------------------------------------------------------------------------
    [RunnableSkippedFact("Requires Aspire AppHost fixture with DAPR_API_TOKEN_MODE=enabled; manual verification in MVP")]
    public async Task DaprSidecar_RequestWithoutApiToken_IsRejected()
    {
        // AC3 is fundamentally not unit-testable (DAPR runtime validates tokens). When the fixture
        // runs with DAPR_API_TOKEN_MODE=enabled the request must target the sidecar directly, not the
        // application endpoint, otherwise a 200 OK can be a false positive caused by bypassing DAPR.
        using HttpClient tokenlessClient = new() { BaseAddress = _fixture.DaprSidecarHttpEndpoint };
        using HttpResponseMessage response = await tokenlessClient.GetAsync("/v1.0/metadata");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
