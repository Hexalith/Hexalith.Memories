// <copyright file="ConsistencySemanticDetail.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

using System.Text.Json.Serialization;

/// <summary>
/// Details extracted from the semantic index entry.
/// </summary>
/// <param name="EmbeddingDimensions">Number of float dimensions in the stored vector (bytes / 4).</param>
/// <param name="SemanticIndexKey">The semantic index key used for operator diagnostics.</param>
public sealed record ConsistencySemanticDetail(
    int EmbeddingDimensions,
    [property: JsonPropertyName("vectorHashKey")] string SemanticIndexKey);
