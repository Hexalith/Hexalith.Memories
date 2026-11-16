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

using Shouldly;

using StackExchange.Redis;

/// <summary>Contains the nightly integration cases for EventIngestionPipelineIntegrationTests.</summary>
public sealed partial class EventIngestionPipelineIntegrationTests
{
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
