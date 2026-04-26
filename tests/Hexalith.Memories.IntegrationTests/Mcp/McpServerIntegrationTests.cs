// <copyright file="McpServerIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Mcp;

using System.Linq;

using Hexalith.Memories.IntegrationTests.Fixtures;

using ModelContextProtocol.Client;

using Shouldly;

using Xunit.Abstractions;

/// <summary>
/// Story 10.1 — Tier-3 Aspire end-to-end tests for the MCP server. Verifies the registered tools
/// surface to a real <see cref="McpClient"/> over the Streamable HTTP transport and that
/// <c>search_memory</c> executes across the DAPR service-invocation hop to the upstream Memories
/// Server. Runs on the Docker-provisioned merge-queue lane (Trait Category=Integration), excluded
/// from per-PR runs.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class McpServerIntegrationTests
{
    private static readonly string[] ExpectedToolNames =
    [
        "search_memory",
        "ingest_content",
        "traverse_relations",
        "get_case_info",
    ];

    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public McpServerIntegrationTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task ListTools_EndToEnd_ReturnsFourToolsWithTypedSchemas()
    {
        // Story 10.2 — `/mcp` requires bearer auth even for ListTools (the tenant-claim filter only
        // activates on tool invocations that bind tenantId; ListTools is a metadata operation that
        // any authenticated principal may call).
        await using McpClient client = await CreateMcpClientAsync("tenant-listtools-probe");

        IList<McpClientTool> tools = await client.ListToolsAsync();

        string[] names = [.. tools.Select(t => t.Name)];
        names.Length.ShouldBe(4);
        foreach (string expected in ExpectedToolNames)
        {
            names.ShouldContain(expected);
        }

        foreach (McpClientTool tool in tools)
        {
            tool.Description.ShouldNotBeNullOrWhiteSpace();
            tool.JsonSchema.ValueKind.ShouldNotBe(System.Text.Json.JsonValueKind.Undefined);
        }
    }

    [Fact]
    public async Task CallSearchMemory_EndToEnd_ExecutesAcrossDaprHop()
    {
        // Ensure a tenant exists so the search call hits an active routing path. Hybrid search
        // returns an empty result when the corpus is empty; the assertion is on the IsError flag,
        // not the result count, since 10.1's contract is "the call traverses the sidecar without
        // error", not "results are non-empty".
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        await using McpClient client = await CreateMcpClientAsync(tenantId);

        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tenantId"] = tenantId,
            ["query"] = "needle",
            ["axes"] = "hybrid",
        };

        ModelContextProtocol.Protocol.CallToolResult result = await client
            .CallToolAsync("search_memory", arguments)
            ;

        _output.WriteLine($"search_memory IsError={result.IsError}; Content={FormatContent(result.Content)}");
        result.IsError.ShouldNotBe(true);
        result.Content.ShouldNotBeEmpty();
    }

    private async Task<McpClient> CreateMcpClientAsync(string? tenantIdForBearer)
    {
        Dictionary<string, string> headers = new(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(tenantIdForBearer))
        {
            string token = AspireIngestionPipelineFixture.MintDevBearer(tenantIdForBearer);
            headers["Authorization"] = $"Bearer {token}";
        }

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_fixture.McpEndpoint, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = headers,
        });

        McpClient client = await McpClient.CreateAsync(transport);
        _output.WriteLine($"Connected to MCP endpoint at {_fixture.McpEndpoint}");
        return client;
    }

    private static string FormatContent(IList<ModelContextProtocol.Protocol.ContentBlock> content)
        => string.Join(
            " | ",
            content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));
}
