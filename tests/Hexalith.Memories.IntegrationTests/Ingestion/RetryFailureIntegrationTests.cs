// <copyright file="RetryFailureIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Ingestion;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Client.Queries;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Actors;
using Hexalith.Memories.Server.Infrastructure;
using Hexalith.Memories.Server.Ingestion;

using Microsoft.AspNetCore.DataProtection;

using Shouldly;

using StackExchange.Redis;

/// <summary>
/// Story 6.3 integration coverage for bulk re-ingestion and counter-stage concurrency.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class RetryFailureIntegrationTests(AspireIngestionPipelineFixture fixture)
{
    [Fact]
    public async Task ReIngestBulk_MixedOutcomes_EnumeratedInResponse()
    {
        const string firstPath = "/first.txt";
        const string secondPath = "/second.txt";
        const string failedKeyValue = "DW-560 incompatible Redis value";
        const string missingId = "missing-unit";
        const string erroredId = "wrong-type-unit";
        CancellationToken token = TestContext.Current.CancellationToken;
        IngestionIntegrationTestDriver driver = new(fixture);
        string tenantId = $"tenant-bulk-retry-{Guid.NewGuid():N}";
        string caseId = await driver.CreateTenantAndCaseAsync(tenantId, cancellationToken: token);
        string otherCase = await driver.CreateCaseAsync(tenantId, cancellationToken: token);
        int sourcesRecovered = 0;
        await using ScriptedHttpServer source = await ScriptedHttpServer.StartAsync((request, _) =>
            ValueTask.FromResult(Volatile.Read(ref sourcesRecovered) == 1 && request.Path.Value is firstPath or secondPath
                ? ScriptedHttpResponse.Text($"Recovered content for {request.Path}.")
                : ScriptedHttpResponse.Text("not found", HttpStatusCode.NotFound)));
        string firstUri = source.GetUri(firstPath).AbsoluteUri;
        string secondUri = source.GetUri(secondPath).AbsoluteUri;
        string conflictedUri = source.GetUri("/conflicted.txt").AbsoluteUri;
        string[] sourceUris = [firstUri, secondUri, conflictedUri];
        List<string> originalWorkflows = [];
        foreach (string sourceUri in sourceUris)
        {
            UrlIngestionResponse accepted = await driver.PostUrlIngestionAsync(tenantId, caseId, sourceUri, token);
            originalWorkflows.Add(accepted.InstanceId);
        }

        await Task.WhenAll(originalWorkflows.Select(workflow =>
            driver.WaitForWorkflowRuntimeStatusAsync(tenantId, workflow, "Failed", cancellationToken: token)));
        FailedUnitsPage originalFailures = await driver.WaitForFailedUnitsPageAsync(
            tenantId, caseId, page => page.TotalCount == sourceUris.Length, cancellationToken: token);
        string firstId = originalFailures.Units.Single(unit => unit.SourceUri == firstUri).MemoryUnitId;
        string secondId = originalFailures.Units.Single(unit => unit.SourceUri == secondUri).MemoryUnitId;
        string conflictedId = originalFailures.Units.Single(unit => unit.SourceUri == conflictedUri).MemoryUnitId;
        string[] failedIds = [firstId, secondId, conflictedId];
        string[] targets = [firstId, missingId, conflictedId, erroredId, secondId];
        string route = $"/api/v1/tenants/{tenantId}/cases/{caseId}/failed-units/re-ingest";
        ReIngestRequest bulkRequest = new(targets);

        IDatabase redis = fixture.RedisConnection.GetDatabase();
        RedisKey[] inventoryKeys = sourceUris.Select(uri => (RedisKey)SourceInventoryStore.BuildKey(tenantId, uri)).ToArray();
        RedisValue[] originalInventory = await redis.StringGetAsync(inventoryKeys);
        string conflictedKey = PersistFailedUnitActivity.BuildHashKey(tenantId, conflictedId);
        HashEntry[] conflictedFields = await redis.HashGetAllAsync(conflictedKey);
        string failedIndex = PersistFailedUnitActivity.BuildSortedSetKey(tenantId, caseId);
        double? conflictedScore = await redis.SortedSetScoreAsync(failedIndex, conflictedId);
        conflictedScore.ShouldNotBeNull();

        using (HttpRequestMessage denied = new(HttpMethod.Post, route))
        {
            denied.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", AspireIngestionPipelineFixture.MintServerBearer("tenant-other-bulk-retry"));
            denied.Content = JsonContent.Create(bulkRequest, options: MemoriesJsonContext.Options);
            using HttpResponseMessage denial = await fixture.MemoriesClient.SendAsync(denied, token);
            denial.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await denial.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options, token))
                .ShouldNotBeNull().Code.ShouldBe("TENANT_FORBIDDEN");
        }

        using (HttpResponseMessage wrongCase = await fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases/{otherCase}/failed-units/re-ingest",
            new ReIngestRequest(failedIds), MemoriesJsonContext.Options, token))
        {
            wrongCase.StatusCode.ShouldBe(HttpStatusCode.OK);
            BulkReIngestionResponse result = (await wrongCase.Content.ReadFromJsonAsync<BulkReIngestionResponse>(
                MemoriesJsonContext.Options, token)).ShouldNotBeNull();
            (result.Scheduled, result.NotFound, result.Conflicted, result.Errored, result.Unsupported)
                .ShouldBe((0, failedIds.Length, 0, 0, 0));
            result.Units.ShouldBe(failedIds.Select(id => new ReIngestedUnitInfo(id, null, "not-found", "case mismatch")));
        }

        (await redis.StringGetAsync(inventoryKeys)).ShouldBe(originalInventory);
        (await redis.SortedSetLengthAsync(PersistFailedUnitActivity.BuildSortedSetKey(tenantId, otherCase))).ShouldBe(0);
        FailedUnitsPage untouched = await driver.WaitForFailedUnitsPageAsync(
            tenantId, caseId, page => page.TotalCount == failedIds.Length, cancellationToken: token);
        untouched.Units.ShouldBe(originalFailures.Units);

        // The AppHost uses a source admission fence for a real ownership conflict. The in-process
        // route test controls the competing claim at the registry's atomic remove boundary.
        SourceInventoryStore inventory = new(fixture.RedisConnection,
            new QueryCursorCodec(new EphemeralDataProtectionProvider(), nameof(RetryFailureIntegrationTests)));
        await inventory.FenceAsync(tenantId, conflictedUri, permanent: false, token);
        SourceInventoryPage fenced = await inventory.ReadAsync(tenantId, conflictedUri, cursor: null, pageSize: 10, token);
        fenced.AdmissionClosed.ShouldBeTrue();
        SourceInventoryEntry owner = fenced.Entries.ShouldHaveSingleItem();
        owner.CaseId.ShouldBe(caseId);
        owner.WorkflowInstanceId.ShouldBe(originalWorkflows[2]);
        owner.CandidateMemoryUnitId.ShouldBe(conflictedId);

        string erroredKey = PersistFailedUnitActivity.BuildHashKey(tenantId, erroredId);
        (await redis.StringSetAsync(erroredKey, failedKeyValue, when: When.NotExists)).ShouldBeTrue();
        try
        {
            Volatile.Write(ref sourcesRecovered, 1);
            using HttpResponseMessage response = await fixture.MemoriesClient.PostAsJsonAsync(
                route, bulkRequest, MemoriesJsonContext.Options, token);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            BulkReIngestionResponse bulk = (await response.Content.ReadFromJsonAsync<BulkReIngestionResponse>(
                MemoriesJsonContext.Options, token)).ShouldNotBeNull();
            (bulk.Scheduled, bulk.NotFound, bulk.Conflicted, bulk.Errored, bulk.Unsupported).ShouldBe((2, 1, 1, 1, 0));
            bulk.Units.Select(unit => unit.MemoryUnitId).ShouldBe(targets);
            bulk.Units.Select(unit => unit.Outcome).ShouldBe(["scheduled", "not-found", "conflict", "error", "scheduled"]);
            bulk.Units[1].ShouldBe(new ReIngestedUnitInfo(missingId, null, "not-found", null));
            bulk.Units[2].ShouldBe(new ReIngestedUnitInfo(conflictedId, null, "conflict", null));
            bulk.Units[3].NewWorkflowInstanceId.ShouldBeNull();
            bulk.Units[3].ErrorMessage.ShouldNotBeNull().ShouldContain("WRONGTYPE");

            string firstRetry = bulk.Units[0].NewWorkflowInstanceId.ShouldNotBeNull();
            string secondRetry = bulk.Units[4].NewWorkflowInstanceId.ShouldNotBeNull();
            originalWorkflows.ShouldNotContain(firstRetry);
            originalWorkflows.ShouldNotContain(secondRetry);
            firstRetry.ShouldNotBe(secondRetry);
            await Task.WhenAll(
                AssertIndexedRetryAsync(firstId, firstUri, firstRetry),
                AssertIndexedRetryAsync(secondId, secondUri, secondRetry));

            string[] indexedIds = [firstId, secondId];
            (await driver.ListRedisKeysAsync($"{IndexSchemaDefinitions.GetSyntacticKeyPrefix(tenantId)}*")).ShouldBe(
                indexedIds.Select(id => IndexSchemaDefinitions.BuildSyntacticKey(tenantId, id)).Order());
            (await driver.ListRedisKeysAsync($"{IndexSchemaDefinitions.GetSemanticKeyPrefix(tenantId)}*")).ShouldBe(
                indexedIds.Select(id => IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, id, sequence: 0)).Order());
            (await redis.SortedSetRangeByRankAsync(failedIndex)).ShouldBe([(RedisValue)conflictedId]);
            (await driver.ListRedisKeysAsync($"{tenantId}:failed-unit:*")).ShouldBe(new[] { conflictedKey, erroredKey }.Order());
            (await redis.HashGetAllAsync(conflictedKey)).OrderBy(field => field.Name.ToString(), StringComparer.Ordinal)
                .ShouldBe(conflictedFields.OrderBy(field => field.Name.ToString(), StringComparer.Ordinal));
            (await redis.SortedSetScoreAsync(failedIndex, conflictedId)).ShouldBe(conflictedScore);
            (await redis.StringGetAsync(erroredKey)).ShouldBe((RedisValue)failedKeyValue);
            SourceInventoryPage after = await inventory.ReadAsync(tenantId, conflictedUri, cursor: null, pageSize: 10, token);
            after.AdmissionClosed.ShouldBeTrue();
            after.Version.ShouldBe(fenced.Version);
            after.Entries.ShouldHaveSingleItem().ShouldBe(owner);

            using HttpClient dapr = AspireIngestionPipelineFixture.CreateDaprSidecarClient(fixture.DaprSidecarHttpEndpoint);
            string actorId = Uri.EscapeDataString($"{tenantId}:{caseId}");
            CaseIngestionCounterState counter = (await dapr.GetFromJsonAsync<CaseIngestionCounterState>(
                $"/v1.0/actors/{nameof(CaseIngestionCounterActor)}/{actorId}/state/counterState", token)).ShouldNotBeNull();
            counter.AppliedTransitionSequences.ShouldNotBeNull().Keys.Order(StringComparer.Ordinal)
                .ShouldBe(originalWorkflows.Concat([firstRetry, secondRetry]).Order(StringComparer.Ordinal));
            (counter.Queued, counter.Extracting, counter.Embedding, counter.Indexing).ShouldBe((0, 0, 0, 0));
        }
        finally
        {
            await redis.KeyDeleteAsync(erroredKey);
        }

        async Task AssertIndexedRetryAsync(string unitId, string sourceUri, string workflowId)
        {
            string payload = await driver.WaitForWorkflowRuntimeStatusAsync(tenantId, workflowId, "Completed", cancellationToken: token);
            IngestionWorkflowStatus status = JsonSerializer.Deserialize<IngestionWorkflowStatus>(payload, MemoriesJsonContext.Options).ShouldNotBeNull();
            status.MemoryUnitId.ShouldBe(unitId);
            MemoryUnit indexed = await driver.WaitForMemoryUnitAsync(
                tenantId, caseId, unitId, unit => unit.Status == MemoryUnitStatus.Indexed, cancellationToken: token);
            indexed.SourceUri.ShouldBe(sourceUri);
            indexed.Content.ShouldContain("Recovered content");
        }
    }

    [Fact(Skip = "26.3-COUNTER-STAGE-BARRIER: Concurrent stage counts require pausing six real workflows at exact durable-workflow stage barriers that the AppHost fixture cannot currently control. Owner: workflow maintainers. Unskip when: test-only stage barriers can pause and release real workflow instances deterministically.")]
    public void CounterActor_TracksConcurrentInflightWorkflows()
    {
        // AC5, AC6: schedule 3 in Embedding, 2 in Extracting, 1 in Queued. Assert
        // GET /cases/{caseId}/status returns ExtractingCount=2, EmbeddingCount=3, QueuedCount=1.
    }
}
