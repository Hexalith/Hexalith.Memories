// <copyright file="SemanticChunkIndexInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Input for writing one or more raw semantic chunk vectors for a base memory unit.</summary>
public sealed record SemanticChunkIndexInput : IWorkflowTraceContextCarrier
{
    /// <summary>Gets the stable base memory-unit identifier.</summary>
    public required string MemoryUnitId { get; init; }

    /// <summary>Gets the tenant identifier.</summary>
    public required string TenantId { get; init; }

    /// <summary>Gets the case identifier.</summary>
    public required string CaseId { get; init; }

    /// <summary>Gets the ordered chunk embeddings.</summary>
    public required IReadOnlyList<ChunkEmbeddingResult> Chunks { get; init; }

    /// <summary>Gets the compound provider identifier.</summary>
    public required string EmbeddingProvider { get; init; }

    /// <summary>Gets the embedding model identifier.</summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>Gets the vector dimensions.</summary>
    public required int EmbeddingDimensions { get; init; }

    /// <summary>Gets metadata used for semantic TAG fields.</summary>
    public Dictionary<string, MetadataField> Metadata
    {
        get => field ??= new Dictionary<string, MetadataField>(StringComparer.Ordinal);
        init => field = value switch
        {
            null => new Dictionary<string, MetadataField>(StringComparer.Ordinal),
            Dictionary<string, MetadataField> existing when ReferenceEquals(existing.Comparer, StringComparer.Ordinal) => existing,
            _ => new Dictionary<string, MetadataField>(value, StringComparer.Ordinal),
        };
    }

    /// <summary>Gets the serialized request trace context captured before workflow scheduling.</summary>
    public WorkflowTraceContext? TraceContext { get; init; }
}
