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

/// <summary>Contains the nightly integration cases for CliSearchIntegrationTests.</summary>
public sealed partial class CliSearchIntegrationTests
{
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
            .PostAsJsonAsync("/api/v1/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        AcceptedResponse? accepted = await ingestResponse.Content
            .ReadFromJsonAsync<AcceptedResponse>(MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();

        await WaitForIngestionCompletionAsync(tenantId, accepted.InstanceId);

        using var http = new HttpClient
        {
            BaseAddress = _fixture.MemoriesClient.BaseAddress,
            Timeout = TimeSpan.FromSeconds(60),
        };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            AspireIngestionPipelineFixture.MintServerBearer(tenantId));
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
}
