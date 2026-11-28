// <copyright file="EventIngestionPipelineIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.EventStoreIntegration;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.EventStore;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Activities.Ingestion;
using Hexalith.Memories.Server.Infrastructure;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the nightly integration cases for EventIngestionPipelineIntegrationTests.</summary>
public sealed partial class EventIngestionPipelineIntegrationTests
{
    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task PublishViaDaprPubSub_IndexesRawAndNaturalLanguageEmbeddingsWithinSevenSeconds()
    {
        string tenantId = _fixture.EventStoreMappedTenantId;
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");
        string eventId = $"evt-dual-{Guid.NewGuid():N}";
        string marker = $"dual{Guid.NewGuid():N}";
        const string Subject = "claim-dual-embedding";
        using HttpClient daprClient = AspireIngestionPipelineFixture.CreateDaprSidecarClient(_fixture.DaprSidecarHttpEndpoint);

        using var indexBudget = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        indexBudget.CancelAfter(TimeSpan.FromSeconds(7));
        CancellationToken token = indexBudget.Token;
        await PublishEventViaDaprAsync(daprClient, eventId, Subject, marker, token);
        SearchResult search = await WaitForSearchAsync(tenantId, marker, Subject, eventId, cancellationToken: token);
        ScoredResult unit = search.Results.Where(hit => hit.SourceUri == eventId).ShouldHaveSingleItem();
        string caseId = unit.CaseId.ShouldNotBeNull();
        caseId.ShouldNotBeNullOrWhiteSpace();

        IDatabase redis = _fixture.RedisConnection.GetDatabase();
        string rawKey = IndexSchemaDefinitions.BuildSemanticChunkKey(tenantId, unit.MemoryUnitId, 0);
        string descriptionKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, unit.MemoryUnitId);
        RedisValue[] names = ["tenantId", "memoryUnitId", "caseId", "embedding", "embeddingDimensions", "naturalLanguageDescription", "descriptionOrigin"];
        RedisValue[] raw;
        RedisValue[] description;
        while (true)
        {
            Task<RedisValue[]> rawRead = redis.HashGetAsync(rawKey, names);
            Task<RedisValue[]> descriptionRead = redis.HashGetAsync(descriptionKey, names);
            RedisValue[][] hashes = await Task.WhenAll(rawRead, descriptionRead).WaitAsync(token);
            raw = hashes[0];
            description = hashes[1];
            if (raw[3].HasValue && description[3].HasValue)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), token);
        }

        foreach (RedisValue[] hash in new[] { raw, description })
        {
            hash[0].ToString().ShouldBe(tenantId);
            hash[1].ToString().ShouldBe(unit.MemoryUnitId);
            hash[2].ToString().ShouldBe(caseId);
            int dimensions = (int)hash[4];
            dimensions.ShouldBeGreaterThan(0);
            ((byte[]?)hash[3]).ShouldNotBeNull().Length.ShouldBe(dimensions * sizeof(float));
        }

        raw[5].IsNull.ShouldBeTrue();
        description[5].ToString().ShouldNotBeNullOrWhiteSpace();
        description[6].ToString().ShouldBe("ai");

        string workflow = Uri.EscapeDataString(DedupKeyBuilder.BuildKey(tenantId, caseId, eventId));
        while (true)
        {
            JsonElement state = await daprClient.GetFromJsonAsync<JsonElement>(
                $"/v1.0/workflows/dapr/{workflow}", token);
            if (string.Equals(state.GetProperty("runtimeStatus").GetString(), "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                string output = state.GetProperty("properties").GetProperty("dapr.workflow.output").GetString().ShouldNotBeNull();
                IngestionResult result = JsonSerializer.Deserialize<IngestionResult>(output, MemoriesJsonContext.Options).ShouldNotBeNull();
                result.MemoryUnitId.ShouldBe(unit.MemoryUnitId);
                result.NaturalLanguageEmbeddingStatus.ShouldBe(NaturalLanguageEmbeddingStatus.Indexed);
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), token);
        }

        // Graph and semantic activities complete independently before the workflow completes.
        var graph = new FalkorDB(_fixture.FalkorDbConnection.GetDatabase()).SelectGraph(tenantId);
        ResultSet graphResult = await graph.QueryAsync(
            "MATCH (unit:MemoryUnit {id: $id}) RETURN unit.isStub AS isStub",
            new Dictionary<string, object> { ["id"] = unit.MemoryUnitId }).WaitAsync(token);
        graphResult.ShouldHaveSingleItem().GetValue<bool>("isStub").ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task PostEventsIngest_ShouldIndexMemoryUnitAndRespectExactSubjectFilter()
    {
        string tenantId = _fixture.EventStoreMappedTenantId;
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        string eventId = $"evt-{Guid.NewGuid():N}";
        string marker = $"eventmark{Guid.NewGuid().ToString("N")[..8]}";
        string indexedText = $"claim event content {marker}";
        const string searchQuery = "claim event";
        const string subject = "claim-42";
        string envelope = $$"""
            {
              "specversion": "1.0",
              "id": "{{eventId}}",
              "source": "{{_fixture.EventStoreMappedSourcePrefix}}/submitted",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "subject": "{{subject}}",
              "time": "{{DateTimeOffset.UtcNow:o}}",
              "data": {
                "summary": "{{indexedText}}",
                "amount": 100
              }
            }
            """;

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsync(
            "/events/ingest",
            new StringContent(envelope, Encoding.UTF8, "application/cloudevents+json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        EventIngestionResponse? accepted = JsonSerializer.Deserialize(
            await response.Content.ReadAsStringAsync(),
            EventStoreJsonContext.Default.EventIngestionResponse);
        accepted.ShouldNotBeNull();
        accepted!.Status.ShouldBe(EventIngestionResponse.StatusAccepted);
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();

        string instanceId = accepted.InstanceId!;
        await WaitForWorkflowCompletionAsync(tenantId, instanceId);

        string caseId = ExtractCaseId(instanceId);
        string memoryUnitId = await WaitForDedupResolutionAsync(instanceId);
        MemoryUnit indexed = await WaitForMemoryUnitAsync(tenantId, caseId, memoryUnitId);
        indexed.Status.ShouldBe(MemoryUnitStatus.Indexed);
        indexed.Content.ShouldContain(indexedText, Shouldly.Case.Sensitive);
        indexed.Metadata.ShouldContainKey("cloudevent.subject");
        indexed.Metadata["cloudevent.subject"].Value.ShouldBe(subject);
        RedisValue indexedSubject = await _fixture.RedisConnection
            .GetDatabase()
            .HashGetAsync($"{tenantId}:mu:{memoryUnitId}", "cloudeventSubject");
        indexedSubject.ToString().ShouldBe(subject);

        SearchResult matching = await WaitForSearchAsync(tenantId, searchQuery, subject, eventId, memoryUnitId);
        matching.Results.ShouldNotBeEmpty();
        matching.Results.ShouldContain(r => string.Equals(r.SourceUri, eventId, StringComparison.Ordinal));

        using HttpResponseMessage wrongSubjectResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(searchQuery)}&subject=claim-999");
        wrongSubjectResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? wrongSubject = await wrongSubjectResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        wrongSubject.ShouldNotBeNull();
        wrongSubject!.Results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task PublishViaDaprPubSub_ShouldBecomeSearchableWithinFiveSeconds_AndIgnoreDuplicateReplay()
    {
        string tenantId = _fixture.EventStoreMappedTenantId;
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        string eventId = $"evt-dapr-{Guid.NewGuid():N}";
        string marker = $"daprmark{Guid.NewGuid().ToString("N")[..8]}";
        string firstText = $"dapr claim content {marker}";
        const string firstQuery = "dapr claim";
        string duplicateText = $"duplicate claim content {marker}";
        string duplicateQuery = $"duplicate claim {marker}";
        const string subject = "claim-42";

        using HttpClient daprClient = new() { BaseAddress = _fixture.DaprSidecarHttpEndpoint };

        Stopwatch freshness = Stopwatch.StartNew();
        await PublishEventViaDaprAsync(daprClient, eventId, subject, firstText);

        SearchResult firstMatch = await WaitForSearchAsync(tenantId, firstQuery, subject, eventId);
        freshness.Stop();

        freshness.Elapsed.ShouldBeLessThanOrEqualTo(
            FreshnessTimeout,
            $"Dapr pub/sub publish path should surface searchable results within {FreshnessTimeout}.");
        firstMatch.TotalCount.ShouldBeGreaterThan(0);
        firstMatch.Results.ShouldContain(result => string.Equals(result.SourceUri, eventId, StringComparison.Ordinal));

        await PublishEventViaDaprAsync(daprClient, eventId, subject, duplicateText);
        await AssertNoSearchMatchWithinAsync(tenantId, duplicateQuery, subject, FreshnessTimeout);
    }
}
