// <copyright file="IngestContentToolTests.cs" company="ITANEO">
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

public sealed class IngestContentToolTests
{
    [Fact]
    public async Task HappyPath_ReturnsWorkflowInstanceId()
    {
        var stub = new StubMemoriesClient
        {
            OnIngest = (_, _) => Task.FromResult("workflow-instance-42"),
        };
        IngestContentTool tool = CreateTool(stub);

        CallToolResult result = await tool.IngestAsync("acme", "case-1", "hello world");

        stub.IngestCalls.ShouldHaveSingleItem();
        result.IsError.ShouldNotBe(true);
        result.StructuredContent!.Value.GetProperty("workflowInstanceId").GetString().ShouldBe("workflow-instance-42");
    }

    [Fact]
    public async Task EmptyContent_ReturnsInvalidInputError()
    {
        var stub = new StubMemoriesClient();
        IngestContentTool tool = CreateTool(stub);

        CallToolResult result = await tool.IngestAsync("acme", "case-1", " ");

        stub.IngestCalls.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "INVALID_INPUT");
    }

    [Fact]
    public async Task TenantSuspended_MapsRemoteException()
    {
        var stub = new StubMemoriesClient
        {
            OnIngest = (_, _) => throw new MemoriesRemoteException(
                HttpStatusCode.Conflict,
                new ErrorResponse("TENANT_SUSPENDED", "tenant is suspended", "contact admin")),
        };
        IngestContentTool tool = CreateTool(stub);

        CallToolResult result = await tool.IngestAsync("acme", "case-1", "content");

        AssertIsErrorWithCode(result, "TENANT_SUSPENDED");
    }

    [Fact]
    public async Task RateLimited_MapsRemoteException()
    {
        var stub = new StubMemoriesClient
        {
            OnIngest = (_, _) => throw new MemoriesRemoteException(
                HttpStatusCode.TooManyRequests,
                new ErrorResponse("RATE_LIMITED", "throttled", "retry after 30s")),
        };
        IngestContentTool tool = CreateTool(stub);

        CallToolResult result = await tool.IngestAsync("acme", "case-1", "content");

        AssertIsErrorWithCode(result, "RATE_LIMITED");
    }

    [Fact]
    public async Task UnsupportedSourceType_RejectsClientSide()
    {
        var stub = new StubMemoriesClient();
        IngestContentTool tool = CreateTool(stub);

        CallToolResult result = await tool.IngestAsync("acme", "case-1", "content", sourceType: McpSourceType.Url);

        stub.IngestCalls.ShouldBeEmpty();
        AssertIsErrorWithCode(result, "UNSUPPORTED_SOURCE_TYPE");
    }

    private static void AssertIsErrorWithCode(CallToolResult result, string expectedCode)
    {
        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe(expectedCode);
    }

    private static IngestContentTool CreateTool(StubMemoriesClient stub)
    {
        var (authorization, accessor) = McpToolTestFactory.CreateAuth();
        return new IngestContentTool(stub, new McpErrorMapper(), authorization, accessor);
    }
}
