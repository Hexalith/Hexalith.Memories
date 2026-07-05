// <copyright file="ChunkEmbeddingResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

using Hexalith.Memories.Contracts.V1;

/// <summary>Embedding result for one raw payload chunk.</summary>
public sealed record ChunkEmbeddingResult
{
    /// <summary>Gets the zero-based chunk sequence.</summary>
    public required int Sequence { get; init; }

    /// <summary>Gets the non-empty chunk text.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the optional claim-check reference for <see cref="Text"/>.</summary>
    public WorkflowPayloadReference? TextReference { get; init; }

    /// <summary>Gets the inclusive source start offset.</summary>
    public required int StartOffset { get; init; }

    /// <summary>Gets the exclusive source end offset.</summary>
    public required int EndOffset { get; init; }

    /// <summary>Gets the chunk token estimate.</summary>
    public required int EstimatedTokens { get; init; }

    /// <summary>Gets the embedding vector for this chunk.</summary>
    public required float[] Vector { get; init; }

    /// <summary>Gets the optional claim-check reference for <see cref="Vector"/>.</summary>
    public WorkflowPayloadReference? VectorReference { get; init; }
}
