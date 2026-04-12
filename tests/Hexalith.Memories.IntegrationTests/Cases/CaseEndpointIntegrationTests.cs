// <copyright file="CaseEndpointIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cases;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using NFalkorDB;

using Shouldly;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

/// <summary>HTTP integration tests for case creation and retrieval running inside the Aspire topology.</summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed partial class CaseEndpointIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public CaseEndpointIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PostCase_ThenList_ShouldReturnCreatedCase()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        DateTimeOffset beforeCreate = DateTimeOffset.UtcNow;
        CreateCaseInput input = new("ignored-body-tenant", "Claims Pilot", "First investigation case");

        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            input,
            MemoriesJsonContext.Options);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseRecord? created = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        created.ShouldNotBeNull();
        UlidPattern().IsMatch(created.Id).ShouldBeTrue();
        created.TenantId.ShouldBe(tenantId);
        created.Name.ShouldBe(input.Name);
        created.Description.ShouldBe(input.Description);
        created.Status.ShouldBe(CaseStatus.Active);
        created.MemoryUnitCount.ShouldBe(0);
        created.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeCreate.AddSeconds(-1));
        created.LastUpdated.ShouldBeGreaterThanOrEqualTo(created.CreatedAt);
        createResponse.Headers.Location.ShouldNotBeNull();
        createResponse.Headers.Location!.ToString().ShouldContain($"/api/tenants/{tenantId}/cases/{created.Id}");

        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync($"/api/tenants/{tenantId}/cases");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<CaseRecord>? cases = await listResponse.Content.ReadFromJsonAsync<List<CaseRecord>>(MemoriesJsonContext.Options);
        cases.ShouldNotBeNull();
        CaseRecord listed = cases.Single(item => item.Id == created.Id);
        listed.TenantId.ShouldBe(tenantId);
        listed.Name.ShouldBe(input.Name);
        listed.Description.ShouldBe(input.Description);
        listed.Status.ShouldBe(CaseStatus.Active);
        listed.MemoryUnitCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetCase_WhenMissing_ShouldReturnNotFoundErrorResponse()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string caseId = $"01{Guid.NewGuid():N}"[..26].ToUpperInvariant();

        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("CASE_NOT_FOUND");
    }

    [Fact]
    public async Task PostCase_ThenIngest_ShouldReportMemoryUnitCountAndContainsEdge()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput createInput = new(tenantId, "Claims Pilot", null);

        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            createInput,
            MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseRecord? created = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        created.ShouldNotBeNull();

        IngestionInput ingestionInput = new()
        {
            TenantId = tenantId,
            CaseId = created.Id,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = "case endpoint integration content"u8.ToArray(),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest",
            ingestionInput,
            MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        AcceptedResponse? accepted = await ingestResponse.Content.ReadFromJsonAsync<AcceptedResponse>(MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();
        accepted.InstanceId.ShouldNotBeNullOrWhiteSpace();

        await WaitForContainsEdgeAsync(tenantId, created.Id);

        using HttpResponseMessage getResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{created.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        CaseRecord? reloaded = await getResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        reloaded.ShouldNotBeNull();
        reloaded.MemoryUnitCount.ShouldBe(1);
    }

    private async Task WaitForContainsEdgeAsync(string tenantId, string caseId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        while (DateTimeOffset.UtcNow < deadline)
        {
            ResultSet result = await falkor.QueryAsync(
                tenantId,
                "MATCH (:Case {id: $caseId})-[r:CONTAINS]->(:MemoryUnit) RETURN count(r) as cnt",
                new Dictionary<string, object>
                {
                    ["caseId"] = caseId,
                }).ConfigureAwait(false);

            if (ReadCount(result) == 1)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Contains edge for case '{caseId}' was not created within the allotted time.");
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);
        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex UlidPattern();

    private sealed record AcceptedResponse(string InstanceId);
}
