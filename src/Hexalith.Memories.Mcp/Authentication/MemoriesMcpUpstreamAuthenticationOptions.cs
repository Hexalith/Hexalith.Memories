// <copyright file="MemoriesMcpUpstreamAuthenticationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

/// <summary>
/// Story 20.x — configuration for the server-realm bearer token the MCP mints when invoking the upstream
/// Memories Server. The values must match the server's <c>Authentication:JwtBearer</c> issuer, audience,
/// signing key, and tenant-claim name so the minted token passes the server's JWT validation and its
/// per-tenant authorization filter. Bound from configuration section <c>Authentication:ServerUpstream</c>.
/// When <see cref="SigningKey"/> is empty the MCP leaves upstream calls unauthenticated (no-op), so
/// environments that have not configured the shared upstream key keep their previous behavior.
/// </summary>
public sealed class MemoriesMcpUpstreamAuthenticationOptions
{
    /// <summary>Gets or sets the issuer stamped on minted upstream tokens.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the audience stamped on minted upstream tokens.</summary>
    public string? Audience { get; set; }

    /// <summary>Gets or sets the symmetric signing key shared with the upstream Memories Server.</summary>
    public string? SigningKey { get; set; }

    /// <summary>Gets or sets the JWT claim name that carries tenant ids (defaults to <c>tenant_id</c>).</summary>
    public string TenantClaimName { get; set; } = "tenant_id";

    /// <summary>Gets or sets the minted token lifetime in minutes (defaults to 5).</summary>
    public int TokenLifetimeMinutes { get; set; } = 5;
}
