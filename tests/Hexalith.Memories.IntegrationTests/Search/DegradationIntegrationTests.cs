// <copyright file="DegradationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Search;

using System.Net;
using System.Net.Http.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Shouldly;

using StackExchange.Redis;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class DegradationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public DegradationIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact(Skip = "26.3-SEMANTIC-CAPABILITY-FAULT: Redis Stack hosts both syntactic and semantic indexes, so stopping it cannot isolate only the semantic capability in the current topology. Owner: search maintainers. Unskip when: semantic failure can be injected independently of the shared Redis resource.")]
    public void HybridSearch_RedisVectorStopped_ShouldReturn200Degraded()
    {
        // Scenario:
        //   1. Stop the Redis Vector container.
        //   2. POST a hybrid search with axes=syntactic,semantic,graph.
        //   3. Expect 200 OK with degraded=true, unavailableAxes=["semantic"],
        //      results from syntactic + graph.
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task HybridSearch_FalkorDbStopped_ShouldDegradeToSyntacticAndSemantic()
    {
        IngestionIntegrationTestDriver driver = new(_fixture);
        string unique = Guid.NewGuid().ToString("N");
        string tenantId = $"tenant-falkor-{unique[..10]}";
        string sourceUri = $"file:///{unique}-falkor-recovery.txt";
        string canary = $"Falkor recovery canary {unique}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId);
        string instanceId = await driver.PostInlineIngestionAsync(tenantId, caseId, sourceUri, canary);
        string workflow = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, instanceId, "Completed");
        string memoryUnitId = IngestionIntegrationTestDriver.TryExtractMemoryUnitId(workflow) ?? instanceId;
        (string syntacticKey, string semanticKey) = await driver.WaitForSingleBackendWriteAsync(
            tenantId,
            caseId,
            sourceUri);

        IReadOnlyDictionary<string, string> syntacticBefore = await ReadHashSnapshotAsync(syntacticKey);
        IReadOnlyDictionary<string, string> semanticBefore = await ReadHashSnapshotAsync(semanticKey);
        HybridSearchResult before = await GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
        before.Degraded.ShouldBeFalse();
        before.UnavailableAxes.ShouldBeEmpty();
        before.Results.ShouldContain(result => result.MemoryUnitId == memoryUnitId && result.ContentSnippet.Contains(unique, StringComparison.Ordinal));
        (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(1);

        bool stopped = false;
        try
        {
            await _fixture.StopFalkorDbContainerAsync();
            stopped = true;

            HybridSearchResult degraded = await GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
            degraded.Degraded.ShouldBeTrue();
            degraded.UnavailableAxes.ShouldBe(["graph"]);
            degraded.Results.ShouldContain(result =>
                result.MemoryUnitId == memoryUnitId &&
                result.ContentSnippet.Contains(unique, StringComparison.Ordinal) &&
                (result.SyntacticScore.HasValue || result.SemanticScore.HasValue));

            using HttpResponseMessage traversal = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/tenants/{tenantId}/traverse?startNodeId={memoryUnitId}&depth=1");
            traversal.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            ErrorResponse? graphError = await traversal.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
            graphError.ShouldNotBeNull();
            graphError.Code.ShouldBe("GRAPH_UNAVAILABLE");
            traversal.Headers.GetValues("Retry-After").Single().ShouldBe("5");

            TenantSummary tenant = await driver.WaitForNewestTenantSummaryAsync(
                tenantId,
                item => item.IndexStatus.Graph == IndexHealth.Unknown);
            tenant.IndexStatus.Graph.ShouldBe(IndexHealth.Unknown);
            tenant.IndexStatus.Syntactic.ShouldBe(IndexHealth.Ready);
            tenant.IndexStatus.Semantic.ShouldBe(IndexHealth.Ready);

            AssertHashSnapshot(syntacticBefore, await ReadHashSnapshotAsync(syntacticKey));
            AssertHashSnapshot(semanticBefore, await ReadHashSnapshotAsync(semanticKey));
        }
        finally
        {
            if (stopped)
            {
                await _fixture.StartFalkorDbContainerAsync();
            }
        }

        (await driver.CountGraphNodesAsync(tenantId, caseId, sourceUri)).ShouldBe(1);
        AssertHashSnapshot(syntacticBefore, await ReadHashSnapshotAsync(syntacticKey));
        AssertHashSnapshot(semanticBefore, await ReadHashSnapshotAsync(semanticKey));
        HybridSearchResult recovered = await GetHybridSearchAsync(tenantId, caseId, memoryUnitId, unique);
        recovered.Degraded.ShouldBeFalse();
        recovered.UnavailableAxes.ShouldBeEmpty();
        recovered.Results.ShouldContain(result => result.MemoryUnitId == memoryUnitId);
    }

    [Fact(Skip = "26.3-ALL-BACKENDS-STATESTORE: Redis Stack is also the DAPR workflow and actor state store, so stopping it destroys the control plane before the search-only all-backends response can be observed. Owner: AppHost maintainers. Unskip when: DAPR state and search indexes use separate resources.")]
    public void HybridSearch_AllBackendsStopped_ShouldReturn503AllBackendsUnavailable()
    {
        // Scenario:
        //   1. Stop Redis Stack + FalkorDB.
        //   2. POST a hybrid search.
        //   3. Expect 503 with ErrorResponse.Code == "ALL_BACKENDS_UNAVAILABLE" and
        //      Retry-After: 5 header. Body message lists all enabled axes.
    }

    [Fact(Skip = "26.3-SINGLE-AXIS-REDIS-COLLAPSE: Redis Stack is shared by search indexes and DAPR state, so stopping it cannot isolate the syntactic backend while keeping the API control plane healthy. Owner: AppHost maintainers. Unskip when: search Redis is separated from workflow and actor state.")]
    public void SingleAxisSearch_RedisStopped_ShouldReturn503BackendUnavailable()
    {
        // Scenario:
        //   1. Stop Redis Stack.
        //   2. GET /api/v1/search?axis=syntactic.
        //   3. Expect 503 with ErrorResponse.Code == "BACKEND_UNAVAILABLE" and
        //      Retry-After: 5 header.
    }

    private async Task<HybridSearchResult> GetHybridSearchAsync(
        string tenantId,
        string caseId,
        string memoryUnitId,
        string query)
    {
        string path = $"/api/v1/search?tenantId={tenantId}&caseId={caseId}&query={query}" +
            $"&axis=hybrid&axes=syntactic,semantic,graph&graphStartNodeId={memoryUnitId}&depth=1";
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<HybridSearchResult>(MemoriesJsonContext.Options)
            ?? throw new InvalidOperationException("Hybrid search returned no response body.");
    }

    private async Task<IReadOnlyDictionary<string, string>> ReadHashSnapshotAsync(string key)
    {
        IDatabase database = _fixture.RedisConnection.GetDatabase();
        (await database.KeyTypeAsync(key)).ShouldBe(RedisType.Hash);
        HashEntry[] entries = await database.HashGetAllAsync(key);
        entries.ShouldNotBeEmpty();
        return entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString(),
            StringComparer.Ordinal);
    }

    private static void AssertHashSnapshot(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        actual.Count.ShouldBe(expected.Count);
        foreach ((string field, string value) in expected)
        {
            actual.ShouldContainKey(field);
            actual[field].ShouldBe(value);
        }
    }
}
