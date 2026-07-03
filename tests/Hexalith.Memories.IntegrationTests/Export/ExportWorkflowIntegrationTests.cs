// <copyright file="ExportWorkflowIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Export;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using NFalkorDB;

using Shouldly;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

/// <summary>
/// Story 8.3 — end-to-end integration for the case + tenant export endpoints against the
/// Aspire-hosted Redis Stack + FalkorDB + Dapr sidecar.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class ExportWorkflowIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    /// <summary>Initializes a new instance of the <see cref="ExportWorkflowIntegrationTests"/> class.</summary>
    /// <param name="fixture">The Aspire integration fixture.</param>
    public ExportWorkflowIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    /// <summary>Ingest three units into one case, export the case, assert manifest + units + edges round-trip.</summary>
    [Fact]
    public async Task IngestThreeUnits_ExportCase_RoundTripsThroughStream()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-export-case-{Guid.NewGuid():N}");
        string caseId = await CreateCaseAsync(tenantId, "Case export integration");

        await IngestMemoryUnitAsync(tenantId, caseId, "case export first memory");
        await IngestMemoryUnitAsync(tenantId, caseId, "case export second memory");
        await IngestMemoryUnitAsync(tenantId, caseId, "case export third memory");
        await WaitForContainsEdgeAsync(tenantId, caseId, expectedCount: 3);
        await Task.Delay(TimeSpan.FromSeconds(1));

        IReadOnlyList<string> memoryUnitIds = await GetCaseMemoryUnitIdsAsync(tenantId, caseId);
        memoryUnitIds.Count.ShouldBeGreaterThanOrEqualTo(3);

        using JsonDocument document = await ReadExportDocumentAsync($"/api/tenants/{tenantId}/cases/{caseId}/export");
        JsonElement root = document.RootElement;

        JsonElement manifest = root.GetProperty("manifest");
        manifest.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        manifest.GetProperty("scope").GetString().ShouldBe("case");
        manifest.GetProperty("tenantId").GetString().ShouldBe(tenantId);
        manifest.GetProperty("caseId").GetString().ShouldBe(caseId);

        root.GetProperty("case").GetProperty("id").GetString().ShouldBe(caseId);

        JsonElement memoryUnits = root.GetProperty("memoryUnits");
        memoryUnits.GetArrayLength().ShouldBeGreaterThanOrEqualTo(3);
        foreach (string memoryUnitId in memoryUnitIds)
        {
            ContainsMemoryUnit(memoryUnits, memoryUnitId).ShouldBeTrue();
        }

        root.GetProperty("edges").ValueKind.ShouldBe(JsonValueKind.Array);

        JsonElement statistics = root.GetProperty("statistics");
        statistics.GetProperty("memoryUnitCount").GetInt32().ShouldBeGreaterThanOrEqualTo(3);
        statistics.GetProperty("caseCount").GetInt32().ShouldBe(1);
    }

    /// <summary>Ingest two cases of units, export the tenant, assert all cases + units + edges are present.</summary>
    [Fact]
    public async Task IngestTwoCases_ExportTenant_ReturnsEverything()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync($"tenant-export-all-{Guid.NewGuid():N}");
        string firstCaseId = await CreateCaseAsync(tenantId, "Tenant export first case");
        string secondCaseId = await CreateCaseAsync(tenantId, "Tenant export second case");

        await IngestMemoryUnitAsync(tenantId, firstCaseId, "tenant export first case memory");
        await IngestMemoryUnitAsync(tenantId, secondCaseId, "tenant export second case memory");
        await WaitForContainsEdgeAsync(tenantId, firstCaseId, expectedCount: 1);
        await WaitForContainsEdgeAsync(tenantId, secondCaseId, expectedCount: 1);
        await Task.Delay(TimeSpan.FromSeconds(1));

        using JsonDocument document = await ReadExportDocumentAsync($"/api/tenants/{tenantId}/export");
        JsonElement root = document.RootElement;

        JsonElement manifest = root.GetProperty("manifest");
        manifest.GetProperty("schemaVersion").GetInt32().ShouldBe(1);
        manifest.GetProperty("scope").GetString().ShouldBe("tenant");
        manifest.GetProperty("tenantId").GetString().ShouldBe(tenantId);
        manifest.GetProperty("caseId").ValueKind.ShouldBe(JsonValueKind.Null);

        JsonElement cases = root.GetProperty("cases");
        cases.GetArrayLength().ShouldBeGreaterThanOrEqualTo(2);
        ContainsCase(cases, firstCaseId).ShouldBeTrue();
        ContainsCase(cases, secondCaseId).ShouldBeTrue();

        JsonElement memoryUnits = root.GetProperty("memoryUnits");
        memoryUnits.GetArrayLength().ShouldBeGreaterThanOrEqualTo(2);
        root.GetProperty("edges").ValueKind.ShouldBe(JsonValueKind.Array);

        JsonElement statistics = root.GetProperty("statistics");
        statistics.GetProperty("caseCount").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
        statistics.GetProperty("memoryUnitCount").GetInt32().ShouldBeGreaterThanOrEqualTo(2);
    }

    private async Task<string> CreateCaseAsync(string tenantId, string caseName)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            new CreateCaseInput("ignored", caseName, null),
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseRecord? createdCase = await response.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        return createdCase.Id;
    }

    private async Task IngestMemoryUnitAsync(string tenantId, string caseId, string content)
    {
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = Encoding.UTF8.GetBytes(content),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest",
            input,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    private async Task WaitForContainsEdgeAsync(string tenantId, string caseId, int expectedCount)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());

        while (DateTimeOffset.UtcNow < deadline)
        {
            ResultSet result = await falkor.QueryAsync(
                tenantId,
                "MATCH (:Case {id: $caseId})-[r:CONTAINS]->(:MemoryUnit) RETURN count(r) AS cnt",
                new Dictionary<string, object> { ["caseId"] = caseId });

            if (ReadCount(result) >= expectedCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new TimeoutException($"Case '{caseId}' did not reach {expectedCount} CONTAINS edges in time.");
    }

    private async Task<IReadOnlyList<string>> GetCaseMemoryUnitIdsAsync(string tenantId, string caseId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.QueryAsync(
            tenantId,
            "MATCH (:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS muId",
            new Dictionary<string, object> { ["caseId"] = caseId });

        List<string> ids = [];
        foreach (Record record in result)
        {
            ids.Add(record.GetValue<string>("muId"));
        }

        return ids;
    }

    private async Task<JsonDocument> ReadExportDocumentAsync(string path)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync(path);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        string json = await response.Content.ReadAsStringAsync();
        json.ShouldNotBeNullOrWhiteSpace();
        return JsonDocument.Parse(json);
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);
        using IEnumerator<Record> enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    private static bool ContainsMemoryUnit(JsonElement memoryUnits, string memoryUnitId)
    {
        foreach (JsonElement memoryUnit in memoryUnits.EnumerateArray())
        {
            string? id = memoryUnit.GetProperty("unit").GetProperty("id").GetString();
            if (string.Equals(id, memoryUnitId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsCase(JsonElement cases, string caseId)
    {
        foreach (JsonElement caseElement in cases.EnumerateArray())
        {
            string? id = caseElement.GetProperty("id").GetString();
            if (string.Equals(id, caseId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
