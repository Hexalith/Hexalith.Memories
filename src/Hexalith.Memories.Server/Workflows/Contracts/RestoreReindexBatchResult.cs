// <copyright file="RestoreReindexBatchResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Bounded re-index page result.</summary>
/// <param name="ProcessedMemoryUnits">Memory units successfully re-indexed.</param>
/// <param name="SemanticChunkCount">Semantic chunks written across the page.</param>
public sealed record RestoreReindexBatchResult(int ProcessedMemoryUnits, int SemanticChunkCount);
