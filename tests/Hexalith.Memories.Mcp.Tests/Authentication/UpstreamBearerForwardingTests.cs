// <copyright file="UpstreamBearerForwardingTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests.Authentication;

using System.Security.Claims;

using Hexalith.Memories.Mcp;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>Tests that MCP forwards the validated OIDC bearer byte-for-byte to Server.</summary>
public sealed class UpstreamBearerForwardingTests
{
    [Fact]
    public void ApplyServerUpstreamBearer_AuthenticatedRequest_ForwardsSameBearer()
    {
        const string Bearer = "header.payload.signature";
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "caller")], "Bearer")),
        };
        context.Request.Headers.Authorization = $"Bearer {Bearer}";
        var accessor = new HttpContextAccessor { HttpContext = context };
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .BuildServiceProvider();
        using var client = new HttpClient();

        McpCompositionRoot.ApplyServerUpstreamBearer(services, client);

        client.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization.Scheme.ShouldBe("Bearer");
        client.DefaultRequestHeaders.Authorization.Parameter.ShouldBe(Bearer);
    }

    [Fact]
    public void ApplyServerUpstreamBearer_UnauthenticatedRequest_DoesNotForwardHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer unvalidated-token";
        var accessor = new HttpContextAccessor { HttpContext = context };
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .BuildServiceProvider();
        using var client = new HttpClient();

        McpCompositionRoot.ApplyServerUpstreamBearer(services, client);

        client.DefaultRequestHeaders.Authorization.ShouldBeNull();
    }
}
