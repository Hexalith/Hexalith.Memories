// <copyright file="DedupKeySaveResult.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Server.Activities.Ingestion;

/// <summary>Result for a permanent dedup key save attempt.</summary>
/// <param name="Status">The save outcome.</param>
/// <param name="MemoryUnitId">The memory unit id associated with the dedup key.</param>
public sealed record DedupKeySaveResult(DedupKeySaveStatus Status, string MemoryUnitId)
{
    /// <summary>Create a saved result for the supplied memory unit id.</summary>
    /// <param name="memoryUnitId">The memory unit id written by this workflow.</param>
    /// <returns>A saved result.</returns>
    public static DedupKeySaveResult Saved(string memoryUnitId)
        => new(DedupKeySaveStatus.Saved, memoryUnitId);

    /// <summary>Create a duplicate-existing result for the supplied winner memory unit id.</summary>
    /// <param name="existingMemoryUnitId">The existing winner memory unit id.</param>
    /// <returns>A duplicate-existing result.</returns>
    public static DedupKeySaveResult DuplicateExisting(string existingMemoryUnitId)
        => new(DedupKeySaveStatus.DuplicateExisting, existingMemoryUnitId);

    /// <summary>Gets a value indicating whether this workflow wrote the dedup key.</summary>
    public bool IsSaved => Status == DedupKeySaveStatus.Saved;
}
