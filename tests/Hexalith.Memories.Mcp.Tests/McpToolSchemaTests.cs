// <copyright file="McpToolSchemaTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.Memories.Mcp.Authentication;
using Hexalith.Memories.Mcp.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

using Shouldly;

/// <summary>
/// Story 10.1 Tier-1 contract tests — the four registered MCP tools must expose stable schemas
/// (FR54 / FR58 / NFR20). These tests resolve the registered <see cref="McpServerTool"/> instances
/// from DI without spinning up the Streamable HTTP transport.
/// </summary>
public sealed class McpToolSchemaTests
{
    private const string SearchMemoryName = "search_memory";
    private const string IngestContentName = "ingest_content";
    private const string TraverseRelationsName = "traverse_relations";
    private const string GetCaseInfoName = "get_case_info";

    private static readonly string[] ExpectedToolNames =
    [
        SearchMemoryName,
        IngestContentName,
        TraverseRelationsName,
        GetCaseInfoName,
    ];

    [Fact]
    public void RegisteredTools_ContainExactlyTheFourEpicTools()
    {
        IReadOnlyList<McpServerTool> tools = ResolveRegisteredTools();
        string[] names = [.. tools.Select(t => t.ProtocolTool.Name)];

        names.Length.ShouldBe(4);
        foreach (string expected in ExpectedToolNames)
        {
            names.ShouldContain(expected);
        }
    }

