// <copyright file="NaturalLanguageSemanticSearchHit.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Search;

/// <summary>Story 9.2 Task 4.9 — a single hit from the natural-language semantic search.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="Similarity">Cosine similarity in [0.0, 1.0] (1.0 = identical).</param>
/// <param name="NaturalLanguageDescription">The LLM-authored description (may be empty if the hash is corrupt).</param>
/// <param name="DescriptionConfidence">Nullable confidence signal matching the ingestion-time confidence.</param>
/// <param name="ConfidenceSource">The confidence source discriminator (<c>logprobs</c>, <c>constant</c>, <c>unknown</c>).</param>
public sealed record NaturalLanguageSemanticSearchHit(
    string MemoryUnitId,
    double Similarity,
    string NaturalLanguageDescription,
    float? DescriptionConfidence,
    string ConfidenceSource);
