// <copyright file="ConsistencyResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Indexing;

/// <summary>Result of consistency verification across all three backends.</summary>
/// <param name="SyntacticExists">Whether the memory unit exists in RediSearch.</param>
/// <param name="SemanticExists">Whether the memory unit exists in Redis Vector.</param>
/// <param name="GraphExists">Whether the memory unit exists in FalkorDB.</param>
public sealed record ConsistencyResult(
    bool SyntacticExists,
    bool SemanticExists,
    bool GraphExists);
