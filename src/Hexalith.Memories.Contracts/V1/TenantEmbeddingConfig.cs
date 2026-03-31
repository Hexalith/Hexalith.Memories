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

    public required string ApiSecretKeyName { get; init; }

    public bool ReindexRequired { get; init; }
}
