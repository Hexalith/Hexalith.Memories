// <copyright file="RateLimitingIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;

using Shouldly;

/// <summary>
/// Integration coverage for per-tenant rate limiting and starvation prevention.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RateLimitingIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public RateLimitingIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TwoTenantIsolation_ShouldEnforceIndependentCeilings()
    {
        string unique = Guid.NewGuid().ToString("N");
        string firstTenant = $"tenant-limit-a-{unique[..10]}";
        string secondTenant = $"tenant-limit-b-{unique[..10]}";
        _ = await _fixture.ProvisionActiveTenantAsync(firstTenant).ConfigureAwait(true);
        _ = await _fixture.ProvisionActiveTenantAsync(secondTenant).ConfigureAwait(true);

        TenantEmbeddingConfig firstConfig = await GetEmbeddingConfigAsync(firstTenant).ConfigureAwait(true);
        TenantEmbeddingConfig secondConfig = await GetEmbeddingConfigAsync(secondTenant).ConfigureAwait(true);
        await PutEmbeddingConfigAsync(firstTenant, firstConfig with { RateLimitPerMinute = 2 }).ConfigureAwait(true);
        await PutEmbeddingConfigAsync(secondTenant, secondConfig with { RateLimitPerMinute = 4 }).ConfigureAwait(true);

        IEmbeddingRateLimiterActor firstActor = _fixture.CreateEmbeddingRateLimiterActorProxy(firstTenant);
        IEmbeddingRateLimiterActor secondActor = _fixture.CreateEmbeddingRateLimiterActorProxy(secondTenant);
        await firstActor.ResetAsync().ConfigureAwait(true);
        await secondActor.ResetAsync().ConfigureAwait(true);

        (await firstActor.TryConsumeWithCeilingAsync(2).ConfigureAwait(true)).ShouldBeTrue();
        (await firstActor.TryConsumeWithCeilingAsync(2).ConfigureAwait(true)).ShouldBeTrue();
        (await firstActor.TryConsumeWithCeilingAsync(2).ConfigureAwait(true)).ShouldBeFalse();
        (await secondActor.TryConsumeWithCeilingAsync(4).ConfigureAwait(true)).ShouldBeTrue();

        RateLimitState firstState = await firstActor.GetStateAsync().ConfigureAwait(true);
        RateLimitState secondState = await secondActor.GetStateAsync().ConfigureAwait(true);
        TenantEmbeddingConfig persistedFirst = await GetEmbeddingConfigAsync(firstTenant).ConfigureAwait(true);
        TenantEmbeddingConfig persistedSecond = await GetEmbeddingConfigAsync(secondTenant).ConfigureAwait(true);

        persistedFirst.RateLimitPerMinute.ShouldBe(2);
        persistedSecond.RateLimitPerMinute.ShouldBe(4);
        firstState.CeilingPerMinute.ShouldBe(2);
        firstState.Remaining.ShouldBe(0);
        secondState.CeilingPerMinute.ShouldBe(4);
        secondState.Remaining.ShouldBe(3);
        (await secondActor.TryConsumeWithCeilingAsync(4).ConfigureAwait(true)).ShouldBeTrue();
        (await firstActor.GetStateAsync().ConfigureAwait(true)).ShouldBe(firstState);
    }

    [Fact(Skip = "26.3-BATCH-STARVATION-PERF: Starvation prevention is a comparative latency claim that cannot be made deterministically by the functional Aspire lane. Owner: performance test maintainers. Unskip when: the performance lane can submit a 500-file batch and enforce an accepted cross-tenant latency budget.")]
    public void BatchVsSingleIngest_ShouldNotStarveRealTimeTenant()
    {
        // AC2: t1 submits 500-file batch, t2 submits single file within 1 s.
        // Assert t2 P50 latency stays within 2× single-tenant baseline.
    }

    private async Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync(string tenantId)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config").ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<TenantEmbeddingConfig>(MemoriesJsonContext.Options).ConfigureAwait(true)
            ?? throw new InvalidOperationException("Tenant embedding configuration response was empty.");
    }

    private async Task PutEmbeddingConfigAsync(string tenantId, TenantEmbeddingConfig config)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config",
            config,
            MemoriesJsonContext.Options).ConfigureAwait(true);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

}
