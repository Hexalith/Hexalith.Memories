// <copyright file="McpEndpointAllowAnonymousPathsTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests.Authentication;

using System.Net;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Shouldly;

/// <summary>
/// Story 10.2 Task 13.7 — endpoint-level guard that keeps probe endpoints anonymous while requiring
/// bearer authentication on the MCP transport route.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpEndpointAllowAnonymousPathsTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/ready")]
    public async Task ProbeEndpoint_WithoutBearer_DoesNotReturnUnauthorized(string path)
    {
        await using var factory = new McpWebAppFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task McpEndpoint_WithoutBearer_ReturnsBearerChallenge()
    {
        await using var factory = new McpWebAppFactory();
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""",
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await client.PostAsync("/mcp", content, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer realm=\"hexalith-memories-mcp\"");
    }

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
