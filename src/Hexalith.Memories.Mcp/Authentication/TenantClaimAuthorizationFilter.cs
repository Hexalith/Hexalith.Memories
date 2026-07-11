// <copyright file="TenantClaimAuthorizationFilter.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using System.Security.Claims;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;

/// <summary>Authorizes MCP tool tenant arguments against the inbound JWT tenant claims.</summary>
internal sealed partial class TenantClaimAuthorizationFilter(
    IHttpContextAccessor httpContextAccessor,
    McpErrorMapper errorMapper,
    IOptions<MemoriesMcpAuthenticationOptions> options,
    ILogger<TenantClaimAuthorizationFilter> logger)
{
    /// <summary>Attempts to authorize a tool call for a tenant.</summary>
    /// <param name="tenantId">The tenant argument supplied to the MCP tool.</param>
    /// <param name="toolName">The MCP tool name.</param>
    /// <param name="authorizedTenant">The tenant id snapshot approved for downstream calls.</param>
    /// <param name="error">The structured MCP error result when authorization fails.</param>
    /// <returns><c>true</c> when the caller is authorized for the requested tenant.</returns>
    public bool TryAuthorizeTenant(string tenantId, string toolName, out string authorizedTenant, out CallToolResult? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        authorizedTenant = string.Empty;
        error = null;

        if (!IsWellFormedTenantId(tenantId))
        {
            error = errorMapper.MapAuthorization(tenantId, toolName, "TENANT_MALFORMED");
            return false;
        }

        HttpContext? context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            logger.LogWarning(
                "MCP tenant authorization failed: SecurityEvent={SecurityEvent}, Tool={Tool}, Tenant={Tenant}, Reason={Reason}",
                "TenantAuthorizationDenied",
                toolName,
                tenantId,
                "MissingHttpContext");
            error = errorMapper.MapAuthorization(tenantId, toolName, "TENANT_FORBIDDEN");
            return false;
        }

        ClaimsPrincipal user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            error = Deny(tenantId, toolName, user, "UnauthenticatedPrincipal");
            return false;
        }

        string[] tenantClaims = [.. user.FindAll(MemoriesMcpClaimsTransformation.TenantClaimType).Select(c => c.Value)];
        if (!tenantClaims.Any(value => string.Equals(value, tenantId, StringComparison.Ordinal)))
        {
            error = Deny(tenantId, toolName, user, "TenantClaimMissingOrMismatch");
            return false;
        }

        authorizedTenant = tenantId;
        return true;
    }

    private static bool IsWellFormedTenantId(string tenantId)
        => !string.IsNullOrWhiteSpace(tenantId) && TenantIdRegex().IsMatch(tenantId);

    private CallToolResult Deny(string tenantId, string toolName, ClaimsPrincipal principal, string reason)
    {
        logger.LogWarning(
            "MCP tenant authorization failed: SecurityEvent={SecurityEvent}, Tool={Tool}, Tenant={Tenant}, Reason={Reason}, Claims={Claims}",
            "TenantAuthorizationDenied",
            toolName,
            tenantId,
            reason,
            FormatClaimsForLog(principal));
        return errorMapper.MapAuthorization(tenantId, toolName, "TENANT_FORBIDDEN");
    }

    private string FormatClaimsForLog(ClaimsPrincipal principal)
    {
        HashSet<string> allowed = new(options.Value.LoggableClaimAllowlist, StringComparer.Ordinal);
        return string.Join(
            ";",
            principal.Claims.Select(c =>
            {
                string value = allowed.Contains(c.Type) ? c.Value : "[REDACTED]";
                return $"{c.Type}={value}";
            }));
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantIdRegex();
}
