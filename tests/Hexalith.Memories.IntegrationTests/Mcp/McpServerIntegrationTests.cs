// <copyright file="McpServerIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Mcp;

using System.Linq;

using Hexalith.Memories.IntegrationTests.Fixtures;

using ModelContextProtocol.Client;

using Shouldly;

using Xunit;

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
    private static readonly string[] DiagnosticResourceNames =
    [
        "memories",
        "memories-mcp",
        "memories-mcp-dapr-cli",
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
        int logStartIndex = _fixture.LogEntryCount;

        // Story 10.2 — `/mcp` requires bearer auth even for ListTools (the tenant-claim filter only
        // activates on tool invocations that bind tenantId; ListTools is a metadata operation that
        // any authenticated principal may call).
        IList<McpClientTool> tools;
        try
        {
            await using McpClient client = await CreateMcpClientAsync("tenant-listtools-probe");
            tools = await client.ListToolsAsync();
        }
        catch
        {
            await WriteFailureDiagnosticsAsync(logStartIndex);
            throw;
        }

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
        int logStartIndex = _fixture.LogEntryCount;

        // Ensure a tenant exists so the search call hits an active routing path. Hybrid search
        // returns an empty result when the corpus is empty; the assertion is on the IsError flag,
        // not the result count, since 10.1's contract is "the call traverses the sidecar without
        // error", not "results are non-empty".
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tenantId"] = tenantId,
            ["query"] = "needle",
            ["axes"] = "hybrid",
        };

        ModelContextProtocol.Protocol.CallToolResult result;
        try
        {
            await using McpClient client = await CreateMcpClientAsync(tenantId);
            result = await client.CallToolAsync("search_memory", arguments);
        }
        catch
        {
            await WriteFailureDiagnosticsAsync(logStartIndex);
            throw;
        }

        _output.WriteLine($"search_memory IsError={result.IsError}; Content={FormatContent(result.Content)}");
        if (result.IsError == true)
        {
            await WriteFailureDiagnosticsAsync(logStartIndex);
        }

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

    private async Task WriteFailureDiagnosticsAsync(int logStartIndex)
    {
        // Aspire resource output is forwarded asynchronously. Give the failing request's final
        // Server/MCP entries a bounded chance to reach the fixture before taking the snapshot.
        await Task.Delay(TimeSpan.FromSeconds(2));
        IEnumerable<AspireIngestionPipelineFixture.CapturedLogEntry> recent = _fixture
            .GetLogEntriesSince(logStartIndex)
            .Where(entry =>
                (entry.Level >= Microsoft.Extensions.Logging.LogLevel.Warning ||
                    IsDiagnosticResourceCategory(entry.Category)) &&
                !entry.Message.Contains("__hexalith_activity__", StringComparison.Ordinal))
            .TakeLast(200);

        foreach (AspireIngestionPipelineFixture.CapturedLogEntry entry in recent)
        {
            _output.WriteLine(AspireIngestionPipelineFixture.RedactSensitiveDiagnostics(
                $"{entry.Level}: {entry.Category}: {entry.Message}"));
        }
    }

    /// <summary>Determines whether a log category represents an MCP failure-diagnostic resource.</summary>
    /// <param name="category">Aspire log category in bare, prefixed, or suffixed form.</param>
    /// <returns><see langword="true"/> when a dot-delimited category segment is a supported resource.</returns>
    internal static bool IsDiagnosticResourceCategory(string category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return category
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(segment => DiagnosticResourceNames.Contains(segment, StringComparer.Ordinal));
    }
}
