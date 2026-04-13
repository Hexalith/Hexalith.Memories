// <copyright file="CaseEndpointIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Cases;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;

using NFalkorDB;

using Shouldly;

using StackExchange.Redis;

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

    [Fact]
    public async Task PutMember_ThenList_ThenDelete_ShouldRoundTrip()
    {
        // Create a case first
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Member Test Case", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        string caseId = createdCase.Id;

        // PUT member -- should return 201 (new)
        var memberInput = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        using HttpResponseMessage putResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members/user-alice",
            memberInput,
            MemoriesJsonContext.Options);
        putResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        CaseMember? addedMember = await putResponse.Content.ReadFromJsonAsync<CaseMember>(MemoriesJsonContext.Options);
        addedMember.ShouldNotBeNull();
        addedMember.MemberId.ShouldBe("user-alice");
        addedMember.MemberType.ShouldBe(CaseMemberType.User);

        // GET members -- should return one member
        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<CaseMember>? members = await listResponse.Content.ReadFromJsonAsync<List<CaseMember>>(MemoriesJsonContext.Options);
        members.ShouldNotBeNull();
        members.Count.ShouldBe(1);
        members[0].MemberId.ShouldBe("user-alice");

        // DELETE member -- should return 204
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members/user-alice");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // GET members after delete -- should return empty
        using HttpResponseMessage listAfterDelete = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members");
        listAfterDelete.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<CaseMember>? membersAfterDelete = await listAfterDelete.Content.ReadFromJsonAsync<List<CaseMember>>(MemoriesJsonContext.Options);
        membersAfterDelete.ShouldNotBeNull();
        membersAfterDelete.ShouldBeEmpty();
    }

    [Fact]
    public async Task PutMember_Idempotent_ShouldReturn201Then200()
    {
        // Create a case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Idempotent Member Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        string caseId = createdCase.Id;

        var memberInput = new AddCaseMemberInput("user-bob", CaseMemberType.User);

        // First PUT -- 201
        using HttpResponseMessage firstPut = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members/user-bob",
            memberInput,
            MemoriesJsonContext.Options);
        firstPut.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second PUT (same member) -- 200 (idempotent)
        using HttpResponseMessage secondPut = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members/user-bob",
            memberInput,
            MemoriesJsonContext.Options);
        secondPut.StatusCode.ShouldBe(HttpStatusCode.OK);

        // List should show exactly one entry
        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members");
        List<CaseMember>? members = await listResponse.Content.ReadFromJsonAsync<List<CaseMember>>(MemoriesJsonContext.Options);
        members.ShouldNotBeNull();
        members.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PutMember_WhenBodyOmitsMemberType_ShouldReturn400()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Missing MemberType Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        using StringContent requestBody = new("{\"memberId\":\"user-alice\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/user-alice",
            requestBody);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("INVALID_MEMBER_TYPE");
    }

    [Fact]
    public async Task PutMember_WhenCaseNotFound_ShouldReturn404()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        var memberInput = new AddCaseMemberInput("user-alice", CaseMemberType.User);

        using HttpResponseMessage response = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/nonexistent-case/members/user-alice",
            memberInput,
            MemoriesJsonContext.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("CASE_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteMember_WhenNotFound_ShouldReturn404()
    {
        // Create a case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // DELETE a member that doesn't exist
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/nonexistent-user");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MEMBER_NOT_FOUND");
    }

    [Fact]
    public async Task ListCasesAsync_AfterAddingMembers_ShouldNotCountMembersKeyAsCase()
    {
        // Create a case and add members, then verify list returns exactly one case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Members Key Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Add a member (creates :members key)
        var memberInput = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        using HttpResponseMessage putResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/user-alice",
            memberInput,
            MemoriesJsonContext.Options);
        putResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // List cases -- should return exactly one case (not counting :members key)
        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<CaseRecord>? cases = await listResponse.Content.ReadFromJsonAsync<List<CaseRecord>>(MemoriesJsonContext.Options);
        cases.ShouldNotBeNull();
        cases.Count.ShouldBe(1);
        cases[0].Id.ShouldBe(createdCase.Id);
    }

    [Fact]
    public async Task GetCaseStatus_AfterAddingMembers_ShouldIncludeMemberCount()
    {
        // Create a case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Status Member Count", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Add two members
        var member1 = new AddCaseMemberInput("user-alice", CaseMemberType.User);
        var member2 = new AddCaseMemberInput("admin-role", CaseMemberType.Role);
        using HttpResponseMessage put1 = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/user-alice",
            member1,
            MemoriesJsonContext.Options);
        put1.StatusCode.ShouldBe(HttpStatusCode.Created);
        using HttpResponseMessage put2 = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/admin-role",
            member2,
            MemoriesJsonContext.Options);
        put2.StatusCode.ShouldBe(HttpStatusCode.Created);

        // GET status -- should include memberCount=2
        using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/status");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        CaseStatusDetail? status = await statusResponse.Content.ReadFromJsonAsync<CaseStatusDetail>(MemoriesJsonContext.Options);
        status.ShouldNotBeNull();
        status.MemberCount.ShouldBe(2);
    }

    [Fact]
    public async Task PutMember_WhenLimitReached_ShouldReturn400MemberLimitExceeded()
    {
        // Create a case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Limit Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        string caseId = createdCase.Id;

        // Add 1000 members via direct Redis (faster than 1000 HTTP calls)
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        string membersKey = $"{tenantId}:case:{caseId}:members";
        for (int i = 0; i < 1000; i++)
        {
            string memberId = $"user-{i:D4}";
            string json = $"{{\"memberId\":\"{memberId}\",\"memberType\":\"user\",\"addedAt\":\"2026-04-12T00:00:00+00:00\"}}";
            await redisDb.HashSetAsync(membersKey, memberId, json);
        }

        // The 1001st member via HTTP should return 400
        var memberInput = new AddCaseMemberInput("user-overflow", CaseMemberType.User);
        using HttpResponseMessage overflowResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{caseId}/members/user-overflow",
            memberInput,
            MemoriesJsonContext.Options);

        overflowResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await overflowResponse.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("MEMBER_LIMIT_EXCEEDED");
    }

    [Fact]
    public async Task PutMember_WhenCaseAtLimitButMemberAlreadyExists_ShouldReturn200()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Limit Replay Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        string membersKey = $"{tenantId}:case:{createdCase.Id}:members";
        for (int i = 0; i < 1000; i++)
        {
            string memberId = $"user-{i:D4}";
            string json = $"{{\"memberId\":\"{memberId}\",\"memberType\":\"user\",\"addedAt\":\"2026-04-12T00:00:00+00:00\"}}";
            await redisDb.HashSetAsync(membersKey, memberId, json);
        }

        var replayInput = new AddCaseMemberInput("user-0000", CaseMemberType.User);
        using HttpResponseMessage replayResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/user-0000",
            replayInput,
            MemoriesJsonContext.Options);

        replayResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        CaseMember? replayedMember = await replayResponse.Content.ReadFromJsonAsync<CaseMember>(MemoriesJsonContext.Options);
        replayedMember.ShouldNotBeNull();
        replayedMember.MemberId.ShouldBe("user-0000");
    }

    [Fact]
    public async Task PutMember_ConcurrentSameMember_ShouldProduceExactlyOneMemberAddedEvent()
    {
        // Create a case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Concurrency Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            caseInput,
            MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();
        string caseId = createdCase.Id;

        // Fire 10 parallel PUTs for the same memberId
        var memberInput = new AddCaseMemberInput("user-concurrent", CaseMemberType.User);
        Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 10)
            .Select(_ => _fixture.MemoriesClient.PutAsJsonAsync(
                $"/api/tenants/{tenantId}/cases/{caseId}/members/user-concurrent",
                memberInput,
                MemoriesJsonContext.Options))
            .ToArray();

        HttpResponseMessage[] responses = await Task.WhenAll(tasks);

        // Exactly one should be 201, the rest 200
        int createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        int okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        createdCount.ShouldBe(1);
        okCount.ShouldBe(9);

        foreach (HttpResponseMessage resp in responses)
        {
            resp.Dispose();
        }

        // Verify exactly 1 MemberAdded event in the activity stream
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        string activityKey = $"{tenantId}:case:{caseId}:activity";
        StreamEntry[] entries = await redisDb.StreamRangeAsync(activityKey);
        int memberAddedCount = entries.Count(e =>
            e.Values.Any(v => v.Name == "type" && v.Value.ToString() == "memberAdded"));
        memberAddedCount.ShouldBe(1);
    }

    // --- Deletion integration tests ---

    [Fact]
    public async Task DeleteMemoryUnit_Roundtrip_ShouldReturn204AndRemoveFromCase()
    {
        // Create case
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string searchToken = $"delete-mu-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete MU Test", null);
        using HttpResponseMessage createCaseResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createCaseResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createCaseResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Ingest MU
        IngestionInput ingestionInput = new()
        {
            TenantId = tenantId,
            CaseId = createdCase.Id,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = Encoding.UTF8.GetBytes($"{searchToken} content for integration"),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };
        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        AcceptedResponse? accepted = await ingestResponse.Content.ReadFromJsonAsync<AcceptedResponse>(MemoriesJsonContext.Options);
        accepted.ShouldNotBeNull();

        // Wait for indexing
        await WaitForContainsEdgeAsync(tenantId, createdCase.Id);

        // Find the MU ID from the graph
        string muId = await GetFirstMemoryUnitIdAsync(tenantId, createdCase.Id);
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{muId}")).ShouldBeTrue();

        // Delete MU
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/memory-units/{muId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify case now has 0 MUs
        using HttpResponseMessage getResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        CaseRecord? reloaded = await getResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        reloaded.ShouldNotBeNull();
        reloaded.MemoryUnitCount.ShouldBe(0);

        // Verify search no longer returns the deleted MU
        using HttpResponseMessage searchAfterResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/search?tenantId={tenantId}&caseId={createdCase.Id}&query={Uri.EscapeDataString(searchToken)}");
        searchAfterResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        SearchResult? searchAfter = await searchAfterResponse.Content.ReadFromJsonAsync<SearchResult>(MemoriesJsonContext.Options);
        searchAfter.ShouldNotBeNull();
        searchAfter.TotalCount.ShouldBe(0);
        searchAfter.Results.ShouldNotContain(r => r.MemoryUnitId == muId);
    }

    [Fact]
    public async Task DeleteMemoryUnit_NotFound_ShouldReturn404()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete MU 404 Test", null);
        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/memory-units/nonexistent-mu-id");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMemoryUnit_WrongCase_ShouldReturn404AndLeaveOriginalMemoryUnit()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string searchToken = $"wrong-case-{Guid.NewGuid():N}";

        using HttpResponseMessage createCaseAResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            new CreateCaseInput("ignored", "Delete MU Case A", null),
            MemoriesJsonContext.Options);
        createCaseAResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? caseA = await createCaseAResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        caseA.ShouldNotBeNull();

        using HttpResponseMessage createCaseBResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases",
            new CreateCaseInput("ignored", "Delete MU Case B", null),
            MemoriesJsonContext.Options);
        createCaseBResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? caseB = await createCaseBResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        caseB.ShouldNotBeNull();

        IngestionInput ingestionInput = new()
        {
            TenantId = tenantId,
            CaseId = caseA.Id,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = Encoding.UTF8.GetBytes($"{searchToken} content for wrong case delete"),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForContainsEdgeAsync(tenantId, caseA.Id);
        string muId = await GetFirstMemoryUnitIdAsync(tenantId, caseA.Id);

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{caseB.Id}/memory-units/{muId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using HttpResponseMessage getCaseAResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{caseA.Id}");
        getCaseAResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        CaseRecord? reloadedCaseA = await getCaseAResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        reloadedCaseA.ShouldNotBeNull();
        reloadedCaseA.MemoryUnitCount.ShouldBe(1);

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{muId}")).ShouldBeTrue();
        (await QueryGraphCountAsync(
            tenantId,
            "MATCH (m:MemoryUnit {id: $muId}) RETURN count(m) AS cnt",
            new Dictionary<string, object> { ["muId"] = muId })).ShouldBe(1);
    }

    [Fact]
    public async Task GetCaseStatus_WhenDeleting_ShouldReturnDeletingWithDeletionStartedAt()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        DateTimeOffset deletionStartedAt = DateTimeOffset.Parse("2026-04-13T09:00:00+00:00");
        CreateCaseInput caseInput = new("ignored", "Deleting Status Test", null);

        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        await redisDb.HashSetAsync(
            $"{tenantId}:case:{createdCase.Id}",
            [
                new HashEntry("status", "deleting"),
                new HashEntry("deletionStartedAt", deletionStartedAt.ToString("o")),
            ]);

        using HttpResponseMessage statusResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/status");
        statusResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        CaseStatusDetail? status = await statusResponse.Content.ReadFromJsonAsync<CaseStatusDetail>(MemoriesJsonContext.Options);
        status.ShouldNotBeNull();
        status.Status.ShouldBe(CaseStatus.Deleting);
        status.DeletionStartedAt.ShouldBe(deletionStartedAt);
    }

    [Fact]
    public async Task DeleteMemoryUnit_WhenCaseDeleting_ShouldReturn409()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete MU Conflict Test", null);

        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        IngestionInput ingestionInput = new()
        {
            TenantId = tenantId,
            CaseId = createdCase.Id,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = "case deleting conflict content"u8.ToArray(),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForContainsEdgeAsync(tenantId, createdCase.Id);
        string muId = await GetFirstMemoryUnitIdAsync(tenantId, createdCase.Id);

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        await redisDb.HashSetAsync(
            $"{tenantId}:case:{createdCase.Id}",
            [
                new HashEntry("status", "deleting"),
                new HashEntry("deletionStartedAt", DateTimeOffset.UtcNow.ToString("o")),
            ]);

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/memory-units/{muId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        ErrorResponse? error = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>(MemoriesJsonContext.Options);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("CASE_DELETING");
    }

    [Fact]
    public async Task DeleteCase_EmptyCase_ShouldReturn204AndRemoveCase()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete Empty Case Test", null);
        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Delete empty case
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify case is gone
        using HttpResponseMessage getResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCase_WithIndexedMemoryUnits_ShouldRemoveAllBackendState()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        string searchToken = $"delete-case-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete Populated Case Test", null);

        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        IngestionInput ingestionInput = new()
        {
            TenantId = tenantId,
            CaseId = createdCase.Id,
            SourceUri = $"file:///{Guid.NewGuid():N}.txt",
            ContentBytes = Encoding.UTF8.GetBytes($"{searchToken} content for delete case"),
            ContentType = "text/plain",
            SourceType = SourceType.File,
            IngestedBy = "integration@test.local",
        };

        using HttpResponseMessage ingestResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            "/api/ingest", ingestionInput, MemoriesJsonContext.Options);
        ingestResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        await WaitForContainsEdgeAsync(tenantId, createdCase.Id);
        string muId = await GetFirstMemoryUnitIdAsync(tenantId, createdCase.Id);

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using HttpResponseMessage getResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        (await redisDb.KeyExistsAsync($"{tenantId}:mu:{muId}")).ShouldBeFalse();
        (await redisDb.KeyExistsAsync($"{tenantId}:vec:{muId}")).ShouldBeFalse();

        (await QueryGraphCountAsync(
            tenantId,
            "MATCH (m:MemoryUnit {id: $muId}) RETURN count(m) AS cnt",
            new Dictionary<string, object> { ["muId"] = muId })).ShouldBe(0);
        (await QueryGraphCountAsync(
            tenantId,
            "MATCH (c:Case {id: $caseId}) RETURN count(c) AS cnt",
            new Dictionary<string, object> { ["caseId"] = createdCase.Id })).ShouldBe(0);
    }

    [Fact]
    public async Task DeleteCase_WithMembers_ShouldReturn204AndCleanUp()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete Case Members Test", null);
        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Add a member
        var memberInput = new AddCaseMemberInput("user-cleanup-test", CaseMemberType.User);
        using HttpResponseMessage putResponse = await _fixture.MemoriesClient.PutAsJsonAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}/members/user-cleanup-test",
            memberInput, MemoriesJsonContext.Options);
        putResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Delete case
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify case is gone
        using HttpResponseMessage getResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify members key is cleaned up
        IDatabase redisDb = _fixture.RedisConnection.GetDatabase();
        string membersKey = $"{tenantId}:case:{createdCase.Id}:members";
        bool membersExist = await redisDb.KeyExistsAsync(membersKey);
        membersExist.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteCase_NotFound_ShouldReturn404()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";

        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/nonexistent-case-id");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCase_ListCasesShouldNoLongerIncludeDeletedCase()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Delete Case List Test", null);
        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // Delete case
        using HttpResponseMessage deleteResponse = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify list no longer returns it
        using HttpResponseMessage listResponse = await _fixture.MemoriesClient.GetAsync(
            $"/api/tenants/{tenantId}/cases");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<CaseRecord>? cases = await listResponse.Content.ReadFromJsonAsync<List<CaseRecord>>(MemoriesJsonContext.Options);
        cases.ShouldNotBeNull();
        cases.ShouldNotContain(c => c.Id == createdCase.Id);
    }

    [Fact]
    public async Task DeleteCase_Idempotent_SecondDeleteReturns404()
    {
        string tenantId = $"tenant-{Guid.NewGuid():N}";
        CreateCaseInput caseInput = new("ignored", "Idempotent Delete Test", null);
        using HttpResponseMessage createResponse = await _fixture.MemoriesClient.PostAsJsonAsync(
            $"/api/tenants/{tenantId}/cases", caseInput, MemoriesJsonContext.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        CaseRecord? createdCase = await createResponse.Content.ReadFromJsonAsync<CaseRecord>(MemoriesJsonContext.Options);
        createdCase.ShouldNotBeNull();

        // First delete
        using HttpResponseMessage deleteResponse1 = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse1.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Second delete
        using HttpResponseMessage deleteResponse2 = await _fixture.MemoriesClient.DeleteAsync(
            $"/api/tenants/{tenantId}/cases/{createdCase.Id}");
        deleteResponse2.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<string> GetFirstMemoryUnitIdAsync(string tenantId, string caseId)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.QueryAsync(
            tenantId,
            "MATCH (:Case {id: $caseId})-[:CONTAINS]->(m:MemoryUnit) RETURN m.id AS muId",
            new Dictionary<string, object> { ["caseId"] = caseId });

        result.Count.ShouldBeGreaterThan(0, "No memory units found in graph for case");
        var enumerator = result.GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        return enumerator.Current.GetValue<string>("muId");
    }

    private async Task<long> QueryGraphCountAsync(string tenantId, string query, IDictionary<string, object> parameters)
    {
        FalkorDB falkor = new(_fixture.FalkorDbConnection.GetDatabase());
        ResultSet result = await falkor.QueryAsync(tenantId, query, parameters);
        return ReadCount(result);
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
