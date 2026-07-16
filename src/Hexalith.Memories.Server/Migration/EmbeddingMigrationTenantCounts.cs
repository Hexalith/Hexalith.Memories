// <copyright file="EmbeddingMigrationTenantCounts.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Inventory counts for one tenant's migration-relevant Redis keys.</summary>
/// <param name="SyntacticMemoryUnitCount">The count of syntactic memory-unit hashes.</param>
/// <param name="RawSemanticUnitCount">The count of raw semantic vector hashes.</param>
/// <param name="NaturalLanguageSemanticUnitCount">The count of natural-language semantic vector hashes.</param>
/// <param name="RawStaleMetadataCount">The count of raw semantic hashes not stamped with the target metadata.</param>
/// <param name="NaturalLanguageStaleMetadataCount">The count of natural-language semantic hashes not stamped with the target metadata.</param>
public sealed record EmbeddingMigrationTenantCounts(
    long SyntacticMemoryUnitCount,
    long RawSemanticUnitCount,
    long NaturalLanguageSemanticUnitCount,
    long RawStaleMetadataCount,
    long NaturalLanguageStaleMetadataCount);
