// <copyright file="MemoriesMcpAuthenticationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Mcp.Authentication;

/// <summary>Configuration options for MCP JWT bearer authentication.</summary>
public sealed record MemoriesMcpAuthenticationOptions
{
    /// <summary>Gets the OIDC authority URL. When set, signing keys are discovered from metadata.</summary>
    public string? Authority { get; init; }

    /// <summary>Gets the expected token audience.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Gets the expected token issuer.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the symmetric development/test signing key used when <see cref="Authority"/> is unset.</summary>
    public string? SigningKey { get; init; }

    /// <summary>Gets a value indicating whether OIDC metadata must be fetched over HTTPS.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>Gets the raw JWT claim name that carries the primary authorized tenant identifier.</summary>
    public string TenantClaimName { get; init; } = "tenant_id";

    /// <summary>Gets accepted JWT signing algorithms.</summary>
    public IReadOnlyList<string> ValidAlgorithms { get; init; } = ["HS256", "RS256", "ES256"];

    /// <summary>Gets claim names whose values may be included in security logs.</summary>
    public IReadOnlyList<string> LoggableClaimAllowlist { get; init; } =
    [
        "sub",
        "aud",
        "iss",
        "exp",
        "tenant_id",
        MemoriesMcpClaimsTransformation.TenantClaimType,
    ];
}
