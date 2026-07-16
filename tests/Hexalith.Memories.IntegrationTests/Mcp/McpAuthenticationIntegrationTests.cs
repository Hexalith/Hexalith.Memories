// <copyright file="McpAuthenticationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Headers;

using Hexalith.Memories.IntegrationTests.Fixtures;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Shouldly;

using Xunit;

/// <summary>
/// Story 10.2 Task 14.1 — Tier-3 Aspire integration coverage for the MCP ingress JWT bearer auth and
/// the tenant-claim authorization filter. Closes AC #4 (RFC 6750-compliant 401 challenge) and AC #5
/// (cross-tenant TENANT_FORBIDDEN gate before the DAPR hop) end-to-end against the live MCP service.
/// Docker-gated under <c>[Trait("Category", "Integration")]</c>; runs on the merge-queue lane.
/// </summary>
[Collection("AspireIngestionPipeline")]
[Trait("Category", "Integration")]
public sealed class McpAuthenticationIntegrationTests
{
    private readonly AspireIngestionPipelineFixture _fixture;
    private readonly ITestOutputHelper _output;

    public McpAuthenticationIntegrationTests(AspireIngestionPipelineFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task PostMcp_NoAuthorizationHeader_ReturnsBearerChallenge()
    {
        using var content = new StringContent(
            "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/list\"}",
            System.Text.Encoding.UTF8,
            "application/json");
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(_fixture.McpEndpoint, "/mcp"))
        {
            Content = content,
        };

        using HttpResponseMessage response = await _fixture.McpClient.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldNotBeEmpty();
        AuthenticationHeaderValue challenge = response.Headers.WwwAuthenticate.First();
        challenge.Scheme.ShouldBe("Bearer");
        challenge.Parameter.ShouldNotBeNullOrWhiteSpace();
        challenge.Parameter.ShouldContain("realm=\"hexalith-memories-mcp\"", Case.Insensitive);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task GetHealth_AllowsAnonymous()
    {
        using HttpResponseMessage response = await _fixture.McpClient.GetAsync("/health");
        string body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task CallTool_ValidBearer_MatchingTenantClaim_Succeeds()
    {
        string tenantId = await _fixture.ProvisionActiveTenantAsync();

        await using McpClient client = await CreateMcpClientAsync(
            AspireIngestionPipelineFixture.MintDevBearer(tenantId));

        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tenantId"] = tenantId,
            ["query"] = "matching-tenant",
            ["axes"] = "hybrid",
        };

        CallToolResult result = await client.CallToolAsync("search_memory", arguments);

        _output.WriteLine($"search_memory IsError={result.IsError}; Content={FormatContent(result.Content)}");
        result.IsError.ShouldNotBe(true);
    }

    [Fact]
    public async Task CallTool_ValidBearer_CrossTenantClaim_ReturnsTenantForbidden()
    {
        string targetTenantId = await _fixture.ProvisionActiveTenantAsync();
        string bearerTenantId = $"tenant-it-{Guid.NewGuid():N}";

        await using McpClient client = await CreateMcpClientAsync(
            AspireIngestionPipelineFixture.MintDevBearer(bearerTenantId));

        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tenantId"] = targetTenantId,
            ["query"] = "cross-tenant-attempt",
            ["axes"] = "hybrid",
        };

        CallToolResult result = await client.CallToolAsync("search_memory", arguments);

        result.IsError.ShouldBe(true);
        result.Content.ShouldNotBeEmpty();
        TextContentBlock textBlock = result.Content
            .OfType<TextContentBlock>()
            .ShouldHaveSingleItem();
        textBlock.Text.ShouldContain("TENANT_FORBIDDEN");
        textBlock.Text.ShouldNotContain(bearerTenantId);
        _output.WriteLine($"Cross-tenant authorization payload: {textBlock.Text}");
    }

    private async Task<McpClient> CreateMcpClientAsync(string bearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bearerToken);

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(_fixture.McpEndpoint, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Authorization"] = $"Bearer {bearerToken}",
            },
        });

        McpClient client = await McpClient.CreateAsync(transport);
        _output.WriteLine($"Connected to MCP endpoint at {_fixture.McpEndpoint}");
        return client;
    }

    private static string FormatContent(IList<ContentBlock> content)
        => string.Join(
            " | ",
            content.OfType<TextContentBlock>().Select(c => c.Text));
}
