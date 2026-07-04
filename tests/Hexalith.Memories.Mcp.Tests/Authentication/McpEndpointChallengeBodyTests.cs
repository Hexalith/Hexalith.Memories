// <copyright file="McpEndpointChallengeBodyTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests.Authentication;

using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Shouldly;

public sealed class McpEndpointChallengeBodyTests
{
    [Fact]
    public async Task McpEndpoint_WithoutBearer_ReturnsSanitizedProblemDetails()
    {
        await using var factory = new McpWebAppFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var content = CreateMcpInitializeRequest();

        using HttpResponseMessage response = await client.PostAsync("/mcp", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        JsonElement root = body.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Unauthorized);
        root.GetProperty("title").GetString().ShouldBe("Unauthorized");
        root.GetProperty("type").GetString().ShouldBe("https://hexalith.dev/problems/authentication-required");
        root.GetProperty("detail").GetString().ShouldBe("Bearer authentication is required to access the MCP endpoint.");
        root.GetProperty("instance").GetString().ShouldBe("/mcp");
    }

    [Fact]
    public async Task McpEndpoint_WithMalformedBearer_ReturnsSanitizedInvalidTokenChallenge()
    {
        await using var factory = new McpWebAppFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        const string RawBearerValue = "raw-token-material-that-must-not-leak";
        client.DefaultRequestHeaders.Authorization = new("Bearer", RawBearerValue);
        using var content = CreateMcpInitializeRequest();

        using HttpResponseMessage response = await client.PostAsync("/mcp", content, TestContext.Current.CancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ToString().ShouldBe(
            "Bearer realm=\"hexalith-memories-mcp\", error=\"invalid_token\", error_description=\"The token is invalid\"");
        response.Headers.WwwAuthenticate.ToString().ShouldNotContain(RawBearerValue);
        responseBody.ShouldNotContain(RawBearerValue);

        using JsonDocument body = JsonDocument.Parse(responseBody);
        JsonElement root = body.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Unauthorized);
        root.GetProperty("title").GetString().ShouldBe("Unauthorized");
        root.GetProperty("type").GetString().ShouldBe("https://hexalith.dev/problems/authentication-required");
        root.GetProperty("detail").GetString().ShouldBe("The provided MCP bearer token is invalid.");
        root.GetProperty("instance").GetString().ShouldBe("/mcp");
    }

    private static StringContent CreateMcpInitializeRequest()
        => new(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""",
            Encoding.UTF8,
            "application/json");

    private sealed class McpWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment("Development");
            _ = builder.ConfigureAppConfiguration((context, configuration) =>
            {
                Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Authentication:JwtBearer:Issuer"] = "hexalith-memories-test",
                    ["Authentication:JwtBearer:Audience"] = "hexalith-memories-mcp",
                    ["Authentication:JwtBearer:SigningKey"] = "hexalith-memories-test-signing-key-32b",
                    ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
                };

                _ = configuration.AddInMemoryCollection(settings);
                _ = context;
            });
        }
    }
}
