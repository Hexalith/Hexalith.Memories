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
        string queryToken = $"claimtoken{Guid.NewGuid():N}";
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
                "summary": "{{queryToken}}",
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
        indexed.Content.ShouldContain(queryToken, Case.Sensitive);
        indexed.Metadata.ShouldContainKey("cloudevent.subject");
        indexed.Metadata["cloudevent.subject"].Value.ShouldBe(subject);

        await WaitForIndexedMatchAsync(tenantId, queryToken, subject);

        SearchResult matching = await WaitForSearchAsync(tenantId, queryToken, subject);
        matching.Results.ShouldNotBeEmpty();
        matching.Results.ShouldContain(r => r.ContentSnippet.Contains(queryToken, StringComparison.Ordinal));

        using HttpResponseMessage wrongSubjectResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(queryToken)}&subject=claim-999");
        wrongSubjectResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        SearchResult? wrongSubject = await wrongSubjectResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        wrongSubject.ShouldNotBeNull();
        wrongSubject!.Results.ShouldBeEmpty();
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/tenants",
                new TenantProvisioningInput(tenantId, displayName),
                MemoriesJsonContext.Options);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        Stopwatch timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < ActivationTimeout)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient.GetAsync($"/api/tenants/{tenantId}");
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
            using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.GetAsync($"/api/ingest/{instanceId}");
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
                if (!string.IsNullOrWhiteSpace(lastValue))
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
                $"/api/tenants/{Uri.EscapeDataString(tenantId)}/cases/{Uri.EscapeDataString(caseId)}/memory-units/{Uri.EscapeDataString(memoryUnitId)}");
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

    private async Task WaitForIndexedMatchAsync(string tenantId, string query, string subject)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        long lastCount = -1;
        string? lastError = null;

        while (timeout.Elapsed < SearchTimeout)
        {
            try
            {
                lastCount = await GetIndexedMatchCountAsync(tenantId, query, subject);
                if (lastCount > 0)
                {
                    return;
                }
            }
            catch (RedisServerException ex)
            {
                lastError = ex.Message;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue(
            $"RediSearch index for tenant '{tenantId}' did not expose a subject-filtered match within {SearchTimeout}. "
            + $"Last count={lastCount}, last error={lastError}");
    }

    private async Task<long> GetIndexedMatchCountAsync(string tenantId, string query, string subject)
    {
        IDatabase db = _fixture.RedisConnection.GetDatabase();
        string indexName = $"{tenantId}:memories:idx";
        string queryString = $"@cloudeventSubject:{{{EscapeRedisSearchValue(subject)}}} {EscapeRedisSearchValue(query)}";

        RedisResult raw = await db.ExecuteAsync(
            "FT.SEARCH",
            indexName,
            queryString,
            "NOCONTENT",
            "LIMIT",
            "0",
            "0",
            "DIALECT",
            "2");

        RedisResult[]? values = (RedisResult[]?)raw;
        if (values is null || values.Length == 0)
        {
            return 0;
        }

        return ParseRedisLong(values[0]);
    }

    private static long ParseRedisLong(RedisResult result)
    {
        string? raw = result.ToString();
        return long.TryParse(raw, out long value) ? value : 0;
    }

    private static string EscapeRedisSearchValue(string value)
        => string.Concat(value.Select(ch => ch switch
        {
            '-' or '@' or '!' or '{' or '}' or '(' or ')' or '[' or ']' or '^' or '~' or '*' or '?' or ':' or '\\' or '"' or '\'' or '|' or ',' => $"\\{ch}",
            _ => ch.ToString(),
        }));

    private async Task<SearchResult> WaitForSearchAsync(string tenantId, string query, string subject)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        HttpStatusCode? lastStatusCode = null;
        string? lastBody = null;
        while (timeout.Elapsed < SearchTimeout)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
                $"/api/search?tenantId={Uri.EscapeDataString(tenantId)}&axis=syntactic&query={Uri.EscapeDataString(query)}&subject={Uri.EscapeDataString(subject)}");
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

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue(
            $"Search did not return a result for subject '{subject}' within {SearchTimeout}. "
            + $"Last status={lastStatusCode}, last body={lastBody}");
        return null!;
    }
}
