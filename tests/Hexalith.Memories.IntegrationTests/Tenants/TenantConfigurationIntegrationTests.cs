// <copyright file="TenantConfigurationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;
using Hexalith.Memories.Server.Tenants;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Integration tests for Story 5.5 — tenant configuration &amp; listing (AC1–AC6 / FR41, FR42, FR43, FR45, FR69, FR70).
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantConfigurationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public TenantConfigurationIntegrationTests(AspireIngestionPipelineFixture fixture)
        => _fixture = fixture;

    // AC1 / FR41 — enriched tenant listing.
    [Fact]
    public async Task ListTenants_ReturnsEnrichedSummaryWithCountsAndIndexHealth()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-list-{unique[..10]}";
        string sourceUri = $"file:///{unique}-tenant-list.txt";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string instanceId = await driver.PostInlineIngestionAsync(tenantId, caseId, sourceUri, $"Tenant list canary {unique}.");
        _ = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
        _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);

        TenantSummary summary = await driver.WaitForNewestTenantSummaryAsync(
            tenantId,
            item => item.IndexStatus == new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready));
        TenantRegistryEntry registry = (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldNotBeNull();

        summary.DisplayName.ShouldBe(registry.Tenant.DisplayName);
        summary.Status.ShouldBe(TenantStatus.Active);
        summary.MemoryUnitCount.ShouldBe(1);
        summary.IndexSizes.SyntacticKeyCount.ShouldBe(1);
        summary.IndexSizes.SemanticKeyCount.ShouldBe(1);
        (summary.IndexSizes.GraphNodeCount ?? 0).ShouldBeGreaterThanOrEqualTo(1);
        summary.IndexStatus.ShouldBe(new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready));
        summary.ReindexRequired.ShouldBeFalse();
        summary.LastActivityAt.ShouldNotBeNull();
    }

    // AC2 / FR45 — tenant configuration view.
    [Fact]
    public async Task GetConfiguration_ReturnsComposedView_WithFullEmbeddingConfig()
    {
        string tenantId = $"tenant-config-{Guid.NewGuid():N}";
        await _fixture.ProvisionActiveTenantAsync(tenantId, vectorDimensions: 768);
        TenantEmbeddingConfig actorConfig = await _fixture.CreateTenantConfigurationActorProxy(tenantId).GetEmbeddingConfigAsync();

        TenantConfigurationView view = await GetJsonAsync<TenantConfigurationView>(
            $"/api/v1/tenants/{tenantId}/configuration");
        TenantRegistryEntry registry = (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldNotBeNull();

        view.Id.ShouldBe(tenantId);
        view.DisplayName.ShouldBe(registry.Tenant.DisplayName);
        view.CreatedAt.ShouldBe(registry.Tenant.CreatedAt);
        view.Status.ShouldBe(TenantStatus.Active);
        view.EmbeddingConfig.ShouldBe(actorConfig);
        view.EmbeddingConfig.ApiSecretKeyName.ShouldNotBeNullOrWhiteSpace();
        view.IndexStatus.ShouldBe(new TenantIndexStatus(IndexHealth.Ready, IndexHealth.Ready, IndexHealth.Ready));
        view.MemoryUnitCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetConfiguration_UnknownTenant_Returns404TenantNotFound()
    {
        string tenantId = $"tenant-missing-{Guid.NewGuid():N}";

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/configuration");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse error = await ReadJsonAsync<ErrorResponse>(response);
        error.Code.ShouldBe("TENANT_NOT_FOUND");
        (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldBeNull();
    }

    // AC3 / FR42 — PATCH display name.
    [Fact]
    public async Task PatchDisplayName_UpdatesRegistryAndReflectsInSubsequentGet()
    {
        string tenantId = $"tenant-rename-{Guid.NewGuid():N}";
        string oldName = $"Old {tenantId}";
        string newName = $"New {tenantId}";
        await _fixture.ProvisionActiveTenantAsync(tenantId, oldName);
        int logStart = _fixture.LogEntryCount;

        using HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/tenants/{tenantId}")
        {
            Content = JsonContent.Create(new TenantUpdateInput(newName), options: MemoriesJsonContext.Options),
        };
        using HttpResponseMessage response = await _fixture.MemoriesClient.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantSummary patched = await ReadJsonAsync<TenantSummary>(response);
        TenantInfo subsequent = await GetJsonAsync<TenantInfo>($"/api/v1/tenants/{tenantId}");
        TenantRegistryEntry registry = (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldNotBeNull();

        patched.DisplayName.ShouldBe(newName);
        subsequent.DisplayName.ShouldBe(newName);
        registry.Tenant.DisplayName.ShouldBe(newName);
        string operationalLog = await WaitForOperationalLogAsync(logStart, tenantId);
        operationalLog.ShouldContain(tenantId);
        operationalLog.ShouldContain("field=displayName");
        operationalLog.ShouldContain($"oldValue={oldName}");
        operationalLog.ShouldContain($"newValue={newName}");
        operationalLog.ShouldContain("actor=operator@");
        Match duration = Regex.Match(operationalLog, @"durationMs=(?<value>\d+)");
        duration.Success.ShouldBeTrue();
        int.Parse(duration.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task PatchDisplayName_NonActiveTenant_Returns409()
    {
        string tenantId = $"tenant-provisioning-{Guid.NewGuid():N}";
        await _fixture.SeedTenantRegistryEntryAsync(tenantId, TenantStatus.Provisioning, $"provision-{tenantId}-seed");
        TenantRegistryEntry before = (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldNotBeNull();

        using HttpRequestMessage request = new(HttpMethod.Patch, $"/api/v1/tenants/{tenantId}")
        {
            Content = JsonContent.Create(new TenantUpdateInput("Must not persist"), options: MemoriesJsonContext.Options),
        };
        using HttpResponseMessage response = await _fixture.MemoriesClient.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse error = await ReadJsonAsync<ErrorResponse>(response);
        TenantRegistryEntry after = (await _fixture.GetTenantRegistryEntryAsync(tenantId)).ShouldNotBeNull();

        error.Code.ShouldBe("TENANT_PROVISIONING");
        after.ShouldBe(before);
    }

    // AC4 / FR43 — embedding config breaking-change flow (existing PUT /embedding-config).
    [Fact]
    public async Task PutEmbeddingConfig_BreakingChange_WithoutForceReindex_Returns409()
    {
        string tenantId = $"tenant-config-reject-{Guid.NewGuid():N}";
        await _fixture.ProvisionActiveTenantAsync(tenantId, vectorDimensions: 768);
        ITenantConfigurationActor actor = _fixture.CreateTenantConfigurationActorProxy(tenantId);
        TenantEmbeddingConfig before = await actor.GetEmbeddingConfigAsync();
        await PutConfigAsync(tenantId, before, forceReindex: false);
        TenantEmbeddingConfig proposed = before with { Dimensions = 1536 };

        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config",
            proposed,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        using JsonDocument conflict = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        conflict.RootElement.GetProperty("error").GetString().ShouldBe("EmbeddingConfigChangeRequired");
        conflict.RootElement.GetProperty("affectedFields").EnumerateArray()
            .Select(field => field.GetString()).ShouldContain("dimensions");

        (await actor.GetEmbeddingConfigAsync()).ShouldBe(before);
        TenantConfigurationView view = await GetJsonAsync<TenantConfigurationView>(
            $"/api/v1/tenants/{tenantId}/configuration");
        view.EmbeddingConfig.ShouldBe(before);
        view.EmbeddingConfig.ReindexRequired.ShouldBeFalse();
    }

    [Fact]
    public async Task PutEmbeddingConfig_BreakingChange_WithForceReindex_Returns200AndSetsReindexRequired()
    {
        string tenantId = $"tenant-config-force-{Guid.NewGuid():N}";
        await _fixture.ProvisionActiveTenantAsync(tenantId, vectorDimensions: 768);
        ITenantConfigurationActor actor = _fixture.CreateTenantConfigurationActorProxy(tenantId);
        TenantEmbeddingConfig before = await actor.GetEmbeddingConfigAsync();
        await PutConfigAsync(tenantId, before, forceReindex: false);
        TenantEmbeddingConfig proposed = before with { Dimensions = 1536 };

        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config?forceReindex=true",
            proposed,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        TenantEmbeddingConfig updated = await ReadJsonAsync<TenantEmbeddingConfig>(response);
        TenantEmbeddingConfig persisted = await actor.GetEmbeddingConfigAsync();
        TenantConfigurationView view = await GetJsonAsync<TenantConfigurationView>(
            $"/api/v1/tenants/{tenantId}/configuration");

        updated.Dimensions.ShouldBe(1536);
        updated.ReindexRequired.ShouldBeTrue();
        persisted.ShouldBe(updated);
        view.EmbeddingConfig.ShouldBe(updated);
    }

    // AC5 / FR69 + Story 23.5 AC4 — rate-limit propagation within the embedding-config cache freshness bound.
    [Fact]
    public async Task PutEmbeddingConfig_RateLimitChange_PropagatesToRateLimiterOnNextIngest()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-limit-a-{unique[..10]}";
        string otherTenantId = $"tenant-limit-b-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string otherCaseId = await driver.CreateTenantAndCaseAsync(otherTenantId);
        ITenantConfigurationActor configActor = _fixture.CreateTenantConfigurationActorProxy(tenantId);
        ITenantConfigurationActor otherConfigActor = _fixture.CreateTenantConfigurationActorProxy(otherTenantId);
        TenantEmbeddingConfig before = await configActor.GetEmbeddingConfigAsync();
        TenantEmbeddingConfig otherBefore = await otherConfigActor.GetEmbeddingConfigAsync();
        TenantEmbeddingConfig proposed = before with { RateLimitPerMinute = 200 };

        using HttpResponseMessage update = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config",
            proposed,
            MemoriesJsonContext.Options);
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        string sourceUri = $"file:///{unique}-rate-a.txt";
        string otherSourceUri = $"file:///{unique}-rate-b.txt";
        string workflow = await driver.PostInlineIngestionAsync(tenantId, caseId, sourceUri, $"Rate limit A {unique}.");
        string otherWorkflow = await driver.PostInlineIngestionAsync(otherTenantId, otherCaseId, otherSourceUri, $"Rate limit B {unique}.");
        _ = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, workflow, "Completed");
        _ = await driver.WaitForWorkflowRuntimeStatusAsync(otherTenantId, otherWorkflow, "Completed");
        _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);
        _ = await driver.WaitForSingleBackendWriteAsync(otherTenantId, otherCaseId, otherSourceUri);

        RateLimitState state = await _fixture.CreateEmbeddingRateLimiterActorProxy(tenantId).GetStateAsync();
        RateLimitState otherState = await _fixture.CreateEmbeddingRateLimiterActorProxy(otherTenantId).GetStateAsync();
        state.CeilingPerMinute.ShouldBe(200);
        state.Remaining.ShouldBeLessThan(200);
        otherState.CeilingPerMinute.ShouldBe(otherBefore.RateLimitPerMinute);
        (await configActor.GetEmbeddingConfigAsync()).RateLimitPerMinute.ShouldBe(200);
        (await otherConfigActor.GetEmbeddingConfigAsync()).ShouldBe(otherBefore);
    }

    // AC6 / FR70 — embedding model field propagation (golden path).
    // Task 6.1: this test should be unskipped if any ingestion-path integration fixture runs in CI.
    // Fallback: IngestionWorkflowTests asserts IndexInput.EmbeddingModel is populated from
    // EmbeddingResult.EmbeddingModel (covered at unit level by GenerateEmbeddingActivityTests and
    // IndexSyntacticActivityTests).
    [Fact]
    public async Task IngestMemoryUnit_EndToEnd_PersistsEmbeddingProviderAndModel()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-provenance-{unique[..10]}";
        string sourceUri = $"file:///{unique}-provenance.txt";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        TenantEmbeddingConfig config = await _fixture.CreateTenantConfigurationActorProxy(tenantId).GetEmbeddingConfigAsync();
        string instanceId = await driver.PostInlineIngestionAsync(tenantId, caseId, sourceUri, $"Provenance canary {unique}.");
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
        string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId;
        MemoryUnit memory = await driver.WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            unit => unit.Status == MemoryUnitStatus.Indexed);
        _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);

        string expectedProvider = $"{config.Provider}:{config.Model}";
        memory.EmbeddingProvider.ShouldBe(expectedProvider);
        memory.EmbeddingModel.ShouldBe(config.Model);
        IDatabase redis = _fixture.RedisConnection.GetDatabase();
        RedisValue[] values = await redis.HashGetAsync(
            $"{tenantId}:mu:{memoryUnitId}",
            ["embeddingProvider", "embeddingModel"]);
        values[0].ToString().ShouldBe(expectedProvider);
        values[1].ToString().ShouldBe(config.Model);
    }

    private async Task<T> GetJsonAsync<T>(string path)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadJsonAsync<T>(response);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<T>(MemoriesJsonContext.Options)
            ?? throw new InvalidOperationException($"Response {(int)response.StatusCode} contained no JSON body.");

    private async Task<TenantEmbeddingConfig> PutConfigAsync(
        string tenantId,
        TenantEmbeddingConfig config,
        bool forceReindex)
    {
        string suffix = forceReindex ? "?forceReindex=true" : string.Empty;
        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config{suffix}",
            config,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadJsonAsync<TenantEmbeddingConfig>(response);
    }

    private async Task<string> WaitForOperationalLogAsync(int logStart, string tenantId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        string captured = string.Empty;
        while (!timeout.IsCancellationRequested)
        {
            captured = string.Join(
                Environment.NewLine,
                _fixture.GetLogEntriesSince(logStart).Select(entry => entry.Message));
            if (captured.Contains(tenantId, StringComparison.Ordinal)
                && captured.Contains("field=displayName", StringComparison.Ordinal))
            {
                return captured;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        return captured;
    }
}
