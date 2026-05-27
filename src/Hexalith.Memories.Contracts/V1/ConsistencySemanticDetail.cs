// <copyright file="ConsistencySemanticDetail.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>
/// Details extracted from the semantic <c>{tenantId}:vec:{id}</c> Redis hash.
/// </summary>
/// <param name="EmbeddingDimensions">Number of float dimensions in the stored vector (bytes / 4).</param>
/// <param name="VectorHashKey">The Redis key of the vector hash (for operator debugging).</param>
public sealed record ConsistencySemanticDetail(
    int EmbeddingDimensions,
    string VectorHashKey);
