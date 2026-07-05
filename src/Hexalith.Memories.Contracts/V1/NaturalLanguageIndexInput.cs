// <copyright file="NaturalLanguageIndexInput.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Story 9.2 Task 4.2 — input for <c>IndexNaturalLanguageSemanticActivity</c>. Mirrors the fields
/// <see cref="IndexInput"/> carries for the raw-payload index, plus the three NL-specific fields
/// (<see cref="NaturalLanguageDescription"/>, <see cref="DescriptionConfidence"/>,
/// <see cref="ConfidenceSource"/>). Field list is explicit (Improvement AH) so the mapping is auditable
/// without grep.</summary>
public sealed record NaturalLanguageIndexInput : IWorkflowTraceContextCarrier
{
    /// <summary>The memory unit identifier.</summary>
    public required string MemoryUnitId { get; init; }

    /// <summary>The tenant identifier.</summary>
    public required string TenantId { get; init; }

    /// <summary>The case identifier.</summary>
    public required string CaseId { get; init; }

    /// <summary>The NL-description embedding vector (same provider + dimensions as the raw index).</summary>
    public required float[] EmbeddingVector { get; init; }

    /// <summary>The embedding provider (e.g., <c>"openai"</c>).</summary>
    public required string EmbeddingProvider { get; init; }

    /// <summary>The embedding model identifier (e.g., <c>"text-embedding-3-small"</c>).</summary>
    public required string EmbeddingModel { get; init; }

    /// <summary>The embedding dimensions — must match the raw semantic index dimensions.</summary>
    public required int EmbeddingDimensions { get; init; }

    /// <summary>The LLM-authored single-sentence business-meaning description. Non-empty on success.</summary>
    public required string NaturalLanguageDescription { get; init; }

    /// <summary>Optional numeric confidence in <c>[0, 1]</c> when <see cref="ConfidenceSource"/> is
    /// <see cref="V1.ConfidenceSource.Logprobs"/>; <see langword="null"/> otherwise (nullable per
    /// Occam refinement — UI renders "measured vs. unmeasured" structurally).</summary>
    public float? DescriptionConfidence { get; init; }

    /// <summary>Classifies how <see cref="DescriptionConfidence"/> was derived.</summary>
    public required ConfidenceSource ConfidenceSource { get; init; }

    /// <summary>Gets the serialized request trace context captured before workflow scheduling.</summary>
    public WorkflowTraceContext? TraceContext { get; init; }
}