    [Fact]
    public void RegisteredTools_HaveDistinctNames()
    {
        IReadOnlyList<McpServerTool> tools = ResolveRegisteredTools();
        string[] names = [.. tools.Select(t => t.ProtocolTool.Name)];

        names.Length.ShouldBe(names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EveryToolHasDescription()
    {
        IReadOnlyList<McpServerTool> tools = ResolveRegisteredTools();

        foreach (McpServerTool tool in tools)
        {
            tool.ProtocolTool.Description.ShouldNotBeNullOrWhiteSpace($"tool '{tool.ProtocolTool.Name}'");
        }
    }

    [Fact]
    public void EveryToolParameterHasNonTrivialDescription()
    {
        IReadOnlyList<McpServerTool> tools = ResolveRegisteredTools();

        foreach (McpServerTool tool in tools)
        {
            JsonElement properties = GetParameterProperties(tool);
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                if (!property.Value.TryGetProperty("description", out JsonElement description))
                {
                    throw new Xunit.Sdk.XunitException($"tool '{tool.ProtocolTool.Name}' parameter '{property.Name}' has no description");
                }

                string text = description.GetString() ?? string.Empty;
                text.ShouldNotBeNullOrWhiteSpace($"tool '{tool.ProtocolTool.Name}' parameter '{property.Name}'");
                text.Length.ShouldBeGreaterThan(property.Name.Length);
                text.Contains(' ').ShouldBeTrue($"tool '{tool.ProtocolTool.Name}' parameter '{property.Name}' description must be prose");
                string.Equals(text, property.Name, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
                    $"tool '{tool.ProtocolTool.Name}' parameter '{property.Name}' description echoes parameter name");
            }
        }
    }

    [Fact]
    public void SearchMemoryTool_AxesParameter_EmitsEnumWithThreeValues()
    {
        McpServerTool tool = ResolveTool(SearchMemoryName);
        JsonElement axesParam = GetParameterProperties(tool).GetProperty("axes");

        // The MCP SDK 1.2.0 schema generator renders enum members using its own default
        // JsonStringEnumConverter (PascalCase) rather than the [JsonConverter] attached to the
        // type, so the schema literals are PascalCase even though wire-level deserialization is
        // case-insensitive. Compare case-insensitively so the contract test expresses the four
        // canonical axes regardless of SDK rendering choice. The Description on the parameter
        // still teaches LLMs the lowercase form; both work at deserialize time. Graph is not a
        // search_memory axis in 10.1 because the server requires a startNodeId; use traverse_relations.
        string[] axisLiterals = ["syntactic", "semantic", "hybrid"];
        string serialized = axesParam.GetRawText();
        foreach (string literal in axisLiterals)
        {
            serialized.Contains(literal, StringComparison.OrdinalIgnoreCase)
                .ShouldBeTrue($"axes schema missing enum literal '{literal}': {serialized}");
        }

        string[] enumLiterals = [.. axesParam.GetProperty("enum").EnumerateArray().Select(v => v.GetString() ?? string.Empty)];
        enumLiterals.ShouldNotContain(
            "Graph",
            "search_memory must not advertise graph as an enum value in 10.1; use traverse_relations instead");
    }

    [Fact]
    public void SearchMemoryTool_TokenBudget_PresentAsOptionalInteger()
    {
        McpServerTool tool = ResolveTool(SearchMemoryName);
        JsonElement properties = GetParameterProperties(tool);

        properties.TryGetProperty("tokenBudget", out JsonElement tokenBudget).ShouldBeTrue();

        // "integer" type appears either as a literal "integer" or in a one-of nullable union; assert
        // on substring rather than exact shape so the schema can evolve to a nullable union
        // without breaking the contract test.
        string serialized = tokenBudget.GetRawText();
        serialized.ShouldContain("\"integer\"");
    }

    [Fact]
    public void SearchMemoryTool_QueryParameter_IsRequired()
    {
        McpServerTool tool = ResolveTool(SearchMemoryName);
        JsonElement schema = tool.ProtocolTool.InputSchema;

        schema.TryGetProperty("required", out JsonElement required).ShouldBeTrue();
        string serializedRequired = required.GetRawText();
        serializedRequired.ShouldContain("\"query\"");
        serializedRequired.ShouldContain("\"tenantId\"");
    }

    [Fact]
    public void TraverseRelationsTool_GraphScope_IsCaseIdString()
    {
        McpServerTool tool = ResolveTool(TraverseRelationsName);
        JsonElement properties = GetParameterProperties(tool);

        // 10.1 simplification: graph_scope is flattened to a single optional caseId (string).
        properties.TryGetProperty("caseId", out JsonElement caseId).ShouldBeTrue();
        caseId.GetRawText().ShouldContain("\"string\"");

        // The complex `graph_scope` object MUST NOT appear in 10.1.
        properties.TryGetProperty("graphScope", out _).ShouldBeFalse();
    }

    private static IReadOnlyList<McpServerTool> ResolveRegisteredTools()
    {
        IServiceProvider services = BuildServiceProvider();
        return [.. services.GetServices<McpServerTool>()];
    }

    private static McpServerTool ResolveTool(string name)
    {
        IReadOnlyList<McpServerTool> tools = ResolveRegisteredTools();
        return tools.Single(t => string.Equals(t.ProtocolTool.Name, name, StringComparison.Ordinal));
    }

    private static JsonElement GetParameterProperties(McpServerTool tool)
    {
        JsonElement schema = tool.ProtocolTool.InputSchema;
        schema.TryGetProperty("properties", out JsonElement properties).ShouldBeTrue($"tool '{tool.ProtocolTool.Name}' missing properties");
        return properties;
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new StubMemoriesClient() as Hexalith.Memories.Client.Rest.MemoriesClient);
        services.AddSingleton<McpErrorMapper>();
        services.AddHttpContextAccessor();
        services.AddSingleton<IOptions<MemoriesMcpAuthenticationOptions>>(Options.Create(new MemoriesMcpAuthenticationOptions()));
        services.AddScoped<TenantClaimAuthorizationFilter>();
        services.AddScoped<IAuthorizedTenantAccessor, AuthorizedTenantAccessor>();
        services.AddSingleton<ILogger<TenantClaimAuthorizationFilter>>(NullLogger<TenantClaimAuthorizationFilter>.Instance);
        services.AddSingleton<IHttpContextAccessor>(_ =>
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(MemoriesMcpClaimsTransformation.TenantClaimType, "acme")],
                "Bearer"));
            return new HttpContextAccessor { HttpContext = context };
        });
        services.AddMcpServer()
            .WithTools<SearchMemoryTool>()
            .WithTools<IngestContentTool>()
            .WithTools<TraverseRelationsTool>()
            .WithTools<GetCaseInfoTool>();
        return services.BuildServiceProvider();
    }
}
