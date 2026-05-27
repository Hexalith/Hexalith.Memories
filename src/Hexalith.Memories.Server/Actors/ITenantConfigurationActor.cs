// <copyright file="ITenantConfigurationActor.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Actors;

using Dapr.Actors;

using Hexalith.Memories.Contracts.V1;

/// <summary>DAPR Actor interface for per-tenant embedding configuration storage.</summary>
public interface ITenantConfigurationActor : IActor
{
    /// <summary>Gets the tenant's embedding configuration, returning Google defaults if not configured.</summary>
    /// <returns>The <see cref="TenantEmbeddingConfig"/> for this tenant.</returns>
    Task<TenantEmbeddingConfig> GetEmbeddingConfigAsync();

    /// <summary>Sets the tenant's embedding configuration with reindex change detection.</summary>
    /// <param name="config">The new embedding configuration.</param>
    /// <param name="forceReindex">If true, allows breaking changes and sets the ReindexRequired flag.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetEmbeddingConfigAsync(TenantEmbeddingConfig config, bool forceReindex);
}
