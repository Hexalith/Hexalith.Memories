// <copyright file="MemoriesClientSearchTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.ClientRest;

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

public sealed class MemoriesClientSearchTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task HybridSearchAsync_OmitsServerDefaultMaxResults()
    {
        HybridSearchResult body = BuildHybrid();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.HybridSearchAsync(
            new HybridSearchRequest(TenantId: "t1", Query: "needle"),
            CancellationToken.None);

        handler.Requests.Count.ShouldBe(1);
        string? uri = handler.Requests[0].RequestUri?.ToString();
        uri.ShouldNotBeNull();
        uri.ShouldContain("tenantId=t1");
        uri.ShouldContain("axis=hybrid");
        uri.ShouldContain("query=needle");
        uri.ShouldNotContain("maxResults=10");
        uri.ShouldNotContain("caseId=");
        uri.ShouldNotContain("offset=");
        uri.ShouldNotContain("explain=true");
    }

    [Fact]
    public async Task HybridSearchAsync_IncludesExplainAndCaseWhenProvided()
    {
        HybridSearchResult body = BuildHybrid();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.HybridSearchAsync(
            new HybridSearchRequest(TenantId: "t1", Query: "needle", CaseId: "case-1", MaxResults: 25, Explain: true, TokenBudget: 1_500),
            CancellationToken.None);

        string? uri = handler.Requests[0].RequestUri?.ToString();
        uri.ShouldNotBeNull();
        uri.ShouldContain("caseId=case-1");
        uri.ShouldContain("maxResults=25");
        uri.ShouldContain("explain=true");
        uri.ShouldContain("tokenBudget=1500");
    }

    [Fact]
    public async Task SearchAsync_GraphAxisOmitsEmptyQuery()
    {
        SearchResult body = new() { Results = [], TotalCount = 0, HasIndexedMemoryUnits = true, Query = string.Empty };
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.SearchAsync(
            new SearchRequest(TenantId: "t1", Axis: "graph", Query: null),
            CancellationToken.None);

        string? uri = handler.Requests[0].RequestUri?.ToString();
        uri.ShouldNotBeNull();
        uri.ShouldContain("axis=graph");
        uri.ShouldNotContain("query=");
    }

    [Fact]
    public async Task SearchAsync_IncludesTokenBudgetWhenProvided()
    {
        SearchResult body = new() { Results = [], TotalCount = 0, HasIndexedMemoryUnits = true, Query = "needle" };
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.SearchAsync(
            new SearchRequest(TenantId: "t1", Axis: "syntactic", Query: "needle", TokenBudget: 750),
            CancellationToken.None);

        string? uri = handler.Requests[0].RequestUri?.ToString();
        uri.ShouldNotBeNull();
        uri.ShouldContain("tokenBudget=750");
    }

    [Fact]
    public async Task SearchAsync_NonSuccess_ThrowsMemoriesRemoteException()
    {
        string error = JsonSerializer.Serialize(
            new ErrorResponse("INVALID_INPUT", "bad", string.Empty),
            MemoriesJsonContext.Options);
        (MemoriesClient client, _) = CreateClient(HttpStatusCode.BadRequest, error);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.SearchAsync(
                new SearchRequest(TenantId: "t1", Axis: "syntactic", Query: "x"),
                CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_INPUT");
    }

    [Fact]
    public async Task HybridSearchAsync_MalformedJson_ThrowsWithInvalidResponseCode()
    {
        (MemoriesClient client, _) = CreateClient(HttpStatusCode.OK, "not json");

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.HybridSearchAsync(
                new HybridSearchRequest(TenantId: "t1", Query: "x"),
                CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task GetMemoryUnitAsync_TargetsExpectedPath()
    {
        MemoryUnit body = BuildMemoryUnit();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.GetMemoryUnitAsync("acme", "case-1", "mu-abc", CancellationToken.None);

        Uri? uri = handler.Requests[0].RequestUri;
        uri.ShouldNotBeNull();
        uri.AbsolutePath.ShouldBe("/api/tenants/acme/cases/case-1/memory-units/mu-abc");
    }

    [Fact]
    public async Task GetMemoryUnitAsync_EscapesSlashInCaseId()
    {
        MemoryUnit body = BuildMemoryUnit();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.GetMemoryUnitAsync("acme", "case/1", "mu-1", CancellationToken.None);

        Uri? uri = handler.Requests[0].RequestUri;
        uri.ShouldNotBeNull();
        uri.AbsolutePath.ShouldContain("case%2F1");
    }

    private static HybridSearchResult BuildHybrid()
        => new()
        {
            Results = [],
            TotalCount = 0,
            Degraded = false,
            UnavailableAxes = [],
            Query = "needle",
        };

    private static MemoryUnit BuildMemoryUnit()
        => new()
        {
            Id = "mu-1",
            TenantId = "acme corp",
            CaseId = "case/1",
            Content = "content",
            ContentHash = "h",
            SourceUri = "mem://case/mu-1",
            SourceType = SourceType.File,
            IngestedBy = "user",
            IngestedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            Status = MemoryUnitStatus.Indexed,
        };

    private static (MemoriesClient Client, TestDelegatingHandler Handler) CreateClient(HttpStatusCode status, string body)
    {
        var handler = new TestDelegatingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            }));
        var httpClient = new HttpClient(handler) { BaseAddress = Endpoint };
        IOptions<MemoriesClientOptions> options = Options.Create(new MemoriesClientOptions { Endpoint = Endpoint });
        return (new MemoriesClient(httpClient, options, NullLogger<MemoriesClient>.Instance), handler);
    }
}
