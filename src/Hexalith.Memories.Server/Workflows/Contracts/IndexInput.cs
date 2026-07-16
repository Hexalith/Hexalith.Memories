// <copyright file="IndexInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Shared input for all three indexing activities (syntactic, semantic, graph).</summary>
public sealed record IndexInput : IWorkflowTraceContextCarrier
{
    public required string MemoryUnitId { get; init; }

    public required string TenantId { get; init; }

    public required string CaseId { get; init; }

    public required string Content { get; init; }

    /// <summary>Gets the optional claim-check reference for <see cref="Content"/> on new workflow paths.</summary>
    public WorkflowPayloadReference? ContentReference { get; init; }

    public required string ContentHash { get; init; }

    public required string SourceUri { get; init; }

    public required SourceType SourceType { get; init; }

    public required string IngestedBy { get; init; }

    public required DateTimeOffset IngestedAt { get; init; }

    public required float[] EmbeddingVector { get; init; }

    /// <summary>Gets the optional claim-check reference for <see cref="EmbeddingVector"/> on new workflow paths.</summary>
    public WorkflowPayloadReference? EmbeddingVectorReference { get; init; }

    public required string EmbeddingProvider { get; init; }

    /// <summary>The embedding model identifier (e.g. "gemini-embedding-001"). Required: every new ingestion must supply the model that generated the vector (FR70, Story 5.5).</summary>
    public required string EmbeddingModel { get; init; }

    public required int EmbeddingDimensions { get; init; }

    // Pinned to StringComparer.Ordinal (decision D6 — committed-branch review 2026-04-24) to
    // match IngestionInput.Metadata and guarantee consistent lookups across the ingestion pipeline.
    // S6-P12 (re-review 2026-04-25): fast-path skips reallocation when the assigned dictionary
    // already uses StringComparer.Ordinal — preserves reference identity for `with`-expressions
    // that round-trip the same Metadata instance.
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

    public string? CausationId { get; init; }

    public string? CorrelationId { get; init; }

    /// <summary>Gets the serialized request trace context captured before workflow scheduling.</summary>
    public WorkflowTraceContext? TraceContext { get; init; }
}
