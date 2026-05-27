// <copyright file="NaturalLanguageMigrationUnit.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Natural-language semantic hash data used for NL vector migration.</summary>
/// <param name="MemoryUnitId">The memory unit identifier.</param>
/// <param name="CaseId">The case identifier.</param>
/// <param name="NaturalLanguageDescription">The persisted natural-language description.</param>
/// <param name="DescriptionOrigin">The persisted description origin.</param>
/// <param name="DescriptionConfidence">The persisted description confidence.</param>
/// <param name="DescriptionConfidenceSource">The persisted confidence source.</param>
/// <param name="State">The current provider, model, and dimension metadata, or null when fully absent.</param>
public sealed record NaturalLanguageMigrationUnit(
    string MemoryUnitId,
    string? CaseId,
    string? NaturalLanguageDescription,
    string? DescriptionOrigin,
    string? DescriptionConfidence,
    string? DescriptionConfidenceSource,
    SemanticMigrationState? State);
