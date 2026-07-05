// <copyright file="ChunkEmbeddingBatchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Batch embedding result for raw payload chunks.</summary>
public sealed record ChunkEmbeddingBatchResult
{
    /// <summary>Gets the ordered chunk embeddings.</summary>
    public required IReadOnlyList<ChunkEmbeddingResult> Chunks { get; init; }

    /// <summary>Gets the compound provider identifier.</summary>
    public required string Provider { get; init; }

    /// <summary>Gets the embedding model identifier.</summary>
    public required string Model { get; init; }

    /// <summary>Gets the configured vector dimensions.</summary>
    public required int Dimensions { get; init; }
}
