// <copyright file="TraverseRelationsToolTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Mcp;
using Hexalith.Memories.Mcp.Tools;

using ModelContextProtocol.Protocol;

using Shouldly;

public sealed class TraverseRelationsToolTests
{
    [Fact]
    public async Task HappyPath_RoutesToTraverseAsync()
    {
        var stub = new StubMemoriesClient();
        TraverseRelationsTool tool = CreateTool(stub);

        _ = await tool.TraverseAsync(tenantId: "acme", from: "mu-1", depth: 2, edgeType: "causedBy,correlatedWith", tokenBudget: 900, cancellationToken: TestContext.Current.CancellationToken);

        stub.TraversalRequests.ShouldHaveSingleItem();
        TraversalRequest captured = stub.TraversalRequests[0];
        captured.TenantId.ShouldBe("acme");
        captured.StartNodeId.ShouldBe("mu-1");
        captured.Depth.ShouldBe(2);
        captured.EdgeTypes.ShouldNotBeNull();
        captured.EdgeTypes!.Count.ShouldBe(2);
        captured.EdgeTypes!.ShouldContain(EdgeType.CausedBy);
        captured.EdgeTypes!.ShouldContain(EdgeType.CorrelatedWith);
        captured.TokenBudget.ShouldBe(900);
    }

    [Fact]
    public async Task InvalidEdgeType_RejectsClientSideWithoutCallingServer()
    {
        var stub = new StubMemoriesClient();
        TraverseRelationsTool tool = CreateTool(stub);

        CallToolResult result = await tool.TraverseAsync("acme", "mu-1", edgeType: "notARealType", cancellationToken: TestContext.Current.CancellationToken);

        stub.TraversalRequests.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "INVALID_EDGE_TYPE");
    }

    [Theory]
    [InlineData(100, TraverseRelationsTool.DepthUpperBound)]
    [InlineData(-1, TraverseRelationsTool.DepthLowerBound)]
    [InlineData(5, 5)]
    public async Task Depth_ClampedToServerRange(int input, int expected)
    {
        var stub = new StubMemoriesClient();
        TraverseRelationsTool tool = CreateTool(stub);

        _ = await tool.TraverseAsync("acme", "mu-1", depth: input, cancellationToken: TestContext.Current.CancellationToken);

        stub.TraversalRequests[0].Depth.ShouldBe(expected);
    }

    [Fact]
    public async Task MissingFrom_ReturnsInvalidInputError()
    {
        var stub = new StubMemoriesClient();
        TraverseRelationsTool tool = CreateTool(stub);

        CallToolResult result = await tool.TraverseAsync("acme", " ", cancellationToken: TestContext.Current.CancellationToken);

        stub.TraversalRequests.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "INVALID_INPUT");
    }

    [Fact]
    public async Task MismatchedTenant_ReturnsAuthorizationErrorWithoutCallingClient()
    {
        var stub = new StubMemoriesClient();
        TraverseRelationsTool tool = CreateTool(stub);

        CallToolResult result = await tool.TraverseAsync("other-tenant", "mu-1", cancellationToken: TestContext.Current.CancellationToken);

        stub.TraversalRequests.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "TENANT_FORBIDDEN");
    }

    private static void AssertIsErrorWithCode(CallToolResult result, string expectedCode)
    {
        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(expectedCode);
    }

    private static TraverseRelationsTool CreateTool(StubMemoriesClient stub)
        => new(stub, McpToolTestFactory.CreateExecutor());
}
