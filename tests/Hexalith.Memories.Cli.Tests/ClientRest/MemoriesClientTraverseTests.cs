// <copyright file="MemoriesClientTraverseTests.cs" company="ITANEO">
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

public sealed class MemoriesClientTraverseTests
{
    private static readonly Uri Endpoint = new("http://127.0.0.1:5000/");

    [Fact]
    public async Task TraverseAsync_TargetsExpectedPathAndQuery()
    {
        TraversalResult body = BuildTraversal();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.TraverseAsync(
            tenantId: "acme",
            startNodeId: "mu-1",
            depth: 4,
            caseId: "case-1",
            edgeTypes: [EdgeType.CausedBy, EdgeType.CorrelatedWith],
            tokenBudget: 1_200,
            ct: CancellationToken.None);

        Uri? uri = handler.Requests[0].RequestUri;
        uri.ShouldNotBeNull();
        uri.AbsolutePath.ShouldBe("/api/tenants/acme/traverse");
        uri.Query.ShouldContain("startNodeId=mu-1");
        uri.Query.ShouldContain("depth=4");
        uri.Query.ShouldContain("caseId=case-1");
        uri.Query.ShouldContain("edgeTypes=causedBy%2CcorrelatedWith");
        uri.Query.ShouldContain("tokenBudget=1200");
    }

    [Fact]
    public async Task TraverseAsync_OmitsCaseIdAndEdgeTypesWhenAbsent()
    {
        TraversalResult body = BuildTraversal();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.TraverseAsync(
            tenantId: "acme",
            startNodeId: "mu-1",
            ct: CancellationToken.None);

        Uri? uri = handler.Requests[0].RequestUri;
        uri.ShouldNotBeNull();
        uri.Query.ShouldContain("startNodeId=mu-1");
        uri.Query.ShouldContain("depth=2");
        uri.Query.ShouldNotContain("caseId=");
        uri.Query.ShouldNotContain("edgeTypes=");
        uri.Query.ShouldNotContain("tokenBudget=");
    }

    [Fact]
    public async Task TraverseAsync_NonSuccess_ThrowsMemoriesRemoteException()
    {
        string error = JsonSerializer.Serialize(
            new ErrorResponse("MEMORY_UNIT_NOT_FOUND", "no such unit", "Check tenant + node id."),
            MemoriesJsonContext.Options);
        (MemoriesClient client, _) = CreateClient(HttpStatusCode.NotFound, error);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.TraverseAsync("acme", "mu-1", ct: CancellationToken.None));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.Error.Code.ShouldBe("MEMORY_UNIT_NOT_FOUND");
    }

    [Fact]
    public async Task TraverseAsync_MalformedJson_ReturnsInvalidResponse()
    {
        (MemoriesClient client, _) = CreateClient(HttpStatusCode.OK, "not json");

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.TraverseAsync("acme", "mu-1", ct: CancellationToken.None));

        exception.Error.Code.ShouldBe("INVALID_RESPONSE");
    }

    [Fact]
    public async Task GetCaseAsync_TargetsExpectedPath()
    {
        Hexalith.Memories.Contracts.V1.Case body = BuildCase();
        string json = JsonSerializer.Serialize(body, MemoriesJsonContext.Options);
        (MemoriesClient client, TestDelegatingHandler handler) = CreateClient(HttpStatusCode.OK, json);

        _ = await client.GetCaseAsync("acme", "case-1", CancellationToken.None);

        Uri? uri = handler.Requests[0].RequestUri;
        uri.ShouldNotBeNull();
        uri.AbsolutePath.ShouldBe("/api/tenants/acme/cases/case-1");
    }

    [Fact]
    public async Task GetCaseAsync_NonSuccess_ThrowsMemoriesRemoteException()
    {
        string error = JsonSerializer.Serialize(
            new ErrorResponse("CASE_NOT_FOUND", "missing", string.Empty),
            MemoriesJsonContext.Options);
        (MemoriesClient client, _) = CreateClient(HttpStatusCode.NotFound, error);

        MemoriesRemoteException exception = await Should.ThrowAsync<MemoriesRemoteException>(
            () => client.GetCaseAsync("acme", "missing", CancellationToken.None));

        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        exception.Error.Code.ShouldBe("CASE_NOT_FOUND");
    }

    private static TraversalResult BuildTraversal() => new(
        StartNodeId: "mu-1",
        Depth: 2,
        Nodes: [],
        TotalNodeCount: 0);

    private static Hexalith.Memories.Contracts.V1.Case BuildCase() => new(
        Id: "case-1",
        TenantId: "acme",
        Name: "Test case",
        Description: null,
        Status: CaseStatus.Active,
        CreatedAt: DateTimeOffset.UtcNow,
        LastUpdated: DateTimeOffset.UtcNow,
        MemoryUnitCount: 0);

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
