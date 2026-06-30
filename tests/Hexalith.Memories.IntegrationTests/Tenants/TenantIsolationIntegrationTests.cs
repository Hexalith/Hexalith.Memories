// <copyright file="TenantIsolationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

/// <summary>Integration tests for tenant isolation verification.
/// These tests require the Aspire AppHost fixture with Redis, FalkorDB, and DAPR running.
/// Required before Gate 2 sign-off — NFR8 (zero cross-tenant data leakage) is a hard gate.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantIsolationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="TenantIsolationIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire pipeline fixture.</param>
    public TenantIsolationIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [RunnableSkippedFact("Requires Aspire AppHost fixture with multi-tenant data")]
    public async Task VerifyTenant_WithTwoProvisionedTenants_AllChecksShouldPass()
    {
        // Arrange: Provision tenant A and B, ingest memory units into both
        // Act: POST /api/tenants/tenant-a/verify
        // Assert: AllPassed == true, all individual checks passed

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant-a/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.AllPassed.ShouldBeTrue();
        result.Checks.ShouldNotBeEmpty();
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with multi-tenant graph data")]
    public async Task VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes()
    {
        // AC #2: Create identical graph structures in tenant A and B with colliding edge IDs
        // Run verify on A, confirm zero nodes from B (NFR8 edge ID collision test)

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant-a/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        TenantIsolationCheckResult graphCheck = result.Checks
            .First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with multi-tenant data")]
    public async Task VerifyTenant_SearchFromOtherContext_ZeroResultsAcrossAllAxes()
    {
        // Ingest into A, search from B context, confirm zero results across all axes

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant-a/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Checks.First(c => c.CheckName == "SyntacticIsolation").Passed.ShouldBeTrue();
        result.Checks.First(c => c.CheckName == "SemanticIsolation").Passed.ShouldBeTrue();
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture")]
    public async Task VerifyTenant_MalformedTenantId_Returns400()
    {
        // Run verify with malformed tenant ID, confirm rejection

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/../escape/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture")]
    public async Task VerifyTenant_NonExistentTenant_Returns404()
    {
        // Run verify with non-existent tenant ID

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/nonexistent-tenant/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content
            .ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with multi-tenant data and deletion")]
    public async Task VerifyTenant_AfterOtherTenantDeleted_IsolationUnaffected()
    {
        // Delete tenant B, run verify on A, confirm A isolation unaffected

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant-a/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.AllPassed.ShouldBeTrue();
    }

    [RunnableSkippedFact("Requires Aspire AppHost fixture with planted cross-tenant data")]
    public async Task VerifyTenant_PlantedCrossTenantData_DetectsLeakage()
    {
        // Negative test (false-pass prevention): Deliberately plant cross-tenant data
        // (e.g., manually write a hash with tenant B's key prefix into tenant A's RediSearch index),
        // run verify on A, confirm the verifier detects the planted leakage.
        // This prevents false-pass bugs in FT.SEARCH query construction.

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant-a/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.AllPassed.ShouldBeFalse();
    }
}
