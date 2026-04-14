// <copyright file="TenantEmbeddingConfig.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Per-tenant embedding provider configuration.</summary>
public sealed record TenantEmbeddingConfig
{
    public required string Provider { get; init; }

    public required string Model { get; init; }

    public required int Dimensions { get; init; }

    public required int RateLimitPerMinute { get; init; }

    /// <summary>
    /// The <em>name / identifier</em> of the secret that stores the provider API key
    /// (e.g. <c>"google-embedding-api-key"</c>) — NOT the secret value itself.
    /// The server resolves the secret via the configured DAPR secret store at embedding time;
    /// the name is safe to return in public-facing configuration responses (see Story 5.5 AC2).
    /// </summary>
    public required string ApiSecretKeyName { get; init; }

    public bool ReindexRequired { get; init; }
}
