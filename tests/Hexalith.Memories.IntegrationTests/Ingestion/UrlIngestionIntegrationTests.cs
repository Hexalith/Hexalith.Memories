// <copyright file="UrlIngestionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class UrlIngestionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public UrlIngestionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-url-ok-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((_, _) =>
            ValueTask.FromResult(ScriptedHttpResponse.Text(
                $"# URL ingestion\n\nSearchable URL canary {unique}.",
                contentType: "text/markdown; charset=utf-8")));
        string sourceUri = source.GetUri($"/documents/{unique}.md").ToString();

        UrlIngestionResponse accepted = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, accepted.InstanceId, "Completed");
        string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? accepted.InstanceId;
        MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            unit => unit.Status == MemoryUnitStatus.Indexed);
        _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);

        using HttpResponseMessage searchResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&caseId={caseId}&axis=syntactic&query={unique}");
        searchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? search = await searchResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);

        accepted.SourceUri.ShouldBe(sourceUri);
        accepted.SourceType.ShouldBe("url");
        indexed.SourceUri.ShouldBe(sourceUri);
        indexed.SourceType.ShouldBe(SourceType.Url);
        indexed.Content.ShouldContain(unique);
        search.ShouldNotBeNull().Results.ShouldContain(result =>
            result.MemoryUnitId == memoryUnitId && result.SourceUri == sourceUri);
        source.RequestCount.ShouldBeGreaterThanOrEqualTo(1);
        source.RequestCount.ShouldBeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task UrlIngestion_404Url_ShouldFailAfterRetries()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-url-404-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((_, _) =>
            ValueTask.FromResult(ScriptedHttpResponse.Text("not found", HttpStatusCode.NotFound)));
        string sourceUri = source.GetUri($"/missing/{unique}.txt").ToString();

        UrlIngestionResponse accepted = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri);
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
        _ = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, accepted.InstanceId, "Failed");

        failed.SourceUri.ShouldBe(sourceUri);
        failed.Stage.ShouldBe("fetching");
        failed.ErrorCode.ShouldBe("URL_CLIENT_ERROR");
        failed.RetryCount.ShouldBeGreaterThan(0);
        failedUnit.FailureDetails!.ErrorCode.ShouldBe("URL_CLIENT_ERROR");
        IDatabase redis = _fixture.RedisConnection.GetDatabase();
        string failedHash = $"{tenantId}:failed-unit:{failed.MemoryUnitId}";
        string failedIndex = $"{tenantId}:case:{caseId}:failed-units";
        (await redis.KeyExistsAsync(failedHash)).ShouldBeTrue();
        (await redis.SortedSetScoreAsync(failedIndex, failed.MemoryUnitId)).ShouldNotBeNull();
        (await driver.ListRedisKeysAsync($"{tenantId}:mu:*")).ShouldBeEmpty();
        (await driver.ListRedisKeysAsync($"{tenantId}:vec:*")).ShouldBeEmpty();
        (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(0);
        source.RequestCount.ShouldBeGreaterThanOrEqualTo(2);
        source.RequestCount.ShouldBeLessThanOrEqualTo(10);
    }

    [Fact(Skip = "26.3-PRIVATE-HOST-FIXTURE: The shared AppHost intentionally enables loopback URL ingestion for deterministic scripted-server coverage, so it cannot prove the production-deny branch. Owner: ingestion maintainers. Unskip when: a second AppHost fixture can override AllowPrivateHosts=false before resource startup.")]
    public void UrlIngestion_PrivateIpWithAllowDisabled_ShouldRejectBeforeScheduling()
    {
        // Scenario (Story 6.1 AC3):
        //   POST /api/v1/ingest/url with http://169.254.169.254/ → 400 INVALID_URL,
        //   and no workflow is scheduled (verify by inspecting the workflow state store).
    }
}
