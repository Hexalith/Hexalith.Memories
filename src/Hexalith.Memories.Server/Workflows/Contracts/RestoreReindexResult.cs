// <copyright file="RestoreReindexResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Workflows.Contracts;

/// <summary>Result of <c>RestoreReindexUnitActivity</c>.</summary>
/// <param name="MemoryUnitId">The re-indexed memory unit.</param>
/// <param name="ChunkCount">The number of semantic chunk vectors written (<c>{tenantId}:vec:{id}:{sequence}</c>).</param>
public sealed record RestoreReindexResult(
    string MemoryUnitId,
    int ChunkCount);
