// <copyright file="ServerTenantClaimsTransformation.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>Normalizes inbound JWT tenant claims for Memories Server authorization.</summary>
public sealed class ServerTenantClaimsTransformation(
    IOptions<MemoriesServerAuthenticationOptions> options,
    ILogger<ServerTenantClaimsTransformation> logger) : IClaimsTransformation
{
    /// <summary>Normalized tenant claim type used by Memories Server authorization.</summary>
    public const string TenantClaimType = "memories:tenant";

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

        MemoriesServerAuthenticationOptions authOptions = options.Value;
        if (HasConflictingCaseInsensitiveTenantClaims(principal, authOptions.TenantClaimName))
        {
            logger.LogWarning(
                "Memories Server authentication rejected principal because duplicate tenant claims differing only by case carried different values.");
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
            "Memories Server claims transformation completed: TenantCount={TenantCount}",
            identity.Claims.Count(c => c.Type == TenantClaimType));

        return Task.FromResult(principal);
    }

    private static bool HasConflictingCaseInsensitiveTenantClaims(ClaimsPrincipal principal, string tenantClaimName)
    {
        string[] sourceClaimTypes = [tenantClaimName, "tenants", "tid", "tenant"];
        foreach (string sourceClaimType in sourceClaimTypes)
        {
            Claim[] claims = [.. principal.Claims
                .Where(c => string.Equals(c.Type, sourceClaimType, StringComparison.OrdinalIgnoreCase))
                .ToArray()];
            string[] claimTypeCasings = [.. claims.Select(c => c.Type).Distinct(StringComparer.Ordinal)];
            string[] values = [.. claims.Select(c => c.Value).Distinct(StringComparer.Ordinal)];

            if (claimTypeCasings.Length > 1 && values.Length > 1)
            {
                return true;
            }
        }

        return false;
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
        AddClaimsFromJwt(principal, identity, tenantClaimName);
        AddClaimsFromJwt(principal, identity, "tenants");

        foreach (string alternate in new[] { "tid", "tenant" })
        {
            string? tenant = principal.FindFirst(alternate)?.Value;
            if (!string.IsNullOrWhiteSpace(tenant) && !identity.HasClaim(TenantClaimType, tenant))
            {
                identity.AddClaim(new Claim(TenantClaimType, tenant));
            }
        }
    }

    private void AddClaimsFromJwt(ClaimsPrincipal principal, ClaimsIdentity identity, string sourceClaimType)
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
                        AddTenantClaim(identity, item);
                    }

                    return;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(
                    "Failed to parse Memories Server JWT claim '{ClaimType}' as JSON array. Falling back to space-delimited parsing. Error: {Error}",
                    sourceClaimType,
                    ex.Message);
            }
        }

        string[] parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            AddTenantClaim(identity, part);
        }
    }

    private static void AddTenantClaim(ClaimsIdentity identity, string tenant)
    {
        if (!identity.HasClaim(TenantClaimType, tenant))
        {
            identity.AddClaim(new Claim(TenantClaimType, tenant));
        }
    }
}
