// <copyright file="ServerUpstreamTokenFactory.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Mints short-lived server-realm bearer tokens so the MCP can call the JWT-protected upstream Memories
/// Server on behalf of an already-authorized caller (Story 20.x). The MCP has already validated the
/// caller's tenant claim before any upstream call is made; this factory carries those tenant claims
/// forward so the server's per-tenant authorization filter accepts the request.
/// </summary>
public sealed class ServerUpstreamTokenFactory(IOptions<MemoriesMcpUpstreamAuthenticationOptions> options)
{
    /// <summary>Mints a server-realm token carrying <paramref name="tenantIds"/> as the tenant claim.</summary>
    /// <param name="tenantIds">The caller's authorized tenant ids. May be empty for an auth-only token.</param>
    /// <returns>The compact-serialized JWT, or <c>null</c> when the upstream realm is not configured.</returns>
    public string? Mint(IReadOnlyCollection<string> tenantIds)
    {
        ArgumentNullException.ThrowIfNull(tenantIds);

        MemoriesMcpUpstreamAuthenticationOptions o = options.Value;
        if (string.IsNullOrWhiteSpace(o.SigningKey))
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["sub"] = "memories-mcp",
        };

        string[] tenants = [.. tenantIds
            .Where(static tenant => !string.IsNullOrWhiteSpace(tenant))
            .Distinct(StringComparer.Ordinal)];
        if (tenants.Length > 0)
        {
            // The server maps a single tenant_id claim to memories:tenant, splitting the value on spaces.
            // Tenant ids never contain spaces (validated against ^[A-Za-z0-9_-]{1,128}$), so a space-joined
            // value carries every authorized tenant in one claim.
            string claimName = string.IsNullOrWhiteSpace(o.TenantClaimName) ? "tenant_id" : o.TenantClaimName;
            claims[claimName] = string.Join(' ', tenants);
        }

        int lifetimeMinutes = o.TokenLifetimeMinutes <= 0 ? 5 : o.TokenLifetimeMinutes;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(lifetimeMinutes),
            Claims = claims,
            SigningCredentials = credentials,
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
