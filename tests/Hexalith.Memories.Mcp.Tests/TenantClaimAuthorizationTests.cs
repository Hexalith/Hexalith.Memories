// <copyright file="TenantClaimAuthorizationTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Security.Claims;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;

using Shouldly;

public sealed class TenantClaimAuthorizationTests
{
    [Fact]
    public void AuthorizeTenant_AllowsMatchingTenantAndSnapshotsIt()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        var accessor = new HttpContextAccessor { HttpContext = context };
        var filter = CreateFilter(accessor);

        bool authorized = filter.TryAuthorizeTenant("tenant-a", "search_memory", out string tenantId, out CallToolResult? error);

        authorized.ShouldBeTrue();
        error.ShouldBeNull();
        tenantId.ShouldBe("tenant-a");
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey].ShouldBe("tenant-a");
    }

    [Fact]
    public void AuthorizeTenant_RejectsMissingTenantClaim()
    {
        DefaultHttpContext context = CreateContext();
        var filter = CreateFilter(new HttpContextAccessor { HttpContext = context });

        bool authorized = filter.TryAuthorizeTenant("tenant-a", "search_memory", out _, out CallToolResult? error);

        authorized.ShouldBeFalse();
        error.ShouldNotBeNull();
        error!.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe("TENANT_FORBIDDEN");
    }

    [Fact]
    public void AuthorizeTenant_RejectsMalformedTenantWithoutEchoingInput()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        var filter = CreateFilter(new HttpContextAccessor { HttpContext = context });
        string poisoned = "tenant-a\u202E";

        bool authorized = filter.TryAuthorizeTenant(poisoned, "search_memory", out _, out CallToolResult? error);

        authorized.ShouldBeFalse();
        error.ShouldNotBeNull();
        error!.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe("TENANT_MALFORMED");
        error.Content[0].ShouldBeOfType<TextContentBlock>().Text.ShouldNotContain(poisoned);
    }

    [Fact]
    public void AuthorizeTenant_RejectsMismatchedTenant()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        var filter = CreateFilter(new HttpContextAccessor { HttpContext = context });

        bool authorized = filter.TryAuthorizeTenant("tenant-b", "search_memory", out _, out CallToolResult? error);

        authorized.ShouldBeFalse();
        error.ShouldNotBeNull();
        error!.StructuredContent!.Value.GetProperty("code").GetString().ShouldBe("TENANT_FORBIDDEN");
    }

    [Fact]
    public void AuthorizeTenant_ThrowsWhenAuthorizedTenantAlreadyPresent()
    {
        DefaultHttpContext context = CreateContext("tenant-a");
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "stale-tenant";
        var filter = CreateFilter(new HttpContextAccessor { HttpContext = context });

        Should.Throw<InvalidOperationException>(
            () => filter.TryAuthorizeTenant("tenant-a", "search_memory", out _, out _))
            .Message.ShouldContain("HttpContext.Items leaked");
    }

    [Fact]
    public void AuthorizedTenantAccessor_ReturnsSnapshot()
    {
        var context = new DefaultHttpContext();
        context.Items[AuthorizedTenantAccessor.HttpContextItemKey] = "tenant-a";
        var accessor = new AuthorizedTenantAccessor(new HttpContextAccessor { HttpContext = context });

        accessor.TryGetAuthorizedTenant(out string tenantId).ShouldBeTrue();
        tenantId.ShouldBe("tenant-a");
    }

    private static DefaultHttpContext CreateContext(params string[] tenants)
    {
        var context = new DefaultHttpContext();
        var claims = tenants
            .Select(t => new Claim(MemoriesMcpClaimsTransformation.TenantClaimType, t))
            .ToArray();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return context;
    }

    private static TenantClaimAuthorizationFilter CreateFilter(IHttpContextAccessor accessor)
        => new(
            accessor,
            new McpErrorMapper(),
            Options.Create(new MemoriesMcpAuthenticationOptions()),
            NullLogger<TenantClaimAuthorizationFilter>.Instance);
}
