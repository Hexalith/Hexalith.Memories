// <copyright file="EmbeddingMigrationIndexInfo.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Migration;

/// <summary>Dimension inventory for the two active semantic Redis Vector indexes.</summary>
/// <param name="RawSemanticDimensions">The raw semantic vector dimensions, or null when unavailable.</param>
/// <param name="NaturalLanguageSemanticDimensions">The natural-language semantic vector dimensions, or null when unavailable.</param>
public sealed record EmbeddingMigrationIndexInfo(int? RawSemanticDimensions, int? NaturalLanguageSemanticDimensions);
