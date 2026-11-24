// <copyright file="McpAuthenticationIntegrationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.IntegrationTests.Mcp;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.IntegrationTests.Fixtures;
using Hexalith.Memories.Server.Infrastructure;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using Shouldly;

using StackExchange.Redis;

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
        challenge.Parameter.ShouldContain("realm=\"hexalith-memories-mcp\"", Shouldly.Case.Insensitive);
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

    [Fact]
    public async Task PostMcp_ExpiredBearer_ReturnsUnauthorized()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(_fixture.McpEndpoint, "/mcp"))
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":"expired","method":"tools/list"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            AspireIngestionPipelineFixture.MintDevBearer("tenant-expiry-probe", expiresAt: DateTime.UtcNow.AddMinutes(-3)));

        using HttpResponseMessage response = await _fixture.McpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldContain(challenge => challenge.Scheme == "Bearer");
    }

    [Fact]
    public async Task ListTools_BearerExpiredWithinClockSkew_Succeeds()
    {
        string bearer = AspireIngestionPipelineFixture.MintDevBearer(
            "tenant-skew-probe", expiresAt: DateTime.UtcNow.AddSeconds(-30));
        await using McpClient client = await CreateMcpClientAsync(bearer);

        IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        tools.ShouldContain(tool => tool.Name == "search_memory");
    }

    [Fact]
    public async Task CallTool_AlternatingTenantBearers_ReturnOnlyTheirOwnResults()
    {
        const string query = "mcptenantisolation";
        string firstTenant = await _fixture.ProvisionActiveTenantAsync(cancellationToken: TestContext.Current.CancellationToken);
        string secondTenant = await _fixture.ProvisionActiveTenantAsync(cancellationToken: TestContext.Current.CancellationToken);
        string[] firstIds = await SeedSearchResultsAsync(firstTenant, "first", query);
        string[] secondIds = await SeedSearchResultsAsync(secondTenant, "second", query);

        await using McpClient firstClient = await CreateMcpClientAsync(AspireIngestionPipelineFixture.MintDevBearer(firstTenant));
        await using McpClient secondClient = await CreateMcpClientAsync(AspireIngestionPipelineFixture.MintDevBearer(secondTenant));

        await AssertResultsAsync(firstClient, firstTenant, query, firstIds);
        await AssertResultsAsync(secondClient, secondTenant, query, secondIds);
        await AssertResultsAsync(firstClient, firstTenant, query, firstIds);
        await AssertTenantDeniedAsync(firstClient, secondTenant, query);
        await AssertTenantDeniedAsync(secondClient, firstTenant, query);
    }

    private async Task<string[]> SeedSearchResultsAsync(string tenantId, string prefix, string query)
    {
        IDatabase database = _fixture.RedisConnection.GetDatabase();
        string[] ids = [$"{prefix}-1", $"{prefix}-2", $"{prefix}-3"];
        string now = DateTimeOffset.UtcNow.ToString("O");
        foreach (string id in ids)
        {
            await database.HashSetAsync(IndexSchemaDefinitions.BuildSyntacticKey(tenantId, id),
                [
                    new HashEntry("id", id),
                    new HashEntry("tenantId", tenantId),
                    new HashEntry("caseId", "case-1"),
                    new HashEntry("content", query),
                    new HashEntry("sourceUri", $"file:///{id}.txt"),
                    new HashEntry("sourceType", "File"),
                    new HashEntry("ingestedAt", now),
                    new HashEntry("lastUpdated", now),
                    new HashEntry("status", "Indexed"),
                    new HashEntry("metadataJson", "{}"),
                ]);
        }

        return ids;
    }

    private static async Task AssertResultsAsync(McpClient client, string tenantId, string query, string[] expectedIds)
    {
        CallToolResult result = await SearchMemoryAsync(client, tenantId, query);

        result.IsError.ShouldBe(false, FormatContent(result.Content));
        result.StructuredContent.ShouldNotBeNull();
        SearchResult search = result.StructuredContent.GetValueOrDefault().Deserialize<SearchResult>(MemoriesJsonContext.Options)
            .ShouldNotBeNull();
        search.Results.Select(hit => hit.MemoryUnitId).Order(StringComparer.Ordinal).ShouldBe(expectedIds);
    }

    private static async Task AssertTenantDeniedAsync(McpClient client, string tenantId, string query)
    {
        CallToolResult result = await SearchMemoryAsync(client, tenantId, query);

        result.IsError.ShouldBe(true);
        FormatContent(result.Content).ShouldContain("TENANT_FORBIDDEN");
    }

    private static async Task<CallToolResult> SearchMemoryAsync(McpClient client, string tenantId, string query)
        => await client.CallToolAsync("search_memory", new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tenantId"] = tenantId,
            ["query"] = query,
            ["axes"] = "syntactic",
        }, cancellationToken: TestContext.Current.CancellationToken);

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
        }, _fixture.McpClient, ownsHttpClient: false);

        McpClient client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
        _output.WriteLine($"Connected to MCP endpoint at {_fixture.McpEndpoint}");
        return client;
    }

    private static string FormatContent(IList<ContentBlock> content)
        => string.Join(
            " | ",
            content.OfType<TextContentBlock>().Select(c => c.Text));
}
