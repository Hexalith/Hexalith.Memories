// <copyright file="TenantConfigurationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using Hexalith.Memories.IntegrationTests.Fixtures;

/// <summary>
/// Integration tests for Story 5.5 — tenant configuration &amp; listing (AC1–AC6 / FR41, FR42, FR43, FR45, FR69, FR70).
/// <para>
/// Follows the 5-1 / 5-2 / 5-3 / 5-4 deferral pattern: tests are marked <c>Skip</c> as documented,
/// discoverable prerequisites for Gate 2 sign-off. Running them requires the full Aspire AppHost
/// with Redis, FalkorDB, and DAPR. Remove the <c>Skip</c> attribute once the fixture is available
/// in CI.
/// </para>
/// <para>
/// The FR70 golden-path end-to-end test (last scenario) SHOULD be unskipped if any ingestion-path
/// integration fixture already runs in CI — per Task 6.1 it's the one new durable field and the
/// primary regression risk; if no fixture exists, a unit-level fallback in
/// <c>IngestionWorkflowTests</c> must assert <c>IndexInput.EmbeddingModel</c> is populated.
/// </para>
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantConfigurationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public TenantConfigurationIntegrationTests(AspireIngestionPipelineFixture fixture)
        => _fixture = fixture;

    // AC1 / FR41 — enriched tenant listing.
    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task ListTenants_ReturnsEnrichedSummaryWithCountsAndIndexHealth()
    {
        // Given a tenant with indexed memory units,
        // When GET /api/tenants,
        // Then the response contains a TenantSummary[] with memoryUnitCount > 0,
        // indexSizes populated, reindexRequired=false, and lastActivityAt set.
        _ = _fixture;
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture with one backend stopped")]
    public async Task ListTenants_WhenOneBackendStopped_TenantStillListedWithUnknownOnThatAxis()
    {
        // Given the Redis Vector backend is stopped,
        // When GET /api/tenants,
        // Then the tenant still appears in the list, IndexSizes.RedisVectorKeyCount == null,
        // and IndexStatus.RedisVector == IndexHealth.Unknown. Other backends report Ready.
        _ = _fixture;
        await Task.CompletedTask;
    }

    // AC2 / FR45 — tenant configuration view.
    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task GetConfiguration_ReturnsComposedView_WithFullEmbeddingConfig()
    {
        // Given a provisioned tenant,
        // When GET /api/tenants/{id}/configuration,
        // Then the response body is a TenantConfigurationView containing the full
        // TenantEmbeddingConfig (including apiSecretKeyName as a non-sensitive name),
        // IndexStatus=Ready on all three backends, memoryUnitCount, lastActivityAt, createdAt.
        _ = _fixture;
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task GetConfiguration_UnknownTenant_Returns404TenantNotFound()
    {
        _ = _fixture;
        await Task.CompletedTask;
    }

    // AC3 / FR42 — PATCH display name.
    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task PatchDisplayName_UpdatesRegistryAndReflectsInSubsequentGet()
    {
        // Given a provisioned tenant with displayName "Old",
        // When PATCH /api/tenants/{id} with {"displayName":"New"},
        // Then the response is 200 with the updated TenantSummary,
        // subsequent GET /api/tenants/{id} reflects "New",
        // and log capture contains the Information operational-log entry with
        // oldValue="Old", newValue="New", actor containing remote IP, durationMs > 0.
        _ = _fixture;
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture with non-Active tenant")]
    public async Task PatchDisplayName_NonActiveTenant_Returns409()
    {
        // Given a tenant in Provisioning / Deleting / Failed state,
        // When PATCH /api/tenants/{id},
        // Then the response is 409 with code TENANT_PROVISIONING / TENANT_DELETING / TENANT_FAILED.
        _ = _fixture;
        await Task.CompletedTask;
    }

    // AC4 / FR43 — embedding config breaking-change flow (existing PUT /embedding-config).
    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task PutEmbeddingConfig_BreakingChange_WithoutForceReindex_Returns409()
    {
        // Given a tenant with dimensions=768,
        // When PUT /api/tenants/{id}/embedding-config with dimensions=1536 and forceReindex=false,
        // Then response is 409 with error code EMBEDDING_CONFIG_BREAKING_CHANGE,
        // affectedFields contains "dimensions",
        // and subsequent GET /api/tenants/{id} shows reindexRequired=false (unchanged).
        _ = _fixture;
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task PutEmbeddingConfig_BreakingChange_WithForceReindex_Returns200AndSetsReindexRequired()
    {
        _ = _fixture;
        await Task.CompletedTask;
    }

    // AC5 / FR69 — rate-limit propagation on next embedding request.
    [Fact(Skip = "Requires Aspire AppHost fixture")]
    public async Task PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest()
    {
        // Given a tenant with rateLimitPerMinute=1500,
        // When PUT /api/tenants/{id}/embedding-config with rateLimitPerMinute=200,
        // Then the next GenerateEmbeddingActivity invocation calls
        // IEmbeddingRateLimiterActor.SetCeilingAsync(200) before TryConsumeAsync.
        _ = _fixture;
        await Task.CompletedTask;
    }

    // AC6 / FR70 — embedding model field propagation (golden path).
    // Task 6.1: this test should be unskipped if any ingestion-path integration fixture runs in CI.
    // Fallback: IngestionWorkflowTests asserts IndexInput.EmbeddingModel is populated from
    // EmbeddingResult.EmbeddingModel (covered at unit level by GenerateEmbeddingActivityTests and
    // IndexSyntacticActivityTests).
    [Fact(Skip = "Requires Aspire AppHost fixture — fall back to unit-level assertions if unavailable")]
    public async Task IngestMemoryUnit_EndToEnd_PersistsEmbeddingProviderAndModel()
    {
        // Given a provisioned tenant using Google provider + gemini-embedding-001,
        // When a memory unit is ingested end-to-end,
        // Then GET /api/tenants/{tid}/cases/{cid}/memory-units/{muid} returns the MU with
        //   embeddingProvider="google:gemini-embedding-001"
        //   embeddingModel="gemini-embedding-001"
        // And Redis hash inspection of {tid}:mu:{muid} shows both "embeddingProvider" and
        // "embeddingModel" fields.
        _ = _fixture;
        await Task.CompletedTask;
    }
}
