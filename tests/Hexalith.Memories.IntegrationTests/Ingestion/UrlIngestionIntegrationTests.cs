// <copyright file="UrlIngestionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class UrlIngestionIntegrationTests
{
    private const string PathSecret = "synthetic-signed-path";
    private const string QuerySecret = "synthetic-signed-query";
    private readonly AspireIngestionPipelineFixture _fixture;

    public UrlIngestionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UrlIngestion_SmallTextPage_ShouldCompleteAndBeSearchable()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-url-ok-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string searchUri = $"/api/v1/search?tenantId={tenantId}&caseId={caseId}&axis=syntactic&query={unique}";
        EvidencePacketScope scope = new(tenantId, caseId, EvidencePacketIsolationStatus.Authorized, "tenant-case");
        using (HttpResponseMessage beforeIngest = await _fixture.MemoriesClient.GetAsync(searchUri))
        {
            beforeIngest.StatusCode.ShouldBe(HttpStatusCode.OK);
            SearchResult empty = (await beforeIngest.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options)).ShouldNotBeNull();
            empty.HasIndexedMemoryUnits.ShouldBeFalse();
            empty.Results.ShouldBeEmpty();
            EvidencePacket evidence = EvidencePacketMapper.FromSearchResult(empty, scope);
            evidence.Recovery.ShouldHaveSingleItem().Guidance.ShouldContain("Ingest and index content");
        }

        int logStart = _fixture.LogEntryCount;
        ConcurrentQueue<string> requests = new();
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((request, _) =>
        {
            requests.Enqueue($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
            return ValueTask.FromResult(ScriptedHttpResponse.Text(
                $"# URL ingestion\n\nSearchable URL canary {unique}.", contentType: "text/markdown; charset=utf-8"));
        });
        string sourceUri = source.GetUri($"/documents/{PathSecret}/{unique}.md?sig={QuerySecret}&version=2").AbsoluteUri;

        UrlIngestionResponse accepted = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, accepted.InstanceId, "Completed");
        string memoryUnitId = JsonSerializer.Deserialize<IngestionWorkflowStatus>(workflow, MemoriesJsonContext.Options)!
            .MemoryUnitId.ShouldNotBeNull();
        UrlIngestionResponse retry = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri);
        string retried = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, retry.InstanceId, "Completed");
        JsonSerializer.Deserialize<IngestionWorkflowStatus>(retried, MemoriesJsonContext.Options)!.MemoryUnitId.ShouldBe(memoryUnitId);
        requests.ToArray().ShouldBe([sourceUri]);
        MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
            tenantId,
            caseId,
            memoryUnitId,
            unit => unit.Status == MemoryUnitStatus.Indexed);
        _ = await driver.WaitForSingleBackendWriteAsync(tenantId, caseId, sourceUri);

        CheckIdempotencyActivity dedup = new(_fixture.RedisConnection);
        IdempotencyInput identity = new(sourceUri, tenantId, caseId);
        (await dedup.RunAsync(null!, identity)).ExistingMemoryUnitId.ShouldBe(memoryUnitId);
        foreach (IdempotencyInput distinct in new[]
        {
            identity with { TenantId = tenantId + "-other" },
            identity with { CaseId = caseId + "-other" },
            identity with { SourceUri = sourceUri.Replace(PathSecret, "other-path", StringComparison.Ordinal) },
            identity with { SourceUri = sourceUri.Replace(QuerySecret, "other-signature", StringComparison.Ordinal) },
        })
        {
            (await dedup.RunAsync(null!, distinct)).IsDuplicate.ShouldBeFalse();
        }

        using HttpResponseMessage searchResponse = await _fixture.MemoriesClient.GetAsync(searchUri);
        searchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? search = await searchResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);

        accepted.SourceUri.ShouldBe(sourceUri);
        accepted.SourceType.ShouldBe("url");
        indexed.SourceUri.ShouldBe(sourceUri);
        indexed.SourceType.ShouldBe(SourceType.Url);
        indexed.Content.ShouldContain(unique);
        search.ShouldNotBeNull().Results.ShouldContain(result =>
            result.MemoryUnitId == memoryUnitId && result.SourceUri == sourceUri);
        search.HasIndexedMemoryUnits.ShouldBeTrue();
        EvidencePacketMapper.FromSearchResult(search, scope).Recovery
            .ShouldNotContain(action => action.Guidance.StartsWith("Ingest and index", StringComparison.Ordinal));
        await AssertActivityAsync(tenantId, caseId, memoryUnitId, CaseActivityEventType.MemoryUnitIngested);
        await AssertWorkflowLogsAsync(logStart, $"Duplicate detected in tenant {tenantId}, existing unit {memoryUnitId}");
    }

    [Fact]
    public async Task UrlIngestion_404Url_ShouldFailAfterRetries()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-url-404-{unique[..10]}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        int logStart = _fixture.LogEntryCount;
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((_, _) =>
            ValueTask.FromResult(ScriptedHttpResponse.Text("not found", HttpStatusCode.NotFound)));
        string sourceUri = source.GetUri($"/missing/{PathSecret}/{unique}.txt?sig={QuerySecret}").AbsoluteUri;

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
        await AssertActivityAsync(tenantId, caseId, failed.MemoryUnitId, CaseActivityEventType.IngestionFailed);
        await AssertWorkflowLogsAsync(logStart, $"Ingestion failed for {failed.MemoryUnitId}.");
    }

    private async Task AssertWorkflowLogsAsync(int start, string expected)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        string logs = string.Empty;
        try
        {
            while (true)
            {
                logs = string.Join("\n", _fixture.GetLogEntriesSince(start).Select(log => log.Message));
                if (logs.Contains(expected, StringComparison.Ordinal))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Aspire forwards child-process stdout asynchronously. After the wait budget,
            // assert a fresh snapshot so a missing duplicate/failure line still fails closed.
            logs = string.Join("\n", _fixture.GetLogEntriesSince(start).Select(log => log.Message));
        }

        logs.ShouldContain(expected);
        AssertNoSecrets(logs);
    }

    private async Task AssertActivityAsync(string tenant, string caseId, string unitId, CaseActivityEventType eventType)
    {
        StreamEntry[] entries = await _fixture.RedisConnection.GetDatabase().StreamRangeAsync($"{tenant}:case:{caseId}:activity");
        StreamEntry ingestion = entries.Single(entry => entry["memoryUnitId"] == unitId);
        ingestion["type"].ToString().ShouldBe(JsonNamingPolicy.CamelCase.ConvertName(eventType.ToString()));
        foreach (NameValueEntry field in entries.SelectMany(entry => entry.Values))
        {
            AssertNoSecrets(field.Name.ToString());
            AssertNoSecrets(field.Value.ToString());
        }

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync($"/api/v1/tenants/{tenant}/cases/{caseId}/activity");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        AssertNoSecrets(body);
        var events = JsonSerializer.Deserialize<List<CaseActivityEvent>>(body, MemoriesJsonContext.Options).ShouldNotBeNull();
        events.Single(e => e.MemoryUnitId == unitId).EventType.ShouldBe(eventType);
    }

    private static void AssertNoSecrets(string value)
    {
        value.ShouldNotContain(PathSecret);
        value.ShouldNotContain(QuerySecret);
    }
}
