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

/// <summary>End-to-end coverage for the Memories event-intake surface. Exercises both direct
/// <c>POST /events/ingest</c> and Dapr pub/sub publish into the Memories subscription, then proves
/// workflow completion, searchable persistence, subject filtering, and duplicate suppression against
/// the Memories-owned Aspire Redis/FalkorDB topology. The fixture does not provision an EventStore
/// gateway resource, so this class is not EventStore-to-Memories full-stack evidence.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed partial class EventIngestionPipelineIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FreshnessTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WorkflowTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(30);

    private readonly AspireIngestionPipelineFixture _fixture;

    public EventIngestionPipelineIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

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

    private async Task WaitForWorkflowCompletionAsync(string tenantId, string instanceId)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        string lastPayload = string.Empty;
        while (timeout.Elapsed < WorkflowTimeout)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/ingest/{instanceId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                AspireIngestionPipelineFixture.MintServerBearer(tenantId));
            using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.SendAsync(request);
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

    private async Task PublishEventViaDaprAsync(
        HttpClient daprClient, string eventId, string subject, string queryToken, CancellationToken cancellationToken = default)
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
            new StringContent(envelope, Encoding.UTF8, "application/cloudevents+json"),
            cancellationToken).ConfigureAwait(false);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private async Task<SearchResult> WaitForSearchAsync(
        string tenantId,
        string query,
        string subject,
        string expectedSourceUri,
        string? memoryUnitId = null,
        CancellationToken cancellationToken = default)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        string? lastUnfilteredBody = null;
        while (timeout.Elapsed < SearchTimeout)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}&subject={Uri.EscapeDataString(subject)}",
                cancellationToken);
            lastStatusCode = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SearchResult? result = JsonSerializer.Deserialize<SearchResult>(lastBody, MemoriesJsonContext.Options);
                if (result is not null
                    && result.Results.Any(candidate =>
                        (memoryUnitId is null || candidate.MemoryUnitId == memoryUnitId)
                        && string.Equals(candidate.SourceUri, expectedSourceUri, StringComparison.Ordinal)))
                {
                    return result;
                }
            }

            using HttpResponseMessage unfiltered = await _fixture.MemoriesClient.GetAsync(
                $"/api/v1/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}",
                cancellationToken);
            lastUnfilteredBody = await unfiltered.Content.ReadAsStringAsync(cancellationToken);

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
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
