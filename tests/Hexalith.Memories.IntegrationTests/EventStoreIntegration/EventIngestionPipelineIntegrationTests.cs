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

/// <summary>End-to-end event-surface coverage for Story 9.1. Publishes a CloudEvents envelope through
/// <c>POST /events/ingest</c>, waits for the workflow to complete, and proves the resulting memory unit is
/// queryable through the search API with exact <c>subject</c> filtering.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class EventIngestionPipelineIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FreshnessTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WorkflowTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

    private readonly AspireIngestionPipelineFixture _fixture;

    public EventIngestionPipelineIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task PostEventsIngest_ShouldIndexMemoryUnitAndRespectExactSubjectFilter()
    {
        string tenantId = _fixture.EventStoreMappedTenantId;
        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        string eventId = $"evt-{Guid.NewGuid():N}";
        const string indexedText = "claim event content";
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
        await WaitForWorkflowCompletionAsync(instanceId);

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

        SearchResult matching = await WaitForSearchAsync(tenantId, searchQuery, subject, memoryUnitId);
        matching.Results.ShouldNotBeEmpty();
        matching.Results.ShouldContain(r => r.ContentSnippet.Contains(indexedText, StringComparison.Ordinal));

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
        const string firstText = "dapr claim content";
        const string firstQuery = "dapr claim";
        const string duplicateText = "duplicate claim content";
        const string duplicateQuery = "duplicate claim";
        const string subject = "claim-42";

        using HttpClient daprClient = new() { BaseAddress = _fixture.DaprSidecarHttpEndpoint };

        Stopwatch freshness = Stopwatch.StartNew();
        await PublishEventViaDaprAsync(daprClient, eventId, subject, firstText);

        SearchResult firstMatch = await WaitForSearchAsync(tenantId, firstQuery, subject);
        freshness.Stop();

        freshness.Elapsed.ShouldBeLessThanOrEqualTo(
            FreshnessTimeout,
            $"Dapr pub/sub publish path should surface searchable results within {FreshnessTimeout}.");
        firstMatch.TotalCount.ShouldBeGreaterThan(0);
        firstMatch.Results.ShouldContain(result => result.ContentSnippet.Contains(firstText, StringComparison.Ordinal));

        await PublishEventViaDaprAsync(daprClient, eventId, subject, duplicateText);
        await AssertNoSearchMatchWithinAsync(tenantId, duplicateQuery, subject, FreshnessTimeout);
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/v1/tenants",
                new TenantProvisioningInput(tenantId, displayName),
                MemoriesJsonContext.Options);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        Stopwatch timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < ActivationTimeout)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient.GetAsync($"/api/v1/tenants/{tenantId}");
            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options);
                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue($"Tenant '{tenantId}' did not reach Active within {ActivationTimeout}.");
    }

    private async Task WaitForWorkflowCompletionAsync(string instanceId)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        string lastPayload = string.Empty;
        while (timeout.Elapsed < WorkflowTimeout)
        {
            using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.GetAsync($"/api/v1/ingest/{instanceId}");
            if (statusResponse.StatusCode == HttpStatusCode.OK)
            {
                lastPayload = await statusResponse.Content.ReadAsStringAsync();
                if (ReachedCompletedRuntimeStatus(lastPayload))
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue(
            $"Ingestion instance '{instanceId}' did not complete within {WorkflowTimeout}. Last payload: {lastPayload}");
    }

    private static bool ReachedCompletedRuntimeStatus(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("runtimeStatus", out JsonElement runtimeStatus))
        {
            if (runtimeStatus.ValueKind == JsonValueKind.String
                && string.Equals(runtimeStatus.GetString(), "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (runtimeStatus.ValueKind == JsonValueKind.Number
                && runtimeStatus.TryGetInt32(out int ordinal)
                && ordinal == 3)
            {
                return true;
            }
        }

        return root.TryGetProperty("isWorkflowCompleted", out JsonElement completed)
            && completed.ValueKind == JsonValueKind.True;
    }

    private static string ExtractCaseId(string instanceId)
    {
        string[] parts = instanceId.Split(':', 4, StringSplitOptions.None);
        parts.Length.ShouldBe(4, $"Workflow instance id '{instanceId}' should use dedup:<tenant>:<case>:<hash> format.");
        parts[0].ShouldBe("dedup");
        parts[2].ShouldNotBeNullOrWhiteSpace();
        return parts[2];
    }

    private async Task<string> WaitForDedupResolutionAsync(string dedupKey)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        string lastValue = string.Empty;

        while (timeout.Elapsed < SearchTimeout)
        {
            RedisValue value = await db.StringGetAsync(dedupKey);
            if (!value.IsNullOrEmpty)
            {
                lastValue = value.ToString();
                if (!string.IsNullOrWhiteSpace(lastValue)
                    && !PreflightDedupReservation.IsTransientReservation(lastValue))
                {
                    return lastValue;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue($"Dedup key '{dedupKey}' did not resolve to a memory unit id within {SearchTimeout}. Last value: {lastValue}");
        return null!;
    }

    private async Task<MemoryUnit> WaitForMemoryUnitAsync(string tenantId, string caseId, string memoryUnitId)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        string? lastBody = null;
        HttpStatusCode? lastStatusCode = null;

        while (timeout.Elapsed < SearchTimeout)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/memory-units/{Uri.EscapeDataString(memoryUnitId)}");
            lastStatusCode = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                MemoryUnit? memoryUnit = JsonSerializer.Deserialize<MemoryUnit>(lastBody, MemoriesJsonContext.Options);
                if (memoryUnit is not null && memoryUnit.Status == MemoryUnitStatus.Indexed)
                {
                    return memoryUnit;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue(
            $"Memory unit '{memoryUnitId}' in '{tenantId}/{caseId}' was not visible as indexed within {SearchTimeout}. "
            + $"Last status={lastStatusCode}, last body={lastBody}");
        return null!;
    }

    private async Task PublishEventViaDaprAsync(HttpClient daprClient, string eventId, string subject, string queryToken)
    {
        string envelope = $$"""
            {
              "specversion": "1.0",
              "id": "{{eventId}}",
              "source": "{{_fixture.EventStoreMappedSourcePrefix}}/submitted",
              "type": "MyApp.Claims.ClaimSubmittedV2",
              "subject": "{{subject}}",
              "time": "{{DateTimeOffset.UtcNow:o}}",
              "data": {
                "summary": "{{queryToken}}",
                "amount": 100
              }
            }
            """;

        using HttpResponseMessage response = await daprClient.PostAsync(
            $"/v1.0/publish/{EventIngestionController.PubSubName}/memories-events",
            new StringContent(envelope, Encoding.UTF8, "application/cloudevents+json")).ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<SearchResult> WaitForSearchAsync(string tenantId, string query, string subject, string? memoryUnitId = null)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        string? lastUnfilteredBody = null;
        while (timeout.Elapsed < SearchTimeout)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}&subject={Uri.EscapeDataString(subject)}");
            lastStatusCode = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SearchResult? result = JsonSerializer.Deserialize<SearchResult>(lastBody, MemoriesJsonContext.Options);
                if (result is not null && result.Results.Count > 0)
                {
                    return result;
                }
            }

            using HttpResponseMessage unfiltered = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}");
            lastUnfilteredBody = await unfiltered.Content.ReadAsStringAsync();

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue(
            $"Search did not return a result for subject '{subject}' within {SearchTimeout}. "
            + $"Last status={lastStatusCode}, last body={lastBody}, last unfiltered body={lastUnfilteredBody}");
        return null!;
    }

    private async Task AssertNoSearchMatchWithinAsync(string tenantId, string query, string subject, TimeSpan duration)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < duration)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}&subject={Uri.EscapeDataString(subject)}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            SearchResult? result = await response.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
            result.ShouldNotBeNull();
            result!.TotalCount.ShouldBe(0);
            result.Results.ShouldBeEmpty();

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }
}
