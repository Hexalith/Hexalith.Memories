// <copyright file="GetCaseInfoToolTests.cs" company="ITANEO">
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

public sealed class GetCaseInfoToolTests
{
    [Fact]
    public async Task HappyPath_ReturnsCaseSummary()
    {
        var stub = new StubMemoriesClient
        {
            OnGetCase = (tenantId, caseId, _) => Task.FromResult(new Hexalith.Memories.Contracts.V1.Case(
                Id: caseId,
                TenantId: tenantId,
                Name: "Test case",
                Description: "desc",
                Status: CaseStatus.Active,
                CreatedAt: DateTimeOffset.UtcNow,
                LastUpdated: DateTimeOffset.UtcNow,
                MemoryUnitCount: 7)),
        };
        GetCaseInfoTool tool = CreateTool(stub);

        CallToolResult result = await tool.GetCaseAsync("acme", "case-1", TestContext.Current.CancellationToken);

        stub.GetCaseCalls.ShouldHaveSingleItem();
        result.IsError.ShouldNotBe(true);
        result.StructuredContent!.Value.GetProperty("name").GetString().ShouldBe("Test case");
        result.StructuredContent!.Value.GetProperty("memoryUnitCount").GetInt32().ShouldBe(7);
    }

    [Fact]
    public async Task CaseNotFound_MapsRemoteException()
    {
        var stub = new StubMemoriesClient
        {
            OnGetCase = (_, _, _) => throw new MemoriesRemoteException(
                HttpStatusCode.NotFound,
                new ErrorResponse("CASE_NOT_FOUND", "no such case", "list cases")),
        };
        GetCaseInfoTool tool = CreateTool(stub);

        CallToolResult result = await tool.GetCaseAsync("acme", "missing", TestContext.Current.CancellationToken);

        result.IsError.ShouldBe(true);
        result.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe("CASE_NOT_FOUND");
    }

    private static GetCaseInfoTool CreateTool(StubMemoriesClient stub)
    {
        var (authorization, accessor) = McpToolTestFactory.CreateAuth();
        return new GetCaseInfoTool(stub, new McpErrorMapper(), authorization, accessor);
    }
}
