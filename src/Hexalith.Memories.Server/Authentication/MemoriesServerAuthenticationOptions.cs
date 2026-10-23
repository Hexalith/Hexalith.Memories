// <copyright file="MemoriesServerAuthenticationOptions.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Authentication;

/// <summary>Configuration options for Memories Server JWT bearer authentication.</summary>
public sealed record MemoriesServerAuthenticationOptions
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

    /// <summary>Gets the raw JWT claim name that carries the tenant identifier for later authorization stories.</summary>
    public string TenantClaimName { get; init; } = "tenant_id";

    private IReadOnlyList<string> _validAlgorithms = ["HS256", "RS256", "ES256"];

    /// <summary>Gets accepted JWT signing algorithms.</summary>
    public IReadOnlyList<string> ValidAlgorithms
    {
        get => _validAlgorithms;
        init => _validAlgorithms = value;
    }

    /// <summary>Replaces an explicit configuration list without changing the public init-only setter contract.</summary>
    /// <param name="algorithms">The configured algorithms, without initialized defaults.</param>
    internal void ReplaceConfiguredAlgorithms(IReadOnlyList<string> algorithms) => _validAlgorithms = algorithms;
}
