// <copyright file="IngestionResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Contracts.V1;

/// <summary>Result of the ingestion workflow indicating outcome and provenance.</summary>
/// <param name="MemoryUnitId">The unique identifier for the memory unit.</param>
/// <param name="Status">The final status of the memory unit after ingestion.</param>
/// <param name="IngestedAt">The timestamp when ingestion completed.</param>
/// <param name="WasDuplicate">Whether the source was already ingested (dedup hit).</param>
/// <param name="ConsistencyNote">Non-null if any backend was missing after indexing.</param>
public sealed record IngestionResult(
    string MemoryUnitId,
    MemoryUnitStatus Status,
    DateTimeOffset IngestedAt,
    bool WasDuplicate,
    string? ConsistencyNote);
