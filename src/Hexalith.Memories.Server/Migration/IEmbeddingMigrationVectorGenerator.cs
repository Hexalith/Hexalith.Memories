// <copyright file="IEmbeddingMigrationVectorGenerator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Contracts.V1;

/// <summary>Generates provider-aware embeddings for the migration service.</summary>
public interface IEmbeddingMigrationVectorGenerator
{
    /// <summary>Generates an embedding vector for one tenant and target configuration.</summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="config">The target embedding configuration.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The generated embedding vector.</returns>
    Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct);
}
