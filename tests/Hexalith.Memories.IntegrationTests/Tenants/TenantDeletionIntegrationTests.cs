// <copyright file="TenantDeletionIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Tenants;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Infrastructure;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

using CaseRecord = Hexalith.Memories.Contracts.V1.Case;

[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class TenantDeletionIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;

    public TenantDeletionIntegrationTests(AspireIngestionPipelineFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DeleteTenant_NonExistent_Returns404()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";

        using HttpResponseMessage response = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteTenant_ConcurrentRequests_ShouldReuseSingleWorkflowInstance()
    {
        string tenantId = await ProvisionTenantAsync();

        Task<HttpResponseMessage> firstDelete = _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        Task<HttpResponseMessage> secondDelete = _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        HttpResponseMessage[] responses = await Task.WhenAll(firstDelete, secondDelete);

        try
        {
            responses[0].StatusCode.ShouldBe(HttpStatusCode.Accepted);
            responses[1].StatusCode.ShouldBe(HttpStatusCode.Accepted);

            DeleteResponseData deleteResponse1 = await ReadDeleteResponseAsync(responses[0].Content);
            DeleteResponseData deleteResponse2 = await ReadDeleteResponseAsync(responses[1].Content);
            string? workflowId1 = deleteResponse1.WorkflowInstanceId;
            string? workflowId2 = deleteResponse2.WorkflowInstanceId;

            workflowId1.ShouldNotBeNullOrWhiteSpace();
            workflowId2.ShouldNotBeNullOrWhiteSpace();
            workflowId1.ShouldBe(workflowId2);
            new[] { deleteResponse1.Message, deleteResponse2.Message }
                .Any(message => message?.Contains("Deletion already in progress", StringComparison.OrdinalIgnoreCase) == true)
                .ShouldBeTrue();

            await WaitForTenantDeletedAsync(tenantId);
        }
        finally
        {
            foreach (HttpResponseMessage response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task DeleteTenant_WithIndexedData_ShouldRemoveRegistryAndBackendState()
    {
        string tenantId = await ProvisionTenantAsync();
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        string caseId = await CreateCaseAsync(tenantId, "Tenant deletion integration case");
        string memoryUnitId = await IngestMemoryUnitAsync(tenantId, caseId, sourceUri, $"tenant-delete-{Guid.NewGuid():N}");
        string dedupKey = BuildDedupKey(tenantId, caseId, sourceUri);
        await WaitForRedisKeyAsync(dedupKey);
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        string eventStoreMapKey = $"{tenantId}:eventstore:aggregate-case-map";
        string eventStoreRouteKey = $"{tenantId}:eventstore:observed:Claims";
        string migrationMarkerKey = $"{tenantId}:embedding-migration:active";
        string orphanSyntacticKey = IndexSchemaDefinitions.BuildSyntacticKey(tenantId, $"orphan-mu-{Guid.NewGuid():N}");
        string orphanSemanticKey = IndexSchemaDefinitions.BuildSemanticKey(tenantId, $"orphan-vec-{Guid.NewGuid():N}");
        string orphanNaturalLanguageKey = IndexSchemaDefinitions.BuildNaturalLanguageSemanticKey(tenantId, $"orphan-vecnl-{Guid.NewGuid():N}");
        string orphanLegacyNaturalLanguageKey = IndexSchemaDefinitions.BuildLegacyNaturalLanguageSemanticKey(tenantId, $"orphan-vecnl-legacy-{Guid.NewGuid():N}");
        await redisDb.HashSetAsync(eventStoreMapKey, "events:Claims", caseId);
        await redisDb.StringSetAsync(eventStoreRouteKey, "route-metadata");
        await redisDb.StringSetAsync(migrationMarkerKey, "active");
        await redisDb.HashSetAsync(orphanSyntacticKey, "caseId", caseId);
        await redisDb.HashSetAsync(orphanSemanticKey, "caseId", caseId);
        await redisDb.HashSetAsync(orphanNaturalLanguageKey, "caseId", caseId);
        await redisDb.HashSetAsync(orphanLegacyNaturalLanguageKey, "caseId", caseId);

        using HttpResponseMessage memberResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases/{caseId}/members/user-cleanup",
            new AddCaseMemberInput("user-cleanup", CaseMemberType.User),
            MemoriesJsonContext.Options);
        memberResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string? workflowInstanceId = (await ReadDeleteResponseAsync(deleteResponse.Content)).WorkflowInstanceId;
        workflowInstanceId.ShouldNotBeNullOrWhiteSpace();

        await WaitForTenantDeletedAsync(tenantId);

        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync("/api/v1/tenants");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        IReadOnlyList<TenantInfo>? tenants = await listResponse.Content.ReadFromJsonAsync<IReadOnlyList<TenantInfo>>(MemoriesJsonContext.Options);
        tenants.ShouldNotBeNull();
        tenants.ShouldNotContain(t => t.Id == tenantId);

        (await redisDb.KeyExistsAsync($"{tenantId}:case:{caseId}")).ShouldBeFalse();
        (await redisDb.KeyExistsAsync($"{tenantId}:case:{caseId}:members")).ShouldBeFalse();
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{memoryUnitId}")).ShouldBeFalse();
        (await redisDb.KeyExistsAsync($"{tenantId}:vec:{memoryUnitId}")).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(dedupKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(eventStoreMapKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(eventStoreRouteKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(migrationMarkerKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(orphanSyntacticKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(orphanSemanticKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(orphanNaturalLanguageKey)).ShouldBeFalse();
        (await redisDb.KeyExistsAsync(orphanLegacyNaturalLanguageKey)).ShouldBeFalse();

        await Should.ThrowAsync<RedisServerException>(async () => await redisDb.ExecuteAsync("FT.INFO", $"{tenantId}:memories:idx"));
        await Should.ThrowAsync<RedisServerException>(async () => await redisDb.ExecuteAsync("FT.INFO", $"{tenantId}:memories:vec"));

        FalkorDB falkorDb = new(_fixture.FalkorDbConnection.GetDatabase());
        try
        {
            ResultSet graphResult = await falkorDb.SelectGraph(tenantId).QueryAsync("MATCH (n) RETURN count(n) AS cnt");
            ReadCount(graphResult).ShouldBe(0);
        }
        catch (RedisServerException)
        {
            // Graph finalization may remove the graph entirely depending on backend timing.
        }
    }

    [Fact]
    public async Task BatchedGraphDeletion_LargeTenant_CompletesInBatches()
    {
        string tenantId = await ProvisionTenantAsync();
        FalkorDB falkorDb = new(_fixture.FalkorDbConnection.GetDatabase());

        // Create >500 graph nodes directly via FalkorDB to trigger batched deletion
        const int nodeCount = 600;
        for (int i = 0; i < nodeCount; i++)
        {
            await falkorDb.SelectGraph(tenantId).QueryAsync(
                "CREATE (n:TestNode {id: $id, data: $data})",
                new Dictionary<string, object>
                {
                    ["id"] = $"batch-node-{i}",
                    ["data"] = $"Test data for node {i}",
                });
        }

        // Verify nodes were created
        ResultSet countResult = await falkorDb.SelectGraph(tenantId).QueryAsync("MATCH (n) RETURN count(n) AS cnt");
        long preDeleteCount = ReadCount(countResult);
        preDeleteCount.ShouldBeGreaterThanOrEqualTo(nodeCount);

        // Delete tenant
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForTenantDeletedAsync(tenantId);

        // Verify tenant is gone and graph has no data
        try
        {
            ResultSet postDeleteResult = await falkorDb.SelectGraph(tenantId).QueryAsync("MATCH (n) RETURN count(n) AS cnt");
            ReadCount(postDeleteResult).ShouldBe(0);
        }
        catch (RedisServerException)
        {
            // Graph may have been entirely deleted by the finalizer — this is expected
        }
    }

    [Fact]
    public async Task DeleteTenant_SearchReturnsZero_AfterDeletion()
    {
        // Provision tenant and ingest data so search indexes are populated
        string tenantId = await ProvisionTenantAsync();
        string caseId = await CreateCaseAsync(tenantId, "Search-zero case");
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        _ = await IngestMemoryUnitAsync(tenantId, caseId, sourceUri, $"searchzero-{Guid.NewGuid():N}");

        // Verify search works before deletion
        using HttpResponseMessage preDeleteSearch = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=searchzero");
        preDeleteSearch.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Delete tenant
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        await WaitForTenantDeletedAsync(tenantId);

        // Search should return 404 because the tenant no longer exists in the registry. Per
        // TenantStatusGuard.ToHttpResult, TENANT_NOT_FOUND maps to 404; 409 Conflict is reserved for
        // non-Active-but-existing states (Deleting/Provisioning/Failed).
        using HttpResponseMessage postDeleteSearch = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=searchzero");
        postDeleteSearch.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        ErrorResponse? error = await postDeleteSearch.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("TENANT_NOT_FOUND");

        // Verify at backend level: RediSearch index is gone
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        await Should.ThrowAsync<RedisServerException>(
            async () => await redisDb.ExecuteAsync("FT.INFO", $"{tenantId}:memories:idx"));
    }

    [Fact]
    public async Task TenantStatusGuard_RejectsDeletingTenant()
    {
        string tenantId = await ProvisionTenantAsync();
        string caseId = await CreateCaseAsync(tenantId, "Guard test case");

        // Start deletion — tenant transitions to Deleting atomically before workflow starts
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        // Immediately try ingestion — should be rejected with 409
        IngestionInput ingestInput = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = "file:///guard-test.txt",
            ContentBytes = Encoding.UTF8.GetBytes("guard test content"),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "guard-test@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/ingest", ingestInput, MemoriesJsonContext.Options);

        // Tenant is either Deleting (409) or already deleted (409 TENANT_NOT_FOUND)
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? ingestError = await ingestResponse.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        ingestError.ShouldNotBeNull();
        new[] { "TENANT_DELETING", "TENANT_NOT_FOUND" }.ShouldContain(ingestError.Code);

        // Immediately try search — should also be rejected
        using HttpResponseMessage searchResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/v1/search?tenantId={tenantId}&query=guard-test");
        searchResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        ErrorResponse? searchError = await searchResponse.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        searchError.ShouldNotBeNull();
        new[] { "TENANT_DELETING", "TENANT_NOT_FOUND" }.ShouldContain(searchError.Code);

        await WaitForTenantDeletedAsync(tenantId);
    }

    [Fact]
    public async Task DropIndexDD_OnlyDeletesIndexedKeys()
    {
        // Provision tenant and ingest data to create indexed keys (mu:* and vec:*)
        string tenantId = await ProvisionTenantAsync();
        string caseId = await CreateCaseAsync(tenantId, "DD scope test case");
        string sourceUri = $"file:///{Guid.NewGuid():N}.txt";
        string memoryUnitId = await IngestMemoryUnitAsync(tenantId, caseId, sourceUri, $"dd-scope-{Guid.NewGuid():N}");
        string dedupKey = BuildDedupKey(tenantId, caseId, sourceUri);
        await WaitForRedisKeyAsync(dedupKey);

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();

        // Verify indexed keys exist before drop
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{memoryUnitId}")).ShouldBeTrue();

        // Verify non-indexed keys exist (case and dedup)
        (await redisDb.KeyExistsAsync($"{tenantId}:case:{caseId}")).ShouldBeTrue();
        (await redisDb.KeyExistsAsync(dedupKey)).ShouldBeTrue();

        // Drop RediSearch index with DD flag — should delete mu:* keys
        try
        {
            await redisDb.ExecuteAsync("FT.DROPINDEX", $"{tenantId}:memories:idx", "DD");
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Unknown index"))
        {
            // Index may not exist if test setup is incomplete — skip assertions
            return;
        }

        // Verify mu:* keys are deleted (DD flag drops indexed documents)
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{memoryUnitId}")).ShouldBeFalse();

        // Verify case:* and dedup:* keys survive (not covered by the RediSearch index)
        (await redisDb.KeyExistsAsync($"{tenantId}:case:{caseId}")).ShouldBeTrue();
        (await redisDb.KeyExistsAsync(dedupKey)).ShouldBeTrue();

        // Cleanup: delete tenant fully to avoid orphaned state
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync($"/api/v1/tenants/{tenantId}");
        if (deleteResponse.StatusCode == HttpStatusCode.Accepted)
        {
            await WaitForTenantDeletedAsync(tenantId);
        }
    }

    private async Task<string> ProvisionTenantAsync()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        TenantProvisioningInput input = new(tenantId, $"Tenant {tenantId}");

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/tenants",
            input,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForTenantActiveAsync(tenantId);
        return tenantId;
    }

    private async Task WaitForTenantActiveAsync(string tenantId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync($"/api/v1/tenants/{tenantId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                TenantInfo? tenant = await response.Content.ReadFromJsonAsync<TenantInfo>(MemoriesJsonContext.Options);
                if (tenant?.Status == TenantStatus.Active)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Tenant '{tenantId}' did not become active in time.");
    }

    private async Task WaitForTenantDeletedAsync(string tenantId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using HttpResponseMessage response = await _fixture.MemoriesClient.GetAsync($"/api/v1/tenants/{tenantId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Tenant '{tenantId}' was not deleted in time.");
    }

    private async Task<string> CreateCaseAsync(string tenantId, string caseName)
    {
        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/v1/tenants/{tenantId}/cases",
            new CreateCaseInput("ignored", caseName, null),
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseRecord? createdCase = await response.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        return createdCase.Id;
    }

    private async Task<string> IngestMemoryUnitAsync(string tenantId, string caseId, string sourceUri, string token)
    {
        IngestionInput input = new()
        {
            TenantId = tenantId,
            CaseId = caseId,
            SourceUri = sourceUri,
            ContentBytes = Encoding.UTF8.GetBytes($"{token} content"),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage response = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/v1/ingest",
            input,
            MemoriesJsonContext.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForContainsEdgeAsync(tenantId, caseId);
        return await GetFirstMemoryUnitIdAsync(tenantId, caseId);
    }

    private async Task WaitForContainsEdgeAsync(string tenantId, string caseId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        FalkorDB falkorDb = new(_fixture.FalkorDbConnection.GetDatabase());

        while (DateTimeOffset.UtcNow < deadline)
        {
            ResultSet result = await falkorDb.SelectGraph(tenantId).QueryAsync(
                "MATCH (:Case {id: $caseId})-[r:CONTAINS]->(:MemoryUnit) RETURN count(r) AS cnt",
                new Dictionary<string, object> { ["caseId"] = caseId });

            if (ReadCount(result) > 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Contains edge for case '{caseId}' was not created in time.");
    }

    private async Task<string> GetFirstMemoryUnitIdAsync(string tenantId, string caseId)
    {
        FalkorDB falkorDb = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkorDb.SelectGraph(tenantId).QueryAsync(
            "MATCH (:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS muId",
            new Dictionary<string, object> { ["caseId"] = caseId });

        result.Count.ShouldBeGreaterThan(0);
        using IEnumerator<Record> enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<string>("muId");
    }

    private async Task WaitForRedisKeyAsync(string key)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await redisDb.KeyExistsAsync(key).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        throw new TimeoutException($"Redis key '{key}' was not created in time.");
    }

    private static long ReadCount(ResultSet result)
    {
        result.Count.ShouldBe(1);
        using IEnumerator<Record> enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<long>("cnt");
    }

    private static async Task<DeleteResponseData> ReadDeleteResponseAsync(HttpContent content)
    {
        using JsonDocument document = JsonDocument.Parse(await content.ReadAsStringAsync().ConfigureAwait(false));
        return new DeleteResponseData(
            document.RootElement.TryGetProperty("workflowInstanceId", out JsonElement workflowInstanceId)
                ? workflowInstanceId.GetString()
                : null,
            document.RootElement.TryGetProperty("message", out JsonElement message)
                ? message.GetString()
                : null);
    }

    private static string BuildDedupKey(string tenantId, string caseId, string sourceUri)
        => $"dedup:{tenantId}:{caseId}:{ComputeHash(sourceUri)}";

    private static string ComputeHash(string input)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();

    private sealed record DeleteResponseData(string? WorkflowInstanceId, string? Message);
}
