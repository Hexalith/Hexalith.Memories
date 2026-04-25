// <copyright file="McpToolTestFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Tests;

using System.Security.Claims;

using Hexalith.Memories.Mcp.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

internal static class McpToolTestFactory
{
    public static (TenantClaimAuthorizationFilter Authorization, IAuthorizedTenantAccessor Accessor) CreateAuth(string tenantId = "acme")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(MemoriesMcpClaimsTransformation.TenantClaimType, tenantId)],
            "Bearer"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var authorization = new TenantClaimAuthorizationFilter(
            httpContextAccessor,
            new McpErrorMapper(),
            Options.Create(new MemoriesMcpAuthenticationOptions()),
            NullLogger<TenantClaimAuthorizationFilter>.Instance);
        return (authorization, new AuthorizedTenantAccessor(httpContextAccessor));
    }
}
