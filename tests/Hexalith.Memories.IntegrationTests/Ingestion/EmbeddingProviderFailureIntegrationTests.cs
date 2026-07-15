// <copyright file="EmbeddingProviderFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 26.3 real-provider boundary proofs for embedding 429 and 5xx failure behavior.</summary>
[Collection("OllamaAspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class EmbeddingProviderFailureIntegrationTests : IAsyncLifetime
{
    private readonly string _clientSecret = $"example-{Guid.NewGuid():N}";
    private AspireIngestionPipelineFixture? _fixture;
    private OllamaOidcFakeServer? _fakeServer;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _fakeServer = await OllamaOidcFakeServer.StartAsync(_clientSecret);
        try
        {
            _fixture = new AspireIngestionPipelineFixture(
                EmbeddingProviderTestMode.OllamaOidcFake,
                new EmbeddingProviderSecret(OllamaOidcFakeServer.SecretName, _clientSecret));
            await _fixture.InitializeAsync();
        }
        catch
        {
            await _fakeServer.DisposeAsync();
            _fakeServer = null;
            _fixture = null;
            throw;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _fakeServer?.ClearEmbedFaultPlan();
        try
        {
            if (_fixture is not null)
            {
                await _fixture.DisposeAsync();
            }
        }
        finally
        {
            if (_fakeServer is not null)
            {
                await _fakeServer.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task TransientIngestionFailure_ShouldCompleteSuccessfullyAfterRetries()
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        OllamaOidcFakeServer fake = _fakeServer!;
        IngestionIntegrationTestDriver driver = new(fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-retry-{unique[..10]}";
        string sourceUri = $"file:///{unique}-provider-retry.txt";
        string caseId = await driver.CreateTenantAndCaseAsync(
            tenantId,
            OllamaOidcFakeServer.OllamaDimensions);
        await ConfigureOllamaTenantAsync(fixture, fake, tenantId);
        fake.SetEmbedFaultPlan(new EmbeddingProviderFaultPlan(HttpStatusCode.InternalServerError, failureCount: 3));

        try
        {
            string instanceId = await driver.PostInlineIngestionAsync(
                tenantId,
                caseId,
                sourceUri,
                $"Durable embedding retry canary {unique}.");
            string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
            string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId;
            MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
                tenantId,
                caseId,
                memoryUnitId,
                unit => unit.Status == MemoryUnitStatus.Indexed);
            FailedUnitsPage failed = await driver.WaitForFailedUnitsPageAsync(
                tenantId,
                caseId,
                page => page.TotalCount == 0);
            (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(
                tenantId,
                caseId,
                sourceUri);

            indexed.SourceUri.ShouldBe(sourceUri);
            failed.Units.ShouldBeEmpty();
            fake.EmbedAttemptCount.ShouldBeGreaterThanOrEqualTo(4);
            fake.EmbedAttemptCount.ShouldBeLessThanOrEqualTo(10);
            (await driver.ListRedisKeysAsync($"{tenantId}:mu:*")).ShouldBe([syntacticKey]);
            (await driver.ListRedisKeysAsync($"{tenantId}:vec:*")).ShouldBe([semanticKey]);
        }
        finally
        {
            fake.ClearEmbedFaultPlan();
        }
    }

    [Fact]
    public async Task Provider500_ExhaustsRetriesAndPersistsFailedUnit()
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        OllamaOidcFakeServer fake = _fakeServer!;
        IngestionIntegrationTestDriver driver = new(fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-provider500-{unique[..10]}";
        string sourceUri = $"file:///{unique}-provider500.txt";
        string caseId = await driver.CreateTenantAndCaseAsync(
            tenantId,
            OllamaOidcFakeServer.OllamaDimensions);
        await ConfigureOllamaTenantAsync(fixture, fake, tenantId);
        fake.SetEmbedFaultPlan(new EmbeddingProviderFaultPlan(HttpStatusCode.InternalServerError, failureCount: 20));

        try
        {
            string instanceId = await driver.PostInlineIngestionAsync(
                tenantId,
                caseId,
                sourceUri,
                $"Provider exhaustion canary {unique}.");
            FailedUnitsPage failedPage = await driver.WaitForFailedUnitsPageAsync(
                tenantId,
                caseId,
                page => page.TotalCount == 1);
            FailedUnitSummary failed = failedPage.Units.Single();
            MemoryUnit failedUnit = await driver.WaitForMemoryUnitAsync(
                tenantId,
                caseId,
                failed.MemoryUnitId,
                unit => unit.Status == MemoryUnitStatus.Failed && unit.FailureDetails is not null);
            string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Failed");

            failed.SourceUri.ShouldBe(sourceUri);
            failed.Stage.ShouldBe("embedding");
            failed.RetryCount.ShouldBeGreaterThan(0);
            failedUnit.Id.ShouldBe(failed.MemoryUnitId);
            failedUnit.FailureDetails!.Stage.ShouldBe("embedding");
            (IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId).ShouldBe(failed.MemoryUnitId);

            IDatabase redis = fixture.RedisConnection.GetDatabase();
            string failedHash = $"{tenantId}:failed-unit:{failed.MemoryUnitId}";
            string failedIndex = $"{tenantId}:case:{caseId}:failed-units";
            (await redis.KeyExistsAsync(failedHash)).ShouldBeTrue();
            (await redis.SortedSetScoreAsync(failedIndex, failed.MemoryUnitId)).ShouldNotBeNull();
            StreamEntry[] activity = await redis.StreamRangeAsync($"{tenantId}:case:{caseId}:activity");
            activity.Count(entry =>
                entry.Values.Any(value => value.Name == "type" && value.Value == "ingestionFailed") &&
                entry.Values.Any(value => value.Name == "memoryUnitId" && value.Value == failed.MemoryUnitId)).ShouldBe(1);
            (await driver.ListRedisKeysAsync($"{tenantId}:mu:*")).ShouldBeEmpty();
            (await driver.ListRedisKeysAsync($"{tenantId}:vec:*")).ShouldBeEmpty();
            (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(0);
            fake.EmbedAttemptCount.ShouldBeGreaterThanOrEqualTo(2);
            fake.EmbedAttemptCount.ShouldBeLessThanOrEqualTo(20);
        }
        finally
        {
            fake.ClearEmbedFaultPlan();
        }
    }

    [Fact]
    public async Task Provider429_ShouldReportToActorAndRetry()
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        OllamaOidcFakeServer fake = _fakeServer!;
        IngestionIntegrationTestDriver driver = new(fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-provider429-{unique[..10]}";
        string sourceUri = $"file:///{unique}-provider429.txt";
        string caseId = await driver.CreateTenantAndCaseAsync(
            tenantId,
            OllamaOidcFakeServer.OllamaDimensions);
        await ConfigureOllamaTenantAsync(fixture, fake, tenantId);
        fake.SetEmbedFaultPlan(new EmbeddingProviderFaultPlan(
            HttpStatusCode.TooManyRequests,
            failureCount: 1,
            retryAfter: TimeSpan.FromSeconds(3)));

        try
        {
            DateTime requestStartedAt = DateTime.UtcNow;
            string instanceId = await driver.PostInlineIngestionAsync(
                tenantId,
                caseId,
                sourceUri,
                $"Provider rate limit recovery canary {unique}.");
            IEmbeddingRateLimiterActor limiter = fixture.CreateEmbeddingRateLimiterActorProxy(tenantId);
            RateLimitState paused = await WaitForRateLimiterStateAsync(
                limiter,
                state => fake.EmbedAttemptCount >= 1 && state.Remaining == 0);
            string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
            string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId;
            MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
                tenantId,
                caseId,
                memoryUnitId,
                unit => unit.Status == MemoryUnitStatus.Indexed);
            FailedUnitsPage failed = await driver.WaitForFailedUnitsPageAsync(
                tenantId,
                caseId,
                page => page.TotalCount == 0);
            _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);
            RateLimitState converged = await limiter.GetStateAsync();

            paused.CeilingPerMinute.ShouldBe(6000);
            DateTime retryWindowOpensAt = paused.WindowStart.AddMinutes(1);
            retryWindowOpensAt.ShouldBeGreaterThanOrEqualTo(requestStartedAt.AddSeconds(2));
            retryWindowOpensAt.ShouldBeLessThan(requestStartedAt.AddSeconds(15));
            converged.CeilingPerMinute.ShouldBe(6000);
            converged.Remaining.ShouldBeLessThan(converged.CeilingPerMinute);
            indexed.SourceUri.ShouldBe(sourceUri);
            failed.Units.ShouldBeEmpty();
            fake.EmbedAttemptCount.ShouldBeGreaterThanOrEqualTo(2);
            fake.EmbedAttemptCount.ShouldBeLessThanOrEqualTo(10);
        }
        finally
        {
            fake.ClearEmbedFaultPlan();
        }
    }

    private static async Task ConfigureOllamaTenantAsync(
        AspireIngestionPipelineFixture fixture,
        OllamaOidcFakeServer fake,
        string tenantId)
    {
        TenantEmbeddingConfig config = new()
        {
            Provider = EmbeddingProviderDefaults.OllamaProviderName,
            Model = OllamaOidcFakeServer.DefaultModel,
            Dimensions = OllamaOidcFakeServer.OllamaDimensions,
            RateLimitPerMinute = 6000,
            ApiSecretKeyName = OllamaOidcFakeServer.SecretName,
            BaseUrl = fake.OllamaBaseUrl.ToString(),
            AuthMode = EmbeddingProviderDefaults.OidcClientCredentialsAuthMode,
            OidcTokenEndpoint = fake.OidcTokenEndpoint.ToString(),
            OidcClientId = OllamaOidcFakeServer.ClientId,
            OidcScope = OllamaOidcFakeServer.Scope,
        };
        using HttpResponseMessage response = await fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/embedding-config?forceReindex=true",
            config,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<RateLimitState> WaitForRateLimiterStateAsync(
        IEmbeddingRateLimiterActor actor,
        Func<RateLimitState, bool> predicate)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(2));
        while (!cts.IsCancellationRequested)
        {
            RateLimitState state = await actor.GetStateAsync();
            if (predicate(state))
            {
                return state;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        }

        throw new TimeoutException("The tenant rate-limiter actor did not expose the expected persisted state.");
    }
}
