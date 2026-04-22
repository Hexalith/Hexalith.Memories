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
public sealed class CliSearchIntegrationTests
{
    private static readonly TimeSpan ActivationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan IngestionTimeout = TimeSpan.FromMinutes(3);

    private readonly AspireIngestionPipelineFixture _fixture;

    public CliSearchIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    [Trait("Category", "IntegrationSlow")]
    public async Task HybridSearchAsync_AfterIngestion_ReturnsCompositeScoreAndCaveat()
    {
        // Arrange — unique identifiers so this test is isolated from peer integration tests.
        string tenantId = $"tenant-cli-search-{Guid.NewGuid():N}";
        string caseId = $"case-{Guid.NewGuid():N}";
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        const string needleQuery = "customerEscalationToken";

        await EnsureTenantActiveAsync(tenantId, $"Tenant {tenantId}");

        var ingestionInput = new IngestionInput
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = System.Text.Encoding.UTF8.GetBytes(
                $"Confidential invoicing discrepancy escalated by the customer. {needleQuery} body."),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync("/api/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        AcceptedResponse? accepted = await ingestResponse.Content
            .ReadFromJsonAsync<AcceptedResponse>(MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();

        await WaitForIngestionCompletionAsync(accepted.InstanceId);

        using var http = new HttpClient
        {
            BaseAddress = _fixture.MemoriesClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions
        {
            Endpoint = _fixture.MemoriesClient.BaseAddress,
        });
        var client = new MemoriesClient(http, options, NullLogger<MemoriesClient>.Instance);

        // Act — run the same hybrid+explain call the `memories search query --explain` path uses.
        HybridSearchResult result = await client.HybridSearchAsync(
            new HybridSearchRequest(
                TenantId: tenantId,
                Query: needleQuery,
                CaseId: caseId,
                Explain: true),
            CancellationToken.None);

        // Assert — the CLI boundary guarantees the caveat substring survives verbatim from the server.
        result.Results.ShouldNotBeEmpty();
        FusedScoredResult match = result.Results[0];
        match.CompositeScore.ShouldBeInRange(0.0d, 1.0d);

        result.Explanation.ShouldNotBeNull();
        result.Explanation!.Caveat.ShouldContain(
            "measure query-result relevance",
            customMessage: "The CLI's compliance-enablement guarantee requires the PRD caveat substring to survive unchanged.");
    }

    private async Task EnsureTenantActiveAsync(string tenantId, string displayName)
    {
        using HttpResponseMessage provisionResponse = await _fixture.MemoriesClient
            .PostAsJsonAsync(
                "/api/tenants",
                new TenantProvisioningInput(tenantId, displayName),
                MemoriesJsonContext.Options);

        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(ActivationTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage tenantResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/tenants/{tenantId}");

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

    private async Task WaitForIngestionCompletionAsync(string instanceId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(IngestionTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage statusResponse = await _fixture.MemoriesClient
                .GetAsync($"/api/ingest/{instanceId}");

            if (statusResponse.StatusCode == HttpStatusCode.OK)
            {
                string body = await statusResponse.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("status", out JsonElement status))
                {
                    string? statusValue = status.GetString();
                    if (string.Equals(statusValue, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(statusValue, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        false.ShouldBeTrue($"Ingestion instance '{instanceId}' did not complete within {IngestionTimeout}.");
    }

    private sealed record AcceptedResponse(string InstanceId);
}
