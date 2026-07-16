// <copyright file="MemoriesMcpClaimsTransformation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>Normalizes inbound JWT tenant claims for MCP authorization.</summary>
public sealed class MemoriesMcpClaimsTransformation(
    IOptions<MemoriesMcpAuthenticationOptions> options,
    ILogger<MemoriesMcpClaimsTransformation> logger) : IClaimsTransformation
{
    /// <summary>Normalized tenant claim type used by MCP authorization.</summary>
    internal const string TenantClaimType = "memories:tenant";

    private const string SubjectClaimType = "sub";

    /// <inheritdoc />
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.HasClaim(c => c.Type == TenantClaimType)
            && (!principal.HasClaim(c => c.Type == SubjectClaimType) || principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier)))
        {
            return Task.FromResult(principal);
        }

        MemoriesMcpAuthenticationOptions authOptions = options.Value;
        if (HasConflictingCaseInsensitiveTenantClaims(principal, authOptions.TenantClaimName))
        {
            logger.LogWarning(
                "MCP authentication rejected principal because duplicate tenant claims differing only by case carried different values.");
            return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity();
        AddNameIdentifierClaim(principal, identity);
        AddTenantClaims(principal, identity, authOptions.TenantClaimName);

        if (identity.Claims.Any())
        {
            principal.AddIdentity(identity);
        }

        logger.LogDebug(
            "MCP claims transformation completed: TenantCount={TenantCount}",
            identity.Claims.Count(c => c.Type == TenantClaimType));

        return Task.FromResult(principal);
    }

    private static bool HasConflictingCaseInsensitiveTenantClaims(ClaimsPrincipal principal, string tenantClaimName)
    {
        string[] values = [.. principal.Claims
            .Where(c => string.Equals(c.Type, tenantClaimName, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)];
        return values.Length > 1;
    }

    private static void AddNameIdentifierClaim(ClaimsPrincipal principal, ClaimsIdentity identity)
    {
        if (principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
        {
            return;
        }

        string? subject = principal.FindFirst(SubjectClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(subject))
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subject));
        }
    }

    private void AddTenantClaims(ClaimsPrincipal principal, ClaimsIdentity identity, string tenantClaimName)
    {
        AddClaimsFromJwt(principal, identity, tenantClaimName, TenantClaimType);
        AddClaimsFromJwt(principal, identity, "tenants", TenantClaimType);

        foreach (string alternate in new[] { "tid", "tenant" })
        {
            string? tenant = principal.FindFirst(alternate)?.Value;
            if (!string.IsNullOrWhiteSpace(tenant) && !identity.HasClaim(TenantClaimType, tenant))
            {
                identity.AddClaim(new Claim(TenantClaimType, tenant));
            }
        }
    }

    private void AddClaimsFromJwt(ClaimsPrincipal principal, ClaimsIdentity identity, string sourceClaimType, string targetClaimType)
    {
        Claim? sourceClaim = principal.FindFirst(sourceClaimType);
        if (sourceClaim is null)
        {
            return;
        }

        string value = sourceClaim.Value;
        if (value.StartsWith('['))
        {
            try
            {
                string[]? items = JsonSerializer.Deserialize<string[]>(value);
                if (items is not null)
                {
                    foreach (string item in items.Where(static item => !string.IsNullOrWhiteSpace(item)))
                    {
                        identity.AddClaim(new Claim(targetClaimType, item));
                    }

                    return;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(
                    "Failed to parse MCP JWT claim '{ClaimType}' as JSON array. Falling back to space-delimited parsing. Error: {Error}",
                    sourceClaimType,
                    ex.Message);
            }
        }

        string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            identity.AddClaim(new Claim(targetClaimType, part));
        }
    }
}
