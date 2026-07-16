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
    public static McpToolExecutor CreateExecutor(params string[] tenantIds)
    {
        if (tenantIds.Length == 0)
        {
            tenantIds = ["acme"];
        }

        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            tenantIds.Select(tenantId => new Claim(MemoriesMcpClaimsTransformation.TenantClaimType, tenantId)),
            "Bearer"));
        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var errorMapper = new McpErrorMapper();
        var authorization = new TenantClaimAuthorizationFilter(
            httpContextAccessor,
            errorMapper,
            Options.Create(new MemoriesMcpAuthenticationOptions()),
            NullLogger<TenantClaimAuthorizationFilter>.Instance);
        return new McpToolExecutor(authorization, errorMapper);
    }
}
