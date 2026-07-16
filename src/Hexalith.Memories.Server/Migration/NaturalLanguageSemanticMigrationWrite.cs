// <copyright file="NaturalLanguageSemanticMigrationWrite.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Natural-language semantic hash write payload.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="CaseId">The case identifier.</param>
/// <param name="NaturalLanguageDescription">The persisted natural-language description.</param>
/// <param name="DescriptionOrigin">The persisted description origin, or null when absent.</param>
/// <param name="DescriptionConfidence">The persisted description confidence, or null when absent.</param>
/// <param name="DescriptionConfidenceSource">The persisted confidence source, or null when absent.</param>
/// <param name="Embedding">The generated embedding vector.</param>
public sealed record NaturalLanguageSemanticMigrationWrite(
    string MemoryUnitId,
    string CaseId,
    string NaturalLanguageDescription,
    string? DescriptionOrigin,
    string? DescriptionConfidence,
    string? DescriptionConfidenceSource,
    float[] Embedding);
