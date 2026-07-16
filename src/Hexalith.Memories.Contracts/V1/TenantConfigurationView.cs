// <copyright file="TenantConfigurationView.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Composed operator-facing configuration view returned by <c>GET /api/v1/tenants/{tenantId}/configuration</c>
/// (Story 5.5 AC2 / FR45). Embeds the full <see cref="TenantEmbeddingConfig"/> directly — no duplicate
/// projection record (Amendment C): <see cref="TenantEmbeddingConfig.ApiSecretKeyName"/> is the
/// <em>name/identifier</em> of the secret in the secret store, not the secret value, and is safe to return.
/// </summary>
public sealed record TenantConfigurationView
{
    /// <summary>The tenant identifier.</summary>
    public required string Id { get; init; }

    /// <summary>The tenant display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The tenant lifecycle status.</summary>
    public required TenantStatus Status { get; init; }

    /// <summary>When the tenant was registered.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the tenant last ingested a memory unit, or <see langword="null"/> for a tenant that has never ingested.</summary>
    public DateTimeOffset? LastActivityAt { get; init; }

    /// <summary>Number of memory units stored for the tenant. <see langword="null"/> when Redis is unavailable.</summary>
    public long? MemoryUnitCount { get; init; }

    /// <summary>The tenant's full embedding configuration (provider, model, dimensions, rate limit, secret name, reindex flag).</summary>
    public required TenantEmbeddingConfig EmbeddingConfig { get; init; }

    /// <summary>Per-backend health for the tenant's indexes/graph.</summary>
    public required TenantIndexStatus IndexStatus { get; init; }
}
