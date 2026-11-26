// <copyright file="EmbeddingProviderFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Ingestion;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>Story 26.3 and DW-564 provider boundary tests for ingestion retries and hybrid search degradation.</summary>
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

    [Fact]
    public async Task HybridSearch_WhenQueryEmbeddingFails_DegradesOnlySemanticAndRecoversAsync()
    {
        AspireIngestionPipelineFixture fixture = _fixture!;
        OllamaOidcFakeServer fake = _fakeServer!;
        IngestionIntegrationTestDriver driver = new(fixture);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-search-fault-{unique[..10]}";
        string sourceUri = $"file:///{unique}-semantic-fault.txt";
        string faultQuery = $"semanticfault{unique}";
        string controlQuery = $"{faultQuery}control";
        string caseId = await driver.CreateTenantAndCaseAsync(
            tenantId,
            OllamaOidcFakeServer.OllamaDimensions,
            cancellationToken);
        await ConfigureOllamaTenantAsync(fixture, fake, tenantId);
        string instanceId = await driver.PostInlineIngestionAsync(
            tenantId, caseId, sourceUri, $"{faultQuery} {controlQuery}", cancellationToken);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(
            tenantId, instanceId, "Completed", cancellationToken: cancellationToken);
        string memoryUnitId = JsonSerializer.Deserialize<IngestionWorkflowStatus>(workflow, MemoriesJsonContext.Options)
            .ShouldNotBeNull().MemoryUnitId.ShouldNotBeNull();
        (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(
            tenantId, caseId, sourceUri, cancellationToken: cancellationToken);

        HashEntry[] syntacticBefore = await driver.ReadHashSnapshotAsync(syntacticKey);
        HashEntry[] semanticBefore = await driver.ReadHashSnapshotAsync(semanticKey);
        string graphContentBefore = await ReadStoredGraphContentAsync(fixture, tenantId, caseId, memoryUnitId, sourceUri);
        graphContentBefore.ShouldContain(faultQuery);
        AssertHealthyHybridSearch(
            await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, faultQuery, cancellationToken),
            memoryUnitId);

        EmbeddingProviderFaultPlan faultPlan = new(
            HttpStatusCode.InternalServerError, failureCount: 1, requiredInput: faultQuery);
        fake.SetEmbedFaultPlan(faultPlan);
        try
        {
            // The control input contains the fault input and must leave the exact-match fault armed.
            AssertHealthyHybridSearch(
                await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, controlQuery, cancellationToken),
                memoryUnitId);
            faultPlan.RemainingFailureCount.ShouldBe(1);

            HybridSearchResult degraded = await driver.GetHybridSearchAsync(
                tenantId, caseId, memoryUnitId, faultQuery, cancellationToken);
            faultPlan.RemainingFailureCount.ShouldBe(0);
            degraded.Degraded.ShouldBeTrue();
            degraded.UnavailableAxes.ShouldBe(["semantic"]);
            FusedScoredResult hit = degraded.Results.Single();
            hit.MemoryUnitId.ShouldBe(memoryUnitId);
            hit.SourceUri.ShouldBe(sourceUri);
            hit.ContentSnippet.ShouldContain(faultQuery);
            hit.SyntacticScore.ShouldNotBeNull();
            hit.GraphScore.ShouldNotBeNull();
            hit.SemanticScore.ShouldBeNull();

            (await driver.ReadHashSnapshotAsync(syntacticKey)).ShouldBe(syntacticBefore);
            (await driver.ReadHashSnapshotAsync(semanticKey)).ShouldBe(semanticBefore);
            (await ReadStoredGraphContentAsync(fixture, tenantId, caseId, memoryUnitId, sourceUri))
                .ShouldBe(graphContentBefore);
            using HttpResponseMessage unitResponse = await fixture.MemoriesClient.GetAsync(
                $"/api/v1/tenants/{tenantId}/cases/{caseId}/memory-units/{memoryUnitId}", cancellationToken);
            unitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            MemoryUnit? indexedUnit = await unitResponse.Content.ReadFromJsonAsync<MemoryUnit>(
                MemoriesJsonContext.Options, cancellationToken);
            indexedUnit.ShouldNotBeNull();
            indexedUnit.Status.ShouldBe(MemoryUnitStatus.Indexed);
            indexedUnit.SourceUri.ShouldBe(sourceUri);

            AssertHealthyHybridSearch(
                await driver.GetHybridSearchAsync(tenantId, caseId, memoryUnitId, faultQuery, cancellationToken),
                memoryUnitId);
        }
        finally
        {
            fake.ClearEmbedFaultPlan();
        }
    }

    private static void AssertHealthyHybridSearch(HybridSearchResult response, string memoryUnitId)
    {
        response.Degraded.ShouldBeFalse();
        response.UnavailableAxes.ShouldBeEmpty();
        FusedScoredResult hit = response.Results.Single();
        hit.MemoryUnitId.ShouldBe(memoryUnitId);
        hit.SyntacticScore.ShouldNotBeNull();
        hit.SemanticScore.ShouldNotBeNull();
        hit.GraphScore.ShouldNotBeNull();
    }

    private static async Task<string> ReadStoredGraphContentAsync(
        AspireIngestionPipelineFixture fixture,
        string tenantId,
        string caseId,
        string memoryUnitId,
        string sourceUri)
    {
        FalkorDB falkor = new(fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.SelectGraph(tenantId).QueryAsync(
            """
            MATCH (:Case {id: $caseId})-[:CONTAINS]->(unit:MemoryUnit {id: $memoryUnitId, caseId: $caseId, sourceUri: $sourceUri})
            RETURN unit.content AS content
            """,
            new Dictionary<string, object>
            {
                ["caseId"] = caseId,
                ["memoryUnitId"] = memoryUnitId,
                ["sourceUri"] = sourceUri,
            });
        result.Count.ShouldBe(1);
        using IEnumerator<Record> rows = result.GetEnumerator();
        rows.MoveNext().ShouldBeTrue();
        return rows.Current.GetValue<string>("content");
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
