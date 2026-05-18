// <copyright file="SearchMemoryToolTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Net;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp;
using Hexalith.Memories.Mcp.Tools;

using ModelContextProtocol.Protocol;

using Shouldly;

public sealed class SearchMemoryToolTests
{
    [Fact]
    public async Task SyntacticAxis_RoutesToSingleAxisSearchAsync()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync(tenantId: "acme", query: "needle", axes: SearchAxis.Syntactic, cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests.ShouldHaveSingleItem();
        stub.HybridSearchRequests.ShouldBeEmpty();
        stub.SearchRequests[0].Axis.ShouldBe("syntactic");
        stub.SearchRequests[0].Query.ShouldBe("needle");
    }

    [Fact]
    public async Task HybridAxis_RoutesToHybridSearchAsync()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync(tenantId: "acme", query: "needle", axes: SearchAxis.Hybrid, cancellationToken: TestContext.Current.CancellationToken);

        stub.HybridSearchRequests.ShouldHaveSingleItem();
        stub.SearchRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task MissingTenantId_ReturnsInvalidInputErrorWithoutCallingClient()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        CallToolResult result = await tool.SearchAsync(tenantId: " ", query: "needle", cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests.ShouldBeEmpty();
        stub.HybridSearchRequests.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "INVALID_INPUT");
    }

    [Fact]
    public async Task ServerTenantNotFound_MapsToErrorPrefix()
    {
        var stub = new StubMemoriesClient
        {
            OnHybridSearch = (_, _) => throw new MemoriesRemoteException(
                HttpStatusCode.NotFound,
                new ErrorResponse("TENANT_NOT_FOUND", "no such tenant", "list tenants")),
        };
        SearchMemoryTool tool = CreateTool(stub);

        CallToolResult result = await tool.SearchAsync("acme", "needle", cancellationToken: TestContext.Current.CancellationToken);

        AssertIsErrorWithCode(result, "TENANT_NOT_FOUND");
        ExtractText(result).ShouldStartWith("[TENANT_NOT_FOUND] (service=memories-server):");
    }

    [Fact]
    public async Task Explain_FlagPropagated()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync("acme", "needle", explain: true, cancellationToken: TestContext.Current.CancellationToken);

        stub.HybridSearchRequests[0].Explain.ShouldBe(true);
    }

    [Theory]
    [InlineData("Syntactic", "syntactic")]
    [InlineData("Semantic", "semantic")]
    public async Task SingleAxis_PassesAxisStringToSearchRequest(string axisName, string expected)
    {
        var axis = Enum.Parse<SearchAxis>(axisName);
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync("acme", "needle", axes: axis, cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests[0].Axis.ShouldBe(expected);
    }

    [Theory]
    [InlineData(int.MaxValue, SearchMemoryTool.MaxResultsUpperBound)]
    [InlineData(-5, SearchMemoryTool.MaxResultsLowerBound)]
    [InlineData(0, SearchMemoryTool.MaxResultsLowerBound)]
    [InlineData(50, 50)]
    public async Task MaxResults_ClampedToRange(int input, int expected)
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync("acme", "needle", axes: SearchAxis.Syntactic, maxResults: input, cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests[0].MaxResults.ShouldBe(expected);
    }

    [Fact]
    public async Task TokenBudget_ForwardsToServerWithoutNarrowingMaxResults()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync(
            tenantId: "acme",
            query: "needle",
            axes: SearchAxis.Syntactic,
            maxResults: 50,
            tokenBudget: 2_000,
            cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests[0].MaxResults.ShouldBe(50);
        stub.SearchRequests[0].TokenBudget.ShouldBe(2_000);
    }

    [Fact]
    public async Task TokenBudget_DoesNotIncreaseAboveCallerMaxResults()
    {
        var stub = new StubMemoriesClient();
        SearchMemoryTool tool = CreateTool(stub);

        _ = await tool.SearchAsync(
            tenantId: "acme",
            query: "needle",
            axes: SearchAxis.Syntactic,
            maxResults: 3,
            tokenBudget: 50_000,
            cancellationToken: TestContext.Current.CancellationToken);

        stub.SearchRequests[0].MaxResults.ShouldBe(3);
        stub.SearchRequests[0].TokenBudget.ShouldBe(50_000);
    }

    private static void AssertIsErrorWithCode(CallToolResult result, string expectedCode)
    {
        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(expectedCode);
    }

    private static string ExtractText(CallToolResult result)
    {
        var block = result.Content[0] as TextContentBlock;
        block.ShouldNotBeNull();
        return block!.Text;
    }

    private static SearchMemoryTool CreateTool(StubMemoriesClient stub)
    {
        var (authorization, accessor) = McpToolTestFactory.CreateAuth();
        return new SearchMemoryTool(stub, new McpErrorMapper(), authorization, accessor);
    }
}
