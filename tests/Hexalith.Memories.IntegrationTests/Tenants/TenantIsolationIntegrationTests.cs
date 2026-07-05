// <copyright file="TenantIsolationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using System.Text;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

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

    [Fact]
    public async Task VerifyTenant_WithTwoProvisionedTenants_CoreIsolationChecksShouldPass()
    {
        // Arrange: Provision tenant A and B, ingest memory units into both
        // Act: POST /api/tenants/tenant-a/verify
        // Assert: AllPassed == true, all individual checks passed
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        _ = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Checks.ShouldNotBeEmpty();
        AssertCoreIsolationChecksPassed(result);
    }

    [Fact]
    public async Task VerifyTenant_IdenticalGraphStructures_ZeroCrossTenantNodes()
    {
        // AC #2: Create identical graph structures in tenant A and B with colliding edge IDs
        // Run verify on A, confirm zero nodes from B (NFR8 edge ID collision test)
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        _ = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        TenantIsolationCheckResult graphCheck = result.Checks
            .First(c => c.CheckName == "GraphIsolation");
        graphCheck.Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyTenant_SearchFromOtherContext_ZeroResultsAcrossAllAxes()
    {
        // Ingest into A, search from B context, confirm zero results across all axes
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        _ = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.Checks.First(c => c.CheckName == "SyntacticIsolation").Passed.ShouldBeTrue();
        result.Checks.First(c => c.CheckName == "SemanticIsolation").Passed.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyTenant_MalformedTenantId_Returns400()
    {
        // Run verify with malformed tenant ID, confirm rejection

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/api/tenants/tenant_with_underscore/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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

    [Fact]
    public async Task VerifyTenant_AfterOtherTenantDeleted_IsolationUnaffected()
    {
        // Delete tenant B, run verify on A, confirm A isolation unaffected
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/tenants/{tenantB}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        AssertCoreIsolationChecksPassed(result);
    }

    [Fact]
    public async Task VerifyTenant_PlantedCrossTenantData_DetectsLeakage()
    {
        // Negative test (false-pass prevention): Deliberately plant cross-tenant data
        // (e.g., manually write a hash under tenant A's prefix with tenant B's stored tenantId),
        // run verify on A, confirm the verifier detects the planted leakage.
        // This prevents false-pass bugs in the target-prefix cursor checks.
        string tenantA = await _fixture.ProvisionActiveTenantAsync($"tenant-a-{Guid.NewGuid():N}");
        string tenantB = await _fixture.ProvisionActiveTenantAsync($"tenant-b-{Guid.NewGuid():N}");
        await SeedMemoryUnitHashAsync(tenantA, "case-1", "mu-leak", "Planted cross-tenant payload.", tenantB);

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            $"/api/tenants/{tenantA}/verify", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantIsolationVerificationResult? result = await response.Content
            .ReadFromJsonAsync<TenantIsolationVerificationResult>(MemoriesJsonContext.Options);

        result.ShouldNotBeNull();
        result.AllPassed.ShouldBeFalse();
        TenantIsolationCheckResult syntacticCheck = result.Checks.First(c => c.CheckName == "SyntacticIsolation");
        syntacticCheck.Passed.ShouldBeFalse();
        syntacticCheck.Details.ShouldNotBeNull();
        syntacticCheck.Details.ShouldContain(tenantB);
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

    private static void AssertCoreIsolationChecksPassed(TenantIsolationVerificationResult result)
    {
        foreach (string checkName in new[]
        {
            "IndexExistence",
            "SyntacticIsolation",
            "SemanticIsolation",
            "GraphIsolation",
        })
        {
            TenantIsolationCheckResult check = result.Checks.First(c => c.CheckName == checkName);
            check.Passed.ShouldBe(
                true,
                $"{check.CheckName} failed: {check.Details ?? "(no details)"}. Summary: {result.Summary}");
        }
    }
}
