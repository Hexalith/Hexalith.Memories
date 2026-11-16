// <copyright file="CliSearchIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cli;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 7.2 Task 10 — end-to-end test that ingests a fixture memory unit, waits for the workflow to
/// land, and asserts <see cref="MemoriesClient.HybridSearchAsync"/> with explain=true produces a
/// non-null composite score and the PRD-mandated caveat substring. Runs in-process; does NOT spawn
/// the <c>memories</c> binary (anti-pattern #8). Filter with
/// <c>dotnet test --filter "Category=Integration"</c> to run locally, or
/// <c>--filter "Category!=Integration"</c> to skip without fixture setup.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed partial class CliSearchIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan IngestionTimeout = TimeSpan.FromMinutes(3);

    private readonly AspireIngestionPipelineFixture _fixture;

    public CliSearchIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/v1/tenants",
                new TenantProvisioningInput(tenantId, displayName),
                MemoriesJsonContext.Options);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActivationTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/v1/tenants/{tenantId}");

            if (tenantResponse.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await tenantResponse.Content
                    .ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options);
                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue($"Tenant '{tenantId}' did not reach Active within {ActivationTimeout}.");
    }

    private async Task WaitForIngestionCompletionAsync(string tenantId, string instanceId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(IngestionTimeout);
        string lastPayload = string.Empty;
        while (DateTimeOffset.UtcNow < deadline)
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
            $"Ingestion instance '{instanceId}' did not complete within {IngestionTimeout}. Last payload: {lastPayload}");
    }

    private static bool ReachedCompletedRuntimeStatus(string payload)
    {
        using JsonDocument doc = JsonDocument.Parse(payload);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("status", out JsonElement status)
            && status.ValueKind == JsonValueKind.String
            && (string.Equals(status.GetString(), "COMPLETED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status.GetString(), "Completed", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

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

    private sealed record AcceptedResponse(string InstanceId);
}
