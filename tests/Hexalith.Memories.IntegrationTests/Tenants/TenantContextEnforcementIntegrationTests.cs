// <copyright file="TenantContextEnforcementIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

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
    [Fact]
    public async Task EmbeddingConfig_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/v1/tenants/unknown-tenant-xyz/embedding-config");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task EmbeddingConfig_ProvisioningTenant_Returns409TenantProvisioning()
    {
        string tenantId = $"tenant-provisioning-{Guid.NewGuid():N}";
        await _fixture.SeedTenantRegistryEntryAsync(tenantId, TenantStatus.Provisioning);

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync($"/api/v1/tenants/{tenantId}/embedding-config");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_PROVISIONING");
    }

    [Fact]
    public async Task ProvisionStatus_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync("/api/v1/tenants/unknown-tenant-xyz/provision-status/provision-unknown-tenant-xyz-abc");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletionStatus_DeletingTenant_Returns200()
    {
        // AC1 edge case: deletion-status must remain callable for Deleting tenants (that's its purpose).
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-delete-status-{Guid.NewGuid():N}");
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string workflowInstanceId = await ReadWorkflowInstanceIdAsync(deleteResponse.Content);

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync($"/api/v1/tenants/{tenantId}/deletion-status/{workflowInstanceId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Verify_FailedTenant_Returns200()
    {
        // AC1 edge case: verify endpoint must work on any existing tenant regardless of status
        // (useful for diagnosing Failed tenants).
        string tenantId = $"tenant-failed-{Guid.NewGuid():N}";
        await _fixture.SeedTenantRegistryEntryAsync(tenantId, TenantStatus.Failed);

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsync($"/api/v1/tenants/{tenantId}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Verify_UnknownTenant_Returns404TenantNotFound()
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient
            .PostAsync("/api/v1/tenants/unknown-tenant-xyz/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    // ---------------------------------------------------------------------------------------------
    // AC2 — Cross-tenant mismatch detection end-to-end
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task MemoryUnit_CorruptedTenantId_Returns404WithoutLeakingData()
    {
        // This is the tertiary-defense test from Story 5.4 AC2:
        //   1. Provision tenants A and B.
        //   2. Ingest a memory unit under tenant A (say mu-xyz).
        //   3. Plant corruption by direct Redis write: HSET tenant-a:mu:mu-xyz tenantId tenant-b.
        //   4. GET /api/v1/tenants/tenant-a/cases/{caseId}/memory-units/mu-xyz
        //   5. Assert the endpoint returns 404 (not 200 — no data leakage).
        //   6. The production path records TENANT_MISMATCH via TenantMismatchMonitor; this
        //      integration fixture asserts the externally enforceable no-leakage boundary.
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");
        string caseId = "case-1";
        string memoryUnitId = "mu-xyz";
        await SeedMemoryUnitHashAsync(tenantA, caseId, memoryUnitId, "Corrupted tenant payload.", storedTenantId: tenantB);

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync($"/api/v1/tenants/{tenantA}/cases/{caseId}/memory-units/{memoryUnitId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_CrossTenantScope_ReturnsZeroResultsFromOtherTenant()
    {
        // Search scoped to tenant A must never return content from tenant B.
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");
        string phrase = $"tenant-b-specific-phrase-{Guid.NewGuid():N}";
        await SeedMemoryUnitHashAsync(tenantB, "case-1", "mu-b", phrase, storedTenantId: tenantB);

        using HttpResponseMessage response = await _fixture.MemoriesClient
            .GetAsync($"/api/v1/search?tenantId={tenantA}&axis=syntactic&query={Uri.EscapeDataString(phrase)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        result.ShouldNotBeNull();
        result.Results.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------
    // AC3 — DAPR API token behavior (manual-verification equivalent)
    // ---------------------------------------------------------------------------------------------
    [Fact]
    public async Task DaprSidecar_RequestWithoutApiToken_IsRejected()
    {
        // AC3 is fundamentally not unit-testable (DAPR runtime validates tokens). When the fixture
        // runs with DAPR_API_TOKEN_MODE=enabled the request must target the sidecar directly, not the
        // application endpoint, otherwise a 200 OK can be a false positive caused by bypassing DAPR.
        using HttpClient tokenlessClient = new() { BaseAddress = _fixture.DaprSidecarHttpEndpoint };
        using HttpResponseMessage response = await tokenlessClient.GetAsync("/v1.0/metadata");

        if (string.Equals(Environment.GetEnvironmentVariable("DAPR_API_TOKEN_MODE"), "enabled", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode.ShouldBeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        }
        else
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    private async Task SeedMemoryUnitHashAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string content,
        string storedTenantId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string contentHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        await _fixture.RedisConnection.GetDatabase().HashSetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            [
                new HashEntry("id", memoryUnitId),
                new HashEntry("tenantId", storedTenantId),
                new HashEntry("caseId", caseId),
                new HashEntry("content", content),
                new HashEntry("contentHash", contentHash),
                new HashEntry("sourceUri", $"file:///{memoryUnitId}.txt"),
                new HashEntry("sourceType", SourceType.File.ToString()),
                new HashEntry("ingestedBy", "integration@test.local"),
                new HashEntry("ingestedAt", now.ToString("O")),
                new HashEntry("lastUpdated", now.ToString("O")),
                new HashEntry("status", MemoryUnitStatus.Indexed.ToString()),
                new HashEntry("metadataJson", "{}"),
            ]);
    }

    private static async Task<string> ReadWorkflowInstanceIdAsync(HttpContent content)
    {
        string body = await content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("workflowInstanceId").GetString()
            ?? throw new InvalidOperationException($"Response did not contain a workflowInstanceId: {body}");
    }
}
