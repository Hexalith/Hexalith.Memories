// <copyright file="ITenantEmbeddingConfigProvider.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Provides tenant embedding configuration with bounded per-process caching.</summary>
public interface ITenantEmbeddingConfigProvider
{
    /// <summary>Gets the embedding configuration for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant embedding configuration.</returns>
    Task<TenantEmbeddingConfig> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Gets the fusion weights for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The tenant fusion weights.</returns>
    Task<FusionWeights> GetFusionWeightsAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Invalidates cached tenant configuration for a tenant.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    void Invalidate(string tenantId);
}
