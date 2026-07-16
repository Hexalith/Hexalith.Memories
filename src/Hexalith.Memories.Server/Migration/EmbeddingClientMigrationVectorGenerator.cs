// <copyright file="EmbeddingClientMigrationVectorGenerator.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Ingestion;

/// <summary>Migration vector generator backed by the committed provider-aware <see cref="EmbeddingClient"/>.</summary>
/// <param name="embeddingClient">The embedding client.</param>
public sealed class EmbeddingClientMigrationVectorGenerator(EmbeddingClient embeddingClient) : IEmbeddingMigrationVectorGenerator
{
    /// <inheritdoc/>
    public Task<float[]> GenerateAsync(string text, string tenantId, TenantEmbeddingConfig config, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(config);
        return embeddingClient.GenerateAsync(text, tenantId, config, ct);
    }
}
